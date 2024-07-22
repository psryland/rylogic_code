// Fluid
#include "src/kdtree_partition.h"
#include "src/particle.h"

namespace pr::fluid
{
	KDTreePartition::KDTreePartition()
		: m_pivots()
		, m_order()
	{
	}

	// Spatially partition the particles for faster locality testing
	void KDTreePartition::Update(std::span<Particle const> particles)
	{
		m_pivots.resize(particles.size());
		m_order.resize(particles.size());
		auto i = 0; for (auto& o : m_order) o = i++;

		kdtree::Build<Dimensions, float, int, kdtree::EStrategy::AxisByLevel>(
			m_order,
			[&](int i, int a) { return particles[i].m_pos[a]; },
			[&](int i, int a) { m_pivots[i] = s_cast<int8_t>(a); }
		);
	}

	// Find all particles within 'radius' of 'position'
	void KDTreePartition::Find(v4_cref position, float radius, std::span<Particle const> particles, std::function<void(Particle const&, float)> found) const
	{
		if constexpr (Dimensions == 2)
		{
			float const search[2] = { position.x, position.y };
			kdtree::Find<2, float, int>(
				m_order,
				search, radius,
				[&](int i, int a) { return particles[i].m_pos[a]; },
				[&](int i) { return s_cast<int>(m_pivots[i]); },
				[&](int i, float dist_sq) { found(particles[i], dist_sq); }
			);
		}
		if constexpr (Dimensions == 3)
		{
			float const search[3] = { position.x, position.y, position.z };
			kdtree::Find<3, float, int>(
				m_order,
				search, radius,
				[&](int i, int a) { return particles[i].m_pos[a]; },
				[&](int i) { return s_cast<int>(m_pivots[i]); },
				[&](int i, float dist_sq) { found(particles[i], dist_sq); }
			);
		}
	}
}
