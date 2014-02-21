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
	template <typename Doit, typename Undo> Scope<Doit,Undo> CreateScope(Doit doit, Undo undo)
	{
		return Scope<Doit,Undo>(doit,undo);
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
		}
	}
}
#endif