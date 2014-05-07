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
		// A common base class for the deferred rendering shaders
		struct DSShader :BaseShader
		{
			// The constant buffer definitions
			#include "renderer11/shaders/hlsl/deferred/gbuffer_cbuf.hlsli"

			DSShader(ShaderManager* mgr);
		};
	}
}