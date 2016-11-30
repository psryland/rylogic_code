//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"
#include "pr/maths/axis_id.h"

namespace pr
{
	// template <typename T> - todo: when MS fix the alignment bug for templates
	struct alignas(16) Mat4x4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { v4 x, y, z, w; };
			struct { m3x4 rot; v4 pos; };
			struct { v4 arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec[4];
			#elif PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec[4];
			#else
			struct { v4 vec[4]; };
			#endif
		};
		#pragma warning(pop)

		// Construct
		Mat4x4() = default;
		Mat4x4(v4 const& x_, v4 const& y_, v4 const& z_, v4 const& w_)
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		{
			assert(maths::is_aligned(this));
		}
		Mat4x4(m3x4 const& rot_, v4 const& pos_)
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
		template <typename T, typename = maths::enable_if_v4<T>> Mat4x4(T const& v)
			:Mat4x4(x_as<v4>(v), y_as<v4>(v), z_as<v4>(v), w_as<v4>(v))
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit Mat4x4(T const* v)
			:Mat4x4(x_as<v4>(v), y_as<v4>(v), z_as<v4>(v), w_as<v4>(v))
		{}
		template <typename T, typename = maths::enable_if_v4<T>> Mat4x4& operator = (T const& rhs)
		{
			x = x_as<v4>(rhs);
			y = y_as<v4>(rhs);
			z = z_as<v4>(rhs);
			w = w_as<v4>(rhs);
			return *this;
		}
		#if PR_MATHS_USE_INTRINSICS
		Mat4x4(__m128 const (&mat)[4])
		{
			vec[0] = mat[0];
			vec[1] = mat[1];
			vec[2] = mat[2];
			vec[3] = mat[3];
		}
		#endif

		// Construct from DirectX::XMMATRIX (if defined)
		template <typename T, typename = maths::enable_if_dx_mat<T>> Mat4x4(T const& mat, int = 0)
		{
			static_assert(PR_MATHS_USE_DIRECTMATH, "This function shouldn't be instantiated unless PR_MATHS_USE_DIRECTMATH is defined");
			vec[0] = mat.r[0];
			vec[1] = mat.r[1];
			vec[2] = mat.r[2];
			vec[3] = mat.r[3];
		}
		template <typename T, typename = maths::enable_if_dx_mat<T>> Mat4x4(T const& mat, v4 const& pos_, int = 0)
		{
			static_assert(PR_MATHS_USE_DIRECTMATH, "This function shouldn't be instantiated unless PR_MATHS_USE_DIRECTMATH is defined");
			vec[0] = mat.r[0];
			vec[1] = mat.r[1];
			vec[2] = mat.r[2];
			vec[3] = pos_.vec;
		}

		// Convert this matrix to a DirectX XMMATRIX (if defined)
		template <typename T = DirectX::XMMATRIX, typename = maths::enable_if_dx_mat<T>> operator DirectX::XMMATRIX const&() const
		{
			return *reinterpret_cast<DirectX::XMMATRIX const*>(&vec[0]);
		}
		template <typename T = DirectX::XMMATRIX, typename = maths::enable_if_dx_mat<T>> operator DirectX::XMMATRIX&()
		{
			return *reinterpret_cast<DirectX::XMMATRIX*>(&vec[0]);
		}

