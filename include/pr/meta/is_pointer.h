#pragma once
#ifndef PR_META_IS_POINTER_H
#define PR_META_IS_POINTER_H

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

		template <typename T>
		struct is_pointer :std::is_pointer<T>
		{};

		#elif !defined(_MSC_VER) || _MSC_VER > 1300

		template <typename T>
		struct is_pointer		{ enum { value = false }; };

		template <typename T>
		struct is_pointer<T*>	{ enum { value = true }; };

		// The visual studio 7.0 way
		#else

		namespace impl
		{
			namespace is_pointer_impl
			{
				struct yes { char c[2]; };

				template <typename T> yes  Resolve(T*);
				char Resolve(...);

			}//namespace is_pointer_impl
		}//namespace impl

		template <typename T>
		struct is_pointer
		{
			static T& make();
			enum { value = sizeof(impl::is_pointer_impl::Resolve(make())) != 1 };
		};

		#endif// Not vc7.0
	}
}

#endif
