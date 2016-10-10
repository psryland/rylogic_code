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
//  bX*a == bXa^-T (invert then transpose)
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
	template <typename T> struct alignas(16) Vec8
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { v4 ang, lin; };
			struct { v4 v0, v1; };
			struct { float arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
		Vec8(v4_cref v0_, v4_cref v1_)
			:v0(v0_)
			,v1(v1_)
		{}
		Vec8(float _00, float _01, float _02, float _10, float _11, float _12)
			:ang(_00, _01, _02, 0)
			,lin(_10, _11, _12, 0)
		{}

		// Array access
		float const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		float& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
	};
	using v8 = Vec8<void>;
	static_assert(std::is_pod<v8>::value, "v8 must be a pod type");
	static_assert(std::alignment_of<v8>::value == 16, "v8 should have 16 byte alignment");

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
	template <typename T> Vec8<T> operator + (Vec8<T> const& lhs)
	{
		return lhs;
	}
	template <typename T> Vec8<T> operator - (Vec8<T> const& lhs)
	{
		return Vec8<T>(-lhs.v0, -lhs.v1);
	}
	template <typename T> Vec8<T> operator + (Vec8<T> const& lhs, Vec8<T> const& rhs)
	{
		return Vec8<T>(lhs.v0 + rhs.v0, lhs.v1 + rhs.v1);
	}
	template <typename T> Vec8<T> operator - (Vec8<T> const& lhs, Vec8<T> const& rhs)
	{
		return lhs + -rhs;
	}
	template <typename T> Vec8<T> operator * (Vec8<T> const& lhs, float rhs)
	{
		return Vec8<T>(lhs.v0 * rhs, lhs.v1 * rhs);
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

	// Transforms for vectors belonging to dual spaces M and F are related like this:
	//  if   X  takes a vector m to m' in space M
	//  then X* takes a vector f to f' in space F
	//  X* = transpose(inverse(X))

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
	// The dot product is only defined for Dot(v8m,v8f) and Dot(v8f,v8m)
	// e.g Dot(force, velocity) == power delivered
	inline float Dot(v8m const& lhs, v8f const& rhs)
	{
		// v8m and v8f are vectors in the dual spaces M and F
		// A property of dual spaces is dot(m,f) = transpose(m)*f
		return Dot3(lhs.ang, rhs.ang) + Dot3(lhs.lin, rhs.lin);
		// This can't be right: return Dot3(lhs.lin, rhs.ang) + Dot3(lhs.ang, rhs.lin);
	}
	inline float Dot(v8f const& lhs, v8m const& rhs)
	{
		return Dot(rhs,lhs);
	}

	// Spatial cross product.
	// There are two cross product operations, one for motion vectors and one for forces
	template <typename T> inline v8m Cross(Vec8<T> const& lhs, v8m const& rhs)
	{
		return v8m(Cross3(lhs.ang, rhs.ang), Cross3(lhs.ang, rhs.lin) + Cross3(lhs.lin, rhs.ang));
	}
	template <typename T> inline v8f Cross(Vec8<T> const& lhs, v8f const& rhs)
	{
		return v8f(Cross3(lhs.ang, rhs.ang) + Cross3(lhs.lin, rhs.lin), Cross3(lhs.ang, rhs.lin));
	}

	// Shift a spatial velocity measured at some point to that same spatial
	// quantity but measured at a new point given by an offset from the old one.
	inline v8m ShiftVelocityBy(v8m const& vel, v4_cref ofs)
	{
		return v8m(vel.ang, vel.lin + Cross(vel.ang, ofs));
	}

	// Shift a spatial acceleration measured at some point to that same spatial
	// quantity but measured at a new point given by an offset from the old one.
	// The shift in location leaves the angular acceleration the same
	// but results in the linear acceleration changing by: a X r + w X (w X r).
	// 'acc' is the spatial acceleration to shift
	// 'avel' is the angular velocity of the frame in which 'acc' is being shifted
	// 'ofs' is the offset from the last position that 'acc' was measured at.
	inline v8m ShiftAccelerationBy(v8m const& acc, v4_cref avel, v4_cref ofs)
	{
		return v8m(acc.ang, acc.lin + Cross(acc.ang, ofs) + Cross(avel, Cross(avel, ofs)));
	}

	#pragma endregion

	// Spatial matrix. Transforms from 'A' to 'B'
	template <typename A, typename B> struct Mat6x8
	{
		m3x4 m11, m12;
		m3x4 m21, m22;

		Mat6x8() = default;
		Mat6x8(m3x4_cref m11_, m3x4_cref m12_, m3x4_cref m21_, m3x4_cref m22_)
			:m11(m11_)
			,m12(m12_)
			,m21(m21_)
			,m22(m22_)
		{}

		// Reinterpret as a different matrix type
		template <typename A2, typename B2> explicit operator Mat6x8<A2,B2>() const
		{
			return reinterpret_cast<Mat6x8<A2,B2> const&>(*this);
		}
	};
	using m6x8 = Mat6x8<void, void>;
	using m6x8_m2m = Mat6x8<struct Motion, struct Motion>;
	using m6x8_f2f = Mat6x8<struct Force , struct Force >;
	using m6x8_m2f = Mat6x8<struct Motion, struct Force >;
	using m6x8_f2m = Mat6x8<struct Force , struct Motion>;

	// Spatial transforms are a special case of a spatial matrix.
	// They allow a more compact representation.
	template <typename A, typename B> struct Txfm6x8
	{
		m4x4 m_a2b; // Affine transform

		Txfm6x8() = delete;
		Txfm6x8(m4x4 const& a2b)
			:m_a2b(a2b)
		{
			assert(Check());
		}

		// Sanity check
		bool Check() const
		{
			// Check the inertia matrix
			if (!IsAffine(m_a2b))
				return false;

			// Check for any value == NaN
			if (IsNaN(m_a2b))
				return false;

			return true;
		}
	};
	using txfm6x8 = Txfm6x8<void,void>;
	using txfm6x8_m2m = Txfm6x8<struct Motion, struct Motion>;
	using txfm6x8_f2f = Txfm6x8<struct Force,  struct Force>;

	#pragma region Constants
	static m6x8 const m6x8Zero     = {m3x4Zero, m3x4Zero, m3x4Zero, m3x4Zero};
	static m6x8 const m6x8Identity = {m3x4Identity, m3x4Zero, m3x4Zero, m3x4Identity};
	#pragma endregion

	#pragma region Operators
	template <typename A, typename B> inline bool operator == (Mat6x8<A,B> const& lhs, Mat6x8<A,B> const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) == 0;
	}
	template <typename A, typename B> inline bool operator != (Mat6x8<A,B> const& lhs, Mat6x8<A,B> const& rhs)
	{
		return !(lhs == rhs);
	}
	template <typename A, typename B> inline Mat6x8<A,B> operator - (Mat6x8<A,B> const& m)
	{
		return Mat6x8<A,B>(-m.m11, -m.m12, -m.m21, -m.m22);
	}
	template <typename A, typename B> inline Vec8<B> operator * (Mat6x8<A,B> const& lhs, Vec8<A> const& rhs)
	{
		// [m11*a + m12*b] = [m11 m12] [a]
		// [m21*a + m22*b]   [m21 m22] [b]
		return Vec8<B>(
			lhs.m11 * rhs.ang + lhs.m12 * rhs.lin,
			lhs.m21 * rhs.ang + lhs.m22 * rhs.lin);
	}
	template <typename A, typename B, typename C> inline Mat6x8<A,C> operator * (Mat6x8<A,B> const& lhs, Mat6x8<B,C> const& rhs)
	{
		//' [a11 a12] [b11 b12] = [a11*b11 + a12*b21 a11*b12 + a12*b22]
		//' [a21 a22] [b21 b22]   [a21*b11 + a22*b21 a21*b12 + a22*b22]
		return Mat6x8<A,C>(
			lhs.m11*rhs.m11 + lhs.m12*rhs.m21 , lhs.m11*rhs.m12 + lhs.m12*rhs.m22,
			lhs.m21*rhs.m11 + lhs.m22*rhs.m21 , lhs.m21*rhs.m12 + lhs.m22*rhs.m22);
	}
	inline v8m operator * (txfm6x8_m2m const& lhs, v8m const& rhs)
	{
		// Spatial transform for motion vectors:
		//' a2b = [rot 0] * [ 1  0] = [ rot     0 ]
		//'       [0 rot]   [-rx 1]   [-rot*rx rot]
		// So:
		//  a2b * v = [ rot     0 ] * [v.ang] = [rot*v.ang               ]
		//            [-rot*rx rot]   [v.lin]   [rot*v.lin - rot*rx*v.ang]
		return v8m(
			lhs.m_a2b.rot * rhs.ang,
			lhs.m_a2b.rot * rhs.lin - lhs.m_a2b.rot * Cross3(lhs.m_a2b.pos, rhs.ang));
	}
	inline v8f operator * (txfm6x8_f2f const& lhs, v8f const& rhs)
	{
		// Spatial transform for force vectors:
		//' a2b = [rot 0] * [1 -rx] = [rot -rot*rx]
		//'       [0 rot]   [0  1 ]   [ 0    rot  ]
		// So:
		//  a2b * v = [rot -rot*rx] * [v.ang] = [rot*v.ang - rot*rx*v.lin]
		//            [ 0    rot  ]   [v.lin]   [rot*v.lin               ]
		return v8f(
			lhs.m_a2b.rot * rhs.ang - lhs.m_a2b.rot * Cross3(lhs.m_a2b.pos, rhs.lin),
			lhs.m_a2b.rot * rhs.lin);
	}
	#pragma endregion

	#pragma region Functions

	// Compare matrices for floating point equality
	template <typename A, typename B> inline bool FEql(Mat6x8<A,B> const& lhs, Mat6x8<A,B> const& rhs)
	{
		return
			FEql(lhs.m11, rhs.m11) &&
			FEql(lhs.m12, rhs.m12) &&
			FEql(lhs.m21, rhs.m21) &&
			FEql(lhs.m22, rhs.m22);
	}

	// Returns the spatial cross product matrix for 'a', for use with motion vectors.
	//' i.e. b = a x m = cpmM(a) * m, where m is a motion vector
	template <typename T> inline m6x8_m2m CrossM(Vec8<T> const& a)
	{
		auto cx_ang = CPM(a.ang);
		auto cx_lin = CPM(a.lin);
		return m6x8_m2m(cx_ang, m3x4Zero, cx_lin, cx_ang);
	}

	// Returns the spatial cross product matrix for 'a', for use with force vectors.
	// i.e. b = a x* f = cpmF(a) * f, where f is a force vector
	template <typename T> inline m6x8_f2f CrossF(Vec8<T> const& a)
	{
		auto cx_ang = CPM(a.ang);
		auto cx_lin = CPM(a.lin);
		return m6x8_f2f(cx_ang, cx_lin, m3x4Zero, cx_ang);
	}

	// Return the transpose of a spatial matrix
	template <typename A, typename B> inline Mat6x8<A,B> Transpose(Mat6x8<A,B> const& m)
	{
		return Mat6x8<A,B>(
			Transpose(m.m11), Transpose(m.m21),
			Transpose(m.m12), Transpose(m.m22));
	}
	#pragma endregion
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
			{
				v4 vel(1,2,3,0);
				v4 ang(4,3,2,0);
				auto o2w = m4x4::Transform(v4ZAxis, maths::tau_by_4, v4(1,1,1,1));
			}
			{// Cross Products
				auto v  =  v8( 1, 1, 1, 2, 2, 2);
				auto vm = v8m(+1,+2,+3,+4,+5,+6);
				auto vf = v8f(-1,-2,-3,-4,-5,-6);
				
				{
					auto r0 = Cross(vm, vf);
					auto r1 = CrossF(vm) * vf;
					PR_CHECK(FEql(r0, r1), true);
				}
				{
					// vx* == -Transpose(vx)
					auto m0 = CrossM(v); // vx
					auto m1 = CrossF(v); // vx*
					auto m2 = Transpose(m1);
					auto m3 = static_cast<m6x8_m2m>(-m2);
					PR_CHECK(FEql(m0, m3), true);
				}
			}
		}
	}
}
#endif

