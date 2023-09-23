//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/main/config.h"
#include "pr/view3d-12/texture/texture_base.h"
#include "pr/view3d-12/texture/texture_2d.h"

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "d3d12.lib")
#pragma comment(lib, "d3dcompiler.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "dxgi.lib")
#pragma comment(lib, "d2d1.lib")
#pragma comment(lib, "dwrite.lib")

namespace pr::rdr12
{
	// Useful reading: http://msdn.microsoft.com/en-us/library/windows/desktop/bb205075(v=vs.85).aspx

	// Registered windows message for BeginInvoke
	static wchar_t const* BeginInvokeWndClassName = L"pr::rdr::BeginInvoke";

	// WndProc for the dummy window used to implement BeginInvoke functionality
	LRESULT __stdcall BeginInvokeWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
	{
		switch (message)
		{
			case WM_BeginInvoke:
			{
				auto& rdr = *reinterpret_cast<Renderer*>(wparam);
				rdr.RunTasks();
				break;
			}
			case WM_TIMER:
			{
				auto& rdr = *reinterpret_cast<Renderer*>(wparam);
				rdr.Poll();
				break;
			}
		}
		return DefWindowProcW(hwnd, message, wparam, lparam);
	}

	// Create Dx interface pointers
	Renderer::RdrState::RdrState(RdrSettings const& settings)
		:m_settings(settings)
		,m_features()
		,m_d3d_device()
		,m_gfx_queue()
		,m_com_queue()
		,m_cpy_queue()
		,m_dx11_device()
		,m_dx11_dc()
		,m_d2dfactory()
		,m_d2d_device()
		,m_main_thread_id(GetCurrentThreadId())
	{
		try
		{
			// Check for incompatible build settings
			RdrSettings::BuildOptions bo;
			pr::CheckBuildOptions(bo, settings.m_build_options);

			// Find the first adapter that supports Dx12
			if (m_settings.m_adapter.ptr == nullptr)
				throw std::runtime_error("No DirectX Adapter found that supports the requested feature level");

			// Add the debug layer in debug mode. Note: this automatically disables multi-sampling as well.
			//m_settings.m_options = SetBits(m_settings.m_options, ERdrOptions::DeviceDebug, true);
			//#pragma message(PR_LINK "WARNING: ************************************************** DeviceDebug enabled")
			PR_INFO_IF(PR_DBG_RDR, AllSet(m_settings.m_options, ERdrOptions::DeviceDebug), "DeviceDebug is enabled");
			PR_INFO_IF(PR_DBG_RDR, AllSet(m_settings.m_options, ERdrOptions::BGRASupport), "BGRASupport is enabled");

			// Enable the debug layer. Must be done before creating the d3d12 device.
			if (AllSet(m_settings.m_options, ERdrOptions::DeviceDebug))
			{
				D3DPtr<ID3D12Debug> dbg;
				Throw(D3D12GetDebugInterface(__uuidof(ID3D12Debug), (void**)&dbg.m_ptr));
				dbg->EnableDebugLayer();

				//D3DPtr<ID3D12Debug1> dbg1;
				//Throw(dbg->QueryInterface<ID3D12Debug1>(&dbg1.m_ptr));
				//dbg1->SetEnableGPUBasedValidation(true);
			}

			// Create the d3d device
			D3DPtr<ID3D12Device> device;
			Throw(D3D12CreateDevice(
				m_settings.m_adapter.ptr.get(),
				m_settings.m_feature_level,
				__uuidof(ID3D12Device),
				(void**)&device.m_ptr));
			Throw(device->QueryInterface<ID3D12Device4>(&m_d3d_device.m_ptr));

			// More debugging now the device exists
			if (AllSet(m_settings.m_options, ERdrOptions::DeviceDebug))
			{
				D3DPtr<ID3D12InfoQueue> info;
				Throw(device->QueryInterface<ID3D12InfoQueue>(&info.m_ptr));
				Throw(info->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, TRUE));
				Throw(info->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, TRUE));
				Throw(info->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_WARNING, TRUE));
		
