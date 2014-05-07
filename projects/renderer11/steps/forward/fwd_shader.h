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
		// A common base class for the forward rendering shaders
		struct FwdShader :BaseShader
		{
			// Constant buffer types for the forward shaders
			#include "renderer11/shaders/hlsl/forward/forward_cbuf.hlsli"

			// Per-model constant buffer
			D3DPtr<ID3D11Buffer> m_cbuf_model;

			FwdShader(ShaderManager* mgr);
		};
	}
}