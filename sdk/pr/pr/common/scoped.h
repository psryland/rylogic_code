//******************************************
// Scoped
//  Copyright © Rylogic Ltd 2007
//******************************************
// A helper struct for changing the state of a variable when it leaves scope
// Usage:
//	pr::Scoped<bool> doing(m_doing, true, false);

#pragma once
#ifndef PR_SCOPED_H
#define PR_SCOPED_H

namespace pr
{
	template <typename Type>
	struct Scoped
	{
		Type&		m_type;
		Type const& m_leave_state;

		Scoped(Type& type, Type const& in_scope_state, Type const& leave_scope_state)
		:m_type(type)
		,m_leave_state(leave_scope_state)	{ m_type = in_scope_state; }
		~Scoped()							{ m_type = m_leave_state; }
		Scoped(Scoped const&);
		Scoped& operator=(Scoped const&);
	};

	template <typename OnEnterFuncPtr, typename OnExitFuncPtr>
	struct ScopedFunc
	{
		OnExitFuncPtr m_on_exit;
		ScopedFunc(OnEnterFuncPtr on_enter, OnExitFuncPtr on_exit)
		:m_on_exit(on_exit)					{ if( on_enter  ) on_enter(); }
		~ScopedFunc()						{ if( m_on_exit ) m_on_exit(); }
	};
}

#endif
