#pragma once

#include <mutex>
#include <condition_variable>
#include <chrono>

namespace pr
{
	namespace threads
	{
		template <typename T> struct WaitState
		{
		private:
			mutable std::mutex m_mutex;
			mutable std::condition_variable m_cv;
			T m_state;

		public:
			WaitState(T initial_state = T()) :m_mutex() ,m_cv() ,m_state(initial_state) {}

			// Block the thread until the internal state becomes 'state' (or timeout)
			bool Wait(T state, int timeout_ms = ~0) const
			{
				std::unique_lock<std::mutex> lock(m_mutex);
				return timeout_ms == ~0
					? m_cv.wait(lock, [&]{ return m_state == state; }), true
					: m_cv.wait_for(lock, std::chrono::milliseconds(timeout_ms), [&]{ return m_state == state; });
			}

			// Set the internal state to 'state', waking up all threads waiting for this state
			void Set(T state)
			{
				std::unique_lock<std::mutex> lock(m_mutex);
				m_state = state;
				m_cv.notify_all();
			}

			// Set the internal state to the result of func(m_state)
			template <typename F> void SetF(F func)
			{
				std::unique_lock<std::mutex> lock(m_mutex);
				m_state = func(m_state);
				m_cv.notify_all();
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <thread>

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_threads_wait_state)
		{
			bool flag = false;
			pr::threads::WaitState<bool> ws(false);

			std::thread thread([&]
			{
				ws.Wait(true);
				flag = true;
				ws.Set(false);
			});

			PR_CHECK(flag, false);

			ws.Set(true);
			ws.Wait(false);

			PR_CHECK(flag, true);
			thread.join();
		}
	}
}
#endif
