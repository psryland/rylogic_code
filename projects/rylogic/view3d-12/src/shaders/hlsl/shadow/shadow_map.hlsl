//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
#include "shadow_map_cbuf.hlsli"
#include "../common/functions.hlsli"

// Texture2D /w sampler
Texture2D<float4> m_texture0 :reg(t0,0);
SamplerState      m_sampler0 :reg(s0,0);

struct PSIn_ShadowMap
{
	float4 ss_vert :SV_Position;
	float4 ws_vert :Position1;
	float4 diff :Color0;
	float2 tex0 :TexCoord0;
};
struct PSOut
{
	float shade :SV_Target0;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_shadow_map
PSIn_ShadowMap main(VSIn In)
{
	PSIn_ShadowMap Out;

	float2 nf = ClipPlanes(m_l2s);
	float4 ws_vert = mul(In.vert, m_o2w);
	float4 ls_vert = mul(ws_vert, m_w2l);

	// Transform. Set ws_vert.w to normalised distance from light
	Out.ws_vert = ws_vert;
	Out.ws_vert.w = Frac(nf.y, -ls_vert.z, nf.x);
	Out.ss_vert = mul(ls_vert, m_l2s);

	// Tinting
	Out.diff = m_tint;

	// Per Vertex colour
	Out.diff = In.diff * Out.diff;

	// Texture2D (with transform)
	Out.tex0 = mul(float4(In.tex0,0,1), m_tex2surf0).xy;

	return Out;
}
#endif

// Pixel shader
#ifdef PR_RDR_PSHADER_shadow_map
PSOut main(PSIn_ShadowMap In)
{
	PSOut Out;

	float4 diff = In.diff;

	// Texture2D (with transform)
	if (HasTex0)
		diff = m_texture0.Sample(m_sampler0, In.tex0) * diff;

	// If not alpha blending, clip alpha pixels
	if (!HasAlpha)
		clip(diff.a - 0.5);

	Out.shade = In.ws_vert.w;
	return Out;
}
#endif
