//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include <cassert>
#include "pr/common/alloca.h"
#include "pr/maths/maths.h"
#include "pr/collision/collision.h"
#include "pr/geometry/closest_point.h"

namespace pr
{
	// Given a 2D line that passes through 'a' and 'b' and another that passes through 'c' and 'd'
	// returns true if the lines intersect, false if they don't. Returns the point of intersect
	inline bool Intersect2D_InfiniteLineToInfiniteLine(v2 const& b, v2 const& a, v2 const& d, v2 const& c, v2& intersect)
	{
		v2 ab = b - a;
		v2 cd = d - c;
		float denom = ab.x * cd.y - ab.y * cd.x;
		if (FEql(denom, 0.0f)) return false;
		float e = b.x * a.y - b.y * a.x;
		float f = d.x * c.y - d.y * c.x;
		intersect.x = (cd.x * e - ab.x * f) / denom;
		intersect.y = (cd.y * e - ab.y * f) / denom;
		return true;
	}

	// Find the region of intersection between two convex polygons.
	// 'out' receives the vertices of the intersection polygon, in winding order
	template <typename Out> bool Intersect_ConvexPolygonToConvexPolygon(v4 const* poly0, int count0, v4 const* poly1, int count1, v4 const& norm, Out& out)
	{
		#if 0
		pr::v4 last_out;

		// Determine whether we're starting inside or outside of 'poly1'
		auto inside = PointWithinConvexPolygon(poly0[0], poly1, count1);
		if (inside)
			out(last_out = poly0[0])

		// Generate the planes for polyA
		auto planes = PR_ALLOCA_POD(pr::Plane, count0);
		for (auto i = 0; i != count0; ++i)
		{
			planes[i] = Cross3(norm, poly0[i] - poly0[i-1]);
			planes[i].w = Dot3(planes[i]
		}

		// For each edge of poly0, clip against edges of poly1
		for (auto i0 = 0; i0 != count0; ++i0)
		{

			for (auto i1 = 0; i1 != count1; ++i1)
			{
				Intersect_LineSegmentToPlane()
			}
		}

			
			
			
			
			auto polyA = poly0;
		auto polyB = poly1;

		if (count0 < count1)
		{

		}
		return false;
		#else
		throw std::exception("Not implemented");
		#endif
	}

	// Given a line that passes through 's' and 'e' and triangle 'abc'
	// Return true if the line intersects the triangle and if so, also
	// return the barycentric coordinates 'u,v,w' and parametric value 't'
	// of the intersection point
	inline bool Intersect_LineToTriangle(v4 const& s, v4 const& e, v4 const& a, v4 const& b, v4 const& c, float* t = nullptr, v4* bary = nullptr, float* f2b = nullptr, float tmin = 0.0f, float tmax = 1.0f)
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
		if (FEqlZero(sum))
			return false;

		float denom = 1.0f / sum;
		bary.x *= denom;
		bary.y *= denom;
		bary.z *= denom; // w = 1.0f - u - v;
		front_to_back = (denom > 0.0f) * 2.0f - 1.0f;
		return bary.x > -maths::tiny && bary.y > -maths::tiny && bary.z > -maths::tiny;
	}

	// Given a line passing through 's' with direction 'd', and initial parametric range [tmin,tmax],
	// returns true if the line pierces the sphere within the initial range.
	// The sphere is centred on the origin, 's' and 'd' should be in sphere space
	// 'tmin' and 'tmax' should be initialised to -FLT_MAX and FLT_MAX respectively for infinite line intersection.
	// Returns the parametric values of the intersection points.
	inline bool Intersect_LineToSphere(v4 const& s, v4 const& d, float radius, float& tmin, float& tmax)
	{
		auto d_sq = Dot3(d,d);
		if (d_sq < maths::tiny)
			return false; // zero length line

		// Find the closest point to the line
		auto c = s - d * (Dot3(d,s) / d_sq);
		auto c_sq = Dot3(c,c);

		// If the closest point is not within the sphere then there is no intersection
		auto rad_sq = radius * radius;
		if (rad_sq < c_sq)
			return false;

		// Get the distance from the closest point to the intersection with the boundary of the sphere
		auto x = Sqrt((rad_sq - c_sq) / d_sq); // include the normalising 1/d in x

		// Get the parametric values of the intersection
		auto offset = d * x;
		auto lstart = c - offset;
		auto lend   = c + offset;
		tmin = std::max(tmin, Dot3(d, lstart - s) / d_sq);
		tmax = std::min(tmax, Dot3(d, lend   - s) / d_sq);
		return true;
	}

