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
		:TextureBase(mgr, res, desc, D3D12_SRV_DIMENSION_TEXTURE2D)
		,m_t2s(m4x4::Identity())
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

	#if 0
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

	#endif
	// Get a D2D device context for drawing on this texture
	Texture2D::D2D1Context Texture2D::GetD2DeviceContext()
	{
		return D2D1Context(rdr().Dx11Device(), rdr().D2DDevice(), m_res.get());
	}

	// RAII Scope for a wrapped Dx12 resource
	Texture2D::D2D1Context::D2D1Context(ID3D11On12Device* dx11, ID2D1Device* dx2, ID3D12Resource* res)
		:m_dx11(dx11)
		,m_res()
	{
		// Create a device context
		Throw(dx2->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &m_dc.m_ptr));

		// Notes:
		//  - The texture needs: D3D12_HEAP_FLAG_SHARED
		
		// Wrap the Dx12 resource
		D3D11_RESOURCE_FLAGS flags = {
			.BindFlags = D3D11_BIND_RENDER_TARGET,
			.MiscFlags = 0,
			.CPUAccessFlags = 0U,
			.StructureByteStride = 0U,
		};
		Throw(m_dx11->CreateWrappedResource(res, &flags, D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_COMMON, __uuidof(IDXGISurface), (void**)&m_res.m_ptr));

		// Get a DXGI surface from the wrapped resource
		D3DPtr<IDXGISurface> surf;
		Throw(m_res->QueryInterface<IDXGISurface>(&surf.m_ptr));

		v2 dpi;
		m_dc->GetDpi(&dpi.x, &dpi.y);

		// Create a bitmap wrapper for 'surf'
		D3DPtr<ID2D1Bitmap1> target;
		auto bp = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED), dpi.x, dpi.y);
		Throw(m_dc->CreateBitmapFromDxgiSurface(surf.m_ptr, bp, &target.m_ptr));

		// Set the render target
		m_dc->SetTarget(target.get());
	}
	Texture2D::D2D1Context::~D2D1Context()
	{
		ID3D11Resource* arr[] = {m_res.get()};
		m_dx11->ReleaseWrappedResources(arr, 1U);

		// todo, need to call dx11_dc->Flush() here
	}
}