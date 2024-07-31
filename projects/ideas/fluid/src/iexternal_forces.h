// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct IExternalForces
	{
		virtual ~IExternalForces() = default;

		// Returns the acceleration Apply external forces to the particles
		virtual v4 ForceAt(FluidSimulation& sim, v4_cref position, std::optional<size_t> index) const = 0;
	};
}
