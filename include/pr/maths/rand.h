//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/rand_mt19937.h"

namespace pr
{
	// Map the MT to a shorter name and simpler interface
	struct Rand :private MersenneTwister
	{
		// 'r' stands for 'range', 'c' stands for 'centred'
		Rand()
			:MersenneTwister()
		{}
		explicit Rand(ulong s)
			:MersenneTwister(s)
		{}

		// Set the send for the random number generator
		void seed(ulong s)
		{
			MersenneTwister::seed(s);
		}

		// Generates a random number on [0,0xffffffff]-interval
		ulong u32()
		{
			return MersenneTwister::u32();
		}

		// Generates a random number on [mn,mx]-interval
		ulong u32r(ulong mn, ulong mx)
		{
			return mn == mx ? mx : (u32() % (mx - mn + 1) + mn);
		}

		// Generates a random number on [avr-d,avr+d]-interval
		ulong u32c(ulong avr, ulong d)
		{
			return u32r(avr - d, avr + d);
		}

		// Generates a random number on [0,0x7fffffff]-interval
		long i32()
		{
			return MersenneTwister::i32();
		}

		// Generates a random number on [mn,mx]-interval
		long i32r(long mn, long mx)
		{
			return mn == mx ? mx : (i32() % (mx - mn + 1) + mn);
		}

		// Generates a random number on [avr-d,avr+d]-interval
		long i32c(long avr, long d)
		{
			return i32r(avr - d, avr + d);
		}

		// Generates a random number on [0,0xff]-interval
		uint8 u8()
		{
			return static_cast<uint8>(u32() & 0xFF);
		}

		// Generates a random number on [mn,mx]-interval
		uint8 u8(uint8 mn, uint8 mx)
		{
			return mn == mx ? mx : (u8() % (mx - mn + 1) + mn);
		}

		// Generates a random number on [0,1)-real-interval
		double dbl()
		{
			return MersenneTwister::f32();
		}

		// Generates a random number on [mn,mx)-real-interval
		double dblr(double mn, double mx)
		{
			return dbl() * (mx - mn) + mn;
		}

		// Generates a random number on [avr-d,avr+d)-real-interval
		double dblc(double avr, double d)
		{
			return (2.0 * dbl() - 1.0) * d + avr;
		}

		// Generates a random number on [0,1)-real-interval
		float flt()
		{
			return static_cast<float>(dbl());
		}

		// Generates a random number on [mn,mx)-real-interval
		float fltr(float mn, float mx)
		{
			return static_cast<float>(dblr(mn, mx));
		}

		// Generates a random number on [avr-d,avr+d)-real-interval
		float fltc(float avr, float d)
		{
			return static_cast<float>(dblc(avr, d));
		}

		// Generates a random true or false
		bool boolean()
		{
			return (u32() % 2) != 0;
		}
	};

	// Global static instance
	inline Rand& g_Rand()
	{
		static Rand s_rnd;
		return s_rnd;
	}

	// Linear congruential integer generator
	struct IRandom
	{
		static uint const A = 16807L, C = 0, M = 2147483647L; // Magic numbers mmmm ;)
		uint m_value; // Range = [0,M)

		IRandom()
			:IRandom(1)
		{}
		IRandom(uint seed)
			:m_value(seed + (seed == 0))
		{
			next();
		}
		uint value() const
		{
			return m_value;
		}
		uint next()
		{
			return m_value = (A * m_value + C) % M; // linear congruential generator
		}
		int next(int mn, int mx)
		{
			return (int)next() % (mx - mn) + mn;
		}
	};
	
	// Linear congruential float generator
	struct FRandom
	{
		static uint const A = 1664525, C = 0, M = 1013904223; // Magic numbers mmmm ;)
		float m_value; // Range = [0,1)

		FRandom()
			:m_value(0.0f)
		{
			next();
		}
		FRandom(float seed)
			:m_value(seed)
		{
			next();
		}
		float value() const
		{
			return m_value;
		}
		float next()
		{
			static_assert(sizeof(float) == sizeof(uint32), ""); // Only works if this is true
			uint32 const float_one  = 0x3f800000, float_mask = 0x007fffff;
			
			auto new_value = A * reinterpret_cast<uint const&>(m_value) + M;
			auto temp = float_one | (float_mask & new_value);
			return m_value = reinterpret_cast<float const&>(temp) - 1.0f;
		}
		float next(float mn, float mx)
		{
			return next() * (mx - mn) + mn;
		}
	};
}

