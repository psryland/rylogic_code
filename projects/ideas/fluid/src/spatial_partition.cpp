// Fluid
#include "src/spatial_partition.h"
#include "src/particles.h"

namespace pr::fluid
{
	SpatialPartition::SpatialPartition()
		: m_pivots()
	{
	}

	void SpatialPartition::Update(std::span<Particle> particles)
	{
		m_pivots.resize(particles.size());

		kdtree::Build<3, float, Particle>(
			particles,
			[](Particle const& p, int a) { return p.m_pos[a]; },
			[&](Particle& p, int a) { m_pivots[&p - particles.data()] = s_cast<int8_t>(a); }
		);
	}

	void SpatialPartition::Find(std::span<Particle const> particles, v4_cref position, float radius, std::function<void(Particle const&)> found) const
	{
		float const search[3] = { position.x,position.y,position.z };

		kdtree::Find<3, float, Particle>(
			particles,
			search, radius,
			[](Particle const& p, int a) { return p.m_pos[a]; },
			[&](Particle const& p) { return s_cast<int>(m_pivots[&p - particles.data()]); },
			[&](Particle const& p, float) { found(p); }
		);
	}
}
