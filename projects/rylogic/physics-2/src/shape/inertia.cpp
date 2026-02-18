//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#include "pr/physics-2/shape/inertia.h"

namespace pr::physics
{
	// Approximate equality
	bool FEql(Inertia const& lhs, Inertia const& rhs)
	{
		return
			FEql(lhs.m_diagonal, rhs.m_diagonal) &&
			FEql(lhs.m_products, rhs.m_products) &&
			FEql(lhs.m_com_and_mass, rhs.m_com_and_mass);
	}
	bool FEql(InertiaInv const& lhs, InertiaInv const& rhs)
	{
		return
			FEql(lhs.m_diagonal, rhs.m_diagonal) &&
			FEql(lhs.m_products, rhs.m_products) &&
			FEql(lhs.m_com_and_invmass, rhs.m_com_and_invmass);
	}

	// Add/Subtract two inertias. 'lhs' and 'rhs' must be in the same frame.
	Inertia Join(Inertia const& lhs, Inertia const& rhs)
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
	Inertia Split(Inertia const& lhs, Inertia const& rhs)
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
	InertiaInv Join(InertiaInv const& lhs, InertiaInv const& rhs)
	{
		if (lhs.CoM() != rhs.CoM())
			throw std::runtime_error("Inertias must be in the same space");

		auto& Ia_inv = lhs;
		auto& Ib_inv = rhs;

		auto massA = Ia_inv.Mass();
		auto massB = Ib_inv.Mass();
		auto mass = massA + massB;
		auto com = lhs.CoM();

		InertiaInv sum = {};
		sum.m_diagonal = (massA*Ia_inv.m_diagonal + massB*Ib_inv.m_diagonal) / mass;
		sum.m_products = (massA*Ia_inv.m_products + massB*Ib_inv.m_products) / mass;
		sum.m_com_and_invmass = v4{com, 1/mass};
		return sum;
	}
	InertiaInv Split(InertiaInv const& lhs, InertiaInv const& rhs)
	{
		if (lhs.CoM() != rhs.CoM())
			throw std::runtime_error("Inertias must be in the same space");

		auto& Ia_inv = lhs;
		auto& Ib_inv = rhs;

		auto massA = Ia_inv.Mass();
		auto massB = Ib_inv.Mass();
		auto mass = massA - massB;
		auto com = lhs.CoM();
		
		// The result must still have a positive mass
		if (mass <= 0)
			throw std::runtime_error("Inertia difference is undefined");
		
		InertiaInv sum = {};
		sum.m_diagonal = (massA*Ia_inv.m_diagonal - massB*Ib_inv.m_diagonal) / mass;
		sum.m_products = (massA*Ia_inv.m_products - massB*Ib_inv.m_products) / mass;
		sum.m_com_and_invmass = v4{com, 1/mass};
		return sum;
	}

	// Invert inertia
	InertiaInv Invert(Inertia const& inertia)
	{
		auto unit_inertia_inv = Invert(inertia.Ic3x3(1));
		return InertiaInv{unit_inertia_inv, inertia.InvMass(), inertia.CoM()};
	}
	Inertia Invert(InertiaInv const& inertia_inv)
	{
		auto unit_inertia = Invert(inertia_inv.Ic3x3(1));
		return Inertia{unit_inertia, inertia_inv.Mass(), inertia_inv.CoM()};
	}

	// Rotate an inertia in frame 'a' to frame 'b'
	Inertia Rotate(Inertia const& inertia, m3_cref a2b)
	{
		// Ib = a2b*Ia*b2a
		auto b2a = InvertAffine(a2b);
		auto Ic = a2b * inertia.Ic3x3(1) * b2a;
		return Inertia{Ic, inertia.Mass(), inertia.CoM()};
	}
	InertiaInv Rotate(InertiaInv const& inertia_inv, m3_cref a2b)
	{
		// Ib¯ = (a2b*Ia*b2a)¯ = b2a¯*Ia¯*a2b¯ = a2b*Ia¯*b2a
		auto b2a = InvertAffine(a2b);
		auto Ic_inv = a2b * inertia_inv.Ic3x3(1) * b2a;
		return InertiaInv{Ic_inv, inertia_inv.InvMass(), inertia_inv.CoM()};
	}

	// Returns an inertia translated using the parallel axis theorem.
	// 'offset' is the vector from (or toward) the centre of mass (determined by 'direction').
	// 'offset' must be in the current frame.
	Inertia Translate(Inertia const& inertia0, v4_cref offset, ETranslateInertia direction)
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
	InertiaInv Translate(InertiaInv const& inertia0_inv, v4_cref offset, ETranslateInertia direction)
	{
		auto inertia0 = Invert(inertia0_inv);
		auto inertia1 = Translate(inertia0, offset, direction);
		auto inertia1_inv = Invert(inertia1);
		return inertia1_inv;
	}

	// Rotate, then translate an inertia
	Inertia Transform(Inertia const& inertia0, m4_cref a2b, ETranslateInertia direction)
	{
		auto inertia1 = inertia0;
		inertia1 = Rotate(inertia1, a2b.rot);
		inertia1 = Translate(inertia1, a2b.pos, direction);
		return inertia1;
	}
	InertiaInv Transform(InertiaInv const& inertia0_inv, m4_cref a2b, ETranslateInertia direction)
	{
		auto inertia1_inv = inertia0_inv;
		inertia1_inv = Rotate(inertia1_inv, a2b.rot);
		inertia1_inv = Translate(inertia1_inv, a2b.pos, direction);
		return inertia1_inv;
	}
}
