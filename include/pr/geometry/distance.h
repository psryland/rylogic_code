//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"
#include "pr/geometry/closest_point.h"

namespace pr
{
	// Return the distance that 'point' is from the infinite plane: 'plane'
	template <typename = void> inline float pr_vectorcall Distance_PointToPlane(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c)
	{
		assert(point.w == 1.0f);
		v4 plane = Normalise(Cross3(b - a, c - a));
		plane.w = -Dot3(plane, a);
		return Dot4(plane, point);
	}
	template <typename = void> inline float pr_vectorcall Distance_PointToPlane(v4_cref<> point, Plane const& plane)
	{
		assert(point.w == 1.0f);
		return Dot(plane, point);
	}

	// Return the distance that 'point' is from the infinite line: 'line'
	template <typename = void> inline float pr_vectorcall Distance_PointToInfiniteLine(v4_cref<> point, v4_cref<> start, v4_cref<> end)
	{
		v4 line     = end   - start;
		v4 to_point = point - start;
		float p_dot_l = Dot3(to_point, line);
		return Sqrt(LengthSq(to_point) - Sqr(p_dot_l) / LengthSq(line));
	}

	// Return the minimum distance between two infinite lines
	template <typename = void> inline float pr_vectorcall Distance_InfiniteLineToInfiniteLine(v4_cref<> s0, v4_cref<> line0, v4_cref<> s1, v4_cref<> line1)
	{
		v4 a = s1 - s0;
		float a_len_sq = LengthSq(a);
		if (a_len_sq == 0.0f)
			return 0.0f;

		v4 b = Cross3(line0, line1);
		if (FEql3(b, pr::v4Zero))
			return Sqrt(a_len_sq - Sqr(Dot3(a, line0)) / LengthSq(line0));
		else
			return Dot3(a, b) / LengthSq(b);
	}

	// Returns the squared distance from 'point' to 'line'
	template <typename = void> inline float pr_vectorcall DistanceSq_PointToInfiniteLine(v4_cref<> point, v4_cref<> s, v4_cref<> d)
	{
		auto sp   = point - s;
		auto d_sq = Dot3(d,d);
		assert(d_sq != 0.0f && "divide by zero in DistanceSq_PointToInfiniteLine");
		return Dot3(sp,sp) - Sqr(Dot3(sp,d))/d_sq;
	}

	// Returns the squared distance from 'point' to 'line'
	template <typename = void> inline float pr_vectorcall DistanceSq_PointToLineSegment(v4_cref<> point, v4_cref<> s, v4_cref<> e)
	{
		auto a = point - s;
		auto d = e - s;

		float ad = Dot3(a,d);
		if (ad <= 0.0f)
			return LengthSq(a);

		float dd = Dot3(d,d);
		if (ad >= dd)
			return LengthSq(point - e);

		return LengthSq(a) - Sqr(ad) / dd;
	}

	// Returns the squared distance from 'point' to 'bbox'
	template <typename = void> inline float pr_vectorcall DistanceSq_PointToBoundingBox(v4_cref<> point, BBox const& bbox)
	{
		float dist_sq = 0.0f;
		v4 lower = bbox.Lower();
		v4 upper = bbox.Upper();
		if      (point.x < lower.x) dist_sq += Sqr(lower.x - point.x);
		else if (point.x > upper.x) dist_sq += Sqr(point.x - upper.x);
		if      (point.y < lower.y) dist_sq += Sqr(lower.y - point.y);
		else if (point.y > upper.y) dist_sq += Sqr(point.y - upper.y);
		if      (point.z < lower.z) dist_sq += Sqr(lower.z - point.z);
		else if (point.z > upper.z) dist_sq += Sqr(point.z - upper.z);
		return dist_sq;
	}

	// Returns the signed minimum distance between a line segment '(s,e)' and an AABB 'bbox'.
	// 's' and 'e' must be in the same space as 'bbox'. A negative value means the line segment intersects the AABB.
	template <typename = void> inline float pr_vectorcall Distance_LineSegmentToBBox(v4_cref<> s, v4_cref<> e, BBox_cref bbox)
	{
		auto pen = ClosestPoint_LineSegmentToBBox(s, e, bbox);
		return -pen.depth(); // pen.depth() is positive for penetration, in this case we want the opposite.
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::geometry
{
	PRUnitTest(DistanceTests)
	{
		{// DistanceSq_PointToLineSegment
			auto s = pr::v4(1.0f, 1.0f, 0.0f, 1.0f);
			auto e = pr::v4(3.0f, 2.0f, 0.0f, 1.0f);
			auto a = pr::v4(2.0f, 1.0f, 0.0f, 1.0f);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment(s, s, e), 0.0f), true);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment(e, s, e), 0.0f), true);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment((s+e)*0.5f, s, e), 0.0f), true);
			PR_CHECK(FEql(DistanceSq_PointToLineSegment(a, s, e), Sqr(sin(atan(0.5f)))), true);
		}
	}
}
#endif
