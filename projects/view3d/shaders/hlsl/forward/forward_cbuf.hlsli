//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for forward shaders.
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_FORWARD_CBUF_HLSL
#define PR_RDR_SHADER_FORWARD_CBUF_HLSL
#include "../types.hlsli"

// 'CBufFrame' is a cbuffer managed by a scene.
// It contains values constant for the whole frame.
// It is defined for every shader because most will probably need it
cbuffer CBufFrame :reg(b0)
{
	// Camera transform
	Camera m_cam;
	
	// Global lighting
	Light m_global_light;

	// EnvMap
	EnvMap m_env_map;

	// Shadows
	Shadow m_shadow;

	// Projected textures
	ProjTexture m_proj_tex;
};

// Constants per render nugget.
cbuffer CBufModel :reg(b1)
{
	// Note: A duplicate of this struct is in 'gbuffer_cbuf.hlsli'

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
	row_major float4x4 m_n2w; // normal to world

	// Texture2D
	row_major float4x4 m_tex2surf0; // texture to surface transform

	// Tinting
	float4 m_tint; // object tint colour

	// EnvMap
	float m_env_reflectivity; // Reflectivity of the environment map
};

// Constants used for radial fading.
cbuffer CBufFade :reg(b2)
{
	// The centre of the fade region. Set to (0,0,0,0) to use the camera position
	float4 m_fade_centre;
	
	// x = Fade starting radius
	// y = Fade ending radius
	float2 m_fade_radius;

	// 0 = Spherical fade
	// 1 = Cylindrical fade
	int m_fade_type;
};

#endif
