//*********************************************
// HLSL
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#ifndef PR_HLSL_INTEROP_HLSLI
#define PR_HLSL_INTEROP_HLSLI

// Shared types for HLSL shaders and C++ code.
// This file is included from both HLSL and C++ source.
// In shader builds, 'SHADER_BUILD' is defined, and the types are set up for HLSL.

// Notes:
// - use float4x4 not matrix... they're not the same (don't know why tho)
// - HLSL float4x4 is column major (by default) but pr::m4x4 is row major.
//   Remember to transpose matrices or use 'row_major' before any float4x4's.
// - For efficiency, constant buffers need to be grouped by frequency of update

#ifdef SHADER_BUILD

	#define reg(reg_number, space) register(reg_number)
	#define semantic(semantic_name) :semantic_name
	#define voidp uint2

#else
	
	// Note:
	//   This error: "error X3000: syntax error: unexpected token 'enum'"
	//   means you have an hlsl file somewhere that hasn't been set to 'Custom Build Tool'.
	//   It will be using the HLSL Compiler build type, which doesn't know about the 'SHADER_BUILD' define

	using float4x4 = pr::m4x4;
	using float4   = pr::v4;
	using float3   = pr::v3;
	using float2   = pr::v2;
	using int2     = pr::iv2;
	using int3     = pr::iv3;
	using int4     = pr::iv4;
	using uint4    = pr::iv4;
	using uint3    = pr::iv3;
	using uint     = uint32_t;
	using voidp    = void const*;

	#define cbuffer struct
	#define reg(reg_number, space) ShaderReg<decltype(reg_number), reg_number, space>
	#define semantic(semantic_name)
	#define row_major

#endif

#endif
