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
		using ClrValue = std::optional<D3D12_CLEAR_VALUE>;

		RdrId    m_id;          // The id to assign to the created texture instance. Use 'AutoId' to auto generate an id.
		ResDesc  m_tdesc;       // A description of the texture to be created.
		RdrId    m_uri;         // An id for the source of this texture
		bool     m_has_alpha;   // True if the texture contains alpha pixels and should be rendered in the alpha group
		ClrValue m_clear_value; // Value to clear to (for RTV, or DSV), otherwise nullptr
		string32 m_name;        // Debugging name for the texture.

		TextureDesc()
			:m_id()
			,m_tdesc()
			,m_uri()
			,m_has_alpha()
			,m_clear_value()
			,m_name()
		{}

		TextureDesc(RdrId id, ResDesc const& td, bool has_alpha = false, RdrId uri = 0, char const* name = "")
			:m_id(id)
			,m_tdesc(td)
			,m_uri(uri)
			,m_has_alpha(has_alpha)
			,m_clear_value()
			,m_name(name)
		{}
	};
}
