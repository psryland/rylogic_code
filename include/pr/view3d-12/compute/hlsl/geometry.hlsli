// Particle collision resolution
#pragma once
#include "utility.hlsli"

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

// Finds the normal(xyz) and distance(w) of a point to a plane
inline float4 DistancePointToPlane(float4 pos, float4 plane)
{
	return float4(plane.xyz, dot(pos, plane));
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

// Returns a spherical direction vector corresponding to the i'th point of a Fibonacci sphere
inline float4 FibonacciSpiral(int i, int N)
{
	// Z goes from -1 to +1
	// Using a half step bias so that there is no point at the poles.
	// This prevents degenerates during 'unmapping' and also results in more evenly
	// spaced points. See "Fibonacci grids: A novel approach to global modelling".
	float z = -1.0f + (2.0f * i + 1.0f) / N;

	// Radius at z
	float r = sqrt(1.0 - z * z);

	// Golden angle increment
	static const float GoldenAngle = 2.39996322972865332223f;
	float theta = i * GoldenAngle;
	
	float x = cos(theta) * r;
	float y = sin(theta) * r;
	return float4(x, y, z, 0);
}
