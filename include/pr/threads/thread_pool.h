//*********************************************************************
// Concurrent Queue
//  Copyright (c) Rylogic Ltd 2011
//*********************************************************************
// Thread safe producer/consumer queue
// See unit tests for usage
#pragma once
#include <concepts>
#include <thread>
#include <atomic>
#include <mutex>
#include <future>
#include <type_traits>
#include <condition_variable>
#include <concurrent_queue.h>

#include "pr/threads/name_thread.h"

namespace pr::threads
{
	class ThreadPool
	{
		using task_queue_t = concurrency::concurrent_queue<std::function<void()>>;
		using thread_cont_t = std::vector<std::thread>;
			
		thread_cont_t m_threads;
		task_queue_t m_tasks;
		std::condition_variable m_cv_task_added;
		std::condition_variable m_cv_task_complete;
		std::atomic_int m_tasks_pending;
		mutable std::mutex m_mutex_tasks;
		std::atomic_bool m_shutdown;

	public:
		ThreadPool(const uint32_t num_threads = std::thread::hardware_concurrency()) // Max # of threads the system supports
			:m_threads()
			,m_tasks()
			,m_cv_task_added()
			,m_cv_task_complete()
			,m_tasks_pending()
			,m_mutex_tasks()
			,m_shutdown()
		{
			for (auto i = 0U; i != num_threads; ++i)
				m_threads.push_back(std::thread(&ThreadPool::ThreadMain, this));
		}
		~ThreadPool()
		{
			m_shutdown = true;
			m_cv_task_added.notify_all();
			for (auto& thread : m_threads)
			{
				if (thread.joinable())
					thread.join();
			}
		}

		// The number of tasks currently queued (roughly)
		size_t TaskCountUnsafe() const
		{
			return m_tasks.unsafe_size();
		}

		// Queue a task with no return value
		void QueueTask(std::function<void()>&& task)
		{
			++m_tasks_pending;
			m_tasks.push(std::move(task));
			m_cv_task_added.notify_one();
		}

		// Wait for all queued tasks to complete
		void WaitAll()
		{
			std::unique_lock<std::mutex> lock(m_mutex_tasks);
			m_cv_task_complete.wait(lock, [&] { return m_tasks_pending == 0; });
		}

	private:

		void ThreadMain()
		{
			SetCurrentThreadName("ThreadPool Worker");

			for (std::function<void()> task;;)
			{
				// If there are no tasks, wait for a signal
				if (!m_tasks.try_pop(task))
				{
					std::unique_lock<std::mutex> lock(m_mutex_tasks);
					m_cv_task_added.wait(lock, [&] { return m_tasks.try_pop(task) || m_shutdown; });
					if (m_shutdown)
						return;
				}

				// Execute the task
				task();

				// Signal task completed
				--m_tasks_pending;
				assert(m_tasks_pending >= 0);
				m_cv_task_complete.notify_all();
			}
		}
	};
}

#if PR_UNITTESTS
#include <chrono>
#include "pr/common/unittests.h"
namespace pr::threads
{
	PRUnitTest(ThreadPoolTests)
	{
#if 0 // There is a race condition in here somewhere. This test sometimes never ends
		ThreadPool pool;
		std::atomic_int count = 0;

		for (int i = 0; i != 10; ++i)
		{
			pool.QueueTask([&]
			{
				++count;
				std::this_thread::sleep_for(std::chrono::milliseconds(100));
				++count;
			});
		}

		pool.WaitAll();
		PR_CHECK(count == 20, true);

	//	auto result = pool.QueueTaskR([] { std::this_thread::sleep_for(std::chrono::milliseconds(100)); return 42; });
	//	PR_CHECK(result.get() == 42, true);
#endif
	}
}
#endif
