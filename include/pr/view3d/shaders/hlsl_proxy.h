//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Hacks to allow hlsl code to build under C++

#pragma once

#include "pr/maths/maths.h"
#include "pr/view3d/renderer.h"

namespace pr
{
	namespace hlsl
	{
		#define uniform
		#define row_major
		#define line
		#define inout
		
		using float2   = v2;
		using float3   = v3;
		using float4   = v4;
		using int4     = iv4;
		using float4x4 = m4x4;
	}
	namespace hlsl
	{
		struct SamplerState
		{
		};

		template <typename Format> struct Texture2D
		{
			pr::rdr::Image m_img;
			virtual Format Sample(SamplerState const&, float2 const& uv)
			{
				int u = int(uv.x * m_img.m_dim.x);
				int v = int(uv.y * m_img.m_dim.y);
				return ReadPixel(u, v);
			}
			virtual Format ReadPixel(int u, int v)
			{
				auto* px = static_cast<Format const*>(m_img.m_pixels);
				if (px == nullptr) return Format();
				return px[v * m_img.m_pitch.x + u];
			}
		};

		template <typename T> struct TriangleStream
		{
			virtual void Append(T const&) {}
			virtual void RestartStrip() {}
		};

		// Shader intrinsic functions
		bool clip(float x)
		{
			return x < 0.0f;
		}
		float step(float lo, float hi)
		{
			return Step(lo,hi);
		}
		float sign(float x)
		{
			return x < 0.0f ? -1.0f : x > 0.0f ? 1.0f : 0.0f;
		}
		float smoothstep(float lo, float hi, float t)
		{
			return SmoothStep(lo,hi,t);
		}
		float saturate(float x)
		{
			return Clamp(x,0.0f,1.0f);
		}

		float2 normalize(float2 const& v)
		{
			return Normalise2(static_cast<v2 const&>(v));
		}

		float length(float4 const& v)
		{
			return Length4(static_cast<v4 const&>(v));
		}
		float4 lerp(float4 const& a, float4 const& b, float t)
		{
			return (1-t)*a + t*b;
		}
		float4 min(float4 const& a, float4 const& b)
		{
			return Min(a,b);
		}
		float4 normalize(float4 const& v)
		{
			return Normalise4(static_cast<v4 const&>(v));
		}
		float dot(float4 const& a, float4 const& b)
		{
			return Dot4(a,b);
		}
		float4 mul(float4 const& v, m4x4 const& m)
		{
			return m * static_cast<v4 const&>(v);
		}
		float4 step(float4 const& lo, float4 const& hi)
		{
			return float4(
				hi.x >= lo.x ? 1.0f : 0.0f,
				hi.y >= lo.y ? 1.0f : 0.0f,
				hi.z >= lo.z ? 1.0f : 0.0f,
				hi.w >= lo.w ? 1.0f : 0.0f);
		}

		struct SLight
		{
			int4   m_info;         // x = light type (0:ambient, 1:directional, 2:point, 3:spot), yzw = unused
			float4 m_ws_direction; // The direction of the global light source
			float4 m_ws_position;  // The position of the global light source
			float4 m_ambient;      // The colour of the ambient light
			float4 m_colour;       // The colour of the directional light
			float4 m_specular;     // The colour of the specular light. alpha channel is specular power
			float4 m_range;        // x = inner angle, y = outer angle, z = range, w = falloff

			SLight(pr::rdr::Light const& light)
				:m_info((int)light.m_type,0,0,0)
				,m_ws_direction(light.m_direction)
				,m_ws_position(light.m_position)
				,m_ambient(To<Colour>(light.m_ambient).rgba)
				,m_colour(To<Colour>(light.m_diffuse).rgba)
				,m_specular(v4(To<Colour>(light.m_specular).rgb, light.m_specular_power))
				,m_range(light.m_inner_angle, light.m_outer_angle, light.m_range, light.m_falloff)
			{}
		};

		#undef uniform
		#undef row_major
		#undef line
		#undef inout
	}
}
