//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX3X3_H
#define PR_MATHS_MATRIX3X3_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/vector4.h"

namespace pr
{
	struct alignas(16) m3x4
	{
		#pragma warning (disable:4201)
		union {
		struct { v4 x, y, z; };
		struct { v4 arr[3]; };
		};
		#pragma warning (default:4201)

		m3x4& set(v4 const& x_, v4 const& y_, v4 const& z_);
		m3x4& set(Quat const& quat);
		m3x4& set(v4 const& from, v4 const& to);
		m3x4& set(v4 const& axis_norm, v4 const& axis_sine_angle, float cos_angle);
		m3x4& set(v4 const& axis_norm, float angle);
		m3x4& set(v4 const& angular_displacement);
		m3x4& set(float pitch, float yaw, float roll);
		m3x4& set(float const* mat);
		m3x4& set(double const* mat);

		// Get/Set by row or column. Note: matrices are stored as row-major
		// but conceptually, x,y,z are still the column vectors in the matrix
		v4   col(int i) const          { return (*this)[i];                       }
		void col(int i, v4 const& col) { (*this)[i] = col;                        }
		v4   row(int i) const          { return v4::make(x[i], y[i], z[i], 0.0f); }
		void row(int i, v4 const& row) { x[i] = row.x;y[i] = row.y;z[i] = row.z;  }

		typedef v4 Array[3];
		Array const& ToArray() const           { return arr; }
		Array&       ToArray()                 { return arr; }
		v4 const&    operator [] (int i) const { assert(i < 3); return arr[i]; }
		v4&          operator [] (int i)       { assert(i < 3); return arr[i]; }

		static m3x4 make(v4 const& x_, v4 const& y_, v4 const& z_) { m3x4 m; return m.set(x_, y_, z_);           }
		static m3x4 make(Quat const& quat)                         { m3x4 m; return m.set(quat);                 }
		static m3x4 make(v4 const& from, v4 const& to)             { m3x4 m; return m.set(from, to);             }
		static m3x4 make(v4 const& axis_norm, float angle)         { m3x4 m; return m.set(axis_norm, angle);     }
		static m3x4 make(v4 const& angular_displacement)           { m3x4 m; return m.set(angular_displacement); }
		static m3x4 make(float pitch, float yaw, float roll)       { m3x4 m; return m.set(pitch, yaw, roll);     }
		static m3x4 make(float const* mat)                         { m3x4 m; return m.set(mat);                  }
	};
	static_assert(std::alignment_of<m3x4>::value == 16, "Should be 16 byte aligned");
	static_assert(std::is_pod<m3x4>::value, "Should be a pod type");

	static m3x4 const m3x4Zero     = {v4Zero, v4Zero, v4Zero};
	static m3x4 const m3x4Identity = {v4XAxis, v4YAxis, v4ZAxis};

	// Element accessors
	inline v4 const& GetX(m3x4 const& m) { return m.x; }
	inline v4 const& GetY(m3x4 const& m) { return m.y; }
	inline v4 const& GetZ(m3x4 const& m) { return m.z; }
	inline v4 const& GetW(m3x4 const&  ) { return pr::v4Origin; }

	// Assignment operators
	m3x4& operator += (m3x4& lhs, float rhs);
	m3x4& operator -= (m3x4& lhs, float rhs);
	m3x4& operator += (m3x4& lhs, m3x4 const& rhs);
	m3x4& operator -= (m3x4& lhs, m3x4 const& rhs);
	m3x4& operator *= (m3x4& lhs, float rhs);
	m3x4& operator /= (m3x4& lhs, float rhs);

	// Binary operators
	m3x4 operator + (m3x4 const& lhs, float rhs);
	m3x4 operator - (m3x4 const& lhs, float rhs);
	m3x4 operator + (float lhs, m3x4 const& rhs);
	m3x4 operator - (float lhs, m3x4 const& rhs);
	m3x4 operator + (m3x4 const& lhs, m3x4 const& rhs);
	m3x4 operator - (m3x4 const& lhs, m3x4 const& rhs);
	m3x4 operator * (m3x4 const& lhs, m3x4 const& rhs);
	m3x4 operator * (m3x4 const& lhs, float rhs);
	m3x4 operator * (float lhs, m3x4 const& rhs);
	m3x4 operator / (m3x4 const& lhs, float rhs);
	v4   operator * (m3x4 const& lhs, v4 const& rhs);
	v3   operator * (m3x4 const& lhs, v3 const& rhs);

	// Unary operators
	m3x4 operator + (m3x4 const& mat);
	m3x4 operator - (m3x4 const& mat);

	// Equality operators
	bool FEql        (m3x4 const& lhs, m3x4 const& rhs, float tol = maths::tiny);
	bool operator == (m3x4 const& lhs, m3x4 const& rhs);
	bool operator != (m3x4 const& lhs, m3x4 const& rhs);
	bool operator <  (m3x4 const& lhs, m3x4 const& rhs);
	bool operator >  (m3x4 const& lhs, m3x4 const& rhs);
	bool operator <= (m3x4 const& lhs, m3x4 const& rhs);
	bool operator >= (m3x4 const& lhs, m3x4 const& rhs);

	// Functions
	bool  IsFinite(m3x4 const& m);
	bool  IsFinite(m3x4 const& m, float max_value);
	m3x4  Abs(m3x4 const& mat);
	float Determinant3(m3x4 const& mat);
	float Trace3(m3x4 const& mat);
	v4    Kernel(m3x4 const& mat);
	m3x4  Transpose3x3(m3x4 const& mat);
	bool  IsInvertable(m3x4 const& mat);
	m3x4  Invert(m3x4 const& mat);
	m3x4  InvertFast(m3x4 const& mat);
	m3x4  Orthonorm(m3x4 const& mat);
	bool  IsOrthonormal(m3x4 const& mat);
	void  GetAxisAngle(m3x4 const& mat, v4& axis, float& angle);
	v4    GetEulerAngles(m3x4 const& mat);
	m3x4  Rotation3x3 (float pitch, float yaw, float roll);
	m3x4  Rotation3x3 (const v3& axis, float angle);
	m3x4  Rotation3x3 (v4 const& axis_norm, float angle);
	m3x4  Rotation3x3 (const Quat& quat);
	m3x4  Rotation3x3 (v4 const& from, v4 const& to);
	m3x4  Scale3x3    (float scale);
	m3x4  Scale3x3    (float sx, float sy, float sz);
	m3x4  Shear3x3    (float sxy, float sxz, float syx, float syz, float szx, float szy);
	m3x4  Diagonalise3x3(m3x4 const& mat, m3x4& eigen_vectors, v4& eigen_values);
	m3x4  RotationToZAxis(v4 const& from);
	m3x4  OriFromDir(v4 const& dir, int axis, v4 const& up);
	m3x4  ScaledOriFromDir(v4 const& dir, int axis, v4 const& up);
	m3x4  CrossProductMatrix3x3(v4 const& vec);
}

#endif
