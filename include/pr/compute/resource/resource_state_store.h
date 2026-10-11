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
		//  - Lookups are constant time because every resource bind and barrier looks up its resource.
		//    'Reset' keeps the capacity, so a command list that records similar work each step does not allocate after warm-up.

		using Store = vector_hash_map<ID3D12Resource const*, ResStateData>;

	private:

		Store m_states;

	public:

		// The tracked resources and their states, in the order they were first tracked
		Store const& States() const
		{
			return m_states;
		}

		// Find the tracked state of 'resource'. Returns null if 'resource' is not tracked.
		ResStateData const* Find(ID3D12Resource const* resource) const
		{
			auto iter = m_states.find(resource);
			return iter != m_states.end() ? &iter->second : nullptr;
		}

		// Start tracking 'resource' with its default state. Returns true if 'resource' was not already tracked.
		bool Track(ID3D12Resource const* resource)
		{
			// Look up before inserting so that the default state is only read for new resources
			PR_ASSERT(PR_DBG_RDR, resource != nullptr, "Resource is null");
			if (m_states.contains(resource))
				return false;

			// New resources start in their default state
			m_states.try_emplace(resource, DefaultResState(resource));
			return true;
		}

		// Access the state of a tracked resource. The caller must have tracked 'resource' first.
		ResStateData& Get(ID3D12Resource const* resource)
		{
			auto iter = m_states.find(resource);
			PR_ASSERT(PR_DBG_RDR, iter != m_states.end(), "Resource is not tracked");
			return iter->second;
		}

		// Stop tracking all resources, keeping the capacity for the next recording
		void Reset()
		{
			m_states.clear();
		}
	};
}
