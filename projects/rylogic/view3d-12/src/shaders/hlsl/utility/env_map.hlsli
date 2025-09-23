//***********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_VIEW3D_SHADER_ENV_MAP_HLSLI
#define PR_VIEW3D_SHADER_ENV_MAP_HLSLI

#include "view3d-12/src/shaders/hlsl/types.hlsli"

// Return the colour due to lighting. Returns unlit_diff if ws_norm is zero
float4 EnvironmentMap(in uniform EnvMap envmap, float4 ws_pos, float4 ws_norm, float4 ws_cam, float4 initial_diff)
{
	float4 r = mul(reflect(ws_pos - ws_cam, ws_norm), envmap.m_w2env);
	float4 col = m_envmap_texture.Sample(m_envmap_sampler, r.xyz);
	return lerp(initial_diff, col, m_env_reflectivity);
}

#endif