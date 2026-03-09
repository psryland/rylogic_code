//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#include "pr/physics-2/collision/broadphase_brute.h"
#include "pr/physics-2/rigid_body/rigid_body.h"

namespace pr::physics::broadphase
{
	Brute::Brute()
		: m_entity()
	{
	}

	// Remove all registered bodies
	void Brute::Clear()
	{
		m_entity.resize(0);
	}

	// Register a body for overlap testing
	void Brute::Add(RigidBody const& obj)
	{
		m_entity.push_back(&obj);
	}

	// Unregister a body
	void Brute::Remove(RigidBody const& obj)
	{
		auto at = std::find(std::begin(m_entity), std::end(m_entity), &obj);
		if (at == std::end(m_entity)) return;
		m_entity.erase(at);
	}

	// Enumerate all pairs of entities whose bounding boxes overlap
	void Brute::EnumOverlappingPairs(std::function<void(RigidBody const&, RigidBody const&)> cb) const
	{
		for (int i = 0, iend = int(m_entity.size()); i != iend; ++i)
		{
			for (auto j = i + 1; j != iend; ++j)
			{
				auto& objA = *m_entity[i];
				auto& objB = *m_entity[j];
				auto bboxA = objA.BBoxWS();
				auto bboxB = objB.BBoxWS();
				if (!geometry::intersect::BBoxVsBBox(bboxA, bboxB)) continue;
				cb(objA, objB);
			}
		}
	}
}
