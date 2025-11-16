//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#include "view3d-12/src/shaders/hlsl/types.hlsli"
#include "view3d-12/src/shaders/hlsl/forward/forward_cbuf.hlsli"

// Converts point geometry into arrow heads
// Uses ss_vert for centre position, and ws_norm as the arrow forward direction
void GSArrowHead(point PSIn In[1], inout TriangleStream<PSIn> OutStream)
{
	PSIn Out;

	// Arrow head direction is in 'ws_norm'
	float4 ss_norm = mul(In[0].ws_norm, m_cam.m_w2s);
	
	// Arrow head size is in 'tex0'
	float width = max(8.0f, In[0].tex0.x);
	float height = max(8.0f, In[0].tex0.y);
	
	// Arrow direction and perpendicular in screen space
	float2 dir = normalize(ss_norm.xy * m_screen_dim.xy);
	float2 tang = dir / m_screen_dim.xy;
	float2 perp = float2(-dir.y, dir.x) / m_screen_dim.xy;

	Out = In[0];
	Out.ws_norm = float4(0, 0, 0, 0); // null out the normal
	Out.tex0 = float2(0, 0); // null out the tex0
	Out.ss_vert.xy = In[0].ss_vert.xy + (1.0f * tang) * height * In[0].ss_vert.w;
	OutStream.Append(Out);
	Out.ss_vert.xy = In[0].ss_vert.xy + (-0.6f * tang + 0.8f * perp) * width * In[0].ss_vert.w;
	OutStream.Append(Out);
	Out.ss_vert.xy = In[0].ss_vert.xy + (-0.6f * tang - 0.8f * perp) * width * In[0].ss_vert.w;
	OutStream.Append(Out);
	OutStream.RestartStrip();
}

#ifdef PR_RDR_GSHADER_arrow_head
[maxvertexcount(3)]
void main(point PSIn In[1], inout TriangleStream<PSIn> OutStream)
{
	GSArrowHead(In, OutStream);
}
#endif
