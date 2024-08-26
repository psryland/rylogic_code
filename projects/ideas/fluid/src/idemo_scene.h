// Fluid
#pragma once
#include "src/forward.h"
#include "src/probe.h"

namespace pr::fluid
{
	struct IDemoScene
	{
		virtual ~IDemoScene() = default;

		// 2D or 3D
		virtual int SpatialDimensions() const = 0;

		// Initial camera position
		virtual std::optional<pr::Camera> Camera() const = 0;

		// Returns initialisation data for the particles.
		// Return empty() if the existing particle state is to be used.
		virtual std::span<Particle const> Particles() const { return {}; }

		// Return initialization data for the static collision scene.
		virtual std::span<CollisionPrim const> Collision() const { return {}; }

		// Particle culling
		virtual ParticleCollision::CullData Culling() const { return {}; }

		// Return a string representation of the scene for visualisation
		virtual std::string LdrScene() const = 0;

		// Move the probe around
		virtual v4 PositionProbe(gui::Point, rdr12::Scene const&) const { return v4::Origin(); }

		// Handle input
		virtual void OnMouseButton(gui::MouseEventArgs&) {}
		virtual void OnMouseMove(gui::MouseEventArgs&) {}
		virtual void OnMouseWheel(gui::MouseWheelArgs&) {}
		virtual void OnKey(gui::KeyEventArgs&) {}

	};
}
