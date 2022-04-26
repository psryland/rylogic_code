//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_forward.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	Forward::Forward(ResourceManager& mgr, GpuSync& gsync)
		:Shader(mgr, gsync, 1024ULL*1024ULL, ShaderCode
		{
			.VS = shader_code::forward_vs,
			.PS = shader_code::forward_ps,
			.GS = shader_code::none,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		})
	{}

	// Add shader constants to an upload buffer
	D3D12_GPU_VIRTUAL_ADDRESS Forward::Set(fwd::CBufFrame const& cbuf, bool might_reuse)
	{
		return m_cbuf.Add(cbuf, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, might_reuse);
	}
	D3D12_GPU_VIRTUAL_ADDRESS Forward::Set(fwd::CBufNugget const& cbuf, bool might_reuse)
	{
		return m_cbuf.Add(cbuf, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, might_reuse);
	}
}

