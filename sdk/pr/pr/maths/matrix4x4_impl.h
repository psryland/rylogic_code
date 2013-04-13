//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX4X4_IMPL_H
#define PR_MATHS_MATRIX4X4_IMPL_H

#include "pr/maths/matrix4x4.h"

namespace pr
{
	inline m4x4 m4x4::make(v4 const& x_, v4 const& y_, v4 const& z_, v4 const& w_)      { m4x4 m; return m.set(x_, y_, z_, w_); }
	inline m4x4 m4x4::make(m3x3 const& ori, v4 const& translation)                      { m4x4 m; return m.set(ori, translation); }
	inline m4x4 m4x4::make(Quat const& quat, v4 const& translation)                     { m4x4 m; return m.set(quat, translation); }
	inline m4x4 m4x4::make(v4 const& axis, float angle, v4 const& translation)          { m4x4 m; return m.set(axis, angle, translation); }
	inline m4x4 m4x4::make(v4 const& angular_displacement, v4 const& translation)       { m4x4 m; return m.set(angular_displacement, translation); }
	inline m4x4 m4x4::make(v4 const& from, v4 const& to, v4 const& translation)         { m4x4 m; return m.set(from, to, translation); }
	inline m4x4 m4x4::make(float pitch,  float yaw, float roll, v4 const& translation)  { m4x4 m; return m.set(pitch,  yaw, roll, translation); }
	inline m4x4 m4x4::make(float const* mat)                                            { m4x4 m; return m.set(mat); }
	
	inline m4x4& m4x4::set(v4 const& x_, v4 const& y_, v4 const& z_, v4 const& w_)
	{
		x = x_;
		y = y_;
		z = z_;
		w = w_;
		return *this;
	}
	
	inline m4x4& m4x4::set(m3x3 const& ori, v4 const& translation)
	{
		cast_m3x3(*this) = ori;
		pos = translation;
		return *this;
	}
	
