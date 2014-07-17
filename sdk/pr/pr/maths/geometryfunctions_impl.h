//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once

#include "pr/maths/geometryfunctions.h"

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
	inline float DistanceSq_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& line)
	{
		v4 sp = point - start;
		return Length3Sq(sp) - Sqr(Dot3(sp, line)) / Length3Sq(line);
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

	// Return the 2D volume (i.e. area) of the triangle
	inline float Volume_Triangle(v4 const& a, v4 const& b, v4 const& c)
	{
		assert(a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);
		return Length3(Cross3(b-a, c-a)) / 2.0f;
	}

	// Return the volume of a tetrahedron
	inline float Volume_Tetrahedron(v4 const& a, v4 const& b, v4 const& c, v4 const& d)
	{
		assert(a.w == 1.0f && b.w == 1.0f && c.w == 1.0f && d.w == 1.0f);
		return Triple3(b-a, c-a, d-a) / 6.0f;
	}

	// Returns true if 'point' lies in front of the plane described by abc (Cross3(b-a, c-a))
	inline bool PointInFrontOfPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c)
	{
		assert(point.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);
		return Triple3(point - a, b - a, c - a) >= 0.0f;
	}

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
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, v4 const& start, v4 const& end)	{ float t; return ClosestPoint_PointToInfiniteLine(point, start, end, t); }
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, const Line3& line, float& t)	{		   return ClosestPoint_PointToInfiniteLine(point, line.m_point, line.m_point + line.m_line, t); }
	inline v4 ClosestPoint_PointToInfiniteLine(v4 const& point, const Line3& line)				{ float t; return ClosestPoint_PointToInfiniteLine(point, line.m_point, line.m_point + line.m_line, t); }

	// Returns the parametric value of the closest point on 'line'
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, v4 const& start, v4 const& end, float& t)
	{
		assert(point.w == 1.0f && start.w == 1.0f && end.w == 1.0f);
		v4 line = end - start;

		// Project 'point' onto 'line', but defer divide by 'line.Length3Sq()'
		t = Dot3(point - start, line);
		if (t <= 0.0f)			{ t = 0.0f;		 return start; }			// 'point' projects outside 'line', clamp to 0.0f
		else
		{
			float denom = Length3Sq(line);
			if (t >= denom)		{ t = 1.0f;		 return end; }				// 'point' projects outside 'line', clamp to 1.0f
			else				{ t = t / denom; return start + t * line; }	// 'point' projects inside 'line', do deferred divide now
		}
	}
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, v4 const& start, v4 const& end)	{ float t; return ClosestPoint_PointToLineSegment(point, start, end, t); }
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, const Line3& line, float& t)		{          return ClosestPoint_PointToLineSegment(point, line.m_point, line.m_point + line.m_line, t); }
	inline v4 ClosestPoint_PointToLineSegment(v4 const& point, const Line3& line)				{ float t; return ClosestPoint_PointToLineSegment(point, line.m_point, line.m_point + line.m_line, t); }

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
		if( minor < maths::tiny )
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
		template <typename T>
		v4 ClosestPoint_PointToTriangle(v4 const& p, v4 const& a, v4 const& b, v4 const& c, v4& barycentric)
		{
			assert(p.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);

			// Check if P in vertex region outside A
			v4 ab = b - a;
			v4 ac = c - a;
			v4 ap = p - a;
			float d1 = Dot3(ab, ap);
			float d2 = Dot3(ac, ap);
			if( d1 <= 0.0f && d2 <= 0.0f ) { barycentric.set(1.0f, 0.0f, 0.0f, 0.0f); return a; }	// Barycentric coordinates (1, 0, 0)

			// Check if P in vertex region outside B
			v4 bp = p - b;
			float d3 = Dot3(ab, bp);
			float d4 = Dot3(ac, bp);
			if( d3 >= 0.0f && d4 <= d3 ) { barycentric.set(0.0f, 1.0f, 0.0f, 0.0f); return b; }		// Barycentric coordinates (0, 1, 0)

			// Check if P in edge region of AB, if so return projection of P onto AB
			float vc = d1*d4 - d3*d2;
			if( vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f )
			{
				float v = d1 / (d1 - d3);
				barycentric.set(1.0f - v, v, 0.0f, 0.0f);
				return a + v * ab;						// Barycentric coordinates (1-v, v, 0)
			}

			// Check if P in vertex region outside C
			v4 cp = p - c;
			float d5 = Dot3(ab, cp);
			float d6 = Dot3(ac, cp);
			if( d6 >= 0.0f && d5 <= d6 ) { barycentric.set(0.0f, 0.0f, 1.0f, 0.0f); return c; }		// Barycentric coordinates (0, 0, 1)

			// Check if P in edge region of AC, if so return projection of P onto AC
			float vb = d5*d2 - d1*d6;
			if( vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f )
			{
				float w = d2 / (d2 - d6);
				barycentric.set(1.0f - w, 0.0f, w, 0.0f);
				return a + w * ac;						// Barycentric coordinates (1-w, 0, w)
			}

			// Check if P in edge region of BC, if so return projection of P onto BC
			float va = d3*d6 - d5*d4;
			if( va <= 0.0f && d4 - d3 >= 0.0f && d5 - d6 >= 0.0f )
			{
				float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				barycentric.set(0.0f, 1.0f - w, w, 0.0f);
				return b + w * (c - b);					// Barycentric coordinates (0, 1-w, w)
			}

			// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
			float denom = 1.0f / (va + vb + vc);
			float v = vb * denom;
			float w = vc * denom;
			barycentric.set(1.0f - v - w, v, w, 0.0f);
			return a + ab * v + ac * w;					// = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
		}
	}//namespace impl
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4& barycentric)	{ return impl::ClosestPoint_PointToTriangle<void>(point, a, b, c, barycentric); }
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c)					{ v4 barycentric; return impl::ClosestPoint_PointToTriangle<void>(point, a, b, c, barycentric); }
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, const v4* tri, v4& barycentric)							{ return impl::ClosestPoint_PointToTriangle<void>(point, tri[0], tri[1], tri[2], barycentric); }
	inline v4 ClosestPoint_PointToTriangle(v4 const& point, const v4* tri)											{ v4 barycentric; return impl::ClosestPoint_PointToTriangle<void>(point, tri[0], tri[1], tri[2], barycentric); }

	namespace impl
	{
		// Returns the closest point on a tetrahedron to 'point'. From "Real time collision detection" by Christer Ericson
		template <typename T>
        v4 ClosestPoint_PointToTetrahedron(v4 const& p, v4 const& a, v4 const& b, v4 const& c, v4 const& d, v4& barycentric)
		{
			assert(p.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f && d.w == 1.0f);

			// Start out assuming point inside all halfspaces, so closest to itself
			v4 closest_point = p;
			float best_dist_sq = maths::float_max;
			bool point_is_inside = true;

			// If point outside face abc then compute closest point on abc
            if( PointInFrontOfPlane(p, a, b, c) )											// Test face abc
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, a, b, c, bary);
				float dist_sq = Length3Sq(q - p);
				if( dist_sq < best_dist_sq ) { best_dist_sq  = dist_sq; closest_point = q; barycentric.set(bary.x, bary.y, bary.z, 0.0f); point_is_inside = false; }
			}
			if( PointInFrontOfPlane(p, a, c, d) )											// Test face acd
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, a, c, d, bary);
				float dist_sq = Length3Sq(q - p);
				if( dist_sq < best_dist_sq ) { best_dist_sq = dist_sq; closest_point = q; barycentric.set(bary.x, 0.0f, bary.y, bary.z); point_is_inside = false; }
			}
			if( PointInFrontOfPlane(p, a, d, b) )											// Test face adb
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, a, d, b, bary);
				float dist_sq = Length3Sq(q - p);
				if( dist_sq < best_dist_sq ) { best_dist_sq = dist_sq; closest_point = q; barycentric.set(bary.x, bary.z, 0.0f, bary.y); point_is_inside = false; }
			}
			if( PointInFrontOfPlane(p, d, c, b) )											// Test face dcb
			{
				v4 bary;
				v4 q = ClosestPoint_PointToTriangle<T>(p, d, c, b, bary);
				float dist_sq = Length3Sq(q - p);
				if( dist_sq < best_dist_sq ) { best_dist_sq = dist_sq; closest_point = q; barycentric.set(0.0f, bary.z, bary.y, bary.x); point_is_inside = false; }
			}
			if( point_is_inside )
			{
				barycentric.set(0.25f, 0.25f, 0.25f, 0.25f);	// This is wrong but it shouldn't be needed
			}
			return closest_point;
		}
	}//namespace impl
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d, v4& barycentric)	{ return impl::ClosestPoint_PointToTetrahedron<void>(point, a, b, c, d, barycentric); }
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d)					{ v4 barycentric; return impl::ClosestPoint_PointToTetrahedron<void>(point, a, b, c, d, barycentric); }
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, const v4* tetra, v4& barycentric)									{ return impl::ClosestPoint_PointToTetrahedron<void>(point, tetra[0], tetra[1], tetra[2], tetra[3], barycentric); }
	inline v4 ClosestPoint_PointToTetrahedron(v4 const& point, const v4* tetra)														{ v4 barycentric; return impl::ClosestPoint_PointToTetrahedron<void>(point, tetra[0], tetra[1], tetra[2], tetra[3], barycentric); }

	// TODO: These should really do the minimal work in the impl:: versions and the extra work in the inline versions
	// Make distanceSq a parameter to impl::XXX
	namespace impl
	{
		// Finds the closest points between two line segments and also
		// the parametric values on each line.
		// From "Real time collision detection" by Christer Ericson
		template <bool test_degenerates>
		void ClosestPoint_LineSegmentToLineSegment(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& e1, float& t0, float& t1)
		{
			assert(s0.w == 1.0f && e0.w == 1.0f && s1.w == 1.0f && e1.w == 1.0f);

			v4 line0				= e0 - s0;
			v4 line1				= e1 - s1;
			v4 separation			= s0 - s1;
			float f					= Dot3(line1, separation);
			float c					= Dot3(line0, separation);
			float line0_length_sq	= Length3Sq(line0);
			float line1_length_sq	= Length3Sq(line1);

			#pragma warning(disable: 4127)// conditional expression is constant
			if( test_degenerates )
			#pragma warning(default: 4127)
			{
				// Check if either or both segments are degenerate
				if( FEqlZero(line0_length_sq) && FEqlZero(line1_length_sq) )
				{	t0 = 0.0f; t1 = 0.0f; return; }
				if( FEqlZero(line0_length_sq) )
				{	t0 = 0.0f; t1 = Clamp<float>(f / line1_length_sq, 0.0f, 1.0f); return; }
				if( FEqlZero(line1_length_sq) )
				{	t1 = 0.0f; t0 = Clamp<float>(-c / line0_length_sq, 0.0f, 1.0f); return; }
			}

			// The general nondegenerate case starts here
			float b = Dot3(line0, line1);
			float denom = line0_length_sq * line1_length_sq - b * b;	// Always non-negative

			// If segments not parallel, calculate closest point on infinite line 'line0'
			// to infinite line 'line1', and clamp to segment 1. Otherwise pick arbitrary t0
			if( denom != 0.0f )	{ t0 = Clamp<float>((b*f - c*line1_length_sq) / denom, 0.0f, 1.0f); }
			else				{ t0 = 0.0f; }

			// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
			// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
			t1 = (b*t0 + f) / line1_length_sq;

			// If t1 in [0,1] then done. Otherwise, clamp t1, recompute t0 for the new value
			// of t1 using t0 = Dot3(pt1 - s0, line0) / line0_length_sq = (b*t1 - c) / line0_length_sq
			// and clamped t0 to [0, 1]
			if     ( t1 < 0.0f )	{ t1 = 0.0f; t0 = Clamp<float>(( -c) / line0_length_sq, 0.0f, 1.0f); }
			else if( t1 > 1.0f )	{ t1 = 1.0f; t0 = Clamp<float>((b-c) / line0_length_sq, 0.0f, 1.0f); }
		}
	}//namespace impl
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
		template <typename T>
		void ClosestPoint_LineSegmentToInfiniteLine(v4 const& s0, v4 const& e0, v4 const& s1, v4 const& line1, float& t0, float& t1)
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
			if( FEqlZero(line0_length_sq) )
			{
				t0 = 0.0f;
				t1 = s0_on_line1 / line1_length_sq;	// t0 = 0 => t1 = (b*t0 + f) / line1_length_sq = f / line1_length_sq
				return;
			}

			// The general nondegenerate case starts here
			float b = Dot3(line0, line1);
			float denom = line0_length_sq * line1_length_sq - b * b;	// Always non-negative

			// If segments not parallel, calculate closest point on infinite line 'line0'
			// to infinite line 'line1', and clamp to the segment. Otherwise pick arbitrary t0
			if( denom != 0.0f )	{ t0 = Clamp<float>((b*s0_on_line1 + s1_on_line0*line1_length_sq) / denom, 0.0f, 1.0f); }
			else				{ t0 = 0.0f; }

			// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
			// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
			t1 = (b*t0 + s0_on_line1) / line1_length_sq;
		}
	}//namespace impl
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
		template <typename T>
		void ClosestPoint_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1, float& t0, float& t1)
		{
			// Degenerate lines should not be passed to this function
			assert(!IsZero3(line0) && !IsZero3(line1));
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
	}//namespace impl
	inline void  ClosestPoint_InfiniteLineToInfiniteLine(v4 const& s0, v4 const& line0, v4 const& s1, v4 const& line1, float& t0, float& t1)
	{
		impl::ClosestPoint_InfiniteLineToInfiniteLine<void>(s0, line0, s1, line1, t0, t1);
	}

	// Return a point that is the weighted result of verts 'a','b','c' and 'bary'
	inline v4 BaryPoint(v4 const& a, v4 const& b, v4 const& c, v4 const& bary)
	{
		return bary.x * a + bary.y * b + bary.z * c;
	}

	// Return the bary centric coordinates for 'point' with respect to triangle a,b,c
	inline v4 BaryCentric(v4 const& point, v4 const& a, v4 const& b, v4 const& c)
	{
		assert(point.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);
		v4 ab = b - a, ac = c - a, pa = point - a;
		float d00 = Dot3(ab, ab);
		float d01 = Dot3(ab, ac);
		float d11 = Dot3(ac, ac);
		float d20 = Dot3(pa, ab);
		float d21 = Dot3(pa, ac);
		float denom = d00 * d11 - d01 * d01;
		assert(denom != 0.0f && "This triangle has no area");
		v4 bary;
		bary.y = (d11 * d20 - d01 * d21) / denom;
		bary.z = (d00 * d21 - d01 * d20) / denom;
		bary.x = 1.0f - bary.y - bary.z;
		bary.w = 0.0f;
		return bary;
	}

	// Returns true if a point projects within a triangle using the triangle normal
	inline bool PointWithinTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, float tol)
	{
		v4 bary = BaryCentric(point, a, b, c);
		return	bary.x >= -tol && bary.x <= 1.0f + tol &&
				bary.y >= -tol && bary.y <= 1.0f + tol &&
				bary.z >= -tol && bary.z <= 1.0f + tol;
	}

	// Returns true if a point projects within a triangle using the triangle normal
	inline bool PointWithinTriangle2(v4 const& point, v4 const& a, v4 const& b, v4 const& c, float tol)
	{
		v4 c0 = Cross3(point - a, b - a);
		v4 c1 = Cross3(point - b, c - b);
		v4 c2 = Cross3(point - c, a - c);
		return Dot3(c0, c1) >= -tol && Dot3(c0, c2) >= -tol;
	}

	// Returns true if a point projects within a triangle using the triangle normal. Also returns the point
	inline bool PointWithinTriangle(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4& pt)
	{
		v4 bary = BaryCentric(point, a, b, c);
		pt = a * bary.x + b * bary.y + c * bary.z; pt.w = 1.0f;
		return	bary.x >= 0.0f && bary.x <= 1.0f &&
				bary.y >= 0.0f && bary.y <= 1.0f &&
				bary.z >= 0.0f && bary.z <= 1.0f;
	}

	// Returns true if 'point' lies on or within the tetrahedron described by abcd (i.e. behind all of it's planes)
	inline bool PointWithinTetrahedron(v4 const& point, v4 const& a, v4 const& b, v4 const& c, v4 const& d)
	{
		return	!PointInFrontOfPlane(point, a, b, c) &&
				!PointInFrontOfPlane(point, a, c, d) &&
				!PointInFrontOfPlane(point, a, d, b) &&
				!PointInFrontOfPlane(point, d, c, b);
	}

	// Given a 2D line that passes through 'a' and 'b' and another that passes through 'c' and 'd'
	// returns true if the lines intersect, false if they don't. Returns the point of intersect
	inline bool Intersect2D_InfiniteLineToInfiniteLine(v2 const& b, v2 const& a, v2 const& d, v2 const& c, v2& intersect)
	{
		v2 ab = b - a;
		v2 cd = d - c;
		float denom = ab.x * cd.y - ab.y * cd.x;
		if( FEql(denom, 0.0f) ) return false;
		float e = b.x * a.y - b.y * a.x;
		float f = d.x * c.y - d.y * c.x;
		intersect.x = (cd.x * e - ab.x * f) / denom;
		intersect.y = (cd.y * e - ab.y * f) / denom;
		return true;
	}

	// Given a line that passes through 's' and 'e' and triangle 'abc'
	// Return true if the line intersects the triangle and if so, also
	// return the barycentric coordinates 'u,v,w' and parametric value 't'
	// of the intersection point
	inline bool Intersect_LineToTriangle(v4 const& s, v4 const& e, v4 const& a, v4 const& b, v4 const& c, float* t, v4* bary, float* f2b, float tmin, float tmax)
	{
		v4 ab = b - a;
		v4 ac = c - a;
		v4 es = s - e;

		// Compute triangle normal.
		v4 n = Cross3(ab, ac);

		// Compute denominator d. If d == 0, the line is parallel to the triangle, so exit early
		float d = Dot3(es, n);
		if (d == 0.0f) return false;
		float sign = Sign(d);
		d = sign * d;

		// Compute intersection 't' value of 'se' with plane of triangle.
		// A ray intersects iff 0 <= t.
		// Segment intersects iff 0 <= t <= 1.
		// Delay dividing by d until intersection has been found to pierce triangle
		v4 as = s - a;
		float T = sign * Dot3(as, n);
		if (T < d*tmin) return false;
		if (T > d*tmax) return false;

		// Compute barycentric coordinate components and test if within bounds
		v4 f = Cross3(es, as);
		v4 Bary;
		Bary.y =  sign * Dot3(ac, f); if (Bary.y < 0.0f || Bary.y          > d) return false;
		Bary.z = -sign * Dot3(ab, f); if (Bary.z < 0.0f || Bary.y + Bary.z > d) return false;

		// Line/segment/ray intersects triangle.
		// Perform delayed division and compute the last barycentric coordinate component
		float ood = 1.0f / d;
		if (t)    { T *= ood; *t = T; }
		if (bary) { Bary.y *= ood; Bary.z *= ood; Bary.x = 1.0f - Bary.y - Bary.z; *bary = Bary.w0(); }
		if (f2b)  { *f2b = sign; }
		return true;
	}

	// Given a line passing through 's' and 'e' and a ccw triangle 'a', 'b', 'c',
	// Returns true if the line pierces triangle.
	// Returns the barycentric coordinates (u,v,w) of the intersection point.
	// If the line pierces from front to back then 'front_to_back' will be 1.0f
	// If the line pierces from back to front then 'front_to_back' will be -1.0f
	// Note about floating point accuracy: always ensure that the line direction and
	// the triangle edges provided to this function have the same direction each time.
	// This ensures the returned results are consistent
	inline bool Intersect_LineToTriangle(v4 const& s, v4 const& e, v4 const& a, v4 const& b, v4 const& c, float& front_to_back, v4& bary)
	{
		v4 line = e - s;
		v4 sa   = a - s;
		v4 sb   = b - s;
		v4 sc   = c - s;

		// Test if 'line' is on or inside the edges ab, bc, and ca. Done by testing
		// that the signed tetrahedral volumes are all positive
		bary.x = Triple3(line, sc, sb);
		bary.y = Triple3(line, sa, sc);
		bary.z = Triple3(line, sb, sa);

		// Compute the barycentric coordinates (u, v, w) determining the
		// intersection point r, r = u*a + v*b + w*c. Note: If the line lies
		// in the plane of the triangle then 'sum' will be zero
		float sum = bary.x + bary.y + bary.z;
		if( FEqlZero(sum) )
			return false;

		float denom = 1.0f / sum;
		bary.x *= denom;
		bary.y *= denom;
		bary.z *= denom; // w = 1.0f - u - v;
		front_to_back = (denom > 0.0f) * 2.0f - 1.0f;
		return bary.x > -maths::tiny && bary.y > -maths::tiny && bary.z > -maths::tiny;
	}

	// Test if a line segment specified by points 'lineS' and 'lineE' intersects AABB b
	inline bool Intersect_LineSegmentToBoundingBox(v4 const& lineS, v4 const& lineE, BBox const& bbox)
	{
		v4 lineM = (lineS + lineE) * 0.5f;	// Line segment midpoint
		v4 lineH  = lineE - lineM;			// Line segment halflength vector
		lineM = lineM - bbox.m_centre;		// Translate box and segment to origin

		// Try world coordinate axes as separating axes
		float adx = Abs(lineH.x);	if( Abs(lineM.x) > bbox.m_radius.x + adx ) return false;
		float ady = Abs(lineH.y);	if( Abs(lineM.y) > bbox.m_radius.y + ady ) return false;
		float adz = Abs(lineH.z);	if( Abs(lineM.z) > bbox.m_radius.z + adz ) return false;

		// Add in an epsilon term to counteract arithmetic errors when segment is
		// (near) parallel to a coordinate axis
		adx += maths::tiny;
		ady += maths::tiny;
		adz += maths::tiny;

		// Try cross products of segment direction vector with coordinate axes
		if( Abs(lineM.y * lineH.z - lineM.z * lineH.y) > bbox.m_radius.y * adz + bbox.m_radius.z * ady) return false;
		if( Abs(lineM.z * lineH.x - lineM.x * lineH.z) > bbox.m_radius.x * adz + bbox.m_radius.z * adx) return false;
		if( Abs(lineM.x * lineH.y - lineM.y * lineH.x) > bbox.m_radius.x * ady + bbox.m_radius.y * adx) return false;

		// No separating axis found; segment must be overlapping AABB
		return true;
	}

	// Returns true if the infinite line that passes through 's' and 'e' passes
	// through the infinite plane 'plane' (i.e. returns false if the line and plane are
	// parallel but not coinsident). Also returns the parametric value of the intercept 't'.
	// 'plane' can be a normalised or unnormalised plane.
	inline bool Intersect_LineToPlane(Plane const& plane, v4 const& s, v4 const& e, float* t, float tmin, float tmax)
	{
		// Find the distances to the plane for the start and end of the line
		float d0 = Distance_PointToPlane(s, plane);
		float d1 = Distance_PointToPlane(e, plane);
		float T = 0.0f;
		if (Abs(d0) > maths::tiny)
		{
			float d = d1 - d0;
			if (Abs(d) < maths::tiny) { return false; } // Line and plane are parallel
			T = -d0 / d; // Use similar triangles to find 't'
		}
		if (t) {*t = T;}
		return T >= tmin && T < tmax;
	}

	// Clip the line segment starting at 'lineS' and ending at 'lineE' with initial
	// parametric values 't0' and 't1' to the infinite plane described by 'plane'.
	// The portion of the line on the  positive side of the plane remains, described
	// by updated 't0' and 't1' values. 'plane' can be a normalised or unnormalised plane.
	// Returns true if the line is not wholely clipped away.
	inline bool Clip_LineSegmentToPlane(Plane const& plane, v4 const& lineS, v4 const& lineE, float& t0, float& t1)
	{
		// Find the distances to the plane for the start and end of the line
		float d0 = Distance_PointToPlane(lineS, plane);
		float d1 = Distance_PointToPlane(lineE, plane);
		if( d0 <= 0.0f && d1 <= 0.0f ) { return false; }
		if( d0 >  0.0f && d1 >  0.0f ) { return true;  }

		// Calculate the parametric value at the intercept
		float t = d0 / (d0 - d1);
		if( d0 < 0.0f && t > t0 ) 	{ t0 = t; } // Move the start point of the line onto the plane
		if( d0 > 0.0f && t < t1 ) 	{ t1 = t; } // Move the end point onto the plane
		return t0 < t1;
	}

	// Clip 'line' to the infinite plane 'plane'. Returns true if the line is not wholely clipped away
	inline bool Clip_LineSegmentToPlane(Plane const& plane, v4& lineS, v4& lineE)
	{
		float d0 = Distance_PointToPlane(lineS, plane);
		float d1 = Distance_PointToPlane(lineE, plane);
		if( d0 <= 0.0f && d1 <= 0.0f ) { lineE = lineS; return false; }
		if( d0 >  0.0f && d1 >  0.0f ) {                return true;  }

		float p = d0 / (d0 - d1);
		v4 intersept = (lineE - lineS) * p;
		if( d0 < 0.0f )	{ lineS = lineS + intersept; } // Move the start point onto the plane
		if( d0 > 0.0f ) { lineE = lineS + intersept; } // Move the end point onto the plane
		return true;
	}

	// Clip the line segment against a bounding box.
	// Remember to initialise t0, t1.
	// e.g. float t0 = -maths::float_max, t1 = maths::float_max;
	// Returns true if some part of the line is within the bounding box
	inline bool Clip_LineSegmentToBoundingBox(v4 const& point, v4 const& line, BBox const& bbox, float& t0, float& t1)
	{
		v4 lower = bbox.Lower();
		v4 upper = bbox.Upper();

		// For all three slabs
		for( int i = 0; i != 3; ++i )
		{
			if( Abs(line[i]) < maths::tiny )
			{
				// Ray is parallel to slab. No hit if origin not within slab
				if( point[i] < lower[i] || point[i] > upper[i] )
					return false;
			}
			else
			{
				// Compute intersection t value of ray with near and far plane of slab
				float u0 = (lower[i] - point[i]) / line[i];
				float u1 = (upper[i] - point[i]) / line[i];

				// Make u0 be intersection with near plane, u1 with far plane
				if( u0 > u1 ) Swap(u0, u1);

				// Record the tightest bounds on the line segment
				if( u0 > t0 ) t0 = u0;
				if( u1 < t1 ) t1 = u1;

				// Exit with no collision as soon as slab intersection becomes empty
				if( t0 > t1 )
					return false;
			}
		}

		// Ray intersects all 3 slabs
		return true;
	}

	// Clip 'line' to the infinite plane 'plane'. Returns true if the line is not wholely clipped away
	inline bool Clip(Plane const& plane, Line3& line)
	{
		float d1 = Distance_PointToPlane(line.start() ,plane);
		float d2 = Distance_PointToPlane(line.end()   ,plane);
		if( d1 < 0.0f && d2 < 0.0f ) { line.m_line = v4Zero; return false; }
		if( d1 > 0.0f && d2 > 0.0f ) {                       return true;  }

		float p = d1 / (d1 - d2);
		if( d1 < 0.0f )	// Move the start point of the line onto the plane
		{
			v4 shorten   = p * line.m_line;
			line.m_point = line.m_point + shorten;
			line.m_line  = line.m_line  - shorten;
		}
		if( d1 > 0.0f ) // Move the end point onto the plane
		{
			line.m_line *= p;
		}
		return true;
	}

	// Clip 'line' to the bounding box 'bbox'. Returns true if the line is not wholy clipped away
	// Note: 'line' and 'bbox' must be in the same space
	inline bool Clip(BBox const& bbox, Line3& line)
	{
		float t0 = 0.0f, t1 = 1.0f;
		if( !Clip_LineSegmentToBoundingBox(line.m_point, line.m_line, bbox, t0, t1) )
			return false;

		line.m_point += t0 * line.m_line;
		line.m_line  *= (t1 - t0);
		return true;
	}

	// Clip a line segment to between two parallel planes.
	// 'dist1' is the near plane distance, 'dist2' is the far plane distance
	// Returns true if any part of the line segment is within the slab
	inline bool ClipToSlab(v4 const& norm, float dist1, float dist2, v4& s, v4& e)
	{
		assert(dist1 <= dist2);
		Plane plane; plane::set(plane, norm, dist1);

		float slab_width = dist2 - dist1;
		float d1 = Distance_PointToPlane(s ,plane);
		float d2 = Distance_PointToPlane(e ,plane);
		if( d1 < 0.0f       && d2 < 0.0f       ) { e = s; return false; }
		if( d1 > slab_width && d2 > slab_width ) { e = s; return false; }

		v4 start = s;
		v4 line  = e - s;
		float dsum = d1 - d2;
		if     ( d1 < 0.0f )		{ float p = d1 / dsum;                s = start + line * p; } // Intercept with the near plane
		else if( d1 > slab_width )	{ float p = (d1 - slab_width) / dsum; s = start + line * p; } // Intercept with the far plane
		if     ( d2 < 0.0f )		{ float p = d1 / dsum;                e = start + line * p; } // Intercept with the near plane
		else if( d2 > slab_width )	{ float p = (d1 - slab_width) / dsum; e = start + line * p; } // Intercept with the far plane
		return true;
	}

	// Return the circum radius of three points
	// 'centre' is only defined if the returned radius is less than float max
	inline float CircumRadius(v4 const& a, v4 const& b, v4 const& c, v4& centre)
	{
		v4 ab = b - a;
		v4 ac = c - a;
		float abab = Length3Sq(ab);
		float acac = Length3Sq(ac);
		float abac = Dot3(ab, ac);
		float e = abab * acac;
		float d = 2.0f * (e - abac * abac);
		if( Abs(d) <= maths::tiny ) return maths::float_max;

		float s = (e - acac * abac) / d;
		float t = (e - abab * abac) / d;

		centre = a + s*ab + t*ac;
		return Length3(centre - a);
	}

	// Returns the angles at each triangle vertex for the triangle v0,v1,v2
	inline v4 TriangleAngles(v4 const& v0, v4 const& v1, v4 const& v2)
	{
		// Angle at a vertex:
		// Cos(C) = a.b / |a|b|
		// Use: Cos(2C) = 2Cos²C - 1
		// Cos(2C) = 2Cos²(C) - 1 = 2*(a.b² / a²b²) - 1
		// C = 0.5 * ACos(2*(a.b² / a²b²) - 1)

		// Choose edges so that 'a' is opposite v0, and angle 'A' is the angle at v0
		auto a = v2 - v1;
		auto b = v0 - v2;
		auto c = v1 - v0;
		auto asq = Length3Sq(a);
		auto bsq = Length3Sq(b);
		auto csq = Length3Sq(c);

		// Use acos for the two smallest angles and 'A+B+C = pi' for the largest
		v4 angles;
		if (csq > asq && csq > bsq)
		{
			auto bc = Dot3(b,c); auto d1 = bsq * csq;
			auto ca = Dot3(c,a); auto d2 = csq * asq;

			angles.x = 0.5f * ACos(Clamp(2*(bc*bc / (d1 + (d1 == 0.0f))) - 1, -1.0f, 1.0f));
			angles.y = 0.5f * ACos(Clamp(2*(ca*ca / (d2 + (d2 == 0.0f))) - 1, -1.0f, 1.0f));
			angles.z = maths::tau_by_2 - angles.x - angles.y;
		}
		else if (asq > bsq && asq > csq)
		{
			auto ab = Dot3(a,b); auto d0 = asq * bsq;
			auto ca = Dot3(c,a); auto d2 = csq * asq;

			angles.y = 0.5f * ACos(Clamp(2*(ca*ca / (d2 + (d2 == 0.0f))) - 1, -1.0f, 1.0f));
			angles.z = 0.5f * ACos(Clamp(2*(ab*ab / (d0 + (d0 == 0.0f))) - 1, -1.0f, 1.0f));
			angles.x = maths::tau_by_2 - angles.y - angles.z;
		}
		else
		{
			auto ab = Dot3(a,b); auto d0 = asq * bsq;
			auto bc = Dot3(b,c); auto d1 = bsq * csq;
			
			angles.x = 0.5f * ACos(Clamp(2*(bc*bc / (d1 + (d1 == 0.0f))) - 1, -1.0f, 1.0f));
			angles.z = 0.5f * ACos(Clamp(2*(ab*ab / (d0 + (d0 == 0.0f))) - 1, -1.0f, 1.0f));
			angles.y = maths::tau_by_2 - angles.x - angles.z;
		}
		angles.w = 0.0f;
		return angles;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_geometryfunctions)
		{
			{//TriangleAngles
				v4 v0 = v4::make(+1.0f, +2.0f, 0.0f, 1.0f);
				v4 v1 = v4::make(-2.0f, -1.0f, 0.0f, 1.0f);
				v4 v2 = v4::make(+0.0f, -1.0f, 0.0f, 1.0f);
				v4 angles = TriangleAngles(v0, v1, v2);
				angles.x = pr::RadiansToDegrees(angles.x);
				angles.y = pr::RadiansToDegrees(angles.y);
				angles.z = pr::RadiansToDegrees(angles.z);
				
				PR_CHECK(FEql(angles.x, 26.56505f, 0.0001f), true);
				PR_CHECK(FEql(angles.y, 45.0f    , 0.0001f), true);
				PR_CHECK(FEql(angles.z, 108.4349f, 0.0001f), true);
			}
		}
	}
}
#endif

