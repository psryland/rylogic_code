//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/scene.h"
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
		#include PR_RDR_SHADER_COMPILED_DIR(forward.vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(forward.ps.h)

		// Forward rendering vertex shader
		struct FwdShaderVS :Shader<ID3D11VertexShader, FwdShaderVS>
		{
			typedef Shader<ID3D11VertexShader, FwdShaderVS> base;
			FwdShaderVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "forward.vs.cso"));
			}
		};

		// Forward rendering pixel shader
		struct FwdShaderPS :Shader<ID3D11PixelShader, FwdShaderPS>
		{
			typedef Shader<ID3D11PixelShader, FwdShaderPS> base;
			FwdShaderPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "forward.ps.cso"));
			}
		};

		// Create the forward shaders
		template <> void ShaderManager::CreateShader<FwdShaderVS>()
		{
			// Create the dx shaders
			VShaderDesc vsdesc(forward_vs, Vert());
			auto dx_ip = GetIP(EStockShader::FwdShaderVS, &vsdesc);
			auto dx_vs = GetVS(EStockShader::FwdShaderVS, &vsdesc);
			
			// Create the shader instances
			auto shdr = CreateShader<FwdShaderVS>(EStockShader::FwdShaderVS, dx_vs, "fwd_shader_vs");
			shdr->m_iplayout = dx_ip;
			shdr->UsedBy(ERenderStep::ForwardRender);
		}
		template <> void ShaderManager::CreateShader<FwdShaderPS>()
		{
			// Create the dx shaders
			PShaderDesc psdesc(forward_ps);
			auto dx_ps = GetPS(EStockShader::FwdShaderPS, &psdesc);

			// Create the shader instances
			auto shdr = CreateShader<FwdShaderPS>(EStockShader::FwdShaderPS, dx_ps, "fwd_shader_ps");
			shdr->UsedBy(ERenderStep::ForwardRender);
		}
	}
}
