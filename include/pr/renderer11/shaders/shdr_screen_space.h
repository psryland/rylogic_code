//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader.h"

namespace pr
{
	namespace rdr
	{
		// A geometry shader for creating quads from point list geometry
		struct PointSpritesGS :ShaderT<ID3D11GeometryShader, PointSpritesGS>
		{
			using base = ShaderT<ID3D11GeometryShader, PointSpritesGS>;
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Per-model constant buffer
			v2 m_size;
			bool m_depth;

			PointSpritesGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr);
			void Setup(ID3D11DeviceContext* dc, DeviceState& state) override;
		};

		// A geometry shader for creating thick lines from line list geometry
		struct ThickLineListGS :ShaderT<ID3D11GeometryShader, ThickLineListGS>
		{
			using base = ShaderT<ID3D11GeometryShader, ThickLineListGS>;
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Per-model constant buffer
			float m_width;
			
			ThickLineListGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr);
			void Setup(ID3D11DeviceContext* dc, DeviceState& state) override;
		};

		// A geometry shader for creating arrow heads from point list geometry
		struct ArrowHeadGS :ShaderT<ID3D11GeometryShader, ArrowHeadGS>
		{
			using base = ShaderT<ID3D11GeometryShader, ArrowHeadGS>;
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Per-model constant buffer
			float m_size;

			ArrowHeadGS(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<ID3D11GeometryShader> const& shdr);
			void Setup(ID3D11DeviceContext* dc, DeviceState& state) override;
		};
	}
}