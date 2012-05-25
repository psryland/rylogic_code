//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MATERIALS_MATERIAL_H
#define PR_RDR_MATERIALS_MATERIAL_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/materials/shaders/shader.h"
#include "pr/renderer11/materials/textures/texture2d.h"

namespace pr
{
	namespace rdr
	{
		// A material for a primitive
		struct Material
		{
			ShaderPtr    m_shader;
			Texture2DPtr m_tex_diffuse;   // The basic diffuse texture
		};
	}
}

#endif
