//*************************************************************
// Unit Test for pr::threads::ThreadPool
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/str/prstring.h"
#include "pr/threads/event.h"
#include "pr/threads/thread.h"
#include "pr/threads/atomic.h"

SUITE(PRThreads)
{
	pr::threads::Event go(TRUE, FALSE);
	volatile long running = 0;
	volatile bool A_running = false;
	volatile bool B_running = false;
	pr::threads::Atom1 carrot;
	
	struct Task :pr::threads::Thread<Task>
	{
		volatile bool& me_running;
		volatile bool& them_running;
		
		Task(int id)
		:me_running(id == 0 ? A_running : B_running)
		,them_running(id == 0 ? B_running : A_running)
		{}
		using pr::threads::Thread<Task>::Start;
		void Main(void*)
		{
			InterlockedIncrement(&running);
			go.Wait();
			
			for (int i = 0; i != 100; ++i)
			{
				pr::threads::Atomic<> lock(carrot);
				me_running = true;
				if (them_running) throw std::exception();
				Sleep(10);
				if (them_running) throw std::exception();
				me_running = false;
			}
			
			InterlockedDecrement(&running);
		}
	};
	
	TEST(Atomic)
	{
		Task t0(0), t1(1);
		t0.Start();
		t1.Start();
		
		for (;running != 2;) Sleep(10);
		go.Signal();
		for (;running != 0;)
		{
			Sleep(10);
			{
				pr::threads::Atomic<> lock(carrot);
				CHECK(!A_running && !B_running);
			}
		}
	}
}
