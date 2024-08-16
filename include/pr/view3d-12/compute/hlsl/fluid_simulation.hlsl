// Fluid
#ifndef SPATIAL_DIMENSIONS 
#define SPATIAL_DIMENSIONS 2
#endif

static const uint ThreadGroupSize = 1024;

#ifndef POS_TYPE
#define POS_TYPE struct PosType { float4 pos; float4 col; float4 vel; float3 accel; float mass; }
#endif
POS_TYPE;

// Note: alignment matters. float4 must be aligned to 16 bytes.
cbuffer cbFluidSim : register(b0)
{
	uint NumParticles;    // The number of particles
	uint NumPrimitives;   // The number of collision primitives
	float ParticleRadius; // The radius of influence for each particle
	float TimeStep;       // Leap-frog time step

	float4 Gravity;          // The acceleration due to gravity

	float Mass;             // The particle mass
	float ForceScale;       // The force scaling factor
	float Viscosity;        // The viscosity scaler
	float ThermalDiffusion; // The thermal diffusion rate

	float Attraction;       // A value that controls the attraction force. <= 1 = no attraction, >1 = more attraction
	float Falloff;          // The falloff distance for the pressure force
	float GridScale;        // The scale factor for the spatial partition grid
	uint CellCount;         // The number of grid cells in the spatial partition

	int RandomSeed;         // Seed value for the RNG
};
cbuffer cbColour : register(b1)
{
	// Note: alignment matters. float4 must be aligned to 16 bytes.
	float4 Colours[4];       // The colour scale to use
	float2 ColourValueRange; // Set scale to colour
	uint Scheme;             // 0 = None, 1 = Velocity, 2 = Accel, 3 = Density, 0x80000000 = Within Probe
};
cbuffer cbProbe : register(b2)
{
	// Note: alignment matters. float4 must be aligned to 16 bytes.
	float4 ProbePosition;
	float4 ProbeColour;
	float ProbeRadius;
	float ProbeForce;
	int ProbeHighlight; // 0 = None, 1 = Highlight
}
cbuffer cbMap : register(b3)
{
	float4x4 MapToWorld; // Transform from map space to world space (including scale)
	int2 MapTexDim;      // The dimensions of the map texture
	int MapType;         // 0 = Pressure
};

static const uint ColourScheme_None = 0;
static const uint ColourScheme_Velocity = 1;
static const uint ColourScheme_Accel = 2;
static const uint ColourScheme_Density = 3;
static const uint MapType_None = 0;
static const uint MapType_Velocity = 1;
static const uint MapType_Accel = 2;
static const uint MapType_Density = 3;

// The positions of each particle
RWStructuredBuffer<PosType> m_particles : register(u0);

// The indices of particle positions sorted spatially
RWStructuredBuffer<uint> m_spatial : register(u1);

// The lowest index (in m_spatial) for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_start : register(u2);

// The number of positions for each cell hash (length CellCount)
RWStructuredBuffer<uint> m_idx_count : register(u3);

#include "E:/Rylogic/include/pr/view3d-12/compute/hlsl/collision_primitives.hlsli"

// The primitives to collide against
RWStructuredBuffer<Prim> m_collision : register(u4);

#include "E:/Rylogic/include/pr/view3d-12/compute/hlsl/spatial_partition.hlsli"
#include "E:/Rylogic/include/pr/view3d-12/compute/hlsl/geometry.hlsli"
#include "E:/Rylogic/include/pr/view3d-12/compute/hlsl/utility.hlsli"

// A texture for writing the density map to
RWTexture2D<float4> m_tex_map : register(u5);

// A random direction vector (not normalised) in 2 or 3 dimensions with components in (-1,+1)
inline float4 Random3WithDim(float seed)
{
	float4 r = Random3N(RandomSeed + seed);
	#if SPATIAL_DIMENSIONS == 2
	r.z = 0;
	#endif
	return r;
}

// Return a volume radius for a neighbour search
inline float4 SearchVolume(float radius)
{
	#if SPATIAL_DIMENSIONS == 2
	return float4(radius, radius, 0, 0);
	#elif SPATIAL_DIMENSIONS == 3
	return float4(radius, radius, radius, 0);
	#endif
}

// Return the colour interpolated on the colour scale
inline float4 LerpColour(float value)
{
	float scale = saturate((value - ColourValueRange.x) / (ColourValueRange.y - ColourValueRange.x));
	if (scale <= 0.0f)  return Colours[0];
	if (scale < 0.333f) return lerp(Colours[0], Colours[1], (scale - 0.000f) / 0.333f);
	if (scale < 0.666f) return lerp(Colours[1], Colours[2], (scale - 0.333f) / 0.333f);
	if (scale < 1.0f)   return lerp(Colours[2], Colours[3], (scale - 0.666f) / 0.333f);
	return Colours[3];
}