//=== Section 5.1.4: =============================================================
//
//// Given point p, return point q on (or in) OBB b, closest to p
//void ClosestPtPointOBB(Point p, OBB b, Point &q)
//{
//    Vector d = p – b.c;
//    // Start result at center of box; make steps from there
//    q = b.c;
//    // For each OBB axis...
//    for (int i = 0; i < 3; i++) {
//        // ...project d onto that axis to get the distance
//        // along the axis of d from the box center
//        float dist = Dot(d, b.u[i]);
//        // If distance farther than the box extents, clamp to the box
//        if (dist > b.e[i]) dist = b.e[i];
//        if (dist < –b.e[i]) dist = –b.e[i];
//        // Step that distance along the axis to get world coordinate
//        q += dist * b.u[i];
//    }
//}
//
//=== Section 5.1.4.1: ===========================================================
//
//// Computes the square distance between point p and OBB b
//float SqDistPointOBB(Point p, OBB b)
//{
//    Point closest;
//    ClosestPtPointOBB(p, b, closest);
//    float sqDist = Dot(closest – p, closest – p);
//    return sqDist;
//}
//
//--------------------------------------------------------------------------------
//
//// Computes the square distance between point p and OBB b
//float SqDistPointOBB(Point p, OBB b)
//{
//    Vector v = p – b.c;
//    float sqDist = 0.0f;
//    for (int i = 0; i < 3; i++) {
//        // Project vector from box center to p on each axis, getting the distance
//        // of p along that axis, and count any excess distance outside box extents
//        float d = Dot(v, b.u[i]), excess = 0.0f;
//        if (d < –b.e[i])
//            excess = d + b.e[i];
//        else if (d > b.e[i])
//            excess = d – b.e[i];
//        sqDist += excess * excess;
//    }
//    return sqDist;
//}
//
//=== Section 5.1.4.2: ===========================================================
//
//struct Rect {
//    Point c;     // center point of rectangle
//    Vector u[2]; // unit vectors determining local x and y axes for the rectangle
//    float e[2];  // the halfwidth extents of the rectangle along the axes
//};
//
//--------------------------------------------------------------------------------
//
//// Given point p, return point q on (or in) Rect r, closest to p
//void ClosestPtPointRect(Point p, Rect r, Point &q)
//{
//    Vector d = p – r.c;
//    // Start result at center of rect; make steps from there
//    q = r.c;
//    // For each rect axis...
//    for (int i = 0; i < 2; i++) {
//        // ...project d onto that axis to get the distance
//        // along the axis of d from the rect center
//        float dist = Dot(d, r.u[i]);
//        // If distance farther than the rect extents, clamp to the rect
//        if (dist > r.e[i]) dist = r.e[i];
//        if (dist < –r.e[i]) dist = –r.e[i];
//        // Step that distance along the axis to get world coordinate
//        q += dist * r.u[i];
//    }
//}
//
//--------------------------------------------------------------------------------
//
//// Return point q on (or in) rect (specified by a, b, and c), closest to given point p
//void ClosestPtPointRect(Point p, Point a, Point b, Point c, Point &q)
//{
//    Vector ab = b – a; // vector across rect
//    Vector ac = c – a; // vector down rect
//    Vector d = p – a;
//    // Start result at top-left corner of rect; make steps from there
//    q = a;
//    // Clamp p’ (projection of p to plane of r) to rectangle in the across direction
//    float dist = Dot(d, ab);
//    float maxdist = Dot(ab, ab);
//    if (dist >= maxdist)
//        q += ab;
//    else if (dist > 0.0f)
//        q += (dist / maxdist) * ab;
//    // Clamp p’ (projection of p to plane of r) to rectangle in the down direction
//    dist = Dot(d, ac);
//    maxdist = Dot(ac, ac);
//    if (dist >= maxdist)
//        q += ac;
//    else if (dist > 0.0f)
//        q += (dist / maxdist) * ac;
//}
//

