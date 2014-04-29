//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Constant buffer definitions for gbuffer shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_GBUFFER_CBUF_HLSL
#define PR_RDR_SHADER_GBUFFER_CBUF_HLSL

// Per frame constants
#if SHADER_BUILD
cbuffer CBufFrame_GBuffer :register(b0)
{
	// Camera transform
	matrix m_c2w :packoffset(c0); // camera to world
	matrix m_w2c :packoffset(c4); // world to camera
	matrix m_w2s :packoffset(c8); // world to screen
};
#else
struct CBufFrame_GBuffer
{
	// Camera transform
	pr::m4x4 m_c2w; // camera to world
	pr::m4x4 m_w2c; // world to camera
	pr::m4x4 m_w2s; // world to screen
};
#endif

// Per-model constants
#if SHADER_BUILD
cbuffer CBufModel_GBuffer :register(b1)
{
	// Object transform
	matrix m_o2s :packoffset(c0); // object to screen
	matrix m_o2w :packoffset(c4); // object to world

	// Tinting
	float4 m_tint :packoffset(c8); // object tint colour

	// Texture2D
	matrix m_tex2surf0 :packoffset(c9); // texture to surface transform

	// Geometry type
	int4 m_geom :packoffset(c13); // x = has pvc, y = has normals, z = has tex0, w = not used
};
#else
struct CBufModel_GBuffer
{
	// Object transform
	pr::m4x4   m_o2s;
	pr::m4x4   m_o2w;

	// Tinting
	pr::Colour m_tint;

	// Texture2D
	pr::m4x4 m_tex2surf0;

	// Geometry type
	pr::iv4 m_geom;
};
#endif

#endif