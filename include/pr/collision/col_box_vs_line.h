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
#include "pr/collision/shape_box.h"
#include "pr/collision/shape_line.h"

namespace pr
{
	namespace collision
	{
		// Test for overlap between two shapes, with generic penetration collection
		template <typename Penetration>
		void BoxVsLine(Shape const& lhs_, m4x4 const& l2w_, Shape const& rhs_, m4x4 const& r2w_, Penetration& pen)
		{
			auto& lhs = shape_cast<ShapeBox>(lhs_);
			auto& rhs = shape_cast<ShapeLine>(rhs_);
			auto l2w = l2w_ * lhs_.m_s2p;
			auto r2w = r2w_ * rhs_.m_s2p;

			// Compute a transform for 'rhs' in 'lhs's frame
			auto r2l = InvertFast(l2w) * r2w;

			// Line segment mid-point in box space
			auto mid = r2l.pos;

			// Line segment "radius" plus an epsilon term to counteract arithmetic
			// errors when segment is (near) parallel to a coordinate axis.
			auto half = rhs.m_radius * r2l.z;
			auto rad = Abs(half) + v4(maths::tiny);

			// Try world coordinate axes as separating axes
			if (!pen(lhs.m_radius.x + rad.x - Abs(mid.x), [&]{ return Sign(mid.x) * l2w.x; })) return;
			if (!pen(lhs.m_radius.y + rad.y - Abs(mid.y), [&]{ return Sign(mid.y) * l2w.y; })) return;
			if (!pen(lhs.m_radius.z + rad.z - Abs(mid.z), [&]{ return Sign(mid.z) * l2w.z; })) return;

			// Lambda for returning a separating axis with the correct sign
			auto sep_axis = [&](v4_cref sa) { return Sign(Dot(r2l.pos, sa)) * sa; };
			float ra,rb;

			// Try cross products of the segment direction with the coordinate axes.
			// Example for XAxis x LineSegment:
			//' axis = Cross(Xaxis, line) = v4(0, -line.z, line.y, 0) ('line' in box space)
			//' ra = Dot(axis, box.radius) = unsigned radius of the box in the direction of 'axis'
			//'    =  axis.y * box.radius.y + axis.z * box.radius.z;
			//'    = -line.z * box.radius.y + line.y * box.radius.z;
			//' rb = Dot(axis, mid) = distance to the line in the direction of 'axis' (note: line is perpendicular to axis)
			//'    =  axis.y * mid.y + axis.z * mid.z;
			//'    = -line.z * mid.y + line.y * mid.z;
			//'Flip 'mid' and 'axis' into the positive octant. The length of 'line'
			// doesn't matter so long as the length of the separating axis is scaled
			// by the same amount, so we can use 'rad == abs(line/2)':
			//'  ra = rad.z * box.radius.y + rad.y * box.radius.z;
			//'  rb = rad.z * abs(mid.y)    + rad.y * abs(mid.z);
			//' depth = ra - rb
			ra = rad.z * lhs.m_radius.y + rad.y * lhs.m_radius.z;
			rb = rad.z * Abs(mid.y)     + rad.y * Abs(mid.z);
			if (!pen(ra - rb, [&]{ return rhs.m_radius * sep_axis(Cross(l2w.x, r2w.z)); }))
				return;

			//' axis = Cross(Yaxis, line) = v4(line.z, 0, -line.x, 0) ('line' in box space)
			ra = rad.z * lhs.m_radius.x + rad.x * lhs.m_radius.z;
			rb = rad.z * Abs(mid.x)     + rad.x * Abs(mid.z);
			if (!pen(ra - rb, [&]{ return rhs.m_radius * sep_axis(Cross(l2w.y, r2w.z)); }))
				return;

			//' axis = Cross(Zaxis, line) = v4(-line.y, line.x, 0, 0) ('line' in box space)
			ra = rad.y * lhs.m_radius.x + rad.x * lhs.m_radius.y;
			rb = rad.y * Abs(mid.x)     + rad.x * Abs(mid.y);
			if (!pen(ra - rb, [&]{ return rhs.m_radius * sep_axis(Cross(l2w.z, r2w.z)); }))
				return;
		}

