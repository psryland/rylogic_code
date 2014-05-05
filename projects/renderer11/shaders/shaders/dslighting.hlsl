//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

#include "common/gbuffer_cbuf.hlsli"
#include "common/gbuffer.hlsli"

// VS input format
struct VS_INPUT
{
	float3 pos  :Position;
	float4 diff :Color0;
	float3 norm :Normal;
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
	// Sample the gbuffer
	GPixel px = ReadGBuffer(In.tex0);
	PS_OUTPUT Out;

	// Do lighting...

	// Output the lit pixel
	//Out.diff0 = px.diff;
	//Out.diff0 = abs(px.ws_norm);
	//Out.diff0 = saturate(0.5f * (1 - px.ws_pos.z)) * float4(1,1,1,1);
	Out.diff0 = float4(1,1,1,1) * abs(frac(px.ws_pos.z));
	//Out.diff0 = float4(1,0,1,1);
	return Out;
}
#endif
