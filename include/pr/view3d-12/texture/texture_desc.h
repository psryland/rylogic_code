//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/utility/wrappers.h"

namespace pr::rdr12
{
	struct TextureDesc
	{
		RdrId             m_id;          // The id to assign to the created texture instance. Use 'AutoId' to auto generate an id.
		TexDesc           m_tdesc;       // A description of the texture to be created.
		SamDesc           m_sdesc;       // A description of the sampler to use.
		bool              m_has_alpha;   // True if the texture contains alpha pixels and should be rendered in the alpha group
		D3D12_CLEAR_VALUE m_clear_value; // Value to clear to (for RTV, or DSV), otherwise nullptr
		wstring32         m_name;        // Debugging name for the texture.

		TextureDesc()
			:m_id()
			,m_tdesc()
			,m_sdesc()
			,m_has_alpha()
			,m_clear_value()
			,m_name()
		{}

		TextureDesc(RdrId id, TexDesc const& td, SamDesc const& sd = SamDesc::LinearClamp(), bool has_alpha = false, wchar_t const* name = L"")
			:m_id(id)
			,m_tdesc(td)
			,m_sdesc(sd)
			,m_has_alpha(has_alpha)
			,m_clear_value()
			,m_name(name)
		{}
	};
}
