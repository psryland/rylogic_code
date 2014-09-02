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
		// A geometry shader for creating thick lines from linelist geometry
		struct ThickLineListShaderGS :Shader<ID3D11GeometryShader, ThickLineListShaderGS>
		{
			typedef Shader<ID3D11GeometryShader, ThickLineListShaderGS> base;
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Per-model constant buffer
			float m_default_width;
			
			ThickLineListShaderGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr);
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state) override;
		};

		// A geometry shader for creating arrow heads from pointlist geometry
		struct ArrowHeadShaderGS :Shader<ID3D11GeometryShader, ArrowHeadShaderGS>
		{
			typedef Shader<ID3D11GeometryShader, ArrowHeadShaderGS> base;
			D3DPtr<ID3D11Buffer> m_cbuf_model; // Per-model constant buffer
			float m_default_width;

			ArrowHeadShaderGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr);
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state) override;
		};
	}
}