//**************************************************************
// Lambda based RAII object
//  Copyright (C) Rylogic Ltd 2014
//**************************************************************
// Use:
//  auto s = pr::CreateScope(
//     [&]{ m_flag = true; },
//     [&]{ m_flag = false; });

#pragma once

namespace pr
{
	// A scope object
	template <typename Doit, typename Undo> struct Scope
	{
		Doit m_doit;
		Undo m_undo;
		bool m_do_undo;

		Scope(Doit doit, Undo undo)
			:m_doit(doit)
			,m_undo(undo)
			,m_do_undo(true)
		{
			doit();
		}
		~Scope()
		{
			if (!m_do_undo) return;
			m_undo();
		}
		Scope(Scope&& rhs)
			:m_doit(rhs.doit)
			,m_undo(rhs.m_undo)
			,m_do_undo(true)
		{
			rhs.m_do_undo = false;
		}

	private:
		Scope(Scope const&);
		Scope& operator=(Scope const&);
	};

	// Create a scope object from two lambda functions
	template <typename Doit, typename Undo> auto CreateScope(Doit doit, Undo undo) -> Scope<Doit,Undo>
	{
		return Scope<Doit,Undo>(doit,undo);
	}

	// A scope object with state
	template <typename State, typename Doit, typename Undo> struct StateScope
	{
		Doit m_doit;
		Undo m_undo;
		bool m_do_undo;
		State m_state;

		StateScope(Doit doit, Undo undo)
			:m_doit(doit)
			,m_undo(undo)
			,m_do_undo(true)
			,m_state(doit())
		{}
		~StateScope()
		{
			if (!m_do_undo) return;
			m_undo(m_state);
		}
		StateScope(StateScope&& rhs)
			:m_doit(rhs.doit)
			,m_undo(rhs.m_undo)
			,m_do_undo(true)
			,m_state(rhs.m_state)
		{
			rhs.m_do_undo = false;
		}

	private:
		StateScope(StateScope const&);
		StateScope& operator=(StateScope const&);
	};

	// Create a state scope object from two lambda functions
	template <typename Doit, typename Undo> auto CreateStateScope(Doit doit, Undo undo) -> StateScope<decltype(doit()),Doit,Undo>
	{
		return StateScope<decltype(doit()),Doit,Undo>(doit,undo);
	}

}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_scope)
		{
			bool flag = false;
			{
				auto s = pr::CreateScope(
					[&]{ flag = true;  },
					[&]{ flag = false; });

				PR_CHECK(flag, true);
			}
			PR_CHECK(flag, false);

			int value = 1;
			{
				auto s = pr::CreateStateScope(
					[=]{ return value; },
					[&](int i){ value = i; });
				PR_CHECK(s.m_state, 1);

				++value;
				PR_CHECK(value, 2);
			}
			PR_CHECK(value, 1);
		}
	}
}
#endif