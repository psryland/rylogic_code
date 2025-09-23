//***********************************************
// HLSL
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_HLSL_GEOMETRY_HLSLI
#define PR_HLSL_GEOMETRY_HLSLI

#include "pr/hlsl/core.hlsli"
#include "pr/hlsl/vector.hlsli"

// Return the normal for the triangle (a,b,c)
// Returns 'def' if the triangle is degenerate
float4 FaceNormal(float4 a, float4 b, float4 c, float4 def = float4(0,0,0,0))
{
	float3 n = cross((b - a).xyz, (c - b).xyz);
	float len = length(n);
	return len > 0 ? float4(n / len, 0) : def;
}

// Measure the coplanarity of the two triangles (a,b,c) and (a,c,d)
// Returns 1 for parallel normals, -1 for opposing normals.
// Returns 0 if either of the triangles are degenerate.
float Coplanarity(float4 a, float4 b, float4 c, float4 d)
{
	float4 n0 = FaceNormal(a, b, c);
	float4 n1 = FaceNormal(a, c, d);
	return dot(n0,n1);
}

// Returns the parametric value of the closest point on 's -> s+d' to 'pt'
// The closest point is at: s + t*d;
float ClosestPoint_PointVsRay(float4 pt, float4 s, float4 d)
{
    return dot(pt - s, d) / dot(d,d);
}

// Returns the parametric values of the closest point between a line segment 'p -> p+seg' to a ray 's -> s+ray'.
// The closest point on the line segment is at: p + return.x*seg;
// The closest point on the ray is at: s + return.y*ray;
float2 ClosestPoint_LineSegmentVsRay(float4 p, float4 seg, float4 s, float4 ray)
{
	float4 separation = p - s;
	float seq_lensq = dot(seg,seg);
	float ray_lensq = dot(ray,ray);
	float s_on_seg = -dot(separation, seg);
	float s_on_ray = +dot(separation, ray);

	// The general non-degenerate case starts here
	float b = dot(seg, ray);
	float denom = seq_lensq * ray_lensq - b * b; // Always non-negative

	// Check if the line segment is degenerate
	bool degenerate = seq_lensq == 0;

	// If segment is not parallel, calculate closest point on the infinite line through pt0 -> pt1
	// to the infinite line 's -> s+d', and clamp to the segment. Otherwise pick arbitrary t0.
	float t0 = !degenerate && denom != 0 ? saturate((b*s_on_ray + ray_lensq*s_on_seg) / denom) : 0;

	// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
	// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
	float t1 = !degenerate ? ((b*t0 + s_on_ray) / ray_lensq) : (s_on_ray / ray_lensq);

	return float2(t0,t1);
}

// Calculate the barycentric coordinates for the intersection of a
// line passing through 'pt -> pt+dir' and a triangle 'a-b-c'.
// Returns (0,0,0,0) if the ray is in the plane of 'a-b-c'.
// The ray intersects the triangle if all components of the returned vector are >= 0
//    i.e. if (!any(abs(r) - r)) { ...intersect... }
// The intersection is at: a*r.x + b*r.y + c*r.z
float4 Intersect_RayVsTriangle(float4 pt, float4 dir, float4 a, float4 b, float4 c)
{
	float4 sa = a - pt;
	float4 sb = b - pt;
	float4 sc = c - pt;

	// Test if 'dir' is on or inside the edges ab, bc, and ca.
	// Done by testing that the signed tetrahedral volumes are all positive.
	float4 bary = float4(
		Triple(dir, sc, sb),
		Triple(dir, sa, sc),
		Triple(dir, sb, sa),
		0);

	// Compute the barycentric coordinates (u, v, w) determining the
	// intersection point u*a + v*b + w*c. Note: If the line lies
	// in the plane of the triangle then 'sum' will be zero
	float sum = SumComponents(bary);
	sum = (abs(sum) > 0.0001) ? (1 / sum) : 0;
	return bary * sum;
}

