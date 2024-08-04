//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/render/drawlist_element.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace fwd;

	struct EReg
	{
		inline static constexpr auto CBufFrame = ECBufReg::b0;
		inline static constexpr auto CBufNugget = ECBufReg::b1;
		inline static constexpr auto CBufFade = ECBufReg::b2;
		inline static constexpr auto CBufScreenSpace = ECBufReg::b3;
		inline static constexpr auto CBufDiag = ECBufReg::b3; // Uses the same reg as ScreenSpace

		inline static constexpr auto DiffTexture = ETexReg::t0;
		inline static constexpr auto EnvMap = ETexReg::t1;
		inline static constexpr auto SMap = ETexReg::t2;
		inline static constexpr auto ProjTex = ETexReg::t3;

		inline static constexpr auto DiffTextureSampler = ESamReg::s0;
	};
	struct ESamp
	{
		inline static constexpr auto EnvMap = SamDescStatic(ESamReg::s1);
		inline static constexpr auto SMap = SamDescStatic(ESamReg::s2).addr(D3D12_TEXTURE_ADDRESS_MODE_CLAMP).filter(D3D12_FILTER_COMPARISON_MIN_MAG_LINEAR_MIP_POINT).compare(D3D12_COMPARISON_FUNC_GREATER_EQUAL);
		inline static constexpr auto ProjTex = SamDescStatic(ESamReg::s3);
	};

	Forward::Forward(ID3D12Device* device)
		:Shader()
	{
		m_code = ShaderCode
		{
			.VS = shader_code::forward_vs,
			.PS = shader_code::forward_ps,
			.DS = shader_code::none,
			.HS = shader_code::none,
			.GS = shader_code::none,
			.CS = shader_code::none,
		};
		
		// Create the root signature
		m_signature = RootSig(ERootSigFlags::GraphicsOnly)

			// Register mappings
			.CBuf(EReg::CBufFrame)
			.CBuf(EReg::CBufNugget)
			.CBuf(EReg::CBufFade)
			.CBuf(EReg::CBufScreenSpace) // Shared to CBufDiag
			.Tex(EReg::DiffTexture, 1)
			.Tex(EReg::EnvMap, 1)
			.Tex(EReg::SMap, shaders::MaxShadowMaps)
			.Tex(EReg::ProjTex, shaders::MaxProjectedTextures)
			.Samp(EReg::DiffTextureSampler, shaders::MaxSamplers)

			// Add stock static samplers
			.Samp(ESamp::EnvMap)
			.Samp(ESamp::SMap)
			.Samp(ESamp::ProjTex)

			.Create(device);
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
			SetShadowMapConstants(cb0.m_shadow, scene.FindRStep<RenderSmap>());
			SetEnvMapConstants(cb0.m_env_map, scene.m_global_envmap.get());
			auto gpu_address = cbuf.Add(cb0, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufFrame, gpu_address);
		}
		// Set the per-element constants
		else
		{
			auto& inst = *dle->m_instance;
			auto& nug = *dle->m_nugget;

			CBufNugget cb1 = {};
			SetFlags(cb1, inst, nug, scene.m_global_envmap != nullptr);
			SetTxfm(cb1, inst, scene.m_cam);
			SetTint(cb1, inst, nug);
			SetTex2Surf(cb1, inst, nug);
			SetReflectivity(cb1, inst, nug);
			auto gpu_address = cbuf.Add(cb1, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
			cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufNugget, gpu_address);
		}
	}
}

