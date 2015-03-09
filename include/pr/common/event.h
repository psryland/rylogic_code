//******************************************
// Multicast Event
//  Copyright (c) Oct 2009 Paul Ryland
//******************************************
// C#-style multicast delegate event
// Use:
//   struct MyType
//   {
//      pr::Event<void(int p0, float p2)> OnEvent;
//   };
//   void Func(int i, float f) {}
//
// MyType type;
//   type.OnEvent += Func;
//   type.OnEvent += [](int,float){};
//   type.OnEvent(1, 3.14f);
#pragma once

#include <list>
#include <algorithm>
#include <functional>
#include <thread>
#include <mutex>
#include "pr/meta/function_arity.h"

namespace pr
{
	typedef unsigned long long EventHandlerId;

	namespace impl
	{
		// Notes:
		// std::function<> is not equality comparable.
		// 'm_id' is used so that Events can be copyable.
		// e.g.
		//  struct A { Event evt; } a;
		//  auto handler = a.evt += user_handler;
		//  A b = a;       <- now b.evt() and a.evt() both call user_handler
		//  b -= handler;  <- now only a.evt() calls user_handler
		//  b -= handler;  <- idempotent
		//
		// All events expect functions with return type void
		// since that's all that makes sense for 'multi'-cast delegates

		// Returns a unique identifier
		inline pr::EventHandlerId GenerateEventHandlerId()
		{
			static std::mutex s_event_handler_mutex;
			static pr::EventHandlerId s_id = 0;
			std::lock_guard<std::mutex> lock(s_event_handler_mutex);
			return ++s_id;
		}

		template <int Arity, typename FunctionType>
		struct Event;

		template <>
		struct Event<0, void(void)>
		{
			typedef std::function<void(void)> Delegate;
			struct Func
			{
				Delegate m_delegate;
				pr::EventHandlerId m_id;
				void operator ()() const { m_delegate(); }
				Func(Delegate delegate, pr::EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::list<Func> m_handlers;
			void operator()() const { for (auto& h : m_handlers) h.m_delegate(); }
		};

		template <typename P0>
		struct Event<1, void(P0)>
		{
			typedef std::function<void(P0)> Delegate;
			struct Func
			{
				Delegate m_delegate;
				pr::EventHandlerId m_id;
				void operator ()(P0 p0) const { m_delegate(p0); }
				Func(Delegate delegate, pr::EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::list<Func> m_handlers;
			void operator()(P0 p0) const { for (auto& h : m_handlers) h(p0); }
		};

		template <typename P0, typename P1>
		struct Event<2, void(P0, P1)>
		{
			typedef std::function<void(P0,P1)> Delegate;
			struct Func
			{
				Delegate m_delegate;
				pr::EventHandlerId m_id;
				void operator()(P0 p0, P1 p1) const { m_delegate(p0,p1); }
				Func(Delegate delegate, pr::EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::list<Func> m_handlers;
			void operator()(P0 p0, P1 p1) const { for (auto& h : m_handlers) h(p0,p1); }
		};

		template <typename P0, typename P1, typename P2>
		struct Event<3, void(P0, P1, P2)>
		{
			typedef std::function<void(P0,P1,P2)> Delegate;
			struct Func
			{
				Delegate m_delegate;
				pr::EventHandlerId m_id;
				void operator()(P0 p0, P1 p1, P2 p2) const { m_delegate(p0,p1,p2); }
				Func(Delegate delegate, pr::EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::list<Func> m_handlers;
			void operator()(P0 p0, P1 p1, P2 p2) const { for (auto& h : m_handlers) h(p0,p1,p2); }
		};

		template <typename P0, typename P1, typename P2, typename P3>
		struct Event<4, void(P0, P1, P2, P3)>
		{
			typedef std::function<void(P0,P1,P2,P3)> Delegate;
			struct Func
			{
				Delegate m_delegate;
				pr::EventHandlerId m_id;
				void operator()(P0 p0, P1 p1, P2 p2, P3 p3) const { m_delegate(p0,p1,p2,p3); }
				Func(Delegate delegate, pr::EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::list<Func> m_handlers;
			void operator()(P0 p0, P1 p1, P2 p2, P3 p3) const { for (auto& h : m_handlers) h(p0,p1,p2,p3); }
		};

		template <typename P0, typename P1, typename P2, typename P3, typename P4>
		struct Event<5, void(P0, P1, P2, P3, P4)>
		{
			typedef std::function<void(P0,P1,P2,P3,P4)> Delegate;
			struct Func
			{
				Delegate m_delegate;
				pr::EventHandlerId m_id;
				void operator()(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4) const { m_delegate(p0,p1,p2,p3,p4); }
				Func(Delegate delegate, pr::EventHandlerId id) :m_delegate(delegate) ,m_id(id) {}
			};
			std::list<Func> m_handlers;
			void operator()(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4) const { for (auto& h : m_handlers) h(p0,p1,p2,p3,p4); }
		};
	}

