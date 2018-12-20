//******************************************
// Multicast Event
//  Copyright (c) Oct 2011 Rylogic Ltd
//******************************************
#pragma once

#include <list>
#include <mutex>
#include <atomic>
#include <algorithm>

namespace pr
{
	// Map place-holders into pr::
	constexpr auto _1 = std::placeholders::_1;
	constexpr auto _2 = std::placeholders::_2;
	constexpr auto _3 = std::placeholders::_3;
	constexpr auto _4 = std::placeholders::_4;

	// Implementation detail for EventHandler
	namespace evt
	{
		using Id = unsigned long long;
		struct Sub;

		// Non-template interface for all event handlers
		struct IEventHandler
		{
			virtual ~IEventHandler() {} 
			virtual void unsubscribe(Sub& sub) = 0;
		};

		// A reference to an event handler subscription. Used for unsubscribing.
		struct Sub
		{
			IEventHandler* m_evt;
			Id m_id;

			Sub()
				:m_evt()
				,m_id()
			{}
			Sub(IEventHandler* evt, Id id)
				:m_evt(evt)
				,m_id(id)
			{}
			static Sub Make(IEventHandler* evt)
			{
				static std::atomic_uint s_id = {};
				auto id = s_id.load();
				for (;!s_id.compare_exchange_weak(id, id + 1);) {}
				return Sub(evt, id + 1);
			}

			// Boolean test for 'subscribed'
			struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
			operator bool_type() const { return m_evt != nullptr ? &bool_tester::x : static_cast<bool_type>(0); }
		};

		// An RAII event un-subscriber
		struct AutoSub
		{
			Sub m_sub;

			AutoSub()
				:m_sub()
			{}
			AutoSub(Sub sub)
				:m_sub(sub)
			{}
			AutoSub(AutoSub&& rhs)
				:m_sub(rhs.m_sub)
			{
				rhs.m_sub = Sub();
			}
			AutoSub& operator =(AutoSub&& rhs)
			{
				if (this == &rhs) return *this;
				m_sub = rhs.m_sub;
				rhs.m_sub = Sub();
				return *this;
			}
			~AutoSub()
			{
				if (m_sub.m_evt != nullptr)
					m_sub.m_evt->unsubscribe(m_sub);
			}

			AutoSub(AutoSub const& rhs) = delete;
			AutoSub& operator = (AutoSub const& rhs) = delete;
		};
	}

	// EventHandler<>
	// Use:
	//   btn.Click += [&](Button&,EmptyArgs const) {...}
	//   btn.Click += std::bind(&MyDlg::HandleBtn, this, _1, _2);
	//   btn.Click += &MyDlg::HandleBtn;
	template <typename Sender, typename Args>
	struct EventHandler :evt::IEventHandler
	{
		// Notes:
		//  - Subscribing to event handlers is not thread safe. Lock guards should be used.

		// The signature of the event handling function
		using Delegate = std::function<void(Sender,Args)>;
		using AutoSub = evt::AutoSub;
		using Sub = evt::Sub;
		using Id = evt::Id;

		// Wraps a handler function
		struct Handler
		{
			Delegate m_delegate;
			Id m_id;

			Handler(Delegate delegate, Id id)
				:m_delegate(delegate)
				,m_id(id)
			{}
		};
		using HandlerCont = std::vector<Handler>;

		// Subscribed handlers
		HandlerCont m_handlers;

		// Construct
		EventHandler()
			:m_handlers()
		{}
		EventHandler(EventHandler&& rhs)
			:m_handlers(std::move(rhs.m_handlers))
		{}
		EventHandler(EventHandler const&) = delete;
		EventHandler& operator=(EventHandler const&) = delete;

