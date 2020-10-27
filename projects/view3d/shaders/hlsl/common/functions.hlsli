//***********************************************
// Renderer
//  Copyright � Rylogic Ltd 2010
//***********************************************

#ifndef PR_RDR_FUNCTIONS_HLSLI
#define PR_RDR_FUNCTIONS_HLSLI

// Conditional helper function
int SelectInt(bool condition, int true_case, int false_case)
{
	return (int)(condition)*true_case + (int)(!condition)*false_case;
}
float SelectFloat(bool condition, float true_case, float false_case)
{
	return (int)(condition)*true_case + (int)(!condition)*false_case;
}
float4 SelectFloat4(bool condition, float4 true_case, float4 false_case)
{
	return (int)(condition)*true_case + (int)(!condition)*false_case;
}

// Returns the near and far plane distances in camera space from a
// projection matrix (either perspective or orthonormal)
float2 ClipPlanes(float4x4 c2s)
{
	float2 dist;
	dist.x = c2s._43 / c2s._33; // near
	dist.y = c2s._43 / (1 + c2s._33) + c2s._44*(c2s._43 - c2s._33 - 1)/(c2s._33 * (1 + c2s._33)); // far
	return dist;
}

// Return the parametric position of 'x' on the range [mn, mx]
float Frac(float mn, float x, float mx)
{
	return (x - mn) / (mx - mn);
}
float4 Frac4(float4 mn, float4 x, float4 mx)
{
	return (x - mn) / (mx - mn);
}

// Returns the parametric value 't' of the intersection between the line
// passing through 's' and 'e' with 'frust'.
// Assumes 's' is within the frustum to start with
float IntersectFrustum(uniform float4x4 frust, float4 s, float4 e)
{
	const float4 T = 1e10f;
	
	// Find the distance from each frustum face for 's' and 'e'
	float4 d0 = mul(s, frust);
	float4 d1 = mul(e, frust);

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

// Projected textures ********************************************
#if 0 // This needs moving to where 'm_proj_tex_count' is defined
float4 ProjTex(float4 ws_pos, float4 in_diff)
{
	float4 out_diff = in_diff;
	for (int i = 0; i < m_proj_tex_count.x; i += 1.0)
	{
		// Project the world space position into projected texture coords
		float2 pt_pos = mul(ws_pos, m_proj_tex[i]).xy;
		float2 pt_pos_sat = saturate(pt_pos);
		float4 diff = m_proj_texture[i].Sample(m_proj_sampler[i], pt_pos_sat);
		if (pt_pos_sat.x == pt_pos.x && pt_pos_sat.y == pt_pos.y)
			out_diff = diff;
	}
	return out_diff;
}
#endif

#endif