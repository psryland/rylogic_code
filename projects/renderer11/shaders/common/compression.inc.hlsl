#ifndef PR_RDR_SHADER_COMPRESSION_INC_HLSL
#define PR_RDR_SHADER_COMPRESSION_INC_HLSL

// Encode/Decode a normal vector
half2 EncodeNormal(half3 norm)
{
	half f = half(sqrt(8 * norm.z + 8));
	return norm.xy / f + 0.5;
}
half3 DecodeNormal(half2 enc)
{
	half2 fenc = enc*4 - 2;
	half f = dot(fenc, fenc);
	half g = half(sqrt(1 - f/4));
	half3 norm;
	norm.xy = fenc * g;
	norm.z = 1 - f/2;
	return norm;
}

#endif
