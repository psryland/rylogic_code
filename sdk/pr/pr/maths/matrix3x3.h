//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX3X3_H
#define PR_MATHS_MATRIX3X3_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/quaternion.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	struct m3x3
	{
		v4 x;
		v4 y;
		v4 z;
		typedef v4 Array[3];

		static m3x3  make(float xx, float xy, float xz, float yx, float yy, float yz, float zx, float zy, float zz);
		static m3x3  make(v4 const& x_, v4 const& y_, v4 const& z_);
		static m3x3  make(Quat const& quat);
		static m3x3  make(v4 const& from, v4 const& to);
		static m3x3  make(v4 const& axis_norm, float angle);
		static m3x3  make(v4 const& angular_displacement);
		static m3x3  make(float pitch, float yaw, float roll);
		static m3x3  make(float const* mat);
		m3x3&        set(float xx, float xy, float xz, float yx, float yy, float yz, float zx, float zy, float zz);
		m3x3&        set(v4 const& x_, v4 const& y_, v4 const& z_);
		m3x3&        set(Quat const& quat);
		m3x3&        set(v4 const& from, v4 const& to);
		m3x3&        set(v4 const& axis_norm, v4 const& axis_sine_angle, float cos_angle);
		m3x3&        set(v4 const& axis_norm, float angle);
		m3x3&        set(v4 const& angular_displacement);
		m3x3&        set(float pitch, float yaw, float roll);
		m3x3&        set(float const* mat);
		m3x3&        set(double const* mat);
		m3x3&        zero();
		m3x3&        identity();
		v4           row(int i) const             { return v4::make(x[i], y[i], z[i], 0.0f); }
		v4           col(int i) const             { return (*this)[i]; }
		void         row(int i, v4 const& row)    { x[i] = row.x; y[i] = row.y; z[i] = row.z; }
		void         col(int i, v4 const& col)    { (*this)[i] = col; }
		Array const& ToArray() const              { return reinterpret_cast<Array const&>(*this); }
		Array&       ToArray()                    { return reinterpret_cast<Array&>      (*this); }
		v4 const&    operator [] (int i) const    { PR_ASSERT(PR_DBG_MATHS, i < 3, ""); return ToArray()[i]; }
		v4&          operator [] (int i)          { PR_ASSERT(PR_DBG_MATHS, i < 3, ""); return ToArray()[i]; }
	};

	m3x3 const m3x3Zero     = {v4Zero, v4Zero, v4Zero};
	m3x3 const m3x3Identity = {v4XAxis, v4YAxis, v4ZAxis};
	
	// Element accessors
	inline v4 const& GetX(m3x3 const& m) { return m.x; }
	inline v4 const& GetY(m3x3 const& m) { return m.y; }
	inline v4 const& GetZ(m3x3 const& m) { return m.z; }
	inline v4 const& GetW(m3x3 const&  ) { return pr::v4Origin; }
	
	// Assignment operators
	m3x3& operator += (m3x3& lhs, float rhs);
	m3x3& operator -= (m3x3& lhs, float rhs);
	m3x3& operator += (m3x3& lhs, m3x3 const& rhs);
	m3x3& operator -= (m3x3& lhs, m3x3 const& rhs);
	m3x3& operator *= (m3x3& lhs, float rhs);
	m3x3& operator /= (m3x3& lhs, float rhs);

	// Binary operators
	m3x3 operator + (m3x3 const& lhs, float rhs);
	m3x3 operator - (m3x3 const& lhs, float rhs);
	m3x3 operator + (float lhs, m3x3 const& rhs);
	m3x3 operator - (float lhs, m3x3 const& rhs);
	m3x3 operator + (m3x3 const& lhs, m3x3 const& rhs);
	m3x3 operator - (m3x3 const& lhs, m3x3 const& rhs);
	m3x3 operator * (m3x3 const& lhs, m3x3 const& rhs);
	m3x3 operator * (m3x3 const& lhs, float rhs);
	m3x3 operator * (float lhs, m3x3 const& rhs);
	m3x3 operator / (m3x3 const& lhs, float rhs);
	v4   operator * (m3x3 const& lhs, v4 const& rhs);
	v3   operator * (m3x3 const& lhs, v3 const& rhs);
	
	// Unary operators
	m3x3 operator + (m3x3 const& mat);
	m3x3 operator - (m3x3 const& mat);

	// Equality operators
	bool FEql        (m3x3 const& lhs, m3x3 const& rhs, float tol = maths::tiny);
	bool FEqlZero    (m3x3 const& lhs, float tol = maths::tiny);
	bool operator == (m3x3 const& lhs, m3x3 const& rhs);
	bool operator != (m3x3 const& lhs, m3x3 const& rhs);
	bool operator <  (m3x3 const& lhs, m3x3 const& rhs);
	bool operator >  (m3x3 const& lhs, m3x3 const& rhs);
	bool operator <= (m3x3 const& lhs, m3x3 const& rhs);
	bool operator >= (m3x3 const& lhs, m3x3 const& rhs);

	// Functions
	bool  IsFinite(m3x3 const& m);
	bool  IsFinite(m3x3 const& m, float max_value);
	m3x3  Abs(m3x3 const& mat);
	float Determinant3(m3x3 const& mat);
	float Trace3(m3x3 const& mat);
	v4    Kernel(m3x3 const& mat);
	m3x3& Transpose(m3x3& mat);
	m3x3  GetTranspose(m3x3 const& mat);
	bool  IsInvertable(m3x3 const& mat);
	m3x3& Inverse(m3x3& mat);
	m3x3  GetInverse(m3x3 const& mat);
	m3x3& InverseFast(m3x3& mat);
	m3x3  GetInverseFast(m3x3 const& mat);
	m3x3& Orthonormalise(m3x3& mat);
	bool  IsOrthonormal(m3x3 const& mat);
	void  GetAxisAngle(m3x3 const& mat, v4& axis, float& angle);
	m3x3& Rotation3x3 (m3x3& mat, float pitch, float yaw, float roll);
	m3x3& Rotation3x3 (m3x3& mat, v3 const& axis_norm, float angle);
	m3x3& Rotation3x3 (m3x3& mat, v4 const& axis_norm, float angle);
	m3x3& Rotation3x3 (m3x3& mat, Quat const& quat);
	m3x3  Rotation3x3 (float pitch, float yaw, float roll);
	m3x3  Rotation3x3 (const v3& axis, float angle);
	m3x3  Rotation3x3 (v4 const& axis_norm, float angle);
	m3x3  Rotation3x3 (const Quat& quat);
	m3x3& Scale3x3    (m3x3& mat, float scale);
	m3x3& Scale3x3    (m3x3& mat, float sx, float sy, float sz);
	m3x3  Scale3x3    (float scale);
	m3x3  Scale3x3    (float sx, float sy, float sz);
	m3x3& Shear3x3    (m3x3& mat, float sxy, float sxz, float syx, float syz, float szx, float szy);
	m3x3  Shear3x3    (float sxy, float sxz, float syx, float syz, float szx, float szy);
	m3x3& Diagonalise3x3(m3x3& mat, m3x3& eigen_vectors, v4& eigen_values);
	m3x3  GetDiagonal3x3(m3x3 const& mat, m3x3& eigen_vectors, v4& eigen_values);
	m3x3& RotationToZAxis(m3x3& mat, v4 const& from);
	m3x3  RotationToZAxis(v4 const& from);
	m3x3& OriFromDir(m3x3& ori, v4 const& dir, int axis, v4 const& up);
	m3x3  OriFromDir(v4 const& dir, int axis, v4 const& up);
	m3x3& ScaledOriFromDir(m3x3& ori, v4 const& dir, int axis, v4 const& up);
	m3x3  ScaledOriFromDir(v4 const& dir, int axis, v4 const& up);
	m3x3  CrossProductMatrix3x3(v4 const& vec);
}

#endif
