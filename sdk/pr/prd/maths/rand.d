module prd.maths.rand;

import std.stdio;

class MersenneTwister
{
	private enum
	{	// Period parameters
		N          = 624,
		M          = 397,
		UPPER_MASK = 0x80000000UL, // most significant w-r bits
		LOWER_MASK = 0x7fffffffUL, // least significant r bits
	};
	private uint mt[N] = void;             // the array for the state vector
	private int  mti   = N + 1;            // mti == N+1 means mt[N] is not initialized
	
	// Singleton instance
	private static MersenneTwister m_singleton = null;
	private static this() { m_singleton = new MersenneTwister; }
	@property public static MersenneTwister Instance() { return m_singleton; }
	
	public this() {}
	public this(uint seed) { Seed(seed); }
	
	// Initializes mt[N] with a seed
	public void Seed(uint s)
	{
		mt[0] = s & 0xffffffffU;
		for (mti = 1; mti != N; ++mti)
		{
			mt[mti] = (1812433253U * (mt[mti-1] ^ (mt[mti-1] >> 30)) + mti); 

			// See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier.
			// In the previous versions, MSBs of the seed affect only MSBs of the array mt[].
			// 2002/01/09 modified by Makoto Matsumoto
			mt[mti] &= 0xffffffffU; // for >32 bit machines
		}
	}
	
	// initialize by an array with array-length
	// init_key is the array for initializing keys
	// slight change for C++, 2004/2/26
	public void InitByArray(uint[] init_key)
	{
		int i, j, k;
		Seed(19650218U);
		i=1; j=0;
		k = (N > init_key.length ? N : init_key.length);
		for (; k; k--)
		{
			mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1664525U)) + init_key[j] + j; // non linear
			mt[i] &= 0xffffffffU; // for WORDSIZE > 32 machines
			i++; j++;
			if (i >= N) { mt[0] = mt[N-1]; i=1; }
			if (j >= init_key.length) j=0;
		}
		for (k=N-1; k; k--)
		{
			mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1566083941U)) - i; // non linear
			mt[i] &= 0xffffffffU; // for WORDSIZE > 32 machines
			i++;
			if (i>=N) { mt[0] = mt[N-1]; i=1; }
		}
		mt[0] = 0x80000000U; // MSB is 1; assuring non-zero initial array 
	}
	
	// Generates a random number on interval [0,0xffffffff]
	public uint RandU32()
	{
		const uint MATRIX_A = 0x9908b0dfU;        // constant vector a
		const uint[] mag01  = [0x0U, MATRIX_A];  // mag01[x] = x * MATRIX_A  for x=0,1
		
		uint y;
		if (mti >= N) // generate N words at one time
		{
			if (mti == N + 1) Seed(5489U);   // if seed() has not been called, a default initial seed is used
			int kk;
			for (kk = 0; kk < N - M; kk++)
			{
				y = (mt[kk] & UPPER_MASK) | (mt[kk+1] & LOWER_MASK);
				mt[kk] = mt[kk+M] ^ (y >> 1) ^ mag01[y & 0x1U];
			}
			for (; kk < N - 1; kk++)
			{
				y = (mt[kk] & UPPER_MASK) | (mt[kk+1] & LOWER_MASK);
				mt[kk] = mt[kk+(M-N)] ^ (y >> 1) ^ mag01[y & 0x1U];
			}
			y = (mt[N-1] & UPPER_MASK) | (mt[0] & LOWER_MASK);
			mt[N-1] = mt[M-1] ^ (y >> 1) ^ mag01[y & 0x1U];
			mti = 0;
		}
		y = mt[mti++];
		
		// Tempering
		y ^= (y >> 11);
		y ^= (y <<  7) & 0x9d2c5680U;
		y ^= (y << 15) & 0xefc60000U;
		y ^= (y >> 18);
		return y;
	}
	
	// generates a random number on interval [0,0x7fffffff]
	public int RandI32()
	{
		return cast(int)(RandU32()>>1);
	}
	
	// generates a random number on interval [0f,1f)
	public double RandF32_1()
	{
		return RandU32() * (1.0 / 4294967296.0); // divided by 2^32
	}
	
	// generates a random number on interval [0f,1f]
	public double RandF32_2()
	{
		return RandU32() * (1.0 / 4294967295.0);  // divided by 2^32-1 
	}
	
	// generates a random number on interval (0f,1f)
	public double RandF32_3()
	{
		return (cast(double)RandU32() + 0.5) * (1.0 / 4294967296.0); // divided by 2^32
	}
	
	// generates a random number on interval [0f,1f) with 53-bit resolution
	public double RandF32_Res53()
	{ 
		uint a = RandU32() >> 5, b = RandU32() >> 6;
		return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
	} 
	// These real versions are due to Isaku Wada, 2002/01/09 added
};
	
// Xn = (A*Xn-1 + C) % M } where A = 16807, C = 0, M = 2147483647, X0 = seed
class LinearCongruentialGenerator
{
	private enum { A = 16807U, C = 0U, M = 2147483647U }; // Period parameters
	private uint m_value = 1; // Range = [0,M)
	
	// Singleton instance
	private static LinearCongruentialGenerator m_singleton = null;
	private static this() { m_singleton = new LinearCongruentialGenerator; }
	@property public static LinearCongruentialGenerator Instance() { return m_singleton; }
	
	public this()                              { next(); }
	public this(uint seed)                     { m_value = seed + 1; next(); }
	
	public uint value() const                  { return m_value; }
	public uint value(uint mn, uint mx) const  { return m_value % (mx - mn) + mn; }
	public uint next()                         { return m_value = (A * m_value + C) % M; }
}
	
void   SeedMT(uint seed)           { MersenneTwister.Instance.Seed(seed); }
uint   URand()                     { return MersenneTwister.Instance.RandU32(); }
uint   URand(uint mn, uint mx)     { return (mx == mn) ? mn : (URand() % (mx - mn) + mn); }
int    IRand()                     { return MersenneTwister.Instance.RandI32(); }
int    IRand(int mn, int mx)       { return (mx == mn) ? mn : (IRand() % (mx - mn) + mn); }
double DRand()                     { return MersenneTwister.Instance.RandF32_1(); }
double DRand(double mn, double mx) { return DRand() * (mx - mn) + mn; }
float  FRand()                     { return cast(float)DRand(); }
float  FRand(float mn, float mx)   { return cast(float)DRand(mn, mx); }
	
unittest
{
	write("pr.rand unittest ... ");
	MersenneTwister mt = new MersenneTwister();
	assert(mt.mti == MersenneTwister.N+1);
	assert(MersenneTwister.Instance.mti != 0);
	float f = FRand(0.5f, 1.5f); assert(f >= 0.5f && f < 1.5f);
	writeln("complete");
}
