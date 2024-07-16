// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct ISpatialPartition
	{
		virtual ~ISpatialPartition() = default;

		// Spatially partition the particles for faster locality testing
		virtual void Update(std::span<Particle> psrticles) = 0;

		// Find all particles within 'radius' of 'position'
		virtual void Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const = 0;
	};
}
