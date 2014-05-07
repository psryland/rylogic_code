//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/steps/render_step.h"
#include "renderer11/steps/common.h"
#include "renderer11/steps/forward/fwd_shader.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include "renderer11/shaders/hlsl/compiled/txfm_tint_tex.vs.h"
		#include "renderer11/shaders/hlsl/compiled/txfm_tint_tex.ps.h"

		struct TxTintTex :FwdShader
		{
			explicit TxTintTex(ShaderManager* mgr) :FwdShader(mgr) {}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				fwd::CBufModel cb = {};
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
		template <> void ShaderManager::CreateShader<TxTintTex>()
		{
			// Create the shader
			VShaderDesc vsdesc(txfm_tint_tex_vs, VertPT());
			PShaderDesc psdesc(txfm_tint_tex_ps);
			CreateShader<TxTintTex>(EStockShader::TxTintTex, &vsdesc, &psdesc, "txfm_tint_tex");
		}
	}
}
