//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR_RAND_H
#define PR_MATHS_VECTOR_RAND_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/rand.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/ivector4.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix2x2.h"
#include "pr/maths/matrix3x3.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/quaternion.h"

namespace pr
{
	inline v2 Random2N(Rnd& rnd)                                                        { v2 v; float len; do { v = v2::make(rnd.f32(-1.0f, 1.0f), rnd.f32(-1.0f, 1.0f)); len = Length2Sq(v); } while (len > 1.0f || len == 0.0f); return Normalise2(v); }
	inline v3 Random2N(Rnd& rnd, float z_)                                              { return v3::make(Random2N(rnd), z_); }
	inline v4 Random2N(Rnd& rnd, float z_, float w_)                                    { return v4::make(Random2N(rnd), z_, w_); }
	inline v2 Random2N()                                                                { return Random2N(rand::Rand()); }
	inline v3 Random2N(          float z_)                                              { return v3::make(Random2N(), z_); }
	inline v4 Random2N(          float z_, float w_)                                    { return v4::make(Random2N(), z_, w_); }
	inline v2 Random2(Rnd& rnd, float min_length, float max_length)                     { return rnd.f32(min_length, max_length) * Random2N(rnd); }
	inline v3 Random2(Rnd& rnd, float min_length, float max_length, float z_)           { return v3::make(Random2(rnd, min_length, max_length), z_); }
	inline v4 Random2(Rnd& rnd, float min_length, float max_length, float z_, float w_) { return v4::make(Random2(rnd, min_length, max_length), z_, w_); }
	inline v2 Random2(          float min_length, float max_length)                     { return Random2(rand::Rand(), min_length, max_length); }
	inline v3 Random2(          float min_length, float max_length, float z_)           { return v3::make(Random2(min_length, max_length), z_); }
	inline v4 Random2(          float min_length, float max_length, float z_, float w_) { return v4::make(Random2(min_length, max_length), z_, w_); }
	inline v2 Random2(Rnd& rnd, v2 const& vmin, v2 const& vmax)                         { return v2::make(rnd.f32(vmin.x, vmax.x), rnd.f32(vmin.y, vmax.y)); }
	inline v3 Random2(Rnd& rnd, v3 const& vmin, v3 const& vmax, float z_)               { return v3::make(Random2(rnd, vmin.xy(), vmax.xy()), z_); }
	inline v4 Random2(Rnd& rnd, v4 const& vmin, v4 const& vmax, float z_, float w_)     { return v4::make(Random2(rnd, vmin.xy(), vmax.xy()), z_, w_); }
	inline v2 Random2(          v2 const& vmin, v2 const& vmax)                         { return Random2(rand::Rand(), vmin, vmax); }
	inline v3 Random2(          v3 const& vmin, v3 const& vmax, float z_)               { return v3::make(Random2(vmin.xy(), vmax.xy()), z_); }
	inline v4 Random2(          v4 const& vmin, v4 const& vmax, float z_, float w_)     { return v4::make(Random2(vmin.xy(), vmax.xy()), z_, w_); }
	inline v2 Random2(Rnd& rnd, v2 const& centre, float radius)                         { return Random2(rnd, 0.0f, radius) + centre; }
	inline v3 Random2(Rnd& rnd, v3 const& centre, float radius, float z_)               { return v3::make(Random2(rnd, centre.xy(), radius), z_); }
	inline v4 Random2(Rnd& rnd, v4 const& centre, float radius, float z_, float w_)     { return v4::make(Random2(rnd, centre.xy(), radius), z_, w_); }
	inline v2 Random2(          v2 const& centre, float radius)                         { return Random2(rand::Rand(), centre, radius); }
	inline v3 Random2(          v3 const& centre, float radius, float z_)               { return v3::make(Random2(centre.xy(), radius), z_); }
	inline v4 Random2(          v4 const& centre, float radius, float z_, float w_)     { return v4::make(Random2(centre.xy(), radius), z_, w_); }
	
