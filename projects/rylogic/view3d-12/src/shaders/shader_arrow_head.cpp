//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_arrow_head.h"
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace fwd;

	ArrowHeadGS::ArrowHeadGS(float size)
		:Shader()
		,m_size(size)
	{
		m_code = ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
			.GS = shader_code::arrow_head_gs,
			.CS = shader_code::none,
		};
	}
	void ArrowHeadGS::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const& scene, DrawListElement const*)
	{
		CBufScreenSpace cb = {};
		cb.m_size = v2(m_size, m_size);
		cb.m_screen_dim = To<v2>(scene.wnd().BackBufferSize());
		cb.m_depth = false;
		auto gpu_address = cbuf.Add(cb, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufScreenSpace, gpu_address);
	}
}
