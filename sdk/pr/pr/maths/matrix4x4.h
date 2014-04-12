//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX4X4_H
#define PR_MATHS_MATRIX4X4_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"

namespace pr
{
	struct m4x4
	{
		v4 x;
		v4 y;
		v4 z;
		union { v4 w; v4 pos; };
		typedef v4 Array[4];

		static m4x4  make(v4 const& x_, v4 const& y_, v4 const& z_, v4 const& w_);
		static m4x4  make(m3x3 const& ori, v4 const& translation);
		static m4x4  make(Quat const& quat, v4 const& translation);
		static m4x4  make(v4 const& axis, float angle, v4 const& translation);
		static m4x4  make(v4 const& angular_displacement, v4 const& translation);
		static m4x4  make(v4 const& from, v4 const& to, v4 const& translation);
		static m4x4  make(float pitch,  float yaw, float roll, v4 const& translation);
		static m4x4  make(float const* mat);
		m4x4&        set(v4 const& x_, v4 const& y_, v4 const& z_, v4 const& w_);
		m4x4&        set(m3x3 const& ori, v4 const& translation);
		m4x4&        set(Quat const& quat, v4 const& translation);
		m4x4&        set(v4 const& axis, float angle, v4 const& translation);
		m4x4&        set(v4 const& angular_displacement, v4 const& translation);
		m4x4&        set(v4 const& from, v4 const& to, v4 const& translation);
		m4x4&        set(float pitch,  float yaw, float roll, v4 const& translation);
		m4x4&        set(float const* mat);
		m4x4&        zero();
		m4x4&        identity();
		v4           row(int i) const                       { return v4::make(x[i], y[i], z[i], w[i]); }
		v4           col(int i) const                       { return (*this)[i]; }
		void         row(int i, v4 const& row)              { x[i] = row.x; y[i] = row.y; z[i] = row.z; w[i] = row.w; }
		void         col(int i, v4 const& col)              { (*this)[i] = col; }
		Array const& ToArray() const                        { return reinterpret_cast<Array const&>(*this); }
		Array&       ToArray()                              { return reinterpret_cast<Array&>      (*this); }
		v4 const&    operator [] (int i) const              { assert(i < 4); return ToArray()[i]; }
		v4&          operator [] (int i)                    { assert(i < 4); return ToArray()[i]; }
	};

	m4x4 const m4x4Zero     = {v4Zero, v4Zero, v4Zero, v4Zero};
	m4x4 const m4x4Identity = {v4XAxis, v4YAxis, v4ZAxis, v4Origin};

	// Element accessors
	inline v4 const& GetX(m4x4 const& m) { return m.x; }
	inline v4 const& GetY(m4x4 const& m) { return m.y; }
	inline v4 const& GetZ(m4x4 const& m) { return m.z; }
	inline v4 const& GetW(m4x4 const& m) { return m.w; }

	// Assignment operators
	m4x4& operator += (m4x4& lhs, float rhs);
	m4x4& operator -= (m4x4& lhs, float rhs);
	m4x4& operator += (m4x4& lhs, m4x4 const& rhs);
	m4x4& operator -= (m4x4& lhs, m4x4 const& rhs);
	m4x4& operator *= (m4x4& lhs, float s);
	m4x4& operator /= (m4x4& lhs, float s);
	m4x4& operator += (m4x4& lhs, m3x3 const& rhs);
	m4x4& operator -= (m4x4& lhs, m3x3 const& rhs);

	// Binary operators
	m4x4 operator + (m4x4 const& lhs, float rhs);
	m4x4 operator - (m4x4 const& lhs, float rhs);
	m4x4 operator + (float lhs, m4x4 const& rhs);
	m4x4 operator - (float lhs, m4x4 const& rhs);
	m4x4 operator + (m4x4 const& lhs, m4x4 const& rhs);
	m4x4 operator - (m4x4 const& lhs, m4x4 const& rhs);
	m4x4 operator * (m4x4 const& lhs, float rhs);
	m4x4 operator * (float lhs, m4x4 const& rhs);
	m4x4 operator / (m4x4 const& lhs, float rhs);
	m4x4 operator * (m4x4 const& lhs, m4x4 const& rhs);
	v4   operator * (m4x4 const& lhs, v4 const& rhs);

	// Unary operators
	m4x4 operator + (m4x4 const& mat);
	m4x4 operator - (m4x4 const& mat);

	// Equality operators
	bool FEql        (m4x4 const& lhs, m4x4 const& rhs, float tol = maths::tiny);
	bool FEqlZero    (m4x4 const& lhs, float tol = maths::tiny);
	bool operator == (m4x4 const& lhs, m4x4 const& rhs);
	bool operator != (m4x4 const& lhs, m4x4 const& rhs);
	bool operator <  (m4x4 const& lhs, m4x4 const& rhs);
	bool operator >  (m4x4 const& lhs, m4x4 const& rhs);
	bool operator <= (m4x4 const& lhs, m4x4 const& rhs);
	bool operator >= (m4x4 const& lhs, m4x4 const& rhs);

	// DirectXMath conversion functions
	#if PR_MATHS_USE_DIRECTMATH
	DirectX::XMMATRIX const& dxm4(m4x4 const& m);
	DirectX::XMMATRIX      & dxm4(m4x4&       m);
	#endif

