//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new//math.h"

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::math
{
	PRUnitTestClass(Quaternion)
	{
		PRUnitTestMethod(Operators, Quat<float>, Quat<double>)
		{
			using quat_t = T;

			#if 0
			//auto Q0 = quat_t(T(2));
			//auto Q1 = quat_t(T(3));

			static_assert(quat_t{+1, +2, +3, +1} == +quat_t{+1, +2, +3, +1});
			static_assert(quat_t{+1, +2, +3, +1} == -quat_t{-1, -2, -3, -1});
			static_assert(quat_t{+1, +2, +3, +1} == ~quat_t{-1, -2, -3, +1});

			{
				template <QuaternionType Quat> constexpr Quat operator + (Quat const& lhs)
				{
					return lhs;
				}
				template <QuaternionType Quat> constexpr Quat operator - (Quat const& lhs) // Note: Not conjugate
				{
					return {
						-vec(lhs).x,
						-vec(lhs).y,
						-vec(lhs).z,
						-vec(lhs).w,
					};
				}
				template <QuaternionType Quat> constexpr Quat operator ~ (Quat const& lhs) // This is conjugate
				{
					return {
						-vec(lhs).x,
						-vec(lhs).y,
						-vec(lhs).z,
						 vec(lhs).w,
					};
				}
				template <QuaternionType Quat> constexpr Quat& operator *= (Quat & lhs, typename vector_traits<Quat>::element_t rhs)
				{
					vec(lhs).x *= rhs;
					vec(lhs).y *= rhs;
					vec(lhs).z *= rhs;
					vec(lhs).w *= rhs;
					return lhs;
				}
				template <QuaternionType Quat> constexpr Quat operator * (Quat const& lhs, typename vector_traits<Quat>::element_t rhs)
				{
					Quat res = lhs;
					return res *= rhs;
				}
				template <QuaternionType Quat> constexpr Quat operator * (typename vector_traits<Quat>::element_t lhs, Quat const& rhs)
				{
					Quat res = rhs;
					return res *= lhs;
				}
				template <QuaternionType Quat> constexpr Quat operator * (Quat const& lhs, Quat const& rhs)
				{
					// Quaternion multiply
					// Note about 'quat multiply' vs. 'r = q*v*conj(q)':
					// To rotate a vector or another quaternion, use the "sandwich product"
					// However, combining rotations is done using q1 * q2.
					// This is because:
					//'  r1 = a * v * conj(a)  - first rotation 
					//'  r2 = b * r1 * conj(b) - second rotation
					//'  r2 = b * a * v * conj(a) * conj(b)     
					//'  r2 = (b*a) * v * conj(b*a)             
					Quat res = {};
					vec(res).x = vec(lhs).w * vec(rhs).x + vec(lhs).x * vec(rhs).w + vec(lhs).y * vec(rhs).z - vec(lhs).z * vec(rhs).y;
					vec(res).y = vec(lhs).w * vec(rhs).y - vec(lhs).x * vec(rhs).z + vec(lhs).y * vec(rhs).w + vec(lhs).z * vec(rhs).x;
					vec(res).z = vec(lhs).w * vec(rhs).z + vec(lhs).x * vec(rhs).y - vec(lhs).y * vec(rhs).x + vec(lhs).z * vec(rhs).w;
					vec(res).w = vec(lhs).w * vec(rhs).w - vec(lhs).x * vec(rhs).x - vec(lhs).y * vec(rhs).y - vec(lhs).z * vec(rhs).z;
					return res;
				}
				template <QuaternionType Quat> constexpr Quat operator /= (Quat & lhs, typename vector_traits<Quat>::element_t rhs)
				{
					vec(lhs).x /= rhs;
					vec(lhs).y /= rhs;
					vec(lhs).z /= rhs;
					vec(lhs).w /= rhs;
					return lhs;
				}
				template <QuaternionType Quat> constexpr Quat operator / (Quat const& lhs, typename vector_traits<Quat>::element_t rhs)
				{
					Quat res = lhs;
					return res /= rhs;
				}
			}
			
			template <QuaternionType Quat>
			constexpr Quat Identity()
			{
				using S = typename vector_traits<Quat>::element_t;
				return { S(0), S(0), S(0), S(1) };
			}
			#endif
		}
	};
}
#endif
