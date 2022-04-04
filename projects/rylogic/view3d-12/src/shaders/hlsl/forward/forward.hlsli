//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
// Shader for forward rendering face data

#ifndef PR_RDR_SHADER_FORWARD_HLSLI
#define PR_RDR_SHADER_FORWARD_HLSLI

#include "../types.hlsli"
#include "forward_cbuf.hlsli"

// Texture2D /w sampler
Texture2D<float4> m_texture0; //:register(t0);
SamplerState      m_sampler0; //:register(s0);

// Environment map
TextureCube<float4> m_envmap_texture; //:register(t1);
SamplerState        m_envmap_sampler; //:register(s1);

// Shadow map
Texture2D<float2> m_smap_texture[MaxShadowMaps]; //:register(t2);
SamplerComparisonState m_smap_sampler; //:register(s2);

// Projected textures
Texture2D<float4> m_proj_texture[MaxProjectedTextures]; //:register(t3);
SamplerState      m_proj_sampler[MaxProjectedTextures]; //:register(s3);

#include "../lighting/phong_lighting.hlsli"
#include "../shadow/shadow_cast.hlsli"
#include "../utility/env_map.hlsli"

// PS output format
struct PSOut
{
	float4 diff :SV_Target;
};

// Main vertex shader
PSIn VSDefault(VSIn In)
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

// Main pixel shader
PSOut PSDefault(PSIn In)
{
	// Notes:
	//  - 'ss_vert:SV_Position' in the pixel shader already has x,y,z divided by w (w unchanged)
	//  - Models without normals can still use 'HasNormals' as true. For this case, and normal to
	//    the light source is used.

	PSOut Out;

	// Tinting
	Out.diff = In.diff;

	// Transform
	if (HasNormals)
	{
		// If the normal is (0,0,0), use a vector to the light source
		In.ws_norm =
			dot(In.ws_norm, In.ws_norm) != 0 ? normalize(In.ws_norm) :
			DirectionalLight(m_global_light) ? -m_global_light.m_ws_direction :
			PointLight(m_global_light)       ? normalize(m_global_light.m_ws_position - In.ws_vert) :
			SpotLight(m_global_light)        ? normalize(m_global_light.m_ws_position - In.ws_vert) :
			float4(0,0,0,0);
	}

	// Texture2D (with transform)
	if (HasTex0)
	{
		if (EnvMapProj)
		{
			float3 dir = mul(In.ws_vert, m_tex2surf0).xyz;
			Out.diff = m_envmap_texture.Sample(m_envmap_sampler, dir);
		}
		else
		{
			Out.diff = m_texture0.Sample(m_sampler0, In.tex0) * Out.diff;
		}
	}

	// Env Map
	if (HasEnvMap && HasNormals)
		Out.diff = EnvironmentMap(m_env_map, In.ws_vert, In.ws_norm, m_cam.m_c2w[3], Out.diff);

	// Shadows
	float light_visible = 1.0f;
	if (ShadowMapCount(m_shadow) != 0)
		light_visible = LightVisibility(m_shadow, In.ws_vert);

	// Lighting
	if (HasNormals)
		Out.diff = Illuminate(m_global_light, In.ws_vert, In.ws_norm, m_cam.m_c2w[3], light_visible, Out.diff);

	// If not alpha blending, clip alpha pixels
	if (!HasAlpha)
		clip(Out.diff.a - 0.5);

	return Out;
}
#endif
