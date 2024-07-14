//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/texture/texture_2d.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/texture/texture_desc.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/utility/utility.h"

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
		pr::Check(m_res->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
		pr::Check(surf->GetDC(discard, &dc), "GetDC can only be called for textures that are GDI compatible");
//		++m_mgr->m_gdi_dc_ref_count;
		return dc;
	}

	// Release the GDI DC from the surface
	void Texture2D::ReleaseDC()
	{
		D3DPtr<IDXGISurface2> surf;
		pr::Check(m_res->QueryInterface(__uuidof(IDXGISurface2), (void **)&surf.m_ptr));
		pr::Check(surf->ReleaseDC(nullptr));
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
		pr::Check(d2dfactory->CreateDxgiSurfaceRenderTarget(surf.m_ptr, props, &rt.m_ptr));
		return rt;
	}

	#endif

}