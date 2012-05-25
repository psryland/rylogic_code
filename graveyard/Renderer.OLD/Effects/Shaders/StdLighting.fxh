//******************************************************************
//
//	StdLighting
//
//******************************************************************
//	This file contains lighting fragment functions:
#ifndef STDLIGHTING_FXH
#define STDLIGHTING_FXH

#include "Common.fxh"

shared float4		g_ws_light_position		: Position;
shared float4		g_ws_light_direction	: Direction;
shared float4		g_light_ambient			: Ambient;
shared float4		g_light_diffuse			: Diffuse;
shared float4		g_light_specular		: Specular;
shared float		g_specular_power 		: SpecularPower
<
    string	UIWidget	= "slider";
    float	UIMin		= 0.0;
    float	UIMax		= 200.0;
    float	UIStep		= 1.0;
    string	UIName		= "Specular Power";
> = 10.0;

//*****
// Ambient light
float4 StdLighting_Ambient() : Color0
{
	return g_light_ambient;
}

//*****
// Directional light
float4 StdLighting_Directional(float3 ws_norm) : Color0
{
	return g_light_diffuse * saturate(dot(-g_ws_light_direction.xyz, ws_norm));
}

//*****
// Point light
float4 StdLighting_Point(float3 ws_pos, float3 ws_norm) : Color0
{
	float ws_to_light = normalize(g_ws_light_position - ws_pos);
	return g_light_diffuse * saturate(dot(ws_to_light, ws_norm));
}

//*****
// Specular light
float4 StdLighting_Specular(float3 ws_norm, float3 ws_to_eye_norm) : Color0
{
	float3 ws_H = normalize(ws_to_eye_norm - g_ws_light_direction);
	return g_light_specular * pow(saturate(dot(ws_norm, ws_H)), g_specular_power);
}

#endif//STDLIGHTING_FXH