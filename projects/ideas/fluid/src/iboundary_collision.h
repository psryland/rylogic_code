// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct IBoundaryCollision
	{
		virtual ~IBoundaryCollision() = default;

		// Distribute the particles within the boundary
		virtual void Fill(std::span<Particle> particles, float radius) const = 0;

		// Apply collision resolution with the container boundary
		using Dynamics = struct { v4 pos, vel; };
		virtual Dynamics ResolveCollision(Particle const& particle, float radius, float dt) const = 0;
	};
}