// Returns the parametric value 't' of the intersection between the line
// passing through 's' and 'e' with 'frust'.
// Assumes 's' is within the frustum to start with.
float Intersect_RayVsFrustum(float4x4 frust, float4 s, float4 e)
{
	const float4 T = 1e10f;
	
	// Find the distance from each frustum face for 's' and 'e'
	float4 d0 = mul(s, frust);
	float4 d1 = mul(e, frust);

	// Clip the edge 's-e' to each of the frustum sides (Actually, find the parametric
	// value of the intercept)(min(T,..) protects against divide by zero)
	float4 t0 = step(d1,d0)   * min(T, -d0/(d1 - d0));        // Clip to the frustum sides
	float  t1 = step(e.z,s.z) * min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane

	// Set all components that are <= 0.0 to BIG
	t0 += step(t0, float4(0,0,0,0)) * T;
	t1 += step(t1, 0.0f) * T.x;

	// Find the smallest positive parametric value
	// => the closest intercept with a frustum plane
	float t = T.x;
	t = min(t, t0.x);
	t = min(t, t0.y);
	t = min(t, t0.z);
	t = min(t, t0.w);
	t = min(t, t1);
	return t;
}

// Return a point that is the weighted result of verts 'a','b','c' and 'bary'
inline float2 BaryPoint(float2 a, float2 b, float2 c, float4 bary)
{
	float4 pt = bary.x * float4(a,0,1) + bary.y * float4(b,0,1) + bary.z * float4(c,0,1);
	return pt.xy / pt.w;
}
inline float4 BaryPoint(float4 a, float4 b, float4 c, float4 bary)
{
	float4 pt = bary.x * a + bary.y * b + bary.z * c;
	return pt / pt.w;
}

