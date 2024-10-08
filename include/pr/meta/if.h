#pragma once
#ifndef PR_META_IF_H
#define PR_META_IF_H

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#include <type_traits>

namespace pr
{
	namespace meta
	{
		#if _MSC_VER >= 1600

		template <bool Test, typename TrueCase, typename FalseCase> struct if_ :std::conditional<Test,TrueCase,FalseCase>
		{};

		#elif !defined(_MSC_VER) || _MSC_VER > 1300

		template <bool expression, typename true_case, typename false_case>
		struct if_
		{
			typedef true_case type;
		};

		template <typename true_case, typename false_case>
		struct if_<false, true_case, false_case>
		{
			typedef false_case type;
		};

		#else

		namespace impl
		{
			namespace if_impl
			{
				template <bool condition>
				struct Impl
				{
					template <typename true_case, typename false_case>
					struct Impl2
					{
						typedef true_case type;
					};
				};

				template <>
				struct Impl<false>
				{
					template <typename true_case, typename false_case>
					struct Impl2
					{
						typedef false_case type;
					};
				};
			}// namespace if_impl
		}//namespace impl

		template <bool condition, typename true_case, typename false_case>
		struct if_c
		{
			typedef typename impl::if_impl::Impl<condition>::template Impl2<true_case, false_case>::type type;
		};

		template <typename expression, typename true_case, typename false_case>
		struct if_
		{
			typedef typename impl::if_impl::Impl<expression::value>::template Impl2<true_case, false_case>::type type;
		};

		#endif
	}
}

#endif
