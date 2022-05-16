//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/mass.h"

namespace pr::physics
{
	// Inertia Matrix
	// The physical meaning of an inertia is the distribution of a rigid body's 
	// mass about a particular point. If that point is the centre of mass of the 
	// body, then the measured inertia is called the "central inertia" of that body. 
	// To write down the inertia, we need to calculate the six scalars of the inertia 
	// matrix, which is a symmetric 3x3 matrix. These scalars must be expressed in 
	// an arbitrary but specified coordinate system. So an Inertia is meaningful only 
	// in conjunction with a particular set of axes, fixed to the body, whose origin 
	// is the point about which the inertia is being measured, and in whose 
	// coordinate system this measurement is being expressed. Note that changing the 
	// reference point results in a new physical quantity, but changing the reference 
	// axes only affects the measured numbers of that quantity. For any reference 
	// point, there is a unique set of reference axes in which the inertia matrix is
	// diagonal; those are called the "principal axes" of the body at that point, and 
	// the resulting diagonal elements are the "principal moments of inertia". When 
	// we speak of an inertia being "in" a frame, we mean the physical quantity 
	// measured about the frame's origin and then expressed in the frame's axes.
	//
	// Changing the coordinate system of an Inertia matrix does not entail a change of
	// physical meaning in the way that shifting it to a different point does.
	// To change coordinates use:
	//    Ib = b2a * Ia * a2b
	//
	// An Inertia is a symmetric matrix and is positive definite for non-singular bodies
	// (that is, a body composed of at least three non-collinear point masses).
	//
	// Note: inertia scales linearly with mass. This means inertia can be stored for a
	// unit mass (=1kg) and scaled when needed.

	// I¯ = I alt+0175

	// Inertia
	// See: RBDA 2.62
	// Inertia as a spatial matrix is a symmetric 6x6 matrix arranged as 2x2 blocks of 3x3 matrices.
	// This type represents the spatial inertia for a simple rigid body (i.e. not articulated) in
	// compact form. In spatial matrix form, the matrix would be:
	//     Io = [Ic + cxTcx , cxT] = [Ic - cxcx , -cx]
	//          [cx         ,   1] = [cx        ,   1]
	//   where:
	//     'Io' is the unit inertia measured about some arbitrary point 'o',
	//     'Ic' is the unit inertia measured about the centre of mass (at 'c'),
	//     'c' is the vector from 'o' back to 'c'
	//     'cx' is the cross product matrix of the vector 'c'
	//     'cxT' is the transpose of 'cx' which is equal to '-cx'.
	//
	// Notes:
	//  - Mass is included in 'Inertia' so that they can be combined with other inertias.
	//  - The inertia matrix is symmetric, so we don't need to store the full matrix.
	//  - The inverse of a symmetric positive definite matrix is also symmetric positive
	//    definite so the inverse of 'Inertia' can be stored the same way.
	//  - 'CoM()' is a vector from the origin of the space that the inertia is in to the
	//    centre of mass. This is really only used with spatial vectors and should be zero
	//    for normal inertia use.
	//  - Using float_inf for infinite mass objects doesn't work well because inf * 0 == NaN.
	//    Instead, use float_max in-place of infinite.
	//  - Infinite inertia matrices are an identity matrix but with 'mass' as float_max.
	//    That way, Invert and other functions don't need to handle special cases.

	// Use the sqrt of float_max as the threshold for infinite mass so that 
	// 'InfiniteMass * InfiniteMass' does not overflow a float. If mass becomes
	// 'inf' then multiplying by 0 creates NaNs.
	constexpr float InfiniteMass = 1.84467435229094026671e19f; // = sqrt(maths::float_max);
	constexpr float ZeroMass = 1.0f / InfiniteMass;

	// Direction for translating an inertia matrix
	enum class ETranslateInertia
	{
		TowardCoM,   // The pointy end of 'offset' is the CoM
		AwayFromCoM, // The base of 'offset' is the CoM
	};

	#pragma region Forwards
	struct Inertia;
	struct InertiaInv;
	bool FEql(Inertia const& lhs, Inertia const& rhs);
	bool FEql(InertiaInv const& lhs, InertiaInv const& rhs);
	Inertia Join(Inertia const& lhs, Inertia const& rhs);
	Inertia Split(Inertia const& lhs, Inertia const& rhs);
	InertiaInv Join(InertiaInv const& lhs, InertiaInv const& rhs);
	InertiaInv Split(InertiaInv const& lhs, InertiaInv const& rhs);
	InertiaInv Invert(Inertia const& inertia);
	Inertia Invert(InertiaInv const& inertia_inv);
	Inertia Rotate(Inertia const& inertia, m3_cref<> a2b);
	InertiaInv Rotate(InertiaInv const& inertia_inv, m3_cref<> a2b);
	Inertia Translate(Inertia const& inertia0, v4_cref<> offset, ETranslateInertia direction);
	InertiaInv Translate(InertiaInv const& inertia0¯, v4_cref<> offset, ETranslateInertia direction);
	Inertia Transform(Inertia const& inertia0, m4_cref<> a2b, ETranslateInertia direction);
	InertiaInv Transform(InertiaInv const& inertia0¯, m4_cref<> a2b, ETranslateInertia direction);
	#pragma endregion

	struct Inertia
	{
		// Notes:
		//  - The 'CoM' is not built into the inertia, it can be freely set to whatever you want.
		//    It's here as a convenience for calculating the inertia, parallel axis translated.
		//    Think of 'CoM' as a vector from your common point (typically the model origin) to
		//    the location of the centre of mass.

		v4 m_diagonal;     // The Ixx, Iyy, Izz terms of the unit inertia at the CoM, Ic.
		v4 m_products;     // The Ixy, Ixz, Iyz terms of the unit inertia at the CoM, Ic.
		v4 m_com_and_mass; // Offset from the origin to the centre of mass, and the mass.

