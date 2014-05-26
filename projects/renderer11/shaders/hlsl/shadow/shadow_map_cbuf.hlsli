//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for shadow map shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_SHADOW_MAP_CBUF_HLSL
#define PR_RDR_SHADER_SHADOW_MAP_CBUF_HLSL

#include "../types.hlsli"

// Camera to world transform and view frustum
cbuffer CBufFrame :cbuf_bank(b0)
{
	row_major float4x4 m_proj[5];  // The five projection transforms onto the frustum faces
	float4 m_frust_dim;            // The dimensions of the shadow frustum
};

// Per nugget constants
cbuffer CBufNugget :cbuf_bank(b1)
{
	// Object transform
	row_major float4x4 m_o2s; // object to screen
	row_major float4x4 m_o2w; // object to world
	row_major float4x4 m_n2w; // normal to world
};
#endif