	// Conversion functions between vector types
	m3x3 const& cast_m3x3(m4x4 const& mat);
	m3x3&       cast_m3x3(m4x4& mat);

	// Return a m4x4 from this m3x3
	m4x4& Zero(m4x4& mat);
	m4x4  Getm4x4(m3x3 const& mat);
	bool  IsFinite(m4x4 const& mat);
	bool  IsFinite(m4x4 const& mat, float max_value);
	bool  IsAffine(m4x4 const& mat);
	float Determinant3(m4x4 const& mat);
	float DeterminantFast4(m4x4 const& mat);
	float Determinant4(m4x4 const& mat);
	float Trace3(m4x4 const& mat);
	float Trace4(m4x4 const& mat);
	v4    Kernel(m4x4 const& mat);
	m4x4& Transpose4x4(m4x4& mat);
	m4x4& Transpose3x3(m4x4& mat);
	m4x4  GetTranspose4x4(m4x4 const& mat);
	m4x4  GetTranspose3x3(m4x4 const& mat);
	m4x4  GetRotation(m4x4 const& mat);
	bool  IsInvertable(m4x4 const& mat);
	m4x4& Inverse(m4x4& mat);
	m4x4  GetInverse(m4x4 const& mat);
	m4x4& InverseFast(m4x4& mat);
	m4x4  GetInverseFast(m4x4 const& mat);
	m4x4& Orthonormalise(m4x4& mat);
	bool  IsOrthonormal(m4x4 const& mat);
	void  GetAxisAngle(m4x4 const& mat, v4& axis, float& angle);
	m4x4  Abs(m4x4 const& mat);
	m4x4  Sqr(m4x4 const& mat);
	m4x4& Translation(m4x4& mat, v3 const& xyz);
	m4x4& Translation(m4x4& mat, v4 const& xyz);
	m4x4& Translation(m4x4& mat, float x, float y, float z);
	m4x4  Translation(v3 const& xyz);
	m4x4  Translation(v4 const& xyz);
	m4x4  Translation(float x, float y, float z);
	m4x4& Rotation4x4(m4x4& mat, float pitch, float yaw, float roll, v4 const& translation);
	m4x4& Rotation4x4(m4x4& mat, v4 const& axis, float angle, v4 const& translation);
	m4x4& Rotation4x4(m4x4& mat, v4 const& angular_displacement, v4 const& translation);
	m4x4& Rotation4x4(m4x4& mat, v4 const& from, v4 const& to, v4 const& translation);
	m4x4& Rotation4x4(m4x4& mat, Quat const& quat, v4 const& translation);
	m4x4  Rotation4x4(float pitch, float yaw, float roll, v4 const& translation);
	m4x4  Rotation4x4(v3 const& axis, float angle, v4 const& translation);
	m4x4  Rotation4x4(v4 const& axis, float angle, v4 const& translation);
	m4x4  Rotation4x4(v4 const& angular_displacement, v4 const& translation);
	m4x4  Rotation4x4(v4 const& from, v4 const& to, v4 const& translation);
	m4x4  Rotation4x4(Quat const& quat, v4 const& translation);
	m4x4& Scale4x4(m4x4& mat, float scale, v4 const& translation);
	m4x4& Scale4x4(m4x4& mat, float sx, float sy, float sz, v4 const& translation);
	m4x4  Scale4x4(float scale, v4 const& translation);
	m4x4  Scale4x4(float sx, float sy, float sz, v4 const& translation);
	m4x4& Shear4x4(m4x4& mat, float sxy, float sxz, float syx, float syz, float szx, float szy, v4 const& translation);
	m4x4  Shear4x4(float sxy, float sxz, float syx, float syz, float szx, float szy, v4 const& translation);
	m4x4& LookAt(m4x4& mat, v4 const& eye, v4 const& at, v4 const& up);
	m4x4  LookAt(v4 const& eye, v4 const& at, v4 const& up);
	m4x4& ProjectionOrthographic(m4x4& mat, float w, float h, float Znear, float Zfar, bool righthanded);
	m4x4  ProjectionOrthographic(float w, float h, float Znear, float Zfar, bool righthanded);
	m4x4& ProjectionPerspective(m4x4& mat, float w, float h, float Znear, float Zfar, bool righthanded);
	m4x4  ProjectionPerspective(float w, float h, float Znear, float Zfar, bool righthanded);
	m4x4& ProjectionPerspective(m4x4& mat, float l, float r, float t, float b, float Znear, float Zfar, bool righthanded);
	m4x4  ProjectionPerspective(float l, float r, float t, float b, float Znear, float Zfar, bool righthanded);
	m4x4& ProjectionPerspectiveFOV(m4x4& mat, float fovY, float aspect, float Znear, float Zfar, bool righthanded);
	m4x4  ProjectionPerspectiveFOV(float FOV, float aspect, float Znear, float Zfar, bool righthanded);
	m4x4  CrossProductMatrix4x4(v4 const& vec);
	m4x4& OriFromDir(m4x4& ori, v4 const& dir, int axis, v4 const& up, v4 const& position);
	m4x4  OriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& position);
	m4x4& ScaledOriFromDir(m4x4& ori, v4 const& dir, int axis, v4 const& up, v4 const& position);
	m4x4  ScaledOriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& position);
	m4x4  Sqrt(m4x4 const& mat);
}

#endif
