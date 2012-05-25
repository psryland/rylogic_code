//***********************************************************************
//
//	IMaterialResolver
//
//***********************************************************************

#ifndef PR_RDR_IMATERIAL_RESOLVER_H
#define PR_RDR_IMATERIAL_RESOLVER_H

#include "PR/Renderer/Materials/Material.h"
#include "PR/Renderer/Materials/Texture.h"
#include "PR/Renderer/Effects/EffectBase.h"

namespace pr
{
	namespace rdr
	{
		// Interface for 'LoadMaterial()'
		struct IMaterialResolver
		{
			// Add a material to your collection. 'index' is the index of the texture filename
			// in the 'pr::Material'. Return false to stop loading the materials
			virtual bool AddMaterial(uint index, rdr::Material material) = 0;

			// Load the effect corresponding to an effect id.
			// Return false to use the default or if the effect failed to load.
			virtual bool LoadEffect (const char* effect_id, effect::Base*& effect) = 0;

			// Load a texture corresponding to a texture filename.
			// Return false if the texture failed to load. The default texture will be used
			virtual bool LoadTexture(const char* texture_filename, rdr::Texture*& texture) = 0;
		};

	}//namespace rdr
}//namespace pr

#endif//PR_RDR_IMATERIAL_RESOLVER_H
