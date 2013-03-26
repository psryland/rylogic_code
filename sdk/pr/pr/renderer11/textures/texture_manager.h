//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_TEXTURES_TEXTURE_MANAGER_H
#define PR_RDR_TEXTURES_TEXTURE_MANAGER_H

#include <hash_map>
#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/textures/texture2d.h"


namespace pr
{
	namespace rdr
	{
		class TextureManager
		{
			typedef Lookup<RdrId, Texture2D*>       TextureLookup;
			typedef Lookup<RdrId, ID3D11Texture2D*> TexFileLookup;
			
			// Textures are shared whenever possible.
			// Users have a TextureXPtr which points to a Texture instance of type TextureX
			// that internally points to a ID3D11TextureX. When a user calls CreateTextureX
			// they can provide the RdrId of an existing texture inst to create a copy of that
			// texture instance. That copy will have a pointer to the same underlying D3D texture.
			// Additionally, if the texture is created from file, the m_lookup_fname map allows
			// the manager to find an existing d3d texture for that file. Think of the fname lookup
			// as mapping from fname to RdrId, and then using the tex lookup to map RdrId to d3d texture.
			// 'AutoId' is a special value that tells the create texture functions to not look for an
			// existing texture and create a new d3d resource for the texture.
			
			pr::rdr::Allocator<Texture2D> m_alex_tex2d;
			D3DPtr<ID3D11Device>          m_device;
			TextureLookup                 m_lookup_tex;     // A map from texture id to existing texture instances
			TexFileLookup                 m_lookup_fname;   // A map from hash of filepath to an existing d3d texture
			
			TextureManager(TextureManager const&); // no copying
			TextureManager& operator = (TextureManager const&);
			
			friend struct pr::rdr::Texture2D;
			void Delete(pr::rdr::Texture2D const* tex);
			
		public:
			TextureManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~TextureManager();
			
			// Create a texture instance.
			// 'id' is the id to assign to this texture, use AutoId if you want a new instance regardless of whether there is an existing one or not.
			// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing one.
			// If 'id' does not exist, create a new d3d texture initialised with 'data','data_size' and a new texture instance that points to this d3d texture.
			// 'pitch' is the size in bytes of the stride of 'data'.
			// If 'data' or 'data_size' is 0, the texture is left uninitialised.
			// Throws if creation fails. On success returns a pointer to the created texture.
			pr::rdr::Texture2DPtr CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, void const* data = 0);
			
			// Create a texture instance from a dds file.
			// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
			// If 'id' does not exist, creates a d3d texture corresponding to 'filepath' (loads if not already loaded)
			// If 'id' already exists, create a new texture instance (with a new id) that points to the same d3d texture as the existing texture.
			// If 'id' does not exist, get a d3d texture corresponding to 'filepath' (load if not already loaded) and 
			//  a new texture instance that points to this d3d texture.
			// If width/height are 0 the dimensions of the image file are used as the texture size.
			// Throws if creation fails. On success returns a pointer to the created texture.
			pr::rdr::Texture2DPtr CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, wchar_t const* filepath);
			pr::rdr::Texture2DPtr CreateTexture2D(RdrId id, pr::rdr::TextureDesc const& desc, char const* filepath); // define this so that the void const* version doesn't get used by accident
			
			// Return a pointer to an existing texture
			pr::rdr::Texture2DPtr FindTexture(RdrId id) const
			{
				auto i = m_lookup_tex.find(id);
				return i != m_lookup_tex.end() ? i->second : 0;
			}
		};
	}
}

#endif
