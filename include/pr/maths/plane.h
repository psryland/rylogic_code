//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"

namespace pr
{
	// Planes are stored as: [dx dy dz -dist]
	using Plane = v4;

	namespace plane
	{
		// Create a plane from components
		inline Plane make(float dx, float dy, float dz, float dist)
		{
			return Plane(dx, dy, dz, dist);
		}

		// Create a point from a point and direction (not necessarily unit length)
		inline Plane make(v4 const& point, v4 const& direction)
		{
			Plane p = direction;
			p.w = -Dot3(point, direction);
			return p;
		}

		// Create a point from 3 points in 3D space
		inline Plane  make(v4 const& a, v4 const& b, v4 const& c)
		{
			Plane p = Normalise3(Cross3(b-a, c-a));
			p.w = -Dot3(a, p);
			return p;
		}

		// Create from a normal direction and distance
		inline Plane  make(v4 const& norm, float dist)
		{
			Plane p = norm;
			p.w = -dist;
			return p;
		}

		// Make a best fit plane for a set of points. (designed for polygons really)
		// This is using Newell's method of projecting the points into the yz, xz, and xy planes
		inline Plane make(v4 const* begin, v4 const* end)
		{
			Plane p = v4Zero;
			v4 centre = v4Zero;
			for (v4 const *i = end - 1, *j = begin; j != end; i = j, ++j)
			{
				// Compute the normal as being proportional to the projected areas
				// of the polygon onto the yz, xz, and xy planes.
				// Also, compute centroid as representative point on the plane
				p.x += (i->y - j->y) * (i->z + j->z); // Projection onto YZ
				p.y += (i->z - j->z) * (i->x + j->x); // Projection onto XZ
				p.z += (i->x - j->x) * (i->y + j->y); // Projection onto XY
				centre  += *j;
			}
			p = Normalise3(p);
			p.w = Dot4(centre, p) / (end - begin);	// Centre / (end - begin) is the true centre
			return p;
		}

		// Return the direction vector component of a plane
		inline v4 Direction(Plane const& plane)
		{
			return plane.w0();
		}

		// Return the distance component of a plane
		inline float Distance(Plane const& plane)
		{
			return -plane.w;
		}

		// Normalise (Canonicalise a plane)
		inline Plane Normalise(Plane const& plane)
		{
			return plane / Length3(plane); // This scales the w-component as well
		}

		// Returns 'v' projected onto 'plane'
		// So if plane.w == -dist, if v.w == 1 the returned point will lie on the plane at 'dist'
		// from the origin. if v.w == 0, the returned vector will lie in a plane parallel to 'plane'.
		inline v4 Project(Plane const& plane, v4 const& v)
		{
			return v - Dot4(plane, v) * plane.w0();
		}
	}
}
