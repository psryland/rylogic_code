//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Hacks to allow hlsl code to build under C++

#pragma once

#include "pr/maths/maths.h"
#include "pr/renderer11/renderer.h"

namespace pr
{
	namespace hlsl
	{
		#define uniform
		#define row_major
		#define line
		#define inout
		
		struct float2 :pr::v2
		{
			float2() :pr::v2() {}
			float2(pr::v2 const& x) :pr::v2(x) {}
			float2(float x, float y) :pr::v2(pr::v2::make(x,y)) {}
		};
		inline float GetX(float2 const& v) { return v.x; }
		inline float GetY(float2 const& v) { return v.y; }

		struct float3 :pr::v3
		{
			float3() :pr::v3() {}
			float3(pr::v3 const& x) :pr::v3(x) {}
			float3(float2 const& xy, float z) :pr::v3(pr::v3::make(xy,z)) {}
			float3(float x, float y, float z) :pr::v3(pr::v3::make(x,y,z)) {}
		};
		inline float GetX(float3 const& v) { return v.x; }
		inline float GetY(float3 const& v) { return v.y; }
		inline float GetZ(float3 const& v) { return v.z; }

		struct float4 :pr::v4
		{
			float4() :pr::v4() {}
			float4(pr::v4 const& x) :pr::v4(x) {}
			float4(float2 const& xy, float z, float w) :pr::v4(pr::v4::make(xy,z,w)) {}
			float4(float x, float y, float z, float w) :pr::v4(pr::v4::make(x,y,z,w)) {}
		};
		inline float GetX(float4 const& v) { return v.x; }
		inline float GetY(float4 const& v) { return v.y; }
		inline float GetZ(float4 const& v) { return v.z; }
		inline float GetW(float4 const& v) { return v.w; }

		struct int4 :pr::iv4
		{
			int4() :pr::iv4() {}
			int4(pr::iv4 const& x) :pr::iv4(x) {}
			int4(int x, int y, int z, int w) :pr::iv4(pr::iv4::make(x,y,z,w)) {}
		};
		struct float4x4 :pr::m4x4
		{
			float4x4() :pr::m4x4() {}
			float4x4(pr::m4x4 const& x) :pr::m4x4(x) {}
			float4x4(float4 const& x, float4 const& y, float4 const& z, float4 const& w)
				:pr::m4x4(pr::m4x4::make(x,y,z,w))
			{}
		};
		inline float4 GetX(float4x4 const& v) { return v.x; }
		inline float4 GetY(float4x4 const& v) { return v.y; }
		inline float4 GetZ(float4x4 const& v) { return v.z; }
		inline float4 GetW(float4x4 const& v) { return v.w; }

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

		// Shader intrinic functions
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
			return pr::SmoothStep(lo,hi,t);
		}
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
		float4 min(float4 const& a, float4 const& b)
		{
			return Min(a,b);
		}
		float4 normalize(float4 const& v)
		{
			return Normalise4(static_cast<pr::v4 const&>(v));
		}
		float dot(float4 const& a, float4 const& b)
		{
			return Dot4(a,b);
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

		struct SLight
		{
			int4   m_info;         // x = light type (0:ambient, 1:directional, 2:point, 3:spot), yzw = unused
			float4 m_ws_direction; // The direction of the global light source
			float4 m_ws_position;  // The position of the global light source
			float4 m_ambient;      // The colour of the ambient light
			float4 m_colour;       // The colour of the directional light
			float4 m_specular;     // The colour of the specular light. alpha channel is specular power
			float4 m_range;        // x = range, y = falloff, z = inner cos angle, w = outer cos angle

			SLight(pr::rdr::Light const& light)
				:m_info((int)light.m_type,0,0,0)
				,m_ws_direction(light.m_direction)
				,m_ws_position(light.m_position)
				,m_ambient(To<Colour>(light.m_ambient))
				,m_colour(To<Colour>(light.m_diffuse))
				,m_specular(pr::v4::make(To<Colour>(light.m_specular).rgb, light.m_specular_power))
				,m_range(light.m_range, light.m_falloff, light.m_inner_cos_angle, light.m_outer_cos_angle)
			{}
		};

		#undef uniform
		#undef row_major
		#undef line
		#undef inout
	}
}
