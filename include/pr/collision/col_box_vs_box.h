//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#include "pr/maths/maths.h"
#include "pr/geometry/closest_point.h"
#include "pr/collision/penetration.h"
#include "pr/collision/support.h"
#include "pr/collision/shape_box.h"
#include "pr/collision/ldr_helper.h"

namespace pr
{
	namespace collision
	{
		// Test for overlap between two oriented boxes, with generic penetration collection
		template <typename Penetration>
		bool BoxVsBox(Shape const& lhs_, pr::m4x4 const& l2w_, Shape const& rhs_, pr::m4x4 const& r2w_, Penetration& pen)
		{
			auto& lhs = shape_cast<ShapeBox>(lhs_);
			auto& rhs = shape_cast<ShapeBox>(rhs_);
			auto l2w = l2w_ * lhs_.m_s2p;
			auto r2w = r2w_ * rhs_.m_s2p;

			// Compute a transform for 'rhs' in 'lhs's frame
			auto r2l = InvertFast(l2w) * r2w;

			// Compute common subexpressions. Add in an epsilon term to counteract arithmetic
			// errors when two edges are parallel and their cross product is (near) 0
			auto r2l_abs = Abs(r2l.rot) + maths::tiny;

			float ra, rb, sp;

			// Test axes L = lhs.x, L = lhs.y, L = lhs.z
			for (int i = 0; i != 3; ++i)
			{
				ra = lhs.m_radius[i];
				rb = rhs.m_radius.x * r2l_abs.x[i] + rhs.m_radius.y * r2l_abs.y[i] + rhs.m_radius.z * r2l_abs.z[i];
				sp = Abs(r2l.pos[i]);
				if (sp > ra + rb) return false;
				pen(l2w[i], ra + rb - sp);
			}

			// Test axes L = rhs.x, L = rhs.y, L = rhs.z
			for (int i = 0; i != 3; ++i)
			{
				ra = Dot3(lhs.m_radius, r2l_abs[i]);
				rb = rhs.m_radius[i];
				sp = Abs(Dot3(r2l.pos, r2l[i]));
				if (sp > ra + rb) return false;
				pen(r2w[i], ra + rb - sp);
			}

			// Test axis L = lhs.x X rhs.x
			ra = lhs.m_radius.y * r2l_abs.x.z + lhs.m_radius.z * r2l_abs.x.y;
			rb = rhs.m_radius.y * r2l_abs.z.x + rhs.m_radius.z * r2l_abs.y.x;
			sp = Abs(r2l.pos.z * r2l.x.y - r2l.pos.y * r2l.x.z);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.x, r2w.x), ra + rb - sp);

