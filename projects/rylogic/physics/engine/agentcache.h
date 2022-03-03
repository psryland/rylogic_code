//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#ifndef PR_PHYSICS_AGENT_CACHE_H
#define PR_PHYSICS_AGENT_CACHE_H
/*
#include "PR/Common/DefaultAllocator.h"
#include "PR/Physics/Types/Types.h"
#include "PR/Physics/Types/Forward.h"
#include "PR/Physics/Engine/CollisionAgent.h"

namespace pr
{
	namespace ph
	{
		class AgentCache
		{
		public:
			AgentCache(AllocFunction allocate, DeAllocFunction deallocate);
			~AgentCache();

			// Set the capacity of the collision agent cache
			// Prime numbers are a good choice
			void SetCacheSize(std::size_t cache_size);

			// Retrieve an agent for two objects
			CollisionAgent& GetAgent(Rigidbody const& objA, Rigidbody const& objB, std::size_t frame_number, CollisionCache* cache);

			// Invalid any cache entries that refer to 'obj'
			void Invalidate(Rigidbody const& obj);
		
		private:
			std::size_t Hash(Shape const* shapeA, Shape const* shapeB);
			std::size_t Hash(Rigidbody const* objA, Rigidbody const* objB);
	
		private:
			AllocFunction	m_Allocate;
			DeAllocFunction m_Deallocate;
			CollisionAgent* m_agent;
			std::size_t		m_max_agents;
			CollisionAgent  m_spare;
		};
	}//namespace ph
}//namespace pr
*/
#endif//PR_PHYSICS_AGENT_CACHE_H
