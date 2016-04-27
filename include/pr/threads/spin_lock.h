//*********************************************************************
// Concurrent Queue
//  Copyright (c) Rylogic Ltd 2011
//*********************************************************************

#pragma once
#include <atomic>
#include <thread>
#include <mutex>

#define PR_STACKTRACE 0
#if PR_STACKTRACE
#include <string>
#include <unordered_map>
#include "pr/common/fmt.h"
#include "pr/common/stackdump.h"
#endif

namespace pr
{
	namespace threads
	{
		// Use with std::lock_guard<SpinLock>
		class SpinLock
		{
			std::atomic_bool m_flag;
			std::thread::id m_owner;
			#if PR_STACKTRACE
			std::string m_stack; // call stack when last locked
			#endif
		
			bool try_lock_internal()
			{
				// exchange() returns the previous value, so the lock is
				// acquired when the previous value was false.
				if (m_flag.exchange(true) != 0)
					return false;

				// Save the lock owner
				m_owner = std::this_thread::get_id();

				// Record the call stack when the lock is acquired
				#if PR_STACKTRACE
				m_stack.resize(0);
				pr::StackDump([&](std::string sym, std::string file, int line)
				{
					m_stack.append(pr::FmtS("%s(%d): %s\n", file.c_str(), line, sym.c_str()));
				});
				#endif

				return true;
			}

			SpinLock(SpinLock const&);
			SpinLock& operator=(SpinLock const&);

		public:
			SpinLock()
				:m_flag()
				,m_owner()
			{}

			void lock()
			{
				// Already locked by this thread?
				if (std::this_thread::get_id() == m_owner)
					return;

				// Spin lock. This works because exchange() returns the previous
				// value, the loop exits when the previous value was false.
				for (;!try_lock_internal();)
					std::this_thread::yield();
			}

			bool try_lock()
			{
				// Already locked by this thread?
				if (std::this_thread::get_id() == m_owner)
					return true;

				// exchange() returns the previous value, so the lock is
				// acquired when the previous value was false.
				return try_lock_internal();
			}

			void unlock()
			{
				m_owner = std::thread::id();
				m_flag.exchange(false);
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		namespace spinlock
		{
			struct Thing
			{
				typedef pr::threads::SpinLock SpinLock;

				SpinLock m_flag;
				int m_count;
				std::atomic_int m_calls;

				Thing()
					:m_flag()
					,m_count()
					,m_calls()
				{}
				void Spam()
				{
					std::lock_guard<SpinLock> lock(m_flag);
					m_count = m_count + 1;
					m_count = m_count - 2;
					m_count = m_count + 1;
					++m_calls;
				}
			};
		}

		PRUnitTest(pr_spinlock)
		{
			spinlock::Thing thing;
			std::atomic_bool exit(false);

			std::thread thd1([&]
			{
				while (!exit)
					thing.Spam();
			});
			std::thread thd2([&]
			{
				while (!exit)
					thing.Spam();
			});
			std::thread thd3([&]
			{
				while (!exit)
					thing.Spam();
			});

			while (thing.m_calls.load() < 100)
				std::this_thread::sleep_for(std::chrono::milliseconds(10));

			exit.exchange(true);
			thd1.join();
			thd2.join();
			thd3.join();

			PR_CHECK(thing.m_count, 0);
			PR_CHECK(thing.m_calls >= 100, true);
		}
	}
}
#endif
