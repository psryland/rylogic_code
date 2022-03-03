//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
// Constant buffer definitions for the show normals shader
// This file is included from C++ source as well
#ifndef PR_RDR_SHADER_DIAGNOSTIC_CBUF_HLSL
#define PR_RDR_SHADER_DIAGNOSTIC_CBUF_HLSL

#include "../types.hlsli"

cbuffer CBufFrame :reg(b0)
{
	Camera m_cam;
	float4 m_colour;
	float  m_length;
	float  pad[3];
};

#endif
