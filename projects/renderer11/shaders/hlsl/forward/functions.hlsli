//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************

// Projected textures ********************************************

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
