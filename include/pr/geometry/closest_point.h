//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"
#include "pr/geometry/distance.h"
#include "pr/geometry/point.h"

namespace pr
{
	// Returns the point closest to 'point' on 'plane'
	inline v4 ClosestPoint_PointToPlane(v4 const& point, Plane const& plane)
	{
		return point - Distance_PointToPlane(point, plane) * plane::GetDirection(plane);
	}
	inline v4 ClosestPoint_PointToPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c)
	{
		return ClosestPoint_PointToPlane(point, plane::make(a, b, c));
	}

	// Returns the parametric value of the closest point on 'line'
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end, float& t)
	{
		assert(point.w == 1.0f && start.w == 1.0f && end.w == 1.0f);
		assert(start != end);
		v4 line = end - start;
		t = Dot3(point - start, line) / Length3Sq(line);
		return start + t * line;
	}
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end) { float t; return ClosestPoint_PointToInfiniteLine(point, start, end, t); }
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, const Line3& line, float& t)    {          return ClosestPoint_PointToInfiniteLine(point, line.m_point, line.m_point + line.m_line, t); }
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, const Line3& line)              { float t; return ClosestPoint_PointToInfiniteLine(point, line.m_point, line.m_point + line.m_line, t); }

	// Returns the parametric value of the closest point on 'line'
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, v4 const& start, v4 const& end, float& t)
	{
		assert(point.w == 1.0f && start.w == 1.0f && end.w == 1.0f);
		v4 line = end - start;

		// Project 'point' onto 'line', but defer divide by 'line.Length3Sq()'
		t = Dot3(point - start, line);
		if (t <= 0.0f) { t = 0.0f; return start; } // 'point' projects outside 'line', clamp to 0.0f
		else
		{
			float denom = Length3Sq(line);
			if (t >= denom) { t = 1.0f; return end; }        // 'point' projects outside 'line', clamp to 1.0f
			else { t = t / denom; return start + t * line; } // 'point' projects inside 'line', do deferred divide now
		}
	}
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, v4 const& start, v4 const& end) { float t; return ClosestPoint_PointToLineSegment(point, start, end, t); }
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, const Line3& line, float& t)    {          return ClosestPoint_PointToLineSegment(point, line.m_point, line.m_point + line.m_line, t); }
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, const Line3& line)              { float t; return ClosestPoint_PointToLineSegment(point, line.m_point, line.m_point + line.m_line, t); }

	// Returns the point on an AABB that is closest to 'point'
	inline v4 ClosestPoint_PointToBoundingBox(v4 const& point, BBox const& bbox)
	{
		v4 result;
		v4 lower = bbox.Lower();
		v4 upper = bbox.Upper();
		result.x = Clamp(point.x, lower.x, upper.x);
		result.y = Clamp(point.y, lower.y, upper.y);
		result.z = Clamp(point.z, lower.z, upper.z);
		result.w = 1.0f;
		return result;
	}

	// Returns the closest point on an ellipse to 'x','y'.
	// 'x','y' are a point in ellipse space
	// 'major' is the size of the major radius of the ellipse (along the x axis)
	// 'minor' is the size of the minor radius of the ellipse (along the y axis)
	// Note: this is only an approximation, the true solution involves finding the
	// largest root of a quartic equation.
	inline v2 ClosestPoint_PointToEllipse(float x, float y, float major, float minor)
	{
		assert(major >= 0.0f && minor >= 0.0f && major >= minor);

		// Special case minor axis lengths of zero
		if (minor < maths::tiny)
			return v2::make(Clamp(x, -major, major), 0.0f);

		float ratio = Sign(y) * minor / (major + maths::tiny); // Add an epsilon to prevent div by zero
		float a = Sqr(major), b = Sqr(minor);
		v2 pt = {x, y};
		v2 nearest;

		// Binary search along X for the nearest
		float bounds[2] = {-major*(x < 0.0f), major*(x >= 0.0f)};
		do
		{
			nearest.x = 0.5f * (bounds[0] + bounds[1]);
			nearest.y = ratio * Sqrt(a - Sqr(nearest.x));
			v2 tang = {nearest.y/b, -nearest.x/a};

			float d = Sign(y) * Dot2(tang, pt - nearest);
			bounds[d < 0.0f] = nearest.x;
		}
		while( !FEql(bounds[0], bounds[1]) );
		return nearest;
	}

	// Returns the closest point on a triangle to 'point'. From "Real time collision detection" by Christer Ericson
	namespace impl
	{
		template <typename T> v4 ClosestPoint_PointToTriangle(v4 const& p, v4 const& a, v4 const& b, v4 const& c, v4& barycentric)
		{
			assert(p.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);

			// Check if P in vertex region outside A
			v4 ab = b - a;
			v4 ac = c - a;
			v4 ap = p - a;
			float d1 = Dot3(ab, ap);
			float d2 = Dot3(ac, ap);
			if (d1 <= 0.0f && d2 <= 0.0f) { barycentric.set(1.0f, 0.0f, 0.0f, 0.0f); return a; } // Barycentric coordinates (1, 0, 0)

			// Check if P in vertex region outside B
			v4 bp = p - b;
			float d3 = Dot3(ab, bp);
			float d4 = Dot3(ac, bp);
			if (d3 >= 0.0f && d4 <= d3) { barycentric.set(0.0f, 1.0f, 0.0f, 0.0f); return b; } // Barycentric coordinates (0, 1, 0)

			// Check if P in edge region of AB, if so return projection of P onto AB
			float vc = d1*d4 - d3*d2;
			if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
			{
				float v = d1 / (d1 - d3);
				barycentric.set(1.0f - v, v, 0.0f, 0.0f);
				return a + v * ab; // Barycentric coordinates (1-v, v, 0)
			}

			// Check if P in vertex region outside C
			v4 cp = p - c;
			float d5 = Dot3(ab, cp);
			float d6 = Dot3(ac, cp);
			if (d6 >= 0.0f && d5 <= d6) { barycentric.set(0.0f, 0.0f, 1.0f, 0.0f); return c; } // Barycentric coordinates (0, 0, 1)

			// Check if P in edge region of AC, if so return projection of P onto AC
			float vb = d5*d2 - d1*d6;
			if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
			{
				float w = d2 / (d2 - d6);
				barycentric.set(1.0f - w, 0.0f, w, 0.0f);
				return a + w * ac; // Barycentric coordinates (1-w, 0, w)
			}

			// Check if P in edge region of BC, if so return projection of P onto BC
			float va = d3*d6 - d5*d4;
			if (va <= 0.0f && d4 - d3 >= 0.0f && d5 - d6 >= 0.0f)
			{
				float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				barycentric.set(0.0f, 1.0f - w, w, 0.0f);
				return b + w * (c - b); // Barycentric coordinates (0, 1-w, w)
			}

			// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
			float denom = 1.0f / (va + vb + vc);
			float v = vb * denom;
			float w = vc * denom;
			barycentric.set(1.0f - v - w, v, w, 0.0f);
			return a + ab * v + ac * w; // = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
		}
	}
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4& barycentric) { return impl::ClosestPoint_PointToTriangle<void>(point, a, b, c, barycentric); }
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c)                  { v4 barycentric; return impl::ClosestPoint_PointToTriangle<void>(point, a, b, c, barycentric); }
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, const v4* tri, v4& barycentric)                         { return impl::ClosestPoint_PointToTriangle<void>(point, tri[0], tri[1], tri[2], barycentric); }
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, const v4* tri)                                          { v4 barycentric; return impl::ClosestPoint_PointToTriangle<void>(point, tri[0], tri[1], tri[2], barycentric); }

	namespace impl
	{
		// Returns the closest point on a tetrahedron to 'point'. From "Real time collision detection" by Christer Ericson
		template <typename T> v4 ClosestPoint_PointToTetrahedron(v4 const& p, v4 const& a, v4 const& b, v4 const& c, v4 const& d, v4& barycentric)
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
				v4 q = ClosestPoint_PointToTriangle<T>(p, a, b, c, bary);
				float dist_sq = Length3Sq(q - p);
				if (dist_sq < best_dist_sq) { best_dist_sq  = dist_sq; closest_point = q; barycentric.set(bary.x, bary.y, bary.z, 0.0f); point_is_inside = false; }
			}
			if (PointInFrontOfPlane(p, a, c, d)) // Test face acd
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, a, c, d, bary);
				float dist_sq = Length3Sq(q - p);
				if (dist_sq < best_dist_sq) { best_dist_sq = dist_sq; closest_point = q; barycentric.set(bary.x, 0.0f, bary.y, bary.z); point_is_inside = false; }
			}
			if (PointInFrontOfPlane(p, a, d, b)) // Test face adb
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, a, d, b, bary);
				float dist_sq = Length3Sq(q - p);
				if (dist_sq < best_dist_sq) { best_dist_sq = dist_sq; closest_point = q; barycentric.set(bary.x, bary.z, 0.0f, bary.y); point_is_inside = false; }
			}
			if (PointInFrontOfPlane(p, d, c, b)) // Test face dcb
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, d, c, b, bary);
				float dist_sq = Length3Sq(q - p);
				if (dist_sq < best_dist_sq) { best_dist_sq = dist_sq; closest_point = q; barycentric.set(0.0f, bary.z, bary.y, bary.x); point_is_inside = false; }
			}
			if (point_is_inside)
			{
				barycentric.set(0.25f, 0.25f, 0.25f, 0.25f); // This is wrong but it shouldn't be needed
			}
			return closest_point;
		}
	}
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d, v4& barycentric) { return impl::ClosestPoint_PointToTetrahedron<void>(point, a, b, c, d, barycentric); }
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d)                  { v4 barycentric; return impl::ClosestPoint_PointToTetrahedron<void>(point, a, b, c, d, barycentric); }
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, const v4* tetra, v4& barycentric)                                    { return impl::ClosestPoint_PointToTetrahedron<void>(point, tetra[0], tetra[1], tetra[2], tetra[3], barycentric); }
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, const v4* tetra)                                                     { v4 barycentric; return impl::ClosestPoint_PointToTetrahedron<void>(point, tetra[0], tetra[1], tetra[2], tetra[3], barycentric); }

	// TODO: These should really do the minimal work in the impl:: versions and the extra work in the inline versions
	// Make distanceSq a parameter to impl::XXX
	namespace impl
	{
		// Finds the closest points between two line segments and also
		// the parametric values on each line.
		// From "Real time collision detection" by Christer Ericson
		template <bool test_degenerates> void ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& t0, float& t1)
		{
			assert(s0.w == 1.0f && e0.w == 1.0f && s1.w == 1.0f && e1.w == 1.0f);

			v4 line0 = e0 - s0;
			v4 line1 = e1 - s1;
			v4 separation = s0 - s1;
			float f = Dot3(line1, separation);
			float c = Dot3(line0, separation);
			float line0_length_sq = Length3Sq(line0);
			float line1_length_sq = Length3Sq(line1);

			#pragma warning(disable: 4127)// conditional expression is constant
			if (test_degenerates)
			#pragma warning(default: 4127)
			{
				// Check if either or both segments are degenerate
				if (FEqlZero(line0_length_sq) && FEqlZero(line1_length_sq)) { t0 = 0.0f; t1 = 0.0f; return; }
				if (FEqlZero(line0_length_sq))                              { t0 = 0.0f; t1 = Clamp<float>(f / line1_length_sq, 0.0f, 1.0f); return; }
				if (FEqlZero(line1_length_sq))                              { t1 = 0.0f; t0 = Clamp<float>(-c / line0_length_sq, 0.0f, 1.0f); return; }
			}

			// The general nondegenerate case starts here
			float b = Dot3(line0, line1);
			float denom = line0_length_sq * line1_length_sq - b * b; // Always non-negative

			// If segments not parallel, calculate closest point on infinite line 'line0'
			// to infinite line 'line1', and clamp to segment 1. Otherwise pick arbitrary t0
			t0 = (denom != 0.0f) ? Clamp<float>((b*f - c*line1_length_sq) / denom, 0.0f, 1.0f) : 0.0f;

			// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
			// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
			t1 = (b*t0 + f) / line1_length_sq;

			// If t1 in [0,1] then done. Otherwise, clamp t1, recompute t0 for the new value
			// of t1 using t0 = Dot3(pt1 - s0, line0) / line0_length_sq = (b*t1 - c) / line0_length_sq
			// and clamped t0 to [0, 1]
			if      (t1 < 0.0f) { t1 = 0.0f; t0 = Clamp<float>((   -c) / line0_length_sq, 0.0f, 1.0f); }
			else if (t1 > 1.0f) { t1 = 1.0f; t0 = Clamp<float>((b - c) / line0_length_sq, 0.0f, 1.0f); }
		}
	}
	inline void ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& t0, float& t1)
	{
		impl::ClosestPoint_LineSegmentToLineSegment<true>(s0, e0, s1, e1, t0, t1);
	}
	inline void ClosestPoint_LineSegmentToLineSegmentFast(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& t0, float& t1)
	{
		impl::ClosestPoint_LineSegmentToLineSegment<false>(s0, e0, s1, e1, t0, t1);
	}
	inline void ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, v4& pt0, v4& pt1)
	{
		float t0, t1;
		impl::ClosestPoint_LineSegmentToLineSegment<true>(s0, e0, s1, e1, t0, t1);
		pt0 = (1.0f - t0) * s0 + t0 * e0;
		pt1 = (1.0f - t1) * s1 + t1 * e1;
	}
	inline void ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, v4& pt0, v4& pt1, float& t0, float& t1)
	{
		impl::ClosestPoint_LineSegmentToLineSegment<true>(s0, e0, s1, e1, t0, t1);
		pt0 = (1.0f - t0) * s0 + t0 * e0;
		pt1 = (1.0f - t1) * s1 + t1 * e1;
	}
	inline void ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& dist_sq)
	{
		float t0, t1;
		impl::ClosestPoint_LineSegmentToLineSegment<true>(s0, e0, s1, e1, t0, t1);
		v4 pt0 = (1.0f - t0) * s0 + t0 * e0;
		v4 pt1 = (1.0f - t1) * s1 + t1 * e1;
		dist_sq = Length3Sq(pt1 - pt0);
	}

	namespace impl
	{
		// Finds the closest point on a line segment to an infinite line.
		// Also the parametric values on each line.
		// From "Real time collision detection" by Christer Ericson
		template <typename T> void ClosestPoint_LineSegmentToInfiniteLine(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& line1, float& t0, float& t1)
		{
			assert(s0.w == 1.0f && e0.w == 1.0f && s1.w == 1.0f && line1.w == 0.0f);
			assert(!IsZero3(line1) && "The infinite line should not be degenerate");

			v4 line0				= e0 - s0;
			float line0_length_sq	= Length3Sq(line0);
			float line1_length_sq	= Length3Sq(line1);
			v4 separation			= s0 - s1;
			float s1_on_line0		= -Dot3(separation, line0);
			float s0_on_line1		=  Dot3(separation, line1);

			// Check if the segment is degenerate
			if (FEqlZero(line0_length_sq))
			{
				t0 = 0.0f;
				t1 = s0_on_line1 / line1_length_sq; // t0 = 0 => t1 = (b*t0 + f) / line1_length_sq = f / line1_length_sq
				return;
			}

			// The general nondegenerate case starts here
			float b = Dot3(line0, line1);
			float denom = line0_length_sq * line1_length_sq - b * b; // Always non-negative

			// If segments not parallel, calculate closest point on infinite line 'line0'
			// to infinite line 'line1', and clamp to the segment. Otherwise pick arbitrary t0
			if (denom != 0.0f) { t0 = Clamp<float>((b*s0_on_line1 + s1_on_line0*line1_length_sq) / denom, 0.0f, 1.0f); }
			else				{ t0 = 0.0f; }

			// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
			// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
			t1 = (b*t0 + s0_on_line1) / line1_length_sq;
		}
	}
	inline void ClosestPoint_LineSegmentToInfiniteLine(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& line1, float& t0, float& t1)
	{
		impl::ClosestPoint_LineSegmentToInfiniteLine<void>(s0, e0, s1, line1, t0, t1);
	}
	inline void ClosestPoint_LineSegmentToInfiniteLine(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& line1, float& t0, float& t1, float& dist_sq)
	{
		impl::ClosestPoint_LineSegmentToInfiniteLine<void>(s0, e0, s1, line1, t0, t1);
		v4 pt0 = (1.0f - t0) * s0 + t0 * e0;
		v4 pt1 = s1 + t1 * line1;
		dist_sq = Length3Sq(pt0 - pt1);
	}

	namespace impl
	{
		// Returns the parametric values of the closest points on two infinite lines
		template <typename T> void ClosestPoint_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1, float& t0, float& t1)
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
	}
	inline void  ClosestPoint_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1, float& t0, float& t1)
	{
		impl::ClosestPoint_InfiniteLineToInfiniteLine<void>(s0, line0, s1, line1, t0, t1);
	}
}