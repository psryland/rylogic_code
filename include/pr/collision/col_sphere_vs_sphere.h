//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once
#include "pr/maths/maths.h"
#include "pr/geometry/closest_point.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"
#include "pr/collision/shape.h"
#include "pr/collision/shape_sphere.h"

namespace pr::collision
{
	// Test for collision between two spheres
	template <typename Penetration>
	void SphereVsSphere(Shape const& lhs_, m4_cref<> l2w_, Shape const& rhs_, m4_cref<> r2w_, Penetration& pen)
	{
		auto& lhs = shape_cast<ShapeSphere>(lhs_);
		auto& rhs = shape_cast<ShapeSphere>(rhs_);
		auto l2w = l2w_ * lhs_.m_s2p;
		auto r2w = r2w_ * rhs_.m_s2p;

		// Distance between centres
		auto r2l = r2w.pos - l2w.pos;
		auto len = Length(r2l);
		auto sep = lhs.m_radius + rhs.m_radius - len;
		pen(sep, [&]{ return r2l/len; }, lhs_.m_material_id, rhs_.m_material_id);
	}

	// Returns true if 'lhs' intersects 'rhs'
	inline bool SphereVsSphere(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w)
	{
		TestPenetration p;
		SphereVsSphere(lhs, l2w, rhs, r2w, p);
		return p.Contact();
	}

	// Returns true if 'lhs' and 'rhs' are intersecting.
	inline bool SphereVsSphere(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w, Contact& contact)
	{
		ContactPenetration p;
		SphereVsSphere(lhs, l2w, rhs, r2w, p);
		if (!p.Contact())
			return false;

		// Determine the sign of the separating axis to make it the normal from 'lhs' to 'rhs'
		auto sep_axis = p.SeparatingAxis();
		auto p0 = Dot3(sep_axis, (l2w * lhs.m_s2p).pos);
		auto p1 = Dot3(sep_axis, (r2w * rhs.m_s2p).pos);
		auto sign = Bool2SignF(p0 < p1);

		contact.m_depth   = p.Depth();
		contact.m_axis    = sign * sep_axis;
		contact.m_point   = FindContactPoint(shape_cast<ShapeSphere>(lhs), l2w, shape_cast<ShapeSphere>(rhs), r2w, contact.m_axis, contact.m_depth);
		contact.m_mat_idA = p.m_mat_idA;
		contact.m_mat_idB = p.m_mat_idB;
		return true;
	}
}

