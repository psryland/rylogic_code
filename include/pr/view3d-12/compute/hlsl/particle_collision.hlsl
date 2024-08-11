// Particle collision resolution
#ifndef POS_TYPE
#define POS_TYPE struct PosType { float4 pos; float4 vel; float3 accel; float density; }
#endif

static const int MaxCollisionResolutionSteps = 10;
static const uint PosCountDimension = 1024;
static const uint Prim_Plane = 0;
static const uint Prim_Sphere = 1;
static const uint Prim_Triangle = 2;

POS_TYPE;

struct Prim
{
	// Note: Alignment matters. These are an array in 'm_collision' so need to be a multiple of 16 bytes.
	// Primitive data:
	//    Plane: data[0].xyz = normal, data[0].w = distance (positive if the normal faces the origin, expects normalised xyz)
	//   Sphere: data[0].xyz = center, data[0].w = radius
	// Triangle: data[0..2] = corners
	float4 data[3];

	// flag.x = primitive type
	uint4 flags;
};

cbuffer cbCollision : register(b0)
{
	// Note: Alignment matters. float2 must be aligned to 8 bytes.
	int NumParticles; // The number of particles
	int NumPrimitives; // The number of primitives
	float2 Restitution; // The coefficient of restitution (normal, tangential)
	float TimeStep; // The time to advance each particle by
};

// The positions/velocities of each particle
RWStructuredBuffer<PosType> m_particles : register(u0);

// The primitives to collide against
RWStructuredBuffer<Prim> m_collision : register(u1);

// The missing 'square' function
inline float sqr(float x)
{
	return x * x;
}

// Return the barycentric coordinates for 'point' with respect to triangle a,b,c
inline float4 Barycentric(float4 pos, float4 tri[3])
{
	float4 ab = tri[1] - tri[0];
	float4 ac = tri[2] - tri[0];
	float4 ap = pos - tri[0];

	float d00 = dot(ab, ab);
	float d01 = dot(ab, ac);
	float d11 = dot(ac, ac);
	float d20 = dot(ap, ab);
	float d21 = dot(ap, ac);

	// If 'denom' == 0, the triangle has no area
	// Return an invalid coordinate to signal this.
	float denom = d00 * d11 - d01 * d01;
	if (denom == 0)
		return float4(0, 0, 0, 0);
	
	float4 bary;
	bary.y = (d11 * d20 - d01 * d21) / denom;
	bary.z = (d00 * d21 - d01 * d20) / denom;
	bary.x = 1.0f - bary.y - bary.z;
	bary.w = 0.0f;
	return bary;
}

// Intersects a ray with a plane, returning true if their is an intercept
inline bool InterceptRayVsPlane(float4 pos, float4 ray, float4 plane, inout float t1, inout float4 normal)
{
	// 'step' is the length of the projection of 'ray' on the normal
	// Is the ray moving away from the triangle?
	float step = dot(ray, plane);
	if (step >= 0)
		return false;
	
	// 'dist' is the distance to the plane (scaled by |n|)
	// Unlike triangles, particles collide from any depth in the plane
	// Is the start point too far away to reach the triangle?
	float dist = dot(pos, plane);
	if (dist >= -step)
		return false;

	// 't' is the parametric value of the intercept point.
	// The condition above guarantees t on the interval [-inf,1)
	float t = dist > 0 ? -dist / step : 0;
	if (t >= t1)
		return false;

	// Return the intercept
	t1 = t;
	normal = float4(plane.xyz, 0);
	return true;
}

// Intersects a ray with a sphere, returning true if their is an intercept
inline bool InterceptRayVsSphere(float4 pos, float4 ray, float4 sphere, inout float t1, inout float4 normal)
{
	float4 p = pos - float4(sphere.xyz, 1);
	float4 q = ray;

	// The length of 'q' squared
	float q_sq = dot(q, q);
	if (q_sq == 0)
		return false;
	
	// Find the closest point on the infinite line through 'ray'
	float4 c = pos - q * dot(q, pos) / q_sq;
	float c_sq = dot(c, c);

	// If the closest point is not within the sphere then there is no intersection
	float radius_sq = sqr(sphere.w);
	if (radius_sq < c_sq)
		return false;

	// Get the distance from the closest point to the intersection with the boundary of the sphere
	float x = sqrt((radius_sq - c_sq) / q_sq); // includes the normalising 1/|q| in x

	// Get the parametric values of the intersection
	float4 half_chord = q * x;
	float4 hit_min = c - half_chord;
	float4 hit_max = c + half_chord;
	float tmin = dot(q, hit_min - pos) / q_sq;
	float tmax = dot(q, hit_max - pos) / q_sq;

	// Does the ray start within the sphere?
	// Is the intercept further than 't1'.
	if (tmin < 0 || tmin >= t1)
		return false;
	
	t1 = tmin;
	normal = normalize(hit_min - float4(sphere.xyz, 1));
	return true;
}

// Intersects a ray with a triangle, returning true if their is an intercept
inline bool InterceptRayVsTriangle(float4 pos, float4 ray, float4 tri[3], inout float t1, inout float4 normal)
{
	float4 e0 = tri[1] - tri[0];
	float4 e1 = tri[2] - tri[0];
	float4 p = pos - tri[0];
	float4 q = ray;
	
	// Triangle normal (not yet normalised)
	float4 n = float4(cross(e0.xyz, e1.xyz), 0);
	
	// 'step' is the length of the projection of 'ray' on the normal (scaled by |n|)
	// Is the ray moving away from the triangle?
	float step = dot(q, n);
	if (step >= 0)
		return false;
	
	// 'dist' is the distance to the plane of the triangle (scaled by |n|)
	// Does the ray start behind the triangle?
	// Is the start point too far away to reach the triangle?
	float dist = dot(p, n);
	if (dist < 0 || dist >= -step)
		return false;

	// 't' is the parametric value of the intercept point.
	// The condition above guarantees t on the interval [0,1)
	float t = -dist / step;
	if (t >= t1)
		return false;
	
	// Find the intercept point
	float4 h = pos + t * ray;

	// Compute barycentric coordinate components and test if within bounds.
	// If any coordinate is outside the interval [0,1], the intercept is not within the triangle.
	float4 bary = Barycentric(h, tri);
	if (any(bary) < 0 || any(bary) >= 1 || all(bary) == 0)
		return false;
	
	// Return the intercept
	t1 = t;
	normal = n / length(n);
	return true;
}

// Evolves the particles forward in time while resolving collisions
[numthreads(PosCountDimension, 1, 1)]
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

		// Advance the point to the intercept and recalculate the velocity
		target.pos += ray * t;

		// Stop if no intercept found or too many collisions
		if (!intercept_found || attempt == MaxCollisionResolutionSteps)
		{
			target.vel = attempt != MaxCollisionResolutionSteps ? vel1 : float4(0,0,0,0);
			break;
		}

		// Recalculate the velocity at the collision point
		vel1 = vel0 + acc * dt * t;
		
		// Calculate the remaining time
		dt = dt * (1 - t);

		// Reflect the velocity + apply restitution.
		// Imagine the particle starting from the intercept with the reflected velocity
		float4 vel_n = -dot(vel1, normal) * normal;
		float4 vel_t = vel1 + vel_n;
		vel0 = vel_n * Restitution.x + vel_t * Restitution.y;
		vel1 = vel0 + acc * dt;
	}
	
	if (gtid.x == NumParticles/2)
	{
	}
	// Update the particle dynamics
	m_particles[gtid.x].pos = target.pos;
	m_particles[gtid.x].vel = target.vel;
	m_particles[gtid.x].accel = float3(0, 0, 0);
}
