//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

#include "screen_space_cbuf.hlsli"
#include "../types.hlsli"

// Converts line segments into tristrip
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
		In[0].ss_vert = p0 = lerp(p0,p1,t);
	}
	if (p1.z < 0.0f)
	{
		float t = -p1.z / (p0.z - p1.z);
		In[1].ss_vert = p1 = lerp(p1,p0,t);
	}

	// Normalise the screen space points
	p0 /= p0.w;
	p1 /= p1.w;

	// Line direction and perpendicular in screen space
	float width = max(1.0f, m_size.x * 0.5f);
	float2 lin = (p1 - p0).xy;
	float2 dir = normalize(lin * m_screen_dim.xy);
	float2 tang = dir / m_screen_dim.xy;
	float2 perp = float2(-dir.y, dir.x) / m_screen_dim.xy;

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

// Converts line geometry into tristrip
#ifdef PR_RDR_GSHADER_thick_linestrip
[maxvertexcount(36)]
void main(lineadj PSIn In[4], inout TriangleStream<PSIn> OutStream)
{
	// To use the thick linestrip shader, you need to add an extra vert/index to the start
	// and end of the strip. The first line segment drawn is from In[1] -> In[2], then from In[2] -> In[3], etc.
	// If In[0].ss_vert.w == 0, assume the start of a line strip
	// If In[3].ss_vert.w == 0, assume the end of a line strip
	PSIn Out;

	float4 p0 = In[0].ss_vert;
	float4 p1 = In[1].ss_vert;
	float4 p2 = In[2].ss_vert;
	float4 p3 = In[3].ss_vert;
	bool line_beg = !any(p0 - p1);
	bool line_end = !any(p2 - p3);

	// Clip the line against the near plane
	if (p1.z < 0.0f)
	{
		float t = -p1.z / (p2.z - p1.z);
		In[1].ss_vert = p1 = lerp(p1,p2,t);
	}
	if (p2.z < 0.0f)
	{
		float t = -p2.z / (p1.z - p2.z);
		In[2].ss_vert = p2 = lerp(p2,p1,t);
	}

	// Normalise the screen space points
	p0 /= p0.w;
	p1 /= p1.w;
	p2 /= p2.w;
	p3 /= p3.w;

	// Line direction and perpendicular in screen space
	float2 lin0 = (p1 - p0).xy;
	float2 lin1 = (p2 - p1).xy;
	float2 lin2 = (p3 - p2).xy;
	float2 dir0 = normalize(lin0 * m_screen_dim.xy);
	float2 dir1 = normalize(lin1 * m_screen_dim.xy);
	float2 dir2 = normalize(lin2 * m_screen_dim.xy);
	
	float2 width = max(1.0f, m_size.x * 0.5f) / m_screen_dim.xy;

	// {0, cos(tau/16), cos(tau*2/16), cos(tau*3/16)};
	const float X[5] = {1, 0.92387953251, 0.70710678118, 0.38268343236, 0};
	const float Y[5] = {0, 0.38268343236, 0.70710678118, 0.92387953251, 1};

	// The start of the line
	if (line_beg)
	{
		// Perpendicular to p1->p2
		float2 perp1 = float2(-dir1.y, dir1.x); // ccw rotation

		// The rounded start of the line
		Out = In[1];
		Out.ss_vert.xy = In[1].ss_vert.xy + (-X[0] * dir1 + Y[0] * perp1) * width * In[1].ss_vert.w;
		OutStream.Append(Out);
		for (int i = 1; i < 5; i += 1)
		{
			Out.ss_vert.xy = In[1].ss_vert.xy + (-X[i] * dir1 - Y[i] * perp1) * width * In[1].ss_vert.w;   
			OutStream.Append(Out);
			Out.ss_vert.xy = In[1].ss_vert.xy + (-X[i] * dir1 + Y[i] * perp1) * width * In[1].ss_vert.w;   
			OutStream.Append(Out);
		}
	}
	else
	{
		// Find the bisector between p0->p1 and p1->p2.
		float2 perp0 = float2(-dir0.y, dir0.x); // ccw rotation
		float2 perp1 = float2(-dir1.y, dir1.x); // ccw rotation
		float2 bisector = perp0 + perp1;
		float  bi_scale = dot(bisector, perp1);
		bisector /= bi_scale;

		Out = In[1];
		Out.ss_vert.xy = In[1].ss_vert.xy - bisector * width * In[1].ss_vert.w;
		OutStream.Append(Out);
		OutStream.Append(Out);
		Out.ss_vert.xy = In[1].ss_vert.xy + bisector * width * In[1].ss_vert.w;
		OutStream.Append(Out);
	}

	// The end of the line
	if (line_end)
	{
		// Perpendicular to p1->p2
		float2 perp1 = float2(-dir1.y, dir1.x); // ccw rotation

		// The rounded end of the line
		Out = In[2];
		for (int j = 4; j > 0; j -= 1)
		{
			Out.ss_vert.xy = In[2].ss_vert.xy + (X[j] * dir1 - Y[j] * perp1) * width * In[2].ss_vert.w;
			OutStream.Append(Out);
			Out.ss_vert.xy = In[2].ss_vert.xy + (X[j] * dir1 + Y[j] * perp1) * width * In[2].ss_vert.w;
			OutStream.Append(Out);
		}
		Out.ss_vert.xy = In[2].ss_vert.xy + (X[0] * dir1 + Y[0] * perp1) * width * In[2].ss_vert.w;
		OutStream.Append(Out);
	}
	else
	{
		// Find the bisector between p1->p2 and p2->p3.
		float2 perp1 = float2(-dir1.y, dir1.x); // ccw rotation
		float2 perp2 = float2(-dir2.y, dir2.x); // ccw rotation
		float2 bisector = perp1 + perp2;
		bisector /= dot(bisector, perp1);

		Out = In[2];
		Out.ss_vert.xy = In[2].ss_vert.xy - bisector * width * In[2].ss_vert.w;
		OutStream.Append(Out);
		Out.ss_vert.xy = In[2].ss_vert.xy + bisector * width * In[2].ss_vert.w;
		OutStream.Append(Out);
		OutStream.Append(Out);
	}

	OutStream.RestartStrip();
}
#endif