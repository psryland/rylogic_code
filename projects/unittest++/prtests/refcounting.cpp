//*************************************************************
// Unit Test for pr::threads::ThreadPool
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/threads/thread_pool.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"

SUITE(PRRefCounting)
{
	struct Thing :pr::RefCount<Thing>
	{
		bool m_on_stack;
		bool* m_deleted;
		
		Thing(bool on_stack, bool* deleted)
		:m_on_stack(on_stack)
		,m_deleted(deleted)
		{}
		
		static void RefCountZero(pr::RefCount<Thing>* doomed)
		{
			Thing* thing = static_cast<Thing*>(doomed);
			*thing->m_deleted = true;
			if (!thing->m_on_stack)
				pr::RefCount<Thing>::RefCountZero(thing);
		}
	};

	struct Derived :Thing
	{
		Derived(bool on_stack, bool* deleted)
		:Thing(on_stack, deleted)
		{}
	};

	volatile bool shutdown = false;
	volatile long running = 0;
	void Thread(void* thing,void*)
	{
		pr::RefPtr<Thing> ptr(static_cast<Thing*>(thing));
		::InterlockedIncrement(&running);
		while (!shutdown) Sleep(0);
		::InterlockedDecrement(&running);
	}

	TEST(RefCountingStackObject)
	{
		bool deleted = false;
		Thing thing(true, &deleted);
		{
			pr::RefPtr<Thing> ptr(&thing);
			CHECK(ptr.RefCount() == 1);

			{// Asynchronous use of the ref pointer
				pr::threads::ThreadPool thread_pool;
				for (int i = 0; i != thread_pool.ThreadCount(); ++i)
					thread_pool.QueueTask(Thread, &thing, 0);
			
				while (running != thread_pool.ThreadCount()) Sleep(0);

				CHECK(ptr.RefCount() == 1 + thread_pool.ThreadCount());

				shutdown = true;
			}

			CHECK(ptr.RefCount() == 1);
		}
		CHECK(thing.m_ref_count == 0);
		CHECK(deleted);
	}
	TEST(RefCountingHeapObject)
	{
		bool deleted = false;
		{
			pr::RefPtr<Thing> ptr(new Thing(false, &deleted));
			CHECK(ptr.RefCount() == 1);
			{
				pr::RefPtr<Thing> ptr2 = ptr;
				CHECK(ptr2.RefCount() == 2);
			}
			CHECK(ptr.RefCount() == 1);
		}
		CHECK(deleted);
	}
	TEST(ImplicitCast)
	{
		bool deleted = false;
		{
			pr::RefPtr<Derived> derived = new Derived(false, &deleted);
			
			// implicit cast to base in construction
			pr::RefPtr<Thing> base = derived;
			CHECK_EQUAL(2, base.RefCount());

			// implicit cast in assignment
			pr::RefPtr<Thing> base2;
			base2 = derived;
			CHECK_EQUAL(3, base.RefCount());
		}
		CHECK(deleted);
	}
}
