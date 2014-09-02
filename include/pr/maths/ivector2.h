//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_IVECTOR2_H
#define PR_MATHS_IVECTOR2_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector2.h"

namespace pr
{
	struct iv2
	{
		int x, y;

		iv2&                       set(int x_, int y_)          { x = x_; y = y_; return *this; }
		iv2&                       set(int const* vec)          { x = vec[0]; y = vec[1]; return *this; }
		template <typename T> iv2& set(T const& vec)            { x = GetXi(vec); y = GetYi(vec); return *this; }
		int const*                 ToArray() const              { return reinterpret_cast<int const*>(this); }
		int*                       ToArray()                    { return reinterpret_cast<int*>      (this); }
		int const&                 operator [] (uint i) const   { assert(i < 2); return ToArray()[i]; }
		int&                       operator [] (uint i)         { assert(i < 2); return ToArray()[i]; }
		iv2& operator = (v2 const& rhs);
		operator v2() const;

		static iv2      make(int x, int y)                      { iv2 v; return v.set(x, y); }
		static iv2      make(v2 const& vec)                     { iv2 v; return v.set(vec); }
		static iv2      make(int const* vec)                    { iv2 v; return v.set(vec); }
		template <typename T> inline iv2 make(T const& pt)      { iv2 v; return v.set(GetXi(pt), GetYi(pt)); }
	};

	static iv2 const iv2Zero   = {0, 0};
	static iv2 const iv2One    = {1, 1};
	static iv2 const iv2Min    = {maths::int_min, maths::int_min};
	static iv2 const iv2Max    = {maths::int_max, maths::int_max};
	static iv2 const iv2XAxis  = {1, 0};
	static iv2 const iv2YAxis  = {0, 1};

	// Limits
	namespace maths
	{
		template <> struct limits<iv2>
		{
			static iv2 min() { return iv2Min; }
			static iv2 max() { return iv2Max; }
		};
	}

	// Element accessors
	inline int GetX(iv2 const& v) { return v.x; }
	inline int GetY(iv2 const& v) { return v.y; }
	inline int GetZ(iv2 const&  ) { return 0; }
	inline int GetW(iv2 const&  ) { return 0; }

	// Assignment operators
	template <typename T> inline iv2& operator += (iv2& lhs, T rhs) { lhs.x += GetXi(rhs); lhs.y += GetYi(rhs); return lhs; }
	template <typename T> inline iv2& operator -= (iv2& lhs, T rhs) { lhs.x -= GetXi(rhs); lhs.y -= GetYi(rhs); return lhs; }
	template <typename T> inline iv2& operator *= (iv2& lhs, T rhs) { lhs.x *= GetXi(rhs); lhs.y *= GetYi(rhs); return lhs; }
	template <typename T> inline iv2& operator /= (iv2& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x /= GetXi(rhs); lhs.y /= GetYi(rhs); return lhs; }
	template <typename T> inline iv2& operator %= (iv2& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x %= GetXi(rhs); lhs.y %= GetYi(rhs); return lhs; }

	// Binary operators
	template <typename T> inline iv2 operator + (iv2 const& lhs, T rhs) { iv2 v = lhs; return v += rhs; }
	template <typename T> inline iv2 operator - (iv2 const& lhs, T rhs) { iv2 v = lhs; return v -= rhs; }
	template <typename T> inline iv2 operator * (iv2 const& lhs, T rhs) { iv2 v = lhs; return v *= rhs; }
	template <typename T> inline iv2 operator / (iv2 const& lhs, T rhs) { iv2 v = lhs; return v /= rhs; }
	template <typename T> inline iv2 operator % (iv2 const& lhs, T rhs) { iv2 v = lhs; return v %= rhs; }
	inline iv2 operator + (int lhs, iv2 const& rhs)                     { iv2 v = rhs; return v += lhs; }
	inline iv2 operator - (int lhs, iv2 const& rhs)                     { iv2 v = rhs; return v -= lhs; }
	inline iv2 operator * (int lhs, iv2 const& rhs)                     { iv2 v = rhs; return v *= lhs; }
	inline iv2 operator / (int lhs, iv2 const& rhs)                     { assert(All4(rhs,maths::NonZero<int>)); return iv2::make(lhs / rhs.x, lhs / rhs.y); }
	inline iv2 operator % (int lhs, iv2 const& rhs)                     { assert(All4(rhs,maths::NonZero<int>)); return iv2::make(lhs % rhs.x, lhs % rhs.y); }

	// Unary operators
	inline iv2 operator + (iv2 const& vec) { return vec; }
	inline iv2 operator - (iv2 const& vec) { return iv2::make(-vec.x, -vec.y); }

	// Equality operators
	inline bool operator == (iv2 const& lhs, iv2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (iv2 const& lhs, iv2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (iv2 const& lhs, iv2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (iv2 const& lhs, iv2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (iv2 const& lhs, iv2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (iv2 const& lhs, iv2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	iv2     Abs(iv2 const& v);
	int     Dot2(iv2 const& lhs, iv2 const& rhs);
}

#endif
