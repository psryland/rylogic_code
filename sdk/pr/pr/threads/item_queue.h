//*********************************************************************
// Item Queue
//  Copyright © Rylogic Ltd 2011
//*********************************************************************
// Thread safe producer/consumer queue
// Boiler plate code for the producer thread:
//	Item item;
//	m_queue.Enqueue(item);
//
// Boiler plate code for the consumer thread:
//	while (m_queue.ItemsPending())
//	{
//		Item item;
//		if (!m_queue.Dequeue(item)) { m_queue.Wait(); continue; }
//		// Use 'item'
//	}
//
#ifndef PR_THREADS_ITEM_QUEUE_H
#define PR_THREADS_ITEM_QUEUE_H

// DEPRECATED,
// Use concurrent_queue.h instead

#include <deque>
#include <windows.h>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

namespace pr
{
	namespace threads
	{
		// Base class for all item queues
		struct ItemQueueBase
		{
		protected:
			mutable CRITICAL_SECTION m_cs;             // Sync queue access
			HANDLE                   m_notify;         // An event that is signalled when consumers need notifying
			volatile size_t          m_producer_count; // Count of the producers contributing to this queue
			volatile bool            m_last;           // A flag to signal when no more will be added to the queue

			ItemQueueBase()
			:m_notify(0)
			,m_cs()
			,m_producer_count(0)
			,m_last(false)
			{
				InitializeCriticalSection(&m_cs);
				m_notify = ::CreateEvent(0, FALSE, FALSE, 0);
			}
			virtual ~ItemQueueBase()
			{
				if (m_notify) CloseHandle(m_notify);
				DeleteCriticalSection(&m_cs);
			}
			virtual void RegisterProducer() = 0;
			virtual void UnregisterProducer() = 0;
			virtual bool ItemsPending() const = 0;
			virtual void LastAdded() = 0;
			virtual void Clear(bool final = false) = 0;
			virtual bool Wait(unsigned int timeout = INFINITE) const = 0;
			virtual void Signal() const = 0;

			friend int WaitMultiple(int count, ItemQueueBase const* queues[], bool wait_all, unsigned int timeout_ms);
		};

