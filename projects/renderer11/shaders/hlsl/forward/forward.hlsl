//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Shader for forward rendering face data
#ifndef PR_RDR_SHADER_FORWARD_HLSL
#define PR_RDR_SHADER_FORWARD_HLSL

#include "../cbuf.hlsli"
#include "../inout.hlsli"

#define PR_RDR_MAX_PROJECTED_TEXTURES 1

// 'CBufFrame' is a cbuffer managed by a scene.
// It contains values constant for the whole frame.
// It is defined for every shader because most will probably need it
cbuffer CBufFrame :cbuf_bank(b0)
{
	// Camera transform
	float4x4 m_c2w; // camera to world
	float4x4 m_c2s; // camera to screen
	float4x4 m_w2c; // world to camera
	float4x4 m_w2s; // world to screen
	
	// Global lighting
	// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
	float4 m_light_info;         // Encoded info for global lighting
	float4 m_ws_light_direction; // The direction of the global light source
	float4 m_ws_light_position;  // The position of the global light source
	float4 m_light_ambient;      // The colour of the ambient light
	float4 m_light_colour;       // The colour of the directional light
	float4 m_light_specular;     // The colour of the specular light. alpha channel is specular power
	float4 m_spot;               // x = inner cos angle, y = outer cos angle, z = range, w = falloff

	// Projected textures
	float4 m_proj_tex_count;
	float4x4 m_proj_tex[PR_RDR_MAX_PROJECTED_TEXTURES];
};

// 'CBufModel' is a cbuffer updated per render nugget.
// Shaders can select components from this structure as needed
cbuffer CBufModel :cbuf_bank(b1)
{
	// Object transform
	float4x4 m_o2s; // object to screen
	float4x4 m_o2w; // object to world
	float4x4 m_n2w; // normal to world

	// Tinting
	float4 m_tint; // object tint colour

	// Texture2D
	float4x4 m_tex2surf0; // texture to surface transform

	// Geometry type
	int4 m_geom;  // x = 1 => has normals, y = 1 => has tex0, z,w = not used
};

#include "../phong_lighting.hlsli"

#if SHADER_BUILD

// Texture2D /w sampler
Texture2D<float4> m_texture0 :register(t0);
SamplerState      m_sampler0 :register(s0);

// Projected textures
Texture2D<float4> m_proj_texture[PR_RDR_MAX_PROJECTED_TEXTURES];
SamplerState      m_proj_sampler[PR_RDR_MAX_PROJECTED_TEXTURES];

#define HAS_NORMALS m_geom.x == 1
#define HAS_TEX0 m_geom.y == 1

// PS output format
struct PSOut
{
	float4 diff :SV_Target;
};

// Main vertex shader
#if PR_RDR_SHADER_VS
PSIn main(VSIn In)
{
	PSIn Out;

	// Transform
	Out.ss_vert = mul(In.vert, m_o2s);
	Out.ws_vert = mul(In.vert, m_o2w);
	Out.ws_norm = mul(In.norm, m_n2w);

	// Tinting
	Out.diff = m_tint;

	// Per Vertex colour
	Out.diff = In.diff * Out.diff;

	// Texture2D (with transform)
	Out.tex0 = mul(float4(In.tex0,0,1), m_tex2surf0).xy;

	return Out;
}
#endif

// Main pixel shader
#if PR_RDR_SHADER_PS
PSOut main(PSIn In)
{
	PSOut Out;

	// Transform
	In.ws_norm = normalize(In.ws_norm);

	// Tinting
	Out.diff = In.diff;

	// Texture2D (with transform)
	if (HAS_TEX0)
		Out.diff = m_texture0.Sample(m_sampler0, In.tex0) * Out.diff;
	//if (HAS_PROJTEX) // Projected textures
	//	Out.diff0 = ProjTex(In.ws_pos, Out.diff);

	// Lighting
	if (HAS_NORMALS)
		Out.diff = Illuminate(In.ws_vert, In.ws_norm, m_c2w[3], Out.diff);

	return Out;
}
#endif

#endif
#endif