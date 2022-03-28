//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
module;

#include "src/forward.h"
#pragma comment(lib, "d3d12.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "dxgi.lib")
#pragma comment(lib, "d2d1.lib")
#pragma comment(lib, "dwrite.lib")

export module View3d;

export import :Settings;
export import :Window;
export import :Scene;
export import :Config;
export import :Utility;
export import :Wrappers;
export import :Features;
export import :EventArgs;

namespace pr::rdr12
{
	// Render main object
	export struct Renderer
	{
	private:
		using TaskQueue = std::vector<std::future<void>>;
		using PollCBList = std::vector<pr::StaticCB<void>>;

		DWORD                      m_main_thread_id;
		Settings                   m_settings;
		D3DPtr<ID3D12Device>       m_d3d_device;
		D3DPtr<ID3D12CommandQueue> m_cmd_queue;
		D3DPtr<ID2D1Factory1>      m_d2dfactory;
		D3DPtr<IDWriteFactory>     m_dwrite;
		D3DPtr<ID2D1Device>        m_d2d_device;
		std::recursive_mutex       m_d3d_mutex;
		std::mutex                 m_mutex_task_queue;
		TaskQueue                  m_task_queue;
		bool                       m_last_task;
		PollCBList                 m_poll_callbacks;
		HWND                       m_dummy_hwnd;
		std::atomic_int            m_id32_src;

	public:

		explicit Renderer(Settings const& settings)
			:m_main_thread_id(GetCurrentThreadId())
			,m_settings(settings)
			,m_d3d_device()
			,m_cmd_queue()
			,m_d2dfactory()
			,m_dwrite()
			,m_d2d_device()
			,m_d3d_mutex()
			,m_mutex_task_queue()
			,m_task_queue()
			,m_last_task()
			,m_poll_callbacks()
			,m_dummy_hwnd()
			,m_id32_src()
		{
			try
			{
				// Check for incompatible build settings
				Settings::BuildOptions bo;
				pr::CheckBuildOptions(bo, settings.m_build_options);

				// Add the debug layer in debug mode
				// Note: this automatically disables multi-sampling as well
				//PR_EXPAND(PR_DBG_RDR, m_settings.m_device_layers |= D3D11_CREATE_DEVICE_DEBUG);
				//#pragma message(PR_LINK "WARNING: ************************************************** D3D11_CREATE_DEVICE_DEBUG enabled")
				//PR_INFO_IF(PR_DBG_RDR, AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_DEBUG       ), "D3D11_CREATE_DEVICE_DEBUG is enabled");
				//PR_INFO_IF(PR_DBG_RDR, AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_BGRA_SUPPORT), "D3D11_CREATE_DEVICE_BGRA_SUPPORT is enabled");

				// Find the first adapter that supports Dx12
				if (m_settings.m_adapter == nullptr)
					throw std::runtime_error("No DirectX Adapter found that supports the requested feature level");
		
				// Create the d3d device
				Throw(D3D12CreateDevice(
					m_settings.m_adapter.get(),
					m_settings.m_feature_level,
					__uuidof(ID3D12Device),
					(void**)&m_d3d_device.m_ptr));

				// Create the main command queue
				D3D12_COMMAND_QUEUE_DESC cmd_queue = {};
				cmd_queue.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;
				cmd_queue.Priority = D3D12_COMMAND_QUEUE_PRIORITY_NORMAL;
				cmd_queue.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
				cmd_queue.NodeMask = 0;
				Throw(m_d3d_device->CreateCommandQueue(&cmd_queue, __uuidof(ID3D12CommandQueue), (void**)&m_cmd_queue));

				// Credit: https://www.rastertek.com/dx12tut03.html
				// Before we can initialize the swap chain we have to get the refresh rate from the video card/monitor.
				// Each computer may be slightly different so we will need to query for that information. We query for
				// the numerator and denominator values and then pass them to DirectX during the setup and it will calculate
				// the proper refresh rate. If we don't do this and just set the refresh rate to a default value which may
				// not exist on all computers then DirectX will respond by performing a buffer copy instead of a buffer flip
				// which will degrade performance and give us annoying errors in the debug output.

				// Check dlls,DX features,etc required to run the renderer are available.
				// Check the given settings are valid for the current adaptor.
				if (m_settings.m_feature_level < D3D_FEATURE_LEVEL_10_0)
					throw std::runtime_error("Graphics hardware does not meet the required feature level.\r\nFeature level 10.0 required\r\n\r\n(e.g. Shader Model 4.0, non power-of-two texture sizes)");
			
				// Check feature support
				FeatureSupport features(m_d3d_device.get());
				if (features.ShaderModel.HighestShaderModel < D3D_SHADER_MODEL_5_1)
					throw std::runtime_error("DirectX device does not support Compute Shaders 4x");
				if (features.Options5.RaytracingTier != D3D12_RAYTRACING_TIER_NOT_SUPPORTED)
					PR_ASSERT(PR_DBG_RDR, true, "Sweet!");

				// Create the direct2d factory
				D2D1_FACTORY_OPTIONS d2dfactory_options;
				d2dfactory_options.debugLevel = AllSet(m_settings.m_debug_layers, EDebugLayer::D2D1_DebugInfo) ? D2D1_DEBUG_LEVEL_INFORMATION  : D2D1_DEBUG_LEVEL_NONE;
				Throw(D2D1CreateFactory(D2D1_FACTORY_TYPE_MULTI_THREADED, __uuidof(ID2D1Factory1), &d2dfactory_options, (void**)&m_d2dfactory.m_ptr));

				// Create the direct write factory
				Throw(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), (IUnknown**)&m_dwrite.m_ptr));

