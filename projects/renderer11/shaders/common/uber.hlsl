//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Uber shader defines:
// PR_RDR_SHADER_VS           - build a vertex shader
// PR_RDR_SHADER_PS           - build a pixel shader
// PR_RDR_SHADER_CBUFFRAME    - include CBufFrame constant buffer
// PR_RDR_SHADER_CBUFMODEL    - include CBufModel constant buffer
// PR_RDR_SHADER_VSIN_POS3    - include position (float3) in the vertex shader input
// PR_RDR_SHADER_VSIN_NORM3   - include normal (float3) in the vertex shader input
// PR_RDR_SHADER_VSIN_DIFF0   - include pvc (float4) in the vertex shader input
// PR_RDR_SHADER_VSIN_2DTEX0  - include 2d tex coords (float2) in the vertex shader input
// PR_RDR_SHADER_VSOUT_SSPOS4 - include screen space position (float4) in the vertex shader output
// PR_RDR_SHADER_VSOUT_WSPOS4 - include world space position (float4) in the vertex shader output
// PR_RDR_SHADER_VSOUT_DIFF0  - include pvc (float4) in the vertex shader output
// PR_RDR_SHADER_VSOUT_2DTEX0 - include 2d tex coords (float2) in the vertex shader output
// PR_RDR_SHADER_TXFM         - use standard object to world transforms of verts
// PR_RDR_SHADER_TXFMWS       - use standard object to world transforms of verts
// PR_RDR_SHADER_TINT0        - base diffuse colour for the whole object
// PR_RDR_SHADER_DIFF0        - use a diffuse base colour per vertex
// PR_RDR_SHADER_TEX0         - use a diffuse base texture
// PR_RDR_SHADER_PVC          - use per-vertex colour when determining base diffuse colour

#ifndef PR_RDR_SHADER_UBER_HLSL
#define PR_RDR_SHADER_UBER_HLSL

// #pragma warning (disable:3557)
// #define TINY 0.0005f

#include "uber_defines.hlsl"
#include "cbuffer.hlsl"
#include "functions.hlsl"

// Vertex input format
#if PR_RDR_SHADER_VS
struct VS_INPUT
{
	EXPAND(float3 pos   :Position  ;,PR_RDR_SHADER_VSIN_POS3  )
	EXPAND(float3 norm  :Normal    ;,PR_RDR_SHADER_VSIN_NORM3 )
	EXPAND(float4 diff0 :Color0    ;,PR_RDR_SHADER_VSIN_DIFF0 )
	EXPAND(float2 tex0  :TexCoord0 ;,PR_RDR_SHADER_VSIN_2DTEX0)
};
#endif

// Vertex output format
#if PR_RDR_SHADER_VS || PR_RDR_SHADER_PS
struct VS_OUTPUT
{
	EXPAND(float4 ss_pos :SV_Position ;,PR_RDR_SHADER_VSOUT_SSPOS4)
	EXPAND(float4 ws_pos :Position1   ;,PR_RDR_SHADER_VSOUT_WSPOS4)
	EXPAND(float4 diff0  :Color0      ;,PR_RDR_SHADER_VSOUT_DIFF0 )
	EXPAND(float2 uv0    :TexCoord0   ;,PR_RDR_SHADER_VSOUT_2DTEX0)
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
	EXPAND(float4 ms_pos  = float4(In.pos ,1) ;,PR_RDR_SHADER_VSIN_POS3 )
	EXPAND(float4 ms_norm = float4(In.norm,0) ;,PR_RDR_SHADER_VSIN_NORM3)
	
	// Transform
	EXPAND(Out.ss_pos = mul(m_o2s ,ms_pos ) ;,PR_RDR_SHADER_TXFM  )
	EXPAND(Out.ws_pos = mul(m_o2w ,ms_pos ) ;,PR_RDR_SHADER_TXFMWS)
	EXPAND(Out.norm   = mul(m_n2w ,ms_norm) ;,PR_RDR_SHADER_TXFMWS)
	
	// Tinting
	EXPAND(Out.diff0 = m_tint ;,PR_RDR_SHADER_TINT0)
	
	// Per Vertex colour
	EXPAND(Out.diff0 = In.diff0 * Out.diff0 ;,PR_RDR_SHADER_PVC)
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
