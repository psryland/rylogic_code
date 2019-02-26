//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/util/util.h"
#include "renderer11/directxtex/directxtex.h"

namespace pr
{
	namespace rdr
	{
		Texture2D::Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
			:Texture2D(mgr, id, tex, nullptr, sdesc, sort_id, has_alpha, name)
		{}
		Texture2D::Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
			:m_t2s(pr::m4x4Identity)
			,m_tex(tex, true)
			,m_srv(srv, true)
			,m_samp()
			,m_id(id == AutoId ? MakeId(this) : id)
			,m_src_id()
			,m_sort_id(sort_id)
			,m_has_alpha(has_alpha)
			,m_mgr(mgr)
			,m_name(name ? name : "")
		{
			if (m_srv == nullptr)
			{
				TextureDesc tdesc;
				tex->GetDesc(&tdesc);

				// If the texture can be a shader resource, create a shader resource view
				if ((tdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE) != 0)
				{
					ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
					srvdesc.Texture2D.MipLevels = tdesc.MipLevels;

					Renderer::Lock lock(mgr->m_rdr);
					pr::Throw(lock.D3DDevice()->CreateShaderResourceView(tex, &srvdesc, &m_srv.m_ptr));
				}
			}
			SamDesc(sdesc);
		}
		Texture2D::Texture2D(TextureManager* mgr, RdrId id, IUnknown* shared_resource, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
			:m_t2s(pr::m4x4Identity)
			,m_tex()
			,m_srv()
			,m_samp()
			,m_id(id == AutoId ? MakeId(this) : id)
			,m_src_id()
			,m_sort_id(sort_id)
			,m_has_alpha(has_alpha)
			,m_mgr(mgr)
			,m_name(name ? name : "")
		{
			Renderer::Lock lock(m_mgr->m_rdr);

			// Get the DXGI resource interface for the shared resource
			D3DPtr<IDXGIResource> dxgi_resource;
			pr::Throw(shared_resource->QueryInterface(__uuidof(IDXGIResource), (void**)&dxgi_resource.m_ptr));

			// Get the handled of the shared resource so that we can open it with our d3d device
			HANDLE shared_handle;
			pr::Throw(dxgi_resource->GetSharedHandle(&shared_handle));

			// Open the shared resource in our d3d device
			D3DPtr<IUnknown> resource;
			pr::Throw(lock.D3DDevice()->OpenSharedResource(shared_handle, __uuidof(ID3D11Resource), (void**)&resource.m_ptr));

			// Query the texture interface from the resource
			pr::Throw(resource->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&m_tex.m_ptr));

			// Read the properties of the texture
			D3D11_TEXTURE2D_DESC tdesc;
			m_tex->GetDesc(&tdesc);

			// If the texture was created with shader binding, get the srv
			// If the texture can be a shader resource, create a shader resource view
			if ((tdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE) != 0)
			{
				ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
				srvdesc.Texture2D.MipLevels = tdesc.MipLevels;
				pr::Throw(lock.D3DDevice()->CreateShaderResourceView(m_tex.get(), &srvdesc, &m_srv.m_ptr));
			}

			// Save the sampler description
			SamDesc(sdesc);
		}
		Texture2D::Texture2D(TextureManager* mgr, RdrId id, Image const& src, TextureDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name, ShaderResourceViewDesc const* srvdesc)
			:m_t2s(pr::m4x4Identity)
			,m_tex()
			,m_srv()
			,m_samp()
			,m_id(id == AutoId ? MakeId(this) : id)
			,m_src_id()
			,m_sort_id(sort_id)
			,m_has_alpha(has_alpha)
			,m_mgr(mgr)
			,m_name(name ? name : "")
		{
			TexDesc(src, tdesc, false, false, srvdesc);
			SamDesc(sdesc);
		}
		Texture2D::Texture2D(TextureManager* mgr, RdrId id, Texture2D const& existing, SortKeyId sort_id, char const* name)
			:m_t2s(existing.m_t2s)
			,m_tex(existing.m_tex)
			,m_srv(existing.m_srv)
			,m_samp(existing.m_samp)
			,m_id(id == AutoId ? MakeId(this) : id)
			,m_src_id(existing.m_src_id)
			,m_sort_id(sort_id)
			,m_has_alpha(existing.m_has_alpha)
			,m_mgr(mgr)
			,m_name(name ? name : "")
		{}

		// Get the description of the current texture pointed to by 'm_tex'
		TextureDesc Texture2D::TexDesc() const
		{
			TextureDesc desc;
			if (m_tex != nullptr) m_tex->GetDesc(&desc);
			return desc;
		}

