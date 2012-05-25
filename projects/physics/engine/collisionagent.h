//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#ifndef PR_PH_COLLISION_AGENT_H
#define PR_PH_COLLISION_AGENT_H
/*
#include "PR/Physics/Types/Types.h"
#include "PR/Physics/Types/Forward.h"
#include "PR/Physics/Collision/ContactManifold.h"
#include "PR/Physics/Collision/Collider.h"

namespace pr
{
	namespace ph
	{
		struct CollisionAgent
		{
			// Returns true if 'agent' is a collision agent for objects 'objA' and 'objB'
			bool IsAgentFor(Rigidbody const* objA, Rigidbody const* objB) const
			{
				#if PR_DBG_PHYSICS == 1
				if( m_objectA == objA && m_objectB == objB )
				{
					CollisionAgent tmp; tmp.Set(*objA, *objB, m_last_used, m_cache);
					PR_ASSERT(PR_DBG_PHYSICS, m_detection == tmp.m_detection);
				}
				#endif//PR_DBG_PHYSICS == 1
				return m_objectA == objA && m_objectB == objB;
			}

			// Set the objects that this agent as for
			void Set(Rigidbody const& objA, Rigidbody const& objB, std::size_t frame_number, CollisionCache* cache);			

			// Perform narrow phase collision detection
			void DetectCollision();

			// The objects whose bounding boxes overlap and therefore
			// need narrow phase collision detection performed
			Rigidbody const* m_objectA;
			Rigidbody const* m_objectB;

			// The collision detection function to call
			CollisionFunction m_detection;

			// The results of collision detection
			ContactManifold m_manifold;

			// The frame number for which this agent was last used
			std::size_t m_last_used;

			// The frame coherency collision cache
			// TODO: This should really just be a collision::CacheData object and forget about CollisionCache
			// Actually, CollisionAgents need to be in a hash table for fast look up (i.e. what CollisionCache is)
			CollisionCache* m_cache;
		};
	}//namespace ph
}//namespace pr
*/
#endif//PR_PH_COLLISION_AGENT_H