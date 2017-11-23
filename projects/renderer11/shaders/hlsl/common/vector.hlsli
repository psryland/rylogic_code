//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************

#ifndef PR_RDR_VECTOR_HLSLI
#define PR_RDR_VECTOR_HLSLI

// Returns true if all vector elements are 0
bool AllZero(float4 a)
{
	return !any(a);
}

// Returns true if all vector elements are >= 0
bool AllZeroOrPositive(float4 a)
{
	return !any(abs(a) - a);
}

// Return the sum of the components of 'vec'
float SumComponents(float4 vec)
{
	return dot(vec, float4(1,1,1,1));
}

// Triple product
float Triple(float4 a, float4 b, float4 c)
{
	return dot(a, float4(cross(b.xyz, c.xyz), 0));
}

#endif
