//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_RAY_CAST_CBUF_HLSL
#define PR_RDR_SHADER_RAY_CAST_CBUF_HLSL

#include "../types.hlsli"

#define HAS_FACE_SNAP  ((m_snap_mode & (1 << 0)) != 0)
#define HAS_EDGE_SNAP  ((m_snap_mode & (1 << 1)) != 0)
#define HAS_VERT_SNAP  ((m_snap_mode & (1 << 2)) != 0)
#define MAX_RAYS 16

// 'CBufRayCastFrame' contains values constant for the whole frame.
cbuffer CBufRayCastFrame :reg(b0)
{
	// Ray to cast
	float4 m_ws_ray_origin[MAX_RAYS];
	float4 m_ws_ray_direction[MAX_RAYS];

	// The number of rays in the arrays
	int m_ray_count;

	// 1 << 0 = snap to faces
	// 1 << 1 = snap to edges
	// 1 << 2 = snap to verts
	int m_snap_mode;

	// The maximum length of the ray (used to normalise to 0->1)
	float m_ray_max_length;

	// The snap distance
	float m_snap_dist;
};

// Constants per render nugget
cbuffer CBufRayCastNugget :reg(b1)
{
	// Object transform
	row_major float4x4 m_o2w; // object to world

	// Model flags
	int4 m_flags; // x,y,z = no used, w = instance id
};

#endif