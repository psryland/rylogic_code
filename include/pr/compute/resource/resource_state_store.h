//*********************************************
// Compute
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/compute/forward.h"
#include "pr/compute/resource/resource_state.h"
#include "pr/compute/utility/utility.h"

namespace pr::compute
{
	// The resources used by one command list recording, and the state of each according to that recording.
	struct ResStateStore
	{
		// Notes:
		//  - Resources need to be tracked per command list because command lists can be built in
		//    parallel. This means there isn't a 'current' state for a resource at any particular
		//    moment in time.
		//  - Entries are kept sorted by resource pointer in a flat vector. 'Reset' keeps the capacity,
		//    so a command list that records similar work each step does not allocate after warm-up.

		struct Entry
		{
			ID3D12Resource const* m_res;
			ResStateData m_state;
		};
		using Store = std::vector<Entry>;

	private:

		Store m_states;

	public:

		// The tracked resources, in pointer order
		std::span<Entry const> States() const
		{
			return m_states;
		}

		// Find the tracked state of 'resource'. Returns null if 'resource' is not tracked.
		ResStateData const* Find(ID3D12Resource const* resource) const
		{
			// Binary search the sorted entries
			auto iter = LowerBound(resource);
			return iter != m_states.end() && iter->m_res == resource ? &iter->m_state : nullptr;
		}

		// Start tracking 'resource' with its default state. Returns true if 'resource' was not already tracked.
		bool Track(ID3D12Resource const* resource)
		{
			// Insert at the sorted position. New resources start in their default state.
			Check(resource != nullptr, "Resource is null");
			auto iter = LowerBound(resource);
			if (iter != m_states.end() && iter->m_res == resource)
				return false;

			m_states.insert(iter, Entry{ .m_res = resource, .m_state = ResStateData(DefaultResState(resource)) });
			return true;
		}

		// Access the state of a tracked resource
		ResStateData& Get(ID3D12Resource const* resource)
		{
			// The caller must have tracked 'resource' first
			auto iter = LowerBound(resource);
			Check(iter != m_states.end() && iter->m_res == resource, "Resource is not tracked");
			return iter->m_state;
		}

		// Stop tracking all resources, keeping the capacity for the next recording
		void Reset()
		{
			m_states.clear();
		}

	private:

		// Find the first entry with a resource pointer not less than 'resource'
		Store::const_iterator LowerBound(ID3D12Resource const* resource) const
		{
			return std::lower_bound(m_states.begin(), m_states.end(), resource, [](Entry const& entry, ID3D12Resource const* res) { return entry.m_res < res; });
		}
		Store::iterator LowerBound(ID3D12Resource const* resource)
		{
			return std::lower_bound(m_states.begin(), m_states.end(), resource, [](Entry const& entry, ID3D12Resource const* res) { return entry.m_res < res; });
		}
	};
}
