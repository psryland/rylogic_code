//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/axis_id.h"

namespace pr
{
	template <typename A, typename B> 
	struct alignas(16) Mat3x4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union {
		struct { Vec4<void> x, y, z; };
		struct { Vec4<void> arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Mat3x4() = default;
		explicit Mat3x4(float x_)
			:x(x_)
			,y(x_)
			,z(x_)
		{
			assert(maths::is_aligned(this));
		}
		Mat3x4(v4_cref<> x_, v4_cref<> y_, v4_cref<> z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{
			assert(maths::is_aligned(this));
		}
		Mat3x4(Quat<A,B> const& q)
		{
			assert("'quat' is a zero quaternion" && !IsZero(q));
			auto s = 2.0f / LengthSq(q);

			float xs = q.x *  s, ys = q.y *  s, zs = q.z *  s;
			float wx = q.w * xs, wy = q.w * ys, wz = q.w * zs;
			float xx = q.x * xs, xy = q.x * ys, xz = q.x * zs;
			float yy = q.y * ys, yz = q.y * zs, zz = q.z * zs;
			x = Vec4<void>{1.0f - (yy + zz), xy + wz, xz - wy, 0};
			y = Vec4<void>{xy - wz, 1.0f - (xx + zz), yz + wx, 0};
			z = Vec4<void>{xz + wy, yz - wx, 1.0f - (xx + yy), 0};
		}
		template <typename V3, typename = maths::enable_if_v3<V3>> Mat3x4(V3 const& v)
			:Mat3x4(x_as<Vec4<void>>(v), y_as<Vec4<void>>(v), z_as<Vec4<void>>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit Mat3x4(CP const* v)
			:Mat3x4(x_as<Vec4<void>>(v), y_as<Vec4<void>>(v), z_as<Vec4<void>>(v))
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> Mat3x4& operator = (V3 const& rhs)
		{
			x = x_as<Vec4<T>>(rhs);
			y = y_as<Vec4<T>>(rhs);
			z = z_as<Vec4<T>>(rhs);
			return *this;
		}

		// Array access
		v4_cref<> operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		Vec4<void>& operator [](int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Convert to a 4x4 matrix with zero translation
		Mat4x4<A,B> m4x4() const
		{
			return Mat4x4<A,B>{*this, v4Origin};
		}

		// Get/Set by row or column. Note: x,y,z are column vectors
		Vec4<void> col(int i) const
		{
			return arr[i];
		}
		Vec4<void> row(int i) const
		{
			return Vec4<void>{x[i], y[i], z[i], 0};
		}
		void col(int i, v4_cref<> col)
		{
			arr[i] = col;
		}
		void row(int i, v4_cref<> row)
		{
			x[i] = row.x;
			y[i] = row.y;
			z[i] = row.z;
		}

		// Create a 4x4 matrix with this matrix as the rotation part
		Mat4x4<A,B> w0() const
		{
			return Mat4x4<A,B>(*this, v4Origin);
		}
		Mat4x4<A,B> w1(v4_cref<> pos) const
		{
			assert("'pos' must be a position vector" && pos.w == 1);
			return Mat4x4<A,B>{*this, pos};
		}

		// Construct a rotation matrix. Order is: roll, pitch, yaw (to match DirectX)
		static Mat3x4 Rotation(float pitch, float yaw, float roll)
		{
			#if PR_MATHS_USE_DIRECTMATH
			auto m = DirectX::XMMatrixRotationRollPitchYaw(pitch, yaw, roll);
			return Mat3x4(m.r[0], m.r[1], m.r[2]);
			#else
			float cos_p = Cos(pitch), sin_p = Sin(pitch);
			float cos_y = Cos(yaw  ), sin_y = Sin(yaw  );
			float cos_r = Cos(roll ), sin_r = Sin(roll );
			return Mat3x4{
				Vec4<void>( cos_y*cos_r + sin_y*sin_p*sin_r , cos_p*sin_r , -sin_y*cos_r + cos_y*sin_p*sin_r , 0.0f),
				Vec4<void>(-cos_y*sin_r + sin_y*sin_p*cos_r , cos_p*cos_r ,  sin_y*sin_r + cos_y*sin_p*cos_r , 0.0f),
				Vec4<void>( sin_y*cos_p                     ,      -sin_p ,                      cos_y*cos_p , 0.0f)};
			#endif
		}

		// Create from an axis, angle
		static Mat3x4 pr_vectorcall Rotation(v4_cref<> axis_norm, v4_cref<> axis_sine_angle, float cos_angle)
		{
			assert("'axis_norm' should be normalised" && IsNormal3(axis_norm));

			auto m = Mat3x4{};
			auto trace_vec = axis_norm * (1.0f - cos_angle);

			m.x.x = trace_vec.x * axis_norm.x + cos_angle;
			m.y.y = trace_vec.y * axis_norm.y + cos_angle;
			m.z.z = trace_vec.z * axis_norm.z + cos_angle;

			trace_vec.x *= axis_norm.y;
			trace_vec.z *= axis_norm.x;
			trace_vec.y *= axis_norm.z;

			m.x.y = trace_vec.x + axis_sine_angle.z;
			m.x.z = trace_vec.z - axis_sine_angle.y;
			m.x.w = 0.0f;
			m.y.x = trace_vec.x - axis_sine_angle.z;
			m.y.z = trace_vec.y + axis_sine_angle.x;
			m.y.w = 0.0f;
			m.z.x = trace_vec.z + axis_sine_angle.y;
			m.z.y = trace_vec.y - axis_sine_angle.x;
			m.z.w = 0.0f;

			return m;
		}

		// Create from an axis and angle. 'axis' should be normalised
		static Mat3x4 pr_vectorcall Rotation(v4_cref<> axis_norm, float angle)
		{
			return Rotation(axis_norm, axis_norm * pr::Sin(angle), pr::Cos(angle));
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat3x4 pr_vectorcall Rotation(v4_cref<> angular_displacement)
		{
			assert("'angular_displacement' should be a scaled direction vector" && angular_displacement.w == 0);
			auto len = Length3(angular_displacement);
			return len > maths::tiny
				? Mat3x4::Rotation(angular_displacement/len, len)
				: Mat3x4(v4XAxis, v4YAxis, v4ZAxis);
		}

		// Create a transform representing the rotation from one vector to another. (Vectors do not need to be normalised)
		static Mat3x4 pr_vectorcall Rotation(v4_cref<> from, v4_cref<> to)
		{
			assert(!FEql(from, v4Zero));
			assert(!FEql(to  , v4Zero));
			auto len = Length3(from) * Length3(to);

			auto cos_angle = Dot3(from, to) / len;
			if (cos_angle >= 1.0f - maths::tiny) return Mat3x4(v4XAxis, v4YAxis, v4ZAxis);
			if (cos_angle <= maths::tiny - 1.0f) return Rotation(Normalise3(Perpendicular(from - to)), float(maths::tau_by_2));

			auto axis_size_angle = Cross3(from, to) / len;
			auto axis_norm = Normalise3(axis_size_angle);
			return Rotation(axis_norm, axis_size_angle, cos_angle);
		}

		// Create a transform from one basis axis to another
		static Mat3x4 Rotation(AxisId from_axis, AxisId to_axis)
		{
			// 'o2f' = the rotation from Z to 'from_axis'
			// 'o2t' = the rotation from Z to 'to_axis'
			// 'f2t' = o2t * Invert(o2f)
			Mat3x4 o2f, o2t;
			switch (from_axis)
			{
			default: assert(false && "axis_id must one of ±1, ±2, ±3"); o2f = m3x4Identity; break;
			case -1: o2f = Mat3x4::Rotation(0.0f, +float(maths::tau_by_4), 0.0f); break;
			case +1: o2f = Mat3x4::Rotation(0.0f, -float(maths::tau_by_4), 0.0f); break;
			case -2: o2f = Mat3x4::Rotation(+float(maths::tau_by_4), 0.0f, 0.0f); break;
			case +2: o2f = Mat3x4::Rotation(-float(maths::tau_by_4), 0.0f, 0.0f); break;
			case -3: o2f = Mat3x4::Rotation(0.0f, +float(maths::tau_by_2), 0.0f); break;
			case +3: o2f = m3x4Identity; break;
			}
			switch (to_axis)
			{
			default: assert(false && "axis_id must one of ±1, ±2, ±3"); o2t = m3x4Identity; break;
			case -1: o2t = Mat3x4::Rotation(0.0f, -float(maths::tau_by_4), 0.0f); break; // I know this sign looks wrong, but it isn't. Must be something to do with signs passed to cos()/sin()
			case +1: o2t = Mat3x4::Rotation(0.0f, +float(maths::tau_by_4), 0.0f); break;
			case -2: o2t = Mat3x4::Rotation(+float(maths::tau_by_4), 0.0f, 0.0f); break;
			case +2: o2t = Mat3x4::Rotation(-float(maths::tau_by_4), 0.0f, 0.0f); break;
			case -3: o2t = Mat3x4::Rotation(0.0f, +float(maths::tau_by_2), 0.0f); break;
			case +3: o2t = m3x4Identity; break;
			}
			return o2t * InvertFast(o2f);
		}

		// Create a scale matrix
		static Mat3x4 Scale(float scale)
		{
			Mat3x4 mat = {};
			mat.x.x = mat.y.y = mat.z.z = scale;
			return mat;
		}
		static Mat3x4 Scale(float sx, float sy, float sz)
		{
			Mat3x4 mat = {};
			mat.x.x = sx;
			mat.y.y = sy;
			mat.z.z = sz;
			return mat;
		}

		// Create a shear matrix
		static Mat3x4 Shear(float sxy, float sxz, float syx, float syz, float szx, float szy)
		{
			Mat3x4 mat = {};
			mat.x = v4(1.0f, sxy, sxz, 0.0f);
			mat.y = v4(syx, 1.0f, syz, 0.0f);
			mat.z = v4(szx, szy, 1.0f, 0.0f);
			return mat;
		}
	};
	static_assert(maths::is_mat3<Mat3x4<void,void>>::value, "");
	static_assert(std::is_pod<Mat3x4<void,void>>::value, "Should be a pod type");
	static_assert(std::alignment_of<Mat3x4<void,void>>::value == 16, "Should be 16 byte aligned");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	template <typename A = void, typename B = void> using m3_cref = Mat3x4<A,B> const;
	#else
	template <typename A = void, typename B = void> using m3_cref = Mat3x4<A,B> const&;
	#endif

	// Define component accessors for pointer types
	template <typename A, typename B> inline v4_cref<> pr_vectorcall x_cp(m3_cref<A,B> v) { return v.x; }
	template <typename A, typename B> inline v4_cref<> pr_vectorcall y_cp(m3_cref<A,B> v) { return v.y; }
	template <typename A, typename B> inline v4_cref<> pr_vectorcall z_cp(m3_cref<A,B> v) { return v.z; }
	template <typename A, typename B> inline v4_cref<> pr_vectorcall w_cp(m3_cref<A,B>)   { return v4Origin; }

	#pragma region Operators
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator + (m3_cref<A,B> mat)
	{
		return mat;
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator - (m3_cref<A,B> mat)
	{
		return Mat3x4<A,B>{-mat.x, -mat.y, -mat.z};
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator * (float lhs, m3_cref<A,B> rhs)
	{
		return rhs * lhs;
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator * (m3_cref<A,B> lhs, float rhs)
	{
		return Mat3x4<A,B>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator / (m3_cref<A,B> lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		return Mat3x4<A,B>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator % (m3_cref<A,B> lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		return Mat3x4<A,B>{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs};
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator + (m3_cref<A,B> lhs, m3_cref<A,B> rhs)
	{
		return Mat3x4<A,B>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
	}
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall operator - (m3_cref<A,B> lhs, m3_cref<A,B> rhs)
	{
		return Mat3x4<A,B>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
	}
	template <typename A, typename B, typename C> inline Mat3x4<A,C> pr_vectorcall operator * (m3_cref<B,C> lhs, m3_cref<A,B> rhs)
	{
		auto ans = Mat3x4<A,C>{};
		auto lhsT = Transpose(lhs);
		ans.x = Vec4<void>{Dot3(lhsT.x, rhs.x), Dot3(lhsT.y, rhs.x), Dot3(lhsT.z, rhs.x), 0};
		ans.y = Vec4<void>{Dot3(lhsT.x, rhs.y), Dot3(lhsT.y, rhs.y), Dot3(lhsT.z, rhs.y), 0};
		ans.z = Vec4<void>{Dot3(lhsT.x, rhs.z), Dot3(lhsT.y, rhs.z), Dot3(lhsT.z, rhs.z), 0};
		return ans;
	}
	template <typename A, typename B> inline Vec4<B> pr_vectorcall operator * (m3_cref<A,B> lhs, v4_cref<A> rhs)
	{
		auto lhsT = Transpose(lhs);
		return Vec4<B>{Dot3(lhsT.x, rhs), Dot3(lhsT.y, rhs), Dot3(lhsT.z, rhs), rhs.w};
	}
	template <typename A, typename B> inline Vec3<B> pr_vectorcall operator * (m3_cref<A,B> lhs, v3_cref<A> rhs)
	{
		auto lhsT = Transpose3x3(lhs);
		return Vec3<B>{Dot3(lhsT.x.xyz, rhs), Dot3(lhsT.y.xyz, rhs), Dot3(lhsT.z.xyz, rhs)};
	}
	#pragma endregion

	#pragma region Functions

	// Return the determinant of 'mat'
	template <typename A, typename B> inline float pr_vectorcall Determinant(m3_cref<A,B> mat)
	{
		return Triple(mat.x, mat.y, mat.z);
	}

	// Return the trace of 'mat'
	template <typename A, typename B> inline float pr_vectorcall Trace(m3_cref<A,B> mat)
	{
		return mat.x.x + mat.y.y + mat.z.z;
	}

	// Return the kernel of 'mat'
	template <typename A, typename B> inline Vec4<void> pr_vectorcall Kernel(m3_cref<A,B> mat)
	{
		return Vec4<void>{mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0};
	}

	// Return the diagonal elements of 'mat'
	template <typename A, typename B> inline Vec4<void> pr_vectorcall Diagonal(m3_cref<A,B> mat)
	{
		return Vec4<void>{mat.x.x, mat.y.y, mat.z.z, 0};
	}

	// Create a cross product matrix for 'vec'.
	template <typename A> inline Mat3x4<A,A> pr_vectorcall CPM(v4_cref<A> vec)
	{
		// This matrix can be used to calculate the cross product with
		// another vector: e.g. Cross3(v1, v2) == CPM(v1) * v2
		return Mat3x4<A,A>{
			Vec4<void>(     0,  vec.z, -vec.y, 0),
			Vec4<void>(-vec.z,      0,  vec.x, 0),
			Vec4<void>( vec.y, -vec.x,      0, 0)};
	}

	// Return the transpose of 'mat'
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall Transpose(m3_cref<A,B> mat)
	{
		auto m = mat;
		std::swap(m.x.y, m.y.x);
		std::swap(m.x.z, m.z.x);
		std::swap(m.y.z, m.z.y);
		return m;
	}

	// Return true if 'mat' is orthonormal
	template <typename A, typename B> inline bool pr_vectorcall IsOrthonormal(m3_cref<A,B> mat)
	{
		return
			FEql(Length3Sq(mat.x), 1.0f) &&
			FEql(Length3Sq(mat.y), 1.0f) &&
			FEql(Length3Sq(mat.z), 1.0f) &&
			FEql(Abs(Determinant(mat)), 1.0f);
	}

	// True if 'mat' can be inverted
	template <typename A, typename B> inline bool pr_vectorcall IsInvertable(m3_cref<A,B> mat)
	{
		return Determinant(mat) != 0.0f;
	}

	// Invert the orthonormal matrix 'mat'
	template <typename A, typename B> inline Mat3x4<B,A> pr_vectorcall InvertFast(m3_cref<A,B> mat)
	{
		assert("Matrix is not orthonormal" && IsOrthonormal(mat));
		return static_cast<Mat3x4<B,A>>(Transpose(mat));
	}

	// Invert the matrix 'mat'
	template <typename A, typename B> inline Mat3x4<B,A> pr_vectorcall Invert(m3_cref<A,B> mat)
	{
		assert("Matrix has no inverse" && IsInvertable(mat));
		auto det = Determinant(mat);
		Mat3x4<B,A> tmp = {};
		tmp.x = Cross3(mat.y, mat.z) / det;
		tmp.y = Cross3(mat.z, mat.x) / det;
		tmp.z = Cross3(mat.x, mat.y) / det;
		return Transpose(tmp);
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using 'Denman-Beavers' square root iteration. Should converge quadratically
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall Sqrt(m3_cref<A,B> mat)
	{
		auto A = mat;           // Converges to mat^0.5
		auto B = m3x4Identity;  // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto A_next = 0.5f * (A + Invert(B));
			auto B_next = 0.5f * (B + Invert(A));
			A = A_next;
			B = B_next;
		}
		return A;
	}

	// Orthonormalises the rotation component of 'mat'
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall Orthonorm(m3_cref<A,B> mat)
	{
		auto m = mat;
		m.x = Normalise3(m.x);
		m.y = Normalise3(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		return m;
	}

	// Return the axis and angle of a rotation matrix
	template <typename A, typename B> inline void pr_vectorcall GetAxisAngle(m3_cref<A,B> mat, Vec4<void>& axis, float& angle)
	{
		assert("Matrix is not a pure rotation matrix" && IsOrthonormal(mat));

		angle = ACos(0.5f * (Trace(mat) - 1.0f));
		axis = 1000.0f * Kernel(m3x4Identity - mat);
		if (IsZero3(axis))
		{
			axis = v4XAxis;
			angle = 0.0f;
			return;
		}
		
		axis = Normalise3(axis);
		if (IsZero3(axis))
		{
			axis = v4XAxis;
			angle = 0.0f;
			return;
		}

		// Determine the correct sign of the angle
		auto vec = CreateNotParallelTo(axis);
		auto X = vec - Dot3(axis, vec) * axis;
		auto Xprim = mat * X;
		auto XcXp = Cross3(X, Xprim);
		if (Dot3(XcXp, axis) < 0.0f)
			angle = -angle;
	}

	// Return the Euler angles for a rotation matrix. Order is: roll, pitch, yaw (to match DirectX)
	template <typename A, typename B> inline Vec4<void> pr_vectorcall GetEulerAngles(m3_cref<A,B> mat)
	{
		assert(IsOrthonormal(mat) && "Matrix is not orthonormal");(void)mat;
		#if 0
		v4 euler = {};

		// roll, pitch, yaw are applied to a body sequentially, so to undo the rotations
		// we first "un-rotate" around mat.y, then around mat.x, then around mat.z.
		// Let X,Y,Z be the world space axes, and x,y,z be the axis of 'mat'.
		// When roll is applied, x and y move within the XY plane, z is still aligned to Z.
		// When pitch is applied, y moves out of the XY plane, z moves away from Z, and x stays within the XY plane.
		// When yaw is applied, z moves again, y remains one rotation out of the XY plane, and x moves out of the XY plane.
		// To reverse this,
		// Undo yaw => rotate x back into the XY plane (a rotation about y)
		// Undo pitch => rotate y back into the XY plane (a rotation about x after removing yaw)
		// Undo roll => rotate x and y back to X and Y (a rotation about z after removing yaw and pitch)

		auto x_xy_sq = Sqr(mat.x.x) + Sqr(mat.x.y);
		auto y_xy_sq = Sqr(mat.y.x) + Sqr(mat.y.y);

		euler.x = atan2(Sqrt(1 - y_xy_sq), Sqrt(y_xy_sq)); // un-pitch
		euler.y = atan2(Sqrt(1 - x_xy_sq), Sqrt(x_xy_sq)); // un-yaw
		euler.z = atan2(mat.x.y); // un-roll





		// yaw   - rotate y or z onto the YZ plane
		// pitch - if y, rotate y to the Y axis, else if x, rotate z to the Z axis
		// roll  - get x onto the X axis, or Y onto the Y axis

		// To get yaw, we rotate about the Y axis, so use the axis with the smallest Y value for accuracy
		if (abs(mat.y.y) < abs(mat.z.y))
		{
			// 'y' is least aligned with the Y axis. Rotate Y onto the YZ plane to get yaw
			euler.x = atan2(mat.y.z, Sqrt(1.0 - Sqr(mat.y.y))); // pitch = rotate y onto the Y axis
			euler.z = atan2(
		}
		else
		{
			// 'z' is least aligned with the Y axis

		}
		// Z aligned with the Y axis
		if (abs(mat.z.y) > 1.0f - maths::tiny)
		{
			euler.x = Sign(mat.z.y) * maths::tau_by_4;
			euler.y = 0.0f;
			euler.z = atan2(mat.x.y, mat.x.x);
		}
		else
		{
			euler.x = asin(m.m10); // pitch
			euler.y = atan2(mat.z.x, mat.z.z); // yaw
			euler.z = atan2(-m.m12, m.m11); // roll
		}
		#endif
		throw std::exception("not implemented");
	}

	// Diagonalise a 3x3 matrix. From numerical recipes
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall Diagonalise3x3(m3_cref<A,B> mat_, Mat3x4<A,B>& eigen_vectors, Vec4<void>& eigen_values)
	{
		struct L
		{
			static void Rotate(Mat3x4<A,B>& mat, int i, int j, int k, int l, float s, float tau)
			{
				auto temp = mat[j][i];
				auto h    = mat[l][k];
				mat[j][i] = temp - s * (h + temp * tau);
				mat[l][k] = h    + s * (temp - h * tau);
			}
		};

		// Initialise the Eigen values and b to be the diagonal elements of 'mat'
		Vec4<void> b;
		eigen_values.x = b.x = mat_.x.x;
		eigen_values.y = b.y = mat_.y.y;
		eigen_values.z = b.z = mat_.z.z;
		eigen_values.w = b.w = 0.0f;
		eigen_vectors = m3x4Identity;

		Mat3x4<A,B> mat = mat_;
		float sum;
		float const diagonal_eps = 1.0e-4f;
		do
		{
			auto z = v4Zero;

			// sweep through all elements above the diagonal
			for (int i = 0; i != 3; ++i) //ip
			{
				for (int j = i + 1; j != 3; ++j) //iq
				{
					if (Abs(mat[j][i]) > diagonal_eps/3.0f)
					{
						auto h     = eigen_values[j] - eigen_values[i];
						auto theta = 0.5f * h / mat[j][i];
						auto t     = Sign(theta) / (Abs(theta) + Sqrt(1.0f + Sqr(theta)));
						auto c     = 1.0f / Sqrt(1.0f + Sqr(t));
						auto s     = t * c;
						auto tau   = s / (1.0f + c);
						h          = t * mat[j][i];

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
	template <typename A> inline Mat3x4<A,A> RotationToZAxis(v4_cref<A> from)
	{
		auto r = Sqr(from.x) + Sqr(from.y);
		auto d = Sqrt(r);
		Mat3x4<A,A> mat = {};
		if (FEql(d, 0.0f))
		{
			mat = m3x4Identity;	// Create an identity transform or a 180 degree rotation
			mat.x.x = from.z;	// about Y depending on the sign of 'from.z'
			mat.z.z = from.z;
		}
		else
		{
			mat.x = Vec4<void>{from.x*from.z/d, -from.y/d, from.x, 0};
			mat.y = Vec4<void>{from.y*from.z/d,  from.x/d, from.y, 0};
			mat.z = Vec4<void>{           -r/d,      0.0f, from.z, 0};
		}
		return mat;
	}

	// Permute the vectors in a rotation matrix by 'n'.
	// n == 0 : x  y  z
	// n == 1 : z  x  y
	// n == 2 : y  z  x
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall PermuteRotation(m3_cref<A,B> mat, int n)
	{
		switch (n%3)
		{
		default:return mat;
		case 1: return Mat3x4<A,B>{mat.z, mat.x, mat.y};
		case 2: return Mat3x4<A,B>{mat.y, mat.z, mat.x};
		}
	}

	// Make an orientation matrix from a direction vector
	// 'dir' is the direction to align the axis 'axis_id' to. (Doesn't need to be normalised)
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be chosen.
	template <typename A = void, typename B = void> inline Mat3x4<A,B> pr_vectorcall OriFromDir(v4_cref<> dir, AxisId axis_id, v4_cref<> up_)
	{
		// Get the preferred up direction (handling parallel cases)
		auto up = Parallel(up_, dir) ? Perpendicular(dir) : up_;

		Mat3x4<A,B> ori = {};
		ori.z = Normalise3(Sign(float(axis_id)) * dir);
		ori.x = Normalise3(Cross3(up, ori.z));
		ori.y = Cross3(ori.z, ori.x);

		// Permute the column vectors so +Z becomes 'axis'
		return PermuteRotation(ori, abs(axis_id));
	}
	template <typename A = void, typename B = void> inline Mat3x4<A,B> pr_vectorcall OriFromDir(v4_cref<> dir, int axis_id)
	{
		return OriFromDir(dir, axis_id, Perpendicular(dir));
	}

	// Make a scaled orientation matrix from a direction vector
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	template <typename A = void, typename B = void> inline Mat3x4<A,B> pr_vectorcall ScaledOriFromDir(v4_cref<> dir, AxisId axis, v4_cref<> up)
	{
		auto len = Length3(dir);
		return len > pr::maths::tiny ? OriFromDir(dir, axis, up) * Mat3x4<A,B>::Scale(len) : m3x4Zero;
	}
	template <typename A = void, typename B = void> inline Mat3x4<A,B> pr_vectorcall ScaledOriFromDir(v4_cref<> dir, AxisId axis)
	{
		return ScaledOriFromDir(dir, axis, Perpendicular(dir));
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	template <typename A, typename B> inline Vec4<void> pr_vectorcall RotationVectorApprox(m3_cref<A,B> from, m3_cref<A,B> to)
	{
		assert("This only works for orthonormal matrices" && IsOrthonormal(from) && IsOrthonormal(to));
		
		auto cpm_x_i2wR = to - from;
		auto w2iR = Transpose(from);
		auto cpm = cpm_x_i2wR * w2iR;
		return Vec4<void>{cpm.y.z, cpm.z.x, cpm.x.y, 0};
	}

	// Spherically interpolate between two rotations
	template <typename A, typename B> inline Mat3x4<A,B> pr_vectorcall Slerp(m3_cref<A,B> lhs, m3_cref<A,B> rhs, float frac)
	{
		if (frac == 0.0f) return lhs;
		if (frac == 1.0f) return rhs;
		return Mat3x4<A,B>{Slerp(Quat<A,B>(lhs), Quat<A,B>(rhs), frac)};
	}
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Matrix3x3Tests)
	{
		std::default_random_engine rng;

		{//OriFromDir
			v4 dir(0,1,0,0);
			{
				auto ori = OriFromDir(dir, AxisId::PosZ, v4ZAxis);
				PR_CHECK(dir == +ori.z, true);
				PR_CHECK(IsOrthonormal(ori), true);
			}
			{
				auto ori = OriFromDir(dir, AxisId::NegX);
				PR_CHECK(dir == -ori.x, true);
				PR_CHECK(IsOrthonormal(ori), true);
			}
			{
				auto scale = 0.125f;
				auto sdir = scale * dir;
				auto ori = ScaledOriFromDir(sdir, AxisId::PosY);
				PR_CHECK(sdir == +ori.y, true);
				PR_CHECK(IsOrthonormal((1/scale) * ori), true);
			}
		}
		{// Inverse
			{
				auto m = Random3x4(rng, Random3N(rng, 0), -float(maths::tau), +float(maths::tau));
				auto inv_m0 = InvertFast(m);
				auto inv_m1 = Invert(m);
				PR_CHECK(FEql(inv_m0, inv_m1), true);
			}{
				auto m = Random3x4(rng, -5.0f, +5.0f);
				auto inv_m = Invert(m);
				auto I0 = inv_m * m;
				auto I1 = m * inv_m;

				PR_CHECK(FEql(I0, m3x4Identity), true);
				PR_CHECK(FEql(I1, m3x4Identity), true);
			}{
				auto m = m3x4(
					v4(0.25f, 0.5f, 1.0f, 0.0f),
					v4(0.49f, 0.7f, 1.0f, 0.0f),
					v4(1.0f, 1.0f, 1.0f, 0.0f));
				auto INV_M = m3x4(
					v4(10.0f, -16.666667f, 6.66667f, 0.0f),
					v4(-17.0f, 25.0f, -8.0f, 0.0f),
					v4(7.0f, -8.333333f, 2.333333f, 0.0f));

				auto inv_m = Invert(m);
				PR_CHECK(FEqlRelative(inv_m, INV_M, 0.0001f), true);
			}
		}
		{// CPM
			{
				auto v = v4(2.0f, -1.0f, 4.0f, 0.0f);
				auto m = CPM(v);

				auto a0 = Random3(rng, v4Origin, 5.0f, 0.0f);
				auto A0 = m * a0;
				auto A1 = Cross(v, a0);

				PR_CHECK(FEql(A0, A1), true);
			}
		}
	}
}
#endif