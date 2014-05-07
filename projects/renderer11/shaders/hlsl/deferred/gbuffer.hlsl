//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

#include "gbuffer_cbuf.hlsli"
#include "gbuffer.hlsli"

// VS input format
struct VS_INPUT
{
	float3 pos  :Position;
	float4 diff :Color0;
	float3 norm :Normal;
	float2 tex  :TexCoord0;
};

// PS input format
struct PS_INPUT
{
	float4 ss_pos  :SV_Position;
	float4 ws_pos  :Position1;
	float4 ws_norm :Normal;
	float4 diff    :Color0;
	float2 tex     :TexCoord0;
};

// Diffuse texture0 /w sampler
SamplerState      m_sampler0 :register(s0);
Texture2D<float4> m_texture0 :register(t0);

// Vertex shader
#if PR_RDR_SHADER_VS
PS_INPUT main(VS_INPUT In)
{
	PS_INPUT Out;
	float4 ms_pos  = float4(In.pos ,1);
	float4 ms_norm = float4(In.norm,0);

	// Transform
	Out.ss_pos  = mul(ms_pos, m_o2s);
	Out.ws_pos  = mul(ms_pos, m_o2w);
	Out.ws_norm = m_geom.y != 0 ? mul(ms_norm, m_n2w) : float4(0,0,0,0);

	// Tinting
	Out.diff = m_tint;

	// Per Vertex colour
	if (m_geom.x != 0)
		Out.diff = In.diff * Out.diff;

	// Texture2D (with transform)
	Out.tex = m_geom.z != 0 ? mul(float4(In.tex,0,1), m_tex2surf0).xy : float2(0,0);

	return Out;
}
#endif

// Pixel shader
#if PR_RDR_SHADER_PS
PS_OUTPUT_GBUFFER main(PS_INPUT In)
{
	// Tinting
	float4 diff = In.diff;

	// Texture2D (with transform)
	diff = m_geom.z != 0 ? m_texture0.Sample(m_sampler0, In.tex) * diff : diff;

	// Transform
	float4 ws_pos = In.ws_pos;
	float4 ws_norm = m_geom.y != 0 ? normalize(In.ws_norm) : In.ws_norm;

	PS_OUTPUT_GBUFFER Out = WriteGBuffer(diff, ws_pos, ws_norm);
	return Out;
}
#endif
