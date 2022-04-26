//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_point_sprites.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	PointSpriteGS::PointSpriteGS(ResourceManager& mgr, GpuSync& gsync)
		:Shader(mgr, gsync, 128 * 1024ULL, ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.GS = shader_code::point_sprites_gs,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		})
		, m_size(10.f, 10.f)
		, m_depth(true)
	{}

	// Add shader constants to an upload buffer
	D3D12_GPU_VIRTUAL_ADDRESS PointSpriteGS::Set(ss::CBufFrame const& cbuf, bool might_reuse)
	{
		return m_cbuf.Add(cbuf, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, might_reuse);
	}
}
