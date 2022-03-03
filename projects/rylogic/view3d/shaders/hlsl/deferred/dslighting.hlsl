//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

#include "gbuffer_cbuf.hlsli"
#include "gbuffer.hlsli"
#include "..\lighting\phong_lighting.hlsli"

// PS input format
struct PSIn_DSLighting
{
	float4 ss_vert :SV_Position;
	float2 tex0    :TexCoord0;
	float4 cs_vdir :TexCoord1;    // direction vector to the  projected point on the view frustum far plane
};

// PS output format
struct PSOut
{
	float4 diff :SV_Target0;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_dslighting
PSIn_DSLighting main(VSIn In)
{
	PSIn_DSLighting Out;
	Out.cs_vdir = m_frustum[(int)In.vert.x];
	Out.ss_vert = mul(Out.cs_vdir, m_cam.m_c2s);
	Out.tex0 = In.tex0;
	return Out;
}
#endif

// Pixel shader
#ifdef PR_RDR_PSHADER_dslighting
PSOut main(PSIn_DSLighting In)
{
	PSOut Out;

	// Sample the gbuffer
	GPixel px = ReadGBuffer(In.tex0, normalize(In.cs_vdir.xyz));
	float4 ws_vert = mul(px.cs_vert, m_cam.m_c2w);

	// Basic diffuse
	Out.diff = px.diff;

	// Do lighting...
	Out.diff = Illuminate(m_light, ws_vert, px.ws_norm, m_cam.m_c2w[3], 1.0f, Out.diff);

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
	//Out.diff = float4(m_tex_normals.Sample(m_point_sampler, In.tex0), 0, 1);
	
	return Out;
}
#endif