		// Set a new texture description and re-create/reinitialise the texture and the src.
		// 'all_instances' - if true, all Texture2D objects that refer to the same underlying
		//  dx texture get updated as well. If false, then this texture becomes a unique instance
		//  and 'm_id' is changed.
		// 'perserve' - if true, the content of the current texture is stretched on to the new texture
		//  if possible. If not possible, an exception is thrown
		// 'srvdesc' - if not null, causes the new shader resource view to be created using this description
		void Texture2D::TexDesc(Image const& src, TextureDesc const& tdesc, bool all_instances, bool preserve, ShaderResourceViewDesc const* srvdesc)
		{
			D3DPtr<ID3D11Texture2D> tex;
			D3DPtr<ID3D11ShaderResourceView> srv;
			Renderer::Lock lock(m_mgr->m_rdr);
			auto device = lock.D3DDevice();

			// If initialisation data is provided, see if we need to generate mip-maps
			if (src.m_pixels != nullptr)
			{
				DirectX::Image img;
				img.width      = src.m_dim.x;
				img.height     = src.m_dim.y;
				img.format     = src.m_format;
				img.rowPitch   = src.m_pitch.x;
				img.slicePitch = src.m_pitch.y;
				img.pixels     = static_cast<uint8_t*>(const_cast<void*>(src.m_pixels));

				DirectX::ScratchImage scratch;
				if (tdesc.MipLevels != 1)
				{
					// This might be needed, but not always.
					// PR_ASSERT(PR_DBG_RDR, pr::CoInitializeCalled(), "'CoInitialize' has not been called.");
					pr::Throw(DirectX::GenerateMipMaps(img, DirectX::TEX_FILTER_FANT, tdesc.MipLevels, scratch));
				}
				else
				{
					scratch.InitializeFromImage(img, false);
				}

				// Create the texture with the initialisation data
				D3DPtr<ID3D11Resource> res;
				pr::Throw(DirectX::CreateTexture(device, scratch.GetImages(), scratch.GetImageCount(), scratch.GetMetadata(), &res.m_ptr));
				pr::Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
				
				// If the texture is to be used in shaders, create a SRV
				if ((tdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE) != 0)
				{
					if (srvdesc)
						pr::Throw(device->CreateShaderResourceView(tex.m_ptr, srvdesc, &srv.m_ptr));
					else
						pr::Throw(DirectX::CreateShaderResourceView(device, scratch.GetImages(), scratch.GetImageCount(), scratch.GetMetadata(), &srv.m_ptr));
				}
			}
			else
			{
				// Create an uninitialised texture
				pr::Throw(device->CreateTexture2D(&tdesc, nullptr, &tex.m_ptr));
				
				// If the texture is to be used in shaders, create a SRV
				if ((tdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE) != 0)
					pr::Throw(device->CreateShaderResourceView(tex.m_ptr, srvdesc, &srv.m_ptr));
			}

			// Copy the surface data from the existing texture
			if (preserve && m_tex != nullptr)
			{
				// Note: it might be tempting to do this but the effect would look the same
				// as just using the old texture, i.e. stretched. There is no stretch copy in
				// dx10+. If you do decide to support this, this is the process that's needed:
				//  - Make a render target texture of the same format as the new texture,
				//  - Use a shader set up for rendering one texture into another,
				//  - Push render states,
				//  - call 'DC->Draw()'
				//  - Restore render states
				TextureDesc existing_desc; m_tex->GetDesc(&existing_desc);
				if (existing_desc.Width != tdesc.Width || existing_desc.Height != tdesc.Height)
					throw std::exception("Cannot preserve content of resized textures");

				// Copy unscaled content from 
				D3DPtr<ID3D11DeviceContext> dc;
				device->GetImmediateContext(&dc.m_ptr);
				if (existing_desc.SampleDesc == tdesc.SampleDesc)
				{
					dc->CopyResource(tex.m_ptr, m_tex.m_ptr);
				}
				else
				{
					throw std::exception("not implemented");
					///for (UINT slice = 0; slice != existing_desc.MipLevels; ++slice)
					///	for (UINT arr = 0; arr != existing_desc.ArraySize; ++arr)
					///		dc->ResolveSubresource(tex.m_ptr, D3D11CalcSubresource(slice, arr, existing_desc.MipLevels);
					
				}
			}

			// Update the texture and optionally all instances of the same dx texture
			m_mgr->ReplaceTexture(*this, tex, srv, all_instances);
		}

		// Returns a description of the current sampler state pointed to by 'm_samp'
		SamplerDesc Texture2D::SamDesc() const
		{
			SamplerDesc desc;
			if (m_samp != nullptr) m_samp->GetDesc(&desc);
			return desc;
		}
		void Texture2D::SamDesc(SamplerDesc const& desc)
		{
			Renderer::Lock lock(m_mgr->m_rdr);
			D3DPtr<ID3D11SamplerState> samp_state;
			pr::Throw(lock.D3DDevice()->CreateSamplerState(&desc, &samp_state.m_ptr));
			m_samp = samp_state;
		}

