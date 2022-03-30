//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/main/window.h"
#include "pr/view3d-12/main/settings.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12
{
	// Constructor
	Window::Window(Renderer& rdr, WndSettings const& settings)
		:m_rdr(&rdr)
		,m_hwnd(settings.m_hwnd)
		,m_db_format(settings.m_depth_format)
		,m_multisamp()//!AllSet(rdr.Settings().m_device_layers, D3D11_CREATE_DEVICE_DEBUG) ? settings.m_multisamp : MultiSamp()) // Disable multi-sampling if debug is enabled
		,m_swap_chain_flags(settings.m_swap_chain_flags)
		,m_vsync(settings.m_vsync)
		,m_swap_chain_dbg()
		,m_swap_chain()
		,m_main_rt()
		,m_cmd_alloc()
		,m_cmd_list()
		//,m_main_srv()
		//,m_main_dsv()
		,m_d2d_dc()
		//,m_query()
		//,m_main_rt()
		,m_bb_count(settings.m_buffer_count)
		,m_bb_index(0)
		,m_idle(false)
		,m_name(settings.m_name)
		//,m_dbg_area()
	{
		try
		{
			Renderer::Lock lock(rdr);
			auto device = lock.D3DDevice();

			// Validate settings
			if (settings.m_buffer_count > _countof(m_main_rt))
				Throw(false, "Unsupported swap chain length");
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && !AllSet(m_rdr->Settings().m_options, ERdrOptions::BGRASupport))
				Throw(false, "D3D device has not been created with GDI compatibility");
			if (AllSet(m_swap_chain_flags, DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE) && settings.m_multisamp.Count != 1)
				Throw(false, "GDI compatibility does not support multi-sampling");
			if (settings.m_vsync && (settings.m_mode.RefreshRate.Numerator == 0 || settings.m_mode.RefreshRate.Denominator == 0))
				Throw(false, "If VSync is enabled, the refresh rate should be provided (matching the value returned from the graphics card)");
			//todo: w-buffer
			// https://docs.microsoft.com/en-us/windows-hardware/drivers/display/w-buffering
			// https://www.mvps.org/directx/articles/using_w-buffers.htm
		
			// Check feature support
			m_multisamp.Validate(device, settings.m_mode.Format);
			m_multisamp.Validate(device, settings.m_depth_format);
		
			// Get the factory that was used to create 'rdr.m_device'
			D3DPtr<IDXGIFactory4> factory;
			Throw(lock.Adapter()->GetParent(__uuidof(IDXGIFactory4), (void **)&factory.m_ptr));
		
			// Create a query interface for querying the GPU events related to this scene
			//D3D12_QUERY_DESC query_desc;
			//query_desc.Query = D3D12_QUERY_EVENT;
			//query_desc.MiscFlags = static_cast<D3D12_QUERY_MISC_FLAG>(0);
			//Throw(device->CreateQuery(&query_desc, &m_query.m_ptr));
		
			// Creating a window with hwnd == nullptr is allowed if you only want to render to 
			// off-screen render targets. If there's no window handle, don't create a swap chain
			if (settings.m_hwnd != nullptr)
			{
				// Uses the flag 'DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE' to enable an application to
				// render using GDI on a swap chain or a surface. This will allow the application
				// to call IDXGISurface1::GetDC on the 0th back buffer or a surface.
				DXGI_SWAP_CHAIN_DESC1 desc0 = {};
				desc0.Width = settings.m_mode.Width;
				desc0.Height = settings.m_mode.Height;
				desc0.Format = settings.m_mode.Format;
				desc0.Stereo = false; // interesting....
				desc0.SampleDesc = m_multisamp;
				desc0.BufferUsage = settings.m_usage;
				desc0.BufferCount = settings.m_buffer_count;
				desc0.Scaling = settings.m_scaling;
				desc0.SwapEffect = settings.m_swap_effect;
				desc0.AlphaMode = settings.m_alpha_mode;
				desc0.Flags = settings.m_swap_chain_flags;
				DXGI_SWAP_CHAIN_FULLSCREEN_DESC desc1 = {};
				desc1.Windowed = settings.m_windowed;
				desc1.RefreshRate = settings.m_mode.RefreshRate;
				desc1.Scaling = settings.m_mode.Scaling;
				desc1.ScanlineOrdering = settings.m_mode.ScanlineOrdering;

				D3DPtr<IDXGISwapChain1> swap_chain;
				Throw(factory->CreateSwapChainForHwnd(lock.CmdQueue(), settings.m_hwnd, &desc0, &desc1, nullptr, &swap_chain.m_ptr));
				Throw(swap_chain->QueryInterface(&m_swap_chain.m_ptr));
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
			if (AllSet(rdr.Settings().m_options, ERdrOptions::DeviceDebug))
			{
				//DXGI_SWAP_CHAIN_DESC sd = {};
				//sd.BufferCount  = 1;
				//sd.BufferDesc   = DisplayMode(16,16);
				//sd.SampleDesc   = MultiSamp();
				//sd.BufferUsage  = DXGI_USAGE_RENDER_TARGET_OUTPUT;
				//sd.OutputWindow = rdr.DummyHwnd();
				//sd.Windowed     = TRUE;
				//sd.SwapEffect   = DXGI_SWAP_EFFECT_FLIP_DISCARD;
				//sd.Flags        = s_cast<DXGI_SWAP_CHAIN_FLAG>(0);
				//Throw(factory->CreateSwapChain(device, &sd, &m_swap_chain_dbg.m_ptr));
				//PR_EXPAND(PR_DBG_RDR, NameResource(m_swap_chain_dbg.get(), FmtS("swap chain dbg")));
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
		// Release COM pointers
		m_event_fence.close();
		m_fence = nullptr;
		m_pipeline_state = nullptr;
		m_cmd_list = nullptr;
		m_cmd_alloc = nullptr;
		m_rtv_heap = nullptr;
		for (auto& rt : m_main_rt)
			rt = nullptr;

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
	ModelManager& Window::mdl_mgr() const
	{
		return rdr().mdl_mgr();
	}
	ShaderManager& Window::shdr_mgr() const
	{
		return rdr().shdr_mgr();
	}
	TextureManager& Window::tex_mgr() const
	{
		return rdr().tex_mgr();
	}
	BlendStateManager& Window::bs_mgr() const
	{
		return rdr().bs_mgr();
	}
	DepthStateManager& Window::ds_mgr() const
	{
		return rdr().ds_mgr();
	}
	RasterStateManager& Window::rs_mgr() const
	{
		return rdr().rs_mgr();
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

	// Create a render target from the swap-chain
	void Window::InitRT()
	{
		// If the renderer has been created without a window handle, there will be no swap chain.
		// In this case the caller will be setting up a render target to an off-screen buffer.
		if (m_swap_chain == nullptr)
			return;

		Renderer::Lock lock(*m_rdr);
		auto device = lock.D3DDevice();
	
		// Create the render target view heap for the back buffers.
		D3D12_DESCRIPTOR_HEAP_DESC rtv_heap_desc = {};
		rtv_heap_desc.NumDescriptors = m_bb_count;
		rtv_heap_desc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
		rtv_heap_desc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
		Throw(device->CreateDescriptorHeap(&rtv_heap_desc, __uuidof(ID3D12DescriptorHeap), (void**)&m_rtv_heap.m_ptr));
	
		// Get a handle to the starting memory location in the render target view heap to identify
		// where the render target views will be located for the two back buffers.
		// Get the size of the memory location for the render target view descriptors.
		D3D12_CPU_DESCRIPTOR_HANDLE rtv_handle = m_rtv_heap->GetCPUDescriptorHandleForHeapStart();

		// Create render target views for all back buffers
		for (int i = 0; i != m_bb_count; ++i)
		{
			// Get a pointer to the first back buffer from the swap chain.
			Throw(m_swap_chain->GetBuffer(s_cast<UINT>(i), __uuidof(ID3D12Resource), (void**)&m_main_rt[i].m_ptr));

			// Create a render target view for the back buffer.
			device->CreateRenderTargetView(m_main_rt[i].get(), nullptr, rtv_handle);

			// Increment the view handle to the next descriptor location in the render target view heap.
			rtv_handle.ptr += device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
		}

		// Get the index of the current back buffer to draw to
		m_bb_index = m_swap_chain->GetCurrentBackBufferIndex();

		// Create a command allocator.
		Throw(device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, __uuidof(ID3D12CommandAllocator), (void**)&m_cmd_alloc.m_ptr));

		// Create a basic command list for the window.
		// Command lists are one of key the components to understand in DirectX 12. Basically you fill the command list with all your rendering commands each frame and
		// then send it into the command queue to execute the command list. And when you get more advanced you will create multiple command lists and execute them in
		// parallel to get more efficiency in rendering. However that is where it gets tricky as you need to manage resources like you would in any multi-threaded program
		// and ensure the execution order and dependencies between threads is safely handled.
		Throw(device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, m_cmd_alloc.get(), nullptr, __uuidof(ID3D12GraphicsCommandList), (void**)&m_cmd_list.m_ptr));
		Throw(m_cmd_list->Close()); // Initially we need to close the command list during initialization as it is created in a recording state.

		// Create a fence for GPU synchronization.
		// The fence as a signalling mechanism to notify when the GPU is completely done rendering the command list that we submitted via the command queue.
		// GPU and CPU synchronization is completely up to us to handle in DirectX 12, so fences become a very necessary tool.
		Throw(device->CreateFence(0, D3D12_FENCE_FLAG_NONE, __uuidof(ID3D12Fence), (void**)&m_fence.m_ptr));

		// Create an event object for the fence.
		m_event_fence = CreateEventExW(nullptr, nullptr, 0, EVENT_ALL_ACCESS);
		Throw(m_event_fence != nullptr, "Creating an event for the thread fence failed");
		//auto m_fence_value = 1; // Initialize the starting fence value. 



		//// Get the back buffer so we can copy its properties
		//D3DPtr<ID3D11Texture2D> back_buffer;
		//Throw(m_swap_chain->GetBuffer(0, __uuidof(ID3D11Texture2D), (void**)&back_buffer.m_ptr));
		//PR_EXPAND(PR_DBG_RDR, NameResource(back_buffer.get(), "main RT"));
		//	
		//// Read the texture properties from the BB
		//Texture2DDesc bbdesc;
		//back_buffer->GetDesc(&bbdesc);
		//static_cast<DXGI_SAMPLE_DESC&>(m_multisamp) = bbdesc.SampleDesc;

		//// Create a render-target view of the back buffer
		//Throw(device->CreateRenderTargetView(back_buffer.m_ptr, nullptr, &m_main_rtv.m_ptr));

		//// If the texture was created with SRV binding, create a SRV
		//if (bbdesc.BindFlags & D3D11_BIND_SHADER_RESOURCE)
		//	Throw(device->CreateShaderResourceView(back_buffer.m_ptr, nullptr, &m_main_srv.m_ptr));

		//// Get the render target as a texture
		//m_main_rt = tex_mgr().CreateTexture2D(AutoId, back_buffer.get(), m_main_srv.get(), SamplerDesc::LinearClamp(), false, "main_rt");

		// Create a texture buffer that we will use as the depth buffer
		//Texture2DDesc desc;
		//desc.Width              = bbdesc.Width;
		//desc.Height             = bbdesc.Height;
		//desc.MipLevels          = 1;
		//desc.ArraySize          = 1;
		//desc.Format             = m_db_format;
		//desc.SampleDesc         = bbdesc.SampleDesc;
		//desc.Usage              = D3D11_USAGE_DEFAULT;
		//desc.BindFlags          = D3D11_BIND_DEPTH_STENCIL;
		//desc.CPUAccessFlags     = 0;
		//desc.MiscFlags          = 0;
		//D3DPtr<ID3D11Texture2D> depth_stencil;
		//Throw(device->CreateTexture2D(&desc, 0, &depth_stencil.m_ptr));
		//PR_EXPAND(PR_DBG_RDR, NameResource(depth_stencil.get(), "main DB"));
		//
		//// Create a depth/stencil view of the texture buffer we just created
		//D3D11_DEPTH_STENCIL_VIEW_DESC dsv_desc = {};
		//dsv_desc.Format             = desc.Format;
		//dsv_desc.ViewDimension      = bbdesc.SampleDesc.Count == 1 ? D3D11_DSV_DIMENSION_TEXTURE2D : D3D11_DSV_DIMENSION_TEXTURE2DMS;
		//dsv_desc.Texture2D.MipSlice = 0;
		//Throw(device->CreateDepthStencilView(depth_stencil.m_ptr, &dsv_desc, &m_main_dsv.m_ptr));
		
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
		
		//// Bind the main render target and depth buffer to the OM
		//RestoreRT();
	}

	// Binds the main render target and depth buffer to the OM
	void Window::RestoreRT()
	{
		//SetRT(m_main_rtv.get(), m_main_dsv.get(), false);
	}

	//
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
		float bgra[4] = {0.8f, 0.2f, 0.3f, 1.0f};
		m_cmd_list->ClearRenderTargetView(rtv_handle, bgra, 0, nullptr);
	
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

		// Signal and increment the fence value.
		Throw(lock.CmdQueue()->Signal(m_fence.get(), ++m_issue));

		// Wait until the GPU is done rendering.
		if (m_fence->GetCompletedValue() < m_issue)
		{
			Throw(m_fence->SetEventOnCompletion(m_issue, m_event_fence));
			WaitForSingleObject(m_event_fence, INFINITE);
		}

		// For the next frame swap to the other back buffer using the alternating index.

		// Alternate the back buffer index back and forth between 0 and 1 each frame.
		m_bb_index = (m_bb_count + 1) % m_bb_count;
	}



	// Returns the size of the current render target
	iv2 Window::RenderTargetSize() const
	{
		auto rt = m_main_rt[m_bb_index].get();
		auto desc = rt->GetDesc();
		return iv2(s_cast<int>(desc.Width), s_cast<int>(desc.Height));
	}
}

