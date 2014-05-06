//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
#ifndef PR_RDR_SHADER_GBUFFER_HLSLI
#define PR_RDR_SHADER_GBUFFER_HLSLI

#include "common/gbuffer_cbuf.hlsli"

// Sampler and gbuffer textures
SamplerState      m_point_sampler :register(s0);
Texture2D<float4> m_tex_diffuse   :register(t0);
Texture2D<float2> m_tex_normals   :register(t1);
Texture2D<float>  m_tex_depth     :register(t2);

// Gbuffer Px out format
struct PS_OUTPUT_GBUFFER
{
	float4 diff     :SV_Target0;
	float2 ws_norm  :SV_Target1;
	float  cs_depth :SV_Target2;
};

// Decoded gbuffer value
struct GPixel
{
	float4 diff;
	float4 ws_norm;
	float4 cs_pos;
};

// Output a pixel for the gbuffer
PS_OUTPUT_GBUFFER WriteGBuffer(float4 diff, float4 ws_pos, float4 ws_norm)
{
	PS_OUTPUT_GBUFFER Out;
	Out.diff = float4(diff.xyz, step(0,ws_norm.z));
	Out.ws_norm = ws_norm.xy * 0.5f + 0.5f;
	Out.cs_depth = length(ws_pos - m_c2w[3]);
	return Out;
}

// Sample the gbuffer
GPixel ReadGBuffer(float2 tex, float3 cs_vdir)
{
	float4 diff = m_tex_diffuse.Sample(m_point_sampler, tex);
	float2 norm = m_tex_normals.Sample(m_point_sampler, tex);
	float depth = m_tex_depth.Sample(m_point_sampler, tex);
	
	GPixel Out;
	Out.diff = float4(diff.xyz, 0);

	norm = norm * 2 - 1;
	Out.ws_norm = normalize(float4(norm, (2*diff.w-1)*sqrt(saturate(1 - dot(norm,norm))), 0));

	Out.cs_pos = float4(cs_vdir * depth, 1);
	return Out;
}

#endif
