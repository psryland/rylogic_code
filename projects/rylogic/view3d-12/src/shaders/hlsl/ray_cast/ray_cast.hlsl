//***********************************************
// View 3d
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

#include "pr/hlsl/core.hlsli"
#include "pr/hlsl/geometry.hlsli"
#include "view3d-12/src/shaders/hlsl/types.hlsli"
#include "view3d-12/src/shaders/hlsl/ray_cast/ray_cast_cbuf.hlsli"

// Skinned Meshes
StructuredBuffer<Mat4x4> m_pose : reg(t4, 0);
StructuredBuffer<Skinfluence> m_skin : reg(t5, 0);

#include "view3d-12/src/shaders/hlsl/skinned/skinned.hlsli"

// Geometry shader input format
struct GSIn_RayCast
{
	float4 ws_vert : WSVertex;
};
struct GSOut_RayCast
{
	float4 ws_intercept : WSIntercept;
	float4 ws_normal : WSNormal;
	int snap_type : SnapType;
	int ray_index : RayIndex;
	uint2 inst_ptr : InstPtr;
};

// Additional geometry locations to snap to
struct SnapPoint
{
	float4 vert;
	int snap_type;
};
struct SnapEdge
{
	float4 vert;
	float4 edge;
	int snap_type;
};

// Default VS
GSIn_RayCast VSDefault(VSIn In)
{
	GSIn_RayCast Out = (GSIn_RayCast) 0;

	// Transform
	float4 os_vert = mul(In.vert, m_m2o);
	float4 os_norm = mul(In.norm, m_m2o);
	
	if (IsSkinned)
	{
		os_vert = SkinVertex(m_pose, m_skin[In.idx0.x], os_vert);
		os_norm = SkinNormal(m_pose, m_skin[In.idx0.x], os_norm);
	}

	Out.ws_vert = mul(os_vert, m_o2w);

	return Out;
}

