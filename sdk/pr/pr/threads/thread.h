//*********************************************************************
// Thread
//  Copyright © Rylogic Ltd 2014
//*********************************************************************
#pragma once
#ifndef PR_THREADS_THREAD_H
#define PR_THREADS_THREAD_H

#include <thread>
#include <mutex>
#include <condition_variable>
#include <chrono>
#include <cassert>

namespace pr
{
	namespace threads
	{
		// A worker thread that provides pause/cancel functionality
		template <class TTask> struct Thread
		{
		private:
			typedef std::unique_lock<std::mutex> Lock;
			typedef std::chrono::milliseconds milliseconds_t;

			mutable std::mutex              m_mutex;          // Synchronising
			mutable std::condition_variable m_cv_running;     // Async testing m_stop_signalled
			mutable std::condition_variable m_cv_pause;       // Async testing m_pause_count
			bool                            m_running;        // True while the thread is running
			bool                            m_stop_signalled; // True when 'SignalCancel' is called
			bool                            m_paused;         // True while the thread is paused
			int                             m_pause_count;    // A ref count of the number of times pause has been called
			std::thread                     m_thread;         // The thread doing the running

			Thread(Thread  const&);
			Thread& operator=(Thread const&);
			template <typename Derived, bool AllowCancel, bool AllowPause> friend struct Task;

			// The thread entry point
			template <typename Derived, bool AllowCancel, bool AllowPause>
			void EntryPoint(Task<Derived,AllowCancel,AllowPause>& task)
			{
				struct RunScope
				{
					Thread* m_thread;
					TTask*  m_task;
					RunScope(Thread* thread, TTask& task) :m_thread(thread), m_task(&task)
					{
						try
						{
							assert(m_task->m_pr_threads_thread == nullptr && "Task already running in another thread");
							task.m_pr_threads_thread = thread;

							{// Signal as running
								Lock lock(m_thread->m_mutex);
								m_thread->m_running = true;
								m_thread->m_cv_running.notify_all();
							}

							// Stop on initial pause
							if (!m_task->Done())
								m_task->Main();
						}
						catch (std::exception const&) { assert(!"Unhandled std::exception thrown in worker thread"); }
						catch (...)                   { assert(!"Unhandled exception thrown in worker thread"); }
					}
					~RunScope()
					{
						{// Signal as not running
							Lock lock(m_thread->m_mutex);
							m_thread->m_running = false;
							m_thread->m_cv_running.notify_all();
						}

						m_task->m_pr_threads_thread = nullptr;
					}
				} run(this, task);
			}

			// Tests for stop being signalled and also pauses the thread if requested
			bool Done()
			{
				Lock lock(m_mutex);

				if (m_cv_running.wait_for(lock, milliseconds_t::zero(), [&]{ return m_stop_signalled; }))
					return true;

				// Test whether we should pause
				// This isn't a race condition because we're locked
				if (m_pause_count != 0)
				{
					// Signal that we're now paused
					m_paused = true;
					m_cv_pause.notify_all();

					// Wait until unpaused
					m_cv_pause.wait(lock, [&]{ return m_pause_count == 0; });
				}

				// If we're flagged as paused, signal unpaused now
				if (m_paused)
				{
					m_paused = false;
					m_cv_pause.notify_all();
				}
				return false;
			}

		public:
			Thread(TTask& task, int pause_count = 0)
				:m_mutex         ()
				,m_cv_running    ()
				,m_cv_pause      ()
				,m_running       (false)
				,m_stop_signalled(false)
				,m_paused        (false)
				,m_pause_count   (pause_count)
				,m_thread        ([this]{ EntryPoint(); })
			{}
			Thread(Thread&& rhs)
				:m_mutex         ()
				,m_cv_running    ()
				,m_cv_pause      ()
				,m_running       (rhs.m_running)
				,m_stop_signalled(rhs.m_stop_signalled)
				,m_paused        (rhs.m_paused)
				,m_pause_count   (rhs.m_pause_count)
				,m_thread        (std::move(rhs.m_thread))
			{}
			~Thread()
			{
				{
					Lock lock(m_mutex);
					m_stop_signalled = true;
					m_pause_count = 0;
					m_cv_pause.notify_all();
					m_cv_running.notify_all();
				}
				if (m_thread.joinable())
					m_thread.join();
			}
			Thread& operator=(Thread&& rhs)
			{
				m_running        = rhs.m_running;
				m_stop_signalled = rhs.m_stop_signalled;
				m_paused         = rhs.m_paused;
				m_pause_count    = rhs.m_pause_count;
				m_thread         = std::move(rhs.m_thread);
				return *this;
			}

			// True if the thread is running
			bool IsRunning(size_t timeout = 0U) const
			{
				Lock lock(m_mutex);
				return m_cv_running.wait_for(lock, milliseconds_t(timeout), [&]{ return m_running; });
			}

			// Block until this thread completes.
			// Returns true if the thread completed before the timeout
			bool Join(size_t timeout = ~0U)
			{
				Lock lock(m_mutex);
				return timeout != ~0U
					? m_cv_running.wait_for(lock, milliseconds_t(timeout), [&]{ return m_running == false; })
					: (m_cv_running.wait(lock, [&]{ return m_running == false; }), true);
			}

			// True if the thread has been cancelled
			typename std::enable_if<TTask::is_cancelable, bool>::type
			IsCancelled(size_t timeout = 0U) const
			{
				Lock lock(m_mutex);
				return m_cv_running.wait_for(lock, milliseconds_t(timeout), [&]{ return m_stop_signalled; });
			}

			// Ask the task to abort and then block for up to 'timeout' until it has.
			// If 'timeout' == max() then Cancel waits indefinitely.
			// This only works if the worker thread calls Done() periodically.
			// 'timeout' is infinite by default because calling code typically
			// wants to use Cancel to terminate the task thread.
			// Returns true if the cancel took effect within the timeout.
			typename std::enable_if<TTask::is_cancelable, bool>::type
			Cancel(size_t timeout = ~0U)
			{
				Lock lock(m_mutex);
				m_stop_signalled = true;
				m_cv_running.notify_all();

				return timeout != ~0U
					? m_cv_running.wait_for(lock, milliseconds_t(timeout), [&]{ return m_running == false; })
					: (m_cv_running.wait(lock, [&]{ return m_running == false; }), true);
			}

			// True if the thread has been paused
			typename std::enable_if<TTask::is_pauseable, bool>::type
			IsPaused(size_t timeout = 0U) const
			{
				Lock lock(m_mutex);
				return m_cv_pause.wait_for(lock, milliseconds_t(timeout), [&]{ return m_pause_count != 0; });
			}

			// Signal the worker thread to pause or unpause and then block for up to
			// 'timeout' until it has. If 'timeout' == max() then Pause() waits indefinitely.
			// This only works if the worker thread calls Done() periodically.
			// 'timeout' is infinite by default because calling code typically
			// wants to use Pause for synchronisation. i.e. Calling "Pause(true, msec_t::zero())"
			// means that 'IsPaused()' may return false after this method returns.
			// Returns true if the pause or unpause took effect within the timeout.
			typename std::enable_if<TTask::is_pauseable, bool>::type
			Pause(bool pause, size_t timeout = ~0U)
			{
				Lock lock(m_mutex);
				m_pause_count += pause ? 1 : -1;
				assert(m_pause_count >= 0 && "Pause count is less than zero");
				m_cv_pause.notify_all();

				if (pause)
				{
					return timeout != ~0U
						? m_cv_pause.wait_for(lock, milliseconds_t(timeout), [&]{ return m_paused; })
						: (m_cv_pause.wait(lock, [&]{ return m_paused; }), true);
				}
				else if (m_pause_count == 0)
				{
					return timeout != ~0U
						? m_cv_pause.wait_for(lock, milliseconds_t(timeout), [&]{ return !m_paused; })
						: (m_cv_pause.wait(lock, [&]{ return !m_paused; }), true);
				}
				else
				{
					return true;
				}
			}
		};