		// Raise the event notifying subscribed observers
		void operator()(Sender& s, Args const& a) const
		{
			// Take a copy in case handlers are changed by handlers
			auto handlers = m_handlers;
			for (auto& h : handlers)
				h.m_delegate(s,a);
		}
		void operator()(Sender& s) const
		{
			(*this)(s, EmptyArgs());
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

		// Assign/Attach/Detach handlers
		Sub operator = (Delegate func)
		{
			reset();
			return *this += func;
		}
		Sub operator += (Delegate func)
		{
			auto sub = Sub::Make(this);
			m_handlers.push_back(Handler(func, sub.m_id));
			return sub;
		}
		void operator -= (Sub& sub)
		{
			// Notes:
			//  - Can't use -= (Delegate function) because std::function<> does not allow operator ==()
			//  - Use stable erase because the order that event handlers are called might be important.
			if (sub)
			{
				auto iter = std::find_if(std::begin(m_handlers), std::end(m_handlers), [=](auto& h){ return h.m_id == sub.m_id; });
				if (iter != std::end(m_handlers))
					m_handlers.erase(iter);

				sub = Sub();
			}
		}

		// Boolean test for no assigned handlers
		struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
		operator bool_type() const { return !m_handlers.empty() ? &bool_tester::x : static_cast<bool_type>(0); }

		// IEventHandler
		void evt::IEventHandler::unsubscribe(Sub& sub)
		{
			*this -= sub;
		}
	};

	// An event subscription
	using EventSub = evt::Sub;

	// An RAII event un-subscriber
	using EventAutoSub = evt::AutoSub;

	// Place-holder for events that take no arguments. (Makes the templating consistent)
	struct EmptyArgs
	{};

	// Event args used in cancel-able operations
	struct CancelEventArgs :EmptyArgs
	{
		bool m_cancel;
		CancelEventArgs(bool cancel = false)
			:m_cancel(cancel)
		{}
	};

	// Event args used to report an error code and error message
	struct ErrorEventArgs :EmptyArgs
	{
		std::wstring m_msg;
		int m_code;

		ErrorEventArgs(std::wstring msg = L"", int code = 0)
			:m_msg(msg)
			,m_code(code)
		{}
	};

	// Event args used to report a change of some value
	template <typename Type> struct ChangeEventArgs :EmptyArgs
	{
		// If 'm_before' is true, this is the old value just prior to being changed.
		// If 'm_before' is false, this is the new value just after being changed.
		Type m_value;

		// True if before the change, false if after
		bool m_before;

		ChangeEventArgs(Type value, bool before)
			:m_value(value)
			,m_before(before)
		{}
		bool before() const { return m_before; }
		bool after() const { return !m_before; }
	};

	// A full-fat, thread-safe, multi-cast delegate
	template <typename FuncType>
	class MultiCast
	{
		using TypeCont = std::vector<FuncType>;

		TypeCont m_cont;
		mutable std::mutex m_cs;
		int m_suspend; // Nesting count of calls to suspend this event
		int m_blocked; // The number of 'Raise' calls that where blocked by being suspended

		// Access 'A' as a reference (independent of whether its a pointer or instance)
		template <typename A> static typename std::enable_if<!std::is_pointer<A>::value, typename std::remove_pointer<A>::type&>::type get(A& i) { return i; }
		template <typename A> static typename std::enable_if< std::is_pointer<A>::value, typename std::remove_pointer<A>::type&>::type get(A& i) { return *i; }

	public:
		typedef typename TypeCont::const_iterator citer;
		typedef typename TypeCont::iterator       iter;

		// A lock context for accessing the handlers
		class Lock
		{
			MultiCast<FuncType>& m_mc;
			std::lock_guard<std::mutex> m_lock;
			Lock(Lock const&) = delete;
			Lock& operator =(Lock const&) = delete;
		public:
			explicit Lock(MultiCast& mc) :m_mc(mc) ,m_lock(mc.m_cs) {}
			citer begin() const { return m_mc.m_cont.begin(); }
			iter  begin()       { return m_mc.m_cont.begin(); }
			citer end() const   { return m_mc.m_cont.end(); }
			iter  end()         { return m_mc.m_cont.end(); }

