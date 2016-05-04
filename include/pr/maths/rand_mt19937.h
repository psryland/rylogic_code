//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
//  A C-program for MT19937, with initialization improved 2002/1/26.
//  Coded by Takuji Nishimura and Makoto Matsumoto.
//
//  Before using, initialize the state by using seed(seed)
//  or init(init_key, key_length).
//
//  Copyright (C) 1997 - 2002, Makoto Matsumoto and Takuji Nishimura,
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without
//  modification, are permitted provided that the following conditions
//  are met:
//
//   1. Redistributions of source code must retain the above copyright
//      notice, this list of conditions and the following disclaimer.
//
//   2. Redistributions in binary form must reproduce the above copyright
//      notice, this list of conditions and the following disclaimer in the
//      documentation and/or other materials provided with the distribution.
//
//   3. The names of its contributors may not be used to endorse or promote
//      products derived from this software without specific prior written
//      permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//  Any feedback is very welcome.
//  http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/emt.html
//  email: m-mat @ math.sci.hiroshima-u.ac.jp (remove space)
//
#pragma once
#ifndef PR_RAND_MT19937_H
#define PR_RAND_MT19937_H

namespace pr
{
	struct MersenneTwister
	{
		enum
		{
			// Period parameters
			Len        = 624,
			M          = 397,
			UpperMask  = 0x80000000UL, // most significant w-r bits
			LowerMask  = 0x7fffffffUL, // least significant r bits
		};
		unsigned long m_state[Len];    // the array for the state vector
		int m_index;                   // m_index == Len+1 means m_state[Len] is not initialized
		
		MersenneTwister()
			:MersenneTwister(0UL)
		{}
		explicit MersenneTwister(unsigned long s)
			:m_index(Len + 1)
		{
			seed(s);
		}
		
		// Initializes m_state[Len] with a seed
		void seed(unsigned long s)
		{
			m_state[0] = s & 0xffffffffUL;
			for (m_index = 1; m_index < Len; ++m_index)
			{
				// See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier.
				// In the previous versions, MSBs of the seed affect
				// only MSBs of the array m_state[].
				// 2002/01/09 modified by Makoto Matsumoto
				m_state[m_index] = (1812433253UL * (m_state[m_index-1] ^ (m_state[m_index-1] >> 30)) + m_index);
				m_state[m_index] &= 0xffffffffUL; // for >32 bit machines
			}
		}
		
		// Initialize by an array with length 'key_length'
		void init(unsigned long const* init_key, int key_length)
		{
			int i=1, j=0, k;
			seed(19650218UL);
			for (k = (Len>key_length ? Len : key_length); k; k--)
			{
				m_state[i] = (m_state[i] ^ (1664525UL * (m_state[i-1] ^ (m_state[i-1] >> 30)))) + init_key[j] + j; // non linear
				m_state[i] &= 0xffffffffUL; // for WORDSIZE > 32 machines
				i++; j++;
				if (i >= Len)
				{
					m_state[0] = m_state[Len-1];
					i=1;
				}
				if (j >= key_length) j=0;
			}
			for (k = Len-1; k; k--)
			{
				m_state[i] = (m_state[i] ^ (1566083941UL * (m_state[i-1] ^ (m_state[i-1] >> 30)))) - i; // non linear
				m_state[i] &= 0xffffffffUL; // for WORDSIZE > 32 machines
				i++;
				if (i>=Len)
				{
					m_state[0] = m_state[Len-1];
					i=1;
				}
			}
			m_state[0] = 0x80000000UL; // MSB is 1; assuring non-zero initial array
		}
		
		// Generates a random number on [0,0xffffffff]-interval
		unsigned long u32()
		{
			unsigned long const matrix_a = 0x9908b0dfUL;    // constant vector a
			unsigned long const mag01[2] = {0x0UL, matrix_a}; // mag01[x] = x * matrix_a  for x=0,1
			unsigned long y;
			if (m_index >= Len)  // generate Len words at one time
			{
				if (m_index == Len + 1)    // if seed() has not been called,
					seed(5489UL);          // a default initial seed is used
				int kk;
				for (kk = 0; kk < Len - M; kk++)
				{
					y = (m_state[kk] & UpperMask) | (m_state[kk+1] & LowerMask);
					m_state[kk] = m_state[kk+M] ^ (y >> 1) ^ mag01[y & 0x1UL];
				}
				for (; kk < Len - 1; kk++)
				{
					y = (m_state[kk] & UpperMask) | (m_state[kk+1] & LowerMask);
					m_state[kk] = m_state[kk+(M-Len)] ^ (y >> 1) ^ mag01[y & 0x1UL];
				}
				y = (m_state[Len-1] & UpperMask) | (m_state[0] & LowerMask);
				m_state[Len-1] = m_state[M-1] ^ (y >> 1) ^ mag01[y & 0x1UL];
				m_index = 0;
			}
			y = m_state[m_index++];
			
			// Tempering
			y ^= (y >> 11);
			y ^= (y <<  7) & 0x9d2c5680UL;
			y ^= (y << 15) & 0xefc60000UL;
			y ^= (y >> 18);
			return y;
		}
		
		// Generates a random number on [0,0x7fffffff]-interval
		long i32()
		{
			return (long)(u32()>>1);
		}
		
		// generates a random number on [0,1)-real-interval
		double f32()
		{
			return u32() * (1.0/4294967296.0); // divided by 2^32
		}
		
		// generates a random number on [0,1]-real-interval
		double f32_2()
		{
			return u32() * (1.0/4294967295.0);  // divided by 2^32-1
		}
		
		// generates a random number on (0,1)-real-interval
		double f32_3()
		{
			return (((double)u32()) + 0.5) * (1.0/4294967296.0); // divided by 2^32
		}
		
		// generates a random number on [0,1) with 53-bit resolution
		double f32_res53()
		{
			unsigned long a=u32()>>5, b=u32()>>6;
			return(a*67108864.0+b)*(1.0/9007199254740992.0); // These real versions are due to Isaku Wada, 2002/01/09 added
		}
	};
}

#endif
