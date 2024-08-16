//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/main/settings.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/frame.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/utility/barrier_batch.h"

namespace pr::rdr12
{
	constexpr int HeapCapacityView = 256;
	constexpr int HeapCapacitySamp = 16;
	constexpr int HeapIdxMsaaRtv = 0;
	constexpr int HeapIdxMsaaDsv = 0;
	constexpr int HeapIdxSwapRtv = 1;

	// Constructor
	Window::Window(Renderer& rdr, WndSettings const& settings)
		:m_rdr(&rdr)
		,m_hwnd(settings.m_hwnd)
		,m_swap_chain_flags(settings.m_swap_chain_flags)
		,m_swap_chain_dbg()
		,m_swap_chain()
		,m_rtv_heap()
		,m_dsv_heap()
		,m_d2d_dc()
		,m_gsync(rdr.D3DDevice())
		,m_swap_bb(settings.m_buffer_count)
		,m_msaa_bb()
		,m_rt_props(settings.m_mode.Format, settings.m_bkgd_colour)
		,m_ds_props(settings.m_depth_format, 1.0f, 0)
		,m_cmd_alloc_pool(m_gsync)
		,m_cmd_list_pool(m_gsync)
		,m_heap_view(HeapCapacityView, m_gsync)
		,m_heap_samp(HeapCapacitySamp, m_gsync)
		,m_res_state()
		,m_diag(*this)
		,m_frame_number()
		,m_vsync(settings.m_vsync)
		,m_idle(false)
		,m_name(settings.m_name)
	{
		// Notes:
		//  The swap chain is the data that's sent to the monitor, and monitors can't display MSAA textures.
		//  In older versions of D3D, they allowed you to do this, and internally they pulled some magic to resolve the MSAA data
		//  to a non-MSAA texture before it was sent to the monitor. D3D12 gets rid of all that internal magic. So -
		//  create your swap chain as normal (non-MSAA), but create yourself another texture that is MSAA and render
		//  the scene to it. Then resolve your MSAA texture to your non-MSAA swap chain.
		//
		// Todo: w-buffer
		//  https://docs.microsoft.com/en-us/windows-hardware/drivers/display/w-buffering
		//  https://www.mvps.org/directx/articles/using_w-buffers.htm

		try
		{
			auto device = rdr.D3DDevice();

			// Validate settings
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && !AllSet(m_rdr->Settings().m_options, ERdrOptions::BGRASupport))
				Check(false, "D3D device has not been created with GDI compatibility");
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && settings.m_multisamp.Count != 1)
				Check(false, "GDI compatibility does not support multi-sampling");
			if (settings.m_vsync && (settings.m_mode.RefreshRate.Numerator == 0 || settings.m_mode.RefreshRate.Denominator == 0))
				Check(false, "If VSync is enabled, the refresh rate should be provided (matching the value returned from the graphics card)");
			if (settings.m_multisamp.Count < 1 || settings.m_multisamp.Count > D3D12_MAX_MULTISAMPLE_SAMPLE_COUNT)
				Check(false, "MSAA sample count invalid");

			// Check feature support
			auto const& features = rdr.Features();
			auto multisamp = settings.m_multisamp;
			multisamp.ScaleQualityLevel(device, m_rt_props.Format);
			multisamp.ScaleQualityLevel(device, m_ds_props.Format);
			if (multisamp.Count != 1 && !features.Format(m_rt_props.Format).Check(D3D12_FORMAT_SUPPORT1_RENDER_TARGET | D3D12_FORMAT_SUPPORT1_MULTISAMPLE_RESOLVE | D3D12_FORMAT_SUPPORT1_MULTISAMPLE_RENDERTARGET))
				Check(false, "Device does not support MSAA for requested render target format");
			if (multisamp.Count != 1 && !features.Format(m_ds_props.Format).Check(D3D12_FORMAT_SUPPORT1_DEPTH_STENCIL | D3D12_FORMAT_SUPPORT1_MULTISAMPLE_RENDERTARGET))
				Check(false, "Device does not support MSAA for requested depth stencil format");

			// Get the factory that was used to create 'rdr.m_device'
			D3DPtr<IDXGIFactory4> factory;
			Check(rdr.Adapter()->GetParent(__uuidof(IDXGIFactory4), (void **)&factory.m_ptr));

