//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/types/vector4.h"
#include "pr/math_new/types/matrix3x4.h"
#include "pr/math_new/types/quaternion.h"

namespace pr::math
{
	template <ScalarType S>
	struct Mat4x4
	{
		// Notes:
		//  - Don't add Mat4x4(v4 const& v) or equivalent. It's ambiguous between being this:
		//    x = v4(v.x, v.x, v.x, v.x), y = v4(v.y, v.y, v.y, v.y), etc and
		//    x = v4(v.x, v.y, v.z, v.w), y = v4(v.x, v.y, v.z, v.w), etc...

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4<S> x, y, z, w; };
			struct { Mat3x4<S> rot; Vec4<S> pos; };
			struct { Vec4<S> arr[4]; };
		};
		#pragma warning(pop)

		// Construct
		Mat4x4() = default;
		constexpr explicit Mat4x4(S x_)
			:x(x_)
			, y(x_)
			, z(x_)
			, w(x_)
		{
		}
		constexpr Mat4x4(Vec4<S> x_, Vec4<S> y_, Vec4<S> z_, Vec4<S> w_)
			:x(x_)
			, y(y_)
			, z(z_)
			, w(w_)
		{
		}
		constexpr Mat4x4(Mat3x4<S> const& rot_, Vec4<S> pos_)
			:rot(rot_)
			, pos(pos_)
		{
			// Don't assert 'pos.w == 1' here. Not all m4x4's are affine transforms
		}
		constexpr Mat4x4(Xform<S> const& xform) requires (std::floating_point<S>);

		// Explicit cast to different Scalar type
		template <ScalarType S2> constexpr explicit operator Mat4x4<S2>() const
		{
			return Mat4x4<S2>(
				static_cast<Vec4<S2>>(x),
				static_cast<Vec4<S2>>(y),
				static_cast<Vec4<S2>>(z),
				static_cast<Vec4<S2>>(w)
			);
		}

		// Array access
		constexpr Vec4<S> const& operator [](int i) const
		{
			pr_assert(i >= 0 && i < 4 && "index out of range");
			if consteval { return i == 0 ? x : i == 1 ? y : i == 2 ? z : w; }
			else { return arr[i]; }
		}
		constexpr Vec4<S>& operator [](int i)
		{
			pr_assert(i >= 0 && i < 4 && "index out of range");
			if consteval { return i == 0 ? x : i == 1 ? y : i == 2 ? z : w; }
			else { return arr[i]; }
		}

		// Constants
		static constexpr Mat4x4 Zero()
		{
			return math::Zero<Mat4x4>();
		}
		static constexpr Mat4x4 Identity()
		{
			return math::Identity<Mat4x4>();
		}

		// Get/Set by row or column. Note: x,y,z are column vectors
		constexpr Vec4<S> col(int i) const
		{
			return arr[i];
		}
		constexpr Vec4<S> row(int i) const
		{
			return Vec4<S>{x[i], y[i], z[i], w[i]};
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
			w[i] = row.w;
		}

		// Create a 4x4 matrix with this matrix as the rotation part
		constexpr Mat4x4 w0() const
		{
			return Mat4x4{ rot, Origin<Vec4<S>>() };
		}
		constexpr Mat4x4 w1(Vec4<S> xyz) const
		{
			pr_assert(xyz.w == 1 && "'pos' must be a position vector");
			return Mat4x4{ rot, xyz };
		}

		// Return the diagonal elements of this matrix
		constexpr Vec4<S> diagonal() const
		{
			return math::Diagonal(*this);
		}

		// Return the scale of this matrix
		constexpr Mat4x4 scale() const
		{
			return math::Scale<Mat4x4>(*this);
		}

		// Return this matrix with the scale removed
		constexpr Mat4x4 unscaled() const
		{
			return math::Unscaled<Mat4x4>(*this);
		}

		// Create a translation matrix
		static Mat4x4 Translation(Vec4<S> xyz)
		{
			return math::Translation<Mat4x4>(xyz);
		}
		static Mat4x4 Translation(S x, S y, S z)
		{
			return math::Translation<Mat4x4>(x, y, z);
		}

		// Create a rotation matrix from Euler angles.  Order is: roll, pitch, yaw (to match DirectX)
		static Mat4x4 TransformRad(S pitch, S yaw, S roll, Vec4<S> pos) requires std::floating_point<S>
		{
			return Mat4x4{ math::RotationRad<Mat3x4>(pitch, yaw, roll), pos };
		}
		static Mat4x4 TransformDeg(S pitch, S yaw, S roll, Vec4<S> pos) requires std::floating_point<S>
		{
			return Mat4x4{ math::RotationDeg<Mat3x4>(pitch, yaw, roll), pos };
		}

		// Create from rotation and translation
		static Mat4x4 Transform(Mat3x4<S> const& rot, Vec4<S> pos)
		{
			return Mat4x4{ rot, pos };
		}

		// Create from quaternion + position
		template <typename Q = S> requires std::floating_point<Q>
		static Mat4x4 Transform(Quat<Q> q, Vec4<S> pos)
		{
			return Mat4x4{ math::ToMatrix<Quat<Q>, Mat3x4<Q>>(q), pos };
		}

		// Create from an axis and angle. 'axis' should be normalised
		static Mat4x4 Transform(Vec4<S> axis, S angle, Vec4<S> pos) requires std::floating_point<S>
		{
			return Mat4x4{ math::Rotation<Mat3x4<S>>(axis, angle), pos };
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat4x4 Transform(Vec4<S> angular_displacement, Vec4<S> pos) requires std::floating_point<S>
		{
			return Mat4x4{ math::Rotation<Mat3x4<S>>(angular_displacement), pos };
		}

		// Create a transform representing the rotation from one vector to another. (Vectors do not need to be normalised)
		static Mat4x4 Transform(Vec4<S> from, Vec4<S> to, Vec4<S> pos) requires std::floating_point<S>
		{
			return Mat4x4{ math::Rotation<Mat3x4<S>>(from, to), pos };
		}

		// Create a transform from one basis axis to another
		static Mat4x4 Transform(AxisId from_axis, AxisId to_axis, Vec4<S> pos) requires std::floating_point<S>
		{
			return Mat4x4{ math::Rotation<Mat3x4<S>>(from_axis, to_axis), pos };
		}

		// Create a scale matrix
		static Mat4x4 Scale(S scale, Vec4<S> pos)
		{
			return Mat4x4{ math::Scale<Mat3x4<S>>(scale), pos };
		}
		static Mat4x4 Scale(S sx, S sy, S sz, Vec4<S> pos)
		{
			return Mat4x4{ math::Scale<Mat3x4<S>>(sx, sy, sz), pos };
		}

		// Create a shear matrix
		static Mat4x4 Shear(S sxy, S sxz, S syx, S syz, S szx, S szy, Vec4<S> pos)
		{
			return Mat4x4{ math::Shear<Mat3x4<S>>(sxy, sxz, syx, syz, szx, szy), pos };
		}

		// Orientation matrix to "look" at a point
		static Mat4x4 LookAt(Vec4<S> eye, Vec4<S> at, Vec4<S> up)
		{
			return math::LookAt<Mat4x4>(eye, at, up);
		}

		// Construct an orthographic projection matrix
		static Mat4x4 ProjectionOrthographic(S w, S h, S zn, S zf, bool righthanded)
		{
			return math::ProjectionOrthographic<Mat4x4>(w, h, zn, zf, righthanded);
		}

		// Construct a perspective projection matrix. 'w' and 'h' are measured at 'zn'
		static Mat4x4 ProjectionPerspective(S w, S h, S zn, S zf, bool righthanded)
		{
			return math::ProjectionPerspective<Mat4x4>(w, h, zn, zf, righthanded);
		}

		// Construct a perspective projection matrix offset from the centre
		static Mat4x4 ProjectionPerspective(S l, S r, S t, S b, S zn, S zf, bool righthanded)
		{
			return math::ProjectionPerspective<Mat4x4>(l, r, t, b, zn, zf, righthanded);
		}

		// Construct a perspective projection matrix using field of view
		static Mat4x4 ProjectionPerspectiveFOV(S fovY, S aspect, S zn, S zf, bool righthanded)
		{
			return math::ProjectionPerspectiveFOV<Mat4x4>(fovY, aspect, zn, zf, righthanded);
		}


		#if 0

		// Create a 4x4 matrix contains random values on the interval [min_value, max_value)
		template <typename Rng = std::default_random_engine>
		static Mat4x4 Random(Rng& rng, S min_value, S max_value)
		{
			std::uniform_real_distribution<S> dist(min_value, max_value);
			Mat4x4 m = {};
			m.x = Vec4<S, void>(dist(rng), dist(rng), dist(rng), dist(rng));
			m.y = Vec4<S, void>(dist(rng), dist(rng), dist(rng), dist(rng));
			m.z = Vec4<S, void>(dist(rng), dist(rng), dist(rng), dist(rng));
			m.w = Vec4<S, void>(dist(rng), dist(rng), dist(rng), dist(rng));
			return m;
		}

		// Create an affine transform matrix with a random rotation about 'axis', located at 'position'
		template <typename Rng = std::default_random_engine>
		static Mat4x4 Random(Rng& rng, Vec4<S> axis, S min_angle, S max_angle, Vec4<S> position)
		{
			std::uniform_real_distribution<S> dist(min_angle, max_angle);
			return Transform(axis, dist(rng), position);
		}

		// Create an affine transform matrix with a random orientation, located at 'position'
		template <typename Rng = std::default_random_engine>
		static Mat4x4 Random(Rng& rng, Vec4<S> position)
		{
			return Random(rng, Vec4<S, void>::RandomN(rng, S(0)), S(0), constants<S>::tau, position);
		}

		// Create an affine transform matrix with a random rotation about 'axis', located randomly within a sphere [centre, radius]
		template <typename Rng = std::default_random_engine>
		static Mat4x4 Random(Rng& rng, Vec4<S> axis, S min_angle, S max_angle, Vec4<S> centre, S radius)
		{
			return Random(rng, axis, min_angle, max_angle, centre + Vec4<S, void>::Random(rng, S(0), radius, S(0)));
		}

		// Create an affine transform matrix with a random orientation, located randomly within a sphere [centre, radius]
		template <typename Rng = std::default_random_engine>
		static Mat4x4 Random(Rng& rng, Vec4<S> centre, S radius)
		{
			return Random(rng, Vec4<S, void>::RandomN(rng, S(0)), S(0), constants<S>::tau, centre, radius);
		}

		#pragma region Operators
		friend constexpr Mat4x4 pr_vectorcall operator + (Mat4x4<S> const& mat)
		{
			return mat;
		}
		friend constexpr Mat4x4 pr_vectorcall operator - (Mat4x4<S> const& mat)
		{
			return Mat4x4{ -mat.x, -mat.y, -mat.z, -mat.w };
		}
		friend Mat4x4 pr_vectorcall operator * (S lhs, Mat4x4<S> const& rhs)
		{
			return rhs * lhs;
		}
		friend Mat4x4 pr_vectorcall operator * (Mat4x4<S> const& lhs, S rhs)
		{
			return Mat4x4{ lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs };
		}
		friend Mat4x4 pr_vectorcall operator / (Mat4x4<S> const& lhs, S rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			return Mat4x4{ lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs };
		}
		friend Mat4x4 pr_vectorcall operator % (Mat4x4<S> const& lhs, S rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			if constexpr (std::floating_point<S>)
			{
				return Mat4x4{ Fmod(lhs.x, rhs), Fmod(lhs.y, rhs), Fmod(lhs.z, rhs), Fmod(lhs.w, rhs) };
			}
			else
			{
				return Mat4x4{ lhs.x % rhs, lhs.y % rhs, lhs.z % rhs, lhs.w % rhs };
			}
		}
		friend Mat4x4 pr_vectorcall operator + (Mat4x4<S> const& lhs, Mat4x4<S> const& rhs)
		{
			return Mat4x4{ lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w };
		}
		friend Mat4x4 pr_vectorcall operator - (Mat4x4<S> const& lhs, Mat4x4<S> const& rhs)
		{
			return Mat4x4{ lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w };
		}
		friend Mat4x4 pr_vectorcall operator + (Mat4x4<S> const& lhs, Mat3x4<S> const& rhs)
		{
			return Mat4x4{ lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z };
		}
		friend Mat4x4 pr_vectorcall operator - (Mat4x4<S> const& lhs, Mat3x4<S> const& rhs)
		{
			return Mat4x4{ lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z };
		}
		friend Vec4<S, B> pr_vectorcall operator * (Mat4x4<S> const& a2b, Vec4<S> v)
		{
			if constexpr (Vec4<S, void>::IntrinsicF)
			{
				auto x = _mm_load_ps(a2b.x.arr);
				auto y = _mm_load_ps(a2b.y.arr);
				auto z = _mm_load_ps(a2b.z.arr);
				auto w = _mm_load_ps(a2b.w.arr);

				auto brod1 = _mm_set_ps1(v.x);
				auto brod2 = _mm_set_ps1(v.y);
				auto brod3 = _mm_set_ps1(v.z);
				auto brod4 = _mm_set_ps1(v.w);

				auto ans = _mm_add_ps(
					_mm_add_ps(
						_mm_mul_ps(brod1, x),
						_mm_mul_ps(brod2, y)),
					_mm_add_ps(
						_mm_mul_ps(brod3, z),
						_mm_mul_ps(brod4, w)));

				return Vec4<S, B>{ans};
			}
			else
			{
				auto a2bT = Transpose4x4(a2b);
				return Vec4<S, B>{Dot4(a2bT.x, v), Dot4(a2bT.y, v), Dot4(a2bT.z, v), Dot4(a2bT.w, v)};
			}
		}
		template <typename C> friend Mat4x4<S, A, C> pr_vectorcall operator * (Mat4x4_cref<S, B, C> b2c, Mat4x4<S> const& a2b)
		{
			// Note:
			//  - The reason for this order is because matrices are applied from right to left
			//    e.g.
			//       auto Va =             V = vector in space 'a'
			//       auto Vb =       a2b * V = vector in space 'b'
			//       auto Vc = b2c * a2b * V = vector in space 'c'
			//  - The shape of the result is:
			//       [   ]       [       ]       [   ]
			//       [a2c]       [  b2c  ]       [a2b]
			//       [1x3]   =   [  2x3  ]   *   [1x2]
			//       [   ]       [       ]       [   ]
			if constexpr (Vec4<S, A>::IntrinsicF)
			{
				auto ans = Mat4x4<S, A, C>{};
				auto x = _mm_load_ps(b2c.x.arr);
				auto y = _mm_load_ps(b2c.y.arr);
				auto z = _mm_load_ps(b2c.z.arr);
				auto w = _mm_load_ps(b2c.w.arr);
				for (int i = 0; i != 4; ++i)
				{
					auto brod1 = _mm_set_ps1(a2b.arr[i].x);
					auto brod2 = _mm_set_ps1(a2b.arr[i].y);
					auto brod3 = _mm_set_ps1(a2b.arr[i].z);
					auto brod4 = _mm_set_ps1(a2b.arr[i].w);
					auto row = _mm_add_ps(
						_mm_add_ps(
							_mm_mul_ps(brod1, x),
							_mm_mul_ps(brod2, y)),
						_mm_add_ps(
							_mm_mul_ps(brod3, z),
							_mm_mul_ps(brod4, w)));
					_mm_store_ps(ans.arr[i].arr, row);
				}
				return ans;
			}
			else
			{
				auto ans = Mat4x4<S, A, C>{};
				auto b2cT = Transpose4x4(b2c);
				ans.x = Vec4<S, void>(Dot4(b2cT.x, a2b.x), Dot4(b2cT.y, a2b.x), Dot4(b2cT.z, a2b.x), Dot4(b2cT.w, a2b.x));
				ans.y = Vec4<S, void>(Dot4(b2cT.x, a2b.y), Dot4(b2cT.y, a2b.y), Dot4(b2cT.z, a2b.y), Dot4(b2cT.w, a2b.y));
				ans.z = Vec4<S, void>(Dot4(b2cT.x, a2b.z), Dot4(b2cT.y, a2b.z), Dot4(b2cT.z, a2b.z), Dot4(b2cT.w, a2b.z));
				ans.w = Vec4<S, void>(Dot4(b2cT.x, a2b.w), Dot4(b2cT.y, a2b.w), Dot4(b2cT.z, a2b.w), Dot4(b2cT.w, a2b.w));
				return ans;
			}
		}
		#pragma endregion

		#endif

		// Return the 4x4 transpose of 'mat'
		friend Mat4x4<S> pr_vectorcall Transpose(Mat4x4<S> const& mat) requires (Vec4<S>::IntrinsicF)
		{
			auto m = mat;
			_MM_TRANSPOSE4_PS(m.x.vec, m.y.vec, m.z.vec, m.w.vec);
			return m;
		}
	};

	#define PR_MATH_DEFINE_TYPE(component, element)\
	template <> struct vector_traits<Mat4x4<element>>\
		: vector_traits_base<element, component, 4>\
		, vector_access_member<Mat4x4<element>, component, 4>\
	{};\
	\
	static_assert(VectorType<Mat4x4<element>>, "Mat4x4<"#element"> is not a valid vector type");\
	static_assert(IsRank2<Mat4x4<element>>, "Mat4x4<"#element"> is not rank 2");\
	static_assert(sizeof(Mat4x4<element>) == 4*4*sizeof(element), "Mat4x4<"#element"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Mat4x4<element>>, "Mat4x4<"#element"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(Vec4<float>, float);
	PR_MATH_DEFINE_TYPE(Vec4<double>, double);
	PR_MATH_DEFINE_TYPE(Vec4<int32_t>, int32_t);
	PR_MATH_DEFINE_TYPE(Vec4<int64_t>, int64_t);
	#undef PR_MATH_DEFINE_TYPE

	// Create a 4x4 matrix from this 3x4 matrix
	template <ScalarType S> constexpr Mat4x4<S> Mat3x4<S>::w1() const
	{
		return Mat4x4{ *this, Origin<Vec4<S>>() };
	}
	template <ScalarType S> constexpr Mat4x4<S> Mat3x4<S>::w1(Vec4<S> xyz) const
	{
		return Mat4x4{ *this, xyz };
	}
}