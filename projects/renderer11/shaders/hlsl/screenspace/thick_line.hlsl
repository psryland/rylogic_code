//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

#include "../inout.hlsli"
#include "thick_line_cbuf.hlsli"

// Converts line geometry into tristrip
#ifdef PR_RDR_GSHADER_thick_linelist
[maxvertexcount(4)]
void main(line PSIn In[2], inout TriangleStream<PSIn> TriStream)
{
	PSIn Out;
	
	// Clip the line against the near plane
	float4 p0 = In[0].ss_vert;
	float4 p1 = In[1].ss_vert;
	if (p0.z < 0.0f)
	{
		float t = -p0.z / (p1.z - p0.z);
		In[0].ss_vert = p0 = lerp(p0,p1,t);//(1-t)*p0 + (t)*p1;
	}
	if (p1.z < 0.0f)
	{
		float t = -p1.z / (p0.z - p1.z);
		In[1].ss_vert = p1 = lerp(p1,p0,t);//(1-t)*p1 + (t)*p0;
	}

	// Normalise the screen space points
	p0 /= p0.w;
	p1 /= p1.w;

	float2 dir = normalize((p1 - p0).xy * m_dim_and_width.xy);
	float2 perp = float2(-dir.y, dir.x) / m_dim_and_width.xy;
	perp = perp * m_dim_and_width.w;

	Out = In[0];
	Out.ss_vert.xy = In[0].ss_vert.xy + perp*In[0].ss_vert.w;
	TriStream.Append(Out);
	Out.ss_vert.xy = In[0].ss_vert.xy - perp*In[0].ss_vert.w;
	TriStream.Append(Out);

	Out = In[1];
	Out.ss_vert.xy = In[1].ss_vert.xy + perp*In[1].ss_vert.w;
	TriStream.Append(Out);
	Out.ss_vert.xy = In[1].ss_vert.xy - perp*In[1].ss_vert.w;
	TriStream.Append(Out);

	TriStream.RestartStrip();
}
#endif

// Converts line geometry into tristrip
#ifdef PR_RDR_GSHADER_thick_linestrip
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