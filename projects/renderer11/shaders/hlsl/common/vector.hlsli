//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************

// Returns true if all vector elements are 0
bool all_zero(float4 a)
{
	return !any(a);
}

// Returns true if all vector elements are >= 0
bool all_zero_or_positive(float4 a)
{
	return !any(abs(a) - a);
}

// Triple product
float triple(float4 a, float4 b, float4 c)
{
	return dot(a, float4(cross(b.xyz, c.xyz), 0));
}
