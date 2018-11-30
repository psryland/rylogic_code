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
		auto len = Length3(r2l);
		auto sep = lhs.m_radius + rhs.m_radius - len;
		pen(sep, [&]{ return r2l/len; });
	}

	// Returns true if 'lhs' intersects 'rhs'
	inline bool SphereVsSphere(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w)
	{
		TestPenetration p;
		SphereVsSphere(lhs, l2w, rhs, r2w, p);
		return p.Contact();
	}

	// Returns true if 'lhs' and 'rhs' are intersecting.
	// 'axis' is the collision normal from 'lhs' to 'rhs'
	// 'penetration' is the depth of penetration between the shapes (positive for contact)
	inline bool SphereVsSphere(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w, v4& axis, float& penetration)
	{
		ContactPenetration p;
		SphereVsSphere(lhs, l2w, rhs, r2w, p);
		if (!p.Contact())
			return false;

		// Determine the sign of the separating axis to make it the normal from 'lhs' to 'rhs'
		auto sep_axis = p.SeparatingAxis();
		auto p0 = Dot3(sep_axis, (l2w * lhs.m_s2p).pos);
		auto p1 = Dot3(sep_axis, (r2w * rhs.m_s2p).pos);
		auto sign = SignF(p0 < p1);

		penetration = p.Depth();
		axis        = sign * sep_axis;
		return true;
	}

	// Returns true if 'lhs' and 'rhs' are intersecting.
	// 'axis' is the collision normal from 'lhs' to 'rhs'
	// 'penetration' is the depth of penetration between the shapes
	// 'point' is the world space contact point between 'lhs','rhs' (Only valid when true is returned)
	// To find the deepest points on 'lhs','rhs' add/subtract half the 'penetration' depth along 'axis'.
	// Note: that applied impulses should be equal and opposite, and applied at the same point in space (hence one contact point).
	inline bool SphereVsSphere(Shape const& lhs, m4_cref<> l2w, Shape const& rhs, m4_cref<> r2w, v4& axis, float& penetration, v4& point)
	{
		if (!SphereVsSphere(lhs, l2w, rhs, r2w, axis, penetration))
			return false;

		point = FindContactPoint(shape_cast<ShapeSphere>(lhs), l2w, shape_cast<ShapeSphere>(rhs), r2w, axis, penetration);
		return true;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/ldraw/ldr_helper.h"
namespace pr::collision
{
	PRUnitTest(CollisionSphereVsSphere)
	{
		auto lhs = ShapeSphere{0.3f};
		auto rhs = ShapeSphere{0.4f};
		m4x4 l2w_[] =
		{
			m4x4Identity,
		};
		m4x4 r2w_[] =
		{
			m4x4::Transform(float(maths::tau_by_8), float(maths::tau_by_8), float(maths::tau_by_8), v4(0.2f, 0.3f, 0.1f, 1.0f)),
		};

		std::default_random_engine rng;
		for (int i = 0; i != 20; ++i)
		{
			v4 axis, pt; float pen;
			m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : Random4x4(rng, v4Origin, 0.5f);
			m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : Random4x4(rng, v4Origin, 0.5f);

			std::string s;
			ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
			ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
			//ldr::Write(s, L"collision_unittests.ldr");
			if (SphereVsSphere(lhs, l2w, rhs, r2w, axis, pen, pt))
			{
				ldr::LineD(s, "sep_axis", Colour32Yellow, pt, axis);
				ldr::Box(s, "pt0", Colour32Yellow, 0.01f, pt - 0.5f*pen*axis);
				ldr::Box(s, "pt1", Colour32Yellow, 0.01f, pt + 0.5f*pen*axis);
			}
			//ldr::Write(s, L"collision_unittests.ldr");
		}
	}
}
#endif
