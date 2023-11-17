//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/main/settings.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/scene/scene.h"

namespace pr::rdr12
{
	constexpr int HeapCapacityView = 256;
	constexpr int HeapCapacitySamp = 16;

	// Constructor
	Window::Window(Renderer& rdr, WndSettings const& settings)
		:m_rdr(&rdr)
		,m_hwnd(settings.m_hwnd)
		,m_db_format(settings.m_depth_format)
		,m_multisamp()
		,m_swap_chain_flags(settings.m_swap_chain_flags)
		,m_swap_chain_dbg()
		,m_swap_chain()
		,m_depth_stencil()
		,m_rtv_heap()
		,m_dsv_heap()
		,m_d2d_dc()
		,m_gsync(rdr.D3DDevice())
		,m_bb(settings.m_buffer_count)
		,m_cmd_alloc_pool(m_gsync)
		,m_cmd_list_pool(m_gsync)
		,m_cmd_lists()
		,m_heap_view(HeapCapacityView, &m_gsync)
		,m_heap_samp(HeapCapacitySamp, &m_gsync)
		,m_diag(*this)
		,m_frame_number()
		,m_vsync(settings.m_vsync)
		,m_idle(false)
		,m_name(settings.m_name)
	{
		try
		{
			// Validate settings
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && !AllSet(m_rdr->Settings().m_options, ERdrOptions::BGRASupport))
				Throw(false, "D3D device has not been created with GDI compatibility");
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && settings.m_multisamp.Count != 1)
				Throw(false, "GDI compatibility does not support multi-sampling");
			if (settings.m_vsync && (settings.m_mode.RefreshRate.Numerator == 0 || settings.m_mode.RefreshRate.Denominator == 0))
				Throw(false, "If VSync is enabled, the refresh rate should be provided (matching the value returned from the graphics card)");
			//todo: w-buffer
			// https://docs.microsoft.com/en-us/windows-hardware/drivers/display/w-buffering
			// https://www.mvps.org/directx/articles/using_w-buffers.htm
		
			auto device = rdr.D3DDevice();

			// Check feature support
			m_multisamp.Validate(device, settings.m_mode.Format);
			m_multisamp.Validate(device, settings.m_depth_format);
		
			// Get the factory that was used to create 'rdr.m_device'
			D3DPtr<IDXGIFactory4> factory;
			Throw(rdr.Adapter()->GetParent(__uuidof(IDXGIFactory4), (void **)&factory.m_ptr));

			// Create the swap chain, if there is a HWND. (Depth Stencil created into BackBufferSize)
			if (settings.m_hwnd != nullptr)
			{
				DXGI_SWAP_CHAIN_DESC1 desc0 = {
					.Width = settings.m_mode.Width,
					.Height = settings.m_mode.Height,
					.Format = settings.m_mode.Format,
					.Stereo = false, // interesting...
					.SampleDesc = m_multisamp,
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

				// Create the swap chain
				D3DPtr<IDXGISwapChain1> swap_chain;
				Throw(factory->CreateSwapChainForHwnd(rdr.GfxQueue(), settings.m_hwnd, &desc0, &desc1, nullptr, &swap_chain.m_ptr));
				Throw(swap_chain->QueryInterface(&m_swap_chain.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain.get(), FmtS("swap chain")));
		
				// Make DXGI monitor for Alt-Enter and switch between windowed and full screen
				Throw(factory->MakeWindowAssociation(settings.m_hwnd, settings.m_allow_alt_enter ? 0 : DXGI_MWA_NO_ALT_ENTER));
			}
			else
			{
				// Creating a 'Window' with hwnd == nullptr is allowed if you only want to render to off-screen render targets.
				// todo
			}

			// Create a CPU descriptor heap for the back buffer render targets.
			D3D12_DESCRIPTOR_HEAP_DESC rtv_heap_desc = {
				.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
				.NumDescriptors = s_cast<UINT>(m_bb.size()),
				.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
			};
			Throw(device->CreateDescriptorHeap(&rtv_heap_desc, __uuidof(ID3D12DescriptorHeap), (void**)&m_rtv_heap.m_ptr));

			// Create a CPU descriptor heap for the depth stencil
			D3D12_DESCRIPTOR_HEAP_DESC dsv_heap_desc = {
				.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
				.NumDescriptors = 1,
				.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
			};
			Throw(device->CreateDescriptorHeap(&dsv_heap_desc, __uuidof(ID3D12DescriptorHeap), (void**)&m_dsv_heap.m_ptr));

			// If D2D is enabled, create a device context for this window
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
			{
				// Create a D2D device context
				Throw(rdr.D2DDevice()->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_ENABLE_MULTITHREADED_OPTIMIZATIONS, &m_d2d_dc.m_ptr));
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
				Throw(factory->CreateSwapChain(device, &sd, &m_swap_chain_dbg.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain_dbg.get(), FmtS("swap chain dbg")));
				#endif
			}

