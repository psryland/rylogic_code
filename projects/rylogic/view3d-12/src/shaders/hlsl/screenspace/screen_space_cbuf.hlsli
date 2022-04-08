//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_SCREEN_SPACE_CBUF_HLSLI
#define PR_RDR_SHADER_SCREEN_SPACE_CBUF_HLSLI

#include "../types.hlsli"

// Constants used for screen space shading.
cbuffer CBufFrame :reg(b0,0)
{
	Camera m_cam;
	float2 m_screen_dim; // x = screen width, y = screen height, 
	float2 m_size;       // x = width in pixels, y = height in pixels
	bool   m_depth;      // True if depth scaling should be used
};

#endif