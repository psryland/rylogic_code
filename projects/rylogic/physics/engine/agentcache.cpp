//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#include "PR/Physics/Utility/Stdafx.h"
#include "PR/Common/Meta/AlignmentOf.h"
#include "PR/Physics/Engine/AgentCache.h"
#include "PR/Physics/Engine/CollisionAgent.h"
#include "PR/Physics/Rigidbody/Rigidbody.h"

using namespace pr;
using namespace pr::ph;

// Hash two shape pointers together to give a cache entry index
inline std::size_t AgentCache::Hash(Shape const* shapeA, Shape const* shapeB)
{
	std::size_t a = byte_ptr_cast(shapeA) - byte_ptr_cast<Shape const>(0);
	std::size_t b = byte_ptr_cast(shapeB) - byte_ptr_cast<Shape const>(0);
	return (a ^ b) % m_max_agents;
}
inline std::size_t AgentCache::Hash(Rigidbody const* objA, Rigidbody const* objB)
{
	return Hash(objA->m_shape, objB->m_shape);
}

// Constructor
AgentCache::AgentCache(AllocFunction allocate, DeAllocFunction deallocate)
:m_Allocate(allocate)
,m_Deallocate(deallocate)
,m_agent(&m_spare)
,m_max_agents(1)
{
	memset(m_agent, 0, m_max_agents * sizeof(CollisionAgent));
}

// Destructor
AgentCache::~AgentCache()
{
	if( m_agent != &m_spare )
		m_Deallocate(m_agent);
}

// Set the capacity of the collision agent cache
// Prime numbers are a good choice
void AgentCache::SetCacheSize(std::size_t cache_size)
{
	if( m_agent != &m_spare )
	{
		m_Deallocate(m_agent);
		m_agent		 = &m_spare;
		m_max_agents = 1;
	}
	if( cache_size > 0 )
	{
		m_agent		 = static_cast<CollisionAgent*>(m_Allocate(cache_size * sizeof(CollisionAgent), meta::alignment_of<CollisionAgent>::value));
		m_max_agents = cache_size;
	}
	memset(m_agent, 0, m_max_agents * sizeof(CollisionAgent));
}

// Retrieve an agent for two objects
CollisionAgent& AgentCache::GetAgent(Rigidbody const& objA, Rigidbody const& objB, std::size_t frame_number, CollisionCache* cache)
{
	// Get the objects the correct way round
	Rigidbody const& obj_a = (objA.m_shape->m_type <= objB.m_shape->m_type) ? objA : objB;
	Rigidbody const& obj_b = (objA.m_shape->m_type <= objB.m_shape->m_type) ? objB : objA;

	std::size_t     hash  = Hash(&obj_a, &obj_b);
	CollisionAgent& agent = m_agent[hash];

	// If the agent returned is the correct one, yay
	if( agent.IsAgentFor(&obj_a, &obj_b) )		{ agent.m_last_used = frame_number; return agent; }

	// If the agent returned is unused, claim it for this pair
	if( frame_number - agent.m_last_used > 1 )	{ agent.Set(obj_a, obj_b, frame_number, cache); return agent; }

	// Otherwise use the spare
	m_spare.Set(obj_a, obj_b, frame_number, cache);
	return m_spare;
}

// Invalidate any cache entries that refer to 'obj'
void AgentCache::Invalidate(Rigidbody const& obj)
{
	for( CollisionAgent* i = m_agent, *i_end = m_agent + m_max_agents; i != i_end; ++i )
	{
		reinterpret_cast<std::ptrdiff_t&>(i->m_objectA) &= (i->m_objectA != &obj);
		reinterpret_cast<std::ptrdiff_t&>(i->m_objectB) &= (i->m_objectB != &obj);
	}
}