	inline v3 Random3N(Rnd& rnd)
	{
		v3 v; float len;
		do
		{
			v = v3::make(rnd.f32(-1.0f, 1.0f), rnd.f32(-1.0f, 1.0f), rnd.f32(-1.0f, 1.0f));
			len = Length3Sq(v);
		}
		while (len > 1.0f || len == 0.0f);
		return v /= Sqrt(len);
	}
	inline v4 Random3N(Rnd& rnd, float w_)                                              { return v4::make(Random3N(rnd), w_); }
	inline v3 Random3N()                                                                { return Random3N(rand::Rand()); }
	inline v4 Random3N(          float w_)                                              { return v4::make(Random3N(), w_); }
	inline v3 Random3(Rnd& rnd, float min_length, float max_length)                     { return rnd.f32(min_length, max_length) * Random3N(rnd); }
	inline v4 Random3(Rnd& rnd, float min_length, float max_length, float w_)           { return v4::make(Random3(rnd, min_length, max_length), w_); }
	inline v3 Random3(          float min_length, float max_length)                     { return Random3(rand::Rand(), min_length, max_length); }
	inline v4 Random3(          float min_length, float max_length, float w_)           { return v4::make(Random3(min_length, max_length), w_); }
	inline v3 Random3(Rnd& rnd, v3 const& vmin, v3 const& vmax)                         { return v3::make(rnd.f32(vmin.x,vmax.x), rnd.f32(vmin.y,vmax.y), rnd.f32(vmin.z,vmax.z)); }
	inline v4 Random3(Rnd& rnd, v4 const& vmin, v4 const& vmax, float w_)               { return v4::make(Random3(rnd, vmin.xyz(), vmax.xyz()), w_); }
	inline v3 Random3(          v3 const& vmin, v3 const& vmax)                         { return Random3(rand::Rand(), vmin, vmax); }
	inline v4 Random3(          v4 const& vmin, v4 const& vmax, float w_)               { return v4::make(Random3(vmin.xyz(), vmax.xyz()), w_); }
	inline v3 Random3(Rnd& rnd, v3 const& centre, float radius)                         { return Random3(rnd, 0.0f, radius) + centre; }
	inline v4 Random3(Rnd& rnd, v4 const& centre, float radius, float w_)               { return v4::make(Random3(rnd, centre.xyz(), radius), w_); }
	inline v3 Random3(          v3 const& centre, float radius)                         { return Random3(rand::Rand(), centre, radius); }
	inline v4 Random3(          v4 const& centre, float radius, float w_)               { return v4::make(Random3(centre.xyz(), radius), w_); }
	
	inline v4 Random4N(Rnd& rnd)
	{
		v4 v; float len;
		do
		{
			v = v4::make(rnd.f32(-1.0f, 1.0f), rnd.f32(-1.0f, 1.0f), rnd.f32(-1.0f, 1.0f), rnd.f32(-1.0f, 1.0f));
			len = Length4Sq(v);
		}
		while (len > 1.0f || len == 0.0f);
		return v /= Sqrt(len);
	}
	inline v4 Random4N()                                                                { return Random4N(rand::Rand()); }
	inline v4 Random4(Rnd& rnd, float min_length, float max_length)                     { return rnd.f32(min_length, max_length) * Random4N(rnd); }
	inline v4 Random4(          float min_length, float max_length)                     { return Random4(rand::Rand(), min_length, max_length); }
	inline v4 Random4(Rnd& rnd, v4 const& vmin, v4 const& vmax)                         { return v4::make(rnd.f32(vmin.x,vmax.x), rnd.f32(vmin.y,vmax.y), rnd.f32(vmin.z,vmax.z), rnd.f32(vmin.w,vmax.w)); }
	inline v4 Random4(          v4 const& vmin, v4 const& vmax)                         { return Random4(rand::Rand(), vmin, vmax); }
	inline v4 Random4(Rnd& rnd, v4 const& centre, float radius)                         { return Random4(rnd, 0, radius) + centre; }
	inline v4 Random4(          v4 const& centre, float radius)                         { return Random4(rand::Rand(), centre, radius); }
	
	inline m2x2 Random2x2(Rnd& rnd, float min_angle, float max_angle)                   { return m2x2::make(rnd.f32(min_angle, max_angle)); }
	inline m2x2 Random2x2(          float min_angle, float max_angle)                   { return Random2x2(rand::Rand(), min_angle, max_angle); }
	inline m2x2 Random2x2(Rnd& rnd)                                                     { return Random2x2(rnd, 0.0f, maths::tau); }
	inline m2x2 Random2x2()                                                             { return Random2x2(rand::Rand()); }
	
