//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"

namespace pr::rdr12
{
	Texture2D::Texture2D(ResourceManager& mgr, ID3D12Resource* res, TextureDesc const& desc)
		:TextureBase(mgr, res, desc)
		,m_t2s(m4x4::Identity())
		,m_has_alpha(desc.m_has_alpha)
	{}

	// Get the GDI DC from the surface
	HDC Texture2D::GetDC(bool discard)
	{
		HDC dc;
		D3DPtr<IDXGISurface2> surf;
		pr::Throw(m_res->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
		pr::Throw(surf->GetDC(discard, &dc), "GetDC can only be called for textures that are GDI compatible");
//		++m_mgr->m_gdi_dc_ref_count;
		return dc;
	}

	// Release the GDI DC from the surface
	void Texture2D::ReleaseDC()
	{
		D3DPtr<IDXGISurface2> surf;
		pr::Throw(m_res->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
		pr::Throw(surf->ReleaseDC(nullptr));
//		--m_mgr->m_gdi_dc_ref_count;
		// Note: the main RT must be restored once all ReleaseDC's have been called
	}

	// Get the DXGI surface within this texture
	D3DPtr<IDXGISurface> Texture2D::GetSurface()
	{
		D3DPtr<IDXGISurface> surf;
		pr::Throw(m_res->QueryInterface(&surf.m_ptr));
		return surf;
	}

	// Get a d2d render target for the DXGI surface within this texture
	D3DPtr<ID2D1RenderTarget> Texture2D::GetD2DRenderTarget(Window const* wnd)
	{
		auto surf = GetSurface();

		// Create render target properties
		Renderer::Lock lock(m_mgr->rdr()); 
		auto props = D2D1::RenderTargetProperties(D2D1_RENDER_TARGET_TYPE_DEFAULT, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED));
		auto dpi = wnd ? wnd->Dpi() : m_mgr->rdr().SystemDpi();
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
		auto surf = GetSurface();
		Renderer::Lock lock(m_mgr->rdr());
		auto d3d_device = lock.D3DDevice();
		auto d2d_device = lock.D2DDevice();

		// Get the DXGI Device from the d3d device
		D3DPtr<IDXGIDevice> dxgi_device;
		Throw(d3d_device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));

		// Create a device context
		D3DPtr<ID2D1DeviceContext> d2d_dc;
		Throw(d2d_device->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &d2d_dc.m_ptr));

		// Create a bitmap wrapper for 'surf'
		D3DPtr<ID2D1Bitmap1> target;
		auto bp = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED));
		auto dpi = m_mgr->rdr().SystemDpi();
		bp.dpiX = dpi.x;
		bp.dpiY = dpi.y;
		Throw(d2d_dc->CreateBitmapFromDxgiSurface(surf.m_ptr, bp, &target.m_ptr));
			
		// Set the render target
		d2d_dc->SetTarget(target.get());
		return d2d_dc;
	}
}