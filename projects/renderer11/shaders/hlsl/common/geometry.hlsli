//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************

#include "vector.hlsli"

// Returns the parametric value of the closest point on 's -> s+d' to 'pt'
// The closest point is at: s + t*d;
float ClosestPoint_PointVsRay(float4 pt, float4 s, float4 d)
{
    return dot(pt - s, d) / dot(d,d);
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
		triple(dir, sc, sb),
		triple(dir, sa, sc),
		triple(dir, sb, sa),
		0);

	// Compute the barycentric coordinates (u, v, w) determining the
	// intersection point u*a + v*b + w*c. Note: If the line lies
	// in the plane of the triangle then 'sum' will be zero
	float sum = dot(bary, float4(1,1,1,1));
	sum = (abs(sum) > 0.0001) ? (1 / sum) : 0;
	return bary * sum;
}