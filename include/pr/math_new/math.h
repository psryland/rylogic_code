//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

// Define switches:
// PR_MATHS_USE_DIRECTMATH
// PR_MATHS_USE_INTRINSICS
// Also remember NOMINMAX

// Notes:
//  - Always pass vectors by value, not by const&. Even if intrinsics are not used
//    the optimiser will pack the float[] into a vector register to pass it to a function call.
//  - Pass matrix types by const&. Basically if the type is larger than 2 registers, pass by const&.

#include "core/forward.h"
#include "core/vector_traits.h"
#include "vector/vector2.h"
#include "vector/vector3.h"
#include "vector/vector4.h"
#include "vector/vector8.h"
// No non-standard dependencies outside of './'

// #include "pr/maths/forward.h"
// #include "pr/maths/constants.h"
// #include "pr/maths/limits.h"
// #include "pr/maths/maths_core.h"
// #include "pr/maths/vector8.h"
// #include "pr/maths/quaternion.h"
// #include "pr/maths/matrix2x2.h"
// #include "pr/maths/matrix3x4.h"
// #include "pr/maths/matrix4x4.h"
// #include "pr/maths/matrix6x8.h"
// #include "pr/maths/matrix.h"
// #include "pr/maths/transform.h"
// #include "pr/maths/bbox.h"
// #include "pr/maths/bsphere.h"
// #include "pr/maths/oriented_box.h"
// #include "pr/maths/rectangle.h"
// #include "pr/maths/plane.h"
// #include "pr/maths/frustum.h"
// #include "pr/maths/axis_id.h"
// #include "pr/maths/conversion.h"
// #include "pr/maths/spline.h"
// #include "pr/maths/line3.h"
// #include "pr/maths/half.h"
// #include "pr/maths/polynomial.h"
// #include "pr/maths/constants_vector.h"

