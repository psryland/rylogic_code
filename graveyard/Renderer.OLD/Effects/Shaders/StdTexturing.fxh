//******************************************************************
//
//	StdTexturing
//
//******************************************************************
//	This file contains texturing fragment functions:
#ifndef STDTEXTURING_FXH
#define STDTEXTURING_FXH

shared float g_env_blend_fraction
<
    string	UIWidget	= "slider";
    float	UIMin		= 0.0;
    float	UIMax		= 1.0;
    float	UIStep		= 0.001;
    string	UIName		= "Env blend Fraction";
> = 0.0;

texture2D g_texture0;
sampler2D texture0_sampler_linear	= 
sampler_state
{
    Texture = <g_texture0>;    
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
};

textureCUBE g_environment0 : Environment	<string ResourceType = "Cube";>;
samplerCUBE environment0_sampler_linear =
sampler_state
{
	Texture = <g_environment0>;
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
};


//*****
// Simple 2D texture lookup
float4 StdTexturing_2D(float2 tex) : Color0
{
	return tex2D(texture0_sampler_linear, tex);	
}


//*****
// Environment texture lookup
float4 StdTexturing_Env(float3 ws_direction) : Color0
{
	return texCUBE(environment0_sampler_linear, ws_direction);
}


#endif//STDTEXTURING_FXH
