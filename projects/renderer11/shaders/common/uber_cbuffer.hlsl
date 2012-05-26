//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Constants buffer for the uber shader
#ifndef PR_RDR_SHADER_UBER_CBUFFER_H
#define PR_RDR_SHADER_UBER_CBUFFER_H

#include "uber_defines.hlsl"

// For efficiency, constant buffers need to be grouped by frequency of update

// Variables that are updated per frame
cbuffer CBufFrame
{
	// Camera transform
	EXPAND(matrix m_c2w; , PR_RDR_SHADER_TXFM) // camera to world
	EXPAND(matrix m_w2c; , PR_RDR_SHADER_TXFM) // world to camera
};

// Variables that are updated per model
cbuffer CBufModel
{
	// Object transform
	EXPAND(matrix m_o2s; , PR_RDR_SHADER_TXFM   ) // object to screen
	EXPAND(matrix m_o2w; , PR_RDR_SHADER_TXFMWS ) // object to world
	EXPAND(matrix m_n2w; , PR_RDR_SHADER_NORM   ) // normal to world

	// Tinting
	EXPAND(float4 m_tint; , PR_RDR_SHADER_TINT0 ) // object tint colour
};


#endif



