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
		if (a_len_sq == 0.0f) return 0.0f;

		v4 b = Cross3(line0, line1);
		if (FEqlZero3(b))	return Sqrt(a_len_sq - Sqr(Dot3(a, line0)) / Length3Sq(line0));
		else				return Dot3(a, b) / Length3Sq(b);
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
	inline float DistanceSq_PointToLineSegment(v4 const& point, const Line3& line)
	{
		v4 point_to_line = point - line.start();

		float p_dot_l = Dot3(point_to_line, line.m_line);
		if (p_dot_l <= 0.0f) return Length3Sq(point_to_line);

		float l_dot_l = Length3Sq(line);
		if (p_dot_l >= l_dot_l) return Length3Sq(point - line.end());

		return Length3Sq(point_to_line) - p_dot_l * p_dot_l / l_dot_l;
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
		{}
	}
}
#endif
