// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct IDemoScene
	{
		virtual ~IDemoScene() = default;

		// Initial camera position
		virtual std::optional<pr::Camera> Camera() const = 0;

		// Returns initialisation data for the particles.
		// Return empty() if the existing particle state is to be used.
		virtual std::span<Particle const> Particles() const { return {}; }

		// Return initialization data for the static collision scene.
		virtual std::span<CollisionPrim const> Collision() const { return {}; }

		// Return a string representation of the scene for visualisation
		virtual std::string LdrScene() const = 0;
	};
}
