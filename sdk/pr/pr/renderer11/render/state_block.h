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

			mutable pr::hash::HashValue m_crc;
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
				for (int i = 0; i != N; ++i)
				{
					if (rhs.m_mask[i] == 0) continue;
					if (m_mask[i] == 0)
						Desc() = rhs.Desc();
					else
						merge(rhs, m_mask[i], i);
					m_crc = 0;
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

			// Get/Create a blend state for 'desc'
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
	}
}

#endif