	inline m3x3 Random3x3(Rnd& rnd, v4 const& axis, float min_angle, float max_angle)   { return m3x3::make(axis, rnd.f32(min_angle, max_angle)); }
	inline m3x3 Random3x3(          v4 const& axis, float min_angle, float max_angle)   { return Random3x3(rand::Rand(), axis, min_angle, max_angle); }
	inline m3x3 Random3x3(Rnd& rnd)                                                     { return Random3x3(Random3N(rnd, 0.0f), 0.0f, maths::tau); }
	inline m3x3 Random3x3()                                                             { return Random3x3(rand::Rand()); }
	
	inline m4x4 Random4x4(Rnd& rnd, float min_value, float max_value)
	{
		m4x4 m;
		m.x.set(rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value));
		m.y.set(rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value));
		m.z.set(rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value));
		m.w.set(rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value), rnd.f32(min_value, max_value));
		return m;
	}
	inline m4x4 Random4x4(          float min_value, float max_value)                                                 { return Random4x4(rand::Rand(), min_value, max_value); }
	inline m4x4 Random4x4(Rnd& rnd, v4 const& axis, float min_angle, float max_angle, v4 const& position)             { m4x4 m; cast_m3x3(m).set(axis, rnd.f32(min_angle, max_angle)); m.pos = position; return m; }
	inline m4x4 Random4x4(          v4 const& axis, float min_angle, float max_angle, v4 const& position)             { return Random4x4(rand::Rand(), axis, min_angle, max_angle, position); }
	inline m4x4 Random4x4(Rnd& rnd, float min_angle, float max_angle, v4 const& position)                             { return Random4x4(Random3N(rnd, 0.0f), min_angle, max_angle, position); }
	inline m4x4 Random4x4(          float min_angle, float max_angle, v4 const& position)                             { return Random4x4(rand::Rand(), min_angle, max_angle, position); }
	inline m4x4 Random4x4(Rnd& rnd, v4 const& axis, float min_angle, float max_angle, v4 const& centre, float radius) { return Random4x4(rnd, axis, min_angle, max_angle, centre + Random3(rnd, 0.0f, radius, 0.0f)); }
	inline m4x4 Random4x4(          v4 const& axis, float min_angle, float max_angle, v4 const& centre, float radius) { return Random4x4(rand::Rand(), axis, min_angle, max_angle, centre, radius); }
	inline m4x4 Random4x4(Rnd& rnd, float min_angle, float max_angle, v4 const& centre, float radius)                 { return Random4x4(rnd, Random3N(rnd, 0.0f), min_angle, max_angle, centre, radius); }
	inline m4x4 Random4x4(          float min_angle, float max_angle, v4 const& centre, float radius)                 { return Random4x4(rand::Rand(), min_angle, max_angle, centre, radius); }
	inline m4x4 Random4x4(Rnd& rnd, v4 const& centre, float radius)                                                   { return Random4x4(Random3N(rnd, 0.0f), 0.0f, maths::tau, centre, radius); }
	inline m4x4 Random4x4(          v4 const& centre, float radius)                                                   { return Random4x4(rand::Rand(), centre, radius); }
	
	inline Quat RandomQ(Rnd& rnd, v4 const& axis, float min_angle, float max_angle) { return Quat::make(axis, rnd.f32(min_angle, max_angle)); }
	inline Quat RandomQ(          v4 const& axis, float min_angle, float max_angle) { return RandomQ(rand::Rand(), axis, min_angle, max_angle); }
	inline Quat RandomQ(Rnd& rnd, float min_angle, float max_angle)                 { return Quat::make(Random3N(rnd, 0.0f), rnd.f32(min_angle, max_angle)); }
	inline Quat RandomQ(          float min_angle, float max_angle)                 { return RandomQ(rand::Rand(), min_angle, max_angle); }
	inline Quat RandomQ(Rnd& rnd)                                                   { return Quat::make(Random3N(rnd, 0.0f), rnd.f32(0.0f, maths::tau)); }
	inline Quat RandomQ()                                                           { return RandomQ(rand::Rand()); }
}

#endif
