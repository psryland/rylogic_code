//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_PLANE_H
#define PR_MATHS_PLANE_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"

namespace pr
{
	// Planes are stored as: [dx dy dz -dist]
	typedef v4 Plane;

	namespace plane
	{
		// The const functions are called 'Make' and the non-const functions
		// are called 'Set' to stop clashes with overloaded function names
		inline Plane& set(Plane& plane, float dx, float dy, float dz, float dist) { return plane.set(dx, dy, dz, -dist); }
		inline Plane& set(Plane& plane, v4 const& point, v4 const& direction)     { plane = direction; plane.w = -Dot3(point, direction); return plane; }
		inline Plane& set(Plane& plane, v4 const& a, v4 const& b, v4 const& c)    { plane = Normalise3(Cross3(b-a, c-a)); plane.w = -Dot3(a, plane); return plane; }
		inline Plane& set(Plane& plane, v4 const& norm, float dist)               { plane = norm; plane.w = -dist; return plane; }
		inline Plane  make(float dx, float dy, float dz, float dist)              { return v4::make(dx, dy, dz, dist); }
		inline Plane  make(v4 const& point, v4 const& direction)                  { Plane p; return set(p, point, direction); }
		inline Plane  make(v4 const& a, v4 const& b, v4 const& c)                 { Plane p; return set(p, a, b, c); }
		inline Plane  make(v4 const& norm, float dist)                            { Plane p; return set(p, norm, dist); }
		inline v4     GetDirection(Plane const& plane)                            { return v4::make(plane.x, plane.y, plane.z, 0.0f); }
		inline float  GetDistance(Plane const& plane)                             { return -plane.w; }
		inline Plane  Normalise(Plane const& plane)                               { return plane / Length3(plane); }

		// Make a best fit plane for a set of points. (designed for polygons really)
		// This is using Newell's method of projecting the points into the yz, xz, and xy planes
		inline Plane& set(Plane& plane, v4 const* begin, v4 const* end)
		{
			plane     = v4Zero;
			v4 centre = v4Zero;
			for (v4 const *i = end - 1, *j = begin; j != end; i = j, ++j)
			{
				// Compute the normal as being proportional to the projected areas
				// of the polygon onto the yz, xz, and xy planes.
				// Also, compute centroid as representative point on the plane		
				plane.x += (i->y - j->y) * (i->z + j->z);	// Projection onto YZ
				plane.y += (i->z - j->z) * (i->x + j->x);	// Projection onto XZ
				plane.z += (i->x - j->x) * (i->y + j->y);	// Projection onto XY
				centre  += *j;
			}
			plane = Normalise3(plane);
			plane.w = Dot4(centre, plane) / (end - begin);	// Centre / (end - begin) is the true centre
			return plane;
		}

		inline Plane make(v4 const* begin, v4 const* end)
		{
			Plane p;
			return set(p, begin, end);
		}
		
		// Returns 'v' projected onto 'plane'
		// So if plane.w == -dist, if v.w == 1 the returned point will lie on the plane at 'dist'
		// from the origin. if v.w == 0, the returned vector will lie in a plane parallel to 'plane'.
		inline v4 Project(Plane const& plane, v4 const& v)
		{
			return v - pr::Dot4(plane, v) * plane.w0();
		}
	}
}

#endif
