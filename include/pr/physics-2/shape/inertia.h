//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics-2/forward.h"
#include "pr/physics-2/shape/mass.h"

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
	Inertia Rotate(Inertia const& inertia, m3_cref a2b);
	InertiaInv Rotate(InertiaInv const& inertia_inv, m3_cref a2b);
	Inertia Translate(Inertia const& inertia0, v4_cref offset, ETranslateInertia direction);
	InertiaInv Translate(InertiaInv const& inertia0_inv, v4_cref offset, ETranslateInertia direction);
	Inertia Transform(Inertia const& inertia0, m4_cref a2b, ETranslateInertia direction);
	InertiaInv Transform(InertiaInv const& inertia0_inv, m4_cref a2b, ETranslateInertia direction);
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
		Inertia(m3_cref unit_inertia, float mass, v4_cref com = v4{})
			:m_diagonal(unit_inertia.x.x, unit_inertia.y.y, unit_inertia.z.z, 0)
			,m_products(unit_inertia.x.y, unit_inertia.x.z, unit_inertia.y.z, 0)
			,m_com_and_mass(com.xyz, mass)
		{
			assert(Check());
		}
		Inertia(v4_cref diagonal, v4_cref products, float mass, v4_cref com = v4{})
			:m_diagonal(diagonal)
			,m_products(products)
			,m_com_and_mass(com.xyz, mass)
		{
			assert(Check());
		}
		Inertia(float diagonal, float mass, v4_cref com = v4{})
			:m_diagonal(diagonal, diagonal, diagonal, 0)
			,m_products()
			,m_com_and_mass(com.xyz, mass)
		{
			assert(Check());
		}
		Inertia(Inertia const& rhs, v4_cref com)
			:m_diagonal(rhs.m_diagonal)
			,m_products(rhs.m_products)
			,m_com_and_mass(com.xyz, rhs.Mass())
		{
			assert(Check());
		}
		explicit Inertia(Mat6x8_cref<float,Motion,Force> inertia, float mass = -1)
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
		static bool Check(m3_cref inertia)
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
		static bool Check(Mat6x8_cref<float,Motion,Force> inertia)
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
		template <typename = void> static Inertia Point(float mass, v4_cref offset = v4{})
		{
			auto ib = Inertia{1.0f, mass};
			ib = Translate(ib, offset, ETranslateInertia::AwayFromCoM);
			return ib;
		}

		// Create an inertia matrix for a sphere at 'offset'
		template <typename = void> static Inertia Sphere(float radius, float mass, v4_cref offset = v4{})
		{
			auto ib = Inertia{(2.0f/5.0f) * Sqr(radius), mass};
			ib = Translate(ib, offset, ETranslateInertia::AwayFromCoM);
			return ib;
		}

		// Create an inertia matrix for a box at 'offset'
		template <typename = void> static Inertia Box(v4_cref radius, float mass, v4_cref offset = v4{})
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
		InertiaInv(m3_cref unit_inertia_inv, float invmass, v4_cref com = v4{})
			:m_diagonal(unit_inertia_inv.x.x, unit_inertia_inv.y.y, unit_inertia_inv.z.z, 0)
			,m_products(unit_inertia_inv.x.y, unit_inertia_inv.x.z, unit_inertia_inv.y.z, 0)
			,m_com_and_invmass(com.xyz, invmass)
		{
			assert(Check());
		}
		InertiaInv(v4_cref diagonal, v4_cref products, float invmass, v4_cref com = v4{})
			:m_diagonal(diagonal)
			,m_products(products)
			,m_com_and_invmass(com.xyz, invmass)
		{
			assert(Check());
		}
		InertiaInv(InertiaInv const& rhs, v4_cref com)
			:m_diagonal(rhs.m_diagonal)
			,m_products(rhs.m_products)
			,m_com_and_invmass(com.xyz, rhs.InvMass())
		{
			assert(Check());
		}
		InertiaInv(Mat6x8_cref<float,Force,Motion> inertia_inv, float invmass = -1)
		{
			// If 'invmass' is given, 'inertia_inv' is assumed to be a unit inverse inertia
			assert(InertiaInv::Check(inertia_inv));

			auto Ic_inv = inertia_inv.m00;
			auto cx  = inertia_inv.m10 * Invert(Ic_inv);
			auto im = invmass >= 0 ? invmass : Trace(inertia_inv.m11 + cx*Ic_inv*cx) / 3.0f;
			*this = InertiaInv{(1/im)*Ic_inv, im, v4{cx.y.z, -cx.x.z, cx.x.y, 0}};
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
			auto Ic_inv = m3x4{
				v4{dia.x, off.x, off.y, 0},
				v4{off.x, dia.y, off.z, 0},
				v4{off.y, off.z, dia.z, 0}};
			return Ic_inv;
		}

		// The mass scaled inverse inertia matrix
		m3x4 To3x3(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			if (inv_mass < ZeroMass || inv_mass >= InfiniteMass)
				return m3x4::Identity();

			auto Ic_inv = Ic3x3(inv_mass);
			if (CoM() == v4{})
				return Ic_inv;

			//' Io¯ = (Ic - mcxcx)¯                                  '
			//' Identity: (A + B)¯ = A¯ - (1 + A¯B)¯A¯BA¯            '
			//'   Let A = Ic, B = -mcxcx                             '
			//'  Then:                                               '
			//' Io¯ = Ic¯ + m(1 - mIc¯cxcx)¯Ic¯cxcxIc¯               '
			//'     = Ic¯ + (1/m - Ic¯cxcx)¯Ic¯cxcxIc¯               '

			// This is cheaper
			auto cx = CPM(CoM());
			auto Io = Invert(Ic_inv) - (1.0f/inv_mass) * cx * cx;
			auto Io_inv = Invert(Io);
			return Io_inv;
		}

		// Return the inverse inertia matrix as a full spatial matrix
		Mat6x8f<Force,Motion> To6x6(float inv_mass = -1) const
		{
			inv_mass = inv_mass >= 0 ? inv_mass : InvMass();
			if (inv_mass < ZeroMass || inv_mass >= InfiniteMass)
				return Mat6x8f<Force,Motion>{m6x8Identity};

			auto Ic_inv = Ic3x3(inv_mass);
			auto cx  = CPM(CoM());
			auto Io_inv = Mat6x8f<Force,Motion>{Ic_inv, -Ic_inv*cx, cx*Ic_inv, inv_mass*m3x4Identity - cx*Ic_inv*cx};
			return Io_inv;
		}

		// Sanity check
		bool Check() const
		{
			return CoM() == v4{} ? InertiaInv::Check(To3x3()) : InertiaInv::Check(To6x6());
		}
		static bool Check(m3_cref inertia_inv)
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
		static bool Check(Mat6x8_cref<float,Force,Motion> inertia_inv)
		{
			// Check for any value == NaN
			if (IsNaN(inertia_inv))
				return assert(false),false;
			
			// Check symmetric
			if (!IsSymmetric(inertia_inv.m00) ||
				!IsSymmetric(inertia_inv.m11))
				return assert(false),false;

			// Check 'Ic¯'
			auto Ic_inv = inertia_inv.m00;
			if (!Check(Ic_inv))
				return assert(false),false;

			// Check 'Ic¯ * cxT'
			auto cxT = Invert(Ic_inv) * inertia_inv.m01;
			if (!FEql(Trace(cxT), 0.f) ||
				!IsAntiSymmetric(cxT))
				return assert(false),false;

			// Check 'cx * Ic¯'
			auto cx = inertia_inv.m10 * Invert(Ic_inv);
			if (!FEql(Trace(cx), 0.f) ||
				!IsAntiSymmetric(cx))
				return assert(false),false;

			// Check 'cx = -cxT'
			if (!FEql(cx + cxT, m3x4{}))
				return assert(false),false;

			// Check '1/m'
			auto im = inertia_inv.m11 + cx * Ic_inv * cx;
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

}

