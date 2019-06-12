//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shdr_fwd.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/render/state_stack.h"
#include "renderer11/shaders/common.h"

namespace pr::rdr
{
	// include generated header files
	#include PR_RDR_SHADER_COMPILED_DIR(forward_vs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(forward_ps.h)
	#include PR_RDR_SHADER_COMPILED_DIR(forward_radial_fade_ps.h)

	// Forward rendering vertex shader
	FwdShaderVS::FwdShaderVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader> const& shdr)
		:base(mgr, id, sort_id, name, shdr)
	{
		PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "forward_vs.cso"));
	}

	// Forward rendering pixel shader
	FwdShaderPS::FwdShaderPS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr)
		:base(mgr, id, sort_id, name, shdr)
	{
		PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "forward_ps.cso"));
	}

	// Forward rendering pixel shader with radial fading
	FwdRadialFadePS::FwdRadialFadePS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr)
		:base(mgr, id, sort_id, name, shdr)
		,m_cbuf(m_mgr->GetCBuf<hlsl::fwd::CBufFade>("fwd::CbufFade"))
		,m_fade_centre()
		,m_fade_radius()
		,m_fade_type()
	{
		PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "forward_radial_fade_ps.cso"));
	}
	void FwdRadialFadePS::Setup(ID3D11DeviceContext* dc, DeviceState& state)
	{
		base::Setup(dc, state);
		
		hlsl::fwd::CBufFade cb = {};
		cb.m_fade_centre = m_fade_centre;
		cb.m_fade_radius = (m_focus_relative ? state.m_rstep->m_scene->m_view.FocusDist() : 1) * m_fade_radius;
		cb.m_fade_type = int(m_fade_type);
		WriteConstants(dc, m_cbuf.get(), cb, EShaderType::PS);
	}

	// Create the forward shaders
	template <> void ShaderManager::CreateShader<FwdShaderVS>()
	{
		VShaderDesc desc(forward_vs, Vert());
		auto dx = GetVS(RdrId(EStockShader::FwdShaderVS), &desc);
		m_stock_shaders.emplace_back(CreateShader<FwdShaderVS>(RdrId(EStockShader::FwdShaderVS), dx, "fwd_shader_vs"));
	}
	template <> void ShaderManager::CreateShader<FwdShaderPS>()
	{
		PShaderDesc desc(forward_ps);
		auto dx = GetPS(RdrId(EStockShader::FwdShaderPS), &desc);
		m_stock_shaders.emplace_back(CreateShader<FwdShaderPS>(RdrId(EStockShader::FwdShaderPS), dx, "fwd_shader_ps"));
	}
	template <> void ShaderManager::CreateShader<FwdRadialFadePS>()
	{
		PShaderDesc desc(forward_radial_fade_ps);
		auto dx = GetPS(RdrId(EStockShader::FwdRadialFadePS), &desc);
		m_stock_shaders.emplace_back(CreateShader<FwdRadialFadePS>(RdrId(EStockShader::FwdRadialFadePS), dx, "fwd_radial_fade_ps"));
	}
}
