//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_MATERIAL_H
#define PR_RDR_MATERIAL_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer/materials/textures/texture.h"

namespace pr
{
	namespace rdr
	{
		// An object that references an effect and its parameters
		struct Material
		{
			pr::rdr::EffectPtr  m_effect;          // Effect used to render this material. Effect render states set next
			pr::rdr::TexturePtr m_diffuse_texture; // Diffuse texture. Texture render states set first
			pr::rdr::TexturePtr m_envmap_texture;  // Environment map texture.
			pr::rdr::rs::Block  m_rsb;             // Additional render states. Set last.
			
			// Future parameters
			// Note, I tried making an EffectParams block that contained the diffuse texture plus
			// future parameters, there's no point. This is a config struct, the client should take
			// a copy of the default material and change the bits they need. For packages, the material
			// objects are in the nuggets, when loading the package the material objects can be filled out
			// directly, materials don't need to be stored in pre-setup form.
			
			//static Material make(pr::rdr::EffectPtr effect, pr::rdr::TexturePtr& diffuse_texture) { Material m = {effect, diffuse_texture, 0}; return m; }
		};
	}
}

#endif
