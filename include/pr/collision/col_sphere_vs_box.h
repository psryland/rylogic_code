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

namespace pr
{
	namespace collision
	{
		// Test for overlap between an orientated box and a sphere, with generic penetration collection
		template <typename Penetration>
		bool SphereVsBox(Shape const& lhs, m4x4 const& l2w_, Shape const& rhs, m4x4 const& r2w_, Penetration& pen)
		{
			auto& sph = shape_cast<ShapeSphere>(lhs);
			auto& box = shape_cast<ShapeBox   >(rhs);
			auto l2w = l2w_ * lhs.m_s2p;
			auto r2w = r2w_ * rhs.m_s2p;

			// Convert into box space
			auto b2s = pr::InvertFast(r2w) * l2w.pos - pr::v4Origin; // Box to sphere vector in box space
	
			// Get a vector from the sphere to the nearest point on the box
			auto closest = pr::v4Zero;
			auto dist_sq = 0.0f;
			for (int i = 0; i != 3; ++i)
			{
				if (b2s[i] > box.m_radius[i])
				{
					dist_sq += pr::Sqr(b2s[i] - box.m_radius[i]);
					closest[i] = box.m_radius[i];
				}
				else if (b2s[i] < -box.m_radius[i])
				{
					dist_sq += pr::Sqr(b2s[i] + box.m_radius[i]);
					closest[i] = -box.m_radius[i];
				}
				else
				{
					closest[i] = b2s[i];
				}
			}
	
			// If the separation is greater than the radius of the sphere then no collision
			if (dist_sq > pr::Sqr(sph.m_radius))
				return false;

			// If 'dist_sq' is zero then the centre of the sphere is inside the box
			// The separating axis is in one of the box axis directions
			if (dist_sq < pr::maths::tiny)
			{
				auto i = pr::LargestElement3(pr::Abs(b2s));

				// Find the separating axis
				auto norm = pr::v4Zero;
				norm[i] = pr::Sign<float>(b2s[i] > 0.0f);
				norm = r2w * norm;

				// Find the penetration depth
				auto depth = sph.m_radius + box.m_radius[i] - pr::Abs(b2s[i]);

				pen(norm, depth);
			}

			// Otherwise the centre of the sphere is outside of the box
			else
			{
				auto dist = Sqrt(dist_sq);

				// Find the separating axis
				auto norm = r2w * ((b2s - closest) / dist);
				
				// Find the penetration depth
				auto depth = sph.m_radius - dist;

				pen(norm, depth);
			}
			return true;
		}

		// Returns true if the sphere  'lhs' intersects the orientated box 'rhs'
		inline bool SphereVsBox(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w)
		{
			IgnorePenetration p;
			return SphereVsBox(lhs, l2w, rhs, r2w, p);
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the shapes
		inline bool SphereVsBox(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration)
		{
			MinPenetration p;
			if (!SphereVsBox(lhs, l2w, rhs, r2w, p))
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
		inline bool SphereVsBox(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration, v4& point)
		{
			if (!SphereVsBox(lhs, l2w, rhs, r2w, axis, penetration))
				return false;

			point = FindContactPoint(shape_cast<ShapeSphere>(lhs), l2w, shape_cast<ShapeBox>(rhs), r2w, axis, penetration);
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
		PRUnitTest(pr_geometry_collision_sph_vs_box)
		{
			using namespace pr::collision;

			auto lhs = ShapeSphere::make(0.3f);
			auto rhs = ShapeBox   ::make(pr::v4::make(0.3f, 0.4f, 0.5f, 0.0f));
			pr::m4x4 l2w_[] =
			{
				pr::m4x4Identity,
			};
			pr::m4x4 r2w_[] =
			{
				pr::Rotation4x4(pr::maths::tau_by_8, pr::maths::tau_by_8, pr::maths::tau_by_8, pr::v4::make(0.2f, 0.3f, 0.1f, 1.0f)),
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
				if (SphereVsBox(lhs, l2w, rhs, r2w, axis, pen, pt))
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
