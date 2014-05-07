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
		#include PR_RDR_COMPILED_SHADER_DIR(txfm_tint_pvc.vs.h)
		#include PR_RDR_COMPILED_SHADER_DIR(txfm_tint_pvc.ps.h)

		struct TxTintPvc :FwdShader
		{
			explicit TxTintPvc(ShaderManager* mgr) :FwdShader(mgr) {}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer
				fwd::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				WriteConstants(dc, m_cbuf_model, cb);
			}
		};

		// Create this shader
		template <> void ShaderManager::CreateShader<TxTintPvc>()
		{
			VShaderDesc vsdesc(txfm_tint_pvc_vs, VertPC());
			PShaderDesc psdesc(txfm_tint_pvc_ps);
			CreateShader<TxTintPvc>(EStockShader::TxTintPvc, &vsdesc, &psdesc, "txfm_tint_pvc");
		}
	}
}
