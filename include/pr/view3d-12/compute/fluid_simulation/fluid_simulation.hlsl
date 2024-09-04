//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "../common/geometry.hlsli"
#include "../common/utility.hlsli"
#include "../particle_collision/particle.hlsli"
#include "../particle_collision/collision.hlsli"

#ifndef THREAD_GROUP_SIZE
#define THREAD_GROUP_SIZE 1024U
#endif
static const uint ThreadGroupSize = THREAD_GROUP_SIZE;

// Valid values are 4096, 7936
#ifndef TOTAL_SHARED_MEM
#define TOTAL_SHARED_MEM 7936U
#endif
static const uint TotalSharedMemory = TOTAL_SHARED_MEM;

// ** Note ** : alignment matters. float4 must be aligned to 16 bytes. [arrays] get aligned to 16 bytes

// Colour spectrum
struct ColourData
{
	float4 Spectrum[4];     // The colour scale to use
	float2 Range;           // Scales [0,1] to the colour range
};

// Constant buffers
cbuffer cbFluidSim : register(b0)
{
	struct
	{
		float4 Gravity;         // The acceleration due to gravity

		int Dimensions;         // 2D or 3D simulation
		int NumParticles;       // The number of particles
		int CellCount;          // The number of grid cells in the spatial partition
		float GridScale;        // The scale factor for the spatial partition grid

		float ParticleRadius;   // The radius of influence for each particle
		float TimeStep;         // Leap-frog time step
		float ThermalDiffusion; // The thermal diffusion rate
		int RandomSeed;         // Seed value for the RNG

		float ForceScale;       // The force scaling factor
		float ForceRange;       // Controls the range between particles
		float ForceBalance;     // The position of the transition from repulsive to attractive forces
		float ForceDip;         // The depth of the attractive force

		float Viscosity;        // The viscosity scaler
		float3 pad;
	} Sim;
};
cbuffer cbProbeData : register(b0)
{
	struct
	{
		float4 Position;      // The position of the probe
		float Radius;         // The radius of the probe
		float Force;          // The force that the probe applies
		int NumParticles;     // The number of particles in the 'm_particles' buffer
	} Probe;
};
cbuffer cbCullData : register(b0)
{
	struct
	{
		int NumParticles;       // The number of particles
		int CellCount;          // The number of grid cells in the spatial partition
	} Cull;
};
cbuffer cbColourData : register(b0)
{
	struct
	{
		ColourData Colours;     // Colouring data
		int NumParticles;       // The number of particles in the 'm_particles' buffer
		int Scheme;             // 0 = None, 1 = Velocity, 2 = Accel, 3 = Density
	} Col;
};
cbuffer cbMapData : register(b0)
{
	struct
	{
		float4x4 MapToWorld;   // Transform from map space to world space (including scale)
		ColourData Colours;    // Colouring data
		int2 TexDim;           // The dimensions of the map texture

		int Type;              // 0 = Pressure
		int Dimensions;        // 2D or 3D simulation
		int CellCount;         // The number of grid cells in the spatial partition
		float GridScale;       // The scale factor for the spatial partition grid
		
		float ForceScale;      // The force scaling factor
		float ForceRange;       // Controls the range between particles
		float ForceBalance;     // The position of the transition from repulsive to attractive forces
		float ForceDip;         // The depth of the attractive force

		float ParticleRadius;  // The particle radius
		float3 pad;
	} Map;
};

static const uint ColourScheme_None = 0;
static const uint ColourScheme_Velocity = 1;
static const uint ColourScheme_Accel = 2;
static const uint ColourScheme_Density = 3;
static const uint ColourScheme_MASK = 0xFF;

static const uint MapType_None = 0;
static const uint MapType_Velocity = 1;
static const uint MapType_Accel = 2;
static const uint MapType_Density = 3;

// The positions of each particle
RWStructuredBuffer<PositionType> m_positions : register(u0);

