//********************************************************************
//
//	Enable If.
//
//********************************************************************
// Usage:
//	enable_if_c<bool, ReturnType>::type Function();
// 
// This will compile to "ReturnType Function();" if bool is true and
// not exist if bool is false.
//
// Example:
//	template <typename T>
//	typename enable_if_c<is_pod<T>::value, void>::type do_something_with_pod(T t);
//
//	template <typename T>
//	struct ReleaseWithBool
//	{
//		ReleaseWithBool(bool b) : m_b(b) {}
//		typename enable_if<is_pointer<T>, void>::type operator () (T t) const
//		{
//			t->Release(m_b);
//		}
//		typename disable_if<is_pointer<T>, void>::type operator () (T& t) const
//		{
//			t.Release(m_b);
//		}
//	};
#pragma once
#ifndef PR_META_ENABLE_IF_H
#define PR_META_ENABLE_IF_H

#include <type_traits>
#include "pr/meta/if.h"

namespace pr
{
	namespace mpl
	{
		#if _MSC_VER >= 1600

		template <bool Test, typename Type> struct enable_if :std::enable_if<Test,Type>
		{};
		
		#elif !defined(_MSC_VER) || _MSC_VER > 1300

		template <bool Test, typename Type>
		struct enable_if_c { typedef Type type; };

		template <typename Type>
		struct enable_if_c<false, Type>
		{};

		template <bool Test, typename Type>
		struct enable_if : enable_if_c<Test, Type>
		{};

		//template <bool B, typename ReturnType>
		//struct disable_if_c { typedef ReturnType type; };

		//template <typename ReturnType>
		//struct disable_if_c<true, ReturnType>
		//{};

		//template <typename Expr, typename ReturnType>
		//struct disable_if : disable_if_c<Expr::value, ReturnType>
		//{};

		// The visual studio 7.0 way
		#else

		namespace impl
		{
			template <typename Result>
			struct enabled	{ typedef Result type; };
			struct disabled	{ };
		}

		template <typename Test, typename Result = void>
		struct enable_if_c : if_c<Test, impl::enabled<Result>, impl::disabled>
		{};

		template <typename Test, typename Result = void>
		struct enable_if : if_<Test, impl::enabled<Result>, impl::disabled>
		{};

		template <typename Test, typename Result = void>
		struct disable_if_c : if_c<Test, impl::disabled, impl::enabled<Result> >
		{};

		template <typename Test, typename Result = void>
		struct disable_if : if_<Test, impl::disabled, impl::enabled<Result> >
		{};

		#endif// Not vc7.0
	}
}

#endif
