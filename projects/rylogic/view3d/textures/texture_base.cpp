//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/textures/texture_base.h"
#include "pr/view3d/textures/texture_manager.h"

namespace pr::rdr
{
	// Get the shared handle from a shared resource
	HANDLE SharedHandleFromSharedResource(IUnknown* shared_resource)
	{
		// Get the DXGI resource interface for the shared resource
		D3DPtr<IDXGIResource> dxgi_resource;
		Check(shared_resource->QueryInterface(__uuidof(IDXGIResource), (void**)dxgi_resource.address_of()));

		// Get the handled of the shared resource so that we can open it with our d3d device
		HANDLE shared_handle;
		Check(dxgi_resource->GetSharedHandle(&shared_handle));

		return shared_handle;
	}

	// Constructors
	TextureBase::TextureBase(TextureManager* mgr, RdrId id, ID3D11Resource* res, ID3D11ShaderResourceView* srv, ID3D11SamplerState* samp, RdrId src_id, char const* name)
		:RefCounted<TextureBase>()
		,m_res(res, true)
		,m_srv(srv, true)
		,m_samp(samp, true)
		,m_id(id == AutoId ? MakeId(this) : id)
		,m_src_id(src_id)
		,m_mgr(mgr)
		,m_name(name ? name : "")
	{
	}
	TextureBase::TextureBase(TextureManager* mgr, RdrId id, HANDLE shared_handle, RdrId src_id, char const* name)
		:TextureBase(mgr, id, nullptr, nullptr, nullptr, src_id, name)
	{
		// Open the shared resource in our d3d device
		D3DPtr<IUnknown> resource;
		Renderer::Lock lock(m_mgr->m_rdr);
		Check(lock.D3DDevice()->OpenSharedResource(shared_handle, __uuidof(ID3D11Resource), (void**)resource.address_of()));

		// Query the resource interface from the resource
		Check(resource->QueryInterface(__uuidof(ID3D11Resource), (void**)m_res.address_of()));
	}
	TextureBase::TextureBase(TextureManager* mgr, RdrId id, IUnknown* shared_resource, RdrId src_id, char const* name)
		:TextureBase(mgr, id, SharedHandleFromSharedResource(shared_resource), src_id, name)
	{
	}

	// Returns a description of the current sampler state pointed to by 'm_samp'
	SamplerDesc TextureBase::SamDesc() const
	{
		SamplerDesc desc;
		if (m_samp != nullptr) m_samp->GetDesc(&desc);
		return desc;
	}
	void TextureBase::SamDesc(SamplerDesc const& desc)
	{
		Renderer::Lock lock(m_mgr->m_rdr);
		D3DPtr<ID3D11SamplerState> samp_state;
		pr::Check(lock.D3DDevice()->CreateSamplerState(&desc, samp_state.address_of()));
		m_samp = samp_state;
	}

	// Set the filtering and address mode for this texture
	void TextureBase::SetFilterAndAddrMode(D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV)
	{
		SamplerDesc desc;
		m_samp->GetDesc(&desc);
		desc.Filter = filter;
		desc.AddressU = addrU;
		desc.AddressV = addrV;

		Renderer::Lock lock(m_mgr->m_rdr);
		D3DPtr<ID3D11SamplerState> samp;
		pr::Check(lock.D3DDevice()->CreateSamplerState(&desc, samp.address_of()));
		m_samp = samp;
	}

	// Return the shared handle associated with this texture
	HANDLE TextureBase::SharedHandle() const
	{
		HANDLE handle;
		D3DPtr<IDXGIResource> res;
		pr::Check(m_res->QueryInterface(__uuidof(IDXGIResource), (void**)res.address_of()));
		pr::Check(res->GetSharedHandle(&handle));
		return handle;
	}

	// Ref counting clean up function
	void TextureBase::RefCountZero(RefCounted<TextureBase>* doomed)
	{
		auto tex = static_cast<TextureBase*>(doomed);
		tex->Delete();
	}
	void TextureBase::Delete()
	{
		m_mgr->Delete(this);
	}
}