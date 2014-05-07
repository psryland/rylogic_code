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
		#include PR_RDR_COMPILED_SHADER_DIR(txfm_tint_pvc_lit.vs.h)
		#include PR_RDR_COMPILED_SHADER_DIR(txfm_tint_pvc_lit.ps.h)

		struct TxTintPvcLit :FwdShader
		{
			explicit TxTintPvcLit(ShaderManager* mgr) :FwdShader(mgr) {}
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep) override
			{
				BaseShader::Setup(dc, dle, rstep);

				// Fill out the model constants buffer and bind it to the VS stage
				fwd::CBufModel cb = {};
				Txfm(*dle.m_instance, rstep.m_scene->m_view, cb);
				Tint(*dle.m_instance, cb);
				WriteConstants(dc, m_cbuf_model, cb);
			}
		};

		template <> void ShaderManager::CreateShader<TxTintPvcLit>()
		{
			// Create the shader
			VShaderDesc vsdesc(txfm_tint_pvc_lit_vs, VertPCNT(), EGeom::Vert|EGeom::Colr|EGeom::Norm);
			PShaderDesc psdesc(txfm_tint_pvc_lit_ps);
			CreateShader<TxTintPvcLit>(EStockShader::TxTintPvcLit, &vsdesc, &psdesc, "txfm_tint_pvc_lit");
		}
	}
}
