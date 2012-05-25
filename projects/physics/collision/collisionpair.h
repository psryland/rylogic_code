
NOT USED


//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

// No this isn't. broadphase::Pair's are now returned
// This is the object returned from the broad phase. There is one
// CollisionPair for every pair of potentially colliding physics
// instances.
// Narrow phase collision detection turns these pairs into ContactManifolds
// 
#ifndef PR_PHYSICS_COLLISION_PAIR_H
#define PR_PHYSICS_COLLISION_PAIR_H

#include "PR/Physics/Types/Forward.h"

namespace pr
{
	namespace ph
	{
		struct CollisionPair
		{
		};
		//Contact			m_contact;			// The deepest point of contact between objA and objB
		//float			m_lamda;			// The magnitude of the contact force from the last frame (0 if first frame)
		//
		//// This is reserved for the step function of the physics engine. The engine
		//// links all of the actually colliding objects for later collision resolution
		//CollisionPair*	m_next_collision;
		//bool CalculatePostDetectionData() { PR_ERROR(1); return true; }

	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_COLLISION_PAIR_H
