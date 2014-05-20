//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

// Helper methods for casting shadows
#ifndef PR_RDR_SHADER_SHADOW_CAST_HLSL
#define PR_RDR_SHADER_SHADOW_CAST_HLSL

#include "../types.hlsli"

// Returns a direction vector of the shadow cast from point 'ws_vert' 
float4 ShadowRayWS(in uniform Light light, in float4 ws_pos)
{
	if (DirectionalLight(light)) return light.m_ws_direction;
	return ws_pos - light.m_ws_position;
}

// Returns the parametric value 't' of the intersect the line
// passing through 's' and 'e' with 'frust'.
// Assumes 's' is within the frustum to start with
float IntersectFrustum(in uniform Shadow shadow, in float4 s, in float4 e)
{
	const float4 T = 1e10f;
	
	// Find the distance from each frustum face for 's' and 'e'
	float4 d0 = mul(s, shadow.m_frust);
	float4 d1 = mul(e, shadow.m_frust);

	// Clip the edge 's-e' to each of the frustum sides (Actually, find the parametric
	// value of the intercept)(min(T,..) protects against divide by zero)
	float4 t0 = step(d1,d0)   * min(T, -d0/(d1 - d0));        // Clip to the frustum sides
	float  t1 = step(e.z,s.z) * min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane

	// Find the smallest parametric value which is the closest intercept with a frustum plane

	// Set all the zeros in t0 to T
	t0 += !t0 * T;
	t1 += !t1 * T.x;

	float t = T.x;
	t = min(t, t0.x);
	t = min(t, t0.y);
	t = min(t, t0.z);
	t = min(t, t0.w);
	t = min(t, t1);

	// working version using ifs:
	// float t = T.x;
	// if (t0.x != 0) t = min(t,t0.x);
	// if (t0.y != 0) t = min(t,t0.y);
	// if (t0.z != 0) t = min(t,t0.z);
	// if (t0.w != 0) t = min(t,t0.w);
	// if (t1   != 0) t = min(t,t1);

	return t;
}

// Returns a value between [0,1] where 0 means fully in shadow, 1 means not in shadow
float LightVisibility(in uniform Shadow shadow, in uniform int smap_index, in uniform Light light, in uniform row_major float4x4 w2c, float4 ws_pos)
{
	// Find the shadow ray in frustum space and its intersection with the frustum
	float4 ws_ray = ShadowRayWS(light, ws_pos);
	float4 fs_pos0 = mul(ws_pos         , w2c); fs_pos0.z += shadow.m_frust_dim.z;
	float4 fs_pos1 = mul(ws_pos + ws_ray, w2c); fs_pos1.z += shadow.m_frust_dim.z;
	float t = IntersectFrustum(shadow, fs_pos0, fs_pos1);

	// Convert the intersection to texture space
	float4 intersect = lerp(fs_pos0, fs_pos1, t);
	float2 uv = float2(0.5 + 0.5*intersect.x/shadow.m_frust_dim.x, 0.5 - 0.5*intersect.y/shadow.m_frust_dim.y);

	// Find the distance from the frustum to 'ws_pos'
	float dist = saturate(t * length(ws_ray) / shadow.m_frust_dim.w) + TINY;

	// Sample the smap and compare depths
	float2 depth = m_smap_texture[smap_index].Sample(m_smap_sampler[smap_index], uv);
return 1.0f;
	//const float d = 0.5 / SMapTexSize;
	//float4 px0 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d,-d));
	//float4 px1 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d,-d));
	//float4 px2 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2(-d, d));
	//float4 px3 = tex2D(g_sampler_smap[g_cast_shadows[light_index]], uv + float2( d, d));
	//if (intersect.z > TINY)
	//	return (step(DecodeFloat2(px0.rg), dist) +
	//			step(DecodeFloat2(px1.rg), dist) +
	//			step(DecodeFloat2(px2.rg), dist) +
	//			step(DecodeFloat2(px3.rg), dist)) / 4.0f;
	//else
	//	return (step(DecodeFloat2(px0.ba), dist) +
	//			step(DecodeFloat2(px1.ba), dist) +
	//			step(DecodeFloat2(px2.ba), dist) +
	//			step(DecodeFloat2(px3.ba), dist)) / 4.0f;
}

#endif
