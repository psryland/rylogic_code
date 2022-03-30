//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/main/settings.h"
#include "pr/view3d-12/utility/features.h"
#include "pr/view3d-12/utility/eventargs.h"

namespace pr::rdr12
{
	// Registered windows message for BeginInvoke
	inline static constexpr UINT WM_BeginInvoke = WM_USER + 0x1976;

	// Render main object
	struct Renderer
	{
	private:

		using TaskQueue = std::vector<std::future<void>>;
		using PollCBList = std::vector<pr::StaticCB<void>>;

		DWORD                      m_main_thread_id;
		RdrSettings                m_settings;
		FeatureSupport             m_features;
		D3DPtr<ID3D12Device1>      m_d3d_device;
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

		// These manager classes form part of the public interface of the renderer
		// Declared last so that events are fully constructed first.
		// Note: model manager is declared last so that it is destructed first
		//BlendStateManager m_bs_mgr;
		//DepthStateManager m_ds_mgr;
		//RasterStateManager m_rs_mgr;
		//TextureManager m_tex_mgr;
		//ShaderManager m_shdr_mgr;
		//ModelManager m_mdl_mgr;

	public:

		explicit Renderer(RdrSettings const& settings);
		~Renderer();

		// Access the renderer manager classes
		ModelManager& mdl_mgr() const;
		ShaderManager& shdr_mgr() const;
		TextureManager& tex_mgr() const;
		BlendStateManager& bs_mgr() const;
		DepthStateManager& ds_mgr() const;
		RasterStateManager& rs_mgr() const;

		// Read access to the initialisation settings
		RdrSettings const& Settings() const;

		// Device supported features
		FeatureSupport const& Features() const;

		// Return the associated HWND. Note: this is not associated with any particular window. 'Window' objects have an hwnd.
		HWND DummyHwnd() const;

		// Return the current desktop DPI (Fall back if window DPI not available)
		v2 SystemDpi() const;

		// Generate a unique Id on each call
		int NewId32();

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
		void RunTasks();

		// Call this during shutdown to flush the task queue and prevent any further tasks from being added.
		void LastTask();

		// Add/Remove a callback function that will be polled as fast as the windows message queue will allow
		void AddPollCB(pr::StaticCB<void> cb);
		void RemovePollCB(pr::StaticCB<void> cb);

		// Call all registered poll event callbacks
		void Poll();

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
			ID3D12Device1* D3DDevice() const
			{
				return m_rdr.m_d3d_device.get();
			}

			// Return the main command queue
			ID3D12CommandQueue* CmdQueue() const
			{
				return m_rdr.m_cmd_queue.get();
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

			// Return the adapter that the device was created on
			IDXGIAdapter* Adapter() const
			{
				return m_rdr.m_settings.m_adapter.ptr.get();
			}
		};

		// For when I'm searching for the d3d device on this object
		enum { D3DDevice_IsAccessedViaTheRendererLock };
	};
}