		Inertia()
			:m_diagonal(1, 1, 1, 0)
			,m_products(0, 0, 0, 0)
			,m_com_and_mass(0,0,0,InfiniteMass)
		{}
		Inertia(m3_cref<> unit_inertia, float mass, v4_cref<> com = v4{})
			:m_diagonal(unit_inertia.x.x, unit_inertia.y.y, unit_inertia.z.z, 0)
			,m_products(unit_inertia.x.y, unit_inertia.x.z, unit_inertia.y.z, 0)
			,m_com_and_mass(com.xyz, mass)
		{
			assert(Check());
		}
		Inertia(v4_cref<> diagonal, v4_cref<> products, float mass, v4_cref<> com = v4{})
			:m_diagonal(diagonal)
			,m_products(products)
			,m_com_and_mass(com.xyz, mass)
		{
			assert(Check());
		}
		Inertia(float diagonal, float mass, v4_cref<> com = v4{})
			:m_diagonal(diagonal, diagonal, diagonal, 0)
			,m_products()
			,m_com_and_mass(com.xyz, mass)
		{
			assert(Check());
		}
		Inertia(Inertia const& rhs, v4_cref<> com)
			:m_diagonal(rhs.m_diagonal)
			,m_products(rhs.m_products)
			,m_com_and_mass(com.xyz, rhs.Mass())
		{
			assert(Check());
		}
		explicit Inertia(m6_cref<Motion,Force> inertia, float mass = -1)
		{
			// If 'mass' is given, 'inertia' is assumed to be a unit inertia
			assert(Inertia::Check(inertia));

			auto m = mass >= 0 ? mass : Trace(inertia.m11) / 3.0f;
			auto cx   = (1.0f/m) * inertia.m01;
			auto Ic   = (1.0f/m) * inertia.m00 + cx * cx;
			*this = Inertia{Ic, m, v4{cx.y.z, -cx.x.z, cx.x.y, 0}};
		}
		explicit Inertia(MassProperties const& mp)
			:Inertia(mp.m_os_unit_inertia, mp.m_mass)
		{}

		// The mass to scale the inertia by
		float Mass() const
		{
			return
				m_com_and_mass.w <  ZeroMass ? 0.0f :
				m_com_and_mass.w >= InfiniteMass ? InfiniteMass :
				m_com_and_mass.w;
		}
		void Mass(float mass)
		{
			assert("Mass must be positive" && mass >= 0);
			assert(!isnan(mass));
			m_com_and_mass.w =
				mass <  ZeroMass ? 0.0f :
				mass >= InfiniteMass ? InfiniteMass :
				mass;
		}

		// The inverse mass
		float InvMass() const
		{
			auto mass = Mass();
			return
				mass <  ZeroMass ? InfiniteMass :
				mass >= InfiniteMass ? 0.0f :
				1.0f / mass;
		}
		void InvMass(float invmass)
		{
			assert("Mass must be positive" && invmass >= 0);
			assert(!isnan(invmass));
			m_com_and_mass.w =
				invmass <  ZeroMass ? InfiniteMass :
				invmass >= InfiniteMass ? 0.0f :
				1.0f / invmass;
		}

		// Offset from the origin of the space this inertia is in to the centre of mass.
		// Note: this is *NOT* equivalent to translating the inertia.
		v4 CoM() const
		{
			return m_com_and_mass.w0();
		}
		void CoM(v4 com)
		{
			m_com_and_mass.xyz = com.xyz;
		}

		// The mass weighted distance from the centre of mass
		v4 MassMoment() const
		{
			return -Mass() * CoM();
		}

		// Return the centre of mass inertia (mass scaled by default, excludes 'com')
		m3x4 Ic3x3(float mass = -1) const
		{
			mass = mass >= 0 ? mass : Mass();
			if (mass < ZeroMass || mass >= InfiniteMass)
				return m3x4Identity;

			auto dia = mass * m_diagonal;
			auto off = mass * m_products;
			auto Ic = m3x4{
				v4{dia.x, off.x, off.y, 0},
				v4{off.x, dia.y, off.z, 0},
				v4{off.y, off.z, dia.z, 0}};
			return Ic;
		}

		// The 3x3 inertia matrix (mass scaled by default, includes 'com')
		m3x4 To3x3(float mass = -1) const
		{
			mass = mass >= 0 ? mass : Mass();
			if (mass < ZeroMass || mass >= InfiniteMass)
				return m3x4Identity;

			auto Ic = Ic3x3(mass);
			if (CoM() == v4{})
				return Ic;

			auto cx = CPM(CoM());
			auto Io = Ic - mass * cx * cx;
			return Io;
		}

		// The 6x6 inertia matrix (mass scaled by default)
		Mat6x8f<Motion,Force> To6x6(float mass = -1) const
		{
			mass = mass >= 0 ? mass : Mass();
			if (mass < ZeroMass || mass >= InfiniteMass)
				return Mat6x8f<Motion,Force>{m6x8Identity};

			auto Ic = Ic3x3(mass);
			auto cx = CPM(CoM());
			auto Io = Mat6x8f<Motion,Force>{Ic - mass*cx*cx , mass*cx, -mass*cx, mass*m3x4Identity};
			return Io;
		}

		// Sanity check
		bool Check() const
		{
			return CoM() == v4{} ? Inertia::Check(To3x3()) : Inertia::Check(To6x6());
		}
		static bool Check(m3_cref<> inertia)
		{
			// Check for any value == NaN
			if (IsNaN(inertia))
				return assert(false),false;
			
			// Check symmetric
			if (!IsSymmetric(inertia))
				return assert(false),false;

			auto dia = v4{inertia.x.x, inertia.y.y, inertia.z.z, 0};
			auto off = v4{inertia.x.y, inertia.x.z, inertia.y.z, 0};

			// Diagonals of an Inertia matrix must be non-negative
			if (dia.x < 0 || dia.y < 0 || dia.z < 0)
				return assert(false),false;

			// Diagonals of an Inertia matrix must satisfy the triangle inequality: a + b >= c
			// Might need to relax 'tol' due to distorted rotation matrices: using: 'Max(Sum(d), 1) * tiny_sqrt'
			if ((dia.x + dia.y) < dia.z ||
				(dia.y + dia.z) < dia.x ||
				(dia.z + dia.x) < dia.y)
				return assert(false),false;

			// The magnitude of a product of inertia was too large to be physical.
			if (dia.x < Abs(2 * off.z) ||
				dia.y < Abs(2 * off.y) ||
				dia.z < Abs(2 * off.x))
				return assert(false),false;

			return true;
		}
		static bool Check(m6_cref<Motion,Force> inertia)
		{
			// Check for any value == NaN
			if (IsNaN(inertia))
				return assert(false),false;
			
			// Check symmetric
			if (!IsSymmetric(inertia.m00) ||
				!IsSymmetric(inertia.m11) ||
				!IsAntiSymmetric(inertia.m01) ||
				!IsAntiSymmetric(inertia.m10) ||
				!FEql(inertia.m01 + inertia.m10, m3x4{}))
				return assert(false),false;

			// Check 'mass * 1'
			auto m = inertia.m11.x.x;
			if (!FEql(inertia.m11.y.y - m, 0.f) ||
				!FEql(inertia.m11.z.z - m, 0.f))
				return assert(false),false;

			// Check 'mass * cx'
			auto mcx = inertia.m01;
			if (!FEql(Trace(mcx), 0.f) ||
				!IsAntiSymmetric(mcx))
				return assert(false),false;

			// Check 'mass * cxT'
			auto mcxT = inertia.m10;
			if (!FEql(Trace(mcxT), 0.f) ||
				!IsAntiSymmetric(mcxT))
				return assert(false),false;

			// Check 'Ic - mcxcx'
			if (!Check(inertia.m00))
				return assert(false),false;

			return true;
		}