// Return the barycentric coordinates for 'point' with respect to triangle a,b,c
inline float4 Barycentric(float2 pos, float2 a, float2 b, float2 c)
{
	float2 ab = b - a;
	float2 ac = c - a;
	float2 ap = pos - a;

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
inline float4 Barycentric(float4 pos, float4 a, float4 b, float4 c)
{
	float4 ab = b - a;
	float4 ac = c - a;
	float4 ap = pos - a;

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

#endif





#if 0 // World space functions

// Finds the closest point on a plane to 'pos'. Returns the normal at the closest point
inline float4 ClosestPoint_PointToPlane(float4 pos, float4 plane)
{
	float dist = dot(pos, plane);
	return pos - dist * plane;
}
inline float4 ClosestPoint_PointToPlane(float4 pos, float4 plane, out float4 normal)
{
	normal = float4(plane.xyz, 0);
	return ClosestPoint_PointToPlane(pos, plane);
}

// Finds the closest point on a sphere to 'pos'
inline float4 ClosestPoint_PointToSphere(float4 pos, float4 sphere)
{
	float4 normal;
	return ClosestPoint_PointToSphere(pos, sphere, normal);
}
inline float4 ClosestPoint_PointToSphere(float4 pos, float4 sphere, out float4 normal)
{
	float4 ray = pos - float4(sphere.xyz, 1);
	float dist_sq = sqr(ray);
	
	normal = select(dist_sq != 0, ray / sqrt(dist_sq), float4(1, 0, 0, 0));
	return float4(sphere.xyz + ray.xyz * sphere.w, 1);
}

// Finds the closest point on a triangle to 'pos'
inline float4 ClosestPoint_PointToTriangle(float4 pos, float4 tri[3])
{
	float4 bary;
	return ClosestPoint_PointToTriangle(pos, tri, bary);
}
inline float4 ClosestPoint_PointToTriangle(float4 pos, float4 tri[3], out float4 bary)
{
	float4 ab = tri[1] - tri[0];
	float4 ac = tri[2] - tri[0];
	float4 ap = pos - tri[0];
	float4 bp = pos - tri[1];
	float4 cp = pos - tri[2];
	float d1 = dot(ab, ap);
	float d2 = dot(ac, ap);
	float d3 = dot(ab, bp);
	float d4 = dot(ac, bp);
	float d5 = dot(ab, cp);
	float d6 = dot(ac, cp);
	float vc = d1*d4 - d3*d2;
	float vb = d5*d2 - d1*d6;
	float va = d3*d6 - d5*d4;

	// Check if P in vertex region outside A
	if (d1 <= 0.0f && d2 <= 0.0f)
	{
		bary = float4(1.0f, 0.0f, 0.0f, 0.0f);
		return tri[0];
	}

	// Check if P in vertex region outside B
	if (d3 >= 0.0f && d4 <= d3)
	{
		bary = float4(0.0f, 1.0f, 0.0f, 0.0f);
		return tri[1];
	}

	// Check if P in edge region of AB, if so return projection of P onto AB
	if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
	{
		// Barycentric coordinates (1-v, v, 0)
		float v = d1 / (d1 - d3);
		bary = float4(1.0f - v, v, 0.0f, 0.0f);
		return tri[0] + v * ab;
	}

	// Check if P in vertex region outside C
	if (d6 >= 0.0f && d5 <= d6)
	{
		bary = float4(0.0f, 0.0f, 1.0f, 0.0f);
		return tri[2];
	}

	// Check if P in edge region of AC, if so return projection of P onto AC
	if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
	{
		// Barycentric coordinates (1-w, 0, w)
		float w = d2 / (d2 - d6);
		bary = float4(1.0f - w, 0.0f, w, 0.0f);
		return tri[0] + w * ac;
	}

	// Check if P in edge region of BC, if so return projection of P onto BC
	if (va <= 0.0f && d4 - d3 >= 0.0f && d5 - d6 >= 0.0f)
	{
		// Barycentric coordinates (0, 1-w, w)
		float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
		bary = float4(0.0f, 1.0f - w, w, 0.0f);
		return tri[1] + w * (tri[2] - tri[1]);
	}

	// P inside face region. Compute Q through its barycentric coordinates (u,v,w)
	//'  = u*a + v*b + w*c, u = va * denom = 1.0f - v - w
	float denom = 1.0f / (va + vb + vc);
	float v = vb * denom;
	float w = vc * denom;
	bary = float4(1.0f - v - w, v, w, 0.0f);
	return tri[0] + ab * v + ac * w;
}
inline float4 ClosestPoint_PointToTriangle(float4 pos, float4 tri[3], out float4 bary, out float4 normal)
{
	float4 pt = ClosestPoint_PointToTriangle(pos, tri, bary);
	float4 ray = pos - pt;
	float dist = length(ray);
	normal = dist != 0 ? ray / dist : float4(normalize(cross((tri[1] - tri[0]).xyz, (tri[2] - tri[0]).xyz)), 0);
	return pt;
}


// Intersects a ray with a plane, returning true if there is an intercept.
// 'plane.xyz' is the (normalised) plane normal, 'plane.w' is the signed distance to the origin from the plane
inline bool Intercept_RayVsPlane(float4 pos, float4 ray, float4 plane, inout float t1, inout float4 normal)
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

// Intersects a ray with a sphere, returning true if there is an intercept
// 'sphere.xyz' is the center of the sphere, 'sphere.w' is the radius
inline bool Intercept_RayVsSphere(float4 pos, float4 ray, float4 sphere, inout float t1, inout float4 normal)
{
	float4 p = pos - float4(sphere.xyz, 1);
	float4 q = ray;

	// The length of 'q' squared
	float q_sq = sqr(q);
	if (q_sq == 0)
		return false;
	
	// Find the closest point on the infinite line through 'ray'
	float4 c = pos - q * dot(q, pos) / q_sq;
	float c_sq = sqr(c);

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


// Intersects a ray with a triangle, returning true if there is an intercept
inline bool Intercept_RayVsTriangle(float4 pos, float4 ray, float4 tri[3], inout float t1, inout float4 normal)
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

// This needs moving to where 'm_proj_tex_count' is defined
float4 ProjTex(float4 ws_pos, float4 in_diff)
{
	float4 out_diff = in_diff;
	for (int i = 0; i < m_proj_tex_count.x; i += 1.0)
	{
		// Project the world space position into projected texture coords
		float2 pt_pos = mul(ws_pos, m_proj_tex[i]).xy;
		float2 pt_pos_sat = saturate(pt_pos);
		float4 diff = m_proj_texture[i].Sample(m_proj_sampler[i], pt_pos_sat);
		if (pt_pos_sat.x == pt_pos.x && pt_pos_sat.y == pt_pos.y)
			out_diff = diff;
	}
	return out_diff;
}

#endif

