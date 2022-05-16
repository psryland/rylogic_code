//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/vector3.h"
#include "pr/maths/quaternion.h"
#include "pr/maths/axis_id.h"

namespace pr
{
	template <typename A, typename B>
	struct alignas(16) Mat3x4f
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union {
		struct { Vec4f<void> x, y, z; };
		struct { Vec4f<void> arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Mat3x4f() = default;
		constexpr explicit Mat3x4f(float x_)
			:x(x_)
			,y(x_)
			,z(x_)
		{}
		constexpr Mat3x4f(v3_cref<> x_, v3_cref<> y_, v3_cref<> z_)
			:x(x_, 0)
			,y(y_, 0)
			,z(z_, 0)
		{
			//assert(maths::is_aligned(this));
		}
		constexpr Mat3x4f(v4_cref<> x_, v4_cref<> y_, v4_cref<> z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{
			//assert(maths::is_aligned(this));
		}
		constexpr explicit Mat3x4f(float const* v) // 12 floats
			:Mat3x4f(Vec4f<void>(&v[0]), Vec4f<void>(&v[4]), Vec4f<void>(&v[8]))
		{}
		template <maths::Matrix3 M> constexpr explicit Mat3x4f(M const& m)
			:Mat3x4f(maths::comp<0>(m), maths::comp<1>(m), maths::comp<2>(m))
		{}

		// Reinterpret as a different matrix type
		template <typename U, typename V> explicit operator Mat3x4f<U, V> const&() const
		{
			return reinterpret_cast<Mat3x4f<U, V> const&>(*this);
		}
		template <typename U, typename V> explicit operator Mat3x4f<U, V>&()
		{
			return reinterpret_cast<Mat3x4f<U, V>&>(*this);
		}

		// Array access
		Vec4f<void> const& operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		Vec4f<void>& operator [](int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Get/Set by row or column. Note: x,y,z are column vectors
		Vec4f<void> col(int i) const
		{
			return arr[i];
		}
		Vec4f<void> row(int i) const
		{
			return Vec4f<void>{x[i], y[i], z[i], 0};
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

		//// Create a 4x4 matrix with this matrix as the rotation part
		//Mat4x4<A,B> w0() const
		//{
		//	return Mat4x4<A, B>(*this, v4{0, 0, 0, 1});
		//}
		//Mat4x4<A,B> w1(v4_cref<> pos) const
		//{
		//	assert("'pos' must be a position vector" && pos.w == 1);
		//	return Mat4x4<A,B>{*this, pos};
		//}
		//// Convert to a 4x4 matrix with zero translation
		//Mat4x4<A,B> m4x4() const
		//{
		//	return Mat4x4<A, B>{*this, {0, 0, 0, 1}};
		//}

		// Basic constants
		static constexpr Mat3x4f Zero()
		{
			return Mat3x4f{v4::Zero(), v4::Zero(), v4::Zero()};
		}
		static constexpr Mat3x4f Identity()
		{
			return Mat3x4f{v4::XAxis(), v4::YAxis(), v4::ZAxis()};
		}

		// Construct a rotation matrix from a quaternion
		static Mat3x4f Rotation(Quatf<A,B> const& q)
		{
			assert("'quat' is a zero quaternion" && (q != quat{}));
			auto s = 2.0f / LengthSq(q);

			Mat3x4f<A,B> m;
			float xs = q.x *  s, ys = q.y *  s, zs = q.z *  s;
			float wx = q.w * xs, wy = q.w * ys, wz = q.w * zs;
			float xx = q.x * xs, xy = q.x * ys, xz = q.x * zs;
			float yy = q.y * ys, yz = q.y * zs, zz = q.z * zs;
			m.x = Vec4f<void>{1.0f - (yy + zz), xy + wz, xz - wy, 0};
			m.y = Vec4f<void>{xy - wz, 1.0f - (xx + zz), yz + wx, 0};
			m.z = Vec4f<void>{xz + wy, yz - wx, 1.0f - (xx + yy), 0};
			return m;
		}

		// Construct a rotation matrix. Order is: roll, pitch, yaw (to match DirectX)
		static Mat3x4f Rotation(float pitch, float yaw, float roll)
		{
			float cos_p = Cos(pitch), sin_p = Sin(pitch);
			float cos_y = Cos(yaw  ), sin_y = Sin(yaw  );
			float cos_r = Cos(roll ), sin_r = Sin(roll );
			return Mat3x4f{
				Vec4f<void>( cos_y*cos_r + sin_y*sin_p*sin_r , cos_p*sin_r , -sin_y*cos_r + cos_y*sin_p*sin_r , 0.0f),
				Vec4f<void>(-cos_y*sin_r + sin_y*sin_p*cos_r , cos_p*cos_r ,  sin_y*sin_r + cos_y*sin_p*cos_r , 0.0f),
				Vec4f<void>( sin_y*cos_p                     ,      -sin_p ,                      cos_y*cos_p , 0.0f)};
		}

		// Create from an axis, angle
		static Mat3x4f pr_vectorcall Rotation(v4_cref<> axis_norm, v4_cref<> axis_sine_angle, float cos_angle)
		{
			assert("'axis_norm' should be normalised" && IsNormal(axis_norm));

			auto m = Mat3x4f{};
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
		static Mat3x4f pr_vectorcall Rotation(v4_cref<> axis_norm, float angle)
		{
			return Rotation(axis_norm, axis_norm * pr::Sin(angle), pr::Cos(angle));
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat3x4f pr_vectorcall Rotation(v4_cref<> angular_displacement)
		{
			assert("'angular_displacement' should be a scaled direction vector" && angular_displacement.w == 0);
			auto len = Length(angular_displacement);
			return len > maths::tinyf
				? Mat3x4f::Rotation(angular_displacement / len, len)
				: Mat3x4f(v4{1, 0, 0, 0}, v4{0, 1, 0, 0}, v4{0, 0, 1, 0});
		}

		// Create a transform representing the rotation from one vector to another. (Vectors do not need to be normalised)
		static Mat3x4f pr_vectorcall Rotation(v4_cref<> from, v4_cref<> to)
		{
			assert(!FEql(from, v4{}));
			assert(!FEql(to  , v4{}));
			auto len = Length(from) * Length(to);

			auto cos_angle = Dot3(from, to) / len;
			if (cos_angle >= 1.0f - maths::tinyf) return Identity();
			if (cos_angle <= maths::tinyf - 1.0f) return Rotation(Normalise(Perpendicular(from - to)), maths::tau_by_2f);

			auto axis_size_angle = Cross3(from, to) / len;
			auto axis_norm = Normalise(axis_size_angle);
			return Rotation(axis_norm, axis_size_angle, cos_angle);
		}

		// Create a transform from one basis axis to another. Remember AxisId can be cast to v4
		static Mat3x4f Rotation(AxisId from_axis, AxisId to_axis)
		{
			// 'o2f' = the rotation from Z to 'from_axis'
			// 'o2t' = the rotation from Z to 'to_axis'
			// 'f2t' = o2t * Invert(o2f)
			Mat3x4f o2f, o2t;
			switch (from_axis)
			{
			case -1: o2f = Mat3x4f::Rotation(0.0f, +float(maths::tau_by_4), 0.0f); break;
			case +1: o2f = Mat3x4f::Rotation(0.0f, -float(maths::tau_by_4), 0.0f); break;
			case -2: o2f = Mat3x4f::Rotation(+float(maths::tau_by_4), 0.0f, 0.0f); break;
			case +2: o2f = Mat3x4f::Rotation(-float(maths::tau_by_4), 0.0f, 0.0f); break;
			case -3: o2f = Mat3x4f::Rotation(0.0f, +float(maths::tau_by_2), 0.0f); break;
			case +3: o2f = Identity(); break;
			default: assert(false && "axis_id must one of +/-1, +/-2, +/-3"); o2f = Identity(); break;
			}
			switch (to_axis)
			{
			case -1: o2t = Mat3x4f::Rotation(0.0f, -float(maths::tau_by_4), 0.0f); break; // I know this sign looks wrong, but it isn't. Must be something to do with signs passed to cos()/sin()
			case +1: o2t = Mat3x4f::Rotation(0.0f, +float(maths::tau_by_4), 0.0f); break;
			case -2: o2t = Mat3x4f::Rotation(+float(maths::tau_by_4), 0.0f, 0.0f); break;
			case +2: o2t = Mat3x4f::Rotation(-float(maths::tau_by_4), 0.0f, 0.0f); break;
			case -3: o2t = Mat3x4f::Rotation(0.0f, +float(maths::tau_by_2), 0.0f); break;
			case +3: o2t = Identity(); break;
			default: assert(false && "axis_id must one of +/-1, +/-2, +/-3"); o2t = Identity(); break;
			}
			return o2t * InvertFast(o2f);
		}

		// Create a scale matrix
		static Mat3x4f Scale(float scale)
		{
			Mat3x4f mat = {};
			mat.x.x = mat.y.y = mat.z.z = scale;
			return mat;
		}
		static Mat3x4f Scale(float sx, float sy, float sz)
		{
			Mat3x4f mat = {};
			mat.x.x = sx;
			mat.y.y = sy;
			mat.z.z = sz;
			return mat;
		}

		// Create a shear matrix
		static Mat3x4f Shear(float sxy, float sxz, float syx, float syz, float szx, float szy)
		{
			Mat3x4f mat = {};
			mat.x = v4(1.0f, sxy, sxz, 0.0f);
			mat.y = v4(syx, 1.0f, syz, 0.0f);
			mat.z = v4(szx, szy, 1.0f, 0.0f);
			return mat;
		}

		// Create a 3x4 matrix containing random values on the interval [min_value, max_value)
		template <typename Rng = std::default_random_engine> static Mat3x4f Random(Rng& rng, float min_value, float max_value)
		{
			std::uniform_real_distribution<float> dist(min_value, max_value);
			Mat3x4f m = {};
			m.x = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			m.y = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			m.z = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			return m;
		}

		// Create a random 3D rotation matrix
		template <typename Rng = std::default_random_engine> static Mat3x4f Random(Rng& rng, v4_cref<> axis, float min_angle, float max_angle)
		{
			std::uniform_real_distribution<float> dist(min_angle, max_angle);
			return Rotation(axis, dist(rng));
		}

		// Create a random 3D rotation matrix
		template <typename Rng = std::default_random_engine> static Mat3x4f Random(Rng& rng)
		{
			return Random(rng, v4::RandomN(rng, 0), 0.0f, maths::tauf);
		}

		#pragma region Operators
		friend constexpr Mat3x4f<A,B> pr_vectorcall operator + (m3_cref<A,B> mat)
		{
			return mat;
		}
		friend constexpr Mat3x4f<A,B> pr_vectorcall operator - (m3_cref<A,B> mat)
		{
			return Mat3x4f<A,B>{-mat.x, -mat.y, -mat.z};
		}
		friend Mat3x4f<A,B> pr_vectorcall operator * (float lhs, m3_cref<A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Mat3x4f<A,B> pr_vectorcall operator * (m3_cref<A,B> lhs, float rhs)
		{
			return Mat3x4f<A,B>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
		}
		friend Mat3x4f<A,B> pr_vectorcall operator / (m3_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat3x4f<A,B>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
		}
		friend Mat3x4f<A,B> pr_vectorcall operator % (m3_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat3x4f<A,B>{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs};
		}
		friend Mat3x4f<A,B> pr_vectorcall operator + (m3_cref<A,B> lhs, m3_cref<A,B> rhs)
		{
			return Mat3x4f<A,B>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Mat3x4f<A,B> pr_vectorcall operator - (m3_cref<A,B> lhs, m3_cref<A,B> rhs)
		{
			return Mat3x4f<A,B>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec4f<B> pr_vectorcall operator * (m3_cref<A,B> lhs, v4_cref<A> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			auto x = _mm_load_ps(lhs.x.arr);
			auto y = _mm_load_ps(lhs.y.arr);
			auto z = _mm_load_ps(lhs.z.arr);

			auto brod1 = _mm_set_ps(0, rhs.x, rhs.x, rhs.x);
			auto brod2 = _mm_set_ps(0, rhs.y, rhs.y, rhs.y);
			auto brod3 = _mm_set_ps(0, rhs.z, rhs.z, rhs.z);

			auto ans = _mm_add_ps(
				_mm_add_ps(
					_mm_mul_ps(brod1, x),
					_mm_mul_ps(brod2, y)),
				_mm_add_ps(
					_mm_mul_ps(brod3, z),
					_mm_set_ps(rhs.w, 0, 0, 0))
			);
		
			return Vec4f<B>{ans};
			#else
			auto lhsT = Transpose(lhs);
			return Vec4f<B>{Dot3(lhsT.x, rhs), Dot3(lhsT.y, rhs), Dot3(lhsT.z, rhs), rhs.w};
			#endif
		}
		friend Vec3f<B> pr_vectorcall operator * (m3_cref<A,B> lhs, v3_cref<A> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			auto x = _mm_load_ps(lhs.x.arr);
			auto y = _mm_load_ps(lhs.y.arr);
			auto z = _mm_load_ps(lhs.z.arr);

			auto brod1 = _mm_set_ps(0, rhs.x, rhs.x, rhs.x);
			auto brod2 = _mm_set_ps(0, rhs.y, rhs.y, rhs.y);
			auto brod3 = _mm_set_ps(0, rhs.z, rhs.z, rhs.z);

			auto ans = _mm_add_ps(
				_mm_add_ps(
					_mm_mul_ps(brod1, x),
					_mm_mul_ps(brod2, y)),
				_mm_mul_ps(brod3, z));
		
			return Vec3f<B>{ans.m128_f32[0], ans.m128_f32[1], ans.m128_f32[2]};
			#else
			auto lhsT = Transpose3x3(lhs);
			return Vec3f<B>{Dot3(lhsT.x.xyz, rhs), Dot3(lhsT.y.xyz, rhs), Dot3(lhsT.z.xyz, rhs)};
			#endif
		}
		template <typename C> friend Mat3x4f<A,C> pr_vectorcall operator * (m3_cref<B,C> lhs, m3_cref<A,B> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			auto ans = Mat3x4f<A,C>{};
			auto x = _mm_load_ps(lhs.x.arr);
			auto y = _mm_load_ps(lhs.y.arr);
			auto z = _mm_load_ps(lhs.z.arr);
			for (int i = 0; i != 3; ++i)
			{
				auto brod1 = _mm_set_ps(0, rhs.arr[i].x, rhs.arr[i].x, rhs.arr[i].x);
				auto brod2 = _mm_set_ps(0, rhs.arr[i].y, rhs.arr[i].y, rhs.arr[i].y);
				auto brod3 = _mm_set_ps(0, rhs.arr[i].z, rhs.arr[i].z, rhs.arr[i].z);
				auto row = _mm_add_ps(
					_mm_add_ps(
						_mm_mul_ps(brod1, x),
						_mm_mul_ps(brod2, y)),
					_mm_mul_ps(brod3, z));

				_mm_store_ps(ans.arr[i].arr, row);
			}
			return ans;
			#else
			auto ans = Mat3x4f<A,C>{};
			auto lhsT = Transpose(lhs);
			ans.x = Vec4f<void>{Dot3(lhsT.x, rhs.x), Dot3(lhsT.y, rhs.x), Dot3(lhsT.z, rhs.x), 0};
			ans.y = Vec4f<void>{Dot3(lhsT.x, rhs.y), Dot3(lhsT.y, rhs.y), Dot3(lhsT.z, rhs.y), 0};
			ans.z = Vec4f<void>{Dot3(lhsT.x, rhs.z), Dot3(lhsT.y, rhs.z), Dot3(lhsT.z, rhs.z), 0};
			return ans;
			#endif
		}
		#pragma endregion
	};
	static_assert(sizeof(Mat3x4f<void,void>) == 3*16);
	static_assert(maths::Matrix3<Mat3x4f<void,void>>);
	static_assert(std::is_trivially_copyable_v<Mat3x4f<void,void>>, "Should be a pod type");
	static_assert(std::alignment_of_v<Mat3x4f<void,void>> == 16, "Should be 16 byte aligned");

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
	template <typename A, typename B> inline Vec4f<void> pr_vectorcall Kernel(m3_cref<A,B> mat)
	{
		return Vec4f<void>{mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0};
	}

	// Return the diagonal elements of 'mat'
	template <typename A, typename B> inline Vec4f<void> pr_vectorcall Diagonal(m3_cref<A,B> mat)
	{
		return Vec4f<void>{mat.x.x, mat.y.y, mat.z.z, 0};
	}

	// Create a cross product matrix for 'vec'.
	template <typename A> inline Mat3x4f<A,A> pr_vectorcall CPM(v4_cref<A> vec)
	{
		// This matrix can be used to calculate the cross product with
		// another vector: e.g. Cross3(v1, v2) == CPM(v1) * v2
		return Mat3x4f<A,A>{
			Vec4f<void>(     0,  vec.z, -vec.y, 0),
			Vec4f<void>(-vec.z,      0,  vec.x, 0),
			Vec4f<void>( vec.y, -vec.x,      0, 0)};
	}

	// Return the transpose of 'mat'
	template <typename A, typename B> inline Mat3x4f<A,B> pr_vectorcall Transpose(m3_cref<A,B> mat)
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
			FEql(LengthSq(mat.x), 1.0f) &&
			FEql(LengthSq(mat.y), 1.0f) &&
			FEql(LengthSq(mat.z), 1.0f) &&
			FEql(Abs(Determinant(mat)), 1.0f);
	}

	// True if 'mat' can be inverted
	template <typename A, typename B> inline bool pr_vectorcall IsInvertible(m3_cref<A,B> mat)
	{
		return Determinant(mat) != 0.0f;
	}

	// True if 'mat' is symmetric
	template <typename A, typename B> inline bool pr_vectorcall IsSymmetric(m3_cref<A,B> mat)
	{
		return
			FEql(mat.x.y, mat.y.x) &&
			FEql(mat.x.z, mat.z.x) &&
			FEql(mat.y.z, mat.z.y);
	}

	// True if 'mat' is anti-symmetric
	template <typename A, typename B> inline bool pr_vectorcall IsAntiSymmetric(m3_cref<A,B> mat)
	{
		return
			FEql(mat.x.y, -mat.y.x) &&
			FEql(mat.x.z, -mat.z.x) &&
			FEql(mat.y.z, -mat.z.y);
	}

	// Invert the orthonormal matrix 'mat'
	template <typename A, typename B> inline Mat3x4f<B,A> pr_vectorcall InvertFast(m3_cref<A,B> mat)
	{
		assert("Matrix is not orthonormal" && IsOrthonormal(mat));
		return static_cast<Mat3x4f<B,A>>(Transpose(mat));
	}

	// Invert the 3x3 matrix 'mat'
	template <typename A, typename B> inline Mat3x4f<B,A> pr_vectorcall Invert(m3_cref<A,B> mat)
	{
		assert("Matrix has no inverse" && IsInvertible(mat));
		auto det = Determinant(mat);
		Mat3x4f<B,A> tmp = {};
		tmp.x = Cross3(mat.y, mat.z) / det;
		tmp.y = Cross3(mat.z, mat.x) / det;
		tmp.z = Cross3(mat.x, mat.y) / det;
		return Transpose(tmp);
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using 'Denman-Beavers' square root iteration. Should converge quadratically
	template <typename A, typename B> inline Mat3x4f<A,B> pr_vectorcall Sqrt(m3_cref<A,B> mat)
	{
		auto a = mat;                     // Converges to mat^0.5
		auto b = Mat3x4f<A,B>::Identity(); // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto a_next = 0.5f * (a + Invert(b));
			auto b_next = 0.5f * (b + Invert(a));
			a = a_next;
			b = b_next;
		}
		return a;
	}

	// Orthonormalises the rotation component of 'mat'
	template <typename A, typename B> inline Mat3x4f<A,B> pr_vectorcall Orthonorm(m3_cref<A,B> mat)
	{
		auto m = mat;
		m.x = Normalise(m.x);
		m.y = Normalise(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		return m;
	}

	// Return the axis and angle of a rotation matrix
	template <typename A, typename B> inline void pr_vectorcall GetAxisAngle(m3_cref<A,B> mat, Vec4f<void>& axis, float& angle)
	{
		assert("Matrix is not a pure rotation matrix" && IsOrthonormal(mat));

		angle = ACos(0.5f * (Trace(mat) - 1.0f));
		axis = 1000.0f * Kernel(Mat3x4f<A,B>::Identity() - mat);
		if (axis == v4{})
		{
			axis = v4{1, 0, 0, 0};
			angle = 0.0f;
			return;
		}
		
		axis = Normalise(axis);
		if (axis == v4{})
		{
			axis = v4{1, 0, 0, 0};
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
	template <typename A, typename B> inline Vec4f<void> pr_vectorcall GetEulerAngles(m3_cref<A,B> mat)
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
		if (abs(mat.z.y) > 1.0f - maths::tinyf)
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
	template <typename A, typename B> inline Mat3x4f<A,B> pr_vectorcall Diagonalise3x3(m3_cref<A,B> mat_, Mat3x4f<A,B>& eigen_vectors, Vec4f<void>& eigen_values)
	{
		struct L
		{
			static void Rotate(Mat3x4f<A,B>& mat, int i, int j, int k, int l, float s, float tau)
			{
				auto temp = mat[j][i];
				auto h    = mat[l][k];
				mat[j][i] = temp - s * (h + temp * tau);
				mat[l][k] = h    + s * (temp - h * tau);
			}
		};

		// Initialise the Eigen values and b to be the diagonal elements of 'mat'
		Vec4f<void> b;
		eigen_values.x = b.x = mat_.x.x;
		eigen_values.y = b.y = mat_.y.y;
		eigen_values.z = b.z = mat_.z.z;
		eigen_values.w = b.w = 0.0f;
		eigen_vectors = Mat3x4f<A,B>::Identity();

		Mat3x4f<A,B> mat = mat_;
		float sum;
		float const diagonal_eps = 1.0e-4f;
		do
		{
			auto z = v4{};

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
	template <typename A> inline Mat3x4f<A,A> RotationToZAxis(v4_cref<A> from)
	{
		auto r = Sqr(from.x) + Sqr(from.y);
		auto d = Sqrt(r);
		Mat3x4f<A,A> mat = {};
		if (FEql(d, 0.0f))
		{
			mat = Mat3x4f<A,A>::Identity(); // Create an identity transform or a 180 degree rotation
			mat.x.x = from.z;              // about Y depending on the sign of 'from.z'
			mat.z.z = from.z;
		}
		else
		{
			mat.x = v4{from.x*from.z/d, -from.y/d, from.x, 0};
			mat.y = v4{from.y*from.z/d,  from.x/d, from.y, 0};
			mat.z = v4{           -r/d,      0.0f, from.z, 0};
		}
		return mat;
	}

	// Permute the vectors in a rotation matrix by 'n'.
	// n == 0 : x  y  z
	// n == 1 : z  x  y
	// n == 2 : y  z  x
	template <typename A, typename B> inline Mat3x4f<A,B> pr_vectorcall PermuteRotation(m3_cref<A,B> mat, int n)
	{
		switch (n%3)
		{
		default:return mat;
		case 1: return Mat3x4f<A,B>{mat.z, mat.x, mat.y};
		case 2: return Mat3x4f<A,B>{mat.y, mat.z, mat.x};
		}
	}

	// Make an orientation matrix from a direction vector
	// 'dir' is the direction to align the axis 'axis_id' to. (Doesn't need to be normalised)
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be chosen.
	template <typename A = void, typename B = void> inline Mat3x4f<A,B> pr_vectorcall OriFromDir(v4_cref<> dir, AxisId axis_id, v4_cref<> up_)
	{
		assert("'dir' cannot be a zero vector" && dir != v4{});

		// Get the preferred up direction (handling parallel cases)
		auto up = Perpendicular(dir, up_);

		Mat3x4f<A,B> ori = {};
		ori.z = Normalise(Sign(float(axis_id)) * dir);
		ori.x = Normalise(Cross3(up, ori.z));
		ori.y = Cross3(ori.z, ori.x);

		// Permute the column vectors so +Z becomes 'axis'
		return PermuteRotation(ori, abs(axis_id));
	}
	template <typename A = void, typename B = void> inline Mat3x4f<A,B> pr_vectorcall OriFromDir(v4_cref<> dir, AxisId axis_id)
	{
		return OriFromDir(dir, axis_id, Perpendicular(dir));
	}

	// Make a scaled orientation matrix from a direction vector
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	template <typename A = void, typename B = void> inline Mat3x4f<A,B> pr_vectorcall ScaledOriFromDir(v4_cref<> dir, AxisId axis, v4_cref<> up)
	{
		auto len = Length(dir);
		return len > pr::maths::tinyf ? OriFromDir(dir, axis, up) * Mat3x4f<A,B>::Scale(len) : m3x4::Zero();
	}
	template <typename A = void, typename B = void> inline Mat3x4f<A,B> pr_vectorcall ScaledOriFromDir(v4_cref<> dir, AxisId axis)
	{
		return ScaledOriFromDir(dir, axis, Perpendicular(dir));
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	template <typename A, typename B> inline Vec4f<void> pr_vectorcall RotationVectorApprox(m3_cref<A,B> from, m3_cref<A,B> to)
	{
		assert("This only works for orthonormal matrices" && IsOrthonormal(from) && IsOrthonormal(to));
		
		auto cpm_x_i2wR = to - from;
		auto w2iR = Transpose(from);
		auto cpm = cpm_x_i2wR * w2iR;
		return Vec4f<void>{cpm.y.z, cpm.z.x, cpm.x.y, 0};
	}

	// Spherically interpolate between two rotations
	template <typename A, typename B> inline Mat3x4f<A,B> pr_vectorcall Slerp(m3_cref<A,B> lhs, m3_cref<A,B> rhs, float frac)
	{
		if (frac == 0.0f) return lhs;
		if (frac == 1.0f) return rhs;
		return Mat3x4f<A,B>{Slerp(Quat<A,B>(lhs), Quat<A,B>(rhs), frac)};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Matrix3x3Tests)
	{
		std::default_random_engine rng;
		{// Multiply scalar
			auto m1 = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto m2 = 2.0f;
			auto m3 = m3x4{v4{2,4,6,8}, v4{2,2,2,2}, v4{8,6,4,2}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{// Multiply vector4
			auto m = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto v = v4{-3,4,2,-2};
			auto R = v4{9,4,-1,-2};
			auto r = m * v;
			PR_CHECK(FEql(r, R), true);
		}
		{// Multiply vector3
			auto m = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto v = v3{-3,4,2};
			auto R = v3{9,4,-1};
			auto r = m * v;
			PR_CHECK(FEql(r, R), true);
		}
		{// Multiply matrix
			auto m1 = m3x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{4,3,2,1}};
			auto m2 = m3x4{v4{1,1,1,1}, v4{2,2,2,2}, v4{-2,-2,-2,-2}};
			auto m3 = m3x4{v4{6,6,6,0}, v4{12,12,12,0}, v4{-12,-12,-12,0}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{//OriFromDir
			using namespace pr;
			v4 dir(0,1,0,0);
			{
				auto ori = OriFromDir(dir, AxisId::PosZ, v4::ZAxis());
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
				auto m = m3x4::Random(rng, v4::RandomN(rng, 0), -maths::tauf, +maths::tauf);
				auto inv_m0 = InvertFast(m);
				auto inv_m1 = Invert(m);
				PR_CHECK(FEql(inv_m0, inv_m1), true);
			}{
				auto m = m3x4::Random(rng, -5.0f, +5.0f);
				auto inv_m = Invert(m);
				auto I0 = inv_m * m;
				auto I1 = m * inv_m;

				PR_CHECK(FEql(I0, m3x4::Identity()), true);
				PR_CHECK(FEql(I1, m3x4::Identity()), true);
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

				auto a0 = v4::Random(rng, v4::Zero(), 5.0f, 0);
				auto A0 = m * a0;
				auto A1 = Cross(v, a0);

				PR_CHECK(FEql(A0, A1), true);
			}
		}
	}
}
#endif
