//******************************************
// Static Callback
//  Copyright (c) Oct 2011 Rylogic Ltd
//******************************************
// Wraps a static function and a void* context pointer

#pragma once

namespace pr
{
	template <typename Ret, typename... Args> struct StaticCB
	{
		typedef Ret (__stdcall *func)(void*, Args...);

		func m_cb;
		void* m_ctx;

		StaticCB(func cb, void* ctx)
			:m_cb(cb)
			,m_ctx(ctx)
		{}

		template <typename... A> void operator()(A&&... args)
		{
			m_cb(m_ctx, std::forward<A>(args)...);
		}

		bool operator == (StaticCB rhs) const { return m_cb == rhs.m_cb; }
		bool operator != (StaticCB rhs) const { return m_cb != rhs.m_cb; }
		bool operator <  (StaticCB rhs) const { return m_cb <  rhs.m_cb; }
		bool operator <= (StaticCB rhs) const { return m_cb <= rhs.m_cb; }
		bool operator >  (StaticCB rhs) const { return m_cb >  rhs.m_cb; }
		bool operator >= (StaticCB rhs) const { return m_cb >= rhs.m_cb; }
		operator func() const { return m_cb; }
	};

	// Create a wrapped static callback function instance
	template <typename Ret, typename... Args> StaticCB<Ret,Args...> StaticCallBack(Ret (__stdcall *cb)(void*,Args...), void* ctx = nullptr)
	{
		return StaticCB<Ret, Args...>(cb, ctx);
	};
}


