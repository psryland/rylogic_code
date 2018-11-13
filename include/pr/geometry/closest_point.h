//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include <cassert>
#include "pr/maths/maths.h"
#include "pr/geometry/distance.h"
#include "pr/geometry/point.h"
#include "pr/collision/penetration.h"

namespace pr
{
	// Returns the point closest to 'point' on 'plane'
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToPlane(v4_cref<> point, Plane const& plane)
	{
		return point - Distance_PointToPlane(point, plane) * plane::Direction(plane);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToPlane(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c)
	{
		return ClosestPoint_PointToPlane(point, plane::make(a, b, c));
	}

	// Returns the parametric value of the closest point on 'line'
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToInfiniteLine(v4_cref<> point, v4_cref<> start, v4_cref<> end, float& t)
	{
		assert(point.w == 1.0f && start.w == 1.0f && end.w == 1.0f);
		assert(start != end);
		v4 line = end - start;
		t = Dot3(point - start, line) / Length3Sq(line);
		return start + t * line;
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToInfiniteLine(v4_cref<> point, v4_cref<> start, v4_cref<> end)
	{
		float t;
		return ClosestPoint_PointToInfiniteLine(point, start, end, t);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToInfiniteLine(v4_cref<> point, const Line3& line, float& t)
	{
		return ClosestPoint_PointToInfiniteLine(point, line.m_point, line.m_point + line.m_line, t);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToInfiniteLine(v4_cref<> point, const Line3& line)
	{
		float t;
		return ClosestPoint_PointToInfiniteLine(point, line.m_point, line.m_point + line.m_line, t);
	}

	// Returns the parametric value of the closest point on 'line'
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToLineSegment(v4_cref<> point, v4_cref<> s, v4_cref<> e, float& t)
	{
		assert(point.w == 1.0f && s.w == 1.0f && e.w == 1.0f);
		auto line = e - s;

		// Project 'point' onto 'line', but defer divide by 'line.Length3Sq()'
		t = Dot3(point - s, line);
		if (t <= 0.0f)
		{
			// 'point' projects outside 'line', clamp to 0.0f
			t = 0.0f;
			return s;
		}

		auto denom = Length3Sq(line);
		if (t >= denom)
		{
			// 'point' projects outside 'line', clamp to 1.0f
			t = 1.0f;
			return e;
		}

		// 'point' projects inside 'line', do deferred divide now
		t = t / denom;
		return s + t * line;
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToLineSegment(v4_cref<> point, v4_cref<> start, v4_cref<> end)
	{
		float t;
		return ClosestPoint_PointToLineSegment(point, start, end, t);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToLineSegment(v4_cref<> point, const Line3& line, float& t)
	{
		return ClosestPoint_PointToLineSegment(point, line.m_point, line.m_point + line.m_line, t);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToLineSegment(v4_cref<> point, const Line3& line)
	{
		float t;
		return ClosestPoint_PointToLineSegment(point, line.m_point, line.m_point + line.m_line, t);
	}

	// Returns the point on an AABB that is closest to 'point'
	// if 'surface_only' is true, the point is projected onto the bbox surface,
	// otherwise points within the bounding box are counted as the closest point
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToBoundingBox(v4_cref<> point, BBox_cref bbox, bool surface_only = false)
	{
		v4 result;
		v4 lower = bbox.Lower();
		v4 upper = bbox.Upper();
		if (!surface_only || !IsWithin(bbox, point))
		{
			result.x = Clamp(point.x, lower.x, upper.x);
			result.y = Clamp(point.y, lower.y, upper.y);
			result.z = Clamp(point.z, lower.z, upper.z);
		}
		else
		{
			result = point;
			auto centre = bbox.Centre();
			auto upr = upper - point;
			auto lwr = point - lower;
			auto i0 = SmallestElement3(upr);
			auto i1 = SmallestElement3(lwr);
			if (upr[i0] < lwr[i1]) result[i0] = upper[i0];
			else                   result[i1] = lower[i1];
		}
		result.w = 1.0f;
		return result;
	}

	// Returns the closest point on an ellipse to 'x','y'.
	// 'x','y' are a point in ellipse space
	// 'major' is the size of the major radius of the ellipse (along the x axis)
	// 'minor' is the size of the minor radius of the ellipse (along the y axis)
	// Note: this is only an approximation, the true solution involves finding the
	// largest root of a quartic equation.
	template <typename = void> inline v2 ClosestPoint_PointToEllipse(float x, float y, float major, float minor)
	{
		assert(major >= 0.0f && minor >= 0.0f && major >= minor);

		// Special case minor axis lengths of zero
		if (minor < maths::tiny)
			return v2(Clamp(x, -major, major), 0.0f);

		auto ratio = Sign(y) * minor / (major + maths::tiny); // Add an epsilon to prevent divide by zero
		auto a = Sqr(major), b = Sqr(minor);
		v2 pt(x, y);
		v2 nearest;

		// Binary search along X for the nearest
		float bounds[2] = { -major*(x < 0.0f), major*(x >= 0.0f) };
		do
		{
			nearest.x = 0.5f * (bounds[0] + bounds[1]);
			nearest.y = ratio * Sqrt(a - Sqr(nearest.x));
			v2 tang(nearest.y / b, -nearest.x / a);

			auto d = Sign(y) * Dot2(tang, pt - nearest);
			bounds[d < 0.0f] = nearest.x;
		}
		while (!FEql(bounds[0], bounds[1]));
		return nearest;
	}

	// Returns the closest point on a triangle to 'point'. From "Real time collision detection" by 'Christer Ericson'
	template <typename = void> v4 pr_vectorcall ClosestPoint_PointToTriangle(v4_cref<> p, v4_cref<> a, v4_cref<> b, v4_cref<> c, v4& barycentric)
	{
		assert(p.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);

		// Check if P in vertex region outside A
		auto ab = b - a;
		auto ac = c - a;
		auto ap = p - a;
		auto d1 = Dot3(ab, ap);
		auto d2 = Dot3(ac, ap);
		if (d1 <= 0.0f && d2 <= 0.0f)
		{
			barycentric = v4(1.0f, 0.0f, 0.0f, 0.0f);
			return a;
		}

		// Check if P in vertex region outside B
		auto bp = p - b;
		auto d3 = Dot3(ab, bp);
		auto d4 = Dot3(ac, bp);
		if (d3 >= 0.0f && d4 <= d3)
		{
			barycentric = v4(0.0f, 1.0f, 0.0f, 0.0f);
			return b;
		}

		// Check if P in edge region of AB, if so return projection of P onto AB
		auto vc = d1*d4 - d3*d2;
		if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
		{
			// Barycentric coordinates (1-v, v, 0)
			float v = d1 / (d1 - d3);
			barycentric = v4(1.0f - v, v, 0.0f, 0.0f);
			return a + v * ab;
		}

		// Check if P in vertex region outside C
		v4 cp = p - c;
		float d5 = Dot3(ab, cp);
		float d6 = Dot3(ac, cp);
		if (d6 >= 0.0f && d5 <= d6)
		{
			barycentric = v4(0.0f, 0.0f, 1.0f, 0.0f);
			return c;
		}

		// Check if P in edge region of AC, if so return projection of P onto AC
		float vb = d5*d2 - d1*d6;
		if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
		{
			// Barycentric coordinates (1-w, 0, w)
			float w = d2 / (d2 - d6);
			barycentric = v4(1.0f - w, 0.0f, w, 0.0f);
			return a + w * ac;
		}

		// Check if P in edge region of BC, if so return projection of P onto BC
		float va = d3*d6 - d5*d4;
		if (va <= 0.0f && d4 - d3 >= 0.0f && d5 - d6 >= 0.0f)
		{
			// Barycentric coordinates (0, 1-w, w)
			float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
			barycentric = v4(0.0f, 1.0f - w, w, 0.0f);
			return b + w * (c - b);
		}

		// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
		//'  = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
		float denom = 1.0f / (va + vb + vc);
		float v = vb * denom;
		float w = vc * denom;
		barycentric = v4(1.0f - v - w, v, w, 0.0f);
		return a + ab * v + ac * w;
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToTriangle(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c)
	{
		v4 barycentric;
		return ClosestPoint_PointToTriangle(point, a, b, c, barycentric);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToTriangle(v4_cref<> point, const v4* tri, v4& barycentric)
	{
		return ClosestPoint_PointToTriangle(point, tri[0], tri[1], tri[2], barycentric);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToTriangle(v4_cref<> point, const v4* tri)
	{
		v4 barycentric;
		return ClosestPoint_PointToTriangle(point, tri[0], tri[1], tri[2], barycentric);
	}

	// Returns the closest point on a tetrahedron to 'point'. From "Real time collision detection" by 'Christer Ericson'
	template <typename = void> v4 pr_vectorcall ClosestPoint_PointToTetrahedron(v4_cref<> p, v4_cref<> a, v4_cref<> b, v4_cref<> c, v4_cref<> d, v4& barycentric)
	{
		assert(p.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f && d.w == 1.0f);

		// Start out assuming point inside all halfspaces, so closest to itself
		v4 closest_point = p;
		float best_dist_sq = maths::float_max;
		bool point_is_inside = true;

		// If point outside face abc then compute closest point on abc
		if (PointInFrontOfPlane(p, a, b, c)) // Test face abc
		{
			v4 bary;
			v4 q = ClosestPoint_PointToTriangle(p, a, b, c, bary);
			float dist_sq = Length3Sq(q - p);
			if (dist_sq < best_dist_sq) { best_dist_sq  = dist_sq; closest_point = q; barycentric = v4(bary.x, bary.y, bary.z, 0.0f); point_is_inside = false; }
		}
		if (PointInFrontOfPlane(p, a, c, d)) // Test face acd
		{
			v4 bary;
			v4 q = ClosestPoint_PointToTriangle(p, a, c, d, bary);
			float dist_sq = Length3Sq(q - p);
			if (dist_sq < best_dist_sq) { best_dist_sq = dist_sq; closest_point = q; barycentric = v4(bary.x, 0.0f, bary.y, bary.z); point_is_inside = false; }
		}
		if (PointInFrontOfPlane(p, a, d, b)) // Test face adb
		{
			v4 bary;
			v4 q = ClosestPoint_PointToTriangle(p, a, d, b, bary);
			float dist_sq = Length3Sq(q - p);
			if (dist_sq < best_dist_sq) { best_dist_sq = dist_sq; closest_point = q; barycentric = v4(bary.x, bary.z, 0.0f, bary.y); point_is_inside = false; }
		}
		if (PointInFrontOfPlane(p, d, c, b)) // Test face dcb
		{
			v4 bary;
			v4 q = ClosestPoint_PointToTriangle(p, d, c, b, bary);
			float dist_sq = Length3Sq(q - p);
			if (dist_sq < best_dist_sq) { best_dist_sq = dist_sq; closest_point = q; barycentric = v4(0.0f, bary.z, bary.y, bary.x); point_is_inside = false; }
		}
		if (point_is_inside)
		{
			barycentric = v4(0.25f, 0.25f, 0.25f, 0.25f); // This is wrong but it shouldn't be needed
		}
		return closest_point;
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToTetrahedron(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c, v4_cref<> d)
	{
		v4 barycentric;
		return ClosestPoint_PointToTetrahedron(point, a, b, c, d, barycentric);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToTetrahedron(v4_cref<> point, const v4* tetra, v4& barycentric)
	{
		return ClosestPoint_PointToTetrahedron(point, tetra[0], tetra[1], tetra[2], tetra[3], barycentric);
	}
	template <typename = void> inline v4 pr_vectorcall ClosestPoint_PointToTetrahedron(v4_cref<> point, const v4* tetra)
	{
		v4 barycentric;
		return ClosestPoint_PointToTetrahedron(point, tetra[0], tetra[1], tetra[2], tetra[3], barycentric);
	}

	// Finds the closest points between two line segments and also
	// the parametric values on each line.
	// From "Real time collision detection" by 'Christer Ericson'
	template <typename = void> void pr_vectorcall ClosestPoint_LineSegmentToLineSegment(v4_cref<> s0, v4_cref<> e0, v4_cref<> s1, v4_cref<> e1, float& t0, float& t1)
	{
		assert(s0.w == 1.0f && e0.w == 1.0f && s1.w == 1.0f && e1.w == 1.0f);

		auto line0   = e0 - s0;
		auto line1   = e1 - s1;
		auto sep     = s0 - s1;
		auto len_sq0 = Length3Sq(line0);
		auto len_sq1 = Length3Sq(line1);
		auto f       = Dot3(line1, sep);
		auto c       = Dot3(line0, sep);

		// Check if either or both segments are degenerate
		if (FEql(len_sq0,0) && FEql(len_sq1,0)) { t0 = 0.0f; t1 = 0.0f; return; }
		if (FEql(len_sq0,0))                    { t0 = 0.0f; t1 = Clamp(+f / len_sq1, 0.0f, 1.0f); return; }
		if (FEql(len_sq1,0))                    { t1 = 0.0f; t0 = Clamp(-c / len_sq0, 0.0f, 1.0f); return; }

		// The general non-degenerate case starts here
		auto b = Dot3(line0, line1);
		auto denom = len_sq0 * len_sq1 - b * b; // Always non-negative

		// If segments not parallel, calculate closest point on infinite line 'line0'
		// to infinite line 'line1', and clamp to segment 1. Otherwise pick arbitrary t0
		t0 = denom != 0.0f ? Clamp((b*f - c*len_sq1) / denom, 0.0f, 1.0f) : 0.0f;

		// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
		// using t1 = Dot3(pt0 - s1, line1) / len_sq1 = (b*t0 + f) / len_sq1
		t1 = (b*t0 + f) / len_sq1;

		// If t1 in [0,1] then done. Otherwise, clamp t1, recompute t0 for the new value
		// of t1 using t0 = Dot3(pt1 - s0, line0) / len_sq0 = (b*t1 - c) / len_sq0
		// and clamped t0 to [0, 1]
		if (t1 < 0.0f)
		{
			t1 = 0.0f;
			t0 = Clamp((0 - c) / len_sq0, 0.0f, 1.0f);
		}
		else if (t1 > 1.0f)
		{
			t1 = 1.0f;
			t0 = Clamp((b - c) / len_sq0, 0.0f, 1.0f);
		}
	}
	template <typename = void> inline void pr_vectorcall ClosestPoint_LineSegmentToLineSegment(v4_cref<> s0, v4_cref<> e0, v4_cref<> s1, v4_cref<> e1, v4& pt0, v4& pt1)
	{
		float t0, t1;
		ClosestPoint_LineSegmentToLineSegment(s0, e0, s1, e1, t0, t1);
		pt0 = (1.0f - t0) * s0 + t0 * e0;
		pt1 = (1.0f - t1) * s1 + t1 * e1;
	}
	template <typename = void> inline void pr_vectorcall ClosestPoint_LineSegmentToLineSegment(v4_cref<> s0, v4_cref<> e0, v4_cref<> s1, v4_cref<> e1, v4& pt0, v4& pt1, float& t0, float& t1)
	{
		ClosestPoint_LineSegmentToLineSegment(s0, e0, s1, e1, t0, t1);
		pt0 = (1.0f - t0) * s0 + t0 * e0;
		pt1 = (1.0f - t1) * s1 + t1 * e1;
	}
	template <typename = void> inline void pr_vectorcall ClosestPoint_LineSegmentToLineSegment(v4_cref<> s0, v4_cref<> e0, v4_cref<> s1, v4_cref<> e1, float& dist_sq)
	{
		float t0, t1;
		ClosestPoint_LineSegmentToLineSegment(s0, e0, s1, e1, t0, t1);
		v4 pt0 = (1.0f - t0) * s0 + t0 * e0;
		v4 pt1 = (1.0f - t1) * s1 + t1 * e1;
		dist_sq = Length3Sq(pt1 - pt0);
	}

	// Returns the parametric values of the closest point between a line segment '(s0,e0)'
	// and an infinite line '(s1,line1)'. From "Real time collision detection" by 'Christer Ericson'
	template <typename = void> void pr_vectorcall ClosestPoint_LineSegmentToInfiniteLine(v4_cref<> s0, v4_cref<> e0, v4_cref<> s1, v4_cref<> line1, float& t0, float& t1)
	{
		assert(s0.w == 1.0f && e0.w == 1.0f && s1.w == 1.0f && line1.w == 0.0f);
		assert(!IsZero3(line1) && "The infinite line should not be degenerate");

		auto line0 = e0 - s0;
		auto line0_length_sq = Length3Sq(line0);
		auto line1_length_sq = Length3Sq(line1);
		auto separation = s0 - s1;
		auto s1_on_line0 = -Dot3(separation, line0);
		auto s0_on_line1 = Dot3(separation, line1);

		// Check if the segment is degenerate
		if (FEql(line0_length_sq, 0))
		{
			t0 = 0.0f;
			t1 = s0_on_line1 / line1_length_sq; // t0 = 0 => t1 = (b*t0 + f) / line1_length_sq = f / line1_length_sq
			return;
		}

		// The general non-degenerate case starts here
		float b = Dot3(line0, line1);
		float denom = line0_length_sq * line1_length_sq - b * b; // Always non-negative

		// If segments not parallel, calculate closest point on infinite line 'line0'
		// to infinite line 'line1', and clamp to the segment. Otherwise pick arbitrary t0
		t0 = denom != 0.0f ? Clamp<float>((b*s0_on_line1 + s1_on_line0*line1_length_sq) / denom, 0.0f, 1.0f) : 0.0f;

		// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
		// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
		t1 = (b*t0 + s0_on_line1) / line1_length_sq;
	}
	template <typename = void> inline void pr_vectorcall ClosestPoint_LineSegmentToInfiniteLine(v4_cref<> s0, v4_cref<> e0, v4_cref<> s1, v4_cref<> line1, float& t0, float& t1, float& dist_sq)
	{
		ClosestPoint_LineSegmentToInfiniteLine(s0, e0, s1, line1, t0, t1);
		v4 pt0 = (1.0f - t0) * s0 + t0 * e0;
		v4 pt1 = s1 + t1 * line1;
		dist_sq = Length3Sq(pt0 - pt1);
	}

	// Returns the parametric values of the closest points on two infinite lines
	template <typename = void> void pr_vectorcall ClosestPoint_InfiniteLineToInfiniteLine(v4_cref<> s0, v4_cref<> line0, v4_cref<> s1, v4_cref<> line1, float& t0, float& t1)
	{
		// Degenerate lines should not be passed to this function
		assert(!IsZero3(line0));
		assert(!IsZero3(line1));
		assert(s0.w == 1.0f && line0.w == 0.0f && s1.w == 1.0f && line1.w == 0.0f);

		v4 r    = s0 - s1;
		float a = Dot3(line0, line0);
		float b = Dot3(line0, line1);
		float e = Dot3(line1, line1);
		float d = a*e - b*b;
		if (d == 0.0f) { t0 = 0.0f; t1 = -Dot3(r, line0)/a; return; } // The lines are parallel, return the start of line0 as the nearest point
		float c = Dot3(line0, r);
		float f = Dot3(line1, r);

		t0 = (b*f - c*e) / d;
		t1 = (a*f - b*c) / d;
	}

	// Returns the minimum distance of a line segment '(s,e)' to the AABB 'bbox'
	template <typename = void> MinPenetration pr_vectorcall ClosestPoint_LineSegmentToBBox(v4_cref<> s, v4_cref<> e, BBox_cref bbox)
	{
		// Note: This code is basically the same as col_box_vs_line.h.
		// Make sure to maintain both

		// Line segment mid-point
		auto mid = (s + e) * 0.5f;

		// Line segment "radius" plus an epsilon term to counteract arithmetic
		// errors when the segment is (near) parallel to a coordinate axis.
		auto half = e - mid;
		auto rad = Abs(half) + v4(maths::tiny); 

		// Translate box and segment to origin
		mid = mid - bbox.m_centre;

		MinPenetration pen;

		// Try world coordinate axes
		pen(bbox.m_radius.x + rad.x - Abs(mid.x), [&]{ return Sign(mid.x) * v4XAxis; });
		pen(bbox.m_radius.y + rad.y - Abs(mid.y), [&]{ return Sign(mid.y) * v4YAxis; });
		pen(bbox.m_radius.z + rad.z - Abs(mid.z), [&]{ return Sign(mid.z) * v4ZAxis; });

		// Lambda for returning a separating axis with the correct sign
		auto sep_axis = [&](v4_cref<> sa) { return Sign(Dot(mid, sa)) * sa; };

		// Try cross products of the segment direction with the coordinate axes.
		pen(rad.z * bbox.m_radius.y + rad.y * bbox.m_radius.z - rad.z * Abs(mid.y) - rad.y * Abs(mid.z), [&]{ return sep_axis(Cross(v4XAxis, half)); });
		pen(rad.z * bbox.m_radius.x + rad.x * bbox.m_radius.z - rad.x * Abs(mid.z) - rad.z * Abs(mid.x), [&]{ return sep_axis(Cross(v4YAxis, half)); });
		pen(rad.y * bbox.m_radius.x + rad.x * bbox.m_radius.y - rad.y * Abs(mid.x) - rad.x * Abs(mid.y), [&]{ return sep_axis(Cross(v4ZAxis, half)); });

		return pen;
	}
	template <typename = void> inline MinPenetration pr_vectorcall ClosestPoint_LineSegmentToBBox(v4_cref<> s, v4_cref<> e, BBox_cref bbox, float& t)
	{
		// Returns the parametric value of the closest point on the line segment '(s,e)' to the AABB 'bbox' and the distance.
		auto pen = ClosestPoint_LineSegmentToBBox(s, e, bbox);
		auto axis = pen.SeparatingAxis();

		// Get the feature on the box in the direction of 'axis'
		v4 points[4] = {}; auto n = 1;
		for (int i = 0; i != 3; ++i)
		{
			if (FGtr(axis[i], 0.0f))
			{
				for (int j = 0; j != n; ++j)
					points[j] += m4x4Identity[i] * bbox.m_radius[i];
			}
			else if (FLess(axis[i], 0.0f))
			{
				for (int j = 0; j != n; ++j)
					points[j] -= m4x4Identity[i] * bbox.m_radius[i];
			}
			else
			{
				for (int j = 0; j != n; ++j)
				{
					points[j+n] = points[j] + m4x4Identity[i] * bbox.m_radius[i];
					points[j  ] = points[j] - m4x4Identity[i] * bbox.m_radius[i];
				}
				n *= 2;
			}
		}

		auto line_s = (s - bbox.m_centre).w1();
		auto line_e = (e - bbox.m_centre).w1();

		// Box corner to line segment
		if (n == 1)
		{
			ClosestPoint_PointToLineSegment(points[0].w1(), line_s, line_e, t);
			return pen;
		}

		// Box edge to line segment
		if (n == 2)
		{
			float t2;
			ClosestPoint_LineSegmentToLineSegment(line_s, line_e, points[0].w1(), points[1].w1(), t, t2);
			return pen;
		}

		// Box face to line segment
		auto d = Dot(line_e - line_s, axis);
		if (FGtr (d, 0.0f)) { t = 1.0f; return pen; }
		if (FLess(d, 0.0f)) { t = 0.0f; return pen; }

		// Box face vs. line edge
		//throw std::exception("not implemented");
		//ClosestPoint_LineSegmentToPlane
		//return pen;
	}
	template <typename = void> inline MinPenetration pr_vectorcall ClosestPoint_LineSegmentToBBox(v4_cref<> s, v4_cref<> e, BBox_cref bbox, v4& pt0, v4& pt1)
	{
		float t;
		auto pen = ClosestPoint_LineSegmentToBBox(s, e, bbox, t);
		pt0 = s + t*(e - s);
		if (t == 0.0f || t == 1.0f)
			pt1 = ClosestPoint_PointToBoundingBox(pt0, bbox, true);
		else
			pt1 = pt0 + pen.Depth() * pen.SeparatingAxis();

		return pen;
	}
}

#if PR_UNITTESTS&&0
#include "pr/common/unittests.h"
#include "pr/linedrawer/ldr_helper.h"
#include "pr/maths/rand_vector.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_closest_point)
		{
			{// ClosestPoint_PointToPlane

			}
			{// ClosestPoint_LineSegmentToBBox
				Rand rng;
				for (int i = 0; i != 100; ++i)
				{
					BBox bbox(Random3(rng, v4Origin, 3.0f, 1.0f), Random3(rng, v4(0), v4(3), 0.0f));
					v4 s = Random3(rng, v4Origin, 10.0f, 1.0f);
					v4 e = Random3(rng, v4Origin, 10.0f, 1.0f);

					v4 pt0, pt1;
					auto pen = ClosestPoint_LineSegmentToBBox(s, e, bbox, pt0, pt1);
					//auto dist = -pen.Depth();
					auto axis = pen.SeparatingAxis();

					std::string str;
					ldr::Box(str, "bbox", 0x8000FF00, bbox.m_centre, bbox.m_radius*2);
					ldr::Line(str, "line", 0xFFFF0000, s, e);
					ldr::Box(str, "cp1", 0xFF0000FF, pt0, v4(0.01f));
					ldr::Box(str, "cp2", 0xFF0000FF, pt1, v4(0.01f));
					ldr::Line(str, "axis", 0xFF0000FF, pt0, pt1);

					ldr::Write(str, L"P:\\dump\\test.ldr");
				}
			}
		}
	}
}
#endif