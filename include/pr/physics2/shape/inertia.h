//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"

namespace pr
{
	namespace physics
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

			// Construct an Inertia from a symmetric 3x3 matrix.
			explicit InertiaBuilder(m3_cref<> rhs) :m(rhs)
			{
				assert(Check());
			}

			// Create a principal inertia matrix with identical diagonal elements, like a sphere
			// where moment=2/5 m r^2, or a cube where moment=1/6 m s^2, with m the total mass,
			// r the sphere's radius and s the length of a side of the cube. Note that many rigid
			// bodies of different shapes and masses can have the same inertia matrix.
			explicit InertiaBuilder(float moment) :m()
			{
				m.x.x = m.y.y = m.z.z = moment;
				assert(Check());
			}

			// Create an inertia matrix from a vector of the moments of inertia (the inertia matrix diagonal)
			// and optionally a vector of the products of inertia (the off-diagonals).
			// Moments are in the order: 'xx, yy, zz'. Products are in the order: 'xy, xz, yz'.
			explicit InertiaBuilder(v4_cref<> moments, v4_cref<> products = v4Zero) :m()
			{
				m.x.x = moments.x;
				m.y.y = moments.x;
				m.z.z = moments.x;
				m.x.y = m.y.x = products.x;
				m.x.z = m.z.x = products.y;
				m.y.z = m.z.y = products.z;
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
			InertiaBuilder Translate(v4_cref<> offset, float mass, EOffset offset_points)
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

		// Inertia
		// Inertia may be usefully viewed as a symmetric spatial matrix, that is, 
		// a 6x6 symmetric matrix arranged as 2x2 blocks of 3x3 matrices. This type represents
		// the spatial inertia in compact form. In spatial matrix form, the matrix has the following
		// interpretation:
		//'     M = mass * [ Io cx ]
		//'                [ -cx 1 ]
		//   where 'c' is the vector from the centre of mass to the point where 'Io' is measured,
		//   'cx' is the cross product matrix formed from 'c'. (note: '-cx == Transpose(cx)')
		//   'Io' is the inertia of the body measured at point 'c'.
		// The Inertia at 'c' ('Ic') is related to the inertia at 'o' ('Io') as follows:
		//     Io = [ Ic + m*cx*cx^T m*cx]    (m = mass, cx = CPM(c), cx^T = transpose(CPM(c))
		//          [ m*cx^T         m*1 ]
		// Notes:
		//  Mass is included in Inertia so that they can be combined with other Spatial inertias.
		//  The inertia matrix is symmetric, so we don't need to store the full matrix.
		struct Inertia
		{
			v4      m_diagonal; // The Ixx, Iyy, Izz terms of the unit inertia tensor
			v4      m_offdiags; // The Ixy, Ixz, Iyz terms of the unit inertia tensor
			v4      m_com_and_mass; // Location of the object centre of mass in the frame that the unit inertia tensor was calculated in.

			Inertia() = default;
			
			// Construct from unit inertia calculated at 'o' (i.e. Io')
			Inertia(m3_cref<> unit_inertia, v4_cref<> com, float mass)
				:m_diagonal(unit_inertia.x.x, unit_inertia.y.y, unit_inertia.z.z, 0)
				,m_offdiags(unit_inertia.x.y, unit_inertia.x.z, unit_inertia.y.z, 0)
				,m_com_and_mass(com.x, com.y, com.z, mass)
			{
				assert(Check());
			}

			// The mass to scale the inertia by
			float Mass() const
			{
				return m_com_and_mass.w;
			}
			void Mass(float mass)
			{
				m_com_and_mass.w = mass;
			}

			// Location of the object centre of mass in the frame that the inertia tensor was calculated in.
			v4 CoM() const
			{
				return m_com_and_mass.w0();
			}
			void CoM(v4 com)
			{
				m_com_and_mass.xyz = com.xyz;
			}

			// The mass weighted distance to the centre of mass
			v4 MassMoment() const
			{
				return Mass() * m_com_and_mass.w0();
			}

			// The mass-scaled inertia matrix
			m3x4 ToUnitInertia() const
			{
				return m3x4(
					v4(m_diagonal.x, m_offdiags.x, m_offdiags.y, 0),
					v4(m_offdiags.x, m_diagonal.y, m_offdiags.z, 0),
					v4(m_offdiags.y, m_offdiags.z, m_diagonal.z, 0));
			}

			// Return the inertia matrix as a full spatial matrix
			m6x8mf ToSpatialMatrix() const
			{
				auto mass = Mass();
				auto com = CoM();
				auto mcx = CPM(mass * com);
				return m6x8mf(mass * ToUnitInertia(), mcx, -mcx, m3x4::Scale(mass));
			}

			// Sanity check
			bool Check() const
			{
				// Check the inertia matrix
				if (!InertiaBuilder(ToUnitInertia()).Check())
					return false;

				// Check for any value == NaN
				if (IsNaN(CoM()) || IsNaN(Mass()))
					return false;

				return true;
			}
		};

		#pragma region Inertia Operators
		
		// Add two inertias. 'lhs' and 'rhs' must be in the same frame.
		inline Inertia operator + (Inertia const& lhs, Inertia const& rhs)
		{
			// When two rigid bodies are joined, you can just add the spatial inertia directly.
			Inertia sum = {};
			sum.m_com_and_mass = lhs.m_com_and_mass + rhs.m_com_and_mass;
			sum.m_diagonal = lhs.m_diagonal + rhs.m_diagonal;
			sum.m_offdiags = lhs.m_offdiags + rhs.m_offdiags;
			return sum;
		}
		
		// Multiply a spatial motion vector by a spatial inertia matrix.
		inline v8f operator * (Inertia const& inertia, v8m const& motion)
		{
			// Typically 'motion' is a velocity or an acceleration.
			// e.g.
			//   I = spatial inertia
			//   v = spatial velocity
			//   h = spatial momentum = I * v
			//   T = kinetic energy = 0.5 * Dot(v, I*v)

			//' h = mass * [Io  cx] * [ang]
			//'            [-cx  1]   [lin]
			return inertia.Mass() * v8f(
				inertia.ToUnitInertia() * motion.ang + Cross3(inertia.CoM(), motion.lin),
				motion.lin - Cross3(inertia.CoM(), motion.ang));
		}

		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::physics
{
	PRUnitTest(InertiaTests)
	{
		auto avel = v4(0, 0, 1, 0); //v4(-1,-2,-3,0);
		auto lvel = v4(0, 1, 0, 0); //v4(+1,+2,+3,0);
		auto vel = v8m(avel, lvel); // Spatial velocity at the CoM

		auto mass = 5.0f;
		auto Ic = InertiaBuilder(1.0f); // unit inertia

		// Use traditional inertia
		auto amom = mass * Ic * avel; // Iw
		auto lmom = mass * lvel;      // Mv

		// Use spatial inertia at the CoM frame
		auto sIc = Inertia(Ic, v4Zero, mass);
		auto mom = sIc * vel;
		PR_CHECK(FEql(mom.ang, amom), true);
		PR_CHECK(FEql(mom.lin, lmom), true);

		// Use full spatial matrix multiply
		auto sIc2 = sIc.ToSpatialMatrix();
		auto mom2 = sIc2 * vel;
		PR_CHECK(FEql(mom, mom2), true);

		// The spatial inertia and spatial velocity, measured at arbitrary
		// point 'o', should produce the same result.
		//auto ofs = v4(1, 0, 0, 0);
		//auto c2o = m4x4::Translation(ofs);
		//auto vel_o = Shift(vel, ofs);
		//auto sIo2 = c2o * sIc2 * InvertFast(c2o);
		//auto mom3 = sIo2 * vel_o;
		//PR_CHECK(FEql(mom, mom3), true);

//		// Change to an arbitrary frame 'o'
//		// This effectively says, "the CoM is now at 'com'"
//		auto com  = v4(1, 0, 0, 0);
//		auto Io = Ic.Translate(com, 1.0f, InertiaBuilder::AwayFromCoM);
//		amom = mass * Io * avel;
//		lmom = mass * lvel;
//
//		// Use spatial inertia at 'o'
//		auto sIo = Inertia(Io, com, mass);
//		//auto velo = Shift(vel, com);
//		mom = sIo * vel;
//		//mom = Shift(momo, -com);
//		PR_CHECK(FEql(mom.ang, amom), true);
//		PR_CHECK(FEql(mom.lin, lmom), true);

//todo		auto sIo2 = sIo.ToSpatialMatrix();
//todo		mom2 = sIo2 * vel;
//todo		PR_CHECK(FEql(mom, mom2), true);
	};
}
#endif