//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************
#ifndef PR_RDR_SHADER_GBUFFER_HLSLI
#define PR_RDR_SHADER_GBUFFER_HLSLI

#include "gbuffer_cbuf.hlsli"

// Sampler and gbuffer textures
SamplerState      m_point_sampler :register(s0);
Texture2D<float4> m_tex_diffuse   :register(t0);
Texture2D<float2> m_tex_normals   :register(t1);
Texture2D<float>  m_tex_depth     :register(t2);

// Gbuffer Px out format
struct PSOut_GBuffer
{
	float4 diff     :SV_Target0;
	float2 ws_norm  :SV_Target1;
	float  cs_depth :SV_Target2;
};

// Decoded gbuffer value
struct GPixel
{
	float4 diff;     // Unlit diffuse
	float4 ws_norm;  // world space normal
	float4 cs_vert;  // Camera space position
	float  cs_depth; // Distance from camera
};

// Output a pixel for the gbuffer
PSOut_GBuffer WriteGBuffer(float4 diff, float4 ws_pos, float4 ws_norm)
{
	PSOut_GBuffer Out;
	Out.diff = float4(diff.xyz, sign(ws_norm.z) * 0.5f + 0.5f);
	Out.ws_norm = ws_norm.xy * 0.5f + 0.5f;
	Out.cs_depth = length(ws_pos - m_cam.m_c2w[3]);
	return Out;
}

// Sample the gbuffer
GPixel ReadGBuffer(float2 tex, float3 cs_vdir)
{
	GPixel Out;
	float4 diff = m_tex_diffuse.Sample(m_point_sampler, tex);
	float2 norm = m_tex_normals.Sample(m_point_sampler, tex);
	float depth = m_tex_depth.Sample(m_point_sampler, tex);

	Out.diff = float4(diff.xyz, 1);

	float4 ws_norm = float4(norm * 2 - 1, diff.w * 2 - 1, 0);
	float has_norm = dot(ws_norm,ws_norm);
	Out.ws_norm = step(0.5f, has_norm) * float4(ws_norm.xy, ws_norm.z * sqrt(saturate(1 - dot(ws_norm.xy,ws_norm.xy))), 0);

	Out.cs_vert = float4(cs_vdir * depth, 1);
	Out.cs_depth = depth;
	return Out;
}

#endif
