//*********************************************************************
// Thread
//  Copyright © Rylogic Ltd 2009
//*********************************************************************
// Usage:
//	struct WorkerThread : Thread<WorkerThread>
//	{
//		typedef Thread<WorkerThread> base;
//
//		// base::Start is protected to allow derived types to decide if it should be exposed
//		using base::Start;
//
//		void Main(void*)
//		{
//			// Thread terminates when main exits
//			while (!Cancelled()) // Poll cancelled event
//				TestPause();     // Put the thread to sleep until unpaused
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
		private:
			HANDLE m_thread_handle;    // Worker thread handle
			HANDLE m_running;          // Set when the worker thread is running
			HANDLE m_paused;           // Set while the thread is paused
			HANDLE m_cancel_signalled; // Set when cancel has been signalled
			HANDLE m_pause_signalled;  // Set when pause has been signalled

			struct StartData
			{
				Thread<Derived>* m_this; void* m_ctx;
				StartData(Thread<Derived>* this_, void* ctx) :m_this(this_) ,m_ctx(ctx) {}
			};

			// Thread entry point
			static unsigned int __stdcall EntryPoint(void* data)
			{
				StartData* ctx = static_cast<StartData*>(data);
				PR_ASSERT(PR_DBG, ctx->m_this->m_running          ,"invalid thread running event"  );
				PR_ASSERT(PR_DBG, ctx->m_this->m_paused           ,"invalid thread paused event"   );
				PR_ASSERT(PR_DBG, ctx->m_this->m_cancel_signalled ,"invalid cancel signalled event");
				PR_ASSERT(PR_DBG, ctx->m_this->m_pause_signalled  ,"invalid pause signalled event" );

				try
				{
					Throw(::SetEvent(ctx->m_this->m_running)); // Set the running flag
					ctx->m_this->Main(ctx->m_ctx);
					Throw(::ResetEvent(ctx->m_this->m_running)); // Clear the running flag
				}
				catch (std::exception const& e) { PR_ASSERT(PR_DBG, false, "Unhandled exception thrown in worker thread"); (void)e; }
				catch (...)                     { PR_ASSERT(PR_DBG, false, "Unhandled exception thrown in worker thread"); }

				delete ctx;
				return 0;
			}

			// Not copyable
			Thread(Thread const&);
			Thread& operator=(Thread const&);

		// All of these methods are protected so that the derived type can
		// choose which interface methods to expose.
		// Remember to use:
		//   typedef Thread<Derived> base;
		//   using base::IsRunning;
		//   using base::Cancel;
		protected:
			static void Throw(BOOL res) { if (res == 0) throw std::exception("operation failed"); }

			Thread()
			:m_thread_handle(0)
			,m_running         (::CreateEvent(0, TRUE, FALSE, 0))
			,m_paused          (::CreateEvent(0, TRUE, FALSE, 0))
			,m_cancel_signalled(::CreateEvent(0, TRUE, FALSE, 0))
			,m_pause_signalled (::CreateEvent(0, FALSE, FALSE, 0))
			{
				if (!m_running) throw std::exception("failed to create thread running event");
				if (!m_paused) throw std::exception("failed to create thread paused event");
				if (!m_cancel_signalled) throw std::exception("failed to create cancel signalled event");
				if (!m_pause_signalled) throw std::exception("failed to create pause signalled event");
			}
			virtual ~Thread()
			{
				if (IsRunning()) Stop(1000);
				if (m_pause_signalled)  CloseHandle(m_pause_signalled);
				if (m_cancel_signalled) CloseHandle(m_cancel_signalled);
				if (m_paused)           CloseHandle(m_paused);
				if (m_running)          CloseHandle(m_running);
				if (m_thread_handle)    CloseHandle(m_thread_handle);
			}

			// Returns true if the worker thread is currently running
			// Any thread can call this
			bool IsRunning(DWORD timeout_ms = 0) const
			{
				DWORD res = WaitForSingleObject(m_running, timeout_ms);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				return res == WAIT_OBJECT_0;
			}

			// Returns true if the worker thread has cancel signalled
			// Any thread can call this
			bool IsCancelled(DWORD timeout_ms = 0) const
			{
				DWORD res = WaitForSingleObject(m_cancel_signalled, timeout_ms);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				return res == WAIT_OBJECT_0;
			}

			// Returns true if the worker is paused
			// Waits up to 'timeout_ms' for it to become paused
			// Note, does not block if already paused
			// Any thread can call this
			bool IsPaused(DWORD timeout_ms = 0) const
			{
				DWORD res = WaitForSingleObject(m_paused, timeout_ms);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				return res == WAIT_OBJECT_0;
			}

			// Signal the worker thread to exit
			void Cancel()
			{
				Throw(::SetEvent(m_cancel_signalled));
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

			// Signal the worker thread to pause and then block for up to 'block_time' until it has.
			// Note: 'block_time' only applies to pausing, not un-pausing. However, when this function
			// returns the worker thread is no longer paused.
			// This only works if the worker thread calls TestPause() periodically.
			// 'block_time' is INFINITE by default because calling code typically
			// wants to use Pause for synchronisation. Calling "Pause(true,0)"
			// means 'IsPaused()' may return false
			void Pause(bool pause, DWORD block_time = INFINITE)
			{
				// Only un-pause if currently paused
				if (IsPaused())
				{
					// When the worker thread is paused it waits on the
					// 'm_pause_signalled' event which can only be set here.
					if (pause == false)
					{
						Throw(::SetEvent(m_pause_signalled));
						for (;IsPaused();Sleep(0)){}// Wait for the thread to say it's not paused
					}
				}
				// Can only pause if not currently paused
				else if (pause)
				{
					Throw(::SetEvent(m_pause_signalled));
					IsPaused(block_time); // Wait for the thread to say it's paused
				}
			}

			// Worker thread can use this to pause at a convenient time
			// Use: while (TestPause() && !IsCancelled()) { ... }
			bool TestPause()
			{
				DWORD res = WaitForSingleObject(m_pause_signalled, 0);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
				if (res == WAIT_OBJECT_0)
				{
					// If pause was signalled, signal the 'I am paused' event, then sleep until pause is signalled again.
					// Note, un-pausing is only possible after 'm_paused' is signalled, so there isn't a race condition
					// with repeated alternating Pause(true),Pause(false) calls
					res = SignalObjectAndWait(m_paused, m_pause_signalled, INFINITE, FALSE);
					PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a thread shutdown error");
					Throw(::ResetEvent(m_paused)); // No longer paused
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
				__try { RaiseException(MS_VC_EXCEPTION, 0, sizeof(info)/sizeof(DWORD), (ULONG_PTR const*)&info); }
				__except(EXCEPTION_CONTINUE_EXECUTION) {}
			}

			// Start the worker thread
			// This method is virtual so that the derived type can create
			// it's own start method with additional parameters and then call this one.
			bool Start(void* ctx = 0, int priority = THREAD_PRIORITY_NORMAL, unsigned int stack_size = 0x2000)
			{
				// Can only run one at a time
				if (IsRunning()) return false;
				if (m_thread_handle)
				{
					Stop();
					CloseHandle(m_thread_handle);
				}

				// Clear flags
				Throw(::ResetEvent(m_running));
				Throw(::ResetEvent(m_paused)); // This does not mean paused can't be signalled however
				Throw(::ResetEvent(m_cancel_signalled));

				// Create the thread
				m_thread_handle = reinterpret_cast<HANDLE>(_beginthreadex(0, stack_size, Thread::EntryPoint, new StartData(this, ctx), 0, 0)); // Don't use _beginthread() or CreateThread() because it closes the thread handle on thread exit (Join() won't work)
				if (!m_thread_handle) return false;

				// Set the initial thread priority
				SetThreadPriority(priority);
				return true;
			}

			// Stop the worker thread (synchronous)
			// This is the same as Cancel() then Join() since an external
			// thread deciding when to stop the thread implies cancelling.
			// Not cancelled means the thread exited by itself.
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

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		namespace threads
		{
			struct Thing :pr::threads::Thread<Thing>
			{
				typedef pr::threads::Thread<Thing> base;

				volatile long m_run_count;
				bool m_test_cancel;
				bool m_test_pause;

				Thing(bool test_cancel, bool test_pause)
					:m_run_count(0)
					,m_test_cancel(test_cancel)
					,m_test_pause(test_pause)
				{}

				using base::Start;
				using base::Pause;
				using base::Cancel;
				using base::Stop;
				using base::IsRunning;
				using base::IsCancelled;
				using base::IsPaused;

				void Main(void*)
				{
					for (;;)
					{
						if (m_test_pause)
							TestPause();

						::InterlockedIncrement(&m_run_count);

						if (m_test_cancel && IsCancelled())
							break;

						if (!m_test_pause && !m_test_cancel)
							break;
					}
				}

				long RunCount() const
				{
					// const cast cause we're not really changing it, just reading it
					return ::InterlockedCompareExchange(const_cast<volatile long*>(&m_run_count), 0L, 0L);
				}
			};
		}

		PRUnitTest(pr_threads_thread)
		{
			using namespace pr::unittests::threads;

			{
				Thing thg(true,true);
				PR_CHECK(thg.IsRunning(), false);
				PR_CHECK(thg.IsCancelled(), false);
				PR_CHECK(thg.IsPaused(), false);
				PR_CHECK(thg.RunCount(), 0);
				thg.Pause(true, 0);
				thg.Start();
				PR_CHECK(thg.IsRunning(1000), true);
				PR_CHECK(thg.IsCancelled(), false);
				PR_CHECK(thg.IsPaused(), true);
				PR_CHECK(thg.RunCount(), 0);
				thg.Pause(false);
				PR_CHECK(thg.IsRunning(), true);
				PR_CHECK(thg.IsCancelled(), false);
				PR_CHECK(thg.IsPaused(), false);

				// Can't test RunCount() due to race condition
				while (thg.RunCount() == 0) Sleep(0);

				// Test for race condition
				for (int i = 0; i != 1000; ++i)
				{
					thg.Pause(true);
					PR_CHECK(thg.IsPaused(), true);
					thg.Pause(false);
					PR_CHECK(thg.IsPaused(), false);

					thg.Pause(false);
					thg.Pause(true );
					thg.Pause(false);
					thg.Pause(true );
					thg.Pause(false);
					thg.Pause(true );

					PR_CHECK(thg.IsPaused(), true);

					thg.Pause(true );
					thg.Pause(false);
					thg.Pause(true );
					thg.Pause(false);
					thg.Pause(true );
					thg.Pause(false);

					PR_CHECK(thg.IsPaused(), false);
				}

				thg.Pause(true);
				PR_CHECK(thg.IsRunning()     , true );
				PR_CHECK(thg.IsCancelled()   , false);
				PR_CHECK(thg.IsPaused()      , true );
				PR_CHECK(thg.RunCount() != 0 , true );
				thg.Pause(false);
				thg.Stop();
				PR_CHECK(thg.IsRunning()     , false);
				PR_CHECK(thg.IsCancelled()   , true );
				PR_CHECK(thg.IsPaused()      , false);
				PR_CHECK(thg.RunCount() != 0 , true );
			}
		}
	}
}
#endif

#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif

#endif
