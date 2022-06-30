//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/window.h"
#include "pr/view3d/render/renderer.h"

namespace pr::rdr
{
	// Choose a default for the client area
	iv2 DefaultClientArea(HWND hwnd, iv2 const& area)
	{
		if (area.x != 0 && area.y != 0) return area;
		if (hwnd != nullptr)
		{
			RECT rect;
			Throw(::GetClientRect(hwnd, &rect), "GetClientRect failed.");
			return iv2(rect.right - rect.left, rect.bottom - rect.top);
		}
		return iv2One;
	}

	// Default WndSettings
	WndSettings::WndSettings(HWND hwnd, bool windowed, bool gdi_compatible_bb, iv2 const& client_area, bool w_buffer)
		:m_hwnd(hwnd)
		,m_windowed(windowed)
		,m_mode(DefaultClientArea(hwnd, client_area))
		,m_multisamp(4)
		,m_buffer_count(2)
		,m_swap_effect(DXGI_SWAP_EFFECT_DISCARD)//DXGI_SWAP_EFFECT_FLIP_DISCARD, DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL <- cannot use with multi-sampling
		,m_swap_chain_flags(DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH|(gdi_compatible_bb ? DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE : 0))
		,m_depth_format(DXGI_FORMAT_D24_UNORM_S8_UINT)
		,m_usage(DXGI_USAGE_RENDER_TARGET_OUTPUT|DXGI_USAGE_SHADER_INPUT)
		,m_vsync(1)
		,m_use_w_buffer(w_buffer)
		,m_allow_alt_enter(false)
		,m_name()
	{
		if (gdi_compatible_bb)
		{
			// Must use B8G8R8A8_UNORM for GDI compatibility
			m_mode.Format = DXGI_FORMAT_B8G8R8A8_UNORM;

			// Also, multi-sampling isn't supported
			m_multisamp = MultiSamp();
		}
	}

	// Window constructor
	Window::Window(Renderer& rdr, WndSettings const& settings)
		:m_rdr(&rdr)
		,m_hwnd(settings.m_hwnd)
		,m_db_format(settings.m_depth_format)
		,m_multisamp(!AllSet(rdr.Settings().m_device_layers, D3D11_CREATE_DEVICE_DEBUG) ? settings.m_multisamp : MultiSamp()) // Disable multi-sampling if debug is enabled
		,m_swap_chain_flags(settings.m_swap_chain_flags)
		,m_vsync(settings.m_vsync)
		,m_swap_chain_dbg()
		,m_swap_chain()
		,m_main_rtv()
		,m_main_srv()
		,m_main_dsv()
		,m_d2d_dc()
		,m_query()
		,m_main_rt()
		,m_idle(false)
		,m_name(settings.m_name)
		,m_dbg_area()
	{
		try
		{
			Renderer::Lock lock(rdr);
			auto device = lock.D3DDevice();

			// Validate settings
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && !AllSet(m_rdr->Settings().m_device_layers, D3D11_CREATE_DEVICE_BGRA_SUPPORT))
				Throw(false, "D3D device has not been created with GDI compatibility");
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && settings.m_multisamp.Count != 1)
				Throw(false, "GDI compatibility does not support multi-sampling");
			//todo: w-buffer
			// https://docs.microsoft.com/en-us/windows-hardware/drivers/display/w-buffering
			// https://www.mvps.org/directx/articles/using_w-buffers.htm

			// Check feature support
			m_multisamp.Validate(device, settings.m_mode.Format);
			m_multisamp.Validate(device, settings.m_depth_format);

