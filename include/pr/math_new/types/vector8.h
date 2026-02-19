//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/functions.h"
#include "pr/math_new/types/vector3.h"
#include "pr/math_new/types/vector4.h"

namespace pr::math
{
	template <ScalarType S>
	struct Vec8
	{
		// Notes:
		//  - Spatial vectors describe a vector at a point plus the field of vectors around that point. 
		using Vec3 = Vec3<S>;
		using Vec4 = Vec4<S>;

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4 ang, lin; };
			struct { S rx, ry, rz, rw, tx, ty, tz, tw; };
			struct { S arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
		constexpr explicit Vec8(S x)
			:ang(x)
			, lin(x)
		{
		}
		constexpr Vec8(Vec3 ang_, Vec3 lin_)
			: ang(ang_, S(0))
			, lin(lin_, S(0))
		{
		}
		constexpr Vec8(Vec4 ang_, Vec4 lin_)
			: ang(ang_)
			, lin(lin_)
		{
		}
		constexpr Vec8(S wx, S wy, S wz, S vx, S vy, S vz)
			: ang(wx, wy, wz, S(0))
			, lin(vx, vy, vz, S(0))
		{
		}
		constexpr Vec8(S wx, S wy, S wz, S ww, S vx, S vy, S vz, S vw)
			: ang(wx, wy, wz, ww)
			, lin(vx, vy, vz, vw)
		{
		}

		// Array access
		constexpr S const& operator [] (int i) const
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}
		constexpr S& operator [] (int i)
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}

		// Constants
		static constexpr Vec8 Zero()
		{
			return Vec8{ Vec4::Zero(), Vec4::Zero() };
		}

		// Sample the vector field at 'ofs'.
		// Returns the direction and magnitude of the vector field at 'ofs'
		Vec4 LinAt(Vec4 ofs) const
		{
			return Vec4{ lin + Cross3(ang, ofs) };
		}

		// Sample the vector field at 'ofs'
		// Not really sure what the physical interpretation of this is.
		// Returns 'ang - ofs x lin', which is the angular required at 'ofs' to ensure that the angular
		// is constant over the whole vector field, given that 'ofs x lin' contributes to the angular.
		Vec4 AngAt(Vec4 ofs) const
		{
			return Vec4{ ang - Cross3(ofs, lin) };
		}

		// Operators
		#pragma region Operators
		friend constexpr Vec8 pr_vectorcall operator + (Vec8 lhs)
		{
			return lhs;
		}
		friend constexpr Vec8 pr_vectorcall operator - (Vec8 lhs)
		{
			return Vec8{ -lhs.ang, -lhs.lin };
		}
		friend Vec8 pr_vectorcall operator * (S lhs, Vec8 rhs)
		{
			return rhs * lhs;
		}
		friend Vec8 pr_vectorcall operator * (Vec8 lhs, S rhs)
		{
			return Vec8{ lhs.ang * rhs, lhs.lin * rhs };
		}
		friend Vec8 pr_vectorcall operator / (Vec8 lhs, S rhs)
		{
			return Vec8{ lhs.ang / rhs, lhs.lin / rhs };
		}
		friend Vec8 pr_vectorcall operator % (Vec8 lhs, S rhs)
		{
			return Vec8{ lhs.ang % rhs, lhs.lin % rhs };
		}
		friend Vec8 pr_vectorcall operator + (Vec8 lhs, Vec8 rhs)
		{
			return Vec8{ lhs.ang + rhs.ang, lhs.lin + rhs.lin };
		}
		friend Vec8 pr_vectorcall operator - (Vec8 lhs, Vec8 rhs)
		{
			return Vec8{ lhs.ang - rhs.ang, lhs.lin - rhs.lin };
		}
		friend Vec8 pr_vectorcall operator * (Vec8 lhs, Vec8 rhs)
		{
			return Vec8{ lhs.ang * rhs.ang, lhs.lin * rhs.lin };
		}
		friend Vec8 pr_vectorcall operator / (Vec8 lhs, Vec8 rhs)
		{
			return Vec8{ lhs.ang / rhs.ang, lhs.lin / rhs.lin };
		}
		friend Vec8 pr_vectorcall operator % (Vec8 lhs, Vec8 rhs)
		{
			return Vec8{ lhs.ang % rhs.ang, lhs.lin % rhs.lin };
		}
		#pragma endregion
	};

	// Note: Vec8 isn't really a vector type, but it has the same memory layout as 8 scalars, so we can treat it as a vector for the purposes of generic algorithms.

	// Compare for floating point equality
	template <ScalarType S>
	constexpr bool FEql(Vec8<S> lhs, Vec8<S> rhs)
	{
		return
			FEql(lhs.ang, rhs.ang) &&
			FEql(lhs.lin, rhs.lin);
	}

	// Project a vector onto an axis. Loosely "dot(vec,axis)*axis"
	template <ScalarType S>
	constexpr inline Vec8<S> Proj(Vec8<S> vec, Vec4<S> axis)
	{
		return Vec8<S>{
			Dot(vec.ang, axis) * axis,
			Dot(vec.lin, axis) * axis};
	}

	// Reflect a vector. Reverses the components of 'vec' in the direction of 'normal'
	template <ScalarType S>
	constexpr inline Vec8<S> Reflect(Vec8<S> vec, Vec4<S> normal)
	{
		return vec - S(2) * Proj(vec, normal);
	}
}
