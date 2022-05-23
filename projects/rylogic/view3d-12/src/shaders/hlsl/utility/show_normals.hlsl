//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#include "../types.hlsli"
#include "../forward/forward_cbuf.hlsli"

// Converts point geometry into normal vectors
#ifdef PR_RDR_GSHADER_show_normals
[maxvertexcount(2)]
void main(point PSIn In[1], inout LineStream<PSIn> OutStream)
{
	PSIn Out = In[0];
	Out.diff = m_colour;
	Out.ws_norm = float4(0,0,0,0);
	
	Out.ws_vert = In[0].ws_vert;
	Out.ss_vert = mul(Out.ws_vert, m_cam.m_w2s);
	OutStream.Append(Out);

	Out.ws_vert = In[0].ws_vert + m_length * In[0].ws_norm;
	Out.ss_vert = mul(Out.ws_vert, m_cam.m_w2s);
	OutStream.Append(Out);

	OutStream.RestartStrip();
}
#endif
