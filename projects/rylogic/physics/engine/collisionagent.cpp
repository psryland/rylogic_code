//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#include "PR/Physics/Utility/Stdafx.h"
#include "PR/Physics/Utility/Profile.h"
#include "PR/Physics/Engine/CollisionAgent.h"
#include "PR/Physics/Rigidbody/Rigidbody.h"

using namespace pr;
using namespace pr::ph;

// Set the objects that this agent as for
void CollisionAgent::Set(Rigidbody const& objA, Rigidbody const& objB, std::size_t frame_number, CollisionCache* cache)
{
	m_objectA	= &objA;
	m_objectB	= &objB;
	m_last_used = frame_number;
	m_detection = GetCollisionDetectionFunction(*objA.m_shape, *objB.m_shape);
	m_cache		= cache;
}

// Perform narrow phase collision detection
void CollisionAgent::DetectCollision()
{
	PR_DECLARE_PROFILE(PR_PROFILE_COLLISION_DETECTION, phDetectCol);
	PR_PROFILE_SCOPE  (PR_PROFILE_COLLISION_DETECTION, phDetectCol);
	PR_EXPAND(PR_DBG_COLLISION, ldr::PhCollisionScene(*m_objectA->m_shape, m_objectA->m_object_to_world, *m_objectB->m_shape, m_objectB->m_object_to_world, "scene"));
	
	m_manifold.Reset(*m_objectA, *m_objectB);
	m_detection(*m_objectA->m_shape, m_objectA->m_object_to_world, *m_objectB->m_shape, m_objectB->m_object_to_world, m_manifold, m_cache);
}
