//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include <random>
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/ivector4.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix2x2.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/quaternion.h"

namespace pr
{
	// A global random generator. Non-deterministically seeded and not re-seed-able.
	inline std::default_random_engine& g_rng()
	{
		static std::random_device s_rd;
		static std::default_random_engine s_rng(s_rd());
		return s_rng;
	}

	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename Rng = std::default_random_engine> inline float Random1(Rng& rng, float vmin, float vmax)
	{
		std::uniform_real_distribution<float> dist(vmin, vmax);
		return dist(rng);
	}
	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename Rng = std::default_random_engine> inline float Random1C(Rng& rng, float centre, float radius)
	{
		return Random1(rng, centre - radius, centre + radius);
	}

	// Create a random vector with unit length
	template <typename Rng = std::default_random_engine> inline v2 Random2N(Rng& rng)
	{
		std::uniform_real_distribution<float> dist(-1.0f, 1.0f);
		for (;;)
		{
			auto x = dist(rng);
			auto y = dist(rng);
			auto v = v2(x, y);
			auto len = Length2Sq(v);
			if (len > 0.01f && len <= 1.0f)
				return Normalise(v);
		}
	}
	// Create a random vector with unit length
	template <typename Rng = std::default_random_engine> inline v3 Random2N(Rng& rng, float z_)
	{
		return v3(Random2N(rng), z_);
	}
	// Create a random vector with unit length
	template <typename Rng = std::default_random_engine> inline v4 Random2N(Rng& rng, float z_, float w_)
	{
		return v4(Random2N(rng), z_, w_);
	}

