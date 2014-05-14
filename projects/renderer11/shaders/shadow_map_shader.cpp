//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
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
		#include PR_RDR_SHADER_COMPILED_DIR(shadow_map.vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(shadow_map.ps.h)

		// Shadow map vertex shader
		struct ShadowMapVS :Shader<ID3D11VertexShader, ShadowMapVS>
		{
			typedef Shader<ID3D11VertexShader, ShadowMapVS> base;
			ShadowMapVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "shadow_map.vs.cso"));
			}
		};

		// Shadow map pixel shader
		struct ShadowMapPS :Shader<ID3D11PixelShader, ShadowMapPS>
		{
			typedef Shader<ID3D11PixelShader, ShadowMapPS> base;
			ShadowMapPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "shadow_map.ps.cso"));
			}
		};

		// Create the shadow map shaders
		template <> void ShaderManager::CreateShader<ShadowMapVS>()
		{
			// Create the dx shaders
			VShaderDesc vsdesc(shadow_map_vs, Vert());
			auto dx_ip = GetIP(EStockShader::ShadowMapVS, &vsdesc);
			auto dx_vs = GetVS(EStockShader::ShadowMapVS, &vsdesc);
			
			// Create the shader instances
			auto shdr = CreateShader<ShadowMapVS>(EStockShader::ShadowMapVS, dx_vs, "smap_vs");
			shdr->m_iplayout = dx_ip;
			shdr->UsedBy(ERenderStep::ShadowMap);
		}
		template <> void ShaderManager::CreateShader<ShadowMapPS>()
		{
			// Create the dx shaders
			PShaderDesc psdesc(shadow_map_ps);
			auto dx_ps = GetPS(EStockShader::ShadowMapPS, &psdesc);

			// Create the shader instances
			auto shdr = CreateShader<ShadowMapPS>(EStockShader::ShadowMapPS, dx_ps, "smap_ps");
			shdr->UsedBy(ERenderStep::ShadowMap);
		}
	}
}
