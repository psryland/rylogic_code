//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#ifndef PR_MATHS_RAND_H
#define PR_MATHS_RAND_H

#include "pr/maths/forward.h"
#include "pr/maths/rand_mt19937.h"

namespace pr
{
	// Map the MT to a shorter name and simpler interface
	struct Rnd :private MersenneTwister
	{
		Rnd() {}
		explicit Rnd(ulong s) :MersenneTwister(s) {}
		void   seed(ulong s)             { MersenneTwister::seed(s); }
		ulong  u32()                     { return MersenneTwister::u32(); }
		ulong  u32(ulong mn, ulong mx)   { return mn == mx ? mx : ((u32() % (mx - mn)) + mn); }
		long   i32()                     { return MersenneTwister::i32(); }
		long   i32(long mn, long mx)     { return mn == mx ? mx : ((i32() % (mx - mn)) + mn); }
		uint8  u8()                      { return static_cast<uint8>(u32() >> 24); }
		uint8  u8(uint8 mn, uint8 mx)    { return mn == mx ? mx : ((u8() % (mx - mn)) + mn); }
		double d32()                     { return MersenneTwister::f32(); }
		double d32(double mn, double mx) { return d32() * (mx - mn) + mn; }
		float  f32()                     { return static_cast<float>(d32()); }
		float  f32(float mn, float mx)   { return static_cast<float>(d32(mn, mx)); }
	};
	
	namespace rand
	{
		inline Rnd&   Rand()                    { static Rnd s_rnd; return s_rnd; }
		inline void   Seed(uint seed)           { Rand().seed(seed); }
		inline uint   u32()                     { return Rand().u32(); }
		inline uint   u32(uint mn, uint mx)     { return Rand().u32(mn,mx); }
		inline int    i32()                     { return Rand().i32(); }
		inline int    i32(int mn, int mx)       { return Rand().i32(mn,mx); }
		inline uint8  u8()                      { return Rand().u8(); }
		inline uint8  u8(uint8 mn, uint8 mx)    { return Rand().u8(mn,mx); }
		inline double d32()                     { return Rand().d32(); }
		inline double d32(double mn, double mx) { return Rand().d32(mn,mx); }
		inline float  f32()                     { return Rand().f32(); }
		inline float  f32(float mn, float mx)   { return Rand().f32(mn,mx); }
	}
	
	// Random integer generator
	struct IRandom
	{
		uint m_value; // Range = [0,M)
		IRandom()          : m_value(1)        { next(); }
		IRandom(uint seed) : m_value(seed + 1) { next(); }
		uint value() const                     { return m_value; }
		uint next()
		{ // Magic numbers mmmm ;)
			const uint A = 16807L;
			const uint C = 0;
			const uint M = 2147483647L;
			return m_value = (A * m_value + C) % M; // linear congruential generator
		}
	};
	inline int IRand(IRandom& rand, int mn, int mx) { return (int)rand.next() % (mx - mn) + mn; }
	
	// Random float generator
	struct FRandom
	{
		float m_value; // Range = [0,1)
		FRandom()           : m_value(0.0f)   { next(); }
		FRandom(float seed) : m_value(seed)   { next(); }
		float value() const                   { return m_value; }
		float next()
		{
			static_assert(sizeof(float)==sizeof(uint32), ""); // Only works if this is true
			const uint32 float_one  = 0x3f800000;
			const uint32 float_mask = 0x007fffff;
			uint32 new_value = 1664525L * reinterpret_cast<const uint32&>(m_value) + 1013904223L; // Magic numbers mmmm ;)
			uint32 temp = float_one | (float_mask & new_value);
			return m_value = reinterpret_cast<const float&>(temp) - 1.0f;
		}
	};
	inline float FRand(FRandom& rand, float mn, float mx) { return rand.next() * (mx - mn) + mn; }
}

#endif
