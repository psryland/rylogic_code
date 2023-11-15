//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/main/settings.h"
#include "pr/view3d-12/resource/resource_manager.h"
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

		// Renderer state
		struct RdrState
		{
			// This is needed so that the Dx12 device is created before the managers are constructed.
			RdrSettings                 m_settings;
			FeatureSupport              m_features;
			D3DPtr<ID3D12Device4>       m_d3d_device;
			D3DPtr<ID3D12CommandQueue>  m_gfx_queue;
			D3DPtr<ID3D12CommandQueue>  m_com_queue;
			D3DPtr<ID3D12CommandQueue>  m_cpy_queue;
			D3DPtr<ID3D11On12Device>    m_dx11_device;
			D3DPtr<ID3D11DeviceContext> m_dx11_dc;
			D3DPtr<ID2D1Factory2>       m_d2dfactory;
			D3DPtr<ID2D1Device>         m_d2d_device;
			DWORD                       m_main_thread_id;

			RdrState(RdrSettings const& settings);
			~RdrState();
		};

		// Members
		RdrState             m_state;
		std::recursive_mutex m_d3d_mutex;
		std::mutex           m_mutex_task_queue;
		TaskQueue            m_task_queue;
		bool                 m_last_task;
		PollCBList           m_poll_callbacks;
		HWND                 m_dummy_hwnd;
		std::atomic_int      m_id32_src;

		// These manager classes form part of the public interface of the renderer
		// Declared last so that events are fully constructed first.
		// Note: model manager is declared last so that it is destructed first
		ResourceManager m_res_mgr;

	public:

		explicit Renderer(RdrSettings const& settings);
		Renderer(Renderer const&) = delete;
		Renderer& operator=(Renderer const&) = delete;
		~Renderer();

		// Access the adapter that the device was created on
		IDXGIAdapter* Adapter() const
		{
			return m_state.m_settings.m_adapter.ptr.get();
		}

		// Access the device
		ID3D12Device4* D3DDevice() const
		{
			// The D3D device is free-threaded in DX12, no need to synchronise access to it.
			return m_state.m_d3d_device.get();
		}

		// Return the graphics command queue
		ID3D12CommandQueue* GfxQueue() const
		{
			// The D3D command queue is free-threaded in DX12, no need to synchronise access to it.
			return m_state.m_gfx_queue.get();
		}

		// Return the compute command queue
		ID3D12CommandQueue* ComQueue() const
		{
			// The D3D command queue is free-threaded in DX12, no need to synchronise access to it.
			return m_state.m_com_queue.get();
		}

		// Return the copy command queue
		ID3D12CommandQueue* CpyQueue() const
		{
			// The D3D command queue is free-threaded in DX12, no need to synchronise access to it.
			return m_state.m_cpy_queue.get();
		}

		// Return the Dx11 device
		ID3D11On12Device* Dx11Device() const
		{
			return m_state.m_dx11_device.get();
		}

		// Return the Dx11 device context
		ID3D11DeviceContext* Dx11DeviceContext() const
		{
			return m_state.m_dx11_dc.get();
		}

		// Return the direct2d factory
		ID2D1Factory* D2DFactory() const
		{
			return m_state.m_d2dfactory.get();
		}

		// Return the d2d device
		ID2D1Device* D2DDevice() const
		{
			return m_state.m_d2d_device.get();
		}

		// Access the renderer manager classes
		Renderer& rdr();
		ResourceManager const& res_mgr() const;
		ResourceManager& res_mgr();

		// Read access to the initialisation settings
		RdrSettings const& Settings() const;

		// Device supported features
		FeatureSupport const& Features() const;

		// Return the associated HWND. Note: this is not associated with any particular window. 'Window' objects have an hwnd.
		HWND DummyHwnd() const;

		// Return the current desktop DPI (Fall back if window DPI not available)
		v2 SystemDpi() const;

		// Return info about the available video memory
		DXGI_QUERY_VIDEO_MEMORY_INFO GPUMemoryInfo() const;

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

		// For when I'm searching for the d3d device on this object
		enum { D3DDevice_IsAccessedViaTheRendererLock };
		bool AssertMainThread() const;
	};
}

