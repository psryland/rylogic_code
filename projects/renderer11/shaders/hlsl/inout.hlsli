//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
#ifndef PR_RDR_SHADER_INOUT_HLSL
#define PR_RDR_SHADER_INOUT_HLSL

// Vertex shader input format
struct VSIn
{
	float4 vert :Position;
	float4 diff :Color0;
	float4 norm :Normal;
	float2 tex0 :TexCoord0;
};

// Pixel shader input format
struct PSIn
{
	float4 ss_vert :SV_Position;
	float4 ws_vert :Position1;
	float4 ws_norm :Normal;
	float4 diff    :Color0;
	float2 tex0    :TexCoord0;
};

#endif

