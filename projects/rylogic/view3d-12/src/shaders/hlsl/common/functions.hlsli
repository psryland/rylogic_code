//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
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

// Return the parametric position of 'x' on the range [mn, mx]
float Frac(float mn, float x, float mx)
{
	return (x - mn) / (mx - mn);
}
float4 Frac4(float4 mn, float4 x, float4 mx)
{
	return (x - mn) / (mx - mn);
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