//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/descriptor.h"

namespace pr::rdr12
{
	// Notes:
	//   - TextureBase (and derived objects) are lightweight, they are basically reference
	//     counted pointers to D3D resources.
	//   - A base class for all renderer texture instances.
	//   - Textures have value semantics (i.e. copyable)
	//   - Each time CreateTexture is called, a new texture instance is allocated.
	//     However, the resources associated with the texture may be shared with other textures.

	// Flags for Textures
	enum class ETextureFlag :int
	{
		None = 0,

		// The texture contains alpha pixels
		HasAlpha = 1 << 0,

		_flags_enum,
	};

	// Texture base class
	struct TextureBase :RefCounted<TextureBase>
	{
		ResourceManager*       m_mgr;    // The manager that created this texture
		D3DPtr<ID3D12Resource> m_res;    // The texture resource (possibly shared with other Texture instances)
		Descriptor             m_srv;    // Shader resource view (if available)
		Descriptor             m_uav;    // Unordered access view (if available)
		Descriptor             m_rtv;    // Render target view (if available)
		RdrId                  m_id;     // Id for this texture in the resource manager
		RdrId                  m_uri;    // An id identifying the source this texture was created from (needed when deleting the last ref to a dx tex)
		ETextureFlag           m_tflags; // Flags for boolean properties of the texture
		string32               m_name;   // Human readable id for the texture

		TextureBase(ResourceManager& mgr, ID3D12Resource* res, TextureDesc const& desc);
		virtual ~TextureBase();

		// Access the renderer
		Renderer& rdr() const;

		// A sort key component for this texture
		SortKeyId SortId() const;

		// Return the shared handle associated with this texture
		HANDLE SharedHandle() const;

		// Delegates to call when the texture is destructed
		// WARNING: Don't add lambdas that capture a ref counted pointer to the texture
		// or the texture will never get destructed, since the ref will never hit zero.
		EventHandler<TextureBase&, EmptyArgs const&, true> OnDestruction;

		// Ref counting clean up
		static void RefCountZero(RefCounted<TextureBase>* doomed);
		protected: virtual void Delete();
	};
}
