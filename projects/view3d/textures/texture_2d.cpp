//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/textures/texture_2d.h"
#include "pr/view3d/textures/texture_manager.h"
#include "pr/view3d/util/util.h"
#include "view3d/directxtex/directxtex.h"

namespace pr::rdr
{
	// Unique identifiers for data attached to the private data of this texture
	GUID const Texture2D::Surface0Pointer = {0x6EE0154E, 0xDEAD, 0x4E2F, 0x86, 0x9B, 0xE4, 0xD1, 0x5C, 0xA2, 0x97, 0x87};

	// Initialise 'tex.m_srv' based on the texture description
	void InitSRV(Texture2D& tex)
	{
		if (tex.m_srv != nullptr)
			return;

		Texture2DDesc tdesc;
		tex.dx_tex()->GetDesc(&tdesc);

		// If the texture can be a shader resource, create a shader resource view
		if ((tdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE) != 0)
		{
			ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
			srvdesc.Texture2D.MipLevels = tdesc.MipLevels;

			Renderer::Lock lock(tex.m_mgr->rdr());
			pr::Throw(lock.D3DDevice()->CreateShaderResourceView(tex.m_res.get(), &srvdesc, &tex.m_srv.m_ptr));
		}
	}

	// Constructors
	Texture2D::Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
		:Texture2D(mgr, id, tex, nullptr, sdesc, sort_id, has_alpha, name)
	{}
	Texture2D::Texture2D(TextureManager* mgr, RdrId id, ID3D11Texture2D* tex, ID3D11ShaderResourceView* srv, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
		:TextureBase(mgr, id, tex, srv, nullptr, 0, name)
		,m_t2s(pr::m4x4Identity)
		,m_sort_id(sort_id)
		,m_has_alpha(has_alpha)
	{
		InitSRV(*this);
		SamDesc(sdesc);
	}
	Texture2D::Texture2D(TextureManager* mgr, RdrId id, IUnknown* shared_resource, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
		:TextureBase(mgr, id, shared_resource, 0, name)
		,m_t2s(pr::m4x4Identity)
		,m_sort_id(sort_id)
		,m_has_alpha(has_alpha)
	{
		InitSRV(*this);
		SamDesc(sdesc);
	}
	Texture2D::Texture2D(TextureManager* mgr, RdrId id, HANDLE shared_handle, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name)
		:TextureBase(mgr, id, shared_handle, 0, name)
		,m_t2s(pr::m4x4Identity)
		,m_sort_id(sort_id)
		,m_has_alpha(has_alpha)
	{
		InitSRV(*this);
		SamDesc(sdesc);
	}
	Texture2D::Texture2D(TextureManager* mgr, RdrId id, Image const& src, Texture2DDesc const& tdesc, SamplerDesc const& sdesc, SortKeyId sort_id, bool has_alpha, char const* name, ShaderResourceViewDesc const* srvdesc)
		:TextureBase(mgr, id, nullptr, nullptr, nullptr, 0, name)
		,m_t2s(pr::m4x4Identity)
		,m_sort_id(sort_id)
		,m_has_alpha(has_alpha)
	{
		SamDesc(sdesc);
		TexDesc(src, tdesc, false, false, srvdesc);
	}
	Texture2D::Texture2D(TextureManager* mgr, RdrId id, Texture2D const& existing, char const* name)
		:TextureBase(mgr, id, existing.m_res.get(), existing.m_srv.get(), existing.m_samp.get(), existing.m_src_id, name)
		,m_t2s(existing.m_t2s)
		,m_sort_id(existing.m_sort_id)
		,m_has_alpha(existing.m_has_alpha)
	{}

	// Get the description of the current texture pointed to by 'm_tex'
	Texture2DDesc Texture2D::TexDesc() const
	{
		Texture2DDesc desc;
		if (dx_tex() != nullptr) dx_tex()->GetDesc(&desc);
		return desc;
	}

	// Set a new texture description and re-create/reinitialise the texture and the src.
	// 'all_instances' - if true, all Texture2D objects that refer to the same underlying dx texture
	//    get updated as well. If false, then this texture becomes a unique instance and 'm_id' is changed.
	// 'preserve' - if true, the content of the current texture is stretched on to the new texture
	//    if possible. If not possible, an exception is thrown.
	// 'srvdesc' - if not null, causes the new shader resource view to be created using this description
	void Texture2D::TexDesc(Image const& src, Texture2DDesc const& tdesc, bool all_instances, bool preserve, ShaderResourceViewDesc const* srvdesc)
	{
		Renderer::Lock lock(m_mgr->m_rdr);
		auto device = lock.D3DDevice();

		// If initialisation data is provided, see if we need to generate mip-maps
		D3DPtr<ID3D11Texture2D> tex;
		D3DPtr<ID3D11ShaderResourceView> srv;
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
		if (preserve && m_res != nullptr)
		{
			// Note: it might be tempting to do this but the effect would look the same
			// as just using the old texture, i.e. stretched. There is no stretch copy in
			// dx10+. If you do decide to support this, this is the process that's needed:
			//  - Make a render target texture of the same format as the new texture,
			//  - Use a shader set up for rendering one texture into another,
			//  - Push render states,
			//  - call 'DC->Draw()'
			//  - Restore render states
			Texture2DDesc existing_desc;
			dx_tex()->GetDesc(&existing_desc);
			if (existing_desc.Width != tdesc.Width || existing_desc.Height != tdesc.Height)
				throw std::exception("Cannot preserve content of resized textures");

			// Copy unscaled content from 
			D3DPtr<ID3D11DeviceContext> dc;
			device->GetImmediateContext(&dc.m_ptr);
			if (existing_desc.SampleDesc == tdesc.SampleDesc)
			{
				dc->CopyResource(tex.m_ptr, m_res.m_ptr);
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
		m_mgr->ReplaceTexture(*this, tex.get(), srv.get(), all_instances);
	}

	// Resize this texture to 'size' optionally applying the resize to all instances of this
	// texture and optionally preserving the current content of the texture
	void Texture2D::Resize(size_t width, size_t height, bool all_instances, bool preserve)
	{
		PR_ASSERT(PR_DBG_RDR, width*height != 0, "Do not resize textures to 0x0");
		Texture2DDesc tdesc;
		dx_tex()->GetDesc(&tdesc);
		tdesc.Width = s_cast<UINT>(width);
		tdesc.Height = s_cast<UINT>(height);
		TexDesc(Image(0,0), tdesc, all_instances, preserve);
	}

	// Access the raw pixel data of this texture.
	// If EMapFlags::DoNotWait is used, the returned image may contain a null
	// pointer for the pixel data. This is because the resource is not available.
	Image Texture2D::GetPixels(Lock& lock, UINT sub, EMap map_type, EMapFlags flags, Range range)
	{
		PR_ASSERT(PR_DBG_RDR, lock.m_dc == nullptr, "lock should be a default constructed instance. It gets 'Mapped' in this function");
		Renderer::Lock rlock(m_mgr->rdr());

		// Get the texture info
		auto tdesc = TexDesc();

		// Create the image
		Image img(static_cast<int>(tdesc.Width), static_cast<int>(tdesc.Height), nullptr, tdesc.Format);
		
		// Access the pixel data
		if (!lock.Map(rlock.ImmediateDC(), dx_tex(), sub, img.m_pitch.x, map_type, flags, range))
			return img;

		// Set the image data pointer
		img.m_pixels = lock.data();
		return img;
	}

	// Get the GDI DC from the surface
	HDC Texture2D::GetDC(bool discard)
	{
		HDC dc;
		D3DPtr<IDXGISurface2> surf;
		pr::Throw(dx_tex()->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
		pr::Throw(surf->GetDC(discard, &dc), "GetDC can only be called for textures that were created with the D3D11_RESOURCE_MISC_GDI_COMPATIBLE flag");
		++m_mgr->m_gdi_dc_ref_count;
		return dc;
	}

	// Release the GDI DC from the surface
	void Texture2D::ReleaseDC()
	{
		D3DPtr<IDXGISurface2> surf;
		pr::Throw(dx_tex()->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
		pr::Throw(surf->ReleaseDC(nullptr));
		--m_mgr->m_gdi_dc_ref_count;
		// Note: the main RT must be restored once all ReleaseDC's have been called
	}

	// Get the DXGI surface within this texture
	D3DPtr<IDXGISurface> Texture2D::GetSurface()
	{
		D3DPtr<IDXGISurface> surf;
		pr::Throw(dx_tex()->QueryInterface(&surf.m_ptr));
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

}