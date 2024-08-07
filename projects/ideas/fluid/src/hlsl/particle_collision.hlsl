// Particle collision resolution

static const uint PosCountDimension = 1024;
static const uint PrimMask = (1 << 3) - 1;
static const uint Prim_Plane = 0;
static const uint Prim_Sphere = 1;
static const uint Prim_Triangle = 2;

struct Vert
{
	float4 pos;
	float4 col;
	float4 vel;
	float3 accel;
	float density;
};

struct Prim
{
	// [0:3] = primitive type
	uint flags;

	// Primitive data:
	//  Plane: data[0] = normal, data[1].x = distance (positive if the normal faces the origin)
	//  Sphere: data[0] = center, data[1].x = radius
	//  Triangle: data = {a, b, c}
	float3 data[3];
};

cbuffer cbCollision : register(b0)
{
	float TimeStep; // The time to advance each particle by
	uint NumParticles; // The number of particles
	uint NumPrimitives; // The number of primitives
	float2 Restitution; // The coefficient of restitution (normal, tangential)
};

// The positions/velocities of each particle
RWStructuredBuffer<Vert> m_particles : register(u0);

// The primitives to collide against
RWStructuredBuffer<Prim> m_collision : register(u1);

// Return the barycentric coordinates for 'point' with respect to triangle a,b,c
inline float3 Barycentric(float3 pos, float3 tri[3])
{
	float3 ab = tri[1] - tri[0];
	float3 ac = tri[2] - tri[0];
	float3 ap = pos - tri[0];

	float d00 = dot(ab, ab);
	float d01 = dot(ab, ac);
	float d11 = dot(ac, ac);
	float d20 = dot(ap, ab);
	float d21 = dot(ap, ac);

	// If 'denom' == 0, the triangle has no area
	// Return an invalid coordinate to signal this.
	float denom = d00 * d11 - d01 * d01;
	if (denom == 0)
		return float3(0, 0, 0);
	
	float3 bary;
	bary.y = (d11 * d20 - d01 * d21) / denom;
	bary.z = (d00 * d21 - d01 * d20) / denom;
	bary.x = 1.0f - bary.y - bary.z;
	return bary;
}

// Intersects a ray with a plane, returning true if their is an intercept
inline bool InterceptRayVsPlane(float3 pos, float3 ray, inout float t1, float3 plane_normal, float distance, out float3 normal, out float3 hit)
{
	// The plane normal (expect normalised)
	float3 n = plane_normal;
	
	// 'step' is the length of the projection of 'ray' on the normal
	// Is the ray moving away from the triangle?
	float step = dot(ray, n);
	if (step >= 0)
		return false;
	
	// 'dist' is the distance to the plane (scaled by |n|)
	// Does the ray start behind the triangle?
	// Is the start point too far away to reach the triangle?
	float dist = dot(pos, n) + distance;
	if (dist < 0 || dist >= -step)
		return false;

	// 't' is the parametric value of the intercept point.
	// The condition above guarantees t on the interval [0,1)
	float t = -dist / step;
	if (t >= t1)
		return false;
	
	// Find the intercept point
	float3 h = pos + t * ray;
	
	// Return the intercept
	normal = n;
	hit = h;
	return true;
}

// Intersects a ray with a sphere, returning true if their is an intercept
inline bool InterceptRayVsSphere(float3 pos, float3 ray, inout float t1, float3 centre, float radius, out float3 normal, out float3 hit)
{
	float3 p = pos - centre;
	float3 q = ray;

	// The length of 'q' squared
	float q_sq = dot(q, q);
	if (q_sq == 0)
		return false;
	
	// Find the closest point on the infinite line through 'ray'
	float3 c = pos - q * dot(q, pos) / q_sq;
	float c_sq = dot(c, c);

	// If the closest point is not within the sphere then there is no intersection
	float radius_sq = radius * radius;
	if (radius_sq < c_sq)
		return false;

	// Get the distance from the closest point to the intersection with the boundary of the sphere
	float x = sqrt((radius_sq - c_sq) / q_sq); // includes the normalising 1/|q| in x

	// Get the parametric values of the intersection
	float3 half_chord = q * x;
	float3 hit_min = c - half_chord;
	float3 hit_max = c + half_chord;
	float tmin = dot(q, hit_min - pos) / q_sq;
	float tmax = dot(q, hit_max - pos) / q_sq;

	// Does the ray start within the sphere?
	// Is the intercept further than 't1'.
	if (tmin < 0 || tmin >= t1)
		return false;
	
	hit = pos + tmin * ray;
	normal = normalize(hit - centre);
	return true;
}

// Intersects a ray with a triangle, returning true if their is an intercept
inline bool InterceptRayVsTriangle(float3 pos, float3 ray, inout float t1, float3 tri[3], out float3 normal, out float3 hit)
{
	float3 e0 = tri[1] - tri[0];
	float3 e1 = tri[2] - tri[0];
	float3 p = pos - tri[0];
	float3 q = ray;
	
	// Triangle normal (not yet normalised)
	float3 n = cross(e0, e1);
	
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
	float3 h = pos + t * ray;

	// Compute barycentric coordinate components and test if within bounds.
	// If any coordinate is outside the interval [0,1], the intercept is not within the triangle.
	float3 bary = Barycentric(h, tri);
	if (any(bary) < 0 || any(bary) >= 1 || all(bary) == 0)
		return false;
	
	// Return the intercept
	normal = n / length(n);
	hit = h;
	return true;
}

// Evolves the particles forward in time while resolving collisions
[numthreads(PosCountDimension, 1, 1)]
void Integrate(uint3 gtid : SV_DispatchThreadID, uint3 gid : SV_GroupID)
{
	if (gtid.x >= NumParticles)
		return;
	
	Vert target = m_particles[gtid.x];
	
	// Integrate velocity
	target.vel.xyz += target.accel * TimeStep;
	
	// The ray to the next position of the particle
	float3 ray = target.vel.xyz * TimeStep;
	
	// Repeat until ray consumed
	for (;;)
	{
		// Find the nearest intercept
		float t = 1.0f;
		float3 normal, hit;
		bool intercept_found = false;
		for (int i = 0; i != NumPrimitives; ++i)
		{
			Prim prim = m_collision[i];
			if ((prim.flags & PrimMask) == Prim_Plane)
			{
				intercept_found |= InterceptRayVsPlane(target.pos.xyz, ray, t, prim.data[0], prim.data[1].x, normal, hit);
			}
			if ((prim.flags & PrimMask) == Prim_Sphere)
			{
				intercept_found |= InterceptRayVsSphere(target.pos.xyz, ray, t, prim.data[0], prim.data[1].x, normal, hit);
			}
			if ((prim.flags & PrimMask) == Prim_Triangle)
			{
				intercept_found |= InterceptRayVsTriangle(target.pos.xyz, ray, t, prim.data, normal, hit);
			}
		}

		// Advance the point to the intercept
		target.pos.xyz += ray * t;
		ray = (1 - t) * ray;

		// Stop if no intercept found
		if (!intercept_found)
			break;

		// Get the normal and tangential part of 'ray' relative to the surface
		float3 ray_n = -dot(ray, normal) * normal;
		float3 ray_t = ray + ray_n;

		// Reflect the ray + apply restitution
		ray = ray_n * Restitution.x + ray_t * Restitution.y;

		// Reflect the velocity + apply restitution
		float3 vel_n = -dot(target.vel.xyz, normal) * normal;
		float3 vel_t = target.vel.xyz + vel_n;
		target.vel.xyz = vel_n * Restitution.x + vel_t * Restitution.y;
	}
}
