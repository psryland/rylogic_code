// Fluid
#ifndef POS_TYPE
#define POS_TYPE struct PosType { float3 pos; float3 vel; float3 accel; float density; }
#endif
#ifndef SPATIAL_DIMENSIONS 
#define SPATIAL_DIMENSIONS 2
#endif

static const uint PosCountDimension = 1024;

POS_TYPE;

cbuffer cbFluid : register(b0)
{
	uint NumParticles;       // The number of particles
	uint CellCount;          // The number of grid cells in the spatial partition
	float GridScale;         // The scale factor for the spatial partition grid
	float Radius;            // The radius of influence for each particle
	float3 Gravity;          // The acceleration due to gravity
	float Mass;              // The particle mass
	float DensityToPressure; // The conversion factor from density to pressure
	float Density0;          // The baseline density
	float Viscosity;         // The viscosity scaler
};

// The positions of each particle
RWStructuredBuffer<PosType> m_particles : register(u0);

// The indices of particle positions sorted spatially
RWStructuredBuffer<uint> m_spatial : register(u1);

// The lowest index (in m_spatial) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start : register(u2);

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count : register(u3);

#include "pr/view3d-12/compute/hlsl/spatial_partition.hlsli"

// Square a value
inline float sqr(float x)
{
	return x * x;
}
inline uint sqr(uint x)
{
	return x * x;
}

// Generate a random float on the interval [0, 1)
inline float Random(uint seed)
{
	return Hash(seed) / 4294967296.0f; // Normalise to [0, 1)
}

// Generate a random float3 with components on the interval (-1, +1)
inline float3 Random3(uint seed)
{
	return float3(
		2 * Random(seed + 0) - 1,
		2 * Random(seed + 1) - 1,
		2 * Random(seed + 2) - 1);
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

	float density = 0.0f;
	float3 target = m_particles[gtid.x].pos.xyz;
	
	// Search the neighbours of 'target'
	FindIter find = Find(target, float3(Radius, Radius, Radius), GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			// 'ParticleInfluenceAt' contains a distance check, so we don't need to check here
			float dist = length(target - m_particles[m_spatial[i]].pos.xyz);
			float influence = ParticleInfluenceAt(dist, Radius);
			density += influence * Mass;
		}
	}
	
	// Record the density
	m_particles[gtid.x].density = density;
}

// Calculates forces at each particle position
[numthreads(PosCountDimension, 1, 1)]
void ApplyForces(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];
	float3 nett_pressure = float3(0, 0, 0);
	float3 nett_viscosity = float3(0, 0, 0);
	
	// Search the neighbours of 'target'
	FindIter find = Find(target.pos.xyz, float3(Radius, Radius, Radius), GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			PosType neighbour = m_particles[m_spatial[i]];
			
			float3 direction = target.pos.xyz - neighbour.pos.xyz;
			float dist = length(direction);

			// 'dParticleInfluenceAt' contains a distance check, so we don't need to check here
			float influence = dParticleInfluenceAt(dist, Radius);
			direction = (dist > 0.0001f) ? direction / dist : normalize(Random3(gtid.x));

			// We need to simulate the force due to pressure being applied to both particles (idx and index).
			// A simple way to do this is to average the pressure between the two particles. Since pressure is
			// a linear function of density, we can use the average density.
			float density = (target.density + neighbour.density) / 2.0f;

			// Convert the density to a pressure (P = k * (rho - rho0))
			float relative_density = density - Density0;

			// Calculate the viscosity from the relative velocity of the particles
			float3 relative_velocity = target.vel.xyz - neighbour.vel.xyz;
			
			// Get the pressure gradient at 'position' due to 'neighbour'
			nett_pressure += DensityToPressure * (relative_density * influence * Mass / density) * direction;

			// Get the viscosity at 'position' due to 'neighbour'
			nett_viscosity += Viscosity * influence * relative_velocity;
		}
	}
	
	// Record the density
	m_particles[gtid.x].accel.xyz +=
		nett_pressure / m_particles[gtid.x].density +
		nett_viscosity +
		Gravity;
}