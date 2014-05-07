//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
#ifndef PR_RDR_SHADER_SHADOW_MAP_HLSLI
#define PR_RDR_SHADER_SHADOW_MAP_HLSLI

#include "shadow_map_cbuf.hlsli"

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
	//float4 ws_pos  :Position1;
	//float4 ws_norm :Normal;
	//float4 diff    :Color0;
	//float2 tex     :TexCoord0;
};

// Vertex shader
#if PR_RDR_SHADER_VS
PS_INPUT main(VS_INPUT In)
{
	PS_INPUT Out;
	//Out.pos      = 0;
	//Out.ws_pos   = 0;
	//Out.ss_pos   = 0;
	//float4 ms_pos  = float4(In.pos  ,1);
	//float4 ms_norm = float4(In.norm ,0);

	//// SMap
	//Out.ws_pos = mul(ms_pos, g_object_to_world);
	//Out.pos    = mul(Out.ws_pos, g_world_to_smap);
	//Out.ss_pos = Out.pos.xy;

	Out.ss_pos = float4(0,0,0,0);
	return Out;
}
#endif

#endif
