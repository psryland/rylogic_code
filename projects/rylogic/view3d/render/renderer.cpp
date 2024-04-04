//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/textures/texture_2d.h"
#include "pr/view3d/util/event_args.h"

#pragma comment(lib, "d3d11.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "dxgi.lib")
#pragma comment(lib, "d2d1.lib")
#pragma comment(lib, "dwrite.lib")

#if PR_LOGGING
#pragma message("PR_LOGGING enabled")
#endif

namespace pr::rdr
{
	// Useful reading:
	//   http://msdn.microsoft.com/en-us/library/windows/desktop/bb205075(v=vs.85).aspx

	// Registered windows message for BeginInvoke
	wchar_t const* BeginInvokeWndClassName = L"pr::rdr::BeginInvoke";

	// WndProc for the dummy window used to implement BeginInvoke functionality
	static LRESULT __stdcall BeginInvokeWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
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

	// Initialise the renderer state variables and creates the DX device and swap chain.
	RdrState::RdrState(RdrSettings const& settings)
		:m_settings(settings)
		,m_feature_level()
		,m_d3d_device()
		,m_immediate()
		,m_d2dfactory()
		,m_dwrite()
		,m_d2d_device()
	{
		// Check for incompatible build settings
		RdrSettings::BuildOptions bo;
		pr::CheckBuildOptions(bo, settings.m_build_options);
 
		// Add the debug layer in debug mode
		// Note: this automatically disables multi-sampling as well
		//PR_EXPAND(PR_DBG_RDR, m_settings.m_device_layers |= D3D11_CREATE_DEVICE_DEBUG);
		//#pragma message(PR_LINK "WARNING: ************************************************** D3D11_CREATE_DEVICE_DEBUG enabled")
		PR_INFO_IF(PR_DBG_RDR, AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_DEBUG       ), "D3D11_CREATE_DEVICE_DEBUG is enabled");
		PR_INFO_IF(PR_DBG_RDR, AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_BGRA_SUPPORT), "D3D11_CREATE_DEVICE_BGRA_SUPPORT is enabled");

		// Create the device interface
		D3DPtr<ID3D11DeviceContext> immediate;
		auto hr = D3D11CreateDevice(
			m_settings.m_adapter.m_ptr,
			m_settings.m_driver_type,
			0,
			m_settings.m_device_layers,
			m_settings.m_feature_levels.empty() ? nullptr : &m_settings.m_feature_levels[0],
			static_cast<UINT>(m_settings.m_feature_levels.size()),
			D3D11_SDK_VERSION,
			&m_d3d_device.m_ptr,
			&m_feature_level,
			&immediate.m_ptr);

		// If the device type is unsupported, fall-back to a software device
		if (hr == DXGI_ERROR_UNSUPPORTED && m_settings.m_fallback_to_sw_device)
		{
			hr = D3D11CreateDevice(
				m_settings.m_adapter.m_ptr,
				D3D_DRIVER_TYPE_SOFTWARE,
				0,
				m_settings.m_device_layers,
				m_settings.m_feature_levels.empty() ? nullptr : &m_settings.m_feature_levels[0],
				static_cast<UINT>(m_settings.m_feature_levels.size()),
				D3D11_SDK_VERSION,
				&m_d3d_device.m_ptr,
				&m_feature_level,
				&immediate.m_ptr);
		}
		Check(hr);
		Check(immediate->QueryInterface(__uuidof(ID3D11DeviceContext1), (void**)&m_immediate.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(m_d3d_device.get(), "D3D device"));
		PR_EXPAND(PR_DBG_RDR, NameResource(immediate.get(), "immediate DC"));

		// Check dlls,DX features,etc required to run the renderer are available.
		// Check the given settings are valid for the current adaptor.
		{
			if (m_feature_level < D3D_FEATURE_LEVEL_10_0)
				throw std::exception("Graphics hardware does not meet the required feature level.\r\nFeature level 10.0 required\r\n\r\n(e.g. Shader Model 4.0, non power-of-two texture sizes)");
			
			D3D11_FEATURE_DATA_D3D10_X_HARDWARE_OPTIONS opts;
			Check(m_d3d_device->CheckFeatureSupport(D3D11_FEATURE_D3D10_X_HARDWARE_OPTIONS, &opts, sizeof(opts)));
			if (opts.ComputeShaders_Plus_RawAndStructuredBuffers_Via_Shader_4_x == FALSE)
				throw std::exception("DirectX device does not support Compute Shaders 4x");
		}

		// Create the direct2d factory
		D2D1_FACTORY_OPTIONS d2dfactory_options;
		d2dfactory_options.debugLevel = AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_DEBUG) ? D2D1_DEBUG_LEVEL_INFORMATION  : D2D1_DEBUG_LEVEL_NONE;
		Check(D2D1CreateFactory(D2D1_FACTORY_TYPE_MULTI_THREADED, __uuidof(ID2D1Factory1), &d2dfactory_options, (void**)&m_d2dfactory.m_ptr));

		// Create the direct write factory
		Check(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), (IUnknown**)&m_dwrite.m_ptr));

		// Creating a D2D device for drawing 2D to the back buffer requires 'D3D11_CREATE_DEVICE_BGRA_SUPPORT'
		if (AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_BGRA_SUPPORT))
		{
			// Get the DXGI Device from the d3d device
			D3DPtr<IDXGIDevice> dxgi_device;
			Check(m_d3d_device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));

			// Create a D2D device
			Check(m_d2dfactory->CreateDevice(dxgi_device.get(), &m_d2d_device.m_ptr));
		}
	}

	// Renderer state destruction
	RdrState::~RdrState()
	{
		PR_EXPAND(PR_DBG_RDR, int rcnt);

		if (m_d2d_device != nullptr)
		{
			PR_ASSERT(PR_DBG_RDR, (rcnt = m_d2d_device.RefCount()) == 1, "Outstanding references to the d2d device");
			m_d2d_device = nullptr;
		}
		if (m_immediate != nullptr)
		{
			PR_ASSERT(PR_DBG_RDR, (rcnt = m_immediate.RefCount()) == 1, "Outstanding references to the immediate device context");
			m_immediate->OMSetRenderTargets(0, 0, 0);
			m_immediate = nullptr;
		}
		m_d2dfactory = nullptr;
		m_dwrite = nullptr;

		if (m_d3d_device != nullptr)
		{
			#ifdef PR_DBG_RDR
			if ((m_settings.m_device_layers & D3D11_CREATE_DEVICE_DEBUG) != 0)
			{
				// Note: this will report that the D3D device is still live
				D3DPtr<ID3D11Debug> dbg;
				Check(m_d3d_device->QueryInterface(__uuidof(ID3D11Debug), (void**)&dbg.m_ptr));
				Check(dbg->ReportLiveDeviceObjects(D3D11_RLDO_DETAIL|D3D11_RLDO_IGNORE_INTERNAL));
			}
			#endif

			PR_ASSERT(PR_DBG_RDR, (rcnt = m_d3d_device.RefCount()) == 1, "Outstanding references to the dx device");
			m_d3d_device = nullptr;
		}
	}

	// Construct the renderer
	Renderer::Renderer(RdrSettings const& settings)
		:RdrState(settings)
		,m_main_thread_id(GetCurrentThreadId())
		,m_d3d_mutex()
		,m_mutex_task_queue()
		,m_task_queue()
		,m_last_task(false)
		,m_poll_callbacks()
		,m_dummy_hwnd()
		,m_id32_src()
		,m_bs_mgr(This())
		,m_ds_mgr(This())
		,m_rs_mgr(This())
		,m_tex_mgr(This())
		,m_shdr_mgr(This())
		,m_mdl_mgr(This())
	{
		try
		{
			// Register a window class for the dummy window
			WNDCLASSEXW wc = {sizeof(WNDCLASSEXW)};
			wc.style         = 0;
			wc.cbClsExtra    = 0;
			wc.cbWndExtra    = 0;
			wc.hInstance     = m_settings.m_instance;
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
			if (m_dummy_hwnd == nullptr)
				throw std::exception(pr::HrMsg(GetLastError()).c_str());
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
		::UnregisterClassW(BeginInvokeWndClassName, m_settings.m_instance);
	}

	// Execute any pending tasks in the task queue
	void Renderer::RunTasks()
	{
		if (GetCurrentThreadId() != m_main_thread_id)
			throw std::runtime_error("RunTasks must be called from the main thread");

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
		if (GetCurrentThreadId() != m_main_thread_id)
			throw std::runtime_error("LastTask must be called from the main thread");

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

	// Call all registered poll event callbacks
	void Renderer::Poll()
	{
		for (auto& cb : m_poll_callbacks)
			cb();

		// Keep polling while 'm_poll_callbacks' is not empty
		if (!m_poll_callbacks.empty())
			::SetTimer(m_dummy_hwnd, UINT_PTR(this), 0, nullptr);
	}
}