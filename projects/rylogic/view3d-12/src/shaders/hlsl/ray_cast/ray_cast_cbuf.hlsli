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

// Keep in sync with ESnapType in 'ray_cast.h'
static const int SnapType_None = 0;
static const int SnapType_Vert = 1;
static const int SnapType_EdgeMiddle = 2;
static const int SnapType_FaceCentre = 3;
static const int SnapType_Edge = 4;
static const int SnapType_Face = 5;

// Keep in sync with ESnapMode in 'ray_cast.h'
static const int SnapMode_Vert = 1 << 0;
static const int SnapMode_Edge = 1 << 1;
static const int SnapMode_Face = 1 << 2;

#define HasSnapFace (m_snap_mode & SnapMode_Vert)
#define HasSnapEdge (m_snap_mode & SnapMode_Edge)
#define HasSnapVert (m_snap_mode & SnapMode_Face)

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

	// Combination of 'SnapFlags_'
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

#endif