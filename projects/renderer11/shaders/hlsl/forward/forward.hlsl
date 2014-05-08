//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Shader for forward rendering face data
#ifndef PR_RDR_SHADER_FORWARD_HLSL
#define PR_RDR_SHADER_FORWARD_HLSL

#include "forward_cbuf.hlsli"
#include "..\inout.hlsli"
#include "..\phong_lighting.hlsli"

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