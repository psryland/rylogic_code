//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

#include "shadow_map_cbuf.hlsli"
#include "../inout.hlsli"

// PS input format
struct PSIn_ShadowMap
{
	// The positions of the vert projected onto each face of the frustum
	float4 ss_vert  :SV_Position;
	float4 ws_vert  :TexCoord0;  // Projection index passed in w
};

struct PSOut
{
	// Expects a 2-channel texture, r = depth on wedge end of frustum, g = depth on far plane
	float2 depth :SV_Target;
};

// Vertex shader
#ifdef PR_RDR_VSHADER_shadow_map
PSIn_ShadowMap main(VSIn In)
{
	// Pass the ws_vert to the GS
	PSIn_ShadowMap Out;
	Out.ss_vert = float4(0,0,0,0);
	Out.ws_vert = mul(In.vert, m_o2w);
	return Out;
}
#endif

// Geometry shader
#ifdef PR_RDR_GSHADER_shadow_map_face
[maxvertexcount(15)]
void main(triangle PSIn_ShadowMap In[3], inout TriangleStream<PSIn_ShadowMap> TriStream)
{
	// Replicate the face for each projection and
	// project it onto the plane of the frustum
	PSIn_ShadowMap Out;
	for (int i = 0; i != 5; ++i)
	for (int j = 0; j != 3; ++j)
	{
		Out = In[j];
		Out.ws_vert.w = float(i);
		Out.ss_vert = mul(In[j].ws_vert, m_proj[i]);
		TriStream.Append(Out);
	}
}
#endif
#ifdef PR_RDR_GSHADER_shadow_map_line
[maxvertexcount(10)]
void main(line PSIn_ShadowMap In[2], inout TriangleStream<PSIn_ShadowMap> TriStream)
{
	// Replicate the line for each projection and
	// project it onto the plane of the frustum
	PSIn_ShadowMap Out;
	for (int i = 0; i != 5; ++i)
	for (int j = 0; j != 2; ++j)
	{
		Out = In[j];
		Out.ws_vert.w = float(i);
		Out.ss_vert = mul(In[j].ws_vert, m_proj[i]);
		TriStream.Append(Out);
	}
}
#endif

// Pixel shader
#ifdef PR_RDR_PSHADER_shadow_map
PSOut main(PSIn_ShadowMap In)
{
	// Output the depth in the appropriate position in the RT
	PSOut Out;
	float4 d = In.ss_vert / In.ss_vert.w;
	clip(In.ws_vert.w == 4 ? 1 : -1);
	Out.depth = float2(0,abs(d.z));
	return Out;
}
#endif