			// Suspend/Resume this event. 'resume' returns true if events were blocked while suspended
			void suspend()
			{
				++m_mc.m_suspend;
			}
			bool resume() 
			{
				--m_mc.m_suspend;
				assert(m_mc.m_suspend >= 0);
				return m_mc.m_suspend == 0 && m_mc.m_blocked != 0;
			}
		};

		// A reference to a handler and the multicast delegate it's attached to
		struct Handler
		{
			MultiCast<FuncType>* m_mc;
			iter m_iter;
			bool m_auto_detach;
			Handler() :m_mc(nullptr) ,m_iter() ,m_auto_detach(false) {}
			Handler(MultiCast<FuncType>* mc, iter const& it) :m_mc(mc) ,m_iter(it) ,m_auto_detach(false) {}
			~Handler()    { if (m_auto_detach) detach(); }
			void detach() { if (m_mc) *m_mc -= *this; m_mc = nullptr; }
		};

		MultiCast()
			:m_cont()
			,m_cs()
			,m_suspend()
			,m_blocked()
		{}
		MultiCast(MultiCast&& rhs)
			:m_cont(std::move(rhs.m_cont))
			,m_cs()
			,m_suspend(rhs.m_suspend)
			,m_blocked(rhs.m_blocked)
		{}
		MultiCast(MultiCast const& rhs)
			:m_cont(rhs.m_cont)
			,m_cs()
			,m_suspend(rhs.m_suspend)
			,m_blocked(rhs.m_blocked)
		{}
		MultiCast& operator = (MultiCast&& rhs)
		{
			if (this == &rhs) return *this;
			std::swap(m_cont, rhs.m_cont);
			std::swap(m_suspend, rhs.m_suspend);
			std::swap(m_blocked, rhs.m_blocked);
			return *this;
		}
		MultiCast& operator = (MultiCast const& rhs)
		{
			if (this == &rhs) return *this;
			m_cont = rhs.m_cont;
			m_suspend = rhs.m_suspend;
			m_blocked = rhs.m_blocked;
			return *this;
		}

		// Attach/Remove event handlers
		// Note: std::function<> is not equality comparable so if your FuncType
		// is a std::function<> you can only attach, never detach.
		// Unless you record the returned iterator and use that for deletion
		Handler operator = (FuncType handler)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.resize(0);
			m_cont.push_back(handler);
			return Handler(this, --m_cont.end());
		}
		Handler operator += (FuncType handler)
		{
			std::lock_guard<std::mutex> lock(m_cs);
			m_cont.push_back(handler);
			return Handler(this, --m_cont.end());
		}
		void operator -= (FuncType handler)
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

		// Allow comparison to null to test for no handlers
		bool operator == (nullptr_t) const
		{
			std::lock_guard<std::mutex> lock(m_cs);
			return m_cont.empty();
		}
		bool operator != (nullptr_t) const
		{
			return !(*this == nullptr);
		}

		// Attach/Remove event handlers (advanced)
		Handler add_unique(FuncType handler) // Add if not already added
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
			//  2) Copy 'm_cont' and call all handlers (a handler may cause another handler object to be deleted = access violation)
			//  3) Iterate through the handlers assuming 'm_cont' is changing with each call (possible missed handlers, lots of locking)

			// Make a copy of the handlers so that modifying the
			// event during the handlers is allowed.
			TypeCont cont;
			{
				std::lock_guard<std::mutex> lock(m_cs);
				if (m_suspend != 0)
				{
					++m_blocked;
					return;
				}
				cont = m_cont;
				m_blocked = 0;
			}
			for (auto i : cont)
				get(i)(std::forward<Args>(args)...);
		}

