//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_COLLISION_OBSERVER_H
#define PR_PHYSICS_COLLISION_OBSERVER_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		struct IPreCollisionObserver
		{
			virtual ~IPreCollisionObserver() {}
			// The manifold is non-const here so that the caller can change things like
			// the materials just prior to a collision.
			virtual bool NotifyPreCollision(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold& manifold) = 0;
		};

		struct IPstCollisionObserver
		{
			virtual ~IPstCollisionObserver() {}
			virtual void NotifyPstCollision(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold const& manifold) = 0;
		};
	}
}

#endif
