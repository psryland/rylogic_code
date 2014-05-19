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
		#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_face_gs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_line_gs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(shadow_map_ps.h)

		// Shadow map vertex shader
		struct ShadowMapVS :Shader<ID3D11VertexShader, ShadowMapVS>
		{
			typedef Shader<ID3D11VertexShader, ShadowMapVS> base;
			ShadowMapVS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11VertexShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "shadow_map_vs.cso"));
			}
		};

		// Shadow map face geometry shader
		struct ShadowMapFaceGS :Shader<ID3D11GeometryShader, ShadowMapFaceGS>
		{
			typedef Shader<ID3D11GeometryShader, ShadowMapFaceGS> base;
			ShadowMapFaceGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "shadow_map_face_gs.cso"));
			}
		};

		// Shadow map line geometry shader
		struct ShadowMapLineGS :Shader<ID3D11GeometryShader, ShadowMapLineGS>
		{
			typedef Shader<ID3D11GeometryShader, ShadowMapLineGS> base;
			ShadowMapLineGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "shadow_map_line_gs.cso"));
			}
		};

		// Shadow map pixel shader
		struct ShadowMapPS :Shader<ID3D11PixelShader, ShadowMapPS>
		{
			typedef Shader<ID3D11PixelShader, ShadowMapPS> base;
			ShadowMapPS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11PixelShader> shdr)
				:base(mgr, id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(id, "shadow_map_ps.cso"));
			}
		};

		// Create the shadow map shaders
		template <> void ShaderManager::CreateShader<ShadowMapVS>()
		{
			VShaderDesc desc(shadow_map_vs, Vert());
			auto dx = GetVS(EStockShader::ShadowMapVS, &desc);
			CreateShader<ShadowMapVS>(EStockShader::ShadowMapVS, dx, "smap_vs");
		}
		template <> void ShaderManager::CreateShader<ShadowMapFaceGS>()
		{
			GShaderDesc desc(shadow_map_face_gs);
			auto dx = GetGS(EStockShader::ShadowMapFaceGS, &desc);
			CreateShader<ShadowMapFaceGS>(EStockShader::ShadowMapFaceGS, dx, "smap_face_gs");
		}
		template <> void ShaderManager::CreateShader<ShadowMapLineGS>()
		{
			GShaderDesc desc(shadow_map_line_gs);
			auto dx = GetGS(EStockShader::ShadowMapLineGS, &desc);
			CreateShader<ShadowMapLineGS>(EStockShader::ShadowMapLineGS, dx, "smap_line_gs");
		}
		template <> void ShaderManager::CreateShader<ShadowMapPS>()
		{
			PShaderDesc desc(shadow_map_ps);
			auto dx = GetPS(EStockShader::ShadowMapPS, &desc);
			CreateShader<ShadowMapPS>(EStockShader::ShadowMapPS, dx, "smap_ps");
		}
	}
}
