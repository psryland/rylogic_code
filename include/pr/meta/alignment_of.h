//******************************************
// Alignment of
//  Copyright (C) Rylogic Ltd 2010
//******************************************
#pragma once

// <type_traits> was introduced in sp1
#if defined(_MSC_FULL_VER) && _MSC_FULL_VER < 150030729
#error VS2008 SP1 or greater is required to build this file
#endif

#include <cstdio>
#include <type_traits>

namespace pr::meta
{
	#if _MSC_VER >= 1600

	template <typename T>
	using alignment_of = std::alignment_of<T>;

	template <typename T>
	constexpr bool alignment_of_v = std::alignment_of_v<T>;

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

	#endif

	// Return true if 'ptr' is aligned to 'alignment'
	template <size_t Alignment> inline bool is_aligned_to(void const* ptr)
	{
		return ((static_cast<char const*>(ptr) - static_cast<char const*>(0)) % Alignment) == 0;
	}

	// Return true if 'ptr' is correctly aligned for a pointer of type 'T const*'
	template <typename T> inline bool is_aligned(void const* ptr)
	{
		return is_aligned_to<alignment_of<T>::value>(ptr);
	}
}
