//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_THICK_LINE_CBUF_HLSL
#define PR_RDR_SHADER_THICK_LINE_CBUF_HLSL

#include "../types.hlsli"

cbuffer CbufFrame :cbuf_bank(b0)
{
	Camera m_cam;
	float4 m_dim_and_width;   // x = screen width, y = screen height, z = 0, w = width in pixels
};

#endif