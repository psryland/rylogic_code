//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_RAY_CAST_RESULT_H
#define PR_PHYSICS_RAY_CAST_RESULT_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		struct RayCastResult
		{
			float			m_t0;				// The parametric range of the portion of the ray inside the shape
			float			m_t1;				// The parametric range of the portion of the ray inside the shape
			v4				m_normal;			// The normal of the incident face
			Shape const*	m_shape;			// The primitive that was hit
		};

		struct RayVsWorldResult
		{
			float				m_intercept;	// The parametric value of the intercept
			v4					m_normal;		// The normal at the point of intercept
			Rigidbody const*	m_object;		// The physics object that was hit
			Shape const*		m_shape;		// The primitive in the shape that was hit
		};
	}
}

#endif

