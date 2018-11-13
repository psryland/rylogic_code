//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"

namespace pr
{
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
		return Triple(b-a, c-a, d-a) / 6.0f;
	}
}