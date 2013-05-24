//******************************************
// Multicast Event
//  Copyright © Oct 2009 Paul Ryland
//******************************************

#ifndef PR_COMMON_EVENT_H
#define PR_COMMON_EVENT_H

#include <vector>
#include <algorithm>
#include "pr/meta/function_arity.h"

namespace pr
{
	namespace impl
	{
		#define PR_FOREACH_FUNC for (Funcs::const_iterator i = m_funcs.begin(), iend = m_funcs.end(); i != iend; ++i)
		
		template <unsigned int Arity, typename FunctionType>
		struct event;

		template <typename R>
		struct event<0, R()>
		{
			typedef std::vector<R(*)()> Funcs; Funcs m_funcs;
			R operator()() const { R r = R(); PR_FOREACH_FUNC{r=(*i)();} return r; }
		};
 
		template <typename R, typename P0>
		struct event<1, R(P0)>
		{
			typedef std::vector<R(*)(P0)> Funcs; Funcs m_funcs;
			R operator()(P0 p0) const { R r = R(); PR_FOREACH_FUNC{(*i)(p0);} return r; }
		};

		template <typename R, typename P0, typename P1>
		struct event<2, R(P0, P1)>
		{
			typedef std::vector<R(*)(P0, P1)> Funcs; Funcs m_funcs;
			R operator()(P0 p0, P1 p1) const { R r = R(); PR_FOREACH_FUNC{(*i)(p0, p1);} return r; }
		};

		template <typename R, typename P0, typename P1, typename P2>
		struct event<3, R(P0, P1, P2)>
		{
			typedef std::vector<R(*)(P0, P1, P2)> Funcs; Funcs m_funcs;
			R operator()(P0 p0, P1 p1, P2 p2) const { R r = R(); PR_FOREACH_FUNC{(*i)(p0, p1, p2);} return r; }
		};

		template <typename R, typename P0, typename P1, typename P2, typename P3>
		struct event<4, R(P0, P1, P2, P3)>
		{
			typedef std::vector<R(*)(P0, P1, P2, P3)> Funcs; Funcs m_funcs;
			R operator()(P0 p0, P1 p1, P2 p2, P3 p3) const { R r = R(); PR_FOREACH_FUNC{(*i)(p0, p1, p2, p3);} return r; }
		};

		template <typename R, typename P0, typename P1, typename P2, typename P3, typename P4>
		struct event<5, R(P0, P1, P2, P3, P4)>
		{
			typedef std::vector<R(*)(P0, P1, P2, P3, P4)> Funcs; Funcs m_funcs;
			R operator()(P0 p0, P1 p1, P2 p2, P3 p3, P4 p4) const { R r = R(); PR_FOREACH_FUNC{(*i)(p0, p1, p2, p3, p4);} return r; }
		};
		
		#undef PR_FOREACH_FUNC
	}
	
	// C#-style multicast delegate event
	// Use:
	//	struct MyType
	//	{
	//		pr::event<void(int p0, float p2)> OnEvent;
	//	};
	//	void Func(int i, float f) {}
	//	
	//	MyType type;
	//	type.OnEvent += Func;
	//	type.OnEvent(1, 3.14);
	template <typename FunctionType>
	struct event : impl::event<pr::mpl::function_arity<FunctionType>::value, FunctionType>
	{
		struct bool_tester { int x; }; typedef int bool_tester::* bool_type;
		operator bool_type() const
		{
			return !m_funcs.empty() ? &bool_tester::x : static_cast<bool_type>(0);
		}
		size_t count() const
		{
			return m_funcs.size();
		}
		void operator = (FunctionType func)
		{
			m_funcs.clear();
			*this += func; 
		}
		template <typename EventType> void operator = (EventType evt)
		{
			m_funcs.clear();
			*this += evt; 
		}
		void operator += (FunctionType func)
		{
			m_funcs.push_back(func);
		}
		template <typename EventType> void operator += (EventType evt)
		{
			m_funcs.insert(m_funcs.end(), evt.m_funcs.begin(), evt.m_funcs.end());
		}
		void operator -= (FunctionType func)
		{
			Funcs::iterator iter = std::find(m_funcs.begin(), m_funcs.end(), func);
			if (iter != m_funcs.end()) m_funcs.erase(iter);
		}
	};
}

#endif

