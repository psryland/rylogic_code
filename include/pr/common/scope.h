//**************************************************************
// Lambda based RAII object
//  Copyright (C) Rylogic Ltd 2014
//**************************************************************
// Use:
//  auto s = pr::Scope<void>(
//     [&]{ m_flag = true; },
//     [&]{ m_flag = false; });
//
#pragma once
#include <functional>
#include <type_traits>
#include <concepts>

namespace pr
{
	template <typename State>
	struct Scope
	{
		// Notes:
		//  - It is possible to implement this with templates and not std::function but it makes the return
		//    impossible to name.
		using doit_t = std::function<State()>;
		using undo_t = std::function<void(State)>;
		using state_t = State;

		state_t m_state;
		undo_t m_undo;
		bool m_dont;

		Scope(state_t const& state, undo_t undo)
			:m_state(state)
			,m_undo(undo)
			,m_dont(false)
		{
		}
		Scope(doit_t doit, undo_t undo)
			:m_state(doit())
			,m_undo(undo)
			,m_dont(false)
		{
		}
		~Scope()
		{
			if (m_dont) return;
			m_undo(m_state);
		}
		Scope(Scope&& rhs)
			:m_state(std::move(rhs.m_state))
			, m_undo(std::move(rhs.m_undo))
			, m_dont(rhs.m_dont)
		{
			rhs.m_dont = true;
		}
		Scope& operator = (Scope&& rhs)
		{
			if (this == &rhs) return *this;
			std::swap(m_state, rhs.m_state);
			std::swap(m_undo, rhs.m_undo);
			std::swap(m_dont, rhs.m_dont);
			return *this;
		}
		Scope(Scope const&) = delete;
		Scope& operator = (Scope const&) = delete;
	};

	template <>
	struct Scope<void>
	{
		using doit_t = std::function<void()>;
		using undo_t = std::function<void()>;

		undo_t m_undo;
		bool m_dont;

		explicit Scope(undo_t undo)
			:m_undo(undo)
			,m_dont(false)
		{}
		Scope(doit_t doit, undo_t undo)
			:m_undo(undo)
			,m_dont(false)
		{
			doit();
		}
		~Scope()
		{
			if (m_dont) return;
			m_undo();
		}
		Scope(Scope&& rhs)
			:m_undo(std::move(rhs.m_undo))
			,m_dont(rhs.m_dont)
		{
			rhs.m_dont = true;
		}
		Scope& operator = (Scope&& rhs)
		{
			if (this == &rhs) return *this;
			std::swap(m_undo, rhs.m_undo);
			std::swap(m_dont, rhs.m_dont);
			return *this;
		}
		Scope(Scope const&) = delete;
		Scope& operator = (Scope const&) = delete;
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTest(ScopeTests)
	{
		bool flag = false;
		{
			auto s = Scope<void>(
				[&]{ flag = true;  },
				[&]{ flag = false; });

			PR_CHECK(flag, true);
		}
		PR_CHECK(flag, false);

		int value = 1;
		{
			auto s = Scope<int>(
				[=]{ return value; },
				[&](int i){ value = i; });
			PR_CHECK(s.m_state, 1);

			++value;
			PR_CHECK(value, 2);
		}
		PR_CHECK(value, 1);
	}
}
#endif