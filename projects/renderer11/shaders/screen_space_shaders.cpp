//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/screen_space_shaders.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/window.h"
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
		// Helper for setting screen space, per instance constants
		template <typename TCBuf> inline void SetScreenSpaceConstants(DeviceState const& state, float default_width, TCBuf& cb)
		{
			auto lw = state.m_dle->m_instance->find<float>(EInstComp::SSWidth);
			auto screen_size = state.m_rstep->m_scene->m_wnd->RenderTargetSize();
			cb.m_dim_and_width = v4(float(screen_size.x), float(screen_size.y), 0, lw ? *lw : default_width);
		}

		#pragma region ThickLineListShaderGS

		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(thick_linelist_gs.h)

		ThickLineListShaderGS::ThickLineListShaderGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr)
			:base(mgr, id, name, shdr)
			,m_cbuf_model(m_mgr->GetCBuf<hlsl::ss::CbufFrame>("ss::CbufFrame"))
			,m_default_width(2.0f)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "thick_line_gs.cso"));
		}

		// Setup the shader ready to be used on 'dle'
		void ThickLineListShaderGS::Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state)
		{
			base::Setup(dc,state);
			hlsl::ss::CbufFrame cb = {};
			SetScreenSpaceConstants(state, m_default_width, cb);
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

		#pragma endregion

		#pragma region ArrowHeadShaderGS

		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(arrow_head_gs.h)

		ArrowHeadShaderGS::ArrowHeadShaderGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr)
			:base(mgr, id, name, shdr)
			,m_cbuf_model(m_mgr->GetCBuf<hlsl::ss::CbufFrame>("ss::CbufFrame"))
			,m_default_width(2.0f)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "arrow_head_gs.cso"));
		}

		// Setup the shader ready to be used on 'dle'
		void ArrowHeadShaderGS::Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state)
		{
			base::Setup(dc,state);
			hlsl::ss::CbufFrame cb = {};
			SetViewConstants(state.m_rstep->m_scene->m_view, cb.m_cam);
			SetScreenSpaceConstants(state, m_default_width, cb);
			WriteConstants(dc, m_cbuf_model, cb, EShaderType::GS);
		}

		// Create the thick line shaders
		template <> void ShaderManager::CreateShader<ArrowHeadShaderGS>()
		{
			GShaderDesc desc(arrow_head_gs);
			auto dx = GetGS(EStockShader::ArrowHeadGS, &desc);
			CreateShader<ArrowHeadShaderGS>(EStockShader::ArrowHeadGS, dx, "arrow_head_gs");
		}

		#pragma endregion
	}
}
