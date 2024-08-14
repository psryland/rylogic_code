// Particle collision resolution

static const int ThreadGroupSize = 1024;
static const int MaxCollisionResolutionSteps = 10;

#ifndef POS_TYPE
#define POS_TYPE struct PosType { float4 pos; float4 col; float4 vel; float3 accel; float density; }
#endif
POS_TYPE;

cbuffer cbCollision : register(b0)
{
	// Note: Alignment matters. float2 must be aligned to 8 bytes.
	int NumParticles; // The number of particles
	int NumPrimitives; // The number of primitives
	float2 Restitution; // The coefficient of restitution (normal, tangential)
	float ParticleRadius; // The radius of volume that each particle represents
	float TimeStep; // The time to advance each particle by
};

#include "geometry.hlsli"
#include "collision_primitives.hlsli"

// The positions/velocities of each particle
RWStructuredBuffer<PosType> m_particles : register(u0);

// The primitives to collide against
RWStructuredBuffer<Prim> m_collision : register(u1);

// Evolves the particles forward in time while resolving collisions
[numthreads(ThreadGroupSize, 1, 1)]
void Integrate(int3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;

	PosType target = m_particles[gtid.x];
	
	// Integrate velocity. Assume the acceleration is constant over the time step.
	// When collisions change the velocity direction, the acceleration shouldn't also change direction.
	float dt = TimeStep;
	float4 acc = float4(target.accel, 0);
	float4 vel0 = target.vel;
	float4 vel1 = vel0 + acc * dt;
	target.accel = float3(0,0,0);

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
		{
			Prim prim = m_collision[i];
			switch (prim.flags.x)
			{
				case Prim_Plane:
				{
					intercept_found |= InterceptRayVsPlane(target.pos, ray, prim.data[0], t, normal);
					break;
				}
				case Prim_Sphere:
				{
					intercept_found |= InterceptRayVsSphere(target.pos, ray, prim.data[0], t, normal);
					break;
				}
				case Prim_Triangle:
				{
					intercept_found |= InterceptRayVsTriangle(target.pos, ray, prim.data, t, normal);
					break;
				}
			}
		}

		// Stop if no intercept found or too many collisions
		if (!intercept_found || attempt == MaxCollisionResolutionSteps)
		{
			target.pos += ray * t;
			target.vel = attempt != MaxCollisionResolutionSteps ? vel1 : float4(0,0,0,0);
			//target.col = attempt != MaxCollisionResolutionSteps ? float4(0,0.6f,0,1) : float4(1,0,1,1);
			break;
		}
		
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

			// Set the "reset" acceleration to the -normal component of the current acceleration
			target.accel += -dot(acc, normal) * normal.xyz;

			// Remove the normal component of the acceleration
			acc = acc - dot(acc, normal) * normal;
		}
	}

	// Update the particle dynamics
	m_particles[gtid.x].pos = target.pos;
	m_particles[gtid.x].col = target.col;
	m_particles[gtid.x].vel = target.vel;
	m_particles[gtid.x].accel = target.accel;
}