		// Set the filtering and address mode for this texture
		void Texture2D::SetFilterAndAddrMode(D3D11_FILTER filter, D3D11_TEXTURE_ADDRESS_MODE addrU, D3D11_TEXTURE_ADDRESS_MODE addrV)
		{
			SamplerDesc desc;
			m_samp->GetDesc(&desc);
			desc.Filter = filter;
			desc.AddressU = addrU;
			desc.AddressV = addrV;

			Renderer::Lock lock(m_mgr->m_rdr);
			D3DPtr<ID3D11SamplerState> samp;
			pr::Throw(lock.D3DDevice()->CreateSamplerState(&desc, &samp.m_ptr));
			m_samp = samp;
		}

		// Resize this texture to 'size' optionally applying the resize to all instances of this
		// texture and optionally preserving the current content of the texture
		void Texture2D::Resize(size_t width, size_t height, bool all_instances, bool preserve)
		{
			PR_ASSERT(PR_DBG_RDR, width*height != 0, "Do not resize textures to 0x0");
			TextureDesc tdesc;
			m_tex->GetDesc(&tdesc);
			tdesc.Width = checked_cast<UINT>(width);
			tdesc.Height = checked_cast<UINT>(height);
			TexDesc(Image(0,0), tdesc, all_instances, preserve);
		}

		// Get the GDI DC from the surface
		HDC Texture2D::GetDC(bool discard)
		{
			HDC dc;
			D3DPtr<IDXGISurface2> surf;
			pr::Throw(m_tex->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
			pr::Throw(surf->GetDC(discard, &dc), "GetDC can only be called for textures that were created with the D3D11_RESOURCE_MISC_GDI_COMPATIBLE flag");
			++m_mgr->m_gdi_dc_ref_count;
			return dc;
		}

		// Release the GDI DC from the surface
		void Texture2D::ReleaseDC()
		{
			D3DPtr<IDXGISurface2> surf;
			pr::Throw(m_tex->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
			pr::Throw(surf->ReleaseDC(nullptr));
			--m_mgr->m_gdi_dc_ref_count;
			// Note: the main RT must be restored once all ReleaseDC's have been called
		}

		// Get the DXGI surface within this texture
		D3DPtr<IDXGISurface> Texture2D::GetSurface()
		{
			D3DPtr<IDXGISurface> surf;
			pr::Throw(m_tex->QueryInterface(&surf.m_ptr));
			return surf;
		}

		// Get a d2d render target for the DXGI surface within this texture
		D3DPtr<ID2D1RenderTarget> Texture2D::GetD2DRenderTarget()
		{
			Renderer::Lock lock(m_mgr->m_rdr); 
			auto surf = GetSurface();

			// Create render target properties
			auto props = D2D1::RenderTargetProperties(D2D1_RENDER_TARGET_TYPE_DEFAULT, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED));
			auto dpi = m_mgr->m_rdr.Dpi();
			props.dpiX = dpi.x;
			props.dpiY = dpi.y;

			// Create a D2D render target which can draw into our off screen D3D surface.
			D3DPtr<ID2D1RenderTarget> rt;
			auto d2dfactory = lock.D2DFactory();
			pr::Throw(d2dfactory->CreateDxgiSurfaceRenderTarget(surf.m_ptr, props, &rt.m_ptr));
			return rt;
		}

		// Get a D2D device context for the DXGI surface within this texture
		D3DPtr<ID2D1DeviceContext> Texture2D::GetD2DeviceContext()
		{
			Renderer::Lock lock(m_mgr->m_rdr);
			auto surf = GetSurface();
			auto d3d_device = lock.D3DDevice();
			auto d2d_device = lock.D2DDevice();

			// Get the DXGI Device from the d3d device
			D3DPtr<IDXGIDevice> dxgi_device;
			pr::Throw(d3d_device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));

			// Create a device context
			D3DPtr<ID2D1DeviceContext> d2d_dc;
			pr::Throw(d2d_device->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &d2d_dc.m_ptr));

			// Create a bitmap wrapper for 'surf'
			D3DPtr<ID2D1Bitmap1> target;
			auto bp = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED));
			auto dpi = m_mgr->m_rdr.Dpi();
			bp.dpiX = dpi.x;
			bp.dpiY = dpi.y;
			pr::Throw(d2d_dc->CreateBitmapFromDxgiSurface(surf.m_ptr, bp, &target.m_ptr));
			
			// Set the render target
			d2d_dc->SetTarget(target.get());
			return d2d_dc;
		}

		// Ref counting clean up function
		void Texture2D::RefCountZero(pr::RefCount<Texture2D>* doomed)
		{
			Texture2D* tex = static_cast<Texture2D*>(doomed);
			tex->Delete();
		}
		void Texture2D::Delete()
		{
			m_mgr->Delete(this);
		}
	}
}