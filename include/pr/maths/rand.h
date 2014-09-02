//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
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
		// 'r' stands for 'range', 'c' stands for 'centred'
		Rnd() {}
		explicit Rnd(ulong s) :MersenneTwister(s) {}
		void seed(ulong s) { MersenneTwister::seed(s); }
		
		// Generates a random number on [0,0xffffffff]-interval
		ulong  u32()                      { return MersenneTwister::u32(); }
		
		// Generates a random number on [mn,mx]-interval
		ulong  u32r(ulong mn, ulong mx)   { return mn == mx ? mx : (u32() % (mx - mn + 1) + mn); }
		
		// Generates a random number on [avr-d,avr+d]-interval
		ulong  u32c(ulong avr, ulong d)   { return u32r(avr-d, avr+d); }
		
		// Generates a random number on [0,0x7fffffff]-interval
		long   i32()                      { return MersenneTwister::i32(); }

		// Generates a random number on [mn,mx]-interval
		long   i32r(long mn, long mx)     { return mn == mx ? mx : (i32() % (mx - mn + 1) + mn); }
		
		// Generates a random number on [avr-d,avr+d]-interval
		long   i32c(long avr, long d)     { return i32r(avr-d,avr+d); }

		// Generates a random number on [0,0xff]-interval
		uint8  u8()                       { return static_cast<uint8>(u32() & 0xFF); }
		
		// Generates a random number on [mn,mx]-interval
		uint8  u8(uint8 mn, uint8 mx)     { return mn == mx ? mx : (u8() % (mx - mn + 1) + mn); }
		
		// generates a random number on [0,1)-real-interval
		double dbl()                      { return MersenneTwister::f32(); }
		
		// generates a random number on [mn,mx)-real-interval
		double dblr(double mn, double mx) { return dbl() * (mx - mn) + mn; }
		
		// generates a random number on [avr-d,avr+d)-real-interval
		double dblc(double avr, double d) { return (2.0 * dbl() - 1.0) * d + avr; }
		
		// generates a random number on [0,1)-real-interval
		float  flt()                      { return static_cast<float>(dbl()); }
		
		// generates a random number on [mn,mx)-real-interval
		float  fltr(float mn, float mx)   { return static_cast<float>(dblr(mn, mx)); }
		
		// generates a random number on [avr-d,avr+d)-real-interval
		float  fltc(float avr, float d)   { return static_cast<float>(dblc(avr, d)); }
	};
	
	namespace rand
	{
		inline Rnd&   Rand()                     { static Rnd s_rnd; return s_rnd; }
		inline void   Seed(uint seed)            { Rand().seed(seed); }
		inline uint   u32()                      { return Rand().u32(); }
		inline uint   u32r(uint mn, uint mx)     { return Rand().u32r(mn,mx); }
		inline uint   u32c(uint avr, uint d)     { return Rand().u32c(avr,d); }
		inline int    i32()                      { return Rand().i32(); }
		inline int    i32r(int mn, int mx)       { return Rand().i32r(mn,mx); }
		inline int    i32c(int avr, int d)       { return Rand().i32c(avr,d); }
		inline uint8  u8()                       { return Rand().u8(); }
		inline uint8  u8(uint8 mn, uint8 mx)     { return Rand().u8(mn,mx); }
		inline double dbl()                      { return Rand().dbl(); }
		inline double dblr(double mn, double mx) { return Rand().dblr(mn,mx); }
		inline double dblc(double avr, double d) { return Rand().dblc(avr,d); }
		inline float  flt()                      { return Rand().flt(); }
		inline float  fltr(float mn, float mx)   { return Rand().fltr(mn,mx); }
		inline float  fltc(float avr, float d)   { return Rand().fltc(avr,d); }

		template <typename T> T range(T mn, T mx)   { return static_cast<T>(Rand().u32r(mn,mx)); }
		template <typename T> T centred(T mn, T mx) { return static_cast<T>(Rand().u32c(mn,mx)); }
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
