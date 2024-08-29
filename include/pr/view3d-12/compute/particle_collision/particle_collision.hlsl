//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
static const int ThreadGroupSize = 1024;
static const int MaxCollisionResolutionSteps = 10;

#ifndef PARTICLE_TYPE
#define PARTICLE_TYPE struct Particle { float4 pos; float4 col; float4 vel; float3 accel; float density; }
#endif
PARTICLE_TYPE;

cbuffer cbCollision : register(b0)
{
	// Note: Alignment matters. float2 must be aligned to 8 bytes.
	int NumParticles;        // The number of particles
	int NumPrimitives;       // The number of primitives
	int SpatialDimensions;   // The number of spatial dimensions
	float TimeStep;          // The time to advance each particle by
	
	float ParticleRadius;    // The radius of volume that each particle represents
	float BoundaryThickness; // The thickness in with boundary effects are applied
	float BoundaryForce;     // The repulsive force of the boundary
	float pad;

	float2 Restitution;      // The coefficient of restitution (normal, tangential)
};
cbuffer cbCull : register(b1)
{
	float4 Cull[2];          // A plane, sphere, etc used to cull particles (set their positions to NaN)
	int Flags;               // [3:0] = 0: No culling, 1: Sphere, 2: Plane, 3: Box
};

#include "collision.hlsli"

static const int Cull_None = 0;         // Cull no used
static const int Cull_Plane = 1;        // Cull[0].xyz = normal, Cull[0].w = distance
static const int Cull_Sphere = 2;       // Cull[0].xyz = centre, Cull[0].w = radius
static const int Cull_SphereInside = 3; // Cull[0].xyz = centre, Cull[0].w = radius
static const int Cull_Box = 4;          // Cull[0].xyz = centre, Cull[1].xyz = half-extents
static const int Cull_BoxInside = 5;    // Cull[0].xyz = centre, Cull[1].xyz = half-extents
static const int Cull_Mask = 0x07;
static const float4 CulledPosition = float4(0,0,0,0)/0;

// The positions/velocities of each particle
RWStructuredBuffer<Particle> m_particles : register(u0);

// The primitives to collide against
StructuredBuffer<Prim> m_collision : register(t0);

// Test a position for being culled
inline float4 CullCheck(float4 pos, uniform int cull_mode)
{
	// Note: the boundary counts as "not culled"
	switch (cull_mode)
	{
		case Cull_None:
		{
			return pos;
		}
		case Cull_Plane:
		{
			// Cull if below the plane
			return dot(pos, Cull[0]) >= 0 ? pos : CulledPosition;
		}
		case Cull_Sphere:
		{
			// Cull if outside the sphere
			float r_sq = sqr(Cull[0].w);
			float3 r = pos.xyz - Cull[0].xyz;
			return dot(r,r) <= r_sq ? pos : CulledPosition;
		}
		case Cull_SphereInside:
		{
			// Cull if inside the sphere
			float r_sq = sqr(Cull[0].w);
			float3 r = pos.xyz - Cull[0].xyz;
			return dot(r,r) >= r_sq ? pos : CulledPosition;
		}
		case Cull_Box:
		{
			// Cull if outside the box
			float3 r = abs(pos.xyz - Cull[0].xyz) - Cull[1].xyz;
			return all(r <= 0) ? pos : CulledPosition;
		}
		case Cull_BoxInside:
		{
			// Cull if inside the box
			float3 r = abs(pos.xyz - Cull[0].xyz) - Cull[1].xyz;
			return any(r >= 0) ? pos : CulledPosition;
		}
		default:
		{
			return pos;
		}
	}
}

// Boundary force profile
inline float BoundaryForceProfile(float normalised_distance)
{
	// -x + 1
	float x = clamp(normalised_distance, 0.0f, 1.0f);
	return 1.0f - x;
}

// The influence at normalised 'distance' from a particle
inline float ForceProfile(float normalised_distance, uniform float amplitude, uniform float balance)
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

// Evolves the particles forward in time while resolving collisions
[numthreads(ThreadGroupSize, 1, 1)]
void Integrate(int3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= NumParticles)
		return;

	Particle target = m_particles[dtid.x];
	
	// Integrate velocity. Assume the acceleration is constant over the time step.
	// When collisions change the velocity direction, the acceleration shouldn't also change direction.
	float dt = TimeStep;
	float4 acc = float4(target.accel, 0);
	float4 vel0 = target.vel;
	float4 vel1 = vel0 + acc * dt;

	// Repeat until ray consumed
	for (int attempt = 0; ; ++attempt)
	{	
		// The ray to the next position of the particle
		float4 ray = vel1 * dt;

		// Find the nearest intercept
		float t = 1.0f;
		int intercept_found = 0;
		float4 normal = float4(0, 0, 0, 0);
		for (int i = 0; i != NumPrimitives; ++i)
			intercept_found |= Intercept_RayVsPrimitive(target.pos, ray, m_collision[i], normal, t);

		// Stop if no intercept found or too many collisions
		bool done = !intercept_found || attempt == MaxCollisionResolutionSteps;
		if (WaveActiveAllTrue(done) || done)
		{
			target.pos += ray * t;
			target.vel = attempt != MaxCollisionResolutionSteps ? vel1 : float4(0,0,0,0);
			//target.col = attempt != MaxCollisionResolutionSteps ? float4(0,1,0,1) : float4(1,0,1,1);
			break;
		}
		
		// Constrain to 2D
		normal.z = select(SpatialDimensions == 3, normal.z, 0);
		
		// Advance the point to the intercept and recalculate the velocity
		target.pos += ray * t;

		// Recalculate vel0 at the collision point
		vel0 = vel0 + acc * dt * t;
		
		// Calculate the remaining time
		dt = dt * (1 - t);

		// Reflect the velocity + apply restitution *before* applying acceleration to find vel1.
		// Imagine the particle starting from the intercept with the reflected velocity.
		// Be careful not to add the acceleration first and then reflect it, accel should be reflected.
		float4 vel_n = -dot(vel0, normal) * normal;
		float4 vel_t = vel0 + vel_n;
		vel0 = vel_n * Restitution.x + vel_t * Restitution.y;
		vel1 = vel0 + acc * dt;

		// If the particle does not bounce sufficiently to escape the surface, then zero the normal
		// velocity and apply a repelling force equal and opposite to the current normal force.
		if (dot(vel1, normal) < 0)
		{
			// Remove the normal component of the velocity
			vel0 = vel0 - dot(vel0, normal) * normal;
			vel1 = vel1 - dot(vel1, normal) * normal;

			// Remove the normal component of the acceleration
			acc = acc - dot(acc, normal) * normal;
		}
	}
	target.pos = CullCheck(target.pos, Flags & Cull_Mask);

	// Update the particle dynamics
	m_particles[dtid.x].pos = target.pos;
	m_particles[dtid.x].col = target.col;
	m_particles[dtid.x].vel = target.vel;
	m_particles[dtid.x].accel = float3(0,0,0);
}

