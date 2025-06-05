//***********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2010
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
float4x4 InvertFast(float4x4 mat)  
{
	float3x3 Rt = transpose((float3x3)mat);
	float3 invT = -mul(mat[3].xyz, Rt);

	// Reconstruct the inverse matrix
	return float4x4(
		Rt[0].x, Rt[0].y, Rt[0].z, 0,
		Rt[1].x, Rt[1].y, Rt[1].z, 0,
		Rt[2].x, Rt[2].y, Rt[2].z, 0,
		invT.x, invT.y, invT.z, 1
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

#endif
