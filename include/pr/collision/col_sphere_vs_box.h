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
		void SphereVsBox(Shape const& lhs, m4x4 const& l2w_, Shape const& rhs, m4x4 const& r2w_, Penetration& pen)
		{
			auto& sph = shape_cast<ShapeSphere>(lhs);
			auto& box = shape_cast<ShapeBox   >(rhs);
			auto l2w = l2w_ * lhs.m_s2p;
			auto r2w = r2w_ * rhs.m_s2p;

			// Convert into box space
			// Box centre to sphere centre vector in box space
			auto l2r = pr::InvertFast(r2w) * l2w.pos - pr::v4Origin;
	
			// Get a vector from the sphere to the nearest point on the box
			auto closest = pr::v4Zero;
			auto dist_sq = 0.0f;
			for (int i = 0; i != 3; ++i)
			{
				if (l2r[i] > box.m_radius[i])
				{
					dist_sq += pr::Sqr(l2r[i] - box.m_radius[i]);
					closest[i] = box.m_radius[i];
				}
				else if (l2r[i] < -box.m_radius[i])
				{
					dist_sq += pr::Sqr(l2r[i] + box.m_radius[i]);
					closest[i] = -box.m_radius[i];
				}
				else
				{
					closest[i] = l2r[i];
				}
			}
	
			// If 'dist_sq' is zero then the centre of the sphere is inside the box
			// The separating axis is in one of the box axis directions
			if (dist_sq < pr::maths::tiny)
			{
				auto i = pr::LargestElement3(pr::Abs(l2r));

				// Find the penetration depth
				auto depth = sph.m_radius + box.m_radius[i] - Abs(l2r[i]);
				pen(depth, [&]
				{
					// Find the separating axis
					auto norm = v4Zero;
					norm[i] = Sign(l2r[i]);
					return r2w * norm;
				});
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
				});
			}
		}

		// Returns true if the sphere  'lhs' intersects the orientated box 'rhs'
		inline bool SphereVsBox(Shape const& lhs, m4x4 const& l2w, Shape const& rhs, m4x4 const& r2w)
		{
			TestPenetration p;
			SphereVsBox(lhs, l2w, rhs, r2w, p);
			return p.Contact();
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the shapes
		inline bool SphereVsBox(Shape const& lhs, m4x4 const& l2w, Shape const& rhs, m4x4 const& r2w, v4& axis, float& penetration)
		{
			ContactPenetration p;
			SphereVsBox(lhs, l2w, rhs, r2w, p);
			if (!p.Contact())
				return false;

			// Determine the sign of the separating axis to make it the normal from 'lhs' to 'rhs'
			auto sep_axis = p.SeparatingAxis();
			auto p0 = Dot3(sep_axis, (l2w * lhs.m_s2p).pos);
			auto p1 = Dot3(sep_axis, (r2w * rhs.m_s2p).pos);
			auto sign = Sign<float>(p0 < p1);

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
		inline bool SphereVsBox(Shape const& lhs, m4x4 const& l2w, Shape const& rhs, m4x4 const& r2w, v4& axis, float& penetration, v4& point)
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
#include "pr/linedrawer/ldr_helper.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_collision_sph_vs_box)
		{
			using namespace pr::collision;

			auto lhs = ShapeSphere::make(0.3f);
			auto rhs = ShapeBox   ::make(pr::v4(0.3f, 0.4f, 0.5f, 0.0f));
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