// The dynamics for each particle
RWStructuredBuffer<DynamicsType> m_dynamics : register(u1);

// The indices of particle positions sorted spatially
RWStructuredBuffer<uint> m_spatial : register(u2);

// The lowest index (in m_spatial) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start : register(u3);

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count : register(u4);

// A buffer for passing back results
RWStructuredBuffer<uint> m_output : register(u5);

// A texture for writing the density map to
RWTexture2D<float4> m_tex_map : register(u6);

// General purpose group shared memory
//groupshared uint gs_memory[TotalSharedMemory];
static const int MaxNeighboursParticle = 50;
struct Neighbour
{
	float4 pos;
	float4 vel;
};
groupshared float4 gs_neighbours[ThreadGroupSize][MaxNeighboursParticle];


#include "../spatial_partition/spatial_partition.hlsli"

// Return a particle from the particle and dynamics buffers
inline Particle GetParticle(int i)
{
	Particle part;
	part.pos = float4(m_positions[i].pos.xyz, 1);
	part.vel = float4(m_dynamics[i].vel.xyz, 0);
	part.acc = float4(m_dynamics[i].accel.xyz, 0);
	part.colour = m_positions[i].col;
	part.surface = float4(m_dynamics[i].surface.xyz, 0);
	part.density = m_dynamics[i].density;
	part.flags = m_dynamics[i].flags;
	return part;
}

// A random direction vector (not normalised) in 2 or 3 dimensions with components in (-1,+1)
inline float4 Random3WithDim(float2 seed, uniform int spatial_dimensions)
{
	return select(spatial_dimensions == 3, Random3N(seed), float4(Random2N(seed), 0, 0));
}

// Return a volume radius for a neighbour search
inline float4 SearchVolume(float radius, uniform int spatial_dimensions)
{
	return float4(radius, radius, select(spatial_dimensions == 3, radius, 0), 0);
}

// Return the colour interpolated on the colour scale
inline float4 LerpColour(float value, uniform ColourData col)
{
	float scale = saturate((value - col.Range.x) / (col.Range.y - col.Range.x));
	if (scale <= 0.0f)  return col.Spectrum[0];
	if (scale < 0.333f) return lerp(col.Spectrum[0], col.Spectrum[1], (scale - 0.000f) / 0.333f);
	if (scale < 0.666f) return lerp(col.Spectrum[1], col.Spectrum[2], (scale - 0.333f) / 0.333f);
	if (scale < 1.0f)   return lerp(col.Spectrum[2], col.Spectrum[3], (scale - 0.666f) / 0.333f);
	return col.Spectrum[3];
}

// The force at normalised 'distance' from a particle. I.e. Force as a function of distance.
inline float ForceKernel(float normalised_distance, uniform float range, uniform float balance, uniform float dip)
{
	// 'range' scales the repulsive force effectively making it act at greater range. [0,inf) (typically 1.0)
	// 'balance' is the position of the switch from the repulsive to the attractive force. [0,1] (typically ~0.8)
	// 'dip' is the depth of the attractive force [0,inf) (typically ~0.1
	// The function is a piecewise function created from:
	//  Repulsive force: (range * balance / x) * sqr(1 - x/balance), for x in [0,balance]
	//  Attractive force: -dip * sqr(sqr(Cx - C + 1) - 1), where C = 2 / (1 - balance), for x in [balance,1]
	float x = clamp(normalised_distance, 0.001f, 1.0f);
	float C = 2.0f / (1.0f - balance);
	return select(x <= range,
		+(range * balance / x) * sqr(1.0f - x / range),
		-(dip) * sqr(sqr(C * x - C + 1) - 1));
}
inline float dForceKernal(float normalised_distance, uniform float range, uniform float balance, uniform float dip)
{
	// The slope of the Repulsive force is: (range/balance) - range * balance / sqr(x)
	// The slope of the attractive force is: -4 * dip * C * (C * x - C + 1) * (sqr(C * x - C + 1) - 1), where C = 2 / (1 - balance)
	float x = clamp(normalised_distance, 0.0001f, 1.0f);
	float C = 2.0f / (1.0f - balance);
	return select(x <= range,
		(range / balance) - (range * balance / sqr(x)),
		-4.0f * dip * C * (C * x - C + 1) * (sqr(C * x - C + 1) - 1));
}

