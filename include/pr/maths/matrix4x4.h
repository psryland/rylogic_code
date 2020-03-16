//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/axis_id.h"

namespace pr
{
	template <typename A, typename B> 
	struct alignas(16) Mat4x4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4<void> x, y, z, w; };
			struct { Mat3x4<A,B> rot; Vec4<void> pos; };
			struct { Vec4<void> arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec[4];
			#elif PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec[4];
			#else
			struct { Vec4<void> vec[4]; };
			#endif
		};
		#pragma warning(pop)

		// Notes:
		//  - Don't add Mat4x4(v4 const& v) or equivalent. It's ambiguous between being this:
		//    x = v4(v.x, v.x, v.x, v.x), y = v4(v.y, v.y, v.y, v.y), etc and
		//    x = v4(v.x, v.y, v.z, v.w), y = v4(v.x, v.y, v.z, v.w), etc...

		// Construct
		Mat4x4() = default;
		Mat4x4(v4_cref<> x_, v4_cref<> y_, v4_cref<> z_, v4_cref<> w_)
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		{
			assert(maths::is_aligned(this));
		}
		Mat4x4(m3_cref<A,B> rot_, v4_cref<> pos_)
			:rot(rot_)
			,pos(pos_)
		{
			// Don't assert 'pos.w == 1' here. Not all m4x4's are affine transforms
			assert(maths::is_aligned(this));
		}
		explicit Mat4x4(float x_)
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		{
			assert(maths::is_aligned(this));
		}
		template <typename V4, typename = maths::enable_if_v4<V4>> explicit Mat4x4(float x, float y, float z, float w)
			:Mat4x4(v4(x), v4(y), v4(z), v4(w))
		{}
		#if PR_MATHS_USE_INTRINSICS
		Mat4x4(__m128 const (&mat)[4])
		{
			vec[0] = mat[0];
			vec[1] = mat[1];
			vec[2] = mat[2];
			vec[3] = mat[3];
		}
		#endif

		// Reinterpret as a different matrix type
		template <typename U, typename V> explicit operator Mat4x4<U, V> const&() const
		{
			return reinterpret_cast<Mat4x4<U, V> const&>(*this);
		}
		template <typename U, typename V> explicit operator Mat4x4<U, V>&()
		{
			return reinterpret_cast<Mat4x4<U, V>&>(*this);
		}

		// Construct from DirectX::XMMATRIX (if defined)
		template <typename M, typename = maths::enable_if_dx_mat<M>> Mat4x4(M const& mat, int = 0)
		{
			static_assert(PR_MATHS_USE_DIRECTMATH, "This function shouldn't be instantiated unless PR_MATHS_USE_DIRECTMATH is defined");
			vec[0] = mat.r[0];
			vec[1] = mat.r[1];
			vec[2] = mat.r[2];
			vec[3] = mat.r[3];
		}
		template <typename M, typename = maths::enable_if_dx_mat<M>> Mat4x4(M const& mat, v4_cref<> pos_, int = 0)
		{
			static_assert(PR_MATHS_USE_DIRECTMATH, "This function shouldn't be instantiated unless PR_MATHS_USE_DIRECTMATH is defined");
			vec[0] = mat.r[0];
			vec[1] = mat.r[1];
			vec[2] = mat.r[2];
			vec[3] = pos_.vec;
		}

		// Convert this matrix to a DirectX XMMATRIX (if defined)
		template <typename M = DirectX::XMMATRIX, typename = maths::enable_if_dx_mat<M>> operator DirectX::XMMATRIX const&() const
		{
			return *reinterpret_cast<DirectX::XMMATRIX const*>(&vec[0]);
		}
		template <typename M = DirectX::XMMATRIX, typename = maths::enable_if_dx_mat<M>> operator DirectX::XMMATRIX&()
		{
			return *reinterpret_cast<DirectX::XMMATRIX*>(&vec[0]);
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

		// Get/Set by row or column. Note: x,y,z are column vectors
		Vec4<void> col(int i) const
		{
			return arr[i];
		}
		Vec4<void> row(int i) const
		{
			return Vec4<void>{x[i], y[i], z[i], w[i]};
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
		Mat4x4 w0() const
		{
			return Mat4x4{rot, v4Origin};
		}
		Mat4x4 w1(v4_cref<> xyz) const
		{
			assert("'pos' must be a position vector" && xyz.w == 1);
			return Mat4x4{rot, xyz};
		}

		#pragma region Operators
		friend Mat4x4<A,B> pr_vectorcall operator + (m4_cref<A,B> mat)
		{
			return mat;
		}
		friend Mat4x4<A,B> pr_vectorcall operator - (m4_cref<A,B> mat)
		{
			return Mat4x4<A,B>{-mat.x, -mat.y, -mat.z, -mat.w};
		}
		friend Mat4x4<A,B> pr_vectorcall operator * (float lhs, m4_cref<A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Mat4x4<A,B> pr_vectorcall operator * (m4_cref<A,B> lhs, float rhs)
		{
			return Mat4x4<A,B>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs};
		}
		friend Mat4x4<A,B> pr_vectorcall operator / (m4_cref<A,B> lhs, float rhs)
		{
			assert("divide by zero" && rhs != 0);
			return Mat4x4<A,B>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
		}
		friend Mat4x4<A,B> pr_vectorcall operator % (m4_cref<A,B> lhs, float rhs)
		{
			assert("divide by zero" && rhs != 0);
			return Mat4x4<A,B>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
		}
		friend Mat4x4<A,B> pr_vectorcall operator + (m4_cref<A,B> lhs, m4_cref<A,B> rhs)
		{
			return Mat4x4<A,B>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w};
		}
		friend Mat4x4<A,B> pr_vectorcall operator - (m4_cref<A,B> lhs, m4_cref<A,B> rhs)
		{
			return Mat4x4<A,B>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w};
		}
		friend Mat4x4<A,B> pr_vectorcall operator + (m4_cref<A,B> lhs, m3_cref<A,B> rhs)
		{
			return Mat4x4<A,B>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Mat4x4<A,B> pr_vectorcall operator - (m4_cref<A,B> lhs, m3_cref<A,B> rhs)
		{
			return Mat4x4<A,B>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec4<B> pr_vectorcall operator * (m4_cref<A,B> lhs, v4_cref<A> rhs)
		{
			#if PR_MATHS_USE_DIRECTMATH
			return Vec4<B>{DirectX::XMVector4Transform(rhs.vec, lhs)};
			#elif PR_MATHS_USE_INTRINSICS
			auto x = _mm_load_ps(lhs.x.arr);
			auto y = _mm_load_ps(lhs.y.arr);
			auto z = _mm_load_ps(lhs.z.arr);
			auto w = _mm_load_ps(lhs.w.arr);

			auto brod1 = _mm_set_ps1(rhs.x);
			auto brod2 = _mm_set_ps1(rhs.y);
			auto brod3 = _mm_set_ps1(rhs.z);
			auto brod4 = _mm_set_ps1(rhs.w);

			auto ans = _mm_add_ps(
				_mm_add_ps(
					_mm_mul_ps(brod1, x),
					_mm_mul_ps(brod2, y)),
				_mm_add_ps(
					_mm_mul_ps(brod3, z),
					_mm_mul_ps(brod4, w)));
		
			return Vec4<B>{ans};
			#else
			auto lhsT = Transpose4x4(lhs);
			return Vec4<B>{Dot4(lhsT.x, rhs), Dot4(lhsT.y, rhs), Dot4(lhsT.z, rhs), Dot4(lhsT.w, rhs)};
			#endif
		}
		template <typename C> friend Mat4x4<A,C> pr_vectorcall operator * (m4_cref<B,C> lhs, m4_cref<A,B> rhs)
		{
			#if PR_MATHS_USE_DIRECTMATH
			return Mat4x4<A,C>{DirectX::XMMatrixMultiply(rhs, lhs)};
			#elif PR_MATHS_USE_INTRINSICS
			auto ans = Mat4x4<A,C>{};
			auto x = _mm_load_ps(lhs.x.arr);
			auto y = _mm_load_ps(lhs.y.arr);
			auto z = _mm_load_ps(lhs.z.arr);
			auto w = _mm_load_ps(lhs.w.arr);
			for (int i = 0; i != 4; ++i)
			{
				auto brod1 = _mm_set_ps1(rhs.arr[i].x);
				auto brod2 = _mm_set_ps1(rhs.arr[i].y);
				auto brod3 = _mm_set_ps1(rhs.arr[i].z);
				auto brod4 = _mm_set_ps1(rhs.arr[i].w);
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
			auto ans = Mat4x4<A,C>{};
			auto lhsT = Transpose4x4(lhs);
			ans.x = Vec4<void>(Dot4(lhsT.x, rhs.x), Dot4(lhsT.y, rhs.x), Dot4(lhsT.z, rhs.x), Dot4(lhsT.w, rhs.x));
			ans.y = Vec4<void>(Dot4(lhsT.x, rhs.y), Dot4(lhsT.y, rhs.y), Dot4(lhsT.z, rhs.y), Dot4(lhsT.w, rhs.y));
			ans.z = Vec4<void>(Dot4(lhsT.x, rhs.z), Dot4(lhsT.y, rhs.z), Dot4(lhsT.z, rhs.z), Dot4(lhsT.w, rhs.z));
			ans.w = Vec4<void>(Dot4(lhsT.x, rhs.w), Dot4(lhsT.y, rhs.w), Dot4(lhsT.z, rhs.w), Dot4(lhsT.w, rhs.w));
			return ans;
			#endif
		}
		#pragma endregion

		// Create a translation matrix
		static Mat4x4 Translation(v4_cref<> xyz)
		{
			assert("translation should be a position vector" && xyz.w == 1.0f);
			return Mat4x4(m3x4Identity, xyz);
		}
		static Mat4x4 Translation(float x, float y, float z)
		{
			return Translation(Vec4<void>{x,y,z,1});
		}

		// Create a rotation matrix from Euler angles.  Order is: roll, pitch, yaw (to match DirectX)
		static Mat4x4 Transform(float pitch, float yaw, float roll, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Rotation(pitch, yaw, roll), pos);
		}

		// Create from an axis and angle. 'axis' should be normalised
		static Mat4x4 Transform(v4_cref<> axis, float angle, v4_cref<> pos)
		{
			assert("'axis' should be normalised" && IsNormal3(axis));
			#if PR_MATHS_USE_DIRECTMATH
			return Mat4x4(DirectX::XMMatrixRotationNormal(axis.vec, angle), pos);
			#else
			return Mat4x4(Mat3x4<A,B>::Rotation(axis, angle), pos);
			#endif
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat4x4 Transform(v4_cref<> angular_displacement, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Rotation(angular_displacement), pos);
		}

		// Create from quaternion
		static Mat4x4 Transform(Quat<A,B> const& q, v4_cref<> pos)
		{
			assert("'q' should be a normalised quaternion" && IsNormal4(q));
			#if PR_MATHS_USE_DIRECTMATH
			return Mat4x4(DirectX::XMMatrixRotationQuaternion(q.vec), pos);
			#else
			return Mat4x4(Mat3x4<A,B>(q), pos);
			#endif
		}

		// Create a transform representing the rotation from one vector to another.
		static Mat4x4 Transform(v4_cref<> from, v4_cref<> to, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Rotation(from, to), pos);
		}

		// Create a transform from one basis axis to another
		static Mat4x4 Transform(AxisId from_axis, AxisId to_axis, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Rotation(from_axis, to_axis), pos);
		}

		// Create a scale matrix
		static Mat4x4 Scale(float scale, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Scale(scale), pos);
		}
		static Mat4x4 Scale(float sx, float sy, float sz, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Scale(sx, sy, sz), pos);
		}

		// Create a shear matrix
		static Mat4x4 Shear(float sxy, float sxz, float syx, float syz, float szx, float szy, v4_cref<> pos)
		{
			return Mat4x4(Mat3x4<A,B>::Shear(sxy, sxz, syx, syz, szx, szy), pos);
		}

		// Orientation matrix to "look" at a point
		static Mat4x4 LookAt(v4_cref<> eye, v4_cref<> at, v4_cref<> up)
		{
			assert("Invalid position/direction vectors passed to LookAt" && eye.w == 1.0f && at.w == 1.0f && up.w == 0.0f);
			assert("LookAt 'eye' and 'at' positions are coincident" && eye - at != v4Zero);
			assert("LookAt 'forward' and 'up' axes are aligned" && !Parallel(eye - at, up, 0));
			auto mat = Mat4x4{};
			mat.z = Normalise3(eye - at);
			mat.x = Normalise3(Cross3(up, mat.z));
			mat.y = Cross3(mat.z, mat.x);
			mat.pos = eye;
			return mat;
		}

		// Construct an orthographic projection matrix
		static Mat4x4 ProjectionOrthographic(float w, float h, float zn, float zf, bool righthanded)
		{
			assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && (zn - zf) != 0);
			auto rh = SignF(righthanded);
			auto mat = Mat4x4{};
			mat.x.x = 2.0f / w;
			mat.y.y = 2.0f / h;
			mat.z.z = rh / (zn - zf);
			mat.w.w = 1.0f;
			mat.w.z = rh * zn / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix
		static Mat4x4 ProjectionPerspective(float w, float h, float zn, float zf, bool righthanded)
		{
			assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
			auto rh = SignF(righthanded);
			auto mat = Mat4x4{};
			mat.x.x = 2.0f * zn / w;
			mat.y.y = 2.0f * zn / h;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix offset from the centre
		static Mat4x4 ProjectionPerspective(float l, float r, float t, float b, float zn, float zf, bool righthanded)
		{
			assert("invalid view rect" && IsFinite(l)  && IsFinite(r) && IsFinite(t) && IsFinite(b) && (r - l) > 0 && (t - b) > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
			auto rh = SignF(righthanded);
			auto mat = Mat4x4{};
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
		static Mat4x4 ProjectionPerspectiveFOV(float fovY, float aspect, float zn, float zf, bool righthanded)
		{
			assert("invalid aspect ratio" && IsFinite(aspect) && aspect > 0);
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
			auto rh = SignF(righthanded);
			auto mat = Mat4x4{};
			mat.y.y = 1.0f / Tan(fovY/2);
			mat.x.x = mat.y.y / aspect;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}
	};
	static_assert(maths::is_mat4<Mat4x4<void,void>>::value, "");
	static_assert(std::is_pod<Mat4x4<void,void>>::value, "Should be a pod type");
	static_assert(std::alignment_of<Mat4x4<void,void>>::value == 16, "Should be 16 byte aligned");

	// Define component accessors for pointer types
	template <typename A, typename B> inline v4_cref<> pr_vectorcall x_cp(m4_cref<A,B> v) { return v.x; }
	template <typename A, typename B> inline v4_cref<> pr_vectorcall y_cp(m4_cref<A,B> v) { return v.y; }
	template <typename A, typename B> inline v4_cref<> pr_vectorcall z_cp(m4_cref<A,B> v) { return v.z; }
	template <typename A, typename B> inline v4_cref<> pr_vectorcall w_cp(m4_cref<A,B> v) { return v.w; }

	#pragma region Functions

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
		#if PR_MATHS_USE_DIRECTMATH
		return Vec4<void>{DirectX::XMMatrixDeterminant(mat)}.x;
		#else
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
		#endif
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
	template <typename A, typename B> inline Mat4x4<A, B> pr_vectorcall CompMul(m4_cref<A, B> mat, v4_cref<> scale)
	{
		return Mat4x4<A,B>(
			mat.x * scale.x,
			mat.y * scale.y,
			mat.z * scale.z,
			mat.w * scale.w);
	}

	// The kernel of the matrix
	template <typename A, typename B> inline Vec4<void> pr_vectorcall Kernel(m4_cref<A,B> mat)
	{
		return v4(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}

	// Return the cross product matrix for 'vec'.
	template <typename A> inline Mat4x4<A,A> pr_vectorcall CPM(v4_cref<A> vec, v4_cref<> pos)
	{
		// This matrix can be used to take the cross product with
		// another vector: e.g. Cross4(v1, v2) == Cross4(v1) * v2
		return Mat4x4<A,A>{CPM(vec), pos};
	}

	// Return the 4x4 transpose of 'mat'
	template <typename A, typename B> inline Mat4x4<A,B> pr_vectorcall Transpose4x4(m4_cref<A,B> mat)
	{
		#if PR_MATHS_USE_DIRECTMATH && 0
		return Mat4x4<A,B>{DirectX::XMMatrixTranspose(mat.vec)};
		#elif PR_MATHS_USE_INTRINSICS
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
	template <typename A, typename B> inline Mat4x4<A,B> pr_vectorcall Transpose3x3(m4_cref<A,B> mat)
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
			FEql(Length3Sq(mat.x), 1.0f) &&
			FEql(Length3Sq(mat.y), 1.0f) &&
			FEql(Length3Sq(mat.z), 1.0f) &&
			FEql(Abs(Determinant3(mat)), 1.0f);
	}

	// True if 'mat' has an inverse
	template <typename A, typename B> inline bool pr_vectorcall IsInvertible(m4_cref<A,B> mat)
	{
		return Determinant4(mat) != 0;
	}

	// Return the inverse of 'mat' (assuming an orthonormal matrix)
	template <typename A, typename B> inline Mat4x4<B,A> pr_vectorcall InvertFast(m4_cref<A,B> mat)
	{
		assert("Matrix is not orthonormal" && IsOrthonormal(mat));
		auto m = Mat4x4<B,A>{Transpose3x3(mat)};
		m.pos.x = -Dot3(mat.x, mat.pos);
		m.pos.y = -Dot3(mat.y, mat.pos);
		m.pos.z = -Dot3(mat.z, mat.pos);
		return m;
	}

	// Return the inverse of 'mat'
	template <typename A, typename B> inline Mat4x4<B,A> pr_vectorcall Invert(m4_cref<A,B> mat)
	{
		#if PR_MATHS_USE_DIRECTMATH
		Vec4<void> det;
		Mat4x4<B,A> m{DirectX::XMMatrixInverse(&det.vec, mat)};
		assert("Matrix has no inverse" && det.x != 0);
		return m;
		#elif 1 // This was lifted from MESA implementation of the GLU library
		Mat4x4<B,A> inv;
		inv.x = Vec4<void>{
			+mat.y.y * mat.z.z * mat.w.w - mat.y.y * mat.z.w * mat.w.z - mat.z.y * mat.y.z * mat.w.w + mat.z.y * mat.y.w * mat.w.z + mat.w.y * mat.y.z * mat.z.w - mat.w.y * mat.y.w * mat.z.z,
			-mat.x.y * mat.z.z * mat.w.w + mat.x.y * mat.z.w * mat.w.z + mat.z.y * mat.x.z * mat.w.w - mat.z.y * mat.x.w * mat.w.z - mat.w.y * mat.x.z * mat.z.w + mat.w.y * mat.x.w * mat.z.z,
			+mat.x.y * mat.y.z * mat.w.w - mat.x.y * mat.y.w * mat.w.z - mat.y.y * mat.x.z * mat.w.w + mat.y.y * mat.x.w * mat.w.z + mat.w.y * mat.x.z * mat.y.w - mat.w.y * mat.x.w * mat.y.z,
			-mat.x.y * mat.y.z * mat.z.w + mat.x.y * mat.y.w * mat.z.z + mat.y.y * mat.x.z * mat.z.w - mat.y.y * mat.x.w * mat.z.z - mat.z.y * mat.x.z * mat.y.w + mat.z.y * mat.x.w * mat.y.z};
		inv.y = Vec4<void>{
			-mat.y.x * mat.z.z * mat.w.w + mat.y.x * mat.z.w * mat.w.z + mat.z.x * mat.y.z * mat.w.w - mat.z.x * mat.y.w * mat.w.z - mat.w.x * mat.y.z * mat.z.w + mat.w.x * mat.y.w * mat.z.z,
			+mat.x.x * mat.z.z * mat.w.w - mat.x.x * mat.z.w * mat.w.z - mat.z.x * mat.x.z * mat.w.w + mat.z.x * mat.x.w * mat.w.z + mat.w.x * mat.x.z * mat.z.w - mat.w.x * mat.x.w * mat.z.z,
			-mat.x.x * mat.y.z * mat.w.w + mat.x.x * mat.y.w * mat.w.z + mat.y.x * mat.x.z * mat.w.w - mat.y.x * mat.x.w * mat.w.z - mat.w.x * mat.x.z * mat.y.w + mat.w.x * mat.x.w * mat.y.z,
			+mat.x.x * mat.y.z * mat.z.w - mat.x.x * mat.y.w * mat.z.z - mat.y.x * mat.x.z * mat.z.w + mat.y.x * mat.x.w * mat.z.z + mat.z.x * mat.x.z * mat.y.w - mat.z.x * mat.x.w * mat.y.z};
		inv.z = Vec4<void>{
			+mat.y.x * mat.z.y * mat.w.w - mat.y.x * mat.z.w * mat.w.y - mat.z.x * mat.y.y * mat.w.w + mat.z.x * mat.y.w * mat.w.y + mat.w.x * mat.y.y * mat.z.w - mat.w.x * mat.y.w * mat.z.y,
			-mat.x.x * mat.z.y * mat.w.w + mat.x.x * mat.z.w * mat.w.y + mat.z.x * mat.x.y * mat.w.w - mat.z.x * mat.x.w * mat.w.y - mat.w.x * mat.x.y * mat.z.w + mat.w.x * mat.x.w * mat.z.y,
			+mat.x.x * mat.y.y * mat.w.w - mat.x.x * mat.y.w * mat.w.y - mat.y.x * mat.x.y * mat.w.w + mat.y.x * mat.x.w * mat.w.y + mat.w.x * mat.x.y * mat.y.w - mat.w.x * mat.x.w * mat.y.y,
			-mat.x.x * mat.y.y * mat.z.w + mat.x.x * mat.y.w * mat.z.y + mat.y.x * mat.x.y * mat.z.w - mat.y.x * mat.x.w * mat.z.y - mat.z.x * mat.x.y * mat.y.w + mat.z.x * mat.x.w * mat.y.y};
		inv.w = Vec4<void>{
			-mat.y.x * mat.z.y * mat.w.z + mat.y.x * mat.z.z * mat.w.y + mat.z.x * mat.y.y * mat.w.z - mat.z.x * mat.y.z * mat.w.y - mat.w.x * mat.y.y * mat.z.z + mat.w.x * mat.y.z * mat.z.y,
			+mat.x.x * mat.z.y * mat.w.z - mat.x.x * mat.z.z * mat.w.y - mat.z.x * mat.x.y * mat.w.z + mat.z.x * mat.x.z * mat.w.y + mat.w.x * mat.x.y * mat.z.z - mat.w.x * mat.x.z * mat.z.y,
			-mat.x.x * mat.y.y * mat.w.z + mat.x.x * mat.y.z * mat.w.y + mat.y.x * mat.x.y * mat.w.z - mat.y.x * mat.x.z * mat.w.y - mat.w.x * mat.x.y * mat.y.z + mat.w.x * mat.x.z * mat.y.y,
			+mat.x.x * mat.y.y * mat.z.z - mat.x.x * mat.y.z * mat.z.y - mat.y.x * mat.x.y * mat.z.z + mat.y.x * mat.x.z * mat.z.y + mat.z.x * mat.x.y * mat.y.z - mat.z.x * mat.x.z * mat.y.y};

		auto det = mat.x.x * inv.x.x + mat.x.y * inv.y.x + mat.x.z * inv.z.x + mat.x.w * inv.w.x;
		assert("matrix has no inverse" && det != 0);
		return inv * (1/det);
		#else // Reference implementation
		auto A = Transpose4x4(mat); // Take the transpose so that row operations are faster
		auto B = static_cast<Mat4x4<B,A>>(m4x4Identity);

		// Loop through columns of 'A'
		for (int j = 0; j != 4; ++j)
		{
			// Select the pivot element: maximum magnitude in this row
			auto pivot = 0; auto val = 0.0f;
			if (j <= 0 && val < Abs(A.x[j])) { pivot = 0; val = Abs(A.x[j]); }
			if (j <= 1 && val < Abs(A.y[j])) { pivot = 1; val = Abs(A.y[j]); }
			if (j <= 2 && val < Abs(A.z[j])) { pivot = 2; val = Abs(A.z[j]); }
			if (j <= 3 && val < Abs(A.w[j])) { pivot = 3; val = Abs(A.w[j]); }
			if (val < maths::tiny)
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
	template <typename A, typename B> inline Mat4x4<B,A> pr_vectorcall InvertPrecise(m4_cref<A,B> mat)
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

		Mat4x4<B,A> m;
		for (int j = 0; j != 4; ++j)
			for (int i = 0; i != 4; ++i)
				m[j][i] = float(inv[j][i] * inv_det);
		return m;
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	template <typename A, typename B> inline Mat4x4<A,B> pr_vectorcall Sqrt(m4_cref<A,B> mat)
	{
		// Using 'Denman-Beavers' square root iteration. Should converge quadratically
		auto A = mat;           // Converges to mat^0.5
		auto B = m4x4Identity;  // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto A_next = 0.5f * (A + Invert(B));
			auto B_next = 0.5f * (B + Invert(A));
			A = A_next;
			B = B_next;
		}
		return A;
	}

	// Orthonormalises the rotation component of the matrix
	template <typename A, typename B> inline Mat4x4<A,B> pr_vectorcall Orthonorm(m4_cref<A,B> mat)
	{
		auto m = mat;
		m.x = Normalise3(m.x);
		m.y = Normalise3(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		assert(IsOrthonormal(m));
		return m;
	}

	// Return the axis and angle of a rotation matrix
	template <typename A, typename B> inline void pr_vectorcall GetAxisAngle(m4_cref<A,B> mat, Vec4<void>& axis, float& angle)
	{
		GetAxisAngle(mat.rot, axis, angle);
	}

	// Make an object to world transform from a direction vector and position
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be chosen.
	template <typename A = void, typename B = void> inline Mat4x4<A,B> pr_vectorcall OriFromDir(v4_cref<> dir, int axis, v4_cref<> up, v4_cref<> pos)
	{
		return Mat4x4<A,B>{OriFromDir(dir, axis, up), pos};
	}

	// Make a scaled object to world transform from a direction vector and position
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	template <typename A = void, typename B = void> inline Mat4x4<A,B> pr_vectorcall ScaledOriFromDir(v4_cref<> dir, int axis, v4_cref<> up, v4_cref<> pos)
	{
		return Mat4x4<A,B>{ScaledOriFromDir(dir, axis, up), pos};
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	template <typename A, typename B> inline Vec4<void> pr_vectorcall RotationVectorApprox(m4_cref<A,B> from, m4_cref<A,B> to)
	{
		assert(IsOrthonormal(from) && IsOrthonormal(to) && "This only works for orthonormal matrices");

		auto cpm_x_i2wR = to - from;
		auto w2iR = Transpose3x3(from).w0();
		auto cpm = cpm_x_i2wR * w2iR;
		return Vec4<void>{cpm.y.z, cpm.z.x, cpm.x.y, 0};
	}

	// Spherically interpolate between two affine transforms
	template <typename A, typename B> inline Mat4x4<A,B> pr_vectorcall Slerp(m4_cref<A,B> lhs, m4_cref<A,B> rhs, float frac)
	{
		assert(IsAffine(lhs));
		assert(IsAffine(rhs));

		auto q = Slerp(Quat<void>(lhs.rot), Quat<void>(rhs.rot), frac);
		auto p = Lerp(lhs.pos, rhs.pos, frac);
		return Mat4x4<T>{q, p};
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr::maths
{
	PRUnitTest(Matrix4x4Tests)
	{
		using namespace pr;
		std::default_random_engine rng;
		{
			auto m1 = m4x4Identity;
			auto m2 = m4x4Identity;
			auto m3 = m1 * m2;
			PR_CHECK(FEql(m3, m4x4Identity), true);
		}
		{// Largest/Smallest element
			auto m1 = m4x4{v4{1,2,3,4}, v4{-2,-3,-4,-5}, v4{1,1,-1,9}, v4{-8, 5, 0, 0}};
			PR_CHECK(MinElement(m1) == -8, true);
			PR_CHECK(MaxElement(m1) == +9, true);
		}
		{// FEql
			// Equal if the relative difference is less than tiny compared to the maximum element in the matrix.
			auto m1 = m4x4Identity;
			auto m2 = m4x4Identity;
			
			m1.x.x = m1.y.y = 1.0e-5f;
			m2.x.x = m2.y.y = 1.1e-5f;
			PR_CHECK(FEql(MaxElement(m1), 1), true);
			PR_CHECK(FEql(MaxElement(m2), 1), true);
			PR_CHECK(FEql(m1,m2), true);
			
			m1.z.z = m1.w.w = 1.0e-5f;
			m2.z.z = m2.w.w = 1.1e-5f;
			PR_CHECK(FEql(MaxElement(m1), 1.0e-5f), true);
			PR_CHECK(FEql(MaxElement(m2), 1.1e-5f), true);
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
			auto m1 = m4x4(v4XAxis, v4YAxis, v4ZAxis, v4(1.0f, 2.0f, 3.0f, 1.0f));
			auto m2 = m4x4::Translation(v4(1.0f, 2.0f, 3.0f, 1.0f));
			PR_CHECK(FEql(m1, m2), true);
		}
		{//m4x4CreateFrom
			auto V1 = Random3(rng, 0.0f, 10.0f, 1.0f);
			auto a2b = m4x4::Transform(v4::Normal3(+3,-2,-1,0), +1.23f, v4(+4.4f, -3.3f, +2.2f, 1.0f));
			auto b2c = m4x4::Transform(v4::Normal3(-1,+2,-3,0), -3.21f, v4(-1.1f, +2.2f, -3.3f, 1.0f));
			PR_CHECK(IsOrthonormal(a2b), true);
			PR_CHECK(IsOrthonormal(b2c), true);
			v4 V2 = a2b * V1;
			v4 V3 = b2c * V2; V3;
			m4x4 a2c = b2c * a2b;
			v4 V4 = a2c * V1; V4;
			PR_CHECK(FEql4(V3, V4), true);
		}
		{//m4x4CreateFrom2
			auto q = quat(1.0f, 0.5f, 0.7f);
			m4x4 m1 = m4x4::Transform(1.0f, 0.5f, 0.7f, v4Origin);
			m4x4 m2 = m4x4::Transform(q, v4Origin);
			PR_CHECK(IsOrthonormal(m1), true);
			PR_CHECK(IsOrthonormal(m2), true);
			PR_CHECK(FEql(m1, m2), true);

			std::uniform_real_distribution<float> dist(-1.0f,+1.0f);
			float ang = dist(rng);
			v4 axis = Random3N(rng, 0.0f);
			m1 = m4x4::Transform(axis, ang, v4Origin);
			m2 = m4x4::Transform(quat(axis, ang), v4Origin);
			PR_CHECK(IsOrthonormal(m1), true);
			PR_CHECK(IsOrthonormal(m2), true);
			PR_CHECK(FEql(m1, m2), true);
		}
		{// Invert
			m4x4 a2b = m4x4::Transform(v4::Normal3(-4,-3,+2,0), -2.15f, v4(-5,+3,+1,1));
			m4x4 b2a = Invert(a2b);
			m4x4 a2a = b2a * a2b;
			PR_CHECK(FEql(m4x4Identity, a2a), true);
			{
				#if PR_MATHS_USE_DIRECTMATH
				auto dx_b2a = m4x4(DirectX::XMMatrixInverse(nullptr, a2b));
				PR_CHECK(FEql(b2a, dx_b2a), true);
				#endif
			}

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