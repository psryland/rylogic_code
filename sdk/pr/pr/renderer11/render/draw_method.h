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
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/raster_state.h"

namespace pr
{
	namespace rdr
	{
		// Encapsulates the resources needed to draw a render nugget
		// These were 'materials' in the old renderer
		struct DrawMethod
		{
			// The shader that does the rendering.
			ShaderPtr m_shader;

			// The properties used by 'm_shader'.
			// Since 'm_shader' can point to any shader, not all of these properties
			// are used by every shader. The client has to know which shader they're using
			// and which properties need to be fill in for it to work correctly. Shaders
			// need to handle uninitialized properties sanely.
			Texture2DPtr m_tex_diffuse; // Base diffuse texture
			Texture2DPtr m_tex_env_map; // Environment map (todo: make into a cube texture)

			// Rendering states
			BSBlock m_bsb;
			DSBlock m_dsb;
			RSBlock m_rsb;

			DrawMethod(ShaderPtr shader = 0, Texture2DPtr diffuse = 0, Texture2DPtr env_map = 0)
				:m_shader(shader)
				,m_tex_diffuse(diffuse)
				,m_tex_env_map(env_map)
				,m_bsb()
				,m_dsb()
				,m_rsb()
			{}
		};
	}
}

#endif
