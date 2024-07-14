// Fluid
#include "src/spatial_partition.h"
#include "src/particle.h"

namespace pr::fluid
{
	SpatialPartition::SpatialPartition()
		: m_pivots()
	{
	}

	void SpatialPartition::Update(std::span<Particle> particles)
	{
		m_pivots.resize(particles.size());

		kdtree::Build<Dimensions, float, Particle, kdtree::EStrategy::AxisByLevel>(
			particles,
			[](Particle const& p, int a) { return p.m_pos[a]; },
			[&](Particle& p, int a) { m_pivots[&p - particles.data()] = s_cast<int8_t>(a); }
		);
	}

	void SpatialPartition::Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const
	{
		if constexpr (Dimensions == 2)
		{
			float const search[2] = { position.x, position.y };
			kdtree::Find<2, float, Particle>(
				particles,
				search, radius,
				[](Particle const& p, int a) { return p.m_pos[a]; },
				[&](Particle const& p) { return s_cast<int>(m_pivots[&p - particles.data()]); },
				[&](Particle const& p, float dist_sq) { found(p, dist_sq); }
			);
		}
		if constexpr (Dimensions == 3)
		{
			float const search[3] = { position.x, position.y, position.z };
			kdtree::Find<3, float, Particle>(
				particles,
				search, radius,
				[](Particle const& p, int a) { return p.m_pos[a]; },
				[&](Particle const& p) { return s_cast<int>(m_pivots[&p - particles.data()]); },
				[&](Particle const& p, float dist_sq) { found(p, dist_sq); }
			);
		}
	}
}
