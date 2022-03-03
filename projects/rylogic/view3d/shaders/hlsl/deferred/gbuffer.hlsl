//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#include "gbuffer_cbuf.hlsli"
#include "gbuffer.hlsli"

// Diffuse texture0 /w sampler
SamplerState      m_sampler0 :register(s0);
Texture2D<float4> m_texture0 :register(t0);

// Vertex shader
#ifdef PR_RDR_VSHADER_gbuffer
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

// Pixel shader
#ifdef PR_RDR_PSHADER_gbuffer
PSOut_GBuffer main(PSIn In)
{
	// Transform
	float4 ws_vert = In.ws_vert;
	float4 ws_norm = HAS_NORMALS ? normalize(In.ws_norm) : float4(0,0,0,0);

	// Tinting
	float4 diff = In.diff;

	// Texture2D (with transform)
	if (HAS_TEX0)
		diff = m_texture0.Sample(m_sampler0, In.tex0) * diff;

	// Generate gbuffer output
	PSOut_GBuffer Out = WriteGBuffer(diff, ws_vert, ws_norm);
	return Out;
}
#endif
