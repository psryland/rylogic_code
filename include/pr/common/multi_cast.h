//******************************************
// Multicast
//  Copyright (c) Oct 2011 Rylogic Ltd
//******************************************
// See unit tests for usage

#pragma once

#include <list>
#include <mutex>
#include <algorithm>

namespace pr
{
	template <typename Type> class MultiCast
	{
		typedef std::list<Type> TypeCont;

		TypeCont m_cont;
		mutable std::mutex m_cs;

		// Access 'A' as a reference (independent of whether its a pointer or instance)
		template <typename A> static typename std::enable_if<!std::is_pointer<A>::value, typename std::remove_pointer<A>::type&>::type get(A& i) { return i; }
		template <typename A> static typename std::enable_if< std::is_pointer<A>::value, typename std::remove_pointer<A>::type&>::type get(A& i) { return *i; }

	public:
		typedef typename TypeCont::const_iterator citer;
		typedef typename TypeCont::iterator       iter;

		// A lock context for accessing the handlers
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

		// A reference to a handler and the multicast delegate it's attached to
		struct Handler
		{
			MultiCast<Type>* m_mc;
			iter             m_iter;
			bool             m_auto_detach;
			Handler() :m_mc(nullptr) ,m_iter() ,m_auto_detach(false) {}
			Handler(MultiCast<Type>* mc, iter const& it) :m_mc(mc) ,m_iter(it) ,m_auto_detach(false) {}
			~Handler()    { if (m_auto_detach) detach(); }
			void detach() { if (m_mc) *m_mc -= *this; m_mc = nullptr; }
		};

		MultiCast() :m_cont() ,m_cs() {}
		MultiCast(MultiCast&& rhs)
			:m_cont(std::move(rhs.m_cont))
			,m_cs()
		{}
		MultiCast& operator=(MultiCast&& rhs)
		{
			if (&rhs != this)
			{
				std::swap(m_cont, rhs.m_cont);
			}
			return *this;
		}
		MultiCast(MultiCast const&) = delete;
		MultiCast& operator=(MultiCast const&) = delete;

		// Attach/Remove event handlers
		// Note: std::function<> is not equality comparable so if your Type
		// is a std::function<> you can only attach, never detach.
		// Unless you record the returned iterator and use that for deletion
		Handler operator =  (Type handler)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.resize(0);
			m_cont.push_back(handler);
			return Handler(this, --m_cont.end());
		}
		Handler operator += (Type handler)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.push_back(handler);
			return Handler(this, --m_cont.end());
		}
		void operator -= (Type handler)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			auto i = std::find(begin(m_cont), end(m_cont), handler);
			if (i != end(m_cont)) m_cont.erase(i);
		}
		void operator -= (Handler handler)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			if (handler.m_mc != this) throw std::exception("'handler' is not attached to this delegate");
			m_cont.erase(handler.m_iter);
		}

		// Attach/Remove event handlers (advanced)
		Handler add_unique(Type handler) // Add if not already added
		{
			std::lock_guard<std::mutex> lock(m_cs);
			auto i = std::find(begin(m_cont), end(m_cont), handler);
			if (i == end(m_cont)) { m_cont.push_back(handler); i = --end(m_cont); }
			return Handler(this, i);
		}

		// Raise the event passing 'args' to the event handler
		template <typename... Args> void Raise(Args&&... args)
		{
			// Re-entry issues here. Raising the event may result in handlers being added or removed.
			// Three options:
			//  1) Don't allow the event to be modified during 'Raise' (forbids self removing handlers)
			//  2) Copy 'm_cont' and call all handlers (a handler may cause another handler object to be delete = access violation)
			//  3) Iterate through the handlers assuming 'm_cont' is changing with each call (possible missed handlers, lots of locking)

			// Make a copy of the handlers so that modifying the
			// event during the handlers is allowed.
			TypeCont cont;
			{
				std::lock_guard<std::mutex> lock(m_cs);
				cont = m_cont;
			}
			for (auto i : cont)
				get(i)(std::forward<Args>(args)...);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/static_callback.h"
namespace pr
{
	namespace unittests
	{
		namespace multicast
		{
			struct Thing
			{
				int m_count1;

				Thing() :m_count1() {}
				Thing(Thing const&) = delete;
				Thing& operator=(Thing const&) = delete;
				
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

				// multicast to wrapped static function
				void Call4() { Call4Happened.Raise(*this); }
				pr::MultiCast<StaticCB<void,Thing&>> Call4Happened;
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

			struct L {
				static void Bob(Thing& t) { t.m_count1++; }
				static void __stdcall Kate(void* ctx, Thing& t) { *static_cast<int*>(ctx) = ++t.m_count1; }
			};

			thg.Call3Happened += [](Thing& t) { t.m_count1++; };
			thg.Call3Happened += L::Bob;
			thg.Call3();
			PR_CHECK(thg.m_count1, 2);
			thg.Call3Happened -= L::Bob;
			thg.Call3();
			PR_CHECK(thg.m_count1, 3);

			int call4 = 0;
			thg.m_count1 = 3;
			thg.Call4Happened += StaticCallBack(L::Kate, &call4);
			thg.Call4();
			PR_CHECK(call4, 4);
			PR_CHECK(thg.m_count1, 4);
			thg.Call4Happened -= StaticCallBack(L::Kate);
			thg.Call4();
			PR_CHECK(call4, 4);
			PR_CHECK(thg.m_count1, 4);
		}
	}
}
#endif
