//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once
#include "pr/geometry/common.h"

namespace pr
{
	// Forwards
	float pr_vectorcall Distance_PointToPlane(v4_cref<> point, Plane const& plane);

	// Returns true if 'point' lies in front of the plane described by 'abc' (Cross3(b-a, c-a))
	inline bool pr_vectorcall PointInFrontOfPlane(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c)
	{
		assert(point.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);
		return Triple(point - a, b - a, c - a) >= 0.0f;
	}

	// Return a point that is the weighted result of verts 'a','b','c' and 'bary'
	inline v4 pr_vectorcall BaryPoint(v4_cref<> a, v4_cref<> b, v4_cref<> c, v4_cref<> bary)
	{
		return bary.x * a + bary.y * b + bary.z * c;
	}

	// Return the 'Bary-Centric' coordinates for 'point' with respect to triangle a,b,c
	inline v4 pr_vectorcall BaryCentric(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c)
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
	inline bool pr_vectorcall PointWithinTriangle(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c, float tol)
	{
		v4 bary = BaryCentric(point, a, b, c);
		return	bary.x >= -tol && bary.x <= 1.0f + tol &&
				bary.y >= -tol && bary.y <= 1.0f + tol &&
				bary.z >= -tol && bary.z <= 1.0f + tol;
	}

	// Returns true if a point projects within a triangle using the triangle normal
	inline bool pr_vectorcall PointWithinTriangle2(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c, float tol)
	{
		v4 c0 = Cross3(point - a, b - a);
		v4 c1 = Cross3(point - b, c - b);
		v4 c2 = Cross3(point - c, a - c);
		return Dot3(c0, c1) >= -tol && Dot3(c0, c2) >= -tol;
	}

	// Returns true if a point projects within a triangle using the triangle normal. Also returns the point
	inline bool pr_vectorcall PointWithinTriangle(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c, v4& pt)
	{
		v4 bary = BaryCentric(point, a, b, c);
		pt = a * bary.x + b * bary.y + c * bary.z; pt.w = 1.0f;
		return	bary.x >= 0.0f && bary.x <= 1.0f &&
				bary.y >= 0.0f && bary.y <= 1.0f &&
				bary.z >= 0.0f && bary.z <= 1.0f;
	}

	// Returns true if 'point' lies on or within the tetrahedron described by 'abcd' (i.e. behind all of it's planes)
	inline bool pr_vectorcall PointWithinTetrahedron(v4_cref<> point, v4_cref<> a, v4_cref<> b, v4_cref<> c, v4_cref<> d)
	{
		return	!PointInFrontOfPlane(point, a, b, c) &&
				!PointInFrontOfPlane(point, a, c, d) &&
				!PointInFrontOfPlane(point, a, d, b) &&
				!PointInFrontOfPlane(point, d, c, b);
	}

	// Returns true if 'point' projects along 'norm' into the convex polygon 'poly'
	// On the edge of the polygon counts as outside so that polygons with
	// degenerate edges are all classed as outside. 
	inline bool pr_vectorcall PointWithinConvexPolygon(v4_cref<> point, v4 const* poly, int count, v4_cref<> norm)
	{
		if (count < 3)
			return false;

		auto TriangleIsCCW = [&](v4_cref<> a, v4_cref<> b, v4_cref<> c) { return Triple(norm, b - a, c - a) > 0.0f; };

		// Do a binary search over polygon vertices to find the triangle fan
		// (poly[0], poly[lo], poly[hi]) that 'point' lies in.
		int lo = 0, hi = count;
		do
		{
			auto mid = (lo + hi) / 2;
			if (TriangleIsCCW(poly[0], poly[mid], point))
				lo = mid;
			else
				hi = mid;
		}
		while (lo + 1 < hi);

		// If point outside last (or first) edge, then it is not inside the polygon
		if (lo == 0 || hi == count)
			return false;

		// 'point' is inside the polygon if it is left of the edge from v[low] to v[high]
		return TriangleIsCCW(poly[lo], poly[hi], point);
	}
	inline bool pr_vectorcall PointWithinConvexPolygon(v4_cref<> point, v4 const* poly, int count)
	{
		if (count < 3)
			return false;

		// Find the face direction
		for (int i = 2; i != count; ++i)
		{
			auto norm = Cross3(poly[i-1] - poly[0], poly[i] - poly[0]);
			if (FEql(norm, v4Zero)) continue;
			return PointWithinConvexPolygon(point, poly, count, norm);
		}
		
		// degenerate polygon, no face normal
		return false;
	}

	// Returns true if 'point' is on the positive side of all of 'planes'
	inline bool pr_vectorcall PointWithinHalfSpaces(v4_cref<> point, Plane const* planes, int count, float tol = maths::tinyf)
	{
		for (auto i = 0; i != count; ++i)
		{
			if (Distance_PointToPlane(point, planes[i]) < -tol)
				return false;
		}
		return true;
	}
}
