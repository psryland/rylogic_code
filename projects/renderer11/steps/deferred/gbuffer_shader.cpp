//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/steps/render_step.h"
#include "renderer11/steps/common.h"
#include "renderer11/steps/deferred/ds_shader.h"
#include "renderer11/util/internal_resources.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include PR_RDR_COMPILED_SHADER_DIR(gbuffer.vs.h)
		#include PR_RDR_COMPILED_SHADER_DIR(gbuffer.ps.h)

		// A shader that creates the gbuffer
		struct GBufferShader :DSShader
		{
			// Per-model constants
			D3DPtr<ID3D11Buffer> m_cbuf_model;

			explicit GBufferShader(ShaderManager* mgr)
				:DSShader(mgr)
			{
				// Create a per-model constants buffer
				CBufferDesc cbdesc(sizeof(ds::CBufModel));
				pr::Throw(mgr->m_device->CreateBuffer(&cbdesc, 0, &m_cbuf_model.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_model, "GBufferCreateShader::CBufModel"));
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Set the constants for the shader
				ds::CBufModel cb = {};
				cb.m_geom = GeomToIV4(dle.m_nugget->m_geom);
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				Tex0(*dle.m_nugget, cb);
				WriteConstants(dc, m_cbuf_model, cb);

				// Set constants for the pixel shader
				BindTextureAndSampler(dc, dle.m_nugget->m_tex_diffuse);
			}
			void Cleanup(D3DPtr<ID3D11DeviceContext>& dc) override
			{
				BindTextureAndSampler(dc, nullptr);
			}
		};

		// Create this shader
		template <> void ShaderManager::CreateShader<GBufferShader>()
		{
			// Create the shader
			VShaderDesc vsdesc(gbuffer_vs, VertPCNT());
			PShaderDesc psdesc(gbuffer_ps);
			CreateShader<GBufferShader>(ERdrShader::GBuffer, &vsdesc, &psdesc, "gbuffer");
		}
	}
}
