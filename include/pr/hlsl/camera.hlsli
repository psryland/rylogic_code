//***********************************************
// HLSL
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
#ifndef PR_HLSL_CAMERA_HLSLI
#define PR_HLSL_CAMERA_HLSLI

// Returns the near and far plane distances in camera space from a
// projection matrix (either perspective or orthonormal)
float2 ClipPlanes(float4x4 c2s)
{
	float2 dist;
	dist.x = c2s._43 / c2s._33; // near
	dist.y = c2s._43 / (1 + c2s._33) + c2s._44*(c2s._43 - c2s._33 - 1)/(c2s._33 * (1 + c2s._33)); // far
	return dist;
}

#endif
