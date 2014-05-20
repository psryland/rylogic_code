//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for gbuffer shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_FORWARD_CBUF_HLSL
#define PR_RDR_SHADER_FORWARD_CBUF_HLSL

#include "../cbuf.hlsli"
#include "../types.hlsli"

#define PR_RDR_MAX_PROJECTED_TEXTURES 1

// 'CBufFrame' is a cbuffer managed by a scene.
// It contains values constant for the whole frame.
// It is defined for every shader because most will probably need it
cbuffer CBufFrame :cbuf_bank(b0)
{
	// Camera transform
	row_major float4x4 m_c2w; // camera to world
	row_major float4x4 m_c2s; // camera to screen
	row_major float4x4 m_w2c; // world to camera
	row_major float4x4 m_w2s; // world to screen
	
	// Global lighting
	Light m_global_light;

	// Shadows
	Shadow m_shadow;
	
	// Projected textures
	float4 m_proj_tex_count;
	row_major float4x4 m_proj_tex[PR_RDR_MAX_PROJECTED_TEXTURES];
};

// 'CBufModel' is a cbuffer updated per render nugget.
// Shaders can select components from this structure as needed
cbuffer CBufModel :cbuf_bank(b1)
{
	// Geometry type
	int4 m_geom;  // x = 1 => has normals, y = 1 => has tex0, z,w = not used

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
