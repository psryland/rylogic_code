//*************************************************************
// Unit Test for pr::threads::ThreadPool
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/threads/event.h"
#include "pr/threads/thread_pool.h"

SUITE(PRThreadPool)
{
	pr::threads::Event go(TRUE, FALSE);
	pr::threads::Event stop(TRUE, FALSE);
	volatile long running = 0;
	volatile long complete = 0;

	void GlobalDo(void*,void*)
	{
		go.Wait();
		InterlockedIncrement(&running);
		stop.Wait();
		InterlockedDecrement(&running);
	}

	TEST(ThreadPool)
	{
		pr::threads::ThreadPool thread_pool;
		CHECK(thread_pool.ThreadCount() >= 1);
		
		for (int i = 0; i != thread_pool.ThreadCount(); ++i)
			thread_pool.QueueTask(GlobalDo, 0, 0);
		
		for (int i = 0; i != 2; ++i)
			thread_pool.QueueTask(&GlobalDo, 0, 0);
		
		go.Signal();
		for (int i = 0; i != 1000 && running != thread_pool.ThreadCount(); ++i) { Sleep(10); }
		CHECK(running == thread_pool.ThreadCount());
		CHECK(thread_pool.RunningTasks() == running);
		CHECK(thread_pool.QueuedTasks() == 2);
		CHECK(thread_pool.Busy());

		stop.Signal();
		for (int i = 0; i != 1000 && complete != thread_pool.ThreadCount() + 2; ++i) { Sleep(1); }
		CHECK(running == 0);
		CHECK(thread_pool.RunningTasks() == running);
		CHECK(thread_pool.QueuedTasks() == 0);
		CHECK(!thread_pool.Busy());
	}
}
