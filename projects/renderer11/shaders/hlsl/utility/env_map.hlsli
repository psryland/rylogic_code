//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

#ifndef PR_RDR_SHADER_ENV_MAP_HLSLI
#define PR_RDR_SHADER_ENV_MAP_HLSLI

#include "../types.hlsli"

// Return the colour due to lighting. Returns unlit_diff if ws_norm is zero
float4 EnvMap(float4 ws_pos, float4 ws_norm, float4 ws_cam, float4 initial_diff)
{
	return initial_diff;
}

#endif