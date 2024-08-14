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

// The missing 'cube' function
inline float cube(float x)
{
	return x * x * x;
}
inline uint cube(uint x)
{
	return x * x * x;
}

// Accumulative hash function
inline uint Hash(int value, uint hash = FNV_offset_basis32)
{
	return hash = (value + hash) * FNV_prime32;
}

// Generate a random float on the interval [0, 1)
inline float Random(uint seed)
{
	return Hash(seed) / 4294967296.0f; // Normalise to [0, 1)
}

// Generate a random direction vector components on the interval (-1, +1)
inline float4 Random3(uint seed)
{
	return float4(
		2 * Random(seed + FNV_prime32) - 1,
		2 * Random(seed + sqr(FNV_prime32)) - 1,
		2 * Random(seed + sqr(FNV_prime32*FNV_prime32)) - 1,
		0);
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
