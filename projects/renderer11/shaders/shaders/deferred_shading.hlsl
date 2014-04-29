//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Uses gbuffer output to light a scene

#include "common/deferred_shading_cbuf.hlsli"
#include "common/compression.hlsli"

// VS input format
struct VS_INPUT
{
	float3 pos  :Position;
	float2 tex0 :TexCoord0;
};

// PS input format
struct PS_INPUT
{
	float4 ss_pos :SV_Position;
	float2 tex0   :TexCoord0;
};

// PS output format
struct PS_OUTPUT
{
	float4 diff0 :SV_Target0;
};

// Sampler and gbuffer textures
SamplerState m_point_sampler :register(s0);
Texture2D<float4> m_tex_diffuse :register(t0);
Texture2D<half2> m_tex_normals :register(t1);
Texture2D<float4> m_tex_depth :register(t2);

// Vertex shader
#if PR_RDR_SHADER_VS
PS_INPUT main(VS_INPUT In)
{
	PS_INPUT Out;
	Out.ss_pos = float4(In.pos ,1);
	Out.tex0 = In.tex0;
	return Out;
}
#endif

// Pixel shader
#if PR_RDR_SHADER_PS
PS_OUTPUT main(PS_INPUT In)
{
	PS_OUTPUT Out;

	// Sample the gbuffer
	float4 diff0 = m_tex_diffuse.Sample(m_point_sampler, In.tex0);
	half2 enc_norm = m_tex_normals.Sample(m_point_sampler, In.tex0);
	float4 norm = float4(DecodeNormal(enc_norm), 0);

	// Do lighting...

	// Output the lit pixel
	Out.diff0 = diff0;
	return Out;
}
#endif
