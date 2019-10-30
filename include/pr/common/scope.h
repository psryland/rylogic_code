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
			:m_doit(rhs.m_doit)
			,m_undo(rhs.m_undo)
			,m_do_undo(true)
		{
			rhs.m_do_undo = false;
		}
		Scope& operator = (Scope&& rhs)
		{
			if (this != &rhs)
			{
				std::swap(m_doit, rhs.m_doit);
				std::swap(m_undo, rhs.m_undo);
				std::swap(m_do_undo, rhs.m_do_undo);
			}
			return *this;
		}
		Scope(Scope const&) = delete;
		Scope& operator = (Scope const&) = delete;
	};

	// Create a scope object from two lambda functions
	template <typename Doit, typename Undo> Scope<Doit,Undo> CreateScope(Doit doit, Undo undo)
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
		StateScope& operator = (StateScope&& rhs)
		{
			if (this != &rhs)
			{
				std::swap(m_doit, rhs.m_doit);
				std::swap(m_undo, rhs.m_undo);
				std::swap(m_do_undo, rhs.m_do_undo);
				std::swap(m_state, rhs.m_state);
			}
			return *this;
		}
		StateScope(StateScope const&) = delete;
		StateScope& operator = (StateScope const&) = delete;
	};

	// Create a state scope object from two lambda functions
	template <typename Doit, typename Undo> auto CreateStateScope(Doit doit, Undo undo) -> StateScope<decltype(doit()),Doit,Undo>
	{
		return StateScope<decltype(doit()),Doit,Undo>(doit,undo);
	}

	// Create a scope that calls a lambda at exit
	template <typename CleanUp> auto AtExit(CleanUp cleanup)
	{
		return CreateScope([] {}, cleanup);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(ScopeTests)
	{
		bool flag = false;
		{
			auto s = CreateScope(
				[&]{ flag = true;  },
				[&]{ flag = false; });

			PR_CHECK(flag, true);
		}
		PR_CHECK(flag, false);

		int value = 1;
		{
			auto s = CreateStateScope(
				[=]{ return value; },
				[&](int i){ value = i; });
			PR_CHECK(s.m_state, 1);

			++value;
			PR_CHECK(value, 2);
		}
		PR_CHECK(value, 1);

		flag = false;
		{
			flag = true;
			auto s = AtExit([&] { flag = false; });
			PR_CHECK(flag, true);
		}
		PR_CHECK(flag, false);
	}
}
#endif