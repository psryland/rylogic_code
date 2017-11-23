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
		using func = Ret (__stdcall *)(void*, Args...);

		func m_cb;
		void* m_ctx;

		StaticCB(nullptr_t)
			:StaticCB(nullptr, nullptr)
		{}
		StaticCB(func cb)
			:StaticCB(cb, nullptr)
		{}
		StaticCB(func cb, void* ctx)
			:m_cb(cb)
			,m_ctx(ctx)
		{}
		template <typename... A> Ret operator()(A&&... args)
		{
			return m_cb(m_ctx, std::forward<A>(args)...);
		}

		operator func() const
		{
			return m_cb;
		}

		// Comparisons for call backs
		bool operator == (nullptr_t) const
		{
			return m_cb == nullptr && m_ctx == nullptr;
		}
		bool operator != (nullptr_t) const
		{
			return !(*this == nullptr);
		}
		bool operator == (StaticCB rhs) const
		{
			return m_cb == rhs.m_cb && m_ctx == rhs.m_ctx;
		}
		bool operator != (StaticCB rhs) const
		{
			return !(*this == rhs);
		}
		bool operator <  (StaticCB rhs) const
		{
			return m_cb != rhs.m_cb ? m_cb < rhs.m_cb : m_ctx < rhs.m_ctx;
		}
		bool operator >  (StaticCB rhs) const
		{
			return m_cb != rhs.m_cb ? m_cb > rhs.m_cb : m_ctx > rhs.m_ctx;
		}
		bool operator <= (StaticCB rhs) const
		{
			return m_cb != rhs.m_cb ? m_cb <= rhs.m_cb : m_ctx <= rhs.m_ctx;
		}
		bool operator >= (StaticCB rhs) const
		{
			return m_cb != rhs.m_cb ? m_cb >= rhs.m_cb : m_ctx >= rhs.m_ctx;
		}
	};

	// Create a wrapped static callback function instance
	template <typename Ret, typename... Args> StaticCB<Ret,Args...> StaticCallBack(Ret (__stdcall *cb)(void*,Args...), void* ctx = nullptr)
	{
		return StaticCB<Ret, Args...>(cb, ctx);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_static_callback)
		{
			struct L
			{
				static void __stdcall Func(void* ctx)
				{
					(void)ctx;
				}
			};

			auto cb0 = StaticCallBack(L::Func, (void*)0);
			auto cb1 = StaticCallBack(L::Func, (void*)0);
			auto cb2 = StaticCallBack(L::Func, (void*)1);

			PR_CHECK(cb0 == cb1, true);
			PR_CHECK(cb0 != cb2, true);
			PR_CHECK(cb0 <  cb2, true);
			PR_CHECK(cb0 >  cb2, false);
			PR_CHECK(cb0 <= cb1, true);
			PR_CHECK(cb0 >= cb2, false);
		}
	}
}
#endif
