//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_smap.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/render/drawlist_element.h"
#include "pr/view3d-12/utility/shadow_caster.h"
//#include "pr/view3d-12/model/nugget.h"
//#include "pr/view3d-12/instance/instance.h"
#include "view3d-12/src/utility/root_signature.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace smap;

	ShadowMap::ShadowMap(ID3D12Device* device)
		:Shader()
	{
		m_code = ShaderCode
		{
			.VS = shader_code::shadow_map_vs,
			.PS = shader_code::shadow_map_ps,
			.DS = shader_code::none,
			.HS = shader_code::none,
			.GS = shader_code::none,
			.CS = shader_code::none,
		};
		
		// Create the root signature
		RootSig<ERootParam, ESampParam> root_sig;
		root_sig.Flags =
			D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
			D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
			D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
			D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS	|
			D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
			D3D12_ROOT_SIGNATURE_FLAG_NONE;

		// Register mappings
		root_sig.CBuf(ERootParam::CBufFrame, ECBufReg::b0);
		root_sig.CBuf(ERootParam::CBufNugget, ECBufReg::b1);
		root_sig.Tex(ERootParam::DiffTexture, ETexReg::t0);
		root_sig.Samp(ERootParam::DiffTextureSampler, ESamReg::s0);

		m_signature = root_sig.Create(device);
	}

	// Config the shader
	void ShadowMap::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, ShadowCaster const& caster, DrawListElement const* dle)
	{
		// Set the frame constants
		if (dle == nullptr)
		{
			CBufFrame cb0 = {};
			cb0.m_w2l = caster.m_params.m_w2ls;
			cb0.m_l2s = caster.m_params.m_ls2s;
			auto gpu_address = cbuf.Add(cb0, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufFrame, gpu_address);
		}
		// Set the per-element constants
		else
		{
			auto& inst = *dle->m_instance;
			auto& nug = *dle->m_nugget;

			CBufNugget cb1 = {};
			SetModelFlags(cb1, inst, nug, false);
			SetTxfm(cb1, inst, *caster.m_scene_cam);
			SetTint(cb1, inst, nug);
			SetTexDiffuse(cb1, nug);
			auto gpu_address = cbuf.Add(cb1, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufNugget, gpu_address);
		}
	}
}