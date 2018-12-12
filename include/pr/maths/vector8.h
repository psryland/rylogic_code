//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	template <typename T>
	struct alignas(16) Vec8
	{
		// Notes:
		//  - Spatial vectors describe a vector at a point plus the field of vectors around that point. 
		//   

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4<void> ang, lin; };
			struct { float arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
		Vec8(v3_cref<> ang_, v3_cref<> lin_)
			:ang(ang_, 0)
			,lin(lin_, 0)
		{}
		Vec8(v4_cref<> ang_, v4_cref<> lin_)
			:ang(ang_)
			,lin(lin_)
		{}
		Vec8(float wx, float wy, float wz, float vx, float vy, float vz)
			:ang(wx, wy, wz, 0)
			,lin(vx, vy, vz, 0)
		{}
		Vec8(float wx, float wy, float wz, float ww, float vx, float vy, float vz, float vw)
			:ang(wx, wy, wz, ww)
			,lin(vx, vy, vz, vw)
		{}

		// Array access
		float const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		float& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Reinterpret as a vector of a different vector space
		template <typename U> explicit operator Vec8<U>() const
		{
			return reinterpret_cast<Vec8<U> const&>(*this);
		}

		// Sample the vector field at 'ofs'
		// Returns the direction and magnitude of the vector field at 'ofs'
		v4 LinAt(v4_cref<> ofs) const
		{
			return v4{lin + Cross(ang, ofs)};
		}

		// Sample the vector field at 'ofs'
		// Not really sure what the physical interpretation of this is.
		// Returns 'ang - ofs x lin', which is the angular required at 'ofs' to ensure that the angular
		// is constant over the whole vector field, given that 'ofs x lin' contributes to the angular.
		v4 AngAt(v4_cref<> ofs) const
		{
			return v4{ang - Cross(ofs, lin)};
		}
	};
	static_assert(maths::is_vec<Vec8<void>>::value, "");
	static_assert(std::is_pod<Vec8<void>>::value, "v8 must be a pod type");
	static_assert(std::alignment_of<Vec8<void>>::value == 16, "v8 should have 16 byte alignment");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	template <typename T = void> using v8_cref = Vec8<T> const;
	#else
	template <typename T = void> using v8_cref = Vec8<T> const&;
	#endif

	//// Define component accessors
	//template <typename T> inline float x_cp(v8_cref<T> v) { return v.lin.x; }
	//template <typename T> inline float y_cp(v8_cref<T> v) { return v.lin.y; }
	//template <typename T> inline float z_cp(v8_cref<T> v) { return v.lin.z; }
	//template <typename T> inline float w_cp(v8_cref<T> v) { return v.lin.w; }

	#pragma region Operators
	template <typename T> inline Vec8<T> pr_vectorcall operator + (v8_cref<T> lhs)
	{
		return lhs;
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator - (v8_cref<T> lhs)
	{
		return Vec8<T>{-lhs.ang, -lhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator * (float lhs, v8_cref<T> rhs)
	{
		return rhs * lhs;
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator * (v8_cref<T> lhs, float rhs)
	{
		return Vec8<T>{lhs.ang * rhs, lhs.lin * rhs};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator / (v8_cref<T> lhs, float rhs)
	{
		return Vec8<T>{lhs.ang / rhs, lhs.lin / rhs};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator % (v8_cref<T> lhs, float rhs)
	{
		return Vec8<T>{lhs.ang % rhs, lhs.lin % rhs};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator + (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang + rhs.ang, lhs.lin + rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator - (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang - rhs.ang, lhs.lin - rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator * (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang * rhs.ang, lhs.lin * rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator / (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang / rhs.ang, lhs.lin / rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator % (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang % rhs.ang, lhs.lin % rhs.lin};
	}
	#pragma endregion

	#pragma region Functions

	// Compare for floating point equality
	//template <typename T> inline bool FEql(v8_cref<T> lhs, v8_cref<T> rhs)
	template <typename T> inline bool FEql(Vec8<T> lhs, Vec8<T> rhs)
	{
		return
			FEql(lhs.ang, rhs.ang) &&
			FEql(lhs.lin, rhs.lin);
	}

	// Project a vector onto an axis. Loosely "dot(vec,axis)*axis"
	template <typename T> inline Vec8<T> Proj(Vec8<T> const& vec, v4_cref<> axis)
	{
		return Vec8<T>{
			Dot(vec.ang, axis) * axis,
			Dot(vec.lin, axis) * axis};
	}

	// Reflect a vector. Reverses the components of 'vec' in the direction of 'normal'
	template <typename T> inline Vec8<T> Reflect(Vec8<T> const& vec, v4_cref<> normal)
	{
		return vec - 2.0f * Proj(vec, normal);
	}

	#pragma endregion

	// Proxy object for Vec8
	#if 0
	struct Vec8ProxyC
	{
		Vec4<void> const& ang;
		Vec4<void> const& lin;

		Vec8ProxyC(Vec4<void> const& ang, Vec4<void> const& lin)
			:ang(ang)
			,lin(lin)
		{}
		Vec8ProxyC(Vec8<void> const& vec)
			:ang(vec.ang)
			,lin(vec.lin)
		{}
		operator Vec8<void>() const
		{
			return Vec8<void>{ang, lin};
		}
		float operator [](int i) const
		{
			return i < 4
				? ang[i  ]
				: lin[i-4];
		}
		friend bool FEql(Vec8ProxyC const& lhs, Vec8ProxyC const& rhs)
		{
			return FEql((Vec8<void>)lhs, (Vec8<void>)rhs);
		}
	};
	struct Vec8Proxy
	{
		Vec4<void>& ang;
		Vec4<void>& lin;
	
		Vec8Proxy(Vec4<void>& ang, Vec4<void>& lin)
			:ang(ang)
			,lin(lin)
		{}
		Vec8Proxy(Vec8<void>& vec)
			:ang(vec.ang)
			,lin(vec.lin)
		{}
		operator Vec8ProxyC() const
		{
			return Vec8ProxyC{ang, lin};
		}
		operator Vec8<void>() const
		{
			return Vec8<void>{ang, lin};
		}
		Vec8Proxy& operator = (Vec8ProxyC rhs)
		{
			ang = rhs.ang;
			lin = rhs.lin;
			return *this;
		}
		Vec8Proxy& operator = (Vec8Proxy rhs)
		{
			ang = rhs.ang;
			lin = rhs.lin;
			return *this;
		}
		Vec8Proxy& operator = (v8_cref<> rhs)
		{
			ang = rhs.ang;
			lin = rhs.lin;
			return *this;
		}
		float& operator [](int i)
		{
			return i < 4
				? ang[i  ]
				: lin[i-4];
		}
	};
	#endif
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr::maths
{
	PRUnitTest(Vector8Tests)
	{
		std::default_random_engine rng;
		{// LinAt, AngAt
			auto v = v8{Random3(rng, v4{}, 10.0f, 0.0f), Random3(rng, v4{}, 10.0f, 0.0f)};
			auto lin = v.LinAt(v4Origin);
			auto ang = v.AngAt(v4Origin);
			auto V = v8{ang, lin};
			PR_CHECK(FEql(v, V), true);
		}
		{// LinAt, AngAt
			auto v = v8{0, 0, 1, 0, 1, 0};

			auto lin0 = v.LinAt(v4{-1,0,0,0});
			auto ang0 = v.AngAt(v4{-1,0,0,0});
			PR_CHECK(FEql(lin0, v4{0,0,0,0}), true);
			PR_CHECK(FEql(ang0, v4{0,0,2,0}), true);

			auto lin1 = v.LinAt(v4{0,0,0,0});
			auto ang1 = v.AngAt(v4{0,0,0,0});
			PR_CHECK(FEql(lin1, v4{0,1,0,0}), true);
			PR_CHECK(FEql(ang1, v4{0,0,1,0}), true);

			auto lin2 = v.LinAt(v4{+1,0,0,0});
			auto ang2 = v.AngAt(v4{+1,0,0,0});
			PR_CHECK(FEql(lin2, v4{0,2,0,0}), true);
			PR_CHECK(FEql(ang2, v4{0,0,0,0}), true);

			auto lin3 = v.LinAt(v4{+2,0,0,0});
			auto ang3 = v.AngAt(v4{+2,0,0,0});
			PR_CHECK(FEql(lin3, v4{0,3,0,0}), true);
			PR_CHECK(FEql(ang3, v4{0,0,-1,0}), true);

			auto lin4 = v.LinAt(v4{+3,0,0,0});
			auto ang4 = v.AngAt(v4{+3,0,0,0});
			PR_CHECK(FEql(lin4, v4{0,4,0,0}), true);
			PR_CHECK(FEql(ang4, v4{0,0,-2,0}), true);
		}
		{// Projection
			auto v = v8{1,-2,3,-3,2,-1};
			auto vn = Proj(v, v4ZAxis);
			auto vt = v - vn;
			auto r = vn + vt;
			PR_CHECK(FEql(vn, v8{0,0,3,0,0,-1}), true);
			PR_CHECK(FEql(vt, v8{1,-2,0,-3,2,0}), true);
			PR_CHECK(FEql(r, v), true);
		}
		{// Projection/Reflect
			auto v = v8{0, 0, 1, 0, 1, 0};
			auto n = v4::Normal3(-1,-1,-1,0);
			auto r = v8{-0.6666666f, -0.6666666f, 0.3333333f, -0.6666666f, 0.3333333f, -0.6666666f};
			auto R = Reflect(v, n);
			PR_CHECK(FEql(r, R), true);
		}
	}
}
#endif