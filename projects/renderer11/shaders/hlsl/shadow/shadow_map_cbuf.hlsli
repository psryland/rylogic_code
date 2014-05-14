//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for shadow map shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_SHADOW_MAP_CBUF_HLSL
#define PR_RDR_SHADER_SHADOW_MAP_CBUF_HLSL

#include "..\cbuf.hlsli"

// Camera to world transform and view frustum
cbuffer CBufFrame :cbuf_bank(b0)
{
	float4x4 m_proj[5];   // The five projection transforms onto the frustum faces
	float4   m_frust_dim; // The dimensions of the shadow frustum
	//	float4x4 m_c2w; // camera to world
//	float4x4 m_c2s; // camera to screen
//	float4x4 m_w2c; // world to camera
//	float4x4 m_w2s; // world to screen
//
//	float4   m_frustum[4]; // View frustum corners in camera space
};

// Light source
cbuffer CBufLighting :cbuf_bank(b1)
{
	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	float4 m_light_info;         // Encoded info for the light
	float4 m_ws_light_direction; // The direction of the light source
	float4 m_ws_light_position;  // The position of the light source
	float4 m_light_ambient;      // The colour of the ambient light
	float4 m_light_colour;       // The colour of the directional light
	float4 m_light_specular;     // The colour of the specular light. alpha channel is specular power
	float4 m_spot;               // x = inner cos angle, y = outer cos angle, z = range, w = falloff
};


// Per nugget constants
cbuffer CBufNugget :cbuf_bank(b2)
{
	// Object transform
	float4x4 m_o2s; // object to screen
	float4x4 m_o2w; // object to world
	float4x4 m_n2w; // normal to world

	// Tinting
	//float4 m_tint; // object tint colour

	// Texture2D
	//float4x4 m_tex2surf0; // texture to surface transform

	// Geometry type
	//int4 m_geom;  // x = 1 => has normals, y = 1 => has tex0, z,w = not used
};
#endif

