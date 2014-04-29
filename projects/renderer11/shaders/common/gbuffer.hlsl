//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// G-Buffer generating shader
// Expects 3 32bit render targets:
//   Color0 = diffuse rgba
//   Color1 = normal x,y (16bits each)
//   Color2 = depth 32bit float (world space depth from camera)

#include "gbuffer_cbuf.hlsl"
#include "compression.inc.hlsl"

// VS input format
struct VS_INPUT
{
	float3 pos   :Position;
	float4 diff0 :Color0;
	float3 norm  :Normal;
	float2 tex0  :TexCoord0;
};

// PS input format
struct PS_INPUT
{
	float4 ss_pos  :SV_Position;
	float4 ws_pos  :Position1;
	float4 ws_norm :Normal;
	float4 diff0   :Color0;
	float2 tex0    :TexCoord0;
};

// PS output format
struct PS_OUTPUT
{
	float4 diff0    :SV_Target0;
	half2  ws_norm  :SV_Target1;
	float  ws_depth :SV_Target2;
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
	Out.ss_pos  = mul(m_o2s ,ms_pos );
	Out.ws_pos  = mul(m_o2w ,ms_pos );
	Out.ws_norm = m_geom.y != 0 ? mul(m_o2w ,ms_norm) : float4(0,0,1,0);

	// Tinting
	Out.diff0 = m_tint;

	// Per Vertex colour
	if (m_geom.x != 0)
		Out.diff0 = In.diff0 * Out.diff0;

	// Texture2D (with transform)
	Out.tex0 = m_geom.z != 0 ? mul(m_tex2surf0, float4(In.tex0,0,1)).xy : float2(0,0);

	return Out;
}
#endif

// Pixel shader
#if PR_RDR_SHADER_PS
PS_OUTPUT main(PS_INPUT In)
{
	PS_OUTPUT Out;

	// Tinting
	Out.diff0 = In.diff0;

	// Texture2D (with transform)
	if (m_geom.z != 0)
		Out.diff0 = m_texture0.Sample(m_sampler0, In.tex0) * Out.diff0;

	// Transform
	half3 ws_norm = m_geom.y != 0 ? normalize(In.ws_norm).xyz : In.ws_norm.xyz;
	Out.ws_norm = EncodeNormal(ws_norm);

	// Depth
	Out.ws_depth = In.ws_pos.z - m_c2w._43;

	return Out;
}
#endif
