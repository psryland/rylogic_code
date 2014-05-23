//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Hacks to allow hlsl code to build under C++

#pragma once

#include "pr/maths/maths.h"

namespace pr
{
	namespace hlsl
	{
		#define uniform const
		#define row_major
		#define line
		#define inout

		#define SV_Position //:SV_Position
		#define Position1   //:Position1
		#define Normal      //:Normal
		#define Color0      //:Color0
		#define TexCoord0   //:TexCoord0
		
		struct float2 :pr::v2
		{
			float2() {}
			float2(pr::v2 const& x) :pr::v2(x) {}
			float2(float x, float y) :pr::v2(pr::v2::make(x,y)) {}
		};
		struct float4 :pr::v4
		{
			float4() {}
			float4(pr::v4 const& x) :pr::v4(x) {}
			float4(float x, float y, float z, float w) :pr::v4(pr::v4::make(x,y,z,w)) {}
		};
		struct int4 :pr::iv4
		{
			int4() {}
			int4(pr::iv4 x) :pr::iv4(x) {}
			int4(int x, int y, int z, int w) :pr::iv4(pr::iv4::make(x,y,z,w)) {}
		};
		struct float4x4 :pr::m4x4
		{
		};

		struct SamplerState
		{
		};

		template <typename Format> struct Texture2D
		{
			virtual Format Sample(SamplerState const&, Format const& uv) = 0;
		};

		template <typename T> struct TriangleStream
		{
			virtual void Append(T const&) {}
			virtual void RestartStrip() {}
		};

		// Shader intrinic functions
		bool clip(float x)
		{
			return x < 0.0f;
		};
		float step(float lo, float hi)
		{
			return hi >= lo ? 1.0f : 0.0f;
		};
		float saturate(float x)
		{
			return Clamp(x,0.0f,1.0f);
		}

		float2 normalize(float2 const& v)
		{
			return Normalise2(static_cast<pr::v2 const&>(v));
		}

		float length(float4 const& v)
		{
			return Length4(static_cast<pr::v4 const&>(v));
		}
		float4 lerp(float4 const& a, float4 const& b, float t)
		{
			return (1-t)*a + t*b;
		}
		float4 normalize(float4 const& v)
		{
			return Normalise4(static_cast<pr::v4 const&>(v));
		}
		float4 mul(float4 const& v, m4x4 const& m)
		{
			return m * v;
		}
		float4 step(float4 const& lo, float4 const& hi)
		{
			return float4::make(
				hi.x >= lo.x ? 1.0f : 0.0f,
				hi.y >= lo.y ? 1.0f : 0.0f,
				hi.z >= lo.z ? 1.0f : 0.0f,
				hi.w >= lo.w ? 1.0f : 0.0f);
		}
	}
}
