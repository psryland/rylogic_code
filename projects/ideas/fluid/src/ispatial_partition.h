// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct ISpatialPartition
	{
		using FoundCB = StaticCB<void, Particle const&, float>;

		virtual ~ISpatialPartition() = default;

		// Spatially partition the particles for faster locality testing
		virtual void Update(rdr12::ComputeJob& job, int64_t count, D3DPtr<ID3D12Resource> positions, bool readback) = 0;


		//// Find all particles within 'radius' of 'position'
		//virtual void Find(v4_cref position, float radius, std::span<Particle const> particles, FoundCB found) const = 0;
	};
}