	// Multicast delegate
	template <typename FunctionType>
	struct Event :impl::Event<pr::meta::function_arity<FunctionType>::value, FunctionType>
	{
		struct bool_tester { int x; }; typedef int bool_tester::* bool_type;

		// Boolean test for no assigned handlers
		operator bool_type() const
		{
			return !m_handlers.empty() ? &bool_tester::x : static_cast<bool_type>(0);
		}

		// Detach all handlers. NOTE: this invalidates all associated Handler's
		void reset()
		{
			m_handlers.clear();
		}

		// Number of attached handlers
		size_t count() const
		{
			return m_handlers.size();
		}

		// Append a handler to the event
		pr::EventHandlerId operator += (std::function<FunctionType> func)
		{
			Func handler(func, pr::impl::GenerateEventHandlerId());
			m_handlers.push_back(handler);
			return handler.m_id;
		}
		pr::EventHandlerId operator = (std::function<FunctionType> func)
		{
			reset();
			return *this += func;
		}
		
		// Remove a handler from the event
		void operator -= (pr::EventHandlerId handler_id)
		{
			auto iter = std::find_if(begin(m_handlers), end(m_handlers), [=](Func const& func){ return func.m_id == handler_id; });
			if (iter != end(m_handlers)) m_handlers.erase(iter);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		struct EventTest
		{
			int m_evt0_handled;
			int m_evt5_handled;

			pr::Event<void()>                        OnEvtNoUsed;
			pr::Event<void()>                        OnEvt0;
			pr::Event<void(int)>                     OnEvt1;
			pr::Event<void(int,int)>                 OnEvt2;
			pr::Event<void(int,int,int)>             OnEvt3;
			pr::Event<void(int,int,int,int)>         OnEvt4;
			pr::Event<void(int,int,int,int,int)>     OnEvt5;

			EventTest()
				:m_evt0_handled()
				,m_evt5_handled()
			{
				using namespace std::placeholders;
				OnEvt0 += std::bind(&EventTest::Evt0Handler, this);
				OnEvt5 += std::bind(&EventTest::Evt5Handler, this, _1, _2, _3, _4, _5);
			}

			void RaiseEvents()
			{
				OnEvtNoUsed();
				OnEvt0();
				OnEvt1(1);
				OnEvt2(1,2);
				OnEvt3(1,2,3);
				OnEvt4(1,2,3,4);
				OnEvt5(1,2,3,4,5);
			}

			void Evt0Handler()
			{
				++m_evt0_handled;
			}
			void Evt5Handler(int,int,int,int,int)
			{
				++m_evt5_handled;
			}
		};

		PRUnitTest(pr_common_event)
		{
			EventTest test;

			int r0 = 0;
			int r1[1] = {};
			int r2[2] = {};
			int r3[3] = {};
			int r4[4] = {};
			int r5[5] = {};

			test.OnEvt0 += [&]()                                    { r0 = 1; };
			test.OnEvt1 += [&](int a)                               { r1[0] = a; };
			test.OnEvt2 += [&](int a,int b)                         { r2[0] = a; r2[1] = b; };
			test.OnEvt3 += [&](int a,int b,int c)                   { r3[0] = a; r3[1] = b; r3[2] = c; };
			test.OnEvt4 += [&](int a,int b,int c,int d)             { r4[0] = a; r4[1] = b; r4[2] = c; r4[3] = d; };
			test.OnEvt5 += [&](int a,int b,int c,int d,int e)       { r5[0] = a; r5[1] = b; r5[2] = c; r5[3] = d; r5[4] = e; };
			test.RaiseEvents();

			PR_CHECK(r0, 1);
			for (int i = 0; i != 1; ++i) PR_CHECK(r1[i], i+1);
			for (int i = 0; i != 2; ++i) PR_CHECK(r2[i], i+1);
			for (int i = 0; i != 3; ++i) PR_CHECK(r3[i], i+1);
			for (int i = 0; i != 4; ++i) PR_CHECK(r4[i], i+1);
			for (int i = 0; i != 5; ++i) PR_CHECK(r5[i], i+1);
			
			int x = 0;
			auto handler = test.OnEvt0 += [&]() { x = 42; };
			test.RaiseEvents();
			PR_CHECK(x, 42);
			x = 0;
			test.OnEvt0 -= handler;
			test.RaiseEvents();
			PR_CHECK(x, 0);

			int y = 0;
			auto handler2 = test.OnEvt1 += [&](int a){ y += a; };
			
			// Test copyable
			EventTest test2 = test;
			test.RaiseEvents();
			PR_CHECK(y, 1);
			test2.RaiseEvents();
			PR_CHECK(y, 2);
			test.OnEvt1 -= handler2;
			test.RaiseEvents();
			test2.RaiseEvents();
			PR_CHECK(y, 3);
			
			// Test idempotent remove
			test.OnEvt1 -= handler2;
			test2.OnEvt1 -= handler2;
			test.RaiseEvents();
			test2.RaiseEvents();
			PR_CHECK(y, 3);

			PR_CHECK(test.m_evt0_handled, 9);
			PR_CHECK(test.m_evt5_handled, 9);
		}
	}
}
#endif
