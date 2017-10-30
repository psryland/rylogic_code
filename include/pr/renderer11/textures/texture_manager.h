//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <unordered_map>
#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/event_types.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/textures/texture2d.h"

namespace pr
{
	namespace rdr
	{
		class TextureManager
		{
			using TextureLookup        = Lookup<RdrId, Texture2D*>;
			using TexFileLookup        = Lookup<RdrId, ID3D11Texture2D*>;
			using Allocator2D          = Allocator<Texture2D>;
			using AllocationsTracker2D = AllocationsTracker<Texture2D>;

			// Textures are shared whenever possible.
			// Users have a TextureXPtr which points to a Texture instance of type TextureX
			// that internally points to a ID3D11TextureX. When a user calls CreateTextureX
			// they can provide the RdrId of an existing texture inst to create a copy of that
			// texture instance. That copy will have a pointer to the same underlying DX texture.
			// Additionally, if the texture is created from file, the 'm_lookup_fname' map allows
			// the manager to find an existing dx texture for that file. Think of the 'fname' lookup
			// as mapping from 'fname' to RdrId, and then using the tex lookup to map RdrId to dx texture.
			// 'AutoId' is a special value that tells the create texture functions to not look for an
			// existing texture and create a new dx resource for the texture.

			Allocator2D              m_alex_tex2d;       //
			AllocationsTracker2D     m_dbg_mem_tex2d;    //
			Renderer&                m_rdr;              //
			TextureLookup            m_lookup_tex;       // A map from texture id to existing texture instances
			TexFileLookup            m_lookup_fname;     // A map from hash of filepath to an existing dx texture
			pr::vector<Texture2DPtr> m_stock_textures;   // A collection of references to the stock textures
			pr::GdiPlus              m_gdiplus;          //
			EvtAutoSub               m_eh_resize;        // Event handler subscription for the RT resize event
			int                      m_gdi_dc_ref_count; // Used to detect outstanding DC references

		public:

			TextureManager(MemFuncs& mem, Renderer& rdr);
			TextureManager(TextureManager const&) = delete;
			TextureManager& operator = (TextureManager const&) = delete;

			// Create a new texture instance.
			// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
			// 'src' is the initialisation data. Use 'Image()' to create the texture without initialisation.
			// 'tdesc' is a description of the texture to be created
			// 'sdesc' is a description of the sampler to use
			Texture2DPtr CreateTexture2D(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, bool has_alpha, char const* name);

			// Create a texture instance from a DDS file.
			// 'filepath' can be a special string identifying a stock texture (e.g.  #black, #white, #checker, etc)
			// Throws if creation fails. On success returns a pointer to the created texture.
			Texture2DPtr CreateTexture2D(RdrId id, SamplerDesc const& sam_desc, wchar_t const* filepath, bool has_alpha, char const* name);
			Texture2DPtr CreateTexture2D(RdrId id, SamplerDesc const& sam_desc, char const* filepath, bool has_alpha, char const* name);

			// Create a new texture instance that wraps an existing dx texture.
			// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
			// 'existing' is an existing dx texture to wrap
			// 'sam_desc' is the sampler state description to use on the texture
			Texture2DPtr CreateTexture2D(RdrId id, ID3D11Texture2D* existing_tex, ID3D11ShaderResourceView* existing_srv, SamplerDesc const& sam_desc, bool has_alpha, char const* name);

			// Create a GDI texture instance
			// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
			// 'src' is the initialisation data
			// 'tdesc' is a description of the texture to be created
			// 'sdesc' is a description of the sampler to use
			Texture2DPtr CreateTextureGdi(RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, bool has_alpha, char const* name);
			Texture2DPtr CreateTextureGdi(RdrId id, Image const& src, bool has_alpha, char const* name);
			Texture2DPtr CreateTextureGdi(RdrId id, int w, int h, bool has_alpha, char const* name);

			// Create a new texture instance that uses the same dx texture as an existing texture.
			// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
			// 'existing' is an existing texture instance to clone
			// 'sam_desc' is an optional sampler state description to set on the clone.
			Texture2DPtr CloneTexture2D(RdrId id, Texture2D const* existing, SamplerDesc const* sam_desc, char const* name);

			// Return a pointer to an existing texture
			Texture2DPtr FindTexture(RdrId id) const
			{
				return Texture2DPtr(GetOrDefault(m_lookup_tex, id, (Texture2D*)nullptr), true);
			}

			// Return a stock texture
			Texture2DPtr FindStockTexture(EStockTexture stock);

			// Convenience method for cached textures
			template <typename Factory> Texture2DPtr GetTexture(RdrId id, Factory factory)
			{
				auto tex = FindTexture(id);
				return tex != nullptr ? tex : factory();
			}

		private:
		
			friend struct Texture2D;
			friend struct TextureGdi;
			void Delete(Texture2D* tex);

			// Create the basic textures that exist from startup
			void CreateStockTextures();

			// Updates the texture and SRV pointers in 'existing' to those provided.
			// If 'all_instances' is true, 'm_lookup_tex' is searched for Texture instances that point to the same
			// dx resource as 'existing'. All are updated to point to the given 'tex' and 'srv' and the RdrId remains unchanged.
			// If 'all_instances' is false, effectively a new entry is added to 'm_lookup_tex'. The RdrId in 'existing' is changed
			// (as if created with AutoId) and only 'existing' has its dx pointers changed. 'existing' also gets a new sort_id.
			void ReplaceTexture(Texture2D& existing, D3DPtr<ID3D11Texture2D> tex, D3DPtr<ID3D11ShaderResourceView> srv, bool all_instances);
		};
	}
}
