//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

#include "screenspace_cbuf.hlsli"
#include "../types.hlsli"

// Converts line geometry into tristrip
#ifdef PR_RDR_GSHADER_thick_linelist
[maxvertexcount(18)]
void main(line PSIn In[2], inout TriangleStream<PSIn> OutStream)
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

	// Line direction and perpendicular in screen space
	float width = max(1.0f, m_dim_and_width.w * 0.5f);
	float2 lin = (p1 - p0).xy;
	float2 dir = normalize(lin * m_dim_and_width.xy);
	float2 tang = dir / m_dim_and_width.xy;
	float2 perp = float2(-dir.y, dir.x) / m_dim_and_width.xy;

	// {0, cos(tau/16), cos(tau*2/16), cos(tau*3/16)};
	const float X[5] = {1, 0.92387953251, 0.70710678118, 0.38268343236, 0};
	const float Y[5] = {0, 0.38268343236, 0.70710678118, 0.92387953251, 1};
	
	// The rounded start of the line
	Out = In[0];
	Out.ss_vert.xy = In[0].ss_vert.xy + (-X[0] * tang + Y[0] * perp) * width * In[0].ss_vert.w;   OutStream.Append(Out);
	for (int i = 0; ++i != 5;) {
	Out.ss_vert.xy = In[0].ss_vert.xy + (-X[i] * tang - Y[i] * perp) * width * In[0].ss_vert.w;   OutStream.Append(Out);
	Out.ss_vert.xy = In[0].ss_vert.xy + (-X[i] * tang + Y[i] * perp) * width * In[0].ss_vert.w;   OutStream.Append(Out);
	}

	// The rounded end of the line
	Out = In[1];
	for (int j = 5; j-- != 1;) {
	Out.ss_vert.xy = In[1].ss_vert.xy + (X[j] * tang - Y[j] * perp) * width * In[1].ss_vert.w;   OutStream.Append(Out);
	Out.ss_vert.xy = In[1].ss_vert.xy + (X[j] * tang + Y[j] * perp) * width * In[1].ss_vert.w;   OutStream.Append(Out);
	}
	Out.ss_vert.xy = In[1].ss_vert.xy + (X[0] * tang + Y[0] * perp) * width * In[1].ss_vert.w;   OutStream.Append(Out);
	OutStream.RestartStrip();
}
#endif