// Apply a force from surfaces to reduce collisions for particles at rest.
// Typically run this after applying forces to particles but before running 'Integrate'
[numthreads(ThreadGroupSize, 1, 1)]
void RestingContact(int3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= NumParticles)
		return;
	
	Particle target = m_particles[dtid.x];

#if 1
#if 0
	// Only slow-moving particles qualify for resting contact.
	float4 vel1 = target.vel + TimeStep * float4(target.accel, 0);
	bool too_fast = dot(vel1, vel1) * sqr(TimeStep) > sqr(BoundaryThickness);
	if (WaveActiveAllTrue(too_fast) || too_fast)
		return;
#endif
	
	for (int i = 0; i != NumPrimitives; ++i)
	{
		float4 normal;
	
		// Find the distance to each boundary surface
		float4 range = target.pos - ClosestPoint_PointToPrimitive(target.pos, m_collision[i], normal);

		// Constrain for 2D
		normal.z = select(SpatialDimensions == 3, normal.z, 0);
		
		// If the particle:
		// Has a starting position within the BoundaryThickness
		float range_sq = dot(range, range);
		bool within_boundary = range_sq < sqr(BoundaryThickness);
		if (WaveActiveAllTrue(!within_boundary) || !within_boundary)
			continue;

		// Has an acceleration that is anti-parallel to the surface normal (for vertical walls),
		float accel_n = dot(normal.xyz, target.accel);
		bool is_anti_parallel = accel_n < 0 && sqr(accel_n) > dot(target.accel, target.accel) * 0.95f;
		if (WaveActiveAllTrue(!is_anti_parallel) || !is_anti_parallel)
			continue;
		
		// Has a velocity that will not take it out of the boundary thickness,
		float dist_n = dot(target.vel, normal) * TimeStep;
		bool moving_within_boundary = dist_n < 0 && -dist_n < BoundaryThickness;
		if (WaveActiveAllTrue(!moving_within_boundary) || !moving_within_boundary)
			continue;

		float nomalised_distance = sqrt(range_sq) / BoundaryThickness;
		float force = ForceProfile(nomalised_distance, 1, 1);
		target.accel += (BoundaryForce * target.density * force) * normal.xyz;
		
		// Then cancel the acceleration into the surface.
		//float attenuation = BoundaryForceProfile(nomalised_distance);
		//target.accel -= attenuation * accel_n * normal.xyz;

		// Increase the mass of particles near the boundary
		//target.mass += sqr(attenuation);
		
		// Cancel the velocity into the surface as well
		//float vel_n = dot(target.vel, normal);
		//target.vel -= attenuation * vel_n * normal;
		//target.vel *= Restitution.y;
	}

	m_particles[dtid.x].accel = target.accel;
	m_particles[dtid.x].vel   = target.vel;

#endif
	
	
	#if 0
	// Find the distance to each boundary surface
	for (int i = 0; i != NumPrimitives; ++i)
	{
		float4 normal;
		float4 range = target.pos - ClosestPoint_PointToPrimitive(target.pos, m_collision[i], normal);
		float range_sq = dot(range, range);
		bool within_boundary = range_sq > sqr(BoundaryThickness);
		if (WaveActiveAllTrue(within_boundary) || within_boundary)
			continue;
		
		// Constrain for 2D
		normal.z = select(SpatialDimensions == 3, normal.z, 0);

		// Check the particle has an acceleration that is anti-parallel to the surface normal (for vertical walls),
		float accel_n = dot(normal.xyz, target.accel);
		bool is_anti_parallel = accel_n > 0 || sqr(accel_n) < dot(target.accel, target.accel) * 0.005f;
		if (WaveActiveAllTrue(is_anti_parallel) || is_anti_parallel)
			continue;		

		float amount = BoundaryForceProfile(sqrt(range_sq) / BoundaryThickness);
		target.accel -= amount * dot(target.accel, normal.xyz) * normal.xyz;
		
		// Apply a boundary force
		//float force = ForceProfile(sqrt(range_sq) / BoundaryThickness, 1, 1);
		//target.accel += 100 * force * normal.xyz;
		
		//target.vel -= amount * target.vel; // Static friction at the boundary surface
		//float vel_n = dot(target.vel, normal);
		//target.vel -= amount * vel_n * normal;
		//target.vel *= Restitution.y;
		
	}
	
	m_particles[dtid.x].accel = target.accel;
	m_particles[dtid.x].vel   = target.vel;
	#endif

}

