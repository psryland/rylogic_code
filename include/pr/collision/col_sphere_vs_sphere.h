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

namespace pr
{
	namespace collision
	{
		// Test for collision between two spheres
		template <typename Penetration>
		void SphereVsSphere(Shape const& lhs_, m4x4 const& l2w_, Shape const& rhs_, m4x4 const& r2w_, Penetration& pen)
		{
			auto& lhs = shape_cast<ShapeSphere>(lhs_);
			auto& rhs = shape_cast<ShapeSphere>(rhs_);
			auto l2w = l2w_ * lhs_.m_s2p;
			auto r2w = r2w_ * rhs_.m_s2p;

			// Distance between centres
			auto r2l = r2w.pos - l2w.pos;
			auto len = pr::Length3(r2l);
			auto sep = lhs.m_radius + rhs.m_radius - len;
			pen(sep, [&]{ return r2l/len; });
		}

		// Returns true if 'lhs' intersects 'rhs'
		inline bool SphereVsSphere(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w)
		{
			TestPenetration p;
			SphereVsSphere(lhs, l2w, rhs, r2w, p);
			return p.Contact();
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the shapes (positive for contact)
		inline bool SphereVsSphere(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration)
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
		inline bool SphereVsSphere(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration, v4& point)
		{
			if (!SphereVsSphere(lhs, l2w, rhs, r2w, axis, penetration))
				return false;

			point = FindContactPoint(shape_cast<ShapeSphere>(lhs), l2w, shape_cast<ShapeSphere>(rhs), r2w, axis, penetration);
			return true;
		}
	}
}

#if PR_UNITTESTS&&0
#include "pr/common/unittests.h"
#include "pr/linedrawer/ldr_helper.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_collision_sph_vs_sph)
		{
			using namespace pr::collision;

			auto lhs = ShapeSphere::make(0.3f);
			auto rhs = ShapeSphere::make(0.4f);
			pr::m4x4 l2w_[] =
			{
				pr::m4x4Identity,
			};
			pr::m4x4 r2w_[] =
			{
				pr::Rotation4x4(pr::maths::tau_by_8, pr::maths::tau_by_8, pr::maths::tau_by_8, pr::v4(0.2f, 0.3f, 0.1f, 1.0f)),
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
				if (SphereVsSphere(lhs, l2w, rhs, r2w, axis, pen, pt))
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