// The influence at normalised 'distance' from a particle
inline float ViscosityKernel(float normalised_distance, uniform float viscosity_power)
{
	// -pow(x, viscosity_power) + 1
	float x = clamp(normalised_distance, 0.0f, 1.0f);
	float viscosity = -pow(x, sqr(viscosity_power)) + 1.0f;
	return select(viscosity_power != 0, viscosity, 0.0f);
}

// Contribution to density based on distance
inline float DensityKernel(float normalised_distance, uniform int spatial_dimensions, uniform float particle_radius)
{
	// The calculated density needs to be independent of particle radius.
	// Conceptually, we divide the sum of particle densities within the kernel
	// by the volume under the kernel.
	const float tau = 6.28318530717958647693f;
	float volume = 1.0f;// select(spatial_dimensions == 2,
		//(1.0f/6.0f) * tau * sqr(particle_radius),
		//(1.0f/6.0f) * tau * cube(particle_radius));

	float x = clamp(normalised_distance, 0.0f, 1.0f);
	return (1.0f - x) / volume;
}

// True if 'particle' is a dead particle
inline bool IsDeadParticle(PositionType position)
{
	return any(isnan(position.pos));
}

// Calculates the acceleration experienced at 'target' as a result of the interaction between 'target' and 'neighbour'
inline float4 ParticleInteraction(uniform int idx, Particle target, Particle neighbour, float time_step)
{
	// Integration stability is an issue because the force kernel is asymptotic at x = 0.
	// Concepts:
	//  - If we had infinitesimal 'dt' steps, the acceleration would fall sharply as the particles moved apart.
	//  - If we do the calculation relative to one of the particles, we only need to do half the calculations.
	//  - For each change in distance between the particles, the relative velocity changes by the area under the
	//    ForceKernel curve, evaluated from dist0 to dist1 (direction is centre to centre).
	//  - We can stop when the particle velocity is moving away and the range is greater than the particle radius.
	//
	// Constrain the step size based on distance travelled, whichever is smaller min(vel * dt, ParticleRadius / N).
	// For each step, we want to integrate the ForceKernel based on the distance between the particles.
	
	const float SubSteps = 10.0f;
	const float MaxStepLength = Sim.ParticleRadius / SubSteps;
	const float MaxStepTime = time_step / SubSteps;
	
	// Get the relative position and velocity. Treat 'target' as stationary.
	float4 pos0 = neighbour.pos - target.pos;
	float4 vel0 = neighbour.vel - target.vel;
	float4 pos = pos0;
	float4 vel = vel0;
	float time_remaining = time_step;

	// Integrate the force kernel over the step
	for (int max_steps = 100; max_steps-- != 0 && time_remaining != 0;)
	{
		// If the particle is moving away and out of range, then there is no more influence from it
		float dist_sq = length_sq(pos);
		if (dot(pos, vel) >= 0 && dist_sq >= sqr(Sim.ParticleRadius))
			break;

		// Determine a step time / size
		float dt = min(MaxStepTime, time_remaining);
		dt = select(length_sq(vel * dt) > sqr(MaxStepLength), MaxStepLength / length(vel), dt);

		// Integrate the force kernel over the step
		{
			float4 hstep = pos + 0.5f * vel * dt;
			float hstep_dist = length(hstep);
			float force = ForceKernel(hstep_dist / Sim.ParticleRadius, Sim.ForceRange, Sim.ForceBalance, Sim.ForceDip) * Sim.ForceScale;
			float4 direction = select(hstep_dist != 0, hstep / hstep_dist, Random3WithDim(float2(idx, Sim.RandomSeed), Sim.Dimensions));
			vel += force * /*target.density * neighbour.density **/ direction;
		}

		// Advance the particles by the step
		pos += vel * dt;
		time_remaining -= dt;
	}

	// 'vel' is the expected final relative velocity of 'neighbour'. 'vel - vel0' is an acceleration that gives this result.
	// The acceleration is shared between both particles, and we're returning the acceleration for 'target'.
	return -0.5f * (vel - vel0);
}

