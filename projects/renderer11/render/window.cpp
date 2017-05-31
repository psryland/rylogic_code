//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/window.h"
#include "pr/renderer11/render/renderer.h"

namespace pr
{
	namespace rdr
	{
		// Default WndSettings
		WndSettings::WndSettings(HWND hwnd, bool windowed, bool gdi_compat, pr::iv2 const& client_area)
			:m_hwnd(hwnd)
			,m_windowed(windowed)
			,m_mode(client_area)
			,m_multisamp(4)
			,m_buffer_count(2)
			,m_swap_effect(DXGI_SWAP_EFFECT_DISCARD)// DXGI_SWAP_EFFECT_SEQUENTIAL <- cannot use with multi-sampling
			,m_swap_chain_flags(DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH|(gdi_compat ? DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE : 0))
			,m_depth_format(DXGI_FORMAT_D24_UNORM_S8_UINT)
			,m_usage(DXGI_USAGE_RENDER_TARGET_OUTPUT|DXGI_USAGE_SHADER_INPUT)
			,m_vsync(1)
			,m_allow_alt_enter(false)
			,m_name()
		{
			if (gdi_compat)
			{
				// Must use B8G8R8A8_UNORM for GDI compatibility
				m_mode.Format = DXGI_FORMAT_B8G8R8A8_UNORM;

				// Also, multi-sampling isn't supported
				m_multisamp = pr::rdr::MultiSamp();
			}
		}