		// An immovable object
		template <typename = void> static Inertia Infinite()
		{
			return Inertia{v4{1,1,1,0}, v4{0,0,0,0}, InfiniteMass};
		}

		// Create an inertia matrix for a point at 'offset'
		template <typename = void> static Inertia Point(float mass, v4_cref<> offset = v4{})
		{
			auto ib = Inertia{1.0f, mass};
			ib = Translate(ib, offset, ETranslateInertia::AwayFromCoM);
			return ib;
		}

		// Create an inertia matrix for a sphere at 'offset'
		template <typename = void> static Inertia Sphere(float radius, float mass, v4_cref<> offset = v4{})
		{
			auto ib = Inertia{(2.0f/5.0f) * Sqr(radius), mass};
			ib = Translate(ib, offset, ETranslateInertia::AwayFromCoM);
			return ib;
		}

		// Create an inertia matrix for a box at 'offset'
		template <typename = void> static Inertia Box(v4_cref<> radius, float mass, v4_cref<> offset = v4{})
		{
			auto xx = (1.0f/3.0f) * (Sqr(radius.y) + Sqr(radius.z));
			auto yy = (1.0f/3.0f) * (Sqr(radius.z) + Sqr(radius.x));
			auto zz = (1.0f/3.0f) * (Sqr(radius.x) + Sqr(radius.y));
			auto ib = Inertia{v4{xx,yy,zz,0}, v4{}, mass};
			ib = Translate(ib, offset, ETranslateInertia::AwayFromCoM);
			return ib;
		}

		#pragma region Operators
		// Note: there is no operator + because its definition is ambiguous
		//  Ia + Ib can either mean:
		//      Ia.To3x3() + Ib.To3x3() or Ia.To6x6() + Ib.To6x6() 
		//  or weld two rigid bodies together:
		//      (ma*Ia + mb*Ib)/(mamb) 

		// Equality
		friend bool operator == (Inertia const& lhs, Inertia const& rhs)
		{
			return
				lhs.m_diagonal == rhs.m_diagonal &&
				lhs.m_products == rhs.m_products &&
				lhs.m_com_and_mass == rhs.m_com_and_mass;
		}
		friend bool operator != (Inertia const& lhs, Inertia const& rhs)
		{
			return !(lhs == rhs);
		}

		// Multiply a vector by 'inertia'.
		friend v4 operator * (Inertia const& inertia, v4 const& v)
		{
			if (inertia.CoM() == v4{})
				return inertia.To3x3() * v;
			else
				return Translate(inertia, -inertia.CoM(), ETranslateInertia::AwayFromCoM).To3x3() * v;
		}

		// Multiply a spatial motion vector by 'inertia'.
		friend v8force operator * (Inertia const& inertia, v8motion const& motion)
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
		
