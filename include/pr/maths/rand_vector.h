//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/ivector4.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix2x2.h"
#include "pr/maths/matrix3x3.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/quaternion.h"
#include "pr/maths/rand.h"

namespace pr
{
	// Create a random vector with unit length
	template <typename = void> inline v2 Random2N(Rand& rnd)
	{
		for (;;)
		{
			auto v = v2(rnd.fltc(0.0f,1.0f), rnd.fltc(0.0f,1.0f));
			auto len = Length2Sq(v);
			if (len > 0.0f && len <= 1.0f)
				return Normalise2(v);
		}
	}
	template <typename = void> inline v3 Random2N(Rand& rnd, float z_)
	{
		return v3(Random2N(rnd), z_);
	}
	template <typename = void> inline v4 Random2N(Rand& rnd, float z_, float w_)
	{
		return v4(Random2N(rnd), z_, w_);
	}

	// Create a random vector with length on interval [min_length, max_length]
	template <typename = void> inline v2 Random2(Rand& rnd, float min_length, float max_length)
	{
		return rnd.fltr(min_length, max_length) * Random2N(rnd);
	}
	template <typename = void> inline v3 Random2(Rand& rnd, float min_length, float max_length, float z_)
	{
		return v3(Random2(rnd, min_length, max_length), z_);
	}
	template <typename = void> inline v4 Random2(Rand& rnd, float min_length, float max_length, float z_, float w_)
	{
		return v4(Random2(rnd, min_length, max_length), z_, w_);
	}

	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename = void> inline v2 Random2(Rand& rnd, v2 const& vmin, v2 const& vmax)
	{
		return v2(rnd.fltr(vmin.x, vmax.x), rnd.fltr(vmin.y, vmax.y));
	}
	template <typename = void> inline v3 Random2(Rand& rnd, v3 const& vmin, v3 const& vmax, float z_)
	{
		return v3(Random2(rnd, vmin.xy, vmax.xy), z_);
	}
	template <typename = void> inline v4 Random2(Rand& rnd, v4 const& vmin, v4 const& vmax, float z_, float w_)
	{
		return v4(Random2(rnd, vmin.xy, vmax.xy), z_, w_);
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename = void> inline v2 Random2(Rand& rnd, v2 const& centre, float radius)
	{
		return Random2(rnd, 0.0f, radius) + centre;
	}
	template <typename = void> inline v3 Random2(Rand& rnd, v3 const& centre, float radius, float z_)
	{
		return v3(Random2(rnd, centre.xy, radius), z_);
	}
	template <typename = void> inline v4 Random2(Rand& rnd, v4 const& centre, float radius, float z_, float w_)
	{
		return v4(Random2(rnd, centre.xy, radius), z_, w_);
	}

	// Create a random vector with unit length
	template <typename = void> inline v3 Random3N(Rand& rnd)
	{
		v3 v; float len;
		do
		{
			v = v3(rnd.fltc(0.0f,1.0f), rnd.fltc(0.0f,1.0f), rnd.fltc(0.0f,1.0f));
			len = Length3Sq(v);
		}
		while (len > 1.0f || len == 0.0f);
		return v /= Sqrt(len);
	}
	template <typename = void> inline v4 Random3N(Rand& rnd, float w_)
	{
		return v4(Random3N(rnd), w_);
	}
	
	// Create a random vector with length on interval [min_length, max_length]
	template <typename = void> inline v3 Random3(Rand& rnd, float min_length, float max_length)
	{
		return rnd.fltr(min_length, max_length) * Random3N(rnd);
	}
	template <typename = void> inline v4 Random3(Rand& rnd, float min_length, float max_length, float w_)
	{
		return v4(Random3(rnd, min_length, max_length), w_);
	}

	// Create a random vector with components on interval ['vmin', 'vmax']
	template <typename = void> inline v3 Random3(Rand& rnd, v3 const& vmin, v3 const& vmax)
	{
		return v3(rnd.fltr(vmin.x,vmax.x), rnd.fltr(vmin.y,vmax.y), rnd.fltr(vmin.z,vmax.z));
	}
	template <typename = void> inline v4 Random3(Rand& rnd, v4 const& vmin, v4 const& vmax, float w_)
	{
		return v4(Random3(rnd, vmin.xyz, vmax.xyz), w_);
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename = void> inline v3 Random3(Rand& rnd, v3 const& centre, float radius)
	{
		return Random3(rnd, 0.0f, radius) + centre;
	}
	template <typename = void> inline v4 Random3(Rand& rnd, v4 const& centre, float radius, float w_)
	{
		return v4(Random3(rnd, centre.xyz, radius), w_);
	}

	// Create a random vector with unit length
	template <typename = void> inline v4 Random4N(Rand& rnd)
	{
		v4 v; float len;
		do
		{
			v = v4(rnd.fltc(0.0f, 1.0f), rnd.fltc(0.0f, 1.0f), rnd.fltc(0.0f, 1.0f), rnd.fltc(0.0f, 1.0f));
			len = Length4Sq(v);
		}
		while (len > 1.0f || len == 0.0f);
		return v /= Sqrt(len);
	}

	// Create a random vector with length on interval [min_length, max_length]
	template <typename = void> inline v4 Random4(Rand& rnd, float min_length, float max_length)
	{
		return rnd.fltr(min_length, max_length) * Random4N(rnd);
	}

	// Create a random vector with components on interval [vmin, vmax]
	template <typename = void> inline v4 Random4(Rand& rnd, v4 const& vmin, v4 const& vmax)
	{
		return v4(rnd.fltr(vmin.x,vmax.x), rnd.fltr(vmin.y,vmax.y), rnd.fltr(vmin.z,vmax.z), rnd.fltr(vmin.w,vmax.w));
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <typename = void> inline v4 Random4(Rand& rnd, v4 const& centre, float radius)
	{
		return Random4(rnd, 0, radius) + centre;
	}

	// Create a random 2D rotation matrix
	template <typename = void> inline m2x2 Random2x2(Rand& rnd, float min_angle, float max_angle)
	{
		return m2x2::Rotation(rnd.fltr(min_angle, max_angle));
	}
	template <typename = void> inline m2x2 Random2x2(Rand& rnd)
	{
		return Random2x2(rnd, 0.0f, maths::tau);
	}

	// Create a random 3D rotation matrix
	template <typename = void> inline m3x4 Random3x4(Rand& rnd, v4 const& axis, float min_angle, float max_angle)
	{
		return m3x4::Rotation(axis, rnd.fltr(min_angle, max_angle));
	}
	template <typename = void> inline m3x4 Random3x4(Rand& rnd)
	{
		return Random3x4(rnd, Random3N(rnd, 0.0f), 0.0f, maths::tau);
	}

	// Create a random 4x4 matrix
	template <typename = void> inline m4x4 Random4x4(Rand& rnd, float min_value, float max_value)
	{
		m4x4 m = {};
		m.x = v4(rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value));
		m.y = v4(rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value));
		m.z = v4(rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value));
		m.w = v4(rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value), rnd.fltr(min_value, max_value));
		return m;
	}

	// Create a random affine transform matrix
	template <typename = void> inline m4x4 Random4x4(Rand& rnd, v4 const& axis, float min_angle, float max_angle, v4 const& position)
	{
		return m4x4::Rotation(axis, rnd.fltr(min_angle, max_angle), position);
	}
	template <typename = void> inline m4x4 Random4x4(Rand& rnd, float min_angle, float max_angle, v4 const& position)
	{
		return Random4x4(rnd, Random3N(rnd, 0.0f), min_angle, max_angle, position);
	}
	template <typename = void> inline m4x4 Random4x4(Rand& rnd, v4 const& axis, float min_angle, float max_angle, v4 const& centre, float radius)
	{
		return Random4x4(rnd, axis, min_angle, max_angle, centre + Random3(rnd, 0.0f, radius, 0.0f));
	}
	template <typename = void> inline m4x4 Random4x4(Rand& rnd, float min_angle, float max_angle, v4 const& centre, float radius)
	{
		return Random4x4(rnd, Random3N(rnd, 0.0f), min_angle, max_angle, centre, radius);
	}
	template <typename = void> inline m4x4 Random4x4(Rand& rnd, v4 const& centre, float radius)
	{
		return Random4x4(rnd, Random3N(rnd, 0.0f), 0.0f, maths::tau, centre, radius);
	}

	// Create a random quaternion rotation
	template <typename = void> inline quat RandomQ(Rand& rnd, v4 const& axis, float min_angle, float max_angle)
	{
		return quat(axis, rnd.fltr(min_angle, max_angle));
	}
	template <typename = void> inline quat RandomQ(Rand& rnd, float min_angle, float max_angle)
	{
		return quat(Random3N(rnd, 0.0f), rnd.fltr(min_angle, max_angle));
	}
	template <typename = void> inline quat RandomQ(Rand& rnd)
	{
		return quat(Random3N(rnd, 0.0f), rnd.fltr(0.0f, maths::tau));
	}
}
