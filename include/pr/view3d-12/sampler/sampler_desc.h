//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	struct SamplerDesc
	{
		using ClrValue = std::optional<D3D12_CLEAR_VALUE>;

		RdrId    m_id;          // The id to assign to the created sampler instance. Use 'AutoId' to auto generate an id.
		SamDesc  m_sdesc;       // A description of the sampler to be created.
		string32 m_name;        // Debugging name for the sampler.

		SamplerDesc()
			:m_id()
			,m_sdesc()
			,m_name()
		{}

		SamplerDesc(RdrId id, SamDesc const& td, char const* name = "")
			:m_id(id)
			,m_sdesc(td)
			,m_name(name)
		{}
	};
}
