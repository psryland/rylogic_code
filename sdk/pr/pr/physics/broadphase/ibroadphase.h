//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_BROADPHASE_IBROADPHASE_H
#define PR_BROADPHASE_IBROADPHASE_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		typedef void (*EnumPairsFunc)(const BPPair& pair, void* context);

		class IBroadphase
		{
		public:
			virtual ~IBroadphase() {}

			// Add/Remove a broadphase entity to/from the broadphase.
			// The entity should be a member of the object you want broadphase'd
			virtual void Add   (BPEntity& entity) = 0;
			virtual void Remove(BPEntity& entity) = 0;
			virtual void Update(BPEntity& entity) = 0;

			// Empty the broadphase.
			virtual void RemoveAll() = 0;

			// Enumerate all pairs of objects in the broadphase
			virtual void EnumPairs(EnumPairsFunc func, void* context) = 0;
			
			// Enumerate all overlaps with 'entity'
			virtual void EnumPairs(EnumPairsFunc func, BPEntity const& entity, void* context) = 0;

			// Enumerate all overlaps with 'ray'
			virtual void EnumPairs(EnumPairsFunc func, Ray const& ray, void* context) = 0;
		};
	}
}

#endif
