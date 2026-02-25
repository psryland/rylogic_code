//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/types/vector4.h"

namespace pr::math
{
	template <ScalarType S>
	struct Mat3x4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4<S> x, y, z; };
			struct { Vec4<S> arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Mat3x4() = default;
		constexpr explicit Mat3x4(S x_)
			:x(x_)
			,y(x_)
			,z(x_)
		{}
		constexpr Mat3x4(Vec3<S> x_, Vec3<S> y_, Vec3<S> z_)
			:x(x_, S(0))
			,y(y_, S(0))
			,z(z_, S(0))
		{}
		constexpr Mat3x4(Vec4<S> x_, Vec4<S> y_, Vec4<S> z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		constexpr explicit Mat3x4(std::ranges::random_access_range auto&& v) // 12 scalars
			:Mat3x4(
				Vec4<S>(v[0], v[1], v[2], v[3]),
				Vec4<S>(v[4], v[5], v[6], v[7]),
				Vec4<S>(v[8], v[9], v[10], v[11]))
		{}

		// Array access
		constexpr Vec4<S> const& operator [](int i) const
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}
		constexpr Vec4<S>& operator [](int i)
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}

		// Constants
		static constexpr Mat3x4 Zero()
		{
			return Mat3x4{Vec4<S>::Zero(), Vec4<S>::Zero(), Vec4<S>::Zero()};
		}
		static constexpr Mat3x4 Identity()
		{
			return Mat3x4{Vec4<S>::XAxis(), Vec4<S>::YAxis(), Vec4<S>::ZAxis()};
		}

		// Get/Set by row or column. Note: x,y,z are column vectors
		Vec4<S> col(int i) const
		{
			return arr[i];
		}
		Vec4<S> row(int i) const
		{
			return Vec4<S>{x[i], y[i], z[i], 0};
		}
		void col(int i, Vec4<S> col)
		{
			arr[i] = col;
		}
		void row(int i, Vec4<S> row)
		{
			x[i] = row.x;
			y[i] = row.y;
			z[i] = row.z;
		}

		// Return the trace of this matrix
		constexpr Vec4<S> trace() const
		{
			return Trace<Mat3x4>(*this);
		}

		// Return the scale of this matrix
		constexpr Mat3x4 scale() const
		{
			return math::ScaleFrom<Mat3x4>(*this);
		}

		// Return this matrix with the scale removed
		Mat3x4 unscaled() const
		{
			return math::Unscaled<Mat3x4>(*this);
		}

		// Construct a rotation matrix. Order is: roll, pitch, yaw (to match DirectX)
		static Mat3x4 Rotation(S pitch, S yaw, S roll)
		{
			return math::Rotation<Mat3x4>(pitch, yaw, roll);
		}

		// Create from an axis, angle
		static Mat3x4 pr_vectorcall Rotation(Vec4<S> axis_norm, Vec4<S> axis_sine_angle, S cos_angle)
		{
			return math::Rotation<Mat3x4>(axis_norm, axis_sine_angle, cos_angle);
		}

		// Create from an axis and angle. 'axis' should be normalised
		static Mat3x4 pr_vectorcall Rotation(Vec4<S> axis_norm, S angle)
		{
			return math::Rotation<Mat3x4>(axis_norm, angle);
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat3x4 pr_vectorcall Rotation(Vec4<S> angular_displacement) // This is ExpMap3x3.
		{
			return math::Rotation<Mat3x4>(angular_displacement);
		}

		// Create a transform representing the rotation from one vector to another. (Vectors do not need to be normalised)
		static Mat3x4 pr_vectorcall Rotation(Vec4<S> from, Vec4<S> to)
		{
			return math::Rotation<Mat3x4>(from, to);
		}

		// Create a transform from one basis axis to another. Remember AxisId can be cast to Vec4
		static Mat3x4 Rotation(AxisId from_axis, AxisId to_axis)
		{
			return math::Rotation<Mat3x4>(from_axis, to_axis);
		}

		// Create a scale matrix
		static Mat3x4 Scale(S scale)
		{
			return math::Scale<Mat3x4>(scale);
		}
		static Mat3x4 Scale(S sx, S sy, S sz)
		{
			return math::Scale<Mat3x4>(Vec3<S>(sx, sy, sz));
		}
		static Mat3x4 Scale(Vec3<S> scale)
		{
			return math::Scale<Mat3x4>(scale);
		}

		// Create a shear matrix
		static Mat3x4 Shear(S sxy, S sxz, S syx, S syz, S szx, S szy)
		{
			return math::Shear<Mat3x4>(sxy, sxz, syx, syz, szx, szy);
		}
	};

	#define PR_MATH_DEFINE_TYPE(component, element)\
	template <> struct vector_traits<Mat3x4<element>>\
		: vector_traits_base<component, element, 3>\
		, vector_access_member<Mat3x4<element>, element, 3>\
	{};\
	\
	static_assert(VectorType<Mat3x4<element>>, "Mat3x4<"#element"> is not a valid vector type");\
	static_assert(sizeof(Mat3x4<element>) == 3*4*sizeof(element), "Mat3x4<"#element"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Mat3x4<element>>, "Mat3x4<"#element"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(Vec4<float>, float);
	PR_MATH_DEFINE_TYPE(Vec4<double>, double);
	PR_MATH_DEFINE_TYPE(Vec4<int32_t>, int32_t);
	PR_MATH_DEFINE_TYPE(Vec4<int64_t>, int64_t);
	#undef PR_MATH_DEFINE_TYPE
}



#if 0
	struct Mat3x4
	{
		#pragma region Operators
		friend constexpr Mat3x4 pr_vectorcall operator + (Mat3x4_cref<S,A,B> mat)
		{
			return mat;
		}
		friend constexpr Mat3x4 pr_vectorcall operator - (Mat3x4_cref<S,A,B> mat)
		{
			return Mat3x4{-mat.x, -mat.y, -mat.z};
		}
		friend Mat3x4 pr_vectorcall operator * (S lhs, Mat3x4_cref<S,A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Mat3x4 pr_vectorcall operator * (Mat3x4_cref<S,A,B> lhs, S rhs)
		{
			return Mat3x4{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
		}
		friend Mat3x4 pr_vectorcall operator / (Mat3x4_cref<S,A,B> lhs, S rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//pr_assert("divide by zero" && rhs != 0);
			return Mat3x4{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
		}
		friend Mat3x4 pr_vectorcall operator % (Mat3x4_cref<S,A,B> lhs, S rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//pr_assert("divide by zero" && rhs != 0);
			return Mat3x4{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs};
		}
		friend Mat3x4 pr_vectorcall operator + (Mat3x4_cref<S,A,B> lhs, Mat3x4_cref<S,A,B> rhs)
		{
			return Mat3x4{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Mat3x4 pr_vectorcall operator - (Mat3x4_cref<S,A,B> lhs, Mat3x4_cref<S,A,B> rhs)
		{
			return Mat3x4{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec4<S, B> pr_vectorcall operator * (Mat3x4_cref<S, A, B> lhs, Vec4_cref<S, A> rhs)
		{
			if constexpr (Vec4<S, A>::IntrinsicF)
			{
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
				return Vec4<S, B>{ans};
			}
			else
			{
				auto lhsT = Transpose(lhs);
				return Vec4<S, B>{Dot3(lhsT.x, rhs), Dot3(lhsT.y, rhs), Dot3(lhsT.z, rhs), rhs.w};
			}
		}
		friend Vec3<S,B> pr_vectorcall operator * (Mat3x4_cref<S,A,B> lhs, Vec3_cref<S,A> rhs)
		{
			if constexpr (Vec4<S, A>::IntrinsicF)
			{
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

				return Vec3<S, B>{ans.m128_f32[0], ans.m128_f32[1], ans.m128_f32[2]};
			}
			else
			{
				auto lhsT = Transpose(lhs);
				return Vec3<S, B>{Dot(lhsT.x.xyz, rhs), Dot(lhsT.y.xyz, rhs), Dot(lhsT.z.xyz, rhs)};
			}
		}
		template <typename C> friend Mat3x4<S,A,C> pr_vectorcall operator * (Mat3x4_cref<S,B,C> lhs, Mat3x4_cref<S,A,B> rhs)
		{
			if constexpr (Vec4<S, A>::IntrinsicF)
			{
				auto ans = Mat3x4<S, A, C>{};
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
			}
			else
			{
				auto ans = Mat3x4<S, A, C>{};
				auto lhsT = Transpose(lhs);
				ans.x = Vec4<S, void>{Dot3(lhsT.x, rhs.x), Dot3(lhsT.y, rhs.x), Dot3(lhsT.z, rhs.x), S(0)};
				ans.y = Vec4<S, void>{Dot3(lhsT.x, rhs.y), Dot3(lhsT.y, rhs.y), Dot3(lhsT.z, rhs.y), S(0)};
				ans.z = Vec4<S, void>{Dot3(lhsT.x, rhs.z), Dot3(lhsT.y, rhs.z), Dot3(lhsT.z, rhs.z), S(0)};
				return ans;
			}
		}
		#pragma endregion
	};

	// Make a scaled orientation matrix from a direction vector
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	template <Scalar S, typename A> inline Mat3x4<S,A,A> pr_vectorcall ScaledOriFromDir(Vec4_cref<S,A> dir, AxisId axis, Vec4_cref<S,A> up)
	{
		auto len = Length(dir);
		return len > maths::tiny<S> ? OriFromDir(dir, axis, up) * Mat3x4<S,A,A>::Scale(len) : Mat3x4<S,A,A>::Zero();
	}
	template <Scalar S, typename A> inline Mat3x4<S,A,A> pr_vectorcall ScaledOriFromDir(Vec4_cref<S,A> dir, AxisId axis)
	{
		return ScaledOriFromDir(dir, axis, Perpendicular(dir));
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	template <Scalar S, typename A, typename B> inline Vec4<S> pr_vectorcall RotationVectorApprox(Mat3x4_cref<S,A,B> from, Mat3x4_cref<S,A,B> to)
	{
		pr_assert("This only works for orthonormal matrices" && IsOrthonormal(from) && IsOrthonormal(to));
		
		auto cpm_x_i2wR = to - from;
		auto w2iR = Transpose(from);
		auto cpm = cpm_x_i2wR * w2iR;
		return Vec4<S>{cpm.y.z, cpm.z.x, cpm.x.y, S(0)};
	}

	// Spherically interpolate between two rotations
	template <Scalar S, typename A, typename B> inline Mat3x4<S,A,B> pr_vectorcall Slerp(Mat3x4_cref<S,A,B> lhs, Mat3x4_cref<S,A,B> rhs, S frac)
	{
		if (frac == S(0)) return lhs;
		if (frac == S(1)) return rhs;
		return Mat3x4<S,A,B>{Slerp(Quat<S,A,B>(lhs), Quat<S,A,B>(rhs), frac)};
	}

	// Create a cross product matrix for 'vec'.
	template <Scalar S, typename A> inline Mat3x4<S,A,A> pr_vectorcall CPM(Vec4_cref<S,A> vec)
	{
		// This matrix can be used to calculate the cross product with
		// another vector: e.g. Cross3(v1, v2) == CPM(v1) * v2
		return Mat3x4<S,A,A>{
			Vec4<S>(  S(0),  vec.z, -vec.y, S(0)),
			Vec4<S>(-vec.z,   S(0),  vec.x, S(0)),
			Vec4<S>( vec.y, -vec.x,   S(0), S(0))};
	}

	// Return 'exp(omega)' (Rodriges' formula)
	template <Scalar S, typename A> inline Mat3x4<S, A, A> pr_vectorcall ExpMap3x3(Vec4_cref<S, A> omega)
	{
		// Converts an angular velocity into a finite rotation that stays within SO(3).
		// If you have an angular velocity, w, that is constant over a time step,
		// then:
		//   R(t + dt) = R(t) * ExpMap(w * dt)
		//   (no need to orthonormalise)
		//
		// Rodrigues' formula:  exp(omega) = I + (sin(theta)/theta) * omega + ((1 - cos(theta)/theta²) * omega²
		// If you want the shortest rotation from R0 to R1:
		//   R(t) = R0 * ExpMap(t * LogMap(Transpose(R0) * ​R1​))
		return Mat3x4<S, A, A>::Rotation(omega);
	}

	// Returns the Axis*Angle vector representation of a rotation matrix (Inverse of ExpMap)
	template <Scalar S, typename A, typename B> inline Vec4<S, A> pr_vectorcall LogMap(Mat3x4_cref<S, A, B> rot)
	{
		auto cos_angle = Clamp<S>((Trace(rot) - S(1)) / S(2), -S(1), +S(1));
		auto theta = Acos(cos_angle);
		if (Abs(theta) < maths::tiny<S>)
			return Vec4<S, A>::Zero();

		auto s = S(1) / (S(2) * Sin(theta));
		auto axis = s * Vec4<S, A>{rot.y.z - rot.z.y, rot.z.x - rot.x.z, rot.x.y - rot.y.x, S(0)};
		return theta * axis;
	}

	// Evaluates 'ori' after 'time' for a constant angular velocity and angular acceleration
	template <Scalar S, typename A, typename B> inline Mat3x4<S, A, B> pr_vectorcall RotationAt(float time, Mat3x4_cref<S, A, B> ori, v4_cref avel, v4_cref aacc)
	{
		// Orientation can be computed analytically if angular velocity
		// and angular acceleration are parallel or angular acceleration is zero.
		if (LengthSq(Cross(avel, aacc)) < maths::tinyf)
		{
			auto w = avel + aacc * time;
			return ExpMap3x3(w * time) * ori;
		}
		else
		{
			// Otherwise, use the SPIRAL(6) algorithm. 6th order accurate for moderate 'time_s'

			// 3-point Gauss-Legendre nodes for 6th order accuracy
			constexpr float root15f = 3.87298334620741688518f;
			constexpr float c1 = 0.5f - root15f / 10.0f;
			constexpr float c2 = 0.5f;
			constexpr float c3 = 0.5f + root15f / 10.0f;

			// Evaluate instantaneous angular velocity at nodes
			auto w0 = avel + aacc * c1 * time;
			auto w1 = avel + aacc * c2 * time;
			auto w2 = avel + aacc * c3 * time;

			auto u0 = ExpMap3x3(w0 * time / 3.0f);
			auto u1 = ExpMap3x3(w1 * time / 3.0f);
			auto u2 = ExpMap3x3(w2 * time / 3.0f);

			return u2 * u1 * u0 * ori;
		}
	}
#endif