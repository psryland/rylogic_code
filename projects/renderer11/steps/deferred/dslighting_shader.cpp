//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/steps/deferred/dslighting.h"
#include "pr/renderer11/steps/deferred/gbuffer.h"
#include "renderer11/steps/common.h"
#include "renderer11/steps/deferred/ds_shader.h"
#include "renderer11/util/internal_resources.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include "renderer11/shaders/hlsl/compiled/dslighting.vs.h"
		#include "renderer11/shaders/hlsl/compiled/dslighting.ps.h"

		struct DSLightingShader :DSShader
		{
			D3DPtr<ID3D11SamplerState> m_point_sampler; // A point sampler used to sample the gbuffer

			explicit DSLightingShader(ShaderManager* mgr)
				:DSShader(mgr)
			{
				// Create a gbuffer sampler
				auto sdesc = SamplerDesc::PointClamp();
				pr::Throw(mgr->m_device->CreateSamplerState(&sdesc, &m_point_sampler.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_point_sampler, "dslighting point sampler"));
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				auto& gbuffer = rstep.as<DSLighting>().m_gbuffer;

				// Set constants for the pixel shader
				dc->PSSetSamplers(0, 1, &m_point_sampler.m_ptr);
				dc->PSSetShaderResources(0, GBuffer::RTCount, (ID3D11ShaderResourceView* const*)gbuffer.m_srv);
			}
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
			{
				ID3D11ShaderResourceView* null_srv[GBuffer::RTCount] = {};
				dc->PSSetShaderResources(0, GBuffer::RTCount, null_srv);

				ID3D11SamplerState* null_samp[1] = {};
				dc->PSSetSamplers(0, 1, null_samp);
			}
		};

		// Create this shader
		template <> void ShaderManager::CreateShader<DSLightingShader>()
		{
			// Create the shader
			VShaderDesc vsdesc(dslighting_vs, VertPCNT());
			PShaderDesc psdesc(dslighting_ps);
			CreateShader<DSLightingShader>(ERdrShader::DSLighting, &vsdesc, &psdesc, "dslighting");
		}
	}
}
