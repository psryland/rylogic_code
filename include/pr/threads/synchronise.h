//*****************************************************************************************
// Async container
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include <mutex>

namespace pr::threads
{
	// A class for providing thread-safe access to an object
	template <typename Type, typename Mutex = std::mutex>
	struct Synchronise
	{
		using base = Synchronise<Type,Mutex>;

	private:

		union {
		Type const* m_cptr;
		Type*       m_mptr;
		};
		std::lock_guard<Mutex> m_lock;

	public:

		// Make sure your mutex is mutable
		Synchronise(Type const& obj, Mutex& mutex)
			:m_cptr(&obj)
			,m_lock(mutex)
		{}
		Synchronise(Type& obj, Mutex& mutex)
			:m_mptr(&obj)
			,m_lock(mutex)
		{}
		Synchronise(Synchronise&& rhs)
			:m_cptr(rhs.m_cptr)
			,m_lock(std::move(rhs.m_lock))
		{}
		Synchronise(Synchronise const&) = delete;
		Synchronise& operator =(Synchronise&& rhs)
		{
			if (this != &rhs)
			{
				std::swap(m_cobj, rhs.m_cobj);
				std::swap(m_lock, rhs.m_lock);
			}
			return *this;
		}
		Synchronise& operator =(Synchronise const&) = delete;

		// Access the object
		Type const& get() const
		{
			return *m_cptr;
		}
		Type& get()
		{
			return *m_mptr;
		}
	};
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <thread>
#include <chrono>
namespace pr::threads
{
	namespace unittests::synchronise
	{
		using Ints = int[10];

		// An object to be accessed by multiple threads
		class Thing
		{
			Ints m_values;
			std::mutex mutable m_mutex;
			friend struct Lock;
			
		public:

			struct Lock :Synchronise<Thing>
			{
				Lock(Thing const& t)
					:base(t, t.m_mutex)
				{}
				Ints const& values() const
				{
					return get().m_values;
				}
				void set(int value)
				{
					for (auto& i : get().m_values)
					{
						i = value;
						std::this_thread::sleep_for(std::chrono::milliseconds(1));
					}
				}
			};
		};
	}
	PRUnitTest(SynchroniseTests)
	{
		using namespace unittests::synchronise;

		Thing t;
		std::thread t0([&]{ Thing::Lock lock(t); lock.set(1); });
		std::thread t1([&]{ Thing::Lock lock(t); lock.set(2); });
		std::thread t2([&]{ Thing::Lock lock(t); lock.set(3); });

		t0.join();
		t1.join();
		t2.join();

		Thing::Lock lock(t);
		auto& values = lock.values();
		for (auto v : values)
			PR_CHECK(v == values[0], true);
	}
}
#endif