void RayCastFace(triangle GSIn_RayCast In[3], inout PointStream<GSOut_RayCast> OutStream)
{
	// Triangle verts
	float4 v0 = In[0].ws_vert;
	float4 v1 = In[1].ws_vert;
	float4 v2 = In[2].ws_vert;

	static const int SnapPointCount = 7;
	SnapPoint points[SnapPointCount] = {
		{v0, ESnapType_Vert},
		{v1, ESnapType_Vert},
		{v2, ESnapType_Vert},
		{(v0 + v1) / 2, ESnapType_EdgeMiddle},
		{(v1 + v2) / 2, ESnapType_EdgeMiddle},
		{(v2 + v0) / 2, ESnapType_EdgeMiddle},
		{(v0 + v1 + v2) / 3, ESnapType_FaceCentre},
	};
	static const int SnapEdgeCount = 3;
	SnapEdge edges[SnapEdgeCount] = {
		{v0, v1 - v0, ESnapType_Edge},
		{v1, v2 - v1, ESnapType_Edge},
		{v2, v0 - v2, ESnapType_Edge},
	};

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin    = m_rays[i].ws_origin;
		float4 direction = m_rays[i].ws_direction;
		
		// Perform a ray vs. triangle intersection test to get the closest point to the triangle
		float4 para = Intersect_RayVsTriangle(origin, direction, v0, v1, v2);
		if (AllZero(para) || any(saturate(para) != para))
			continue; // no intersection, co-planar

		// Find the nearest point on the ray
		float4 pt = float4((v0*para.x + v1*para.y + v2*para.z).xyz, 1);
		float depth = dot(pt - origin, direction);
		if (depth <= 0)
			continue; // too close, or behind the ray origin

		// Variables for finding the nearest snap-to point
		float4 intercept = pt;
		float4 normal = NormaliseOrZero(float4(cross(v1.xyz - v0.xyz, v2.xyz - v0.xyz), 0));
		float dist = select(PerspectiveSnap, depth * m_snap_distance, m_snap_distance);
		int snap_type = ESnapType_None;
		int j, jend;

		// Snap to verts within 'snap_dist'
		jend = select(TrySnapToVert, SnapPointCount, 0);
		for (j = 0; j != jend; ++j)
		{
			float4 p = points[j].vert;
			float  d = distance(pt, p);

			// Select based on distance
			bool snap = d < dist;
			snap_type = select(snap, points[j].snap_type, snap_type);
			intercept = select(snap, p, intercept);
			dist      = select(snap, d, dist);
			normal    = select(snap, float4(0, 0, 0, 0), normal);
		}
		
		// Only test edges if there are no vert snaps
		jend = select(TrySnapToEdge && snap_type == ESnapType_None, SnapEdgeCount, 0);
		for (j = 0; j != jend; ++j)
		{
			float  t = ClosestPoint_PointVsRay(pt, edges[j].vert, edges[j].edge);
			float4 p = edges[j].vert + saturate(t) * edges[j].edge;
			float  d = distance(pt, p);

			// Select based on distance
			bool snap = d < dist;
			snap_type = select(snap, edges[j].snap_type, snap_type);
			intercept = select(snap, p, intercept);
			dist      = select(snap, d, dist);
			normal    = select(snap, float4(0, 0, 0, 0), normal);
		}

		// Only test faces if there are no edge or vert snaps
		jend = select(TrySnapToFace && snap_type == ESnapType_None, 1, 0);
		for (j = 0; j != jend; ++j)
		{
			snap_type = ESnapType_Face;
			intercept = pt;
		}

		// Output an intercept
		// The 'ws_intercept.w' value is used to depth-sort the intercepts
		GSOut_RayCast Out = (GSOut_RayCast) 0;
		Out.ws_intercept = float4(intercept.xyz, depth);
		Out.ws_normal = normal;
		Out.snap_type = snap_type;
		Out.ray_index = i;
		Out.inst_ptr = m_inst_ptr;
		OutStream.Append(Out);
	}
}
void RayCastEdge(line GSIn_RayCast In[2], inout PointStream<GSOut_RayCast> OutStream)
{
	float4 v0 = In[0].ws_vert;
	float4 v1 = In[1].ws_vert;
	float4 edge = v1 - v0;

	static const int SnapPointCount = 3;
	SnapPoint points[SnapPointCount] = {
		{ v0, ESnapType_Vert },
		{ v1, ESnapType_Vert },
		{ (v0 + v1) / 2, ESnapType_EdgeMiddle },
	};

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin = m_rays[i].ws_origin;
		float4 direction = m_rays[i].ws_direction;

		// Perform a ray vs. line segment test to get the closest point to the edge
		float2 para = ClosestPoint_LineSegmentVsRay(v0, edge, origin, direction);

		// Find the nearest point on the ray and the edge
		float4 pt = origin + para.y * direction;
		float depth = dot(pt - origin, direction);
		if (depth <= 0)
			continue; // too close, or behind the ray origin

		// Variables for finding the nearest snap-to point
		float4 intercept = pt;
		float dist = select(PerspectiveSnap, depth * m_snap_distance, m_snap_distance);
		int snap_type = ESnapType_None;
		int j, jend;

		// Snap to points within 'snap_dist'
		jend = select(TrySnapToVert, SnapPointCount, 0);
		for (j = 0; j != jend; ++j)
		{
			float4 p = points[j].vert;
			float d = distance(pt, p);

			// Select based on distance
			bool snap = d < dist;
			snap_type = select(snap, points[j].snap_type, snap_type);
			intercept = select(snap, p, intercept);
			dist = select(snap, d, dist);
		}

		// Only test edges if there are no vert snaps
		jend = select(TrySnapToEdge && snap_type == ESnapType_None, 1, 0);
		for (j = 0; j != jend; ++j)
		{
			float4 p = v0 + para.x * edge;
			float d = distance(pt, p);

			bool snap = d < dist;
			snap_type = select(snap, ESnapType_Edge, snap_type);
			intercept = select(snap, p, intercept);
		}

		// No intercept? try the next ray
		if (snap_type == ESnapType_None)
			continue;

		// Output an intercept
		GSOut_RayCast Out = (GSOut_RayCast) 0;
		Out.ws_intercept = float4(intercept.xyz, depth);
		Out.ws_normal = float4(0, 0, 0, 0);
		Out.snap_type = snap_type;
		Out.ray_index = i;
		Out.inst_ptr = m_inst_ptr;
		OutStream.Append(Out);
	}
}
void RayCastVert(point GSIn_RayCast In[1], inout PointStream<GSOut_RayCast> OutStream)
{
	float4 v0 = In[0].ws_vert;

	for (int i = 0; i != m_ray_count; ++i)
	{
		float4 origin = m_rays[i].ws_origin;
		float4 direction = m_rays[i].ws_direction;

		// Perform a ray vs. point test
		float para = ClosestPoint_PointVsRay(v0, origin, direction);

		// Find the nearest point on the ray and the point
		float4 pt = origin + para * direction;
		float depth = dot(pt - origin, direction);
		if (depth <= 0)
			continue; // too close, or behind the ray origin

		// Variables for finding the nearest snap-to point
		float d = distance(pt, v0);
		float dist = select(PerspectiveSnap, depth * m_snap_distance, m_snap_distance);
		bool snap = TrySnapToVert && d < dist;
		float4 intercept = select(snap, v0, float4(0, 0, 0, 0));
		int snap_type = select(snap, ESnapType_Vert, ESnapType_None);

		// No intercept? try the next ray
		if (snap_type == ESnapType_None)
			continue;

		// Output an intercept
		GSOut_RayCast Out = (GSOut_RayCast) 0;
		Out.ws_intercept = float4(intercept.xyz, depth);
		Out.ws_normal = float4(0, 0, 0, 0);
		Out.snap_type = snap_type;
		Out.ray_index = i;
		Out.inst_ptr = m_inst_ptr;
		OutStream.Append(Out);
	}
}

// Vertex shader
#ifdef PR_RDR_VSHADER_ray_cast
GSIn_RayCast main(VSIn In)
{
	GSIn_RayCast Out = VSDefault(In);
	return Out;
}
#endif

// Geometry shaders
#ifdef PR_RDR_GSHADER_ray_cast_face
[maxvertexcount(MaxRays)]
void main(triangle GSIn_RayCast In[3], inout PointStream<GSOut_RayCast> OutStream)
{
	RayCastFace(In, OutStream);
}
#endif
#ifdef PR_RDR_GSHADER_ray_cast_edge
[maxvertexcount(MaxRays)]
void main(line GSIn_RayCast In[2], inout PointStream<GSOut_RayCast> OutStream)
{
	RayCastEdge(In, OutStream);
}
#endif
#ifdef PR_RDR_GSHADER_ray_cast_vert
[maxvertexcount(MaxRays)]
void main(point GSIn_RayCast In[1], inout PointStream<GSOut_RayCast> OutStream)
{
	RayCastVert(In, OutStream);
}
#endif
