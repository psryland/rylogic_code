//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX2X2_H
#define PR_MATHS_MATRIX2X2_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector2.h"

namespace pr
{
	struct m2x2
	{
		#pragma warning (disable:4201)
		union {
		struct { v2 x, y; };
		struct { v2 arr[2]; };
		};
		#pragma warning (default:4201)

		m2x2& set(v2 const& x_, v2 const& y_);
		m2x2& set(float xx, float xy, float yx, float yy);
		m2x2& set(float const* mat);
		m2x2& set(float angle);

		typedef v2 Array[2];
		Array const& ToArray() const               { return arr; }
		Array&       ToArray()                     { return arr; }
		v2 const&    operator [](uint i) const     { assert(i < 2); return arr[i]; }
		v2&          operator [](uint i)           { assert(i < 2); return arr[i]; }

		static m2x2 make(v2 const& x, v2 const& y)                { m2x2 m; return m.set(x, y);           }
		static m2x2 make(float xx, float xy, float yx, float yy)  { m2x2 m; return m.set(xx, xy, yx, yy); }
		static m2x2 make(float const* mat)                        { m2x2 m; return m.set(mat);            }
		static m2x2 make(float angle)                             { m2x2 m; return m.set(angle);          }
	};
	static_assert(std::is_pod<m2x2>::value, "Should be a pod type");

	static m2x2 const m2x2Zero     = {v2Zero, v2Zero};
	static m2x2 const m2x2Identity = {v2XAxis, v2YAxis};

	// Element accessors
	inline v4 GetX(m2x2 const& m) { return v4::make(m.x, 0.0f, 0.0f); }
	inline v4 GetY(m2x2 const& m) { return v4::make(m.y, 0.0f, 0.0f); }
	inline v4 GetZ(m2x2 const&  ) { return pr::v4ZAxis; }
	inline v4 GetW(m2x2 const&  ) { return pr::v4Origin; }

	// Assignment operators
	m2x2& operator += (m2x2& lhs, m2x2 const& rhs);
	m2x2& operator -= (m2x2& lhs, m2x2 const& rhs);
	m2x2& operator *= (m2x2& lhs, float s);
	m2x2& operator /= (m2x2& lhs, float s);

	// Binary operators
	m2x2 operator + (m2x2 const& lhs, m2x2 const& rhs);
	m2x2 operator - (m2x2 const& lhs, m2x2 const& rhs);
	m2x2 operator * (m2x2 const& lhs, m2x2 const& rhs);
	m2x2 operator * (m2x2 const& lhs, float s);
	m2x2 operator * (float s, m2x2 const& rhs);
	m2x2 operator / (m2x2 const& lhs, float s);

	// Unary operators
	m2x2 operator + (m2x2 const& mat);
	m2x2 operator - (m2x2 const& mat);

	// Equality operators
	bool operator == (m2x2 const& lhs, m2x2 const& rhs);
	bool operator != (m2x2 const& lhs, m2x2 const& rhs);
	bool operator <  (m2x2 const& lhs, m2x2 const& rhs);
	bool operator >  (m2x2 const& lhs, m2x2 const& rhs);
	bool operator <= (m2x2 const& lhs, m2x2 const& rhs);
	bool operator >= (m2x2 const& lhs, m2x2 const& rhs);

	// Functions
	bool    IsFinite(m2x2 const& m);
	bool    IsFinite(m2x2 const& m, float max_value);
	m2x2    Abs(m2x2 const& m);
	float   Determinant(m2x2 const& m);
	m2x2&   Transpose(m2x2& m);
	m2x2    GetTranspose(m2x2 const& m);
	bool    IsInvertable(m2x2 const& m);
	m2x2    Invert(m2x2 const& m);
	m2x2    InvertFast(m2x2 const& m);
}

#endif