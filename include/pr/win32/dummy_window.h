//**********************************************
// Dummy window
//  Copyright (c) Rylogic Ltd 2020
//**********************************************
#pragma once
#include <mutex>
#include <future>
#include <vector>
#include <exception>
#include <Windows.h>
#include "pr/common/hresult.h"
#include "pr/common/event_handler.h"

namespace pr
{
	struct DummyWindow
	{
		HINSTANCE m_hinstance;
		HWND m_hwnd;

	private:

		static inline wchar_t const* DummyWndClassName = L"Rylogic-DummyWindow";
		static UINT const WM_BeginInvoke = WM_USER + 0x1976;

		using TaskQueue = std::vector<std::future<void>>;
		std::mutex m_mutex_task_queue;
		TaskQueue m_task_queue;
		DWORD m_main_thread_id;
		bool m_last_task;

	public:

		DummyWindow(HINSTANCE hinstance = nullptr)
			:m_hinstance(hinstance ? hinstance : GetModuleHandleW(nullptr))
			,m_hwnd()
			,m_mutex_task_queue()
			,m_task_queue()
			,m_main_thread_id(GetCurrentThreadId())
			,m_last_task()
		
		{
			try
			{
				// Register a window class for the dummy window
				WNDCLASSEXW wc = {sizeof(WNDCLASSEXW)};
				auto atom = ATOM(::GetClassInfoExW(m_hinstance, DummyWndClassName, &wc));
				if (atom == 0)
				{
					// RegisterClass only sets last error if there is actually an error
					SetLastError(S_OK);

					wc.style         = 0;
					wc.cbClsExtra    = 0;
					wc.cbWndExtra    = 0;
					wc.hInstance     = m_hinstance;
					wc.hIcon         = nullptr;
					wc.hIconSm       = nullptr;
					wc.hCursor       = nullptr;
					wc.hbrBackground = nullptr;
					wc.lpszMenuName  = nullptr;
					wc.lpfnWndProc   = &StaticWndProc;
					wc.lpszClassName = DummyWndClassName;
					atom = ATOM(::RegisterClassExW(&wc));
					if (atom == 0)
						throw std::runtime_error(HrMsg(GetLastError()).c_str());
				}

				::CreateWindowExW(0, (LPCWSTR)MAKEINTATOM(atom), L"", 0, 0, 0, 0, 0, HWND_MESSAGE, nullptr, m_hinstance, this);
				if (m_hwnd == nullptr)
					throw std::runtime_error(HrMsg(GetLastError()).c_str());
			}
			catch (std::exception const&)
			{
				this->~DummyWindow();
				throw;
			}
		}
		~DummyWindow()
		{
			if (m_hwnd != nullptr)
			{
				::DestroyWindow(m_hwnd);
				m_hwnd = nullptr;
			}

			// Un-register the dummy window class
			::UnregisterClassW(DummyWndClassName, m_hinstance);
		}
		DummyWindow(DummyWindow const&) = delete;
		DummyWindow& operator =(DummyWindow const&) = delete;

		// Pump the message queue on this window. Returns false if WM_QUIT is received
		bool Pump()
		{
			for (MSG msg; ::PeekMessageW(&msg, 0, 0, 0, PM_REMOVE);)
			{
				::TranslateMessage(&msg);
				::DispatchMessageW(&msg);
				if (msg.message == WM_QUIT)
					return false;
			}
			return true;
		}

		/// <summary>Window message received</summary>
		pr::EventHandler<DummyWindow&, WindowEventArgs&> Message;

		// Pump the message queue for this window
		template <typename Func, typename... Args>
		void BeginInvoke(Func&& func, Args&&... args)
		{
			static_assert(noexcept(func(args...)));
			BeginInvoke(std::launch::deferred, func, args...);
		}
		template <typename Func, typename... Args>
		void BeginInvoke(std::launch policy, Func&& func, Args&&... args)
		{
			{
				std::lock_guard<std::mutex> lock(m_mutex_task_queue);
				if (m_last_task) return; // Don't add further tasks after 'LastTask' has been called.
				m_task_queue.emplace_back(std::async(policy, func, args...));
			}

			// Post a message to notify of the new task
			for (; !::PostMessageW(m_hwnd, WM_BeginInvoke, WPARAM(this), LPARAM());)
			{
				auto err = GetLastError();
				if (err == ERROR_NOT_ENOUGH_QUOTA)
				{
					// The message queue is full, just wait a bit. This is probably a deadlock though
					std::this_thread::yield();
					continue;
				}

				auto msg = HrMsg(err);
				throw std::runtime_error(msg.c_str());
			}
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

	private:

		// WndProc for the dummy window
		LRESULT WndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
		{
			switch (message)
			{
				case WM_BeginInvoke:
				{
					auto& self = *reinterpret_cast<DummyWindow*>(wparam);
					self.RunTasks();
					break;
				}
				default:
				{
					WindowEventArgs args = {hwnd, message, wparam, lparam, false};
					Message(*this, args);
					break;
				}
			}
			return DefWindowProcW(hwnd, message, wparam, lparam);
		}
		static LRESULT __stdcall StaticWndProc(HWND hwnd, UINT message, WPARAM wparam, LPARAM lparam)
		{
			DummyWindow* self;
			if (message == WM_NCCREATE) 
			{
				auto cp = reinterpret_cast<LPCREATESTRUCT>(lparam);
				self = reinterpret_cast<DummyWindow*>(cp->lpCreateParams);
				SetWindowLongPtrW(hwnd, GWLP_USERDATA, reinterpret_cast<LPARAM>(self));
				self->m_hwnd = hwnd;
			} 
			else
			{
				self = reinterpret_cast<DummyWindow*>(GetWindowLongPtrW(hwnd, GWLP_USERDATA));
			}
			return self
				? self->WndProc(hwnd, message, wparam, lparam)
				: DefWindowProcW(hwnd, message, wparam, lparam);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::win32
{
	PRUnitTest(DummyWindowTests)
	{
		DummyWindow dw0;
		DummyWindow dw1;
	}
}
#endif
