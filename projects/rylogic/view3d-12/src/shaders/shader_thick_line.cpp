﻿//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_thick_line.h"
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	ThickLineStripGS::ThickLineStripGS(float width)
		:ShaderOverride()
		,m_width(width)
	{
		m_code = ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
			.GS = shader_code::thick_line_strip_gs,
			.CS = shader_code::none,
		};
	}
	void ThickLineStripGS::SetupOverride(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, Scene const& scene, DrawListElement const*)
	{
		fwd::CBufScreenSpace cb = {};
		cb.m_size = v2(m_width, m_width);
		cb.m_screen_dim = To<v2>(scene.wnd().BackBufferSize());
		cb.m_depth = false;
		auto gpu_address = upload.Add(cb, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)fwd::ERootParam::CBufScreenSpace, gpu_address);
	}

	ThickLineListGS::ThickLineListGS(float width)
		:ShaderOverride()
		,m_width(width)
	{
		m_code = ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
			.GS = shader_code::thick_line_list_gs,
			.CS = shader_code::none,
		};
	}
	void ThickLineListGS::SetupOverride(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, Scene const& scene, DrawListElement const*)
	{
		fwd::CBufScreenSpace cb = {};
		cb.m_size = v2(m_width, m_width);
		cb.m_screen_dim = To<v2>(scene.wnd().BackBufferSize());
		cb.m_depth = false;
		auto gpu_address = upload.Add(cb, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)fwd::ERootParam::CBufScreenSpace, gpu_address);
	}
}
