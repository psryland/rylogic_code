//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX4X4_IMPL_H
#define PR_MATHS_MATRIX4X4_IMPL_H

#include "pr/maths/matrix4x4.h"

namespace pr
{
	inline m4x4& m4x4::set(v4 const& x_, v4 const& y_, v4 const& z_, v4 const& w_)
	{
		x = x_;
		y = y_;
		z = z_;
		w = w_;
		return *this;
	}
	inline m4x4& m4x4::set(m3x4 const& ori, v4 const& translation)
	{
		cast_m3x4(*this) = ori;
		pos = translation;
		return *this;
	}
	inline m4x4& m4x4::set(Quat const& quat, v4 const& translation)
	{
		assert(IsNormal4(quat) && "'quat' should be a normalised quaternion");

#if PR_MATHS_USE_DIRECTMATH
		dxm4(*this) = DirectX::XMMatrixRotationQuaternion(quat.vec);
#else
		cast_m3x4(*this).set(quat);
#endif
		pos = translation;
		return *this;
	}
	inline m4x4& m4x4::set(v4 const& axis, float angle, v4 const& translation)
	{
		assert(IsNormal3(axis) && "'axis' should be normalised");
#if PR_MATHS_USE_DIRECTMATH
		dxm4(*this) = DirectX::XMMatrixRotationNormal(axis.vec, angle);
#else
		cast_m3x4(*this).set(axis, angle);
#endif
		pos = translation;
		return *this;
	}
	inline m4x4& m4x4::set(v4 const& angular_displacement, v4 const& translation)
	{
		cast_m3x4(*this).set(angular_displacement);
		pos = translation;
		return *this;
	}
	inline m4x4& m4x4::set(v4 const& from, v4 const& to, v4 const& translation)
	{
		assert(IsNormal3(from) && IsNormal3(to) && "'from' and 'to' should be normalised");

		float cos_angle = Dot3(from, to);   // Cos angle
		if (cos_angle >= 1.0f - pr::maths::tiny)
		{
			x = pr::v4XAxis;
			y = pr::v4YAxis;
			z = pr::v4ZAxis;
		}
		else if (cos_angle <= pr::maths::tiny - 1.0f)
		{
			x = -pr::v4XAxis;
			y = -pr::v4YAxis;
			z = -pr::v4ZAxis;
		}
		else
		{
			v4 axis_sine_angle  = Cross3(from, to); // Axis multiplied by sine of the angle
			v4 axis_norm        = Normalise3(axis_sine_angle);
			cast_m3x4(*this).set(axis_norm, axis_sine_angle, cos_angle);
		}
		pos = translation;
		return *this;
	}
	inline m4x4& m4x4::set(float pitch,  float yaw, float roll, v4 const& translation)
	{
		cast_m3x4(*this).set(pitch, yaw, roll);
		pos = translation;
		return *this;
	}
	inline m4x4& m4x4::set(float const* mat)
	{
		x.set(mat);
		y.set(mat +  4);
		z.set(mat +  8);
		w.set(mat + 12);
		return *this;
	}

	// Assignment operators
	inline m4x4& operator += (m4x4& lhs, float rhs)            { lhs.x += rhs  ; lhs.y += rhs  ; lhs.z += rhs  ; lhs.w += rhs  ; return lhs; }
	inline m4x4& operator -= (m4x4& lhs, float rhs)            { lhs.x -= rhs  ; lhs.y -= rhs  ; lhs.z -= rhs  ; lhs.w -= rhs  ; return lhs; }
	inline m4x4& operator += (m4x4& lhs, m4x4 const& rhs)      { lhs.x += rhs.x; lhs.y += rhs.y; lhs.z += rhs.z; lhs.w += rhs.w; return lhs; }
	inline m4x4& operator -= (m4x4& lhs, m4x4 const& rhs)      { lhs.x -= rhs.x; lhs.y -= rhs.y; lhs.z -= rhs.z; lhs.w -= rhs.w; return lhs; }
	inline m4x4& operator *= (m4x4& lhs, float s)              { lhs.x *= s    ; lhs.y *= s    ; lhs.z *= s    ; lhs.w *= s    ; return lhs; }
	inline m4x4& operator /= (m4x4& lhs, float s)              { lhs.x /= s    ; lhs.y /= s    ; lhs.z /= s    ; lhs.w /= s    ; return lhs; }
	inline m4x4& operator += (m4x4& lhs, m3x4 const& rhs)      { lhs.x += rhs.x; lhs.y += rhs.y; lhs.z += rhs.z; return lhs; }
	inline m4x4& operator -= (m4x4& lhs, m3x4 const& rhs)      { lhs.x -= rhs.x; lhs.y -= rhs.y; lhs.z -= rhs.z; return lhs; }

