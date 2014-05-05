//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_STATE_BLOCK_H
#define PR_RDR_RENDER_STATE_BLOCK_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		// A template base class instance of a state block
		template <typename TStateDesc, typename TFieldEnum, size_t N>
		struct StateBlock :TStateDesc
		{
			typedef StateBlock<TStateDesc, TFieldEnum, N> base;

			// 'TStateDesc' is a dx structure containing sets of render states such as 'D3D11_DEPTH_STENCIL_DESC'
			// 'm_mask' is a bit field indicating which members in 'TStateDesc' have had a value set. The reason
			// it is an array is to handle internal arrays in 'TStateDesc'.
			// E.g., say 'TStateDesc' was:
			//  struct SomeStateDesc
			//  {
			//      int awesome;
			//      char weight[3];
			//  };
			//  m_mask[0] would have a bit for 'awesome' and 'weight[0]'
			//  m_mask[1] would have a bit for 'weight[1]' (at the same bit index as weight[0])
			//  m_mask[2] would have a bit for 'weight[2]' (at the same bit index as weight[0])
			// The bit indices in 'm_mask[1..2]' for 'awesome' are not used and should never be set.
			// This way 'm_mask' indicates which members, including those in arrays, have been changed.

			// Cached crc of the state desc
			mutable pr::hash::HashValue m_crc;

			// A bit field of the members in 'TStateDesc' that have had a value set.
			TFieldEnum m_mask[N];

			StateBlock()
				:TStateDesc()
				,m_crc()
				,m_mask()
			{}

			// Returns the description
			TStateDesc const& Desc() const { return *this; }
			TStateDesc& Desc() { return *this; }

			// Return the hash of the data in this state object
			pr::hash::HashValue Hash() const { return m_crc != 0 ? m_crc : (m_crc = pr::hash::HashObj(*this)); }

			// Clear a field in the state description
			void Clear(TFieldEnum field)
			{
				m_mask[0] = pr::SetBits(m_mask[0], field, false);
				m_crc = 0;
			}
			void Clear(TFieldEnum field, int n)
			{
				m_mask[n] = pr::SetBits(m_mask[n], field, false);
				m_crc = 0;
			}

			// Set the value of a field in the state description
			void Set(TFieldEnum field)
			{
				m_mask[0] = pr::SetBits(m_mask[0], field, true);
				m_crc = 0;
			}
			void Set(TFieldEnum field, int n)
			{
				m_mask[n] = pr::SetBits(m_mask[n], field, true);
				m_crc = 0;
			}

			// Combine two states into one. 'rhs' has priority over 'this'
			template <typename MergeFunc>
			void Merge(StateBlock const& rhs, MergeFunc merge)
			{
				// If no values in 'this' have been set, we can just copy 'rhs' wholesale
				TFieldEnum mask = m_mask[0];
				for (int i = 1; i < N; ++i) mask |= m_mask[i];
				if (mask == 0) { *this = rhs; return; }

				// If no values in 'rhs' have been set, we can ignore it
				mask = rhs.m_mask[0];
				for (int i = 1; i < N; ++i) mask |= rhs.m_mask[i];
				if (mask == 0) { return; }

				// Otherwise, we have to through field-by-field copying those
				// that are set in 'rhs' over to 'this'
				for (int i = 0; i != N; ++i)
				{
					for (auto field : pr::EnumerateBits(rhs.m_mask[i]))
					{
						merge((TFieldEnum)field, i, rhs);
						m_crc = 0;
					}
				}
			}
		};

		// Provides a pool of TStateBlock objects
		template <typename TStateBlock, typename TD3DInterface> struct StateManager
		{
			typedef StateManager<TStateBlock, TD3DInterface> base;
			typedef Lookup<pr::hash::HashValue, TD3DInterface*> Lookup;
			D3DPtr<ID3D11Device> m_device;
			Lookup m_lookup;

			StateManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
				:m_device(device)
				,m_lookup(mem)
			{}
			~StateManager()
			{
				Flush(0);
			}

			// Get/Create a state buffer for 'desc'
			template <typename CreateFunc>
			D3DPtr<TD3DInterface> GetState(TStateBlock const& desc, CreateFunc create)
			{
				// Look for a corresponding state object
				auto hash = desc.Hash();
				auto iter = m_lookup.find(hash);
				if (iter == end(m_lookup))
				{
					// If not found, create one
					TD3DInterface* s = create(desc.Desc());
					iter = m_lookup.insert(iter, Lookup::pair(hash, s));
				}
				return D3DPtr<TD3DInterface>(iter->second, true);
			}

			// Called to limit the number of pooled state objects
			// Must be called while no state objects are in use
			void Flush(size_t high_tide = 500)
			{
				// Only flush if we need to
				if (m_lookup.size() <= high_tide)
					return;

				// We could flush down to a low tide mark, but there isn't a sensible way of doing
				// this that doesn't risk leaving some unused states in the pool indefinitely.
				// Just flush all. Remember, 'm_lookup.size()' is the number of *unique*
				// states currently active.
				// Notice, it doesn't actually matter if there are outstanding references to the
				// states being released here. Those states will release when the go out of scope.
				for (auto i = begin(m_lookup); !m_lookup.empty(); i = m_lookup.erase(i))
					i->second->Release();
			}
		};

		// Operators
		template <typename TStateDesc, typename TFieldEnum, size_t N>
		inline bool operator == (StateBlock<TStateDesc, TFieldEnum, N> const& lhs, StateBlock<TStateDesc, TFieldEnum, N> const& rhs)
		{
			return lhs.Hash() == rhs.Hash();
		}
		template <typename TStateDesc, typename TFieldEnum, size_t N>
		inline bool operator != (StateBlock<TStateDesc, TFieldEnum, N> const& lhs, StateBlock<TStateDesc, TFieldEnum, N> const& rhs)
		{
			return !(lhs == rhs);
		}
	}
}

#endif
