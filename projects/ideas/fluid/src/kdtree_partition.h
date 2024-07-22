// Fluid
#pragma once
#include "src/forward.h"
#include "src/ispatial_partition.h"

namespace pr::fluid
{
	struct KDTreePartition :ISpatialPartition
	{
		std::vector<int8_t> m_pivots;
		std::vector<int> m_order;

		KDTreePartition();

		// Spatially partition the particles for faster locality testing
		void Update(std::span<Particle const> particles) override;

		// Find all particles within 'radius' of 'position'
		void Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const override;
	};
}
