// Fluid
#ifndef POS_TYPE
#define POS_TYPE struct PosType { float4 pos; float4 col; float4 vel; float3 accel; float density; }
#endif
#ifndef SPATIAL_DIMENSIONS 
#define SPATIAL_DIMENSIONS 2
#endif

static const uint PosCountDimension = 1024;

POS_TYPE;

cbuffer cbFluid : register(b0)
{
	// Note: alignment matters. float4 must be aligned to 16 bytes.
	uint NumParticles;       // The number of particles
	float ParticleRadius;    // The radius of influence for each particle
	uint CellCount;          // The number of grid cells in the spatial partition
	float GridScale;         // The scale factor for the spatial partition grid

	float4 Gravity;          // The acceleration due to gravity

	float Mass;              // The particle mass
	float DensityToPressure; // The conversion factor from density to pressure
	float Density0;          // The baseline density
	float Viscosity;         // The viscosity scaler

	float ThermalDiffusion;  // The thermal diffusion rate
	float TimeStep;          // Leap-frog time step
	int RandomSeed;          // Seed value for the RNG
};

cbuffer cbColour : register(b1)
{
	// Note: alignment matters. float4 must be aligned to 16 bytes.
	// [0] = Velocity Based
	// [1] = Density Based
	// [2] = Within Probe
	float4 Colours[4];  // The colour scale to use
	float2 Range;       // Set scale to colour
	uint Scheme;        // Bit field of colouring schemes
};

cbuffer cbProbe : register(b2)
{
	// Note: alignment matters. float4 must be aligned to 16 bytes.
	float4 ProbePosition;
	float4 ProbeColour;
	float ProbeRadius;
	float ProbeForce;
}

static const uint ColourScheme_Velocity = 1;
static const uint ColourScheme_Density = 2;
static const uint ColourScheme_WithinProbe = 4;

// The positions of each particle
RWStructuredBuffer<PosType> m_particles : register(u0);

// The indices of particle positions sorted spatially
RWStructuredBuffer<uint> m_spatial : register(u1);

// The lowest index (in m_spatial) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start : register(u2);

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count : register(u3);

//#include "pr/view3d-12/compute/hlsl/spatial_partition.hlsli"
#include "E:/Rylogic/include/pr/view3d-12/compute/hlsl/spatial_partition.hlsli"

// Square a value
inline float sqr(float x)
{
	return x * x;
}
inline uint sqr(uint x)
{
	return x * x;
}

// Return the colour interpolated on the colour scale
inline float4 LerpColour(float value)
{
	float scale = saturate((value - Range.x) / (Range.y - Range.x));
	if (scale <= 0.0f)  return Colours[0];
	if (scale < 0.333f) return lerp(Colours[0], Colours[1], (scale - 0.000f) / 0.333f);
	if (scale < 0.666f) return lerp(Colours[1], Colours[2], (scale - 0.333f) / 0.333f);
	if (scale < 1.0f)   return lerp(Colours[2], Colours[3], (scale - 0.666f) / 0.333f);
	return Colours[3];
}

// Generate a random float on the interval [0, 1)
inline float Random(uint seed)
{
	return Hash(RandomSeed + seed) / 4294967296.0f; // Normalise to [0, 1)
}

// Generate a random direction vector components on the interval (-1, +1)
inline float4 Random3(uint seed)
{
	return float4(
		2 * Random(seed + FNV_prime32) - 1,
		2 * Random(seed + sqr(FNV_prime32)) - 1,
		#if SPATIAL_DIMENSIONS == 2
		0,
		#elif SPATIAL_DIMENSIONS == 3
		2 * Random(seed + sqr(FNV_prime32*FNV_prime32)) - 1,
		#endif
		0);
}

