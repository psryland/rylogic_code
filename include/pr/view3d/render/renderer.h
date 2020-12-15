//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/config/config.h"
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/textures/texture_manager.h"
#include "pr/view3d/render/state_block.h"

namespace pr::rdr
{
	// Registered windows message for BeginInvoke
	static UINT const WM_BeginInvoke = WM_USER + 0x1976;

	// Settings for constructing the renderer
	struct RdrSettings
	{
		struct BuildOptions
		{
			StdBuildOptions   m_std;
			MathsBuildOptions m_maths;
			int RunTimeShaders;
			BuildOptions()
				:m_std()
				,m_maths()
				,RunTimeShaders(PR_RDR_RUNTIME_SHADERS)
			{}
		};

		HINSTANCE                     m_instance;              // Executable instance 
		BuildOptions                  m_build_options;         // The state of #defines. Used to check for incompatibilities
		D3DPtr<IDXGIAdapter>          m_adapter;               // The adapter to use. nullptr means use the default
		D3D_DRIVER_TYPE               m_driver_type;           // HAL, REF, etc
		UINT                          m_device_layers;         // Add layers over the basic device (see D3D11_CREATE_DEVICE_FLAG)
		pr::vector<D3D_FEATURE_LEVEL> m_feature_levels;        // Features to support. Empty implies 9.1 -> 11.1
		bool                          m_fallback_to_sw_device; // True to use a software device if 'm_driver_type' fails

		// Keep this inline so that m_build_options can be verified.
		RdrSettings(HINSTANCE inst, D3D11_CREATE_DEVICE_FLAG device_flags)
			:m_instance(inst)
			,m_build_options()
			,m_adapter()
			,m_driver_type(D3D_DRIVER_TYPE_HARDWARE)
			,m_device_layers(device_flags)
			,m_feature_levels()
			,m_fallback_to_sw_device(true)
		{}
	};

	// Renderer state variables
	struct RdrState
	{
		RdrSettings                  m_settings;
		D3D_FEATURE_LEVEL            m_feature_level;
		D3DPtr<ID3D11Device>         m_d3d_device;
		D3DPtr<ID3D11DeviceContext1> m_immediate;
		D3DPtr<ID2D1Factory1>        m_d2dfactory;
		D3DPtr<IDWriteFactory>       m_dwrite;
		D3DPtr<ID2D1Device>          m_d2d_device;

		RdrState(RdrSettings const& settings);
		~RdrState();
	};

	// The main renderer object
	class Renderer :RdrState
	{
		using TaskQueue = pr::vector<std::future<void>>;
		using PollCBList = pr::vector<pr::StaticCB<void>>;

		DWORD m_main_thread_id;
		std::recursive_mutex m_d3d_mutex;
		std::mutex m_mutex_task_queue;
		TaskQueue m_task_queue;
		bool m_last_task;
		PollCBList m_poll_callbacks;
		HWND m_dummy_hwnd;
		std::atomic_int m_id32_src;
		
		Renderer& This() { return *this; }

	public:

		explicit Renderer(RdrSettings const& settings);
		~Renderer();

		// Synchronise access to D3D/D2D interfaces
		class Lock
		{
			Renderer& m_rdr;
			std::lock_guard<std::recursive_mutex> m_lock;

		public:

			Lock(Renderer& rdr)
				:m_rdr(rdr)
				,m_lock(rdr.m_d3d_mutex)
			{}

			// Return the D3D device
			ID3D11Device* D3DDevice() const
			{
				return m_rdr.m_d3d_device.get();
			}

			// Return the immediate device context
			ID3D11DeviceContext1* ImmediateDC() const
			{
				return m_rdr.m_immediate.get();
			}

			// Create a new deferred device context
			ID3D11DeviceContext1* DeferredDC() const
			{
				throw std::exception("not supported");
			}

			// Return the D2D device
			ID2D1Device* D2DDevice() const
			{
				return m_rdr.m_d2d_device.get();
			}

			// Return the direct2d factory
			ID2D1Factory* D2DFactory() const
			{
				return m_rdr.m_d2dfactory.get();
			}

			// Return the direct write factory
			IDWriteFactory* DWrite() const
			{
				return m_rdr.m_dwrite.get();
			}
		};

		// Return the associated HWND. Note: this is not associated with any particular window. 'Window' objects have an hwnd.
		HWND DummyHwnd() const
		{
			return m_dummy_hwnd;
		}

		// Return the current desktop DPI
		v2 Dpi() const
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

		// Return the scaling factors to convert DIP to physical pixels
		v2 DpiScale() const
		{
			return Dpi() / 96.0f;
		}

		// Read access to the initialisation settings
		RdrSettings const& Settings() const
		{
			return m_settings;
		}

		// Generate a unique Id on each call
		int NewId32()
		{
			return ++m_id32_src;
		}

		// Raised when a window resizes it's back buffer.
		// This is provided on the renderer so that other managers can receive notification
		// without having to sign up to ever window that gets created.
		EventHandler<Window&, BackBufferSizeChangedEventArgs> BackBufferSizeChanged;

		// Run the given function on the Main/GUI thread
		// 'policy = std::launch::deferred' means the function is executed by the main thread during 'RunTasks'
		// 'policy = std::launch::async' means the function is run at any time in a worker thread. The result is collected in 'RunTasks'
		// 'policy' can be a bitwise OR of both deferred and async
		// WARNING: be careful with shutdown. Although functions are called on the main thread, than can still be called after
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

		// Execute any pending tasks in the task queue. Must be called from the Main/GUI thread
		void RunTasks();

		// Call this during shutdown to flush the task queue and prevent any further tasks from being added.
		void LastTask();

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
		void Poll();

		// These manager classes form part of the public interface of the renderer
		// Declared last so that events are fully constructed first.
		// Note: model manager is declared last so that it is destructed first
		BlendStateManager m_bs_mgr;
		DepthStateManager m_ds_mgr;
		RasterStateManager m_rs_mgr;
		TextureManager m_tex_mgr;
		ShaderManager m_shdr_mgr;
		ModelManager m_mdl_mgr;
	};
}
