//*********************************************
// Renderer
//  Copyright � Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
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
		#include PR_RDR_SHADER_COMPILED_DIR(dslighting.vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(dslighting.ps.h)

		// Deferred lighting vertex shader
		struct DSLightingShaderVS :Shader<ID3D11VertexShader, DSLightingShaderVS>
		{
			typedef Shader<ID3D11VertexShader, DSLightingShaderVS> base;
			DSLightingShaderVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "dslighting.vs.cso"));
			}
		};

		// Deferred lighting pixel shader
		struct DSLightingShaderPS :Shader<ID3D11PixelShader, DSLightingShaderPS>
		{
			typedef Shader<ID3D11PixelShader, DSLightingShaderPS> base;
			D3DPtr<ID3D11SamplerState> m_point_sampler; // A point sampler used to sample the gbuffer

			DSLightingShaderPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
				,m_point_sampler()
			{
				// Create a gbuffer sampler
				auto sdesc = SamplerDesc::PointClamp();
				pr::Throw(mgr->m_device->CreateSamplerState(&sdesc, &m_point_sampler.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_point_sampler, "dslighting point sampler"));

				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "dslighting.ps.cso"));
			}

			// Setup the shader ready to be used on 'dle'
			// Note, shaders are set/cleared by the state stack.
			// Only per-model constants, textures, and samplers need to be set here.
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state) override
			{
				base::Setup(dc, state);

				// Get the gbuffer render step and bind the gbuffer render targets to the PS
				auto& gbuffer = state.m_rstep->as<DSLighting>().m_gbuffer;
				dc->PSSetShaderResources(0, GBuffer::RTCount, (ID3D11ShaderResourceView* const*)gbuffer.m_srv);
				dc->PSSetSamplers(0, 1, &m_point_sampler.m_ptr);
			}

			// Undo any changes made by this shader on the dc
			// Note, shaders are set/cleared by the state stack.
			// This method is only needed to clear texture/sampler slots
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
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
			// Create the dx shaders
			VShaderDesc vsdesc(dslighting_vs, Vert());
			auto dx_ip = GetIP(EStockShader::DSLightingVS, &vsdesc);
			auto dx_vs = GetVS(EStockShader::DSLightingVS, &vsdesc);
			
			// Create the shader instances
			auto shdr = CreateShader<DSLightingShaderVS>(EStockShader::DSLightingVS, dx_vs, "dslighting_vs");
			shdr->m_iplayout = dx_ip;
			shdr->UsedBy(ERenderStep::DSLighting);
		}
		template <> void ShaderManager::CreateShader<DSLightingShaderPS>()
		{
			// Create the dx shaders
			PShaderDesc psdesc(dslighting_ps);
			auto dx_ps = GetPS(EStockShader::DSLightingPS, &psdesc);

			// Create the shader instances
			auto shdr = CreateShader<DSLightingShaderPS>(EStockShader::DSLightingPS, dx_ps, "dslighting_ps");
			shdr->UsedBy(ERenderStep::DSLighting);
		}
	}
}