//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX2X2_IMPL_H
#define PR_MATHS_MATRIX2X2_IMPL_H

#include "pr/maths/matrix2x2.h"

namespace pr
{
	// Type methods
	inline m2x2      m2x2::make(v2 const& x, v2 const& y)                { m2x2 m; return m.set(x, y); }
	inline m2x2      m2x2::make(float xx, float xy, float yx, float yy)  { m2x2 m; return m.set(xx, xy, yx, yy); }
	inline m2x2      m2x2::make(float const* mat)                        { m2x2 m; return m.set(mat); }
	inline m2x2      m2x2::make(float angle)                             { m2x2 m; return m.set(angle); }
	inline m2x2&     m2x2::set(v2 const& x_, v2 const& y_)               { x = x_; y = y_; return *this; }
	inline m2x2&     m2x2::set(float xx, float xy, float yx, float yy)   { x.set(xx, xy); y.set(yx, yy); return *this; }
	inline m2x2&     m2x2::set(float const* mat)                         { x.set(mat); y.set(mat+2); return *this; }
	inline m2x2&     m2x2::set(float angle)                              { y.y = (x.x = Cos(angle)); y.x = -(x.y = Sin(angle)); return *this; }
	inline m2x2&     m2x2::zero()                                        { return *this = m2x2Zero; }
	inline m2x2&     m2x2::identity()                                    { return *this = m2x2Identity; }

	// Assignment operators
	inline m2x2& operator += (m2x2& lhs, m2x2 const& rhs)        { lhs.x += rhs.x; lhs.y += rhs.y; return lhs; }
	inline m2x2& operator -= (m2x2& lhs, m2x2 const& rhs)        { lhs.x -= rhs.x; lhs.y -= rhs.y; return lhs; }
	inline m2x2& operator *= (m2x2& lhs, float s)                { lhs.x *= s; lhs.y *= s; return lhs; }
	inline m2x2& operator /= (m2x2& lhs, float s)                { lhs.x /= s; lhs.y /= s; return lhs; }

	// Binary operators
	inline m2x2 operator + (m2x2 const& lhs, m2x2 const& rhs)    { m2x2 m = lhs; return m += rhs; }
	inline m2x2 operator - (m2x2 const& lhs, m2x2 const& rhs)    { m2x2 m = lhs; return m -= rhs; }
	inline m2x2 operator * (m2x2 const& lhs, float s)            { m2x2 m = lhs; return m *= s; }
	inline m2x2 operator * (float s, m2x2 const& rhs)            { m2x2 m = rhs; return m *= s; }
	inline m2x2 operator / (m2x2 const& lhs, float s)            { m2x2 m = lhs; return m /= s; }
	inline m2x2 operator * (m2x2 const& lhs, m2x2 const& rhs)
	{
		m2x2 ans, lhs_t = GetTranspose(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int j = 0; j < 2; ++j)
		{
			#pragma PR_OMP_PARALLEL_FOR
			for (int i = 0; i < 2; ++i)
				ans[j][i] = Dot2(lhs_t[i], rhs[j]);
		}
		return ans;
	}
	inline v2 operator * (m2x2 const& lhs, v2 const& rhs)
	{
		v2 ans;
		m2x2 lhs_t = GetTranspose(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int i = 0; i < 2; ++i)
			ans[i] = Dot2(lhs_t[i], rhs);
		return ans;
	}
	
	// Unary operators
	inline m2x2 operator + (m2x2 const& mat) { return mat; }
	inline m2x2 operator - (m2x2 const& mat) { return m2x2::make(-mat.x, -mat.y); }

	// Equality operators
	inline bool operator == (m2x2 const& lhs, m2x2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (m2x2 const& lhs, m2x2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (m2x2 const& lhs, m2x2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (m2x2 const& lhs, m2x2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (m2x2 const& lhs, m2x2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (m2x2 const& lhs, m2x2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	inline bool IsFinite(m2x2 const& m)
	{
		return IsFinite(m.x) && IsFinite(m.y);
	}
	inline bool IsFinite(m2x2 const& m, float max_value)
	{
		return IsFinite(m.x, max_value) && IsFinite(m.y, max_value);
	}
	inline m2x2 Abs(m2x2 const& m)
	{
		return m2x2::make(Abs(m.x), Abs(m.y));
	}
	inline float Determinant(m2x2 const& m)
	{
		return m.x.x*m.y.y - m.x.y*m.y.x;
	}
	inline m2x2& Transpose(m2x2& m)
	{
		float tmp = m.x.y; m.x.y = m.y.x; m.y.x = tmp;
		return m;
	}
	inline m2x2 GetTranspose(m2x2 const& m)
	{
		m2x2 m_ = {{m.x.x, m.y.x}, {m.x.y, m.y.y}};
		return m_;
	}
	inline bool IsInvertable(m2x2 const& m)
	{
		return !FEql(Determinant(m), 0.0f);
	}
	inline m2x2& Inverse(m2x2& m)
	{
		float det = Determinant(m); PR_ASSERT(PR_DBG_MATHS, det != 0.0f, "Matrix is singular");
		float inv_det = 1.0f / det;
		float xx = m.x.x; m.x.x =  inv_det * m.y.y; m.y.y =  inv_det * xx;
		float xy = m.x.y; m.x.y = -inv_det * m.y.x; m.y.x = -inv_det * xy;
		return m;
	}
	inline m2x2& InverseFast(m2x2& m)
	{
		PR_ASSERT(PR_DBG_MATHS, FEql(Determinant(m) ,1.0f), "Matrix is not pure rotation");
		float xx = m.x.x; m.x.x =  m.y.y; m.y.y =  xx;
		float xy = m.x.y; m.x.y = -m.y.x; m.y.x = -xy;
		return m;
	}
	inline m2x2 GetInverse(m2x2 const& m)
	{
		m2x2 m_ = m;
		return Inverse(m_);
	}
	inline m2x2 GetInverseFast(m2x2 const& m)
	{
		m2x2 m_ = m;
		return InverseFast(m_);
	}
}

#endif