// The influence at 'distance' from a particle
inline float ForceProfile(float distance)
{
	if (distance >= ParticleRadius)
		return 0.0f;

	// Normalise distance
	distance /= ParticleRadius;
	
	// Peak is the maximum influence scaler. You probably want this to be 1.0f and control the force separately.
	// Range is the distance along X where the influence is not zero. 5 works about to be about y = 0 at x = 1
	// Attraction controls the depth of the negative repulsive force.
	const float Peak = 1.0f;
	const float Range = 5.0f;
	
	float A = -Attraction * distance + Peak;
	float B = Peak * exp(-Range * pow(distance, Falloff));
	return A * B;
}

// The influence at 'distance' from a particle
inline float ViscosityProfile(float distance)
{
	if (distance >= ParticleRadius)
		return 0.0f;

	distance /= ParticleRadius;
	return 1.0f / (distance + 0.001f);
}

// Calculates forces at each particle position
[numthreads(ThreadGroupSize, 1, 1)]
void ApplyForces(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];

	// Advance the particle by a leap-frog integration half step
	float4 target_pos = target.pos + TimeStep * target.vel;

	// Search the neighbours of 'target'
	float4 nett_accel = float4(0, 0, 0, 0);
	FindIter find = Find(target_pos, SearchVolume(ParticleRadius), GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			if (m_spatial[i] == gtid.x) continue; // no self-interaction
			PosType neighbour = m_particles[m_spatial[i]];

			// Advance the neighbour by a leap-frog integration half step
			float4 neighbour_pos = neighbour.pos + TimeStep * neighbour.vel;

			// Distance check
			float4 direction = target_pos - neighbour_pos;
			float distance_sq = sqr(direction);
			if (distance_sq >= sqr(ParticleRadius))
				continue;

			// Get the normalised direction vector to 'neighbour'
			float distance = sqrt(distance_sq);
			direction = distance > 0.0001f ? direction / distance : normalize(Random3WithDim(gtid.x));
			
			// Determine the force experienced by 'target' due to 'neighbour' at 'distance'
			// F = S * M * M / r^2, A = F / M, so A = S * M / r^2
			nett_accel += (ForceScale * Mass * ForceProfile(distance)) * direction;
			
			// Calculate the viscosity from the relative velocity
			nett_accel += Viscosity * ViscosityProfile(distance) * (neighbour.vel - target.vel);
		}
	}
	
	// Record the nett force
	m_particles[gtid.x].accel += nett_accel.xyz;
	m_particles[gtid.x].accel += Random3WithDim(gtid.x).xyz * ThermalDiffusion;
	m_particles[gtid.x].accel += Gravity.xyz;
}

// Apply an attractor to the particles
[numthreads(ThreadGroupSize, 1, 1)]
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
		m_particles[gtid.x].accel += force.xyz * Mass;
	}
}

// Apply colours to the particles
[numthreads(ThreadGroupSize, 1, 1)]
void ColourParticles(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	m_particles[gtid.x].col = Colours[0];

	// Colour the particles based on their velocity
	if (Scheme == ColourScheme_Velocity)
	{
		float speed = length(m_particles[gtid.x].vel);
		m_particles[gtid.x].col = LerpColour(speed);
	}
	
	// Colour the particles based on their density
	if (Scheme == ColourScheme_Accel)
	{
		float accel = length(m_particles[gtid.x].accel);
		m_particles[gtid.x].col = LerpColour(accel);
	}

	// Colour particles within the probe radius
	if (Scheme == ColourScheme_Density)
	{
		float weight = 0.0f;
		float4 pos = m_particles[gtid.x].pos;
		FindIter find = Find(pos, SearchVolume(ParticleRadius), GridScale, CellCount);
		while (DoFind(find, CellCount))
		{
			for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
			{
				PosType neighbour = m_particles[m_spatial[i]];
				float distance = length(pos - neighbour.pos);
				weight += lerp(Mass, 0.0, distance / ParticleRadius);
			}
		}
		m_particles[gtid.x].col = LerpColour(weight);
	}
	
	// Highlight particles within the probe radius
	if (ProbeHighlight)
	{
		float4 r = ProbePosition - m_particles[gtid.x].pos;
		if (dot(r, r) < sqr(ProbeRadius))
			m_particles[gtid.x].col = ProbeColour;
	}
}