//
//
//--------------------------------------------------------------------------------
//
//// Test if point p and d lie on opposite sides of plane through abc
//int PointOutsideOfPlane(Point p, Point a, Point b, Point c, Point d)
//{
//    float signp = Dot(p – a, Cross(b – a, c – a)); // [AP AB AC]
//    float signd = Dot(d – a, Cross(b – a, c – a)); // [AD AB AC]
//    // Points on opposite sides if expression signs are opposite
//    return signp * signd < 0.0f;
//}
//
//=== Section 5.1.9: =============================================================
//
//// Clamp n to lie within the range [min, max]
//float Clamp(float n, float min, float max) {
//    if (n < min) return min;
//    if (n > max) return max;
//    return n;
//}
//
//--------------------------------------------------------------------------------
//
//...
//float tnom = b*s + f;
//if (tnom < 0.0f) {
//    t = 0.0f;
//    s = Clamp(-c / a, 0.0f, 1.0f);
//} else if (tnom > e) {
//    t = 1.0f;
//    s = Clamp((b - c) / a, 0.0f, 1.0f);
//} else {
//    t = tnom / e;
//}
//
//=== Section 5.1.9.1: ===========================================================
//
//// Returns 2 times the signed triangle area. The result is positive if
//// abc is ccw, negative if abc is cw, zero if abc is degenerate.
//float Signed2DTriArea(Point a, Point b, Point c)
//{
//    return (a.x - c.x) * (b.y - c.y) - (a.y - c.y) * (b.x - c.x);
//}
//
//--------------------------------------------------------------------------------
//
//// Test if segments ab and cd overlap. If they do, compute and return
//// intersection t value along ab and intersection position p
//int Test2DSegmentSegment(Point a, Point b, Point c, Point d, float &t, Point &p)
//{
//    // Sign of areas correspond to which side of ab points c and d are
//    float a1 = Signed2DTriArea(a, b, d); // Compute winding of abd (+ or -)
//    float a2 = Signed2DTriArea(a, b, c); // To intersect, must have sign opposite of a1
//
//    // If c and d are on different sides of ab, areas have different signs
//    if (a1 * a2 < 0.0f) {
//        // Compute signs for a and b with respect to segment cd
//        float a3 = Signed2DTriArea(c, d, a); // Compute winding of cda (+ or -)
//        // Since area is constant a1-a2 = a3-a4, or a4=a3+a2-a1
////      float a4 = Signed2DTriArea(c, d, b); // Must have opposite sign of a3
//        float a4 = a3 + a2 - a1;
//        // Points a and b on different sides of cd if areas have different signs
//        if (a3 * a4 < 0.0f) {
//            // Segments intersect. Find intersection point along L(t)=a+t*(b-a).
//            // Given height h1 of a over cd and height h2 of b over cd,
//            // t = h1 / (h1 - h2) = (b*h1/2) / (b*h1/2 - b*h2/2) = a3 / (a3 - a4),
//            // where b (the base of the triangles cda and cdb, i.e., the length
//            // of cd) cancels out.
//            t = a3 / (a3 - a4);
//            p = a + t * (b - a);
//            return 1;
//        }
//    }
//
//    // Segments not intersecting (or collinear)
//    return 0;
//}
//
//--------------------------------------------------------------------------------
//
//if (a1 != 0.0f && a2 != 0.0f && a1*a2 < 0.0f) ... // for floating-point variables
//if ((a1 | a2) != 0 && a1 ^ a2 < 0) ... // for integer variables
//
//=== Section 5.2.1.1: ===========================================================
//
//// Compute a tentative separating axis for ab and cd
//Vector m = Cross(ab, cd);
//if (!IsZeroVector(m)) {
//    // Edges ab and cd not parallel, continue with m as a potential separating axis
//    ...
// } else {
//    // Edges ab and cd must be (near) parallel, and therefore lie in some plane P.
//    // Thus, as a separating axis try an axis perpendicular to ab and lying in P
//    Vector n = Cross(ab, c - a);
//    m = Cross(ab, n);
//    if (!IsZeroVector(m)) {
//        // Continue with m as a potential separating axis
//        ...
//    }
//    // ab and ac are parallel too, so edges must be on a line. Ignore testing
//    // the axis for this combination of edges as it won’t be a separating axis.
//    // (Alternatively, test if edges overlap on this line, in which case the
//    // objects are overlapping.)
//    ...
//}
//
//=== Section 5.2.2: =============================================================
//
//// Determine whether plane p intersects sphere s
//int TestSpherePlane(Sphere s, Plane p)
//{
//    // For a normalized plane (|p.n| = 1), evaluating the plane equation
//    // for a point gives the signed distance of the point to the plane
//    float dist = Dot(s.c, p.n) - p.d;
//    // If sphere center within +/-radius from plane, plane intersects sphere
//    return Abs(dist) <= s.r;
//}
//
//--------------------------------------------------------------------------------
//
//// Determine whether sphere s fully behind (inside negative halfspace of) plane p
//int InsideSpherePlane(Sphere s, Plane p)
//{
//    float dist = Dot(s.c, p.n) - p.d;
//    return dist < -s.r;
//}
//
//--------------------------------------------------------------------------------
//
//// Determine whether sphere s intersects negative halfspace of plane p
//int TestSphereHalfspace(Sphere s, Plane p)
//{
//    float dist = Dot(s.c, p.n) - p.d;
//    return dist <= s.r;
//}
//
//=== Section 5.2.3: =============================================================
//
//// Test if OBB b intersects plane p
//int TestOBBPlane(OBB b, Plane p)
//{
//    // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
//    float r = b.e[0]*Abs(Dot(p.n, b.u[0])) +
//              b.e[1]*Abs(Dot(p.n, b.u[1])) +
//              b.e[2]*Abs(Dot(p.n, b.u[2]));
//    // Compute distance of box center from plane
//    float s = Dot(p.n, b.c) – p.d;
//    // Intersection occurs when distance s falls within [-r,+r] interval
//    return Abs(s) <= r;
//}
//
//--------------------------------------------------------------------------------
//
//// Test if AABB b intersects plane p
//int TestAABBPlane(AABB b, Plane p)
//{
//    // These two lines not necessary with a (center, extents) AABB representation
//    Point c = (b.max + b.min) * 0.5f; // Compute AABB center
//    Point e = b.max - c; // Compute positive extents
//
//    // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
//    float r = e[0]*Abs(p.n[0]) + e[1]*Abs(p.n[1]) + e[2]*Abs(p.n[2]);
//    // Compute distance of box center from plane
//    float s = Dot(p.n, c) - p.d;
//    // Intersection occurs when distance s falls within [-r,+r] interval
//    return Abs(s) <= r;
//}
//
//=== Section 5.2.5: =============================================================
//
//// Returns true if sphere s intersects AABB b, false otherwise
//int TestSphereAABB(Sphere s, AABB b)
//{
//    // Compute squared distance between sphere center and AABB
//    float sqDist = SqDistPointAABB(s.c, b);
//
//    // Sphere and AABB intersect if the (squared) distance
//    // between them is less than the (squared) sphere radius
//    return sqDist <= s.r * s.r;
//}
//
//--------------------------------------------------------------------------------
//
//// Returns true if sphere s intersects AABB b, false otherwise.
//// The point p on the AABB closest to the sphere center is also returned
//int TestSphereAABB(Sphere s, AABB b, Point &p)
//{
//    // Find point p on AABB closest to sphere center
//    ClosestPtPointAABB(s.c, b, p);
//
//    // Sphere and AABB intersect if the (squared) distance from sphere
//    // center to point p is less than the (squared) sphere radius
//    Vector v = p - s.c;
//    return Dot(v, v) <= s.r * s.r;
//}
//
//=== Section 5.2.6: =============================================================
//
//// Returns true if sphere s intersects OBB b, false otherwise.
//// The point p on the OBB closest to the sphere center is also returned
//int TestSphereOBB(Sphere s, OBB b, Point &p)
//{
//    // Find point p on OBB closest to sphere center
//    ClosestPtPointOBB(s.c, b, p);
//
//    // Sphere and OBB intersect if the (squared) distance from sphere
//    // center to point p is less than the (squared) sphere radius
//    Vector v = p - s.c;
//    return Dot(v, v) <= s.r * s.r;
//}
//
//=== Section 5.2.7: =============================================================
//
//// Returns true if sphere s intersects triangle ABC, false otherwise.
//// The point p on abc closest to the sphere center is also returned
//int TestSphereTriangle(Sphere s, Point a, Point b, Point c, Point &p)
//{
//    // Find point P on triangle ABC closest to sphere center
//    p = ClosestPtPointTriangle(s.c, a, b, c);
//
//    // Sphere and triangle intersect if the (squared) distance from sphere
//    // center to point p is less than the (squared) sphere radius
//    Vector v = p - s.c;
//    return Dot(v, v) <= s.r * s.r;
//}
//
//=== Section 5.2.8: =============================================================
//
//// Test whether sphere s intersects polygon p
//int TestSpherePolygon(Sphere s, Polygon p)
//{
//    // Compute normal for the plane of the polygon
//    Vector n = Normalize(Cross(p.v[1] – p.v[0], p.v[2] – p.v[0]));
//    // Compute the plane equation for p
//    Plane m; m.n = n; m.d = -Dot(n, p.v[0]);
//    // No intersection if sphere not intersecting plane of polygon
//    if (!TestSpherePlane(s, m)) return 0;
//    // Test to see if any one of the polygon edges pierces the sphere
//    for (int k = p.numVerts, i = 0, j = k - 1; i < k; j = i, i++) {
//        float t;
//        Point q;
//        // Test if edge (p.v[j], p.v[i]) intersects s
//        if (IntersectRaySphere(p.v[j], p.v[i] – p.v[j], s, t, q) && t <= 1.0f)
//            return 1;
//    }
//    // Test if the orthogonal projection q of the sphere center onto m is inside p
//    Point q = ClosestPtPointPlane(s.c, m);
//    return PointInPolygon(q, p);
//}
//
//=== Section 5.2.9: =============================================================
//
//int TestTriangleAABB(Point v0, Point v1, Point v2, AABB b)
//{
//    float p0, p1, p2, r;
//
//    // Compute box center and extents (if not already given in that format)
//    Vector c = (b.min + b.max) * 0.5f;
//    float e0 = (b.max.x – b.min.x) * 0.5f;
//    float e1 = (b.max.y – b.min.y) * 0.5f;
//    float e2 = (b.max.z – b.min.z) * 0.5f;
//
//    // Translate triangle as conceptually moving AABB to origin
//    v0 = v0 – c;
//    v1 = v1 – c;
//    v2 = v2 – c;
//
//    // Compute edge vectors for triangle
//    Vector f0 = v1 – v0,  f1 = v2 – v1, f2 = v0 – v2;
//
//    // Test axes a00..a22 (category 3)
//    // Test axis a00
//    p0 = v0.z*v1.y – v0.y*v1.z;
//    p2 = v2.z*(v1.y – v0.y) – v2.z*(v1.z – v0.z);
//    r = e1 * Abs(f0.z) + e2 * Abs(f0.y);
//    if (Max(-Max(p0, p2), Min(p0, p2)) > r) return 0; // Axis is a separating axis
//
//    // Repeat similar tests for remaining axes a01..a22
//    ...
//
//    // Test the three axes corresponding to the face normals of AABB b (category 1).
//    // Exit if...
//    // ... [-e0, e0] and [min(v0.x,v1.x,v2.x), max(v0.x,v1.x,v2.x)] do not overlap
//    if (Max(v0.x, v1.x, v2.x) < -e0 || Min(v0.x, v1.x, v2.x) > e0) return 0;
//    // ... [-e1, e1] and [min(v0.y,v1.y,v2.y), max(v0.y,v1.y,v2.y)] do not overlap
//    if (Max(v0.y, v1.y, v2.y) < -e1 || Min(v0.y, v1.y, v2.y) > e1) return 0;
//    // ... [-e2, e2] and [min(v0.z,v1.z,v2.z), max(v0.z,v1.z,v2.z)] do not overlap
//    if (Max(v0.z, v1.z, v2.z) < -e2 || Min(v0.z, v1.z, v2.z) > e2) return 0;
//
//    // Test separating axis corresponding to triangle face normal (category 2)
//    Plane p;
//    p.n = Cross(f0, f1);
//    p.d = Dot(p.n, v0);
//    return TestAABBPlane(b, p);
//}
//
//=== Section 5.3.1: =============================================================
//
//int IntersectSegmentPlane(Point a, Point b, Plane p, float &t, Point &q)
//{
//    // Compute the t value for the directed line ab intersecting the plane
//    Vector ab = b - a;
//    t = (p.d - Dot(p.n, a)) / Dot(p.n, ab);
//
//    // If t in [0..1] compute and return intersection point
//    if (t >= 0.0f && t <= 1.0f) {
//        q = a + t * ab;
//        return 1;
//    }
//    // Else no intersection
//    return 0;
//}
//
//--------------------------------------------------------------------------------
//
//// Intersect segment ab against plane of triangle def. If intersecting,
//// return t value and position q of intersection
//int IntersectSegmentPlane(Point a, Point b, Point d, Point e, Point f,
//                          float &t, Point &q)
//{
//    Plane p;
//    p.n = Cross(e – d, f – d);
//    p.d = Dot(p.n, d);
//    return IntersectSegmentPlane(a, b, p, t, q);
//}
//
//=== Section 5.3.2: =============================================================
//
//// Intersects ray r = p + td, |d| = 1, with sphere s and, if intersecting,
//// returns t value of intersection and intersection point q
//int IntersectRaySphere(Point p, Vector d, Sphere s, float &t, Point &q)
//{
//    Vector m = p – s.c;
//    float b = Dot(m, d);
//    float c = Dot(m, m) – s.r * s.r;
//    // Exit if r’s origin outside s (c > 0)and r pointing away from s (b > 0)
//    if (c > 0.0f && b > 0.0f) return 0;
//    float discr = b*b – c;
//    // A negative discriminant corresponds to ray missing sphere
//    if (discr < 0.0f) return 0;
//    // Ray now found to intersect sphere, compute smallest t value of intersection
//    t = -b – Sqrt(discr);
//    // If t is negative, ray started inside sphere so clamp t to zero
//    if (t < 0.0f) t = 0.0f;
//    q = p + t * d;
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//// Test if ray r = p + td intersects sphere s
//int TestRaySphere(Point p, Vector d, Sphere s)
//{
//    Vector m = p – s.c;
//    float c = Dot(m, m) – s.r * s.r;
//    // If there is definitely at least one real root, there must be an intersection
//    if (c <= 0.0f) return 1;
//    float b = Dot(m, d);
//    // Early exit if ray origin outside sphere and ray pointing away from sphere
//    if (b > 0.0f) return 0;
//    float disc = b*b – c;
//    // A negative discriminant corresponds to ray missing sphere
//    if (disc < 0.0f) return 0;
//    // Now ray must hit sphere
//    return 1;
//}
//
//=== Section 5.3.3: =============================================================
//
//// Intersect ray R(t) = p + t*d against AABB a. When intersecting,
//// return intersection distance tmin and point q of intersection
//int IntersectRayAABB(Point p, Vector d, AABB a, float &tmin, Point &q)
//{
//    tmin = 0.0f;          // set to -FLT_MAX to get first hit on line
//    float tmax = FLT_MAX; // set to max distance ray can travel (for segment)
//
//    // For all three slabs
//    for (int i = 0; i < 3; i++) {
//        if (Abs(d[i]) < EPSILON) {
//            // Ray is parallel to slab. No hit if origin not within slab
//            if (p[i] < a.min[i] || p[i] > a.max[i]) return 0;
//        } else {
//            // Compute intersection t value of ray with near and far plane of slab
//            float ood = 1.0f / d[i];
//            float t1 = (a.min[i] - p[i]) * ood;
//            float t2 = (a.max[i] - p[i]) * ood;
//            // Make t1 be intersection with near plane, t2 with far plane
//            if (t1 > t2) Swap(t1, t2);
//            // Compute the intersection of slab intersections intervals
//            if (t1 > tmin) tmin = t1;
//            if (t2 > tmax) tmax = t2;
//            // Exit with no collision as soon as slab intersection becomes empty
//            if (tmin > tmax) return 0;
//        }
//    }
//    // Ray intersects all 3 slabs. Return point (q) and intersection t value (tmin)
//    q = p + d * tmin;
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//--------------------------------------------------------------------------------
//
//    Vector e = b.max – b.min;
//    Vector d = p1 - p0;
//    Point m = p0 + p1 - b.min - b.max;
//
//=== Section 5.3.4: =============================================================
//
//--------------------------------------------------------------------------------
//
//Vector m = Cross(pq, pc);
//u = Dot(pb, m); // ScalarTriple(pq, pc, pb);
//if (u < 0.0f) return 0;
//v = -Dot(pa, m); // ScalarTriple(pq, pa, pc);
//if (v < 0.0f) return 0;
//w = ScalarTriple(pq, pb, pa);
//if (w < 0.0f) return 0;
//
//--------------------------------------------------------------------------------
//
//Vector m = Cross(pq, pc);
//u = Dot(pb, m); // ScalarTriple(pq, pc, pb);
//v = -Dot(pa, m); // ScalarTriple(pq, pa, pc);
//if (!SameSign(u, v)) return 0;
//w = ScalarTriple(pq, pb, pa);
//if (!SameSign(u, w)) return 0;
//
//--------------------------------------------------------------------------------
//
//Vector m = Cross(pq, p);
//u = Dot(pq, Cross(c, b)) + Dot(m, c – b);
//v = Dot(pq, Cross(a, c)) + Dot(m, a – c);
//w = Dot(pq, Cross(b, a)) + Dot(m, b – a);
//
//--------------------------------------------------------------------------------
//
//Vector m = Cross(pq, p);
//float s = Dot(m, c – b);
//float t = Dot(m, a – c);
//u = Dot(pq, Cross(c, b)) + s;
//v = Dot(pq, Cross(a, c)) + t;
//w = Dot(pq, Cross(b, a)) – s - t;
//
//=== Section 5.3.5: =============================================================
//
//// Given line pq and ccw quadrilateral abcd, return whether the line
//// pierces the triangle. If so, also return the point r of intersection
//int IntersectLineQuad(Point p, Point q, Point a, Point b, Point c, Point d, Point &r)
//{
//    Vector pq = q - p;
//    Vector pa = a - p;
//    Vector pb = b - p;
//    Vector pc = c - p;
//    // Determine which triangle to test against by testing against diagonal first
//    Vector m = Cross(pc, pq);
//    float v = Dot(pa, m); // ScalarTriple(pq, pa, pc);
//    if (v >= 0.0f) {
//        // Test intersection against triangle abc
//        float u = -Dot(pb, m); // ScalarTriple(pq, pc, pb);
//        if (u < 0.0f) return 0;
//        float w = ScalarTriple(pq, pb, pa);
//        if (w < 0.0f) return 0;
//        // Compute r, r = u*a + v*b + w*c, from barycentric coordinates (u, v, w)
//        float denom = 1.0f / (u + v + w);
//        u *= denom;
//        v *= denom;
//        w *= denom; // w = 1.0f - u - v;
//        r = u*a + v*b + w*c;
//    } else {
//        // Test intersection against triangle dac
//        Vector pd = d - p;
//        float u = Dot(pd, m); // ScalarTriple(pq, pd, pc);
//        if (u < 0.0f) return 0;
//        float w = ScalarTriple(pq, pa, pd);
//        if (w < 0.0f) return 0;
//        v = -v;
//        // Compute r, r = u*a + v*d + w*c, from barycentric coordinates (u, v, w)
//        float denom = 1.0f / (u + v + w);
//        u *= denom;
//        v *= denom;
//        w *= denom; // w = 1.0f - u - v;
//        r = u*a + v*d + w*c;
//    }
//    return 1;
//}
//
//=== Section 5.3.6: =============================================================
//
//--------------------------------------------------------------------------------
//
//struct Triangle {
//    Plane p;           // Plane equation for triangle plane
//    Plane edgePlaneBC; // When evaluated gives barycentric weight u (for vertex A)
//    Plane edgePlaneCA; // When evaluated gives barycentric weight v (for vertex B)
//};
//
//// Given segment pq and precomputed triangle tri, returns whether segment intersects
//// triangle. If so, also returns the barycentric coordinates (u,v,w) of the
//// intersection point s, and the parameterized intersection t value
//int IntersectSegmentTriangle(Point p, Point q, Triangle tri,
//                             float &u, float &v, float &w, float &t, Point &s)
//{
//    // Compute distance of p to triangle plane. Exit if p lies behind plane
//    float distp = Dot(p, tri.p.n) – tri.p.d;
//    if (distp < 0.0f) return 0;
//
//    // Compute distance of q to triangle plane. Exit if q lies in front of plane
//    float distq = Dot(q, tri.p.n) – tri.p.d;
//    if (distq >= 0.0f) return 0;
//
//    // Compute t value and point s of intersection with triangle plane
//    float denom = distp – distq;
//    t = distp / denom;
//    s = p + t * (q – p);
//
//    // Compute the barycentric coordinate u; exit if outside 0..1 range
//    u = Dot(s, tri.edgePlaneBC.n) – tri.edgePlaneBC.d;
//    if (u < 0.0f || u > 1.0f) return 0;
//    // Compute the barycentric coordinate v; exit if negative
//    v = Dot(s, tri.edgePlaneCA.n) – tri.edgePlaneCA.d;
//    if (v < 0.0f) return 0;
//    // Compute the barycentric coordinate w; exit if negative
//    w = 1.0f – u – v;
//    if (w < 0.0f) return 0;
//
//    // Segment intersects tri at distance t in position s (s = u*A + v*B + w*C)
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//Triangle tri;
//Vector n = Cross(b – a, c – a);
//tri.p = Plane(n, a);
//tri.edgePlaneBC = Plane(Cross(n, c – b), b);
//tri.edgePlaneCA = Plane(Cross(n, a – c), c);
//
//--------------------------------------------------------------------------------
//
//tri.edgePlaneBC *= 1.0f / (Dot(a, tri.edgePlaneBC.n) – tri.edgePlaneBC.d);
//tri.edgePlaneCA *= 1.0f / (Dot(b, tri.edgePlaneCA.n) – tri.edgePlaneCA.d);
//
//=== Section 5.3.7: =============================================================
//
//// Intersect segment S(t)=sa+t(sb-sa), 0<=t<=1 against cylinder specified by p, q and r
//int IntersectSegmentCylinder(Point sa, Point sb, Point p, Point q, float r, float &t)
//{
//    Vector d = q – p, m = sa – p, n = sb – sa;
//    float md = Dot(m, d);
//    float nd = Dot(n, d);
//    float dd = Dot(d, d);
//    // Test if segment fully outside either endcap of cylinder
//    if (md < 0.0f && md + nd < 0.0f) return 0; // Segment outside ‘p’ side of cylinder
//    if (md > dd && md + nd > dd) return 0;     // Segment outside ‘q’ side of cylinder
//    float nn = Dot(n, n);
//    float mn = Dot(m, n);
//    float a = dd * nn – nd * nd;
//    float k = Dot(m, m) – r * r;
//    float c = dd * k – md * md;
//    if (Abs(a) < EPSILON) {
//        // Segment runs parallel to cylinder axis
//        if (c > 0.0f) return 0; // ‘a’ and thus the segment lie outside cylinder
//        // Now known that segment intersects cylinder; figure out how it intersects
//        if (md < 0.0f) t = -mn / nn; // Intersect segment against ‘p’ endcap
//        else if (md > dd) t = (nd - mn) / nn; // Intersect segment against ‘q’ endcap
//        else t = 0.0f; // ‘a’ lies inside cylinder
//        return 1;
//    }
//    float b = dd * mn – nd * md;
//    float discr = b * b – a * c;
//    if (discr < 0.0f) return 0; // No real roots; no intersection
//    t = (-b – Sqrt(discr)) / a;
//    if (t < 0.0f || t > 1.0f) return 0; // Intersection lies outside segment
//    if (md + t * nd < 0.0f) {
//        // Intersection outside cylinder on ‘p’ side
//        if (nd <= 0.0f) return 0; // Segment pointing away from endcap
//        t = -md / nd;
//        // Keep intersection if Dot(S(t) - p, S(t) - p) <= r^2
//        return k + 2 * t * (mn + t * nn) <= 0.0f;
//    } else if (md + t * nd > dd) {
//        // Intersection outside cylinder on ‘q’ side
//        if (nd >= 0.0f) return 0; // Segment pointing away from endcap
//        t = (dd – md) / nd;
//        // Keep intersection if Dot(S(t) - q, S(t) - q) <= r^2
//        return k + dd – 2 * md + t * (2 * (mn – nd) + t * nn) <= 0.0f;
//    }
//    // Segment intersects cylinder between the end-caps; t is correct
//    return 1;
//}
//
//=== Section 5.3.8: =============================================================
//
//// Intersect segment S(t)=A+t(B-A), 0<=t<=1 against convex polyhedron specified
//// by the n halfspaces defined by the planes p[]. On exit tfirst and tlast
//// define the intersection, if any
//int IntersectSegmentPolyhedron(Point a, Point b, Plane p[], int n,
//                               float &tfirst, float &tlast)
//{
//    // Compute direction vector for the segment
//    Vector d = b – a;
//    // Set initial interval to being the whole segment. For a ray, tlast should be
//    // set to +FLT_MAX. For a line, additionally tfirst should be set to –FLT_MAX
//    tfirst = 0.0f;
//    tlast = 1.0f;
//    // Intersect segment against each plane
//    for (int i = 0; i < n; i++) {
//        float denom = Dot(p[i].n, d);
//        float dist = p[i].d - Dot(p[i].n, a);
//        // Test if segment runs parallel to the plane
//        if (denom == 0.0f) {
//            // If so, return “no intersection” if segment lies outside plane
//            if (dist > 0.0f) return 0;
//        } else {
//            // Compute parameterized t value for intersection with current plane
//            float t = dist / denom;
//            if (denom < 0.0f) {
//                // When entering halfspace, update tfirst if t is larger
//                if (t > tfirst) tfirst = t;
//            } else {
//                // When exiting halfspace, update tlast if t is smaller
//                if (t < tlast) tlast = t;
//            }
//            // Exit with “no intersection” if intersection becomes empty
//            if (tfirst > tlast) return 0;
//        }
//    }
//    // A nonzero logical intersection, so the segment intersects the polyhedron
//    return 1;
//}
//
//=== Section 5.4.1: =============================================================
//
//// Test if point p lies inside ccw-specified convex n-gon given by vertices v[]
//int PointInConvexPolygon(Point p, int n, Point v[])
//{
//    // Do binary search over polygon vertices to find the fan triangle
//    // (v[0], v[low], v[high]) the point p lies within the near sides of
//    int low = 0, high = n;
//    do {
//        int mid = (low + high) / 2;
//        if (TriangleIsCCW(v[0], v[mid], p))
//            low = mid;
//        else
//            high = mid;
//    } while (low + 1 < high);
//
//    // If point outside last (or first) edge, then it is not inside the n-gon
//    if (low == 0 || high == n) return 0;
//
//    // p is inside the polygon if it is left of
//    // the directed edge from v[low] to v[high]
//    return TriangleIsCCW(v[low], v[high], p);
//}
//
//=== Section 5.4.2: =============================================================
//
//// Test if point P lies inside the counterclockwise triangle ABC
//int PointInTriangle(Point p, Point a, Point b, Point c)
//{
//    // Translate point and triangle so that point lies at origin
//    a -= p; b -= p; c -= p;
//    // Compute normal vectors for triangles pab and pbc
//    Vector u = Cross(b, c);
//    Vector v = Cross(c, a);
//    // Make sure they are both pointing in the same direction
//    if (Dot(u, v) < 0.0f) return 0;
//    // Compute normal vector for triangle pca
//    Vector w = Cross(a, b);
//    // Make sure it points in the same direction as the first two
//    if (Dot(u, w) < 0.0f) return 0;
//    // Otherwise P must be in (or on) the triangle
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//// Test if point P lies inside the counterclockwise 3D triangle ABC
//int PointInTriangle(Point p, Point a, Point b, Point c)
//{
//    // Translate point and triangle so that point lies at origin
//    a -= p; b -= p; c -= p;
//
//    float ab = Dot(a, b);
//    float ac = Dot(a, c);
//    float bc = Dot(b, c);
//    float cc = Dot(c, c);
//    // Make sure plane normals for pab and pbc point in the same direction
//    if (bc * ac – cc * ab < 0.0f) return 0;
//    // Make sure plane normals for pab and pca point in the same direction
//    float bb = Dot(b, b);
//    if (ab * bc – ac * bb < 0.0f) return 0;
//    // Otherwise P must be in (or on) the triangle
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//// Compute the 2D pseudo cross product Dot(Perp(u), v)
//float Cross2D(Vector2D u, Vector2D v)
//{
//    return u.y * v.x – u.x * v.y;
//}
//
//--------------------------------------------------------------------------------
//
//// Test if 2D point P lies inside the counterclockwise 2D triangle ABC
//int PointInTriangle(Point2D p, Point2D a, Point2D b, Point2D c)
//{
//    // If P to the right of AB then outside triangle
//    if (Cross2D(p – a, b – a) < 0.0f) return 0;
//    // If P to the right of BC then outside triangle
//    if (Cross2D(p – b, c – b) < 0.0f) return 0;
//    // If P to the right of CA then outside triangle
//    if (Cross2D(p – c, a – c) < 0.0f) return 0;
//    // Otherwise P must be in (or on) the triangle
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//// Test if 2D point P lies inside 2D triangle ABC
//int PointInTriangle2D(Point2D p, Point2D a, Point2D b, Point2D c)
//{
//    float pab = Cross2D(p – a, b – a);
//    float pbc = Cross2D(p – b, c – b);
//    // If P left of one of AB and BC and right of the other, not inside triangle
//    if (!SameSign(pab, pbc)) return 0;
//    float pca = Cross2D(p – c, a – c);
//    // If P left of one of AB and CA and right of the other, not inside triangle
//    if (!SameSign(pab, pca)) return 0;
//    // P left or right of all edges, so must be in (or on) the triangle
//    return 1;
//}
//
//=== Section 5.4.3: =============================================================
//
//// Test if point p inside polyhedron given as the intersection volume of n halfspaces
//int TestPointPolyhedron(Point p, Plane *h, int n)
//{
//    for (int i = 0; i < n; i++) {
//        // Exit with ‘no containment’ if p ever found outside a halfspace
//        if (DistPointPlane(p, h[i]) > 0.0f) return 0;
//    }
//    // p inside all halfspaces, so p must be inside intersection volume
//    return 1;
//}
//
//=== Section 5.4.4: =============================================================
//
//// Given planes p1 and p2, compute line L = p+t*d of their intersection.
//// Return 0 if no such line exists
//int IntersectPlanes(Plane p1, Plane p2, Point &p, Vector &d)
//{
//    // Compute direction of intersection line
//    d = Cross(p1.n, p2.n);
//
//    // If d is zero, the planes are parallel (and separated)
//    // or coincident, so they’re not considered intersecting
//    if (Dot(d, d) < EPSILON) return 0;
//
//    float d11 = Dot(p1.n, p1.n);
//    float d12 = Dot(p1.n, p2.n);
//    float d22 = Dot(p2.n, p2.n);
//
//    float denom = d11*d22 - d12*d12;
//    float k1 = (p1.d*d22 - p2.d*d12) / denom;
//    float k2 = (p2.d*d11 - p1.d*d12) / denom;
//    p = k1*p1.n + k2*p2.n;
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//// Given planes p1 and p2, compute line L = p+t*d of their intersection.
//// Return 0 if no such line exists
//int IntersectPlanes(Plane p1, Plane p2, Point &p, Vector &d)
//{
//    // Compute direction of intersection line
//    d = Cross(p1.n, p2.n);
//
//    // If d is (near) zero, the planes are parallel (and separated)
//    // or coincident, so they’re not considered intersecting
//    float denom = Dot(d, d);
//    if (denom < EPSILON) return 0;
//
//    // Compute point on intersection line
//    p = Cross(p1.d*p2.n - p2.d*p1.n, d) / denom;
//    return 1;
//}
//
//=== Section 5.4.5: =============================================================
//
//// Compute the point p at which the three planes p1, p2 and p3 intersect (if at all)
//int IntersectPlanes(Plane p1, Plane p2, Plane p3, Point &p)
//{
//    Vector m1 = Vector(p1.n.x, p2.n.x, p3.n.x);
//    Vector m2 = Vector(p1.n.y, p2.n.y, p3.n.y);
//    Vector m3 = Vector(p1.n.z, p2.n.z, p3.n.z);
//
//    Vector u = Cross(m2, m3);
//    float denom = Dot(m1, u);
//    if (Abs(denom) < EPSILON) return 0; // Planes do not intersect in a point
//    Vector d(p1.d, p2.d, p3.d);
//    Vector v = Cross(m1, d);
//    float ood = 1.0f / denom;
//    p.x = Dot(d, u) * ood;
//    p.y = Dot(m3, v) * ood;
//    p.z = -Dot(m2, v) * ood;
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//// Compute the point p at which the three planes p1, p2, and p3 intersect (if at all)
//int IntersectPlanes(Plane p1, Plane p2, Plane p3, Point &p)
//{
//    Vector u = Cross(p2.n, p3.n);
//    float denom = Dot(p1.n, u);
//    if (Abs(denom) < EPSILON) return 0; // Planes do not intersect in a point
//    p = (p1.d * u + Cross(p1.n, p3.d * p2.n – p2.d * p3.n)) / denom;
//    return 1;
//}
//
//=== Section 5.5.1: =============================================================
//
//// Intersect sphere s0 moving in direction d over time interval t0 <= t <= t1, against
//// a stationary sphere s1. If found intersecting, return time t of collision
//int TestMovingSphereSphere(Sphere s0, Vector d, float t0, float t1, Sphere s1, float &t)
//{
//    // Compute sphere bounding motion of s0 during time interval from t0 to t1
//    Sphere b;
//    float mid = (t0 + t1) * 0.5f;
//    b.c = s0.c + d * mid;
//    b.r = (mid – t0) * Length(d) + s0.r;
//    // If bounding sphere not overlapping s1, then no collision in this interval
//    if (!TestSphereSphere(b, s1)) return 0;
//
//    // Cannot rule collision out: recurse for more accurate testing. To terminate the
//    // recursion, collision is assumed when time interval becomes sufficiently small
//    if (t1 - t0 < INTERVAL_EPSILON) {
//        t = t0;
//        return 1;
//    }
//
//    // Recursively test first half of interval; return collision if detected
//    if (TestMovingSphereSphere(s0, d, t0, mid, s1, t)) return 1;
//
//    // Recursively test second half of interval
//    return TestMovingSphereSphere(s0, d, mid, t1, s1, t);
//}
//
//--------------------------------------------------------------------------------
//
//// Test collision between objects a and b moving over the time interval
//// [startTime, endTime]. When colliding, time of collision is returned in hitTime
//int IntervalCollision(Object a, Object b, float startTime, float endTime, float &hitTime)
//{
//    // Compute the maximum distance objects a and b move over the time interval
//    float maxMoveA = MaximumObjectMovementOverTime(a, startTime, endTime);
//    float maxMoveB = MaximumObjectMovementOverTime(b, startTime, endTime);
//    float maxMoveDistSum = maxMoveA + maxMoveB;
//    // Exit if distance between a and b at start larger than sum of max movements
//    float minDistStart = MinimumObjectDistanceAtTime(a, b, startTime);
//    if (minDistStart > maxMoveDistSum) return 0;
//    // Exit if distance between a and b at end larger than sum of max movements
//    float minDistEnd = MinimumObjectDistanceAtTime(a, b, endTime);
//    if (minDistEnd > maxMoveDistSum) return 0;
//
//    // Cannot rule collision out: recurse for more accurate testing. To terminate the
//    // recursion, collision is assumed when time interval becomes sufficiently small
//    if (endTime – startTime < INTERVAL_EPSILON) {
//        hitTime = startTime;
//        return 1;
//    }
//    // Recursively test first half of interval; return collision if detected
//    float midTime = (startTime + endTime) * 0.5f;
//    if (IntervalCollision(a, b, startTime, midTime, hitTime)) return 1;
//    // Recursively test second half of interval
//    return IntervalCollision(a, b, midTime, endTime, hitTime);
//}
//
//=== Section 5.5.3: =============================================================
//
//// Intersect sphere s with movement vector v with plane p. If intersecting
//// return time t of collision and point q at which sphere hits plane
//int IntersectMovingSpherePlane(Sphere s, Vector v, Plane p, float &t, Point &q)
//{
//    // Compute distance of sphere center to plane
//    float dist = Dot(p.n, s.c) - p.d;
//    if (Abs(dist) <= s.r) {
//        // The sphere is already overlapping the plane. Set time of
//        // intersection to zero and q to sphere center
//        t = 0.0f;
//        q = s.c;
//        return 1;
//    } else {
//        float denom = Dot(p.n, v);
//        if (denom * dist >= 0.0f) {
//            // No intersection as sphere moving parallel to or away from plane
//            return 0;
//        } else {
//            // Sphere is moving towards the plane
//
//            // Use +r in computations if sphere in front of plane, else -r
//            float r = dist > 0.0f ? s.r : -s.r;
//            t = (r - dist) / denom;
//            q = s.c + t * v – r * p.n;
//            return 1;
//        }
//    }
//}
//
//--------------------------------------------------------------------------------
//
//// Test if sphere with radius r moving from a to b intersects with plane p
//int TestMovingSpherePlane(Point a, Point b, float r, Plane p)
//{
//    // Get the distance for both a and b from plane p
//    float adist = Dot(a, p.n) - p.d;
//    float bdist = Dot(b, p.n) - p.d;
//    // Intersects if on different sides of plane (distances have different signs)
//    if (adist * bdist < 0.0f) return 1;
//    // Intersects if start or end position within radius from plane
//    if (Abs(adist) <= r || Abs(bdist) <= r) return 1;
//    // No intersection
//    return 0;
//}
//
//=== Section 5.5.5: =============================================================
//
//int TestMovingSphereSphere(Sphere s0, Sphere s1, Vector v0, Vector v1, float &t)
//{
//    Vector s = s1.c - s0.c;      // Vector between sphere centers
//    Vector v = v1 - v0;          // Relative motion of s1 with respect to stationary s0
//    float r = s1.r + s0.r;       // Sum of sphere radii
//    float c = Dot(s, s) – r * r;
//    if (c < 0.0f) {
//        // Spheres initially overlapping so exit directly
//        t = 0.0f;
//        return 1;
//    }
//    float a = Dot(v, v);
//    if (a < EPSILON) return 0; // Spheres not moving relative each other
//    float b = Dot(v, s);
//    if (b >= 0.0f) return 0;   // Spheres not moving towards each other
//    float d = b * b – a * c;
//    if (d < 0.0f) return 0;    // No real-valued root, spheres do not intersect
//
//    t = (-b – Sqrt(d)) / a;
//    return 1;
//}
//
//--------------------------------------------------------------------------------
//
//int TestMovingSphereSphere(Sphere s0, Sphere s1, Vector v0, Vector v1, float &t)
//{
//    // Expand sphere s1 by the radius of s0
//    s1.r += s0.r;
//    // Subtract movement of s1 from both s0 and s1, making s1 stationary
//    Vector v = v0 – v1;
//    // Can now test directed segment s = s0.c + tv, v = (v0-v1)/||v0-v1|| against
//    // the expanded sphere for intersection
//    Point q;
//    float vlen = Length(v);
//    if (IntersectRaySphere(s0.c, v / vlen, s1, t, q)) {
//        return t <= vlen;
//    }
//    return 0;
//}
//
//=== Section 5.5.7: =============================================================
//
//int IntersectMovingSphereAABB(Sphere s, Vector d, AABB b, float &t)
//{
//    // Compute the AABB resulting from expanding b by sphere radius r
//    AABB e = b;
//    e.min.x -= s.r; e.min.y -= s.r; e.min.z -= s.r;
//    e.max.x += s.r; e.max.y += s.r; e.max.z += s.r;
//
//    // Intersect ray against expanded AABB e. Exit with no intersection if ray
//    // misses e, else get intersection point p and time t as result
//    Point p;
//    if (!IntersectRayAABB(s.c, d, e, t, p) || t > 1.0f)
//        return 0;
//
//    // Compute which min and max faces of b the intersection point p lies
//    // outside of. Note, u and v cannot have the same bits set and
//    // they must have at least one bit set amongst them
//    int u = 0, v = 0;
//    if (p.x < b.min.x) u |= 1;
//    if (p.x > b.max.x) v |= 1;
//    if (p.y < b.min.y) u |= 2;
//    if (p.y > b.max.y) v |= 2;
//    if (p.z < b.min.z) u |= 4;
//    if (p.z > b.max.z) v |= 4;
//
//    // ‘Or’ all set bits together into a bit mask (note: here u + v == u | v)
//    int m = u + v;
//
//    // Define line segment [c, c+d] specified by the sphere movement
//    Segment seg(s.c, s.c + d);
//
//    // If all 3 bits set (m == 7) then p is in a vertex region
//    if (m == 7) {
//        // Must now intersect segment [c, c+d] against the capsules of the three
//        // edges meeting at the vertex and return the best time, if one or more hit
//        float tmin = FLT_MAX;
//        if (IntersectSegmentCapsule(seg, Corner(b, v), Corner(b, v ^ 1), s.r, &t))
//            tmin = Min(t, tmin);
//        if (IntersectSegmentCapsule(seg, Corner(b, v), Corner(b, v ^ 2), s.r, &t))
//            tmin = Min(t, tmin);
//        if (IntersectSegmentCapsule(seg, Corner(b, v), Corner(b, v ^ 4), s.r, &t))
//            tmin = Min(t, tmin);
//        if (tmin == FLT_MAX) return 0; // No intersection
//        t = tmin;
//        return 1; // Intersection at time t == tmin
//    }
//    // If only one bit set in m, then p is in a face region
//    if ((m & (m - 1)) == 0) {
//        // Do nothing. Time t from intersection with
//        // expanded box is correct intersection time
//        return 1;
//    }
//    // p is in an edge region. Intersect against the capsule at the edge
//    return IntersectSegmentCapsule(seg, Corner(b, u ^ 7), Corner(b, v), s.r, &t);
//}
//
//// Support function that returns the AABB vertex with index n
//Point Corner(AABB b, int n)
//{
//    Point p;
//    p.x = ((n & 1) ? b.max.x : b.min.x);
//    p.y = ((n & 1) ? b.max.y : b.min.y);
//    p.z = ((n & 1) ? b.max.z : b.min.z);
//    return p;
//}
//
//=== Section 5.5.8: =============================================================
//
//// Intersect AABBs ‘a’ and ‘b’ moving with constant velocities va and vb.
//// On intersection, return time of first and last contact in tfirst and tlast
//int IntersectMovingAABBAABB(AABB a, AABB b, Vector va, Vector vb, float &tfirst, float &tlast)
//{
//    // Exit early if ‘a’ and ‘b’ initially overlapping
//    if (TestAABBAABB(a, b)) {
//        tfirst = tlast = 0.0f;
//        return 1;
//    }
//
//    // Use relative velocity; effectively treating 'a' as stationary
//    Vector v = vb - va;
//
//    // Initialize times of first and last contact
//    tfirst = 0.0f;
//    tlast = 1.0f;
//
//    // For each axis, determine times of first and last contact, if any
//    for (int i = 0; i < 3; i++) {
//        if (v[i] < 0.0f) {
//            if (b.max[i] < a.min[i]) return 0; // Nonintersecting and moving apart
//            if (a.max[i] < b.min[i]) tfirst = Max((a.max[i] - b.min[i]) / v[i], tfirst);
//            if (b.max[i] > a.min[i]) tlast  = Min((a.min[i] - b.max[i]) / v[i], tlast);
//        }
//        if (v[i] > 0.0f) {
//            if (b.min[i] > a.max[i]) return 0; // Nonintersecting and moving apart
//            if (b.max[i] < a.min[i]) tfirst = Max((a.min[i] - b.max[i]) / v[i], tfirst);
//            if (a.max[i] > b.min[i]) tlast = Min((a.max[i] - b.min[i]) / v[i], tlast);
//        }
//
//        // No overlap possible if time of first contact occurs after time of last contact
//        if (tfirst > tlast) return 0;
//    }
//
//    return 1;
//}
//
