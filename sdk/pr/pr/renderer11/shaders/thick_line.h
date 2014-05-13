//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
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
			float m_default_linewidth;
			
			ThickLineListShaderGS(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<ID3D11GeometryShader> shdr);

			// Setup the shader ready to be used on 'dle'
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state) override;
		};
	}
}