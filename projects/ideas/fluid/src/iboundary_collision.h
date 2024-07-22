// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	enum class EFillStyle
	{
		Point,
		Random,
		Lattice,
	};

	struct IBoundaryCollision
	{
		virtual ~IBoundaryCollision() = default;

		// The approximate volume (in m^3 or m^2 depending on Dimensions) occupied by the particles under normal conditions
		virtual float Volume() const = 0;

		// Distribute the particles within the boundary
		virtual void Fill(EFillStyle style, std::span<Particle> particles, float radius) const = 0;

		// Apply collision resolution with the container boundary
		struct Dynamics { v4 pos, vel; };
		virtual Dynamics ResolveCollision(Particle const& particle, float radius, float dt) const = 0;
	};
}
