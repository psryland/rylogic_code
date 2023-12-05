//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_RAY_CAST_CBUF_HLSL
#define PR_RDR_SHADER_RAY_CAST_CBUF_HLSL
#include "../types.hlsli"

// The maximum number of rays in a single pass
static const int MaxRays = 16;
static const int MaxIntercepts = 256;

// A single ray to cast
struct Ray
{
	float4 ws_origin;
	float4 ws_direction;
};

// The intercept between a ray and an object
struct Intercept
{
	float4 ws_intercept; // xyz = intercept, w = distance
	int    snap_type;    // See ESnapType
	int    ray_index;    // The index of the input ray that caused this intercept
	voidp  inst_ptr;     // The address of the instance that was hit
};

// Per-frame constants
cbuffer CBufFrame :reg(b0, 0)
{
	Ray m_rays[MaxRays];

	// The number of rays to cast
	int m_ray_count;

	// 1 << 0 = snap to faces
	// 1 << 1 = snap to edges
	// 1 << 2 = snap to verts
	int m_snap_mode;

	// The snap distance
	float m_snap_dist;
	float pad0;
};

// Per-nugget constants
cbuffer CBufNugget :reg(b1, 0)
{
	// Object transform
	row_major float4x4 m_o2w;

	// Instance pointer
	voidp m_inst_ptr;
};

#ifdef SHADER_BUILD

	#define HAS_FACE_SNAP  ((m_snap_mode & (1 << 0)) != 0)
	#define HAS_EDGE_SNAP  ((m_snap_mode & (1 << 1)) != 0)
	#define HAS_VERT_SNAP  ((m_snap_mode & (1 << 2)) != 0)

	// Keep in sync with ESnapType in 'ray_cast.h'
	#define SNAP_TYPE_NONE 0
	#define SNAP_TYPE_VERT 1
	#define SNAP_TYPE_EDGEMIDDLE 2
	#define SNAP_TYPE_FACECENTRE 3
	#define SNAP_TYPE_EDGE 4
	#define SNAP_TYPE_FACE 5

#endif

#endif