// Populate a texture with each pixel representing the property at that point
[numthreads(32, 32, 1)]
void GenerateMap(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x > MapTexDim.x || gtid.y > MapTexDim.y)
		return;
	
	// The position in world space of the texture coordinate
	float4 pos = mul(MapToWorld, float4(gtid.x, gtid.y, 0, 1));
	
	float4 value = float4(0,0,0,0);

	// Search the neighbours of 'pos'
	FindIter find = Find(pos, SearchVolume(ParticleRadius), GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			PosType neighbour = m_particles[m_spatial[i]];

			float4 direction = pos - neighbour.pos;
			float distance_sq = sqr(direction);
			if (distance_sq >= sqr(ParticleRadius))
				continue;

			// Get the normalised direction vector to 'neighbour'
			float distance = sqrt(distance_sq);
			direction = distance > 0.0001f ? direction / distance : normalize(Random3WithDim(gtid.x));

			if (MapType == MapType_Velocity)
			{
				// Add up the weighted group velocity
				value += lerp(neighbour.vel, float4(0,0,0,0), distance / ParticleRadius) * direction;
			}
			if (MapType == MapType_Accel)
			{
				// Add up the nett force
				value += (ForceScale * Mass * ForceProfile(distance)) * direction;
			}
			if (MapType == MapType_Density)
			{
				// Add up the density
				value += lerp(Mass, 0.0, distance / ParticleRadius);
			}
		}
	}
	
	// Write the density to the map
	m_tex_map[uint2(gtid.x, gtid.y)] = LerpColour(length(value));
}

// Debug Function
[numthreads(1, 1, 1)]
void Debugging(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{	
	float4 pos = ProbePosition;
	float radius_sq = sqr(ProbeRadius);
	
	// Search the neighbours of 'target'
	FindIter find = Find(pos, SearchVolume(ProbeRadius), GridScale, CellCount);
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



#if 0

// The influence at 'distance' from a particle
inline float DensityKernel(float distance)
{
	// Influence is the contribution to a property that a particle has at a given distance. The range of this contribution is controlled
	// by 'radius', which is the smoothing kernel radius. A property at a given point is calculated by taking the sum of that property for
	// all particles, weighted by their distance from the given point. If we limit the influence to a given radius, then we don't need to
	// consider all particles when measuring a property.
	// 
	// As 'radius' increases, more particles contribute to the measurement of the property. This means the weights need to reduce.
	// Consider a uniform grid of particles. A measured property (e.g. density) should be constant regardless of the value of 'radius'.
	// To make the weights independent of radius, we need to normalise them, i.e. divide by the total weight, which is the "volume" under
	// the influence curve in 2D (in 3D, it's a hyper volume).
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

	if (distance >= ParticleRadius)
		return 0.0f;

	#if SPATIAL_DIMENSIONS == 2
	{
		// P(r) = (1/V) * (R - |r|)^2, where V is the volume under the curve (R - |r|)^2
		// V = (tau/12) * radius^4
		#if 1
		{
			//const float C = 0.95f * (1.0f / 4.0f);
			//return C * sqr(ParticleRadius - distance) / pow(ParticleRadius, 4.0f);
			const float C = 0.52359877559829887308f * pow(ParticleRadius, 4); // = (tau/12) * radius^4
			return sqr(ParticleRadius - distance) / C;
		}
		#endif
		#if 0
		{ // P(r) = (R - |r|)
			const float C = 1.04719755119659774615f * pow(ParticleRadius, 3.0f); // = (tau/6) * radius^3
			return (ParticleRadius - distance) / C;
		}
		#endif
		
	}
	#elif SPATIAL_DIMENSIONS == 3
	{
		const float C = 0.00242f;
		return C * sqr(ParticleRadius - distance) / pow(ParticleRadius, 5.0f);
	}
	#endif
}
	
// The gradient of the influence function at 'distance' from a particle
inline float dDensityKernel(float distance)
{
	if (distance >= ParticleRadius)
		return 0.0f;

	#if SPATIAL_DIMENSIONS == 2
	{
		// dP(r)/dr = 2 * (1/V) * (R - |r|)
		#if 1
		{
			//const float C = 0.00242f;
			//return 2 * C * (ParticleRadius - distance) / pow(ParticleRadius, 4.0f);
			const float C = 0.52359877559829887308f * pow(ParticleRadius, 4); // = (tau/12) * radius^4
			return 2 * (ParticleRadius - distance) / C;
		}
		#endif
		#if 0
		{// dP(r)/dr = -1
			const float C = 1.04719755119659774615f * pow(ParticleRadius, 3.0f); // = (tau/6) * radius^3
			return -1.0f / C;
		}
		#endif
	}
	#elif SPATIAL_DIMENSIONS == 3
	{
		const float C = 0.00242f;
		return 2 * C * (ParticleRadius - distance) / pow(ParticleRadius, 5.0f);
	}
	#endif
}

// Calculate the density at each particle
[numthreads(ThreadGroupSize, 1, 1)]
void DensityAtParticles(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];
	
	// Calculate the density at the predicted particle position
	float4 target_pos = target.pos + TimeStep * target.vel;
	float density = DensityKernel(0.0f) * Mass;
	
	// Search the neighbours of 'target_pos'
	FindIter find = Find(target_pos, SearchVolume(ParticleRadius), GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			// 'DensityKernel' contains a distance check, so we don't need to check here
			// It's faster to sum with a multiply by zero than to branch.
			if (m_spatial[i] == gtid.x) continue; // no self-contribution
			PosType neighbour = m_particles[m_spatial[i]];

			float dist = length(target_pos - neighbour.pos);
			float influence = DensityKernel(dist);
			density += influence * Mass;
		}
	}
	
	// Record the density - It should be impossible for density to be
	// zero because the particle itself contributes to the density.
	m_particles[gtid.x].density = density;
}

