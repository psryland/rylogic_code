//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"

namespace pr::physics::broadphase
{
	// A simple O(n²) broad phase implementation
	template <typename TObject>
	class Brute
	{
		std::vector<TObject const*> m_entity;

	public:
		void Clear()
		{
			m_entity.resize(0);
		}
		void Add(TObject const& obj)
		{
			m_entity.push_back(&obj);
		}
		void Remove(TObject const& obj)
		{
			auto at = std::find(std::begin(m_entity), std::end(m_entity), &obj);
			if (at == std::end(m_entity)) return;
			m_entity.erase(at);
		}

		// Enumerate all pairs of entities that are overlapping
		template <typename TEnumFuncCB>
		void EnumOverlappingPairs(TEnumFuncCB pairs_cb, void* ctx = nullptr) const
		{
			for (int i = 0, iend = int(m_entity.size()); i != iend; ++i)
			{
				for (auto j = i + 1; j != iend; ++j)
				{
					auto& objA = *m_entity[i];
					auto& objB = *m_entity[j];
					auto bboxA = BBoxWS(objA);
					auto bboxB = BBoxWS(objB);
					if (!Intersect_BBoxToBBox(bboxA, bboxB)) continue;
					pairs_cb(ctx, objA, objB);
				}
			}
		}
	};
}
