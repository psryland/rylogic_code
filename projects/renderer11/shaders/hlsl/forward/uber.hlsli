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
// PR_RDR_SHADER_PSIN_SSPOS4 - include screen space position (float4) in the vertex shader output
// PR_RDR_SHADER_PSIN_WSPOS4 - include world space position (float4) in the vertex shader output
// PR_RDR_SHADER_PSIN_DIFF0  - include pvc (float4) in the vertex shader output
// PR_RDR_SHADER_PSIN_2DTEX0 - include 2d tex coords (float2) in the vertex shader output
// PR_RDR_SHADER_TXFM         - use standard object to world transforms of verts
// PR_RDR_SHADER_TXFMWS       - use standard object to world transforms of verts
// PR_RDR_SHADER_TINT0        - base diffuse colour for the whole object
// PR_RDR_SHADER_DIFF0        - use a diffuse base colour per vertex
// PR_RDR_SHADER_TEX0         - use a diffuse base texture
// PR_RDR_SHADER_PVC          - use per-vertex colour when determining base diffuse colour

#ifndef PR_RDR_SHADER_UBER_HLSL
#define PR_RDR_SHADER_UBER_HLSL

#include "uber_defines.hlsli"
#include "forward_cbuf.hlsli"
#include "phong_lighting.hlsli"

// VS input format
struct VS_INPUT
{
	EXPAND(float3 pos   :Position  ;,PR_RDR_SHADER_VSIN_POS3  )
	EXPAND(float4 diff0 :Color0    ;,PR_RDR_SHADER_VSIN_DIFF0 )
	EXPAND(float3 norm  :Normal    ;,PR_RDR_SHADER_VSIN_NORM3 )
	EXPAND(float2 tex0  :TexCoord0 ;,PR_RDR_SHADER_VSIN_2DTEX0)
};

// PS input format
struct PS_INPUT
{
	EXPAND(float4 ss_pos  :SV_Position ;,PR_RDR_SHADER_PSIN_SSPOS4)
	EXPAND(float4 ws_pos  :Position1   ;,PR_RDR_SHADER_PSIN_WSPOS4)
	EXPAND(float4 ws_norm :Normal      ;,PR_RDR_SHADER_PSIN_WSNORM4)
	EXPAND(float4 diff0   :Color0      ;,PR_RDR_SHADER_PSIN_DIFF0 )
	EXPAND(float2 tex0    :TexCoord0   ;,PR_RDR_SHADER_PSIN_2DTEX0)
};

// PS output format
struct PS_OUTPUT
{
	float4 diff0 :SV_Target;
};

// Main vertex shader
#if PR_RDR_SHADER_VS
PS_INPUT main(VS_INPUT In)
{
	PS_INPUT Out;
	EXPAND(float4 ms_pos  = float4(In.pos ,1); , PR_RDR_SHADER_VSIN_POS3 )
	EXPAND(float4 ms_norm = float4(In.norm,0); , PR_RDR_SHADER_VSIN_NORM3)

	// Transform
	EXPAND(Out.ss_pos  = mul(ms_pos, m_o2s); , PR_RDR_SHADER_TXFM  )
	EXPAND(Out.ws_pos  = mul(ms_pos, m_o2w); , PR_RDR_SHADER_TXFMWS)
	EXPAND(Out.ws_norm = mul(ms_norm, m_n2w); , PR_RDR_SHADER_TXFMWS)

	// Tinting
	EXPAND(Out.diff0 = m_tint; , PR_RDR_SHADER_TINT0)

	// Per Vertex colour
	EXPAND(Out.diff0 = In.diff0 * Out.diff0; , PR_RDR_SHADER_PVC)

	// Texture2D (with transform)
	EXPAND(Out.tex0 = mul(float4(In.tex0,0,1), m_tex2surf0).xy; , PR_RDR_SHADER_TEX0)

	return Out;
}
#endif

// Main pixel shader
#if PR_RDR_SHADER_PS
PS_OUTPUT main(PS_INPUT In)
{
	PS_OUTPUT Out;
	Out.diff0 = float4(1,1,1,1);

	// Transform
	EXPAND(In.ws_norm = normalize(In.ws_norm); , PR_RDR_SHADER_TXFMWS)

	// Tinting
	EXPAND(Out.diff0 = In.diff0; , PR_RDR_SHADER_TINT0)

	// Texture2D (with transform)
	EXPAND(Out.diff0 = m_texture0.Sample(m_sampler0, In.tex0) * Out.diff0; , PR_RDR_SHADER_TEX0)

	// Projected textures
	//EXPAND(Out.diff0 = ProjTex(In.ws_pos, Out.diff0); , PR_RDR_SHADER_PROJTEX)

	// Lighting
	EXPAND(Out.diff0 = Illuminate(In.ws_pos, In.ws_norm, m_c2w[3], Out.diff0); , PR_RDR_SHADER_LIGHTING)

	return Out;
}
#endif

#if PR_RDR_SHADER_GS
//  input topology    size of input array           comments
//  point             1                             a single vertex of input geometry
//  line              2                             two adjacent vertices
//  triangle          3                             three vertices of a triangle
//  lineadj           4                             two vertices defining a line segment, as well as vertices adjacent to the segment end points
//  triangleadj       6                             describes a triangle as well as 3 surrounding triangles
//  output topology        comments
//  PointStream            output is interpreted as a series of disconnected points
//  LineStream             each vertex in the list is connected with the next by a line segment
//  TriangleStream         the first 3 vertices in the stream will become the first triangle; any additional vertices become additional triangles (when combined with the previous 2)

// Stereo triangle geometry shader
[maxvertexcount(6)]
void gs_main(triangle PS_INPUT In[3], inout TriangleStream<PS_INPUT> TriStream)
{
	PS_INPUT Out;
	float4x4 w2s;

	// Left
	w2s = m_w2s;
	for (int i = 0; i != 3; ++i)
	{
		Out.ss_pos   = mul(w2s, In[i].ws_pos);
		Out.ws_pos   = In[i].ws_pos;
		Out.ws_norm  = In[i].ws_norm;
		Out.diff0    = In[i].diff0;
		Out.tex0     = In[i].tex0;
		TriStream.Append(Out);
	}
	TriStream.RestartStrip();

	// Right
	w2s = m_w2s;
	for (int i = 0; i != 3; ++i)
	{
		Out.ss_pos   = mul(w2s, In[i].ws_pos);
		Out.ws_pos   = In[i].ws_pos;
		Out.ws_norm  = In[i].ws_norm;
		Out.diff0    = In[i].diff0;
		Out.tex0     = In[i].tex0;
		TriStream.Append(Out);
	}
	TriStream.RestartStrip();
}
#endif

#endif