		// A thread safe producer/consumer queue of 'items'
		template <typename Item> class ItemQueue :public ItemQueueBase
		{
			// Scope helper for critical sections
			struct CSLock
			{
				CRITICAL_SECTION* m_cs;
				CSLock(CRITICAL_SECTION& cs) :m_cs(&cs) { EnterCriticalSection(m_cs); }
				~CSLock()                               { LeaveCriticalSection(m_cs); }
			};

			typedef std::deque<Item> ItemCont;
			ItemCont m_queue; // The queue of items

			ItemQueue(ItemQueue const&); // no copying
			ItemQueue& operator=(ItemQueue const&); // no copying

		public:
			// Scope object for locking the queue, preventing changes to the queue by other threads.
			// Also provides methods for enumerating items in the queue
			class Lock
			{
				CSLock     m_lock;
				ItemQueue& m_queue;
				Lock(Lock const&); // no copying
				Lock& operator=(Lock const&); // no copying

			public:
				explicit Lock(ItemQueue& queue) :m_lock(queue.m_cs) ,m_queue(queue) {}
				explicit Lock(ItemQueue const& queue) :m_lock(queue.m_cs) ,m_queue(const_cast<ItemQueue&>(queue)) {}

				// Operations that can be performed on the queue while this lock exists
				size_t      ProducerCount() const              { return m_queue.m_producer_count; }
				size_t      Count() const                      { return m_queue.m_queue.size(); }
				Item const& GetItem(size_t i) const            { return m_queue.m_queue.at(i); }
				void        Insert(size_t i, Item const& item) { m_queue.m_queue.insert(m_queue.m_queue.begin() + i, item); }
				void        Remove(size_t i)                   { m_queue.m_queue.erase (m_queue.m_queue.begin() + i); }
			};

			// Scope object for producers
			struct Producer
			{
				ItemQueue* m_queue;
				Producer(ItemQueue& queue) :m_queue(&queue) { m_queue->RegisterProducer(); }
				~Producer()                                 { m_queue->UnregisterProducer(); }
			};

			ItemQueue() {}

			// Register a producer source
			void RegisterProducer()
			{
				CSLock cs(m_cs);
				++m_producer_count;
				PR_ASSERT(PR_DBG, !m_last, "Cannot add producers once the last item has been added");
			}

			// Unregister a producer source
			void UnregisterProducer()
			{
				CSLock cs(m_cs);
				PR_ASSERT(PR_DBG, m_producer_count > 0, "Producer register/unregister mismatch");
				if (--m_producer_count == 0) LastAdded();
			}

			// Indicates whether the last item has been added to the queue.
			// If ItemsPending returns false, then there are no more items in the queue and no more will be added
			bool ItemsPending() const
			{
				CSLock cs(m_cs);
				return !m_queue.empty() || !m_last;
			}

			// Call this method after the last item has been added to
			// indicate that no more items will be added to the queue
			void LastAdded()
			{
				CSLock cs(m_cs);
				m_last = true;
				Signal();
			}

			// Reset the queue.
			// 'final' == true signals that no more items will be added
			void Clear(bool final = false)
			{
				CSLock cs(m_cs);
				m_queue.resize(0);
				if (final) LastAdded();
			}

			// Atomic enqueue
			void Enqueue(Item const& item)
			{
				CSLock cs(m_cs);
				PR_ASSERT(PR_DBG, !m_last, "Item added after 'last' flag was set");
				m_queue.push_back(item);
				Signal();
			}

			// Atomic dequeue
			// Returns true if an item was dequeued
			bool Dequeue(Item& item)
			{
				CSLock cs(m_cs);
				if (m_queue.empty()) return false;
				item = m_queue.front();
				m_queue.pop_front();
				return true;
			}

			// Consumer can call this to wait for an item to be added to the queue
			// or to be notified when the queue will stop receiving items.
			bool Wait(unsigned int timeout = INFINITE) const
			{
				if (m_last) { Signal(); return false; }
				DWORD res = WaitForSingleObject(m_notify, timeout);
				PR_ASSERT(PR_DBG, res != WAIT_ABANDONED, "Receiving WAIT_ABANDONED indicates a shutdown logic error");
				if (m_last) { Signal(); } // cascade notifies to all other waiting consumers
				return res == WAIT_OBJECT_0;
			}

			// Manually trigger the notify event for the queue
			void Signal() const
			{
				::SetEvent(m_notify);
			}
		};

		// A consumer can call this to wait on multiple queues.
		// The each queue must be unique.
		// The returned value is:
		//  -1 if a timeout occured
		//  if wait_all is true, 0 is returned when all queues have items ready
		//  if wait_all is false, [0,n) is returned when the i'th queue has an item ready
		inline int WaitMultiple(int count, ItemQueueBase const* queues[], bool wait_all, unsigned int timeout_ms = INFINITE)
		{
			// make an array of notify handles
			HANDLE* handles = static_cast<HANDLE*>(_alloca(count * sizeof(HANDLE)));
			for (int i = 0; i != count; ++i)
			{
				handles[i] = queues[i]->m_notify;
				PR_ASSERT(PR_DBG, !wait_all || queues[i]->ItemsPending(), "WaitAll on a queue that will not receive any more items is an error");
			}

			// wait on them
			DWORD res = WaitForMultipleObjects(count, handles, wait_all, timeout_ms);
			PR_ASSERT(PR_DBG, !(res >= WAIT_ABANDONED_0 && res < WAIT_ABANDONED_0+count), "Receiving WAIT_ABANDONED indicates a shutdown logic error");
			if (res == WAIT_TIMEOUT) return -1;
			if (wait_all) return 0;
			int signalled = (int)(res - WAIT_OBJECT_0);
			if (queues[signalled]->m_last) queues[signalled]->Signal(); // If the woken queue was because of LastAdded(), cascade notifies
			return signalled;
		}
	}
}

#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif

#endif
