//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_BROADPHASE_SNP_H
#define PR_BROADPHASE_SNP_H

#include "pr/physics/types/forward.h"
#include "pr/physics/broadphase/ibroadphase.h"
#include "pr/physics/broadphase/bpentity.h"

namespace pr
{
	namespace ph
	{
		class BPSweepAndPrune :public IBroadphase
		{
			typedef pr::Array<BPEntity*, 256> BPEntityCont;
			BPEntityCont m_entity;        // Pointers to the entities in the broadphase
			int          m_axis;          // Sort axis
			bool         m_sorted;        // 'Dirty' flag for sorting the entity array
			bool         m_enumerating;   // True during a call to the EnumPairs() methods
			
			void Sort();
			
		public:
			BPSweepAndPrune();
			
			// Add/Remove a broadphase entity to the broadphase.
			// The entity should be a member of the object you want broadphase'd
			void Add(BPEntity& entity);
			void Remove(BPEntity& entity);
			void Update(BPEntity&) { m_sorted = false; }
			
			// Empty the broadphase.
			void RemoveAll();
			
			// Iterate over the colliding pairs
			void EnumPairs(EnumPairsFunc func, void* context);
			
			// Enumerate all overlaps with 'entity'
			void EnumPairs(EnumPairsFunc func, BPEntity const& entity, void* context);
			
			// Enumerate all overlaps with 'ray'
			void EnumPairs(EnumPairsFunc func, Ray const& ray, void* context);
		};
	}
}

#endif
