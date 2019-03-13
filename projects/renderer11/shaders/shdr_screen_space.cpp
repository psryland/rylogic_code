//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shdr_screen_space.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/window.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/render/state_stack.h"
#include "renderer11/shaders/common.h"

namespace pr
{
	namespace rdr
	{
		// Helper for setting screen space, per instance constants
		template <typename TCBuf> inline void SetScreenSpaceConstants(DeviceState const& state, v2 size, bool depth, TCBuf& cb)
		{
			auto sz = state.m_dle->m_instance->find<v2>(EInstComp::SSSize);
			auto rt_size = state.m_rstep->m_scene->m_wnd->RenderTargetSize();
			cb.m_screen_dim = v2(float(rt_size.x), float(rt_size.y));
			cb.m_size = sz ? *sz : size;
			cb.m_depth = depth;
		}

		#pragma region PointSpritesGS

		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(point_sprites_gs.h)

		PointSpritesGS::PointSpritesGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
			,m_cbuf_model(m_mgr->GetCBuf<hlsl::ss::CbufFrame>("ss::CbufFrame"))
			,m_size(10.0f, 10.0f)
			,m_depth(true)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "point_sprites_gs.cso"));
		}

		// Set up the shader ready to be used on 'dle'
		void PointSpritesGS::Setup(ID3D11DeviceContext* dc, DeviceState& state)
		{
			base::Setup(dc, state);
			hlsl::ss::CbufFrame cb = {};
			SetViewConstants(state.m_rstep->m_scene->m_view, cb.m_cam);
			SetScreenSpaceConstants(state, m_size, m_depth, cb);
			WriteConstants(dc, m_cbuf_model.get(), cb, EShaderType::GS);
		}

		// Create the point sprites shaders
		template <> void ShaderManager::CreateShader<PointSpritesGS>()
		{
			// Create the dx shaders
			GShaderDesc desc(point_sprites_gs);
			auto dx = GetGS(RdrId(EStockShader::PointSpritesGS), &desc);
			m_stock_shaders.emplace_back(CreateShader<PointSpritesGS>(RdrId(EStockShader::PointSpritesGS), dx, "point_sprites_gs"));
		}

		#pragma endregion

		#pragma region ThickLineListGS

		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(thick_linelist_gs.h)

		ThickLineListGS::ThickLineListGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
			,m_cbuf_model(m_mgr->GetCBuf<hlsl::ss::CbufFrame>("ss::CbufFrame"))
			,m_width(2.0f)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "thick_line_gs.cso"));
		}

		// Set up the shader ready to be used on 'dle'
		void ThickLineListGS::Setup(ID3D11DeviceContext* dc, DeviceState& state)
		{
			base::Setup(dc, state);
			hlsl::ss::CbufFrame cb = {};
			SetScreenSpaceConstants(state, v2(m_width, m_width), false, cb);
			WriteConstants(dc, m_cbuf_model.get(), cb, EShaderType::GS);
		}

		// Create the thick line shaders
		template <> void ShaderManager::CreateShader<ThickLineListGS>()
		{
			// Create the dx shaders
			GShaderDesc desc(thick_linelist_gs);
			auto dx = GetGS(RdrId(EStockShader::ThickLineListGS), &desc);
			m_stock_shaders.emplace_back(CreateShader<ThickLineListGS>(RdrId(EStockShader::ThickLineListGS), dx, "thick_linelist_gs"));
		}

		#pragma endregion

		#pragma region ArrowHeadGS

		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(arrow_head_gs.h)

		ArrowHeadGS::ArrowHeadGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
			,m_cbuf_model(m_mgr->GetCBuf<hlsl::ss::CbufFrame>("ss::CbufFrame"))
			,m_size(2.0f)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "arrow_head_gs.cso"));
		}

		// Set up the shader ready to be used on 'dle'
		void ArrowHeadGS::Setup(ID3D11DeviceContext* dc, DeviceState& state)
		{
			base::Setup(dc, state);
			hlsl::ss::CbufFrame cb = {};
			SetViewConstants(state.m_rstep->m_scene->m_view, cb.m_cam);
			SetScreenSpaceConstants(state, v2(m_size, m_size), false, cb);
			WriteConstants(dc, m_cbuf_model.get(), cb, EShaderType::GS);
		}

		// Create the thick line shaders
		template <> void ShaderManager::CreateShader<ArrowHeadGS>()
		{
			GShaderDesc desc(arrow_head_gs);
			auto dx = GetGS(RdrId(EStockShader::ArrowHeadGS), &desc);
			m_stock_shaders.emplace_back(CreateShader<ArrowHeadGS>(RdrId(EStockShader::ArrowHeadGS), dx, "arrow_head_gs"));
		}

		#pragma endregion
	}
}
