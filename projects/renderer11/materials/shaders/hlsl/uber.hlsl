//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Uber shader defines:
// PR_RDR_SHADER_VS     - define to build a vertex shader
// PR_RDR_SHADER_PS     - define to build a pixel shader
// PR_RDR_SHADER_TXFM   - define to use standard object to world transforms of verts
// PR_RDR_SHADER_TXFMWS - define to use standard object to world transforms of verts
// PR_RDR_SHADER_TINT0  - define for base diffuse colour for the whole object
// PR_RDR_SHADER_DIFF0  - define to use a diffuse base colour per vertex
// PR_RDR_SHADER_TEX0   - define to use a diffuse base texture

#ifndef PR_RDR_SHADER_UBER_HLSL
#define PR_RDR_SHADER_UBER_HLSL

// #pragma warning (disable:3557)
// #define TINY 0.0005f

#include "uber_cbuffer.hlsl"
#include "uber_defines.hlsl"
#include "functions.hlsl"

// Vertex input format
#if PR_RDR_SHADER_VS
struct VS_INPUT
{
	EXPAND(float3 pos   :Position;  , PR_RDR_SHADER_TXFM##PR_RDR_SHADER_TXFMWS)
	EXPAND(float3 norm  :Normal;    , PR_RDR_SHADER_NORM )
	EXPAND(float4 diff0 :Color0;    , PR_RDR_SHADER_DIFF0)
	EXPAND(float2 tex0  :TexCoord0; , PR_RDR_SHADER_TEX0 )
};
#endif

// Vertex output format
#if PR_RDR_SHADER_VS || PR_RDR_SHADER_PS
struct VS_OUTPUT
{
	EXPAND(float4 ss_pos :SV_Position; , PR_RDR_SHADER_TXFM   )
	EXPAND(float4 ws_pos :Position1;   , PR_RDR_SHADER_TXFMWS )
	EXPAND(float4 diff0  :Color0;      , PR_RDR_SHADER_DIFF0##PR_RDR_SHADER_TINT0)
	EXPAND(float2 uv0    :TexCoord0;   , PR_RDR_SHADER_TEX0   )
};
#endif

// Pixel output format
#if PR_RDR_SHADER_PS
struct PS_OUTPUT
{
	float4 diff0 :SV_Target;
};
#endif

// Main vertex shader
#if PR_RDR_SHADER_VS
VS_OUTPUT main(VS_INPUT In)
{
	VS_OUTPUT Out;
	EXPAND(float4 ms_pos  = float4(In.pos,1);  , PR_RDR_SHADER_TXFM##PR_RDR_SHADER_TXFMWS)
	EXPAND(float4 ms_norm = float4(In.norm,0); , PR_RDR_SHADER_NORM  )
	
	// Transform
	EXPAND(Out.ss_pos = mul(ms_pos , m_o2s);  , PR_RDR_SHADER_TXFM  )
	EXPAND(Out.ws_pos = mul(ms_pos , m_o2s);  , PR_RDR_SHADER_TXFMWS)
	EXPAND(Out.norm   = mul(ms_norm, m_n2w);  , PR_RDR_SHADER_NORM  )
	
	// Tinting
	EXPAND(Out.diff0 = m_tint; , PR_RDR_SHADER_TINT0)
	return Out;
}
#endif

// Main pixel shader
#if PR_RDR_SHADER_PS
PS_OUTPUT main(VS_OUTPUT In)
{
	PS_OUTPUT Out;
	Out.diff0 = float4(1,1,0,1);
	
	// Tinting
	EXPAND(Out.diff0 = In.diff0; , PR_RDR_SHADER_TINT0)
	
	return Out;
}
#endif

#endif
