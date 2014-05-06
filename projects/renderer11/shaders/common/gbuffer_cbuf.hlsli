//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for gbuffer shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_GBUFFER_CBUF_HLSL
#define PR_RDR_SHADER_GBUFFER_CBUF_HLSL

// Notes:
// - use float4x4 not matrix... they're not the same (don't know why tho)
// - hlsl float4x4 is column major (by default) but pr::m4x4 is row major.
//   Rememeber to transpose matrices
// - For efficiency, constant buffers need to be grouped by frequency of update
//   The C/C++ versions of the buffer structs should contain elements assuming
//   every struct member is selected. Make sure the packoffset is correct for
//   each member.

// Camera to world transform
#if SHADER_BUILD
cbuffer CBufCamera :register(b0)
{
	float4x4 m_c2w        :packoffset(c0); // camera to world
	float4x4 m_c2s        :packoffset(c4); // camera to screen
	float4x4 m_w2c        :packoffset(c8); // world to camera
	float4x4 m_w2s        :packoffset(c12); // world to screen
	float4   m_frustum[4] :packoffset(c16); // View frustum corners in camera space
};
#else
struct CBufCamera
{
	enum { Slot = 0 };

	pr::m4x4 m_c2w;        // camera to world
	pr::m4x4 m_c2s;        // camera to screen
	pr::m4x4 m_w2c;        // world to camera
	pr::m4x4 m_w2s;        // world to screen
	pr::v4   m_frustum[4]; // View frustum corners in camera space
};
#endif

// Global lighting
#if SHADER_BUILD
cbuffer CBufLighting :register(b1)
{
	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	float4 m_global_lighting    :packoffset(c0); // Encoded info for global lighting
	float4 m_ws_light_direction :packoffset(c1); // The direction of the global light source
	float4 m_ws_light_position  :packoffset(c2); // The position of the global light source
	float4 m_light_ambient      :packoffset(c3); // The colour of the ambient light
	float4 m_light_colour       :packoffset(c4); // The colour of the directional light
	float4 m_light_specular     :packoffset(c5); // The colour of the specular light. alpha channel is specular power
	float4 m_spot               :packoffset(c6); // x = inner cos angle, y = outer cos angle, z = range, w = falloff
};
#else
struct CBufLighting
{
	enum { Slot = 1 };

	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	pr::v4     m_global_lighting;    // Encoded info for global lighting
	pr::v4     m_ws_light_direction; // The direction of the global light source
	pr::v4     m_ws_light_position;  // The position of the global light source
	pr::Colour m_light_ambient;      // The colour of the ambient light
	pr::Colour m_light_colour;       // The colour of the directional light
	pr::Colour m_light_specular;     // The colour of the specular light. alpha channel is specular power
	pr::v4     m_spot;               // x = inner cos angle, y = outer cos angle, z = range, w = falloff
};
#endif

// Per-model constants
#if SHADER_BUILD
cbuffer CBufModel :register(b2)
{
	// Object transform
	float4x4 m_o2s :packoffset(c0); // object to screen
	float4x4 m_o2w :packoffset(c4); // object to world
	float4x4 m_n2w :packoffset(c8); // normal to world (o2w unscaled)
	
	// Tinting
	float4 m_tint :packoffset(c12); // object tint colour

	// Texture2D
	float4x4 m_tex2surf0 :packoffset(c13); // texture to surface transform

	// Geometry type
	int4 m_geom :packoffset(c17); // x = has pvc, y = has normals, z = has tex0, w = not used
};
#else
struct CBufModel
{
	enum { Slot = 2 };

	// Object transform
	pr::m4x4   m_o2s; // object to screen
	pr::m4x4   m_o2w; // object to world
	pr::m4x4   m_n2w; // normal to world (o2w unscaled)

	// Tinting
	pr::Colour m_tint; // object tint colour

	// Texture2D
	pr::m4x4 m_tex2surf0; // texture to surface transform

	// Geometry type
	pr::iv4 m_geom; // x = has pvc, y = has normals, z = has tex0, w = not used
};
#endif

#endif