//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************
// Shader for forward rendering face data

#include "forward_cbuf.hlsli"
#include "../types.hlsli"

// Texture2D /w sampler
Texture2D<float4> m_texture0 :register(t0);
SamplerState      m_sampler0 :register(s0);

// Shadow map
Texture2D<float2> m_smap_texture[1];
SamplerState      m_smap_sampler[1];

// Projected textures
Texture2D<float4> m_proj_texture[PR_RDR_MAX_PROJECTED_TEXTURES];
SamplerState      m_proj_sampler[PR_RDR_MAX_PROJECTED_TEXTURES];

#include "../lighting/phong_lighting.hlsli"
#include "../shadow/shadow_cast.hlsli"

// PS output format
struct PSOut
{
	float4 diff :SV_Target;
};

// Main vertex shader
#ifdef PR_RDR_VSHADER_forward
PSIn main(VSIn In)
{
	PSIn Out;

	// Transform
	Out.ss_vert = mul(In.vert, m_o2s);
	Out.ws_vert = mul(In.vert, m_o2w);
	Out.ws_norm = mul(In.norm, m_n2w);

	// Tinting
	Out.diff = m_tint;

	// Per Vertex colour
	Out.diff = In.diff * Out.diff;

	// Texture2D (with transform)
	Out.tex0 = mul(float4(In.tex0,0,1), m_tex2surf0).xy;

	return Out;
}
#endif



// Main pixel shader
#ifdef PR_RDR_PSHADER_forward
PSOut main(PSIn In)
{
	PSOut Out;

	// Transform
	In.ws_norm = normalize(In.ws_norm);

	// Tinting
	Out.diff = In.diff;

	// Texture2D (with transform)
	if (HasTex0)
		Out.diff = m_texture0.Sample(m_sampler0, In.tex0) * Out.diff;

	// Shadows
	float light_visible = 1.0f;
	if (ShadowMapCount(m_shadow) == 1)
		light_visible = LightVisibility(m_shadow, 0, m_global_light, m_cam.m_w2c, In.ws_vert);

	// Lighting
	if (HasNormals)
		Out.diff = Illuminate(m_global_light, In.ws_vert, In.ws_norm, m_cam.m_c2w[3], light_visible, Out.diff);

	return Out;
}
#endif
