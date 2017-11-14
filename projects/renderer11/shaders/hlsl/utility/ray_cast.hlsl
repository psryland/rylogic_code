//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

// Perform a ray vs. model test on all verts, edges, faces in a model

// How it works:
//  When Snap-To Ray casting is wanted, a render step is added to the scene.
//  The render step, renders the drawlist again using the RayCast shaders below.
//  The shaders write the model id and location of the hit point into a 1D texture (managed by the render step)
//  The render step can then be queried to find the model that was hit


#include "../types.hlsli"
#include "../common/geometry.hlsli"
#include "ray_cast_cbuf.hlsli"

// Compute shader
#ifdef PR_RDR_CSHADER_ray_cast

// A single ray to cast
struct Ray
{
	float4 ws_origin;
	float4 ws_direction;
};

// The intercept between a ray and an object
struct Intercept
{
	float4 ws_intercept;
	float distance;
	int instance_id;
};

// The input buffer of rays to cast
StructuredBuffer<Ray> g_rays;

// The output buffer of intercepts
RWStructuredBuffer<Intercept> g_intercepts;

// Rays vs. Geometry
[numthreads(1, 1, 1)]
void main(CSIn addr)
{
	Intercept intercept;
	intercept.ws_intercept = float4(addr.thread_id.x/1024.0f, 0, 0, 1);
	intercept.distance = 1.0f;
	intercept.instance_id = addr.group_idx;
	g_intercepts[addr.thread_id.x] = intercept;
}

#endif













// Pixel shader input format
struct PSIn_RayCast
{
	float4 ws_vert :SV_Position; // gets divided by w and written to the depth buffer
	float4 intercept :Intercept;
};
struct PSOut
{
	float4 intercept :SV_Target;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_ray_cast
PSIn_RayCast main(VSIn In)
{
	PSIn_RayCast Out;

	// Transform the model verts to world space
	Out.ws_vert = mul(In.vert, m_o2w);
	Out.intercept = float4(0,0,0,0);

	return Out;
}
#endif

// Geometry shaders
#ifdef PR_RDR_GSHADER_ray_cast_face
[maxvertexcount(MAX_RAYS)]
void main(triangle PSIn_RayCast In[3], inout PointStream<PSIn_RayCast> OutStream)
{
	float4 v0 = In[0].ws_vert;
	float4 v1 = In[1].ws_vert;
	float4 v2 = In[2].ws_vert;
	float snap_dist = m_snap_dist;

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin = m_ws_ray_origin[i];
		float4 direction = m_ws_ray_direction[i];

		// Perform a ray vs. triangle intersection test
		float4 bary = Intersect_RayVsTriangle(origin, direction, v0, v1, v2);
		if (all_zero(bary))
			continue; // no intersection, co-planar
	
		// Find the intersection point
		float4 pt = v0*bary.x + v1*bary.y + v2*bary.z;
	
		// Apply 'snap'. Priority order is Vert, Edge, Face
		bool snapped = false;
		if (!snapped && HAS_VERT_SNAP)
		{
			float d0 = distance(pt, v0);
			float d1 = distance(pt, v1);
			float d2 = distance(pt, v2);
		
			// Snap to verts
			snapped = true;
			if      (abs(d0 - 1) < snap_dist) pt = v0;
			else if (abs(d1 - 1) < snap_dist) pt = v1;
			else if (abs(d2 - 1) < snap_dist) pt = v2;
			else snapped = false;
		}
		if (!snapped && HAS_EDGE_SNAP)
		{
			float4 e0 = v1 - v0;
			float4 e1 = v2 - v1;
			float4 e2 = v0 - v2;
		
			float4 p0 = v0 + saturate(ClosestPoint_PointVsRay(pt,v0,e0)) * e0;
			float4 p1 = v1 + saturate(ClosestPoint_PointVsRay(pt,v1,e1)) * e1;
			float4 p2 = v2 + saturate(ClosestPoint_PointVsRay(pt,v2,e2)) * e2;
		
			float d0 = distance(pt, p0);
			float d1 = distance(pt, p1);
			float d2 = distance(pt, p2);

			// Snap to edges
			snapped = true;
			if      (abs(d0) < snap_dist) pt = p0;
			else if (abs(d1) < snap_dist) pt = p1;
			else if (abs(d2) < snap_dist) pt = p2;
			else snapped = false;
		}
		if (!snapped && HAS_FACE_SNAP)
		{
			snapped = all_zero_or_positive(bary);
		}

		// If no intersect, drop the triangle
		if (!snapped)
			continue;

		// Get the distance from ray origin to intersect
		float dist = distance(origin, pt);
	
		// Output a "screen space" pixel at (i,0)
		PSIn_RayCast Out;
		Out.ws_vert = float4(0, 0, dist, m_ray_max_length);
		Out.intercept = float4(pt.xyz, m_flags.w + 0.5);
		OutStream.Append(Out);
		OutStream.RestartStrip();
		break;
	}
}
#endif

// Pixel shader
#ifdef PR_RDR_PSHADER_ray_cast
PSOut main(PSIn_RayCast In)
{
	PSOut Out;
	if (In.ws_vert.z > 1000000) clip(In.ws_vert.z);
	Out.intercept = In.intercept;
	return Out;
}
#endif




#ifdef PR_RDR_GSHADER_ray_cast_edge
[maxvertexcount(1)]
void main(line PSIn_RayCast In[2], inout PointStream<PSIn_RayCast> OutStream)
{
	// Todo: use adjacency, to only snap to non-planar verts/edges

	// Triangles come in

	// Ray test them

	// output "screen space" pixels to a 1D texture where
	// depth is the parametric value of the intersect
	// Pixels 0,1,2 are the x,y,z of the intersect
	OutStream.Append(In[0]);
	OutStream.RestartStrip();
}
#endif
#ifdef PR_RDR_GSHADER_ray_cast_vert
[maxvertexcount(1)]
void main(point PSIn_RayCast In[1], inout PointStream<PSIn_RayCast> OutStream)
{
	// Todo: use adjacency, to only snap to non-planar verts/edges

	// Triangles come in

	// Ray test them

	// output "screen space" pixels to a 1D texture where
	// depth is the parametric value of the intersect
	// Pixels 0,1,2 are the x,y,z of the intersect
	OutStream.Append(In[0]);
	OutStream.RestartStrip();
}
#endif