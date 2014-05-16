//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PH_RESOLVE_COLLISION_H
#define PR_PH_RESOLVE_COLLISION_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		void ResolveCollision(Rigidbody& objectA, Rigidbody& objectB, ContactManifold& manifold);

		// Sets the material properties for the collision
		void SetMaterialProperties(Contact& contact);

		// Returns the impulse that would resolve a collision between two objects.
		// 'ws_impulse' is a world space impulse that should be applied to object A
		// (-ws_impulse should be applied to object B)
		void ImpulseResponse(Contact& contact, v4& ws_impulse);
		
		// Calculate rolling friction impulses
		void RollingFriction(Contact& contact, v4& rf_impulse, v4& rf_twist);

		// Separate two objects by the penetration distance
		void PushOut(Rigidbody& objA, Rigidbody& objB, Contact& contact);
	}
}

#endif
