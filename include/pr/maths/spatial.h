//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
// Based on Featherstone Rigid Body Dynamics
//
// Spatial algebra uses dual vector spaces M6 and F6 (motion vectors, and force vectors)
// M6 is for velocities, accelerations, etc
// F6 is for forces, moments, momentum, etc.
// Some operators are only defined for (M6,F6), not (M6,M6) or (F6,F6).
//  E.g. Scalar product m.f = work, M6 X F6 => R
// 
// Spatial vectors
// Spatial vectors use 'Plucker coordinates' which are the components in the
// x,y,z directions, and the components of rotation about the x,y,z axes.
// Normally, a rigid body has a linear velocity, v, and an angular velocity, w.
// The spatial velocity is defined as [w,v] = [wx, wy, wz, vx, vy, vz] = [Plucker Coords]
// although, strictly, the order is not important (could be [vx, wx, vy, wy, vz, wz])
// Similarly, the spatial force vector is [T,F] where T = torque, F = linear force.

// Spatial matrices 
//  e.g. Spatial inertia matrix is a mapping from M6 to F6
//
// Spatial transforms are a special case of spatial matrices.
// Spatial transforms have this form:
//   [  R  0]
//   [-d^R R] (d^ represents the cross product matrix associated with the vector d)
// This means a spatial transform can be created from a normal affine transform.
//   m4x4 o2w = [o2w.rot                0      ]
//              [-CPM(o2w.pos)*o2w.rot  o2w.rot] (CPM = cross product matrix)
// If X is a matrix that transforms a to b for M6 vectors, and X* is a matrix that
// performs the same transform for F6 vectors, then X* == X^-T (invert then transpose).
// A spatial transform from A to B for motion vectors = bXa.
// A spatial transform from A to B for force vectors = bX*a.
//  bX*a == bXa^-T (invert then transpose)
// 
// 
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/vector8.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/matrix6x8.h"

namespace pr::maths::spatial
{
	// Spatial vector spaces
	struct Motion {};
	struct Force {};

	// Type traits
	template <typename T> struct is_motion :std::is_same<T, Motion> {};
	template <typename T> struct is_force  :std::is_same<T, Force> {};
	template <typename T> using enable_if_motion = typename std::enable_if<is_motion<T>::value>::type;
	template <typename T> using enable_if_force  = typename std::enable_if<is_force<T>::value>::type;

	// Spatial vector in the motion vector space.
	// Used for: velocity, acceleration, infinitesimal displacement, directions of motion freedom and constraint
	using v8m = Vec8<Motion>;

	// Spatial vector in the force vector space.
	// Used for: momentum, impulse, directions of force freedom and constraint
	using v8f = Vec8<Force>;

	// General Spatial matrix for Motion and Force vector spaces
	// Notes:
	//  - Transforms for vectors belonging to dual spaces M and F are related like this:
	//     if  X  takes a vector m to m' in space M
	//     and X* takes a vector f to f' in space F
	//     then X* = transpose(inverse(X))
	using m6x8m = Mat6x8<Motion, Motion>;
	using m6x8f = Mat6x8<Force, Force>;
	using m6x8mf = Mat6x8<Motion, Force>;
	using m6x8fm = Mat6x8<Force, Motion>;

	#pragma region Operators

	// Transform a spatial motion vector by an affine transform
	inline Vec8<Motion> pr_vectorcall operator * (m4_cref<> lhs, Vec8<Motion> const& rhs)
	{
		// [ E    0] * [v.ang] = [E*v.ang             ]
		// [-E*rx E]   [v.lin]   [E*v.lin - E*rx*v.ang]
		assert("'lhs' is not an affine transform" && IsAffine(lhs));
		auto const& E = lhs.rot;
		auto const& r = lhs.pos;
		return Vec8<Motion>(
			E * rhs.ang,
			E * (rhs.lin - Cross(r, rhs.ang)));
	}

	// Transform a spatial force vector by an affine transform
	inline Vec8<Force> pr_vectorcall operator * (m4_cref<> lhs, Vec8<Force> const& rhs)
	{
		// [E -E*rx] * [v.ang] = [E*v.ang - E*rx*v.lin]
		// [0     E]   [v.lin]   [E*v.lin             ]
		assert("'lhs' is not an affine transform" && IsAffine(lhs));
		auto E = lhs.rot;
		auto r = lhs.pos;
		return Vec8<Force>(
			E * (rhs.ang - Cross(r, rhs.lin)),
			E * rhs.lin);
	}

	#pragma endregion

	#pragma region Functions

	// Spatial dot product
	// The dot product is only defined for Dot(v8m,v8f) and Dot(v8f,v8m).
	// e.g Dot(force, velocity) == power delivered
	inline float Dot(Vec8<Motion> const& lhs, Vec8<Force> const& rhs)
	{
		// v8m and v8f are vectors in the dual spaces M and F
		// A property of dual spaces is dot(m,f) = transpose(m)*f
		return Dot3(lhs.ang, rhs.ang) + Dot3(lhs.lin, rhs.lin);
	}
	inline float Dot(Vec8<Force> const& lhs, Vec8<Motion> const& rhs)
	{
		return Dot(rhs, lhs);
	}

