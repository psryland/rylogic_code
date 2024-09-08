//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
static const int ThreadGroupSize = 1024;
static const int MaxCollisionResolutionSteps = 10;

cbuffer cbCollision : register(b0)
{
	struct
	{
		// Note: Alignment matters. float2 must be aligned to 8 bytes.
		int NumParticles; // The number of particles
		int NumPrimitives; // The number of primitives
		int SpatialDimensions; // The number of spatial dimensions
		float TimeStep; // The time to advance each particle by
	
		float ParticleRadius; // The radius of volume that each particle represents
		float BoundaryThickness; // The thickness in with boundary effects are applied
		float BoundaryForce; // The repulsive force of the boundary
		float pad;

		float2 Restitution; // The coefficient of restitution (normal, tangential)
	} Sim;
};
cbuffer cbCull : register(b1)
{
	struct
	{
		float4 Geom[2]; // A plane, sphere, etc used to cull particles (set their positions to NaN)
		int Flags; // [3:0] = 0: No culling, 1: Sphere, 2: Plane, 3: Box
	} Cull;
};

#include "collision.hlsli"
#include "particle.hlsli"

static const int Cull_None = 0;         // Cull no used
static const int Cull_Plane = 1;        // Cull[0].xyz = normal, Cull[0].w = distance
static const int Cull_Sphere = 2;       // Cull[0].xyz = centre, Cull[0].w = radius
static const int Cull_SphereInside = 3; // Cull[0].xyz = centre, Cull[0].w = radius
static const int Cull_Box = 4;          // Cull[0].xyz = centre, Cull[1].xyz = half-extents
static const int Cull_BoxInside = 5;    // Cull[0].xyz = centre, Cull[1].xyz = half-extents
static const int Cull_Mask = 0x07;
static const float4 CulledPosition = float4(0,0,0,0)/0;

// The positions/colours of each particle
RWStructuredBuffer<PositionType> m_positions : register(u0);

// The dynamics for each particle
RWStructuredBuffer<DynamicsType> m_dynamics : register(u1);

// The primitives to collide against
StructuredBuffer<Prim> m_collision : register(t0);

// Return a particle from the particle and dynamics buffers
inline Particle GetParticle(int i)
{
	Particle part;
	part.pos = float4(m_positions[i].pos.xyz, 1);
	part.vel = float4(m_dynamics[i].vel.xyz, 0);
	part.acc = float4(m_dynamics[i].accel.xyz, 0);
	part.surface = m_dynamics[i].surface;
	part.colour = m_positions[i].col;
	part.density = m_dynamics[i].density;
	part.flags = m_dynamics[i].flags;
	return part;
}

// Boundary force profile
inline float BoundaryForceProfile(float normalised_distance)
{
	// -x + 1
	float x = clamp(normalised_distance, 0.0f, 1.0f);
	return 1.0f - x;
}

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
			return dot(pos, Cull.Geom[0]) >= 0 ? pos : CulledPosition;
		}
		case Cull_Sphere:
		{
			// Cull if outside the sphere
			float r_sq = sqr(Cull.Geom[0].w);
			float3 r = pos.xyz - Cull.Geom[0].xyz;
			return dot(r,r) <= r_sq ? pos : CulledPosition;
		}
		case Cull_SphereInside:
		{
			// Cull if inside the sphere
			float r_sq = sqr(Cull.Geom[0].w);
			float3 r = pos.xyz - Cull.Geom[0].xyz;
			return dot(r,r) >= r_sq ? pos : CulledPosition;
		}
		case Cull_Box:
		{
			// Cull if outside the box
			float3 r = abs(pos.xyz - Cull.Geom[0].xyz) - Cull.Geom[1].xyz;
			return all(r <= 0) ? pos : CulledPosition;
		}
		case Cull_BoxInside:
		{
			// Cull if inside the box
			float3 r = abs(pos.xyz - Cull.Geom[0].xyz) - Cull.Geom[1].xyz;
			return any(r >= 0) ? pos : CulledPosition;
		}
		default:
		{
			return pos;
		}
	}
}

