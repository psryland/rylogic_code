//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_MATRIX3X3_IMPL_H
#define PR_MATHS_MATRIX3X3_IMPL_H

#include "pr/maths/matrix3x3.h"

namespace pr
{
	// Create from vectors
	inline m3x4& m3x4::set(v4 const& x_, v4 const& y_, v4 const& z_)
	{
		x = x_;
		y = y_;
		z = z_;
		return *this;
	}

	// Create from quaterion
	inline m3x4& m3x4::set(Quat const& quat)
	{
		assert(!IsZero(quat) && "'quat' is a zero quaternion");

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
	inline m3x4& m3x4::set(v4 const& from, v4 const& to)
	{
		assert(IsNormal3(from) && IsNormal3(to) && "'from' and 'to' should be normalised");

		float cos_angle    = Dot3(from, to);      // Cos angle
		v4 axis_sine_angle = Cross3(from, to);    // Axis multiplied by sine of the angle
		v4 axis_norm       = Normalise3(axis_sine_angle);
		return set(axis_norm, axis_sine_angle, cos_angle);
	}

	// Create from an axis, angle
	inline m3x4& m3x4::set(v4 const& axis_norm, v4 const& axis_sine_angle, float cos_angle)
	{
		assert(IsNormal3(axis_norm) && "'axis_norm' should be normalised");

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
	inline m3x4& m3x4::set(v4 const& axis_norm, float angle)
	{
		assert(IsNormal3(axis_norm) && "'axis_norm' should be normalised");
		return set(axis_norm, axis_norm * pr::Sin(angle), pr::Cos(angle));
	}

	// Create from an angular_displacement vector where the length represents
	// the rotation in radians and the direction represents the axis of rotation
	inline m3x4& m3x4::set(v4 const& angular_displacement)
	{
		assert(FEql(angular_displacement.w, 0.0f) && "'angular_displacement' should be a scaled direction vector");
		float len = Length3(angular_displacement);
		return len > maths::tiny ? set(angular_displacement/len, len) : *this = m3x4Identity;
	}

	// Create from an pitch, yaw, and roll.
	// Order is roll, pitch, yaw because objects usually face along Z and have Y as up.
	inline m3x4& m3x4::set(float pitch, float yaw, float roll)
	{
		#if PR_MATHS_USE_DIRECTMATH
		m4x4 mat;
		dxm4(mat) = DirectX::XMMatrixRotationRollPitchYaw(pitch, yaw, roll);
		return *this = cast_m3x4(mat);
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
	inline m3x4& m3x4::set(float const* mat)
	{
		x.set(mat  );
		y.set(mat+4);
		z.set(mat+8);
		return *this;
	}
	inline m3x4& m3x4::set(double const* mat)
	{
		x.set(mat  );
		y.set(mat+4);
		z.set(mat+8);
		return *this;
	}

	// Assignment operators
	inline m3x4& operator += (m3x4& lhs, float rhs)                { lhs.x.xyz += rhs;   lhs.y.xyz += rhs;   lhs.z.xyz += rhs;   return lhs; }
	inline m3x4& operator -= (m3x4& lhs, float rhs)                { lhs.x.xyz -= rhs;   lhs.y.xyz -= rhs;   lhs.z.xyz -= rhs;   return lhs; }
	inline m3x4& operator += (m3x4& lhs, m3x4 const& rhs)          { lhs.x     += rhs.x; lhs.y     += rhs.y; lhs.z     += rhs.z; return lhs; }
	inline m3x4& operator -= (m3x4& lhs, m3x4 const& rhs)          { lhs.x     -= rhs.x; lhs.y     -= rhs.y; lhs.z     -= rhs.z; return lhs; }
	inline m3x4& operator *= (m3x4& lhs, float rhs)                { lhs.x     *= rhs;   lhs.y     *= rhs;   lhs.z     *= rhs;   return lhs; }
	inline m3x4& operator /= (m3x4& lhs, float rhs)                { lhs.x     /= rhs;   lhs.y     /= rhs;   lhs.z     /= rhs;   return lhs; }

	// Binary operators
	inline m3x4 operator + (m3x4 const& lhs, float rhs)            { m3x4 m = lhs; return m += rhs; }
	inline m3x4 operator - (m3x4 const& lhs, float rhs)            { m3x4 m = lhs; return m -= rhs; }
	inline m3x4 operator + (float lhs, m3x4 const& rhs)            { m3x4 m = rhs; return m += lhs; }
	inline m3x4 operator - (float lhs, m3x4 const& rhs)            { m3x4 m = rhs; return m -= lhs; }
	inline m3x4 operator + (m3x4 const& lhs, m3x4 const& rhs)      { m3x4 m = lhs; return m += rhs; }
	inline m3x4 operator - (m3x4 const& lhs, m3x4 const& rhs)      { m3x4 m = lhs; return m -= rhs; }
	inline m3x4 operator * (m3x4 const& lhs, float rhs)            { m3x4 m = lhs; return m *= rhs; }
	inline m3x4 operator * (float lhs, m3x4 const& rhs)            { m3x4 m = rhs; return m *= lhs; }
	inline m3x4 operator / (m3x4 const& lhs, float rhs)            { m3x4 m = lhs; return m /= rhs; }

	#pragma warning (push)
	#pragma warning (disable : 4701) // 'ans' may not be fully initialised
	inline m3x4 operator * (m3x4 const& lhs, m3x4 const& rhs)
	{
		m3x4 ans, lhs_t = Transpose3x3(lhs);
		for (int j = 0; j < 3; ++j)
		{
			ans[j].w = 0.0f;
			for (int i = 0; i < 3; ++i)
				ans[j][i] = Dot3(lhs_t[i], rhs[j]);
		}
		return ans;
	}
	inline v4 operator * (m3x4 const& lhs, v4 const& rhs)
	{
		v4 ans;
		m3x4 lhs_t = Transpose3x3(lhs);
		for (int i = 0; i < 3; ++i)
			ans[i] = Dot3(lhs_t[i], rhs);
		ans.w = rhs.w;
		return ans;
	}
	inline v3 operator * (m3x4 const& lhs, v3 const& rhs)
	{
		v3 ans;
		m3x4 lhs_t = Transpose3x3(lhs);
		for (int i = 0; i < 3; ++i)
			ans[i] = Dot3(lhs_t[i].xyz, rhs);
		return ans;
	}
	#pragma warning (pop)

	// Unary operators
	inline m3x4 operator + (m3x4 const& mat) { return mat; }
	inline m3x4 operator - (m3x4 const& mat) { return m3x4::make(-mat.x, -mat.y, -mat.z); }

	// Equality operators
	inline bool FEql        (m3x4 const& lhs, m3x4 const& rhs, float tol) { return FEql3(lhs.x, rhs.x, tol) && FEql3(lhs.y, rhs.y, tol) && FEql3(lhs.z, rhs.z, tol); }
	inline bool FEqlZero    (m3x4 const& lhs, float tol)                  { return FEqlZero3(lhs.x, tol) && FEqlZero3(lhs.y, tol) && FEqlZero3(lhs.z, tol); }
	inline bool operator == (m3x4 const& lhs, m3x4 const& rhs)            { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (m3x4 const& lhs, m3x4 const& rhs)            { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (m3x4 const& lhs, m3x4 const& rhs)            { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (m3x4 const& lhs, m3x4 const& rhs)            { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (m3x4 const& lhs, m3x4 const& rhs)            { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (m3x4 const& lhs, m3x4 const& rhs)            { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Functions
	// Return true if 'm' is finite
	inline bool	IsFinite(m3x4 const& m)
	{
		return IsFinite(m.x) && IsFinite(m.y) && IsFinite(m.z);
	}
	inline bool IsFinite(m3x4 const& m, float max_value)
	{
		return IsFinite(m.x, max_value) && IsFinite(m.y, max_value) && IsFinite(m.z, max_value);
	}

	// Reset 'mat' to all zeros
	inline m3x4& Zero(m3x4& mat)
	{
		return mat = m3x4Zero;
	}

	// Return 'mat' with all elements positive
	inline m3x4 Abs(m3x4 const& mat)
	{
		return m3x4::make(Abs(mat.x), Abs(mat.y), Abs(mat.z));
	}

	// Return the determinant of 'mat'
	inline float Determinant3(m3x4 const& mat)
	{
		return Triple3(mat.x, mat.y, mat.z);
	}

	// Return the trace of 'mat'
	inline float Trace3(m3x4 const& mat)
	{
		return mat.x.x + mat.y.y + mat.z.z;
	}

	// Return the kernel of 'mat'
	inline v4 Kernel(m3x4 const& mat)
	{
		return v4::make(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}

	// Return the transpose of 'mat'
	inline m3x4 Transpose3x3(m3x4 const& mat)
	{
		m3x4 m = mat;
		Swap(m.x.y, m.y.x);
		Swap(m.x.z, m.z.x);
		Swap(m.y.z, m.z.y);
		return m;
	}

	// True if 'mat' can be inverted
	inline bool IsInvertable(m3x4 const& mat)
	{
		return !FEql(Determinant3(mat), 0.0f);
	}

	// Invert the matrix 'mat'
	inline m3x4 Invert(m3x4 const& mat)
	{
		assert(IsInvertable(mat) && "Matrix has no inverse");
		float inv_det = 1.0f / Determinant3(mat);
		m3x4 tmp;
		tmp.x = Cross3(mat.y, mat.z) * inv_det;
		tmp.y = Cross3(mat.z, mat.x) * inv_det;
		tmp.z = Cross3(mat.x, mat.y) * inv_det;
		return Transpose3x3(tmp);
	}

	// Invert the orthonormal matrix 'mat'
	inline m3x4 InvertFast(m3x4 const& mat_)
	{
		assert(IsOrthonormal(mat_) && "Matrix is not orthonormal");
		m3x4 mat = mat_;
		return Transpose3x3(mat);
	}

	// Orthonormalises the rotation component of 'mat'
	inline m3x4 Orthonorm(m3x4 const& mat)
	{
		m3x4 m = mat;
		m.x = Normalise3(m.x);
		m.y = Normalise3(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		return m;
	}

	// Return true if 'mat' is orthonormal
	inline bool IsOrthonormal(m3x4 const& mat)
	{
		return	FEql(Length3Sq(mat.x), 1.0f) &&
				FEql(Length3Sq(mat.y), 1.0f) &&
				FEql(Length3Sq(mat.z), 1.0f) &&
				FEql(Abs(Determinant3(mat)), 1.0f);
	}

	// Return the axis and angle of a rotation matrix
	inline void GetAxisAngle(m3x4 const& mat, v4& axis, float& angle)
	{
		assert(IsOrthonormal(mat) && "Matrix is not a pure rotation matrix");

		angle = pr::ACos(0.5f * (Trace3(mat) - 1.0f));
		axis = 1000.0f * Kernel(pr::m3x4Identity - mat);
		if (IsZero3(axis)) { axis = v4XAxis; angle = 0.0f; return; }
		axis = Normalise3(axis);
		if (IsZero3(axis)) { axis = v4XAxis; angle = 0.0f; return; }

		// Determine the correct sign of the angle
		v4 vec = CreateNotParallelTo(axis);
		v4 X = vec - Dot3(axis, vec) * axis;
		v4 Xprim = mat * X;
		v4 XcXp = Cross3(X, Xprim);
		if (Dot3(XcXp, axis) < 0.0f) angle = -angle;
	}

	// Cosntruct a rotation matrix
	inline m3x4 Rotation3x3(float pitch, float yaw, float roll)
	{
		return m3x4::make(pitch, yaw, roll);
	}
	inline m3x4 Rotation3x3(v3 const& axis_norm, float angle)
	{
		return m3x4::make(v4::make(axis_norm, 0.0f), angle);
	}
	inline m3x4 Rotation3x3(v4 const& axis_norm, float angle)
	{
		return m3x4::make(axis_norm, angle);
	}
	inline m3x4 Rotation3x3(v4 const& angular_displacement)
	{
		return m3x4::make(angular_displacement);
	}
	inline m3x4 Rotation3x3(Quat const& quat)
	{
		return m3x4::make(quat);
	}
	inline m3x4 Rotation3x3(v4 const& from, v4 const& to)
	{
		return m3x4::make(from, to);
	}

	// Construct a scale matrix
	inline m3x4 Scale3x3(float scale)
	{
		m3x4 mat = {};
		mat.x.x = mat.y.y = mat.z.z = scale;
		return mat;
	}
	inline m3x4 Scale3x3(float sx, float sy, float sz)
	{
		m3x4 mat = {};
		mat.x.x = sx;
		mat.y.y = sy;
		mat.z.z = sz;
		return mat;
	}

	// Construct a shear matrix
	inline m3x4 Shear3x3(float sxy, float sxz, float syx, float syz, float szx, float szy)
	{
		m3x4 mat;
		mat.x.set(1.0f, sxy, sxz, 0.0f);
		mat.y.set(syx, 1.0f, syz, 0.0f);
		mat.z.set(szx, szy, 1.0f, 0.0f);
		return mat;
	}
	
	// Diagonalise a 3x3 matrix. From numerical recipes
	inline m3x4 Diagonalise3x3(m3x4 const& mat_, m3x4& eigen_vectors, v4& eigen_values)
	{
		struct L
		{
			static void Rotate(m3x4& mat, int i, int j, int k, int l, float s, float tau)
			{
				float temp = mat[j][i];
				float h    = mat[l][k];
				mat[j][i] = temp - s * (h + temp * tau);
				mat[l][k] = h    + s * (temp - h * tau);
			}
		};

		// Initialise the eigen values and b to be the diagonal elements of 'mat'
		v4 b;
		eigen_values.x = b.x = mat_.x.x;
		eigen_values.y = b.y = mat_.y.y;
		eigen_values.z = b.z = mat_.z.z;
		eigen_values.w = b.w = 0.0f;
		eigen_vectors = m3x4Identity;

		m3x4 mat = mat_;
		float sum;
		float const diagonal_eps = 1.0e-4f;
		do
		{
			v4 z = v4Zero;

			// sweep through all elements above the diagonal
			for (int i = 0; i != 3; ++i) //ip
			{
				for (int j = i + 1; j != 3; ++j) //iq
				{
					if (Abs(mat[j][i]) > diagonal_eps/3.0f)
					{
						float h     = eigen_values[j] - eigen_values[i];
						float theta = 0.5f * h / mat[j][i];
						float t     = Sign(theta) / (Abs(theta) + Sqrt(1.0f + Sqr(theta)));
						float c     = 1.0f / Sqrt(1.0f + Sqr(t));
						float s     = t * c;
						float tau   = s / (1.0f + c);
						h           = t * mat[j][i];

						z[i] -= h;
						z[j] += h;
						eigen_values[i] -= h;
						eigen_values[j] += h;
						mat[j][i] = 0.0f;

						for (int k = 0; k != i; ++k)
							L::Rotate(mat, k, i, k, j, s, tau); //changes mat( 0:i-1 ,i) and mat( 0:i-1 ,j)

						for (int k = i + 1; k != j; ++k)
							L::Rotate(mat, i, k, k, j, s, tau); //changes mat(i, i+1:j-1 ) and mat( i+1:j-1 ,j)

						for (int k = j + 1; k != 3; ++k)
							L::Rotate(mat, i, k, j, k, s, tau); //changes mat(i, j+1:2 ) and mat(j, j+1:2 )

						for (int k = 0; k != 3; ++k)
							L::Rotate(eigen_vectors, k, i, k, j, s, tau); //changes EigenVec( 0:2 ,i) and evec( 0:2 ,j)
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

	// Construct a rotation matrix that transforms 'from' onto the z axis
	// Other points can then be projected onto the XY plane by rotating by this
	// matrix and then setting the z value to zero
	inline m3x4 RotationToZAxis(v4 const& from)
	{
		float r = Sqr(from.x) + Sqr(from.y);
		float d = Sqrt(r);
		m3x4 mat;
		if (FEql(d, 0.0f))
		{
			mat = m3x4Identity;	// Create an identity transform or a 180 degree rotation
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

	// Make an orientation matrix from a direction vector
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be choosen.
	inline m3x4 OriFromDir(v4 const& dir, int axis_id, v4 const& up)
	{
		assert(axis_id >= 0 && axis_id <= 2 && "axis_id out of range");
		v4 up_ = pr::Parallel(up, dir) ? Perpendicular(dir) : up;
		auto _0 = (axis_id + 0) % 3;
		auto _1 = (axis_id + 1) % 3;
		auto _2 = (axis_id + 2) % 3;
		m3x4 ori;
		ori[_0] = Normalise3(dir);
		ori[_1] = Normalise3(Cross3(up_, ori[_0]));
		ori[_2] = Cross3(ori[_0], ori[_1]);
		return ori;
	}

	// Make a scaled orientation matrix from a direction vector
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	inline m3x4 ScaledOriFromDir(v4 const& dir, int axis, v4 const& up)
	{
		float len = pr::Length3(dir);
		if (len < pr::maths::tiny) return Scale3x3(0.0f);
		return OriFromDir(dir, axis, up) * Scale3x3(len);
	}

	// Return the cross product matrix for 'vec'. This matrix can be used to take the
	// cross product of another vector: e.g. Cross(v1, v2) == CrossProductMatrix3x3(v1) * v2
	inline m3x4 CrossProductMatrix3x3(v4 const& vec)
	{
		return m3x4::make(
			v4::make(     0,  vec.z, -vec.y, 0),
			v4::make(-vec.z,      0,  vec.x, 0),
			v4::make( vec.y, -vec.x,      0, 0));
	}
}

#endif