		// A base class for types that encapsulate a task.
		template <typename Derived, bool AllowCancel = true, bool AllowPause = true>
		struct Task
		{
		private:
			// The thread currently running this task.
			template <typename TTask> friend struct Thread;
			Thread<Task<Derived,AllowCancel,AllowPause>>* m_pr_threads_thread;

			enum
			{
				is_cancelable = AllowCancel,
				is_pauseable  = AllowPause,
			};

		protected:
			Task() :m_pr_threads_thread(nullptr) {}
			virtual ~Task() {}

			// The task body
			virtual void Main() = 0;

			// Main should periodically call 'Done' to support pause and cancel
			bool Done() { return m_pr_threads_thread->Done(); }
		};

		// Start a thread to run 'task'
		template <typename TTask> inline Thread<TTask> Start(TTask& task, int pause_count = 0)
		{
			return Thread<TTask>(task, pause_count);
		}

		// Like start except an heap allocated instance of the thread is returned
		template <typename TTask> inline std::unique_ptr<Thread<TTask>> Create(TTask& task, int pause_count = 0)
		{
			return std::unique_ptr<Thread<TTask>>(new Thread<TTask>(task, pause_count));
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <atomic>
namespace pr
{
	namespace unittests
	{
		namespace threads
		{
			class MyTask :public pr::threads::Task<MyTask>
			{
			public:
				typedef pr::threads::Task<MyTask> base;

				std::atomic_long m_run_count;
				MyTask() :m_run_count() {}
				void Main()
				{
					for (;!Done();)
					{
						m_run_count++;
					}
				}
			};
		}

		PRUnitTest(pr_threads_thread)
		{
			using namespace pr::unittests::threads;
			using namespace pr::threads;

			{
				MyTask task;
				auto job = Start(task, 1);

				PR_CHECK(job.IsRunning(100) , true );
				PR_CHECK(job.IsCancelled()  , false);
				PR_CHECK(job.IsPaused()     , true );
				PR_CHECK(task.m_run_count   , 0    );

				job.Pause(true); // count = 2

				PR_CHECK(job.IsRunning(100) , true );
				PR_CHECK(job.IsCancelled()  , false);
				PR_CHECK(job.IsPaused()     , true );
				PR_CHECK(task.m_run_count   , 0    );

				job.Pause(false); // count = 1

				PR_CHECK(job.IsRunning()   , true );
				PR_CHECK(job.IsCancelled() , false);
				PR_CHECK(job.IsPaused()    , true );

				job.Pause(false); // count = 0

				PR_CHECK(job.IsRunning()   , true );
				PR_CHECK(job.IsCancelled() , false);
				PR_CHECK(job.IsPaused()    , false);

				while (task.m_run_count < 10)
					std::this_thread::yield();

				// Test for race condition
				for (int i = 0; i != 1000; ++i)
				{
					job.Pause(true);
					PR_CHECK(job.IsPaused() , true);

					job.Pause(false);
					PR_CHECK(job.IsPaused() , false);

					job.Pause(true );
					job.Pause(false);
					job.Pause(true );
					job.Pause(false);
					job.Pause(true );

					PR_CHECK(job.IsPaused() , true);

					job.Pause(false);
					job.Pause(true );
					job.Pause(false);
					job.Pause(true );
					job.Pause(false);

					PR_CHECK(job.IsPaused() , false);
				}

				job.Pause(true);

				PR_CHECK(job.IsRunning()       , true );
				PR_CHECK(job.IsCancelled()     , false);
				PR_CHECK(job.IsPaused()        , true );
				PR_CHECK(task.m_run_count != 0 , true );

				job.Pause(false);
				job.Cancel();

				PR_CHECK(job.IsRunning()       , false);
				PR_CHECK(job.IsCancelled()     , true );
				PR_CHECK(job.IsPaused()        , false);
				PR_CHECK(task.m_run_count != 0 , true );
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

/*

#if 0

//WARNING: this implementation is broken,
// The Thread destructor calls Stop() on a thread that is running a
// derived type, however by the time ~Thread() is called the derived
// type has already been destructed.
// A correct implementation should separate the logic to be run from
// the control of the thread, having a type that runs whatever is derived
// from it is broken due to construction/destruction order.

// This needs replacing with a std::thread based implementation anyway

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
#endif

*/