// Evolves the particles forward in time while resolving collisions
[numthreads(ThreadGroupSize, 1, 1)]
void Integrate(int3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Sim.NumParticles)
		return;

	Particle target = GetParticle(dtid.x);
	
	float dt = Sim.TimeStep;
	float4 acc = target.acc;
	float4 vel0 = target.vel;
	float4 vel1 = vel0 + acc * dt;
	target.acc = float4(0,0,0,0);

	// Integrate position and velocity and resolve collisions.
	// Assume the acceleration is constant over the time step.
	// The ApplyForces function should calculate an acceleration that represents the average for the time step.
	// When collisions change the velocity direction, the acceleration shouldn't also change direction.
	for (int attempt = 0; ; ++attempt)
	{	
		// The ray to the next position of the particle
		float4 ray = vel1 * dt;

		// Find the nearest intercept
		float t = 1.0f;
		int intercept_found = 0;
		float4 normal = float4(0, 0, 0, 0);
		for (int i = 0; i != Sim.NumPrimitives; ++i)
			intercept_found |= Intercept_RayVsPrimitive(target.pos, ray, m_collision[i], normal, t);

		// Stop if no intercept found or too many collisions
		bool done = !intercept_found || attempt == MaxCollisionResolutionSteps;
		if (WaveActiveAllTrue(done) || done)
		{
			target.pos += ray * t;
			target.vel = select(attempt != MaxCollisionResolutionSteps, vel1, float4(0,0,0,0));
			if (attempt == MaxCollisionResolutionSteps)
				target.colour = float4(1,0,1,1);
			break;
		}
		
		// Constrain to 2D
		normal.z = select(Sim.SpatialDimensions == 3, normal.z, 0);
		
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
		vel0 = vel_n * Sim.Restitution.x + vel_t * Sim.Restitution.y;
		vel1 = vel0 + acc * dt;

		// If the particle does not bounce sufficiently to escape the surface, then zero the normal
		// velocity and apply a repelling force equal and opposite to the current normal force.
		if (dot(vel1, normal) < 0)
		{
			// Remove the normal component of the velocity
			vel0 -= dot(vel0, normal) * normal;
			vel1 -= dot(vel1, normal) * normal;

			// Remove the normal component of the acceleration
			acc -= dot(acc, normal) * normal;

			// Use for testing boundaries
			// target.colour = float4(0,1,0,1);
		}
	}

	// Measure proximity to the nearest boundary
	float4 blended_normal = float4(0,0,0,0);
	float blended_distance = Sim.ParticleRadius;
	for (int i = 0; i != Sim.NumPrimitives; ++i)
	{
		float4 normal;
		float4 range = target.pos - ClosestPoint_PointToPrimitive(target.pos, m_collision[i], normal);
		float distance = length(range);
		
		blended_normal += clamp(1 - distance / Sim.ParticleRadius, 0, 1) * normal;
		blended_distance = min(blended_distance, distance);
	}

	// Constrain to 2D
	target.surface = select(any(blended_normal), float4(normalize(blended_normal).xyz, blended_distance), float4(0,0,0,Sim.ParticleRadius));
	target.surface.z = select(Sim.SpatialDimensions == 3, target.surface.z, 0);

	// Delete dead particles
	target.pos = CullCheck(target.pos, Cull.Flags & Cull_Mask);

	// Update the particle dynamics
	m_positions[dtid.x].pos.xyz = target.pos.xyz;
	m_positions[dtid.x].col = target.colour;
	m_dynamics[dtid.x].vel.xyz = target.vel.xyz;
	m_dynamics[dtid.x].accel.xyz = target.acc.xyz;
	m_dynamics[dtid.x].surface = target.surface;
	m_dynamics[dtid.x].flags = target.flags;
}

// Apply a force from surfaces to reduce collisions for particles at rest.
// Typically run this after applying forces to particles but before running 'Integrate'
[numthreads(ThreadGroupSize, 1, 1)]
void RestingContact(int3 dtid : SV_DispatchThreadID)
{
	if (dtid.x >= Sim.NumParticles)
		return;
	
	Particle target = GetParticle(dtid.x);
	float4 normal = float4(target.surface.xyz, 0);

	// Only slow-moving particles near boundaries qualify for resting contact.
	// Don't don't use anti-parallel velocity/accel as a test, that causes discontinuous forces.
	float4 vel1 = target.vel + Sim.TimeStep * target.acc;
	//float vel_n = dot(vel1, normal) * normal;
	//float vel_t = vel1 - vel_n;
	bool within_boundary = target.surface.w < Sim.BoundaryThickness; // Starting within the boundary
	bool slow_enough = length_sq(vel1) < sqr(Sim.BoundaryThickness); // Not going to move more than the boundary thickness in 1 sec.
	bool at_rest = within_boundary && slow_enough;
	if (WaveActiveAllTrue(!at_rest) || !at_rest)
		return;

	// Resting forces:
	// - We need the force to be smooth and continuous or particles get kicked.
	// - Particles within 'TINY' of the boundary have zero acceleration into the boundary.
	// - Particles at the BoundaryThickness range have no adjustment to their acceleration.
	float4 accel_n = dot(target.acc, normal) * normal;
	target.acc -= Sim.BoundaryForce * BoundaryForceProfile(target.surface.w / Sim.BoundaryThickness) * accel_n;

	// Apply tangential restitution (Assume vel_n is near zero already)
	target.vel *= Sim.Restitution.y * (abs(target.vel) > TINY);

	// Update the particle dynamics
	m_dynamics[dtid.x].vel.xyz = target.vel.xyz;
	m_dynamics[dtid.x].accel.xyz = target.acc.xyz;
}
	
	
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
	
	m_positions[dtid.x].accel = target.accel;
	m_positions[dtid.x].vel   = target.vel;
	#endif


