//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2010
//***********************************************

// Encode/Decode a normal vector
float2 EncodeNormal(float3 norm)
{
	float f = sqrt(8 * norm.z + 8);
	return norm.xy / f + 0.5;
}
float3 DecodeNormal(float2 enc)
{
	float2 fenc = enc*4 - 2;
	float f = dot(fenc, fenc);
	float g = sqrt(1 - f/4);
	float3 norm;
	norm.xy = fenc * g;
	norm.z = 1 - f/2;
	return norm;
}

// half2 EncodeNormal(half3 norm)
// {
	// half f = half(sqrt(8 * norm.z + 8));
	// return norm.xy / f + 0.5;
// }
// half3 DecodeNormal(half2 enc)
// {
	// half2 fenc = enc*4 - 2;
	// half f = dot(fenc, fenc);
	// half g = half(sqrt(1 - f/4));
	// half3 norm;
	// norm.xy = fenc * g;
	// norm.z = 1 - f/2;
	// return norm;
// }

// half2 EncodeNormal(half3 norm)
// {
	// half scale = 1.7777;
	// half2 enc = norm.xy / (norm.z+1);
	// enc /= scale;
	// enc = enc*0.5+0.5;
	// return enc;
// }
// half3 DecodeNormal(half2 enc)
// {
	// half scale = 1.7777;
	// half3 nn = half3(2*scale*enc.xy,0) + half3(-scale,-scale,1);
	// half g = 2.0 / dot(nn.xyz,nn.xyz);
	// half3 norm;
	// norm.xy = g*nn.xy;
	// norm.z = g-1;
	// return norm;
// }
