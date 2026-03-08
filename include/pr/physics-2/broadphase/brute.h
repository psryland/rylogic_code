//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics-2/broadphase/ibroadphase.h"
#include "pr/physics-2/rigid_body/rigid_body.h"

namespace pr::physics::broadphase
{
	// A simple O(n²) broad phase implementation.
	// Tests every pair of registered bodies for bounding box overlap.
	// Suitable for small scenes (< ~100 bodies). For larger scenes,
	// consider sweep-and-prune or spatial hashing.
	class Brute : public IBroadphase
	{
		std::vector<RigidBody const*> m_entity;

	public:

		void Clear() override
		{
			m_entity.resize(0);
		}
		void Add(RigidBody const& obj) override
		{
			m_entity.push_back(&obj);
		}
		void Remove(RigidBody const& obj) override
		{
			auto at = std::find(std::begin(m_entity), std::end(m_entity), &obj);
			if (at == std::end(m_entity)) return;
			m_entity.erase(at);
		}

		// Enumerate all pairs of entities whose bounding boxes overlap
		void EnumOverlappingPairs(std::function<void(RigidBody const&, RigidBody const&)> cb) const override
		{
			for (int i = 0, iend = int(m_entity.size()); i != iend; ++i)
			{
				for (auto j = i + 1; j != iend; ++j)
				{
					auto& objA = *m_entity[i];
					auto& objB = *m_entity[j];
					auto bboxA = BBoxWS(objA);
					auto bboxB = BBoxWS(objB);
					if (!geometry::intersect::BBoxVsBBox(bboxA, bboxB)) continue;
					cb(objA, objB);
				}
			}
		}
	};
}
