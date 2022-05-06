//******************************************
// Static Callback
//  Copyright (c) Oct 2011 Rylogic Ltd
//******************************************
// Wraps a static function and a void* context pointer

#pragma once

namespace pr
{
	template <typename Ret, typename... Args>
	struct StaticCB
	{
		// Notes:
		//  - This type is basically just a pair containing a function pointer and context pointer.
		//    It's makes passing callback functions as parameters easier.
		//  - Remember, parameters can be constructed like this:
		//      void MyFunc(StaticCB<void, int> cb) {...}
		//      MyFunc({func, this});

		using func = Ret(__stdcall*)(void*, Args...);

		func m_cb;
		void* m_ctx;

		StaticCB()
			:StaticCB(nullptr)
		{
		}
		StaticCB(nullptr_t)
			:StaticCB(nullptr, nullptr)
		{
		}
		StaticCB(func cb)
			:StaticCB(cb, nullptr)
		{
		}
		StaticCB(func cb, void* ctx)
			:m_cb(cb)
			, m_ctx(ctx)
		{
		}
		StaticCB(StaticCB& rhs) = default;
		StaticCB(StaticCB const& rhs) = default;
		StaticCB& operator=(StaticCB&& rhs) = default;
		StaticCB& operator=(StaticCB const& rhs) = default;

		template <typename... A> Ret operator()(A&&... args) const
		{
			return m_cb(m_ctx, std::forward<A>(args)...);
		}

		explicit operator bool() const
		{
			return m_cb != nullptr;
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
		friend bool operator == (StaticCB lhs, StaticCB rhs)
		{
			return lhs.m_cb == rhs.m_cb && lhs.m_ctx == rhs.m_ctx;
		}
		friend bool operator != (StaticCB lhs, StaticCB rhs)
		{
			return !(lhs == rhs);
		}
		friend bool operator <  (StaticCB lhs, StaticCB rhs)
		{
			return lhs.m_cb != rhs.m_cb ? lhs.m_cb < rhs.m_cb : lhs.m_ctx < rhs.m_ctx;
		}
		friend bool operator >  (StaticCB lhs, StaticCB rhs)
		{
			return lhs.m_cb != rhs.m_cb ? lhs.m_cb > rhs.m_cb : lhs.m_ctx > rhs.m_ctx;
		}
		friend bool operator <= (StaticCB lhs, StaticCB rhs)
		{
			return lhs.m_cb != rhs.m_cb ? lhs.m_cb <= rhs.m_cb : lhs.m_ctx <= rhs.m_ctx;
		}
		friend bool operator >= (StaticCB lhs, StaticCB rhs)
		{
			return lhs.m_cb != rhs.m_cb ? lhs.m_cb >= rhs.m_cb : lhs.m_ctx >= rhs.m_ctx;
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
namespace pr::common
{
	PRUnitTest(StaticCallbackTests)
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
#endif
