//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/thick_line.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/event_types.h"
#include "renderer11/render/state_stack.h"
#include "renderer11/shaders/common.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(thick_linelist_gs.h)

		ThickLineListShaderGS::ThickLineListShaderGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr)
			:base(mgr, id, name, shdr)
			,m_cbuf_model(m_mgr->GetCBuf<hlsl::screenspace::CbufThickLine>("CbufThickLine"))
			,m_default_linewidth(2.0f)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "thick_line_gs.cso"));
		}

		// Setup the shader ready to be used on 'dle'
		void ThickLineListShaderGS::Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state)
		{
			base::Setup(dc,state);

			auto lw = state.m_dle->m_instance->find<float>(EInstComp::LineWidth);
			auto screen_size = state.m_rstep->m_scene->m_rdr->RenderTargetSize();
			//auto screen_size = state.m_rstep->m_scene->m_view.ViewArea(1.0f);

			hlsl::screenspace::CbufThickLine cb = {};
			cb.m_dim_and_width.x = float(screen_size.x);
			cb.m_dim_and_width.y = float(screen_size.y);
			cb.m_dim_and_width.w = lw ? *lw : m_default_linewidth;
			WriteConstants(dc, m_cbuf_model, cb, EShaderType::GS);
		}

		// Create the thick line shaders
		template <> void ShaderManager::CreateShader<ThickLineListShaderGS>()
		{
			// Create the dx shaders
			GShaderDesc desc(thick_linelist_gs);
			auto dx = GetGS(EStockShader::ThickLineListGS, &desc);
			CreateShader<ThickLineListShaderGS>(EStockShader::ThickLineListGS, dx, "thick_linelist_gs");
		}
	}
}
