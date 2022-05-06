//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_thick_line.h"
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace fwd;

	ThickLineStripGS::ThickLineStripGS(float width)
		:Shader()
		,m_width(width)
	{
		Code = ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.GS = shader_code::thick_line_strip_gs,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		};
	}
	void ThickLineStripGS::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const&, DrawListElement const*)
	{
		CBufScreenSpace cb = {};
		cb.m_screen_dim = v2(m_width, m_width);
		cb.m_depth = false;
		auto gpu_address = cbuf.Add(cb, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufScreenSpace, gpu_address);
	}

	ThickLineListGS::ThickLineListGS(float width)
		:Shader()
		,m_width(width)
	{
		Code = ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.GS = shader_code::thick_line_list_gs,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		};
	}
	void ThickLineListGS::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const&, DrawListElement const*)
	{
		CBufScreenSpace cb = {};
		cb.m_screen_dim = v2(m_width, m_width);
		cb.m_depth = false;
		auto gpu_address = cbuf.Add(cb, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufScreenSpace, gpu_address);
	}
}
