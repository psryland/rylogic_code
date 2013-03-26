//***********************************************************************
// ThreadPool
//  Copyright © Rylogic Ltd 2011
//***********************************************************************

#pragma once
#ifndef PR_THREADS_THREADPOOL_H
#define PR_THREADS_THREADPOOL_H

#include <vector>
#include <windows.h>
#include <process.h>
#include "pr/macros/stringise.h"
#include "pr/common/assert.h"
#include "pr/threads/event.h"
#include "pr/threads/item_queue.h"

#pragma warning (disable: 4355) // 'this' used in constructor

namespace pr
{
	namespace threads
	{
		class ThreadPool
		{
			typedef void (*TaskFunc)(void* ctx, void* data);
			
			// Creates a unique static reference to 'this_' thread pool.
			// Each thread pool has it's own static reference so the ThreadPool is not a singleton.
			struct StaticRef
			{
				StaticRef(ThreadPool* this_) { This() = this_; }
				~StaticRef()                 { This() = 0; }
				
				#define PR_THREADS_THREADPOOL_NAME2 s_thread_pool_##__COUNTER__
				#define PR_THREADS_THREADPOOL_NAME PR_THREADS_THREADPOOL_NAME2
				static ThreadPool*& This() { static ThreadPool* PR_THREADS_THREADPOOL_NAME = 0; return PR_THREADS_THREADPOOL_NAME; }
				#undef PR_THREADS_THREADPOOL_NAME
				#undef PR_THREADS_THREADPOOL_NAME2
			};
			
			typedef std::vector<HANDLE> ThreadCont;
			
			// Encapsulates a task
			struct Task
			{
				TaskFunc m_func;
				void*    m_ctx;
				void*    m_data;
				Task() :m_func(0) ,m_ctx(0) ,m_data(0) {}
				Task(TaskFunc func, void* ctx, void* data) :m_func(func) ,m_ctx(ctx) ,m_data(data) {}
			};
			typedef pr::threads::ItemQueue<Task> TaskQueue;
			
			// Members
			StaticRef          m_static_ref;         // A static reference to this thread pool
			pr::threads::Event m_pending;            // An Event to signal that there are tasks to execute
			TaskQueue          m_tasks;              // The tasks awaiting execution
			ThreadCont         m_thread;             // The pool of threads
			volatile bool      m_shutdown;           // Used to signal that the thread pool is shutting down
			volatile LONG      m_active_count;       // Interlocked count of threads that are doing something

			// Entry point for every worker thread
			static unsigned long __stdcall ThreadEntry(void*) { StaticRef::This()->ThreadMain(); return 0; }
			void ThreadMain()
			{
				while (!m_shutdown)
				{
					Task task;
					if (!m_tasks.Dequeue(task))
					{
						m_pending.Wait();
						continue;
					}
					InterlockedIncrement(&m_active_count);
					try { task.m_func(task.m_ctx, task.m_data); }
					catch (...) { PR_ASSERT(PR_DBG, false, "task threw any unhandled exception"); }
					InterlockedDecrement(&m_active_count);
				}
				m_pending.Signal(); // Signal the event when we shutdown so that other threads shutdown too
			}
			
		public:
			
			// Passing 0 causes the thread pool to create 1 thread for each core on the system
			ThreadPool(unsigned int max_thread_count = 0)
			:m_static_ref   (this)
			,m_pending      (FALSE, FALSE)
			,m_tasks        ()
			,m_thread       ()
			,m_shutdown     (false)
			,m_active_count (0)
			{
				if (max_thread_count == 0)
				{
					SYSTEM_INFO sysinfo; ::GetSystemInfo(&sysinfo);
					max_thread_count = sysinfo.dwNumberOfProcessors;
				}
				m_thread.resize(max_thread_count);
				for (int i = 0, iend = int(max_thread_count); i != iend; ++i)
					m_thread[i] = CreateThread(0, 0, ThreadPool::ThreadEntry, this, 0, 0);
			}
			
			~ThreadPool()
			{
				m_tasks.Clear(true);
				m_shutdown = true;
				m_pending.Signal();
				WaitForMultipleObjects((DWORD)m_thread.size(), &m_thread[0], TRUE, INFINITE);
				for (int i = 0, iend = int(m_thread.size()); i != iend; ++i)
					CloseHandle(m_thread[i]);
			}
			
			// Add a user task to the task queue. If threads are available it
			// will begin execution immediately. If not, then it will begin
			// execution when the next thread becomes available
			void QueueTask(TaskFunc func, void* ctx, void* data)
			{
				m_tasks.Enqueue(Task(func, ctx, data));
				m_pending.Signal();
			}
			
			// Return the number of threads currently in the thread pool
			int ThreadCount() const
			{
				return int(m_thread.size());
			}
			
			// Return the number of tasks waiting for a free thread
			// WARNING: indicative only, don't use to create race conditions
			int QueuedTasks() const
			{
				TaskQueue::Lock lock(m_tasks);
				return int(lock.Count());
			}
			
			// Return the number of threads currently processing tasks
			// WARNING: indicative only, don't use to create race conditions
			int RunningTasks() const
			{
				return int(m_active_count);
			}

			// Returns true if there are tasks running or waiting to run
			// WARNING: indicative only, don't use to create race conditions
			bool Busy() const
			{
				return QueuedTasks() || RunningTasks();
			}
		};
	}
}

#pragma warning (default: 4355) // 'this' used in constructor

#endif
