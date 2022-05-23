//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

#include "vector.hlsli"
#include "functions.hlsli"

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