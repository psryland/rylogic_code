//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
//
// Perform a ray vs. model test on all verts, edges, faces in a model
//
// Algorithm:
//  For each Frame:
//     Constants { rays[16], ray_count, snap_mode, snap_distance, }
//     For each nugget:
//         Constants { o2w, instance_pointer }
//         Use a vertex shader to transform MS verts to world space.
//         Use geometry shaders to perform Ray Vs. Prim tests
//         Use a null pixel shader and set the stream output stage target to a buffer.
//         The format of the SO target buffer is:
//            float4 ws_intercept
//            float distance from ray origin to intercept
//            uint ray index
//            uint2 instance_pointer (x=hi,y=lo)
//  Use CopyStructureCount to read the number of intercepts written to the stream output buffer.
//  Use CopyResource to copy the intercepts buffer to a staging buffer

#include "../types.hlsli"
#include "../common/functions.hlsli"
#include "../common/geometry.hlsli"
#include "ray_cast_cbuf.hlsli"

// Geometry shader input format
struct GSIn_RayCast
{
	float4 ws_vert :WSVert;
};
struct GSOut_RayCast
{
	float4 ws_intercept :WSIntercept;
	int    snap_type    :SnapType;
	int    ray_index    :RayIndex;
	uint2  inst_ptr     :InstPtr;
};
struct TestPoint
{
	float4 vert;
	int snap_type;
};
struct TestEdge
{
	float4 vert;
	float4 edge;
	int snap_type;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_ray_cast
GSIn_RayCast main(VSIn In)
{
	// Transform the model verts to world space
	GSIn_RayCast Out = (GSIn_RayCast)0;
	Out.ws_vert = mul(In.vert, m_o2w);
	return Out;
}
#endif

// Geometry shaders
#ifdef PR_RDR_GSHADER_ray_cast_face
[maxvertexcount(MaxRays)]
void main(triangle GSIn_RayCast In[3], inout PointStream<GSOut_RayCast> OutStream)
{
	float4 v0 = In[0].ws_vert;
	float4 v1 = In[1].ws_vert;
	float4 v2 = In[2].ws_vert;

	TestPoint points[] =
	{
		{v0, SNAP_TYPE_VERT},
		{v1, SNAP_TYPE_VERT},
		{v2, SNAP_TYPE_VERT},
		{(v0 + v1) / 2, SNAP_TYPE_EDGECENTRE},
		{(v1 + v2) / 2, SNAP_TYPE_EDGECENTRE},
		{(v2 + v0) / 2, SNAP_TYPE_EDGECENTRE},
		{(v0 + v1 + v2) / 3, SNAP_TYPE_FACECENTRE},
	};
	TestEdge edges[] =
	{
		{v0, v1 - v0, SNAP_TYPE_EDGE},
		{v1, v2 - v1, SNAP_TYPE_EDGE},
		{v2, v0 - v2, SNAP_TYPE_EDGE},
	};

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin    = m_rays[i].ws_origin;
		float4 direction = m_rays[i].ws_direction;
		
		// Perform a ray vs. triangle intersection test to get the closest point to the triangle
		float4 para = Intersect_RayVsTriangle(origin, direction, v0, v1, v2);
		if (AllZero(para))
			continue; // no intersection, co-planar

		// Find the nearest point on the ray
		float4 pt = float4((v0*para.x + v1*para.y + v2*para.z).xyz, 1);

		// Variables for finding the nearest snap-to point
		float4 intercept = float4(0,0,0,0);
		int snap_type    = SNAP_TYPE_NONE;
		float dist       = m_snap_dist;
		int j,jend;
		
		// Snap to points/edges within 'snap_dist'
		jend = SelectInt(HAS_VERT_SNAP, 7, 0);
		[unroll] for (j = 0; j != jend; ++j)
		{
			float4 p = points[j].vert;
			float  d = distance(pt, p);
			bool snap = d < dist;

			// Select based on distance
			snap_type = SelectInt(snap, points[j].snap_type, snap_type);
			intercept = SelectFloat4(snap, p, intercept);
			dist      = SelectFloat(snap, d, dist);
		}
		
		// Only test edges if there are no vert snaps
		jend = SelectInt(HAS_EDGE_SNAP && dist == m_snap_dist, 3, 0);
		[unroll] for (j = 0; j != jend; ++j)
		{
			float  t = ClosestPoint_PointVsRay(pt, edges[j].vert, edges[j].edge);
			float4 p = edges[j].vert + saturate(t) * edges[j].edge;
			float  d = distance(pt, p);
			bool snap = d < dist;

			// Select based on distance
			snap_type = SelectInt(snap, edges[j].snap_type, snap_type);
			intercept = SelectFloat4(snap, p, intercept);
			dist      = SelectFloat(snap, d, dist);
		}

		// Only test faces if there are no edge or vert snaps
		{
			bool snap = HAS_FACE_SNAP && dist == m_snap_dist && AllZeroOrPositive(para);

			snap_type = SelectInt(snap, SNAP_TYPE_FACE, snap_type);
			intercept = SelectFloat4(snap, pt, intercept);
		}

		// No intercept? try the next ray
		if (snap_type == SNAP_TYPE_NONE)
			continue;

		// Output an intercept
		GSOut_RayCast Out = (GSOut_RayCast)0;
		Out.ws_intercept = float4(intercept.xyz, distance(intercept, origin));
		Out.snap_type = snap_type;
		Out.ray_index = i;
		Out.inst_ptr = m_inst_ptr;
		OutStream.Append(Out);
	}
}
#endif
#ifdef PR_RDR_GSHADER_ray_cast_edge
[maxvertexcount(MaxRays)]
void main(line GSIn_RayCast In[2], inout PointStream<GSOut_RayCast> OutStream)
{
	float4 v0 = In[0].ws_vert;
	float4 v1 = In[1].ws_vert;
	float4 edge = v1 - v0;

	TestPoint points[] =
	{
		{v0, SNAP_TYPE_VERT},
		{v1, SNAP_TYPE_VERT},
		{(v0 + v1) / 2, SNAP_TYPE_EDGECENTRE},
	};

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin    = m_rays[i].ws_origin;
		float4 direction = m_rays[i].ws_direction;

		// Perform a ray vs. line segment test to get the closest point to the edge
		float2 para = ClosestPoint_LineSegmentVsRay(v0, edge, origin, direction);

		// Find the nearest point on the ray and the edge
		float4 pt = origin + para.y * direction;

		// Variables for finding the nearest snap-to point
		float4 intercept = float4(0,0,0,0);
		int snap_type    = SNAP_TYPE_NONE;
		float dist       = m_snap_dist;
		int j,jend;

		// Snap to points/edges within 'snap_dist'
		jend = SelectInt(HAS_VERT_SNAP, 3, 0);
		[unroll] for (j = 0; j != jend; ++j)
		{
			float4 p = points[j].vert;
			float  d = distance(pt, p);
			bool snap = d < dist;

			// Select based on distance
			snap_type = SelectInt(snap, points[j].snap_type, snap_type);
			intercept = SelectFloat4(snap, p, intercept);
			dist      = SelectFloat(snap, d, dist);
		}

		// Only test edges if there are no vert snaps
		{
			float4 p = v0 + para.x * edge;
			float  d = distance(pt, p);
			bool snap = HAS_EDGE_SNAP && dist == m_snap_dist && d < dist;

			snap_type = SelectInt(snap, SNAP_TYPE_EDGE, snap_type);
			intercept = SelectFloat4(snap, p, intercept);
		}

		// No intercept? try the next ray
		if (snap_type == SNAP_TYPE_NONE)
			continue;

		// Output an intercept
		GSOut_RayCast Out = (GSOut_RayCast)0;
		Out.ws_intercept = float4(intercept.xyz, distance(intercept, origin));
		Out.snap_type = snap_type;
		Out.ray_index = i;
		Out.inst_ptr = m_inst_ptr;
		OutStream.Append(Out);
	}
}
#endif
#ifdef PR_RDR_GSHADER_ray_cast_vert
[maxvertexcount(MaxRays)]
void main(point GSIn_RayCast In[1], inout PointStream<GSOut_RayCast> OutStream)
{
	float4 v0 = In[0].ws_vert;

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin    = m_rays[i].ws_origin;
		float4 direction = m_rays[i].ws_direction;

		// Perform a ray vs. point test
		float para = ClosestPoint_PointVsRay(v0, origin, direction);

		// Find the nearest point on the ray
		float4 pt = origin + para * direction;
		float  d = distance(pt, v0);
		bool snap = HAS_VERT_SNAP && d < m_snap_dist;

		// Variables for finding the nearest snap-to point
		float4 intercept = SelectFloat4(snap, v0, float4(0,0,0,0));
		int snap_type    = SelectInt(snap, SNAP_TYPE_VERT, SNAP_TYPE_NONE);

		// No intercept? try the next ray
		if (snap_type == SNAP_TYPE_NONE)
			continue;

		// Output an intercept
		GSOut_RayCast Out = (GSOut_RayCast)0;
		Out.ws_intercept = float4(intercept.xyz, distance(intercept, origin));
		Out.snap_type = snap_type;
		Out.ray_index = i;
		Out.inst_ptr = m_inst_ptr;
		OutStream.Append(Out);
	}
}
#endif