		// Returns true if the sphere  'lhs' intersects the orientated box 'rhs'
		inline bool BoxVsLine(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w)
		{
			TestPenetration p;
			BoxVsLine(lhs, l2w, rhs, r2w, p);
			return p.Contact();
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the shapes
		inline bool BoxVsLine(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration)
		{
			ContactPenetration pen;
			BoxVsLine(lhs, l2w, rhs, r2w, pen);
			if (!pen.Contact())
				return false;

			// Determine the sign of the separating axis to make it the normal from 'lhs' to 'rhs'
			auto sep_axis = pen.SeparatingAxis();
			auto p0 = Dot3(sep_axis, l2w * lhs.m_s2p.pos);
			auto p1 = Dot3(sep_axis, r2w * rhs.m_s2p.pos);
			auto sign = Sign<float>(p0 < p1);

			penetration = pen.Depth();
			axis        = sign * sep_axis;
			return true;
		}

		// Returns true if 'lhs' and 'rhs' are intersecting.
		// 'axis' is the collision normal from 'lhs' to 'rhs'
		// 'penetration' is the depth of penetration between the shapes
		// 'point' is the world space contact point between 'lhs','rhs' (Only valid when true is returned)
		// To find the deepest points on 'lhs','rhs' add/subtract half the 'penetration' depth along 'axis'.
		// Note: that applied impulses should be equal and opposite, and applied at the same point in space (hence one contact point).
		inline bool BoxVsLine(Shape const& lhs, pr::m4x4 const& l2w, Shape const& rhs, pr::m4x4 const& r2w, v4& axis, float& penetration, v4& point)
		{
			if (!BoxVsLine(lhs, l2w, rhs, r2w, axis, penetration))
				return false;

			point = FindContactPoint(shape_cast<ShapeBox>(lhs), l2w, shape_cast<ShapeLine>(rhs), r2w, axis, penetration);
			return true;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/linedrawer/ldr_helper.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_collision_box_vs_line)
		{
			using namespace pr::collision;

			ShapeBox lhs(v4(0.3f, 0.5f, 0.2f, 0.0f));
			ShapeLine rhs(3.0f);
			pr::m4x4 l2w_[] =
			{
				pr::m4x4Identity,
			};
			pr::m4x4 r2w_[] =
			{
				pr::m4x4::Rotation(pr::maths::tau_by_8, pr::maths::tau_by_8, pr::maths::tau_by_8, pr::v4(0.2f, 0.3f, 0.1f, 1.0f)),
			};

			Rand rng;
			for (int i = 0; i != 20; ++i)
			{
				pr::v4 axis, pt; float pen;
				pr::m4x4 l2w = i < _countof(l2w_) ? l2w_[i] : pr::Random4x4(rng, pr::v4Origin, 0.3f);
				pr::m4x4 r2w = i < _countof(r2w_) ? r2w_[i] : pr::Random4x4(rng, pr::v4Origin, 0.3f);

				pr::string<> s;
				pr::ldr::Shape(s, "lhs", 0x30FF0000, lhs, l2w);
				pr::ldr::Shape(s, "rhs", 0x3000FF00, rhs, r2w);
				pr::ldr::Write(s, L"collision_unittests.ldr");
				if (!BoxVsLine(lhs, l2w, rhs, r2w, axis, pen, pt))
					continue;

				pr::ldr::LineD(s, "sep_axis", pr::Colour32Yellow, pt - 0.5f*pen*axis, axis);
				pr::ldr::Box(s, "pt0", pr::Colour32Yellow, pt - 0.5f*pen*axis, 0.002f);
				pr::ldr::Box(s, "pt1", pr::Colour32Yellow, pt + 0.5f*pen*axis, 0.002f);
				pr::ldr::Write(s, L"collision_unittests.ldr");
			}
		}
	}
}
#endif
