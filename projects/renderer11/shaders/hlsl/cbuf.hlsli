//***********************************************
// Renderer
//  Copyright © Rylogic Ltd 2014
//***********************************************
// Hacks to make shader struct declarations compile in C++.
// Allowing DRY declaration of shader types

#ifndef PR_RDR_SHADER_CBUF_HLSL
#define PR_RDR_SHADER_CBUF_HLSL

// Notes:
// - use float4x4 not matrix... they're not the same (don't know why tho)
// - HLSL float4x4 is column major (by default) but pr::m4x4 is row major.
//   Remember to transpose matrices or use 'row_major' before any float4x4's.
// - For efficiency, constant buffers need to be grouped by frequency of update

#ifdef SHADER_BUILD

	#define reg(b) register(b)
	#define voidp uint2

#else

	enum class ERegister
	{ 
		// Used for constant buffer declarations.
		//  e.g. cbuffer MyCBuf :reg(b0)
		b0 = 0, b1, b2, b3, b4, b5, b6, b7, b8, b9, //...

		// Used for texture declarations or shader resource views.
		// e.g. Texture2D<float4> tex[4] :reg(t0)
		t0 = 0, t1, t2, t3, t4, t5, t6, t7, t8, t9, //...

		// Used for sampler declarations
		s0 = 0, s1, s2, s3, //...

		// Used for unordered access views
		u0 = 0, u1, u2, u3, //...
	};
	
	template <ERegister rn> struct Register
	{
		enum { slot = rn };
	};

	using float4x4 = pr::m4x4;
	using float4   = pr::v4;
	using float3   = pr::v3;
	using float2   = pr::v2;
	using int4     = pr::iv4;
	using voidp    = void const*;

	#define cbuffer struct
	#define reg(b) Register<ERegister::b>
	#define row_major

#endif

#endif
