//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/core/axis_id.h"

namespace pr::math
{
	// Notes:
	//  - This file contains operators for generic vector types.
	//  - Overloads for specific types can be created (e.g. Vec4)

	// Operators
	template <VectorType Vec> constexpr Vec pr_vectorcall operator + (Vec lhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = +vec(lhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = +vec(lhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = +vec(lhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = +vec(lhs).w;
		return res;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator - (Vec lhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = -vec(lhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = -vec(lhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = -vec(lhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = -vec(lhs).w;
		return res;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator += (Vec& lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x += vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y += vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z += vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w += vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator -= (Vec& lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x -= vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y -= vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z -= vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w -= vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator *= (Vec& lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x *= vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y *= vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z *= vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w *= vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator *= (Vec& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x *= rhs;
		if constexpr (vt::dimension > 1) vec(lhs).y *= rhs;
		if constexpr (vt::dimension > 2) vec(lhs).z *= rhs;
		if constexpr (vt::dimension > 3) vec(lhs).w *= rhs;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator /= (Vec& lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x /= vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y /= vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z /= vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w /= vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator /= (Vec& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x /= rhs;
		if constexpr (vt::dimension > 1) vec(lhs).y /= rhs;
		if constexpr (vt::dimension > 2) vec(lhs).z /= rhs;
		if constexpr (vt::dimension > 3) vec(lhs).w /= rhs;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator %= (Vec& lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (std::is_integral_v<typename vt::element_t>)
		{
			if constexpr (vt::dimension > 0) vec(lhs).x %= vec(rhs).x;
			if constexpr (vt::dimension > 1) vec(lhs).y %= vec(rhs).y;
			if constexpr (vt::dimension > 2) vec(lhs).z %= vec(rhs).z;
			if constexpr (vt::dimension > 3) vec(lhs).w %= vec(rhs).w;
		}
		if constexpr (std::is_floating_point_v<typename vt::element_t>)
		{
			if constexpr (vt::dimension > 0) vec(lhs).x = std::fmod(vec(lhs).x, vec(rhs).x);
			if constexpr (vt::dimension > 1) vec(lhs).y = std::fmod(vec(lhs).y, vec(rhs).y);
			if constexpr (vt::dimension > 2) vec(lhs).z = std::fmod(vec(lhs).z, vec(rhs).z);
			if constexpr (vt::dimension > 3) vec(lhs).w = std::fmod(vec(lhs).w, vec(rhs).w);
		}
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& pr_vectorcall operator %= (Vec& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (std::is_integral_v<typename vt::element_t>)
		{
			if constexpr (vt::dimension > 0) vec(lhs).x %= rhs;
			if constexpr (vt::dimension > 1) vec(lhs).y %= rhs;
			if constexpr (vt::dimension > 2) vec(lhs).z %= rhs;
			if constexpr (vt::dimension > 3) vec(lhs).w %= rhs;
		}
		if constexpr (std::is_floating_point_v<typename vt::element_t>)
		{
			if constexpr (vt::dimension > 0) vec(lhs).x = std::fmod(vec(lhs).x, rhs);
			if constexpr (vt::dimension > 1) vec(lhs).y = std::fmod(vec(lhs).y, rhs);
			if constexpr (vt::dimension > 2) vec(lhs).z = std::fmod(vec(lhs).z, rhs);
			if constexpr (vt::dimension > 3) vec(lhs).w = std::fmod(vec(lhs).w, rhs);
		}
		return lhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator + (Vec lhs, Vec rhs)
	{
		Vec res = lhs;
		return res += rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator - (Vec lhs, Vec rhs)
	{
		Vec res = lhs;
		return res -= rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator * (Vec lhs, Vec rhs)
	{
		Vec res = lhs;
		return res *= rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator * (Vec lhs, typename vector_traits<Vec>::element_t rhs)
	{
		Vec res = lhs;
		return res *= rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator * (typename vector_traits<Vec>::element_t lhs, Vec rhs)
	{
		Vec res = rhs;
		return res *= lhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator / (Vec lhs, Vec rhs)
	{
		Vec res = lhs;
		return res /= rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator / (Vec lhs, typename vector_traits<Vec>::element_t rhs)
	{
		Vec res = lhs;
		return res /= rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator % (Vec lhs, Vec rhs)
	{
		Vec res = lhs;
		return res %= rhs;
	}
	template <VectorType Vec> constexpr Vec pr_vectorcall operator % (Vec lhs, typename vector_traits<Vec>::element_t rhs)
	{
		Vec res = lhs;
		return res %= rhs;
	}
	template <TensorType Vec> constexpr auto pr_vectorcall operator <=> (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) if (auto cmp = std::partial_order(vec(lhs).x, vec(rhs).x); cmp != 0) return cmp;
		if constexpr (vt::dimension > 1) if (auto cmp = std::partial_order(vec(lhs).y, vec(rhs).y); cmp != 0) return cmp;
		if constexpr (vt::dimension > 2) if (auto cmp = std::partial_order(vec(lhs).z, vec(rhs).z); cmp != 0) return cmp;
		if constexpr (vt::dimension > 3) if (auto cmp = std::partial_order(vec(lhs).w, vec(rhs).w); cmp != 0) return cmp;
		return std::partial_ordering::equivalent;
	}
	template <TensorType Vec> constexpr bool pr_vectorcall operator == (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) if (!(vec(lhs).x == vec(rhs).x)) return false;
		if constexpr (vt::dimension > 1) if (!(vec(lhs).y == vec(rhs).y)) return false;
		if constexpr (vt::dimension > 2) if (!(vec(lhs).z == vec(rhs).z)) return false;
		if constexpr (vt::dimension > 3) if (!(vec(lhs).w == vec(rhs).w)) return false;
		return true;
	}
	template <TensorType Vec> constexpr bool pr_vectorcall operator != (Vec lhs, Vec rhs)
	{
		return !(lhs == rhs);
	}
	template <TensorType Vec> constexpr std::ostream& operator << (std::ostream& out, Vec v)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) out << vec(v).x << (vt::dimension > 1 ? ", " : "");
		if constexpr (vt::dimension > 1) out << vec(v).y << (vt::dimension > 2 ? ", " : "");
		if constexpr (vt::dimension > 2) out << vec(v).z << (vt::dimension > 3 ? ", " : "");
		if constexpr (vt::dimension > 3) out << vec(v).w << (vt::dimension > 4 ? ", " : "");
		return out;
	}

	// Bitwise operators (only for integral vectors)
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator ~ (Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = ~vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = ~vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = ~vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = ~vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator ! (Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = !vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = !vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = !vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = !vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator | (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x | vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y | vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z | vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w | vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator & (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x & vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y & vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z & vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w & vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator ^ (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x ^ vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y ^ vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z ^ vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w ^ vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator << (Vec lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x << rhs;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y << rhs;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z << rhs;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w << rhs;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator << (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x << vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y << vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z << vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w << vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator >> (Vec lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x >> rhs;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y >> rhs;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z >> rhs;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w >> rhs;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator >> (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x >> vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y >> vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z >> vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w >> vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator || (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x || vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y || vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z || vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w || vec(rhs).w;
		return res;
	}
	template <VectorType Vec> requires (std::integral<typename vector_traits<Vec>::element_t>) constexpr Vec pr_vectorcall operator && (Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(lhs).x && vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(lhs).y && vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(lhs).z && vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(lhs).w && vec(rhs).w;
		return res;
	}

	// Quaternion Operators
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator + (Quat const& lhs)
	{
		return lhs;
	}
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator - (Quat const& lhs) // Note: Not conjugate
	{
		return {
			-vec(lhs).x,
			-vec(lhs).y,
			-vec(lhs).z,
			-vec(lhs).w,
		};
	}
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator ~ (Quat const& lhs) // This is conjugate
	{
		return {
			-vec(lhs).x,
			-vec(lhs).y,
			-vec(lhs).z,
			 vec(lhs).w,
		};
	}
	template <QuaternionType Quat> constexpr Quat& pr_vectorcall operator *= (Quat& lhs, typename vector_traits<Quat>::element_t rhs)
	{
		vec(lhs).x *= rhs;
		vec(lhs).y *= rhs;
		vec(lhs).z *= rhs;
		vec(lhs).w *= rhs;
		return lhs;
	}
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator * (Quat const& lhs, typename vector_traits<Quat>::element_t rhs)
	{
		Quat res = lhs;
		return res *= rhs;
	}
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator * (typename vector_traits<Quat>::element_t lhs, Quat const& rhs)
	{
		Quat res = rhs;
		return res *= lhs;
	}
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator * (Quat const& lhs, Quat const& rhs)
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
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator /= (Quat& lhs, typename vector_traits<Quat>::element_t rhs)
	{
		vec(lhs).x /= rhs;
		vec(lhs).y /= rhs;
		vec(lhs).z /= rhs;
		vec(lhs).w /= rhs;
		return lhs;
	}
	template <QuaternionType Quat> constexpr Quat pr_vectorcall operator / (Quat const& lhs, typename vector_traits<Quat>::element_t rhs)
	{
		Quat res = lhs;
		return res /= rhs;
	}

	// Matrix Multiply forward
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr typename vector_traits<Mat>::component_t pr_vectorcall operator * (Mat const& a2b, typename vector_traits<Mat>::component_t v) noexcept;
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall operator * (Mat const& b2c, Mat const& a2b) noexcept;

	// Constants
	template <typename S> constexpr S Zero()
	{
		return S(0);
	}
	template <typename S> constexpr S One()
	{
		return S(1);
	}
	template <typename S> constexpr S Min()
	{
		return std::numeric_limits<S>::lowest();
	}
	template <typename S> constexpr S Max()
	{
		return std::numeric_limits<S>::max();
	}
	template <typename S> constexpr S Infinity()
	{
		return std::numeric_limits<S>::infinity();
	}
	template <typename S> constexpr S Epsilon()
	{
		return std::numeric_limits<S>::epsilon();
	}
	template <VectorType Vec> constexpr Vec Zero()
	{
		using vt = vector_traits<Vec>;
		using C = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Zero<C>();
		if constexpr (vt::dimension > 1) vec(res).y = Zero<C>();
		if constexpr (vt::dimension > 2) vec(res).z = Zero<C>();
		if constexpr (vt::dimension > 3) vec(res).w = Zero<C>();
		return res;
	}
	template <VectorType Vec> constexpr Vec One()
	{
		using vt = vector_traits<Vec>;
		using C = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = One<C>();
		if constexpr (vt::dimension > 1) vec(res).y = One<C>();
		if constexpr (vt::dimension > 2) vec(res).z = One<C>();
		if constexpr (vt::dimension > 3) vec(res).w = One<C>();
		return res;
	}
	template <VectorType Vec> constexpr Vec Min()
	{
		using vt = vector_traits<Vec>;
		using C = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Min<C>();
		if constexpr (vt::dimension > 1) vec(res).y = Min<C>();
		if constexpr (vt::dimension > 2) vec(res).z = Min<C>();
		if constexpr (vt::dimension > 3) vec(res).w = Min<C>();
		return res;
	}
	template <VectorType Vec> constexpr Vec Max()
	{
		using vt = vector_traits<Vec>;
		using C = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Max<C>();
		if constexpr (vt::dimension > 1) vec(res).y = Max<C>();
		if constexpr (vt::dimension > 2) vec(res).z = Max<C>();
		if constexpr (vt::dimension > 3) vec(res).w = Max<C>();
		return res;
	}
	template <VectorType Vec> constexpr Vec Infinity()
	{
		using vt = vector_traits<Vec>;
		using C = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Infinity<C>();
		if constexpr (vt::dimension > 1) vec(res).y = Infinity<C>();
		if constexpr (vt::dimension > 2) vec(res).z = Infinity<C>();
		if constexpr (vt::dimension > 3) vec(res).w = Infinity<C>();
		return res;
	}
	template <VectorType Vec> constexpr Vec Epsilon()
	{
		using vt = vector_traits<Vec>;
		using C = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Epsilon<C>();
		if constexpr (vt::dimension > 1) vec(res).y = Epsilon<C>();
		if constexpr (vt::dimension > 2) vec(res).z = Epsilon<C>();
		if constexpr (vt::dimension > 3) vec(res).w = Epsilon<C>();
		return res;
	}
	template <VectorType Vec> requires (IsRank1<Vec>&& vector_traits<Vec>::dimension >= 1) constexpr Vec XAxis()
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = S(1);
		if constexpr (vt::dimension > 1) vec(res).y = S(0);
		if constexpr (vt::dimension > 2) vec(res).z = S(0);
		if constexpr (vt::dimension > 3) vec(res).w = S(0);
		return res;
	}
	template <VectorType Vec> requires (IsRank1<Vec>&& vector_traits<Vec>::dimension >= 2) constexpr Vec YAxis()
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = S(0);
		if constexpr (vt::dimension > 1) vec(res).y = S(1);
		if constexpr (vt::dimension > 2) vec(res).z = S(0);
		if constexpr (vt::dimension > 3) vec(res).w = S(0);
		return res;
	}
	template <VectorType Vec> requires (IsRank1<Vec>&& vector_traits<Vec>::dimension >= 3) constexpr Vec ZAxis()
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = S(0);
		if constexpr (vt::dimension > 1) vec(res).y = S(0);
		if constexpr (vt::dimension > 2) vec(res).z = S(1);
		if constexpr (vt::dimension > 3) vec(res).w = S(0);
		return res;
	}
	template <VectorType Vec> requires (IsRank1<Vec>&& vector_traits<Vec>::dimension >= 4) constexpr Vec WAxis()
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = S(0);
		if constexpr (vt::dimension > 1) vec(res).y = S(0);
		if constexpr (vt::dimension > 2) vec(res).z = S(0);
		if constexpr (vt::dimension > 3) vec(res).w = S(1);
		return res;
	}
	template <VectorType Vec> requires (IsRank1<Vec>&& vector_traits<Vec>::dimension >= 4) constexpr Vec Origin()
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = S(0);
		if constexpr (vt::dimension > 1) vec(res).y = S(0);
		if constexpr (vt::dimension > 2) vec(res).z = S(0);
		if constexpr (vt::dimension > 3) vec(res).w = S(1);
		return res;
	}
	template <VectorType Mat> requires (IsRank2<Mat>) constexpr Mat Identity()
	{
		using vt = vector_traits<Mat>;

		Mat res = {};
		if constexpr (vt::dimension > 0) vec(res).x = XAxis<typename vt::component_t>();
		if constexpr (vt::dimension > 1) vec(res).y = YAxis<typename vt::component_t>();
		if constexpr (vt::dimension > 2) vec(res).z = ZAxis<typename vt::component_t>();
		if constexpr (vt::dimension > 3) vec(res).w = Origin<typename vt::component_t>();
		return res;
	}
	template <QuaternionType Quat> constexpr Quat Identity()
	{
		using S = typename vector_traits<Quat>::element_t;
		return { S(0), S(0), S(0), S(1) };
	}

	// NaN test
	template <ScalarType S> constexpr bool IsNaN(S value)
	{
		return value != value; // NaN is the only value that is not equal to itself
	}
	template <TensorType Vec> constexpr bool pr_vectorcall IsNaN(Vec v, bool any = true) // false = all
	{
		using vt = vector_traits<Vec>;
		bool yes;
		if (any)
		{
			yes = false;
			if constexpr (vt::dimension > 0) yes = yes || IsNaN(vec(v).x);
			if constexpr (vt::dimension > 1) yes = yes || IsNaN(vec(v).y);
			if constexpr (vt::dimension > 2) yes = yes || IsNaN(vec(v).z);
			if constexpr (vt::dimension > 3) yes = yes || IsNaN(vec(v).w);
		}
		else
		{
			yes = true;
			if constexpr (vt::dimension > 0) yes = yes && IsNaN(vec(v).x);
			if constexpr (vt::dimension > 1) yes = yes && IsNaN(vec(v).y);
			if constexpr (vt::dimension > 2) yes = yes && IsNaN(vec(v).z);
			if constexpr (vt::dimension > 3) yes = yes && IsNaN(vec(v).w);
		}
		return yes;
	}

	// Finite test
	template <ScalarType S> constexpr bool IsFinite(S value)
	{
		if consteval
		{
			// When float operations are performed at compile time, the compiler warnings about 'inf' and 'nan' are annoying and unhelpful, so handle them manually.
			return value != std::numeric_limits<S>::infinity() && value != -std::numeric_limits<S>::infinity() && value == value;
		}
		else
		{
			if constexpr (std::floating_point<S>)
				return std::isfinite(value);
			else
				return true;
		}
	}
	template <ScalarType S> constexpr bool IsFinite(S value, S max_value)
	{
		if consteval
		{
			return IsFinite(value) && Abs(value) < max_value;
		}
		else
		{
			return IsFinite(value) && Abs(value) < max_value;
		}
	}
	template <TensorType Vec> constexpr bool pr_vectorcall IsFinite(Vec v, bool any = false)
	{
		using vt = vector_traits<Vec>;
		bool yes;
		if (any)
		{
			yes = false;
			if constexpr (vt::dimension > 0) yes = yes || IsFinite(vec(v).x);
			if constexpr (vt::dimension > 1) yes = yes || IsFinite(vec(v).y);
			if constexpr (vt::dimension > 2) yes = yes || IsFinite(vec(v).z);
			if constexpr (vt::dimension > 3) yes = yes || IsFinite(vec(v).w);
		}
		else
		{
			yes = true;
			if constexpr (vt::dimension > 0) yes = yes && IsFinite(vec(v).x);
			if constexpr (vt::dimension > 1) yes = yes && IsFinite(vec(v).y);
			if constexpr (vt::dimension > 2) yes = yes && IsFinite(vec(v).z);
			if constexpr (vt::dimension > 3) yes = yes && IsFinite(vec(v).w);
		}
		return yes;
	}

	// Return true if any element satisfies 'Pred'
	template <TensorType Vec, typename Pred> constexpr bool pr_vectorcall Any(Vec v, Pred pred)
	{
		using vt = vector_traits<Vec>;
		bool yes = false;
		if constexpr (vt::dimension > 0) yes = yes || pred(vec(v).x);
		if constexpr (vt::dimension > 1) yes = yes || pred(vec(v).y);
		if constexpr (vt::dimension > 2) yes = yes || pred(vec(v).z);
		if constexpr (vt::dimension > 3) yes = yes || pred(vec(v).w);
		return yes;
	}

	// Return true if all elements satisfy 'Pred'
	template <TensorType Vec, typename Pred> constexpr bool pr_vectorcall All(Vec v, Pred pred)
	{
		using vt = vector_traits<Vec>;
		bool yes = true;
		if constexpr (vt::dimension > 0) yes = yes && pred(vec(v).x);
		if constexpr (vt::dimension > 1) yes = yes && pred(vec(v).y);
		if constexpr (vt::dimension > 2) yes = yes && pred(vec(v).z);
		if constexpr (vt::dimension > 3) yes = yes && pred(vec(v).w);
		return yes;
	}

	// Absolute value (component-wise)
	template <std::integral S> constexpr S Abs(S v)
	{
		return v >= S(0) ? v : -v;
	}
	template <std::floating_point S> constexpr S Abs(S v)
	{
		return v >= S(0) ? v : -v;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Abs(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Abs(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Abs(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Abs(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Abs(vec(v).w);
		return res;
	}

	// Min/Max
	template <ScalarType S> constexpr S Min(S x, S y)
	{
		return (x < y) ? x : y;
	}
	template <ScalarType S> constexpr S Max(S x, S y)
	{
		return (x < y) ? y : x;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Min(Vec x, Vec y)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Min(vec(x).x, vec(y).x);
		if constexpr (vt::dimension > 1) vec(res).y = Min(vec(x).y, vec(y).y);
		if constexpr (vt::dimension > 2) vec(res).z = Min(vec(x).z, vec(y).z);
		if constexpr (vt::dimension > 3) vec(res).w = Min(vec(x).w, vec(y).w);
		return res;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Max(Vec x, Vec y)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Max(vec(x).x, vec(y).x);
		if constexpr (vt::dimension > 1) vec(res).y = Max(vec(x).y, vec(y).y);
		if constexpr (vt::dimension > 2) vec(res).z = Max(vec(x).z, vec(y).z);
		if constexpr (vt::dimension > 3) vec(res).w = Max(vec(x).w, vec(y).w);
		return res;
	}
	template <typename T, typename... A> constexpr T Min(T const& x, T const& y, A&&... a)
	{
		return Min(Min(x, y), std::forward<A>(a)...);
	}
	template <typename T, typename... A> constexpr T Max(T const& x, T const& y, A&&... a)
	{
		return Max(Max(x, y), std::forward<A>(a)...);
	}

	// Clamp
	template <ScalarType S> constexpr S Clamp(S x, S mn, S mx)
	{
		pr_assert(!(mx < mn) && "[min,max] must be a positive range");
		return (mx < x) ? mx : (x < mn) ? mn : x;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Clamp(Vec x, Vec mn, Vec mx)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Clamp(vec(x).x, vec(mn).x, vec(mx).x);
		if constexpr (vt::dimension > 1) vec(res).y = Clamp(vec(x).y, vec(mn).y, vec(mx).y);
		if constexpr (vt::dimension > 2) vec(res).z = Clamp(vec(x).z, vec(mn).z, vec(mx).z);
		if constexpr (vt::dimension > 3) vec(res).w = Clamp(vec(x).w, vec(mn).w, vec(mx).w);
		return res;
	}

	// Square/Signed Square
	template <ScalarType S> constexpr S Square(S x)
	{
		return x * x;
	}
	template <ScalarType S> constexpr S SignedSqr(S x)
	{
		return x >= S() ? +Square(x) : -Square(x);
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall SignedSqr(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = SignedSqr(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = SignedSqr(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = SignedSqr(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = SignedSqr(vec(v).w);
		return res;
	}

	// Square root/Signed Square root
	constexpr double SqrtCT(double x)
	{
		// For a finite and non-negative value of "x", returns an approximation for the square root of "x", otherwise returns NaN.
		struct L
		{
			constexpr static double NewtonRaphson(double x, double curr, double prev)
			{
				return curr == prev ? curr : NewtonRaphson(x, 0.5 * (curr + x / curr), curr);
			}
		};
		return x >= 0 && x < std::numeric_limits<double>::infinity() ? L::NewtonRaphson(x, x, 0) : std::numeric_limits<double>::quiet_NaN();
	}
	template <ScalarType S> constexpr S Sqrt(S x)
	{
		if constexpr (std::floating_point<S>)
			pr_assert("Sqrt of undefined value" && IsFinite(x));
		if constexpr (std::is_signed_v<S>)
			pr_assert("Sqrt of negative value" && x >= S(0));

		if consteval
		{
			return static_cast<S>(SqrtCT(x));
		}
		else
		{
			return static_cast<S>(std::sqrt(x));
		}
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Sqrt(Vec)
	{
		// Sqrt is ill-defined for non-square matrices.
		// Matrices have an overload that finds the matrix whose product is 'x'.
		static_assert(std::is_same_v<Vec, void>, "Sqrt is not defined for general vector types");
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall CompSqrt(Vec v) // Component Sqrt
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Sqrt(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Sqrt(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Sqrt(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Sqrt(vec(v).w);
		return res;
	}
	template <ScalarType S> constexpr S SignedSqrt(S x)
	{
		return x >= S(0) ? +Sqrt(x) : -Sqrt(-x);
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall CompSignedSqrt(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = SignedSqrt(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = SignedSqrt(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = SignedSqrt(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = SignedSqrt(vec(v).w);
		return res;
	}

	// Integer square root
	template <std::integral T> constexpr T ISqrt(T x)
	{
		// Compile time version of the square root.
		//  - For a finite and non-negative value of "x", returns an approximation for the square root of "x"
		//  - This method always converges or oscillates about the answer with a difference of 1.
		//  - Otherwise, returns 0
		if (x < 0)
			return std::numeric_limits<T>::quiet_NaN();

		T curr = x, prev = 0, pprev = 0;
		for (;curr != prev && curr != pprev;)
		{
			pprev = prev;
			prev = curr;
			curr = (curr + x / curr) >> 1;
		}
		return Abs(x - curr * curr) < Abs(x - prev * prev) ? curr : prev;
	}

	// Return the component sum
	template <VectorType Vec> constexpr typename vector_traits<Vec>::element_t pr_vectorcall CompSum(Vec v)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		S sum = {};
		if constexpr (vt::dimension > 0) sum += vec(v).x;
		if constexpr (vt::dimension > 1) sum += vec(v).y;
		if constexpr (vt::dimension > 2) sum += vec(v).z;
		if constexpr (vt::dimension > 3) sum += vec(v).w;
		return sum;
	}

	// Scale each component of 'mat' by the values in 'scale'
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall CompMul(Mat const& mat, typename vector_traits<Mat>::component_t scale)
	{
		Mat m = mat;
		if constexpr (vector_traits<Mat>::dimension > 0) vec(m).x *= scale;
		if constexpr (vector_traits<Mat>::dimension > 1) vec(m).y *= scale;
		if constexpr (vector_traits<Mat>::dimension > 2) vec(m).z *= scale;
		if constexpr (vector_traits<Mat>::dimension > 3) vec(m).w *= scale;
		return m;
	}

	// Min/Max element (i.e. nearest to -inf/+inf)
	template <ScalarType S> inline constexpr S MinElement(S v)
	{
		return v;
	}
	template <ScalarType S> inline constexpr S MaxElement(S v)
	{
		return v;
	}
	template <TensorType Vec> constexpr typename vector_traits<Vec>::element_t pr_vectorcall MinElement(Vec v)
	{
		using vt = vector_traits<Vec>;
		auto minimum = Max<typename vt::element_t>();
		if constexpr (vt::dimension > 0) minimum = std::min(minimum, MinElement(vec(v).x));
		if constexpr (vt::dimension > 1) minimum = std::min(minimum, MinElement(vec(v).y));
		if constexpr (vt::dimension > 2) minimum = std::min(minimum, MinElement(vec(v).z));
		if constexpr (vt::dimension > 3) minimum = std::min(minimum, MinElement(vec(v).w));
		return minimum;
	}
	template <TensorType Vec> constexpr typename vector_traits<Vec>::element_t pr_vectorcall MaxElement(Vec v)
	{
		using vt = vector_traits<Vec>;
		auto maximum = Min<typename vt::element_t>();
		if constexpr (vt::dimension > 0) maximum = std::max(maximum, MaxElement(vec(v).x));
		if constexpr (vt::dimension > 1) maximum = std::max(maximum, MaxElement(vec(v).y));
		if constexpr (vt::dimension > 2) maximum = std::max(maximum, MaxElement(vec(v).z));
		if constexpr (vt::dimension > 3) maximum = std::max(maximum, MaxElement(vec(v).w));
		return maximum;
	}

	// Min/Max absolute element (i.e. nearest to 0/+inf)
	template <TensorType Vec> constexpr typename vector_traits<Vec>::element_t pr_vectorcall MinElementAbs(Vec v)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		auto res = Max<S>();
		if constexpr (vt::dimension > 0) res = std::min(res, MinElementAbs(vec(v).x));
		if constexpr (vt::dimension > 1) res = std::min(res, MinElementAbs(vec(v).y));
		if constexpr (vt::dimension > 2) res = std::min(res, MinElementAbs(vec(v).z));
		if constexpr (vt::dimension > 3) res = std::min(res, MinElementAbs(vec(v).w));
		return res;
	}
	template <TensorType Vec> constexpr typename vector_traits<Vec>::element_t pr_vectorcall MaxElementAbs(Vec v)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		auto res = Min<S>();
		if constexpr (vt::dimension > 0) res = std::max(res, MinElementAbs(vec(v).x));
		if constexpr (vt::dimension > 1) res = std::max(res, MinElementAbs(vec(v).y));
		if constexpr (vt::dimension > 2) res = std::max(res, MinElementAbs(vec(v).z));
		if constexpr (vt::dimension > 3) res = std::max(res, MinElementAbs(vec(v).w));
		return res;
	}

	// Smallest/Largest element index. Returns the index of the first min/max element if elements are equal.
	template <TensorType Vec> constexpr int pr_vectorcall MinElementIndex(Vec v)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		auto idx = 0;
		if constexpr (vt::dimension > 0) idx = 0;
		if constexpr (vt::dimension > 1) idx = vec(v).y < vec(v)[idx] ? 1 : idx;
		if constexpr (vt::dimension > 2) idx = vec(v).z < vec(v)[idx] ? 2 : idx;
		if constexpr (vt::dimension > 3) idx = vec(v).w < vec(v)[idx] ? 3 : idx;
		return idx;
	}
	template <TensorType Vec> constexpr int pr_vectorcall MaxElementIndex(Vec v)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		auto idx = 0;
		if constexpr (vt::dimension > 0) idx = 0;
		if constexpr (vt::dimension > 1) idx = vec(v).y > vec(v)[idx] ? 1 : idx;
		if constexpr (vt::dimension > 2) idx = vec(v).z > vec(v)[idx] ? 2 : idx;
		if constexpr (vt::dimension > 3) idx = vec(v).w > vec(v)[idx] ? 3 : idx;
		return idx;
	}

	// Floating point comparisons. *WARNING* 'tol' is an absolute tolerance. Returns true if 'a' is in the range (b-tol,b+tol)
	template <std::integral T> constexpr bool FEqlAbsolute(T a, T b, auto)
	{
		return a == b;
	}
	template <std::floating_point T> constexpr bool FEqlAbsolute(T a, T b, T tol)
	{
		// When float operations are performed at compile time, the compiler warnings about 'inf'
		assert(tol >= 0 || !(tol == tol)); // NaN is not an error, comparisons with NaN are defined to always be false
		return Abs(a - b) < tol;
	}
	template <TensorType Vec> constexpr bool pr_vectorcall FEqlAbsolute(Vec lhs, Vec rhs, auto tol)
	{
		using vt = vector_traits<Vec>;
		bool eql = true;
		if constexpr (vt::dimension > 0) eql &= FEqlAbsolute(vec(lhs).x, vec(rhs).x, tol);
		if constexpr (vt::dimension > 1) eql &= FEqlAbsolute(vec(lhs).y, vec(rhs).y, tol);
		if constexpr (vt::dimension > 2) eql &= FEqlAbsolute(vec(lhs).z, vec(rhs).z, tol);
		if constexpr (vt::dimension > 3) eql &= FEqlAbsolute(vec(lhs).w, vec(rhs).w, tol);
		return eql;
	}

	// Floating point comparisons. *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
	template <std::integral T> constexpr bool FEqlRelative(T a, T b, auto)
	{
		return a == b;
	}
	template <std::floating_point T> constexpr bool FEqlRelative(T a, T b, T tol)
	{
		// Floating point compare is dangerous and subtle.
		// See: https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
		// and: http://floating-point-gui.de/errors/NearlyEqualsTest.java
		// Tests against zero treat 'tol' as an absolute difference threshold.
		// Tests between two non-zero values use 'tol' as a relative difference threshold.
		// i.e.
		//    FEql(2e-30, 1e-30) == false
		//    FEql(2e-30 - 1e-30, 0) == true

		// Handles tests against zero where relative error is meaningless
		// Tests with 'b == 0' are the most common so do them first
		if (b == 0) return Abs(a) < tol;
		if (a == 0) return Abs(b) < tol;

		// Handle infinities and exact values
		if (a == b) return true;

		// Test relative error as a fraction of the largest value
		auto abs_max_element = std::max(Abs(a), Abs(b));
		return FEqlAbsolute(a, b, tol * abs_max_element);
	}
	template <TensorType Vec> constexpr bool pr_vectorcall FEqlRelative(Vec lhs, Vec rhs, auto tol)
	{
		auto max_a = MaxElement(Abs(lhs));
		auto max_b = MaxElement(Abs(rhs));
		if (max_b == 0) return max_a < tol;
		if (max_a == 0) return max_b < tol;
		auto abs_max_element = std::max(max_a, max_b);
		return FEqlAbsolute(lhs, rhs, tol * abs_max_element);
	}

	// FEqlRelative using 'Tiny'. Returns true if a in the range (b - max(a,b)*tiny, b + max(a,b)*tiny)
	template <std::integral S> constexpr bool FEql(S a, S b)
	{
		return a == b;
	}
	template <std::floating_point S> constexpr bool FEql(S a, S b)
	{
		// Don't add a 'tol' parameter because it looks like the function should perform a == b +- tol, which isn't what it does.
		return FEqlRelative(a, b, constants<S>::tiny);
	}
	template <TensorType Vec> constexpr bool pr_vectorcall FEql(Vec lhs, Vec rhs)
	{
		using S = typename vector_traits<Vec>::element_t;
		return FEqlRelative(lhs, rhs, constants<S>::tiny);
	}

	// Ceil/Floor/Round/Modulus
	template <ScalarType S> constexpr S Ceil(S x)
	{
		if consteval
		{
			return
				(x == +std::numeric_limits<S>::infinity()) ? x :
				(x == -std::numeric_limits<S>::infinity()) ? x :
				(x == x) ? static_cast<S>(std::ceil(x)) :
				std::numeric_limits<S>::quiet_NaN();
		}
		else
		{
			return static_cast<S>(std::ceil(x));
		}
	}
	template <ScalarType S> constexpr S Floor(S x)
	{
		if consteval
		{
			return
				(x == +std::numeric_limits<S>::infinity()) ? x :
				(x == -std::numeric_limits<S>::infinity()) ? x :
				(x == x) ? static_cast<S>(std::floor(x)) :
				std::numeric_limits<S>::quiet_NaN();
		}
		else
		{
			return static_cast<S>(std::floor(x));
		}
	}
	template <ScalarType S> constexpr S Round(S x)
	{
		if consteval
		{
			return
				(x == +std::numeric_limits<S>::infinity()) ? x :
				(x == -std::numeric_limits<S>::infinity()) ? x :
				(x == x) ? static_cast<S>(std::round(x)) :
				std::numeric_limits<S>::quiet_NaN();
		}
		else
		{
			return static_cast<S>(std::round(x));
		}
	}
	template <ScalarType S> constexpr S RoundSD(S d, int significant_digits)
	{
		pr_assert(significant_digits >= 0 && "'significant_digits' value must be >= 0");

		// No significant digits is always zero
		if (d == 0 || significant_digits == 0)
			return 0;

		if constexpr (std::is_same_v<S, long long>) // int64_t is 19 digits
		{
			if (significant_digits > 19)
				return d;
		}
		if constexpr (std::is_same_v<S, float>) // float's mantissa is 7 digits
		{
			if (significant_digits > 7)
				return d;
		}
		if constexpr (std::is_same_v<S, double>) // double's mantissa is 17 digits
		{
			if (significant_digits > 17)
				return d;
		}

		auto pow = static_cast<int>(std::floor(std::log10(std::abs(d))));
		auto scale = std::pow(S(10), significant_digits - pow - 1);
		auto result = scale != 0 ? static_cast<S>(Round<double>(d * scale) / scale) : S{};
		return result;
	}
	template <ScalarType S> constexpr S Modulus(S x, S y)
	{
		if constexpr (std::floating_point<S>)
			return std::fmod(x, y);
		else
			return x % y;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Ceil(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Ceil(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Ceil(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Ceil(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Ceil(vec(v).w);
		return res;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Floor(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Floor(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Floor(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Floor(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Floor(vec(v).w);
		return res;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Round(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Round(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Round(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Round(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Round(vec(v).w);
		return res;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall RoundSD(Vec v, int significant_digits)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = RoundSD(vec(v).x, significant_digits);
		if constexpr (vt::dimension > 1) vec(res).y = RoundSD(vec(v).y, significant_digits);
		if constexpr (vt::dimension > 2) vec(res).z = RoundSD(vec(v).z, significant_digits);
		if constexpr (vt::dimension > 3) vec(res).w = RoundSD(vec(v).w, significant_digits);
		return res;
	}
	template <TensorType Vec> constexpr Vec Modulus(Vec x, Vec y)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Modulus(vec(x).x, vec(y).x);
		if constexpr (vt::dimension > 1) vec(res).y = Modulus(vec(x).y, vec(y).y);
		if constexpr (vt::dimension > 2) vec(res).z = Modulus(vec(x).z, vec(y).z);
		if constexpr (vt::dimension > 3) vec(res).w = Modulus(vec(x).w, vec(y).w);
		return res;
	}

	// Wrap 'x' to range [mn, mx)
	template <ScalarType S> constexpr S Wrap(S x, S mn, S mx)
	{
		// Given the range ['mn', 'mx') and 'x' somewhere on the number line.
		// Return 'x' wrapped into the range, allowing for 'x' < 'mn'.
		auto range = mx - mn;
		return mn + Modulus((Modulus(x - mn, range) + range), range);
	}

	// Converts bool to +1,-1 (note: no 0 value)
	constexpr int Bool2SignI(bool positive)
	{
		return positive ? +1 : -1;
	}
	constexpr float Bool2SignF(bool positive)
	{
		return positive ? +1.0f : -1.0f;
	}

	// Sign, returns +1 if x >= 0 otherwise -1. If 'zero_is_positive' is false, then 0 in gives 0 out.
	template <ScalarType S> constexpr S Sign(S x, bool zero_is_positive = true)
	{
		if constexpr (std::is_unsigned_v<S>)
			return x > 0 ? +S(1) : static_cast<S>(zero_is_positive);
		else
			return x > 0 ? +S(1) : x < 0 ? -S(1) : static_cast<S>(zero_is_positive);
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Sign(Vec v, bool zero_is_positive = true)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Sign(vec(v).x, zero_is_positive);
		if constexpr (vt::dimension > 1) vec(res).y = Sign(vec(v).y, zero_is_positive);
		if constexpr (vt::dimension > 2) vec(res).z = Sign(vec(v).z, zero_is_positive);
		if constexpr (vt::dimension > 3) vec(res).w = Sign(vec(v).w, zero_is_positive);
		return res;
	}

	// Divide 'a' by 'b' if 'b' is not equal to zero, otherwise return 'def'
	template <typename T> constexpr T Div(T a, T b, T def = {}) requires (requires (T x) { x / x; x != x; })
	{
		return b != T{} ? a / b : def;
	}

	// Truncate value
	template <ScalarType S> constexpr S Trunc(S x, ETruncate trunc = ETruncate::TowardZero) requires (std::floating_point<S>)
	{
		switch (trunc)
		{
			case ETruncate::ToNearest:  return static_cast<S>(static_cast<long long>(x + Sign(x) * S(0.5)));
			case ETruncate::TowardZero: return static_cast<S>(static_cast<long long>(x));
			default: pr_assert("Unknown truncation type" && false); return x;
		}
	}
	template <TensorTypeFP Vec> constexpr Vec pr_vectorcall Trunc(Vec v, ETruncate trunc = ETruncate::TowardZero)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Trunc(vec(v).x, trunc);
		if constexpr (vt::dimension > 1) vec(res).y = Trunc(vec(v).y, trunc);
		if constexpr (vt::dimension > 2) vec(res).z = Trunc(vec(v).z, trunc);
		if constexpr (vt::dimension > 3) vec(res).w = Trunc(vec(v).w, trunc);
		return res;
	}

	// Fractional part
	template <ScalarType S> constexpr S Frac(S x) requires (std::floating_point<S>)
	{
		if consteval
		{
			if (x == +std::numeric_limits<S>::infinity() || x == -std::numeric_limits<S>::infinity() || !(x == x))
				return std::numeric_limits<S>::quiet_NaN();
			else
				return x - Floor(x);
		}
		else
		{
			S n;
			return std::modf(x, &n);
		}
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Frac(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Frac(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Frac(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Frac(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Frac(vec(v).w);
		return res;
	}

	// Square a value
	template <ScalarType S> constexpr S Sqr(S x)
	{
		if constexpr (std::is_same_v<S, int8_t>)
			pr_assert("Overflow" && Abs(x) <= 0xB);
		if constexpr (std::is_same_v<S, uint8_t>)
			pr_assert("Overflow" && Abs(x) <= 0xF);
		if constexpr (std::is_same_v<S, int16_t>)
			pr_assert("Overflow" && Abs(x) <= 0xB5);
		if constexpr (std::is_same_v<S, uint16_t>)
			pr_assert("Overflow" && Abs(x) <= 0xFF);
		if constexpr (std::is_same_v<S, int32_t> || std::is_same_v<S, long>)
			pr_assert("Overflow" && Abs(x) <= 0xB504);
		if constexpr (std::is_same_v<S, uint32_t> || std::is_same_v<S, unsigned long>)
			pr_assert("Overflow" && Abs(x) <= 0xFFFFU);
		if constexpr (std::is_same_v<S, int64_t>)
			pr_assert("Overflow" && Abs(x) <= 0xB504F333LL);
		if constexpr (std::is_same_v<S, uint64_t>)
			pr_assert("Overflow" && Abs(x) <= 0xFFFFFFFFULL);

		return x * x;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Sqr(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Sqr(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Sqr(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Sqr(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Sqr(vec(v).w);
		return res;
	}

	// Cube a value
	template <ScalarType S> constexpr S Cube(S x)
	{
		if constexpr (std::is_same_v<S, int8_t>)
			pr_assert("Overflow" && Abs(x) <= 0x5);
		if constexpr (std::is_same_v<S, uint8_t>)
			pr_assert("Overflow" && Abs(x) <= 0x6);
		if constexpr (std::is_same_v<S, int16_t>)
			pr_assert("Overflow" && Abs(x) <= 0x1F);
		if constexpr (std::is_same_v<S, uint16_t>)
			pr_assert("Overflow" && Abs(x) <= 0x28);
		if constexpr (std::is_same_v<S, int32_t> || std::is_same_v<S, long>)
			pr_assert("Overflow" && Abs(x) <= 0x50A);
		if constexpr (std::is_same_v<S, uint32_t> || std::is_same_v<S, unsigned long>)
			pr_assert("Overflow" && Abs(x) <= 0x659U);
		if constexpr (std::is_same_v<S, int64_t>)
			pr_assert("Overflow" && Abs(x) <= 0x1FFFFFLL);
		if constexpr (std::is_same_v<S, uint64_t>)
			pr_assert("Overflow" && Abs(x) <= 0x285145ULL);

		return x * x * x;
	}
	template <TensorType Vec> constexpr Vec pr_vectorcall Cube(Vec v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Cube(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Cube(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Cube(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Cube(vec(v).w);
		return res;
	}

	// Raise 'x' to an integer power
	template <ScalarType S> constexpr S Pow(S x, int y)
	{
		return y == 0 ? 1 : x * Pow(x, y - 1);
	}

	// Convert degrees/radians
	template <ScalarType S> constexpr S DegreesToRadians(S degrees)
	{
		return static_cast<S>(degrees * Tau<S>() / S(360));
	}
	template <ScalarType S> constexpr S RadiansToDegrees(S radians)
	{
		return static_cast<S>(radians * S(360) / Tau<S>());
	}

	// Vector dot product
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr typename vector_traits<Vec>::element_t Dot(Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		auto product = S(0);
		if constexpr (vt::dimension > 0) product += vec(lhs).x * vec(rhs).x;
		if constexpr (vt::dimension > 1) product += vec(lhs).y * vec(rhs).y;
		if constexpr (vt::dimension > 2) product += vec(lhs).z * vec(rhs).z;
		if constexpr (vt::dimension > 3) product += vec(lhs).w * vec(rhs).w;
		return product;
	}
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr typename vector_traits<Vec>::element_t Dot3(Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		auto product = S(0);
		if constexpr (vt::dimension > 0) product += vec(lhs).x * vec(rhs).x;
		if constexpr (vt::dimension > 1) product += vec(lhs).y * vec(rhs).y;
		if constexpr (vt::dimension > 2) product += vec(lhs).z * vec(rhs).z;
		return product;
	}

	// Vector cross product
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension == 2)
	constexpr typename vector_traits<Vec>::component_t Cross(Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		return vec(lhs).y * vec(rhs).x - vec(lhs).x * vec(rhs).y; // 2D Cross product == Dot(Rotate90CW(lhs), rhs)
	}
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension == 3)
	constexpr Vec Cross(Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		return Vec {
			vec(lhs).y * vec(rhs).z - vec(lhs).z * vec(rhs).y,
			vec(lhs).z * vec(rhs).x - vec(lhs).x * vec(rhs).z,
			vec(lhs).x * vec(rhs).y - vec(lhs).y * vec(rhs).x,
		};
	}
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension == 4)
	constexpr Vec Cross3(Vec lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;
		return Vec {
			vec(lhs).y * vec(rhs).z - vec(lhs).z * vec(rhs).y,
			vec(lhs).z * vec(rhs).x - vec(lhs).x * vec(rhs).z,
			vec(lhs).x * vec(rhs).y - vec(lhs).y * vec(rhs).x,
			typename vt::element_t(0)
		};
	}

	// Vector triple product: a . b x c
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension == 3)
	constexpr typename vector_traits<Vec>::element_t Triple(Vec a, Vec b, Vec c)
	{
		return Dot(a, Cross(b, c));
	}
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension >= 3)
	constexpr typename vector_traits<Vec>::element_t Triple3(Vec a, Vec b, Vec c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Squared Length of a vector
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr typename vector_traits<Vec>::element_t LengthSq(Vec v)
	{
		return Dot(v, v);
	}

	// Length of a vector
	template <std::integral S> constexpr S Length(S x)
	{
		// Defined for use in recursive vector functions
		return Abs(x);
	}
	template <std::floating_point S> constexpr S Length(S x)
	{
		// Defined for use in recursive vector functions
		return Abs(x);
	}
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr typename vector_traits<Vec>::element_t Length(Vec v)
	{
		using S = typename vector_traits<Vec>::element_t;
		return Sqrt<S>(LengthSq(v));
	}

	// Return the trace of this matrix, 
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr typename vector_traits<Mat>::element_t pr_vectorcall Trace(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		S res = {};
		if constexpr (vt::dimension > 0) res += vec(vec(mat).x).x;
		if constexpr (vt::dimension > 1) res += vec(vec(mat).y).y;
		if constexpr (vt::dimension > 2) res += vec(vec(mat).z).z;
		if constexpr (vt::dimension > 3) res += vec(vec(mat).w).w;
		return res;
	}

	// Return the determinant of 'mat'
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr typename vector_traits<Mat>::element_t pr_vectorcall Determinant(Mat const& mat)
	{
		using vt = vector_traits<Mat>;

		// @Copilot, is this correct?
		if constexpr (vt::dimension == 1)
		{
			return vec(mat).x;
		}
		if constexpr (vt::dimension == 2)
		{
			return vec(mat).x * vec(mat).y - vec(mat).y * vec(mat).x;
		}
		if constexpr (vt::dimension == 3)
		{
			return
				vec(mat).x * Cross(vec(mat).y, vec(mat).z) -
				vec(mat).y * Cross(vec(mat).x, vec(mat).z) +
				vec(mat).z * Cross(vec(mat).x, vec(mat).y);
		}
		if constexpr (vt::dimension == 4)
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
	}

	// Return the 4x4 determinant of the affine transform 'mat'
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr typename vector_traits<Mat>::element_t pr_vectorcall DeterminantAffine(Mat const& mat)
	{
		using vt = vector_traits<Mat>;

		// Theres probably versions of this for 2x2 and 3x3 as well
		if constexpr (vt::dimension == 4)
		{
			pr_assert(IsAffine(mat) && "'mat' must be an affine transform to use this function");
			return
				(mat.x.x * mat.y.y * mat.z.z) +
				(mat.x.y * mat.y.z * mat.z.x) +
				(mat.x.z * mat.y.x * mat.z.y) -
				(mat.x.z * mat.y.y * mat.z.x) -
				(mat.x.y * mat.y.x * mat.z.z) -
				(mat.x.x * mat.y.z * mat.z.y);
		}
	}

	// Return a vector containing the diagonal elements of this matrix, 
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr typename vector_traits<Mat>::component_t pr_vectorcall Diagonal(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using Vec = typename vt::component_t;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = vec(vec(mat).x).x;
		if constexpr (vt::dimension > 1) vec(res).y = vec(vec(mat).y).y;
		if constexpr (vt::dimension > 2) vec(res).z = vec(vec(mat).z).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(vec(mat).w).w;
		return res;
	}

	// Return the kernel of 'mat'
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr typename vector_traits<Mat>::component_t pr_vectorcall Kernel(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using Vec = typename vt::component_t;

		// @Copilot, please check if these are correct
		if constexpr (vt::dimension == 2)
		{
			auto xx = vec(vec(mat).x).x, xy = vec(vec(mat).x).y;
			auto yx = vec(vec(mat).y).x, yy = vec(vec(mat).y).y;

			return Vec{
				+yy,
				-yx,
			};
		}
		if constexpr (vt::dimension == 3)
		{
			auto xx = vec(vec(mat).x).x, xy = vec(vec(mat).x).y, xz = vec(vec(mat).x).z;
			auto yx = vec(vec(mat).y).x, yy = vec(vec(mat).y).y, yz = vec(vec(mat).y).z;
			auto zx = vec(vec(mat).z).x, zy = vec(vec(mat).z).y, zz = vec(vec(mat).z).z;

			return Vec{
				+yy * zz - yz * zy,
				-yx * zz + yz * zx,
				+yx * zy - yy * zx,
			};
		}
		if constexpr (vt::dimension == 4)
		{
			auto xx = vec(vec(mat).x).x, xy = vec(vec(mat).x).y, xz = vec(vec(mat).x).z, xw = vec(vec(mat).x).w;
			auto yx = vec(vec(mat).y).x, yy = vec(vec(mat).y).y, yz = vec(vec(mat).y).z, yw = vec(vec(mat).y).w;
			auto zx = vec(vec(mat).z).x, zy = vec(vec(mat).z).y, zz = vec(vec(mat).z).z, zw = vec(vec(mat).z).w;
			auto wx = vec(vec(mat).w).x, wy = vec(vec(mat).w).y, wz = vec(vec(mat).w).z, ww = vec(vec(mat).w).w;

			return Vec{
				+yy * (zz * ww - zw * wz) - yz * (zy * ww - zw * wy) + yw * (zy * wz - zz * wy),
				-yx * (zz * ww - zw * wz) + yz * (zx * ww - zw * wx) - yw * (zx * wz - zz * wx),
				+yx * (zy * ww - zw * wy) - yy * (zx * ww - zw * wx) + yw * (zx * wy - zy * wx),
				-yx * (zy * wz - zz * wy) + yy * (zx * wz - zz * wx) - yz * (zx * wy - zy * wx),
			};
		}
	}

	// Normalise a vector
	template <TensorTypeFP Vec> requires (IsRank1<Vec>)
	constexpr Vec pr_vectorcall Normalise(Vec v)
	{
		return v / Length(v);
	}
	template <TensorTypeFP Vec> requires (IsRank1<Vec>)
	constexpr Vec pr_vectorcall Normalise(Vec v, Vec value_if_zero_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		auto len = Length(v);
		return len > Tiny<S>() ? v / len : value_if_zero_length;
	}
	template <TensorTypeFP Vec, typename IfZeroFactory> requires (IsRank1<Vec> && requires (IfZeroFactory f) { { f() } -> std::convertible_to<Vec>; })
	constexpr Vec pr_vectorcall Normalise(Vec v, IfZeroFactory value_if_zero_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		auto len = Length(v);
		return len > Tiny<S>() ? v / len : value_if_zero_length();
	}

	// Normalise the columns of a matrix returning the lengths prior to renormalising
	template <VectorTypeFP Mat> requires (IsRank2<Mat>)
	inline std::tuple<Mat, typename vector_traits<Mat>::component_t> Normalise(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		auto m = mat;
		auto scale = Vec{ Length(vec(m).x), Length(vec(m).y), Length(vec(m).z) };
		vec(m).x /= vec(scale).x;
		vec(m).y /= vec(scale).y;
		vec(m).z /= vec(scale).z;
		return { m, scale };
	}

	// Return true if 'mat' is an orthonormal matrix
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr bool pr_vectorcall IsNormalised(Vec v, typename vector_traits<Vec>::element_t tol = tiny<typename vector_traits<Vec>::element_t>)
	{
		using S = typename vector_traits<Vec>::element_t;
		return Abs(LengthSq(v) - S(1)) < tol;
	}

	// Return true if 'mat' is an orthogonal matrix
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension >= 2)
	constexpr bool pr_vectorcall IsOrthogonal(Mat const& mat, typename vector_traits<Mat>::element_t tol = tiny<typename vector_traits<Mat>::element_t>)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		if constexpr (vt::dimension == 2)
		{
			return
				Abs(Dot(vec(mat).x, vec(mat).y)) < tol;
		}
		if constexpr (vt::dimension == 3)
		{
			return
				Abs(Dot(vec(mat).x, vec(mat).y)) < tol &&
				Abs(Dot(vec(mat).x, vec(mat).z)) < tol &&
				Abs(Dot(vec(mat).y, vec(mat).z)) < tol;
		}
		if constexpr (vt::dimension == 4)
		{
			return
				Abs(Dot(vec(mat).x, vec(mat).y)) < tol &&
				Abs(Dot(vec(mat).x, vec(mat).z)) < tol &&
				Abs(Dot(vec(mat).x, vec(mat).w)) < tol &&
				Abs(Dot(vec(mat).y, vec(mat).z)) < tol &&
				Abs(Dot(vec(mat).y, vec(mat).w)) < tol &&
				Abs(Dot(vec(mat).z, vec(mat).w)) < tol;
		}
	}

	// Return true if 'mat' is an orthonormal matrix
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr bool pr_vectorcall IsOrthonormal(Mat const& mat, typename vector_traits<Mat>::element_t tol = tiny<typename vector_traits<Mat>::element_t>)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		if constexpr (vt::dimension >= 1) if (Abs(LengthSq(vec(mat).x) - S(1)) > tol) return false;
		if constexpr (vt::dimension >= 2) if (Abs(LengthSq(vec(mat).y) - S(1)) > tol) return false;
		if constexpr (vt::dimension >= 3) if (Abs(LengthSq(vec(mat).z) - S(1)) > tol) return false;
		if constexpr (vt::dimension == 2) if (Abs(Cross(vec(mat).x, vec(mat).y) - S(1)) > tol) return false;
		if constexpr (vt::dimension >= 3) if (Abs(Triple3(vec(mat).x, vec(mat).y, vec(mat).z) - S(1)) > tol) return false;
		return true;
	}

	// Return true if 'mat' is an affine transform
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension >= 3)
	constexpr bool pr_vectorcall IsAffine(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		if constexpr (vt::dimension >= 3)
		{
			if (vec(vec(mat).x).w != S(0)) return false;
			if (vec(vec(mat).y).w != S(0)) return false;
			if (vec(vec(mat).z).w != S(0)) return false;
		}
		if constexpr (vt::dimension >= 4)
		{
			if (vec(vec(mat).w).w != S(1)) return false;
		}
		return true;
	}

	// True if 'mat' can be inverted
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr bool pr_vectorcall IsInvertible(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		return Determinant(mat) != S(0);
	}

	// True if 'mat' is symmetric
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr bool pr_vectorcall IsSymmetric(Mat const& mat, typename vector_traits<Mat>::element_t tol = tiny<typename vector_traits<Mat>::element_t>)
	{
		using vt = vector_traits<Mat>;

		if constexpr (vt::dimension >= 2)
		{
			if (Abs(vec(vec(mat).x).y - vec(vec(mat).y).x) > tol)
				return false;
		}
		if constexpr (vt::dimension >= 3)
		{
			if (Abs(vec(vec(mat).x).z - vec(vec(mat).z).x) > tol ||
				Abs(vec(vec(mat).y).z - vec(vec(mat).z).y) > tol)
				return false;
		}
		if constexpr (vt::dimension >= 4)
		{
			if (Abs(vec(vec(mat).x).w - vec(vec(mat).w).x) > tol ||
				Abs(vec(vec(mat).y).w - vec(vec(mat).w).y) > tol ||
				Abs(vec(vec(mat).z).w - vec(vec(mat).w).z) > tol)
				return false;
		}
		return true;
	}

	// True if 'mat' is anti-symmetric
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr bool pr_vectorcall IsAntiSymmetric(Mat const& mat, typename vector_traits<Mat>::element_t tol = tiny<typename vector_traits<Mat>::element_t>)
	{
		using vt = vector_traits<Mat>;

		if constexpr (vt::dimension >= 2)
		{
			if (Abs(vec(vec(mat).x).y + vec(vec(mat).y).x) > tol)
				return false;
		}
		if constexpr (vt::dimension >= 3)
		{
			if (Abs(vec(vec(mat).x).z + vec(vec(mat).z).x) > tol ||
				Abs(vec(vec(mat).y).z + vec(vec(mat).z).y) > tol)
				return false;
		}
		if constexpr (vt::dimension >= 4)
		{
			if (Abs(vec(vec(mat).x).w + vec(vec(mat).w).x) > tol ||
				Abs(vec(vec(mat).y).w + vec(vec(mat).w).y) > tol ||
				Abs(vec(vec(mat).z).w + vec(vec(mat).w).z) > tol)
				return false;
		}
		return true;
	}

	// Returns true if 'a' and 'b' are parallel
	template <VectorType Vec> requires (IsRank1<Vec>)
	constexpr bool pr_vectorcall IsParallel(Vec v0, Vec v1, typename vector_traits<Vec>::element_t tol = tiny<typename vector_traits<Vec>::element_t>)
	{
		return LengthSq(Cross3(v0, v1)) <= Sqr(tol); // '<=' to allow for 'tol' == 0.0
	}

	// Returns a vector guaranteed not parallel to 'v'
	template <VectorType Vec> requires (IsRank1<Vec>)
	constexpr Vec pr_vectorcall CreateNotParallelTo(Vec v)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		if constexpr (vt::dimension < 3)
		{
			return Vec{ -vec(v).y, vec(v).x };
		}
		else
		{
			bool x_aligned = Abs(vec(v).x) > Abs(vec(v).y) && Abs(vec(v).x) > Abs(vec(v).z);
			return Vec{ static_cast<S>(!x_aligned), S(0), static_cast<S>(x_aligned) };
		}
	}

	// Returns a vector perpendicular to 'v' with the same length of 'v'
	template <VectorType Vec> requires (IsRank1<Vec>)
	constexpr Vec pr_vectorcall Perpendicular(Vec vec)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		pr_assert(vec != Zero<Vec>() && "Cannot make a perpendicular to a zero vector");

		auto v = Cross(vec, CreateNotParallelTo(vec));
		v *= Sqrt(LengthSq(vec) / LengthSq(v));
		return v;
	}

	// Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular.
	template <VectorType Vec> requires (IsRank1<Vec>)
	constexpr Vec pr_vectorcall Perpendicular(Vec vec, Vec previous)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		// The length of the returned vector will be 'Length(vec)' or 'Length(previous)' (typically they'd be the same)
		// Either 'vec' or 'previous' can be zero, but not both.
		if (vec == Zero<Vec>())
		{
			// Both 'vec' and 'previous' cannot be zero
			pr_assert(previous != Zero<Vec>() && "Cannot make a perpendicular to a zero vector");
			return previous;
		}
		if (IsParallel(vec, previous)) // includes 'previous' == zero
		{
			// If 'previous' is parallel to 'vec', choose a new perpendicular
			return Perpendicular(vec);
		}

		// If 'previous' is still perpendicular, reuse it
		if (Abs(Dot3(vec, previous)) < tiny<S>)
			return previous;

		// Otherwise, make a perpendicular that is close to 'previous'
		return Normalise(Cross(Cross(vec, previous), vec));
	}

	// Permute the values in a vector. Rolls <--. e.g. 0:xyzw, 1:yzwx, 2:zwxy, etc
	template <VectorType Vec> requires (IsRank1<Vec>)
	constexpr Vec pr_vectorcall Permute(Vec v, int n)
	{
		using vt = vector_traits<Vec>;

		// Negative 'n' rolls in the opposite direction. e.g. -1: wxyz, -2: zwxy, etc
		if (n < 0)
			n = vt::dimension + (n % vt::dimension);

		if constexpr (vt::dimension == 2)
		{
			switch (n % 2)
			{
				case 0:  return v;
				case 1:  return Vec{ vec(v).y, vec(v).x };
			}
		}
		if constexpr (vt::dimension == 3)
		{
			switch (n % 3)
			{
				case 0:  return v;
				case 1:  return Vec{ vec(v).y, vec(v).z, vec(v).x };
				case 2:  return Vec{ vec(v).z, vec(v).x, vec(v).y };
			}
		}
		if constexpr (vt::dimension == 4)
		{
			switch (n % 4)
			{
				case 0:  return v;
				case 1:  return Vec{ vec(v).y, vec(v).z, vec(v).w, vec(v).x };
				case 2:  return Vec{ vec(v).z, vec(v).w, vec(v).x, vec(v).y };
				case 3:  return Vec{ vec(v).w, vec(v).x, vec(v).y, vec(v).z };
			}
		}
		pr_assert(false && "Invalid vector dimension");
	}
	
	// Permute the vectors in a matrix by 'n'. Rolls <--. e.g. 0:xyzw, 1:yzwx, 2:zwxy, etc
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall Permute(Mat mat, int n)
	{
		// Rolls the vectors around, last becomes first
		using vt = vector_traits<Mat>;
		pr_assert(n >= 0 && "'n' should be non-negative");

		if constexpr (vt::dimension == 2)
		{
			switch (n % 2)
			{
				case 0: return mat;
				case 1: return Mat{ vec(mat).y, vec(mat).x };
			};
		}
		if constexpr (vt::dimension == 3)
		{
			switch (n % 3)
			{
				case 0: return mat;
				case 1: return Mat{ vec(mat).y, vec(mat).z, vec(mat).x };
				case 2: return Mat{ vec(mat).z, vec(mat).x, vec(mat).y };
			};
		}
		if constexpr (vt::dimension == 4)
		{
			switch (n % 4)
			{
				case 0: return mat;
				case 1: return Mat{ vec(mat).y, vec(mat).z, vec(mat).w, vec(mat).x };
				case 2: return Mat{ vec(mat).z, vec(mat).w, vec(mat).x, vec(mat).y };
				case 3: return Mat{ vec(mat).w, vec(mat).x, vec(mat).y, vec(mat).z };
			};
		}
	}

	// Returns a n-bit bitmask of the orthant that the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	template <VectorType Vec> requires (IsRank1<Vec>)
	constexpr uint32_t Orthant(Vec v) // Octant, Quadrant, etc depending on the dimension of 'Vec'
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;

		uint32_t mask = 0;
		if constexpr (vt::dimension > 0) mask |= (vec(v).x >= S(0)) << 0;
		if constexpr (vt::dimension > 1) mask |= (vec(v).y >= S(0)) << 1;
		if constexpr (vt::dimension > 2) mask |= (vec(v).z >= S(0)) << 2;
		if constexpr (vt::dimension > 0) mask |= (vec(v).w >= S(0)) << 3;
		return mask;
	}

	// Return the transpose of 'mat'
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall Transpose(Mat const& mat)
	{
		using vt = vector_traits<Mat>;

		Mat m = mat;
		if constexpr (vector_traits<Mat>::dimension >= 2)
		{
			std::swap(vec(vec(m).x).y, vec(vec(m).y).x);
		}
		if constexpr (vector_traits<Mat>::dimension >= 3)
		{
			std::swap(vec(vec(m).x).z, vec(vec(m).z).x);
			std::swap(vec(vec(m).y).z, vec(vec(m).z).y);
		}
		if constexpr (vector_traits<Mat>::dimension >= 4)
		{
			std::swap(vec(vec(m).x).w, vec(vec(m).w).x);
			std::swap(vec(vec(m).y).w, vec(vec(m).w).y);
			std::swap(vec(vec(m).z).w, vec(vec(m).w).z);
		}
		return m;
	}
	template <VectorType Mat> requires (IsRank2<Mat>&& vector_traits<Mat>::dimension >= 3)
	constexpr Mat pr_vectorcall Transpose3x3(Mat const& mat)
	{
		using vt = vector_traits<Mat>;

		Mat m = mat;
		std::swap(vec(vec(m).x).y, vec(vec(m).y).x);
		std::swap(vec(vec(m).x).z, vec(vec(m).z).x);
		std::swap(vec(vec(m).y).z, vec(vec(m).z).y);
		return m;
	}

	// Return the inverse of 'mat' (assuming an orthonormal matrix)
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat pr_vectorcall InvertOrthonormal(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		pr_assert(IsOrthonormal(mat) && "Matrix is not orthonormal");

		Mat m = mat;
		if constexpr (vector_traits<Mat>::dimension >= 2)
		{
			std::swap(vec(vec(m).x).y, vec(vec(m).y).x);
		}
		if constexpr (vector_traits<Mat>::dimension >= 3)
		{
			std::swap(vec(vec(m).x).z, vec(vec(m).z).x);
			std::swap(vec(vec(m).y).z, vec(vec(m).z).y);
		}
		if constexpr (vector_traits<Mat>::dimension >= 4)
		{
			vec(vec(m).w).x = -Dot(vec(mat).x, vec(mat).w);
			vec(vec(m).w).y = -Dot(vec(mat).y, vec(mat).w);
			vec(vec(m).w).z = -Dot(vec(mat).z, vec(mat).w);
		}
		return m;
	}

	// Invert the orthonormal matrix 'mat'
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension >= 3)
	constexpr Mat pr_vectorcall InvertAffine(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		pr_assert(IsAffine(mat) && "Matrix is not affine");

		Mat m = mat;

		auto scale = Vec{ LengthSq(vec(mat).x), LengthSq(vec(mat).y), LengthSq(vec(mat).z) };
		if (Abs(scale - One<Vec>()) > tiny<S>)
		{
			pr_assert(vec(scale).x != 0 && vec(scale).y != 0 && vec(scale).z != 0 && "Cannot invert a degenerate matrix");
			scale = CompSqrt(scale);
		}

		// Remove scale
		vec(m).x /= vec(scale).x;
		vec(m).y /= vec(scale).y;
		vec(m).z /= vec(scale).z;

		// Invert rotation
		m = Transpose3x3(m);

		// Invert scale
		vec(m).x /= vec(scale).x;
		vec(m).y /= vec(scale).y;
		vec(m).z /= vec(scale).z;

		// Invert translation
		if constexpr (vt::dimension == 4)
		{
			vec(m).w = Vec{
				-(vec(vec(mat).w).x * vec(vec(m).x).x + vec(vec(mat).w).y * vec(vec(m).y).x + vec(vec(mat).w).z * vec(vec(m).z).x),
				-(vec(vec(mat).w).x * vec(vec(m).x).y + vec(vec(mat).w).y * vec(vec(m).y).y + vec(vec(mat).w).z * vec(vec(m).z).y),
				-(vec(vec(mat).w).x * vec(vec(m).x).z + vec(vec(mat).w).y * vec(vec(m).y).z + vec(vec(mat).w).z * vec(vec(m).z).z),
				S(1)
			};
		}

		return m;
	}

	// Return the inverse of 'mat'
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall Invert(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		if constexpr (vt::dimension == 3)
		{
			Mat inv = {};
			vec(inv).x = Cross3(vec(mat).y, vec(mat).z);
			vec(inv).y = Cross3(vec(mat).z, vec(mat).x);
			vec(inv).z = Cross3(vec(mat).x, vec(mat).y);
			inv = Transpose(inv);
			
			auto det = Determinant(mat);
			pr_assert(det != S(0) && "Matrix has no inverse");
			return inv * (S(1) / det);
		}
		if constexpr (vt::dimension == 4)
		{
			S xx = vec(vec(mat).x).x, xy = vec(vec(mat).x).y, xz = vec(vec(mat).x).z, xw = vec(vec(mat).x).w;
			S yx = vec(vec(mat).y).x, yy = vec(vec(mat).y).y, yz = vec(vec(mat).y).z, yw = vec(vec(mat).y).w;
			S zx = vec(vec(mat).z).x, zy = vec(vec(mat).z).y, zz = vec(vec(mat).z).z, zw = vec(vec(mat).z).w;
			S wx = vec(vec(mat).w).x, wy = vec(vec(mat).w).y, wz = vec(vec(mat).w).z, ww = vec(vec(mat).w).w;

			Mat inv = {};
			vec(inv).x = Vec{
				+yy * zz * ww - yy * zw * wz - zy * yz * ww + zy * yw * wz + wy * yz * zw - wy * yw * zz,
				-xy * zz * ww + xy * zw * wz + zy * xz * ww - zy * xw * wz - wy * xz * zw + wy * xw * zz,
				+xy * yz * ww - xy * yw * wz - yy * xz * ww + yy * xw * wz + wy * xz * yw - wy * xw * yz,
				-xy * yz * zw + xy * yw * zz + yy * xz * zw - yy * xw * zz - zy * xz * yw + zy * xw * yz };
			vec(inv).y = Vec{
				-yx * zz * ww + yx * zw * wz + zx * yz * ww - zx * yw * wz - wx * yz * zw + wx * yw * zz,
				+xx * zz * ww - xx * zw * wz - zx * xz * ww + zx * xw * wz + wx * xz * zw - wx * xw * zz,
				-xx * yz * ww + xx * yw * wz + yx * xz * ww - yx * xw * wz - wx * xz * yw + wx * xw * yz,
				+xx * yz * zw - xx * yw * zz - yx * xz * zw + yx * xw * zz + zx * xz * yw - zx * xw * yz };
			vec(inv).z = Vec{
				+yx * zy * ww - yx * zw * wy - zx * yy * ww + zx * yw * wy + wx * yy * zw - wx * yw * zy,
				-xx * zy * ww + xx * zw * wy + zx * xy * ww - zx * xw * wy - wx * xy * zw + wx * xw * zy,
				+xx * yy * ww - xx * yw * wy - yx * xy * ww + yx * xw * wy + wx * xy * yw - wx * xw * yy,
				-xx * yy * zw + xx * yw * zy + yx * xy * zw - yx * xw * zy - zx * xy * yw + zx * xw * yy };
			vec(inv).w = Vec{
				-yx * zy * wz + yx * zz * wy + zx * yy * wz - zx * yz * wy - wx * yy * zz + wx * yz * zy,
				+xx * zy * wz - xx * zz * wy - zx * xy * wz + zx * xz * wy + wx * xy * zz - wx * xz * zy,
				-xx * yy * wz + xx * yz * wy + yx * xy * wz - yx * xz * wy - wx * xy * yz + wx * xz * yy,
				+xx * yy * zz - xx * yz * zy - yx * xy * zz + yx * xz * zy + zx * xy * yz - zx * xz * yy };

			auto det = xx * vec(vec(inv).x).x + xy * vec(vec(inv).y).x + xz * vec(vec(inv).z).x + xw * vec(vec(inv).w).x;
			pr_assert(det != S(0) && "matrix has no inverse");
			return inv * (S(1) / det);

			// Reference invert 4x4 implementation
			if constexpr (false)
			{
				auto mA = Transpose(mat); // Take the transpose so that row operations are faster
				auto mB = Identity<Mat>();

				// Loop through columns of 'A'
				for (int j = 0; j != 4; ++j)
				{
					// Select the pivot element: maximum magnitude in this row
					auto pivot = 0; auto val = S(0);
					if (j <= 0 && val < Abs(mA.x[j])) { pivot = 0; val = Abs(mA.x[j]); }
					if (j <= 1 && val < Abs(mA.y[j])) { pivot = 1; val = Abs(mA.y[j]); }
					if (j <= 2 && val < Abs(mA.z[j])) { pivot = 2; val = Abs(mA.z[j]); }
					if (j <= 3 && val < Abs(mA.w[j])) { pivot = 3; val = Abs(mA.w[j]); }
					if (val < tiny<S>)
					{
						pr_assert(false && "Matrix has no inverse");
						return mat;
					}

					// Interchange rows to put pivot element on the diagonal
					if (pivot != j) // skip if already on diagonal
					{
						std::swap(mA[j], mA[pivot]);
						std::swap(mB[j], mB[pivot]);
					}

					// Divide row by pivot element. Pivot element becomes 1
					auto scale = mA[j][j];
					mA[j] /= scale;
					mB[j] /= scale;

					// Subtract this row from others to make the rest of column j zero
					if (j != 0) { scale = mA.x[j]; mA.x -= scale * mA[j]; mB.x -= scale * mB[j]; }
					if (j != 1) { scale = mA.y[j]; mA.y -= scale * mA[j]; mB.y -= scale * mB[j]; }
					if (j != 2) { scale = mA.z[j]; mA.z -= scale * mA[j]; mB.z -= scale * mB[j]; }
					if (j != 3) { scale = mA.w[j]; mA.w -= scale * mA[j]; mB.w -= scale * mB[j]; }
				}

				// When these operations have been completed, A should have been transformed to the identity matrix
				// and B should have been transformed into the inverse of the original A
				mB = Transpose(mB);
				return mB;
			}
		}
	}

	// Return the inverse of 'mat' using double precision floats
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat pr_vectorcall InvertPrecise(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		auto xx = vec(vec(mat).x).x, xy = vec(vec(mat).x).y, xz = vec(vec(mat).x).z, xw = vec(vec(mat).x).w;
		auto yx = vec(vec(mat).y).x, yy = vec(vec(mat).y).y, yz = vec(vec(mat).y).z, yw = vec(vec(mat).y).w;
		auto zx = vec(vec(mat).z).x, zy = vec(vec(mat).z).y, zz = vec(vec(mat).z).z, zw = vec(vec(mat).z).w;
		auto wx = vec(vec(mat).w).x, wy = vec(vec(mat).w).y, wz = vec(vec(mat).w).z, ww = vec(vec(mat).w).w;

		double inv[4][4];
		inv[0][0] = 0.0 + yy * zz * ww - yy * zw * wz - zy * yz * ww + zy * yw * wz + wy * yz * zw - wy * yw * zz;
		inv[0][1] = 0.0 - xy * zz * ww + xy * zw * wz + zy * xz * ww - zy * xw * wz - wy * xz * zw + wy * xw * zz;
		inv[0][2] = 0.0 + xy * yz * ww - xy * yw * wz - yy * xz * ww + yy * xw * wz + wy * xz * yw - wy * xw * yz;
		inv[0][3] = 0.0 - xy * yz * zw + xy * yw * zz + yy * xz * zw - yy * xw * zz - zy * xz * yw + zy * xw * yz;
		inv[1][0] = 0.0 - yx * zz * ww + yx * zw * wz + zx * yz * ww - zx * yw * wz - wx * yz * zw + wx * yw * zz;
		inv[1][1] = 0.0 + xx * zz * ww - xx * zw * wz - zx * xz * ww + zx * xw * wz + wx * xz * zw - wx * xw * zz;
		inv[1][2] = 0.0 - xx * yz * ww + xx * yw * wz + yx * xz * ww - yx * xw * wz - wx * xz * yw + wx * xw * yz;
		inv[1][3] = 0.0 + xx * yz * zw - xx * yw * zz - yx * xz * zw + yx * xw * zz + zx * xz * yw - zx * xw * yz;
		inv[2][0] = 0.0 + yx * zy * ww - yx * zw * wy - zx * yy * ww + zx * yw * wy + wx * yy * zw - wx * yw * zy;
		inv[2][1] = 0.0 - xx * zy * ww + xx * zw * wy + zx * xy * ww - zx * xw * wy - wx * xy * zw + wx * xw * zy;
		inv[2][2] = 0.0 + xx * yy * ww - xx * yw * wy - yx * xy * ww + yx * xw * wy + wx * xy * yw - wx * xw * yy;
		inv[2][3] = 0.0 - xx * yy * zw + xx * yw * zy + yx * xy * zw - yx * xw * zy - zx * xy * yw + zx * xw * yy;
		inv[3][0] = 0.0 - yx * zy * wz + yx * zz * wy + zx * yy * wz - zx * yz * wy - wx * yy * zz + wx * yz * zy;
		inv[3][1] = 0.0 + xx * zy * wz - xx * zz * wy - zx * xy * wz + zx * xz * wy + wx * xy * zz - wx * xz * zy;
		inv[3][2] = 0.0 - xx * yy * wz + xx * yz * wy + yx * xy * wz - yx * xz * wy - wx * xy * yz + wx * xz * yy;
		inv[3][3] = 0.0 + xx * yy * zz - xx * yz * zy - yx * xy * zz + yx * xz * zy + zx * xy * yz - zx * xz * yy;

		auto det = xx * inv[0][0] + xy * inv[1][0] + xz * inv[2][0] + xw * inv[3][0];
		pr_assert(det != 0.0 && "matrix has no inverse");

		Mat m;
		vec(m).x = Vec(S(inv[0][0]), S(inv[0][1]), S(inv[0][2]), S(inv[0][3]));
		vec(m).y = Vec(S(inv[1][0]), S(inv[1][1]), S(inv[1][2]), S(inv[1][3]));
		vec(m).z = Vec(S(inv[2][0]), S(inv[2][1]), S(inv[2][2]), S(inv[2][3]));
		vec(m).w = Vec(S(inv[3][0]), S(inv[3][1]), S(inv[3][2]), S(inv[3][3]));
		m /= det;
		return m;
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	template <VectorTypeFP Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall Sqrt(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		// Using 'Denman-Beavers' square root iteration. Should converge quadratically
		auto a = mat;             // Converges to mat^0.5
		auto b = Identity<Mat>(); // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto a_next = S(0.5) * (a + Invert(b));
			auto b_next = S(0.5) * (b + Invert(a));
			a = a_next;
			b = b_next;
		}
		return a;
	}

	// Orthonormalise the rotation component of 'mat'
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension >= 3)
	inline Mat pr_vectorcall Orthonorm(Mat const& mat)
	{
		auto m = mat;
		vec(m).x = Normalise(vec(m).x);
		vec(m).y = Normalise(Cross3(vec(m).z, vec(m).x));
		vec(m).z = Cross3(vec(m).x, vec(m).y);
		pr_assert(IsOrthonormal(m));
		return m;
	}

	// Matrix Multiply
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr typename vector_traits<Mat>::component_t pr_vectorcall operator * (Mat const& a2b, typename vector_traits<Mat>::component_t v) noexcept
	{
		using vt = vector_traits<Mat>;
		using Vec = typename vt::component_t;

		Vec res = {};
		auto a2bT = Transpose(a2b);
		if constexpr (vt::dimension > 0) vec(res).x = Dot(vec(a2bT).x, v);
		if constexpr (vt::dimension > 1) vec(res).y = Dot(vec(a2bT).y, v);
		if constexpr (vt::dimension > 2) vec(res).z = Dot(vec(a2bT).z, v);
		if constexpr (vt::dimension > 3) vec(res).w = Dot(vec(a2bT).w, v);
		return res;
	}
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall operator * (Mat const& b2c, Mat const& a2b) noexcept
	{
		// Note:
		//  - The reason for this order is because matrices are applied from right to left
		//    e.g.
		//       auto Va =             V = vector in space 'a'
		//       auto Vb =       a2b * V = vector in space 'b'
		//       auto Vc = b2c * a2b * V = vector in space 'c'
		//  - The shape of the result is:
		//       [a2c] = [b2c] * [a2b]
		//       [1x3]   [2x3]   [1x2]
		using vt = vector_traits<Mat>;
		using Vec = typename vt::component_t;

		Mat ans = {};
		auto b2cT = Transpose(b2c);
		if constexpr (vt::dimension == 2)
		{
			vec(ans).x = Vec{ Dot(vec(b2cT).x, vec(a2b).x), Dot(vec(b2cT).y, vec(a2b).x) };
			vec(ans).y = Vec{ Dot(vec(b2cT).x, vec(a2b).y), Dot(vec(b2cT).y, vec(a2b).y) };
		}
		if constexpr (vt::dimension == 3)
		{
			vec(ans).x = Vec{ Dot(vec(b2cT).x, vec(a2b).x), Dot(vec(b2cT).y, vec(a2b).x), Dot(vec(b2cT).z, vec(a2b).x) };
			vec(ans).y = Vec{ Dot(vec(b2cT).x, vec(a2b).y), Dot(vec(b2cT).y, vec(a2b).y), Dot(vec(b2cT).z, vec(a2b).y) };
			vec(ans).z = Vec{ Dot(vec(b2cT).x, vec(a2b).z), Dot(vec(b2cT).y, vec(a2b).z), Dot(vec(b2cT).z, vec(a2b).z) };
		}
		if constexpr (vt::dimension == 4)
		{
			vec(ans).x = Vec(Dot(vec(b2cT).x, vec(a2b).x), Dot(vec(b2cT).y, vec(a2b).x), Dot(vec(b2cT).z, vec(a2b).x), Dot(vec(b2cT).w, vec(a2b).x));
			vec(ans).y = Vec(Dot(vec(b2cT).x, vec(a2b).y), Dot(vec(b2cT).y, vec(a2b).y), Dot(vec(b2cT).z, vec(a2b).y), Dot(vec(b2cT).w, vec(a2b).y));
			vec(ans).z = Vec(Dot(vec(b2cT).x, vec(a2b).z), Dot(vec(b2cT).y, vec(a2b).z), Dot(vec(b2cT).z, vec(a2b).z), Dot(vec(b2cT).w, vec(a2b).z));
			vec(ans).w = Vec(Dot(vec(b2cT).x, vec(a2b).w), Dot(vec(b2cT).y, vec(a2b).w), Dot(vec(b2cT).z, vec(a2b).w), Dot(vec(b2cT).w, vec(a2b).w));
		}
		return ans;
	}

	// Matrix creation ----- 

	// Create a translation matrix
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat Translation(typename vector_traits<Mat>::component_t xyz)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		pr_assert((xyz.w == S(0) || xyz.w == S(1)) && "translation should be an affine vector");
		Mat m = Identity<Mat>();
		vec(m).w = xyz;         // 'xyz' can be a position or an offset
		vec(vec(m).w).w = S(1); // Ensure affine
		return m;
	}
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat Translation(typename vector_traits<Mat>::element_t x, typename vector_traits<Mat>::element_t y, typename vector_traits<Mat>::element_t z)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		return Translation(Vec{ x, y,z, S(1) });
	}

	// Create a 2D rotation matrix
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 2)
	inline Mat pr_vectorcall Rotation(typename vector_traits<Mat>::element_t angle)
	{
		using vt = vector_traits<Mat>;
		using Vec = typename vt::component_t;
		return Mat{
			Vec{+std::cos(angle), +std::sin(angle)},
			Vec{-std::sin(angle), +std::cos(angle)},
		};
	}

	// Create a 3D rotation matrix. Order is: roll, pitch, yaw (to match DirectX)
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall RotationRad(typename vector_traits<Mat>::element_t pitch, typename vector_traits<Mat>::element_t yaw, typename vector_traits<Mat>::element_t roll)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		
		S cos_p = std::cos(pitch), sin_p = std::sin(pitch);
		S cos_y = std::cos(yaw  ), sin_y = std::sin(yaw  );
		S cos_r = std::cos(roll ), sin_r = std::sin(roll );
		return Mat{
			Vec{+cos_y * cos_r + sin_y * sin_p * sin_r , cos_p * sin_r , -sin_y * cos_r + cos_y * sin_p * sin_r},
			Vec{-cos_y * sin_r + sin_y * sin_p * cos_r , cos_p * cos_r , +sin_y * sin_r + cos_y * sin_p * cos_r},
			Vec{+sin_y * cos_p                         ,        -sin_p ,                          cos_y * cos_p}
		};
	}
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall RotationDeg(typename vector_traits<Mat>::element_t pitch, typename vector_traits<Mat>::element_t yaw, typename vector_traits<Mat>::element_t roll)
	{
		return RotationRad<Mat>(
			DegreesToRadians(pitch),
			DegreesToRadians(yaw),
			DegreesToRadians(roll)
		);
	}

	// Create a 3D rotation matrix from an axis and angle.
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	constexpr Mat pr_vectorcall Rotation(typename vector_traits<Mat>::component_t axis_norm, typename vector_traits<Mat>::component_t axis_sine_angle, typename vector_traits<Mat>::element_t cos_angle)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		pr_assert(IsNormal(axis_norm) && "'axis_norm' should be normalised");

		Mat m = {};
		auto trace_vec = axis_norm * (S(1) - cos_angle);

		vec(vec(m).x).x = vec(trace_vec).x * vec(axis_norm).x + cos_angle;
		vec(vec(m).y).y = vec(trace_vec).y * vec(axis_norm).y + cos_angle;
		vec(vec(m).z).z = vec(trace_vec).z * vec(axis_norm).z + cos_angle;

		vec(trace_vec).x *= vec(axis_norm).y;
		vec(trace_vec).z *= vec(axis_norm).x;
		vec(trace_vec).y *= vec(axis_norm).z;

		vec(vec(m).x).y = vec(trace_vec).x + vec(axis_sine_angle).z;
		vec(vec(m).x).z = vec(trace_vec).z - vec(axis_sine_angle).y;
		vec(vec(m).y).x = vec(trace_vec).x - vec(axis_sine_angle).z;
		vec(vec(m).y).z = vec(trace_vec).y + vec(axis_sine_angle).x;
		vec(vec(m).z).x = vec(trace_vec).z + vec(axis_sine_angle).y;
		vec(vec(m).z).y = vec(trace_vec).y - vec(axis_sine_angle).x;

		return m;
	}

	// Create a 3D rotation from an axis and angle. 'axis' should be normalised
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall Rotation(typename vector_traits<Mat>::component_t axis_norm, typename vector_traits<Mat>::element_t angle)
	{
		return Rotation<Mat>(axis_norm, axis_norm * std::sin(angle), std::cos(angle));
	}

	// Create a 3D rotation from an angular displacement vector. length = angle(rad), direction = axis
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall Rotation(typename vector_traits<Mat>::component_t angular_displacement) // This is ExpMap3x3.
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		pr_assert(angular_displacement.w == S(0) && "'angular_displacement' should be a scaled direction vector");

		// Rodrigues' formula:  exp(omega) = I + (sin(theta)/theta) * omega + ((1 - cos(theta)/theta²) * omega²
		auto len = Length(angular_displacement);
		return len > constants<S>::tiny
			? Rotation<Mat>(angular_displacement / len, len)
			: Identity<Mat>();
	}

	// Create a 3D rotation representing the rotation from one vector to another. (Vectors do not need to be normalised)
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall Rotation(typename vector_traits<Mat>::component_t from, typename vector_traits<Mat>::component_t to)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		pr_assert(!FEql(from, Vec{}));
		pr_assert(!FEql(to  , Vec{}));

		auto len = Length(from) * Length(to);
		auto cos_angle = Dot3(from, to) / len;
		if (cos_angle >= 1.0f - tiny<S>) return Identity<Mat>();
		if (cos_angle <= tiny<S> - S(1)) return Rotation<Mat>(Normalise(Perpendicular(from - to)), constants<S>::tau_by_2);

		auto axis_size_angle = Cross3(from, to) / len;
		auto axis_norm = Normalise(axis_size_angle);
		return Rotation<Mat>(axis_norm, axis_size_angle, cos_angle);
	}

	// Create a 3D rotation transform from one basis axis to another. Remember AxisId can be cast to Vec4
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall Rotation(AxisId from_axis, AxisId to_axis)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		// 'o2f' = the rotation from Z to 'from_axis'
		// 'o2t' = the rotation from Z to 'to_axis'
		// 'f2t' = o2t * Invert(o2f)
		Mat o2f = {}, o2t = {};
		switch (from_axis)
		{
			case -1: o2f = Rotation<Mat>(S(0), +constants<S>::tau_by_4, S(0)); break;
			case +1: o2f = Rotation<Mat>(S(0), -constants<S>::tau_by_4, S(0)); break;
			case -2: o2f = Rotation<Mat>(+constants<S>::tau_by_4, S(0), S(0)); break;
			case +2: o2f = Rotation<Mat>(-constants<S>::tau_by_4, S(0), S(0)); break;
			case -3: o2f = Rotation<Mat>(S(0), +constants<S>::tau_by_2, S(0)); break;
			case +3: o2f = Identity<Mat>(); break;
			default: pr_assert(false && "axis_id must one of +/-1, +/-2, +/-3"); o2f = Identity<Mat>(); break;
		}
		switch (to_axis)
		{
			case -1: o2t = Rotation<Mat>(S(0), -constants<S>::tau_by_4, S(0)); break; // I know this sign looks wrong, but it isn't. Must be something to do with signs passed to cos()/sin()
			case +1: o2t = Rotation<Mat>(S(0), +constants<S>::tau_by_4, S(0)); break;
			case -2: o2t = Rotation<Mat>(+constants<S>::tau_by_4, S(0), S(0)); break;
			case +2: o2t = Rotation<Mat>(-constants<S>::tau_by_4, S(0), S(0)); break;
			case -3: o2t = Rotation<Mat>(S(0), +constants<S>::tau_by_2, S(0)); break;
			case +3: o2t = Identity<Mat>(); break;
			default: pr_assert(false && "axis_id must one of +/-1, +/-2, +/-3"); o2t = Identity<Mat>(); break;
		}
		return o2t * InvertAffine(o2f);
	}

	// Create a scale matrix
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall Scale(typename vector_traits<Mat>::element_t scale)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		Mat mat = {};
		if constexpr (vt::dimension == 1) vec(vec(mat).x).x = scale;
		if constexpr (vt::dimension == 2) vec(vec(mat).y).y = scale;
		if constexpr (vt::dimension == 3) vec(vec(mat).z).z = scale;
		if constexpr (vt::dimension == 4) vec(vec(mat).w).w = scale;
		return mat;
	}
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall Scale(typename vector_traits<Mat>::component_t scale)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		Mat mat = {};
		if constexpr (vt::dimension == 1) vec(vec(mat).x).x = vec(scale).x;
		if constexpr (vt::dimension == 2) vec(vec(mat).y).y = vec(scale).y;
		if constexpr (vt::dimension == 3) vec(vec(mat).z).z = vec(scale).z;
		if constexpr (vt::dimension == 4) vec(vec(mat).w).w = vec(scale).w;
		return mat;
	}

	// Create a 2D shear matrix
	template <VectorType Mat, ScalarType S = typename vector_traits<Mat>::element_t> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 2)
	constexpr Mat Shear(S sxy, S syx)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		Mat mat = {};
		vec(mat).x = Vec{ S(1), sxy };
		vec(mat).y = Vec{ syx, S(1) };
		return mat;
	}

	// Create a 3D shear matrix
	template <VectorType Mat, ScalarType S = typename vector_traits<Mat>::element_t> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	constexpr Mat Shear(S sxy, S sxz, S syx, S syz, S szx, S szy)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		Mat mat = {};
		vec(mat).x = Vec{ S(1), sxy, sxz };
		vec(mat).y = Vec{ syx, S(1), syz };
		vec(mat).z = Vec{ szx, szy, S(1) };
		return mat;
	}

	// Create a rotation matrix from Euler angles.  Order is: roll, pitch, yaw (to match DirectX)
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	inline Mat TransformRad(typename vector_traits<Mat>::element_t pitch, typename vector_traits<Mat>::element_t yaw, typename vector_traits<Mat>::element_t roll, typename vector_traits<Mat>::component_t pos)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		Mat m = RotationRad<Mat>(pitch, yaw, roll);
		vec(m).w = pos;
		return m;
	}
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	inline Mat TransformDeg(typename vector_traits<Mat>::element_t pitch, typename vector_traits<Mat>::element_t yaw, typename vector_traits<Mat>::element_t roll, typename vector_traits<Mat>::component_t pos)
	{
		return TransformRad<Mat>(
			DegreesToRadians(pitch),
			DegreesToRadians(yaw),
			DegreesToRadians(roll),
			pos
		);
	}

	// Create a Look-At transform
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	inline Mat LookAt(typename vector_traits<Mat>::component_t eye, typename vector_traits<Mat>::component_t at, typename vector_traits<Mat>::component_t up)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		pr_assert(vec(eye).w == S(1) && vec(at).w == S(1) && vec(up).w == S(0) && "Invalid position/direction vectors passed to LookAt");
		pr_assert(eye - at != Zero<Vec>() && "LookAt 'eye' and 'at' positions are coincident");
		pr_assert(!Parallel(eye - at, up, S(0)) && "LookAt 'forward' and 'up' axes are aligned");

		Mat mat = {};
		vec(mat).z = Normalise(eye - at);
		vec(mat).x = Normalise(Cross3(up, vec(mat).z));
		vec(mat).y = Cross3(vec(mat).z, vec(mat).x);
		vec(mat).w = eye;
		return mat;
	}

	// Construct an orthographic projection matrix
	template <VectorType Mat, ScalarType S = typename vector_traits<Mat>::element_t> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat ProjectionOrthographic(S w, S h, S zn, S zf, bool righthanded)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		pr_assert(IsFinite(w) && IsFinite(h) && w > S(0) && h > S(0) && "invalid view rect");
		pr_assert(IsFinite(zn) && IsFinite(zf) && (zn - zf) != 0 && "invalid near/far planes");

		auto rh = Bool2SignF(righthanded);

		Mat mat = {};
		vec(vec(mat).x).x = S(2) / w;
		vec(vec(mat).y).y = S(2) / h;
		vec(vec(mat).z).z = rh / (zn - zf);
		vec(vec(mat).w).w = S(1);
		vec(vec(mat).w).z = rh * zn / (zn - zf);
		return mat;
	}

	// Construct a perspective projection matrix. 'w' and 'h' are measured at 'zn'
	template <VectorType Mat, ScalarType S = typename vector_traits<Mat>::element_t> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat ProjectionPerspective(S w, S h, S zn, S zf, bool righthanded)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		pr_assert(IsFinite(w) && IsFinite(h) && w > S(0) && h > S(0) && "invalid view rect");
		pr_assert(IsFinite(zn) && IsFinite(zf) && zn > S(0) && zf > S(0) && (zn - zf) != S(0) && "invalid near/far planes");

		// Getting your head around perspective transforms:
		//   p0 = c2s * Vec4(0,0,-zn,1); p0/p0.w = (0,0,0,1)
		//   p1 = c2s * Vec4(0,0,-zf,1); p1/p1.w = (0,0,1,1)

		auto rh = Bool2SignF(righthanded);

		Mat mat = {};
		vec(vec(mat).x).x = S(2) * zn / w;
		vec(vec(mat).y).y = S(2) * zn / h;
		vec(vec(mat).z).w = -rh;
		vec(vec(mat).z).z = rh * zf / (zn - zf);
		vec(vec(mat).w).z = zn * zf / (zn - zf);
		return mat;
	}

	// Construct a perspective projection matrix offset from the centre
	template <VectorType Mat, ScalarType S = typename vector_traits<Mat>::element_t> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	constexpr Mat ProjectionPerspective(S l, S r, S t, S b, S zn, S zf, bool righthanded)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		pr_assert(IsFinite(l)  && IsFinite(r) && IsFinite(t) && IsFinite(b) && (r - l) > S(0) && (t - b) > S(0) && "invalid view rect");
		pr_assert(IsFinite(zn) && IsFinite(zf) && zn > S(0) && zf > S(0) && (zn - zf) != S(0) && "invalid near/far planes");
		
		auto rh = Bool2SignF(righthanded);
		
		Mat mat = {};
		vec(vec(mat).x).x = S(2) * zn / (r - l);
		vec(vec(mat).y).y = S(2) * zn / (t - b);
		vec(vec(mat).z).x = rh * (r + l) / (r - l);
		vec(vec(mat).z).y = rh * (t + b) / (t - b);
		vec(vec(mat).z).w = -rh;
		vec(vec(mat).z).z = rh * zf / (zn - zf);
		vec(vec(mat).w).z = zn * zf / (zn - zf);
		return mat;
	}

	// Construct a perspective projection matrix using field of view
	template <VectorType Mat, ScalarType S = typename vector_traits<Mat>::element_t> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 4)
	inline Mat ProjectionPerspectiveFOV(S fovY, S aspect, S zn, S zf, bool righthanded)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		pr_assert(IsFinite(aspect) && aspect > S(0) && "invalid aspect ratio");
		pr_assert(IsFinite(zn) && IsFinite(zf) && zn > S(0) && zf > S(0) && (zn - zf) != S(0) && "invalid near/far planes");

		auto rh = Bool2SignF(righthanded);

		Mat mat = {};
		vec(vec(mat).y).y = S(1) / std::tan(fovY / S(2));
		vec(vec(mat).x).x = vec(vec(mat).y).y / aspect;
		vec(vec(mat).z).w = -rh;
		vec(vec(mat).z).z = rh * zf / (zn - zf);
		vec(vec(mat).w).z = zn * zf / (zn - zf);
		return mat;
	}

	// General operations -----

	// Diagonalise a 3x3 matrix. From numerical recipes
	template <VectorType Mat> requires (IsRank2<Mat> && ArrayAccess<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall Diagonalise(Mat const& mat_, Mat& eigen_vectors, typename vector_traits<Mat>::component_t& eigen_values)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		struct L
		{
			static void Rotate(Mat& mat, int i, int j, int k, int l, S s, S tau)
			{
				auto temp = mat[j][i];
				auto h = mat[l][k];
				mat[j][i] = temp - s * (h + temp * tau);
				mat[l][k] = h + s * (temp - h * tau);
			}
		};

		// Initialise the Eigen values and b to be the diagonal elements of 'mat'
		eigen_values = Diagonal(mat_);
		eigen_vectors = Identity<Mat>();
		Vec b = eigen_values;

		auto mat = mat_;
		S sum = {};
		S const diagonal_eps = S(1.0e-4);
		do
		{
			auto z = Vec{};

			// Sweep through all elements above the diagonal
			for (int i = 0; i != 3; ++i) //ip
			{
				for (int j = i + 1; j != 3; ++j) //iq
				{
					if (Abs(mat[j][i]) > diagonal_eps/S(3))
					{
						auto h     = eigen_values[j] - eigen_values[i];
						auto theta = S(0.5) * h / mat[j][i];
						auto t     = Sign(theta) / (Abs(theta) + Sqrt(S(1) + Sqr(theta)));
						auto c     = S(1) / Sqrt(S(1) + Sqr(t));
						auto s     = t * c;
						auto tau   = s / (S(1) + c);
						h          = t * mat[j][i];

						z[i] -= h;
						z[j] += h;
						eigen_values[i] -= h;
						eigen_values[j] += h;
						mat[j][i] = S(0);

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
			sum  = Abs(vec(vec(mat).y).x);
			sum += Abs(vec(vec(mat).z).x);
			sum += Abs(vec(vec(mat).z).y);
		}
		while (sum > diagonal_eps);
		return mat;
	}

	// Return the axis and angle of a rotation matrix
	template <VectorType Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline std::tuple<typename vector_traits<Mat>::component_t, typename vector_traits<Mat>::element_t> pr_vectorcall AxisAngle(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		pr_assert(IsOrthonormal(mat) && "Matrix is not a pure rotation matrix");

		auto angle = std::acos(S(0.5) * (Trace(mat) - S(1)));
		auto axis = S(1000) * Kernel(Identity<Mat>() - mat);
		if (axis == Vec{})
			return { Vec{1, 0, 0, 0}, S(0) };
		
		axis = Normalise(axis);

		// Determine the correct sign of the angle
		auto vec = CreateNotParallelTo(axis);
		auto X = vec - Dot(axis, vec) * axis;
		auto Xprim = mat * X;
		auto XcXp = Cross3(X, Xprim);
		if (Dot3(XcXp, axis) < S(0))
			angle = -angle;

		return { axis, angle };
	}

	// Create a scale matrix from 'mat'. Use 'Diagonal' to get the scale components as a vector
	template <VectorType Mat> requires (IsRank2<Mat>)
	constexpr Mat pr_vectorcall ScaleFrom(Mat const& mat)
	{
		using vt = vector_traits<Mat>;

		Mat res = {};
		if constexpr (vt::dimension == 1) vec(vec(res).x).x = Length(vec(mat).x);
		if constexpr (vt::dimension == 2) vec(vec(res).y).y = Length(vec(mat).y);
		if constexpr (vt::dimension == 3) vec(vec(res).z).z = Length(vec(mat).z);
		if constexpr (vt::dimension == 4) vec(vec(res).w).w = Length(vec(mat).w);
		return res;
	}

	// Return a copy of 'mat' with the component vectors normalised
	template <VectorTypeFP Mat> requires (IsRank2<Mat>)
	inline Mat pr_vectorcall Unscaled(Mat const& mat)
	{
		using vt = vector_traits<Mat>;

		Mat res = {};
		if constexpr (vt::dimension == 1) vec(res).x = Normalise(vec(mat).x);
		if constexpr (vt::dimension == 2) vec(res).y = Normalise(vec(mat).y);
		if constexpr (vt::dimension == 3) vec(res).z = Normalise(vec(mat).z);
		if constexpr (vt::dimension == 4) vec(res).w = Normalise(vec(mat).w);
		return res;
	}

	// Construct a rotation matrix that transforms 'from' onto the z axis
	// Other points can then be projected onto the XY plane by rotating by this
	// matrix and then setting the z value to zero
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat RotationToZAxis(typename vector_traits<Mat>::component_t from)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		auto x = vec(from).x;
		auto y = vec(from).y;
		auto z = vec(from).z;
		auto r = Sqr(x) + Sqr(y);
		auto d = Sqrt(r);

		if (Abs(d) < tiny<S>)
		{
			auto mat = Identity<Mat>();
			vec(vec(mat).x).x = z; // Create an identity transform or a 180 degree rotation
			vec(vec(mat).z).z = z; // about Y depending on the sign of 'from.z'
			return mat;
		}
		else
		{
			return Mat{
				Vec{x * z / d, -y / d, x},
				Vec{y * z / d, +x / d, y},
				Vec{   -r / d,   S(0), z}
			};
		}
	}

	// Make an orientation matrix from a direction vector
	// 'dir' is the direction to align the axis 'axis_id' to. (Doesn't need to be normalised)
	// 'up' is the preferred up direction, however if up is parallel to 'dir'
	// then a vector perpendicular to 'dir' will be chosen.
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall OriFromDir(typename vector_traits<Mat>::component_t dir, AxisId axis_id, typename vector_traits<Mat>::component_t up_ = {})
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		pr_assert(dir != Zero<Vec>() && "'dir' cannot be a zero vector");

		// Get the preferred up direction (handling parallel cases)
		auto up = Perpendicular(dir, up_);

		Mat ori = {};
		vec(ori).z = Normalise(Sign(S(axis_id)) * dir);
		vec(ori).x = Normalise(Cross3(up, vec(ori).z));
		vec(ori).y = Cross3(vec(ori).z, vec(ori).x);

		// Permute the column vectors so +Z becomes 'axis'
		return Permute(ori, -Abs(axis_id)); // permute rolls: xyz, yzx, zxy // @Copilot, please check this logic.
	}

	// Make a scaled orientation matrix from a direction vector
	// Returns a transform for scaling and rotating the 'axis'th axis to 'dir'
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall ScaledOriFromDir(typename vector_traits<Mat>::component_t dir, AxisId axis, typename vector_traits<Mat>::component_t up = {})
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		auto len = Length(dir);
		return len > tiny<S>
			? OriFromDir(dir, axis, up) * Scale<Mat>(len)
			: Zero<Mat>();
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	constexpr typename vector_traits<Mat>::component_t pr_vectorcall RotationVectorApprox(Mat const& from, Mat const& to)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;
		pr_assert(IsOrthonormal(from) && IsOrthonormal(to) && "This only works for orthonormal matrices");
		
		auto cpm_x_i2wR = to - from;
		auto w2iR = Transpose(from);
		auto cpm = cpm_x_i2wR * w2iR;
		return Vec{
			vec(vec(cpm).y).z,
			vec(vec(cpm).z).x,
			vec(vec(cpm).x).y
		};
	}
	
	// Create a cross product matrix for 'vec'.
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	constexpr Mat pr_vectorcall CPM(typename vector_traits<Mat>::component_t v)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		// This matrix can be used to calculate the cross product with
		// another vector: e.g. Cross3(v1, v2) == CPM(v1) * v2
		return Mat{
			Vec{     S(0),  vec(v).z, -vec(v).y},
			Vec{-vec(v).z,      S(0),  vec(v).x},
			Vec{ vec(v).y, -vec(v).x,      S(0)}
		};
	}

	// Return 'exp(omega)' (Rodriges' formula)
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall ExpMap3x3(typename vector_traits<Mat>::component_t omega)
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
		return Rotation<Mat>(omega);
	}

	// Returns the Axis*Angle vector representation of a rotation matrix (Inverse of ExpMap)
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline typename vector_traits<Mat>::component_t pr_vectorcall LogMap3x3(Mat const& rot)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		auto cos_angle = Clamp<S>((Trace(rot) - S(1)) / S(2), -S(1), +S(1));
		auto theta = std::acos(cos_angle);
		if (Abs(theta) < tiny<S>)
			return Zero<Vec>();

		auto s = S(1) / (S(2) * std::sin(theta));
		auto axis = s * Vec{
			vec(vec(rot).y).z - vec(vec(rot).z).y,
			vec(vec(rot).z).x - vec(vec(rot).x).z,
			vec(vec(rot).x).y - vec(vec(rot).y).x
		};
		return theta * axis;
	}

	// Evaluates 'ori' after 'time' for a constant angular velocity and angular acceleration
	template <VectorTypeFP Mat> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 3)
	inline Mat pr_vectorcall RotationAt(float time, Mat const& ori, typename vector_traits<Mat>::component_t avel, typename vector_traits<Mat>::component_t aacc)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		using Vec = typename vt::component_t;

		// Orientation can be computed analytically if angular velocity
		// and angular acceleration are parallel or angular acceleration is zero.
		if (LengthSq(Cross(avel, aacc)) < tiny<S>)
		{
			auto w = avel + aacc * time;
			return ExpMap3x3(w * time) * ori;
		}
		else
		{
			// Otherwise, use the SPIRAL(6) algorithm. 6th order accurate for moderate 'time_s'

			// 3-point Gauss-Legendre nodes for 6th order accuracy
			constexpr S root15 = S(3.87298334620741688518);
			constexpr S c1 = S(0.5) - root15 / S(10);
			constexpr S c2 = S(0.5);
			constexpr S c3 = S(0.5) + root15 / S(10);

			// Evaluate instantaneous angular velocity at nodes
			auto w0 = avel + aacc * c1 * time;
			auto w1 = avel + aacc * c2 * time;
			auto w2 = avel + aacc * c3 * time;

			auto u0 = ExpMap3x3(w0 * time / S(3));
			auto u1 = ExpMap3x3(w1 * time / S(3));
			auto u2 = ExpMap3x3(w2 * time / S(3));

			return u2 * u1 * u0 * ori;
		}
	}

	// Random -----

	// Create a random vector with unit length
	template <VectorTypeFP Vec, typename Rng = std::default_random_engine>
	inline Vec pr_vectorcall RandomN(Rng& rng)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		using C = typename vt::component_t;

		if constexpr (IsRank1<Vec>)
		{
			std::uniform_real_distribution<S> dist(S(-1), S(1));
			for (;;)
			{
				Vec res = {};
				if constexpr (vt::dimension > 0) vec(res).x = dist(rng);
				if constexpr (vt::dimension > 1) vec(res).y = dist(rng);
				if constexpr (vt::dimension > 2) vec(res).z = dist(rng);
				if constexpr (vt::dimension > 3) vec(res).w = dist(rng);
				if (auto len = LengthSq(res); len > S(0.01) && len <= S(1))
					return res / Sqrt(len);
			}
		}
		else
		{
			Vec res = {};
			if constexpr (vt::dimension > 0) vec(res).x = RandomN<C>(rng);
			if constexpr (vt::dimension > 1) vec(res).y = RandomN<C>(rng);
			if constexpr (vt::dimension > 2) vec(res).z = RandomN<C>(rng);
			if constexpr (vt::dimension > 3) vec(res).w = RandomN<C>(rng);
			return Normalise(res);
		}
	}

	// Create a random vector with components on interval [vmin, vmax]
	template <VectorType Vec, typename Rng = std::default_random_engine>
	constexpr Vec pr_vectorcall Random(Rng& rng, Vec vmin, Vec vmax)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		using C = typename vt::component_t;

		if constexpr (IsRank1<Vec>)
		{
			std::uniform_real_distribution<S> dist_x(vec(vmin).x, vec(vmax).x);
			std::uniform_real_distribution<S> dist_y(vec(vmin).y, vec(vmax).y);
			std::uniform_real_distribution<S> dist_z(vec(vmin).z, vec(vmax).z);
			std::uniform_real_distribution<S> dist_w(vec(vmin).w, vec(vmax).w);

			Vec res = {};
			if constexpr (vt::dimension > 0) vec(res).x = dist_x(rng);
			if constexpr (vt::dimension > 1) vec(res).y = dist_y(rng);
			if constexpr (vt::dimension > 2) vec(res).z = dist_z(rng);
			if constexpr (vt::dimension > 3) vec(res).w = dist_w(rng);
			return res;
		}
		else
		{
			Vec res = {};
			if constexpr (vt::dimension > 0) vec(res).x = Random<C>(rng, vec(vmin).x, vec(vmax).x);
			if constexpr (vt::dimension > 1) vec(res).y = Random<C>(rng, vec(vmin).y, vec(vmax).y);
			if constexpr (vt::dimension > 2) vec(res).z = Random<C>(rng, vec(vmin).z, vec(vmax).z);
			if constexpr (vt::dimension > 3) vec(res).w = Random<C>(rng, vec(vmin).w, vec(vmax).w);
			return res;
		}
	}

	// Create a random vector with length on interval [min_length, max_length]
	template <VectorType Vec, typename Rng = std::default_random_engine>
	constexpr Vec pr_vectorcall Random(Rng& rng, typename vector_traits<Vec>::component_t min_length, typename vector_traits<Vec>::component_t max_length)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		using C = typename vt::component_t;

		if constexpr (IsRank1<Vec>)
		{
			std::uniform_real_distribution<S> dist(min_length, max_length);
			return dist(rng) * RandomN<Vec>(rng);
		}
		else
		{
			Vec res = {};
			if constexpr (vt::dimension > 0) vec(res).x = Random<C>(rng, vec(min_length).x, vec(max_length).x);
			if constexpr (vt::dimension > 1) vec(res).y = Random<C>(rng, vec(min_length).y, vec(max_length).y);
			if constexpr (vt::dimension > 2) vec(res).z = Random<C>(rng, vec(min_length).z, vec(max_length).z);
			if constexpr (vt::dimension > 3) vec(res).w = Random<C>(rng, vec(min_length).w, vec(max_length).w);
			return res;
		}
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <VectorType Vec, typename Rng = std::default_random_engine>
	constexpr Vec pr_vectorcall Random(Rng& rng, Vec centre, typename vector_traits<Vec>::component_t radius)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
		using C = typename vt::component_t;

		if constexpr (IsRank1<Vec>)
		{
			return Random<Vec>(rng, S(0), radius) + centre;
		}
		else
		{
			Vec res = {};
			if constexpr (vt::dimension > 0) vec(res).x = Random<C>(rng, vec(centre).x, vec(radius).x);
			if constexpr (vt::dimension > 1) vec(res).y = Random<C>(rng, vec(centre).y, vec(radius).y);
			if constexpr (vt::dimension > 2) vec(res).z = Random<C>(rng, vec(centre).z, vec(radius).z);
			if constexpr (vt::dimension > 3) vec(res).w = Random<C>(rng, vec(centre).w, vec(radius).w);
			return res;
		}
	}

	// Create a 3D matrix containing random rotation about 'axis' and between angles [min_angle, max_angle)
	template <VectorTypeFP Mat, typename Rng = std::default_random_engine> requires (IsRank2<Mat> && vector_traits<Mat>::dimension >= 3)
	inline Mat pr_vectorcall Random(Rng& rng, typename vector_traits<Mat>::component_t axis_norm, typename vector_traits<Mat>::element_t min_angle, typename vector_traits<Mat>::element_t max_angle)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		std::uniform_real_distribution<S> dist(min_angle, max_angle);
		return Rotation<Mat>(axis_norm, dist(rng));
	}

	// Create a 2D matrix containing random rotation between angles [min_angle, max_angle)
	template <VectorType Mat, typename Rng = std::default_random_engine> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 2)
	inline Mat pr_vectorcall Random(Rng& rng, typename vector_traits<Mat>::element_t min_angle, typename vector_traits<Mat>::element_t max_angle)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		std::uniform_real_distribution<S> dist(min_angle, max_angle);
		return Rotation<Mat>(dist(rng));
	}

	// Create a random 2D rotation matrix
	template <VectorType Mat, typename Rng = std::default_random_engine> requires (IsRank2<Mat> && vector_traits<Mat>::dimension == 2)
	inline Mat pr_vectorcall Random(Rng& rng)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		return Random(rng, S(0), constants<S>::tau);
	}
}