		// Array access
		v4 const& operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		v4& operator [](int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Get/Set by row or column. Note: x,y,z are column vectors
		v4 col(int i) const
		{
			return arr[i];
		}
		v4 row(int i) const
		{
			return v4(x[i], y[i], z[i], w[i]);
		}
		void col(int i, v4 const& col)
		{
			arr[i] = col;
		}
		void row(int i, v4 const& row)
		{
			x[i] = row.x;
			y[i] = row.y;
			z[i] = row.z;
			w[i] = row.w;
		}

		// Create a 4x4 matrix with this matrix as the rotation part
		Mat4x4 w0() const
		{
			return Mat4x4(rot, v4Origin);
		}
		Mat4x4 w1(v4 const& xyz) const
		{
			assert("'pos' must be a position vector" && xyz.w == 1);
			return Mat4x4(rot, xyz);
		}

		// Create a translation matrix
		static Mat4x4 Translation(v4 const& xyz)
		{
			assert("translation should be a position vector" && xyz.w == 1.0f);
			return Mat4x4(m3x4Identity, xyz);
		}
		static Mat4x4 Translation(float x, float y, float z)
		{
			return Translation(v4(x,y,z,1));
		}

		// Create a rotation matrix from Euler angles.  Order is: roll, pitch, yaw (to match DirectX)
		static Mat4x4 Transform(float pitch, float yaw, float roll, v4 const& pos)
		{
			return Mat4x4(m3x4::Rotation(pitch, yaw, roll), pos);
		}

		// Create from an axis and angle. 'axis' should be normalised
		static Mat4x4 Transform(v4 const& axis, float angle, v4 const& pos)
		{
			assert("'axis' should be normalised" && IsNormal3(axis));
			#if PR_MATHS_USE_DIRECTMATH
			return Mat4x4(DirectX::XMMatrixRotationNormal(axis.vec, angle), pos);
			#else
			return Mat4x4(m3x4::Rotation(axis, angle), pos);
			#endif
		}

		// Create from an angular displacement vector. length = angle(rad), direction = axis
		static Mat4x4 Transform(v4 const& angular_displacement, v4 const& pos)
		{
			return Mat4x4(m3x4::Rotation(angular_displacement), pos);
		}

		// Create from quaternion
		template <typename = void> static Mat4x4 Transform(Quat const& q, v4 const& pos)
		{
			assert("'q' should be a normalised quaternion" && IsNormal4(q));
			#if PR_MATHS_USE_DIRECTMATH
			return Mat4x4(DirectX::XMMatrixRotationQuaternion(q.vec), pos);
			#else
			return Mat4x4(m3x4(q), pos);
			#endif
		}

		// Create a transform representing the rotation from one vector to another.
		static Mat4x4 Transform(v4 const& from, v4 const& to, v4 const& pos)
		{
			return Mat4x4(m3x4::Rotation(from, to), pos);
		}

		// Create a transform from one basis axis to another
		static Mat4x4 Transform(AxisId from_axis, AxisId to_axis, v4 const& pos)
		{
			return Mat4x4(m3x4::Rotation(from_axis, to_axis), pos);
		}

		// Create a scale matrix
		static Mat4x4 Scale(float scale, v4 const& pos)
		{
			return Mat4x4(m3x4::Scale(scale), pos);
		}
		static Mat4x4 Scale(float sx, float sy, float sz, v4 const& pos)
		{
			return Mat4x4(m3x4::Scale(sx, sy, sz), pos);
		}

		// Create a shear matrix
		static Mat4x4 Shear(float sxy, float sxz, float syx, float syz, float szx, float szy, v4 const& pos)
		{
			return Mat4x4(m3x4::Shear(sxy, sxz, syx, syz, szx, szy), pos);
		}

		// Orientation matrix to "look" at a point
		static Mat4x4 LookAt(v4 const& eye, v4 const& at, v4 const& up)
		{
			assert("Invalid position/direction vectors passed to LookAt" && eye.w == 1.0f && at.w == 1.0f && up.w == 0.0f);
			assert("LookAt point and up axis are aligned" && !pr::Parallel(at - eye, up));
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
			assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
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
			mat.y.y = 1.0f / pr::Tan(fovY/2);
			mat.x.x = mat.y.y / aspect;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}
	};
	using m4x4 = Mat4x4;
	static_assert(maths::is_mat4<m4x4>::value, "");
	static_assert(std::is_pod<m4x4>::value, "Should be a pod type");
	static_assert(std::alignment_of<m4x4>::value == 16, "Should be 16 byte aligned");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	using m4x4_cref = m4x4 const;
	#else
	using m4x4_cref = m4x4 const&;
	#endif

