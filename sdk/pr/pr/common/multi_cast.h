//******************************************
// Multicast
//  Copyright (c) Oct 2011 Rylogic Ltd
//******************************************
// See unit tests for usage

#pragma once

#include <list>
#include <mutex>

namespace pr
{
	template <typename Type> class MultiCast
	{
		typedef std::list<Type> TypeCont;
		TypeCont m_cont;
		mutable std::mutex m_cs;

		// Access 'A' as a reference (independant of whether its a pointer or instance)
		template <typename A> static typename std::enable_if<!std::is_pointer<A>::value, typename std::remove_pointer<A>::type&>::type get(A& i) { return i; }
		template <typename A> static typename std::enable_if< std::is_pointer<A>::value, typename std::remove_pointer<A>::type&>::type get(A& i) { return *i; }

	public:
		typedef typename TypeCont::const_iterator citer;
		typedef typename TypeCont::iterator       iter;

		// A lock context for accessing the clients
		class Lock
		{
			MultiCast<Type>& m_mc;
			std::lock_guard<std::mutex> m_lock;
			Lock(Lock const&);
			Lock& operator =(Lock const&);
		public:
			Lock(MultiCast& mc) :m_mc(mc) ,m_lock(mc.m_cs) {}
			citer begin() const { return m_mc.m_cont.begin(); }
			iter  begin()       { return m_mc.m_cont.begin(); }
			citer end() const   { return m_mc.m_cont.end(); }
			iter  end()         { return m_mc.m_cont.end(); }
		};

		// Attach/Remove event handlers
		// Note: std::function<> is not equality comparable so if your Type
		// is a std::function<> you can only attach, never detach.
		// Unless you record the returned iterator and use that for deletion
		iter operator =  (Type client)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.resize(0);
			m_cont.push_back(client);
			return --m_cont.end();
		}
		iter operator += (Type client)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.push_back(client);
			return --m_cont.end();
		}
		void operator -= (Type client)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			for (auto i = m_cont.begin(), iend = m_cont.end(); i != iend; ++i)
				if (*i == client) { m_cont.erase(i); break; }
		}
		void operator -= (iter at)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.erase(at);
		}

		// Raise the event passing 'args' to the event handler
		template <typename... Args> void Raise(Args&&... args)
		{
			Lock lock(*this);
			for (auto i : lock)
				get(i)(std::forward<Args>(args)...);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		namespace multicast
		{
			struct Thing
			{
				int m_count1;

				Thing()
					:m_count1()
				{}
				
				// multicast to function or lambda
				void Call1() { Call1Happened.Raise(*this); }
				pr::MultiCast<std::function<void(Thing&)>> Call1Happened;

				// multicast to interface
				void Call2() { Call2Happened.Raise(*this); }
				struct ICall2 { virtual void operator()(Thing&) = 0; };
				pr::MultiCast<ICall2*> Call2Happened;

				// multicast to static function
				void Call3() { Call3Happened.Raise(*this); }
				pr::MultiCast<void(*)(Thing&)> Call3Happened;
			};

			struct Observer :Thing::ICall2
			{
				int calls;
				Observer() :calls() {}
				void Thing::ICall2::operator()(Thing&) override { ++calls; }
			};
		}

		PRUnitTest(pr_multicast)
		{
			using namespace pr::unittests::multicast;
			Thing thg;
			Observer obs;

			int call1 = 0;
			auto call1_handler = [&](Thing&){ call1++; };
			auto handle = thg.Call1Happened += call1_handler; // use the returned handle for unsubscribing
			thg.Call1();
			PR_CHECK(call1, 1);
			thg.Call1Happened -= handle;
			thg.Call1();
			PR_CHECK(call1, 1);

			thg.Call2Happened += &obs;
			thg.Call2();
			thg.Call2();
			PR_CHECK(obs.calls, 2);
			thg.Call2Happened -= &obs;
			thg.Call2();
			PR_CHECK(obs.calls, 2);

			struct L { static void Bob(Thing& t) { t.m_count1++; } };
			thg.Call3Happened += [](Thing& t) { t.m_count1++; };
			thg.Call3Happened += L::Bob;
			thg.Call3();
			PR_CHECK(thg.m_count1, 2);
			thg.Call3Happened -= L::Bob;
			thg.Call3();
			PR_CHECK(thg.m_count1, 3);
		}
	}
}
#endif
