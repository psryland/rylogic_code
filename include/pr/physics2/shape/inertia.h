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
		// axes only affects the measure numbers of that quantity. For any reference 
		// point, there is a unique set of reference axes in which the inertia tensor is
		// diagonal; those are called the "principal axes" of the body at that point, and 
		// the resulting diagonal elements are the "principal moments of inertia". When 
		// we speak of an inertia being "in" a frame, we mean the physical quantity 
		// measured about the frame's origin and then expressed in the frame's axes.
		//
		// This low-level Inertia type does not attempt to keep track of which frame 
		// it is in. It provides construction and operations involving inertia that can 
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
		struct Inertia
		{
			// Direction for translating an inertia tensor
			enum ETranslateType { TowardCoM, AwayFromCoM };

			m3x4 m;

			Inertia() = default;

			// Construct an Inertia from a symmetric 3x3 matrix.
			explicit Inertia(m3x4_cref rhs) :m(rhs)
			{
				assert(Check());
			}

			// Create a principal inertia matrix with identical diagonal elements, like a sphere
			// where moment=2/5 m r^2, or a cube where moment=1/6 m s^2, with m the total mass,
			// r the sphere's radius and s the length of a side of the cube. Note that many rigid
			// bodies of different shapes and masses can have the same inertia matrix.
			explicit Inertia(float moment) :m()
			{
				m.x.x = m.y.y = m.z.z = moment;
				assert(Check());
			}

			// Create an inertia matrix from a vector of the moments of inertia (the inertia matrix diagonal)
			// and optionally a vector of the products of inertia (the off-diagonals).
			// Moments are in the order: 'xx, yy, zz'. Products are in the order: 'xy, xz, yz'.
			explicit Inertia(v4_cref moments, v4_cref products = v4Zero) :m()
			{
				m.x.x = moments.x;
				m.y.y = moments.x;
				m.z.z = moments.x;
				m.x.y = m.y.x = products.x;
				m.x.z = m.z.x = products.y;
				m.y.z = m.z.y = products.z;
				assert(Check());
			}

			// Returns an Inertia, assumed to be in frame A, rotated to frame B using rotation 'a2b'
			Inertia Rotate(m3x4_cref a2b) const 
			{
				return Inertia(a2b * m * Transpose(a2b));
			}

			// Returns an inertia tensor transformed using the parallel axis theorem.
			// 'offset' is the distance from (or toward) the centre of mass (determined
			// by 'translate_type'). 'inertia' and 'offset' must be in the same frame.
			// Note: If this is a unit inertia, remember to translate with 'mass = 1.0'.
			Inertia Translate(v4_cref offset, float mass, ETranslateType translate_type)
			{
				// This is basically: I +=/-= PointMassAt(offset)
				if (translate_type == ETranslateType::TowardCoM)
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
				return Inertia(inertia);
			}

			// Create an Inertia matrix for a point at 'position'
			static Inertia PointAt(v4_cref position)
			{
				auto xx = position.x * position.x;
				auto yy = position.y * position.y;
				auto zz = position.z * position.z;
				auto xy = position.x * position.y;
				auto xz = position.x * position.z;
				auto yz = position.y * position.z;
				return Inertia(v4(yy+zz, xx+zz, xx+yy, 0), v4(-xy, -xz, -yz, 0));
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
				if (FLess(d[0],0) || FLess(d[1],0) || FLess(d[2],0))
					return false;

				// Diagonals of an Inertia matrix must satisfy the triangle inequality: a + b >= c
				// Might need to relax 'tol' due to distorted rotation matrices: using: 'Max(Sum(d), 1) * tiny_sqrt'
				if (FLess(d[0] + d[1], d[2]) ||
					FLess(d[1] + d[2], d[0]) ||
					FLess(d[2] + d[0], d[1]))
					return false;

				// The magnitude of a product of inertia was too large to be physical.
				if (FLess(d[0], Abs(2 * p[2])) || 
					FLess(d[1], Abs(2 * p[1])) ||
					FLess(d[2], Abs(2 * p[0])))
					return false;

				return true;
			}
		};

		#pragma region Inertia Operators
		inline Inertia operator + (Inertia const& lhs, Inertia const& rhs)
		{
			// These two inertia matrices must be in the same space
			return Inertia(lhs.m + rhs.m);
		}
		inline Inertia operator - (Inertia const& lhs, Inertia const& rhs)
		{
			// These two inertia matrices must be in the same space
			return Inertia(lhs.m - rhs.m);
		}
		inline Inertia operator * (float lhs, Inertia const& rhs)
		{
			return Inertia(lhs * rhs.m);
		}
		inline Inertia operator * (Inertia const& lhs, float rhs)
		{
			return Inertia(lhs.m * rhs);
		}
		inline Inertia operator / (Inertia const& lhs, float rhs)
		{
			return lhs * (1.0f / rhs);
		}
		inline v4 operator * (Inertia const& lhs, v4_cref rhs)
		{
			return lhs.m * rhs;
		}
		inline Inertia& operator += (Inertia& lhs, Inertia const& rhs)
		{
			return lhs = lhs + rhs;
		}
		inline Inertia& operator -= (Inertia& lhs, Inertia const& rhs)
		{
			return lhs = lhs - rhs;
		}
		inline Inertia& operator *= (Inertia& lhs, float rhs)
		{
			return lhs = lhs * rhs;
		}
		inline Inertia& operator /= (Inertia& lhs, float rhs)
		{
			return lhs = lhs / rhs;
		}
		#pragma endregion

		// Spatial Inertia
		// Spatial inertia may be usefully viewed as a symmetric spatial matrix, that is, 
		// a 6x6 symmetric matrix arranged as 2x2 blocks of 3x3 matrices. This type represents
		// the spatial inertia in compact form. In spatial matrix form, the matrix has the following
		// interpretation:
		//'     M = mass * [ Io cx ]
		//'                [ -cx 1 ]
		//   where 'c' is the vector from the centre of mass to the point where 'Io' is measured,
		//   'cx' is the cross product matrix formed from 'c'. (note: '-cx == Transpose(cx)')
		//   'Io' is the inertia of the body measured at point 'c'.
		// Notes:
		//  Mass is included in SpatialInertia so that they can be combined with other Spatial inertias.
		struct SpatialInertia
		{
			Inertia m_inertia; // Mass distribution matrix at 'm_ofs' in body space for a unit mass object.
			v4      m_com;     // Location of the object centre of mass in the frame that 'm_inertia' is measured in.
			kg_t    m_mass;    // The body mass

			SpatialInertia() = default;
			SpatialInertia(Inertia const& inertia, v4_cref com, float mass)
				:m_inertia(inertia)
				,m_com(com)
				,m_mass(mass)
			{
				assert(Check());
			}

			// The mass weighted distance to the centre of mass
			v4 MassMoment() const
			{
				return m_mass * m_com;
			}

			// The mass-scaled inertia matrix
			Inertia Inertia() const
			{
				return m_mass * m_inertia;
			}

			// Return the inertia matrix as a full spatial matrix
			m6x8_m2f ToSpatialMatrix() const
			{
				auto cx = CPM(m_mass * m_com);
				return m6x8_m2f(m_mass * m_inertia.m, cx, -cx, m3x4::Scale(m_mass));
			}

			// Sanity check
			bool Check() const
			{
				// Check the inertia matrix
				if (!m_inertia.Check())
					return false;

				// Check for any value == NaN
				if (IsNaN(m_com) || IsNaN(m_mass))
					return false;

				return true;
			}
		};

		#pragma region SpatialInertia Operators
		inline SpatialInertia operator + (SpatialInertia const& lhs, SpatialInertia const& rhs)
		{
			// When two rigid bodies are joined, you can just add the spatial inertia directly.
			assert("The combined mass cannot be zero." && !FEql(lhs.m_mass + rhs.m_mass, 0));

			// Find the combined mass, combined centre of mass, and unit inertia
			auto mass    = lhs.m_mass + rhs.m_mass;
			auto fracL   = lhs.m_mass / mass;
			auto fracR   = rhs.m_mass / mass;
			auto com     = fracL*lhs.m_com     + fracR*rhs.m_com;
			auto inertia = fracL*lhs.m_inertia + fracR*rhs.m_inertia;
			return SpatialInertia(inertia, com, mass);
		}
		inline v8f operator * (SpatialInertia const& lhs, v8m const& rhs)
		{
			// Multiply a spatial motion vector by a spatial inertia matrix.
			//   I = lhs = spatial inertia
			//   v = rhs = spatial velocity
			//   h = spatial momentum = I * v
			//   T = kinetic energy = 0.5 * Dot(v, I*v)
			
			//' m * [  I  cx ] * [ang]
			//'     [ -cx  1 ]   [lin]
			return lhs.m_mass * v8f(lhs.m_inertia.m * rhs.ang + Cross3(lhs.m_com, rhs.lin), rhs.lin - Cross3(lhs.m_com, rhs.ang));
		}
		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_physics_shape_inertia)
		{
			using namespace pr::physics;

			auto avel = v4(-1,-2,-3,0);
			auto lvel = v4(+1,+2,+3,0);
			auto vel = v8m(avel, lvel);

			auto mass = 10.0f;
			auto Ic   = Inertia(5.0f);

			// Use traditional inertia
			auto amom = mass * Ic * avel; // Iw
			auto lmom = mass * lvel;      // Mv

			// Use spatial inertia
			auto sIc = SpatialInertia(Ic, v4Zero, mass);
			auto mom = sIc * vel;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);

			// Use full spatial matrix multiply
			auto sIc2 = sIc.ToSpatialMatrix();
			auto mom2 = sIc2 * vel;
			PR_CHECK(FEql(mom, mom2), true);

			// Change to an arbitrary frame 'o'
			auto com  = v4(1, 0, 0, 0);
			auto Io = Ic.Translate(com, 1.0f, Inertia::ETranslateType::AwayFromCoM);
			amom = mass * Io * avel;
			lmom = mass * lvel;

	//todo		auto sIo = SpatialInertia(Io, com, mass);
	//todo		mom = sIo * vel;
	//todo		PR_CHECK(FEql(mom.ang, amom), true);
	//todo		PR_CHECK(FEql(mom.lin, lmom), true);

	//todo		auto sIo2 = sIo.ToSpatialMatrix();
	//todo		mom2 = sIo2 * vel;
	//todo		PR_CHECK(FEql(mom, mom2), true);
		}
	}
}
#endif