			// Test axis L = lhs.x X rhs.y
			ra = lhs.m_radius.y * r2l_abs.y.z + lhs.m_radius.z * r2l_abs.y.y;
			rb = rhs.m_radius.x * r2l_abs.z.x + rhs.m_radius.z * r2l_abs.x.x;
			sp = Abs(r2l.pos.z * r2l.y.y - r2l.pos.y * r2l.y.z);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.x, r2w.y), ra + rb - sp);

			// Test axis L = lhs.x X rhs.z
			ra = lhs.m_radius.y * r2l_abs.z.z + lhs.m_radius.z * r2l_abs.z.y;
			rb = rhs.m_radius.x * r2l_abs.y.x + rhs.m_radius.y * r2l_abs.x.x;
			sp = Abs(r2l.pos.z * r2l.z.y - r2l.pos.y * r2l.z.z);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.x, r2w.z), ra + rb - sp);

			// Test axis L = lhs.y X rhs.x
			ra = lhs.m_radius.x * r2l_abs.x.z + lhs.m_radius.z * r2l_abs.x.x;
			rb = rhs.m_radius.y * r2l_abs.z.y + rhs.m_radius.z * r2l_abs.y.y;
			sp = Abs(r2l.pos.x * r2l.x.z - r2l.pos.z * r2l.x.x);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.y, r2w.x), ra + rb - sp);

			// Test axis L = lhs.y X rhs.y
			ra = lhs.m_radius.x * r2l_abs.y.z + lhs.m_radius.z * r2l_abs.y.x;
			rb = rhs.m_radius.x * r2l_abs.z.y + rhs.m_radius.z * r2l_abs.x.y;
			sp = Abs(r2l.pos.x * r2l.y.z - r2l.pos.z * r2l.y.x);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.y, r2w.y), ra + rb - sp);

			// Test axis L = lhs.y X rhs.z
			ra = lhs.m_radius.x * r2l_abs.z.z + lhs.m_radius.z * r2l_abs.z.x;
			rb = rhs.m_radius.x * r2l_abs.y.y + rhs.m_radius.y * r2l_abs.x.y;
			sp = Abs(r2l.pos.x * r2l.z.z - r2l.pos.z * r2l.z.x);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.y, r2w.z), ra + rb - sp);

			// Test axis L = lhs.z X rhs.x
			ra = lhs.m_radius.x * r2l_abs.x.y + lhs.m_radius.y * r2l_abs.x.x;
			rb = rhs.m_radius.y * r2l_abs.z.z + rhs.m_radius.z * r2l_abs.y.z;
			sp = Abs(r2l.pos.y * r2l.x.x - r2l.pos.x * r2l.x.y);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.z, r2w.x), ra + rb - sp);

			// Test axis L = lhs.z X rhs.y
			ra = lhs.m_radius.x * r2l_abs.y.y + lhs.m_radius.y * r2l_abs.y.x;
			rb = rhs.m_radius.x * r2l_abs.z.z + rhs.m_radius.z * r2l_abs.x.z;
			sp = Abs(r2l.pos.y * r2l.y.x - r2l.pos.x * r2l.y.y);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.z, r2w.y), ra + rb - sp);

			// Test axis L = lhs.z X rhs.z
			ra = lhs.m_radius.x * r2l_abs.z.y + lhs.m_radius.y * r2l_abs.z.x;
			rb = rhs.m_radius.x * r2l_abs.y.z + rhs.m_radius.y * r2l_abs.x.z;
			sp = Abs(r2l.pos.y * r2l.z.x - r2l.pos.x * r2l.z.y);
			if (sp > ra + rb) return false;
			pen(Cross3(l2w.z, r2w.z), ra + rb - sp);

			// Since no separating axis is found, the boxes intersect
			return true;
		}

		// Returns true if orientated boxes 'lhs' and 'rhs' are intersecting.
		inline bool BoxVsBox(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w)
		{
			IgnorePenetration p;
			return BoxVsBox(lhs, l2w, rhs, r2w, p);
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the boxes
		inline bool BoxVsBox(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration)
		{
			MinPenetration p;
			if (!BoxVsBox(lhs, l2w, rhs, r2w, p))
				return false;

			// Determine the sign of the separating axis to make it the normal from 'lhs' to 'rhs'
			auto p0 = Dot3(p.m_sep_axis, (l2w * lhs.m_s2p).pos);
			auto p1 = Dot3(p.m_sep_axis, (r2w * rhs.m_s2p).pos);
			auto sign = Sign<float>(p0 < p1);

			penetration = p.m_penetration;
			axis        = sign * p.m_sep_axis;
			return true;
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the shapes
		// 'point' is the world space contact point between 'lhs','rhs' (Only valid when true is returned)
		// To find the deepest points on 'lhs','rhs' add/subtract half the 'penetration' depth along 'axis'.
		// Note: that applied impulses should be equal and opposite, and applied at the same point in space (hence one contact point).
		inline bool BoxVsBox(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration, v4& point)
		{
			if (!BoxVsBox(lhs, l2w, rhs, r2w, axis, penetration))
				return false;

			point = FindContactPoint(shape_cast<ShapeBox>(lhs), l2w, shape_cast<ShapeBox>(rhs), r2w, axis, penetration);
			return true;
		}
	}
}

#if PR_UNITTESTS&&0
#include "pr/common/unittests.h"
#include "pr/str/prstring.h"
#include "pr/linedrawer/ldr_helper.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_collision_box_vs_box)
		{
			using namespace pr::collision;

			auto lhs = ShapeBox::make(pr::v4::make(0.3f, 0.4f, 0.5f, 0.0f));
			auto rhs = ShapeBox::make(pr::v4::make(0.3f, 0.4f, 0.5f, 0.0f));
			pr::m4x4 l2w_[] =
			{
				pr::m4x4Identity,
			};
			pr::m4x4 r2w_[] =
			{
				pr::Rotation4x4(pr::maths::tau_by_8, 0, 0, pr::v4::make(0.2f, 0.3f, 0.1f, 1.0f)),
				pr::Rotation4x4(0, pr::maths::tau_by_8, 0, pr::v4::make(0.2f, 0.3f, 0.1f, 1.0f)),
				pr::Rotation4x4(0, 0, pr::maths::tau_by_8, pr::v4::make(0.2f, 0.3f, 0.1f, 1.0f)),
				pr::Rotation4x4(0, 0, -3*pr::maths::tau_by_8, pr::v4::make(0.2f, 0.3f, 0.1f, 1.0f)),
			};

			for (int i = 0; i != 20; ++i)
			{
				pr::v4 axis, pt; float pen;
				pr::m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : pr::Random4x4(pr::v4Origin, 0.5f);
				pr::m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : pr::Random4x4(pr::v4Origin, 0.5f);

				pr::string<> s;
				pr::ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
				pr::ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
				pr::ldr::Write(s, "collision_unittests.ldr");
				if (BoxVsBox(lhs, l2w, rhs, r2w, axis, pen, pt))
				{
					pr::ldr::LineD(s, "sep_axis", pr::Colour32Yellow, pt, axis);
					pr::ldr::Box(s, "pt0", pr::Colour32Yellow, pt - 0.5f*pen*axis, 0.01f);
					pr::ldr::Box(s, "pt1", pr::Colour32Yellow, pt + 0.5f*pen*axis, 0.01f);
				}
				pr::ldr::Write(s, "collision_unittests.ldr");
			}
		}
	}
}
#endif