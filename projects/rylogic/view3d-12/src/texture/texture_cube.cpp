//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_cube.h"
#include "pr/view3d-12/resource/resource_manager.h"

namespace pr::rdr12
{
	TextureCube::TextureCube(ResourceManager& mgr, ID3D12Resource* res, TextureDesc const& desc)
		:TextureBase(mgr, res, desc, D3D12_SRV_DIMENSION_TEXTURECUBE)
		,m_cube2w(m4x4::Identity())
	{}
}