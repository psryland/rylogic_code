//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

#include "common/gbuffer_cbuf.hlsli"
#include "common/gbuffer.hlsli"
#include "common/phong_lighting.hlsli"

// VS input format
struct VS_INPUT
{
	float3 pos  :Position;    // x,y = unused.  z = view frustum corner index
	float4 diff :Color0;      // unused
	float3 norm :Normal;      // unused
	float2 tex  :TexCoord0;
};

// PS input format
struct PS_INPUT
{
	float4 ss_pos  :SV_Position;
	float2 tex     :TexCoord0;
	float4 cs_vdir :TexCoord1;    // direction vector to the  projected point on the view frustum far plane
};

// PS output format
struct PS_OUTPUT
{
	float4 diff :SV_Target0;
};

// Vertex shader
#if PR_RDR_SHADER_VS
PS_INPUT main(VS_INPUT In)
{
	PS_INPUT Out;
	Out.cs_vdir = m_frustum[(int)In.pos.x];
	Out.ss_pos = mul(Out.cs_vdir, m_c2s);
	Out.tex = In.tex;
	return Out;
}
#endif

// Pixel shader
#if PR_RDR_SHADER_PS
PS_OUTPUT main(PS_INPUT In)
{
	PS_OUTPUT Out;

	// Sample the gbuffer
	GPixel px = ReadGBuffer(In.tex, normalize(In.cs_vdir.xyz));
	float4 ws_pos = mul(px.cs_pos, m_c2w);

	// Basic diffuse
	Out.diff = px.diff;

	// Do lighting...
	if (dot(px.ws_norm,px.ws_norm) > 0.5f)
		Out.diff = Illuminate(ws_pos, px.ws_norm, m_c2w[3], Out.diff);

	//Out.diff = float4(1,0,1,1);
	//Out.diff = px.diff;
	//Out.diff = abs(float4(px.ws_norm,0));
	//Out.diff = float4(1,1,1,1) * m_tex_depth.Sample(m_point_sampler, In.tex)*0.05f;
	//Out.diff = saturate(0.5f * (1 - ws_pos.z)) * float4(1,1,1,1);
	//Out.diff = float4(1,1,1,1) * frac(px.cs_pos.x);
	//Out.diff = float4(1,1,1,1) * (ws_pos.x);
	//Out.diff = frac(float4(1,1,1,1) * ws_pos.x);
	//Out.diff = normalize(float4(abs(In.cs_vdir),1));
	//Out.diff = float4(In.tex,0,1);
	return Out;
}
#endif