			// Initialise the render target views
			BackBufferSize(iv2{s_cast<int>(settings.m_mode.Width), s_cast<int>(settings.m_mode.Height)}, true);
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
		m_depth_stencil = nullptr;
		m_bb.resize(0);
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
	
	// The current back buffer index
	int Window::BBIndex() const
	{
		return m_swap_chain != nullptr ? m_swap_chain->GetCurrentBackBufferIndex() : 0;
	}
	int Window::BBCount() const
	{
		return s_cast<int>(m_bb.size());
	}

	// The number of times 'RenderFrame' has been called
	int64_t Window::FrameNumber() const
	{
		return m_frame_number;
	}

	// Get/Set the size of the back buffer render target
	iv2 Window::BackBufferSize() const
	{
		auto rt = m_bb[BBIndex()].m_render_target.get();
		if (rt == nullptr)
			return iv2::Zero();

		auto desc = rt->GetDesc();
		return iv2(s_cast<int>(desc.Width), s_cast<int>(desc.Height));
	}
	void Window::BackBufferSize(iv2 size, bool force)
	{
		if (size.x <= 0 || size.y <= 0)
			throw std::runtime_error("Back buffer size must be greater than zero");

		auto queue = rdr().GfxQueue();
		auto device = rdr().D3DDevice();

		// No-op if not a resize
		if (!force && BackBufferSize() == size)
			return;

		// Flush any GPU commands that are still in flight
		m_gsync.AddSyncPoint(queue);
		m_gsync.Wait();

		// Release references to swap chain resources
		for (auto& bb : m_bb)
		{
			bb.m_render_target = nullptr;
			bb.m_depth_stencil = nullptr;
			bb.m_d2d_target = nullptr;
		}

		// Resize the swap chain buffers
		if (m_swap_chain != nullptr)
		{
			DXGI_SWAP_CHAIN_DESC scdesc = {};
			Throw(m_swap_chain->GetDesc(&scdesc));
			Throw(m_swap_chain->ResizeBuffers(s_cast<UINT>(m_bb.size()), size.x, size.y, scdesc.BufferDesc.Format, scdesc.Flags));
		}

		// Default to the description of the current depth buffer if it exists
		auto dsdesc = m_depth_stencil != nullptr ? m_depth_stencil->GetDesc() : ResDesc::Tex2D(Image{16, 16, nullptr, DXGI_FORMAT_D32_FLOAT}, 1U, EUsage::DepthStencil|EUsage::DenyShaderResource);
		dsdesc.Width = size.x;
		dsdesc.Height = size.y;
		auto clear_value = D3D12_CLEAR_VALUE {
			.Format = DXGI_FORMAT_D32_FLOAT,
			.DepthStencil = {.Depth = 1.0f, .Stencil=0},
		};

		// Resize the depth stencil
		m_depth_stencil = nullptr;
		Throw(device->CreateCommittedResource(
			&HeapProps::Default(), D3D12_HEAP_FLAG_NONE, &dsdesc,
			D3D12_RESOURCE_STATE_DEPTH_WRITE, &clear_value,
			__uuidof(ID3D12Resource), (void**)&m_depth_stencil.m_ptr));
		Throw(m_depth_stencil->SetName(L"DepthStencil"));

		// Get the pointer and item size of the RTV descriptor heap.
		D3D12_CPU_DESCRIPTOR_HANDLE rtv_handle = m_rtv_heap->GetCPUDescriptorHandleForHeapStart();
		auto rtv_size = device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

		// Get the pointer and item size of the DSV descriptor heap.
		D3D12_CPU_DESCRIPTOR_HANDLE dsv_handle = m_dsv_heap->GetCPUDescriptorHandleForHeapStart();
		auto dsv_size = device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

		// Populate the back buffer objects
		for (int i = 0; i != s_cast<int>(m_bb.size()); ++i)
		{
			auto& bb = m_bb[i];
			bb.m_wnd = this;
			bb.m_bb_index = i;
			bb.m_sync_point = m_gsync.CompletedSyncPoint();
			
			// Save the pointers to the descriptors
			bb.m_rtv = rtv_handle;
			bb.m_rtv.ptr += i * rtv_size; // one RTV for each back buffer
			bb.m_dsv = dsv_handle;
			bb.m_dsv.ptr += 0 * dsv_size; // there is only one DSV

			// Create RTVs for the back buffer resources.
			if (m_swap_chain != nullptr)
			{
				Throw(m_swap_chain->GetBuffer(s_cast<UINT>(i), __uuidof(ID3D12Resource), (void**)&bb.m_render_target.m_ptr));
				device->CreateRenderTargetView(bb.m_render_target.get(), nullptr, bb.m_rtv);
			}
			else
			{
				// If the renderer has been created without a window handle, there will be no swap chain.
				// In this case the caller will be setting up a render target to an off-screen buffer.
				// todo - off-screen rendering should create it's on back buffer which will need an rtv
			}

			// Create DSV for the depth stencil buffer
			bb.m_depth_stencil = m_depth_stencil;
			device->CreateDepthStencilView(bb.m_depth_stencil.get(), nullptr, bb.m_dsv);

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
				Throw(m_d2d_dc->CreateBitmapFromDxgiSurface(dxgi_back_buffer.get(), &bp, &bb.m_d2d_target.m_ptr));
			}
		}
	}

	// Render a frame
	Window::Frame Window::RenderFrame()
	{
		// Get the current back buffer
		auto& bb = m_bb[BBIndex()];

		// Ensure the back buffer is available for rendering
		m_gsync.Wait(bb.m_sync_point);

		// Bind D2D to the current render target
		if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE))
			m_d2d_dc->SetTarget(bb.m_d2d_target.get());

		// Flush any pending resource commands to the GPU
		res().FlushToGpu(false);

		// Start a frame
		++m_frame_number;
		m_cmd_lists.resize(0);
		return Frame(bb, m_cmd_lists);
	}

	// Construct a frame render instance
	Window::Frame::Frame(BackBuffer& bb, CmdLists& cmd_lists)
		:m_bb(bb)
		,m_cmd_lists(cmd_lists)
	{
		m_cmd_lists.push_back(nullptr); // Make a slot for the first barrier
	}

	// Render a scene
	void Window::Frame::Render(Scene& scene)
	{
		// Start the scene rendering - If this is done in a background thread, 'scene.Render' will return immediately
		// returning the not-yet-closed command list that it is rendering to. This allows the window to submit the
		// command lists in the same order that the caller called 'Render' in.
		// Remember: One allocator per thread, per list, per frame.

		auto cmd_list = scene.Render(m_bb);
		m_cmd_lists.insert(m_cmd_lists.end(), cmd_list.begin(), cmd_list.end());
	}

	// Present the frame
	void Window::Frame::Present()
	{
		// Add a resource barrier for switching the back buffer to the render target state
		D3D12_RESOURCE_BARRIER barrier0 = {
			.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
			.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
			.Transition = {
				.pResource = m_bb.m_render_target.get(),
				.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
				.StateBefore = D3D12_RESOURCE_STATE_PRESENT,
				.StateAfter = D3D12_RESOURCE_STATE_RENDER_TARGET,
			},
		};
		auto cmd_list0 = m_bb.wnd().m_cmd_list_pool.Get();
		cmd_list0.Reset(m_bb.wnd().m_cmd_alloc_pool.Get());
		cmd_list0.ResourceBarrier(barrier0);
		cmd_list0.Close();
		m_cmd_lists[0] = cmd_list0;

		// Transition the back buffer to the presenting state.
		D3D12_RESOURCE_BARRIER barrier1 = {
			.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
			.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
			.Transition = {
				.pResource = m_bb.m_render_target.get(),
				.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
				.StateBefore = D3D12_RESOURCE_STATE_RENDER_TARGET,
				.StateAfter = D3D12_RESOURCE_STATE_PRESENT,
			},
		};
		auto cmd_list1 = m_bb.wnd().m_cmd_list_pool.Get();
		cmd_list1.Reset(m_bb.wnd().m_cmd_alloc_pool.Get());
		cmd_list1.ResourceBarrier(barrier1);
		cmd_list1.Close();
		m_cmd_lists.push_back(cmd_list1);

		// todo - Wait until all rendering worker threads have completed 
		{}

		// Submit the command lists to the GPU
		auto queue = m_bb.rdr().GfxQueue();
		queue->ExecuteCommandLists(s_cast<UINT>(m_cmd_lists.size()), m_cmd_lists.data());

		// Flip the swap chain
		m_bb.wnd().Flip();

		// Set the next sync point for this backbuffer
		m_bb.m_sync_point = m_bb.wnd().m_gsync.AddSyncPoint(queue);
	}

	// Move to the next back buffer
	void Window::Flip()
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
				Throw(res);
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
				throw Exception<HRESULT>(rdr().D3DDevice()->GetDeviceRemovedReason(), "Graphics adapter no longer available");
			}
			default:
			{
				throw std::runtime_error("Unknown result from SwapChain::Present");
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
			Throw(m_cmd_alloc[0]->Reset());
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
				Throw(res);
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
		Throw(m_cmd_alloc->Reset());

		// Reset the command list, use empty pipeline state for now since there are no shaders and we are just clearing the screen.
		Throw(m_cmd_list->Reset(m_cmd_alloc.get(), m_pipeline_state.get()));

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
		Throw(m_cmd_list->Close());

		// Execute the list of commands.
		auto cmd_lists = {static_cast<ID3D12CommandList*>(m_cmd_list.get())};
		lock.CmdQueue()->ExecuteCommandLists(1, cmd_lists.begin());

		// We then call the swap chain to present the rendered frame to the screen.

		// Finally present the back buffer to the screen since rendering is complete.
		Throw(m_swap_chain->Present(m_vsync, 0));

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