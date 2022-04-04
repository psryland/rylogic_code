//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12
{
	struct TextureBase :RefCounted<TextureBase>
	{
		// Notes:
		//   - TextureBase (and derived objects) are lightweight, they are basically reference
		//     counted pointers to D3D resources.
		//   - A base class for all renderer texture instances.
		//   - Textures have value semantics (i.e. copyable)
		//   - Each time CreateTexture is called, a new texture instance is allocated.
		//     However, the resources associated with the texture may be shared with other textures.

		ResourceManager*                 m_mgr;       // The manager that created this texture
		D3DPtr<ID3D12Resource>           m_res;       // The texture resource (possibly shared with other Texture instances)
		//D3DPtr<ID3D11ShaderResourceView> m_srv;       // A shader resource view of the texture
		//D3DPtr<ID3D11SamplerState>       m_samp;      // The sampler state to use with this texture
		RdrId                            m_id;        // Id for this texture in the resource manager
		RdrId                            m_uri;    // An id identifying the source this texture was created from (needed when deleting the last ref to a dx tex)
		//string32                         m_name;      // Human readable id for the texture

		TextureBase(ResourceManager& mgr, RdrId id, ID3D12Resource* res, RdrId uri); //, char const* name, ID3D12ShaderResourceView* srv, ID3D12SamplerState* samp, 
		//TextureBase(ResourceManager* mgr, RdrId id, HANDLE shared_handle, RdrId src_id, char const* name);
		//TextureBase(ResourceManager* mgr, RdrId id, IUnknown* shared_resource, RdrId src_id, char const* name);
		virtual ~TextureBase()
		{
			OnDestruction(*this, EmptyArgs());
		}

		//// Get/Set the description of the current sampler state pointed to by 'm_samp'
		//// Setting a new sampler description, re-creates the sampler state
		//SamplerDesc SamDesc() const;
		//void SamDesc(SamplerDesc const& desc);

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
