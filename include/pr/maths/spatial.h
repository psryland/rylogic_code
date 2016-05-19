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
// A spatial transform from A to B for motion vectors = bXa.
// A spatial transform from A to B for force vectors = bX*a.
//  bX*a == bXa^-T (inverse transpose)
// 
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	// An Affine spatial vector
	template <typename T = void> struct alignas(16) Vec8
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { v4 ang, lin; };
			struct { float arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
		Vec8(v4_cref ang_, v4_cref lin_)
			:ang(ang_)
			,lin(lin_)
		{}
	};
	static_assert(std::is_pod<Vec8<>>::value, "v8 must be a pod type");
	static_assert(std::alignment_of<Vec8<>>::value == 16, "v8 should have 16 byte alignment");
	using v8  = Vec8<void>;

	// Spatial motion vector. Used for: velocity, acceleration, infinitesimal displacement, directions of motion freedom and constraint
	using v8m = Vec8<struct Motion>;

	// Spatial force vector. Used for: momentum, impulse, directions of force freedom and constraint
	using v8f = Vec8<struct Force>;

	// Traits
	namespace maths
	{
		template <typename T> struct is_vec<Vec8<T>> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 8;
		};
	}

	// Define component accessors
	template <typename T> inline float x_cp(Vec8<T> const& v) { return v.lin.x; }
	template <typename T> inline float y_cp(Vec8<T> const& v) { return v.lin.y; }
	template <typename T> inline float z_cp(Vec8<T> const& v) { return v.lin.z; }
	template <typename T> inline float w_cp(Vec8<T> const& v) { return v.lin.w; }

	#pragma region Operators
	template <typename T> Vec8<T> operator + (Vec8<T> const& lhs, Vec8<T> const& rhs)
	{
		return Vec8<T>(lhs.ang + rhs.ang, lhs.lin + rhs.lin);
	}
	template <typename T> Vec8<T> operator - (Vec8<T> const& lhs, Vec8<T> const& rhs)
	{
		return Vec8<T>(lhs.ang - rhs.ang, lhs.lin - rhs.lin);
	}
	template <typename T> Vec8<T> operator * (Vec8<T> const& lhs, float rhs)
	{
		return Vec8<T>(lhs.ang * rhs, lhs.lin * rhs);
	}
	template <typename T> Vec8<T> operator * (float lhs, Vec8<T> const& rhs)
	{
		return rhs * lhs;
	}
	template <typename T> Vec8<T>& operator += (Vec8<T>& lhs, Vec8<T> const& rhs)
	{
		return lhs = lhs + rhs;
	}
	template <typename T> Vec8<T>& operator -= (Vec8<T>& lhs, Vec8<T> const& rhs)
	{
		return lhs = lhs + rhs;
	}
	template <typename T> Vec8<T>& operator *= (Vec8<T>& lhs, float rhs)
	{
		return lhs = lhs * rhs;
	}

	// Transform a spatial motion vector by an affine transform
	v8m pr_vectorcall operator * (m4x4_cref lhs, v8m const& rhs)
	{
		// [] = [o2w.rot                0      ] * [ang]
		// []   [-CPM(o2w.pos)*o2w.rot  o2w.rot]   [lin]
		// (CPM = cross product matrix)
		assert("'lhs' is not an affine transform" && IsAffine(lhs));
		auto a = lhs.rot * rhs.ang;
		auto l = lhs.rot * rhs.lin;
		return v8m(a, l - Cross3(lhs.pos, a));
	}

	// Transform a spatial force vector by an affine transform
	v8f pr_vectorcall operator * (m4x4_cref lhs, v8f const& rhs)
	{
		// [] = [o2w.rot  -CPM(o2w.pos)*o2w.rot] * [ang]
		// []   [      0                o2w.rot]   [lin]
		// (CPM = cross product matrix)
		assert("'lhs' is not an affine transform" && IsAffine(lhs));
		auto a = lhs.rot * rhs.ang;
		auto l = lhs.rot * rhs.lin;
		return v8f(a - Cross3(lhs.pos, l), l);
	}

	#pragma endregion

	#pragma region Functions
	
	// Spatial dot product
	// The dot product is only defined for Dot(v8m,v8f) or Dot(v8f,v8m)
	// e.g Dot(force, velocity) == power delivered
	inline float Dot(v8m const& lhs, v8f const& rhs)
	{
		(void)lhs,rhs;
	}
	inline float Dot(v8f const& lhs, v8m const& rhs)
	{
		return Dot(rhs,lhs);
	}

	// Spatial cross product.
	// There are two cross product operations, one for motion vectors and one for forces
	inline v8m Cross(v8m const& lhs, v8m const& rhs)
	{
		return v8m(Cross3(lhs.ang, rhs.ang), Cross3(lhs.ang, rhs.lin) + Cross3(lhs.lin, rhs.ang));
	}
	inline v8f Cross(v8m const& lhs, v8f const& rhs)
	{
		return v8f(Cross3(lhs.ang, rhs.ang) + Cross3(lhs.lin, rhs.lin), Cross3(lhs.ang, rhs.lin));
	}

	#pragma endregion


	// Spatial matrix
	template <typename T> struct Mat6x8
	{
		m3x4 m11, m12;
		m3x4 m21, m22;

		Mat6x8() = default;
	};
	using m6x8m = Mat6x8<struct Motion>;
	using m6x8f = Mat6x8<struct Force>;
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_spatial)
		{
			v4 vel(1,2,3,0);
			v4 ang(4,3,2,0);
			auto o2w = m4x4::Rotation(v4ZAxis, maths::tau_by_4, v4(1,1,1,1));
		}
	}
}
#endif

//// Accessor functions
//void		maSetValueVs(MAv6& v, MAint i, MAreal value);
//bool		maIsZeroVs(const MAv6& v);
//MAreal		maValueVs(const MAv6& v, MAint i);
//const MAv4&	maValueV4Vs(const MAv6& v, MAint i);
//      MAv4&	maValueV4Vs(      MAv6& v, MAint i);
//
//// Manipulation functions
//void		maGetSumVs(MAv6& sum, const MAv6& v1, const MAv6& v2);
//void		maGetScalarMulVs(MAv6& mul, MAreal s, const MAv6& v);
//MAreal		maInnerVs(const MAv6& v1, const MAv6& v2);
//
//
//// Spatial matrix data types and access functions
//// _       _
//// | m1 m2 |
//// |       |
//// | m3 m4 |
//// ¬       ¬
//struct MAm6
//{
//	MAm3 m1;
//	MAm3 m2;
//	MAm3 m3;
//	MAm3 m4;
//};

