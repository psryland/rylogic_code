//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Ocean pixel shader with PBR-style water rendering.
// Features: Fresnel reflectance, procedural sky reflection, depth-tinted refraction,
// subsurface scattering, sun specular highlight, foam at wave crests.
#include "view3d-12/src/shaders/hlsl/forward/forward_cbuf.hlsli"
#include "lost_at_sea/src/shaders/ocean_common.hlsli"

struct PSOut
{
	float4 diff :SV_TARGET;
};

// Procedural sky colour based on a direction vector.
// This is a placeholder until a real skybox/cubemap is available for reflections.
float3 ProceduralSky(float3 dir)
{
	// Blend from horizon colour to zenith based on the z component of the direction
	float3 horizon = float3(0.75, 0.85, 0.95); // Pale blue-white at horizon
	float3 zenith  = float3(0.25, 0.45, 0.80); // Deep blue at zenith
	float t = saturate(dir.z);
	float3 sky = lerp(horizon, zenith, t * t);

	// Add sun disc
	float sun_dot = saturate(dot(dir, m_sun_direction.xyz));
	float sun_disc = pow(sun_dot, 512.0);
	float sun_glow = pow(sun_dot, 8.0) * 0.3;
	sky += m_sun_colour.rgb * (sun_disc + sun_glow);

	return sky;
}

// Schlick's Fresnel approximation
float FresnelSchlick(float cos_theta, float f0)
{
	return f0 + (1.0 - f0) * pow(saturate(1.0 - cos_theta), 5.0);
}

#ifdef PR_RDR_PSHADER_ocean

PSOut main(PSIn In)
{
	PSOut Out = (PSOut)0;

	// Surface normal and view direction
	float3 N = normalize(In.ws_norm.xyz);
	float3 V = normalize(m_cam.m_c2w[3].xyz - In.ws_vert.xyz);
	float NdotV = saturate(dot(N, V));
	float foam_factor = In.diff.a;

	// --- Fresnel reflectance ---
	float fresnel = FresnelSchlick(NdotV, m_fresnel_f0);

	// --- Reflection ---
	float3 R = reflect(-V, N);
	float3 reflection = ProceduralSky(R);

	// --- Refraction / water body colour ---
	// Approximate depth: steeper viewing angles see deeper water
	float depth_factor = saturate(1.0 - NdotV);
	float3 refraction = lerp(m_colour_shallow.rgb, m_colour_deep.rgb, depth_factor);

	// --- Subsurface scattering ---
	// Light passing through thin wave crests when backlit by the sun
	float3 L = m_sun_direction.xyz;
	float sss_dot = saturate(dot(V, -L));
	float wave_height = saturate(In.ws_vert.z * 0.5 + 0.5); // Normalise wave height around 0
	float sss = pow(sss_dot, 4.0) * wave_height * m_sss_strength;
	float3 sss_colour = m_colour_shallow.rgb * 1.5; // Brighter turquoise for SSS

	// --- Specular (sun glint) ---
	float3 H = normalize(L + V);
	float NdotH = saturate(dot(N, H));
	float specular = pow(NdotH, m_specular_power) * fresnel;
	float3 spec_colour = m_sun_colour.rgb * specular;

	// --- Combine ---
	float3 colour = lerp(refraction, reflection, fresnel);
	colour += sss * sss_colour;
	colour += spec_colour;

	// --- Foam ---
	// Foam appears at wave crests where the Gerstner Jacobian drops below 1
	colour = lerp(colour, m_colour_foam.rgb, foam_factor * 0.8);

	// --- Basic ambient from scene lighting ---
	float ambient = 0.15;
	float diffuse_light = saturate(dot(N, L)) * 0.5;
	colour *= (ambient + diffuse_light + 0.5); // Brighten overall to avoid too-dark water

	Out.diff = float4(colour, 1.0);
	return Out;
}

#endif