	// Given a line passing through 's' with direction 'd', and initial parametric range [tmin,tmax],
	// returns true if the line pierces the axis aligned box within the initial range.
	// 'tmin' and 'tmax' should be initialised to -FLT_MAX and FLT_MAX respectively for infinite line intersection.
	// Returns the parametric values of the intersection points.
	inline bool Intersect_LineToBBox(v4 const& s, v4 const& d, BBox const& box, float& tmin, float& tmax)
	{
		auto bb_min = box.Lower();
		auto bb_max = box.Upper();

		// For all three slabs
		for (int i = 0; i != 3; ++i)
		{
			// If the line is parallel to the slab, then no hit if origin not within slab
			if (FEql(d[i], 0.0f))
			{
				if (s[i] < bb_min[i] || s[i] > bb_max[i])
					return false;
			}
			else
			{
				// Compute intersection t value of ray with near and far plane of slab
				auto ood = 1.0f / d[i];
				auto t1 = (bb_min[i] - s[i]) * ood;
				auto t2 = (bb_max[i] - s[i]) * ood;

				// Make t1 be intersection with near plane, t2 with far plane
				if (t1 > t2) Swap(t1, t2);

				// Compute the intersection of slab intersections intervals
				if (t1 > tmin) tmin = t1;
				if (t2 < tmax) tmax = t2;

				// Exit with no collision as soon as slab intersection becomes empty
				if (tmin > tmax)
					return false;
			}
		}
		return true;
	}

	// Test if the line segment starting at 's' and ending at 'e' with initial
	// parametric values 't0' and 't1' to the infinite plane described by 'plane'.
	// The portion of the line on the positive side of the plane is returned, described
	// by updated 't0' and 't1' values. 'plane' can be a normalised or unnormalised plane.
	// Returns true if the interval [t0,t1] is not zero.
	inline bool Intersect_LineSegmentToPlane(Plane const& plane, v4 const& s, v4 const& e, float& t0, float& t1)
	{
		// Find the distances to the plane for the start and end of the line
		float d0 = Distance_PointToPlane(s, plane);
		float d1 = Distance_PointToPlane(e, plane);
		if (d0 <= 0.0f && d1 <= 0.0f) return false;
		if (d0 >  0.0f && d1 >  0.0f) return true;

		// Calculate the parametric value at the intercept
		float t = d0 / (d0 - d1);
		if (d0 < 0.0f && t > t0) t0 = t; // Move the start point of the line onto the plane
		if (d0 > 0.0f && t < t1) t1 = t; // Move the end point onto the plane
		return t0 < t1;
	}

	// Test if the line segment starting at 's' and ending at 'e' intersects the infinite plane 'plane'.
	// Returns true if any part of the line is on the positive side of the plane.
	// Parameter aliasing is allowed, i.e. &s_out == &s is allowed
	inline bool Intersect_LineSegmentToPlane(Plane const& plane, v4 const& s, v4 const& e, v4& s_out, v4& e_out)
	{
		float d0 = Distance_PointToPlane(s, plane);
		float d1 = Distance_PointToPlane(e, plane);
		if (d0 <= 0.0f && d1 <= 0.0f) { s_out = s; e_out = s; return false; }
		if (d0 >  0.0f && d1 >  0.0f) { s_out = s; e_out = e; return true;  }

		float t = d0 / (d0 - d1);
		v4 intersept = (e - s) * t;
		if (d0 < 0.0f) s_out = s + intersept; // Move the start point onto the plane
		if (d0 > 0.0f) e_out = s + intersept; // Move the end point onto the plane
		return true;
	}

