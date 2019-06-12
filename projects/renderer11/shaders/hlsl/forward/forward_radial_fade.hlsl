//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
// Shader for forward rendering face data

#include "forward.hlsli"

// Main pixel shader
#ifdef PR_RDR_PSHADER_forward_radial_fade
PSOut main(PSIn In)
{
	PSOut Out = PSDefault(In);

	// Sample the env map along the camera->point ray
	float4 ray = mul(In.ws_vert - m_cam.m_c2w[3], m_env_map.m_w2env);
	float4 env = m_envmap_texture.Sample(m_envmap_sampler, ray.xyz);

	float4 centre = m_fade_centre != float4(0,0,0,0) ? m_fade_centre : m_cam.m_c2w[3];
	float4 radial = In.ws_vert - centre;
	float radius = 
		m_fade_type == 0 ? length(radial) : // Spherical
		m_fade_type == 1 ? length(radial - dot(radial, m_cam.m_c2w[1]) * m_cam.m_c2w[1]) : // Cylindrical
		0;

	// Lerp between the texture and the env map based on distance
	float frac = smoothstep(m_fade_radius[0], m_fade_radius[1], radius);
	Out.diff = lerp(Out.diff, env, frac);
	return Out;
}
#endif
