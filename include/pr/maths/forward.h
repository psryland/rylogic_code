//*********************************************
// Maths Library
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_MATHS_FORWARD_H
#define PR_MATHS_FORWARD_H

#include <iterator>
#include <algorithm>
#include <thread>
#include <limits>
#include <cassert>
#include <type_traits>
#include <intrin.h>
#include <math.h>
#include <float.h>
#include <stdlib.h>
#include <memory.h>

// Libraries built to use directmath should be fine when linked in projects
// that don't use directmath because all of the maths types have the same
// size/alignment requirements reguardless.

// Select directmath if already included
#ifndef PR_MATHS_USE_DIRECTMATH
#  if defined(DIRECTX_MATH_VERSION)
#    define PR_MATHS_USE_DIRECTMATH 1
#  else
#    define PR_MATHS_USE_DIRECTMATH 0
#  endif
#endif
#if PR_MATHS_USE_DIRECTMATH
#  include <directxmath.h>
#endif

// Use intrinsics by default
#ifndef PR_MATHS_USE_INTRINSICS
#define PR_MATHS_USE_INTRINSICS 1
#endif
#if PR_MATHS_USE_INTRINSICS
#  pragma intrinsic(sin, cos, tan, asin, acos, atan, atan2, sinh, cosh, tanh, pow, fmod, sqrt, exp, log10, log, abs, fabs, labs, memcmp, memcpy, memset)
#endif

// C++11's thread_local
#ifndef thread_local
#define thread_local __declspec(thread)
#endif

// C++11's alignas
#ifndef alignas
#define alignas(alignment) __declspec(align(alignment))
#endif

namespace pr
{
	// Copied from "pr/common/PRTypes.h" to remove the dependency
	typedef          char      int8;
	typedef unsigned char     uint8;
	typedef          short    int16;
	typedef unsigned short   uint16;
	typedef          int      int32;
	typedef unsigned int     uint32;
	typedef          __int64  int64;
	typedef unsigned __int64 uint64;
	typedef unsigned int       uint;
	typedef unsigned long     ulong;

	struct v2;
	struct v3;
	struct v4;
	struct m2x2;
	struct m3x4;
	struct m4x4;
	struct Quat;
	struct BBox;
	struct OBox;
	struct BSphere;
	struct Line3;
	struct FRect;
	struct IRect;
	struct ISize;
	struct iv2;
	struct iv4;
	struct Frustum;
	typedef v4 Plane;

	namespace maths
	{
		// Test alignment of 't'
		template <typename T, int A> inline bool is_aligned(T const* t)
		{
			return (reinterpret_cast<char const*>(t) - static_cast<char const*>(nullptr)) % A == 0;
		}
		template <typename T> inline bool is_aligned(T const* t)
		{
			return is_aligned<T, std::alignment_of<T>::value>(t);
		}
	}
}

#endif
