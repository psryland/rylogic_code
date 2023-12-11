// Fluid
#pragma once
#include "src/forward.h"
#include "src/particles.h"
#include "src/spatial_partition.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		Particles m_particles;
		SpatialPartition m_spatial;
		v4 m_gravity;
		float m_restitution;

		FluidSimulation();
		int ParticleCount() const;
		void Step(float dt);
		void BoundaryCollision(Particle& particle, v4_cref next) const;

		// Calculates the fluid density at 'position'
		float DensityAt(v4_cref position) const;

		// The influence at a distance of 'distance' from a particle with radius 'radius'
		float SmoothingKernel(float radius, float distance) const;

		// The derivative of the smoothing kernel
		float dSmoothingKernel(float radius, float distance) const;
	};
}