	// Spatial cross product.
	// There are two cross product operations, one for motion vectors (rx) and one for forces (rx*)
	template <typename T> inline Vec8<Motion> Cross(Vec8<T> const& lhs, Vec8<Motion> const& rhs)
	{
		return Vec8<Motion>(Cross3(lhs.ang, rhs.ang), Cross3(lhs.ang, rhs.lin) + Cross3(lhs.lin, rhs.ang));
	}
	template <typename T> inline Vec8<Force> Cross(Vec8<T> const& lhs, Vec8<Force> const& rhs)
	{
		return Vec8<Force>(Cross3(lhs.ang, rhs.ang) + Cross3(lhs.lin, rhs.lin), Cross3(lhs.ang, rhs.lin));
	}

	// Return a motion vector, equal to 'motion', but expressed
	// at a new location equal to the previous location + 'ofs'.
	// This is equivalent to a translation of the vector field.
	inline Vec8<Motion> Shift(Vec8<Motion> const& motion, v4_cref<void> ofs)
	{
		// c.f. RBDS 2.21
		return Vec8<Motion>(motion.ang, motion.lin - Cross(ofs, motion.ang));
	}

	// Return a force vector, equal to 'force', but expressed
	// at a new location equal to the previous location + 'ofs'.
	inline Vec8<Force> Shift(Vec8<Force> const& force, v4_cref<void> ofs)
	{
		// c.f. RBDS 2.22
		return Vec8<Force>(force.ang - Cross(ofs, force.lin), force.lin);
	}

	// Shift a spatial acceleration measured at some point to that same spatial
	// quantity but measured at a new point given by an offset from the old one.
	// The shift in location leaves the angular acceleration the same
	// but results in the linear acceleration changing by: a X r + w X (w X r).
	// 'acc' is the spatial acceleration to shift
	// 'avel' is the angular velocity of the frame in which 'acc' is being shifted
	// 'ofs' is the offset from the last position that 'acc' was measured at.
	inline Vec8<Motion> ShiftAccelerationBy(Vec8<Motion> const& acc, v4_cref<void> avel, v4_cref<void> ofs)
	{
		return Vec8<Motion>(acc.ang, acc.lin + Cross(acc.ang, ofs) + Cross(avel, Cross(avel, ofs)));
	}

	// Returns the spatial cross product matrix for 'a', for use with motion vectors.
	//' i.e. b = a x m = CPM(a) * m, where m is a motion vector
	inline Mat6x8<Motion,Motion> CPM(Vec8<Motion> const& a)
	{
		auto cx_ang = CPM(a.ang);
		auto cx_lin = CPM(a.lin);
		return Mat6x8<Motion, Motion>(cx_ang, m3x4Zero, cx_lin, cx_ang);
	}

	// Returns the spatial cross product matrix for 'a', for use with force vectors.
	// i.e. b = a x* f = CPM(a) * f, where f is a force vector
	inline Mat6x8<Force, Force> CPM(Vec8<Force> const& a)
	{
		auto cx_ang = CPM(a.ang);
		auto cx_lin = CPM(a.lin);
		return Mat6x8<Force, Force>(cx_ang, cx_lin, m3x4Zero, cx_ang);
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(SpatialTests)
	{
		using namespace spatial;

		{
			v4 vel(1,2,3,0);
			v4 ang(4,3,2,0);
			auto o2w = m4x4::Transform(v4ZAxis, float(maths::tau_by_4), v4(1,1,1,1));
		}
		{// Cross Products
			{
				auto v0 = v8m(1, 1, 1, 2, 2, 2);
				auto v1 = v8m(-1, -2, -3, -4, -5, -6);
				auto r0 = Cross(v0, v1);
				auto r1 = CPM(v0) * v1;
				PR_CHECK(FEql(r0, r1), true);
			}
			{
				auto v0 = v8f(1, 1, 1, 2, 2, 2);
				auto v1 = v8f(-1, -2, -3, -4, -5, -6);
				auto r0 = Cross(v0, v1);
				auto r1 = CPM(v0) * v1;
				PR_CHECK(FEql(r0, r1), true);
			}
			{// Test: vx* == -Transpose(vx)
				auto v = v8(-2.3f, +1.3f, 0.9f, -2.2f, 0.0f, -1.0f);

				auto m0 = CPM(static_cast<v8m>(v)); // vx
				auto m1 = CPM(static_cast<v8f>(v)); // vx*
				auto m2 = Transpose(m1);
				auto m3 = static_cast<m6x8m>(-m2);
				PR_CHECK(FEql(m0, m3), true);
			}
		}
	}
}
#endif