	// Define component accessors for pointer types
	inline v4 const& x_cp(m4x4 const& v) { return v.x; }
	inline v4 const& y_cp(m4x4 const& v) { return v.y; }
	inline v4 const& z_cp(m4x4 const& v) { return v.z; }
	inline v4 const& w_cp(m4x4 const& v) { return v.w; }

	#pragma region Constants
	static m4x4 const m4x4Zero     = {v4Zero, v4Zero, v4Zero, v4Zero};
	static m4x4 const m4x4Identity = {v4XAxis, v4YAxis, v4ZAxis, v4Origin};
	static m4x4 const m4x4One      = {v4One, v4One, v4One, v4One};
	static m4x4 const m4x4Min      = {+v4Min, +v4Min, +v4Min, +v4Min};
	static m4x4 const m4x4Max      = {+v4Max, +v4Max, +v4Max, +v4Max};
	static m4x4 const m4x4Lowest   = {-v4Max, -v4Max, -v4Max, -v4Max};
	static m4x4 const m4x4Epsilon  = {+v4Epsilon, +v4Epsilon, +v4Epsilon, +v4Epsilon};
	#pragma endregion

	#pragma region Operators
	inline m4x4 pr_vectorcall operator + (m4x4_cref mat)
	{
		return mat;
	}
	inline m4x4 pr_vectorcall operator - (m4x4_cref mat)
	{
		return m4x4(-mat.x, -mat.y, -mat.z, -mat.w);
	}
	inline m4x4& pr_vectorcall operator *= (m4x4& lhs, float rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		lhs.z *= rhs;
		lhs.w *= rhs;
		return lhs;
	}
	inline m4x4& pr_vectorcall operator /= (m4x4& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		lhs.z /= rhs;
		lhs.w /= rhs;
		return lhs;
	}
	inline m4x4& pr_vectorcall operator %= (m4x4& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		lhs.z /= rhs;
		lhs.w /= rhs;
		return lhs;
	}
	inline m4x4 pr_vectorcall operator * (m4x4 const& lhs, float rhs)
	{
		auto x = lhs;
		return x *= rhs;
	}
	inline m4x4 pr_vectorcall operator / (m4x4 const& lhs, float rhs)
	{
		auto x = lhs;
		return x /= rhs;
	}
	inline m4x4 pr_vectorcall operator % (m4x4 const& lhs, float rhs)
	{
		auto x = lhs;
		return x %= rhs;
	}
	inline m4x4& pr_vectorcall operator += (m4x4& lhs, m4x4_cref rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		lhs.z += rhs.z;
		lhs.w += rhs.w;
		return lhs;
	}
	inline m4x4& pr_vectorcall operator -= (m4x4& lhs, m4x4_cref rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		lhs.z -= rhs.z;
		lhs.w -= rhs.w;
		return lhs;
	}
	inline m4x4& pr_vectorcall operator += (m4x4& lhs, m3x4_cref rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		lhs.z += rhs.z;
		return lhs;
	}
	inline m4x4& pr_vectorcall operator -= (m4x4& lhs, m3x4_cref rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		lhs.z -= rhs.z;
		return lhs;
	}
	inline m4x4 pr_vectorcall operator + (m4x4 const& lhs, m4x4_cref rhs)
	{
		auto x = lhs;
		return x += rhs;
	}
	inline m4x4 pr_vectorcall operator - (m4x4 const& lhs, m4x4_cref rhs)
	{
		auto x = lhs;
		return x -= rhs;
	}
	inline m4x4 pr_vectorcall operator + (m4x4 const& lhs, m3x4_cref rhs)
	{
		auto x = lhs;
		return x += rhs;
	}
	inline m4x4 pr_vectorcall operator - (m4x4 const& lhs, m3x4_cref rhs)
	{
		auto x = lhs;
		return x -= rhs;
	}
	template <typename = void> inline m4x4 pr_vectorcall operator * (m4x4_cref lhs, m4x4_cref rhs)
	{
		#if PR_MATHS_USE_DIRECTMATH
		return m4x4(DirectX::XMMatrixMultiply(rhs, lhs));
		#else
		auto ans = m4x4{};
		auto lhsT = Transpose4x4(lhs);
		ans.x = v4(Dot4(lhsT.x, rhs.x), Dot4(lhsT.y, rhs.x), Dot4(lhsT.z, rhs.x), Dot4(lhsT.w, rhs.x));
		ans.y = v4(Dot4(lhsT.x, rhs.y), Dot4(lhsT.y, rhs.y), Dot4(lhsT.z, rhs.y), Dot4(lhsT.w, rhs.y));
		ans.z = v4(Dot4(lhsT.x, rhs.z), Dot4(lhsT.y, rhs.z), Dot4(lhsT.z, rhs.z), Dot4(lhsT.w, rhs.z));
		ans.w = v4(Dot4(lhsT.x, rhs.w), Dot4(lhsT.y, rhs.w), Dot4(lhsT.z, rhs.w), Dot4(lhsT.w, rhs.w));
		return ans;
		#endif
	}
	template <typename = void> inline v4 pr_vectorcall operator * (m4x4_cref lhs, v4_cref rhs)
	{
		#if PR_MATHS_USE_DIRECTMATH
		return v4(DirectX::XMVector4Transform(rhs.vec, lhs));
		#else
		auto lhsT = Transpose4x4(lhs);
		return v4(Dot4(lhsT.x, rhs), Dot4(lhsT.y, rhs), Dot4(lhsT.z, rhs), Dot4(lhsT.w, rhs));
		#endif
	}
	#pragma endregion

