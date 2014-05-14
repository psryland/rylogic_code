//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
#ifndef PR_RDR_SHADER_SHADOW_MAP_HLSLI
#define PR_RDR_SHADER_SHADOW_MAP_HLSLI

#include "shadow_map_cbuf.hlsli"
#include "../inout.hlsli"

// PS input format
struct PSIn_ShadowMap
{
	// The positions of the vert projected onto each face of the frustum
	float4 ss_vert   :SV_Position;
	float4 ss_vert0  :TexCoord0;
	float4 ss_vert1  :TexCoord1;
	float4 ss_vert2  :TexCoord2;
	float4 ss_vert3  :TexCoord3;
	float4 ss_vert4  :TexCoord4;
	//float4 ws_norm :Normal;
	//float4 diff    :Color0;
	//float2 tex     :TexCoord0;
};

struct PSOut
{
	// Expects a 2-channel texture, r = depth on wedge end of frustum, g = depth on far plane
	float2 depth :SV_Target;
};

// Vertex shader
#if PR_RDR_SHADER_VS
PSIn_ShadowMap main(VSIn In)
{
	PSIn_ShadowMap Out;
	
	float4 ws_vert = mul(In.vert, m_o2w);
	Out.ss_vert  = mul(In.vert, m_o2s);
	
	// Project the vertex onto each plane of the frustum
	Out.ss_vert0 = mul(ws_vert, m_proj[0]);
	Out.ss_vert1 = mul(ws_vert, m_proj[1]);
	Out.ss_vert2 = mul(ws_vert, m_proj[2]);
	Out.ss_vert3 = mul(ws_vert, m_proj[3]);
	Out.ss_vert4 = mul(ws_vert, m_proj[4]);
	
	//Out.pos      = 0;
	//Out.ws_pos   = 0;
	//Out.ss_pos   = 0;
	//float4 ms_pos  = float4(In.pos  ,1);
	//float4 ms_norm = float4(In.norm ,0);

	//// SMap
	//Out.ws_pos = mul(ms_pos, g_object_to_world);
	//Out.pos    = mul(Out.ws_pos, g_world_to_smap);
	//Out.ss_pos = Out.pos.xy;

	return Out;
}
#endif

#if PR_RDR_SHADER_PS
PSOut main(PSIn_ShadowMap In)
{
	PSOut Out;
	float4 d0 = In.ss_vert0 / In.ss_vert0.w;
	float4 d1 = In.ss_vert1 / In.ss_vert1.w;
	float4 d2 = In.ss_vert2 / In.ss_vert2.w;
	float4 d3 = In.ss_vert3 / In.ss_vert3.w;
	float4 d4 = In.ss_vert4 / In.ss_vert4.w;
	
	Out.depth = float2(0,100*d4.z);
	//Out.depth = d4.xy;
	return Out;
}
#endif

#endif