// Make adjustments to the particles near the boundaries
[numthreads(ThreadGroupSize, 1, 1)]
void BoundaryEffects(int3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	// Because of the way density is calculated, particles near the boundaries have lower density.
	// This causes them to get pushed closer together. The density of particles near the boundary
	// needs to be artificially increased.
	//
	// Density is calculated from the weighted sum of particles in a sphere around the particle.
	// Near the boundary, that sphere should really be clipped. The ratio of volume between the
	// unclipped and clipped sphere is the ratio to multiply the density by.
	//
	// Clipping a sphere to a set of arbitrary planes is a bit tricky. Instead, approximate the
	// sphere with a cloud of points. Clip the points, and use the ratio of clipped to unclipped
	// points to scale the density.
	//
	// Unfortunately, just increasing the density near the boundary doesn't work, because adjacent
	// particles on the boundary all have the same density so the pressure between them is zero.
	// All this does is push the layer above the boundary further away.
	//
	// I really need a way to push adjacent particles apart. I could try to push them apart by
	// applying a force to each particle that is proportional to the distance to the boundary.
	//
	// I could mark particles that are near a boundary, then in 'ApplyForces' increase the 'DensityToPressure'
	// scalar between boundary particles.
	
	if (gtid.x >= NumParticles)
		return;

	// Assume particle accelerations have already been set.
	PosType target = m_particles[gtid.x];

	#if 0
	// 'clipped' is a bit field of Fibonacci spiral points that are clipped
	static const int NumFibSpiralPoints = 32;
	uint clipped = 0;

	// Find the distance to each boundary surface
	for (int i = 0; i != NumPrimitives; ++i)
	{
		Prim prim = m_collision[i];

		// The returned value is a plane relative to 'target.pos'.
		// i.e. 'target.pos' is 'above.w' units above the plane with normal 'above.xyz'.
		float4 above = float4(0, 0, 0, 0);
		switch (prim.flags.x)
		{
			case Prim_Plane:
			{
				above = DistancePointToPlane(target.pos, prim.data[0]);
				break;
			}
			case Prim_Sphere:
			{
				break;
			}
			case Prim_Triangle:
			{
				break;
			}
		}

		// If the particle is near the boundary, then clip the sample points
		if (abs(above.w) < ParticleRadius)
		{
			// Given some sample points around a sphere, clip those behind the plane 'distance'
			[unroll]
			for (int i = 0; i != NumFibSpiralPoints; ++i)
			{
				if (clipped & (1u << i)) continue;
				clipped |= uint(dot(above, FibonacciSpiral(i, NumFibSpiralPoints)) < 0) << i;
			}
		}
	}
	
	// Count the number of bits in 'clipped'
	int removed = CountBits(clipped);
	if (removed != NumFibSpiralPoints)
		m_particles[gtid.x].density *= float(NumFibSpiralPoints) / float(NumFibSpiralPoints - removed);
	#endif
}

// Calculates forces at each particle position
[numthreads(ThreadGroupSize, 1, 1)]
void ApplyForces(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];

	// Calculate pressure at the predicted particle position
	float4 target_pos = target.pos + TimeStep * target.vel;

	float4 nett_pressure = float4(0, 0, 0, 0);
	float4 nett_viscosity = float4(0, 0, 0, 0);

	// Search the neighbours of 'target'
	FindIter find = Find(target_pos, SearchVolume(ParticleRadius), GridScale, CellCount);
	while (DoFind(find, CellCount))
	{
		for (int i = find.idx_range.x; i != find.idx_range.y; ++i)
		{
			if (m_spatial[i] == gtid.x) continue; // no self-interaction
			PosType neighbour = m_particles[m_spatial[i]];
			float4 neighbour_pos = neighbour.pos;// + TimeStep * neighbour.vel;
			
			float4 direction = target_pos - neighbour_pos;
			float dist = length(direction);

			// 'dDensityKernel' contains a distance check, so we don't need to check here
			float influence = dDensityKernel(dist);
			
			direction = (dist > 0.0001f) ? direction / dist : normalize(Random3WithDim(gtid.x));

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
	m_particles[gtid.x].accel += Random3WithDim(gtid.x).xyz * ThermalDiffusion;
	m_particles[gtid.x].accel += Gravity.xyz;
}

#endif
