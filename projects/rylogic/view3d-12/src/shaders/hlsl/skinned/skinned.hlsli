//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_SKINNED_HLSLI
#define PR_RDR_SHADER_SKINNED_HLSLI
#include "../types.hlsli"

// Skin 'vert'. 'vert' is a vertex in model space when in the rest pose
float4 SkinVertex(in uniform StructuredBuffer<float4x4> skeleton, in uniform Skinfluence influence, in float4 vert)
{
	float4 skinned_vert = 0;
	skinned_vert += mul(vert, skeleton[influence.m_bones.x]) * influence.m_weights.x;
	skinned_vert += mul(vert, skeleton[influence.m_bones.y]) * influence.m_weights.y;
	skinned_vert += mul(vert, skeleton[influence.m_bones.z]) * influence.m_weights.z;
	skinned_vert += mul(vert, skeleton[influence.m_bones.w]) * influence.m_weights.w;
	return skinned_vert;
}

// Skin 'norm'. 'norm' is a normal in model space when in the rest pose
float4 SkinNormal(in uniform StructuredBuffer<float4x4> skeleton, in uniform Skinfluence influence, in float4 norm)
{
	float4 skinned_vert = 0;
	skinned_vert += mul(norm, skeleton[influence.m_bones.x]) * influence.m_weights.x;
	skinned_vert += mul(norm, skeleton[influence.m_bones.y]) * influence.m_weights.y;
	skinned_vert += mul(norm, skeleton[influence.m_bones.z]) * influence.m_weights.z;
	skinned_vert += mul(norm, skeleton[influence.m_bones.w]) * influence.m_weights.w;
	return skinned_vert;
}

#endif
