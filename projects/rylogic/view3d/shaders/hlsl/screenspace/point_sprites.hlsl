//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

#include "screen_space_cbuf.hlsli"
#include "../types.hlsli"

// Converts point geometry into billboard quads
#ifdef PR_RDR_GSHADER_point_sprites
[maxvertexcount(4)]
void main(point PSIn In[1], inout TriangleStream<PSIn> OutStream)
{
	PSIn Out;

	// Size of the sprite
	float w = m_size.x * 0.5f;
	float h = m_size.y * 0.5f;

	// Output a camera facing quad: bottom to top 'S' order.
	if (m_depth)
	{
		float4 radx = w * m_cam.m_c2w[0];
		float4 rady = h * m_cam.m_c2w[1];

		Out = In[0];
		Out.ws_vert = In[0].ws_vert + (-radx - rady);
		Out.ss_vert = mul(Out.ws_vert, m_cam.m_w2s);
		Out.tex0.xy = float2(0,0);
		OutStream.Append(Out);
		
		Out = In[0];
		Out.ws_vert = In[0].ws_vert + (+radx - rady);
		Out.ss_vert = mul(Out.ws_vert, m_cam.m_w2s);
		Out.tex0.xy = float2(1,0);
		OutStream.Append(Out);
		
		Out = In[0];
		Out.ws_vert = In[0].ws_vert + (-radx + rady);
		Out.ss_vert = mul(Out.ws_vert, m_cam.m_w2s);
		Out.tex0.xy = float2(0,1);
		OutStream.Append(Out);
		
		Out = In[0];
		Out.ws_vert = In[0].ws_vert + (+radx + rady);
		Out.ss_vert = mul(Out.ws_vert, m_cam.m_w2s);
		Out.tex0.xy = float2(1,1);
		OutStream.Append(Out);
	}
	else
	{
		// Screen space only. No depth test or distance scaling
		float2 radx = float2(w / m_screen_dim.x, 0);
		float2 rady = float2(0, h / m_screen_dim.y);

		Out = In[0];
		Out.ss_vert.xy = In[0].ss_vert.xy + (-radx - rady) * In[0].ss_vert.w;  Out.tex0.xy = float2(0,0);  OutStream.Append(Out);
		Out.ss_vert.xy = In[0].ss_vert.xy + (+radx - rady) * In[0].ss_vert.w;  Out.tex0.xy = float2(1,0);  OutStream.Append(Out);
		Out.ss_vert.xy = In[0].ss_vert.xy + (-radx + rady) * In[0].ss_vert.w;  Out.tex0.xy = float2(0,1);  OutStream.Append(Out);
		Out.ss_vert.xy = In[0].ss_vert.xy + (+radx + rady) * In[0].ss_vert.w;  Out.tex0.xy = float2(1,1);  OutStream.Append(Out);
	}
	OutStream.RestartStrip();
}
#endif
