//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_RDR_SHADER_SKINNED_HLSLI
#define PR_RDR_SHADER_SKINNED_HLSLI
#include "../types.hlsli"

// Skin 'vert'. 'vert' is a vertex in model space when in the rest pose
float4 SkinVertex(in uniform StructuredBuffer<Mat4x4> pose, in uniform Skinfluence influence, in float4 vert)
{
	float4 skinned_vert = float4(0, 0, 0, 0);
	skinned_vert += mul(vert, pose[influence.m_bones.x].m) * influence.m_weights.x;
	skinned_vert += mul(vert, pose[influence.m_bones.y].m) * influence.m_weights.y;
	skinned_vert += mul(vert, pose[influence.m_bones.z].m) * influence.m_weights.z;
	skinned_vert += mul(vert, pose[influence.m_bones.w].m) * influence.m_weights.w;
	return skinned_vert;
}

// Skin 'norm'. 'norm' is a normal in model space when in the rest pose
float4 SkinNormal(in uniform StructuredBuffer<Mat4x4> pose, in uniform Skinfluence influence, in float4 norm)
{
	float4 skinned_norm = float4(0, 0, 0, 0);
	skinned_norm += mul(norm, pose[influence.m_bones.x].m) * influence.m_weights.x;
	skinned_norm += mul(norm, pose[influence.m_bones.y].m) * influence.m_weights.y;
	skinned_norm += mul(norm, pose[influence.m_bones.z].m) * influence.m_weights.z;
	skinned_norm += mul(norm, pose[influence.m_bones.w].m) * influence.m_weights.w;
	return skinned_norm;
}

#endif

