//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"

namespace pr
{
	// Return the distance that 'point' is from the infinite plane: 'plane'
	inline float Distance_PointToPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c)
	{
		assert(point.w == 1.0f);
		v4 plane = Normalise3(Cross3(b - a, c - a));
		plane.w = -Dot3(plane, a);
		return Dot4(plane, point);
	}
	inline float Distance_PointToPlane(v4 const& point, Plane const& plane)
	{
		assert(point.w == 1.0f);
		return Dot4(plane, point);
	}

	// Return the distance that 'point' is from the infinite line: 'line'
	inline float Distance_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end)
	{
		v4 line     = end   - start;
		v4 to_point = point - start;
		float p_dot_l = Dot3(to_point, line);
		return Sqrt(Length3Sq(to_point) - Sqr(p_dot_l) / Length3Sq(line));
	}

	// Return the minimum distance between two infinite lines
	inline float Distance_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1)
	{
		v4 a = s1 - s0;
		float a_len_sq = Length3Sq(a);
		if (a_len_sq == 0.0f)
			return 0.0f;

		v4 b = Cross3(line0, line1);
		if (FEql3(b, pr::v4Zero))
			return Sqrt(a_len_sq - Sqr(Dot3(a, line0)) / Length3Sq(line0));
		else
			return Dot3(a, b) / Length3Sq(b);
	}

	// Returns the squared distance from 'point' to 'line'
	inline float DistanceSq_PointToInfiniteLine(v4 const& point, v4 const& s, v4 const& d)
	{
		auto sp   = point - s;
		auto d_sq = Dot3(d,d);
		assert(d_sq != 0.0f && "divide by zero in DistanceSq_PointToInfiniteLine");
		return Dot3(sp,sp) - Sqr(Dot3(sp,d))/d_sq;
	}

	// Returns the squared distance from 'point' to 'line'
	inline float DistanceSq_PointToLineSegment(v4 const& point, v4 const& s, v4 const& e)
	{
		auto a = point - s;
		auto d = e - s;

		float ad = Dot3(a,d);
		if (ad <= 0.0f)
			return Length3Sq(a);

		float dd = Dot3(d,d);
		if (ad >= dd)
			return Length3Sq(point - e);

		return Length3Sq(a) - Sqr(ad) / dd;
	}

	// Returns the squared distance from 'point' to 'bbox'
	inline float DistanceSq_PointToBoundingBox(v4 const& point, BBox const& bbox)
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
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_distance)
		{
			{// DistanceSq_PointToLineSegment
				auto s = pr::v4::make(1.0f, 1.0f, 0.0f, 1.0f);
				auto e = pr::v4::make(3.0f, 2.0f, 0.0f, 1.0f);
				auto a = pr::v4::make(2.0f, 1.0f, 0.0f, 1.0f);
				PR_CHECK(FEql(DistanceSq_PointToLineSegment(s, s, e), 0.0f), true);
				PR_CHECK(FEql(DistanceSq_PointToLineSegment(e, s, e), 0.0f), true);
				PR_CHECK(FEql(DistanceSq_PointToLineSegment((s+e)*0.5f, s, e), 0.0f), true);
				PR_CHECK(FEql(DistanceSq_PointToLineSegment(a, s, e), Sqr(sin(atan(0.5f)))), true);
			}
		}
	}
}
#endif
