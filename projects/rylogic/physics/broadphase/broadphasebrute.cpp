//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#include "physics/utility/stdafx.h"
#include "pr/physics/broadphase/broadphasebrute.h"
#include "pr/physics/ray/ray.h"
#include "physics/broadphase/bppair.h"
#include "physics/utility/assert.h"
#include "physics/utility/profile.h"

using namespace pr;
using namespace pr::ph;
using namespace pr::geometry;

BPBruteForce::BPBruteForce()
{
	PR_EXPAND(PR_DBG_PHYSICS, m_enumerating = false);
}

// Add/Remove a broadphase entity to the broadphase.
// The entity should be a member of the object you want broadphase'd
void BPBruteForce::Add(BPEntity& entity)
{
	PR_ASSERT(PR_DBG_PHYSICS, !m_enumerating, "Do not modify the broadphase while pair enumeration is happening");
	entity.m_broadphase = this;

	PR_ASSERT(PR_DBG_PHYSICS, std::find(m_entity.begin(), m_entity.end(), &entity) == m_entity.end(), "Object already in broadphase");
	m_entity.push_back(&entity);
}

// Add/Remove a broadphase entity to the broadphase.
// The entity should be a member of the object you want broadphase'd
void BPBruteForce::Remove(BPEntity& entity)
{
	PR_ASSERT(PR_DBG_PHYSICS, !m_enumerating, "Do not modify the broadphase while pair enumeration is happening");
	entity.m_broadphase = 0;

	BPEntityCont::iterator at = std::find(m_entity.begin(), m_entity.end(), &entity);
	PR_ASSERT(PR_DBG_PHYSICS, at != m_entity.end(), "Object not in broadphase");
	m_entity.erase(at);
}

// Empty the broadphase.
void BPBruteForce::RemoveAll()
{
	PR_ASSERT(PR_DBG_PHYSICS, !m_enumerating, "Do not modify the broadphase while pair enumeration is happening");
	for (BPEntityCont::iterator i = m_entity.begin(), i_end = m_entity.end(); i != i_end; ++i)
	{
		PR_ASSERT(PR_DBG_PHYSICS, (*i)->m_broadphase == this, "This entity does not refer to this broadphase");
		(*i)->m_broadphase = 0;
	}
	m_entity.clear();
}

// Iterate over the colliding pairs
void BPBruteForce::EnumPairs(EnumPairsFunc func, void* context)
{
	PR_DECLARE_PROFILE(PR_PROFILE_BROADPHASE, phBroadphaseBrute);
	PR_PROFILE_SCOPE(PR_PROFILE_BROADPHASE, phBroadphaseBrute);
	PR_ASSERT(PR_DBG_PHYSICS, !m_enumerating, "Pair enumeration is not reentrant");
	PR_EXPAND(PR_DBG_PHYSICS, auto enumer = pr::Scope<void>(
		[&] { m_enumerating = true; },
		[&] { m_enumerating = false; }));

	// An O(N^2) test for colliding pairs 
	for (BPEntityCont::const_iterator i = m_entity.begin(), i_end = m_entity.end(); i != i_end; ++i)
	{
		for (BPEntityCont::const_iterator j = i + 1, j_end = m_entity.end(); j != j_end; ++j)
		{
			if (intersect::BBoxVsBBox(*(*i)->m_bbox, *(*j)->m_bbox))
			{
				BPPair pair;
				pair.m_objectA = *i;
				pair.m_objectB = *j;
				func(pair, context);
			}
		}
	}
}

// Enumerate all overlaps with 'entity'
void BPBruteForce::EnumPairs(EnumPairsFunc func, BPEntity const& entity, void* context)
{
	PR_EXPAND(PR_DBG_PHYSICS, auto enumer = pr::Scope<void>(
		[&] { m_enumerating = true; },
		[&] { m_enumerating = false; }));

	for (BPEntityCont::const_iterator i = m_entity.begin(), i_end = m_entity.end(); i != i_end; ++i)
	{	
		if (intersect::BBoxVsBBox(*(*i)->m_bbox, *entity.m_bbox))
		{
			BPPair pair;
			pair.m_objectA = *i;
			pair.m_objectB = &entity;
			func(pair, context);
		}
	}
}

// Enumerate all overlaps with 'ray'
void BPBruteForce::EnumPairs(EnumPairsFunc func, Ray const& ray, void* context)
{
	PR_EXPAND(PR_DBG_PHYSICS, auto enumer = pr::Scope<void>(
		[&] { m_enumerating = true; },
		[&] { m_enumerating = false; }));

	for (BPEntityCont::const_iterator i = m_entity.begin(), i_end = m_entity.end(); i != i_end; ++i)
	{	
		if (intersect::LineVsBoundingBox(ray.m_point, ray.m_point + ray.m_direction, *(*i)->m_bbox))
		{
			BPPair pair;
			pair.m_objectA   = *i;
			pair.m_objB_void = &ray;
			func(pair, context);
		}
	}
}
