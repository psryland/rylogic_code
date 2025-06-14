//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/resource_state.h"
#include "pr/view3d-12/utility/lookup.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	struct ResStateStore
	{
		// Notes:
		//  - Resources need to be tracked per command list because command lists can be built in
		//    parallel. This means there isn't a 'current' state for a resource at any particular
		//    moment in time.

	private:

		using Store = Lookup<ID3D12Resource const*, ResStateData>;
		Store m_states;

	public:

		Store const& States() const { return m_states; }
		ResStateData const& Get(ID3D12Resource const* resource) const
		{
			Check(resource != nullptr, "Resource is null");
			return m_states.at(resource);
		}
		ResStateData& Get(ID3D12Resource const* resource)
		{
			Check(resource != nullptr, "Resource is null");
			auto iter = m_states.find(resource);
			if (iter == end(m_states))
			{
				auto state = ResStateData(DefaultResState(resource));
				iter = m_states.emplace(resource, state).first;
			}
			return iter->second;
		}
		void Forget(ID3D12Resource const* resource)
		{
			m_states.erase(resource);
		}
		void Reset()
		{
			m_states.clear();
		}
	};
}
