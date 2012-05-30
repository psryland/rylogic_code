//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX3X3_IMPL_H
#define PR_MATHS_MATRIX3X3_IMPL_H

#include "pr/maths/matrix3x3.h"

namespace pr
{
	inline m3x3 m3x3::make(v4 const& x_, v4 const& y_, v4 const& z_)   { m3x3 m; return m.set(x_, y_, z_); }
	inline m3x3 m3x3::make(Quat const& quat)                           { m3x3 m; return m.set(quat); }
	inline m3x3 m3x3::make(v4 const& from, v4 const& to)               { m3x3 m; return m.set(from, to); }
	inline m3x3 m3x3::make(v4 const& axis_norm, float angle)           { m3x3 m; return m.set(axis_norm, angle); }
	inline m3x3 m3x3::make(v4 const& angular_displacement)             { m3x3 m; return m.set(angular_displacement); }
	inline m3x3 m3x3::make(float pitch, float yaw, float roll)         { m3x3 m; return m.set(pitch, yaw, roll); }
	inline m3x3 m3x3::make(float const* mat)                           { m3x3 m; return m.set(mat); }

	// Create from vectors
	inline m3x3& m3x3::set(v4 const& x_, v4 const& y_, v4 const& z_)
	{
		x = x_;
		y = y_;
		z = z_;
		return *this;
	}

	// Create from quaterion
	inline m3x3& m3x3::set(Quat const& quat)
	{
		PR_ASSERT(PR_DBG_MATHS, !IsZero(quat), "'quat' is a zero quaternion");
		
		float quat_length_sq = Length4Sq(quat);
		float s              = 2.0f / quat_length_sq;
		
		float xs = quat.x *  s, ys = quat.y *  s, zs = quat.z *  s;
		float wx = quat.w * xs, wy = quat.w * ys, wz = quat.w * zs;
		float xx = quat.x * xs, xy = quat.x * ys, xz = quat.x * zs;
		float yy = quat.y * ys, yz = quat.y * zs, zz = quat.z * zs;

		x.x = 1.0f - (yy + zz); y.x = xy - wz;          z.x = xz + wy;
		x.y = xy + wz;          y.y = 1.0f - (xx + zz); z.y = yz - wx;
		x.z = xz - wy;          y.z = yz + wx;          z.z = 1.0f - (xx + yy);
		x.w =                   y.w =                   z.w = 0.0f;
		return *this;
	}
	
