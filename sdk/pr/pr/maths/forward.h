//*********************************************
// Maths Library
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_MATHS_FORWARD_H
#define PR_MATHS_FORWARD_H

#include "pr/maths/macros.h"
#include "pr/maths/mathsassert.h"

#include <intrin.h>
#include <math.h>
#include <float.h>
#include <stdlib.h>
#include <memory.h>

#if PR_MATHS_USE_D3DX
#  include <d3d9.h> // Required libs: d3d9.lib d3dx9.lib
#  include <d3dx9.h>
#endif

#if PR_MATHS_USE_OPEN_MP
#  include <omp.h>
#  define PR_OMP_PARALLEL     omp parallel
#  define PR_OMP_PARALLEL_FOR omp parallel for
#else
#  define PR_OMP_PARALLEL
#  define PR_OMP_PARALLEL_FOR
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
	struct iv2;
	struct iv4;
	struct Frustum;
	typedef v4 Plane;
}

#endif
