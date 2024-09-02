//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "../common/geometry.hlsli"
#include "../common/utility.hlsli"
#include "../particle_collision/particle.hlsli"

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
		float ForceAmplitude;   // The magnitude of the force profile at X = 0 
		float ForceBalance;     // The X value of the Y = 0 intercept in the force profile
		float Viscosity;        // The viscosity scaler
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
		float4x4 MapToWorld;  // Transform from map space to world space (including scale)
		ColourData Colours;     // Colouring data
		int2 TexDim;          // The dimensions of the map texture

		int Type;             // 0 = Pressure
		int Dimensions;         // 2D or 3D simulation
		int CellCount;        // The number of grid cells in the spatial partition
		float GridScale;      // The scale factor for the spatial partition grid
		
		float ParticleRadius; // The particle radius
		float ForceScale;     // The force scaling factor
		float ForceAmplitude;   // The magnitude of the force profile at X = 0 
		float ForceBalance;     // The X value of the Y = 0 intercept in the force profile
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
groupshared uint gs_memory[TotalSharedMemory];

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

// The influence at normalised 'distance' from a particle
inline float ForceKernel(float normalised_distance, uniform float amplitude, uniform float balance)
{
	// Functions:
	//   a(x) = -x + B
	//   b(x) = 2.x^3 - 3.x^2 + 1
	//   Profile(x) = (A/B).a(x).b(x)
	// 
	// Controls:
	//  A = amplitude = the value at X = 0
	//  B = balance point = (0,1] = Controls where the profile goes below zero or where the forces balance
	float x = clamp(normalised_distance, 0.0f, 1.0f);
	float a = -x + balance;
	float b = 2.0f * cube(x) - 3.0f * sqr(x) + 1.0f;
	return (amplitude / balance) * a * b;
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
void ApplyForces(uint3 dtid : SV_DispatchThreadID)
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

	// Acceleration integration sub-step 
	const int sub_steps = 5;
	const float sub_step = Sim.TimeStep / sub_steps;
	
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
			{
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
					float force = ForceKernel(distance / Sim.ParticleRadius, Sim.ForceAmplitude, Sim.ForceBalance);
					float accel = Sim.ForceScale * target.density * neighbour.density * force;

					//// If a particle is on the boundary, then it can't be accelerated into the boundary surface.
					//// Any velocity the particle has will be in the plane of the boundary, so 'cross(velocity, cross(velocity, direction))'
					//// is normal to the boundary surface.
					//float4 t_direction = direction;
					//float4 n_direction = direction;
					//if (target.flags & ParticleFlag_Boundary)
					//{
					//	float4 boundary_normal = NormaliseOrZero(float4(cross(target.vel, cross(target.vel, direction.xyz)), 0));
					//	t_direction -= dot(boundary_normal, t_direction) * boundary_normal;
					//}
					//if (neighbour.flags & ParticleFlag_Boundary)
					//{
					//	float4 boundary_normal = NormaliseOrZero(float4(cross(neighbour.vel, cross(neighbour.vel, -direction.xyz)), 0));
					//	n_direction -= dot(boundary_normal, n_direction) * boundary_normal;
					//}					
					////can only be accelerated in the direction of the particles velocity
					//// since collision detection will ensure the velocity is not into the boundary.
					
					////float4 t_accel = select(target.flags & ParticleFlag_Boundary, accel, accel)
					////float4 n_accel = select(neighbour.flags & ParticleFlag_Boundary, accel, accel)

					
					// Update the particle velocities by the acceleration
					t_vel += sub_step * accel * direction;
					n_vel -= sub_step * accel * direction;

					// Advance the particles by a sub step
					t_pos += t_vel * sub_step;
					n_pos += n_vel * sub_step;
				}
			
				// 't_pos' and 't_vel' is where the particle should end up, calculate an acceleration that gives this result
				nett_accel += t_vel - target.vel;
			}
		}
	}

	// Apply viscosity. Add an acceleration that makes the particles velocity closer to the group average
	{
		float4 target_vel = target.vel + 0.5f * nett_accel * Sim.TimeStep;
		group_velocity *= 1.0f / group_velocity_weight;
		nett_accel += (group_velocity - target_vel) / Sim.TimeStep;
	}
	
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
			float normalised_distance = distance / Map.ParticleRadius;

			if (Map.Type == MapType_Velocity)
			{
				// Add up the weighted group velocity
				value += lerp(neighbour.vel, float4(0,0,0,0), distance / Map.ParticleRadius) * direction;
			}
			if (Map.Type == MapType_Accel)
			{
				// Add up the nett force
				value += (Map.ForceScale * ForceKernel(normalised_distance, Map.ForceAmplitude, Map.ForceBalance)) * direction;
			}
			if (Map.Type == MapType_Density)
			{
				// Add up the density
				value += DensityKernel(normalised_distance, Map.Dimensions, Map.ParticleRadius);
			}
		}
	}
	
	// Write the density to the map
	m_tex_map[uint2(dtid.x, dtid.y)] = LerpColour(length(value), Map.Colours);
}
