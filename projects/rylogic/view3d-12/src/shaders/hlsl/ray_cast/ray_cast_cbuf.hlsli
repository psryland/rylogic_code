﻿//***********************************************
// View 3d
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_VIEW3D_SHADER_RAY_CAST_CBUF_HLSL
#define PR_VIEW3D_SHADER_RAY_CAST_CBUF_HLSL

#include "view3d-12/src/shaders/hlsl/types.hlsli"

// The maximum number of rays in a single pass
static const int MaxRays = 16;
static const int MaxIntercepts = 256;

// Keep in sync with ESnapType in 'ray_cast.h'
static const int ESnapType_None = 0;
static const int ESnapType_Vert = 1;
static const int ESnapType_EdgeMiddle = 2;
static const int ESnapType_FaceCentre = 3;
static const int ESnapType_Edge = 4;
static const int ESnapType_Face = 5;

// Keep in sync with ESnapMode in 'ray_cast.h'
static const int ESnapMode_Vert = 1 << 0;
static const int ESnapMode_Edge = 1 << 1;
static const int ESnapMode_Face = 1 << 2;

#define TrySnapToFace ((m_snap_mode & ESnapMode_Face) != 0)
#define TrySnapToEdge ((m_snap_mode & ESnapMode_Edge) != 0)
#define TrySnapToVert ((m_snap_mode & ESnapMode_Vert) != 0)

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
	float4 ws_normal;    // Normal at the intercept point (if available)
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
	
	// Combination of 'ESnapMode'. What sort of snapping to perform
	int m_snap_mode;

	// The snap distance
	float m_snap_dist;
};

// Per-nugget constants
cbuffer CBufNugget :reg(b1, 0)
{
	// x = Model flags - See types.hlsli
	// y = Texture flags
	// z = Alpha flags
	// w = Instance Id
	int4 m_flags;

	// Object transform
	row_major float4x4 m_m2o; // model to object space
	row_major float4x4 m_o2w; // object to world
	row_major float4x4 m_n2w; // normal to world

	// Instance pointer
	voidp m_inst_ptr;
};

#endif