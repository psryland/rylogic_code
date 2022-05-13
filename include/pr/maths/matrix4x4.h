//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/quaternion.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/axis_id.h"

namespace pr
{
	template <typename A, typename B> 
	struct alignas(16) Mat4x4f
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4f<void> x, y, z, w; };
			struct { Mat3x4f<A,B> rot; Vec4f<void> pos; };
			struct { Vec4f<void> arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec[4];
			#endif
		};
		#pragma warning(pop)

		// Notes:
		//  - Don't add Mat4x4f(v4 const& v) or equivalent. It's ambiguous between being this:
		//    x = v4(v.x, v.x, v.x, v.x), y = v4(v.y, v.y, v.y, v.y), etc and
		//    x = v4(v.x, v.y, v.z, v.w), y = v4(v.x, v.y, v.z, v.w), etc...

		// Construct
		Mat4x4f() = default;
		constexpr explicit Mat4x4f(float x_)
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		{}
		constexpr Mat4x4f(v4_cref<> x_, v4_cref<> y_, v4_cref<> z_, v4_cref<> w_)
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		{
			//assert(maths::is_aligned(this));
		}
		constexpr Mat4x4f(m3_cref<A,B> rot_, v4_cref<> pos_)
			:rot(rot_)
			,pos(pos_)
		{
			// Don't assert 'pos.w == 1' here. Not all m4x4's are affine transforms
			//assert(maths::is_aligned(this));
		}
		#if PR_MATHS_USE_INTRINSICS
		Mat4x4f(__m128 const (&mat)[4])
		{
			vec[0] = mat[0];
			vec[1] = mat[1];
			vec[2] = mat[2];
			vec[3] = mat[3];
		}
		#endif

		// Reinterpret as a different matrix type
		template <typename U, typename V> explicit operator Mat4x4f<U, V> const&() const
		{
			return reinterpret_cast<Mat4x4f<U, V> const&>(*this);
		}
		template <typename U, typename V> explicit operator Mat4x4f<U, V>&()
		{
			return reinterpret_cast<Mat4x4f<U, V>&>(*this);
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
			return Vec4f<void>{x[i], y[i], z[i], w[i]};
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
			w[i] = row.w;
		}

		// Create a 4x4 matrix with this matrix as the rotation part
		Mat4x4f w0() const
		{
			return Mat4x4f{rot, v4{0, 0, 0, 1}};
		}
		Mat4x4f w1(v4_cref<> xyz) const
		{
			assert("'pos' must be a position vector" && xyz.w == 1);
			return Mat4x4f{rot, xyz};
		}

		// Basic constants
		static constexpr Mat4x4f Zero()
		{
			return Mat4x4f{v4::Zero(), v4::Zero(), v4::Zero(), v4::Zero()};
		}
		static constexpr Mat4x4f Identity()
		{
			return Mat4x4f{v4::XAxis(), v4::YAxis(), v4::ZAxis(), v4::Origin()};
		}

		// Create a translation matrix
		static Mat4x4f Translation(v4_cref<> xyz)
		{
			// 'xyz' can be a position or an offset
			assert("translation should be an affine vector" && (xyz.w == 0.0f || xyz.w == 1.0f));
			return Mat4x4f(m3x4::Identity(), xyz.w1());
		}
		static Mat4x4f Translation(float x, float y, float z)
		{
			return Translation(Vec4f<void>{x,y,z,1});
		}

		// Create a rotation matrix from Euler angles.  Order is: roll, pitch, yaw (to match DirectX)
		static Mat4x4f Transform(float pitch, float yaw, float roll, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Rotation(pitch, yaw, roll), pos);
		}

		// Create from an axis and angle. 'axis' should be normalised
		static Mat4x4f Transform(v4_cref<> axis, float angle, v4_cref<> pos)
		{
			assert("'axis' should be normalised" && IsNormal(axis));
			return Mat4x4f(Mat3x4f<A,B>::Rotation(axis, angle), pos);
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat4x4f Transform(v4_cref<> angular_displacement, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Rotation(angular_displacement), pos);
		}

		// Create from quaternion
		static Mat4x4f Transform(Quatf<A,B> const& q, v4_cref<> pos)
		{
			assert("'q' should be a normalised quaternion" && IsNormal(q));
			return Mat4x4f(Mat3x4f<A,B>::Rotation(q), pos);
		}

		// Create a transform representing the rotation from one vector to another.
		static Mat4x4f Transform(v4_cref<> from, v4_cref<> to, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Rotation(from, to), pos);
		}

		// Create a transform from one basis axis to another
		static Mat4x4f Transform(AxisId from_axis, AxisId to_axis, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Rotation(from_axis, to_axis), pos);
		}

		// Create a scale matrix
		static Mat4x4f Scale(float scale, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Scale(scale), pos);
		}
		static Mat4x4f Scale(float sx, float sy, float sz, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Scale(sx, sy, sz), pos);
		}

		// Create a shear matrix
		static Mat4x4f Shear(float sxy, float sxz, float syx, float syz, float szx, float szy, v4_cref<> pos)
		{
			return Mat4x4f(Mat3x4f<A,B>::Shear(sxy, sxz, syx, syz, szx, szy), pos);
		}

		// Orientation matrix to "look" at a point
		static Mat4x4f LookAt(v4_cref<> eye, v4_cref<> at, v4_cref<> up)
		{
			assert("Invalid position/direction vectors passed to LookAt" && eye.w == 1.0f && at.w == 1.0f && up.w == 0.0f);
			assert("LookAt 'eye' and 'at' positions are coincident" && eye - at != v4{});
			assert("LookAt 'forward' and 'up' axes are aligned" && !Parallel(eye - at, up, 0.f));
			auto mat = Mat4x4f{};
			mat.z = Normalise(eye - at);
			mat.x = Normalise(Cross3(up, mat.z));
			mat.y = Cross3(mat.z, mat.x);
			mat.pos = eye;
			return mat;
		}

		// Construct an orthographic projection matrix
		static Mat4x4f ProjectionOrthographic(float w, float h, float zn, float zf, bool righthanded)
		{
			assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && (zn - zf) != 0);
			auto rh = Bool2SignF(righthanded);
			auto mat = Mat4x4f{};
			mat.x.x = 2.0f / w;
			mat.y.y = 2.0f / h;
			mat.z.z = rh / (zn - zf);
			mat.w.w = 1.0f;
			mat.w.z = rh * zn / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix. 'w' and 'h' are measured at 'zn'
		static Mat4x4f ProjectionPerspective(float w, float h, float zn, float zf, bool righthanded)
		{
			// Getting your head around perspective transforms:
			//   p0 = c2s * v4(0,0,-zn,1); p0/p0.w = (0,0,0,1)
			//   p1 = c2s * v4(0,0,-zf,1); p1/p1.w = (0,0,1,1)

			assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
			auto rh = Bool2SignF(righthanded);
			auto mat = Mat4x4f{};
			mat.x.x = 2.0f * zn / w;
			mat.y.y = 2.0f * zn / h;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix offset from the centre
		static Mat4x4f ProjectionPerspective(float l, float r, float t, float b, float zn, float zf, bool righthanded)
		{
			assert("invalid view rect" && IsFinite(l)  && IsFinite(r) && IsFinite(t) && IsFinite(b) && (r - l) > 0 && (t - b) > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
			auto rh = Bool2SignF(righthanded);
			auto mat = Mat4x4f{};
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
		static Mat4x4f ProjectionPerspectiveFOV(float fovY, float aspect, float zn, float zf, bool righthanded)
		{
			assert("invalid aspect ratio" && IsFinite(aspect) && aspect > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
			auto rh = Bool2SignF(righthanded);
			auto mat = Mat4x4f{};
			mat.y.y = 1.0f / Tan(fovY/2);
			mat.x.x = mat.y.y / aspect;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}

		// Create a 4x4 matrix contains random values on the interval [min_value, max_value)
		template <typename Rng = std::default_random_engine> static Mat4x4f Random(Rng& rng, float min_value, float max_value)
		{
			std::uniform_real_distribution<float> dist(min_value, max_value);
			Mat4x4f m = {};
			m.x = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			m.y = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			m.z = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			m.w = v4(dist(rng), dist(rng), dist(rng), dist(rng));
			return m;
		}

		// Create an affine transform matrix with a random rotation about 'axis', located at 'position'
		template <typename Rng = std::default_random_engine> static Mat4x4f Random(Rng& rng, v4_cref<> axis, float min_angle, float max_angle, v4_cref<> position)
		{
			std::uniform_real_distribution<float> dist(min_angle, max_angle);
			return Transform(axis, dist(rng), position);
		}

		// Create an affine transform matrix with a random orientation, located at 'position'
		template <typename Rng = std::default_random_engine> static Mat4x4f Random(Rng& rng, v4_cref<> position)
		{
			return Random(rng, v4::RandomN(rng, 0), 0.0f, maths::tauf, position);
		}

		// Create an affine transform matrix with a random rotation about 'axis', located randomly within a sphere [centre, radius]
		template <typename Rng = std::default_random_engine> static Mat4x4f Random(Rng& rng, v4_cref<> axis, float min_angle, float max_angle, v4_cref<> centre, float radius)
		{
			return Random(rng, axis, min_angle, max_angle, centre + v4::Random(rng, 0.0f, radius, 0));
		}

		// Create an affine transform matrix with a random orientation, located randomly within a sphere [centre, radius]
		template <typename Rng = std::default_random_engine> static Mat4x4f Random(Rng& rng, v4_cref<> centre, float radius)
		{
			return Random(rng, v4::RandomN(rng, 0), 0.0f, maths::tauf, centre, radius);
		}

		#pragma region Operators
		friend constexpr Mat4x4f<A,B> pr_vectorcall operator + (m4_cref<A,B> mat)
		{
			return mat;
		}
		friend constexpr Mat4x4f<A,B> pr_vectorcall operator - (m4_cref<A,B> mat)
		{
			return Mat4x4f<A,B>{-mat.x, -mat.y, -mat.z, -mat.w};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator * (float lhs, m4_cref<A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Mat4x4f<A,B> pr_vectorcall operator * (m4_cref<A,B> lhs, float rhs)
		{
			return Mat4x4f<A,B>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator / (m4_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat4x4f<A,B>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator % (m4_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat4x4f<A,B>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator + (m4_cref<A,B> lhs, m4_cref<A,B> rhs)
		{
			return Mat4x4f<A,B>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator - (m4_cref<A,B> lhs, m4_cref<A,B> rhs)
		{
			return Mat4x4f<A,B>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator + (m4_cref<A,B> lhs, m3_cref<A,B> rhs)
		{
			return Mat4x4f<A,B>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Mat4x4f<A,B> pr_vectorcall operator - (m4_cref<A,B> lhs, m3_cref<A,B> rhs)
		{
			return Mat4x4f<A,B>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec4f<B> pr_vectorcall operator * (m4_cref<A,B> a2b, v4_cref<A> v)
		{
			#if PR_MATHS_USE_INTRINSICS
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
		
			return Vec4f<B>{ans};
			#else
			auto a2bT = Transpose4x4(a2b);
			return Vec4f<B>{Dot4(a2bT.x, v), Dot4(a2bT.y, v), Dot4(a2bT.z, v), Dot4(a2bT.w, v)};
			#endif
		}
		template <typename C> friend Mat4x4f<A,C> pr_vectorcall operator * (m4_cref<B,C> b2c, m4_cref<A,B> a2b)
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
			#if PR_MATHS_USE_INTRINSICS
			auto ans = Mat4x4f<A,C>{};
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
			#else
			auto ans = Mat4x4f<A,C>{};
			auto b2cT = Transpose4x4(b2c);
			ans.x = Vec4f<void>(Dot4(b2cT.x, a2b.x), Dot4(b2cT.y, a2b.x), Dot4(b2cT.z, a2b.x), Dot4(b2cT.w, a2b.x));
			ans.y = Vec4f<void>(Dot4(b2cT.x, a2b.y), Dot4(b2cT.y, a2b.y), Dot4(b2cT.z, a2b.y), Dot4(b2cT.w, a2b.y));
			ans.z = Vec4f<void>(Dot4(b2cT.x, a2b.z), Dot4(b2cT.y, a2b.z), Dot4(b2cT.z, a2b.z), Dot4(b2cT.w, a2b.z));
			ans.w = Vec4f<void>(Dot4(b2cT.x, a2b.w), Dot4(b2cT.y, a2b.w), Dot4(b2cT.z, a2b.w), Dot4(b2cT.w, a2b.w));
			return ans;
			#endif
		}
		#pragma endregion
	};
	static_assert(sizeof(Mat4x4f<void,void>) == 4*16);
	static_assert(maths::Matrix4<Mat4x4f<void,void>>);
	static_assert(std::is_trivially_copyable_v<Mat4x4f<void,void>>, "Should be a pod type");
	static_assert(std::alignment_of_v<Mat4x4f<void,void>> == 16, "Should be 16 byte aligned");

	// Return true if 'mat' is an affine transform
	template <typename A, typename B> inline bool pr_vectorcall IsAffine(m4_cref<A,B> mat)
	{
		return
			mat.x.w == 0.0f &&
			mat.y.w == 0.0f &&
			mat.z.w == 0.0f &&
			mat.w.w == 1.0f;
	}

	// Return the determinant of the rotation part of this matrix
	template <typename A, typename B> inline float pr_vectorcall Determinant3(m4_cref<A,B> mat)
	{
		return Triple(mat.x, mat.y, mat.z);
	}

	// Return the 4x4 determinant of the affine transform 'mat'
	template <typename A, typename B> inline float pr_vectorcall DeterminantFast4(m4_cref<A,B> mat)
	{
		assert("'mat' must be an affine transform to use this function" && IsAffine(mat));
		return
			(mat.x.x * mat.y.y * mat.z.z) +
			(mat.x.y * mat.y.z * mat.z.x) +
			(mat.x.z * mat.y.x * mat.z.y) -
			(mat.x.z * mat.y.y * mat.z.x) -
			(mat.x.y * mat.y.x * mat.z.z) -
			(mat.x.x * mat.y.z * mat.z.y);
	}

	// Return the 4x4 determinant of the arbitrary transform 'mat'
	template <typename A, typename B> inline float pr_vectorcall Determinant4(m4_cref<A,B> mat)
	{
		auto c1 = (mat.z.z * mat.w.w) - (mat.z.w * mat.w.z);
		auto c2 = (mat.z.y * mat.w.w) - (mat.z.w * mat.w.y);
		auto c3 = (mat.z.y * mat.w.z) - (mat.z.z * mat.w.y);
		auto c4 = (mat.z.x * mat.w.w) - (mat.z.w * mat.w.x);
		auto c5 = (mat.z.x * mat.w.z) - (mat.z.z * mat.w.x);
		auto c6 = (mat.z.x * mat.w.y) - (mat.z.y * mat.w.x);
		return
			mat.x.x * (mat.y.y*c1 - mat.y.z*c2 + mat.y.w*c3) -
			mat.x.y * (mat.y.x*c1 - mat.y.z*c4 + mat.y.w*c5) +
			mat.x.z * (mat.y.x*c2 - mat.y.y*c4 + mat.y.w*c6) -
			mat.x.w * (mat.y.x*c3 - mat.y.y*c5 + mat.y.z*c6);
	}

	// Returns the sum of the first 3 diagonal elements of 'mat'
	template <typename A, typename B> inline float pr_vectorcall Trace3(m4_cref<A,B> mat)
	{
		return mat.x.x + mat.y.y + mat.z.z;
	}

	// Returns the sum of the diagonal elements of 'mat'
	template <typename A, typename B> inline float pr_vectorcall Trace4(m4_cref<A,B> mat)
	{
		return mat.x.x + mat.y.y + mat.z.z + mat.w.w;
	}

	// Scale each component of 'mat' by the values in 'scale'
	template <typename A, typename B> inline Mat4x4f<A, B> pr_vectorcall CompMul(m4_cref<A, B> mat, v4_cref<> scale)
	{
		return Mat4x4f<A,B>(
			mat.x * scale.x,
			mat.y * scale.y,
			mat.z * scale.z,
			mat.w * scale.w);
	}

	// The kernel of the matrix
	template <typename A, typename B> inline Vec4f<void> pr_vectorcall Kernel(m4_cref<A,B> mat)
	{
		return v4(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}

	// Return the cross product matrix for 'vec'.
	template <typename A> inline Mat4x4f<A,A> pr_vectorcall CPM(v4_cref<A> vec, v4_cref<> pos)
	{
		// This matrix can be used to take the cross product with
		// another vector: e.g. Cross4(v1, v2) == Cross4(v1) * v2
		return Mat4x4f<A,A>{CPM(vec), pos};
	}

	// Return the 4x4 transpose of 'mat'
	template <typename A, typename B> inline Mat4x4f<A,B> pr_vectorcall Transpose4x4(m4_cref<A,B> mat)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto m = mat;
		_MM_TRANSPOSE4_PS(m.x.vec, m.y.vec, m.z.vec, m.w.vec);
		return m;
		#else
		auto m = mat;
		std::swap(m.x.y, m.y.x);
		std::swap(m.x.z, m.z.x);
		std::swap(m.x.w, m.w.x);
		std::swap(m.y.z, m.z.y);
		std::swap(m.y.w, m.w.y);
		std::swap(m.z.w, m.w.z);
		return m;
		#endif
	}

	// Return the 3x3 transpose of 'mat'
	template <typename A, typename B> inline Mat4x4f<A,B> pr_vectorcall Transpose3x3(m4_cref<A,B> mat)
	{
		auto m = mat;
		std::swap(m.x.y, m.y.x);
		std::swap(m.x.z, m.z.x);
		std::swap(m.y.z, m.z.y);
		return m;
	}

	// Return true if this matrix is orthonormal
	template <typename A, typename B> inline bool pr_vectorcall IsOrthonormal(m4_cref<A,B> mat)
	{
		return
			FEql(LengthSq(mat.x), 1.0f) &&
			FEql(LengthSq(mat.y), 1.0f) &&
			FEql(LengthSq(mat.z), 1.0f) &&
			FEql(Abs(Determinant3(mat)), 1.0f);
	}

	// True if 'mat' has an inverse
	template <typename A, typename B> inline bool pr_vectorcall IsInvertible(m4_cref<A,B> mat)
	{
		return Determinant4(mat) != 0;
	}

	// Return the inverse of 'mat' (assuming an orthonormal matrix)
	template <typename A, typename B> inline Mat4x4f<B,A> pr_vectorcall InvertFast(m4_cref<A,B> mat)
	{
		assert("Matrix is not orthonormal" && IsOrthonormal(mat));
		auto m = Mat4x4f<B,A>{Transpose3x3(mat)};
		m.pos.x = -Dot3(mat.x, mat.pos);
		m.pos.y = -Dot3(mat.y, mat.pos);
		m.pos.z = -Dot3(mat.z, mat.pos);
		return m;
	}

	// Return the inverse of 'mat'
	template <typename A, typename B> inline Mat4x4f<B,A> pr_vectorcall Invert(m4_cref<A,B> mat)
	{
		#if 1 // This was lifted from MESA implementation of the GLU library
		Mat4x4f<B,A> inv;
		inv.x = Vec4f<void>{
			+mat.y.y * mat.z.z * mat.w.w - mat.y.y * mat.z.w * mat.w.z - mat.z.y * mat.y.z * mat.w.w + mat.z.y * mat.y.w * mat.w.z + mat.w.y * mat.y.z * mat.z.w - mat.w.y * mat.y.w * mat.z.z,
			-mat.x.y * mat.z.z * mat.w.w + mat.x.y * mat.z.w * mat.w.z + mat.z.y * mat.x.z * mat.w.w - mat.z.y * mat.x.w * mat.w.z - mat.w.y * mat.x.z * mat.z.w + mat.w.y * mat.x.w * mat.z.z,
			+mat.x.y * mat.y.z * mat.w.w - mat.x.y * mat.y.w * mat.w.z - mat.y.y * mat.x.z * mat.w.w + mat.y.y * mat.x.w * mat.w.z + mat.w.y * mat.x.z * mat.y.w - mat.w.y * mat.x.w * mat.y.z,
			-mat.x.y * mat.y.z * mat.z.w + mat.x.y * mat.y.w * mat.z.z + mat.y.y * mat.x.z * mat.z.w - mat.y.y * mat.x.w * mat.z.z - mat.z.y * mat.x.z * mat.y.w + mat.z.y * mat.x.w * mat.y.z};
		inv.y = Vec4f<void>{
			-mat.y.x * mat.z.z * mat.w.w + mat.y.x * mat.z.w * mat.w.z + mat.z.x * mat.y.z * mat.w.w - mat.z.x * mat.y.w * mat.w.z - mat.w.x * mat.y.z * mat.z.w + mat.w.x * mat.y.w * mat.z.z,
			+mat.x.x * mat.z.z * mat.w.w - mat.x.x * mat.z.w * mat.w.z - mat.z.x * mat.x.z * mat.w.w + mat.z.x * mat.x.w * mat.w.z + mat.w.x * mat.x.z * mat.z.w - mat.w.x * mat.x.w * mat.z.z,
			-mat.x.x * mat.y.z * mat.w.w + mat.x.x * mat.y.w * mat.w.z + mat.y.x * mat.x.z * mat.w.w - mat.y.x * mat.x.w * mat.w.z - mat.w.x * mat.x.z * mat.y.w + mat.w.x * mat.x.w * mat.y.z,
			+mat.x.x * mat.y.z * mat.z.w - mat.x.x * mat.y.w * mat.z.z - mat.y.x * mat.x.z * mat.z.w + mat.y.x * mat.x.w * mat.z.z + mat.z.x * mat.x.z * mat.y.w - mat.z.x * mat.x.w * mat.y.z};
		inv.z = Vec4f<void>{
			+mat.y.x * mat.z.y * mat.w.w - mat.y.x * mat.z.w * mat.w.y - mat.z.x * mat.y.y * mat.w.w + mat.z.x * mat.y.w * mat.w.y + mat.w.x * mat.y.y * mat.z.w - mat.w.x * mat.y.w * mat.z.y,
			-mat.x.x * mat.z.y * mat.w.w + mat.x.x * mat.z.w * mat.w.y + mat.z.x * mat.x.y * mat.w.w - mat.z.x * mat.x.w * mat.w.y - mat.w.x * mat.x.y * mat.z.w + mat.w.x * mat.x.w * mat.z.y,
			+mat.x.x * mat.y.y * mat.w.w - mat.x.x * mat.y.w * mat.w.y - mat.y.x * mat.x.y * mat.w.w + mat.y.x * mat.x.w * mat.w.y + mat.w.x * mat.x.y * mat.y.w - mat.w.x * mat.x.w * mat.y.y,
			-mat.x.x * mat.y.y * mat.z.w + mat.x.x * mat.y.w * mat.z.y + mat.y.x * mat.x.y * mat.z.w - mat.y.x * mat.x.w * mat.z.y - mat.z.x * mat.x.y * mat.y.w + mat.z.x * mat.x.w * mat.y.y};
		inv.w = Vec4f<void>{
			-mat.y.x * mat.z.y * mat.w.z + mat.y.x * mat.z.z * mat.w.y + mat.z.x * mat.y.y * mat.w.z - mat.z.x * mat.y.z * mat.w.y - mat.w.x * mat.y.y * mat.z.z + mat.w.x * mat.y.z * mat.z.y,
			+mat.x.x * mat.z.y * mat.w.z - mat.x.x * mat.z.z * mat.w.y - mat.z.x * mat.x.y * mat.w.z + mat.z.x * mat.x.z * mat.w.y + mat.w.x * mat.x.y * mat.z.z - mat.w.x * mat.x.z * mat.z.y,
			-mat.x.x * mat.y.y * mat.w.z + mat.x.x * mat.y.z * mat.w.y + mat.y.x * mat.x.y * mat.w.z - mat.y.x * mat.x.z * mat.w.y - mat.w.x * mat.x.y * mat.y.z + mat.w.x * mat.x.z * mat.y.y,
			+mat.x.x * mat.y.y * mat.z.z - mat.x.x * mat.y.z * mat.z.y - mat.y.x * mat.x.y * mat.z.z + mat.y.x * mat.x.z * mat.z.y + mat.z.x * mat.x.y * mat.y.z - mat.z.x * mat.x.z * mat.y.y};

		auto det = mat.x.x * inv.x.x + mat.x.y * inv.y.x + mat.x.z * inv.z.x + mat.x.w * inv.w.x;
		assert("matrix has no inverse" && det != 0);
		return inv * (1/det);
		#else // Reference implementation
		auto A = Transpose4x4(mat); // Take the transpose so that row operations are faster
		auto B = static_cast<Mat4x4f<B,A>>(m4x4Identity);

		// Loop through columns of 'A'
		for (int j = 0; j != 4; ++j)
		{
			// Select the pivot element: maximum magnitude in this row
			auto pivot = 0; auto val = 0.0f;
			if (j <= 0 && val < Abs(A.x[j])) { pivot = 0; val = Abs(A.x[j]); }
			if (j <= 1 && val < Abs(A.y[j])) { pivot = 1; val = Abs(A.y[j]); }
			if (j <= 2 && val < Abs(A.z[j])) { pivot = 2; val = Abs(A.z[j]); }
			if (j <= 3 && val < Abs(A.w[j])) { pivot = 3; val = Abs(A.w[j]); }
			if (val < maths::tinyf)
			{
				assert("Matrix has no inverse" && false);
				return mat;
			}

			// Interchange rows to put pivot element on the diagonal
			if (pivot != j) // skip if already on diagonal
			{
				std::swap(A[j], A[pivot]);
				std::swap(B[j], B[pivot]);
			}

			// Divide row by pivot element. Pivot element becomes 1.0f
			auto scale = A[j][j];
			A[j] /= scale;
			B[j] /= scale;

			// Subtract this row from others to make the rest of column j zero
			if (j != 0) { scale = A.x[j]; A.x -= scale * A[j]; B.x -= scale * B[j]; }
			if (j != 1) { scale = A.y[j]; A.y -= scale * A[j]; B.y -= scale * B[j]; }
			if (j != 2) { scale = A.z[j]; A.z -= scale * A[j]; B.z -= scale * B[j]; }
			if (j != 3) { scale = A.w[j]; A.w -= scale * A[j]; B.w -= scale * B[j]; }
		}

		// When these operations have been completed, A should have been transformed to the identity matrix
		// and B should have been transformed into the inverse of the original A
		B = Transpose4x4(B);
		return B;
		#endif
	}

	// Return the inverse of 'mat' using double precision floats
	template <typename A, typename B> inline Mat4x4f<B,A> pr_vectorcall InvertPrecise(m4_cref<A,B> mat)
	{
		double inv[4][4];
		inv[0][0] = 0.0 + mat.y.y * mat.z.z * mat.w.w - mat.y.y * mat.z.w * mat.w.z - mat.z.y * mat.y.z * mat.w.w + mat.z.y * mat.y.w * mat.w.z + mat.w.y * mat.y.z * mat.z.w - mat.w.y * mat.y.w * mat.z.z;
		inv[0][1] = 0.0 - mat.x.y * mat.z.z * mat.w.w + mat.x.y * mat.z.w * mat.w.z + mat.z.y * mat.x.z * mat.w.w - mat.z.y * mat.x.w * mat.w.z - mat.w.y * mat.x.z * mat.z.w + mat.w.y * mat.x.w * mat.z.z;
		inv[0][2] = 0.0 + mat.x.y * mat.y.z * mat.w.w - mat.x.y * mat.y.w * mat.w.z - mat.y.y * mat.x.z * mat.w.w + mat.y.y * mat.x.w * mat.w.z + mat.w.y * mat.x.z * mat.y.w - mat.w.y * mat.x.w * mat.y.z;
		inv[0][3] = 0.0 - mat.x.y * mat.y.z * mat.z.w + mat.x.y * mat.y.w * mat.z.z + mat.y.y * mat.x.z * mat.z.w - mat.y.y * mat.x.w * mat.z.z - mat.z.y * mat.x.z * mat.y.w + mat.z.y * mat.x.w * mat.y.z;
		inv[1][0] = 0.0 - mat.y.x * mat.z.z * mat.w.w + mat.y.x * mat.z.w * mat.w.z + mat.z.x * mat.y.z * mat.w.w - mat.z.x * mat.y.w * mat.w.z - mat.w.x * mat.y.z * mat.z.w + mat.w.x * mat.y.w * mat.z.z;
		inv[1][1] = 0.0 + mat.x.x * mat.z.z * mat.w.w - mat.x.x * mat.z.w * mat.w.z - mat.z.x * mat.x.z * mat.w.w + mat.z.x * mat.x.w * mat.w.z + mat.w.x * mat.x.z * mat.z.w - mat.w.x * mat.x.w * mat.z.z;
		inv[1][2] = 0.0 - mat.x.x * mat.y.z * mat.w.w + mat.x.x * mat.y.w * mat.w.z + mat.y.x * mat.x.z * mat.w.w - mat.y.x * mat.x.w * mat.w.z - mat.w.x * mat.x.z * mat.y.w + mat.w.x * mat.x.w * mat.y.z;
		inv[1][3] = 0.0 + mat.x.x * mat.y.z * mat.z.w - mat.x.x * mat.y.w * mat.z.z - mat.y.x * mat.x.z * mat.z.w + mat.y.x * mat.x.w * mat.z.z + mat.z.x * mat.x.z * mat.y.w - mat.z.x * mat.x.w * mat.y.z;
		inv[2][0] = 0.0 + mat.y.x * mat.z.y * mat.w.w - mat.y.x * mat.z.w * mat.w.y - mat.z.x * mat.y.y * mat.w.w + mat.z.x * mat.y.w * mat.w.y + mat.w.x * mat.y.y * mat.z.w - mat.w.x * mat.y.w * mat.z.y;
		inv[2][1] = 0.0 - mat.x.x * mat.z.y * mat.w.w + mat.x.x * mat.z.w * mat.w.y + mat.z.x * mat.x.y * mat.w.w - mat.z.x * mat.x.w * mat.w.y - mat.w.x * mat.x.y * mat.z.w + mat.w.x * mat.x.w * mat.z.y;
		inv[2][2] = 0.0 + mat.x.x * mat.y.y * mat.w.w - mat.x.x * mat.y.w * mat.w.y - mat.y.x * mat.x.y * mat.w.w + mat.y.x * mat.x.w * mat.w.y + mat.w.x * mat.x.y * mat.y.w - mat.w.x * mat.x.w * mat.y.y;
		inv[2][3] = 0.0 - mat.x.x * mat.y.y * mat.z.w + mat.x.x * mat.y.w * mat.z.y + mat.y.x * mat.x.y * mat.z.w - mat.y.x * mat.x.w * mat.z.y - mat.z.x * mat.x.y * mat.y.w + mat.z.x * mat.x.w * mat.y.y;
		inv[3][0] = 0.0 - mat.y.x * mat.z.y * mat.w.z + mat.y.x * mat.z.z * mat.w.y + mat.z.x * mat.y.y * mat.w.z - mat.z.x * mat.y.z * mat.w.y - mat.w.x * mat.y.y * mat.z.z + mat.w.x * mat.y.z * mat.z.y;
		inv[3][1] = 0.0 + mat.x.x * mat.z.y * mat.w.z - mat.x.x * mat.z.z * mat.w.y - mat.z.x * mat.x.y * mat.w.z + mat.z.x * mat.x.z * mat.w.y + mat.w.x * mat.x.y * mat.z.z - mat.w.x * mat.x.z * mat.z.y;
		inv[3][2] = 0.0 - mat.x.x * mat.y.y * mat.w.z + mat.x.x * mat.y.z * mat.w.y + mat.y.x * mat.x.y * mat.w.z - mat.y.x * mat.x.z * mat.w.y - mat.w.x * mat.x.y * mat.y.z + mat.w.x * mat.x.z * mat.y.y;
		inv[3][3] = 0.0 + mat.x.x * mat.y.y * mat.z.z - mat.x.x * mat.y.z * mat.z.y - mat.y.x * mat.x.y * mat.z.z + mat.y.x * mat.x.z * mat.z.y + mat.z.x * mat.x.y * mat.y.z - mat.z.x * mat.x.z * mat.y.y;

		auto det = mat.x.x * inv[0][0] + mat.x.y * inv[1][0] + mat.x.z * inv[2][0] + mat.x.w * inv[3][0];
		assert("matrix has no inverse" && det != 0);
		auto inv_det = 1.0 / det;

		Mat4x4f<B,A> m;
		for (int j = 0; j != 4; ++j)
			for (int i = 0; i != 4; ++i)
				m[j][i] = float(inv[j][i] * inv_det);
		return m;
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	template <typename A, typename B> inline Mat4x4f<A,B> pr_vectorcall Sqrt(m4_cref<A,B> mat)
	{
		// Using 'Denman-Beavers' square root iteration. Should converge quadratically
		auto a = mat;              // Converges to mat^0.5
		auto b = m4x4::Identity(); // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto a_next = 0.5f * (a + Invert(B));
			auto b_next = 0.5f * (b + Invert(A));
			a = a_next;
			b = b_next;
		}
		return a;
	}

	// Orthonormalises the rotation component of the matrix
	template <typename A, typename B> inline Mat4x4f<A,B> pr_vectorcall Orthonorm(m4_cref<A,B> mat)
	{
		auto m = mat;
		m.x = Normalise(m.x);
		m.y = Normalise(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		assert(IsOrthonormal(m));
		return m;
	}

	// Return the axis and angle of a rotation matrix
	template <typename A, typename B> inline void pr_vectorcall GetAxisAngle(m4_cref<A,B> mat, Vec4f<void>& axis, float& angle)
	{
		GetAxisAngle(mat.rot, axis, angle);
	}

	// Make an object to world transform from a direction vector and position
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be chosen.
	template <typename A = void, typename B = void> inline Mat4x4f<A,B> pr_vectorcall OriFromDir(v4_cref<> dir, AxisId axis, v4_cref<> up, v4_cref<> pos)
	{
		return Mat4x4f<A,B>{OriFromDir(dir, axis, up), pos};
	}

	// Make a scaled object to world transform from a direction vector and position
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	template <typename A = void, typename B = void> inline Mat4x4f<A,B> pr_vectorcall ScaledOriFromDir(v4_cref<> dir, AxisId axis, v4_cref<> up, v4_cref<> pos)
	{
		return Mat4x4f<A,B>{ScaledOriFromDir(dir, axis, up), pos};
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	template <typename A, typename B> inline Vec4f<void> pr_vectorcall RotationVectorApprox(m4_cref<A,B> from, m4_cref<A,B> to)
	{
		assert(IsOrthonormal(from) && IsOrthonormal(to) && "This only works for orthonormal matrices");

		auto cpm_x_i2wR = to - from;
		auto w2iR = Transpose3x3(from).w0();
		auto cpm = cpm_x_i2wR * w2iR;
		return Vec4f<void>{cpm.y.z, cpm.z.x, cpm.x.y, 0};
	}

	// Spherically interpolate between two affine transforms
	template <typename A, typename B> inline Mat4x4f<A,B> pr_vectorcall Slerp(m4_cref<A,B> lhs, m4_cref<A,B> rhs, float frac)
	{
		assert(IsAffine(lhs));
		assert(IsAffine(rhs));

		auto q = Slerp(Quatf<void>(lhs.rot), Quatf<void>(rhs.rot), frac);
		auto p = Lerp(lhs.pos, rhs.pos, frac);
		return Mat4x4f<A,B>{q, p};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Matrix4x4Tests)
	{
		using namespace pr;
		std::default_random_engine rng;
		{
			auto m1 = m4x4::Identity();
			auto m2 = m4x4::Identity();
			auto m3 = m1 * m2;
			PR_CHECK(FEql(m3, m4x4::Identity()), true);
		}
		{// Largest/Smallest element
			auto m1 = m4x4{v4{1,2,3,4}, v4{-2,-3,-4,-5}, v4{1,1,-1,9}, v4{-8, 5, 0, 0}};
			PR_CHECK(MinComponent(m1) == -8, true);
			PR_CHECK(MaxComponent(m1) == +9, true);
		}
		{// FEql
			// Equal if the relative difference is less than tiny compared to the maximum element in the matrix.
			auto m1 = m4x4::Identity();
			auto m2 = m4x4::Identity();
			
			m1.x.x = m1.y.y = 1.0e-5f;
			m2.x.x = m2.y.y = 1.1e-5f;
			PR_CHECK(FEql(MaxComponent(m1), 1), true);
			PR_CHECK(FEql(MaxComponent(m2), 1), true);
			PR_CHECK(FEql(m1,m2), true);
			
			m1.z.z = m1.w.w = 1.0e-5f;
			m2.z.z = m2.w.w = 1.1e-5f;
			PR_CHECK(FEql(MaxComponent(m1), 1.0e-5f), true);
			PR_CHECK(FEql(MaxComponent(m2), 1.1e-5f), true);
			PR_CHECK(FEql(m1,m2), false);
		}
		{// Multiply scalar
			auto m1 = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto m2 = 2.0f;
			auto m3 = m4x4{v4{2,4,6,8}, v4{2,2,2,2}, v4{-4,-4,-4,-4}, v4{8,6,4,2}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{// Multiply vector
			auto m = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto v = v4{-3,4,2,-1};
			auto R = v4{-7,-9,-11,-13};
			auto r = m * v;
			PR_CHECK(FEql(r, R), true);
		}
		{// Multiply matrix
			auto m1 = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto m2 = m4x4{v4{1,1,1,1}, v4{2,2,2,2}, v4{-1,-1,-1,-1}, v4{-2,-2,-2,-2}};
			auto m3 = m4x4{v4{4,4,4,4}, v4{8,8,8,8}, v4{-4,-4,-4,-4}, v4{-8,-8,-8,-8}};
			auto r = m1 * m2;
			PR_CHECK(FEql(r, m3), true);
		}
		{// Component multiply
			auto m1 = m4x4{v4{1,2,3,4}, v4{1,1,1,1}, v4{-2,-2,-2,-2}, v4{4,3,2,1}};
			auto m2 = v4(2,1,-2,-1);
			auto m3 = m4x4{v4{2,4,6,8}, v4{1,1,1,1}, v4{+4,+4,+4,+4}, v4{-4,-3,-2,-1}};
			auto r = CompMul(m1, m2);
			PR_CHECK(FEql(r, m3), true);
		}
		{//m4x4Translation
			auto m1 = m4x4(v4::XAxis(), v4::YAxis(), v4::ZAxis(), v4(1.0f, 2.0f, 3.0f, 1.0f));
			auto m2 = m4x4::Translation(v4(1.0f, 2.0f, 3.0f, 1.0f));
			PR_CHECK(FEql(m1, m2), true);
		}
		{//m4x4CreateFrom
			auto V1 = v4::Random(rng, 0.0f, 10.0f, 1);
			auto a2b = m4x4::Transform(v4::Normal(+3,-2,-1,0), +1.23f, v4(+4.4f, -3.3f, +2.2f, 1.0f));
			auto b2c = m4x4::Transform(v4::Normal(-1,+2,-3,0), -3.21f, v4(-1.1f, +2.2f, -3.3f, 1.0f));
			PR_CHECK(IsOrthonormal(a2b), true);
			PR_CHECK(IsOrthonormal(b2c), true);
			v4 V2 = a2b * V1;
			v4 V3 = b2c * V2; V3;
			m4x4 a2c = b2c * a2b;
			v4 V4 = a2c * V1; V4;
			PR_CHECK(FEql(V3, V4), true);
		}
		{//m4x4CreateFrom2
			auto q = quat(1.0f, 0.5f, 0.7f);
			m4x4 m1 = m4x4::Transform(1.0f, 0.5f, 0.7f, v4::Origin());
			m4x4 m2 = m4x4::Transform(q, v4::Origin());
			PR_CHECK(IsOrthonormal(m1), true);
			PR_CHECK(IsOrthonormal(m2), true);
			PR_CHECK(FEql(m1, m2), true);

			std::uniform_real_distribution<float> dist(-1.0f,+1.0f);
			float ang = dist(rng);
			v4 axis = v4::RandomN(rng, 0);
			m1 = m4x4::Transform(axis, ang, v4::Origin());
			m2 = m4x4::Transform(quat(axis, ang), v4::Origin());
			PR_CHECK(IsOrthonormal(m1), true);
			PR_CHECK(IsOrthonormal(m2), true);
			PR_CHECK(FEql(m1, m2), true);
		}
		{// Invert
			m4x4 a2b = m4x4::Transform(v4::Normal(-4,-3,+2,0), -2.15f, v4(-5,+3,+1,1));
			m4x4 b2a = Invert(a2b);
			m4x4 a2a = b2a * a2b;
			PR_CHECK(FEql(m4x4::Identity(), a2a), true);

			m4x4 b2a_fast = InvertFast(a2b);
			PR_CHECK(FEql(b2a_fast, b2a), true);
		}
		{//m4x4Orthonormalise
			m4x4 a2b;
			a2b.x = v4(-2.0f, 3.0f, 1.0f, 0.0f);
			a2b.y = v4(4.0f,-1.0f, 2.0f, 0.0f);
			a2b.z = v4(1.0f,-2.0f, 4.0f, 0.0f);
			a2b.w = v4(1.0f, 2.0f, 3.0f, 1.0f);
			PR_CHECK(IsOrthonormal(Orthonorm(a2b)), true);
		}
	}
}
#endif
