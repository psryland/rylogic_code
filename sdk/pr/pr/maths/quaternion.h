//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_QUATERNION_H
#define PR_MATHS_QUATERNION_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"

namespace pr
{
	struct Quat
	{
		#if PR_MATHS_USE_INTRINSICS || PR_MATHS_USE_DIRECTMATH
		#pragma warning(push)
		#pragma warning(disable:4201)
		union {
			struct {
				float x;
				float y;
				float z;
				float w;
			};
			#if PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec;
			#else
			__m128 vec;
			#endif
		};
		#pragma warning(pop)
		#else
		float x;
		float y;
		float z;
		float w;
		#endif
		
		Quat&        set(float x_, float y_, float z_, float w_);
		Quat&        set(v4 const& axis, float angle);
		Quat&        set(float pitch, float yaw, float roll);
		Quat&        set(m3x3 const& m);
		Quat&        set(m4x4 const& m);
		Quat&        set(v4 const& from, v4 const& to);
		float const* ToArray() const           { return reinterpret_cast<float const*>(this); }
		float*       ToArray()                 { return reinterpret_cast<float*>(this); }
		float const& operator [](int i) const  { PR_ASSERT(PR_DBG_MATHS, i < 4, ""); return ToArray()[i]; }
		float&       operator [](int i)        { PR_ASSERT(PR_DBG_MATHS, i < 4, ""); return ToArray()[i]; }
		
		static Quat  make(float x_, float y_, float z_, float w_)  { Quat q; return q.set(x_, y_, z_, w_); }
		static Quat  make(v4 const& axis, float angle)             { Quat q; return q.set(axis, angle); }
		static Quat  make(float pitch, float yaw, float roll)      { Quat q; return q.set(pitch, yaw, roll); }
		static Quat  make(m3x3 const& m)                           { Quat q; return q.set(m); }
		static Quat  make(m4x4 const& m)                           { Quat q; return q.set(m); }
		static Quat  make(v4 const& from, v4 const& to)            { Quat q; return q.set(from, to); }
	};
	
	Quat const QuatZero     = {0.0f, 0.0f, 0.0f, 0.0f};
	Quat const QuatIdentity = {0.0f, 0.0f, 0.0f, 1.0f};
	
	// Element accessors
	inline float GetX(Quat const& q) { return q.x; }
	inline float GetY(Quat const& q) { return q.y; }
	inline float GetZ(Quat const& q) { return q.z; }
	inline float GetW(Quat const& q) { return q.w; }
	
	// Assignment operators
	inline Quat& operator += (Quat& lhs, Quat const& rhs) { lhs.x += rhs.x; lhs.y += rhs.y; lhs.z += rhs.z; lhs.w += rhs.w; return lhs; }
	inline Quat& operator -= (Quat& lhs, Quat const& rhs) { lhs.x -= rhs.x; lhs.y -= rhs.y; lhs.z -= rhs.z; lhs.w -= rhs.w; return lhs; }
	inline Quat& operator *= (Quat& lhs, float s)         { lhs.x *= s; lhs.y *= s; lhs.z *= s; lhs.w *= s; return lhs; }
	inline Quat& operator /= (Quat& lhs, float s)         { PR_ASSERT(PR_DBG_MATHS, s != 0.0f, ""); return lhs *= (1.0f/s); }
	
	// Binary operators
	inline Quat operator + (Quat const& lhs, Quat const& rhs) { Quat q = lhs; return q += rhs; }
	inline Quat operator - (Quat const& lhs, Quat const& rhs) { Quat q = lhs; return q -= rhs; }
	inline Quat operator * (Quat const& lhs, float s)         { Quat q = lhs; return q *= s; }
	inline Quat operator * (float s, Quat const& rhs)         { Quat q = rhs; return q *= s; }
	inline Quat operator / (Quat const& lhs, float s)         { Quat q = lhs; return q /= s; }
	Quat operator * (Quat const& lhs, Quat const& rhs);
	// Note: Quat * vec is not defined because it is incorrect semantically
	// To rotate a vector by a quaternion use: r = q * (v3,0) * conj(q)
	// Use the 'Rotate()' functions.
	
	// Unary operators
	inline Quat operator + (Quat const& quat) { return quat; }
	inline Quat operator - (Quat const& quat) { return Quat::make(-quat.x, -quat.y, -quat.z, -quat.w); }
	
	// Equality operators
	inline bool operator == (Quat const& lhs, Quat const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (Quat const& lhs, Quat const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (Quat const& lhs, Quat const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (Quat const& lhs, Quat const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (Quat const& lhs, Quat const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (Quat const& lhs, Quat const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	
	// DirectXMath conversion functions
	#if PR_MATHS_USE_DIRECTMATH
	inline DirectX::XMVECTOR const& dxv4(Quat const& quat) { return quat.vec; }
	inline DirectX::XMVECTOR&       dxv4(Quat&       quat) { return quat.vec; }
	#endif
	
	// Conversion functions between quaternions and vectors
	inline v3 const& cast_v3(Quat const& quat) { return reinterpret_cast<v3 const&>(quat); }
	inline v3&       cast_v3(Quat&       quat) { return reinterpret_cast<v3&>(quat); }
	inline v4 const& cast_v4(Quat const& quat) { return reinterpret_cast<v4 const&>(quat); }
	inline v4&       cast_v4(Quat&       quat) { return reinterpret_cast<v4&>(quat); }
	
	// Functions
	bool  IsZero(Quat const& quat);
	bool  IsFinite(Quat const& q);
	bool  IsFinite(Quat const& q, float max_value);
	Quat& Conjugate(Quat& quat);
	Quat  GetConjugate(Quat const& quat);
	void  AxisAngle(Quat const& quat, v4& axis, float& angle);
	Quat  Slerp(Quat const& src, Quat const& dst, float frac);
	Quat  Rotate(Quat const& rotator, Quat const& rotatee);
	v3    Rotate(Quat const& quat, v3 const& vec);
	v4    Rotate(Quat const& quat, v4 const& vec);
}

#endif