			// Get the factory that was used to create 'rdr.m_device'
			D3DPtr<IDXGIDevice> dxgi_device;
			D3DPtr<IDXGIAdapter> adapter;
			D3DPtr<IDXGIFactory> factory;
			Throw(device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));
			Throw(dxgi_device->GetParent(__uuidof(IDXGIAdapter), (void**)&adapter.m_ptr));
			Throw(adapter->GetParent(__uuidof(IDXGIFactory), (void **)&factory.m_ptr));

			// Create a query interface for querying the GPU events related to this scene
			D3D11_QUERY_DESC query_desc;
			query_desc.Query = D3D11_QUERY_EVENT;
			query_desc.MiscFlags = static_cast<D3D11_QUERY_MISC_FLAG>(0);
			Throw(device->CreateQuery(&query_desc, &m_query.m_ptr));

			// Creating a device with hwnd == nullptr is allowed if you only want to render to 
			// off-screen render targets. If there's no window handle, don't create a swap chain
			if (settings.m_hwnd != 0)
			{
				// Uses the flag 'DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE' to enable an application to
				// render using GDI on a swap chain or a surface. This will allow the application
				// to call IDXGISurface1::GetDC on the 0th back buffer or a surface.
				DXGI_SWAP_CHAIN_DESC sd = {};
				sd.BufferCount  = settings.m_buffer_count;
				sd.BufferDesc   = settings.m_mode;
				sd.SampleDesc   = m_multisamp;
				sd.BufferUsage  = settings.m_usage;
				sd.OutputWindow = settings.m_hwnd;
				sd.Windowed     = settings.m_windowed;
				sd.SwapEffect   = settings.m_swap_effect;
				sd.Flags        = settings.m_swap_chain_flags;
				Throw(factory->CreateSwapChain(device, &sd, &m_swap_chain.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain.get(), FmtS("swap chain")));

				// Make DXGI monitor for Alt-Enter and switch between windowed and full screen
				Throw(factory->MakeWindowAssociation(settings.m_hwnd, settings.m_allow_alt_enter ? 0 : DXGI_MWA_NO_ALT_ENTER));
			}

			// If D2D is enabled, Connect D2D to the same render target as D3D
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
			{
				// Create a D2D device context
				Throw(lock.D2DDevice()->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS, &m_d2d_dc.m_ptr));
			}

			// In device debug mode, create a dummy swap chain so that the graphics debugging
			// sees 'Present' calls allowing it to capture frames.
			if (AllSet(rdr.Settings().m_device_layers, D3D11_CREATE_DEVICE_DEBUG))
			{
				DXGI_SWAP_CHAIN_DESC sd = {};
				sd.BufferCount  = 1;
				sd.BufferDesc   = settings.m_mode;
				sd.SampleDesc   = MultiSamp();
				sd.BufferUsage  = DXGI_USAGE_RENDER_TARGET_OUTPUT;
				sd.OutputWindow = rdr.DummyHwnd();
				sd.Windowed     = TRUE;
				sd.SwapEffect   = DXGI_SWAP_EFFECT_DISCARD;
				sd.Flags        = DXGI_SWAP_CHAIN_FLAG(0);
				Throw(factory->CreateSwapChain(device, &sd, &m_swap_chain_dbg.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain_dbg.get(), FmtS("swap chain dbg")));
			}

			InitRT();
		}
		catch (...)
		{
			this->~Window();
			throw;
		}
	}
	Window::~Window()
	{
		PR_EXPAND(PR_DBG_RDR, int rcnt);

		m_main_rtv = nullptr;
		m_main_dsv = nullptr;
		m_main_srv = nullptr;
		m_main_rt = nullptr;

		// Destroy the D2D device context
		if (m_d2d_dc != nullptr)
		{
			PR_ASSERT(PR_DBG_RDR, (rcnt = m_d2d_dc.RefCount()) == 1, "Outstanding references to the immediate device context");
			m_d2d_dc->SetTarget(nullptr);
			m_d2d_dc = nullptr;
		}

		// Destroying a Swap Chain:
		// You may not release a swap chain in full-screen mode because doing so may create thread contention
		// (which will cause DXGI to raise a non-continuable exception). Before releasing a swap chain, first
		// switch to windowed mode (using IDXGISwapChain::SetFullscreenState( FALSE, NULL )) and then call IUnknown::Release.
		if (m_swap_chain != nullptr)
		{
			PR_ASSERT(PR_DBG_RDR, (rcnt = m_swap_chain.RefCount()) == 1, "Outstanding references to the swap chain");
			m_swap_chain->SetFullscreenState(FALSE, nullptr);
			m_swap_chain = nullptr;
		}

		// Release the debug swap chain
		if (m_swap_chain_dbg != nullptr)
		{
			PR_ASSERT(PR_DBG_RDR, (rcnt = m_swap_chain_dbg.RefCount()) == 1, "Outstanding references to the dbg swap chain");
			m_swap_chain_dbg->SetFullscreenState(FALSE, nullptr);
			m_swap_chain_dbg = nullptr;
		}
	}

	// Access the renderer manager classes
	Renderer& Window::rdr() const
	{
		return *m_rdr;
	}
	ModelManager& Window::mdl_mgr() const
	{
		return m_rdr->m_mdl_mgr;
	}
	ShaderManager& Window::shdr_mgr() const
	{
		return m_rdr->m_shdr_mgr;
	}
	TextureManager& Window::tex_mgr() const
	{
		return m_rdr->m_tex_mgr;
	}
	BlendStateManager& Window::bs_mgr() const
	{
		return m_rdr->m_bs_mgr;
	}
	DepthStateManager& Window::ds_mgr() const
	{
		return m_rdr->m_ds_mgr;
	}
	RasterStateManager& Window::rs_mgr() const
	{
		return m_rdr->m_rs_mgr;
	}

	// Create a render target from the swap-chain
	void Window::InitRT()
	{
		// If the renderer has been created without a window handle, there will be no swap chain.
		// In this case the caller will be setting up a render target to an off-screen buffer
		if (m_swap_chain == nullptr)
			return;

		Renderer::Lock lock(*m_rdr);
		auto device = lock.D3DDevice();

		// Get the back buffer so we can copy its properties
		D3DPtr<ID3D11Texture2D> back_buffer;
		Throw(m_swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&back_buffer.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(back_buffer.get(), "main RT"));
			
		// Read the texture properties from the BB
		Texture2DDesc bbdesc;
		back_buffer->GetDesc(&bbdesc);
		static_cast<DXGI_SAMPLE_DESC&>(m_multisamp) = bbdesc.SampleDesc;

		// Create a render-target view of the back buffer
		Throw(device->CreateRenderTargetView(back_buffer.m_ptr, nullptr, &m_main_rtv.m_ptr));

		// If the texture was created with SRV binding, create a SRV
		if (bbdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE)
			Throw(device->CreateShaderResourceView(back_buffer.m_ptr, nullptr, &m_main_srv.m_ptr));

		// Get the render target as a texture
		m_main_rt = tex_mgr().CreateTexture2D(AutoId, back_buffer.get(), m_main_srv.get(), SamplerDesc::LinearClamp(), false, "main_rt");

		// Create a texture buffer that we will use as the depth buffer
		Texture2DDesc desc;
		desc.Width              = bbdesc.Width;
		desc.Height             = bbdesc.Height;
		desc.MipLevels          = 1;
		desc.ArraySize          = 1;
		desc.Format             = m_db_format;
		desc.SampleDesc         = bbdesc.SampleDesc;
		desc.Usage              = D3D11_USAGE_DEFAULT;
		desc.BindFlags          = D3D11_BIND_DEPTH_STENCIL;
		desc.CPUAccessFlags     = 0;
		desc.MiscFlags          = 0;
		D3DPtr<ID3D11Texture2D> depth_stencil;
		Throw(device->CreateTexture2D(&desc, 0, &depth_stencil.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(depth_stencil.get(), "main DB"));

		// Create a depth/stencil view of the texture buffer we just created
		D3D11_DEPTH_STENCIL_VIEW_DESC dsv_desc = {};
		dsv_desc.Format             = desc.Format;
		dsv_desc.ViewDimension      = bbdesc.SampleDesc.Count == 1 ? D3D11_DSV_DIMENSION_TEXTURE2D : D3D11_DSV_DIMENSION_TEXTURE2DMS;
		dsv_desc.Texture2D.MipSlice = 0;
		Throw(device->CreateDepthStencilView(depth_stencil.m_ptr, &dsv_desc, &m_main_dsv.m_ptr));

		// Re-link the D2D device context to the back buffer
		if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
		{
			// Direct2D needs the DXGI version of the back buffer
			D3DPtr<IDXGISurface> dxgi_back_buffer;
			Throw(m_swap_chain->GetBuffer(0, __uuidof(IDXGISurface), (void**)&dxgi_back_buffer.m_ptr));

			// Create bitmap properties for the bitmap view of the back buffer
			auto bp = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_IGNORE));
			auto dpi = Dpi();
			bp.dpiX = dpi.x;
			bp.dpiY = dpi.y;

			// Wrap the back buffer as a bitmap for D2D
			D3DPtr<ID2D1Bitmap1> d2d_render_target;
			Throw(m_d2d_dc->CreateBitmapFromDxgiSurface(dxgi_back_buffer.get(), &bp, &d2d_render_target.m_ptr));

			// Set the bitmap as the render target
			m_d2d_dc->SetTarget(d2d_render_target.get());
		}

		// Bind the main render target and depth buffer to the OM
		RestoreRT();
	}

	// Binds the main render target and depth buffer to the OM
	void Window::RestoreRT()
	{
		SetRT(m_main_rtv.get(), m_main_dsv.get(), false);
	}

	// Binds the given render target and depth buffer views to the OM
	void Window::SetRT(ID3D11RenderTargetView* rtv, ID3D11DepthStencilView* dsv, bool is_new_main_rt)
	{
		Renderer::Lock lock(*m_rdr);
		auto dc = lock.ImmediateDC();
		ID3D11RenderTargetView* targets[] = { rtv };
		dc->OMSetRenderTargets(1, targets, dsv);

		// Set the current render target as the main render target
		if (is_new_main_rt)
		{
			// Replace the previous RT/DS
			m_main_rtv = D3DPtr<ID3D11RenderTargetView>(rtv, true);
			m_main_dsv = D3DPtr<ID3D11DepthStencilView>(dsv, true);
		}
	}

	// Render this window into 'render_target'
	// 'render_target' is the texture that is rendered onto
	// 'depth_buffer' is an optional texture that will receive the depth information (can be null)
	// 'depth_buffer' will be created if not provided.
	void Window::SetRT(ID3D11Texture2D* render_target, ID3D11Texture2D* depth_buffer, bool is_new_main_rt)
	{
		// Allow setting the render target to null
		if (render_target == nullptr)
		{
			SetRT(static_cast<ID3D11RenderTargetView*>(nullptr), static_cast<ID3D11DepthStencilView*>(nullptr), is_new_main_rt);
		    if (is_new_main_rt)
		    {
			    m_main_rt = nullptr;
			    m_main_srv = nullptr;
		    }
			return;
		}

		// Get the description of the render target texture
		Texture2DDesc tdesc;
		render_target->GetDesc(&tdesc);
		PR_ASSERT(PR_DBG_RDR, (tdesc.BindFlags & D3D11_BIND_RENDER_TARGET) != 0, "This texture is not a render target");

		Renderer::Lock lock(*m_rdr);
		auto device = lock.D3DDevice();

		// Get a render target view of the render target texture
		D3DPtr<ID3D11RenderTargetView> rtv;
		Throw(device->CreateRenderTargetView(render_target, nullptr, &rtv.m_ptr));

		// If no depth buffer is given, create a temporary depth buffer
		D3DPtr<ID3D11Texture2D> tmp_depth_buffer;
		if (depth_buffer == nullptr)
		{
			Texture2DDesc dbdesc;
			dbdesc.Width = tdesc.Width;
			dbdesc.Height = tdesc.Height;
			dbdesc.Format = m_db_format;
			dbdesc.SampleDesc = tdesc.SampleDesc;
			dbdesc.Usage = D3D11_USAGE_DEFAULT;
			dbdesc.BindFlags = D3D11_BIND_DEPTH_STENCIL;
			dbdesc.CPUAccessFlags = 0;
			dbdesc.MiscFlags = 0;
			Throw(lock.D3DDevice()->CreateTexture2D(&dbdesc, nullptr, &tmp_depth_buffer.m_ptr));
			depth_buffer = tmp_depth_buffer.get();
		}

		// Create a depth stencil view of the depth buffer
		D3DPtr<ID3D11DepthStencilView> dsv = nullptr;
		Throw(device->CreateDepthStencilView(depth_buffer, nullptr, &dsv.m_ptr));

		// Set the render target
		SetRT(rtv.get(), dsv.get(), is_new_main_rt);

		if (is_new_main_rt)
		{
			D3DPtr<ID3D11ShaderResourceView> srv;
			Throw(device->CreateShaderResourceView(render_target, nullptr, &srv.m_ptr));
			m_main_rt = tex_mgr().CreateTexture2D(AutoId, render_target, srv.get(), SamplerDesc::LinearClamp(), false, "main_rt");
			m_main_srv = srv;
		}
	}

	// Draw text directly to the back buffer
	void Window::DrawString(wchar_t const* text, float x, float y)
	{
		Renderer::Lock lock(*m_rdr);
		auto dwrite = lock.DWrite();

		// Create a solid brush
		D3DPtr<ID2D1SolidColorBrush> brush;
		Throw(m_d2d_dc->CreateSolidColorBrush(D2D1::ColorF(D2D1::ColorF::Blue), &brush.m_ptr));

		// Create a text format
		D3DPtr<IDWriteTextFormat> text_format;
		Throw(dwrite->CreateTextFormat(L"tahoma", nullptr, DWRITE_FONT_WEIGHT_LIGHT, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL, 20.0f, L"en-GB", &text_format.m_ptr));

		// Create a text layout
		D3DPtr<IDWriteTextLayout> text_layout;
		Throw(dwrite->CreateTextLayout(text, UINT32(wcslen(text)), text_format.get(), 100.0f, 100.0f, &text_layout.m_ptr));

		// Draw the string
		m_d2d_dc->BeginDraw();
		m_d2d_dc->DrawTextLayout(D2D1::Point2F(x, y), text_layout.get(), brush.get());
		Throw(m_d2d_dc->EndDraw());
	}

	// Set the viewport to all of the render target
	void Window::RestoreFullViewport()
	{
		Renderer::Lock lock(*m_rdr);
		Viewport vp(BackBufferSize());
		lock.ImmediateDC()->RSSetViewports(1, &vp);
	}

	// Get/Set full screen mode
	// Don't use the automatic alt-enter system, it's too uncontrollable
	// Handle WM_SYSKEYDOWN for VK_RETURN, then call FullScreenMode
	bool Window::FullScreenMode() const
	{
		BOOL full_screen;
		D3DPtr<IDXGIOutput> ppTarget;
		Throw(m_swap_chain->GetFullscreenState(&full_screen, &ppTarget.m_ptr));
		return full_screen != 0;
	}
	void Window::FullScreenMode(bool on, rdr::DisplayMode mode)
	{
		// For D3D11 you should initially set DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH, as explained above.
		// There are then two main things you must do.
		// The first is to call SetFullScreenState on your swap chain object. This will just switch the
		// display mode to full screen, but doesn't change anything else relating to the mode. You've
		// still got the same height, width, format, etc as before.
		// The second is to call ResizeTarget and ResizeBuffers (both again on the swap chain) to actually
		// change the mode.
		// Here's where things can get a little bit more complex (although it's actually simple if you follow
		// the logic of the design).
		// When switching from windowed to full screen I've found it best to call ResizeTarget first, then
		// call SetFullScreenState.
		// When going the other way (full screen to windowed) you call SetFullScreenState first, then call
		// ResizeTarget.
		// When just switching modes (but leaving full screen state alone) you just need to call ResizeTarget.
		// These orders may not be 100% necessary (I haven't found anything in the documentation to confirm
		// or deny) but I've found through trial, error and experimentation that they work and give predictable
		// behaviour. Otherwise you may get extra mode changes which slow down the process, and may get weird
		// behaviour.
		// In all cases, you should ensure that the mode you're switching to has been properly enumerated
		// (via EnumAdapters/EnumOutputs/GetDisplayModeList) - it's not necessary for switching to a windowed
		// mode, but is highly recommended for switching to a full screen mode, and makes things easier overall.
		// Following that you need to respond to WM_SIZE in your message loop; when you receive a WM_SIZE you
		// just need to call ResizeBuffers in response to it. That bit is optional but if you leave it out you're
		// keeping the old frame buffer sizes, meaning that D3D11 has to do a stretch operation to the buffers,
		// which can degrade performance and quality. If you need to destroy any old render targets/depth
		// stencils/etc and create new ones at the new mode resolution, this is also a good place to do it.
		//
		// One thing I've left out until now is that your context will hold references to render targets/etc
		// so before calling ResizeBuffers you need to call ID3D11DeviceContext1::ClearState to release those
		// references (or you could just call OMSetRenderTargets with NULL parameters - I haven't personally
		// tested this but logic says that it should work), then release your render target view, otherwise
		// ResizeBuffers will fail. When done, set everything up again (you don't need to recreate anything
		// that you don't explicitly destroy yourself though). If you're using a depth buffer also release
		// and recreate it (both the view and the texture).
		//
		// There's a pretty good write up, with sample code and other useful info, of the process in the SDK
		// documentation under the heading "DXGI Overview", but unfortunately the help file index seems to be
		// - shall we say - not quite as good as it once was these days, so you may need to use the Search
		// function. A search for ResizeBuffers should give you this article as the first hit with the June 2010 SDK.

		BOOL currently_fullscreen;
		D3DPtr<IDXGIOutput> output;
		Throw(m_swap_chain->GetFullscreenState(&currently_fullscreen, &output.m_ptr));

		// Windowed -> Full screen
		if (!currently_fullscreen && on)
		{
			Throw(m_swap_chain->ResizeTarget(&mode));
			Throw(m_swap_chain->SetFullscreenState(TRUE, output.m_ptr));
		}
		// Full screen -> windowed
		else if (currently_fullscreen && !on)
		{
			Throw(m_swap_chain->SetFullscreenState(FALSE, nullptr));
			Throw(m_swap_chain->ResizeTarget(&mode));
		}
		// Full screen -> Full screen
		else if (currently_fullscreen && on)
		{
			Throw(m_swap_chain->ResizeTarget(&mode));
		}
	}

	// The display mode of the main render target
	DXGI_FORMAT Window::DisplayFormat() const
	{
		if (m_swap_chain == nullptr)
			return DXGI_FORMAT_UNKNOWN;
			
		DXGI_SWAP_CHAIN_DESC desc;
		Throw(m_swap_chain->GetDesc(&desc));
		return desc.BufferDesc.Format;
	}

	// Returns the size of the current render target
	iv2 Window::RenderTargetSize() const
	{
		Renderer::Lock lock(*m_rdr);

		// Get the current render target view
		D3DPtr<ID3D11RenderTargetView> rtv;
		lock.ImmediateDC()->OMGetRenderTargets(1, &rtv.m_ptr, nullptr);
		if (rtv == nullptr)
			return iv2Zero;
			
		// Get the resource associated with that view
		D3DPtr<ID3D11Resource> res;
		rtv->GetResource(&res.m_ptr);

		// Get the Texture2D pointer to the resource
		D3DPtr<ID3D11Texture2D> rt;
		Throw(res->QueryInterface<ID3D11Texture2D>(&rt.m_ptr));
		if (rt == nullptr)
			return iv2Zero;

		// Return the size of the texture
		Texture2DDesc tdesc;
		rt->GetDesc(&tdesc);
		return iv2(tdesc.Width, tdesc.Height);
	}

	// Returns the size of the swap chain back buffer
	iv2 Window::BackBufferSize() const
	{
		// When used in WPF, the swap chain isn't used. WPF renders to an off-screen dx9 render target.
		// WPF calls should not land here, they need to be handled by the D3D11Image class.
		// This should be an assert, but automatic property evaluation in C# causes it to be called.
		//PR_ASSERT(PR_DBG_RDR, m_swap_chain != nullptr, "The back buffer size is meaningless when there is no swap chain");
		if (m_swap_chain == nullptr)
			return iv2{};

		DXGI_SWAP_CHAIN_DESC desc;
		Throw(m_swap_chain->GetDesc(&desc));
		return iv2(desc.BufferDesc.Width, desc.BufferDesc.Height);
	}

	// Called when the window size changes (e.g. from a WM_SIZE message)
	void Window::BackBufferSize(iv2 const& size, bool force)
	{
		PR_ASSERT(PR_DBG_RDR, size.x >= 0 && size.y >= 0, "Size should be positive definite");
		PR_ASSERT(PR_DBG_RDR, m_swap_chain != nullptr, "Do not set the RenderTargetSize when in off-screen only mode (i.e. not swap chain)");

		// Ignore resizes that aren't changes in size
		auto area = BackBufferSize();
		if (size == area && !force)
			return;

		RebuildRT([size, this](ID3D11Device*)
		{
			// Get the swap chain to resize itself
			// Pass 0 for width and height, DirectX gets them from the associated window
			Throw(m_swap_chain->ResizeBuffers(0, s_cast<UINT>(size.x), s_cast<UINT>(size.y), DXGI_FORMAT_UNKNOWN, m_swap_chain_flags));
		});
	}

	// Get/Set the multi sampling used.
	MultiSamp Window::MultiSampling() const
	{
		return m_multisamp;
	}
	void Window::MultiSampling(MultiSamp ms)
	{
		if (m_swap_chain == nullptr)
			throw std::runtime_error(
				"Setting MultiSampling on a window only applies when there is a back buffer. "
				"If you're using a window for off-screen rendering only, you'll need to create "
				"a larger render target texture and use ResolveSubresource. (See D3D11Image)");

		// Changing the multi-sampling mode is a bit like resizing the back buffer
		RebuildRT([&ms, this](ID3D11Device* device)
		{
			// Get the factory that was used to create 'device'
			D3DPtr<IDXGIDevice> dxgi_device;
			D3DPtr<IDXGIAdapter> adapter;
			D3DPtr<IDXGIFactory> factory;
			Throw(device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));
			Throw(dxgi_device->GetParent(__uuidof(IDXGIAdapter), (void**)&adapter.m_ptr));
			Throw(adapter->GetParent(__uuidof(IDXGIFactory), (void **)&factory.m_ptr));

			// Get the description of the existing swap chain
			DXGI_SWAP_CHAIN_DESC sd = {0};
			Throw(m_swap_chain->GetDesc(&sd), "Failed to get current swap chain description");
			Throw(!AllSet(sd.Flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) || ms.Count == 1, "GDI compatibility cannot be used with multi-sampling");

			// Check for feature support
			ms.Validate(device, sd.BufferDesc.Format);
			sd.SampleDesc = ms;

			// Create a new swap chain with the new multi-sampling mode
			// Uses the flag 'DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE' to enable an application to
			// render using GDI on a swap chain or a surface. This will allow the application
			// to call IDXGISurface1::GetDC on the 0th back buffer or a surface.
			m_swap_chain = nullptr;
			Throw(factory->CreateSwapChain(device, &sd, &m_swap_chain.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain.get(), "swap chain"));

			m_multisamp = ms;
		});
	}

	// Release all references to the swap chain to allow it to be created or resized.
	void Window::RebuildRT(std::function<void(ID3D11Device*)> work)
	{
		// Applications can make some changes to make the transition from windowed to full screen more efficient.
		// For example, on a WM_SIZE message, the application should release any outstanding swap-chain back buffers,
		// call IDXGISwapChain::ResizeBuffers, then re-acquire the back buffers from the swap chain(s). This gives the
		// swap chain(s) an opportunity to resize the back buffers, and/or recreate them to enable full-screen flipping
		// operation. If the application does not perform this sequence, DXGI will still make the full-screen/windowed
		// transition, but may be forced to use a stretch operation (since the back buffers may not be the correct size),
		// which may be less efficient. Even if a stretch is not required, presentation may not be optimal because the back
		// buffers might not be directly interchangeable with the front buffer. Thus, a call to ResizeBuffers on WM_SIZE is
		// always recommended, since WM_SIZE is always sent during a full screen transition.

		// While you don't have to write any more code than has been described, a few simple steps can make your application
		// more responsive. The most important consideration is the resizing of the swap chain's buffers in response to the
		// resizing of the output window. Naturally, the application's best route is to respond to WM_SIZE, and call
		// IDXGISwapChain::ResizeBuffers, passing the size contained in the message's parameters. This behaviour obviously makes
		// your application respond well to the user when he or she drags the window's borders, but it is also exactly what
		// enables a smooth full-screen transition. Your window will receive a WM_SIZE message whenever such a transition happens,
		// and calling IDXGISwapChain::ResizeBuffers is the swap chain's chance to re-allocate the buffers' storage for optimal
		// presentation. This is why the application is required to release any references it has on the existing buffers before
		// it calls IDXGISwapChain::ResizeBuffers.
		// Failure to call IDXGISwapChain::ResizeBuffers in response to switching to full-screen mode (most naturally, in response
		// to WM_SIZE), can preclude the optimization of flipping, wherein DXGI can simply swap which buffer is being displayed,
		// rather than copying a full screen's worth of data around.

		Renderer::Lock lock(*m_rdr);
		auto device = lock.D3DDevice();
		auto dc = lock.ImmediateDC();

		// Notify that a resize of the swap chain is about to happen.
		// Receivers need to ensure they don't have any outstanding references to the swap chain resources
		m_rdr->BackBufferSizeChanged(*this, BackBufferSizeChangedEventArgs(BackBufferSize(), false));

		// Drop the render targets from the immediate context and D2D
		if (m_d2d_dc != nullptr) m_d2d_dc->SetTarget(nullptr);
		dc->OMSetRenderTargets(0, nullptr, nullptr);
		dc->ClearState();

		m_main_rt = nullptr;
		m_main_rtv = nullptr;
		m_main_srv = nullptr;
		m_main_dsv = nullptr;

		PR_EXPAND(PR_DBG_RDR, auto rcnt = (m_swap_chain->AddRef(), m_swap_chain->Release()));
		PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the dx device");

		// Do the operation that requires the swap chain tear-down
		work(device);

		// Set up the render targets again
		InitRT();

		// Notify that the resize is done
		m_rdr->BackBufferSizeChanged(*this, BackBufferSizeChangedEventArgs(m_dbg_area = BackBufferSize(), true));
	}

	// Signal the start and end of a frame.
	void Window::FrameBeg()
	{
		if (m_swap_chain == nullptr)
		{
			Renderer::Lock lock(rdr());
			auto dc = lock.ImmediateDC();
			dc->Begin(m_query.get());
		}
	}
	void Window::FrameEnd()
	{
		if (m_swap_chain == nullptr)
		{
			Renderer::Lock lock(rdr());
			auto dc = lock.ImmediateDC();
			dc->End(m_query.get());
		}
	}

	// Flip the rendered scenes to the display
	void Window::Present()
	{
		// Be careful that you never have the message-pump thread wait on the render thread.
		// For instance, calling IDXGISwapChain1::Present1 (from the render thread) may cause
		// the render thread to wait on the message-pump thread. When a mode change occurs,
		// this scenario is possible if Present1 calls ::SetWindowPos() or ::SetWindowStyle()
		// and either of these methods call ::SendMessage(). In this scenario, if the message-pump
		// thread has a critical section guarding it or if the render thread is blocked, then the
		// two threads will deadlock.

		// IDXGISwapChain1::Present1 will inform you if your output window is entirely occluded via DXGI_STATUS_OCCLUDED.
		// When this occurs, we recommended that your application go into standby mode (by calling IDXGISwapChain1::Present1
		// with DXGI_PRESENT_TEST) since resources used to render the frame are wasted. Using DXGI_PRESENT_TEST will prevent
		// any data from being presented while still performing the occlusion check. Once IDXGISwapChain1::Present1 returns
		// S_OK, you should exit standby mode; do not use the return code to switch to standby mode as doing so can leave
		// the swap chain unable to relinquish full-screen mode.
		// ^^ This means: Don't use calls to Present(?, DXGI_PRESENT_TEST) to test if the window is occluded,
		// only use it after Present() has returned DXGI_STATUS_OCCLUDED.

		// Present with the debug swap chain so that graphics debugging detects a frame
		if (m_swap_chain_dbg != nullptr)
			m_swap_chain_dbg->Present(m_vsync, 0);

		// If there is no swap chain, then we must be rendering to an off-screen texture.
		// In that case, flush to the graphics card
		if (m_swap_chain == nullptr)
		{
			Renderer::Lock lock(*m_rdr);
			auto dc = lock.ImmediateDC();

			// Flush is asynchronous so it may return before the frame has been rendered.
			// Call flush, then block until the GPU has finished processing all the commands.
			dc->Flush();
			for (;; std::this_thread::yield())
			{
				BOOL complete;
				auto res = dc->GetData(m_query.get(), &complete, sizeof(complete), static_cast<D3D11_ASYNC_GETDATA_FLAG>(0));
				if (res == S_OK) break;
				if (res == S_FALSE) continue;
				Throw(res);
			}
			return;
		}

		// Render to the display
		auto res = m_swap_chain->Present(m_vsync, m_idle ? DXGI_PRESENT_TEST : 0);
		switch (res)
		{
			case S_OK:
			{
				m_idle = false;
				break;
			}
			case DXGI_STATUS_OCCLUDED:
			{
				// This happens when the window is not visible on-screen, the app should go into idle mode
				m_idle = true;
				break;
			}
			case DXGI_ERROR_DEVICE_RESET:
			{
				// The device failed due to a badly formed command. This is a run-time issue;
				// The application should destroy and recreate the device.
				throw Exception<HRESULT>(DXGI_ERROR_DEVICE_RESET, "Graphics adapter reset");
			}
			case DXGI_ERROR_DEVICE_REMOVED:
			{
				// This happens in situations like, laptop un-docked, or remote desktop connect etc.
				// We'll just throw so the app can shutdown/reset/whatever
				Renderer::Lock lock(*m_rdr);
				throw Exception<HRESULT>(lock.D3DDevice()->GetDeviceRemovedReason(), "Graphics adapter no longer available");
			}
			default:
			{
				throw std::runtime_error("Unknown result from SwapChain::Present");
			}
		}
	}
}