// Update the 'density' property of each particle
[numthreads(ThreadGroupSize, 1, 1)]
void MeasureDensity(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Sim.NumParticles)
		return;

	Particle target = GetParticle(dtid.x);
	
	// Sample the environment for neighbours at the predicted particle position at TimeStep/2
	float4 target_pos = target.pos + 0.5f * Sim.TimeStep * target.vel;
	
	// Weighted particle density
	float density = 1.0f;
	
	// Search the neighbours of 'target'
	FindIter find = Find(target_pos, SearchVolume(Sim.ParticleRadius, Sim.Dimensions), Sim.GridScale, Sim.CellCount);
	while (DoFind(find, Sim.CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			// Self already accounted for
			if (m_spatial[i] == dtid.x)
				continue;

			Particle neighbour = GetParticle(m_spatial[i]);
			float4 neighbour_pos = neighbour.pos + 0.5f * Sim.TimeStep * neighbour.vel;
			float normalised_distance = length(neighbour_pos - target_pos) / Sim.ParticleRadius;
			density += DensityKernel(normalised_distance, Sim.Dimensions, Sim.ParticleRadius);
		}
	}
	
	m_dynamics[dtid.x].density = density;
}

// Calculates forces at each particle position
[numthreads(ThreadGroupSize, 1, 1)]
void ApplyForces(uint3 dtid : SV_DispatchThreadID, uint3 gtid : SV_GroupThreadID)
{
	if (dtid.x >= Sim.NumParticles)
		return;

	Particle target = GetParticle(dtid.x);
	
	// Sample the environment for neighbours at the predicted particle position at TimeStep/2
	float4 target_pos = target.pos + 0.5f * Sim.TimeStep * target.vel;
	
	// Apply gravity and thermal diffusion
	float4 nett_accel = float4(0, 0, 0, 0);
	nett_accel += Sim.Gravity;
	nett_accel += Random3WithDim(float2(dtid.x, Sim.RandomSeed), Sim.Dimensions) * Sim.ThermalDiffusion;

	// The weighted group velocity
	float4 group_velocity = target.vel;
	float group_velocity_weight = 1;

	// Get the range of shared memory available to this thread.
	
	//gs_neighbours[gtid.x]
	
	
	// Search the neighbours of 'target'
	FindIter find = Find(target_pos, SearchVolume(Sim.ParticleRadius, Sim.Dimensions), Sim.GridScale, Sim.CellCount);
	while (DoFind(find, Sim.CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			// No self-interaction
			if (m_spatial[i] == dtid.x)
				continue;

			Particle neighbour = GetParticle(m_spatial[i]);
			float4 neighbour_pos = neighbour.pos + 0.5f * Sim.TimeStep * neighbour.vel;
			float normalised_distance = length(neighbour_pos - target_pos) / Sim.ParticleRadius;

			// Viscosity
			{
				// Viscosity is inter-particle friction. When particles are close, their velocities should
				// tend toward the average of the group. Find the group average velocity (weighted by distance)
				float visocity = ViscosityKernel(normalised_distance, Sim.Viscosity);
				group_velocity += visocity * neighbour.vel;
				group_velocity_weight += visocity;
			}
			
			// Acceleration
			nett_accel += ParticleInteraction(dtid.x, target, neighbour, Sim.TimeStep);
		}
	}

	// Apply viscosity. Add an acceleration that makes the particles velocity closer to the group average
	{
		float4 target_vel = target.vel + 0.5f * nett_accel * Sim.TimeStep;
		group_velocity *= 1.0f / group_velocity_weight;
		nett_accel += (group_velocity - target_vel) / Sim.TimeStep;
	}


	//// If the particle is on a boundary, don't allow acceleration into the boundary
	//nett_accel -= dot(nett_accel, target.surface) * target.surface;
	
	// Record the nett force
	m_dynamics[dtid.x].accel.xyz += nett_accel.xyz;
}

