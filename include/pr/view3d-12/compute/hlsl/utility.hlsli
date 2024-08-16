#pragma once

static const uint FNV_offset_basis32 = 2166136261U;
static const uint FNV_prime32 = 16777619U;

// The missing 'square' function
inline float sqr(float x)
{
	return x * x;
}
inline uint sqr(uint x)
{
	return x * x;
}
inline float sqr(float4 x)
{
	return dot(x, x);
}

// The missing 'cube' function
inline float cube(float x)
{
	return x * x * x;
}
inline uint cube(uint x)
{
	return x * x * x;
}

// Intrinsic for condition ? true_case : false_case
inline float select(bool condition, float true_case, float false_case)
{
	return lerp(false_case, true_case, float(condition));
}
inline float4 select(bool condition, float4 true_case, float4 false_case)
{
	return lerp(false_case, true_case, float(condition));
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
	float z = sqrt(1 - sqr(x) - sqr(y));
	return float4(x, y, z, 0);
}

// Constant time bit count
inline int CountBits(uint n)
{
	// http://infolab.stanford.edu/~manku/bitcount/bitcount.html
	// Constant time bit count works for 32-bit numbers only.
	uint tmp = n
		- ((n >> 1) & 033333333333)
		- ((n >> 2) & 011111111111);
	return ((tmp + (tmp >> 3)) & 030707070707) % 63;
}