		// Raise the event passing 'args' to the event handler
		// 'result' is the combined boolean result of all handlers.
		// If initially 'true', result is the AND'd result of all handlers.
		// If initially 'false', result is the OR'd result of all handlers.
		template <typename... Args> void Raise(bool& result, Args&&... args)
		{
			TypeCont cont;
			{
				std::lock_guard<std::mutex> lock(m_cs);
				if (m_suspend != 0)
				{
					++m_blocked;
					return;
				}
				cont = m_cont;
				m_blocked = 0;
			}

			auto combine_with_and = result;
			for (auto i : cont)
			{
				bool r = get(i)(std::forward<Args>(args)...);
				if (combine_with_and) result &= r;
				else                  result |= r;
			}
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/static_callback.h"
namespace pr::common
{
	namespace unittests::multicast
	{
		struct Thing
		{
			int m_count1;

			Thing() :m_count1() {}
			Thing(Thing const&) = delete;
			Thing& operator=(Thing const&) = delete;
				
			// multicast to function or lambda
			void Call1()
			{
				Call1Happened.Raise(*this);
				Call1Event(*this, EmptyArgs{});
			}
			MultiCast<std::function<void(Thing&)>> Call1Happened;
			EventHandler<Thing&, EmptyArgs const&> Call1Event;

			// multicast to interface
			void Call2() { Call2Happened.Raise(*this); }
			struct ICall2 { virtual void operator()(Thing&) = 0; };
			MultiCast<ICall2*> Call2Happened;

			// multicast to static function
			void Call3() { Call3Happened.Raise(*this); }
			MultiCast<void(*)(Thing&)> Call3Happened;

			// multicast to wrapped static function
			void Call4() { Call4Happened.Raise(*this); }
			MultiCast<StaticCB<void,Thing&>> Call4Happened;
		};

		struct Observer :Thing::ICall2
		{
			int calls;
			Observer() :calls() {}
			void operator()(Thing&) override { ++calls; }
		};
	}
	PRUnitTest(EventHandlerTests)
	{
		using namespace unittests::multicast;

		Thing thg;

		int call1 = 0;
		auto sub = thg.Call1Event += [&](Thing&, EmptyArgs const&) { ++call1; };
		PR_CHECK(call1, 0);
		thg.Call1();
		PR_CHECK(call1, 1);
		thg.Call1Event -= sub;
		thg.Call1();
		PR_CHECK(call1, 1);
	}
	PRUnitTest(MultiCastTests)
	{
		using namespace unittests::multicast;

		Thing thg;
		Observer obs;

		int call1 = 0;
		auto call1_handler = [&](Thing&){ call1++; };
		auto handle = thg.Call1Happened += call1_handler; // use the returned handle for unsubscribing
		PR_CHECK(call1, 0);
		thg.Call1();
		PR_CHECK(call1, 1);
		thg.Call1Happened -= handle;
		thg.Call1();
		PR_CHECK(call1, 1);

		// Careful, this only works if 'self_removing_handler' does not go out of scope.
		MultiCast<std::function<void(Thing&)>>::Handler self_removing_handler;
		self_removing_handler = thg.Call1Happened += [&](Thing&) { call1++; thg.Call1Happened -= self_removing_handler; };
		thg.Call1();
		PR_CHECK(call1, 2);
		thg.Call1();
		PR_CHECK(call1, 2);
		
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

		// Add a callback
		thg.Call4Happened += StaticCallBack(L::Kate, &call4);
		thg.Call4();
		PR_CHECK(call4, 4);
		PR_CHECK(thg.m_count1, 4);

		// The callback has to match including the ctx pointer (i.e. this doesn't remove the callback)
		thg.Call4Happened -= StaticCallBack(L::Kate);
		thg.Call4();
		PR_CHECK(call4, 5);
		PR_CHECK(thg.m_count1, 5);

		// Now it's removed...
		thg.Call4Happened -= StaticCallBack(L::Kate, &call4);
		thg.Call4();
		PR_CHECK(call4, 5);
		PR_CHECK(thg.m_count1, 5);
	}
}
#endif
