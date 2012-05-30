//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_UBER_CBUFFER_HLSL
#define PR_RDR_SHADER_UBER_CBUFFER_HLSL

#if SHADER_BUILD
#include "uber_defines.hlsl"
#endif

// Notes:
// For efficiency, constant buffers need to be grouped by frequency of update
// The C/C++ versions of the buffer structs should contain elements assuming
// every struct member is selected. Make sure the packoffset is correct for
// each member.

// 'CBufFrame' is a cbuffer managed by a scene.
// It contains values constant for the whole frame.
// It is defined for every shader because most will probably need it
#if SHADER_BUILD
cbuffer CBufFrame :register(b0)
{
	// Camera transform
	matrix m_c2w :packoffset(c0); // camera to world
	matrix m_w2c :packoffset(c4); // world to camera
};
#else
struct CBufFrame
{
	pr::m4x4 m_c2w;
	pr::m4x4 m_w2c;
};
#endif


// 'CBufModel' is a cbuffer updated per render nugget.
// Shaders can select components from this structure as needed
#if SHADER_BUILD
cbuffer CBufModel :register(b1)
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
struct CBufModel
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

// Texture2D /w sampler
#if SHADER_BUILD
Texture2D    m_texture0 :register(t0);
SamplerState m_tex0     :register(s0);
#endif

#endif