	// Binary operators
	inline m4x4 operator + (m4x4 const& lhs, float rhs)        { m4x4 m = lhs; return m += rhs; }
	inline m4x4 operator - (m4x4 const& lhs, float rhs)        { m4x4 m = lhs; return m -= rhs; }
	inline m4x4 operator + (float lhs, m4x4 const& rhs)        { m4x4 m = rhs; return m += lhs; }
	inline m4x4 operator - (float lhs, m4x4 const& rhs)        { m4x4 m = rhs; return m -= lhs; }
	inline m4x4 operator + (m4x4 const& lhs, m4x4 const& rhs)  { m4x4 m = lhs; return m += rhs; }
	inline m4x4 operator - (m4x4 const& lhs, m4x4 const& rhs)  { m4x4 m = lhs; return m -= rhs; }
	inline m4x4 operator * (m4x4 const& lhs, float rhs)        { m4x4 m = lhs; return m *= rhs; }
	inline m4x4 operator * (float lhs, m4x4 const& rhs)        { m4x4 m = rhs; return m *= lhs; }
	inline m4x4 operator / (m4x4 const& lhs, float rhs)        { m4x4 m = lhs; return m /= rhs; }

	inline m4x4 operator * (m4x4 const& lhs, m4x4 const& rhs)
	{
		#if PR_MATHS_USE_DIRECTMATH
			m4x4 ans;
			dxm4(ans) = DirectX::XMMatrixMultiply(dxm4(rhs), dxm4(lhs));
			return ans;
		#else
			m4x4 ans, tlhs = GetTranspose4x4(lhs);
			ans.x.x = Dot4(tlhs.x, rhs.x);
			ans.y.x = Dot4(tlhs.x, rhs.y);
			ans.z.x = Dot4(tlhs.x, rhs.z);
			ans.w.x = Dot4(tlhs.x, rhs.w);

			ans.x.y = Dot4(tlhs.y, rhs.x);
			ans.y.y = Dot4(tlhs.y, rhs.y);
			ans.z.y = Dot4(tlhs.y, rhs.z);
			ans.w.y = Dot4(tlhs.y, rhs.w);

			ans.x.z = Dot4(tlhs.z, rhs.x);
			ans.y.z = Dot4(tlhs.z, rhs.y);
			ans.z.z = Dot4(tlhs.z, rhs.z);
			ans.w.z = Dot4(tlhs.z, rhs.w);

			ans.x.w = Dot4(tlhs.w, rhs.x);
			ans.y.w = Dot4(tlhs.w, rhs.y);
			ans.z.w = Dot4(tlhs.w, rhs.z);
			ans.w.w = Dot4(tlhs.w, rhs.w);
			return ans;
		#endif
	}
	inline v4 operator * (m4x4 const& lhs, v4 const& rhs)
	{
		#if PR_MATHS_USE_DIRECTMATH
			v4 ans;
			dxv4(ans) = DirectX::XMVector4Transform(dxv4(rhs), dxm4(lhs));
			return ans;
		#else
			v4 ans; m4x4 tlhs = GetTranspose4x4(lhs);
			ans.x = Dot4(tlhs.x, rhs);
			ans.y = Dot4(tlhs.y, rhs);
			ans.z = Dot4(tlhs.z, rhs);
			ans.w = Dot4(tlhs.w, rhs);
			return ans;
		#endif
	}

	// Unary operators
	inline m4x4 operator + (m4x4 const& mat) { return mat; }
	inline m4x4 operator - (m4x4 const& mat) { m4x4 ans = {-mat.x, -mat.y, -mat.z, -mat.w}; return ans; }

