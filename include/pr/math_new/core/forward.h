//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include <concepts>
#include <type_traits>
#include <span>
#include <ranges>
#include <limits>
#include <memory>
#include <random>
#include <cmath>
#include <cstdint>
#include <cstdlib>
#include <intrin.h>
#include <immintrin.h>
#include <emmintrin.h>
#include <cassert>
// #include <iterator>
// #include <algorithm>
// #include <thread>
// #include <array>
// #include <stdexcept>
// #include <complex>
// No non-standard dependencies outside of './'

// Use intrinsics by default
#ifndef PR_MATHS_USE_INTRINSICS
#define PR_MATHS_USE_INTRINSICS 1
#endif

// Use 'vectorcall' if intrinsics are enabled
#if PR_MATHS_USE_INTRINSICS
#  define pr_vectorcall __vectorcall
#  pragma intrinsic(sin, cos, tan, asin, acos, atan, atan2, sinh, cosh, tanh, pow, fmod, sqrt, exp, log10, log, abs, fabs, labs, memcmp, memcpy, memset)
#else
#  define pr_vectorcall __fastcall
#endif

// C++11's thread_local
#ifndef thread_local
#define thread_local __declspec(thread)
#endif

// Allow assert handler replacement
#ifndef pr_assert
#define pr_assert(x) assert(x)
//#define pr_assert(condition, message)\
//	do {\
//		if constexpr (std::is_constant_evaluated()) { \
//			if (!(condition)) throw std::logic_error(message); \
//		} else\
//			if (!(condition)) pr_assert_fail(#condition, message, __FILE__, __LINE__); \
//		} \
//	} while(0)
#endif

// C++11's 'alignas'
#if _MSC_VER < 1900
#  ifndef alignas
#    define alignas(alignment) __declspec(align(alignment))
#  endif
#endif
#if _MSC_VER < 1900
#  ifndef constexpr
#    define constexpr
#  endif
#endif

namespace pr
{
}
