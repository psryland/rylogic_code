//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/mass.h"

namespace pr::physics
{
	// Inertia Tensor/Matrix
	// The physical meaning of an inertia is the distribution of a rigid body's 
	// mass about a particular point. If that point is the centre of mass of the 
	// body, then the measured inertia is called the "central inertia" of that body. 
	// To write down the inertia, we need to calculate the six scalars of the inertia 
	// tensor, which is a symmetric 3x3 matrix. These scalars must be expressed in 
	// an arbitrary but specified coordinate system. So an Inertia is meaningful only 
	// in conjunction with a particular set of axes, fixed to the body, whose origin 
	// is the point about which the inertia is being measured, and in whose 
	// coordinate system this measurement is being expressed. Note that changing the 
	// reference point results in a new physical quantity, but changing the reference 
	// axes only affects the measured numbers of that quantity. For any reference 
	// point, there is a unique set of reference axes in which the inertia tensor is
	// diagonal; those are called the "principal axes" of the body at that point, and 
	// the resulting diagonal elements are the "principal moments of inertia". When 
	// we speak of an inertia being "in" a frame, we mean the physical quantity 
	// measured about the frame's origin and then expressed in the frame's axes.
	//
	// InertiaBuilder does not attempt to keep track of which frame it is in.
	// It provides construction and operations involving inertia that can 
	// proceed using only an implicit frame F. Clients of this class are responsible 
	// for keeping track of that frame. In particular, in order to shift the 
	// inertia's "measured-about" point one must know whether either the starting or 
	// final inertia is central, because we must always shift inertias by passing 
	// through the central inertia. So this class provides operations for doing the 
	// shifting, but expects to be told by the client where to find the centre of mass.
	//
	// Changing the coordinate system of an Inertia tensor does not entail a change of
	// physical meaning in the way that shifting it to a different point does. To change
	// coordinates use:
	//    I_frameB = b2a * I_frameA * a2b
	//
	// An Inertia is a symmetric matrix and is positive definite for non-singular bodies
	// (that is, a body composed of at least three non-collinear point masses).
	//
	// Note: inertia scales linearly with mass. This means typically inertia is stored
	// for a unit mass (=1kg) and scaled when needed.
	struct InertiaBuilder
	{
		// Direction for translating an inertia tensor
		enum EOffset
		{
			TowardCoM,   // The pointy end of 'offset' is the CoM
			AwayFromCoM, // The base of 'offset' is the CoM
		};

		m3x4 m;

		InertiaBuilder() = default;

		// Create an inertia matrix from a vector of the moments of inertia (the inertia matrix diagonal)
		// and optionally a vector of the products of inertia (the off-diagonals).
		// Moments are in the order: 'xx, yy, zz'. Products are in the order: 'xy, xz, yz'.
		InertiaBuilder(v4_cref<> moments, v4_cref<> products = v4Zero)
			:m()
		{
			m.x.x = moments.x;
			m.y.y = moments.y;
			m.z.z = moments.z;
			m.x.y = m.y.x = products.x;
			m.x.z = m.z.x = products.y;
			m.y.z = m.z.y = products.z;
			assert(Check());
		}

		// Construct an Inertia from a symmetric 3x3 matrix.
		explicit InertiaBuilder(m3_cref<> rhs)
			:m(rhs)
		{
			assert(Check());
		}

		// Create a principal inertia matrix with identical diagonal elements, like a sphere
		// where moment=2/5 m r^2, or a cube where moment=1/6 m s^2, with m the total mass,
		// r the sphere's radius and s the length of a side of the cube. Note that many rigid
		// bodies of different shapes and masses can have the same inertia matrix.
		explicit InertiaBuilder(float moment)
			:m()
		{
			m.x.x = m.y.y = m.z.z = moment;
			assert(Check());
		}

		// Convertible to m3x4
		operator m3x4 const&() const
		{
			return m;
		}

		// Returns an Inertia, assumed to be in frame A, rotated to frame B using rotation 'a2b'
		InertiaBuilder Rotate(m3_cref<> a2b) const
		{
			return InertiaBuilder(a2b * m * Transpose(a2b));
		}

		// Returns an inertia tensor transformed using the parallel axis theorem.
		// 'offset' is the vector from (or toward) the centre of mass (determined
		// by 'translate_type'). 'offset' must be in the current frame.
		// Note: If this is a unit inertia, remember to translate with 'mass = 1.0'.
		InertiaBuilder Translate(v4_cref<> offset, float mass, EOffset offset_points) const
		{
			// This is basically: I +=/-= PointMassAt(offset)
			if (offset_points == EOffset::TowardCoM)
				mass = -mass;

			auto inertia = m;
			for (auto i = 0; i != 3; ++i)
			{
				for (auto j = i; j != 3; ++j)
				{
					if (i == j)
					{
						// For the diagonal elements:
						//'  I = Io + md^2 (away from CoM), Io = I - md^2 (toward CoM)
						//' 'd' is the perpendicular component of 'offset'
						auto i1 = (i + 1) % 3;
						auto i2 = (i + 2) % 3;
						inertia[i][i] += mass * (offset[i1] * offset[i1] + offset[i2] * offset[i2]);
					}
					else
					{
						// For off diagonal elements:
						//'  Ixy = Ioxy + mdxdy  (away from CoM), Io = I - mdxdy (toward CoM)
						//'  Ixz = Ioxz + mdxdz  (away from CoM), Io = I - mdxdz (toward CoM)
						//'  Iyz = Ioyz + mdydz  (away from CoM), Io = I - mdydz (toward CoM)
						auto delta = mass * (offset[i] * offset[j]);
						inertia[i][j] += delta;
						inertia[j][i] += delta;
					}
				}
			}
			return InertiaBuilder(inertia);
		}

