#ifndef HLSL_VS_BASIC_FXH
#define HLSL_VS_BASIC_FXH

#include "InOutStructs.fxh"
#include "Identifiers.fxh"
#include "Common.fxh"
#include "StdLighting.fxh"

// 1.1
VSOut_PosDiffTexSpecEnv VS11_Generic(VSIn_PosNormDiffTex In,
	uniform VertexDiffuse		vertex_diffuse,
	uniform VertexSpecular		vertex_specular,
	uniform TexCoordsDiffuse	tex_diffuse,
	uniform TexCoordsEnviroMap	enviro_map,
	uniform VSDiffOut			diffuse_source)
{
    float4	ws_pos			= ObjectToWorld(float4(In.pos,  1));
	float4	ws_norm			= ObjectToWorld(float4(In.norm, 0));
	float4	ws_to_eye_norm	= normalize(WSCameraPosition() - ws_pos);

	// Diffuse light
	float4 light_diff = 1;
	if( vertex_diffuse == VertexDiffuse_Amb )				light_diff = StdLighting_Ambient();
	if( vertex_diffuse == VertexDiffuse_Amb_p_Directional )	light_diff = StdLighting_Ambient() + StdLighting_Directional(ws_norm);
	if( vertex_diffuse == VertexDiffuse_Amb_p_Point )		light_diff = StdLighting_Ambient() + StdLighting_Point(ws_pos, ws_norm);
	if( vertex_diffuse == VertexDiffuse_Directional )		light_diff = StdLighting_Directional(ws_norm);
	if( vertex_diffuse == VertexDiffuse_Point )				light_diff = StdLighting_Point(ws_pos, ws_norm);
	
	// Specular	light
	float4	light_spec = 0;
	if( vertex_specular == VertexSpecular_On )				light_spec = StdLighting_Specular(ws_norm.xyz, ws_to_eye_norm.xyz);

	// Texture coords
	float2	tex = 0;
	if( tex_diffuse == TexCoordsDiffuse_On )				tex = In.tex;

	// Enviro map
	float3 ws_env = 0;
	if( enviro_map == TexCoordsEnviroMap_On )				ws_env = reflect(-ws_to_eye_norm, ws_norm);
	
	// Diffuse output
	float4 diff = 1;
		 if( diffuse_source == VSDiffOut_Zero )				{ diff = 0; }
	else if( diffuse_source == VSDiffOut_One )				{ diff = 1; }
	else if( diffuse_source == VSDiffOut_InDiff ) 			{ diff = In.diff; }
	else if( diffuse_source == VSDiffOut_InDiff_p_LtDiff ) 	{ diff = In.diff + light_diff; }
	else if( diffuse_source == VSDiffOut_InDiff_x_LtDiff ) 	{ diff = In.diff * light_diff; }
	else if( diffuse_source == VSDiffOut_LtDiff )			{ diff = light_diff; }
	else if( diffuse_source == VSDiffOut_LtDiff_p_LtSpec )	{ diff = light_diff + light_spec; }
	
    VSOut_PosDiffTexSpecEnv Out;
    Out.pos			= ObjectToScreen(float4(In.pos, 1));
    Out.diff		= diff;
    Out.tex			= tex;
    Out.spec		= light_spec;
    Out.ws_env		= ws_env;
    return Out;
}

// 2.0
VSOut_PosDiffTexSpecEnvWSNormWSToeye VS20_Generic(VSIn_PosNormDiffTex In,
	uniform VertexDiffuse		vertex_diffuse,
	uniform VertexSpecular		vertex_specular,
	uniform TexCoordsDiffuse	tex_diffuse,
	uniform TexCoordsEnviroMap	enviro_map,
	uniform VSDiffOut			diffuse_source)
{
	VSOut_PosDiffTexSpecEnv Out11 = VS11_Generic(In, vertex_diffuse, vertex_specular, tex_diffuse, enviro_map, diffuse_source); 
	
    float4	ws_pos	= ObjectToWorld(float4(In.pos,  1));
	float4	ws_norm	= ObjectToWorld(float4(In.norm, 0));

	VSOut_PosDiffTexSpecEnvWSNormWSToeye Out;
	Out.pos			= Out11.pos;
	Out.diff		= Out11.diff;
	Out.tex			= Out11.tex;
	Out.spec		= Out11.spec;
	Out.ws_norm		= ws_norm.xyz;
	Out.ws_to_eye	= (WSCameraPosition() - ws_pos).xyz;
	return Out;
}

#endif//HLSL_VS_BASIC_FXH
