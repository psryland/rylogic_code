//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_RENDER_DRAW_METHOD_H
#define PR_RDR_RENDER_DRAW_METHOD_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/textures/texture2d.h"

namespace pr
{
	namespace rdr
	{
		// Encapsulates the resources needed to draw a render nugget
		struct DrawMethod
		{
			// The shader that does the rendering.
			pr::rdr::ShaderPtr m_shader;
			
			// The properties used by 'm_shader'.
			// Since 'm_shader' can point to any shader, not all of these properties
			// are used by every shader. The client has to know which shader they're using
			// and which properties need to be fill in for it to work correctly. Shaders
			// need to handle uninitialised properties sanely.
			pr::rdr::Texture2DPtr m_tex_diffuse; // Base diffuse texture
			pr::rdr::Texture2DPtr m_tex_env_map; // Environment map (todo: make into a cude texture)
		};
	}
}

#endif
