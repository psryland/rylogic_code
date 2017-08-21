//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/common.h"

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include PR_RDR_SHADER_COMPILED_DIR(gbuffer_vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(gbuffer_ps.h)

		// GBuffer creation vertex shader
		struct GBufferShaderVS :Shader<ID3D11VertexShader, GBufferShaderVS>
		{
			typedef Shader<ID3D11VertexShader, GBufferShaderVS> base;
			GBufferShaderVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "gbuffer_vs.cso"));
			}
		};

		// GBuffer creation pixel shader
		struct GBufferShaderPS :Shader<ID3D11PixelShader, GBufferShaderPS>
		{
			typedef Shader<ID3D11PixelShader, GBufferShaderPS> base;
			GBufferShaderPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "gbuffer_ps.cso"));
			}
		};

		// Create the GBuffer shaders
		template <> void ShaderManager::CreateShader<GBufferShaderVS>()
		{
			VShaderDesc desc(gbuffer_vs, Vert());
			auto dx = GetVS(EStockShader::GBufferVS, &desc);
			CreateShader<GBufferShaderVS>(EStockShader::GBufferVS, dx, "gbuffer_vs");
		}
		template <> void ShaderManager::CreateShader<GBufferShaderPS>()
		{
			PShaderDesc desc(gbuffer_ps);
			auto dx = GetPS(EStockShader::GBufferPS, &desc);
			CreateShader<GBufferShaderPS>(EStockShader::GBufferPS, dx, "gbuffer_ps");
		}
	}
}
