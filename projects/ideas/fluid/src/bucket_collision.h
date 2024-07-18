// Fluid
#pragma once
#include "src/forward.h"
#include "src/iboundary_collision.h"

namespace pr::fluid
{
	struct BucketCollision :IBoundaryCollision
	{
		// A square well with walls at +/- 'm_hwidth' and a lid at 'm_ceiling'
		float const m_hwidth;  // Half width of the bucket
		float const m_ceiling; // The height limit
		v2 m_restitution;      // The coefficient of restitution (normal, tangent)

		BucketCollision();
		
		// Distribute the particles within the boundary
		void Fill(std::span<Particle> particles, float radius) const override;

		// Apply collision resolution with the container boundary
		Dynamics ResolveCollision(Particle const& particle, float radius, float dt) const override;
	};
}
