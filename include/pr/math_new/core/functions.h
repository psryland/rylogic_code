//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"

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

	// Matrix Operators
	template <MatrixType Mat, VectorType Vec> requires (vector_traits<Mat>::dimension == 4 && vector_traits<Vec>::dimension == 4)
		constexpr Vec pr_vectorcall operator * (Mat const& a2b, Vec v); // Transforms vec from a-space to b-space
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
		constexpr Mat pr_vectorcall operator * (Mat const& b2c, Mat const& a2b); // Returns 'a2c'

	// Constants
	template <typename S> constexpr S Zero()
	{
		return S(0);
	}
	template <typename S> constexpr S Min()
	{
		return std::numeric_limits<S>::lowest();
	}
	template <typename S> constexpr S Max()
	{
		return std::numeric_limits<S>::max();
	}
	template <VectorType Vec> constexpr Vec Zero()
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Zero<typename vt::component_t>();
		if constexpr (vt::dimension > 1) vec(res).y = Zero<typename vt::component_t>();
		if constexpr (vt::dimension > 2) vec(res).z = Zero<typename vt::component_t>();
		if constexpr (vt::dimension > 3) vec(res).w = Zero<typename vt::component_t>();
		return res;
	}
	template <VectorType Vec> constexpr Vec Min()
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Min<typename vt::component_t>();
		if constexpr (vt::dimension > 1) vec(res).y = Min<typename vt::component_t>();
		if constexpr (vt::dimension > 2) vec(res).z = Min<typename vt::component_t>();
		if constexpr (vt::dimension > 3) vec(res).w = Min<typename vt::component_t>();
		return res;
	}
	template <VectorType Vec> constexpr Vec Max()
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Max<typename vt::component_t>();
		if constexpr (vt::dimension > 1) vec(res).y = Max<typename vt::component_t>();
		if constexpr (vt::dimension > 2) vec(res).z = Max<typename vt::component_t>();
		if constexpr (vt::dimension > 3) vec(res).w = Max<typename vt::component_t>();
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
	template <VectorType Vec> requires (VectorType<typename vector_traits<Vec>::component_t>) constexpr Vec Identity()
	{
		using vt = vector_traits<Vec>;

		Vec res = {};
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
		return FEqlRelative(a, b, tiny<S>);
	}
	template <TensorType Vec> constexpr bool pr_vectorcall FEql(Vec lhs, Vec rhs)
	{
		using S = typename vector_traits<Vec>::element_t;
		return FEqlRelative(lhs, rhs, tiny<S>);
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
	template <TensorType Vec> constexpr Vec pr_vectorcall Trunc(Vec v, ETruncate trunc = ETruncate::TowardZero) requires (std::floating_point<typename vector_traits<Vec>::element_t>)
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

	// Normalise a vector
	template <TensorType Vec> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	constexpr Vec Normalise(Vec v)
	{
		return v / Length(v);
	}
	template <TensorType Vec> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	constexpr Vec Normalise(Vec v, Vec value_if_zero_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		auto len = Length(v);
		return len > Tiny<S>() ? v / len : value_if_zero_length;
	}
	template <TensorType Vec, typename IfZeroFactory> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t> && requires (IfZeroFactory f) { { f() } -> std::convertible_to<Vec>; })
	constexpr Vec Normalise(Vec v, IfZeroFactory value_if_zero_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		auto len = Length(v);
		return len > Tiny<S>() ? v / len : value_if_zero_length();
	}

	// Return true if 'mat' is an orthonormal matrix
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr bool IsNormalised(Vec v, typename vector_traits<Vec>::element_t tol = Tiny<typename vector_traits<Vec>::element_t>())
	{
		using S = typename vector_traits<Vec>::element_t;
		return Abs(LengthSq(v) - S(1)) < tol;
	}

	// Return true if 'mat' is an orthonormal matrix
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension <= 4)
	constexpr bool IsOrthonormal(Mat const& mat, typename vector_traits<Mat>::element_t tol = Tiny<typename vector_traits<Mat>::element_t>())
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		if constexpr (vt::dimension  > 0) if (!IsNormalised(vec(mat).x, tol)) return false;
		if constexpr (vt::dimension  > 1) if (!IsNormalised(vec(mat).y, tol)) return false;
		if constexpr (vt::dimension  > 2) if (!IsNormalised(vec(mat).z, tol)) return false;
		if constexpr (vt::dimension == 2) if (Abs(Cross(vec(mat).x, vec(mat).y) - S(1)) > tol) return false;
		if constexpr (vt::dimension >= 3) if (Abs(Triple3(vec(mat).x, vec(mat).y, vec(mat).z) - S(1)) > tol) return false;
		return true;
	}

	// Return the transpose of 'mat'
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
	constexpr Mat Transpose(Mat const& mat)
	{
		Mat m = mat;
		std::swap(vec(vec(m).x).y, vec(vec(m).y).x);
		std::swap(vec(vec(m).x).z, vec(vec(m).z).x);
		std::swap(vec(vec(m).x).w, vec(vec(m).w).x);
		std::swap(vec(vec(m).y).z, vec(vec(m).z).y);
		std::swap(vec(vec(m).y).w, vec(vec(m).w).y);
		std::swap(vec(vec(m).z).w, vec(vec(m).w).z);
		return m;
	}

	// Return the inverse of 'mat' (assuming an orthonormal matrix)
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
	constexpr Mat InvertOrthonormal(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		assert("Matrix is not orthonormal" && IsOrthonormal(mat));

		Mat m = mat;
		std::swap(vec(vec(m).x).y, vec(vec(m).y).x); // Transpose
		std::swap(vec(vec(m).x).z, vec(vec(m).z).x);
		std::swap(vec(vec(m).y).z, vec(vec(m).z).y);
		vec(vec(m).w).x = -Dot(vec(mat).x, vec(mat).w);
		vec(vec(m).w).y = -Dot(vec(mat).y, vec(mat).w);
		vec(vec(m).w).z = -Dot(vec(mat).z, vec(mat).w);
		return m;
	}

	// Return the inverse of 'mat'
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
	constexpr Mat Invert(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;

		Mat inv = {};
		vec(inv).x = typename vt::component_t{
			+vec(vec(mat).y).y * vec(vec(mat).z).z * vec(vec(mat).w).w - vec(vec(mat).y).y * vec(vec(mat).z).w * vec(vec(mat).w).z - vec(vec(mat).z).y * vec(vec(mat).y).z * vec(vec(mat).w).w + vec(vec(mat).z).y * vec(vec(mat).y).w * vec(vec(mat).w).z + vec(vec(mat).w).y * vec(vec(mat).y).z * vec(vec(mat).z).w - vec(vec(mat).w).y * vec(vec(mat).y).w * vec(vec(mat).z).z,
			-vec(vec(mat).x).y * vec(vec(mat).z).z * vec(vec(mat).w).w + vec(vec(mat).x).y * vec(vec(mat).z).w * vec(vec(mat).w).z + vec(vec(mat).z).y * vec(vec(mat).x).z * vec(vec(mat).w).w - vec(vec(mat).z).y * vec(vec(mat).x).w * vec(vec(mat).w).z - vec(vec(mat).w).y * vec(vec(mat).x).z * vec(vec(mat).z).w + vec(vec(mat).w).y * vec(vec(mat).x).w * vec(vec(mat).z).z,
			+vec(vec(mat).x).y * vec(vec(mat).y).z * vec(vec(mat).w).w - vec(vec(mat).x).y * vec(vec(mat).y).w * vec(vec(mat).w).z - vec(vec(mat).y).y * vec(vec(mat).x).z * vec(vec(mat).w).w + vec(vec(mat).y).y * vec(vec(mat).x).w * vec(vec(mat).w).z + vec(vec(mat).w).y * vec(vec(mat).x).z * vec(vec(mat).y).w - vec(vec(mat).w).y * vec(vec(mat).x).w * vec(vec(mat).y).z,
			-vec(vec(mat).x).y * vec(vec(mat).y).z * vec(vec(mat).z).w + vec(vec(mat).x).y * vec(vec(mat).y).w * vec(vec(mat).z).z + vec(vec(mat).y).y * vec(vec(mat).x).z * vec(vec(mat).z).w - vec(vec(mat).y).y * vec(vec(mat).x).w * vec(vec(mat).z).z - vec(vec(mat).z).y * vec(vec(mat).x).z * vec(vec(mat).y).w + vec(vec(mat).z).y * vec(vec(mat).x).w * vec(vec(mat).y).z };
		vec(inv).y = typename vt::component_t{
			-vec(vec(mat).y).x * vec(vec(mat).z).z * vec(vec(mat).w).w + vec(vec(mat).y).x * vec(vec(mat).z).w * vec(vec(mat).w).z + vec(vec(mat).z).x * vec(vec(mat).y).z * vec(vec(mat).w).w - vec(vec(mat).z).x * vec(vec(mat).y).w * vec(vec(mat).w).z - vec(vec(mat).w).x * vec(vec(mat).y).z * vec(vec(mat).z).w + vec(vec(mat).w).x * vec(vec(mat).y).w * vec(vec(mat).z).z,
			+vec(vec(mat).x).x * vec(vec(mat).z).z * vec(vec(mat).w).w - vec(vec(mat).x).x * vec(vec(mat).z).w * vec(vec(mat).w).z - vec(vec(mat).z).x * vec(vec(mat).x).z * vec(vec(mat).w).w + vec(vec(mat).z).x * vec(vec(mat).x).w * vec(vec(mat).w).z + vec(vec(mat).w).x * vec(vec(mat).x).z * vec(vec(mat).z).w - vec(vec(mat).w).x * vec(vec(mat).x).w * vec(vec(mat).z).z,
			-vec(vec(mat).x).x * vec(vec(mat).y).z * vec(vec(mat).w).w + vec(vec(mat).x).x * vec(vec(mat).y).w * vec(vec(mat).w).z + vec(vec(mat).y).x * vec(vec(mat).x).z * vec(vec(mat).w).w - vec(vec(mat).y).x * vec(vec(mat).x).w * vec(vec(mat).w).z - vec(vec(mat).w).x * vec(vec(mat).x).z * vec(vec(mat).y).w + vec(vec(mat).w).x * vec(vec(mat).x).w * vec(vec(mat).y).z,
			+vec(vec(mat).x).x * vec(vec(mat).y).z * vec(vec(mat).z).w - vec(vec(mat).x).x * vec(vec(mat).y).w * vec(vec(mat).z).z - vec(vec(mat).y).x * vec(vec(mat).x).z * vec(vec(mat).z).w + vec(vec(mat).y).x * vec(vec(mat).x).w * vec(vec(mat).z).z + vec(vec(mat).z).x * vec(vec(mat).x).z * vec(vec(mat).y).w - vec(vec(mat).z).x * vec(vec(mat).x).w * vec(vec(mat).y).z };
		vec(inv).z = typename vt::component_t{
			+vec(vec(mat).y).x * vec(vec(mat).z).y * vec(vec(mat).w).w - vec(vec(mat).y).x * vec(vec(mat).z).w * vec(vec(mat).w).y - vec(vec(mat).z).x * vec(vec(mat).y).y * vec(vec(mat).w).w + vec(vec(mat).z).x * vec(vec(mat).y).w * vec(vec(mat).w).y + vec(vec(mat).w).x * vec(vec(mat).y).y * vec(vec(mat).z).w - vec(vec(mat).w).x * vec(vec(mat).y).w * vec(vec(mat).z).y,
			-vec(vec(mat).x).x * vec(vec(mat).z).y * vec(vec(mat).w).w + vec(vec(mat).x).x * vec(vec(mat).z).w * vec(vec(mat).w).y + vec(vec(mat).z).x * vec(vec(mat).x).y * vec(vec(mat).w).w - vec(vec(mat).z).x * vec(vec(mat).x).w * vec(vec(mat).w).y - vec(vec(mat).w).x * vec(vec(mat).x).y * vec(vec(mat).z).w + vec(vec(mat).w).x * vec(vec(mat).x).w * vec(vec(mat).z).y,
			+vec(vec(mat).x).x * vec(vec(mat).y).y * vec(vec(mat).w).w - vec(vec(mat).x).x * vec(vec(mat).y).w * vec(vec(mat).w).y - vec(vec(mat).y).x * vec(vec(mat).x).y * vec(vec(mat).w).w + vec(vec(mat).y).x * vec(vec(mat).x).w * vec(vec(mat).w).y + vec(vec(mat).w).x * vec(vec(mat).x).y * vec(vec(mat).y).w - vec(vec(mat).w).x * vec(vec(mat).x).w * vec(vec(mat).y).y,
			-vec(vec(mat).x).x * vec(vec(mat).y).y * vec(vec(mat).z).w + vec(vec(mat).x).x * vec(vec(mat).y).w * vec(vec(mat).z).y + vec(vec(mat).y).x * vec(vec(mat).x).y * vec(vec(mat).z).w - vec(vec(mat).y).x * vec(vec(mat).x).w * vec(vec(mat).z).y - vec(vec(mat).z).x * vec(vec(mat).x).y * vec(vec(mat).y).w + vec(vec(mat).z).x * vec(vec(mat).x).w * vec(vec(mat).y).y };
		vec(inv).w = typename vt::component_t{
			-vec(vec(mat).y).x * vec(vec(mat).z).y * vec(vec(mat).w).z + vec(vec(mat).y).x * vec(vec(mat).z).z * vec(vec(mat).w).y + vec(vec(mat).z).x * vec(vec(mat).y).y * vec(vec(mat).w).z - vec(vec(mat).z).x * vec(vec(mat).y).z * vec(vec(mat).w).y - vec(vec(mat).w).x * vec(vec(mat).y).y * vec(vec(mat).z).z + vec(vec(mat).w).x * vec(vec(mat).y).z * vec(vec(mat).z).y,
			+vec(vec(mat).x).x * vec(vec(mat).z).y * vec(vec(mat).w).z - vec(vec(mat).x).x * vec(vec(mat).z).z * vec(vec(mat).w).y - vec(vec(mat).z).x * vec(vec(mat).x).y * vec(vec(mat).w).z + vec(vec(mat).z).x * vec(vec(mat).x).z * vec(vec(mat).w).y + vec(vec(mat).w).x * vec(vec(mat).x).y * vec(vec(mat).z).z - vec(vec(mat).w).x * vec(vec(mat).x).z * vec(vec(mat).z).y,
			-vec(vec(mat).x).x * vec(vec(mat).y).y * vec(vec(mat).w).z + vec(vec(mat).x).x * vec(vec(mat).y).z * vec(vec(mat).w).y + vec(vec(mat).y).x * vec(vec(mat).x).y * vec(vec(mat).w).z - vec(vec(mat).y).x * vec(vec(mat).x).z * vec(vec(mat).w).y - vec(vec(mat).w).x * vec(vec(mat).x).y * vec(vec(mat).y).z + vec(vec(mat).w).x * vec(vec(mat).x).z * vec(vec(mat).y).y,
			+vec(vec(mat).x).x * vec(vec(mat).y).y * vec(vec(mat).z).z - vec(vec(mat).x).x * vec(vec(mat).y).z * vec(vec(mat).z).y - vec(vec(mat).y).x * vec(vec(mat).x).y * vec(vec(mat).z).z + vec(vec(mat).y).x * vec(vec(mat).x).z * vec(vec(mat).z).y + vec(vec(mat).z).x * vec(vec(mat).x).y * vec(vec(mat).y).z - vec(vec(mat).z).x * vec(vec(mat).x).z * vec(vec(mat).y).y };

		auto det = vec(vec(mat).x).x * vec(vec(inv).x).x + vec(vec(mat).x).y * vec(vec(inv).y).x + vec(vec(mat).x).z * vec(vec(inv).z).x + vec(vec(mat).x).w * vec(vec(inv).w).x;
		assert("matrix has no inverse" && det != S(0));
		return inv * (S(1) / det);
	}

	// Matrix Operators
	template <MatrixType Mat, VectorType Vec> requires (vector_traits<Mat>::dimension == 4 && vector_traits<Vec>::dimension == 4)
	constexpr Vec pr_vectorcall operator * (Mat const& a2b, Vec v)
	{
		using vt = vector_traits<Mat>;
		auto a2bT = Transpose(a2b);
		return Vec{
			Dot(vec(a2bT).x, v),
			Dot(vec(a2bT).y, v),
			Dot(vec(a2bT).z, v),
			Dot(vec(a2bT).w, v),
		};
	}
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
	constexpr Mat pr_vectorcall operator * (Mat const& b2c, Mat const& a2b)
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
		vec(ans).x = Vec(Dot(vec(b2cT).x, vec(a2b).x), Dot(vec(b2cT).y, vec(a2b).x), Dot(vec(b2cT).z, vec(a2b).x), Dot(vec(b2cT).w, vec(a2b).x));
		vec(ans).y = Vec(Dot(vec(b2cT).x, vec(a2b).y), Dot(vec(b2cT).y, vec(a2b).y), Dot(vec(b2cT).z, vec(a2b).y), Dot(vec(b2cT).w, vec(a2b).y));
		vec(ans).z = Vec(Dot(vec(b2cT).x, vec(a2b).z), Dot(vec(b2cT).y, vec(a2b).z), Dot(vec(b2cT).z, vec(a2b).z), Dot(vec(b2cT).w, vec(a2b).z));
		vec(ans).w = Vec(Dot(vec(b2cT).x, vec(a2b).w), Dot(vec(b2cT).y, vec(a2b).w), Dot(vec(b2cT).z, vec(a2b).w), Dot(vec(b2cT).w, vec(a2b).w));
		return ans;
	}

	// Create an affine transform from axis, angle, and translation
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
	inline Mat Transform(typename vector_traits<Mat>::component_t const& axis, typename vector_traits<Mat>::element_t angle, typename vector_traits<Mat>::component_t const& translation)
	{
		using vt = vector_traits<Mat>;
		using S = typename vt::element_t;
		assert(IsNormalised(axis) && "Axis should be unit length");

		Mat mat = {};

		auto sin_angle = static_cast<S>(std::sin(angle));
		auto cos_angle = static_cast<S>(std::cos(angle));
		auto trace_vec = axis * (S(1) - cos_angle);

		vec(vec(mat).x).x = vec(trace_vec).x * vec(axis).x + cos_angle;
		vec(vec(mat).y).y = vec(trace_vec).y * vec(axis).y + cos_angle;
		vec(vec(mat).z).z = vec(trace_vec).z * vec(axis).z + cos_angle;

		vec(trace_vec).x *= vec(axis).y;
		vec(trace_vec).z *= vec(axis).x;
		vec(trace_vec).y *= vec(axis).z;

		vec(vec(mat).x).y = vec(trace_vec).x + sin_angle * vec(axis).z;
		vec(vec(mat).x).z = vec(trace_vec).z - sin_angle * vec(axis).y;
		vec(vec(mat).x).w = 0.0f;
		vec(vec(mat).y).x = vec(trace_vec).x - sin_angle * vec(axis).z;
		vec(vec(mat).y).z = vec(trace_vec).y + sin_angle * vec(axis).x;
		vec(vec(mat).y).w = 0.0f;
		vec(vec(mat).z).x = vec(trace_vec).z + sin_angle * vec(axis).y;
		vec(vec(mat).z).y = vec(trace_vec).y - sin_angle * vec(axis).x;
		vec(vec(mat).z).w = 0.0f;

		vec(mat).w = translation;

		return mat;
	}

	// Create a scale vector from the rotation part of a matrix
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension <= 4)
	constexpr typename vector_traits<Mat>::component_t ScaleFrom(Mat const& mat)
	{
		using vt = vector_traits<Mat>;
		if constexpr (vt::dimension == 1) return { Length(vec(mat).x) };
		if constexpr (vt::dimension == 2) return { Length(vec(mat).x), Length(vec(mat).y) };
		if constexpr (vt::dimension == 3) return { Length(vec(mat).x), Length(vec(mat).y), Length(vec(mat).z) };
		if constexpr (vt::dimension == 4) return { Length(vec(mat).x), Length(vec(mat).y), Length(vec(mat).z), Length(vec(mat).w) };
	}

	// Create a random vector with unit length
	template <TensorType Vec, typename Rng = std::default_random_engine> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	inline Vec pr_vectorcall RandomN(Rng& rng)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
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

	// Create a random vector with components on interval [vmin, vmax]
	template <TensorType Vec, typename Rng = std::default_random_engine> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	inline Vec pr_vectorcall Random(Rng& rng, Vec vmin, Vec vmax)
	{
		using vt = vector_traits<Vec>;
		using S = typename vt::element_t;
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

	// Create a random vector with length on interval [min_length, max_length]
	template <TensorType Vec, typename Rng = std::default_random_engine> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	inline Vec pr_vectorcall Random(Rng& rng, typename vector_traits<Vec>::element_t min_length, typename vector_traits<Vec>::element_t max_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		std::uniform_real_distribution<S> dist(min_length, max_length);
		return dist(rng) * RandomN<Vec>(rng);
	}

	// Create a random vector centred on 'centre' with radius 'radius'
	template <TensorType Vec, typename Rng = std::default_random_engine> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	inline Vec pr_vectorcall Random(Rng& rng, Vec centre, typename vector_traits<Vec>::element_t radius)
	{
		using S = typename vector_traits<Vec>::element_t;
		return Random<Vec>(rng, S(0), radius) + centre;
	}
}
