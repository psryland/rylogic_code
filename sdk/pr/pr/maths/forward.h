//*********************************************
// Maths Library
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_MATHS_FORWARD_H
#define PR_MATHS_FORWARD_H

#include "pr/maths/mathsassert.h"
#include <intrin.h>
#include <math.h>
#include <float.h>
#include <stdlib.h>
#include <memory.h>

#define PR_MATHS_USE_D3DX ERROR_FIX_PLEASE
#define PR_MATHS_USE_OPEN_MP ERROR_FIX_PLEASE
#define PR_OMP_PARALLEL ERROR_FIX_PLEASE
#define PR_OMP_PARALLEL_FOR ERROR_FIX_PLEASE

#ifndef PR_MATHS_USE_DIRECTMATH
#  if defined(DIRECTX_MATH_VERSION)
#    define PR_MATHS_USE_DIRECTMATH 1
#  else
#    define PR_MATHS_USE_DIRECTMATH 0
#  endif
#endif

#ifndef PR_MATHS_USE_INTRINSICS
#define PR_MATHS_USE_INTRINSICS 1
#endif

#if PR_MATHS_USE_DIRECTMATH
#  include <directxmath.h>
#endif

#if PR_MATHS_USE_INTRINSICS
#  pragma intrinsic(sin, cos, tan, asin, acos, atan, atan2, sinh, cosh, tanh, pow, fmod, sqrt, exp, log10, log, abs, fabs, labs, memcmp, memcpy, memset)
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
	struct m3x3;
	struct m4x4;
	struct Quat;
	struct BoundingBox;
	struct OrientedBox;
	struct BoundingSphere;
	struct Line3;
	struct FRect;
	struct IRect;
	struct ISize;
	struct iv2;
	struct iv4;
	struct Frustum;
	typedef v4 Plane;
}

#endif