// Apply an attractor to the particles
[numthreads(ThreadGroupSize, 1, 1)]
void ApplyProbe(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Probe.NumParticles)
		return;

	Particle target = GetParticle(dtid.x);

	// If the particle is within the probe radius apply a force toward the centre.
	// Scale by inverse distance so that the centre of the probe isn't high pressure.
	float4 r = target.pos - Probe.Position;
	float dist_sq = dot(r, r);
	if (dist_sq < sqr(Probe.Radius))
	{
		float dist = sqrt(dist_sq);
		float influence = saturate(dist / Probe.Radius); // 1 at range, 0 in the centre
		float4 direction = dist > 0.0001f ? r / dist : float4(0, 0, 0, 0);
		float4 force = influence * (Probe.Force * direction - target.vel);
		m_dynamics[dtid.x].accel += force.xyz; // Probe ignores mass
	}
}

// Remove NaN particles
[numthreads(ThreadGroupSize, 1, 1)]
void CullParticles(uint3 dtid : SV_DispatchThreadID)
{
	// Expect the spatial partitioning to have sorted the NaN particles to the end.
	// Rules:
	//  - If the src particle is NaN => done
	//  - If the src particle is not NaN
	//    -> If the dst index is output the dead range => Replace => done
	//    -> The dst index is in the dead range, look up the next dst index
	//       Repeat until outside the dead range.

	int src_idx = dtid.x;
	int dst_idx = m_spatial[dtid.x];
	int2 dead_range = IndexRange(Cull.CellCount - 1);

	// Return the new length of the particle buffer in 'm_output'
	if (dtid.x == 0)
		m_output[0] = Cull.NumParticles - (dead_range.y - dead_range.x);
	
	// If we're not in the dead range, nothing to do.
	if (src_idx < dead_range.x || src_idx >= dead_range.y)
		return;
	
	// If the source particle is dead then there is a dead particle in the dead range already. We're done
	if (IsDeadParticle(m_positions[src_idx]))
		return;

	// While 'dst_idx' is within the dead range, look up the next 'dst_idx'
	for (; dst_idx >= dead_range.x && dst_idx < dead_range.y; )
		dst_idx = m_spatial[dst_idx];
	
	// If the destination index is not within the dead range then replace and we're done
	if (dst_idx < dead_range.x || dst_idx >= dead_range.y)
	{
		m_positions[dst_idx] = m_positions[src_idx];
		m_dynamics[dst_idx] = m_dynamics[src_idx];
	}
}

// Apply colours to the particles
[numthreads(ThreadGroupSize, 1, 1)]
void ColourParticles(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Col.NumParticles)
		return;

	Particle target = GetParticle(dtid.x);
	
	int scheme = Col.Scheme & ColourScheme_MASK;
	m_positions[dtid.x].col = Col.Colours.Spectrum[0];

	// Colour the particles based on their velocity
	if (scheme == ColourScheme_Velocity)
	{
		float speed = length(target.vel);
		m_positions[dtid.x].col = LerpColour(speed, Col.Colours);
	}
	
	// Colour the particles based on their density
	if (scheme == ColourScheme_Accel)
	{
		float accel = length(target.acc);
		m_positions[dtid.x].col = LerpColour(accel, Col.Colours);
	}

	// Colour particles within the probe radius
	if (scheme == ColourScheme_Density)
	{
		float density = length(target.density);
		m_positions[dtid.x].col = LerpColour(density, Col.Colours);
	}
}

