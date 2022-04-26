//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/resource/resource_manager.h"

namespace pr::rdr12
{
	TextureCube::TextureCube(ResourceManager& mgr, RdrId id, ID3D12Resource* res, RdrId uri, char const* name)
		:TextureBase(mgr, id, res, uri, name)
		,m_cube2w(m4x4::Identity())
	{}
}