// The influence at 'distance' from a particle
inline float ParticleInfluenceAt(float distance, float radius)
{
	// Influence is the contribution to a property that a particle has at a given distance. The range of this contribution is controlled
	// by 'radius', which is the smoothing kernel radius. A property at a given point is calculated by taking the sum of that property for
	// all particles, weighted by their distance from the given point. If we limit the influence to a given radius, then we don't need to
	// consider all particles when measuring a property.
	// 
	// As 'radius' increases, more particles contribute to the measurement of the property. This means the weights need to reduce.
	// Consider a uniform grid of particles. A measured property (e.g. density) should be constant regardless of the value of 'radius'.
	// To make the weights independent of radius, we need to normalise them, i.e. divide by the total weight, which is the volume (in 2D)
	// under the influence curve (in 3D, it's a hyper volume).
	// 
	// If the smoothing curve is: P(r) = (R - |r|)^2
	// Then the volume under the curve is found from the double integral (in polar coordinates)
	// 2D:
	//   The volume under the curve is found from the double integral (in polar coordinates)
	//   (To understand where the extra 'r' comes from: https://youtu.be/PeeC_rWbios. Basically the delta area is r * dr * dtheta)
	//   $$\int_{0}_{tau} \int_{0}_{R} P(r) r dr dtheta$$
	//   auto volume = (1.0f / 12.0f) * maths::tauf * Pow(radius, 4.0f);
	// 3D:
	//   The volume under the curve is found from the triple integral (in spherical coordinates)
	//   $$\int_{0}_{tau} \int_{0}_{pi} \int_{0}_{R} P(r) r^2 sin(theta) dr dtheta dphi$$
	//   auto volume = (1.0f / 15.0f) * maths::tauf * Pow(radius, 5.0f);
	//
	// In reality, it doesn't matter what the volume is, as long as it scales correctly with 'radius' (i.e. R^4 for 2D, R^5 for 3D).
	// So, start with a uniform grid and a known property (e.g. density @ 1g/cm^3) and a radius that ensures a typical number of particles
	// influence each point. Then measure the combined influence, and use that value to rescale.

	if (distance >= radius)
		return 0.0f;

	#if SPATIAL_DIMENSIONS == 2
	{
		const float C = 0.95f * (1.0f / 4.0f);
		return C * sqr(radius - distance) / pow(radius, 4.0f);
	}
	#elif SPATIAL_DIMENSIONS == 3
	{
		const float C = 0.00242f;
		return C * sqr(radius - distance) / pow(radius, 5.0f);
	}
	#endif
}
	
// The gradient of the influence function at 'distance' from a particle
inline float dParticleInfluenceAt(float distance, float radius)
{
	if (distance >= radius)
		return 0.0f;

	#if SPATIAL_DIMENSIONS == 2
	{
		const float C = 0.00242f;
		return 2 * C * (radius - distance) / pow(radius, 4.0f);
	}
	#elif SPATIAL_DIMENSIONS == 3
	{
		const float C = 0.00242f;
		return 2 * C * (radius - distance) / pow(radius, 5.0f);
	}
	#endif
}

// Calculate the density at each particle
[numthreads(PosCountDimension, 1, 1)]
void DensityAtParticles(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];
	
	// Calculate the density at the predicted particle position
	float4 target_pos = target.pos + TimeStep * target.vel;

	#if SPATIAL_DIMENSIONS == 2
	float4 volume = float4(ParticleRadius, ParticleRadius, 0, 0);
	#elif SPATIAL_DIMENSIONS == 3
	float4 volume = float4(ParticleRadius, ParticleRadius, ParticleRadius, 0);
	#endif
	
	float density = ParticleInfluenceAt(0.0f, ParticleRadius) * Mass;
	
	// Search the neighbours of 'target_pos'
	FindIter find = Find(target_pos, volume, GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			// 'ParticleInfluenceAt' contains a distance check, so we don't need to check here
			// It's faster to sum with a multiply by zero than to branch.
			if (m_spatial[i] == gtid.x) continue; // no self-contribution
			PosType neighbour = m_particles[m_spatial[i]];
			float4 neighbour_pos = neighbour.pos;// + TimeStep * neighbour.vel;

			float dist = length(target_pos - neighbour_pos);
			float influence = ParticleInfluenceAt(dist, ParticleRadius);
			density += influence * Mass;
		}
	}
	
	// Record the density - It should be impossible for density to be
	// zero because the particle itself contributes to the density.
	m_particles[gtid.x].density = density;
}

