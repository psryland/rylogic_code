//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************

// Helper methods for casting shadows
#ifndef PR_RDR_SHADER_SHADOW_CAST_HLSL
#define PR_RDR_SHADER_SHADOW_CAST_HLSL

#include "../types.hlsli"

// Returns a direction vector of the shadow cast from point 'ws_vert' 
float4 ShadowRayWS(uniform Light light, float4 ws_pos)
{
	return DirectionalLight(light) ? light.m_ws_direction : (ws_pos - light.m_ws_position);
}

// Returns the parametric value 't' of the intersection between the line
// passing through 's' and 'e' with 'frust'.
// Assumes 's' is within the frustum to start with
float IntersectFrustum(uniform Shadow shadow, float4 s, float4 e)
{
	const float4 T = 1e10f;
	
	// Find the distance from each frustum face for 's' and 'e'
	float4 d0 = mul(s, shadow.m_frust);
	float4 d1 = mul(e, shadow.m_frust);

	// Clip the edge 's-e' to each of the frustum sides (Actually, find the parametric
	// value of the intercept)(min(T,..) protects against divide by zero)
	float4 t0 = step(d1,d0)   * min(T, -d0/(d1 - d0));        // Clip to the frustum sides
	float  t1 = step(e.z,s.z) * min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane

	// Set all components that are <= 0.0 to BIG
	t0 += step(t0, float4(0,0,0,0)) * T;
	t1 += step(t1, 0.0f) * T.x;

	// Find the smallest positive parametric value
	// => the closest intercept with a frustum plane
	float t = T.x;
	t = min(t, t0.x);
	t = min(t, t0.y);
	t = min(t, t0.z);
	t = min(t, t0.w);
	t = min(t, t1);
	return t;
}

// Returns a value between [0,1] where 0 means fully in shadow, 1 means not in shadow
float LightVisibility(uniform Shadow shadow, uniform int smap_index, uniform Light light, uniform row_major float4x4 w2c, float4 ws_pos)
{
	// Find the shadow ray in frustum space and its intersection with the frustum
	float4 ws_ray = ShadowRayWS(light, ws_pos);
	float4 fs_pos0 = mul(ws_pos         , w2c); fs_pos0.z += shadow.m_frust_dim.z;
	float4 fs_pos1 = mul(ws_pos + ws_ray, w2c); fs_pos1.z += shadow.m_frust_dim.z;
	float t = IntersectFrustum(shadow, fs_pos0, fs_pos1);
	float4 intercept = lerp(fs_pos0, fs_pos1, t);

	// Find the normalised fractional distance between the intercept (0) and the light (1)
	float dist = saturate(t * length(ws_ray) / shadow.m_frust_dim.w) + TINY;

	// Convert the intersection to texture space and sample the smap
	float2 uv = float2(0.5 + 0.5*intercept.x/shadow.m_frust_dim.x, 0.5 - 0.5*intercept.y/shadow.m_frust_dim.y);
	float2 depth = m_smap_texture[smap_index].Sample(m_smap_sampler[smap_index], uv);

	// R channel is near frustum faces, G channel is the far frustum plane.
	// If intercept.z is 0 then the intercept is on the far plane.
	const float Eps = TINY * 10;
	float smap_dist = step(Eps, intercept.z) * depth.x + step(intercept.z, Eps) * depth.y;

	// "depths" are actually fractional distances between the frustum plane
	// and the light. e.g. a depth of 0.25 means the shadow caster is closer
	// to the frustum face than the light. So for 'ws_pos' to have a smaller
	// depth value means it's in shadow.
	return smoothstep(smap_dist-Eps, smap_dist, dist);
}

#endif
