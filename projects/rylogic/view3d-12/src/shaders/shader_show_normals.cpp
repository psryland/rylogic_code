//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_show_normals.h"
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace fwd;

	ShowNormalsGS::ShowNormalsGS()
		:Shader()
	{
		Code = ShaderCode
		{
			.VS = shader_code::none,
			.PS = shader_code::none,
			.GS = shader_code::show_normals_gs,
			.CS = shader_code::none,
			.DS = shader_code::none,
			.HS = shader_code::none,
		};
	}
	void ShowNormalsGS::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const& scene, DrawListElement const* dle)
	{
		if (dle != nullptr)
		{
			auto& diag = scene.wnd().m_diag;

			CBufDiag cb = {
				.m_colour = Colour(diag.m_normal_colour).rgba,
				.m_length = diag.m_normal_lengths,
			};
			auto gpu_address = cbuf.Add(cb, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufScreenSpace, gpu_address);
		}
	}
}
