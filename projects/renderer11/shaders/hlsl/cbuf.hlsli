//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Hacks to make cbuffer definitions compile in C++
#ifndef PR_RDR_SHADER_CBUF_HLSL
#define PR_RDR_SHADER_CBUF_HLSL

// Notes:
// - use float4x4 not matrix... they're not the same (don't know why tho)
// - hlsl float4x4 is column major (by default) but pr::m4x4 is row major.
//   Rememeber to transpose matrices
// - For efficiency, constant buffers need to be grouped by frequency of update

#ifdef SHADER_BUILD

	#define cbuf_bank(b) register(b)

#else

	enum class EBank { b0, b1, b2, b3, b4, b5 };
	template <EBank bn> struct Bank
	{
		enum { slot = bn };
	};

	typedef pr::m4x4 float4x4;
	typedef pr::v4   float4;
	typedef pr::v2   float2;
	typedef pr::iv4  int4;

	#define cbuffer struct
	#define cbuf_bank(b) Bank<EBank::b>
	#define row_major

#endif

#endif