		// Window constructor
		Window::Window(Renderer& rdr, WndSettings const& settings)
			:m_rdr(&rdr)
			,m_hwnd(settings.m_hwnd)
			,m_multisamp(!AllSet(rdr.Settings().m_device_layers, D3D11_CREATE_DEVICE_DEBUG) ? settings.m_multisamp : pr::rdr::MultiSamp()) // Disable multi-sampling if debug is enabled
			,m_db_format(settings.m_depth_format)
			,m_swap_chain_flags(settings.m_swap_chain_flags)
			,m_vsync(settings.m_vsync)
			,m_swap_chain()
			,m_main_rtv()
			,m_main_srv()
			,m_main_dsv()
			,m_main_tex()
			,m_idle(false)
			,m_name(settings.m_name)
			,m_area()
		{
			try
			{
				auto device = rdr.Device();

				// Validate settings
				if (AllSet(settings.m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && !AllSet(m_rdr->Settings().m_device_layers, D3D11_CREATE_DEVICE_BGRA_SUPPORT))
					pr::Throw(false, "D3D device has not been created with GDI compatibility");
				if (AllSet(settings.m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && settings.m_multisamp.Count != 1)
					pr::Throw(false, "GDI compatibility does not support multi-sampling");

				// Check feature support
				m_multisamp.Validate(device, settings.m_mode.Format);
				m_multisamp.Validate(device, settings.m_depth_format);

				// Get the factory that was used to create 'rdr.m_device'
				D3DPtr<IDXGIDevice> dxgi_device;
				D3DPtr<IDXGIAdapter> adapter;
				D3DPtr<IDXGIFactory> factory;
				pr::Throw(device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));
				pr::Throw(dxgi_device->GetParent(__uuidof(IDXGIAdapter), (void**)&adapter.m_ptr));
				pr::Throw(adapter->GetParent(__uuidof(IDXGIFactory), (void **)&factory.m_ptr));

				// Uses the flag 'DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE' to enable an application to
				// render using GDI on a swap chain or a surface. This will allow the application
				// to call IDXGISurface1::GetDC on the 0th back buffer or a surface.
				DXGI_SWAP_CHAIN_DESC sd = {0};
				sd.BufferCount  = settings.m_buffer_count;
				sd.BufferDesc   = settings.m_mode;
				sd.SampleDesc   = m_multisamp;
				sd.BufferUsage  = settings.m_usage;
				sd.OutputWindow = settings.m_hwnd;
				sd.Windowed     = settings.m_windowed;
				sd.SwapEffect   = settings.m_swap_effect;
				sd.Flags        = settings.m_swap_chain_flags;
				pr::Throw(factory->CreateSwapChain(device.m_ptr, &sd, &m_swap_chain.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain , pr::FmtS("swap chain")));

				// Make DXGI monitor for Alt-Enter and switch between windowed and full screen
				pr::Throw(factory->MakeWindowAssociation(settings.m_hwnd, settings.m_allow_alt_enter ? 0 : DXGI_MWA_NO_ALT_ENTER));

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
			m_main_rtv = nullptr;
			m_main_dsv = nullptr;
			m_main_srv = nullptr;
			m_main_tex = nullptr;

			// Destroying a Swap Chain:
			// You may not release a swap chain in full-screen mode because doing so may create thread contention
			// (which will cause DXGI to raise a non-continuable exception). Before releasing a swap chain, first
			// switch to windowed mode (using IDXGISwapChain::SetFullscreenState( FALSE, NULL )) and then call IUnknown::Release.
			if (m_swap_chain != nullptr)
			{
				PR_EXPAND(PR_DBG_RDR, int rcnt);
				PR_ASSERT(PR_DBG_RDR, (rcnt = m_swap_chain.RefCount()) == 1, "Outstanding references to the swap chain");
				m_swap_chain->SetFullscreenState(FALSE, nullptr);
				m_swap_chain = nullptr;
			}
		}

		// Return the DX device
		D3DPtr<ID3D11Device> Window::Device() const
		{
			return m_rdr->Device();
		}

		// Return the immediate device context
		D3DPtr<ID3D11DeviceContext> Window::ImmediateDC() const
		{
			return m_rdr->ImmediateDC();
		}

		// Access the renderer manager classes
		ModelManager& Window::mdl_mgr()
		{
			return m_rdr->m_mdl_mgr;
		}
		ShaderManager& Window::shdr_mgr()
		{
			return m_rdr->m_shdr_mgr;
		}
		TextureManager& Window::tex_mgr()
		{
			return m_rdr->m_tex_mgr;
		}
		BlendStateManager& Window::bs_mgr()
		{
			return m_rdr->m_bs_mgr;
		}
		DepthStateManager& Window::ds_mgr()
		{
			return m_rdr->m_ds_mgr;
		}
		RasterStateManager& Window::rs_mgr()
		{
			return m_rdr->m_rs_mgr;
		}

		// Create a render target from the swap-chain
		void Window::InitRT()
		{
			auto device = m_rdr->Device();

			// Get the back buffer so we can copy its properties
			D3DPtr<ID3D11Texture2D> back_buffer;
			pr::Throw(m_swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&back_buffer.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(back_buffer, "main RT"));
			
			// Read the texture properties from the BB
			TextureDesc bbdesc;
			back_buffer->GetDesc(&bbdesc);
			static_cast<DXGI_SAMPLE_DESC&>(m_multisamp) = bbdesc.SampleDesc;

			// Create a render-target view of the back buffer
			pr::Throw(device->CreateRenderTargetView(back_buffer.m_ptr, nullptr, &m_main_rtv.m_ptr));

			// If the texture was created with SRV binding, create a SRV
			if (bbdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE)
				pr::Throw(device->CreateShaderResourceView(back_buffer.m_ptr, nullptr, &m_main_srv.m_ptr));

			// Get the render target as a texture
			m_main_tex = tex_mgr().CreateTexture2D(AutoId, back_buffer, m_main_srv, SamplerDesc::LinearClamp(), "main_rt");

			// Create a texture buffer that we will use as the depth buffer
			TextureDesc desc;
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
			pr::Throw(device->CreateTexture2D(&desc, 0, &depth_stencil.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(depth_stencil, "main DB"));

			// Create a depth/stencil view of the texture buffer we just created
			D3D11_DEPTH_STENCIL_VIEW_DESC dsv_desc = {};
			dsv_desc.Format             = desc.Format;
			dsv_desc.ViewDimension      = bbdesc.SampleDesc.Count == 1 ? D3D11_DSV_DIMENSION_TEXTURE2D : D3D11_DSV_DIMENSION_TEXTURE2DMS;
			dsv_desc.Texture2D.MipSlice = 0;
			pr::Throw(device->CreateDepthStencilView(depth_stencil.m_ptr, &dsv_desc, &m_main_dsv.m_ptr));

			// Bind the main render target and depth buffer to the OM
			RestoreRT();
		}

		// Binds the main render target and depth buffer to the OM
		void Window::RestoreRT()
		{
			ImmediateDC()->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);
		}

		// Binds the given render target and depth buffer views to the OM
		void Window::SetRT(D3DPtr<ID3D11RenderTargetView>& rtv, D3DPtr<ID3D11DepthStencilView>& dsv)
		{
			ImmediateDC()->OMSetRenderTargets(1, &rtv.m_ptr, dsv.m_ptr);
		}

		// Set the viewport to all of the render target
		void Window::RestoreFullViewport()
		{
			auto sz = RenderTargetSize();
			Viewport vp(float(sz.x), float(sz.y));
			ImmediateDC()->RSSetViewports(1, &vp);
		}

		// Get/Set full screen mode
		// Don't use the automatic alt-enter system, it's too uncontrollable
		// Handle WM_SYSKEYDOWN for VK_RETURN, then call FullScreenMode
		bool Window::FullScreenMode() const
		{
			BOOL full_screen;
			D3DPtr<IDXGIOutput> ppTarget;
			pr::Throw(m_swap_chain->GetFullscreenState(&full_screen, &ppTarget.m_ptr));
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
			// so before calling ResizeBuffers you need to call ID3D11DeviceContext::ClearState to release those
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
			pr::Throw(m_swap_chain->GetFullscreenState(&currently_fullscreen, &output.m_ptr));

			// Windowed -> Full screen
			if (!currently_fullscreen && on)
			{
				pr::Throw(m_swap_chain->ResizeTarget(&mode));
				pr::Throw(m_swap_chain->SetFullscreenState(TRUE, output.m_ptr));
			}
			// Full screen -> windowed
			else if (currently_fullscreen && !on)
			{
				pr::Throw(m_swap_chain->SetFullscreenState(FALSE, nullptr));
				pr::Throw(m_swap_chain->ResizeTarget(&mode));
			}
			// Full screen -> Full screen
			else if (currently_fullscreen && on)
			{
				pr::Throw(m_swap_chain->ResizeTarget(&mode));
			}
		}

		// The display mode of the main render target
		DXGI_FORMAT Window::DisplayFormat() const
		{
			DXGI_SWAP_CHAIN_DESC desc;
			pr::Throw(m_swap_chain->GetDesc(&desc));
			return desc.BufferDesc.Format;
		}

		// Returns the size of the render target
		iv2 Window::RenderTargetSize() const
		{
			DXGI_SWAP_CHAIN_DESC desc;
			Throw(m_swap_chain->GetDesc(&desc));
			return iv2(desc.BufferDesc.Width, desc.BufferDesc.Height);
		}

		// Called when the window size changes (e.g. from a WM_SIZE message)
		void Window::RenderTargetSize(iv2 const& size, bool force)
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

			// Ignore resizes that aren't changes in size
			auto area = RenderTargetSize();
			if (size == area && !force)
				return;

			PR_ASSERT(PR_DBG_RDR, size.x >= 0 && size.y >= 0, "Size should be positive definite");
			auto dc = m_rdr->ImmediateDC();

			// Notify that a resize of the swap chain is about to happen.
			// Receivers need to ensure they don't have any outstanding references to the swap chain resources
			pr::events::Send(rdr::Evt_Resize(this, false, area)); // notify before changing the RT (with the old size)

			// Drop the render targets from the immediate context
			dc->OMSetRenderTargets(0, nullptr, nullptr);
			dc->ClearState();

			m_main_tex = nullptr;
			m_main_rtv = nullptr;
			m_main_srv = nullptr;
			m_main_dsv = nullptr;

			// Get the swap chain to resize itself
			// Pass 0 for width and height, DirectX gets them from the associated window
			pr::Throw(m_swap_chain->ResizeBuffers(0, checked_cast<UINT>(size.x), checked_cast<UINT>(size.y), DXGI_FORMAT_UNKNOWN, m_swap_chain_flags));

			//// Only set the width/height, leave the other options unchanged
			//DXGI_MODE_DESC target_mode = {};
			//target_mode.Width = area.x;
			//target_mode.Height = area.y;
			//pr::Throw(m_swap_chain->ResizeTarget(&target_mode));

			// Set up the render targets again
			InitRT();
			RestoreRT();

			// Notify that the resize is done
			m_area = RenderTargetSize();
			pr::events::Send(rdr::Evt_Resize(this, true, m_area)); // notify after changing the RT (with the new size)
		}

		// Get/Set the multi sampling used.
		// Changing the multi-sampling mode is a bit like resizing the back buffer
		MultiSamp Window::MultiSampling() const
		{
			return m_multisamp;
		}
		void Window::MultiSampling(MultiSamp ms)
		{
			auto device = m_rdr->Device();
			auto dc = m_rdr->ImmediateDC();
			auto area = RenderTargetSize();

			// Get the description of the existing swap chain
			DXGI_SWAP_CHAIN_DESC sd = {0};
			pr::Throw(m_swap_chain->GetDesc(&sd), "Failed to get current swap chain description");
			pr::Throw(!AllSet(sd.Flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) || ms.Count == 1, "GDI compatibility cannot be used with multi-sampling");

			// Check for feature support
			ms.Validate(device, sd.BufferDesc.Format);

			// Notify that a resize of the swap chain is about to happen.
			// Receivers need to ensure they don't have any outstanding references to the swap chain resources
			pr::events::Send(rdr::Evt_Resize(this, false, area)); // notify before changing the RT (with the old size)

			// Drop the render targets from the immediate context
			dc->OMSetRenderTargets(0, nullptr, nullptr);
			dc->ClearState();

			m_main_tex = nullptr;
			m_main_rtv = nullptr;
			m_main_srv = nullptr;
			m_main_dsv = nullptr;

			// Get the factory that was used to create 'device'
			D3DPtr<IDXGIDevice> dxgi_device;
			D3DPtr<IDXGIAdapter> adapter;
			D3DPtr<IDXGIFactory> factory;
			pr::Throw(device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));
			pr::Throw(dxgi_device->GetParent(__uuidof(IDXGIAdapter), (void**)&adapter.m_ptr));
			pr::Throw(adapter->GetParent(__uuidof(IDXGIFactory), (void **)&factory.m_ptr));

			sd.SampleDesc = ms;

			// Create a new swap chain with the new multi-sampling mode
			// Uses the flag 'DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE' to enable an application to
			// render using GDI on a swap chain or a surface. This will allow the application
			// to call IDXGISurface1::GetDC on the 0th back buffer or a surface.
			pr::Throw(factory->CreateSwapChain(device.m_ptr, &sd, &m_swap_chain.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain , pr::FmtS("swap chain")));

			m_multisamp = ms;

			// Set up the render targets again
			InitRT();
			RestoreRT();

			// Notify that the resize is done
			m_area = RenderTargetSize();
			pr::events::Send(rdr::Evt_Resize(this, true, m_area)); // notify after changing the RT (with the new size)
		}

		// Flip the scene to the display
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

			HRESULT res = m_swap_chain->Present(m_vsync, m_idle ? DXGI_PRESENT_TEST : 0);
			switch (res)
			{
			case S_OK:
				m_idle = false;
				break;

			// This happens when the window is not visible on-screen, the app should go into idle mode
			case DXGI_STATUS_OCCLUDED:
				m_idle = true;
				break;

			// The device failed due to a badly formed command. This is a run-time issue;
			// The application should destroy and recreate the device.
			case DXGI_ERROR_DEVICE_RESET:
				throw pr::Exception<HRESULT>(DXGI_ERROR_DEVICE_RESET, "Graphics adapter reset");

			// This happens in situations like, laptop un-docked, or remote desktop connect etc.
			// We'll just through so the app can shutdown/reset/whatever
			case DXGI_ERROR_DEVICE_REMOVED:
				throw pr::Exception<HRESULT>(m_rdr->Device()->GetDeviceRemovedReason(), "Graphics adapter no longer available");
			}
		}
	}
}