				//// These are work arounds for issues with integrated graphics chips
				//DXGI_INFO_QUEUE_MESSAGE_ID hide[] =
				//{
				//	80 /* IDXGISwapChain::GetContainingOutput: The swapchain's adapter does not control the output on which the swapchain's window resides. */,
				//};
				//DXGI_INFO_QUEUE_FILTER filter = {};
				//filter.DenyList.NumIDs = static_cast<UINT>(std::size(hide));
				//filter.DenyList.pIDList = hide;
				//info->AddStorageFilterEntries(DXGI_DEBUG_DXGI, &filter);

				//D3D12_MESSAGE_ID hide[] =
				//{
				//	D3D12_MESSAGE_ID_MAP_INVALID_NULLRANGE,
				//	D3D12_MESSAGE_ID_UNMAP_INVALID_NULLRANGE,
				//	// Workarounds for debug layer issues on hybrid-graphics systems
				//	D3D12_MESSAGE_ID_EXECUTECOMMANDLISTS_WRONGSWAPCHAINBUFFERREFERENCE,
				//	D3D12_MESSAGE_ID_RESOURCE_BARRIER_MISMATCHING_COMMAND_LIST_TYPE,
				//};
				//D3D12_INFO_QUEUE_FILTER filter = {};
				//filter.DenyList.NumIDs = _countof(hide);
				//filter.DenyList.pIDList = hide;
				//Throw(info->AddStorageFilterEntries(&filter));
			}

			// Read the supported features
			m_features.Read(m_d3d_device.get());

			// Create the command queues
			D3D12_COMMAND_QUEUE_DESC cmd_queue = {
				.Type = D3D12_COMMAND_LIST_TYPE_DIRECT,
				.Priority = D3D12_COMMAND_QUEUE_PRIORITY_NORMAL,
				.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE,
				.NodeMask = 0,
			};
			cmd_queue.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;
			Throw(m_d3d_device->CreateCommandQueue(&cmd_queue, __uuidof(ID3D12CommandQueue), (void**)&m_gfx_queue.m_ptr));
			cmd_queue.Type = D3D12_COMMAND_LIST_TYPE_COMPUTE;
			Throw(m_d3d_device->CreateCommandQueue(&cmd_queue, __uuidof(ID3D12CommandQueue), (void**)&m_com_queue.m_ptr));
			cmd_queue.Type = D3D12_COMMAND_LIST_TYPE_COPY;
			Throw(m_d3d_device->CreateCommandQueue(&cmd_queue, __uuidof(ID3D12CommandQueue), (void**)&m_cpy_queue.m_ptr));

			// Check dlls,DX features,etc required to run the renderer are available.
			// Check the given settings are valid for the current adaptor.
			if (m_settings.m_feature_level < D3D_FEATURE_LEVEL_10_0)
				throw std::runtime_error("Graphics hardware does not meet the required feature level.\r\nFeature level 10.0 required\r\n\r\n(e.g. Shader Model 4.0, non power-of-two texture sizes)");
			
			// Check feature support
			if (m_features.ShaderModel.HighestShaderModel < D3D_SHADER_MODEL_5_1)
				throw std::runtime_error("DirectX device does not support Compute Shaders 4x");
			if (m_features.Options5.RaytracingTier != D3D12_RAYTRACING_TIER_NOT_SUPPORTED)
				PR_ASSERT(PR_DBG_RDR, true, "Sweet!");

			// Create the D3D11-on-12 device so that D2D can draw on Dx12 resources
			D3DPtr<ID3D11Device> dx11_device;
			IUnknown* gfx_queue = m_gfx_queue.get();
			UINT dx11_device_flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT | (AllSet(m_settings.m_options, ERdrOptions::DeviceDebug) ? D3D11_CREATE_DEVICE_DEBUG : 0);
			Throw(D3D11On12CreateDevice(m_d3d_device.get(), dx11_device_flags, nullptr, 0, &gfx_queue, 1, 0, &dx11_device.m_ptr, &m_dx11_dc.m_ptr, nullptr));
			Throw(dx11_device->QueryInterface<ID3D11On12Device>(&m_dx11_device.m_ptr));

			// Create the direct2d factory
			D2D1_FACTORY_OPTIONS d2dfactory_options = {};
			d2dfactory_options.debugLevel = AllSet(m_settings.m_options, ERdrOptions::D2D1_DebugInfo) ? D2D1_DEBUG_LEVEL_INFORMATION  : D2D1_DEBUG_LEVEL_NONE;
			Throw(D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, __uuidof(ID2D1Factory2), &d2dfactory_options, (void**)&m_d2dfactory.m_ptr));

			// Create a D2D device
			D3DPtr<IDXGIDevice> dxgi_device;
			Throw(m_dx11_device->QueryInterface<IDXGIDevice>(&dxgi_device.m_ptr));
			Throw(m_d2dfactory->CreateDevice(dxgi_device.get(), &m_d2d_device.m_ptr));
		}
		catch (...)
		{
			this->~RdrState();
			throw;
		}
	}
	Renderer::RdrState::~RdrState()
	{
		// Release COM pointers
		m_cpy_queue = nullptr;
		m_com_queue = nullptr;
		m_gfx_queue = nullptr;
		m_d2dfactory = nullptr;
		m_dx11_dc = nullptr;
		m_dx11_device = nullptr;

		// Do reference count checking
		if (m_d2d_device != nullptr)
		{
			PR_EXPAND(PR_DBG_RDR, auto rcnt = m_d2d_device.RefCount());
			PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the d2d device");
			m_d2d_device = nullptr;
		}
		if (m_d3d_device != nullptr)
		{
			if (AllSet(m_settings.m_options, ERdrOptions::DeviceDebug))
			{
				//// Note: this will report that the D3D device is still live
				//D3DPtr<ID3D12Debug> dbg;
				//Throw(m_d3d_device->QueryInterface(__uuidof(ID3D12Debug), (void**)&dbg.m_ptr));
				//Throw(dbg->ReportLiveDeviceObjects(D3D11_RLDO_DETAIL|D3D11_RLDO_IGNORE_INTERNAL));
			}
			PR_EXPAND(PR_DBG_RDR, auto rcnt = m_d3d_device.RefCount());
			PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the dx device");
			m_d3d_device = nullptr;
		}
	}

	// Constructor
	Renderer::Renderer(RdrSettings const& settings)
		:m_state(settings)
		,m_d3d_mutex()
		,m_mutex_task_queue()
		,m_task_queue()
		,m_last_task()
		,m_poll_callbacks()
		,m_dummy_hwnd()
		,m_id32_src()
		,m_res_mgr(rdr())
	{
		try
		{
			// Register a window class for the dummy window
			WNDCLASSEXW wc = {sizeof(WNDCLASSEXW)};
			wc.style         = 0;
			wc.cbClsExtra    = 0;
			wc.cbWndExtra    = 0;
			wc.hInstance     = m_state.m_settings.m_instance;
			wc.hIcon         = nullptr;
			wc.hIconSm       = nullptr;
			wc.hCursor       = nullptr;
			wc.hbrBackground = nullptr;
			wc.lpszMenuName  = nullptr;
			wc.lpfnWndProc   = &BeginInvokeWndProc;
			wc.lpszClassName = BeginInvokeWndClassName;
			auto atom = ATOM(::RegisterClassExW(&wc));
			if (atom == 0)
				throw std::exception(pr::HrMsg(GetLastError()).c_str());

			// Create a dummy window for BeginInvoke functionality
			m_dummy_hwnd = ::CreateWindowExW(0, (LPCWSTR)MAKEINTATOM(atom), L"", 0, 0, 0, 0, 0, HWND_MESSAGE, nullptr, nullptr, nullptr);
			Throw(m_dummy_hwnd != nullptr, HrMsg(GetLastError()).c_str());
		}
		catch (...)
		{
			this->~Renderer();
			throw;
		}
	}
	Renderer::~Renderer()
	{
		LastTask();

		// Release the dummy window
		if (m_dummy_hwnd != nullptr)
		{
			::DestroyWindow(m_dummy_hwnd);
			m_dummy_hwnd = nullptr;
		}

		// Un-register the dummy window class
		::UnregisterClassW(BeginInvokeWndClassName, m_state.m_settings.m_instance);
	}

	// Access the renderer manager classes
	Renderer& Renderer::rdr()
	{
		return *this;
	}
	ResourceManager const& Renderer::res_mgr() const
	{
		return m_res_mgr;
	}
	ResourceManager& Renderer::res_mgr()
	{
		return const_cast<ResourceManager&>(std::as_const(*this).res_mgr());
	}

	// Read access to the initialisation settings
	RdrSettings const& Renderer::Settings() const
	{
		return m_state.m_settings;
	}

	// Device supported features
	FeatureSupport const& Renderer::Features() const
	{
		return m_state.m_features;
	}

	// Return the associated HWND. Note: this is not associated with any particular window. 'Window' objects have an hwnd.
	HWND Renderer::DummyHwnd() const
	{
		return m_dummy_hwnd;
	}

	// Return the current desktop DPI (Fall back if window DPI not available)
	v2 Renderer::SystemDpi() const
	{
		// Notes:
		//  - Window's have their own version of this function which detects the DPI
		//    of the monitor they're on, and fall's back to the system DPI.
		//  - Don't cache the DPI value because it can change at any time.
		#if (WINVER >= 0x0605)
		auto dpi = (float)GetDpiForSystem();
		#else
		auto dpi = (float)96.0f;
		#endif
		return v2(dpi, dpi);
	}

	// Return info about the available video memory
	DXGI_QUERY_VIDEO_MEMORY_INFO Renderer::GPUMemoryInfo() const
	{
		D3DPtr<IDXGIAdapter3> adapter;
		Throw(m_state.m_settings.m_adapter.ptr->QueryInterface<IDXGIAdapter3>(&adapter.m_ptr));

		DXGI_QUERY_VIDEO_MEMORY_INFO info = {};
		Throw(adapter->QueryVideoMemoryInfo(0, DXGI_MEMORY_SEGMENT_GROUP_LOCAL, &info));
		return info;
	}

	// Generate a unique Id on each call
	int Renderer::NewId32()
	{
		return ++m_id32_src;
	}
	
	// Execute any pending tasks in the task queue
	void Renderer::RunTasks()
	{
		AssertMainThread();
		
		TaskQueue tasks;
		{
			std::lock_guard<std::mutex> lock(m_mutex_task_queue);
			std::swap(tasks, m_task_queue);
		}

		// Execute each task
		for (auto& task : tasks)
		{
			try { task.get(); }
			catch (std::exception const&)
			{
				// These tasks shouldn't throw exceptions because they won't be handled.
				assert(false && "Unhandled task error");
				throw;
			}
		}
	}

	// Call this during shutdown to flush the task queue and prevent any further tasks from being added.
	void Renderer::LastTask()
	{
		AssertMainThread();
		
		// Idempotent
		if (m_last_task)
			return;

		{// Block any further tasks being added
			std::lock_guard<std::mutex> lock(m_mutex_task_queue);
			m_last_task = true;
		}

		// Run tasks left in the queue
		RunTasks();
	}

	// Add/Remove a callback function that will be polled as fast as the windows message queue will allow
	void Renderer::AddPollCB(pr::StaticCB<void> cb)
	{
		AssertMainThread();
		m_poll_callbacks.push_back(cb);
		Poll();
	}
	void Renderer::RemovePollCB(pr::StaticCB<void> cb)
	{
		AssertMainThread();
		erase_stable(m_poll_callbacks, cb);
	}
	
	// Call all registered poll event callbacks
	void Renderer::Poll()
	{
		for (auto& cb : m_poll_callbacks)
			cb();

		// Keep polling while 'm_poll_callbacks' is not empty
		if (!m_poll_callbacks.empty())
			::SetTimer(m_dummy_hwnd, UINT_PTR(this), 0, nullptr);
	}

	// Check the current thread is the main thread
	bool Renderer::AssertMainThread() const
	{
		if (GetCurrentThreadId() == m_state.m_main_thread_id) return true;
		throw std::runtime_error("RunTasks must be called from the main thread");
	}
}

