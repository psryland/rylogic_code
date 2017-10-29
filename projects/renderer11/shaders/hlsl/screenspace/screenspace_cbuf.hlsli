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
	float2 m_screen_dim; // x = screen width, y = screen height, 
	float2 m_size;       // x = width in pixels, y = height in pixels
	bool   m_depth;      // True if depth scaling should be used
};

#endif