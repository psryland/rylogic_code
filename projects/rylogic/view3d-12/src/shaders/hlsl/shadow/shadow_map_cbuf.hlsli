//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for shadow map shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_SHADOW_MAP_CBUF_HLSL
#define PR_RDR_SHADER_SHADOW_MAP_CBUF_HLSL
#include "../types.hlsli"

// Camera to world transform and view frustum
cbuffer CBufFrame :reg(b0)
{
	// Not using 'Shadow' from types because the shadow map generation
	// shaders operate on one shadow map at a time. The main render can
	// support multiple shadow maps.
	row_major float4x4 m_w2l; // World space to light space
	row_major float4x4 m_l2s; // Light space to screen space
};

// Per nugget constants
cbuffer CBufNugget :reg(b1)
{
	// x = Model flags:
	//   1 << 0 = has normals
	// y = Texture flags:
	//   1 << 0 = has diffuse texture
	//   1 << 1 = use env map
	// z = Alpha flags:
	//   1 << 0 = has alpha
	// w = Instance Id
	int4 m_flags;

	// Object transform
	row_major float4x4 m_o2s; // object to screen
	row_major float4x4 m_o2w; // object to world
	row_major float4x4 m_n2w; // normal to world

	// Texture2D
	row_major float4x4 m_tex2surf0; // texture to surface transform

	// Tinting
	float4 m_tint; // object tint colour
};
#endif

