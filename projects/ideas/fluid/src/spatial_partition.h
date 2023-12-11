// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct SpatialPartition
	{
		std::vector<int8_t> m_pivots;

		SpatialPartition();
		void Update(std::span<Particle> particles);
		void Find(std::span<Particle const> particles, v4_cref position, float radius, std::function<void(Particle const&)> found) const;
	};
}
