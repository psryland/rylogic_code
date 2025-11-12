//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_RAY_CAST_H
#define PR_PHYSICS_RAY_CAST_H

#include "pr/physics/types/forward.h"
#include "pr/physics/ray/ray.h"
#include "pr/physics/ray/raycastresult.h"

namespace pr
{
	namespace ph
	{
		// Return the intercept of a ray vs. a shape.
		// The ray must be in shape space. 
		// 'result' is not cummulative, each call overwrites t0 and t1
		// Returns true if the ray hits the shape
		bool RayCast(Ray const& ray, Shape         const& shape, RayCastResult& result);
		bool RayCast(Ray const& ray, ShapeSphere   const& shape, RayCastResult& result);
		bool RayCast(Ray const& ray, ShapeBox      const& shape, RayCastResult& result);
		bool RayCast(Ray const& ray, ShapeCylinder const& shape, RayCastResult& result);
		bool RayCast(Ray const& ray, ShapePolytope const& shape, RayCastResult& result);
		bool RayCast(Ray const& ray, ShapeTriangle const& shape, RayCastResult& result);
		bool RayCast(Ray const& ray, ShapeArray    const& shape, RayCastResult& result);

		// Cast a world space ray
		template <typename ShapeType>
		inline bool RayCastWS(Ray const& ray, ShapeType const& shape, m4x4 const& s2w, RayCastResult& result)
		{
			if( !RayCast(InvertAffine(s2w) * ray, shape, result) ) return false;
			result.m_normal = s2w * result.m_normal;
			return true;
		}

		bool RayCastBruteForce(Ray const& ray, ShapePolytope const& shape, RayCastResult& result);
	}
}

#endif

