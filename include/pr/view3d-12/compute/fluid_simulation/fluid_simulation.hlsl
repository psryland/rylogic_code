//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "../common/geometry.hlsli"
#include "../common/utility.hlsli"

#ifndef PARTICLE_TYPE
#define PARTICLE_TYPE struct Particle { float4 pos; float4 col; float4 vel; float3 accel; float mass; }
#endif
PARTICLE_TYPE;

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
		int Dimensions;         // 2D or 3D simulation
		int NumParticles;       // The number of particles
		int CellCount;          // The number of grid cells in the spatial partition
		float GridScale;        // The scale factor for the spatial partition grid

		int RandomSeed;         // Seed value for the RNG
		float ParticleRadius;   // The radius of influence for each particle
		float TimeStep;         // Leap-frog time step
		float Mass;             // The particle mass

		float4 Gravity;         // The acceleration due to gravity

		float ForceScale;       // The force scaling factor
		float Viscosity;        // The viscosity scaler
		float ThermalDiffusion; // The thermal diffusion rate
		int ForceProfileLength; // The length of the force profile buffer
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
		int Dimensions;         // 2D or 3D simulation
		int NumParticles;       // The number of particles in the 'm_particles' buffer
		
		int Scheme;             // 0 = None, 1 = Velocity, 2 = Accel, 3 = Density
		int CellCount;          // The number of grid cells in the spatial partition
		float GridScale;        // The scale factor for the spatial partition grid
		float ParticleRadius;   // The particle radius
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
		float ParticleMass;   // The particle mass
		float ForceScale;     // The force scaling factor
		int ForceProfileLength; // The length of the force profile buffer
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
RWStructuredBuffer<Particle> m_particles : register(u0);

// The indices of particle positions sorted spatially
StructuredBuffer<uint> m_spatial : register(t0);

// The lowest index (in m_spatial) for each cell hash (length CellCount)
StructuredBuffer<uint> m_idx_start : register(t1);

// The number of positions for each cell hash (length CellCount)
StructuredBuffer<uint> m_idx_count : register(t2);

// A function that defines the normalised force vs. distance from a particle. Values should be [0, 1]. (length ForceProfileLength)
StructuredBuffer<float> m_force_profile : register(t3);

#include "../spatial_partition/spatial_partition.hlsli"

// A buffer for passing back results
RWStructuredBuffer<uint> m_output : register(u1);

// A texture for writing the density map to
RWTexture2D<float4> m_tex_map : register(u2);

// General purpose group shared memory
groupshared uint gs_memory[TotalSharedMemory];

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
inline float ForceProfile(float normalised_distance, uniform int force_profile_length)
{
	int index = (int)floor(force_profile_length * normalised_distance);
	if (index >= force_profile_length)
		return 0.0f;
	
	return lerp(m_force_profile[index], m_force_profile[index+1], frac(force_profile_length * normalised_distance));
}

// The influence at normalised 'distance' from a particle
inline float ViscosityProfile(float normalised_distance)
{
	if (normalised_distance >= 1)
		return 0.0f;

	return 1.0f / (normalised_distance + 0.001f);
}

// True if 'particle' is a dead particle
inline bool IsDeadParticle(Particle particle)
{
	return any(isnan(particle.pos));
}

// Calculates forces at each particle position
[numthreads(ThreadGroupSize, 1, 1)]
void ApplyForces(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Sim.NumParticles)
		return;

	Particle target = m_particles[dtid.x];

	// Sub step integration
	const int sub_steps = 5;
	const float sub_step = Sim.TimeStep / sub_steps;
	
	// Sample the environment for neighbours at the predicted particle position at TimeStep/2
	float4 target_pos = target.pos + 0.5f * Sim.TimeStep * target.vel;

	// Search the neighbours of 'target'
	float4 nett_accel = float4(0, 0, 0, 0);
	FindIter find = Find(target_pos, SearchVolume(Sim.ParticleRadius, Sim.Dimensions), Sim.GridScale, Sim.CellCount);
	while (DoFind(find, Sim.CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			// No self-interaction
			if (m_spatial[i] == dtid.x)
				continue;

			Particle neighbour = m_particles[m_spatial[i]];

			float4 t_pos = target.pos;
			float4 n_pos = neighbour.pos;
			float4 t_vel = target.vel;
			float4 n_vel = neighbour.vel;

			// Loop over sub-steps
			[unroll]
			for (int k = 0; k != sub_steps; ++k)
			{
				// Advance the particles by a half step
				float4 t_pos_half_step = t_pos + 0.5f * sub_step * t_vel;
				float4 n_pos_half_step = n_pos + 0.5f * sub_step * n_vel;

				// Get the separating vector and distance
				float4 sep = t_pos_half_step - n_pos_half_step;
				float distance = length(sep);
				float4 direction = select(distance != 0, sep / distance, Random3WithDim(float2(dtid.x, Sim.RandomSeed), Sim.Dimensions));
				float normalised_distance = distance / Sim.ParticleRadius;

				// Calculate the acceleration
				float4 accel =
					(Sim.ForceScale * Sim.Mass * ForceProfile(normalised_distance, Sim.ForceProfileLength)) * direction +
					(Sim.Viscosity * ViscosityProfile(normalised_distance)) * signed_sqr(n_vel - t_vel);

				// Update the particle velocities by the acceleration
				t_vel += sub_step * accel;
				n_vel -= sub_step * accel;
				
				// Advance the particles by a sub step
				t_pos += t_vel * sub_step;
				n_pos += n_vel * sub_step;
			}

			// 't_pos' and 't_vel' is where the particle should end up, calculate an acceleration that gives this result
			nett_accel += t_vel - target.vel;
		}
	}

	// Apply the thermal diffusion and gravity
	nett_accel += Random3WithDim(float2(dtid.x, Sim.RandomSeed), Sim.Dimensions) * Sim.ThermalDiffusion;
	nett_accel += Sim.Gravity;
	
	// Record the nett force
	m_particles[dtid.x].accel += nett_accel.xyz;
}