	// Create a random vector with length on interval [min_length, max_length]
	template <typename Rng = std::default_random_engine> inline v2 Random2(Rng& rng, float min_length, float max_length)
	{
		std::uniform_real_distribution<float> dist(min_length, max_length);
		return dist(rng) * Random2N(rng);
	}
	// Create a random vector with length on interval [min_length, max_length]
	template <typename Rng = std::default_random_engine> inline v3 Random2(Rng& rng, float min_length, float max_length, float z_)
	{
		return v3(Random2(rng, min_length, max_length), z_);
	}
	// Create a random vector with length on interval [min_length, max_length]
	template <typename Rng = std::default_random_engine> inline v4 Random2(Rng& rng, float min_length, float max_length, float z_, float w_)
	{
		return v4(Random2(rng, min_length, max_length), z_, w_);
	}

	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename Rng = std::default_random_engine> inline v2 Random2(Rng& rng, v2 const& vmin, v2 const& vmax)
	{
		std::uniform_real_distribution<float> dist_x(vmin.x, vmax.x);
		std::uniform_real_distribution<float> dist_y(vmin.y, vmax.y);
		return v2(dist_x(rng), dist_y(rng));
	}
	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename Rng = std::default_random_engine> inline v3 Random2(Rng& rng, v3 const& vmin, v3 const& vmax, float z_)
	{
		return v3(Random2(rng, vmin.xy, vmax.xy), z_);
	}
	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename Rng = std::default_random_engine> inline v4 Random2(Rng& rng, v4 const& vmin, v4 const& vmax, float z_, float w_)
	{
		return v4(Random2(rng, vmin.xy, vmax.xy), z_, w_);
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename Rng = std::default_random_engine> inline v2 Random2(Rng& rng, v2 const& centre, float radius)
	{
		return Random2(rng, 0.0f, radius) + centre;
	}
	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename Rng = std::default_random_engine> inline v3 Random2(Rng& rng, v3 const& centre, float radius, float z_)
	{
		return v3(Random2(rng, centre.xy, radius), z_);
	}
	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename Rng = std::default_random_engine> inline v4 Random2(Rng& rng, v4 const& centre, float radius, float z_, float w_)
	{
		return v4(Random2(rng, centre.xy, radius), z_, w_);
	}

	// Create a random vector with unit length
	template <typename Rng = std::default_random_engine> inline v3 Random3N(Rng& rng)
	{
		std::uniform_real_distribution<float> dist(-1.0f, 1.0f);
		for (;;)
		{
			auto x = dist(rng);
			auto y = dist(rng);
			auto z = dist(rng);
			auto v = v3(x, y, z);
			auto len = LengthSq(v);
			if (len > 0.01f && len <= 1.0f)
				return v /= Sqrt(len);
		}
	}
	// Create a random vector with unit length
	template <typename Rng = std::default_random_engine> inline v4 Random3N(Rng& rng, float w_)
	{
		return v4(Random3N(rng), w_);
	}
	
	// Create a random vector with length on interval [min_length, max_length]
	template <typename Rng = std::default_random_engine> inline v3 Random3(Rng& rng, float min_length, float max_length)
	{
		std::uniform_real_distribution<float> dist(min_length, max_length);
		return dist(rng) * Random3N(rng);
	}
	// Create a random vector with length on interval [min_length, max_length]
	template <typename Rng = std::default_random_engine> inline v4 Random3(Rng& rng, float min_length, float max_length, float w_)
	{
		return v4(Random3(rng, min_length, max_length), w_);
	}

	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename Rng = std::default_random_engine> inline v3 Random3(Rng& rng, v3 const& vmin, v3 const& vmax)
	{
		std::uniform_real_distribution<float> dist_x(vmin.x, vmax.x);
		std::uniform_real_distribution<float> dist_y(vmin.y, vmax.y);
		std::uniform_real_distribution<float> dist_z(vmin.z, vmax.z);
		return v3(dist_x(rng), dist_y(rng), dist_z(rng));
	}
	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename Rng = std::default_random_engine> inline v4 Random3(Rng& rng, v4 const& vmin, v4 const& vmax, float w_)
	{
		return v4(Random3(rng, vmin.xyz, vmax.xyz), w_);
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename Rng = std::default_random_engine> inline v3 Random3(Rng& rng, v3 const& centre, float radius)
	{
		return Random3(rng, 0.0f, radius) + centre;
	}
	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename Rng = std::default_random_engine> inline v4 Random3(Rng& rng, v4 const& centre, float radius, float w_)
	{
		return v4(Random3(rng, centre.xyz, radius), w_);
	}

	// Create a random vector with unit length
	template <typename Rng = std::default_random_engine> inline v4 Random4N(Rng& rng)
	{
		std::uniform_real_distribution<float> dist(-1.0f, 1.0f);
		for (;;)
		{
			auto x = dist(rng);
			auto y = dist(rng);
			auto z = dist(rng);
			auto w = dist(rng);
			auto v = v4(x, y, z, w);
			auto len = LengthSq(v);
			if (len >= 0.01f && len <= 1.0f)
				return v /= Sqrt(len);
		}
	}

	// Create a random vector with length on interval [min_length, max_length]
	template <typename Rng = std::default_random_engine> inline v4 Random4(Rng& rng, float min_length, float max_length)
	{
		std::uniform_real_distribution<float> dist(min_length, max_length);
		return dist(rng) * Random4N(rng);
	}

	// Create a random vector with components on interval '[vmin, vmax]'
	template <typename Rng = std::default_random_engine> inline v4 Random4(Rng& rng, v4 const& vmin, v4 const& vmax)
	{
		std::uniform_real_distribution<float> dist_x(vmin.x, vmax.x);
		std::uniform_real_distribution<float> dist_y(vmin.y, vmax.y);
		std::uniform_real_distribution<float> dist_z(vmin.z, vmax.z);
		std::uniform_real_distribution<float> dist_w(vmin.w, vmax.w);
		return v4(dist_x(rng), dist_y(rng), dist_z(rng), dist_w(rng));
	}

	// Create a random vector centred on 'centre' with radius 'radius'.
	template <typename Rng = std::default_random_engine> inline v4 Random4(Rng& rng, v4 const& centre, float radius)
	{
		return Random4(rng, 0, radius) + centre;
	}

	// Create a random 2D rotation matrix
	template <typename Rng = std::default_random_engine> inline m2x2 Random2x2(Rng& rng, float min_angle, float max_angle)
	{
		std::uniform_real_distribution<float> dist(min_angle, max_angle);
		return m2x2::Rotation(dist(rng));
	}
	// Create a random 2D rotation matrix
	template <typename Rng = std::default_random_engine> inline m2x2 Random2x2(Rng& rng)
	{
		return Random2x2(rng, 0.0f, maths::tau);
	}

	// Create a random 3D matrix
	template <typename Rng = std::default_random_engine> inline m3x4 Random3x4(Rng& rng, float min_value, float max_value)
	{
		std::uniform_real_distribution<float> dist(min_value, max_value);
		m3x4 m = {};
		m.x = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		m.y = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		m.z = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		return m;
	}

	// Create a random 3D rotation matrix
	template <typename Rng = std::default_random_engine> inline m3x4 Random3x4(Rng& rng, v4 const& axis, float min_angle, float max_angle)
	{
		std::uniform_real_distribution<float> dist(min_angle, max_angle);
		return m3x4::Rotation(axis, dist(rng));
	}
	// Create a random 3D rotation matrix
	template <typename Rng = std::default_random_engine> inline m3x4 Random3x4(Rng& rng)
	{
		return Random3x4(rng, Random3N(rng, 0.0f), 0.0f, float(maths::tau));
	}

	// Create a random 4x4 matrix
	template <typename Rng = std::default_random_engine> inline m4x4 Random4x4(Rng& rng, float min_value, float max_value)
	{
		std::uniform_real_distribution<float> dist(min_value, max_value);
		m4x4 m = {};
		m.x = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		m.y = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		m.z = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		m.w = v4(dist(rng), dist(rng), dist(rng), dist(rng));
		return m;
	}

	// Create a random affine transform matrix
	template <typename Rng = std::default_random_engine> inline m4x4 Random4x4(Rng& rng, v4 const& axis, float min_angle, float max_angle, v4 const& position)
	{
		std::uniform_real_distribution<float> dist(min_angle, max_angle);
		return m4x4::Transform(axis, dist(rng), position);
	}
	// Create a random affine transform matrix
	template <typename Rng = std::default_random_engine> inline m4x4 Random4x4(Rng& rng, float min_angle, float max_angle, v4 const& position)
	{
		return Random4x4(rng, Random3N(rng, 0.0f), min_angle, max_angle, position);
	}
	// Create a random affine transform matrix
	template <typename Rng = std::default_random_engine> inline m4x4 Random4x4(Rng& rng, v4 const& axis, float min_angle, float max_angle, v4 const& centre, float radius)
	{
		return Random4x4(rng, axis, min_angle, max_angle, centre + Random3(rng, 0.0f, radius, 0.0f));
	}
	// Create a random affine transform matrix
	template <typename Rng = std::default_random_engine> inline m4x4 Random4x4(Rng& rng, float min_angle, float max_angle, v4 const& centre, float radius)
	{
		return Random4x4(rng, Random3N(rng, 0.0f), min_angle, max_angle, centre, radius);
	}
	// Create a random affine transform matrix
	template <typename Rng = std::default_random_engine> inline m4x4 Random4x4(Rng& rng, v4 const& centre, float radius)
	{
		return Random4x4(rng, Random3N(rng, 0.0f), 0.0f, float(maths::tau), centre, radius);
	}

	// Create a random quaternion rotation
	template <typename Rng = std::default_random_engine> inline quat RandomQ(Rng& rng, v4 const& axis, float min_angle, float max_angle)
	{
		std::uniform_real_distribution<float> dist(min_angle, max_angle);
		return quat(axis, dist(rng));
	}
	// Create a random quaternion rotation
	template <typename Rng = std::default_random_engine> inline quat RandomQ(Rng& rng, float min_angle, float max_angle)
	{
		std::uniform_real_distribution<float> dist(min_angle, max_angle);
		return quat(Random3N(rng, 0.0f), dist(rng));
	}
	// Create a random quaternion rotation
	template <typename Rng = std::default_random_engine> inline quat RandomQ(Rng& rng)
	{
		std::uniform_real_distribution<float> dist(0.0f, float(maths::tau));
		return quat(Random3N(rng, 0.0f), dist(rng));
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(RandVectorTests)
	{
		{// Random4
			auto radius = 10;
			auto centre = v4{1,1,1,1};
			auto prev = v4{};
			for (int i = 0; i != 100; ++i)
			{
				auto v = Random4(g_rng(), centre, 10);
				PR_CHECK(v != prev, true);
				PR_CHECK(Length(v - centre) < radius, true);
			}
		}
	}
}
#endif