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

	// Inertia
	// See: RBDA 2.62
	// Inertia as a spatial matrix is a symmetric 6x6 matrix arranged as 2x2 blocks of 3x3 matrices.
	// This type represents the spatial inertia for a simple rigid body (i.e. not articulated) in
	// compact form. In spatial matrix form, the matrix would be:
	//     Io = [Ic + cxcxT , cx] = [Ic - cxcx , cx]
	//          [cxT        ,  1] = [-cx       ,  1]
	//   where:
	//     'Io' is the unit inertia measured about some arbitrary point 'o',
	//     'Ic' is the unit inertia measured about the centre of mass (at 'c'),
	//     'c' is the vector from 'o' to 'c'
	//     'cx' is the cross product matrix of the vector 'c'
	//     'cxT' is the transpose of 'cx' which is equal to '-cx'.
	//
	// Notes:
	//  Mass is included in Inertia so that they can be combined with other inertias.
	//  The inertia matrix is symmetric, so we don't need to store the full matrix.
	//  The inverse of a symmetric positive definite matrix is also symmetric positive
	//   definite so the inverse of 'Inertia' can be stored the same way.
	//  0 Mass is treated as infinite mass.
	//  'CoM()' is used when an inertia is being used at a position that it wasn't measured at.
	//  e.g. if you had a sphere, and a spatial vector measured on the surface of the sphere,
	//  you would need to use 'CoM()' set as a vector from the point on the surface back to the
	//  CoM so that you were using the spatial inertia and spatial vector at the same point.
	//  This is really only used with spatial vectors and should be zero for normal inertia use.

	struct Inertia
	{
		v4 m_diagonal; // The Ixx, Iyy, Izz terms of the unit inertia, Ic.
		v4 m_offdiags; // The Ixy, Ixz, Iyz terms of the unit inertia, Ic.
		v4 m_com_and_mass; // Location of the centre of mass in the frame that Ic was calculated in.

		Inertia() = default;
		Inertia(m3_cref<> unit_inertia, float mass, v4_cref<> com = v4{})
			:m_diagonal(unit_inertia.x.x, unit_inertia.y.y, unit_inertia.z.z, 0)
			,m_offdiags(unit_inertia.x.y, unit_inertia.x.z, unit_inertia.y.z, 0)
			,m_com_and_mass(com.x, com.y, com.z, mass)
		{
			// 'com' is a vector that points from the location that the inertia
			// is expressed at, back to where 'unit_inertia' was measured.
			assert(Check());
		}
		explicit Inertia(MassProperties const& mp)
			:Inertia(mp.m_os_unit_inertia, mp.m_mass)
		{}

		// The mass to scale the inertia by
		float Mass() const
		{
			return m_com_and_mass.w != 0 ? m_com_and_mass.w : maths::float_inf;
		}
		void Mass(float mass)
		{
			m_com_and_mass.w = mass;
		}

		// The inverse mass
		float InvMass() const
		{
			return 1.0f / Mass();
		}

		// Location of the object centre of mass
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
			return Mass() * CoM();
		}

		// The inertia matrix (mass scaled by default)
		m3x4 To3x3(float mass = -1) const
		{
			// Note: if 'CoM' in not zero, it's assumed the caller will manage the CoM offset
			mass = mass >= 0 ? mass : Mass();
			auto dia = mass * m_diagonal;
			auto off = mass * m_offdiags;
			return m3x4{
				v4(dia.x, off.x, off.y, 0),
				v4(off.x, dia.y, off.z, 0),
				v4(off.y, off.z, dia.z, 0)};
		}

		// Return the inertia matrix as a full spatial matrix (mass scaled by default)
		Mat6x8<Motion,Force> To6x6(float mass = -1) const
		{
			mass = mass >= 0 ? mass : Mass();
			auto Ic = To3x3(1);
			auto cx = CPM(CoM());
			auto Io = Mat6x8<Motion,Force>{Ic - cx*cx , cx, -cx, m3x4Identity};
			return mass * Io;
		}

		// Sanity check
		bool Check() const
		{
			return Check(To3x3(1), false);
		}
		static bool Check(m3_cref<> inertia, bool is_inverse)
		{
			// Check for any value == NaN
			if (IsNaN(inertia))
				return false;
			
			// Check symmetric
			if (inertia.x.y != inertia.y.x ||
				inertia.x.z != inertia.z.x ||
				inertia.y.z != inertia.z.y)
				return false;

			auto dia = v4{inertia.x.x, inertia.y.y, inertia.z.z, 0};
			auto off = v4{inertia.x.y, inertia.x.z, inertia.y.z, 0};

			// Diagonals of an Inertia matrix must be non-negative
			if (dia.x < 0 || dia.y < 0 || dia.z < 0)
				return false;

			// Diagonals of an Inertia matrix must satisfy the triangle inequality: a + b >= c
			// Might need to relax 'tol' due to distorted rotation matrices: using: 'Max(Sum(d), 1) * tiny_sqrt'
			if (!is_inverse && (
				(dia.x + dia.y) < dia.z ||
				(dia.y + dia.z) < dia.x ||
				(dia.z + dia.x) < dia.y))
				return false;

			// The magnitude of a product of inertia was too large to be physical.
			if (!is_inverse && (
				dia.x < Abs(2 * off.z) || 
				dia.y < Abs(2 * off.y) ||
				dia.z < Abs(2 * off.x)))
				return false;

			return true;
		}
	};

	// Inverse Inertia.
	// See: RBDA 2.73
	// The format of the inverse inertia expressed at the centre of mass is:
	//  InvIc = InvMass * [Ic^ 0]
	//                    [0   1]
	//  where:
	//    'Ic^' is the inverse of 'Ic', the inertia expressed at the centre of mass,
	// The form of the inverse inertia expressed at an arbitrary point is:
	//  InvIo = InvMass * [Ic^    ,       Ic^cxT] = InvMass * [Ic^   ,      -Ic^cx]
	//                    [cxIc^  , 1 + cxIc^cxT]             [cxIc^ , 1 - cxIc^cx]
	struct InertiaInv
	{
		v4 m_diagonal; // The Ixx, Iyy, Izz terms of the unit inverse inertia tensor
		v4 m_offdiags; // The Ixy, Ixz, Iyz terms of the unit inverse inertia tensor
		v4 m_com_and_mass; // Location of the centre of mass in the frame that Ic was calculated in.

		InertiaInv() = default;

		// 'com' is the location of the CoM in the frame that the inertia is measured in.
		InertiaInv(m3_cref<> unit_inertia_inv, float mass, v4_cref<> com = v4{})
			:m_diagonal(unit_inertia_inv.x.x, unit_inertia_inv.y.y, unit_inertia_inv.z.z, 0)
			,m_offdiags(unit_inertia_inv.x.y, unit_inertia_inv.x.z, unit_inertia_inv.y.z, 0)
			,m_com_and_mass(com.x, com.y, com.z, mass)
		{
			assert(Check());
		}

		// The mass to scale the inertia by
		float Mass() const
		{
			return m_com_and_mass.w != 0 ? m_com_and_mass.w : maths::float_inf;
		}
		void Mass(float mass)
		{
			m_com_and_mass.w = mass;
		}

		// The inverse mass
		float InvMass() const
		{
			return 1.0f / Mass();
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
			return Mass() * CoM();
		}

		// The mass scaled inverse inertia matrix
		m3x4 To3x3(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			auto dia = inv_mass * m_diagonal;
			auto off = inv_mass * m_offdiags;
			return m3x4{
				v4(dia.x, off.x, off.y, 0),
				v4(off.x, dia.y, off.z, 0),
				v4(off.y, off.z, dia.z, 0)};
		}

		// Return the inverse inertia matrix as a full spatial matrix
		Mat6x8<Force,Motion> To6x6(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			auto Ic = m3x4{To3x3(1)};
			auto cx = CPM(CoM());
			auto Io = Mat6x8<Force,Motion>{Ic, -Ic * cx, cx * Ic, m3x4Identity - cx * Ic * cx};
			return inv_mass * Io;
		}

		// Sanity check
		bool Check() const
		{
			return Inertia::Check(To3x3(1), true);
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

	// Multiply a vector by 'inertia'.
	inline v4 operator * (Inertia const& inertia, v4 const& ang)
	{
		if (inertia.CoM() == v4{})
			return inertia.To3x3() * ang;
		else
			throw std::runtime_error("not supported");
	}

	// Multiply a vector by 'inertia_inv'.
	inline v4 operator * (InertiaInv const& inertia_inv, v4 const& ang)
	{
		if (inertia_inv.CoM() == v4{})
			return inertia_inv.To3x3() * ang;
		else
			throw std::runtime_error("not supported");
	}

	// Multiply a spatial motion vector by 'inertia'.
	inline v8f operator * (Inertia const& inertia, v8m const& motion)
	{
		// Typically 'motion' is a velocity or an acceleration.
		// e.g.
		//   I = spatial inertia
		//   v = spatial velocity
		//   h = spatial momentum = I * v
		//   T = kinetic energy = 0.5 * Dot(v, I*v)
		//
		//  h = mass * [Ic - cxcx , cx] * [ang]
		//             [-cx       ,  1]   [lin]
		if (inertia.CoM() == v4{})
			return v8f{inertia.To3x3() * motion.ang, inertia.Mass() * motion.lin};
		else
			return inertia.To6x6() * motion;
	}

	// Multiply a spatial force vector by 'inertia_inv'
	inline v8m operator * (InertiaInv const& inertia_inv, v8f const& force)
	{
		if (inertia_inv.CoM() == v4{})
			return v8m{inertia_inv.To3x3() * force.ang, inertia_inv.InvMass() * force.lin};
		else
			return inertia_inv.To6x6() * force;
	}

	#pragma endregion

	#pragma region Functions

	// Transform an inertia in frame 'a' to frame 'b'
	inline Inertia Transform(Inertia const& inertia, m3_cref<> a2b)
	{
		// Ib = a2b*Ia*b2a
		auto b2a = InvertFast(a2b);
		auto I = a2b * inertia.To3x3(1) * b2a;
		return Inertia(I, inertia.Mass(), inertia.CoM());
	}
	inline InertiaInv Transform(InertiaInv const& inertia_inv, m3_cref<> a2b)
	{
		// Ib^ = (a2b*Ia*b2a)^ = b2a^*Ia^*a2b^ = a2b*Ia^*b2a
		auto b2a = InvertFast(a2b);
		auto I = a2b * inertia_inv.To3x3(1) * b2a;
		return InertiaInv(I, inertia_inv.Mass(), inertia_inv.CoM());
	}

	// Invert inertia
	inline InertiaInv Invert(Inertia const& inertia)
	{
		auto unit_inertia_inv = m3x4{Invert(inertia.To3x3(1))};
		return InertiaInv(unit_inertia_inv, inertia.Mass(), inertia.CoM());
	}
	inline Inertia Invert(InertiaInv const& inertia_inv)
	{
		auto unit_inertia = m3x4{Invert(inertia_inv.To3x3(1))};
		return Inertia(unit_inertia, inertia_inv.Mass(), inertia_inv.CoM());
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr::physics
{
	PRUnitTest(InertiaTests)
	{
		auto mass = 5.0f;
		{
			auto avel = v4{0, 0, 1, 0}; //v4(-1,-2,-3,0);
			auto lvel = v4{0, 1, 0, 0}; //v4(+1,+2,+3,0);
			auto vel = v8m{avel, lvel};

			// Inertia of a sphere with radius 1, positioned at (0,0,0), measured at (0,0,0) (2/5 m r^2)
			auto Ic = (2.0f/5.0f) * m3x4Identity;

			// Traditional momentum calculation
			auto amom = mass * Ic * avel; // I.w
			auto lmom = mass * lvel;      // M.v

			// Spatial inertia for the same sphere, expressed at (0,0,0)
			auto sIc = Inertia(Ic, mass);
			auto mom = sIc * vel;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);

			// Full spatial matrix multiply
			auto sIc2 = sIc.To6x6();
			auto mom2 = sIc2 * vel;
			PR_CHECK(FEql(mom, mom2), true);
		}
		{
			auto avel = v4{0, 0, 1, 0}; //v4(-1,-2,-3,0);
			auto lvel = v4{0, 1, 0, 0}; //v4(+1,+2,+3,0);
			auto vel = v8m{avel, lvel};

			// Inertia of a sphere with radius 1, positioned at (0,0,0), measured at (0,0,0) (2/5 m r^2)
			auto Ic = (2.0f/5.0f) * m3x4Identity;

			// Calculate the momentum expressed at 'r'
			auto r = v4{1,0,0,0};
			auto amom = mass * Ic * avel - Cross(r, mass * lvel);
			auto lmom = mass * lvel;

			// Inertia of a sphere with radius 1, positioned at (0,0,0) and measured at (0,0,0) expressed at 'r'
			auto vel_r = Shift(vel, r);
			auto sI_r = Inertia(Ic, mass, -r); // 'r' points back at the CoM
			auto mom = sI_r * vel_r;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);
		
			// Expressed at another point
			r = v4{1,2,3,0};
			amom = mass * Ic * avel - Cross(r, mass * lvel);
			lmom = mass * lvel;

			// Inertia of a sphere with radius 1, positioned at (0,0,0) and measured at (0,0,0) expressed at 'r'
			vel_r = Shift(vel, r);
			sI_r = Inertia(Ic, mass, -r); // 'r' points back at the CoM
			mom = sI_r * vel_r;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);
		}
		{ // Kinetic energy: 0.5 * Dot(h, v)

			// Sphere travelling at 'vel'
			auto avel = v4{0, 0, 1, 0}; //v4(-1,-2,-3,0);
			auto lvel = v4{0, 1, 0, 0}; //v4(+1,+2,+3,0);
			auto vel = v8m{avel, lvel};

			// Inertia of a sphere with radius 1, positioned at (0,0,0), measured at (0,0,0) (2/5 m r^2)
			auto Ic = (2.0f/5.0f) * m3x4Identity;

			// Calculate kinetic energy
			auto ke_lin = 0.5f * mass * Dot(lvel,lvel);
			auto ke_ang = 0.5f * mass * Dot(avel, Ic * avel);
			auto ke = ke_lin + ke_ang;

			auto sIc = Inertia(Ic, mass);
			auto mom = sIc * vel;
			auto KE = 0.5f * Dot(vel, mom); // 0.5 v.I.v

			PR_CHECK(FEql(KE, ke), true);
		}
	};
}
#endif