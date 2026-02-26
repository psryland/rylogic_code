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
//  - Functions are implemented for vector concepts, not specific types. So that they can be used on
//    any type that conforms to the vector concept

// Design Goals:
//  - Vector types and functions should be independent. Functions should work for any type that meets
//    the vector concept.
//  - Vector types do not contain functions because they would be limited to just that type. They can,
//    however, contain methods that forward to global functions (e.g. Vec3::Length() forwards to Length(Vec3)).

#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/core/axis_id.h"
#include "pr/math_new/core/functions.h"
#include "pr/math_new/types/vector2.h"
#include "pr/math_new/types/vector3.h"
#include "pr/math_new/types/vector4.h"
#include "pr/math_new/types/vector8.h"
#include "pr/math_new/types/quaternion.h"
#include "pr/math_new/types/transform.h"
#include "pr/math_new/types/matrix2x2.h"
#include "pr/math_new/types/matrix3x4.h"
#include "pr/math_new/types/matrix4x4.h"
#include "pr/math_new/types/matrix6x8.h"
#include "pr/math_new/types/matrix.h"
#include "pr/math_new/primitives/bsphere.h"
#include "pr/math_new/primitives/bbox.h"
#include "pr/math_new/primitives/oriented_box.h"
#include "pr/math_new/primitives/plane.h"
#include "pr/math_new/primitives/spline.h"
// No non-standard dependencies outside of './'

// #include "pr/maths/rectangle.h"
// #include "pr/maths/frustum.h"
// #include "pr/maths/conversion.h"
// #include "pr/maths/line3.h"
// #include "pr/maths/half.h"
// #include "pr/maths/polynomial.h"
// #include "pr/maths/constants_vector.h"

// @Copilot, please check each function for mathematical correctness and numerical stability.

// @Copilot, look for patterns that appear to be broken. There should be a sort of symmetry to the code.

// @Copilot, please check this math library for consistency and correctness.

// @Copilot, please look for any missing constexpr

// @Copilot, please look for any missing noexcept, or other qualifiers that could be added to improve the code/performance.

// @Copilot, please check that I've used pr_assert rather than assert

// @Copilot, check for consistency with the design goals

// @Copilot, check that 'pr_vectorcall ' is used for all vector functions, and that it's not used for non-vector functions.

// @Copilot, check that vector types (i.e. Rank1) are passed by value, and that matrix types (i.e. Rank2) are passed by reference

// @Copilot The constants in the vector and matrix types should all be using the functions from 'functions.h'