// Apply an attractor to the particles
[numthreads(ThreadGroupSize, 1, 1)]
void ApplyProbe(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Probe.NumParticles)
		return;

	// If the particle is within the probe radius apply a force toward the centre.
	// Scale by inverse distance so that the centre of the probe isn't high pressure.
	float4 r = m_particles[dtid.x].pos - Probe.Position;
	float dist_sq = dot(r, r);
	if (dist_sq < sqr(Probe.Radius))
	{
		float dist = sqrt(dist_sq);
		float influence = saturate(dist / Probe.Radius); // 1 at range, 0 in the centre
		float4 direction = dist > 0.0001f ? r / dist : float4(0, 0, 0, 0);
		float4 force = influence * (Probe.Force * direction - m_particles[dtid.x].vel);
		m_particles[dtid.x].accel += force.xyz; // Probe ignores mass
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
	if (IsDeadParticle(m_particles[src_idx]))
		return;

	// While 'dst_idx' is within the dead range, look up the next 'dst_idx'
	for (; dst_idx >= dead_range.x && dst_idx < dead_range.y; )
		dst_idx = m_spatial[dst_idx];
	
	// If the destination index is not within the dead range then replace and we're done
	if (dst_idx < dead_range.x || dst_idx >= dead_range.y)
		m_particles[dst_idx] = m_particles[src_idx];
}

// Apply colours to the particles
[numthreads(ThreadGroupSize, 1, 1)]
void ColourParticles(uint3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Col.NumParticles)
		return;

	int scheme = Col.Scheme & ColourScheme_MASK;
	m_particles[dtid.x].col = Col.Colours.Spectrum[0];

	// Colour the particles based on their velocity
	if (scheme == ColourScheme_Velocity)
	{
		float speed = length(m_particles[dtid.x].vel);
		m_particles[dtid.x].col = LerpColour(speed, Col.Colours);
	}
	
	// Colour the particles based on their density
	if (scheme == ColourScheme_Accel)
	{
		float accel = length(m_particles[dtid.x].accel);
		m_particles[dtid.x].col = LerpColour(accel, Col.Colours);
	}

	// Colour particles within the probe radius
	if (scheme == ColourScheme_Density)
	{
		float weight = 0.0f;
		float4 pos = m_particles[dtid.x].pos;
		FindIter find = Find(pos, SearchVolume(Col.ParticleRadius, Col.Dimensions), Col.GridScale, Col.CellCount);
		while (DoFind(find, Col.CellCount))
		{
			for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
			{
				Particle neighbour = m_particles[m_spatial[i]];
				float distance = length(pos - neighbour.pos);
				weight += lerp(1, 0, distance / Col.ParticleRadius);
			}
		}
		m_particles[dtid.x].col = LerpColour(weight, Col.Colours);
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
			Particle neighbour = m_particles[m_spatial[i]];

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
				value += (Map.ForceScale * Map.ParticleMass * ForceProfile(normalised_distance, Map.ForceProfileLength)) * direction;
			}
			if (Map.Type == MapType_Density)
			{
				// Add up the density
				value += lerp(Map.ParticleMass, 0.0, distance / Map.ParticleRadius);
			}
		}
	}
	
	// Write the density to the map
	m_tex_map[uint2(dtid.x, dtid.y)] = LerpColour(length(value), Map.Colours);
}
