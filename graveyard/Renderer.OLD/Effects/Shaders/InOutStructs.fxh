//************************************************************
//
//	InOutStructs.h
//
//************************************************************
#ifndef HLSL_IN_OUT_STRUCTS_FXH
#define HLSL_IN_OUT_STRUCTS_FXH

// Input structs ***************************

struct VSIn_PosNormDiffTex
{
    float3	pos 			:Position;
    float3	norm			:Normal;
    float4	diff			:Color0;
    float2	tex				:TexCoord0;
};

// Output structs ***************************

struct VSOut_PosDiffTexSpecEnv
{
    float4	pos				:Position;
	float4	diff			:Color0;
	float2	tex				:TexCoord0;
    float4	spec			:Color1;
    float3	ws_env			:TexCoord1;
};

struct VSOut_PosDiffTexSpecEnvWSNormWSToeye
{
 	float4	pos				:Position;
 	float4	diff			:Color0;
 	float2	tex				:TexCoord0;
 	float4	spec			:Color1;
	float3	ws_norm			:TexCoord1;
	float3	ws_to_eye		:TexCoord2;
};

#endif//HLSL_IN_OUT_STRUCTS_FXH