// Populate a texture with each pixel representing the property at that point
[numthreads(32, 32, 1)]
void GenerateMap(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x > Map.TexDim.x || dtid.y > Map.TexDim.y)
		return;
	
	// The position in world space of the texture coordinate
	float4 pos = mul(Map.MapToWorld, float4(dtid.x, dtid.y, 0, 1));
	
	float4 value = float4(0,0,0,0);

	// Search the neighbours of 'pos'
	FindIter find = Find(pos, SearchVolume(Map.ParticleRadius, Map.Dimensions), Map.GridScale, Map.CellCount);
	while (DoFind(find, Map.CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			Particle neighbour = GetParticle(m_spatial[i]);

			float4 direction = pos - neighbour.pos;
			float distance_sq = dot(direction, direction);
			if (distance_sq >= sqr(Map.ParticleRadius))
				continue;

			// Get the normalised direction vector to 'neighbour'
			float distance = sqrt(distance_sq);
			direction = distance > 0.0001f ? direction / distance : normalize(Random3WithDim(float2(dtid.x, 0), Map.Dimensions));

			if (Map.Type == MapType_Velocity)
			{
				// Add up the weighted group velocity
				value += lerp(neighbour.vel, float4(0,0,0,0), distance / Map.ParticleRadius) * direction;
			}
			if (Map.Type == MapType_Accel)
			{
				// Add up the nett force
				float force = ForceKernel(distance / Map.ParticleRadius, Map.ForceRange, Map.ForceBalance, Map.ForceDip);
				value += (Map.ForceScale * force) * direction;
			}
			if (Map.Type == MapType_Density)
			{
				// Add up the density
				value += DensityKernel(distance / Map.ParticleRadius, Map.Dimensions, Map.ParticleRadius);
			}
		}
	}
	
	// Write the density to the map
	m_tex_map[uint2(dtid.x, dtid.y)] = LerpColour(length(value), Map.Colours);
}







			#if 0 // Good version
			{
				// Acceleration integration sub-step 
				const int sub_steps = 5;
				const float sub_step = Sim.TimeStep / sub_steps;

				float4 t_pos = target.pos;
				float4 n_pos = neighbour.pos;
				float4 t_vel = target.vel;
				float4 n_vel = neighbour.vel;

				// Calculate sub-steps for a more accurate acceleration calculation
				[unroll] for (int k = 0; k != sub_steps; ++k)
				{
					// Advance the particles by a half step
					float4 t_pos_half_step = t_pos + 0.5f * sub_step * t_vel;
					float4 n_pos_half_step = n_pos + 0.5f * sub_step * n_vel;

					// Get the separating vector and distance
					float4 sep = t_pos_half_step - n_pos_half_step;
					float distance = length(sep);
					float4 direction = select(distance != 0, sep / distance, Random3WithDim(float2(dtid.x, Sim.RandomSeed), Sim.Dimensions)); // points at target

					// Calculate the acceleration due to particle forces
					float force = ForceKernel(distance / Sim.ParticleRadius, Sim.ForceRange, Sim.ForceBalance, Sim.ForceDip);
					float accel = Sim.ForceScale * target.density * neighbour.density * force;

					// TODO Accel based sub step size
					
					// If a particle is on the boundary, then it can't be accelerated into the boundary surface.
					// Instead the acceleration is reflected
					float4 t_accel = +accel * direction;
					float4 n_accel = -accel * direction;

					t_accel -= max(0, 2 * dot(t_accel, target.surface)) * target.surface;
					n_accel -= max(0, 2 * dot(n_accel, neighbour.surface)) * neighbour.surface;
					
					// Update the particle velocities by the acceleration
					t_vel += sub_step * t_accel;
					n_vel += sub_step * n_accel;

					// Advance the particles by a sub step
					t_pos += t_vel * sub_step;
					n_pos += n_vel * sub_step;
				}
			
				// 't_pos' and 't_vel' is where the particle should end up, calculate an acceleration that gives this result
				nett_accel += t_vel - target.vel;
			}
			#endif