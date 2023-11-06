//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/render/drawlist_element.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/instance/instance.h"
#include "view3d-12/src/utility/root_signature.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace fwd;

	Forward::Forward(ID3D12Device* device)
		:Shader()
	{
		Code = ShaderCode
		{
			.VS = shader_code::forward_vs,
			.PS = shader_code::forward_ps,
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
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
			//D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
			D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS	|
			D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
			D3D12_ROOT_SIGNATURE_FLAG_NONE;

		// Register mappings
		root_sig.CBuf(ERootParam::CBufFrame, ECBufReg::b0);
		root_sig.CBuf(ERootParam::CBufNugget, ECBufReg::b1);
		root_sig.CBuf(ERootParam::CBufFade, ECBufReg::b2);
		root_sig.CBuf(ERootParam::CBufScreenSpace, ECBufReg::b3);
		root_sig.CBuf(ERootParam::CBufDiag, ECBufReg::b3); // Uses the same reg as ScreenSpace
		root_sig.Tex(ERootParam::DiffTexture, ETexReg::t0);
		root_sig.Tex(ERootParam::EnvMap, ETexReg::t1);
		root_sig.Tex(ERootParam::SMap, ETexReg::t2, shaders::MaxShadowMaps);
		root_sig.Tex(ERootParam::ProjTex, ETexReg::t3, shaders::MaxProjectedTextures);
		root_sig.Samp(ERootParam::DiffTextureSampler, ESamReg::s0, shaders::MaxSamplers);

		// Add stock static samplers
		root_sig.Samp(ESampParam::EnvMap, SamDescStatic(ESamReg::s1));
		root_sig.Samp(ESampParam::SMap, SamDescStatic(ESamReg::s2));
		root_sig.Samp(ESampParam::ProjTex, SamDescStatic(ESamReg::s3));
		root_sig.Samp(ESampParam::PointClamp, SamDescStatic(ESamReg::s4));
		root_sig.Samp(ESampParam::LinearWrap, SamDescStatic(ESamReg::s5));

		Signature = root_sig.Create(device);
	}

	// Config the shader
	void Forward::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, Scene const& scene, DrawListElement const* dle)
	{
		// Set the frame constants
		if (dle == nullptr)
		{
			CBufFrame cb0 = {};
			SetViewConstants(cb0.m_cam, scene.m_cam);
			SetLightingConstants(cb0.m_global_light, scene.m_global_light, scene.m_cam);
			//todo SetShadowMapConstants(cb0.m_shadow, smap_rstep);
			//todo SetEnvMapConstants(cb0.m_env_map, scene.m_global_envmap.get());
			auto gpu_address = cbuf.Add(cb0, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufFrame, gpu_address);
		}
		// Set the per-element constants
		else
		{
			auto& inst = *dle->m_instance;
			auto& nug = *dle->m_nugget;

			CBufNugget cb1 = {};
			SetModelFlags(cb1, inst, nug, scene);
			SetTxfm(cb1, inst, scene.m_cam);
			SetTint(cb1, inst, nug);
			SetEnvMap(cb1, inst, nug);
			SetTexDiffuse(cb1, nug);
			auto gpu_address = cbuf.Add(cb1, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufNugget, gpu_address);
		}
	}
}

