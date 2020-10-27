//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2014
//***********************************************

// Helper methods for casting shadows
#ifndef PR_RDR_SHADER_SHADOW_CAST_HLSL
#define PR_RDR_SHADER_SHADOW_CAST_HLSL

#include "../types.hlsli"
#include "../common/functions.hlsli"

// Returns a value between [0,1] where 0 means fully in shadow, 1 means not in shadow
float LightVisibility(uniform Shadow shadow, float4 ws_pos)
{
	float visibility = 1.0f;
	for (int i = 0; i != ShadowMapCount(shadow); ++i)
	{
		// Get the distance from the light, normalised within the projection volume
		float4 ls_pos = mul(ws_pos, shadow.m_w2l[i]);
		float z = Frac(shadow.m_zclip[i].y, -ls_pos.z, shadow.m_zclip[i].x);
		
		float4 ss_pos = mul(ls_pos, shadow.m_l2s[i]);
		float2 uv = 0.5*float2(1.0 + ss_pos.x, 1.0 - ss_pos.y);
		float2 depth = m_smap_texture[i].Sample(m_smap_sampler, uv);

		// 1.0 = near plane, 0.0 = far plane, so the
		// largest value is closest to the light source
		const float Eps = TINY * 10;
		visibility = smoothstep(depth.x-Eps, depth.x, z);
	}
	return visibility;
}

#endif
