#pragma once

#include <mutex>
#include <thread>
#include <condition_variable>
#include <chrono>
#include <cassert>

namespace pr
{
	namespace threads
	{
		// Mix-in class for pause thread functionality
		struct PauseThread
		{
			mutable std::mutex      m_mutex_pause;
			std::condition_variable m_cv_pause;
			size_t                  m_pause_requests;
			bool                    m_paused;

			PauseThread(size_t init_pause_requests = 0)
				:m_mutex_pause()
				,m_cv_pause()
				,m_pause_requests(init_pause_requests)
				,m_paused(false)
			{}

			// Test if the associated thread is paused
			bool IsPaused() const
			{
				std::unique_lock<std::mutex> lock(m_mutex_pause);
				return m_paused;
			}

			// Request the thread to pause/unpause and wait for 'wait_ms' milliseconds for it to happen
			// Returns true if the thread was paused/unpaused within the timeout period
			bool Pause(bool pause, int wait_ms = ~0)
			{
				std::unique_lock<std::mutex> lock(m_mutex_pause);

				m_pause_requests += pause ? 1 : -1;
				assert(m_pause_requests >= 0 && "Pause request mismatch");
				m_cv_pause.notify_all();

				// Wait till paused/unpaused
				return wait_ms == ~0
					? (m_cv_pause.wait(lock, [&]{ return m_paused == pause; }), true)
					: m_cv_pause.wait_for(lock, std::chrono::milliseconds(wait_ms), [&]{ return m_paused == pause; });
			}

			// Unpause the thread regardless of the pause requests count
			// Used for thread shutdown
			void ForceUnpause()
			{
				std::unique_lock<std::mutex> lock(m_mutex_pause);

				m_pause_requests = 0;
				m_cv_pause.notify_all();
			}

			// Checks for pause requests and pauses the current thread until the requests == 0
			// Returns true so that this method can be used in conditionals
			bool TestPaused()
			{
				std::unique_lock<std::mutex> lock(m_mutex_pause);

				// Test if pause requested.
				if (m_pause_requests == 0)
					return true;

				// Notify that the thread is paused
				m_paused = true;
				m_cv_pause.notify_all();

				// Wait till unpaused
				m_cv_pause.wait(lock, [&]{ return m_pause_requests == 0; });

				// Notify that the thread is unpaused
				m_paused = false;
				m_cv_pause.notify_all();
				return true;
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <atomic>
#include "pr/threads/name_thread.h"

namespace pr
{
	namespace unittests
	{
		namespace threads
		{
			struct PauseTestWorker :private pr::threads::PauseThread
			{
				std::atomic_bool m_exit_signalled;
				std::atomic_int m_count;

				PauseTestWorker()
					:m_exit_signalled()
					,m_count()
				{}

				using PauseThread::Pause;
				using PauseThread::IsPaused;

				void Main()
				{
					pr::threads::SetCurrentThreadName("PauseTestWorker");
					for (;!m_exit_signalled && TestPaused(); ++m_count)
						std::this_thread::yield();
				}
			};
		}

		PRUnitTest(pr_threads_pause_thread)
		{
			pr::unittests::threads::PauseTestWorker worker;
			std::thread thread([&]
			{
				worker.Main();
			});

			PR_CHECK(thread.joinable(), true);
			PR_CHECK(worker.IsPaused(), false);

			worker.Pause(true);

			PR_CHECK(worker.IsPaused(), true);

			int count = worker.m_count;
			std::this_thread::sleep_for(std::chrono::milliseconds(10));

			PR_CHECK(worker.IsPaused(), true);
			PR_CHECK(worker.m_count, count);

			worker.Pause(false);

			PR_CHECK(worker.IsPaused(), false);

			for (;worker.m_count == count; std::this_thread::yield()) {}

			PR_CHECK(worker.m_count > count, true);

			worker.m_exit_signalled = true;
			thread.join();
		}
	}
}
#endif
