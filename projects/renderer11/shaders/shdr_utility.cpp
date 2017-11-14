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
		#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_vs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_face_gs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_edge_gs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_vert_gs.h)
		#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_ps.h)
		#include PR_RDR_SHADER_COMPILED_DIR(ray_cast_cs.h)

		// Ray cast vertex shader
		struct RayCastVS :Shader<ID3D11VertexShader, RayCastVS>
		{
			using base = Shader<ID3D11VertexShader, RayCastVS>;
			RayCastVS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11VertexShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_vs.cso"));
			}
		};

		// Ray cast geometry shaders
		struct RayCastFaceGS :Shader<ID3D11GeometryShader, RayCastFaceGS>
		{
			using base = Shader<ID3D11GeometryShader, RayCastFaceGS>;
			RayCastFaceGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_face_gs.cso"));
			}
		};
		struct RayCastEdgeGS :Shader<ID3D11GeometryShader, RayCastEdgeGS>
		{
			using base = Shader<ID3D11GeometryShader, RayCastEdgeGS>;
			RayCastEdgeGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_edge_gs.cso"));
			}
		};
		struct RayCastVertGS :Shader<ID3D11GeometryShader, RayCastVertGS>
		{
			using base = Shader<ID3D11GeometryShader, RayCastVertGS>;
			RayCastVertGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_vert_gs.cso"));
			}
		};

		// Ray cast pixel shader
		struct RayCastPS :Shader<ID3D11PixelShader, RayCastPS>
		{
			using base = Shader<ID3D11PixelShader, RayCastPS>;
			RayCastPS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11PixelShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_ps.cso"));
			}
		};

		// Ray cast compute shader
		struct RayCastCS :Shader<ID3D11ComputeShader, RayCastCS>
		{
			using base = Shader<ID3D11ComputeShader, RayCastCS>;
			RayCastCS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11ComputeShader> const& shdr)
				:base(mgr, id, sort_id, name, shdr)
			{
				PR_EXPAND(PR_RDR_RUNTIME_SHADERS, RegisterRuntimeShader(m_orig_id, "ray_cast_cs.cso"));
			}
			void Setup(ID3D11DeviceContext* dc, DeviceState& state) override
			{
				base::Setup(dc, state);
			}
		};

		// Create the ray cast shaders
		template <> void ShaderManager::CreateShader<RayCastVS>()
		{
			VShaderDesc desc(ray_cast_vs, Vert());
			auto dx = GetVS(RdrId(EStockShader::RayCastVS), &desc);
			m_stock_shaders.emplace_back(CreateShader<RayCastVS>(RdrId(EStockShader::RayCastVS), dx, "ray_cast_vs"));
		}
		template <> void ShaderManager::CreateShader<RayCastFaceGS>()
		{
			GShaderDesc desc(ray_cast_face_gs);
			auto dx = GetGS(RdrId(EStockShader::RayCastFaceGS), &desc);
			m_stock_shaders.emplace_back(CreateShader<RayCastFaceGS>(RdrId(EStockShader::RayCastFaceGS), dx, "ray_cast_face_gs"));
		}
		template <> void ShaderManager::CreateShader<RayCastEdgeGS>()
		{
			GShaderDesc desc(ray_cast_edge_gs);
			auto dx = GetGS(RdrId(EStockShader::RayCastEdgeGS), &desc);
			m_stock_shaders.emplace_back(CreateShader<RayCastEdgeGS>(RdrId(EStockShader::RayCastEdgeGS), dx, "ray_cast_edge_gs"));
		}
		template <> void ShaderManager::CreateShader<RayCastVertGS>()
		{
			GShaderDesc desc(ray_cast_vert_gs);
			auto dx = GetGS(RdrId(EStockShader::RayCastVertGS), &desc);
			m_stock_shaders.emplace_back(CreateShader<RayCastVertGS>(RdrId(EStockShader::RayCastVertGS), dx, "ray_cast_vert_gs"));
		}
		template <> void ShaderManager::CreateShader<RayCastPS>()
		{
			PShaderDesc desc(ray_cast_ps);
			auto dx = GetPS(RdrId(EStockShader::RayCastPS), &desc);
			m_stock_shaders.emplace_back(CreateShader<RayCastPS>(RdrId(EStockShader::RayCastPS), dx, "ray_cast_ps"));
		}
		template <> void ShaderManager::CreateShader<RayCastCS>()
		{
			CShaderDesc desc(ray_cast_cs);
			auto dx = GetCS(RdrId(EStockShader::RayCastCS), &desc);
			m_stock_shaders.emplace_back(CreateShader<RayCastCS>(RdrId(EStockShader::RayCastCS), dx, "ray_cast_cs"));
		}
	}
}
