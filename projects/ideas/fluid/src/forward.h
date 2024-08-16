// Fluid
#pragma once

#include <cstdint>
#include <span>
#include <vector>
#include <unordered_set>
#include <stdexcept>
#include <windows.h>

#include "pr/common/cast.h"
#include "pr/common/fmt.h"
#include "pr/common/coalesce.h"
#include "pr/common/tweakables.h"
#include "pr/common/static_callback.h"
#include "pr/common/resource.h"
#include "pr/maths/bit_fields.h"
#include "pr/maths/maths.h"
#include "pr/maths/conversion.h"
#include "pr/container/vector.h"
#include "pr/container/kdtree.h"
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"

#include "pr/view3d-12/view3d.h"
#include "pr/view3d-12/ldraw/ldr_object.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/gpu_radix_sort.h"
#include "pr/view3d-12/compute/spatial_partition.h"
#include "pr/view3d-12/compute/particle_collision.h"
#include "pr/view3d-12/compute/fluid_simulation.h"

namespace pr::fluid
{
	using namespace tweakables;
	inline static const int Dimensions = 2;

	struct FluidVisualisation;

	using IndexSet = std::unordered_set<int64_t>;

	using ComputeStep = rdr12::ComputeStep;
	using FluidSimulation = rdr12::compute::fluid::FluidSimulation<Dimensions>;
	using SpatialPartition = rdr12::compute::spatial_partition::SpatialPartition;
	using ParticleCollision = rdr12::compute::particle_collision::ParticleCollision;
	using CollisionBuilder = rdr12::compute::particle_collision::CollisionBuilder;
	using CollisionPrim = rdr12::compute::particle_collision::Prim;
	using Particle = rdr12::compute::fluid::Particle;
	using GpuJob = FluidSimulation::GpuJob;
}

