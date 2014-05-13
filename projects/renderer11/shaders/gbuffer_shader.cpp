//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
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
		#include PR_RDR_SHADER_COMPILED_DIR(gbuffer.vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(gbuffer.ps.h)

		// gbuffer creation vertex shader
		struct GBufferShaderVS :Shader<ID3D11VertexShader, GBufferShaderVS>
		{
			typedef Shader<ID3D11VertexShader, GBufferShaderVS> base;
			GBufferShaderVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "gbuffer.vs.cso"));
			}
		};

		// gbuffer creation pixel shader
		struct GBufferShaderPS :Shader<ID3D11PixelShader, GBufferShaderPS>
		{
			typedef Shader<ID3D11PixelShader, GBufferShaderPS> base;
			GBufferShaderPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "gbuffer.ps.cso"));
			}
		};

		// Create the gbuffer shaders
		template <> void ShaderManager::CreateShader<GBufferShaderVS>()
		{
			// Create the dxshaders
			VShaderDesc vsdesc(gbuffer_vs, Vert());
			auto dx_ip = GetIP(EStockShader::GBufferVS, &vsdesc);
			auto dx_vs = GetVS(EStockShader::GBufferVS, &vsdesc);

			// Create the shader instances
			auto shdr = CreateShader<GBufferShaderVS>(EStockShader::GBufferVS, dx_vs, "gbuffer_vs");
			shdr->m_iplayout = dx_ip;
			shdr->UsedBy(ERenderStep::GBuffer);
		}
		template <> void ShaderManager::CreateShader<GBufferShaderPS>()
		{
			// Create the dxshaders
			PShaderDesc psdesc(gbuffer_ps);
			auto dx_ps = GetPS(EStockShader::GBufferPS, &psdesc);

			// Create the shader instances
			auto shdr = CreateShader<GBufferShaderPS>(EStockShader::GBufferPS, dx_ps, "gbuffer_ps");
			shdr->UsedBy(ERenderStep::GBuffer);
		}
	}
}