	inline m4x4& m4x4::set(Quat const& quat, v4 const& translation)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal4(quat), "'quat' should be a normalised quaternion");
		#if PR_MATHS_USE_D3DX
		D3DXMatrixRotationQuaternion(&d3dm4(*this), &d3dq(quat));
		#else
		cast_m3x3(*this).set(quat);
		#endif
		pos = translation;
		return *this;
	}
	
	inline m4x4& m4x4::set(v4 const& axis, float angle, v4 const& translation)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal3(axis), "'axis' should be normalised");
		#if PR_MATHS_USE_D3DX
		D3DXMatrixRotationAxis(&d3dm4(*this), &d3dv3(axis), angle);
		#else
		cast_m3x3(*this).set(axis, angle);
		#endif
		pos = translation;
		return *this;
	}
	
	inline m4x4& m4x4::set(v4 const& angular_displacement, v4 const& translation)
	{
		cast_m3x3(*this).set(angular_displacement);
		pos = translation;
		return *this;
	}
	
	inline m4x4& m4x4::set(v4 const& from, v4 const& to, v4 const& translation)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal3(from) && IsNormal3(to), "'from' and 'to' should be normalised");
		
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
			v4 axis_norm        = GetNormal3(axis_sine_angle);
			cast_m3x3(*this).set(axis_norm, axis_sine_angle, cos_angle);
		}
		pos = translation;
		return *this;
	}
	
	inline m4x4& m4x4::set(float pitch,  float yaw, float roll, v4 const& translation)
	{
		cast_m3x3(*this).set(pitch, yaw, roll);
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
	
	inline m4x4&        m4x4::zero()                              { return *this = m4x4Zero; }
	inline m4x4&        m4x4::identity()                          { return *this = m4x4Identity; }
	
	// Assignment operators
	inline m4x4& operator += (m4x4& lhs, float rhs)            { lhs.x += rhs; lhs.y += rhs; lhs.z += rhs; lhs.w += rhs; return lhs; }
	inline m4x4& operator -= (m4x4& lhs, float rhs)            { lhs.x -= rhs; lhs.y -= rhs; lhs.z -= rhs; lhs.w -= rhs; return lhs; }
	inline m4x4& operator += (m4x4& lhs, m4x4 const& rhs)      { lhs.x += rhs.x; lhs.y += rhs.y; lhs.z += rhs.z; lhs.w += rhs.w; return lhs; }
	inline m4x4& operator -= (m4x4& lhs, m4x4 const& rhs)      { lhs.x -= rhs.x; lhs.y -= rhs.y; lhs.z -= rhs.z; lhs.w -= rhs.w; return lhs; }
	inline m4x4& operator *= (m4x4& lhs, float s)              { lhs.x *= s; lhs.y *= s; lhs.z *= s; lhs.w *= s; return lhs; }
	inline m4x4& operator /= (m4x4& lhs, float s)              { lhs.x /= s; lhs.y /= s; lhs.z /= s; lhs.w /= s; return lhs; }
	inline m4x4& operator += (m4x4& lhs, m3x3 const& rhs)      { lhs.x += rhs.x; lhs.y += rhs.y; lhs.z += rhs.z; return lhs; }
	inline m4x4& operator -= (m4x4& lhs, m3x3 const& rhs)      { lhs.x -= rhs.x; lhs.y -= rhs.y; lhs.z -= rhs.z; return lhs; }
	
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
	
	#pragma warning (push)
	#pragma warning (disable : 4701) // 'ans' may not be fully initialised
	inline m4x4 operator * (m4x4 const& lhs, m4x4 const& rhs)
	{
		m4x4 ans;
		#if PR_MATHS_USE_D3DX
		D3DXMatrixMultiply(&d3dm4(ans), &d3dm4(rhs), &d3dm4(lhs));
		#else
		m4x4 lhs_t = GetTranspose4x4(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int j = 0; j < 4; ++j)
		{
			#pragma PR_OMP_PARALLEL_FOR
			for (int i = 0; i < 4; ++i)
				ans[j][i] = Dot4(lhs_t[i], rhs[j]);
		}
		#endif
		return ans;
	}
	inline v4 operator * (m4x4 const& lhs, v4 const& rhs)
	{
		v4 ans;
		#if PR_MATHS_USE_D3DX
		D3DXVec4Transform(&d3dv4(ans), &d3dv4(rhs), &d3dm4(lhs));
		#else
		m4x4 lhs_t = GetTranspose4x4(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int i = 0; i < 4; ++i)
			ans[i] = Dot4(lhs_t[i], rhs);
		#endif
		return ans;
	}
	#pragma warning (pop)
	
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
	
	// D3DX conversion functions
	#if PR_MATHS_USE_D3DX
	inline D3DXMATRIX const& d3dm4(m4x4 const& m) { return reinterpret_cast<D3DXMATRIX const&>(m); }
	inline D3DXMATRIX&       d3dm4(m4x4&       m) { return reinterpret_cast<D3DXMATRIX&>(m); }
	#endif
	
	// Conversion functions between vector types
	inline m3x3 const& cast_m3x3(m4x4 const& mat) { return reinterpret_cast<m3x3 const&>(mat); }
	inline m3x3&       cast_m3x3(m4x4& mat)       { return reinterpret_cast<m3x3&>(mat); }
	
	// Zero the matrix
	inline m4x4& Zero(m4x4& mat)
	{
		return mat.zero();
	}
	
	// Return a m4x4 from this m3x3
	inline m4x4 Getm4x4(m3x3 const& mat)
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
		PR_ASSERT(PR_DBG_MATHS, IsAffine(mat), "'mat' must be an affine transform to use this function");
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
		#if PR_MATHS_USE_D3DX
		return D3DXMatrixDeterminant(&d3dm4(mat));
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
	
	inline float Trace3(m4x4 const& mat)
	{
		return mat.x.x + mat.y.y + mat.z.z;
	}
	
	inline float Trace4(m4x4 const& mat)
	{
		return mat.x.x + mat.y.y + mat.z.z + mat.w.w;
	}
	
	inline v4 Kernel(m4x4 const& mat)
	{
		return v4::make(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}
	
	// Transpose the matrix
	inline m4x4& Transpose4x4(m4x4& mat)
	{
		#if PR_MATHS_USE_D3DX
		D3DXMatrixTranspose(&d3dm4(mat), &d3dm4(mat));
		#else
		#pragma PR_OMP_PARALLEL
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
	
	inline m4x4 GetTranspose4x4(m4x4 const& mat)
	{
		m4x4 m = mat;
		return Transpose4x4(m);
	}
	
	inline m4x4 GetTranspose3x3(m4x4 const& mat)
	{
		m4x4 m = mat;
		return Transpose3x3(m);
	}
	
	inline m4x4 GetRotation(m4x4 const& mat)
	{
		m4x4 m = mat;
		m.pos = v4Origin;
		return m;
	}
	
	inline bool IsInvertable(m4x4 const& mat)
	{
		return !FEql(Determinant4(mat), 0.0f);
	}
	
	// Invert this matrix
	inline m4x4& Inverse(m4x4& mat)
	{
#if PR_MATHS_USE_D3DX
		void* p = D3DXMatrixInverse(&d3dm4(mat), 0, &d3dm4(mat));
		PR_ASSERT(PR_DBG_MATHS, p, "Matrix has no inverse"); (void)p;
#else
		m4x4  A = GetTranspose4x4(mat); // Take the transpose so that row operations are faster
		m4x4& B = mat; B.identity();
		
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
				PR_ASSERT(PR_DBG_MATHS, false, "Matrix has no inverse");
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
	inline m4x4 GetInverse(m4x4 const& mat)
	{
		m4x4 m = mat;
		return Inverse(m);
	}
	
	// Find the inverse of this matrix. It must be orthonormal
	inline m4x4& InverseFast(m4x4& mat)
	{
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(mat), "Matrix is not orthonormal");
		v4 translation = mat.pos;
		Transpose3x3(mat);
		mat.pos.x = -(translation.x * mat.x.x + translation.y * mat.y.x + translation.z * mat.z.x);
		mat.pos.y = -(translation.x * mat.x.y + translation.y * mat.y.y + translation.z * mat.z.y);
		mat.pos.z = -(translation.x * mat.x.z + translation.y * mat.y.z + translation.z * mat.z.z);
		return mat;
	}
	
	// Return the inverse of this matrix. It must be orthonormal
	inline m4x4 GetInverseFast(m4x4 const& mat)
	{
		m4x4 m = mat;
		return InverseFast(m);
	}
	
	// Orthonormalises the rotation component of the matrix
	inline m4x4& Orthonormalise(m4x4& mat)
	{
		Normalise3(mat.x);
		mat.y = GetNormal3(Cross3(mat.z, mat.x));
		mat.z = Cross3(mat.x, mat.y);
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(mat), "");
		return mat;
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
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(mat), "Matrix is not pure rotation");
		
		angle = pr::ACos(0.5f * (Trace3(mat) - 1.0f));
		axis = 1000.0f * Kernel(m4x4Identity - mat);
		if (IsZero3(axis))  { axis = v4XAxis; angle = 0.0f; return; }
		Normalise3(axis);
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
	inline m4x4& Translation(m4x4& mat, v3 const& xyz)
	{
		mat.identity();
		mat.pos.set(xyz, 1.0f);
		return mat;
	}
	inline m4x4& Translation(m4x4& mat, v4 const& xyz)
	{
		mat.identity();
		mat.pos = xyz;
		return mat;
	}
	inline m4x4& Translation(m4x4& mat, float x, float y, float z)
	{
		mat.identity();
		mat.pos.set(x, y, z, 1.0f);
		return mat;
	}
	inline m4x4 Translation(v3 const& xyz)
	{
		m4x4 m;
		return Translation(m, xyz);
	}
	inline m4x4 Translation(v4 const& xyz)
	{
		m4x4 m;
		return Translation(m, xyz);
	}
	inline m4x4 Translation(float x, float y, float z)
	{
		m4x4 m;
		return Translation(m, x, y, z);
	}
	inline m4x4& Rotation4x4(m4x4& mat, float pitch, float yaw, float roll, v4 const& translation)
	{
		return mat.set(pitch, yaw, roll, translation);
	}
	inline m4x4& Rotation4x4(m4x4& mat, v3 const& axis, float angle, v4 const& translation)
	{
		return mat.set(v4::make(axis, 0.0f), angle, translation);
	}
	inline m4x4& Rotation4x4(m4x4& mat, v4 const& axis, float angle, v4 const& translation)
	{
		return mat.set(axis, angle, translation);
	}
	inline m4x4& Rotation4x4(m4x4& mat, v4 const& angular_displacement, v4 const& translation)
	{
		return mat.set(angular_displacement, translation);
	}
	inline m4x4& Rotation4x4(m4x4& mat, v4 const& from, v4 const& to, v4 const& translation)
	{
		return mat.set(from, to, translation);
	}
	inline m4x4& Rotation4x4(m4x4& mat, Quat const& quat, v4 const& translation)
	{
		return mat.set(quat, translation);
	}
	inline m4x4 Rotation4x4(float pitch, float yaw, float roll, v4 const& translation)
	{
		return m4x4::make(pitch, yaw, roll, translation);
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
	inline m4x4& Scale4x4(m4x4& mat, float scale, v4 const& translation)
	{
		Zero(mat);
		mat.x.x = mat.y.y = mat.z.z = scale;
		mat.pos = translation;
		return mat;
	}
	inline m4x4& Scale4x4(m4x4& mat, float sx, float sy, float sz, v4 const& translation)
	{
		Zero(mat);
		mat.x.x = sx;
		mat.y.y = sy;
		mat.z.z = sz;
		mat.pos = translation;
		return mat;
	}
	inline m4x4 Scale4x4(float scale, v4 const& translation)
	{
		m4x4 m;
		return Scale4x4(m, scale, translation);
	}
	inline m4x4 Scale4x4(float sx, float sy, float sz, v4 const& translation)
	{
		m4x4 m;
		return Scale4x4(m, sx, sy, sz, translation);
	}
	
	inline m4x4& Shear4x4(m4x4& mat, float sxy, float sxz, float syx, float syz, float szx, float szy, v4 const& translation)
	{
		Shear3x3(cast_m3x3(mat), sxy, sxz, syx, syz, szx, szy);
		mat.pos = translation;
		return mat;
	}
	inline m4x4 Shear4x4(float sxy, float sxz, float syx, float syz, float szx, float szy, v4 const& translation)
	{
		m4x4 m;
		return Shear4x4(m, sxy, sxz, syx, syz, szx, szy, translation);
	}
	
	inline m4x4& LookAt(m4x4& mat, v4 const& eye, v4 const& at, v4 const& up)
	{
		PR_ASSERT(PR_DBG_MATHS, eye.w == 1.0f && at.w == 1.0f && up.w == 0.0f, "Invalid position/direction vectors passed to Lookat");
		PR_ASSERT(PR_DBG_MATHS, !pr::Parallel(at - eye, up), "Lookat point and up axis are aligned");
		mat.z = GetNormal3(eye - at);
		mat.x = GetNormal3(Cross3(up, mat.z));
		mat.y = Cross3(mat.z, mat.x);
		mat.pos = eye;
		return mat;
	}
	inline m4x4 LookAt(v4 const& eye, v4 const& at, v4 const& up)
	{
		m4x4 m;
		return LookAt(m, eye, at, up);
	}
	
	// Construct an orthographic projection matrix
	inline m4x4& ProjectionOrthographic(m4x4& mat, float w, float h, float Znear, float Zfar, bool righthanded)
	{
#if PR_MATHS_USE_D3DX
		if (righthanded) D3DXMatrixOrthoRH(&d3dm4(mat), w, h, Znear, Zfar);
		else             D3DXMatrixOrthoLH(&d3dm4(mat), w, h, Znear, Zfar);
#else
		float diff = Zfar - Znear;
		Zero(mat);
		mat.x.x = 2.0f / w;
		mat.y.y = 2.0f / h;
		mat.z.z = Sign<float>(!righthanded) / diff;
		mat.w.w = 1.0f;
		mat.w.z = -Znear / diff;
#endif
		return mat;
	}
	inline m4x4 ProjectionOrthographic(float w, float h, float Znear, float Zfar, bool righthanded)
	{
		m4x4 m;
		return ProjectionOrthographic(m, w, h, Znear, Zfar, righthanded);
	}
	
	// Construct a perspective projection matrix
	inline m4x4& ProjectionPerspective(m4x4& mat, float w, float h, float Znear, float Zfar, bool righthanded)
	{
#if PR_MATHS_USE_D3DX
		if (righthanded) D3DXMatrixPerspectiveRH(&d3dm4(mat), w, h, Znear, Zfar);
		else             D3DXMatrixPerspectiveLH(&d3dm4(mat), w, h, Znear, Zfar);
#else
		float zn   = 2.0f * Znear;
		float diff = Zfar - Znear;
		Zero(mat);
		mat.x.x = zn / w;
		mat.y.y = zn / h;
		mat.z.w = Sign<float>(!righthanded);
		mat.z.z = mat.z.w * Zfar / diff;
		mat.w.z = -Znear * Zfar / diff;
#endif
		return mat;
	}
	inline m4x4 ProjectionPerspective(float w, float h, float Znear, float Zfar, bool righthanded)
	{
		m4x4 m;
		return ProjectionPerspective(m, w, h, Znear, Zfar, righthanded);
	}
	
	// Construct a perspective projection matrix offset from the centre
	inline m4x4& ProjectionPerspective(m4x4& mat, float l, float r, float t, float b, float Znear, float Zfar, bool righthanded)
	{
#if PR_MATHS_USE_D3DX
		if (righthanded) D3DXMatrixPerspectiveOffCenterRH(&d3dm4(mat), l, r, b, t, Znear, Zfar);
		else             D3DXMatrixPerspectiveOffCenterLH(&d3dm4(mat), l, r, b, t, Znear, Zfar);
#else
		float zn   = 2.0f * Znear;
		float diff = Zfar - Znear;
		Zero(mat);
		mat.x.x = zn / (r - l);
		mat.y.y = zn / (t - b);
		mat.z.x = (l+r)/(l-r);
		mat.z.y = (t+b)/(b-t);
		mat.z.w = Sign<float>(!righthanded);
		mat.z.z = mat.z.w * Zfar / diff;
		mat.w.z = -Znear * Zfar / diff;
#endif
		return mat;
	}
	inline m4x4 ProjectionPerspective(float l, float r, float t, float b, float Znear, float Zfar, bool righthanded)
	{
		m4x4 m;
		return ProjectionPerspective(m, l, r, t, b, Znear, Zfar, righthanded);
	}
	
	// Construct a perspective projection matrix using field of view
	inline m4x4& ProjectionPerspectiveFOV(m4x4& mat, float fovY, float aspect, float Znear, float Zfar, bool righthanded)
	{
#if PR_MATHS_USE_D3DX
		if (righthanded) D3DXMatrixPerspectiveFovRH(&d3dm4(mat), fovY, aspect, Znear, Zfar);
		else             D3DXMatrixPerspectiveFovLH(&d3dm4(mat), fovY, aspect, Znear, Zfar);
#else
		float diff = Zfar - Znear;
		Zero(mat);
		mat.y.y = 1.0f / pr::Tan(fovY/2);
		mat.x.x = mat.y.y / aspect;
		mat.z.w = Sign<float>(!righthanded);
		mat.z.z = mat.z.w * Zfar / diff;
		mat.w.z = -Znear * Zfar / diff;
#endif
		return mat;
	}
	inline m4x4 ProjectionPerspectiveFOV(float fovY, float aspect, float Znear, float Zfar, bool righthanded)
	{
		m4x4 m;
		return ProjectionPerspectiveFOV(m, fovY, aspect, Znear, Zfar, righthanded);
	}
	
	// Return the cross product matrix for 'vec'. This matrix can be used to take the
	// cross product of another vector: e.g. Cross(v1, v2) == CrossProductMatrix4x4(v1) * v2
	inline m4x4 CrossProductMatrix4x4(v4 const& vec)
	{
		return m4x4::make(
				   v4::make(0.0f,  vec.z, -vec.y, 0.0f),
				   v4::make(-vec.z,   0.0f,  vec.x, 0.0f),
				   v4::make(vec.y, -vec.x,   0.0f, 0.0f),
				   v4Zero);
	}
	
	// Make an object to world transform from a direction vector and position
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be choosen.
	inline m4x4& OriFromDir(m4x4& ori, v4 const& dir, int axis, v4 const& up, v4 const& position)
	{
		OriFromDir(cast_m3x3(ori), dir, axis, up);
		ori.pos = position;
		return ori;
	}
	inline m4x4  OriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& position)
	{
		m4x4 m;
		return OriFromDir(m, dir, axis, up, position);
	}
	
	// Make a scaled object to world transform from a direction vector and position
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	inline m4x4& ScaledOriFromDir(m4x4& ori, v4 const& dir, int axis, v4 const& up, v4 const& position)
	{
		ScaledOriFromDir(cast_m3x3(ori), dir, axis, up);
		ori.pos = position;
		return ori;
	}
	inline m4x4  ScaledOriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& position)
	{
		m4x4 m;
		return ScaledOriFromDir(m, dir, axis, up, position);
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using Denman-Beavers square root iteration. Should converge quadratically
	inline m4x4 Sqrt(m4x4 const& mat)
	{
		m4x4 Y = mat;           // Converges to mat^0.5
		m4x4 Z = m4x4Identity;  // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			m4x4 Y_next = 0.5 * (Y + GetInverse(Z));
			m4x4 Z_next = 0.5 * (Z + GetInverse(Y));
			Y = Y_next;
			Z = Z_next;
		}
		return Y;
	}
}

#endif
