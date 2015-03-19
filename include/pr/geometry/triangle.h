//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"

namespace pr
{
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