			// Create the swap chain, if there is a HWND. (Depth Stencil created into BackBufferSize)
			if (settings.m_hwnd != nullptr)
			{
				DXGI_SWAP_CHAIN_DESC1 desc0 = {
					.Width = settings.m_mode.Width,
					.Height = settings.m_mode.Height,
					.Format = settings.m_mode.Format,
					.Stereo = false, // interesting...
					.SampleDesc = MultiSamp{}, // Flip Swap chains are always 1 sample
					.BufferUsage = settings.m_usage,
					.BufferCount = settings.m_buffer_count,
					.Scaling = settings.m_scaling,
					.SwapEffect = settings.m_swap_effect,
					.AlphaMode = settings.m_alpha_mode,
					.Flags = s_cast<UINT>(settings.m_swap_chain_flags),
				};
				DXGI_SWAP_CHAIN_FULLSCREEN_DESC desc1 = {
					.RefreshRate = settings.m_mode.RefreshRate,
					.ScanlineOrdering = settings.m_mode.ScanlineOrdering,
					.Scaling = settings.m_mode.Scaling,
					.Windowed = settings.m_windowed,
				};

				// Create the swap chain. Swap chains only contain render targets, not deep buffers.
				D3DPtr<IDXGISwapChain1> swap_chain;
				Check(factory->CreateSwapChainForHwnd(rdr.GfxQueue(), settings.m_hwnd, &desc0, &desc1, nullptr, &swap_chain.m_ptr));
				Check(swap_chain->QueryInterface(&m_swap_chain.m_ptr));
				DebugName(m_swap_chain, "SwapChain");
		
				// Make DXGI monitor for Alt-Enter and switch between windowed and full screen
				Check(factory->MakeWindowAssociation(settings.m_hwnd, settings.m_allow_alt_enter ? 0 : DXGI_MWA_NO_ALT_ENTER));
			}
			else
			{
				// Creating a 'Window' with hwnd == nullptr is allowed if you only want to render to off-screen render targets.
				// todo
			}

			// Create a CPU descriptor heap for the back buffers (swap chain buffers + MSAA RT).
			D3D12_DESCRIPTOR_HEAP_DESC rtv_heap_desc = {
				.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
				.NumDescriptors = s_cast<UINT>(1 + m_swap_bb.size()),
				.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
			};
			Check(device->CreateDescriptorHeap(&rtv_heap_desc, __uuidof(ID3D12DescriptorHeap), (void**)&m_rtv_heap.m_ptr));

			// Create a CPU descriptor heap for the depth stencil
			D3D12_DESCRIPTOR_HEAP_DESC dsv_heap_desc = {
				.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
				.NumDescriptors = 1,
				.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
			};
			Check(device->CreateDescriptorHeap(&dsv_heap_desc, __uuidof(ID3D12DescriptorHeap), (void**)&m_dsv_heap.m_ptr));