			// Special case when the inertia is in CoM frame.
			if (inertia.CoM() == v4{})
				return v8force{inertia.To3x3() * motion.ang, inertia.Mass() * motion.lin};
			else
				return inertia.To6x6() * motion;
		}
		#pragma endregion
	};

	// Inverse Inertia.
	// See: RBDA 2.73
	// The format of the inverse inertia expressed at the centre of mass is:
	//   InvMass * [Ic¯ 0]
	//             [0   1]
	//  where:
	//    'Ic¯' is the inverse of 'Ic', the inertia expressed at the centre of mass,
	// The form of the inverse inertia expressed at an arbitrary point is:
	//  Io¯ = InvMass * [Ic¯   ,       Ic¯cxT] = InvMass * [Ic¯   ,      -Ic¯cx]
	//                  [cxIc¯ , 1 + cxIc¯cxT]             [cxIc¯ , 1 - cxIc¯cx]
	struct InertiaInv
	{
		v4 m_diagonal;        // The Ixx, Iyy, Izz terms of the unit inverse inertia
		v4 m_products;        // The Ixy, Ixz, Iyz terms of the unit inverse inertia
		v4 m_com_and_invmass; // Offset from the origin to the centre of mass, and the inverse mass.

		InertiaInv()
			:m_diagonal(1, 1, 1, 0)
			,m_products(0, 0, 0, 0)
			,m_com_and_invmass(0, 0, 0, 0)
		{}
		InertiaInv(m3_cref<> unit_inertia_inv, float invmass, v4_cref<> com = v4{})
			:m_diagonal(unit_inertia_inv.x.x, unit_inertia_inv.y.y, unit_inertia_inv.z.z, 0)
			,m_products(unit_inertia_inv.x.y, unit_inertia_inv.x.z, unit_inertia_inv.y.z, 0)
			,m_com_and_invmass(com.xyz, invmass)
		{
			assert(Check());
		}
		InertiaInv(v4_cref<> diagonal, v4_cref<> products, float invmass, v4_cref<> com = v4{})
			:m_diagonal(diagonal)
			,m_products(products)
			,m_com_and_invmass(com.xyz, invmass)
		{
			assert(Check());
		}
		InertiaInv(InertiaInv const& rhs, v4_cref<> com)
			:m_diagonal(rhs.m_diagonal)
			,m_products(rhs.m_products)
			,m_com_and_invmass(com.xyz, rhs.InvMass())
		{
			assert(Check());
		}
		InertiaInv(m6_cref<Force,Motion> inertia_inv, float invmass = -1)
		{
			// If 'invmass' is given, 'inertia_inv' is assumed to be a unit inverse inertia
			assert(InertiaInv::Check(inertia_inv));

			auto Ic¯ = inertia_inv.m00;
			auto cx  = inertia_inv.m10 * Invert(Ic¯);
			auto im = invmass >= 0 ? invmass : Trace(inertia_inv.m11 + cx*Ic¯*cx) / 3.0f;
			*this = InertiaInv{(1/im)*Ic¯, im, v4{cx.y.z, -cx.x.z, cx.x.y, 0}};
		}

		// The mass to scale the inertia by
		float Mass() const
		{
			auto im = InvMass();
			return
				im <  ZeroMass ? InfiniteMass :
				im >= InfiniteMass ? 0.0f :
				1.0f / im;
		}
		void Mass(float mass)
		{
			assert("Mass must be positive" && mass >= 0);
			assert(!isnan(mass));
			m_com_and_invmass.w =
				mass <  ZeroMass ? InfiniteMass :
				mass >= InfiniteMass ? 0.0f :
				1.0f / mass;
		}

		// The inverse mass
		float InvMass() const
		{
			auto invmass = m_com_and_invmass.w;
			return
				invmass <  ZeroMass ? 0.0f :
				invmass >= InfiniteMass ? InfiniteMass :
				invmass;
		}
		void InvMass(float invmass)
		{
			assert("Mass must be positive" && invmass >= 0);
			assert(!isnan(invmass));
			m_com_and_invmass.w =
				invmass <  ZeroMass ? 0.0f :
				invmass >= InfiniteMass ? InfiniteMass :
				invmass;
		}

		// Offset to the location to use the inverse inertia
		v4 CoM() const
		{
			return m_com_and_invmass.w0();
		}
		void CoM(v4 com)
		{
			m_com_and_invmass.xyz = com.xyz;
		}

		// The centre of mass inverse inertia (mass scaled by default, excludes 'com')
		m3x4 Ic3x3(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			if (inv_mass < ZeroMass || inv_mass >= InfiniteMass)
				return m3x4Identity;

			auto dia = inv_mass * m_diagonal;
			auto off = inv_mass * m_products;
			auto Ic¯ = m3x4{
				v4{dia.x, off.x, off.y, 0},
				v4{off.x, dia.y, off.z, 0},
				v4{off.y, off.z, dia.z, 0}};
			return Ic¯;
		}

		// The mass scaled inverse inertia matrix
		m3x4 To3x3(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			if (inv_mass < ZeroMass || inv_mass >= InfiniteMass)
				return m3x4::Identity();

			auto Ic¯ = Ic3x3(inv_mass);
			if (CoM() == v4{})
				return Ic¯;

			//' Io¯ = (Ic - mcxcx)¯                                  '
			//' Identity: (A + B)¯ = A¯ - (1 + A¯B)¯A¯BA¯            '
			//'   Let A = Ic, B = -mcxcx                             '
			//'  Then:                                               '
			//' Io¯ = Ic¯ + m(1 - mIc¯cxcx)¯Ic¯cxcxIc¯               '
			//'     = Ic¯ + (1/m - Ic¯cxcx)¯Ic¯cxcxIc¯               '

			// This is cheaper
			auto cx = CPM(CoM());
			auto Io = Invert(Ic¯) - (1.0f/inv_mass) * cx * cx;
			auto Io¯ = Invert(Io);
			return Io¯;
		}

		// Return the inverse inertia matrix as a full spatial matrix
		Mat6x8f<Force,Motion> To6x6(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			if (inv_mass < ZeroMass || inv_mass >= InfiniteMass)
				return Mat6x8f<Force,Motion>{m6x8Identity};

			auto Ic¯ = Ic3x3(inv_mass);
			auto cx  = CPM(CoM());
			auto Io¯ = Mat6x8f<Force,Motion>{Ic¯, -Ic¯*cx, cx*Ic¯, inv_mass*m3x4Identity - cx*Ic¯*cx};
			return Io¯;
		}

		// Sanity check
		bool Check() const
		{
			return CoM() == v4{} ? InertiaInv::Check(To3x3()) : InertiaInv::Check(To6x6());
		}
		static bool Check(m3_cref<> inertia_inv)
		{
			// Check for any value == NaN
			if (IsNaN(inertia_inv))
				return assert(false),false;
			
			// Check symmetric
			if (!IsSymmetric(inertia_inv))
				return assert(false),false;

			auto dia = v4{inertia_inv.x.x, inertia_inv.y.y, inertia_inv.z.z, 0};
			auto off = v4{inertia_inv.x.y, inertia_inv.x.z, inertia_inv.y.z, 0};

			// Diagonals of an inverse inertia matrix must be non-negative
			if (dia.x < 0 || dia.y < 0 || dia.z < 0)
				return assert(false),false;

			// Diagonals of an inverse inertia matrix must satisfy the triangle inequality: a + b >= c
			// Might need to relax 'tol' due to distorted rotation matrices: using: 'Max(Sum(d), 1) * tiny_sqrt'
			//if (!is_inverse && (
			//	(dia.x + dia.y) < dia.z ||
			//	(dia.y + dia.z) < dia.x ||
			//	(dia.z + dia.x) < dia.y))
			//	return assert(false),false;

			// The magnitude of a product of inertia was too large to be physical.
			//if (!is_inverse && (
			//	dia.x < Abs(2 * off.z) || 
			//	dia.y < Abs(2 * off.y) ||
			//	dia.z < Abs(2 * off.x)))
			//	return assert(false),false;

			return true;
		}
		static bool Check(m6_cref<Force,Motion> inertia_inv)
		{
			// Check for any value == NaN
			if (IsNaN(inertia_inv))
				return assert(false),false;
			
			// Check symmetric
			if (!IsSymmetric(inertia_inv.m00) ||
				!IsSymmetric(inertia_inv.m11))
				return assert(false),false;

			// Check 'Ic¯'
			auto Ic¯ = inertia_inv.m00;
			if (!Check(Ic¯))
				return assert(false),false;

			// Check 'Ic¯ * cxT'
			auto cxT = Invert(Ic¯) * inertia_inv.m01;
			if (!FEql(Trace(cxT), 0.f) ||
				!IsAntiSymmetric(cxT))
				return assert(false),false;

			// Check 'cx * Ic¯'
			auto cx = inertia_inv.m10 * Invert(Ic¯);
			if (!FEql(Trace(cx), 0.f) ||
				!IsAntiSymmetric(cx))
				return assert(false),false;

			// Check 'cx = -cxT'
			if (!FEql(cx + cxT, m3x4{}))
				return assert(false),false;

			// Check '1/m'
			auto im = inertia_inv.m11 + cx * Ic¯ * cx;
			if (!FEql(im.y.y - im.x.x, 0.f) ||
				!FEql(im.z.z - im.x.x, 0.f))
				return assert(false),false;

			return true;
		}

		// An immovable object
		template <typename = void> static InertiaInv Zero()
		{
			return InertiaInv{v4{1,1,1,0}, v4{0,0,0,0}, 0};
		}

		#pragma region Operators
		// Note: there is no operator + because its definition is ambiguous
		//  Ia + Ib can either mean:
		//      Ia.To3x3() + Ib.To3x3() or Ia.To6x6() + Ib.To6x6() 
		//  or weld two rigid bodies together:
		//      (ma*Ia + mb*Ib)/(mamb) 

		// Equality
		friend bool operator == (InertiaInv const& lhs, InertiaInv const& rhs)
		{
			return
				lhs.m_diagonal == rhs.m_diagonal &&
				lhs.m_products == rhs.m_products &&
				lhs.m_com_and_invmass == rhs.m_com_and_invmass;
		}
		friend bool operator != (InertiaInv const& lhs, InertiaInv const& rhs)
		{
			return !(lhs == rhs);
		}

		// Multiply a vector by 'inertia_inv'.
		friend v4 operator * (InertiaInv const& inertia_inv, v4 const& h)
		{
			if (inertia_inv.CoM() == v4{})
				return inertia_inv.To3x3() * h;
			else
				return Translate(inertia_inv, -inertia_inv.CoM(), ETranslateInertia::AwayFromCoM).To3x3() * h;
		}

		// Multiply a spatial force vector by 'inertia_inv' (i.e. F/M = a)
		friend v8motion operator * (InertiaInv const& inertia_inv, v8force const& force)
		{
			// Special case when the inertia is in CoM frame.
			if (inertia_inv.CoM() == v4{})
				return v8motion{inertia_inv.To3x3() * force.ang, inertia_inv.InvMass() * force.lin};
			else
				return inertia_inv.To6x6() * force;
		}
		#pragma endregion
	};

	#pragma region Functions

	// Approximate equality
	inline bool FEql(Inertia const& lhs, Inertia const& rhs)
	{
		return
			FEql(lhs.m_diagonal, rhs.m_diagonal) &&
			FEql(lhs.m_products, rhs.m_products) &&
			FEql(lhs.m_com_and_mass, rhs.m_com_and_mass);
	}
	inline bool FEql(InertiaInv const& lhs, InertiaInv const& rhs)
	{
		return
			FEql(lhs.m_diagonal, rhs.m_diagonal) &&
			FEql(lhs.m_products, rhs.m_products) &&
			FEql(lhs.m_com_and_invmass, rhs.m_com_and_invmass);
	}

	// Add/Subtract two inertias. 'lhs' and 'rhs' must be in the same frame.
	inline Inertia Join(Inertia const& lhs, Inertia const& rhs)
	{
		// Todo: this is not the correct check, so long as the inertias are in the same frame
		// they can be added after parallel axis transformed to a common point.
		if (lhs.CoM() != rhs.CoM())
			throw std::runtime_error("Inertias must be in the same space");

		auto& Ia = lhs;
		auto& Ib = rhs;

		auto massA = Ia.Mass();
		auto massB = Ib.Mass();
		auto mass = massA + massB;
		auto com = lhs.CoM();

		// Once inertia's are in the same space they can just be added.
		// Since these are normalised inertias however we need to add proportionally.
		// i.e.
		//   U = I/m = unit inertia = inertia / mass
		//   I3 = I1 + I2, I1 = m1U1, I2 = m2U2
		//   I3 = m3U3 = m1U1 + m2U2
		//   U3 = (m1U1 + m2U2)/m3
		Inertia sum = {};
		if (mass < maths::tinyf)
		{
			sum.m_diagonal = (Ia.m_diagonal + Ib.m_diagonal) / 2.0f;
			sum.m_products = (Ia.m_products + Ib.m_products) / 2.0f;
			sum.m_com_and_mass = v4{ com, mass };
		}
		else
		{
			sum.m_diagonal = (massA * Ia.m_diagonal + massB * Ib.m_diagonal) / mass;
			sum.m_products = (massA * Ia.m_products + massB * Ib.m_products) / mass;
			sum.m_com_and_mass = v4{ com, mass };
		}
		return sum;
	}
	inline Inertia Split(Inertia const& lhs, Inertia const& rhs)
	{
		if (lhs.CoM() != rhs.CoM())
			throw std::runtime_error("Inertias must be in the same space");

		auto& Ia = lhs;
		auto& Ib = rhs;

		auto massA = Ia.Mass();
		auto massB = Ib.Mass();
		auto mass = massA - massB;
		auto com = lhs.CoM();
		
		// The result must still have a positive mass
		if (mass <= 0)
			throw std::runtime_error("Inertia difference is undefined");
		
		Inertia sum = {};
		sum.m_diagonal = (massA*Ia.m_diagonal - massB*Ib.m_diagonal) / mass;
		sum.m_products = (massA*Ia.m_products - massB*Ib.m_products) / mass;
		sum.m_com_and_mass = v4{com, mass};
		return sum;
	}

	// Add/Subtract inverse inertias. 'lhs' and 'rhs' must be in the same frame.
	inline InertiaInv Join(InertiaInv const& lhs, InertiaInv const& rhs)
	{
		if (lhs.CoM() != rhs.CoM())
			throw std::runtime_error("Inertias must be in the same space");

		auto& Ia¯ = lhs;
		auto& Ib¯ = rhs;

		auto massA = Ia¯.Mass();
		auto massB = Ib¯.Mass();
		auto mass = massA + massB;
		auto com = lhs.CoM();

		InertiaInv sum = {};
		sum.m_diagonal = (massA*Ia¯.m_diagonal + massB*Ib¯.m_diagonal) / mass;
		sum.m_products = (massA*Ia¯.m_products + massB*Ib¯.m_products) / mass;
		sum.m_com_and_invmass = v4{com, 1/mass};
		return sum;
	}
	inline InertiaInv Split(InertiaInv const& lhs, InertiaInv const& rhs)
	{
		if (lhs.CoM() != rhs.CoM())
			throw std::runtime_error("Inertias must be in the same space");

		auto& Ia¯ = lhs;
		auto& Ib¯ = rhs;

		auto massA = Ia¯.Mass();
		auto massB = Ib¯.Mass();
		auto mass = massA - massB;
		auto com = lhs.CoM();
		
		// The result must still have a positive mass
		if (mass <= 0)
			throw std::runtime_error("Inertia difference is undefined");
		
		InertiaInv sum = {};
		sum.m_diagonal = (massA*Ia¯.m_diagonal - massB*Ib¯.m_diagonal) / mass;
		sum.m_products = (massA*Ia¯.m_products - massB*Ib¯.m_products) / mass;
		sum.m_com_and_invmass = v4{com, 1/mass};
		return sum;
	}

	// Invert inertia
	inline InertiaInv Invert(Inertia const& inertia)
	{
		auto unit_inertia¯ = Invert(inertia.Ic3x3(1));
		return InertiaInv{unit_inertia¯, inertia.InvMass(), inertia.CoM()};
	}
	inline Inertia Invert(InertiaInv const& inertia_inv)
	{
		auto unit_inertia = Invert(inertia_inv.Ic3x3(1));
		return Inertia{unit_inertia, inertia_inv.Mass(), inertia_inv.CoM()};
	}

	// Rotate an inertia in frame 'a' to frame 'b'
	inline Inertia Rotate(Inertia const& inertia, m3_cref<> a2b)
	{
		// Ib = a2b*Ia*b2a
		auto b2a = InvertFast(a2b);
		auto Ic = a2b * inertia.Ic3x3(1) * b2a;
		return Inertia{Ic, inertia.Mass(), inertia.CoM()};
	}
	inline InertiaInv Rotate(InertiaInv const& inertia_inv, m3_cref<> a2b)
	{
		// Ib¯ = (a2b*Ia*b2a)¯ = b2a¯*Ia¯*a2b¯ = a2b*Ia¯*b2a
		auto b2a = InvertFast(a2b);
		auto Ic¯ = a2b * inertia_inv.Ic3x3(1) * b2a;
		return InertiaInv{Ic¯, inertia_inv.InvMass(), inertia_inv.CoM()};
	}

	// Returns an inertia translated using the parallel axis theorem.
	// 'offset' is the vector from (or toward) the centre of mass (determined by 'direction').
	// 'offset' must be in the current frame.
	inline Inertia Translate(Inertia const& inertia0, v4_cref<> offset, ETranslateInertia direction)
	{
		//' Io = Ic - cxcx (for unit inertia away from CoM) '
		//' Ic = Io + cxcx (for unit inertia toward CoM)    '
		auto inertia1 = inertia0;
		auto sign = (direction == ETranslateInertia::AwayFromCoM) ? +1.0f : -1.0f;

		// For the diagonal elements:
		//'  I = Io + md² (away from CoM), Io = I - md² (toward CoM) '
		//' 'd' is the perpendicular component of 'offset'
		inertia1.m_diagonal.x += sign * (Sqr(offset.y) + Sqr(offset.z));
		inertia1.m_diagonal.y += sign * (Sqr(offset.z) + Sqr(offset.x));
		inertia1.m_diagonal.z += sign * (Sqr(offset.x) + Sqr(offset.y));

		// For off diagonal elements:
		//'  Ixy = Ioxy + mdxdy  (away from CoM), Io = I - mdxdy (toward CoM) '
		//'  Ixz = Ioxz + mdxdz  (away from CoM), Io = I - mdxdz (toward CoM) '
		//'  Iyz = Ioyz + mdydz  (away from CoM), Io = I - mdydz (toward CoM) '
		inertia1.m_products.x += sign * (offset.x * offset.y); // xy
		inertia1.m_products.y += sign * (offset.x * offset.z); // xz
		inertia1.m_products.z += sign * (offset.y * offset.z); // yz

		// 'com' is mainly used for spatial inertia when multiplying the inertia
		// at a point other than where the inertia was measured at. Translate()
		// moves the measure point, so if 'com' is non-zero, update it to reflect
		// the new offset.
		if (inertia1.m_com_and_mass.xyz != v3{})
			inertia1.m_com_and_mass.xyz -= sign * offset.xyz;

		return inertia1;
	}
	inline InertiaInv Translate(InertiaInv const& inertia0¯, v4_cref<> offset, ETranslateInertia direction)
	{
		auto inertia0 = Invert(inertia0¯);
		auto inertia1 = Translate(inertia0, offset, direction);
		auto inertia1¯ = Invert(inertia1);
		return inertia1¯;
	}

	// Rotate, then translate an inertia
	inline Inertia Transform(Inertia const& inertia0, m4_cref<> a2b, ETranslateInertia direction)
	{
		auto inertia1 = inertia0;
		inertia1 = Rotate(inertia1, a2b.rot);
		inertia1 = Translate(inertia1, a2b.pos, direction);
		return inertia1;
	}
	inline InertiaInv Transform(InertiaInv const& inertia0¯, m4_cref<> a2b, ETranslateInertia direction)
	{
		auto inertia1¯ = inertia0¯;
		inertia1¯ = Rotate(inertia1¯, a2b.rot);
		inertia1¯ = Translate(inertia1¯, a2b.pos, direction);
		return inertia1¯;
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
		{// Inertia Construction
			auto moment = (1.0f/6.0f) * Sqr(2.0f);

			auto I0 = Inertia{moment, mass};
			PR_CHECK(FEql(I0, Inertia{I0.To3x3(1), I0.Mass()}), true);
			PR_CHECK(FEql(I0, Inertia{I0.To6x6()}), true);

			auto I1 = Transform(I0, m4x4::Transform(float(maths::tau_by_4), float(maths::tau_by_4), 0, v4{1,2,3,0}), ETranslateInertia::AwayFromCoM);
			PR_CHECK(FEql(I1, Inertia{I1.To3x3(1), I1.Mass()}), true);
			PR_CHECK(FEql(I1, Inertia{I1.To6x6()}), true);

			// Note: about Inertia{3x3, com} vs. Translate,
			//  Inertia{3x3, com} says "'3x3' is the inertia over there at 'com'"
			//  Translate(3x3, ofs) says "'3x3' is the inertia here at the CoM, now measure it over there at 'ofs'"
			auto I2 = Inertia{I1, -v4{3,2,1,0}};
			PR_CHECK(FEql(I2, Inertia{I2.To6x6()}), true);
		}
		{// InertiaInv Construction
			auto moment = (1.0f/6.0f) * Sqr(2.0f);
			
			auto I0¯ = Invert(Inertia{moment, mass});
			PR_CHECK(FEql(I0¯, InertiaInv{I0¯.To3x3(1), I0¯.InvMass()}), true);
			PR_CHECK(FEql(I0¯, InertiaInv{I0¯.To6x6()}), true);
			
			auto I1¯ = Transform(I0¯, m4x4::Transform(float(maths::tau_by_4), float(maths::tau_by_4), 0, v4{1,2,3,0}), ETranslateInertia::AwayFromCoM);
			PR_CHECK(FEql(I1¯, InertiaInv{I1¯.To3x3(1), I1¯.InvMass()}), true);
			PR_CHECK(FEql(I1¯, InertiaInv{I1¯.To6x6()}), true);

			auto I2¯ = InertiaInv{I1¯, -v4{3,2,1,0}};
			PR_CHECK(FEql(I2¯, InertiaInv{I2¯.To6x6()}), true);
		}
		{// Infinite
			auto inf¯ = Invert(Inertia::Infinite());
			PR_CHECK(inf¯ == InertiaInv::Zero(), true);
			auto inf = Invert(inf¯);
			PR_CHECK(inf == Inertia::Infinite(), true);
		}
		{// Translate and Rotate
			auto moment = (1.0f/6.0f) * Sqr(2.0f);
			auto Ic0 = Inertia{moment, mass};
			auto Ic1 = Ic0;
			
			Ic1 = Translate(Ic1, v4{1,0,0,0}, ETranslateInertia::AwayFromCoM);
			Ic1 = Rotate(Ic1, m3x4::Rotation(float(maths::tau_by_4), 0, 0));
			Ic1 = Rotate(Ic1, m3x4::Rotation(0, float(maths::tau_by_4), 0));
			Ic1 = Translate(Ic1, v4{0,0,1,0}, ETranslateInertia::TowardCoM);
			
			PR_CHECK(FEql(Ic0, Ic1), true);
		}
		{// Transform
			auto moment = (1.0f/6.0f) * Sqr(2.0f);
			auto a2b = m4x4::Transform(float(maths::tau_by_4), float(maths::tau_by_4), 0, v4{0,0,1,1});
			auto Ic0 = Inertia{moment, mass};
			auto Ic1 = Translate(Rotate(Ic0, a2b.rot), a2b.pos, ETranslateInertia::AwayFromCoM);
			auto Ic2 = Transform(Ic0, a2b, ETranslateInertia::AwayFromCoM);
			PR_CHECK(FEql(Ic1, Ic2), true);
		}
		{// Translate Inverse
			auto a2b = m4x4::Transform(float(maths::tau_by_4), float(maths::tau_by_4), 0, v4{0,0,1,1});
			auto Ic0 = Rotate(Inertia::Box(v4{1,2,3,0}, mass, v4{1,1,1,0}), a2b.rot);
			auto Ic0¯ = Invert(Ic0);

			// Translate by invert-translate-invert
			auto Ic1 = Invert(Ic0¯);
			auto Io1 = Translate(Ic1, +a2b.pos, ETranslateInertia::AwayFromCoM);
			auto Io1¯ = Invert(Io1);

			auto Io2 = Invert(Io1¯);
			auto Ic2 = Translate(Io2, -a2b.pos, ETranslateInertia::TowardCoM);
			auto Ic2¯ = Invert(Ic2);

			// Directly translate the inverse inertia
			auto IC0¯ = Ic0¯;
			auto IO1¯ = Translate(IC0¯, +a2b.pos, ETranslateInertia::AwayFromCoM);
			auto IC2¯ = Translate(IO1¯, -a2b.pos, ETranslateInertia::TowardCoM);

			PR_CHECK(FEql(Ic0¯, IC0¯), true);
			PR_CHECK(FEql(Io1¯, IO1¯), true);
			PR_CHECK(FEql(Ic2¯, IC2¯), true);
		}
		{// 6x6 vs 3x3 no offset
			auto avel = v4{0, 0, 1, 0}; //v4(-1,-2,-3,0);
			auto lvel = v4{0, 1, 0, 0}; //v4(+1,+2,+3,0);
			auto vel = v8motion{avel, lvel};

			// Inertia of a sphere with radius 1, positioned at (0,0,0), measured at (0,0,0) (2/5 m r²)
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
		{// 6x6 with offset
			auto avel = v4{0, 0, 1, 0};
			auto lvel = v4{0, 1, 0, 0};
			auto vel = v8motion{avel, lvel};

			// Inertia of a sphere with radius 0.5, positioned at (0,0,0), measured at (0,0,0) (2/5 m r²)
			auto Ic = Inertia::Sphere(0.5f, 1);

			// Calculate the momentum at 'r'
			auto r = v4{1,0,0,0};
			auto amom = mass * (Ic.To3x3() * avel - Cross(r, lvel));
			auto lmom = mass * lvel;

			// Inertia of a sphere with radius 0.5, positioned at (0,0,0) and measured at (0,0,0) expressed at 'r'
			auto vel_r = Shift(vel, r);
			auto sI_r = Inertia(Ic.To3x3(1), mass, -r);
			auto mom = sI_r * vel_r;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);

			// Expressed at another point
			r = v4{2,0,0,0};
			amom = mass * (Ic.To3x3() * avel - Cross(r, lvel));
			lmom = mass * lvel;

			// Inertia of a sphere with radius 0.5, positioned at (0,0,0) and measured at (0,0,0) expressed at 'r'
			vel_r = Shift(vel, r);
			sI_r = Inertia(Ic.To3x3(1), mass, -r);
			mom = sI_r * vel_r;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);

			// Expressed at another point
			r = v4{1,2,3,0};
			amom = mass * (Ic.To3x3() * avel - Cross(r, lvel));
			lmom = mass * lvel;

			// Inertia of a sphere with radius 0.5, positioned at (0,0,0) and measured at (0,0,0) expressed at 'r'
			vel_r = Shift(vel, r);
			sI_r = Inertia(Ic.To3x3(1), mass, -r);
			mom = sI_r * vel_r;
			PR_CHECK(FEql(mom.ang, amom), true);
			PR_CHECK(FEql(mom.lin, lmom), true);
		}
		{// Addition/Subtraction inertia
			auto sph0 = Inertia::Sphere(0.5f, mass);
			auto sph1 = Inertia::Sphere(0.5f, mass);

			// Simple addition/subtraction of inertia in CoM frame
			auto sph2 = Inertia::Sphere(0.5f, 2*mass);
			auto SPH2 = Join(sph0, sph1);
			auto SPH3 = Split(sph2, sph1);
			PR_CHECK(FEql(sph2, SPH2), true);
			PR_CHECK(FEql(sph0, SPH3), true);

			// Addition/Subtraction of translated inertias
			auto sph4 = Translate(sph0, v4{-1,0,0,0}, ETranslateInertia::AwayFromCoM);
			auto sph5 = Translate(sph1, v4{+1,0,0,0}, ETranslateInertia::AwayFromCoM);
			auto sph6 = Inertia{v4{0.1f,1.1f,1.1f,0}, v4{}, 2*mass};
			auto SPH6 = Join(sph4, sph5);
			auto SPH7 = Split(sph6, sph4);
			PR_CHECK(FEql(sph6, SPH6), true);
			PR_CHECK(FEql(sph5, SPH7), true);

			// Addition/Subtraction of inertias with offsets
			auto sph8 = Inertia{sph0, v4{1,2,3,0}};
			auto sph9 = Inertia{sph1, v4{1,2,3,0}};
			auto sph10 = Inertia{sph2, v4{1,2,3,0}};
			auto SPH10 = Join(sph8, sph9);
			auto SPH11 = Split(sph10, sph9);
			PR_CHECK(FEql(sph10, SPH10), true);
			PR_CHECK(FEql(sph8, SPH11), true);
		}
		{// Addition/Subtraction inverse inertia
			auto sph0 = Inertia::Sphere(0.5f, mass);
			auto sph1 = Inertia::Sphere(0.5f, mass);
			auto sph2 = Inertia::Sphere(0.5f, 2*mass);

			// Simple addition/subtraction of inertia in CoM frame
			auto SPH2 = Join(Invert(sph0), Invert(sph1));
			auto SPH3 = Split(Invert(sph2), Invert(sph1));
			PR_CHECK(FEql(Invert(sph2), SPH2), true);
			PR_CHECK(FEql(Invert(sph0), SPH3), true);

			// Addition/Subtraction of translated inertias
			auto sph4 = Translate(sph0, v4{-1,0,0,0}, ETranslateInertia::AwayFromCoM);
			auto sph5 = Translate(sph1, v4{+1,0,0,0}, ETranslateInertia::AwayFromCoM);
			auto sph6 = Inertia{v4{0.1f,1.1f,1.1f,0}, v4{}, 2*mass};
			auto SPH6 = Join(Invert(sph4), Invert(sph5));
			auto SPH7 = Split(Invert(sph6), Invert(sph4));
			PR_CHECK(FEql(Invert(sph6), SPH6), true);
			PR_CHECK(FEql(Invert(sph5), SPH7), true);

			// Addition/Subtraction of inertias with offsets
			auto sph8 = Inertia{sph0, v4{1,2,3,0}};
			auto sph9 = Inertia{sph1, v4{1,2,3,0}};
			auto sph10 = Inertia{sph2, v4{1,2,3,0}};
			auto SPH10 = Join(Invert(sph8), Invert(sph9));
			auto SPH11 = Split(Invert(sph10), Invert(sph9));
			PR_CHECK(FEql(Invert(sph10), SPH10), true);
			PR_CHECK(FEql(Invert(sph8), SPH11), true);
		}
		{// Inverting 6x6 inertia
			auto Ic = Inertia::Sphere(0.5f, 1);
			auto r = v4{1,2,3,0};
			
			auto a = Inertia{Ic.Ic3x3(1), mass, -r};
			auto b = Invert(a);
			auto c = Invert(b);
			auto a6x6 = a.To6x6();
			auto b6x6 = b.To6x6();
			auto c6x6 = c.To6x6();
			
			auto A = a.To6x6();
			auto B = Invert(A);
			auto C = Invert(B);

			PR_CHECK(FEqlRelative(a6x6, A, 0.001f), true);
			PR_CHECK(FEqlRelative(b6x6, B, 0.001f), true);
			PR_CHECK(FEqlRelative(c6x6, C, 0.001f), true);
		}
		{// a = I¯.f
			auto F = 2.0f;
			auto L = 1.0f;
			auto I = (1.0f/12.0f) * mass * Sqr(L); // I = 1/12 m L²

			// Create a vertical rod inertia
			auto Ic = Inertia::Box(v4{0.0001f, 0.5f*L, 0.0001f,0}, mass);
			auto Ic¯ = Invert(Ic);

			// Apply a force at the CoM
			auto f0 = v8force{0, 0, 0, F, 0, 0};
			auto a0 = Ic¯ * f0;
			PR_CHECK(FEql(a0, v8motion{0, 0, 0, F/mass, 0, 0}), true);

			// Apply a force at the top
			// a = F/m, A = F.d/I
			auto r = v4{0, 0.5f*L, 0, 0};
			auto f1 = Shift(f0, -r);
			auto a1 = Ic¯ * f1;
			PR_CHECK(FEql(a1, v8motion{0, 0, -F*r.y/I, F/mass, 0, 0}), true);

			// Apply a force at an arbitrary point
			r = v4{3,2,0,0};
			auto f2 = Shift(f0, -r);
			auto a2 = Ic¯ * f2;
			auto a = (1.0f/mass)*f0.lin;
			auto A = Ic¯.To3x3() * Cross(r, f0.lin);
			PR_CHECK(FEql(a2, v8motion{A, a}), true);
		}
		{ // Kinetic energy: 0.5 * Dot(v, h) = 0.5 * v.I.v

			// Sphere travelling at 'vel'
			auto avel = v4{0, 0, 1, 0}; //v4(-1,-2,-3,0);
			auto lvel = v4{0, 1, 0, 0}; //v4(+1,+2,+3,0);
			auto vel = v8motion{avel, lvel};

			// Inertia of a sphere with radius 1, positioned at (0,0,0), measured at (0,0,0) (2/5 m r²)
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
