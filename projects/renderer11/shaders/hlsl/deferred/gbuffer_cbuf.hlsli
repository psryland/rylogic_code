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
cbuffer CBufCamera :cbuf_bank(b0)
{
	Camera m_cam;
	float4 m_frustum[4]; // View frustum corners in camera space
};

// Global lighting
cbuffer CBufLighting :cbuf_bank(b1)
{
	Light m_light;
};

// Per-model constants
cbuffer CBufModel :cbuf_bank(b2)
{
	// Object transform
	row_major float4x4 m_o2s; // object to screen
	row_major float4x4 m_o2w; // object to world
	row_major float4x4 m_n2w; // normal to world (o2w unscaled)
	
	// Tinting
	float4 m_tint; // object tint colour

	// Texture2D
	row_major float4x4 m_tex2surf0; // texture to surface transform

	// Geometry type
	int4 m_geom;  // x = 1 => has normals, y = 1 => has tex0, z,w = not used
};

#if SHADER_BUILD

#define HAS_NORMALS m_geom.x == 1
#define HAS_TEX0 m_geom.y == 1

#endif

#endif