//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_smap.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/render/drawlist_element.h"
#include "pr/view3d-12/utility/shadow_caster.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace smap;

	struct EReg
	{
		inline static constexpr auto CBufFrame = ECBufReg::b0;
		inline static constexpr auto CBufNugget = ECBufReg::b1;
		inline static constexpr auto DiffTexture = ESRVReg::t0;
		inline static constexpr auto DiffTextureSampler = ESamReg::s0;
	};

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
		m_signature = RootSig(ERootSigFlags::VertGeomPixelOnly)
			.CBuf(EReg::CBufFrame)
			.CBuf(EReg::CBufNugget)
			.SRV(EReg::DiffTexture, 1)
			.Samp(EReg::DiffTextureSampler, 1)
			.Create(device, "ShadowMapSig");
	}

	// Config the shader
	void ShadowMap::Setup(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& cbuf, DrawListElement const* dle, ShadowCaster const& caster, SceneCamera const& cam)
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
			SetFlags(cb1, inst, nug, false);
			SetTxfm(cb1, inst, nug.m_model, cam); // todo frame constant?
			SetTint(cb1, inst, nug);
			SetTex2Surf(cb1, inst, nug);
			auto gpu_address = cbuf.Add(cb1, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufNugget, gpu_address);
		}
	}
}