// Calculates forces at each particle position
[numthreads(PosCountDimension, 1, 1)]
void ApplyForces(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];

	// Calculate pressure at the predicted particle position
	float4 target_pos = target.pos + TimeStep * target.vel;
	
	#if SPATIAL_DIMENSIONS == 2
	float4 volume = float4(ParticleRadius, ParticleRadius, 0, 0);
	#elif SPATIAL_DIMENSIONS == 3
	float4 volume = float4(ParticleRadius, ParticleRadius, ParticleRadius, 0);
	#endif

	float4 nett_pressure = float4(0, 0, 0, 0);
	float4 nett_viscosity = float4(0, 0, 0, 0);

	// Search the neighbours of 'target'
	FindIter find = Find(target_pos, volume, GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			if (m_spatial[i] == gtid.x) continue; // no self-interaction
			PosType neighbour = m_particles[m_spatial[i]];
			float4 neighbour_pos = neighbour.pos;// + TimeStep * neighbour.vel;
			
			float4 direction = target_pos - neighbour_pos;
			float dist = length(direction);

			// 'dParticleInfluenceAt' contains a distance check, so we don't need to check here
			float influence = dParticleInfluenceAt(dist, ParticleRadius);
			direction = (dist > 0.0001f) ? direction / dist : normalize(Random3(gtid.x));

			// We need to simulate the force due to pressure being applied to both particles (target and neighbour).
			// A simple way to do this is to average the pressure between the two particles. Since pressure is
			// a linear function of density, we can use the average density.
			float density = (target.density + neighbour.density) / 2.0f;

			// Convert the density to a pressure (P = k * (rho - rho0))
			float pressure = DensityToPressure * (density - Density0);

			// Calculate the viscosity from the relative velocity of the particles
			float4 relative_velocity = neighbour.vel - target.vel;
			
			// Get the pressure gradient at 'position' due to 'neighbour'
			nett_pressure += (pressure * influence * Mass / density) * direction;

			// Get the viscosity at 'position' due to 'neighbour'
			nett_viscosity += Viscosity * influence * relative_velocity;
		}
	}
	
	// Record the nett force
	m_particles[gtid.x].accel += nett_pressure.xyz / m_particles[gtid.x].density;
	m_particles[gtid.x].accel += nett_viscosity.xyz;
	m_particles[gtid.x].accel += Random3(gtid.x).xyz * ThermalDiffusion;
	m_particles[gtid.x].accel += Gravity.xyz;
}

// Apply colours to the particles
[numthreads(PosCountDimension, 1, 1)]
void ColourParticles(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	m_particles[gtid.x].col = Colours[0];

	// Colour the particles based on their velocity
	if (Scheme & ColourScheme_Velocity)
	{
		float speed = length(m_particles[gtid.x].vel);
		m_particles[gtid.x].col = LerpColour(speed);
	}
	
	// Colour the particles based on their density
	if (Scheme & ColourScheme_Density)
	{
		float density = m_particles[gtid.x].density;
		m_particles[gtid.x].col = LerpColour(density);
	}

	// Colour particles within the probe radius
	if (Scheme & ColourScheme_WithinProbe)
	{
		float radius_sq = sqr(ProbeRadius);
		float4 r = ProbePosition - m_particles[gtid.x].pos;
		if (dot(r, r) < radius_sq)
			m_particles[gtid.x].col = ProbeColour;
	}
}

// Apply an attractor to the particles
[numthreads(PosCountDimension, 1, 1)]
void ApplyProbe(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	float4 r = m_particles[gtid.x].pos - ProbePosition;
	float dist_sq = dot(r, r);
	if (dist_sq < sqr(ProbeRadius))
	{
		float dist = sqrt(dist_sq);
		float influence = 1 - saturate(dist / ProbeRadius);
		float4 direction = dist > 0.0001f ? r / dist : float4(0, 0, 0, 0);
		float4 force = influence * (ProbeForce * direction - m_particles[gtid.x].vel);
		m_particles[gtid.x].accel += force.xyz * (Mass / m_particles[gtid.x].density);
	}
}

// Debug Function
[numthreads(1, 1, 1)]
void Debugging(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{	
	#if SPATIAL_DIMENSIONS == 2
	float4 volume = float4(ProbeRadius, ProbeRadius, 0, 0);
	#elif SPATIAL_DIMENSIONS == 3
	float4 volume = float4(ProbeRadius, ProbeRadius, ProbeRadius, 0);
	#endif

	float4 pos = ProbePosition;
	float radius_sq = sqr(ProbeRadius);
	
	// Search the neighbours of 'target'
	FindIter find = Find(pos, volume, GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			PosType neighbour = m_particles[m_spatial[i]];
			
			//float4 r = ProbePosition - .pos;
			//if (dot(r, r) > radius_sq)
			//	continue;
				
			m_particles[m_spatial[i]].col = float4(0, 1, 0, 1);
		}
	}
}