		// Create an Inertia matrix for a point at 'position'
		static InertiaBuilder PointAt(v4_cref<> position)
		{
			auto xx = position.x * position.x;
			auto yy = position.y * position.y;
			auto zz = position.z * position.z;
			auto xy = position.x * position.y;
			auto xz = position.x * position.z;
			auto yz = position.y * position.z;
			return InertiaBuilder(v4(yy+zz, xx+zz, xx+yy, 0), v4(-xy, -xz, -yz, 0));
		}

		// Sanity check
		bool Check() const
		{
			// Check for any value == NaN
			if (IsNaN(m))
				return false;

			float d[] = {m.x.x, m.y.y, m.z.z}; // moments (diagonal)
			float p[] = {m.x.y, m.x.z, m.y.z}; // products (lower triangle)

			// Diagonals of an Inertia matrix must be non-negative
			if (d[0] < 0 || d[1] < 0 || d[2] < 0)
				return false;

			// Diagonals of an Inertia matrix must satisfy the triangle inequality: a + b >= c
			// Might need to relax 'tol' due to distorted rotation matrices: using: 'Max(Sum(d), 1) * tiny_sqrt'
			if (d[0] + d[1] < d[2] ||
				d[1] + d[2] < d[0] ||
				d[2] + d[0] < d[1])
				return false;

			// The magnitude of a product of inertia was too large to be physical.
			if (d[0] < Abs(2 * p[2]) || 
				d[1] < Abs(2 * p[1]) ||
				d[2] < Abs(2 * p[0]))
				return false;

			return true;
		}
	};

	#pragma region InertiaBuilder Operators
	inline InertiaBuilder operator + (InertiaBuilder const& lhs, InertiaBuilder const& rhs)
	{
		// These two inertia matrices must be in the same space
		return InertiaBuilder(lhs.m + rhs.m);
	}
	inline InertiaBuilder operator - (InertiaBuilder const& lhs, InertiaBuilder const& rhs)
	{
		// These two inertia matrices must be in the same space
		return InertiaBuilder(lhs.m - rhs.m);
	}
	inline InertiaBuilder operator * (float lhs, InertiaBuilder const& rhs)
	{
		return InertiaBuilder(lhs * rhs.m);
	}
	inline InertiaBuilder operator * (InertiaBuilder const& lhs, float rhs)
	{
		return InertiaBuilder(lhs.m * rhs);
	}
	inline InertiaBuilder operator / (InertiaBuilder const& lhs, float rhs)
	{
		return lhs * (1.0f / rhs);
	}
	inline v4 operator * (InertiaBuilder const& lhs, v4_cref<> rhs)
	{
		return lhs.m * rhs;
	}
	inline InertiaBuilder& operator += (InertiaBuilder& lhs, InertiaBuilder const& rhs)
	{
		return lhs = lhs + rhs;
	}
	inline InertiaBuilder& operator -= (InertiaBuilder& lhs, InertiaBuilder const& rhs)
	{
		return lhs = lhs - rhs;
	}
	inline InertiaBuilder& operator *= (InertiaBuilder& lhs, float rhs)
	{
		return lhs = lhs * rhs;
	}
	inline InertiaBuilder& operator /= (InertiaBuilder& lhs, float rhs)
	{
		return lhs = lhs / rhs;
	}
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::physics
{
	PRUnitTest(InertiaBuilderTests)
	{
		{
			// Cube with side length = 1 and mass = 1 (unit inertia)
			auto side = 1.0f;
			auto mass = 1.0f;
			auto moment = (1.0f/6.0f) * mass * Sqr(side);
			auto Ic = InertiaBuilder(moment); // unit inertia
			auto Ic2 = Ic;
			
			Ic2 = Ic2.Translate(v4{1,0,0,0}, 1, InertiaBuilder::EOffset::AwayFromCoM);
			Ic2 = Ic2.Rotate(m3x4::Rotation(float(maths::tau_by_4), 0, 0));
			Ic2 = Ic2.Rotate(m3x4::Rotation(0, float(maths::tau_by_4), 0));
			Ic2 = Ic2.Translate(v4{0,0,1,0}, 1, InertiaBuilder::EOffset::TowardCoM);
			
			PR_CHECK(FEql(Ic.m, Ic2.m), true);
		}
	};
}
#endif