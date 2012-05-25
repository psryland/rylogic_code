#pragma once
#ifndef PR_META_IF_H
#define PR_META_IF_H

#include <type_traits>

namespace pr
{
	namespace mpl
	{
		#if _MSC_VER >= 1600

		template <bool Test, typename TrueCase, typename FalseCase> struct if_ :std::conditional<Test,TrueCase,FalseCase>
		{};

		// The proper way
		#elif !defined(_MSC_VER) || _MSC_VER > 1300

		template <bool condition, typename true_case, typename false_case>
		struct if_c
		{
			typedef true_case type;
		};

		template <typename true_case, typename false_case>
		struct if_c<false, true_case, false_case>
		{
			typedef false_case type;
		};

		template <typename expression, typename true_case, typename false_case>
		struct if_
		{
			typedef typename if_c<expression::value, true_case, false_case>::type type;
		};

		// The visual studio 7.0 way
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

		#endif// Not vc7.0
	}//namespace mpl
}//namespace pr

#endif//PR_META_IF_H
