//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/drawlist_element.h"
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
		#include PR_RDR_SHADER_COMPILED_DIR(forward_vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(forward_ps.h)

		// Forward rendering vertex shader
		struct FwdShaderVS :Shader<ID3D11VertexShader, FwdShaderVS>
		{
			typedef Shader<ID3D11VertexShader, FwdShaderVS> base;
			FwdShaderVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "forward_vs.cso"));
			}
		};

		// Forward rendering pixel shader
		struct FwdShaderPS :Shader<ID3D11PixelShader, FwdShaderPS>
		{
			typedef Shader<ID3D11PixelShader, FwdShaderPS> base;
			FwdShaderPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "forward_ps.cso"));
			}
		};

		// Create the forward shaders
		template <> void ShaderManager::CreateShader<FwdShaderVS>()
		{
			VShaderDesc desc(forward_vs, Vert());
			auto dx = GetVS(EStockShader::FwdShaderVS, &desc);
			CreateShader<FwdShaderVS>(EStockShader::FwdShaderVS, dx, "fwd_shader_vs");
		}
		template <> void ShaderManager::CreateShader<FwdShaderPS>()
		{
			PShaderDesc desc(forward_ps);
			auto dx = GetPS(EStockShader::FwdShaderPS, &desc);
			CreateShader<FwdShaderPS>(EStockShader::FwdShaderPS, dx, "fwd_shader_ps");
		}
	}
}
