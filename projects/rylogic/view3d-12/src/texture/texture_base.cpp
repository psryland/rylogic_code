//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_base.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/utility/utility.h"

namespace pr::rdr12
{
	// Get the shared handle from a shared resource
	HANDLE SharedHandleFromSharedResource(IUnknown* shared_resource)
	{
		// Get the DXGI resource interface for the shared resource
		D3DPtr<IDXGIResource> dxgi_resource;
		Throw(shared_resource->QueryInterface<IDXGIResource>(&dxgi_resource.m_ptr));

		// Get the handled of the shared resource so that we can open it with our d3d device
		HANDLE shared_handle;
		Throw(dxgi_resource->GetSharedHandle(&shared_handle));
		return shared_handle;
	}

	// Constructors
	TextureBase::TextureBase(ResourceManager& mgr, RdrId id, ID3D12Resource* res, RdrId uri)
		:RefCounted<TextureBase>()
		,m_mgr(&mgr)
		,m_res(res, true)
		//,m_srv(srv, true)
		//,m_samp(samp, true)
		,m_id(id == AutoId ? MakeId(this) : id)
		,m_uri(uri)
		//,m_name(name ? name : "")
	{
	}
	//TextureBase::TextureBase(ResourceManager* mgr, RdrId id, HANDLE shared_handle, RdrId src_id, char const* name)
	//	:TextureBase(mgr, id, static_cast<ID3D12Resource*>(nullptr), src_id, name)
	//{
	//	//todo
	//	(void)shared_handle;

	//	Renderer::Lock lock(m_mgr->rdr());
	//	auto device = lock.D3DDevice();
	//	(void)device;

	//	// Open the shared resource in our d3d device
	//	D3DPtr<IUnknown> resource;
	//	//Throw(device->OpenSharedResource(shared_handle, __uuidof(ID3D11Resource), (void**)&resource.m_ptr));
	//	throw std::runtime_error("not implemented");

	//	// Query the resource interface from the resource
	//	Throw(resource->QueryInterface(__uuidof(ID3D11Resource), (void**)&m_res.m_ptr));
	//}
	//TextureBase::TextureBase(ResourceManager* mgr, RdrId id, IUnknown* shared_resource, RdrId src_id, char const* name)
	//	:TextureBase(mgr, id, SharedHandleFromSharedResource(shared_resource), src_id, name)
	//{}

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