	#pragma region Functions

	// Return true if 'mat' is an affine transform
	inline bool IsAffine(m4x4 const& mat)
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
	inline float DeterminantFast4(m4x4 const& mat)
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
	inline float Determinant4(pr::m4x4 const& mat)
	{
		#if PR_MATHS_USE_DIRECTMATH
		return v4(DirectX::XMMatrixDeterminant(mat)).x;
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
		return v4(mat.y.y*mat.z.z - mat.y.z*mat.z.y, -mat.y.x*mat.z.z + mat.y.z*mat.z.x, mat.y.x*mat.z.y - mat.y.y*mat.z.x, 0.0f);
	}

	// Return the cross product matrix for 'vec'.
	inline m4x4 CPM(v4 const& vec, v4 const& pos)
	{
		// This matrix can be used to take the cross product with
		// another vector: e.g. Cross4(v1, v2) == Cross4(v1) * v2
		return m4x4(CPM(vec), pos);
	}

	// Return the 4x4 transpose of 'mat'
	inline m4x4 Transpose4x4(m4x4 const& mat)
	{
		#if PR_MATHS_USE_DIRECTMATH && 0
		return m4x4(DirectX::XMMatrixTranspose(mat.vec));
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
	inline m4x4 Transpose3x3(m4x4 const& mat)
	{
		auto m = mat;
		std::swap(m.x.y, m.y.x);
		std::swap(m.x.z, m.z.x);
		std::swap(m.y.z, m.z.y);
		return m;
	}

	// Return true if this matrix is orthonormal
	inline bool IsOrthonormal(m4x4 const& mat)
	{
		return
			FEql(Length3Sq(mat.x), 1.0f) &&
			FEql(Length3Sq(mat.y), 1.0f) &&
			FEql(Length3Sq(mat.z), 1.0f) &&
			FEql(Abs(Determinant3(mat)), 1.0f);
	}

	// True if 'mat' has an inverse
	inline bool IsInvertable(m4x4 const& mat)
	{
		return !FEql(Determinant4(mat), 0.0f);
	}

	// Return the inverse of 'mat' (assuming an orthonormal matrix)
	inline m4x4 InvertFast(m4x4 const& mat)
	{
		assert("Matrix is not orthonormal" && IsOrthonormal(mat));
		auto m = Transpose3x3(mat);
		m.pos.x = -Dot3(mat.x, mat.pos);
		m.pos.y = -Dot3(mat.y, mat.pos);
		m.pos.z = -Dot3(mat.z, mat.pos);
		return m;
	}

	// Return the inverse of 'mat'
	inline m4x4 Invert(m4x4 const& mat)
	{
		#if PR_MATHS_USE_DIRECTMATH
		v4 det;
		m4x4 m(DirectX::XMMatrixInverse(&det.vec, mat));
		assert("Matrix has no inverse" && det.x != 0);
		return m;
		#else

		m4x4 A = Transpose4x4(mat); // Take the transpose so that row operations are faster
		m4x4 B = m4x4Identity;

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

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	inline m4x4 Sqrt(m4x4 const& mat)
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
	inline m4x4 Orthonorm(m4x4 const& mat)
	{
		auto m = mat;
		m.x = Normalise3(m.x);
		m.y = Normalise3(Cross3(m.z, m.x));
		m.z = Cross3(m.x, m.y);
		assert(IsOrthonormal(m));
		return m;
	}

	// Return the axis and angle of a rotation matrix
	inline void GetAxisAngle(m4x4 const& mat, v4& axis, float& angle)
	{
		GetAxisAngle(mat.rot, axis, angle);
	}

	// Make an object to world transform from a direction vector and position
	// 'dir' is the direction to align the 'axis'th axis to
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be chosen.
	inline m4x4 OriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& pos)
	{
		return m4x4(OriFromDir(dir, axis, up), pos);
	}

	// Make a scaled object to world transform from a direction vector and position
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	inline m4x4 ScaledOriFromDir(v4 const& dir, int axis, v4 const& up, v4 const& pos)
	{
		return m4x4(ScaledOriFromDir(dir, axis, up), pos);
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	inline v4 RotationVectorApprox(m4x4 const& from, m4x4 const& to)
	{
		assert(IsOrthonormal(from) && IsOrthonormal(to) && "This only works for orthonormal matrices");

		m4x4 cpm_x_i2wR = to - from;
		m4x4 w2iR = Transpose3x3(from).w0();
		m4x4 cpm = cpm_x_i2wR * w2iR;
		return v4(cpm.y.z, cpm.z.x, cpm.x.y, 0.0f);
	}

	// Spherically interpolate between two affine transforms
	template <typename = void> inline m4x4 pr_vectorcall Slerp(m4x4_cref lhs, m4x4_cref rhs, float frac)
	{
		assert(IsAffine(lhs));
		assert(IsAffine(rhs));

		auto q = Slerp(quat(lhs.rot), quat(rhs.rot), frac);
		auto p = Lerp(lhs.pos, rhs.pos, frac);
		return m4x4(q, p);
	}

	#pragma endregion
}

namespace std
{
	#pragma region Numeric limits
	template <> class std::numeric_limits<pr::m4x4>
	{
	public:
		static pr::m4x4 min() throw()     { return pr::m4x4Min; }
		static pr::m4x4 max() throw()     { return pr::m4x4Max; }
		static pr::m4x4 lowest() throw()  { return pr::m4x4Lowest; }
		static pr::m4x4 epsilon() throw() { return pr::m4x4Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_matrix4x4)
		{
			using namespace pr;
			std::default_random_engine rng;
			{
				m4x4 m1 = m4x4Identity;
				m4x4 m2 = m4x4Identity;
				m4x4 m3 = m1 * m2;
				PR_CHECK(FEql(m3, m4x4Identity), true);
			}
			{//m4x4Translation
				m4x4 m2;
				m4x4 m1 = m4x4(v4XAxis, v4YAxis, v4ZAxis, v4(1.0f, 2.0f, 3.0f, 1.0f));
				m2 = m4x4::Translation(v4(1.0f, 2.0f, 3.0f, 1.0f));
				PR_CHECK(FEql(m1, m2), true);
			}
			{//m4x4CreateFrom
				v4 V1 = Random3(rng, 0.0f, 10.0f, 1.0f);
				m4x4 a2b = m4x4::Transform(v4::Normal3(+3,-2,-1,0), +1.23f, v4(+4.4f, -3.3f, +2.2f, 1.0f));
				m4x4 b2c = m4x4::Transform(v4::Normal3(-1,+2,-3,0), -3.21f, v4(-1.1f, +2.2f, -3.3f, 1.0f));
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
}
#endif