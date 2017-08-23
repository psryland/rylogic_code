//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/steps/dslighting.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/common.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(dslighting_vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(dslighting_ps.h)

		// Deferred lighting vertex shader
		struct DSLightingShaderVS :Shader<ID3D11VertexShader, DSLightingShaderVS>
		{
			typedef Shader<ID3D11VertexShader, DSLightingShaderVS> base;
			DSLightingShaderVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "dslighting_vs.cso"));
			}
		};

		// Deferred lighting pixel shader
		struct DSLightingShaderPS :Shader<ID3D11PixelShader, DSLightingShaderPS>
		{
			typedef Shader<ID3D11PixelShader, DSLightingShaderPS> base;
			D3DPtr<ID3D11SamplerState> m_point_sampler; // A point sampler used to sample the GBuffer

			DSLightingShaderPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
				,m_point_sampler()
			{
				// Create a GBuffer sampler
				Renderer::Lock lock(*m_rdr);
				auto sdesc = SamplerDesc::PointClamp();
				pr::Throw(lock.D3DDevice()->CreateSamplerState(&sdesc, &m_point_sampler.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_point_sampler, "dslighting point sampler"));

				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "dslighting_ps.cso"));
			}

			// Set up the shader ready to be used on 'dle'
			// Note, shaders are set/cleared by the state stack.
			// Only per-model constants, textures, and samplers need to be set here.
			void Setup(ID3D11DeviceContext* dc, DeviceState& state) override
			{
				base::Setup(dc, state);

				// Get the GBuffer render step and bind the GBuffer render targets to the PS
				auto& gbuffer = state.m_rstep->as<DSLighting>().m_gbuffer;
				dc->PSSetShaderResources(0, GBuffer::RTCount, (ID3D11ShaderResourceView* const*)gbuffer.m_srv);
				dc->PSSetSamplers(0, 1, &m_point_sampler.m_ptr);
			}

			// Undo any changes made by this shader on the DC
			// Note, shaders are set/cleared by the state stack.
			// This method is only needed to clear texture/sampler slots
			void Cleanup(ID3D11DeviceContext* dc) override
			{
				ID3D11ShaderResourceView* null_srv[GBuffer::RTCount] = {};
				dc->PSSetShaderResources(0, GBuffer::RTCount, null_srv);

				ID3D11SamplerState* null_samp[1] = {};
				dc->PSSetSamplers(0, 1, null_samp);
			}
		};

		// Create this shader
		template <> void ShaderManager::CreateShader<DSLightingShaderVS>()
		{
			VShaderDesc desc(dslighting_vs, Vert());
			auto dx = GetVS(EStockShader::DSLightingVS, &desc);
			CreateShader<DSLightingShaderVS>(EStockShader::DSLightingVS, dx, "dslighting_vs");
		}
		template <> void ShaderManager::CreateShader<DSLightingShaderPS>()
		{
			PShaderDesc desc(dslighting_ps);
			auto dx = GetPS(EStockShader::DSLightingPS, &desc);
			CreateShader<DSLightingShaderPS>(EStockShader::DSLightingPS, dx, "dslighting_ps");
		}
	}
}