	// Equality operators
	inline bool FEql(m4x4 const& lhs, m4x4 const& rhs, float tol)  { return FEql4(lhs.x, rhs.x, tol) && FEql4(lhs.y, rhs.y, tol) && FEql4(lhs.z, rhs.z, tol) && FEql4(lhs.pos, rhs.pos, tol); }
	inline bool FEqlZero(m4x4 const& lhs, float tol)               { return FEqlZero4(lhs.x, tol) && FEqlZero4(lhs.y, tol) && FEqlZero4(lhs.z, tol) && FEqlZero4(lhs.pos, tol); }
	inline bool operator == (m4x4 const& lhs, m4x4 const& rhs)     { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (m4x4 const& lhs, m4x4 const& rhs)     { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (m4x4 const& lhs, m4x4 const& rhs)     { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (m4x4 const& lhs, m4x4 const& rhs)     { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (m4x4 const& lhs, m4x4 const& rhs)     { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (m4x4 const& lhs, m4x4 const& rhs)     { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// DirectXMath conversion functions
	#if PR_MATHS_USE_DIRECTMATH
	inline DirectX::XMMATRIX const& dxm4(m4x4 const& m) { assert(maths::is_aligned(&m)); return reinterpret_cast<DirectX::XMMATRIX const&>(m); }
	inline DirectX::XMMATRIX&       dxm4(m4x4&       m) { assert(maths::is_aligned(&m)); return reinterpret_cast<DirectX::XMMATRIX&>(m); }
	#endif

	// Conversion functions between vector types
	inline m3x4 const& cast_m3x4(m4x4 const& mat) { return reinterpret_cast<m3x4 const&>(mat); }
	inline m3x4&       cast_m3x4(m4x4& mat)       { return reinterpret_cast<m3x4&>(mat); }

	// Zero the matrix
	inline m4x4& Zero(m4x4& mat)
	{
		return mat = m4x4Zero;
	}

	// Return a m4x4 from this m3x4
	inline m4x4 Getm4x4(m3x4 const& mat)
	{
		return m4x4::make(mat.x, mat.y, mat.z, v4Origin);
	}

	// Return true if 'm' is finite
	inline bool IsFinite(m4x4 const& m)
	{
		return IsFinite(m.x) && IsFinite(m.y) && IsFinite(m.z) && IsFinite(m.w);
	}
	inline bool IsFinite(m4x4 const& m, float max_value)
	{
		return IsFinite(m.x, max_value) && IsFinite(m.y, max_value) && IsFinite(m.z, max_value) && IsFinite(m.w, max_value);
	}

	// Return true if 'mat' is an affine transform
	inline bool IsAffine(pr::m4x4 const& mat)
	{
		return
			mat.x.w == 0.0f &&
			mat.y.w == 0.0f &&
			mat.z.w == 0.0f &&
			mat.w.w == 1.0f;
	}

	// Return the determinant of the rotation part of this matrix
	inline float Determinant3(m4x4 const& mat)
	{
		return Triple3(mat.x, mat.y, mat.z);
	}

	// Return the 4x4 determinant of the affine transform 'mat'
	inline float DeterminantFast4(pr::m4x4 const& mat)
	{
		assert(IsAffine(mat) && "'mat' must be an affine transform to use this function");
		return
			(mat.x.x * mat.y.y * mat.z.z) +
			(mat.x.y * mat.y.z * mat.z.x) +
			(mat.x.z * mat.y.x * mat.z.y) -
			(mat.x.z * mat.y.y * mat.z.x) -
			(mat.x.y * mat.y.x * mat.z.z) -
			(mat.x.x * mat.y.z * mat.z.y);
	}

	// Return the 4x4 determinant of the arbitrary transform 'mat'
	inline float Determinant4(pr::m4x4 const& mat)
	{
#if PR_MATHS_USE_DIRECTMATH
		v4 det;
		dxv4(det) = DirectX::XMMatrixDeterminant(dxm4(mat));
		return det.x;
#else
		float c1 = (mat.z.z * mat.w.w) - (mat.z.w * mat.w.z);
		float c2 = (mat.z.y * mat.w.w) - (mat.z.w * mat.w.y);
		float c3 = (mat.z.y * mat.w.z) - (mat.z.z * mat.w.y);
		float c4 = (mat.z.x * mat.w.w) - (mat.z.w * mat.w.x);
		float c5 = (mat.z.x * mat.w.z) - (mat.z.z * mat.w.x);
		float c6 = (mat.z.x * mat.w.y) - (mat.z.y * mat.w.x);
		return
			mat.x.x * (mat.y.y*c1 - mat.y.z*c2 + mat.y.w*c3) -
			mat.x.y * (mat.y.x*c1 - mat.y.z*c4 + mat.y.w*c5) +
			mat.x.z * (mat.y.x*c2 - mat.y.y*c4 + mat.y.w*c6) -
			mat.x.w * (mat.y.x*c3 - mat.y.y*c5 + mat.y.z*c6);
#endif
	}

	// Returns the sum of the first 3 diagonal elements of 'mat'
	inline float Trace3(m4x4 const& mat)
	{
		return mat.x.x + mat.y.y + mat.z.z;
	}

	// Returns the sum of the diagonal elements of 'mat'
	inline float Trace4(m4x4 const& mat)
	{
		return mat.x.x + mat.y.y + mat.z.z + mat.w.w;
	}

	// The kernel of the matrix
	inline v4 Kernel(m4x4 const& mat)
	{
		return v4::make(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}

	// Transpose the matrix
	inline m4x4& Transpose4x4(m4x4& mat)
	{
		#if PR_MATHS_USE_DIRECTMATH
		dxm4(mat) = DirectX::XMMatrixTranspose(dxm4(mat));
		#elif PR_MATHS_USE_INTRINSICS
		_MM_TRANSPOSE4_PS(mat.x.vec, mat.y.vec, mat.z.vec, mat.w.vec);
		#else
		{
			Swap(mat.x.y, mat.y.x);
			Swap(mat.x.z, mat.z.x);
			Swap(mat.x.w, mat.w.x);
			Swap(mat.y.z, mat.z.y);
			Swap(mat.y.w, mat.w.y);
			Swap(mat.z.w, mat.w.z);
		}
		#endif
		return mat;
	}

	// Transpose the rotation part of a matrix
	inline m4x4& Transpose3x3(m4x4& mat)
	{
		Swap(mat.x.y, mat.y.x);
		Swap(mat.x.z, mat.z.x);
		Swap(mat.y.z, mat.z.y);
		return mat;
	}

	// Return the 4x4 transpose of 'mat'
	inline m4x4 GetTranspose4x4(m4x4 const& mat)
	{
		m4x4 m = mat;
		return Transpose4x4(m);
	}

	// Return the 3x3 transpose of 'mat'
	inline m4x4 GetTranspose3x3(m4x4 const& mat)
	{
		m4x4 m = mat;
		return Transpose3x3(m);
	}

	// Returns 'mat' with no translation component
	inline m4x4 GetRotation(m4x4 const& mat)
	{
		m4x4 m = mat;
		m.pos = v4Origin;
		return m;
	}

	// True if 'mat' has an inverse
	inline bool IsInvertable(m4x4 const& mat)
	{
		return !FEql(Determinant4(mat), 0.0f);
	}

	// Invert this matrix
	inline m4x4 Invert(m4x4 const& mat_)
	{
		m4x4 mat = mat_;
#if PR_MATHS_USE_DIRECTMATH
		v4 det;
		dxv4(det) = DirectX::XMMatrixDeterminant(dxm4(mat));
		dxm4(mat) = DirectX::XMMatrixInverse(&dxv4(det), dxm4(mat));
		assert(det.x != 0.f && "Matrix has no inverse");
#else
		m4x4  A = GetTranspose4x4(mat); // Take the transpose so that row operations are faster
		m4x4& B = mat; B = m4x4Identity;

		// Loop through columns
		for (int j = 0; j < 4; ++j)
		{
			// Select pivot element: maximum magnitude in this column1
			v4 col = Abs(A.row(j)); // Remember, we've transposed '*this'
			int pivot = j;
			for (int i = pivot + 1; i != 4; ++i)
			{
				if (col[i] > col[pivot])
					pivot = i;
			}
			if (col[pivot] < maths::tiny)
			{
				assert(false && "Matrix has no inverse");
				return mat;
			}

			// Interchange rows to put pivot element on the diagonal
			if (pivot != j) // skip if already on diagonal
			{
				Swap(A[j], A[pivot]);
				Swap(B[j], B[pivot]);
			}

			// Divide row by pivot element
			float scale = A[j][j];
			if (scale != 1.0f)  // skip if already equal to 1
			{
				A[j] /= scale;
				B[j] /= scale;
				// now the pivot element is 1
			}

			// Subtract this row from others to make the rest of column j zero
			j == 0 || (scale = A[0][j], A[0] -= scale * A[j], B[0] -= scale * B[j], true);
			j == 1 || (scale = A[1][j], A[1] -= scale * A[j], B[1] -= scale * B[j], true);
			j == 2 || (scale = A[2][j], A[2] -= scale * A[j], B[2] -= scale * B[j], true);
			j == 3 || (scale = A[3][j], A[3] -= scale * A[j], B[3] -= scale * B[j], true);
		}
		// When these operations have been completed, A should have been transformed to the identity matrix
		// and B should have been transformed into the inverse of the original A
		Transpose4x4(B);
#endif
		return mat;
	}

	// Find the inverse of this matrix. It must be orthonormal
	inline m4x4 InvertFast(m4x4 const& mat_)
	{
		assert(IsOrthonormal(mat_) && "Matrix is not orthonormal");
		m4x4 mat = GetTranspose3x3(mat_);
		mat.pos.x = -Dot3(mat_.x, mat_.pos);
		mat.pos.y = -Dot3(mat_.y, mat_.pos);
		mat.pos.z = -Dot3(mat_.z, mat_.pos);
		return mat;
	}

	// Orthonormalises the rotation component of the matrix
	inline m4x4 Orthonorm(m4x4 const& mat)
	{
		m4x4 m = mat;
		m.x = Normalise3(m.x);
		m.y = Normalise3(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		assert(IsOrthonormal(m));
		return m;
	}

	// Return true if this matrix is orthonormal
	inline bool IsOrthonormal(m4x4 const& mat)
	{
		return  FEql(Length3Sq(mat.x), 1.0f) &&
			FEql(Length3Sq(mat.y), 1.0f) &&
			FEql(Length3Sq(mat.z), 1.0f) &&
			FEql(Abs(Determinant3(mat)), 1.0f);//Zero3(Cross3(mat.x, mat.y) - mat.z);
	}

	// Return the axis and angle of a rotation matrix
	inline void GetAxisAngle(m4x4 const& mat, v4& axis, float& angle)
	{
		assert(IsOrthonormal(mat) && "Matrix is not pure rotation");

		angle = pr::ACos(0.5f * (Trace3(mat) - 1.0f));
		axis = 1000.0f * Kernel(m4x4Identity - mat);
		if (IsZero3(axis))  { axis = v4XAxis; angle = 0.0f; return; }
		axis = Normalise3(axis);
		if (IsZero3(axis))  { axis = v4XAxis; angle = 0.0f; return; }

		// Determine the correct sign of the angle
		v4 vec = CreateNotParallelTo(axis);
		v4 X = vec - Dot3(axis, vec) * axis;
		v4 Xprim = mat * X;
		v4 XcXp = Cross3(X, Xprim);
		if (Dot3(XcXp, axis) < 0.0f) angle = -angle;
	}
	inline m4x4 Abs(m4x4 const& mat)
	{
		return m4x4::make(Abs(mat.x), Abs(mat.y), Abs(mat.z), Abs(mat.pos));
	}
	inline m4x4 Sqr(m4x4 const& mat)
	{
		return mat * mat;
	}
	inline m4x4 Translation4x4(v3 const& xyz)
	{
		m4x4 mat = m4x4Identity;
		mat.pos.set(xyz, 1.0f);
		return mat;
	}
	inline m4x4 Translation4x4(v4 const& xyz)
	{
		assert(xyz.w == 1.0f && "translation should be a position vector");
		m4x4 mat = m4x4Identity;
		mat.pos = xyz;
		return mat;
	}
	inline m4x4 Translation4x4(float x, float y, float z)
	{
		m4x4 mat = m4x4Identity;
		mat.pos.set(x,y,z,1.0f);
		return mat;
	}
	inline m4x4 Rotation4x4(float pitch, float yaw, float roll, v4 const& translation)
	{
		return m4x4::make(pitch, yaw, roll, translation);
	}
	inline m4x4 Rotation4x4(v3 const& axis, float angle, v4 const& translation)
	{
		return m4x4::make(v4::make(axis, 0.0f), angle, translation);
	}
	inline m4x4 Rotation4x4(v4 const& axis, float angle, v4 const& translation)
	{
		return m4x4::make(axis, angle, translation);
	}
	inline m4x4 Rotation4x4(v4 const& angular_displacement, v4 const& translation)
	{
		return m4x4::make(angular_displacement, translation);
	}
	inline m4x4 Rotation4x4(v4 const& from, v4 const& to, v4 const& translation)
	{
		return m4x4::make(from, to, translation);
	}
	inline m4x4 Rotation4x4(Quat const& quat, v4 const& translation)
	{
		return m4x4::make(quat, translation);
	}
	inline m4x4 Scale4x4(float scale, v4 const& translation)
	{
		m4x4 mat = {};
		mat.x.x = mat.y.y = mat.z.z = scale;
		mat.pos = translation;
		return mat;
	}
	inline m4x4 Scale4x4(float sx, float sy, float sz, v4 const& translation)
	{
		m4x4 mat = {};
		mat.x.x = sx;
		mat.y.y = sy;
		mat.z.z = sz;
		mat.pos = translation;
		return mat;
	}
	inline m4x4 Shear4x4(float sxy, float sxz, float syx, float syz, float szx, float szy, v4 const& translation)
	{
		return m4x4::make(Shear3x3(sxy, sxz, syx, syz, szx, szy), translation);
	}

	// Orientation matrix to "look" at a point
	inline m4x4 LookAt(v4 const& eye, v4 const& at, v4 const& up)
	{
		assert(eye.w == 1.0f && at.w == 1.0f && up.w == 0.0f && "Invalid position/direction vectors passed to Lookat");
		assert(!pr::Parallel(at - eye, up) && "Lookat point and up axis are aligned");
		m4x4 mat;
		mat.z = Normalise3(eye - at);
		mat.x = Normalise3(Cross3(up, mat.z));
		mat.y = Cross3(mat.z, mat.x);
		mat.pos = eye;
		return mat;
	}

	// Construct an orthographic projection matrix
	inline m4x4 ProjectionOrthographic(float w, float h, float zn, float zf, bool righthanded)
	{
		auto rh = Sign<float>(righthanded);
		m4x4 mat = {};
		mat.x.x = 2.0f / w;
		mat.y.y = 2.0f / h;
		mat.z.z = rh / (zn - zf);
		mat.w.w = 1.0f;
		mat.w.z = rh * zn / (zn - zf);
		return mat;
	}

	// Construct a perspective projection matrix
	inline m4x4 ProjectionPerspective(float w, float h, float zn, float zf, bool righthanded)
	{
		auto rh = Sign<float>(righthanded);
		m4x4 mat = {};
		mat.x.x = 2.0f * zn / w;
		mat.y.y = 2.0f * zn / h;
		mat.z.w = -rh;
		mat.z.z = rh * zf / (zn - zf);
		mat.w.z = zn * zf / (zn - zf);
		return mat;
	}

	// Construct a perspective projection matrix offset from the centre
	inline m4x4 ProjectionPerspective(float l, float r, float t, float b, float zn, float zf, bool righthanded)
	{
		auto rh = Sign<float>(righthanded);
		m4x4 mat = {};
		mat.x.x = 2.0f * zn / (r - l);
		mat.y.y = 2.0f * zn / (t - b);
		mat.z.x = rh * (r + l) / (r - l);
		mat.z.y = rh * (t + b) / (t - b);
		mat.z.w = -rh;
		mat.z.z = rh * zf / (zn - zf);
		mat.w.z = zn * zf / (zn - zf);
		return mat;
	}

	// Construct a perspective projection matrix using field of view
	inline m4x4 ProjectionPerspectiveFOV(float fovY, float aspect, float zn, float zf, bool righthanded)
	{
		auto rh = Sign<float>(righthanded);
		m4x4 mat = {};
		mat.y.y = 1.0f / pr::Tan(fovY/2);
		mat.x.x = mat.y.y / aspect;
		mat.z.w = -rh;
		mat.z.z = rh * zf / (zn - zf);
		mat.w.z = zn * zf / (zn - zf);
		return mat;
	}

	// Return the cross product matrix for 'vec'. This matrix can be used to take the
	// cross product of another vector: e.g. Cross(v1, v2) == CrossProductMatrix4x4(v1) * v2
	inline m4x4 CrossProductMatrix4x4(v4 const& vec)
	{
		return m4x4::make(
			v4::make(+0.0f,  vec.z, -vec.y, 0.0f),
			v4::make(-vec.z,   0.0f,  vec.x, 0.0f),
			v4::make(+vec.y, -vec.x,   0.0f, 0.0f),
			v4Zero);
	}

	// Make an object to world transform from a direction vector and position
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be choosen.
	inline m4x4 OriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& position)
	{
		return m4x4::make(OriFromDir(dir, axis, up), position);
	}

	// Make a scaled object to world transform from a direction vector and position
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	inline m4x4 ScaledOriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& position)
	{
		return m4x4::make(ScaledOriFromDir(dir, axis, up), position);
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using Denman-Beavers square root iteration. Should converge quadratically
	inline m4x4 Sqrt(m4x4 const& mat)
	{
		m4x4 Y = mat;           // Converges to mat^0.5
		m4x4 Z = m4x4Identity;  // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			m4x4 Y_next = 0.5 * (Y + Invert(Z));
			m4x4 Z_next = 0.5 * (Z + Invert(Y));
			Y = Y_next;
			Z = Z_next;
		}
		return Y;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_matrix4x4)
		{
			using namespace pr;
			{
				m4x4 m1 = m4x4Identity;
				m4x4 m2 = m4x4Identity;
				m4x4 m3 = m1 * m2;
				PR_CHECK(FEql(m3, m4x4Identity), true);
			}
			{//m4x4Translation
				m4x4 m2;
				m4x4 m1 = m4x4::make(v4XAxis, v4YAxis, v4ZAxis, v4::make(1.0f, 2.0f, 3.0f, 1.0f));
				m2 = Translation4x4(v3::make(1.0f, 2.0f, 3.0f));
				PR_CHECK(FEql(m1, m2), true);
				m2 = Translation4x4(v4::make(1.0f, 2.0f, 3.0f, 1.0f));
				PR_CHECK(FEql(m1, m2), true);
			}
			{//m4x4CreateFrom
				v4 V1 = Random3(0.0f, 10.0f, 1.0f);
				m4x4 a2b; a2b.set(Random3N(0.0f), rand::fltc(0, maths::tau_by_2), Random3(0.0f, 10.0f, 1.0f));
				m4x4 b2c; b2c.set(Random3N(0.0f), rand::fltc(0, maths::tau_by_2), Random3(0.0f, 10.0f, 1.0f));
				PR_CHECK(IsOrthonormal(a2b), true);
				PR_CHECK(IsOrthonormal(b2c), true);
				v4 V2 = a2b * V1;
				v4 V3 = b2c * V2; V3;
				m4x4 a2c = b2c * a2b;
				v4 V4 = a2c * V1; V4;
				PR_CHECK(FEql4(V3, V4), true);
			}
			{//m4x4CreateFrom2
				m4x4 m1 = Rotation4x4(1.0f, 0.5f, 0.7f, v4Origin);
				m4x4 m2 = m4x4::make(Quat::make(1.0f, 0.5f, 0.7f), v4Origin);
				PR_CHECK(IsOrthonormal(m1), true);
				PR_CHECK(IsOrthonormal(m2), true);
				PR_CHECK(FEql(m1, m2), true);

				float ang = rand::fltc(0.0f,1.0f);
				v4 axis = Random3N(0.0f);
				m1 = Rotation4x4(axis, ang, v4Origin);
				m2 = m4x4::make(Quat::make(axis, ang), v4Origin);
				PR_CHECK(IsOrthonormal(m1), true);
				PR_CHECK(IsOrthonormal(m2), true);
				PR_CHECK(FEql(m1, m2), true);
			}
			{//m4x4CreateFrom3
				m4x4 a2b;
				a2b.set(Random3N(0.0f), rand::fltc(0.0f,1.0f), Random3(0.0f, 10.0f, 1.0f));
				
				m4x4 b2a = Invert(a2b);
				m4x4 a2a = b2a * a2b;
				PR_CHECK(FEql(m4x4Identity, a2a), true);

				m4x4 b2a_fast = InvertFast(a2b);
				PR_CHECK(FEql(b2a_fast, b2a), true);
			}
			{//m4x4Orthonormalise
				m4x4 a2b;
				a2b.x.set(-2.0f, 3.0f, 1.0f, 0.0f);
				a2b.y.set(4.0f,-1.0f, 2.0f, 0.0f);
				a2b.z.set(1.0f,-2.0f, 4.0f, 0.0f);
				a2b.w.set(1.0f, 2.0f, 3.0f, 1.0f);
				PR_CHECK(IsOrthonormal(Orthonorm(a2b)), true);
			}
		}
	}
}
#endif

#endif
