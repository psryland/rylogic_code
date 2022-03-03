//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/steps/render_step.h"
#include "pr/view3d/util/stock_resources.h"
#include "view3d/shaders/common.h"

namespace pr::rdr
{
	// include generated header files
	#include PR_RDR_SHADER_COMPILED_DIR(gbuffer_vs.h)
	#include PR_RDR_SHADER_COMPILED_DIR(gbuffer_ps.h)

	// GBuffer creation vertex shader
	struct GBufferVS :ShaderT<ID3D11VertexShader, GBufferVS>
	{
		using base = ShaderT<ID3D11VertexShader, GBufferVS>;
		GBufferVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "gbuffer_vs.cso"));
		}
	};

	// GBuffer creation pixel shader
	struct GBufferPS :ShaderT<ID3D11PixelShader, GBufferPS>
	{
		using base = ShaderT<ID3D11PixelShader, GBufferPS>;
		GBufferPS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr)
			:base(mgr, id, sort_id, name, shdr)
		{
			PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "gbuffer_ps.cso"));
		}
	};

	// Create the GBuffer shaders
	template <> void ShaderManager::CreateStockShader<GBufferVS>()
	{
		VShaderDesc desc(gbuffer_vs, Vert());
		auto dx = GetVS(RdrId(EStockShader::GBufferVS), &desc);
		m_stock_shaders.emplace_back(CreateShader<GBufferVS>(RdrId(EStockShader::GBufferVS), dx, "gbuffer_vs"));
	}
	template <> void ShaderManager::CreateStockShader<GBufferPS>()
	{
		PShaderDesc desc(gbuffer_ps);
		auto dx = GetPS(RdrId(EStockShader::GBufferPS), &desc);
		m_stock_shaders.emplace_back(CreateShader<GBufferPS>(RdrId(EStockShader::GBufferPS), dx, "gbuffer_ps"));
	}
}
