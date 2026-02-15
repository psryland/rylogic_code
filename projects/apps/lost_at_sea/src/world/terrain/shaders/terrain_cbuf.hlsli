//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Shared types for terrain vertex and pixel shaders.
// This file is included from both HLSL and C++ source.
#ifndef LAS_TERRAIN_CBUF_HLSLI
#define LAS_TERRAIN_CBUF_HLSLI

#include "pr/hlsl/interop.hlsli"

// Terrain constant buffer. Bound to b3 (reusing the CBufScreenSpace slot since
// the terrain shader does not use screen-space geometry).
cbuffer CBufTerrain :reg(b3,0)
{
	// Camera world-space position (xyz), w = unused
	float4 m_camera_pos;

	// Mesh radii: x=inner, y=outer, z=num_rings, w=num_segments
	float4 m_mesh_config;

	// Noise parameters: x=octaves(int), y=base_frequency, z=persistence, w=amplitude
	float4 m_noise_params;

	// x=sea_level_bias, yzw=unused
	float4 m_noise_bias;

	// Sun direction (world space, normalised, points toward sun)
	float4 m_sun_direction;

	// Sun colour (RGB intensity)
	float4 m_sun_colour;
};

#endif
