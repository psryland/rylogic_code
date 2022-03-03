//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/shdr_diagnostic.h"
#include "pr/view3d/steps/render_step.h"
#include "pr/view3d/util/stock_resources.h"
#include "view3d/render/state_stack.h"
#include "view3d/shaders/common.h"

namespace pr::rdr
{
	// include generated header files
	#include PR_RDR_SHADER_COMPILED_DIR(show_normals_gs.h)
	
	ShowNormalsGS::ShowNormalsGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
		:base(mgr, id, sort_id, name, shdr)
		,m_cbuf(m_mgr->GetCBuf<hlsl::diag::CBufFrame>("diag::CBufFrame"))
	{
		PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "show_normals_gs.cso"));
	}

	// Set up the shader ready to be used on 'dle'
	void ShowNormalsGS::Setup(ID3D11DeviceContext* dc, DeviceState& state)
	{
		base::Setup(dc, state);
		hlsl::diag::CBufFrame cb = {};
		SetViewConstants(state.m_rstep->m_scene->m_view, cb.m_cam);
		cb.m_colour = state.m_rstep->m_scene->m_diag.m_normal_colour;
		cb.m_length = state.m_rstep->m_scene->m_diag.m_normal_lengths;
		WriteConstants(dc, m_cbuf.get(), cb, EShaderType::GS);
	}

	// Create the show normals shader
	template <> void ShaderManager::CreateStockShader<ShowNormalsGS>()
	{
		// Create the dx shaders
		GShaderDesc desc(show_normals_gs);
		auto dx = GetGS(RdrId(EStockShader::ShowNormalsGS), &desc);
		m_stock_shaders.emplace_back(CreateShader<ShowNormalsGS>(RdrId(EStockShader::ShowNormalsGS), dx, "show_normals_gs"));
	}
}