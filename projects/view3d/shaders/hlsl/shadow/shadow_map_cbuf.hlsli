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
	float m_zn, m_zf, pad0, pad1; // x = near, y = far
};

// Per nugget constants
cbuffer CBufNugget :reg(b1)
{
	// Object transform
	row_major float4x4 m_o2s; // object to screen
	row_major float4x4 m_o2w; // object to world
	row_major float4x4 m_n2w; // normal to world
};
#endif

