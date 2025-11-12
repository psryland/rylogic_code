//*********************************************
// HLSL
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#ifndef PR_HLSL_VECTOR_HLSLI
#define PR_HLSL_VECTOR_HLSLI

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

// Rotate a 2D vector
float2 RotateCW(float2 a)
{
	return float2(a.y, -a.x);
}
float2 RotateCCW(float2 a)
{
	return float2(-a.y, a.x);
}

// Invert a matrix assuming that it's an orthonormal matrix
float4x4 InvertOrthonormal(float4x4 mat)
{
	float3x3 rt = transpose((float3x3) mat);
	float3 inv_t = -mul(mat[3].xyz, rt);
	
	// Reconstruct the inverse matrix
	return float4x4(
		rt[0].x, rt[0].y, rt[0].z, 0,
		rt[1].x, rt[1].y, rt[1].z, 0,
		rt[2].x, rt[2].y, rt[2].z, 0,
		inv_t.x, inv_t.y, inv_t.z, 1
	);
}

// Invert a matrix assuming that it's an affine matrix
float4x4 InvertAffine(float4x4 mat)
{
	float3 t = mat[3].xyz;
	float3x3 r = (float3x3)mat;
	float3 s = float3(length(r[0]), length(r[1]), length(r[2]));
	
	// Remove scale
	r[0] /= s.x;
	r[1] /= s.y;
	r[2] /= s.z;

	// Invert rotation
	float3x3 rt = transpose(r);

	// Invert scale
	rt[0] /= s.x;
	rt[1] /= s.y;
	rt[2] /= s.z;

	// Invert translation
	float3 inv_t = float3(
		-(t.x * rt[0].x + t.y * rt[1].x + t.z * rt[2].x),
		-(t.x * rt[0].y + t.y * rt[1].y + t.z * rt[2].y),
		-(t.x * rt[0].z + t.y * rt[1].z + t.z * rt[2].z)
	);

	// Reconstruct the inverse matrix
	return float4x4(
		rt[0].x, rt[0].y, rt[0].z, 0,
		rt[1].x, rt[1].y, rt[1].z, 0,
		rt[2].x, rt[2].y, rt[2].z, 0,
		inv_t.x, inv_t.y, inv_t.z, 1
	);
}

// Return a vector representing the approximate rotation between two orthonormal transforms
float3 RotationVectorApprox(float3x3 from, float3x3 to)
{
	// Note: 'from' and 'to' must be orthonormal matrices.
	//
	// Given a vector 'avel' representing a small step of angular velocity, a rotation
	// can be applied to an orthonormal matrix by adding the cross product of each basis
	// vector to the matrix. e.g.,
	//   float3x3 orientation = {...};
	//   orientation[0] += cross(avel, orientation[0]);
	//   orientation[1] += cross(avel, orientation[2]);
	//   orientation[2] += cross(avel, orientation[1]);
	//   Orthonormalize(orientation); // renormalize the basis vectors
	//
	// Vector cross product can also be represented using a cross product matrix:
	//   CPM of avel = 
	//      [   0     avel.z  -avel.y]
	//      [-avel.z    0      avel.x]
	//      [ avel.y -avel.x     0   ]
	// So the rotation can be applied like this:
	//   orientation += CPM(avel) * orientation;
	//
	// This functions uses the inverse of this principle to recreate the 'avel' vector
	// from two orientation matrices, assuming one matrix is a small rotation from the other.
	// i.e.
	//   to = from + CPM(avel) * from
	//   to - from = CPM(avel) * from
	//   (to - from) * fromT = CPM(avel) * from * fromT
	//   (to - from) * fromT = CPM(avel) <-- can read out the components of 'avel' from this
	float3x3 cpm = mul((to - from), transpose(from));
	return float3(cpm[1].z, cpm[2].x, cpm[0].y);
}

// Orthonormalise a rotation matrix
float3x3 Orthonormalise(float3x3 mat)
{
	mat[0] = normalize(mat[0]);
	mat[1] = normalize(cross(mat[2], mat[0]));
	mat[2] = cross(mat[0], mat[1]);
	return mat;
}
float4x4 Orthonormalise(float4x4 mat)
{
	mat[0].xyz = normalize(mat[0].xyz);
	mat[1].xyz = normalize(cross(mat[2].xyz, mat[0].xyz));
	mat[2].xyz = cross(mat[0].xyz, mat[1].xyz);
	return mat;
}


#endif
