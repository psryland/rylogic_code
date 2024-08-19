//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#ifndef UTILITY_HLSLI
#define UTILITY_HLSLI

static const uint FNV_offset_basis32 = 2166136261U;
static const uint FNV_prime32 = 16777619U;

// Component square function
inline uint sqr(uint v)
{
	return v * v;
}
inline float sqr(float v)
{
	return v * v;
}
inline float2 sqr(float2 v)
{
	return v * v;
}
inline float3 sqr(float3 v)
{
	return v * v;
}
inline float4 sqr(float4 v)
{
	return v * v;
}

// Swap two values
inline void swap(inout float a, inout float b)
{
	float t = a;
	a = b;
	b = t;
}
inline void swap(inout float2 a, inout float2 b)
{
	float2 t = a;
	a = b;
	b = t;
}
inline void swap(inout float3 a, inout float3 b)
{
	float3 t = a;
	a = b;
	b = t;
}
inline void swap(inout float4 a, inout float4 b)
{
	float4 t = a;
	a = b;
	b = t;
}

// Some the components of a float4
inline float sum(float2 v)
{
	return dot(v, float2(1,1));
}
inline float sum(float3 v)
{
	return dot(v, float3(1,1,1));
}
inline float sum(float4 v)
{
	return dot(v, float4(1,1,1,1));
}

// Intrinsic for condition ? true_case : false_case
inline int select(bool condition, int true_case, int false_case)
{
	int mask = -(int)condition;
	return (mask & true_case) | (~mask & false_case);
}
inline float select(bool condition, float true_case, float false_case)
{
	return lerp(false_case, true_case, float(condition));
}
inline float2 select(bool condition, float2 true_case, float2 false_case)
{
	return lerp(false_case, true_case, float(condition));
}
inline float3 select(bool condition, float3 true_case, float3 false_case)
{
	return lerp(false_case, true_case, float(condition));
}
inline float4 select(bool condition, float4 true_case, float4 false_case)
{
	return lerp(false_case, true_case, float(condition));
}

// Return the index of the component with the minimum value
inline int min_component_index(float2 v)
{
	// step(b,a) == (a >= b) ? 1 : 0 == (b < a) ? 1 : 0
	return (int)step(v.y, v.x); // y < x ? 1 : 0
}
inline int min_component_index(float3 v)
{
	// step(b,a) == (a >= b) ? 1 : 0 == (b < a) ? 1 : 0
	int xy = (int)step(v.y, v.x); // y < x ? 1 : 0
	return select(v[xy] < v.z, xy, 2);
}
inline int min_component_index(float4 v)
{
	// step(b,a) == (a >= b) ? 1 : 0 == (b < a) ? 1 : 0
	int xy = (int)step(v.y, v.x) + 0; // y < x ? 1 : 0
	int zw = (int)step(v.w, v.z) + 2; // w < z ? 3 : 2
	return select(v[xy] < v[zw], xy, zw);
}
inline int max_component_index(float2 v)
{
	// step(b,a) == (a >= b) ? 1 : 0 == (b < a) ? 1 : 0
	return (int)step(v.x, v.y); // x < y ? 1 : 0
}
inline int max_component_index(float3 v)
{
	// step(b,a) == (a >= b) ? 1 : 0 == (b < a) ? 1 : 0
	int xy = (int)step(v.x, v.y) + 0; // x < y ? 1 : 0
	return select(v[xy] > v.z, xy, 2);
}
inline int max_component_index(float4 v)
{
	// step(b,a) == (a >= b) ? 1 : 0 == (b < a) ? 1 : 0
	int xy = (int)step(v.x, v.y) + 0; // x < y ? 1 : 0
	int zw = (int)step(v.z, v.w) + 2; // z < w ? 3 : 2
	return select(v[xy] > v[zw], xy, zw);
}

// Accumulative hash function
inline uint Hash(int value, uint hash = FNV_offset_basis32)
{
	return hash = (value + hash) * FNV_prime32;
}

// A random number on the interval (-1, +1)
inline float RandomN(float seed)
{
	const float prime_32bit = 4294967291.0f;
	const float phi = 2.39996322972865332;

	seed = Hash(asuint(seed * phi));
	float fseed = float(seed) / prime_32bit;
	return sin(fseed);
}

// A random normalised direction vector on the interval (-1, +1)
inline float4 Random3N(float seed)
{
	float x = RandomN(seed);
	float y = RandomN(x);
	float z = RandomN(y);
	return normalize(float4(x, y, z, 0));
}

// Constant time bit count
// use countbits()
//inline int CountBits(uint n)
//{
//	// http://infolab.stanford.edu/~manku/bitcount/bitcount.html
//	// Constant time bit count works for 32-bit numbers only.
//	uint tmp = n - ((n >> 1) & 033333333333) - ((n >> 2) & 011111111111);
//	return ((tmp + (tmp >> 3)) & 030707070707) % 63;
//}

// Return a vector that is not parallel to 'v'
inline float2 NotParallel(float2 v)
{
	v = abs(v);
	return select(v.x > v.y, float2(0,1), float2(1,0));
}
inline float3 NotParallel(float3 v)
{
	v = abs(v);
	return select(v.x > v.y && v.x > v.z, float3(0,0,1), float3(1,0,0));
}
inline float4 NotParallel(float4 v)
{
	v = abs(v);
	return select(v.x > v.y && v.x > v.z, float4(0,0,1,0), float4(1,0,0,0));
}

// Invert an orthonormal matrix
inline row_major float4x4 InvertOrthonormal(row_major float4x4 mat)
{
	// This assumes row_major float4x4's
	return float4x4(
		mat._m00, mat._m10, mat._m20, 0,
		mat._m01, mat._m11, mat._m21, 0,
		mat._m02, mat._m12, mat._m22, 0,
		-dot(mat._m00_m01_m02_m03, mat._m30_m31_m32_m33),
		-dot(mat._m10_m11_m12_m13, mat._m30_m31_m32_m33),
		-dot(mat._m20_m21_m22_m23, mat._m30_m31_m32_m33),
		1);
}

#endif