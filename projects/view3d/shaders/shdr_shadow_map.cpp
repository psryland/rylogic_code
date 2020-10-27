//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/render/drawlist_element.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/steps/render_step.h"
#include "pr/view3d/util/stock_resources.h"
#include "view3d/shaders/common.h"

namespace pr::rdr
{
	// include generated header files
	#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_vs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_ps.h)

	// Shadow map vertex shader
	struct ShadowMapVS :ShaderT<ID3D11VertexShader, ShadowMapVS>
	{
		using base = ShaderT<ID3D11VertexShader, ShadowMapVS>;
		ShadowMapVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "shadow_map_vs.cso"));
		}
	};

	// Shadow map pixel shader
	struct ShadowMapPS :ShaderT<ID3D11PixelShader, ShadowMapPS>
	{
		using base = ShaderT<ID3D11PixelShader, ShadowMapPS>;
		ShadowMapPS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "shadow_map_ps.cso"));
		}
	};

	// Create the shadow map shaders
	template <> void ShaderManager::CreateShader<ShadowMapVS>()
	{
		VShaderDesc desc(shadow_map_vs, Vert());
		auto dx = GetVS(RdrId(EStockShader::ShadowMapVS), &desc);
		m_stock_shaders.emplace_back(CreateShader<ShadowMapVS>(RdrId(EStockShader::ShadowMapVS), dx, "smap_vs"));
	}
	template <> void ShaderManager::CreateShader<ShadowMapPS>()
	{
		PShaderDesc desc(shadow_map_ps);
		auto dx = GetPS(RdrId(EStockShader::ShadowMapPS), &desc);
		m_stock_shaders.emplace_back(CreateShader<ShadowMapPS>(RdrId(EStockShader::ShadowMapPS), dx, "smap_ps"));
	}
}
