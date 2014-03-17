//*********************************************************************
// Concurrent Queue
//  Copyright © Rylogic Ltd 2011
//*********************************************************************
// Thread safe producer/consumer queue
// See unit tests for usage
#pragma once
#ifndef PR_COMMON_CONCURRENT_QUEUE_H
#define PR_COMMON_CONCURRENT_QUEUE_H

#include <deque>
#include <thread>
#include <mutex>
#include <chrono>
#include <condition_variable>

namespace pr
{
	namespace threads
	{
		// Base class for concurrent queues
		struct IConcurrentQueue
		{
		protected:
			typedef std::unique_lock<std::mutex> MLock;

			std::mutex m_mutex;
			std::condition_variable m_cv;
			bool m_last;

			IConcurrentQueue() :m_mutex() ,m_cv() ,m_last(false) {}
			IConcurrentQueue(IConcurrentQueue const&);
			IConcurrentQueue& operator=(IConcurrentQueue const&);

			// Call this after the last item has been added to the queue.
			// Queueing anything after 'm_last' has been set throws an exception
			void LastAdded()
			{
				{
					MLock lock(m_mutex);
					m_last = true;
				}
				m_cv.notify_all();
			}
		};

		template <typename T> class ConcurrentQueue :IConcurrentQueue
		{
			std::deque<T> m_queue;

			ConcurrentQueue(ConcurrentQueue const&);
			ConcurrentQueue& operator=(ConcurrentQueue const&);

		public:
			ConcurrentQueue() {}

			// A scope object for locking the queue
			// Allows enumeration methods while locked
			// Use:
			//   ConcurrentQueue<Blah> queue;
			//   ...
			//   {
			//      ConcurrentQueue<Blah>::Lock lock(queue);
			//      // use 'lock' like a container
			//   }
			class Lock
			{
				ConcurrentQueue<T>& m_owner;
				IConcurrentQueue::MLock m_lock;

				Lock(Lock const&);
				Lock& operator=(Lock const&);

			public:
				std::deque<T>& m_queue;

				explicit Lock(ConcurrentQueue<T>& queue)
					:m_owner(queue)
					,m_lock(m_owner.m_mutex)
					,m_queue(m_owner.m_queue)
				{}
			};

			// Call this after the last item has been added to the queue.
			// Queueing anything after 'm_last' has been set throws an exception
			using IConcurrentQueue::LastAdded;

			// Dequeue blocks until data is available in the queue
			// Returns true if an item was dequeued, or false if no
			// more data will be added to the queue.
			bool Dequeue(T& item)
			{
				MLock lock(m_mutex);
				m_cv.wait(lock, [&](){ return !m_queue.empty() || m_last; });
				if (m_queue.empty()) return false;
				item = m_queue.front();
				m_queue.pop_front();
				return true;
			}

			// Add something to the queue
			void Enqueue(T&& item)
			{
				{
					MLock lock(m_mutex);
					m_queue.push_back(std::move(item));
				}
				m_cv.notify_one();
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/fmt.h"
#include <string>
#include <algorithm>

namespace pr
{
	namespace unittests
	{
		namespace threads
		{
			struct Item
			{
				std::string m_str;
				Item(){}
				Item(char const* name, int idx) :m_str(pr::Fmt("%s%d",name,idx)) {}
			};

			void Produce(char const* name, pr::threads::ConcurrentQueue<Item>& queue)
			{
				for (int i = 0; i != 10; ++i)
					queue.Enqueue(Item(name, i));
			}
			void Consume(pr::threads::ConcurrentQueue<Item>& queue, std::vector<std::string>& items)
			{
				Item item;
				while (queue.Dequeue(item))
					items.push_back(item.m_str);
			}
		}

		PRUnitTest(pr_threads_concurrent_queue)
		{
			using namespace pr::unittests::threads;
			pr::threads::ConcurrentQueue<Item> queue;
			std::vector<std::string> items;

			std::thread t0(Produce, "t0_", std::ref(queue));
			std::thread t1(Produce, "t1_", std::ref(queue));
			std::thread t2(Produce, "t2_", std::ref(queue));
			std::thread t3(Consume, std::ref(queue), std::ref(items));

			t0.join();
			t1.join();

			{
				pr::threads::ConcurrentQueue<Item>::Lock lock(queue);
				auto size = lock.m_queue.size() + items.size();
				PR_CHECK(size >= 20 && size <= 30, true); // since t0,t1 have finished
			}

			t2.join();

			queue.LastAdded();

			t3.join();

			PR_CHECK(items.size(), 30U);
			std::sort(begin(items),end(items));
			for (auto i = 0U; i != items.size(); ++i)
				PR_CHECK(items[i], pr::Fmt("t%d_%d", i/10, i%10));
		}
	}
}
#endif

#endif
