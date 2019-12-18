//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <unordered_map>
#include "pr/view3d/forward.h"
#include "pr/view3d/util/allocator.h"
#include "pr/view3d/util/lookup.h"
#include "pr/view3d/util/event_args.h"
#include "pr/view3d/util/stock_resources.h"
#include "pr/view3d/textures/texture_2d.h"

namespace pr::rdr
{
	class TextureManager
	{
		using DxTexPointers      = struct { ID3D11Resource* res; ID3D11ShaderResourceView* srv; };
		using TextureLookup      = Lookup<RdrId, TextureBase*>;
		using DxTexLookup        = Lookup<RdrId, DxTexPointers>;
		using AllocationsTracker = AllocationsTracker<void>;

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

		MemFuncs                 m_mem_funcs;        // Allocation function pointers
		AllocationsTracker       m_mem_tracker;      // Texture allocation tracker
		Renderer&                m_rdr;              // The owning renderer instance
		TextureLookup            m_lookup_tex;       // A map from texture id to existing texture instances
		DxTexLookup              m_lookup_dxtex;     // A map from hash of resource uri to existing dx texture pointers
		pr::vector<Texture2DPtr> m_stock_textures;   // A collection of references to the stock textures
		pr::GdiPlus              m_gdiplus;          // Context scope for GDI
		AutoSub                  m_eh_resize;        // Event handler subscription for the RT resize event
		int                      m_gdi_dc_ref_count; // Used to detect outstanding DC references

	public:

		TextureManager(MemFuncs mem, Renderer& rdr);
		TextureManager(TextureManager const&) = delete;
		TextureManager& operator = (TextureManager const&) = delete;

		// Renderer access
		Renderer& rdr() const { return m_rdr; }

		// Create a new texture instance.
		// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
		// 'src' is the initialisation data. Use 'Image()' to create the texture without initialisation.
		// 'tdesc' is a description of the texture to be created
		// 'sdesc' is a description of the sampler to use
		Texture2DPtr CreateTexture2D(RdrId id, Image const& src, Texture2DDesc const& tdesc, SamplerDesc const& sdesc, bool has_alpha, char const* name);

		// Create a texture instance from a filepath, embedded resource, or stock texture id.
		// 'resource_path' has the following formats:
		// '#<stock_texture_name>' - '#' indicates stock texture, the remaining text is the stock texture name  (e.g.  #black, #white, #checker, etc)
		// '@<module>:<resource_type>:<resource_name>' - '@' indicates embedded resource, <module> and <resource_type> are optional  (e.g. @::tex_resource, @00FE123:jpg:tex_resource)
		// '<filepath>' - All other strings are interpreted as filepaths.
		// Throws if creation fails. On success returns a pointer to the created texture.
		Texture2DPtr CreateTexture2D(RdrId id, wchar_t const* resource_path, SamplerDesc const& sam_desc, bool has_alpha, char const* name);

		// Create a new texture instance that wraps an existing dx texture.
		// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
		// 'existing' is an existing dx texture to wrap
		// 'sam_desc' is the sampler state description to use on the texture
		Texture2DPtr CreateTexture2D(RdrId id, ID3D11Texture2D* existing_tex, ID3D11ShaderResourceView* existing_srv, SamplerDesc const& sam_desc, bool has_alpha, char const* name);

		// Create a new texture instance that wraps a shared texture resource.
		// 'shared_resource' is a resource created on another d3d device (possibly dx9,dx10,etc).
		Texture2DPtr CreateTexture2D(RdrId id, IUnknown* shared_resource, SamplerDesc const& sdesc, bool has_alpha, char const* name);

		// Create a GDI texture instance
		// 'id' is the id to assign to the created texture instance. Use 'AutoId' to auto generate an id
		// 'src' is the initialisation data
		// 'tdesc' is a description of the texture to be created
		// 'sdesc' is a description of the sampler to use
		Texture2DPtr CreateTextureGdi(RdrId id, Image const& src, Texture2DDesc const& tdesc, SamplerDesc const& sdesc, bool has_alpha, char const* name);
		Texture2DPtr CreateTextureGdi(RdrId id, Image const& src, bool has_alpha, char const* name);
		Texture2DPtr CreateTextureGdi(RdrId id, int w, int h, bool has_alpha, char const* name);

		// Create a cube map texture instance
		TextureCubePtr CreateTextureCube(RdrId id, wchar_t const* resource_name, SamplerDesc const& sdesc, char const* name);

		// Create a new texture instance that uses the same dx texture as an existing texture.
		// 'id' is the id to assign to this new texture instance. Use 'AutoId' to auto generate an id
		// 'existing' is an existing texture instance to clone
		// 'sam_desc' is an optional sampler state description to set on the clone.
		Texture2DPtr CloneTexture2D(RdrId id, Texture2D const* existing, SamplerDesc const* sam_desc, char const* name);

		// Create a texture that references a shared resource
		Texture2DPtr OpenSharedTexture2D(RdrId id, HANDLE shared_handle, SamplerDesc const& sdesc, bool has_alpha, char const* name);

		// Return a stock texture
		Texture2DPtr FindStockTexture(EStockTexture stock);

		// Return a pointer to an existing texture
		template <typename TextureType, typename = std::enable_if_t<std::is_base_of_v<TextureBase, TextureType>>>
		pr::RefPtr<TextureType> FindTexture(RdrId id) const
		{
			auto tex = GetOrDefault(m_lookup_tex, id, (TextureBase*)nullptr);
			return pr::RefPtr<TextureType>(static_cast<TextureType*>(tex), true);
		}

		// Convenience method for cached textures
		template <typename TextureType, typename Factory, typename = std::enable_if_t<std::is_base_of_v<TextureBase, TextureType>>>
		pr::RefPtr<TextureType> GetTexture(RdrId id, Factory factory)
		{
			auto tex = FindTexture<TextureType>(id);
			return tex != nullptr ? tex : factory();
		}

	private:
		
		friend struct TextureBase;
		friend struct Texture2D;
		friend struct TextureGdi;
		friend struct TextureCube;

		// Clean up a texture
		void Delete(TextureBase* tex);

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