	// Create a transform representing the rotation from one vector to another.
	// 'from' and 'to' should be normalised
	inline m3x3& m3x3::set(v4 const& from, v4 const& to)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal3(from) && IsNormal3(to), "'from' and 'to' should be normalised");

		float cos_angle    = Dot3(from, to);      // Cos angle
		v4 axis_sine_angle = Cross3(from, to);    // Axis multiplied by sine of the angle
		v4 axis_norm       = GetNormal3(axis_sine_angle);
		return set(axis_norm, axis_sine_angle, cos_angle);
	}

	// Create from an axis, angle
	inline m3x3& m3x3::set(v4 const& axis_norm, v4 const& axis_sine_angle, float cos_angle)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal3(axis_norm), "'axis_norm' should be normalised");

		v4 trace_vec = axis_norm * (1.0f - cos_angle);
		
		x.x = trace_vec.x * axis_norm.x + cos_angle;
		y.y = trace_vec.y * axis_norm.y + cos_angle;
		z.z = trace_vec.z * axis_norm.z + cos_angle;
		
		trace_vec.x *= axis_norm.y;
		trace_vec.z *= axis_norm.x;
		trace_vec.y *= axis_norm.z;
		
		x.y = trace_vec.x + axis_sine_angle.z;
		x.z = trace_vec.z - axis_sine_angle.y;
		x.w = 0.0f;
		y.x = trace_vec.x - axis_sine_angle.z;
		y.z = trace_vec.y + axis_sine_angle.x;
		y.w = 0.0f;
		z.x = trace_vec.z + axis_sine_angle.y;
		z.y = trace_vec.y - axis_sine_angle.x;
		z.w = 0.0f;
		return *this;
	}

	// Create from an axis and angle. 'axis' should be normalised
	inline m3x3& m3x3::set(v4 const& axis_norm, float angle)
	{
		PR_ASSERT(PR_DBG_MATHS, IsNormal3(axis_norm), "'axis_norm' should be normalised");
		return set(axis_norm, axis_norm * pr::Sin(angle), pr::Cos(angle));
	}

	// Create from an angular_displacement vector where the length represents
	// the rotation in radians and the direction represents the axis of rotation
	inline m3x3& m3x3::set(v4 const& angular_displacement)
	{
		PR_ASSERT(PR_DBG_MATHS, FEql(angular_displacement.w, 0.0f), "'angular_displacement' should be a scaled direction vector");
		float len = Length3(angular_displacement);
		return len > maths::tiny ? set(angular_displacement/len, len) : identity();
	}

	// Create from an pitch, yaw, and roll.
	// Order is roll, pitch, yaw because objects usually face along Z and have Y as up.
	inline m3x3& m3x3::set(float pitch, float yaw, float roll)
	{
		#if PR_MATHS_USE_D3DX
		m4x4 mat;
		D3DXMatrixRotationYawPitchRoll(&d3dm4(mat), yaw, pitch, roll);
		return *this = cast_m3x3(mat);
		#else
		float cos_p = pr::Cos(pitch), sin_p = pr::Sin(pitch);
		float cos_y = pr::Cos(yaw  ), sin_y = pr::Sin(yaw  );
		float cos_r = pr::Cos(roll ), sin_r = pr::Sin(roll );
		x.set( cos_y*cos_r + sin_y*sin_p*sin_r , cos_p*sin_r , -sin_y*cos_r + cos_y*sin_p*sin_r , 0.0f);
		y.set(-cos_y*sin_r + sin_y*sin_p*cos_r , cos_p*cos_r ,  sin_y*sin_r + cos_y*sin_p*cos_r , 0.0f);
		z.set( sin_y*cos_p                     ,      -sin_p ,                      cos_y*cos_p , 0.0f);
		return *this;
		#endif
	}
	
	// Create from a pointer to an array of 12 floats, i.e. 4x3
	inline m3x3& m3x3::set(float const* mat)
	{
		x.set(mat  );
		y.set(mat+4);
		z.set(mat+8);
		return *this;
	}
	inline m3x3& m3x3::set(double const* mat)
	{
		x.set(mat  );
		y.set(mat+4);
		z.set(mat+8);
		return *this;
	}
	inline m3x3&     m3x3::zero()                                  { return *this = m3x3Zero; }
	inline m3x3&     m3x3::identity()                              { return *this = m3x3Identity; }
	
	// Assignment operators
	inline m3x3& operator += (m3x3& lhs, float rhs)                { cast_v3(lhs.x) += rhs;   cast_v3(lhs.y) += rhs;   cast_v3(lhs.z) += rhs;   return lhs; }
	inline m3x3& operator -= (m3x3& lhs, float rhs)                { cast_v3(lhs.x) -= rhs;   cast_v3(lhs.y) -= rhs;   cast_v3(lhs.z) -= rhs;   return lhs; }
	inline m3x3& operator += (m3x3& lhs, m3x3 const& rhs)          { lhs.x += rhs.x; lhs.y += rhs.y; lhs.z += rhs.z; return lhs; }
	inline m3x3& operator -= (m3x3& lhs, m3x3 const& rhs)          { lhs.x -= rhs.x; lhs.y -= rhs.y; lhs.z -= rhs.z; return lhs; }
	inline m3x3& operator *= (m3x3& lhs, float rhs)                { lhs.x *= rhs;   lhs.y *= rhs;   lhs.z *= rhs;   return lhs; }
	inline m3x3& operator /= (m3x3& lhs, float rhs)                { lhs.x /= rhs;   lhs.y /= rhs;   lhs.z /= rhs;   return lhs; }

	// Binary operators
	inline m3x3 operator + (m3x3 const& lhs, float rhs)            { m3x3 m = lhs; return m += rhs; }
	inline m3x3 operator - (m3x3 const& lhs, float rhs)            { m3x3 m = lhs; return m -= rhs; }
	inline m3x3 operator + (float lhs, m3x3 const& rhs)            { m3x3 m = rhs; return m += lhs; }
	inline m3x3 operator - (float lhs, m3x3 const& rhs)            { m3x3 m = rhs; return m -= lhs; }
	inline m3x3 operator + (m3x3 const& lhs, m3x3 const& rhs)      { m3x3 m = lhs; return m += rhs; }
	inline m3x3 operator - (m3x3 const& lhs, m3x3 const& rhs)      { m3x3 m = lhs; return m -= rhs; }
	inline m3x3 operator * (m3x3 const& lhs, float rhs)            { m3x3 m = lhs; return m *= rhs; }
	inline m3x3 operator * (float lhs, m3x3 const& rhs)            { m3x3 m = rhs; return m *= lhs; }
	inline m3x3 operator / (m3x3 const& lhs, float rhs)            { m3x3 m = lhs; return m /= rhs; }

	#pragma warning (push)
	#pragma warning (disable : 4701) // 'ans' may not be fully initialised
	inline m3x3 operator * (m3x3 const& lhs, m3x3 const& rhs)
	{
		m3x3 ans, lhs_t = GetTranspose(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int j = 0; j < 3; ++j)
		{
			ans[j].w = 0.0f;
			#pragma PR_OMP_PARALLEL_FOR
			for (int i = 0; i < 3; ++i)
				ans[j][i] = Dot3(lhs_t[i], rhs[j]);
		}
		return ans;
	}
	inline v4 operator * (m3x3 const& lhs, v4 const& rhs)
	{
		v4 ans;
		m3x3 lhs_t = GetTranspose(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int i = 0; i < 3; ++i)
			ans[i] = Dot3(lhs_t[i], rhs);
		ans.w = rhs.w;
		return ans;
	}
	inline v3 operator * (m3x3 const& lhs, v3 const& rhs)
	{
		v3 ans;
		m3x3 lhs_t = GetTranspose(lhs);
		#pragma PR_OMP_PARALLEL_FOR
		for (int i = 0; i < 3; ++i)
			ans[i] = Dot3(cast_v3(lhs_t[i]), rhs);
		return ans;
	}
	#pragma warning (pop)

	// Unary operators
	inline m3x3 operator + (m3x3 const& mat)                                { return mat; }
	inline m3x3 operator - (m3x3 const& mat)                                { return m3x3::make(-mat.x, -mat.y, -mat.z); }

	// Equality operators
	inline bool FEql        (m3x3 const& lhs, m3x3 const& rhs, float tol)   { return FEql3(lhs.x, rhs.x, tol) && FEql3(lhs.y, rhs.y, tol) && FEql3(lhs.z, rhs.z, tol); }
	inline bool FEqlZero    (m3x3 const& lhs, float tol)                    { return FEqlZero3(lhs.x, tol) && FEqlZero3(lhs.y, tol) && FEqlZero3(lhs.z, tol); }
	inline bool operator == (m3x3 const& lhs, m3x3 const& rhs)              { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (m3x3 const& lhs, m3x3 const& rhs)              { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (m3x3 const& lhs, m3x3 const& rhs)              { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (m3x3 const& lhs, m3x3 const& rhs)              { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (m3x3 const& lhs, m3x3 const& rhs)              { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (m3x3 const& lhs, m3x3 const& rhs)              { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	// Return true if 'm' is finite
	inline bool	IsFinite(m3x3 const& m)
	{
		return IsFinite(m.x) && IsFinite(m.y) && IsFinite(m.z);
	}
	inline bool IsFinite(m3x3 const& m, float max_value)
	{
		return IsFinite(m.x, max_value) && IsFinite(m.y, max_value) && IsFinite(m.z, max_value);
	}

	inline m3x3& Zero(m3x3& mat)
	{
		return mat.zero();
	}

	inline m3x3 Abs(m3x3 const& mat)
	{
		return m3x3::make(Abs(mat.x), Abs(mat.y), Abs(mat.z));
	}

	// Return the determinant of 'mat'
	inline float Determinant3(m3x3 const& mat)
	{
		return Triple3(mat.x, mat.y, mat.z);
	}

	inline float Trace3(m3x3 const& mat)
	{
		return mat.x.x + mat.y.y + mat.z.z;
	}

	inline v4 Kernel(m3x3 const& mat)
	{
		return v4::make(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}
	
	inline m3x3& Transpose(m3x3& mat)
	{
		Swap(mat.x.y, mat.y.x);
		Swap(mat.x.z, mat.z.x);
		Swap(mat.y.z, mat.z.y);
		return mat;
	}
	
	inline m3x3 GetTranspose(m3x3 const& mat)
	{
		m3x3 m = mat;
		return Transpose(m);
	}

	inline bool IsInvertable(m3x3 const& mat)
	{
		return !FEql(Determinant3(mat), 0.0f);
	}
	
	inline m3x3& Inverse(m3x3& mat)
	{
		PR_ASSERT(PR_DBG_MATHS, IsInvertable(mat), "Matrix has no inverse");
		float inv_det = 1.0f / Determinant3(mat);
		m3x3  tmp     = GetTranspose(mat);
		mat.x = Cross3(tmp.y, tmp.z) * inv_det;
		mat.y = Cross3(tmp.z, tmp.x) * inv_det;
		mat.z = Cross3(tmp.x, tmp.y) * inv_det;
		return mat;
	}
	
	inline m3x3 GetInverse(m3x3 const& mat)
	{
		PR_ASSERT(PR_DBG_MATHS, IsInvertable(mat), "Matrix has no inverse");
		float inv_det = 1.0f / Determinant3(mat);
		m3x3 tmp;
		tmp.x = Cross3(mat.y, mat.z) * inv_det;
		tmp.y = Cross3(mat.z, mat.x) * inv_det;
		tmp.z = Cross3(mat.x, mat.y) * inv_det;
		return Transpose(tmp);
	}
	
	inline m3x3& InverseFast(m3x3& mat)
	{
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(mat), "Matrix is not orthonormal");
		return Transpose(mat);
	}
	
	inline m3x3 GetInverseFast(m3x3 const& mat)
	{
		m3x3 m = mat;
		return InverseFast(m);
	}

	// Orthonormalises the rotation component of 'mat'
	inline m3x3& Orthonormalise(m3x3& mat)
	{
		Normalise3(mat.x);
		mat.y = GetNormal3(Cross3(mat.z, mat.x));
		mat.z = Cross3(mat.x, mat.y);
		return mat;
	}

	// Return true if 'mat' is orthonormal
	inline bool IsOrthonormal(m3x3 const& mat)
	{
		return	FEql(Length3Sq(mat.x), 1.0f) &&
				FEql(Length3Sq(mat.y), 1.0f) &&
				FEql(Length3Sq(mat.z), 1.0f) &&
				FEql(Abs(Determinant3(mat)), 1.0f);
	}

	// Return the axis and angle of a rotation matrix
	inline void GetAxisAngle(m3x3 const& mat, v4& axis, float& angle)
	{
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(mat), "Matrix is not a pure rotation matrix");
		
		angle = pr::ACos(0.5f * (Trace3(mat) - 1.0f));
		axis = 1000.0f * Kernel(pr::m3x3Identity - mat);
		if (IsZero3(axis))		{ axis = v4XAxis; angle = 0.0f; return; }
		Normalise3(axis);
		if (IsZero3(axis))		{ axis = v4XAxis; angle = 0.0f; return; }

		// Determine the correct sign of the angle
		v4 vec = CreateNotParallelTo(axis);
		v4 X = vec - Dot3(axis, vec) * axis;
		v4 Xprim = mat * X;
		v4 XcXp = Cross3(X, Xprim);
		if (Dot3(XcXp, axis) < 0.0f) angle = -angle;
	}
	
	// Cosntruct a rotation matrix
	inline m3x3& Rotation3x3 (m3x3& mat, float pitch, float yaw, float roll)   { return mat.set(pitch, yaw, roll); }
	inline m3x3& Rotation3x3 (m3x3& mat, v3 const& axis_norm, float angle)     { return mat.set(v4::make(axis_norm, 0.0f), angle); }
	inline m3x3& Rotation3x3 (m3x3& mat, v4 const& axis_norm, float angle)     { return mat.set(axis_norm, angle); }
	inline m3x3& Rotation3x3 (m3x3& mat, v4 const& angular_displacement)       { return mat.set(angular_displacement); }
	inline m3x3& Rotation3x3 (m3x3& mat, Quat const& quat)                     { return mat.set(quat); }
	inline m3x3  Rotation3x3 (float pitch, float yaw, float roll)              { m3x3 m; return Rotation3x3(m, pitch, yaw, roll); }
	inline m3x3  Rotation3x3 (v4 const& angular_displacement)                  { m3x3 m; return Rotation3x3(m, angular_displacement); }
	inline m3x3  Rotation3x3 (v3 const& axis, float angle)                     { m3x3 m; return Rotation3x3(m, axis, angle); }
	inline m3x3  Rotation3x3 (v4 const& axis_norm, float angle)                { m3x3 m; return Rotation3x3(m, axis_norm, angle); }
	inline m3x3  Rotation3x3 (const Quat& quat)                                { m3x3 m; return Rotation3x3(m, quat); }
	
	// Construct a scale matrix
	inline m3x3& Scale3x3    (m3x3& mat, float scale)                          { Zero(mat); mat.x.x = mat.y.y = mat.z.z = scale; return mat; }
	inline m3x3& Scale3x3    (m3x3& mat, float sx, float sy, float sz)         { Zero(mat); mat.x.x = sx; mat.y.y = sy; mat.z.z = sz; return mat; }
	inline m3x3  Scale3x3    (float scale)                                     { m3x3 m; return Scale3x3(m, scale); }
	inline m3x3  Scale3x3    (float sx, float sy, float sz)                    { m3x3 m; return Scale3x3(m, sx, sy, sz); }
	
	// Construct a shear matrix
	inline m3x3& Shear3x3    (m3x3& mat, float sxy, float sxz, float syx, float syz, float szx, float szy) { mat.x.set(1.0f, sxy, sxz, 0.0f); mat.y.set(syx, 1.0f, syz, 0.0f); mat.z.set(szx, szy, 1.0f, 0.0f); return mat; }
	inline m3x3  Shear3x3    (float sxy, float sxz, float syx, float syz, float szx, float szy)            { m3x3 m; return Shear3x3(m, sxy, sxz, syx, syz, szx, szy); }
	
	// Diagonalise a 3x3 matrix. From numerical recipes
	namespace impl
	{
		inline void Rotate(m3x3& mat, int i, int j, int k, int l, float s, float tau)
		{
			float temp = mat[j][i];
			float h    = mat[l][k];
			mat[j][i] = temp - s * (h + temp * tau);
			mat[l][k] = h    + s * (temp - h * tau);
		}
	}
	inline m3x3& Diagonalise3x3(m3x3& mat, m3x3& eigen_vectors, v4& eigen_values)
	{
		// Initialise the eigen values and b to be the diagonal elements of 'mat'
		v4 b;
		eigen_values.x = b.x = mat.x.x;
		eigen_values.y = b.y = mat.y.y;
		eigen_values.z = b.z = mat.z.z;
		eigen_values.w = b.w = 0.0f;

		eigen_vectors.identity();

		float sum;
		float const diagonal_eps = 1.0e-4f;
		do
		{
			v4 z = v4Zero;

			// sweep through all elements above the diagonal
			for( int i = 0; i != 3; ++i ) //ip
			{
				for( int j = i + 1; j != 3; ++j ) //iq
				{
					if( Abs(mat[j][i]) > diagonal_eps/3.0f )
					{
						float h		= eigen_values[j] - eigen_values[i];
						float theta = 0.5f * h / mat[j][i];
						float t		= Sign(theta) / (Abs(theta) + Sqrt(1.0f + Sqr(theta)));
						float c		= 1.0f / Sqrt(1.0f + Sqr(t));
						float s		= t * c;
						float tau	= s / (1.0f + c);
						h			= t * mat[j][i];

						z[i] -= h;
						z[j] += h;
						eigen_values[i] -= h;
						eigen_values[j] += h;
						mat[j][i] = 0.0f;

						for( int k = 0; k != i; ++k )
							impl::Rotate(mat, k, i, k, j, s, tau); //changes mat( 0:i-1 ,i) and mat( 0:i-1 ,j)
						
						for( int k = i + 1; k != j; ++k )
							impl::Rotate(mat, i, k, k, j, s, tau); //changes mat(i, i+1:j-1 ) and mat( i+1:j-1 ,j)
						
						for( int k = j + 1; k != 3; ++k )
							impl::Rotate(mat, i, k, j, k, s, tau); //changes mat(i, j+1:2 ) and mat(j, j+1:2 )
						
						for( int k = 0; k != 3; ++k )
							impl::Rotate(eigen_vectors, k, i, k, j, s, tau); //changes EigenVec( 0:2 ,i) and evec( 0:2 ,j)
					}
				}
			}

			b = b + z;
			eigen_values = b;

			// Calculate sum of abs. values of off diagonal elements, to see if we've finished
			sum  = Abs(mat.y.x);
			sum += Abs(mat.z.x);
			sum += Abs(mat.z.y);
		}
		while (sum > diagonal_eps);
		return mat;
	}
	inline m3x3 GetDiagonal3x3(m3x3 const& mat, m3x3& eigen_vectors, v4& eigen_values)
	{
		m3x3 m = mat;
		return Diagonalise3x3(m, eigen_vectors, eigen_values);
	}

	// Construct a rotation matrix that transforms 'from' onto the z axis
	// Other points can then be projected onto the XY plane by rotating by this
	// matrix and then setting the z value to zero
	inline m3x3& RotationToZAxis(m3x3& mat, v4 const& from)
	{
		float r = Sqr(from.x) + Sqr(from.y);
		float d = Sqrt(r);
		if (FEql(d, 0.0f))
		{
			mat = m3x3Identity;	// Create an identity transform or a 180 degree rotation
			mat.x.x = from.z;	// about Y depending on the sign of 'from.z'
			mat.z.z = from.z;
		}
		else
		{
			mat.x.set( from.x*from.z/d, -from.y/d, from.x, 0.0f);
			mat.y.set( from.y*from.z/d,  from.x/d, from.y, 0.0f);
			mat.z.set(            -r/d,      0.0f, from.z, 0.0f);
		}
		return mat;
	}
	inline m3x3  RotationToZAxis(v4 const& from)
	{
		m3x3 m;
		return RotationToZAxis(m, from);
	}
	
	// Make an orientation matrix from a direction vector
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be choosen.
	inline m3x3& OriFromDir(m3x3& ori, v4 const& dir, int axis, v4 const& up)
	{
		v4 up_ = pr::Parallel(up, dir) ? Perpendicular(dir) : up;
		ori[ axis       ] = GetNormal3(dir);
		ori[(axis+1) % 3] = GetNormal3(Cross3(up_, ori[axis]));
		ori[(axis+2) % 3] = Cross3(ori[axis], ori[(axis+1)%3]);
		return ori;
	}
	inline m3x3  OriFromDir(v4 const& dir, int axis, v4 const& up)
	{
		m3x3 m;
		return OriFromDir(m, dir, axis, up);
	}
	
	// Make a scaled orientation matrix from a direction vector
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	inline m3x3& ScaledOriFromDir(m3x3& ori, v4 const& dir, int axis, v4 const& up)
	{
		float len = pr::Length3(dir);
		if (len < pr::maths::tiny) return Scale3x3(ori, 0.0f);
		OriFromDir(ori, dir, axis, up);
		ori = ori * Scale3x3(len);
		return ori;
	}
	inline m3x3  ScaledOriFromDir(v4 const& dir, int axis, v4 const& up)
	{
		m3x3 ori;
		return ScaledOriFromDir(ori, dir, axis, up);
	}
	
	// Return the cross product matrix for 'vec'. This matrix can be used to take the
	// cross product of another vector: e.g. Cross(v1, v2) == CrossProductMatrix3x3(v1) * v2
	inline m3x3 CrossProductMatrix3x3(v4 const& vec)
	{
		return m3x3::make(
			v4::make(     0,  vec.z, -vec.y, 0),
			v4::make(-vec.z,      0,  vec.x, 0),
			v4::make( vec.y, -vec.x,      0, 0));
	}
}

#endif
