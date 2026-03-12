//*********************************************
// Physics Sandbox — GPU Sort-and-Sweep Broadphase
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#include "src/collision/gpu_sort_and_sweep.h"
#include "pr/physics-2/rigid_body/rigid_body.h"

namespace pr::physics
{
	GpuSortAndSweep::GpuSortAndSweep(Gpu& gpu)
		: m_job(gpu, "BroadphaseSort", 0xFF40FF80, 2)
		, m_sorter(gpu.m_gpu)
		, m_keys()
		, m_payloads()
		, m_bboxes()
		, m_entity()
	{}

	// Remove all registered bodies
	void GpuSortAndSweep::Clear()
	{
		m_entity.resize(0);
	}

	// Register a body for overlap testing
	void GpuSortAndSweep::Add(pr::physics::RigidBody const& obj)
	{
		m_entity.push_back(&obj);
	}

	// Unregister a body
	void GpuSortAndSweep::Remove(pr::physics::RigidBody const& obj)
	{
		auto at = std::find(std::begin(m_entity), std::end(m_entity), &obj);
		if (at == std::end(m_entity)) return;
		m_entity.erase(at);
	}

	// Enumerate all pairs of bodies whose bounding boxes overlap.
	// Uses GPU radix sort for the primary axis, then CPU sweep with Y/Z filtering.
	void GpuSortAndSweep::EnumOverlappingPairs(std::function<void(pr::physics::RigidBody const&, pr::physics::RigidBody const&)> cb) const
	{
		auto n = static_cast<int>(m_entity.size());
		if (n < 2) return;

		// Step 1: Compute world-space AABBs and choose the primary sort axis.
		// The axis with the largest position variance gives the best separation.
		m_bboxes.resize(n);
		auto var_sum = pr::v4::Zero();
		auto var_sq = pr::v4::Zero();
		for (int i = 0; i != n; ++i)
		{
			m_bboxes[i] = m_entity[i]->BBoxWS();
			auto c = m_bboxes[i].Centre();
			var_sum += c;
			var_sq += pr::v4(c.x * c.x, c.y * c.y, c.z * c.z, 0);
		}

		// Variance = E[x²] - E[x]² per axis
		auto inv_n = 1.0f / n;
		auto mean = var_sum * inv_n;
		auto variance = var_sq * inv_n - pr::v4(mean.x * mean.x, mean.y * mean.y, mean.z * mean.z, 0);

		// Choose axis with maximum variance
		int axis = 0;
		if (variance.y > variance.x) axis = 1;
		if (variance.z > variance[axis]) axis = 2;

		// Step 2: Pack AABB endpoints for the chosen axis.
		// Each body produces two entries: a "begin" (min) and "end" (max).
		// Also precompute the secondary axis bounds for the CPU sweep inner loop.
		auto endpoint_count = n * 2;
		m_keys.resize(endpoint_count);
		m_payloads.resize(endpoint_count);
		m_axis_bounds.resize(n);

		int axis_y = (axis + 1) % 3;
		int axis_z = (axis + 2) % 3;

		for (int i = 0; i != n; ++i)
		{
			auto& bb = m_bboxes[i];
			auto lo = bb.Centre()[axis] - bb.Radius()[axis];
			auto hi = bb.Centre()[axis] + bb.Radius()[axis];

			m_keys[i * 2 + 0] = lo;
			m_keys[i * 2 + 1] = hi;
			m_payloads[i * 2 + 0] = static_cast<uint32_t>(i << 1) | 0; // begin
			m_payloads[i * 2 + 1] = static_cast<uint32_t>(i << 1) | 1; // end

			// Precompute secondary axis bounds to avoid repeated Centre/Radius lookups in sweep
			m_axis_bounds[i] = {
				bb.Centre()[axis_y] - bb.Radius()[axis_y],
				bb.Centre()[axis_y] + bb.Radius()[axis_y],
				bb.Centre()[axis_z] - bb.Radius()[axis_z],
				bb.Centre()[axis_z] + bb.Radius()[axis_z],
			};
		}

		// Step 3: GPU radix sort by primary axis coordinate.
		{
			m_sorter.Resize(m_job.m_cmd_list, endpoint_count);
			m_sorter.Sort(m_keys, m_payloads, m_job);
		}

		// Step 4: CPU sweep with full 3-axis AABB filtering.
		// Walk the sorted endpoints. "Begin" markers add to the active set,
		// "end" markers remove. For each new "begin", test Y and Z overlap
		// against all currently active bodies.
		auto active = std::vector<int>();
		active.reserve(n);

		for (int i = 0; i != endpoint_count; ++i)
		{
			auto payload = m_payloads[i];
			auto body_idx = static_cast<int>(payload >> 1);
			auto is_end = (payload & 1) != 0;

			if (is_end)
			{
				// Remove from active set (swap-with-last for O(1))
				auto at = std::find(active.begin(), active.end(), body_idx);
				if (at != active.end())
				{
					*at = active.back();
					active.pop_back();
				}
			}
			else
			{
				// "Begin" — test against all active bodies on the remaining two axes
				auto const& new_ab = m_axis_bounds[body_idx];

				for (auto active_idx : active)
				{
					auto const& act_ab = m_axis_bounds[active_idx];

					// Y-axis overlap test
					if (new_ab.lo_y > act_ab.hi_y || new_ab.hi_y < act_ab.lo_y)
						continue;

					// Z-axis overlap test
					if (new_ab.lo_z > act_ab.hi_z || new_ab.hi_z < act_ab.lo_z)
						continue;

					// Full 3-axis AABB overlap — emit pair
					cb(*m_entity[active_idx], *m_entity[body_idx]);
				}

				active.push_back(body_idx);
			}
		}
	}

	// Custom deleter implementation (GpuIntegrator is complete here)
	void Deleter<GpuSortAndSweep>::operator()(GpuSortAndSweep* p) const
	{
		delete p;
	}
}
