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
#include "pr/renderer11/textures/text_manager.h"
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
			// texture instance. That copy will have a pointer to the same underlying DX texture.
			// Additionally, if the texture is created from file, the m_lookup_fname map allows
			// the manager to find an existing dx texture for that file. Think of the fname lookup
			// as mapping from fname to RdrId, and then using the tex lookup to map RdrId to dx texture.
			// 'AutoId' is a special value that tells the create texture functions to not look for an
			// existing texture and create a new d3d resource for the texture.

			pr::rdr::Allocator<Texture2D> m_alex_tex2d;
			D3DPtr<ID3D11Device>          m_device;
			TextureLookup                 m_lookup_tex;     // A map from texture id to existing texture instances
			TexFileLookup                 m_lookup_fname;   // A map from hash of filepath to an existing d3d texture
			pr::Array<Texture2DPtr>       m_stock_textures; // A collection of references to the stock textures

			TextureManager(TextureManager const&); // no copying
			TextureManager& operator = (TextureManager const&);

			friend struct pr::rdr::Texture2D;
			void Delete(pr::rdr::Texture2D const* tex);

			// Generates the stock textures
			void CreateStockTextures();

		public:
			TextureManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~TextureManager();

			// Create a new texture instance.
			// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
			// 'src' is the initialisation data
			// 'tdesc' is a description of the texture to be created
			// 'sdesc' is a description of the sampler to use
			Texture2DPtr CreateTexture2D(RdrId id, Image const& src, pr::rdr::TextureDesc const& tdesc, pr::rdr::SamplerDesc const& sdesc);

			// Create a new texture instance that uses the same d3d texture as an existing texture.
			// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
			// 'existing' is an existing texture instance to clone
			// 'sam_desc' is an optional sampler state description to set on the clone.
			Texture2DPtr CloneTexture2D(RdrId id, Texture2DPtr const& existing, pr::rdr::SamplerDesc const* sam_desc = nullptr);

			// Create a texture instance from a dds file.
			// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
			// Throws if creation fails. On success returns a pointer to the created texture.
			pr::rdr::Texture2DPtr CreateTexture2D(RdrId id, pr::rdr::SamplerDesc const& sam_desc, wchar_t const* filepath);
			pr::rdr::Texture2DPtr CreateTexture2D(RdrId id, pr::rdr::SamplerDesc const& sam_desc, char const* filepath);

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
