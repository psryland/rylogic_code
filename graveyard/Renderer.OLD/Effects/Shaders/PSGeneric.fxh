#ifndef HLSL_PS_GENERIC_FXH
#define HLSL_PS_GENERIC_FXH

#include "InOutStructs.fxh"
#include "Identifiers.fxh"
#include "Common.fxh"
#include "StdLighting.fxh"
#include "StdTexturing.fxh"

// 1.1
float4 PS11_Generic(VSOut_PosDiffTexSpecEnv In,
	uniform TexDiffuse		tex_diffuse,
	uniform EnviroMap		enviro_map,
 	uniform PSOut			ps_out) : Color0
{
	// Base texture
	float4 tex = 1;
	if( tex_diffuse ) tex = StdTexturing_2D(In.tex);

	// Enviro mapping
	if( enviro_map ) tex = lerp(tex, StdTexturing_Env(In.ws_env), g_env_blend_fraction);

	// Diffuse
	float4 result = 1;
		 if( ps_out == PSOut_Zero )						{ result = 0; }
	else if( ps_out == PSOut_One )						{ result = 1; }
	else if( ps_out == PSOut_Amb )						{ result = StdLighting_Ambient(); }
	else if( ps_out == PSOut_Amb_p_InDiff )				{ result = StdLighting_Ambient() + In.diff; }
	else if( ps_out == PSOut_Amb_p_Tex )				{ result = StdLighting_Ambient() + tex; }
	else if( ps_out == PSOut_Amb_x_Tex )				{ result = StdLighting_Ambient() * tex; }
	else if( ps_out == PSOut_InDiff )					{ result = In.diff; }
	else if( ps_out == PSOut_InDiff_p_InSpec )			{ result = In.diff + In.spec; }
	else if( ps_out == PSOut_InDiff_p_Tex )				{ result = In.diff + tex; }
	else if( ps_out == PSOut_InDiff_x_Tex )				{ result = In.diff * tex; }
	else if( ps_out == PSOut_InDiff_x_Tex_p_InSpec )	{ result = In.diff * tex + In.spec; }
	else if( ps_out == PSOut_InSpec )					{ result = In.spec; }
	else if( ps_out == PSOut_Tex )						{ result = tex; }
	else { result = In.pos; } // Compile error here means unsupported ps_out
	return result;
}

// 2.0
float4 PS20_Generic(VSOut_PosDiffTexSpecEnvWSNormWSToeye In,
	uniform PerPixelDiffuse		per_pixel_diffuse,
	uniform PerPixelSpecular	per_pixel_specular,
	uniform TexDiffuse			tex_diffuse,
	uniform EnviroMap			enviro_map,
	uniform PSOut				ps_out) : Color0
{
	float3	ws_norm			= normalize(In.ws_norm);
	float3	ws_to_eye_norm	= normalize(In.ws_to_eye);

	// Diffuse lighting
	float4 light_diff = 1;
	if( per_pixel_diffuse == PerPixelDiffuse_Amb )					{ light_diff = StdLighting_Ambient(); }
	if( per_pixel_diffuse == PerPixelDiffuse_Amb_p_Directional )	{ light_diff = StdLighting_Ambient() + StdLighting_Directional(ws_norm); }
	if( per_pixel_diffuse == PerPixelDiffuse_Directional )			{ light_diff = StdLighting_Directional(ws_norm); }

	// Specular
	float4 light_spec = 0;
	if( per_pixel_specular == PerPixelSpecular_On )					light_spec = StdLighting_Specular(ws_norm, ws_to_eye_norm);

	// Base texture
	float4 tex = 1;
	if( tex_diffuse == TexDiffuse_On )								tex = StdTexturing_2D(In.tex);

	// Enviro mapping
	if( enviro_map == EnviroMap_On )
	{
		float3 ws_env = reflect(-ws_to_eye_norm, ws_norm);
		tex = lerp(tex, StdTexturing_Env(ws_env), g_env_blend_fraction);
	}

	// Diffuse
	float4 result = 1;
		 if( ps_out == PSOut_Zero )								{ result = 0; }
	else if( ps_out == PSOut_One )								{ result = 1; }
	else if( ps_out == PSOut_Amb )								{ result = StdLighting_Ambient(); }
	else if( ps_out == PSOut_Amb_p_InDiff )						{ result = StdLighting_Ambient() + In.diff; }
	else if( ps_out == PSOut_Amb_p_Tex )						{ result = StdLighting_Ambient() + tex; }
	else if( ps_out == PSOut_Amb_x_Tex_p_LtSpec )				{ result = StdLighting_Ambient() * tex + light_spec; }
	else if( ps_out == PSOut_InDiff )							{ result = In.diff; }
	else if( ps_out == PSOut_InDiff_p_InSpec )					{ result = In.diff + In.spec; }
	else if( ps_out == PSOut_InDiff_p_Tex )						{ result = In.diff + tex; }
	else if( ps_out == PSOut_InDiff_x_Tex )						{ result = In.diff * tex; }
	else if( ps_out == PSOut_InDiff_x_Tex_p_InSpec )			{ result = In.diff * tex + In.spec; }
	else if( ps_out == PSOut_InDiff_x_Tex_p_LtSpec )			{ result = In.diff * tex + light_spec; }
	else if( ps_out == PSOut_InDiff_p_Tex_x_LtDiff )			{ result = In.diff + tex * light_diff; }
	else if( ps_out == PSOut_InDiff_x_Tex_x_LtDiff )			{ result = In.diff * tex * light_diff; }
	else if( ps_out == PSOut_InDiff_x_Tex_x_LtDiff_p_InSpec )	{ result = In.diff * tex * light_diff + In.spec; }
	else if( ps_out == PSOut_InDiff_x_Tex_x_LtDiff_p_LtSpec )	{ result = In.diff * tex * light_diff + light_spec; }
	else if( ps_out == PSOut_InDiff_p_LtDiff )					{ result = In.diff + light_diff; }
	else if( ps_out == PSOut_InDiff_x_LtDiff ) 					{ result = In.diff * light_diff; }
	else if( ps_out == PSOut_InDiff_x_LtDiff_p_LtSpec )			{ result = In.diff * light_diff + light_spec; }
	else if( ps_out == PSOut_InSpec )							{ result = In.spec; }
	else if( ps_out == PSOut_Tex )								{ result = tex; }
	else if( ps_out == PSOut_Tex_x_LtDiff )						{ result = tex * light_diff; }
	else if( ps_out == PSOut_Tex_x_LtDiff_p_LtSpec )			{ result = tex * light_diff + light_spec; }
	else if( ps_out == PSOut_Tex_p_LtSpec )						{ result = tex + light_spec; }
	else if( ps_out == PSOut_LtDiff )							{ result = light_diff; }
	else if( ps_out == PSOut_LtDiff_p_LtSpec )					{ result = light_diff + light_spec; }
	else if( ps_out == PSOut_LtSpec )							{ result = light_spec; }
	else { result = In.pos; } // Compile error here means unsupported ps_out
	return result;
}

#endif//HLSL_PS_GENERIC_FXH