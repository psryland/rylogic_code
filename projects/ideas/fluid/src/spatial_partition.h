// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct SpatialPartition
	{
		std::vector<int8_t> m_pivots;

		SpatialPartition();

		// Update the spatial partitioning of the particles
		void Update(std::span<Particle> particles);

		// Find all particles within 'radius' of 'position'
		void Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const;
	};
}
