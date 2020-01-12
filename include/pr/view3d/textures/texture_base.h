//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/config/config.h"

namespace pr::rdr
{
	struct TextureBase :RefCounted<TextureBase>
	{
		// Notes:
		//   - A base class for all renderer texture instances.
		//   - Textures have value semantics (i.e. copyable)
		//   - Each time MatMgr.CreateTexture is called, a new texture instance is allocated.
		//     However, the resources associated with the texture may be shared with other textures.
		
		D3DPtr<ID3D11Resource>           m_res;       // The texture resource
		D3DPtr<ID3D11ShaderResourceView> m_srv;       // A shader resource view of the texture
		D3DPtr<ID3D11SamplerState>       m_samp;      // The sampler state to use with this texture
		RdrId                            m_id;        // Id for this texture in the texture managers lookup map
		RdrId                            m_src_id;    // An id identifying the source this texture was created from (needed when deleting the last ref to a dx tex)
		TextureManager*                  m_mgr;       // The texture manager that created this texture
		string32                         m_name;      // Human readable id for the texture

		TextureBase(TextureManager* mgr, RdrId id, ID3D11Resource* res, ID3D11ShaderResourceView* srv, ID3D11SamplerState* samp, RdrId src_id, char const* name);
		TextureBase(TextureManager* mgr, RdrId id, HANDLE shared_handle, RdrId src_id, char const* name);
		TextureBase(TextureManager* mgr, RdrId id, IUnknown* shared_resource, RdrId src_id, char const* name);
		virtual ~TextureBase()
		{
			OnDestruction(*this, EmptyArgs());
		}

		// Get/Set the description of the current sampler state pointed to by 'm_samp'
		// Setting a new sampler description, re-creates the sampler state
		SamplerDesc SamDesc() const;
		void SamDesc(SamplerDesc const& desc);

		// Set the filtering and address mode for this texture
		void SetFilterAndAddrMode(D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV);

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