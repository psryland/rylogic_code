//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_UBER_CBUFFER_HLSL
#define PR_RDR_SHADER_UBER_CBUFFER_HLSL

#if SHADER_BUILD
#include "uber_defines.hlsl"
#endif

#define PR_RDR_MAX_PROJECTED_TEXTURES 1

// Notes:
// For efficiency, constant buffers need to be grouped by frequency of update
// The C/C++ versions of the buffer structs should contain elements assuming
// every struct member is selected. Make sure the packoffset is correct for
// each member.

// 'CBufFrame' is a cbuffer managed by a scene.
// It contains values constant for the whole frame.
// It is defined for every shader because most will probably need it
#if SHADER_BUILD
cbuffer CBufFrame_Forward :register(b0)
{
	// Camera transform
	matrix m_c2w :packoffset(c0); // camera to world
	matrix m_w2c :packoffset(c4); // world to camera
	matrix m_w2s :packoffset(c8); // world to screen

	// Global lighting
	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	float4 m_global_lighting    :packoffset(c12); // Encoded info for global lighting
	float4 m_ws_light_direction :packoffset(c13); // The direction of the global light source
	float4 m_ws_light_position  :packoffset(c14); // The position of the global light source
	float4 m_light_ambient      :packoffset(c15); // The colour of the ambient light
	float4 m_light_colour       :packoffset(c16); // The colour of the directional light
	float4 m_light_specular     :packoffset(c17); // The colour of the specular light. alpha channel is specular power
	float4 m_spot               :packoffset(c18); // x = inner cos angle, y = outer cos angle, z = range, w = falloff

	// Projected textures
	float4 m_proj_tex_count :packoffset(c19);
	matrix m_proj_tex[PR_RDR_MAX_PROJECTED_TEXTURES] :packoffset(c20);
};
#else
struct CBufFrame_Forward
{
	// Camera transform
	pr::m4x4 m_c2w; // camera to world
	pr::m4x4 m_w2c; // world to camera
	pr::m4x4 m_w2s; // world to screen

	// Global lighting
	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	pr::v4 m_global_lighting;    // Encoded info for global lighting
	pr::v4 m_ws_light_direction; // The direction of the global light source
	pr::v4 m_ws_light_position;  // The position of the global light source
	pr::Colour m_light_ambient;  // The colour of the ambient light
	pr::Colour m_light_colour;   // The colour of the directional light
	pr::Colour m_light_specular; // The colour of the specular light. alpha channel is specular power
	pr::v4 m_spot;               // x = inner cos angle, y = outer cos angle, z = range, w = falloff

	// Projected textures
	pr::v4 m_proj_tex_count;
	pr::m4x4 m_proj_tex[PR_RDR_MAX_PROJECTED_TEXTURES];
};
#endif

// 'CBufModel' is a cbuffer updated per render nugget.
// Shaders can select components from this structure as needed
#if SHADER_BUILD
cbuffer CBufModel_Forward :register(b1)
{
	// Object transform
	EXPAND(matrix m_o2s :packoffset(c0) ;,PR_RDR_SHADER_TXFM  ) // object to screen
	EXPAND(matrix m_o2w :packoffset(c4) ;,PR_RDR_SHADER_TXFMWS) // object to world
	EXPAND(matrix m_n2w :packoffset(c8) ;,PR_RDR_SHADER_TXFMWS) // normal to world

	// Tinting
	EXPAND(float4 m_tint :packoffset(c12) ;,PR_RDR_SHADER_TINT0) // object tint colour

	// Texture2D
	EXPAND(matrix m_tex2surf0 :packoffset(c13) ;,PR_RDR_SHADER_TEX0) // texture to surface transform
};
#else
struct CBufModel_Forward
{
	// Object transform
	pr::m4x4   m_o2s;
	pr::m4x4   m_o2w;
	pr::m4x4   m_n2w;

	// Tinting
	pr::Colour m_tint;

	// Texture2D
	pr::m4x4 m_tex2surf0;
};
#endif

#if SHADER_BUILD

// Texture2D /w sampler
Texture2D<float4> m_texture0 :register(t0);
SamplerState      m_sampler0 :register(s0);

// Projected textures
Texture2D<float4> m_proj_texture[PR_RDR_MAX_PROJECTED_TEXTURES];
SamplerState      m_proj_sampler[PR_RDR_MAX_PROJECTED_TEXTURES];

#endif

#endif
