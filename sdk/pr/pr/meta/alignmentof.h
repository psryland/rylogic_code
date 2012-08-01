//******************************************
// Alignment of
//  Copyright © Rylogic Ltd 2010
//******************************************

#pragma once
#ifndef PR_META_ALIGNMENT_OF_H
#define PR_META_ALIGNMENT_OF_H

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#include <cstdio>
#include <type_traits>

namespace pr
{
	namespace mpl
	{
		#if _MSC_VER >= 1600
		
		template <typename T> struct alignment_of :std::alignment_of<T>
		{};
		
		#else
		
		template <typename T> struct alignment_of
		{
			#if defined(_MSC_VER)

			static const ::size_t value = __alignof(T);
			
			#else
			
			struct tester { char a; T t; };
			static const ::size_t value = sizeof(tester) - sizeof(T) < sizeof(T) ? sizeof(tester) - sizeof(T) : sizeof(T);

			#endif
		};

		// Return true if 'ptr' is correctly aligned for a pointer of type 'T const*'
		template <typename T> inline bool is_aligned(void const* ptr)
		{
			return ((static_cast<char const*>(ptr) - static_cast<char const*>(0)) % alignment_of<T>::value) == 0;
		}
		
		#endif
	}
}

#endif
