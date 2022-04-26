//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/resource/resource_manager.h"

namespace pr::rdr12
{
	Texture2D::Texture2D(ResourceManager& mgr, RdrId id, ID3D12Resource* res, RdrId uri, bool has_alpha, char const* name)
		:TextureBase(mgr, id, res, uri, name)
		,m_t2s(m4x4::Identity())
		,m_has_alpha(has_alpha)
	{}
}