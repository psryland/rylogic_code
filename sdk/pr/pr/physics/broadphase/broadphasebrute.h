//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_BROADPHASE_BRUTE_FORCE_H
#define PR_BROADPHASE_BRUTE_FORCE_H

#include "pr/physics/types/forward.h"
#include "pr/physics/broadphase/ibroadphase.h"
#include "pr/physics/broadphase/bpentity.h"

namespace pr
{
	namespace ph
	{
		class BPBruteForce :public IBroadphase
		{
			typedef std::vector<BPEntity*> BPEntityCont;
			BPEntityCont m_entity;
			bool m_enumerating;

		public:
			BPBruteForce();

			// Add/Remove a broadphase entity to the broadphase.
			// The entity should be a member of the object you want broadphase'd
			void Add   (BPEntity& entity);
			void Remove(BPEntity& entity);
			void Update(BPEntity&) {}

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
