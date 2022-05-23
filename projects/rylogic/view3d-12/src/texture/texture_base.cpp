//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_base.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/resource/descriptor_store.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/render/sortkey.h"
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
	TextureBase::TextureBase(ResourceManager& mgr, ID3D12Resource* res, TextureDesc const& desc)
		:RefCounted<TextureBase>()
		,m_mgr(&mgr)
		,m_res(res, true)
		,m_srv()
		,m_uav()
		,m_rtv()
		,m_id(desc.m_id == AutoId ? MakeId(this) : desc.m_id)
		,m_uri(desc.m_uri)
		,m_name(desc.m_name)
	{
		auto device = m_mgr->D3DDevice();
		auto tdesc = desc.m_tdesc;
		Throw(res->SetName(FmtS(L"%S", m_name.c_str())));

		// Create views for the texture
		if (!AllSet(tdesc.Flags, D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE))
		{
			// Check the texture format is supported
			D3D12_FEATURE_DATA_FORMAT_SUPPORT support = {tdesc.Format};
			Throw(device->CheckFeatureSupport(D3D12_FEATURE_FORMAT_SUPPORT, &support, sizeof(support)));
			if (!AllSet(support.Support1, D3D12_FORMAT_SUPPORT1_SHADER_SAMPLE))
				throw std::runtime_error("Texture format is not supported as a shader resource view");

			// Create the SRV
			D3D12_SHADER_RESOURCE_VIEW_DESC srv_desc = {
				.Format = tdesc.Format,
				.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D,
				.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
				.Texture2D = {
					.MostDetailedMip = 0,
					.MipLevels = s_cast<UINT>(-1),
					.PlaneSlice = 0,
					.ResourceMinLODClamp = 0.f,
				},
			};
			m_srv = m_mgr->m_descriptor_store.Create(res, srv_desc);
		}
		if (AllSet(tdesc.Flags, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS))
		{
			// Check the texture format is supported
			D3D12_FEATURE_DATA_FORMAT_SUPPORT support = {tdesc.Format};
			Throw(device->CheckFeatureSupport(D3D12_FEATURE_FORMAT_SUPPORT, &support, sizeof(support)));
			if (!AllSet(support.Support1, D3D12_FORMAT_SUPPORT1_TYPED_UNORDERED_ACCESS_VIEW) ||
				!AllSet(support.Support2, D3D12_FORMAT_SUPPORT2_UAV_TYPED_LOAD) ||
				!AllSet(support.Support2, D3D12_FORMAT_SUPPORT2_UAV_TYPED_STORE))
				throw std::runtime_error("Texture format is not supported as a unordered access view");

			// Create the UAV
			D3D12_UNORDERED_ACCESS_VIEW_DESC uav_desc = {
				.Format = tdesc.Format,
				.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D,
				.Texture2D = {
					.MipSlice = 0,
					.PlaneSlice = 0,
				},
			};
			m_uav = m_mgr->m_descriptor_store.Create(res, uav_desc);
		}
		if (AllSet(tdesc.Flags, D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET))
		{
			D3D12_FEATURE_DATA_FORMAT_SUPPORT support = {tdesc.Format};
			Throw(device->CheckFeatureSupport(D3D12_FEATURE_FORMAT_SUPPORT, &support, sizeof(support)));
			if (!AllSet(support.Support1, D3D12_FORMAT_SUPPORT1_RENDER_TARGET))
				throw std::runtime_error("Texture format is not supported as a render target view");

			// Create the RTV
			D3D12_RENDER_TARGET_VIEW_DESC rtv_desc = {
				.Format = tdesc.Format,
				.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D,
				.Texture2D = {
					.MipSlice = 0,
					.PlaneSlice = 0,
				},
			};
			m_rtv = m_mgr->m_descriptor_store.Create(res, rtv_desc);
		}
	}
	TextureBase::~TextureBase()
	{
		OnDestruction(*this, EmptyArgs());
	}

	// Return the shared handle associated with this texture
	HANDLE TextureBase::SharedHandle() const
	{
		HANDLE handle;
		D3DPtr<IDXGIResource> res;
		Throw(m_res->QueryInterface(__uuidof(IDXGIResource), (void**)&res.m_ptr));
		Throw(res->GetSharedHandle(&handle));
		return handle;
	}

	// A sort key component for this texture
	SortKeyId TextureBase::SortId() const
	{
		return m_id % SortKey::MaxTextureId;
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