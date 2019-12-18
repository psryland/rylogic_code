//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for gbuffer shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_GBUFFER_CBUF_HLSL
#define PR_RDR_SHADER_GBUFFER_CBUF_HLSL

#include "../types.hlsli"

// Camera to world transform
cbuffer CBufCamera :reg(b0)
{
	Camera m_cam;
	float4 m_frustum[4]; // View frustum corners in camera space
};

// Global lighting
cbuffer CBufLighting :reg(b1)
{
	Light m_light;
};

// Per-model constants
cbuffer CBufModel :reg(b2)
{
	// Note: A duplicate of this struct is in 'forward_cbuf.hlsli'

	// x = Model flags:
	//   1 << 0 = has normals
	// y = Texture flags:
	//   1 << 0 = has diffuse texture
	//   1 << 1 = use env map
	// z = Alpha flags:
	//   1 < 0 = has alpha
	// w = Instance Id
	int4 m_flags;

	// Object transform
	row_major float4x4 m_o2s; // object to screen
	row_major float4x4 m_o2w; // object to world
	row_major float4x4 m_n2w; // normal to world (o2w unscaled)

	// Texture2D
	row_major float4x4 m_tex2surf0; // texture to surface transform

	// Tinting
	float4 m_tint; // object tint colour
};

#if SHADER_BUILD

#define HAS_NORMALS (m_flags.x == 1)
#define HAS_TEX0    (m_flags.y == 1)

#endif

#endif