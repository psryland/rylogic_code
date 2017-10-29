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
			using base = Shader<ID3D11VertexShader, ShadowMapVS>;
			ShadowMapVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "shadow_map_vs.cso"));
			}
		};

		// Shadow map face geometry shader
		struct ShadowMapFaceGS :Shader<ID3D11GeometryShader, ShadowMapFaceGS>
		{
			using base = Shader<ID3D11GeometryShader, ShadowMapFaceGS>;
			ShadowMapFaceGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "shadow_map_face_gs.cso"));
			}
		};

		// Shadow map line geometry shader
		struct ShadowMapLineGS :Shader<ID3D11GeometryShader, ShadowMapLineGS>
		{
			using base = Shader<ID3D11GeometryShader, ShadowMapLineGS>;
			ShadowMapLineGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "shadow_map_line_gs.cso"));
			}
		};

		// Shadow map pixel shader
		struct ShadowMapPS :Shader<ID3D11PixelShader, ShadowMapPS>
		{
			using base = Shader<ID3D11PixelShader, ShadowMapPS>;
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
			auto dx = GetVS(EStockShader::ShadowMapVS, &desc);
			m_stock_shaders.emplace_back(CreateShader<ShadowMapVS>(EStockShader::ShadowMapVS, dx, "smap_vs"));
		}
		template <> void ShaderManager::CreateShader<ShadowMapFaceGS>()
		{
			GShaderDesc desc(shadow_map_face_gs);
			auto dx = GetGS(EStockShader::ShadowMapFaceGS, &desc);
			m_stock_shaders.emplace_back(CreateShader<ShadowMapFaceGS>(EStockShader::ShadowMapFaceGS, dx, "smap_face_gs"));
		}
		template <> void ShaderManager::CreateShader<ShadowMapLineGS>()
		{
			GShaderDesc desc(shadow_map_line_gs);
			auto dx = GetGS(EStockShader::ShadowMapLineGS, &desc);
			m_stock_shaders.emplace_back(CreateShader<ShadowMapLineGS>(EStockShader::ShadowMapLineGS, dx, "smap_line_gs"));
		}
		template <> void ShaderManager::CreateShader<ShadowMapPS>()
		{
			PShaderDesc desc(shadow_map_ps);
			auto dx = GetPS(EStockShader::ShadowMapPS, &desc);
			m_stock_shaders.emplace_back(CreateShader<ShadowMapPS>(EStockShader::ShadowMapPS, dx, "smap_ps"));
		}
	}
}
