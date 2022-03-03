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
#include "pr/collision/shape_box.h"

namespace pr::collision
{
	// Test for overlap between an orientated box and a sphere, with generic penetration collection
	template <typename Penetration>
	void SphereVsBox(Shape const& lhs, m4_cref<> l2w_, Shape const& rhs, m4_cref<> r2w_, Penetration& pen)
	{
		auto& sph = shape_cast<ShapeSphere>(lhs);
		auto& box = shape_cast<ShapeBox   >(rhs);
		auto l2w = l2w_ * lhs.m_s2p;
		auto r2w = r2w_ * rhs.m_s2p;

		// Convert into box space
		// Box centre to sphere centre vector in box space
		auto l2r = InvertFast(r2w) * l2w.pos - v4Origin;
	
		// Get a vector from the sphere to the nearest point on the box
		auto closest = v4Zero;
		auto dist_sq = 0.0f;
		for (int i = 0; i != 3; ++i)
		{
			if (l2r[i] > box.m_radius[i])
			{
				dist_sq += Sqr(l2r[i] - box.m_radius[i]);
				closest[i] = box.m_radius[i];
			}
			else if (l2r[i] < -box.m_radius[i])
			{
				dist_sq += Sqr(l2r[i] + box.m_radius[i]);
				closest[i] = -box.m_radius[i];
			}
			else
			{
				closest[i] = l2r[i];
			}
		}
	
		// If 'dist_sq' is zero then the centre of the sphere is inside the box
		// The separating axis is in one of the box axis directions
		if (dist_sq < maths::tinyf)
		{
			auto i = MaxElementIndex(Abs(l2r).xyz);

			// Find the penetration depth
			auto depth = sph.m_radius + box.m_radius[i] - Abs(l2r[i]);
			pen(depth, [&]
			{
				// Find the separating axis
				auto norm = v4Zero;
				norm[i] = Sign(l2r[i]);
				return r2w * norm;
			}, lhs.m_material_id, rhs.m_material_id);
		}

		// Otherwise the centre of the sphere is outside of the box
		else
		{
			// Find the penetration depth
			auto dist = Sqrt(dist_sq);
			auto depth = sph.m_radius - dist;
			pen(depth, [&]
			{
				// Find the separating axis
				return r2w * ((l2r - closest) / dist);
			}, lhs.m_material_id, rhs.m_material_id);
		}
	}

	// Returns true if the sphere  'lhs' intersects the orientated box 'rhs'
	inline bool SphereVsBox(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w)
	{
		TestPenetration p;
		SphereVsBox(lhs, l2w, rhs, r2w, p);
		return p.Contact();
	}

	// Returns true if 'lhs' and 'rhs' are intersecting.
	inline bool SphereVsBox(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w, Contact& contact)
	{
		ContactPenetration p;
		SphereVsBox(lhs, l2w, rhs, r2w, p);
		if (!p.Contact())
			return false;

		// Determine the sign of the separating axis to make it the normal from 'lhs' to 'rhs'
		auto sep_axis = p.SeparatingAxis();
		auto p0 = Dot3(sep_axis, (l2w * lhs.m_s2p).pos);
		auto p1 = Dot3(sep_axis, (r2w * rhs.m_s2p).pos);
		auto sign = Bool2SignF(p0 < p1);

		contact.m_depth   = p.Depth();
		contact.m_axis    = sign * sep_axis;
		contact.m_point   = FindContactPoint(shape_cast<ShapeSphere>(lhs), l2w, shape_cast<ShapeBox>(rhs), r2w, contact.m_axis, contact.m_depth);
		contact.m_mat_idA = p.m_mat_idA;
		contact.m_mat_idB = p.m_mat_idB;
		return true;
	}
}

