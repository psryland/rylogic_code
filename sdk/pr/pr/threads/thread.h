//*********************************************************************
// Thread
//  Copyright © Rylogic Ltd 2009
//*********************************************************************
// Usage:
//	struct WorkerThread : Thread<WorkerThread>
//	{
//		void Main()
//		{
//			// Thread terminates when main exits
//			if (Cancelled()) return; // Poll cancelled event
//			TestPause();	// Put the thread to sleep until unpaused
//		}
//	}
	
#pragma once
#ifndef PR_THREADS_THREAD_H
#define PR_THREADS_THREAD_H
	
#include <windows.h>
#include <process.h>
#include <exception>
	
//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	namespace threads
	{
		template <typename Derived>
		class Thread
		{
			HANDLE        m_thread_handle; // Worker thread handle
			HANDLE        m_cancel_event;  // True when cancel has been signalled
			HANDLE        m_pause_event;   // True when pausing or unpausing
			HANDLE        m_paused_event;  // True while the worker thread is paused
			volatile bool m_running;       // True while the worker thread is running
			
			struct StartData
			{
				Thread<Derived>* m_this; void* m_ctx;
				StartData(Thread<Derived>* this_, void* ctx) :m_this(this_) ,m_ctx(ctx) {}
			};
			
			// Thread entry point
			static unsigned int __stdcall EntryPoint(void* data)
			{
				StartData* ctx = static_cast<StartData*>(data);
				try { ctx->m_this->Main(ctx->m_ctx); }
				catch (std::exception const& e) { PR_ASSERT(PR_DBG, false, "Unhandled exception thrown in worker thread"); (void)e; }
				catch (...)                     { PR_ASSERT(PR_DBG, false, "Unhandled exception thrown in worker thread"); }
				ctx->m_this->m_running = false;
				delete ctx;
				return 0;
			}
			
			// Not copyable
			Thread(Thread const&);
			Thread& operator=(Thread const&);
			
		// All of these methods are protected so that the derived type can
		// choose which interface methods to expose.
		// Remember to use:
		//   using Thread<Derived>::IsRunning;
		//   using Thread<Derived>::Cancel;
		//   etc
		protected:
			void Throw(BOOL res) { if (res == 0) throw std::exception("operation failed"); }
			
			Thread()
			:m_thread_handle(0)
			,m_cancel_event (::CreateEvent(0, TRUE, FALSE, 0))
			,m_pause_event  (::CreateEvent(0, FALSE, FALSE, 0))
			,m_paused_event (::CreateEvent(0, TRUE, FALSE, 0))
			,m_running      (false)
			{
				if (!m_cancel_event) throw std::exception("failed to create cancel thread event");
				if (!m_pause_event ) throw std::exception("failed to create pause thread event");
				if (!m_paused_event) throw std::exception("failed to create thread paused event");
			}
			virtual ~Thread()
			{
				if (m_running)       Stop(1000);
				if (m_thread_handle) CloseHandle(m_thread_handle);
				if (m_cancel_event ) CloseHandle(m_cancel_event);
				if (m_pause_event  ) CloseHandle(m_pause_event);
				if (m_paused_event ) CloseHandle(m_paused_event);
			}
			
			// Returns true if the worker thread is currently running
			volatile bool IsRunning() const
			{
				return m_running;
			}
			
			// Returns true if the worker thread is/was cancelled
			bool Cancelled(DWORD timeout_ms = 0) const
			{
				DWORD res = WaitForSingleObject(m_cancel_event, timeout_ms);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				return res == WAIT_OBJECT_0;
			}
			
			// Signal the worker thread to exit
			void Cancel()
			{
				Throw(::SetEvent(m_cancel_event));
			}
			
			// Wait on an object while pumping the message queue
			// Use 'wake_mask' to only allow certain messages to be pumped
			// QS_ALLEVENTS      - An input, WM_TIMER, WM_PAINT, WM_HOTKEY, or posted message is in the queue. This value is a combination of QS_INPUT, QS_POSTMESSAGE, QS_TIMER, QS_PAINT, and QS_HOTKEY.
			// QS_ALLINPUT       - Any message is in the queue. This value is a combination of QS_INPUT, QS_POSTMESSAGE, QS_TIMER, QS_PAINT, QS_HOTKEY, and QS_SENDMESSAGE.
			// QS_ALLPOSTMESSAGE - A posted message is in the queue. This value is cleared when you call GetMessage or PeekMessage without filtering messages.
			// QS_HOTKEY         - A WM_HOTKEY message is in the queue.
			// QS_INPUT          - An input message is in the queue. This value is a combination of QS_MOUSE, QS_KEY, and QS_RAWINPUT. Windows 2000:  This value does not include QS_RAWINPUT.
			// QS_KEY            - A WM_KEYUP, WM_KEYDOWN, WM_SYSKEYUP, or WM_SYSKEYDOWN message is in the queue.
			// QS_MOUSE          - A WM_MOUSEMOVE message or mouse-button message (WM_LBUTTONUP, WM_RBUTTONDOWN, and so on). This value is a combination of QS_MOUSEMOVE and QS_MOUSEBUTTON.
			// QS_MOUSEBUTTON    - A mouse-button message (WM_LBUTTONUP, WM_RBUTTONDOWN, and so on).
			// QS_MOUSEMOVE      - A WM_MOUSEMOVE message is in the queue.
			// QS_PAINT          - A WM_PAINT message is in the queue.
			// QS_POSTMESSAGE    - A posted message is in the queue. This value is cleared when you call GetMessage or PeekMessage, whether or not you are filtering messages.
			// QS_RAWINPUT       - A raw input message is in the queue. For more information, see Raw Input. Windows 2000:  This value is not supported.
			// QS_SENDMESSAGE    - A message sent by another thread or application is in the queue.
			// QS_TIMER          - A WM_TIME
			// WARNING: This is generally a broken design, message pumping should be avoided
			bool WaitWithPumping(DWORD count, HANDLE const* handles, BOOL wait_all, DWORD timeout, DWORD wake_mask = QS_ALLPOSTMESSAGE|QS_ALLINPUT|QS_ALLEVENTS)
			{
				for(;;)
				{
					DWORD res = MsgWaitForMultipleObjects(count, handles, wait_all, timeout, wake_mask);
					PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
					if (res == WAIT_TIMEOUT) return false;
					if (res == WAIT_OBJECT_0) return true;
					for (MSG msg; PeekMessage(&msg, 0, 0, 0, PM_REMOVE); DispatchMessage(&msg)) {} // Pump messages
				}
			}
			
			// Wait for the worker thread to exit.
			// Returns true if the worker thread exited
			bool Join(unsigned int timeout = INFINITE, DWORD* exit_code = 0)
			{
				if (!m_thread_handle) return true;
				DWORD res = WaitForSingleObject(m_thread_handle, timeout);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				if (res == WAIT_TIMEOUT) return false;
				if (exit_code) GetExitCodeThread(m_thread_handle, exit_code);
				return true;
			}
			
			// This version of join pumps the message queue while waiting for the worker thread to exit.
			// WARNING: This is generally a broken design, message pumping should be avoided
			bool JoinWithMsgPumping(DWORD wake_mask = QS_ALLPOSTMESSAGE|QS_ALLINPUT|QS_ALLEVENTS, unsigned int timeout = INFINITE, DWORD* exit_code = 0)
			{
				if (!m_thread_handle) return true;
				if (!WaitWithPumping(1, &m_thread_handle, FALSE, timeout, wake_mask)) return false;
				if (exit_code) GetExitCodeThread(m_thread_handle, exit_code);
				return true;
			}
			
			// Returns true if the worker is paused
			bool Paused()
			{
				DWORD res = WaitForSingleObject(m_paused_event, 0);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				return res == WAIT_OBJECT_0;
			}
			
			// Signal the worker thread to pause and block until it has.
			// Clients can call 'Pause' to stop the worker thread at a convenient time
			// The worker thread should call TestPause() periodically.
			void Pause(bool pause, DWORD block_time = INFINITE)
			{
				if (pause)         { Throw(::SetEvent(m_pause_event)); WaitWithPumping(1, &m_paused_event, FALSE, block_time); }
				else if (Paused()) { Throw(::SetEvent(m_pause_event)); }
			}
			
			// Worker thread can use this to pause at a convenient time
			// Use: while (TestPause() && !Cancelled()) { ... }
			bool TestPause()
			{
				DWORD res = WaitForSingleObject(m_pause_event, 0);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				if (res == WAIT_OBJECT_0)
				{
					res = SignalObjectAndWait(m_paused_event, m_pause_event, INFINITE, FALSE);
					PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
					Throw(::ResetEvent(m_paused_event));
				}
				return true;
			}
			
			// Change the priority of the thread
			// i.e.
			//	THREAD_PRIORITY_TIME_CRITICAL 
			//	THREAD_PRIORITY_HIGHEST 
			//	THREAD_PRIORITY_ABOVE_NORMAL 
			//	THREAD_PRIORITY_NORMAL 
			//	THREAD_PRIORITY_BELOW_NORMAL 
			//	THREAD_PRIORITY_LOWEST 
			//	THREAD_PRIORITY_IDLE 
			void SetThreadPriority(int priority)
			{
				PR_ASSERT(PR_DBG, m_thread_handle, "No thread handle");
				Throw(::SetThreadPriority(m_thread_handle, priority));
			}
			
			// Set the name of the thread
			void SetThreadName(char const* name)
			{
				struct THREADNAME_INFO
				{
					DWORD  dwType;     // Must be 0x1000.
					LPCSTR szName;     // Pointer to name (in user addr space).
					DWORD  dwThreadID; // Thread ID (-1=caller thread).
					DWORD  dwFlags;    // Reserved for future use, must be zero.
				};
				
				unsigned int const MS_VC_EXCEPTION = 0x406D1388;
				THREADNAME_INFO info;
				info.dwType     = 0x1000;
				info.szName     = name;
				info.dwThreadID = DWORD(-1);
				info.dwFlags    = 0;
				__try { RaiseException(MS_VC_EXCEPTION, 0, sizeof(info)/sizeof(DWORD), (DWORD*)&info); }
				__except(EXCEPTION_CONTINUE_EXECUTION) {}
			}
			
			// Start the worker thread
			// This method is virtual so that the derived type can create
			// it's own start method with additional parameters and then call this one.
			bool Start(void* ctx = 0, int priority = THREAD_PRIORITY_NORMAL, unsigned int stack_size = 0x2000)
			{
				// Can only run one at a time
				if (m_running) return false;
				if (m_thread_handle)
				{
					Stop();
					CloseHandle(m_thread_handle);
				}
				
				// Clear flags
				Throw(::ResetEvent(m_cancel_event));
				Throw(::ResetEvent(m_pause_event ));
				Throw(::ResetEvent(m_paused_event));
				PR_ASSERT(PR_DBG, !Cancelled(), "");
				PR_ASSERT(PR_DBG, !Paused(), "");
				m_running = true;
				
				// Create the thread
				m_thread_handle = reinterpret_cast<HANDLE>(_beginthreadex(0, stack_size, Thread::EntryPoint, new StartData(this, ctx), 0, 0)); // Don't use _beginthread() or CreateThread() because it closes the thread handle on thread exit (Join() won't work)
				if (!m_thread_handle) { m_running = false; return false; }
				
				// Set the initial thread priority
				SetThreadPriority(priority);
				return true;
			}
			
			// Stop the worker thread (synchronous)
			bool Stop(unsigned int timeout = INFINITE)
			{
				Cancel();
				return Join(timeout);
			}
			
			// Stop the worker thread asynchronously.
			// This stop method is intended for stopping groups of threads. The caller should compile an
			// array of the thread handles returned by this method for each Thread, then WaitForMultipleObjects
			// on the array of handles.
			// Note: Don't be tempted to add a GetThreadHandle() type function in order to do something like this:
			//  if (my_thread.GetThreadHandle() != 0) handles.push_back(my_thread.Stop());
			//  This is a race condition. The correct way is:
			//  HANDLE h = my_thread.Stop(); if (h) handles.push_back(h);
			HANDLE StopAsync()
			{
				Cancel();
				return m_thread_handle;
			}
			
			// The implementation entry point
			virtual void Main(void* ctx) = 0;
		};
	}
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
