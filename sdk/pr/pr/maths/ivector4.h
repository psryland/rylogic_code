//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_IVECTOR4_H
#define PR_MATHS_IVECTOR4_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"

namespace pr
{
	struct iv4
	{
		int x;
		int y;
		int z;
		int w;

		iv4&                       set(int x_, int y_, int z_, int w_) { x = x_; y = y_; z = z_; w = w_; return *this; }
		template <typename T> iv4& set(T const& vec, int z_, int w_)   { x = GetXi(vec); y = GetYi(vec); z = z_;         w = w_;         return *this; }
		template <typename T> iv4& set(T const& vec, int w_)           { x = GetXi(vec); y = GetYi(vec); z = GetZi(vec); w = w_;         return *this; }
		template <typename T> iv4& set(T const& vec)                   { x = GetXi(vec); y = GetYi(vec); z = GetZi(vec); w = GetWi(vec); return *this; }
		iv4&                       set(int const* vec)                 { x = vec[0]; y = vec[1]; z = vec[2]; w = vec[3]; return *this; }
		int const*                 ToArray() const                     { return reinterpret_cast<int const*>(this); }
		int*                       ToArray()                           { return reinterpret_cast<int*>      (this); }
		int const&                 operator [] (int i) const           { assert(i < 4); return ToArray()[i]; }
		int&                       operator [] (int i)                 { assert(i < 4); return ToArray()[i]; }
		iv4& operator =  (v4 const& vec);
		operator v4 () const;

		static iv4 make(int x, int y, int z, int w)     { iv4 v; return v.set(x, y, z, w); }
		static iv4 make(iv2 const& vec, int z, int w)   { iv4 v; return v.set(vec, z, w); }
		static iv4 make(v4 const& vec)                  { iv4 v; return v.set(vec); }
		static iv4 make(int const* vec)                 { iv4 v; return v.set(vec); }
	};

	iv4 const iv4Zero   = {0, 0, 0, 0};
	iv4 const iv4One    = {1, 1, 1, 1};
	iv4 const iv4XAxis  = {1, 0, 0, 0};
	iv4 const iv4YAxis  = {0, 1, 0, 0};
	iv4 const iv4ZAxis  = {0, 0, 1, 0};
	iv4 const iv4Origin = {0, 0, 0, 1};

	// Element accessors
	inline int GetX(iv4 const& v) { return v.x; }
	inline int GetY(iv4 const& v) { return v.y; }
	inline int GetZ(iv4 const& v) { return v.z; }
	inline int GetW(iv4 const& v) { return v.w; }

	// Assignment operators
	template <typename T> inline iv4& operator += (iv4& lhs, T rhs) { lhs.x += GetXi(rhs); lhs.y += GetYi(rhs); lhs.z += GetZi(rhs); lhs.w += GetWi(rhs); return lhs; }
	template <typename T> inline iv4& operator -= (iv4& lhs, T rhs) { lhs.x -= GetXi(rhs); lhs.y -= GetYi(rhs); lhs.z -= GetZi(rhs); lhs.w -= GetWi(rhs); return lhs; }
	template <typename T> inline iv4& operator *= (iv4& lhs, T rhs) { lhs.x *= GetXi(rhs); lhs.y *= GetYi(rhs); lhs.z *= GetZi(rhs); lhs.w *= GetWi(rhs); return lhs; }
	template <typename T> inline iv4& operator /= (iv4& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x /= GetXi(rhs); lhs.y /= GetYi(rhs); lhs.z /= GetZi(rhs); lhs.w /= GetWi(rhs); return lhs; }
	template <typename T> inline iv4& operator %= (iv4& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x %= GetXi(rhs); lhs.y %= GetYi(rhs); lhs.z %= GetZi(rhs); lhs.w %= GetWi(rhs); return lhs; }

	// Binary operators
	template <typename T> inline iv4 operator + (iv4 const& lhs, T rhs) { iv4 v = lhs; return v += rhs; }
	template <typename T> inline iv4 operator - (iv4 const& lhs, T rhs) { iv4 v = lhs; return v -= rhs; }
	template <typename T> inline iv4 operator * (iv4 const& lhs, T rhs) { iv4 v = lhs; return v *= rhs; }
	template <typename T> inline iv4 operator / (iv4 const& lhs, T rhs) { iv4 v = lhs; return v /= rhs; }
	template <typename T> inline iv4 operator % (iv4 const& lhs, T rhs) { iv4 v = lhs; return v %= rhs; }
	inline iv4 operator + (int lhs, iv4 const& rhs)                     { iv4 v = rhs; return v += lhs; }
	inline iv4 operator - (int lhs, iv4 const& rhs)                     { iv4 v = rhs; return v -= lhs; }
	inline iv4 operator * (int lhs, iv4 const& rhs)                     { iv4 v = rhs; return v *= lhs; }
	inline iv4 operator / (int lhs, iv4 const& rhs)                     { assert(All4(rhs,maths::NonZero<int>)); return iv4::make(lhs/GetXi(rhs), lhs/GetYi(rhs), lhs/GetZi(rhs), lhs/GetWi(rhs)); }
	inline iv4 operator % (int lhs, iv4 const& rhs)                     { assert(All4(rhs,maths::NonZero<int>)); return iv4::make(lhs%GetXi(rhs), lhs%GetYi(rhs), lhs%GetZi(rhs), lhs%GetWi(rhs)); }

	// Unary operators
	inline iv4 operator + (iv4 const& vec) { return vec; }
	inline iv4 operator - (iv4 const& vec) { return iv4::make(-vec.x, -vec.y, -vec.z, -vec.w); }

	// Equality operators
	inline bool operator == (iv4 const& lhs, iv4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (iv4 const& lhs, iv4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (iv4 const& lhs, iv4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (iv4 const& lhs, iv4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (iv4 const& lhs, iv4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (iv4 const& lhs, iv4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	iv4&  Zero(iv4 const& v);
	iv4   Abs(iv4 const& v);
	int   Dot3(iv4 const& lhs, iv4 const& rhs);
	int   Dot4(iv4 const& lhs, iv4 const& rhs);
	iv4   Cross3(iv4 const& lhs, iv4 const& rhs);
}

#endif
