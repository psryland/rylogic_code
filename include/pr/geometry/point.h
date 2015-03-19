//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"

namespace pr
{
	// Returns true if 'point' lies in front of the plane described by abc (Cross3(b-a, c-a))
	inline bool PointInFrontOfPlane(v4 const& point, v4 const& a, v4 const& b, v4 const& c)
	{
		assert(point.w == 1.0f && a.w == 1.0f && b.w == 1.0f && c.w == 1.0f);
		return Triple3(point - a, b - a, c - a) >= 0.0f;
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
}