				// Creating a D2D device for drawing 2D to the back buffer requires 'D3D11_CREATE_DEVICE_BGRA_SUPPORT'
				//if (AllSet(m_settings.m_device_layers, D3D11_CREATE_DEVICE_BGRA_SUPPORT))
				//{
				//	// Get the DXGI Device from the d3d device
				//	D3DPtr<IDXGIDevice> dxgi_device;
				//	Throw(m_d3d_device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgi_device.m_ptr));
				//
				//	// Create a D2D device
				//	Throw(m_d2dfactory->CreateDevice(dxgi_device.get(), &m_d2d_device.m_ptr));
				//}

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
					throw std::runtime_error(pr::HrMsg(GetLastError()).c_str());
			}
			catch (...)
			{
				this->~Renderer();
				throw;
			}
		}
		~Renderer()
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

			// Do reference count checking
			if (m_d2d_device != nullptr)
			{
				PR_EXPAND(PR_DBG_RDR, auto rcnt = m_d2d_device.RefCount());
				PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the d2d device");
				m_d2d_device = nullptr;
			}
			if (m_cmd_queue != nullptr)
			{
				PR_EXPAND(PR_DBG_RDR, auto rcnt = m_cmd_queue.RefCount());
				PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the command queue");
			//	m_cmd_queue->OMSetRenderTargets(0, 0, 0);
				m_cmd_queue = nullptr;
			}
			m_d2dfactory = nullptr;
			m_dwrite = nullptr;

			if (m_d3d_device != nullptr)
			{
				//if ((m_settings.m_device_layers & D3D11_CREATE_DEVICE_DEBUG) != 0)
				//{
				//	// Note: this will report that the D3D device is still live
				//	D3DPtr<ID3D11Debug> dbg;
				//	pr::Throw(m_d3d_device->QueryInterface(__uuidof(ID3D11Debug), (void**)&dbg.m_ptr));
				//	pr::Throw(dbg->ReportLiveDeviceObjects(D3D11_RLDO_DETAIL|D3D11_RLDO_IGNORE_INTERNAL));
				//}
				PR_EXPAND(PR_DBG_RDR, auto rcnt = m_d3d_device.RefCount());
				PR_ASSERT(PR_DBG_RDR, rcnt == 1, "Outstanding references to the dx device");
				m_d3d_device = nullptr;
			}
		}

		// Return the associated HWND. Note: this is not associated with any particular window. 'Window' objects have an hwnd.
		HWND DummyHwnd() const
		{
			return m_dummy_hwnd;
		}

		// Return the current desktop DPI (Fall back if window DPI not available)
		v2 SystemDpi() const
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

		// Read access to the initialisation settings
		Settings const& Settings() const
		{
			return m_settings;
		}

		// Generate a unique Id on each call
		int NewId32()
		{
			return ++m_id32_src;
		}

		// Raised when a window resizes it's back buffer.
		// This is provided on the renderer so that other managers can receive
		// notification without having to sign up to every window that gets created.
		EventHandler<Window&, BackBufferSizeChangedEventArgs> BackBufferSizeChanged;

		// Run the given function on the Main/GUI thread
		// 'policy = std::launch::deferred' means the function is executed by the main thread during 'RunTasks'
		// 'policy = std::launch::async' means the function is run at any time in a worker thread. The result is collected in 'RunTasks'
		// 'policy' can be a bitwise OR of both deferred and async
		// WARNING: be careful with shutdown. Although functions are called on the main thread, they can still be called after
		// referenced data has been destructed.
		template <typename Func, typename... Args>
		void RunOnMainThread(std::launch policy, Func&& func, Args&&... args)
		{
			{
				std::lock_guard<std::mutex> lock(m_mutex_task_queue);
				if (m_last_task) return; // Don't add further tasks after 'LastTask' has been called.
				m_task_queue.emplace_back(std::async(policy, func, args...));
			}

			// Post a message to notify of the new task
			for (; !::PostMessageW(m_dummy_hwnd, WM_BeginInvoke, WPARAM(this), LPARAM());)
			{
				auto err = GetLastError();
				if (err == ERROR_NOT_ENOUGH_QUOTA)
				{
					// The message queue is full, just wait a bit. This is probably a deadlock though
					std::this_thread::yield();
					continue;
				}

				auto msg = pr::HrMsg(err);
				throw std::exception(msg.c_str());
			}
		}
		template <typename Func, typename... Args>
		void RunOnMainThread(Func&& func, Args&&... args)
		{
			static_assert(noexcept(func(args...)));
			RunOnMainThread(std::launch::deferred, func, args...);
		}

		// Execute any pending tasks in the task queue
		void RunTasks()
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
		void LastTask()
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

		// Add/Remove a callback function that will be polled as fast as the windows message queue will allow
		void AddPollCB(pr::StaticCB<void> cb)
		{
			if (GetCurrentThreadId() != m_main_thread_id)
				throw std::runtime_error("RunTasks must be called from the main thread");

			m_poll_callbacks.push_back(cb);
			Poll();
		}
		void RemovePollCB(pr::StaticCB<void> cb)
		{
			if (GetCurrentThreadId() != m_main_thread_id)
				throw std::runtime_error("RunTasks must be called from the main thread");

			erase_stable(m_poll_callbacks, cb);
		}

		// Call all registered poll event callbacks
		void Poll()
		{
			for (auto& cb : m_poll_callbacks)
				cb();

			// Keep polling while 'm_poll_callbacks' is not empty
			if (!m_poll_callbacks.empty())
				::SetTimer(m_dummy_hwnd, UINT_PTR(this), 0, nullptr);
		}

		// These manager classes form part of the public interface of the renderer
		// Declared last so that events are fully constructed first.
		// Note: model manager is declared last so that it is destructed first
		//BlendStateManager m_bs_mgr;
		//DepthStateManager m_ds_mgr;
		//RasterStateManager m_rs_mgr;
		//TextureManager m_tex_mgr;
		//ShaderManager m_shdr_mgr;
		//ModelManager m_mdl_mgr;

	private:

		// Useful reading:
		//   http://msdn.microsoft.com/en-us/library/windows/desktop/bb205075(v=vs.85).aspx

		// Registered windows message for BeginInvoke
		static constexpr UINT WM_BeginInvoke = WM_USER + 0x1976;

		// Registered windows message for BeginInvoke
		inline static wchar_t const* BeginInvokeWndClassName = L"pr::rdr::BeginInvoke";

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

		// For when I'm searching for the d3d device on this object
		enum { D3DDevice_IsAccessedViaTheRendererLock };
	};
}

