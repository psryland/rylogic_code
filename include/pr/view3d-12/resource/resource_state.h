//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct ResState
	{
	private:

		// Notes:
		//  - This object is stored in the PrivateData of a resource. It keeps track
		//    of the resource state for each sub resource (i.e. mip level).
		//  - The 'm_state' is stored as a value type in the PrivateData because there
		//    is no convenient way to delete allocated private data when the resource is released.
		//  - 'm_state[0]' is the state for the 'AllSubresources' special case.
		//  - 'm_state[1:]' is flat-map of mip-to-state. mip0 == [1], mip1 == [2], etc
		//  - States are encoded as [state:24, subresource:8] since resource states have
		//    a max value of 0x80_0000, and textures won't have more than 0xFF mips.
		//  - Only m_state[>0].index() == 0 is a sentinel value indicating the end of the list.
		// 
		// WARNING: Multiple instances of this object for the same resource will data-race.

		inline static GUID const Guid_ResourceStates = { 0x5DFA5A73, 0xA8A0, 0x466B, { 0xA1, 0x0A, 0x3E, 0x3A, 0x35, 0x87, 0x5B, 0xB3 } };
		inline static constexpr uint32_t StateMask = 0x00FFFFFF;
		inline static constexpr uint32_t IndexMask = 0x000000FF;
		inline static constexpr uint32_t IndexBits = 8;
		struct MipState
		{
			// Encoded as [state:24, subresource:8] 
			uint32_t m_data;

			// Get/Set the index that 
			int sub() const
			{
				return int(m_data & IndexMask) - 1;
			}
			void sub(int s)
			{
				assert(s < int(IndexMask));
				auto idx = s_cast<int>((s + 1) & IndexMask);
				m_data = SetBits(m_data, int(IndexMask), idx);
				assert(sub() == s);
			}

			// Get/Set the resource state
			D3D12_RESOURCE_STATES state() const
			{
				return s_cast<D3D12_RESOURCE_STATES>(m_data >> IndexBits);
			}
			void state(D3D12_RESOURCE_STATES s)
			{
				assert(s_cast<int>(s & StateMask) == s);
				m_data = SetBits(m_data, StateMask << IndexBits, s_cast<uint32_t>(s) << IndexBits);
				assert(state() == s);
			}
		};

		ID3D12Resource* m_res;
		MipState m_state[8]; // last value is a sentinel

	public:

		explicit ResState(ID3D12Resource* res)
			:m_res(res)
		{
			Read();
		}
		explicit ResState(ID3D12Resource const* res)
			:ResState(const_cast<ID3D12Resource*>(res))
		{}

		// Read state data from the resource private data
		void Read()
		{
			// Try to read the states from the private data. If that fails, write default values
			UINT size = sizeof(m_state);
			if (Failed(m_res->GetPrivateData(Guid_ResourceStates, &size, &m_state)))
				Apply(D3D12_RESOURCE_STATE_COMMON);
		}

		// Return the default state
		D3D12_RESOURCE_STATES DefaultState() const
		{
			return m_state[0].state();
		}

		// True if not all mips have the same (default) state
		bool HasMipSpecificStates() const
		{
			// This means there is at least one mip-specific state.
			return m_state[1].sub() != D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
		}

		// Return the state of subresource 'sub'.
		D3D12_RESOURCE_STATES operator[](int sub) const
		{
			// Convert 'sub'([-1,N]) to an index in 'm_state'
			int idx = mip_state_index(sub);

			// 'idx' shouldn't equal the sentinel. This would mean
			// 'm_state' is full which should be prevented in 'Apply'.
			assert(idx != _countof(m_state) - 1);

			// If 'sub' == ALL(-1), then the caller is expecting all
			// subresources to have the same state. Throw if this isn't true.
			if (sub == D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES && HasMipSpecificStates())
				throw std::runtime_error("Subresources are not all the same state");

			// Return the state for 'sub'. This might be the default (m_state[0]).
			return m_state[idx].state();
		}

		// Set subresource 'sub' to 'state'.
		void Apply(D3D12_RESOURCE_STATES state, int sub = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES)
		{
			// If 'sub' == ALL, set all states to 'state'.
			if (sub == D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES)
			{
				memset(&m_state[0], 0, sizeof(m_state));
				m_state[0].state(state);
			}
			
			// Otherwise, find the mip-specific state
			else
			{
				int idx = mip_state_index(sub);

				// If 'idx != 0' then a mip-specific state was found
				if (idx != 0)
				{
					// If 'state' == the default state, remove 'idx'
					if (state == DefaultState())
					{
						for (auto i = idx; i != _countof(m_state) - 1; ++i)
							m_state[i] = m_state[i+1];
					}
					else // Otherwise, update 'idx'
					{
						assert(m_state[idx].sub() == sub);
						m_state[idx].state(state);
					}
				}

				// If 'idx == 0' then there is no mip-speicifc state yet
				else
				{
					// If 'state' == the default state, no need to add a mip-specific state
					if (state != DefaultState())
					{
						// Find the next free slot. If 'idx' is the sentinel,
						// then 'm_state' is full. It might need enlarging.
						auto free = mip_state_count();
						if (free == _countof(m_state) - 1)
							throw std::runtime_error("Too many unique mip states");

						m_state[free].sub(sub);
						m_state[free].state(state);
					}
				}
			}

			// Write the resource state data
			Throw(m_res->SetPrivateData(Guid_ResourceStates, sizeof(m_state), &m_state));
			assert((*this)[sub] == state);
		}

		// Return the mip-specific states
		template <std::invocable<int, D3D12_RESOURCE_STATES> CB> void EnumMipSpecificStates(CB cb) const
		{
			for (int i = 1; m_state[i].sub() != D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES; ++i)
				cb(m_state[i].sub(), m_state[i].state());
		}

		// True if the resource state for all mips equals 'rhs'
		friend bool operator == (ResState const& lhs, D3D12_RESOURCE_STATES rhs)
		{
			// The default state == 'rhs' and there are no mip-specific states
			return lhs.m_state[0].state() == rhs && !lhs.HasMipSpecificStates();
		}
		friend bool operator != (ResState const& lhs, D3D12_RESOURCE_STATES rhs)
		{
			return !(lhs == rhs);
		}

	private:

		// Find the index in 'm_state' for 'sub'
		int mip_state_index(int sub) const
		{
			// 'sub' == -1(ALL) => 'idx' == 0
			// 'sub' == 0(mip0) => 'idx' == 1
			// 'sub' == 1(mip1) => 'idx' == 2 etc...
			if (sub == int(D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES))
				return 0;

			for (int i = 1; i != _countof(m_state); ++i)
			{
				// Found the state for 'sub'? Return its index
				if (m_state[i].sub() == sub)
					return i;

				// Found the sentinel?End of the list? No mip-specific state found, return the index for the default state
				if (m_state[i].sub() == D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES)
					return 0;
			}
			throw std::runtime_error("The sentinel has been overwritten");
		}

		// Returns the index of the first free slot
		int mip_state_count() const
		{
			int i = 1; // [0] is the default state and is never free
			for (; i != _countof(m_state) && m_state[i].sub() != D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES; ++i) {}
			return i;
		}
	};
}