	// Test if a line segment specified by points 's' and 'e' intersects AABB b
	inline bool Intersect_LineSegmentToBoundingBox(v4 const& s, v4 const& e, BBox const& bbox)
	{
		v4 lineM = (s + e) * 0.5f;	// Line segment midpoint
		v4 lineH  = e - lineM;			// Line segment halflength vector
		lineM = lineM - bbox.m_centre;		// Translate box and segment to origin

		// Try world coordinate axes as separating axes
		float adx = Abs(lineH.x);	if (Abs(lineM.x) > bbox.m_radius.x + adx) return false;
		float ady = Abs(lineH.y);	if (Abs(lineM.y) > bbox.m_radius.y + ady) return false;
		float adz = Abs(lineH.z);	if (Abs(lineM.z) > bbox.m_radius.z + adz) return false;

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

	// Clip a line segment to between two parallel planes.
	// 'dist1' is the near plane distance, 'dist2' is the far plane distance
	// Returns true if any part of the line segment is within the slab
	// Parameter aliasing is allowed, i.e. &s_out == &s is allowed
	inline bool Intersect_LineToSlab(v4 const& norm, float dist1, float dist2, v4 const& s, v4 const& e, v4& s_out, v4& e_out)
	{
		assert(dist1 <= dist2);
		Plane plane;
		plane::set(plane, norm, dist1);

		float slab_width = dist2 - dist1;
		float d1 = Distance_PointToPlane(s ,plane);
		float d2 = Distance_PointToPlane(e ,plane);
		if (d1 < 0.0f       && d2 < 0.0f      ) { e_out = s_out = s; return false; }
		if (d1 > slab_width && d2 > slab_width) { e_out = s_out = s; return false; }

		v4 start = s;
		v4 line  = e - s;
		float dsum = d1 - d2;
		if      (d1 < 0.0f)       { float p = d1 / dsum;                s_out = start + line * p; } // Intercept with the near plane
		else if (d1 > slab_width) { float p = (d1 - slab_width) / dsum; s_out = start + line * p; } // Intercept with the far plane
		if      (d2 < 0.0f)       { float p = d1 / dsum;                e_out = start + line * p; } // Intercept with the near plane
		else if (d2 > slab_width) { float p = (d1 - slab_width) / dsum; e_out = start + line * p; } // Intercept with the far plane
		return true;
	}

	// Returns true if 'bbox' intersects 'plane'
	inline bool Intersect_BBoxToPlane(BBox const& bbox, Plane const& plane)
	{
		// If the eight corners of the box are on the same side of the plane then there's no intersect
		bool first_side = Dot4(GetCorner(bbox, 0), plane) > 0.0f;
		for (uint corner = 1; corner != 8; ++corner)
		{
			bool this_side = Dot4(GetCorner(bbox, corner), plane) > 0.0f;
			if (this_side != first_side)
				return false;
		}
		return true;
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool Intersect_BBoxToBBox(BBox const& lhs, BBox const& rhs)
	{
		return	Abs(lhs.m_centre.x - rhs.m_centre.x) <= (lhs.m_radius.x + rhs.m_radius.x) &&
				Abs(lhs.m_centre.y - rhs.m_centre.y) <= (lhs.m_radius.y + rhs.m_radius.y) &&
				Abs(lhs.m_centre.z - rhs.m_centre.z) <= (lhs.m_radius.z + rhs.m_radius.z);
	}

	// Returns true if 'lhs' and 'rhs' intersect
	inline bool Intersect_OBoxToOBox(OBox const& lhs, OBox const& rhs)
	{
		using namespace pr::collision;
		auto b0 = ShapeBox::make(lhs);
		auto b1 = ShapeBox::make(rhs);
		return BoxVsBox(b0, lhs.m_box_to_world, b1, rhs.m_box_to_world);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_geometry_intersect)
		{
			{// Intersect_LineToBBox
				float tmin = 0.0f, tmax = 1.0f;
				auto s = pr::v4::make(+1.0f, +0.2f, +0.5f, 1.0f);
				auto e = pr::v4::make(-1.0f, -0.2f, -0.4f, 1.0f);
				auto d = e - s;
				auto bbox = pr::BBox::make(pr::v4Origin, pr::v4::make(0.25f, 0.15f, 0.2f, 0.0f));
				
				auto r = Intersect_LineToBBox(s, d, bbox, tmin, tmax);
				PR_CHECK(r, true);
				PR_CHECK(pr::FEql3(s + tmin*d, pr::v4::make(+0.25f, +0.05f, +0.163f, 1.0f), 0.001f), true);
				PR_CHECK(pr::FEql3(s + tmax*d, pr::v4::make(-0.25f, -0.05f, -0.063f, 1.0f), 0.001f), true);

				s = pr::v4::make(+1.0f, +0.2f, -0.22f, 1.0f);
				r = Intersect_LineToBBox(s, d, bbox, tmin, tmax);
				PR_CHECK(r, false);
			}
			{// Intersect_LineToSphere
				float tmin = 0.0f, tmax = 1.0f;
				auto s = pr::v4::make(+1.0f, +0.2f, +0.5f, 1.0f);
				auto e = pr::v4::make(-1.0f, -0.2f, -0.4f, 1.0f);
				auto d = e - s;
				auto rad = 0.3f;
				
				auto r = Intersect_LineToSphere(s, d, rad, tmin, tmax);
				PR_CHECK(r, true);
				PR_CHECK(pr::FEql3(s + tmin*d, pr::v4::make(+0.247f, +0.049f, +0.161f, 1.0f), 0.001f), true);
				PR_CHECK(pr::FEql3(s + tmax*d, pr::v4::make(-0.284f, -0.057f, -0.078f, 1.0f), 0.001f), true);

				s = pr::v4::make(+1.0f, +0.2f, -0.22f, 1.0f);
				r = Intersect_LineToSphere(s, d, rad, tmin, tmax);
				PR_CHECK(r, false);
			}
		}
	}
}
#endif