//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
#include "shadow_map_cbuf.hlsli"
#include "../common/functions.hlsli"

struct PSIn_ShadowMap
{
	float4 ss_vert :SV_Position;
	float4 ws_vert :TexCoord0;
};
struct PSOut
{
	float2 shade :SV_Target0;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_shadow_map
PSIn_ShadowMap main(VSIn In)
{
	PSIn_ShadowMap Out;

	float4 ws_vert = mul(In.vert, m_o2w);
	float4 ls_vert = mul(ws_vert, m_w2l);

	float2 nf = ClipPlanes(m_l2s);

	// Transform. Set ws_vert.w to normalised distance from light
	Out.ws_vert = ws_vert;
	Out.ws_vert.w = Frac(nf.y, -ls_vert.z, nf.x);
	Out.ss_vert = mul(ls_vert, m_l2s);
	return Out;
}
#endif

// Pixel shader
#ifdef PR_RDR_PSHADER_shadow_map
PSOut main(PSIn_ShadowMap In)
{
	PSOut Out;
	Out.shade = float2(In.ws_vert.w, 0.0f);
	return Out;
}
#endif
