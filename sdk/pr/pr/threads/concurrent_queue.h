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
			typedef std::unique_lock<std::mutex> MLock;

		protected:
			std::mutex& m_mutex;
			std::condition_variable m_cv_added;
			std::condition_variable m_cv_empty;
			bool m_last;

			IConcurrentQueue(std::mutex& mutex) :m_mutex(mutex) ,m_cv_added() ,m_cv_empty() ,m_last(false) {}
			IConcurrentQueue(IConcurrentQueue const&);
			IConcurrentQueue& operator=(IConcurrentQueue const&);

			// Call this after the last item has been added to the queue.
			// Queueing anything after 'm_last' has been set throws an exception
			void LastAdded()
			{
				MLock lock(m_mutex);
				m_last = true;
				m_cv_added.notify_all();
				m_cv_empty.notify_all();
			}
		};

		// Concurrent queue implementation.
		// Caller provides the mutex on which the queue is synchronised
		template <typename T> struct ConcurrentQueue :IConcurrentQueue
		{
		protected:

			std::deque<T> m_queue;

			ConcurrentQueue(ConcurrentQueue const&);
			ConcurrentQueue& operator=(ConcurrentQueue const&);

		public:

			explicit ConcurrentQueue(std::mutex& mutex) :IConcurrentQueue(mutex) {}

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

			// Tests if the LastAdded flag is set and the queue is empty
			bool Exhausted() const
			{
				MLock lock(m_mutex);
				return m_last && m_queue.empty();
			}

			// Call this after the last item has been added to the queue.
			// Queueing anything after 'm_last' has been set throws an exception
			void LastAdded()
			{
				IConcurrentQueue::LastAdded();
			}

			// Dequeue blocks until data is available in the queue
			// Returns true if an item was dequeued, or false if no
			// more data will be added to the queue.
			bool Dequeue(T& item, MLock& lock, int timeout_ms = ~0)
			{
				// Notify before we block. Waiting threads won't see 'm_queue'
				// as empty unless we actually wait (which releases the lock)
				if (m_queue.empty())
					m_cv_empty.notify_all();
				
				// Wait for an item to dequeue
				if (timeout_ms == ~0)
					m_cv_added.wait(lock, [&]{ return !m_queue.empty() || m_last; });
				else
					m_cv_added.wait_for(lock, std::chrono::milliseconds(timeout_ms), [&]{ return !m_queue.empty() || m_last; });

				// Timeout or last added
				if (m_queue.empty())
					return false;
				
				// Pop the queued item
				item = m_queue.front();
				m_queue.pop_front();
				return true;
			}
			bool Dequeue(T& item, int timeout_ms = ~0)
			{
				MLock lock(m_mutex);
				return Dequeue(item, lock, timeout_ms);
			}

			// Add something to the queue
			void Enqueue(T&& item, MLock&)
			{
				m_queue.push_back(std::move(item));
				m_cv_added.notify_one();
			}
			void Enqueue(T&& item)
			{
				MLock lock(m_mutex);
				Enqueue(std::forward<T>(item), lock);
			}
			void Enqueue(T const& item, MLock&)
			{
				m_queue.push_back(item);
				m_cv_added.notify_one();
			}
			void Enqueue(T const& item)
			{
				MLock lock(m_mutex);
				Enqueue(item, lock);
			}

			// Block until the queue is empty
			// WARNING: don't assume this means the consumer has finished
			// processing the last item removed from the queue.
			void Flush()
			{
				MLock lock(m_mutex);
				m_cv_empty.wait(lock, [&]{ return m_queue.empty(); });
			}
		};
	
		// Concurrent queue that provides it's own mutex
		template <typename T> struct ConcurrentQueue2 :ConcurrentQueue<T>
		{
			std::mutex m_mutex;
			ConcurrentQueue2() :ConcurrentQueue<T>(m_mutex) {}
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

			std::mutex mutex;
			pr::threads::ConcurrentQueue<Item> queue(mutex);
			std::vector<std::string> items;

			std::thread t0(Produce, "t0_", std::ref(queue));
			std::thread t1(Produce, "t1_", std::ref(queue));
			std::thread t2(Produce, "t2_", std::ref(queue));

			t0.join();
			t1.join();
			{
				pr::threads::ConcurrentQueue<Item>::Lock lock(queue);
				auto size = lock.m_queue.size() + items.size();
				PR_CHECK(size >= 20 && size <= 30, true); // since t0,t1 have finished
			}

			// Start consuming
			std::thread t3(Consume, std::ref(queue), std::ref(items));

			// Finish adding
			t2.join();
			queue.LastAdded();

			// Finish consuming
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