			// If D2D is enabled, create a device context for this window
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
			{
				// Create a D2D device context
				Check(rdr.D2DDevice()->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS, &m_d2d_dc.m_ptr));
			}

			// In device debug mode, create a dummy swap chain so that the graphics debugging sees 'Present' calls allowing it to capture frames.
			if (AllSet(rdr.Settings().m_options, ERdrOptions::DeviceDebug))
			{
				#if 0 // todo
				DXGI_SWAP_CHAIN_DESC sd = {};
				sd.BufferCount  = 1;
				sd.BufferDesc   = DisplayMode(16,16);
				sd.SampleDesc   = MultiSamp();
				sd.BufferUsage  = DXGI_USAGE_RENDER_TARGET_OUTPUT;
				sd.OutputWindow = rdr.DummyHwnd();
				sd.Windowed     = TRUE;
				sd.SwapEffect   = DXGI_SWAP_EFFECT_FLIP_DISCARD;
				sd.Flags        = s_cast<DXGI_SWAP_CHAIN_FLAG>(0);
				Check(factory->CreateSwapChain(device, &sd, &m_swap_chain_dbg.m_ptr));
				PR_EXPAND(PR_DBG_RDR, DebugName(m_swap_chain_dbg, FmtS("swap chain dbg")));
				#endif
			}

			// Initialise the render target views
			BackBufferSize(iv2{s_cast<int>(settings.m_mode.Width), s_cast<int>(settings.m_mode.Height)}, true, &multisamp);
		}
		catch (...)
		{
			this->~Window();
			throw;
		}
	}
	Window::~Window()
	{
		// Release COM pointers
		m_rtv_heap = nullptr;
		m_dsv_heap = nullptr;
		m_msaa_bb = {};
		m_swap_bb.resize(0);
		m_gsync.Release();

		// Destroy the D2D device context
		if (m_d2d_dc != nullptr)
		{
			PR_EXPAND(PR_DBG_RDR, auto rcnt = m_d2d_dc.RefCount());
			PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the immediate device context");
			m_d2d_dc->SetTarget(nullptr);
			m_d2d_dc = nullptr;
		}

		// Destroying a swap chain
		if (m_swap_chain != nullptr)
		{
			// You may not release a swap chain in full-screen mode because doing so may create thread contention
			// (which will cause DXGI to raise a non-continuable exception). Before releasing a swap chain, first
			// switch to windowed mode (using IDXGISwapChain::SetFullscreenState( FALSE, NULL )) and then call IUnknown::Release.
			PR_EXPAND(PR_DBG_RDR, auto rcnt = m_swap_chain.RefCount());
			PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the swap chain");
			m_swap_chain->SetFullscreenState(FALSE, nullptr);
			m_swap_chain = nullptr;
		}

		// Release the debug swap chain
		if (m_swap_chain_dbg != nullptr)
		{
			PR_EXPAND(PR_DBG_RDR, auto rcnt = m_swap_chain_dbg.RefCount());
			PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the dbg swap chain");
			m_swap_chain_dbg->SetFullscreenState(FALSE, nullptr);
			m_swap_chain_dbg = nullptr;
		}
	}

	// Access the renderer manager classes
	ID3D12Device4* Window::d3d() const
	{
		return rdr().d3d();
	}
	Renderer& Window::rdr() const
	{
		return *m_rdr;
	}
	ResourceManager& Window::res() const
	{
		return rdr().res();
	}

	// Return the current DPI for this window. Use DIPtoPhysical(pt, Dpi()) for converting points
	v2 Window::Dpi() const
	{
		// Don't cache the DPI value. It can change at any point.
		#if WINVER >= 0x0605
		auto dpi = m_hwnd != nullptr ? (float)::GetDpiForWindow(m_hwnd) : (float)::GetDpiForSystem();
		return v2(dpi, dpi);
		#else
		// Support old windows by dynamically looking for the new DPI functions
		// and falling back to the GDI functions if not available.
		auto user32 = CreateStateScope(
			[=] { return ::LoadLibraryW(L"user32.dll"); }, 
			[=](HMODULE m) {::FreeLibrary(m); });

		// Look for the new windows functions for DPI
		auto GetDpiForWindowFunc = reinterpret_cast<UINT(far __stdcall*)(HWND)>(GetProcAddress(user32.m_state, "GetDpiForWindow"));
		auto GetDpiForSystemFunc = reinterpret_cast<UINT(far __stdcall*)()>(GetProcAddress(user32.m_state, "GetDpiForSystem"));

		if (m_hwnd != nullptr && GetDpiForWindowFunc != nullptr)
		{
			auto dpi = (float)GetDpiForWindowFunc(m_hwnd);
			return v2(dpi, dpi);
		}
		if (GetDpiForSystemFunc != nullptr)
		{
			auto dpi = (float)GetDpiForSystemFunc();
			return v2(dpi, dpi);
		}

		gdi::Graphics g(m_hwnd);
		auto dpi = v2(g.GetDpiX(), g.GetDpiY());
		return dpi;
		//auto desktop_dc = CreateStateScope(
		//	[&] { return g.GetHDC(); },
		//	[&](HDC dc) { g.ReleaseHDC(dc); });
		//
		//auto logical_screen_height  = GetDeviceCaps(desktop_dc.m_state, VERTRES);
		//auto physical_screen_height = GetDeviceCaps(desktop_dc.m_state, DESKTOPVERTRES); 
		//if (logical_screen_height  != 0 && physical_screen_height != 0)
		//	dpi = physical_screen_height * 96.0f / logical_screen_height;
		//else
		//	dpi = 96.0f;
		#endif
	}
	
	// The current swap chain back buffer index
	int Window::BBIndex() const
	{
		return m_swap_chain != nullptr ? m_swap_chain->GetCurrentBackBufferIndex() : 0;
	}
	int Window::BBCount() const
	{
		return s_cast<int>(m_swap_bb.size());
	}

	// The number of times 'RenderFrame' has been called
	int64_t Window::FrameNumber() const
	{
		return m_frame_number;
	}

	// Get/Set the window background colour / clear value
	Colour Window::BkgdColour() const
	{
		return Colour(m_rt_props.Color);
	}
	void Window::BkgdColour(Colour const& colour)
	{
		if (BkgdColour() == colour)
			return;

		// Need to recreate the render targets because the clear colour has changed
		m_rt_props.Color[0] = colour.r;
		m_rt_props.Color[1] = colour.g;
		m_rt_props.Color[2] = colour.b;
		m_rt_props.Color[3] = colour.a;
		BackBufferSize(BackBufferSize(), true);
	}

	// Get/Set the size of the swap chain back buffer (basically the window size in pixels)
	iv2 Window::BackBufferSize() const
	{
		auto rt = m_swap_bb[BBIndex()].m_render_target.get();
		if (rt == nullptr)
			return iv2::Zero();

		auto desc = rt->GetDesc();
		return iv2(s_cast<int>(desc.Width), s_cast<int>(desc.Height));
	}
	void Window::BackBufferSize(iv2 size, bool force, MultiSamp const* multisamp)
	{
		if (size.x <= 0 || size.y <= 0)
			throw std::runtime_error("Back buffer size must be greater than zero");

		// No-op if not a resize
		if (!force && BackBufferSize() == size)
			return;

		auto queue = rdr().GfxQueue();

		// Flush any GPU commands that are still in flight
		m_gsync.AddSyncPoint(queue);
		m_gsync.Wait();

		// Release references to swap chain resources
		for (auto& bb : m_swap_bb)
		{
			m_res_state.Forget(bb.m_render_target.get());
			m_res_state.Forget(bb.m_depth_stencil.get());

			bb.m_render_target = nullptr;
			bb.m_depth_stencil = nullptr;
			bb.m_d2d_target = nullptr;
		}
		if (auto& bb = m_msaa_bb; true)
		{
			m_res_state.Forget(bb.m_render_target.get());
			m_res_state.Forget(bb.m_depth_stencil.get());

			bb.m_render_target = nullptr;
			bb.m_depth_stencil = nullptr;
			bb.m_d2d_target = nullptr;
		}

		// Finished releasing references. Now to create new resources.

		// Swap chain render targets
		if (m_swap_chain != nullptr)
		{
			CreateSwapChain(size);
		}

		// MSAA render target
		if (auto& bb = m_msaa_bb; true)
		{
			multisamp = multisamp != nullptr ? multisamp : &bb.m_multisamp;
			CreateMSAA(bb, size, *multisamp);
		}
	}

	// Get/Set the multi sampling used.
	MultiSamp Window::MultiSampling() const
	{
		return m_msaa_bb.m_multisamp;
	}
	void Window::MultiSampling(MultiSamp ms)
	{
		if (MultiSampling() == ms)
			return;

		// Check feature support
		auto const& features = rdr().Features();
		ms.ScaleQualityLevel(d3d(), m_rt_props.Format);
		ms.ScaleQualityLevel(d3d(), m_ds_props.Format);
		if (ms.Count != 1 && !features.Format(m_rt_props.Format).Check(D3D12_FORMAT_SUPPORT1_RENDER_TARGET | D3D12_FORMAT_SUPPORT1_MULTISAMPLE_RESOLVE | D3D12_FORMAT_SUPPORT1_MULTISAMPLE_RENDERTARGET))
			Check(false, "Device does not support MSAA for requested render target format");
		if (ms.Count != 1 && !features.Format(m_ds_props.Format).Check(D3D12_FORMAT_SUPPORT1_DEPTH_STENCIL | D3D12_FORMAT_SUPPORT1_MULTISAMPLE_RENDERTARGET))
			Check(false, "Device does not support MSAA for requested depth stencil format");

		auto queue = rdr().GfxQueue();
		auto size = BackBufferSize();

		// Flush any GPU commands that are still in flight
		m_gsync.AddSyncPoint(queue);
		m_gsync.Wait();

		// Release the MSAA render target and depth stencil
		if (auto& bb = m_msaa_bb; true)
		{
			m_res_state.Forget(bb.m_render_target.get());
			m_res_state.Forget(bb.m_depth_stencil.get());

			bb.m_render_target = nullptr;
			bb.m_depth_stencil = nullptr;
			bb.m_d2d_target = nullptr;
		}

		// Create MSAA render target
		if (auto& bb = m_msaa_bb; true)
		{
			CreateMSAA(bb, size, ms);
		}
	}

	// Start rendering a new frame. Returns an object that scenes can render into
	Frame Window::NewFrame()
	{
		++m_frame_number;

		// Flush any pending resource commands to the GPU
		res().FlushToGpu(EGpuFlush::Async);

		// Get the current swap chain back buffer
		auto& bb_main = m_msaa_bb;
		auto& bb_post = m_swap_bb[BBIndex()];

		// Ensure the back buffer is available for rendering
		m_gsync.Wait(bb_main.m_sync_point);
		m_gsync.Wait(bb_post.m_sync_point);

		// Bind D2D to the current swap chain render target
		if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
			m_d2d_dc->SetTarget(bb_post.m_d2d_target.get());

		// Create the frame object to be passed to the scenes 
		Frame frame(d3d(), bb_main, bb_post, m_cmd_alloc_pool);

		// Prepare
		if (bb_main.m_render_target != nullptr && bb_post.m_render_target != nullptr)
		{
			// The MSAA render target goes to the 'render target' state
			BarrierBatch bb(frame.m_prepare);
			bb.Transition(bb_main.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
			bb.Transition(bb_post.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
			bb.Commit();

			frame.m_prepare.ClearRenderTargetView(bb_main.m_rtv, bb_main.rt_clear());
			frame.m_prepare.ClearDepthStencilView(bb_main.m_dsv, D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, bb_main.ds_depth(), bb_main.ds_stencil());
		}
		else if (bb_main.m_render_target != nullptr)
		{
			// The MSAA render target goes to the 'resolve source' state
			BarrierBatch bb(frame.m_prepare);
			bb.Transition(bb_main.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
			bb.Commit();
		}

		// Resolve
		if (bb_main.m_render_target != nullptr && bb_post.m_render_target != nullptr)
		{
			if (bb_main.m_multisamp.Count > 1)
			{
				// Resolve the MSAA render target into the swap chain render target
				BarrierBatch bb(frame.m_resolve);
				bb.Transition(bb_main.m_render_target.get(), D3D12_RESOURCE_STATE_RESOLVE_SOURCE);
				bb.Transition(bb_post.m_render_target.get(), D3D12_RESOURCE_STATE_RESOLVE_DEST);
				bb.Commit();

				// Resolve the msaa render target into the swap chain render target
				frame.m_resolve.ResolveSubresource(bb_post.m_render_target.get(), bb_main.m_render_target.get(), m_rt_props.Format);

				// The swap chain render target goes to the 'render target' state
				bb.Transition(bb_post.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
				bb.Transition(bb_main.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
				bb.Commit();
			}
			else
			{
				// Copy the MSAA render target into the swap chain render target
				BarrierBatch bb(frame.m_resolve);
				bb.Transition(bb_main.m_render_target.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
				bb.Transition(bb_post.m_render_target.get(), D3D12_RESOURCE_STATE_COPY_DEST);
				bb.Commit();

				frame.m_resolve.CopyResource(bb_post.m_render_target.get(), bb_main.m_render_target.get());

				// The swap chain render target goes to the 'render target' state
				bb.Transition(bb_post.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
				bb.Transition(bb_main.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
				bb.Commit();
			}
		}

		// Present
		if (bb_post.m_render_target != nullptr)
		{
			// The swap chain render target goes to the 'present' state
			BarrierBatch bb(frame.m_present);
			bb.Transition(bb_post.m_render_target.get(), D3D12_RESOURCE_STATE_PRESENT);
			bb.Commit();
		}

		// Return the frame object
		return frame;
	}
	
	// Present the frame to the display
	void Window::Present(Frame& frame, EGpuFlush flush)
	{
		// Be careful that you never have the message-pump thread wait on the render thread.
		// For instance, calling IDXGISwapChain1::Present1 (from the render thread) may cause
		// the render thread to wait on the message-pump thread. When a mode change occurs,
		// this scenario is possible if Present1 calls ::SetWindowPos() or ::SetWindowStyle()
		// and either of these methods call ::SendMessage(). In this scenario, if the message-pump
		// thread has a critical section guarding it or if the render thread is blocked, then the
		// two threads will deadlock.
		if (flush == EGpuFlush::DontFlush)
			return;

		frame.m_prepare.Close();
		frame.m_resolve.Close();
		frame.m_present.Close();

		// Submit the command lists to the GPU
		rdr().ExecuteGfxCommandLists({
			frame.m_prepare,
			frame.m_main,
			frame.m_resolve,
			frame.m_post,
			frame.m_present,
		});

		// Present with the debug swap chain so that graphics debugging detects a frame
		if (m_swap_chain_dbg != nullptr)
			m_swap_chain_dbg->Present(m_vsync, 0);

		// If there is no swap chain, then we must be rendering to an off-screen texture.
		// In that case, flush to the graphics card
		if (m_swap_chain == nullptr)
		{
			#if 0 // todo
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
				Check(res);
			}
			#endif
			return;
		}

		// Render to the display
		// IDXGISwapChain1::Present1 will inform you if your output window is entirely occluded via DXGI_STATUS_OCCLUDED.
		// When this occurs, we recommended that your application go into standby mode (by calling IDXGISwapChain1::Present1
		// with DXGI_PRESENT_TEST) since resources used to render the frame are wasted. Using DXGI_PRESENT_TEST will prevent
		// any data from being presented while still performing the occlusion check. Once IDXGISwapChain1::Present1 returns
		// S_OK, you should exit standby mode; do not use the return code to switch to standby mode as doing so can leave
		// the swap chain unable to relinquish full-screen mode.
		// ^^ This means: Don't use calls to Present(?, DXGI_PRESENT_TEST) to test if the window is occluded,
		// only use it after Present() has returned DXGI_STATUS_OCCLUDED.
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
				Check(DXGI_ERROR_DEVICE_RESET, "Graphics adapter reset");
			}
			case DXGI_ERROR_DEVICE_REMOVED:
			{
				// This happens in situations like, laptop un-docked, or remote desktop connect etc.
				// We'll just throw so the app can shutdown/reset/whatever
				Check(rdr().D3DDevice()->GetDeviceRemovedReason(), "Graphics adapter no longer available");
			}
			default:
			{
				throw std::runtime_error("Unknown result from SwapChain::Present");
			}
		}

		// Set the next sync point for the swap chain back buffer
		auto sync_point = m_gsync.AddSyncPoint(rdr().GfxQueue());
		frame.m_bb_main.m_sync_point = sync_point;
		frame.m_bb_post.m_sync_point = sync_point;

		if (flush == EGpuFlush::Block)
			m_gsync.Wait();
	}

	// Create the MSAA render target and depth stencil
	void Window::CreateMSAA(BackBuffer& bb, iv2 size, MultiSamp ms)
	{
		auto device = rdr().D3DDevice();

		bb.m_wnd = this;
		bb.m_sync_point = m_gsync.CompletedSyncPoint();
		bb.m_multisamp = ms;

		// Render target
		auto rtdesc = ResDesc::Tex2D(Image{ size.x, size.y, nullptr, m_rt_props.Format }, 1U, EUsage::RenderTarget)
			.multisamp(ms)
			.clear(m_rt_props);
		Check(device->CreateCommittedResource(
			&HeapProps::Default(), D3D12_HEAP_FLAG_NONE, &rtdesc, D3D12_RESOURCE_STATE_RENDER_TARGET,
			&*rtdesc.ClearValue, __uuidof(ID3D12Resource), (void**)&bb.m_render_target.m_ptr));
		DefaultResState(bb.m_render_target.get(), D3D12_RESOURCE_STATE_RENDER_TARGET);
		DebugName(bb.m_render_target, "RenderTarget");

		// Depth stencil
		auto dsdesc = ResDesc::Tex2D(Image{ size.x, size.y, nullptr, m_ds_props.Format }, 1U, EUsage::DepthStencil | EUsage::DenyShaderResource)
			.multisamp(ms)
			.clear(m_ds_props);
		Check(device->CreateCommittedResource(
			&HeapProps::Default(), D3D12_HEAP_FLAG_NONE, &dsdesc, D3D12_RESOURCE_STATE_DEPTH_WRITE,
			&*dsdesc.ClearValue, __uuidof(ID3D12Resource), (void**)&bb.m_depth_stencil.m_ptr));
		DefaultResState(bb.m_depth_stencil.get(), D3D12_RESOURCE_STATE_DEPTH_WRITE);
		DebugName(bb.m_depth_stencil, "DepthStencil");

		// Save the pointer to where the RTV descriptor will be stored
		D3D12_CPU_DESCRIPTOR_HANDLE rtv_handle = m_rtv_heap->GetCPUDescriptorHandleForHeapStart();
		auto rtv_size = s_cast<int64_t>(device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV));
		rtv_handle.ptr += HeapIdxMsaaRtv * rtv_size;
		bb.m_rtv = rtv_handle;

		// Save the pointer to where the DSV descriptor will be stored
		D3D12_CPU_DESCRIPTOR_HANDLE dsv_handle = m_dsv_heap->GetCPUDescriptorHandleForHeapStart();
		auto dsv_size = s_cast<int64_t>(device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV));
		dsv_handle.ptr += HeapIdxMsaaDsv * dsv_size;
		bb.m_dsv = dsv_handle;

		// Create RTV for the MSAA render target
		auto rtvdesc = D3D12_RENDER_TARGET_VIEW_DESC{
			.Format = m_rt_props.Format,
			.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2DMS,
		};
		device->CreateRenderTargetView(bb.m_render_target.get(), &rtvdesc, bb.m_rtv);

		// Create the DSV for the MSAA depth stencil
		auto dsvdesc = D3D12_DEPTH_STENCIL_VIEW_DESC{
			.Format = m_ds_props.Format,
			.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2DMS,
		};
		device->CreateDepthStencilView(bb.m_depth_stencil.get(), &dsvdesc, bb.m_dsv);
	}

	// Create the swap chain back buffers
	void Window::CreateSwapChain(iv2 size)
	{
		auto device = rdr().D3DDevice();

		DXGI_SWAP_CHAIN_DESC scdesc = {};
		Check(m_swap_chain->GetDesc(&scdesc));
		Check(m_swap_chain->ResizeBuffers(s_cast<UINT>(m_swap_bb.size()), size.x, size.y, scdesc.BufferDesc.Format, scdesc.Flags));

		// Get the pointer and item size of the RTV descriptor heap.
		D3D12_CPU_DESCRIPTOR_HANDLE rtv_handle = m_rtv_heap->GetCPUDescriptorHandleForHeapStart();
		auto rtv_size = s_cast<int64_t>(device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV));
		rtv_handle.ptr += HeapIdxSwapRtv * rtv_size;

		for (int i = 0; i != s_cast<int>(m_swap_bb.size()); ++i)
		{
			auto& bb = m_swap_bb[i];
			bb.m_wnd = this;
			bb.m_sync_point = m_gsync.CompletedSyncPoint();

			// Get the render target resource pointer
			Check(m_swap_chain->GetBuffer(s_cast<UINT>(i), __uuidof(ID3D12Resource), (void**)&bb.m_render_target.m_ptr));
			DefaultResState(bb.m_render_target.get(), D3D12_RESOURCE_STATE_PRESENT);
			DebugName(bb.m_render_target, FmtS("SwapChainRT-%d", i));

			// Save the pointers to where the descriptors will be stored
			bb.m_rtv = rtv_handle; rtv_handle.ptr += rtv_size; // one RTV for each back buffer
			bb.m_dsv = {};

			// Create RTVs for the back buffer resources.
			auto rtvdesc = D3D12_RENDER_TARGET_VIEW_DESC{ .Format = m_rt_props.Format, .ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D };
			device->CreateRenderTargetView(bb.m_render_target.get(), &rtvdesc, bb.m_rtv);

			// Re-link the D2D device context to the back buffer
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
			{
				// Direct2D needs the DXGI version of the back buffer
				D3DPtr<IDXGISurface> dxgi_back_buffer;
				Check(m_swap_chain->GetBuffer(s_cast<UINT>(i), __uuidof(IDXGISurface), (void**)&dxgi_back_buffer.m_ptr));

				// Create bitmap properties for the bitmap view of the back buffer
				auto bp = D2D1::BitmapProperties1(D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW, D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_IGNORE));
				auto dpi = Dpi();
				bp.dpiX = dpi.x;
				bp.dpiY = dpi.y;

				// Wrap the back buffer as a bitmap for D2D
				Check(m_d2d_dc->CreateBitmapFromDxgiSurface(dxgi_back_buffer.get(), &bp, &bb.m_d2d_target.m_ptr));
			}
		}
	}
}

	#if 0 // todo
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

		// Flip the back buffer after presenting
		auto do_flip = Scope([] {}, [this]
		{
			// Flip n-buffered items
			m_main_rt.flip();
			m_cmd_alloc.flip();
				
			// Reset (re-use) the memory associated with the next available command allocator.
			Check(m_cmd_alloc[0]->Reset());
		});

		// Present with the debug swap chain so that graphics debugging detects a frame
		if (m_swap_chain_dbg != nullptr)
			m_swap_chain_dbg->Present(m_vsync, 0);

		// If there is no swap chain, then we must be rendering to an off-screen texture.
		// In that case, flush to the graphics card
		if (m_swap_chain == nullptr)
		{
			#if 0 // todo
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
				Check(res);
			}
			#endif
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
	#endif


	#if 0 // unused
	void Window::TestRender()
	{
		Renderer::Lock lock(*m_rdr);
		auto device = lock.D3DDevice();

		// The first step in rendering is that we reset both the command allocator and command list memory.
		// You will notice here we use a pipeline that is currently NULL. This is because pipelines require
		// shaders and extra setup that we will not be covering until the next tutorial.

		// Reset (re-use) the memory associated command allocator.
		Check(m_cmd_alloc->Reset());

		// Reset the command list, use empty pipeline state for now since there are no shaders and we are just clearing the screen.
		Check(m_cmd_list->Reset(m_cmd_alloc.get(), m_pipeline_state.get()));

		// The second step is to use a resource barrier to synchronize/transition the next back buffer for rendering. We then set that as a step in the command list.

		// Record commands in the command list now.
		// Start by setting the resource barrier.
		D3D12_RESOURCE_BARRIER barrier;
		barrier.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
		barrier.Transition.pResource = m_main_rt[m_bb_index].get();
		barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_PRESENT;
		barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET;
		barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
		barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
		m_cmd_list->ResourceBarrier(1, &barrier);
	
		// The third step is to get the back buffer view handle and then set the back buffer as the render target.

		// Get the render target view handle for the current back buffer.
		D3D12_CPU_DESCRIPTOR_HANDLE rtv_handle = m_rtv_heap->GetCPUDescriptorHandleForHeapStart();
		rtv_handle.ptr += m_bb_index * device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

		// Set the back buffer as the render target.
		m_cmd_list->OMSetRenderTargets(1, &rtv_handle, false, nullptr);
	
		// In the fourth step we set the clear color and clear the render target using that color and submit that to the command list.

		// Then set the color to clear the window to.
		float rgba[4] = {1.0f, 1.0f, 0.0f, 1.0f};
		m_cmd_list->ClearRenderTargetView(rtv_handle, rgba, 0, nullptr);
	
		// And finally we then set the state of the back buffer to transition into a presenting state and store that in the command list.

		// Indicate that the back buffer will now be used to present.
		barrier.Transition.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET;
		barrier.Transition.StateAfter = D3D12_RESOURCE_STATE_PRESENT;
		m_cmd_list->ResourceBarrier(1, &barrier);
	
		// Once we are done our rendering list we close the command list and then submit it to the command queue to execute that list for us.
		Check(m_cmd_list->Close());

		// Execute the list of commands.
		auto cmd_lists = {static_cast<ID3D12CommandList*>(m_cmd_list.get())};
		lock.CmdQueue()->ExecuteCommandLists(1, cmd_lists.begin());

		// We then call the swap chain to present the rendered frame to the screen.

		// Finally present the back buffer to the screen since rendering is complete.
		Check(m_swap_chain->Present(m_vsync, 0));

		// Then we setup the fence to synchronize and let us know when the GPU is complete rendering.
		// For this tutorial we just wait infinitely until it's done this single command list.
		// However you can get optimisations by doing other processing while waiting for the GPU to finish.

		// Add a sync point, and wait
		m_gsync_sync.AddSyncPoint(lock.CmdQueue());
		m_gsync_sync.Wait();

		// For the next frame swap to the other back buffer using the alternating index.

		// Alternate the back buffer index back and forth between 0 and 1 each frame.
		m_bb_index = (m_bb_index + 1) % m_bb_count;
	}
	#endif