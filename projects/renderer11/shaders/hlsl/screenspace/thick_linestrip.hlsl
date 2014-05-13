//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_THICK_LINESTRIP_HLSL
#define PR_RDR_SHADER_THICK_LINESTRIP_HLSL

#include "../inout.hlsli"
#include "thick_line_cbuf.hlsli"

// Converts line geometry into tristrip
#if PR_RDR_SHADER_GS
[maxvertexcount(4)]
void main(lineadj PSIn In[4], inout TriangleStream<PSIn> TriStream)
{
	PSIn Out;
	float2 dir = normalize(In[1].ss_vert.xy - In[0].ss_vert.xy);
	float4 perp = float4(-dir.y, dir.x, 0, 0) * 0.1f;

	Out = In[0];
	Out.ss_vert = In[0].ss_vert + perp;
	TriStream.Append(Out);
	Out.ss_vert = In[0].ss_vert - perp;
	TriStream.Append(Out);
	
	Out = In[1];
	Out.ss_vert = In[1].ss_vert + perp;
	TriStream.Append(Out);
	Out.ss_vert = In[1].ss_vert - perp;
	TriStream.Append(Out);
	


	//float2 dir0 = normalize(In[1].ss_vert.xy - In[0].ss_vert.xy);
	//float2 dir1 = normalize(In[2].ss_vert.xy - In[1].ss_vert.xy);
	//float2 dir2 = normalize(In[3].ss_vert.xy - In[2].ss_vert.xy);
	//float2 perp = float2(dir1.y, -dir1.x);

	//{
	//	Out.ss_pos   = mul(w2s, In[i].ws_pos);
	//	Out.ws_pos   = In[i].ws_pos;
	//	Out.ws_norm  = In[i].ws_norm;
	//	Out.diff0    = In[i].diff0;
	//	Out.tex0     = In[i].tex0;
	//	TriStream.Append(Out);
	//}
	TriStream.RestartStrip();
}
#endif

#endif