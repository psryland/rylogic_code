//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/steps/render_step.h"
#include "renderer11/steps/common.h"
#include "renderer11/steps/forward/fwd_shader.h"
#include "renderer11/util/internal_resources.h"

namespace pr
{
	namespace rdr
	{
		namespace fwd
		{
			// Constant buffer types for the forward shaders
			#include "renderer11/shaders/hlsl/forward/forward_cbuf.hlsli"
		}

		// include generated header files
		#include PR_RDR_COMPILED_SHADER_DIR(forward.vs.h)
		#include PR_RDR_COMPILED_SHADER_DIR(forward.ps.h)

		struct FwdShader :BaseShader
		{
			// Per-model constant buffer
			D3DPtr<ID3D11Buffer> m_cbuf_model;

			explicit FwdShader(ShaderManager* mgr)
				:BaseShader(mgr)
			{
				// Create a per-model constants buffer
				CBufferDesc cbdesc(sizeof(fwd::CBufModel));
				pr::Throw(mgr->m_device->CreateBuffer(&cbdesc, 0, &m_cbuf_model.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_model, "FwdShader::CBufModel"));
			}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				fwd::CBufModel cb = {};
				Geom(*dle.m_nugget, cb);
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
		template <> void ShaderManager::CreateShader<FwdShader>()
		{
			// Create the shader
			VShaderDesc vsdesc(forward_vs, Vert());
			PShaderDesc psdesc(forward_ps);
			CreateShader<FwdShader>(ERdrShader::FwdShader, &vsdesc, &psdesc, "fwd_shader");
		}
	}
}
