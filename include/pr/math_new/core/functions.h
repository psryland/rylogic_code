//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "../core/forward.h"
#include "../core/traits.h"
#include "../core/constants.h"

namespace pr::math
{
	// Notes:
	//  - This file contains operators for generic vector types.
	//  - Overloads for specific types can be created (e.g. Vec4)

	// Operators
	template <VectorType Vec> constexpr Vec operator + (Vec const& lhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = +vec(lhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = +vec(lhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = +vec(lhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = +vec(lhs).w;
		return res;
	}
	template <VectorType Vec> constexpr Vec operator - (Vec const& lhs)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = -vec(lhs).x;
		if constexpr (vt::dimension > 1) vec(res).y = -vec(lhs).y;
		if constexpr (vt::dimension > 2) vec(res).z = -vec(lhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = -vec(lhs).w;
		return res;
	}
	template <VectorType Vec> constexpr Vec& operator += (Vec& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x += vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y += vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z += vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w += vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& operator -= (Vec& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x -= vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y -= vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z -= vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w -= vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& operator *= (Vec& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x *= vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y *= vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z *= vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w *= vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& operator *= (Vec& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x *= rhs;
		if constexpr (vt::dimension > 1) vec(lhs).y *= rhs;
		if constexpr (vt::dimension > 2) vec(lhs).z *= rhs;
		if constexpr (vt::dimension > 3) vec(lhs).w *= rhs;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& operator /= (Vec& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x /= vec(rhs).x;
		if constexpr (vt::dimension > 1) vec(lhs).y /= vec(rhs).y;
		if constexpr (vt::dimension > 2) vec(lhs).z /= vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(lhs).w /= vec(rhs).w;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& operator /= (Vec& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) vec(lhs).x /= rhs;
		if constexpr (vt::dimension > 1) vec(lhs).y /= rhs;
		if constexpr (vt::dimension > 2) vec(lhs).z /= rhs;
		if constexpr (vt::dimension > 3) vec(lhs).w /= rhs;
		return lhs;
	}
	template <VectorType Vec> constexpr Vec& operator %= (Vec& lhs, Vec const& rhs)
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
	template <VectorType Vec> constexpr Vec& operator %= (Vec& lhs, typename vector_traits<Vec>::element_t rhs)
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
	template <VectorType Vec> constexpr Vec operator + (Vec const& lhs, Vec const& rhs)
	{
		Vec res = lhs;
		return res += rhs;
	}
	template <VectorType Vec> constexpr Vec operator - (Vec const& lhs, Vec const& rhs)
	{
		Vec res = lhs;
		return res -= rhs;
	}
	template <VectorType Vec> constexpr Vec operator * (Vec const& lhs, Vec const& rhs)
	{
		Vec res = lhs;
		return res *= rhs;
	}
	template <VectorType Vec> constexpr Vec operator * (Vec const& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		Vec res = lhs;
		return res *= rhs;
	}
	template <VectorType Vec> constexpr Vec operator * (typename vector_traits<Vec>::element_t lhs, Vec const& rhs)
	{
		Vec res = rhs;
		return res *= lhs;
	}
	template <VectorType Vec> constexpr Vec operator / (Vec const& lhs, Vec const& rhs)
	{
		Vec res = lhs;
		return res /= rhs;
	}
	template <VectorType Vec> constexpr Vec operator / (Vec const& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		Vec res = lhs;
		return res /= rhs;
	}
	template <VectorType Vec> constexpr Vec operator % (Vec const& lhs, Vec const& rhs)
	{
		Vec res = lhs;
		return res %= rhs;
	}
	template <VectorType Vec> constexpr Vec operator % (Vec const& lhs, typename vector_traits<Vec>::element_t rhs)
	{
		Vec res = lhs;
		return res %= rhs;
	}
	template <TensorType Vec> constexpr auto operator <=> (Vec const& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) if (auto cmp = std::partial_order(vec(lhs).x, vec(rhs).x); cmp != 0) return cmp;
		if constexpr (vt::dimension > 1) if (auto cmp = std::partial_order(vec(lhs).y, vec(rhs).y); cmp != 0) return cmp;
		if constexpr (vt::dimension > 2) if (auto cmp = std::partial_order(vec(lhs).z, vec(rhs).z); cmp != 0) return cmp;
		if constexpr (vt::dimension > 3) if (auto cmp = std::partial_order(vec(lhs).w, vec(rhs).w); cmp != 0) return cmp;
		return std::partial_ordering::equivalent;
	}
	template <TensorType Vec> constexpr bool operator == (Vec const& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) if (!(vec(lhs).x == vec(rhs).x)) return false;
		if constexpr (vt::dimension > 1) if (!(vec(lhs).y == vec(rhs).y)) return false;
		if constexpr (vt::dimension > 2) if (!(vec(lhs).z == vec(rhs).z)) return false;
		if constexpr (vt::dimension > 3) if (!(vec(lhs).w == vec(rhs).w)) return false;
		return true;
	}
	template <TensorType Vec> constexpr bool operator != (Vec const& lhs, Vec const& rhs)
	{
		return !(lhs == rhs);
	}
	template <TensorType Vec> constexpr std::ostream& operator << (std::ostream& out, Vec const& v)
	{
		using vt = vector_traits<Vec>;
		if constexpr (vt::dimension > 0) out << vec(v).x << (vt::dimension > 1 ? ", " : "");
		if constexpr (vt::dimension > 1) out << vec(v).y << (vt::dimension > 2 ? ", " : "");
		if constexpr (vt::dimension > 2) out << vec(v).z << (vt::dimension > 3 ? ", " : "");
		if constexpr (vt::dimension > 3) out << vec(v).w << (vt::dimension > 4 ? ", " : "");
		return out;
	}
	
	// Quaternion Operators
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
	template <QuaternionType Quat> constexpr Quat& operator *= (Quat& lhs, typename vector_traits<Quat>::element_t rhs)
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
	template <QuaternionType Quat> constexpr Quat operator /= (Quat& lhs, typename vector_traits<Quat>::element_t rhs)
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

	// Matrix Operators
	template <MatrixType Mat, VectorType Vec> requires (vector_traits<Mat>::dimension == 4 && vector_traits<Vec>::dimension == 4)
	constexpr Vec operator * (Mat const& a2b, Vec const& vec); // Transforms vec from a-space to b-space
	template <MatrixType Mat> requires (vector_traits<Mat>::dimension == 4)
	constexpr Mat operator * (Mat const& b2c, Mat const& a2b); // Returns 'a2c'

	#if 0
	// Transform Operators
	constexpr bool operator == (Xform const& lhs, Xform const& rhs)
	{
		return
			lhs.rotation == rhs.rotation &&
			lhs.translation == rhs.translation &&
			lhs.scale == rhs.scale;
	}
	constexpr bool operator != (Xform const& lhs, Xform const& rhs)
	{
		return !(lhs == rhs);
	}
	#endif

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
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension >= 1)
	constexpr Vec XAxis()
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
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension >= 2)
	constexpr Vec YAxis()
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
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension >= 3)
	constexpr Vec ZAxis()
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
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension >= 4)
	constexpr Vec Origin()
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
	template <VectorType Vec> requires (VectorType<typename vector_traits<Vec>::component_t>)
	constexpr Vec Identity()
	{
		using vt = vector_traits<Vec>;

		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = XAxis<typename vt::component_t>();
		if constexpr (vt::dimension > 1) vec(res).y = YAxis<typename vt::component_t>();
		if constexpr (vt::dimension > 2) vec(res).z = ZAxis<typename vt::component_t>();
		if constexpr (vt::dimension > 3) vec(res).w = Origin<typename vt::component_t>();
		return res;
	}
	template <QuaternionType Quat>
	constexpr Quat Identity()
	{
		using S = typename vector_traits<Quat>::element_t;
		return { S(0), S(0), S(0), S(1) };
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
	template <TensorType Vec> constexpr Vec Abs(Vec const& v)
	{
		using vt = vector_traits<Vec>;
		Vec res = {};
		if constexpr (vt::dimension > 0) vec(res).x = Abs(vec(v).x);
		if constexpr (vt::dimension > 1) vec(res).y = Abs(vec(v).y);
		if constexpr (vt::dimension > 2) vec(res).z = Abs(vec(v).z);
		if constexpr (vt::dimension > 3) vec(res).w = Abs(vec(v).w);
		return res;
	}

	// Square a value
	template <ScalarType S> constexpr S Square(S x)
	{
		return x * x;
	}

	// Compile time version of the square root.
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
		if (std::is_constant_evaluated())
			return static_cast<S>(SqrtCT(x));
		else
			return static_cast<S>(std::sqrt(x));
	}

	// Min/Max element (i.e. nearest to -inf/+inf)
	template <std::integral S> inline constexpr S MinElement(S v)
	{
		return v;
	}
	template <std::floating_point S> inline constexpr S MinElement(S v)
	{
		return v;
	}
	template <std::integral S> inline constexpr S MaxElement(S v)
	{
		return v;
	}
	template <std::floating_point S> inline constexpr S MaxElement(S v)
	{
		return v;
	}
	template <TensorType Vec> constexpr typename vector_traits<Vec>::element_t MinElement(Vec const& v)
	{
		using vt = vector_traits<Vec>;
		auto minimum = Max<typename vt::element_t>();
		if constexpr (vt::dimension > 0) minimum = std::min(minimum, MinElement(vec(v).x));
		if constexpr (vt::dimension > 1) minimum = std::min(minimum, MinElement(vec(v).y));
		if constexpr (vt::dimension > 2) minimum = std::min(minimum, MinElement(vec(v).z));
		if constexpr (vt::dimension > 3) minimum = std::min(minimum, MinElement(vec(v).w));
		return minimum;
	}
	template <TensorType Vec> constexpr typename vector_traits<Vec>::element_t MaxElement(Vec const& v)
	{
		using vt = vector_traits<Vec>;
		auto maximum = Min<typename vt::element_t>();
		if constexpr (vt::dimension > 0) maximum = std::max(maximum, MaxElement(vec(v).x));
		if constexpr (vt::dimension > 1) maximum = std::max(maximum, MaxElement(vec(v).y));
		if constexpr (vt::dimension > 2) maximum = std::max(maximum, MaxElement(vec(v).z));
		if constexpr (vt::dimension > 3) maximum = std::max(maximum, MaxElement(vec(v).w));
		return maximum;
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
	template <TensorType Vec> constexpr bool FEqlAbsolute(Vec const& lhs, Vec const& rhs, auto tol)
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
		if (b == 0) return std::abs(a) < tol;
		if (a == 0) return std::abs(b) < tol;

		// Handle infinities and exact values
		if (a == b) return true;

		// Test relative error as a fraction of the largest value
		auto abs_max_element = std::max(std::abs(a), std::abs(b));
		return FEqlAbsolute(a, b, tol * abs_max_element);
	}
	template <TensorType Vec> constexpr bool FEqlRelative(Vec const& lhs, Vec const& rhs, auto tol)
	{
		auto max_a = MaxElement(Abs(lhs));
		auto max_b = MaxElement(Abs(rhs));
		if (max_b == 0) return max_a < tol;
		if (max_a == 0) return max_b < tol;
		auto abs_max_element = std::max(max_a, max_b);
		return FEqlAbsolute(lhs, rhs, tol * abs_max_element);
	}

	// Test two quaternions for equivalence (i.e. do they represent the same orientation)
	template <QuaternionType Quat>
	inline bool FEqlOrientation(Quat const& lhs, Quat const& rhs, typename vector_traits<Quat>::element_t tol = Tiny<typename vector_traits<Quat>::element_t>())
	{
		using S = typename vector_traits<Quat>::element_t;
		return FEqlAbsolute(AxisAngle(rhs * ~lhs).angle, S(0), tol);
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
	template <TensorType Vec> constexpr bool FEql(Vec const& lhs, Vec const& rhs)
	{
		using S = typename vector_traits<Vec>::element_t;
		return FEqlRelative(lhs, rhs, tiny<S>);
	}
	#if 0
	inline bool FEql(transform const& lhs, transform const& rhs)
	{
		return
			FEqlOrientation(lhs.rotation, rhs.rotation) &&
			FEql(lhs.translation, rhs.translation) &&
			FEql(lhs.scale, rhs.scale);
	}
	#endif

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
	constexpr typename vector_traits<Vec>::element_t Dot(Vec const& lhs, Vec const& rhs)
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
	constexpr typename vector_traits<Vec>::element_t Dot3(Vec const& lhs, Vec const& rhs)
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
	constexpr typename vector_traits<Vec>::component_t Cross(Vec const& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		return vec(lhs).y * vec(rhs).x - vec(lhs).x * vec(rhs).y; // 2D Cross product == Dot(Rotate90CW(lhs), rhs)
	}
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension == 3)
	constexpr Vec Cross(Vec const& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;
		return Vec {
			vec(lhs).y * vec(rhs).z - vec(lhs).z * vec(rhs).y,
			vec(lhs).z * vec(rhs).x - vec(lhs).x * vec(rhs).z,
			vec(lhs).x * vec(rhs).y - vec(lhs).y * vec(rhs).x,
		};
	}
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension == 4)
	constexpr Vec Cross3(Vec const& lhs, Vec const& rhs)
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
	constexpr typename vector_traits<Vec>::element_t Triple(Vec const& a, Vec const& b, Vec const& c)
	{
		return Dot(a, Cross(b, c));
	}
	template <VectorType Vec> requires (IsRank1<Vec> && vector_traits<Vec>::dimension >= 3)
	constexpr typename vector_traits<Vec>::element_t Triple3(Vec const& a, Vec const& b, Vec const& c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Squared Length of a vector
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr typename vector_traits<Vec>::element_t LengthSq(Vec const& v)
	{
		return Dot(v, v);
	}

	// Length of a vector
	template <std::integral S> constexpr S Length(S x)
	{
		// Defined for use in recursive vector functions
		return std::abs(x);
	}
	template <std::floating_point S> constexpr S Length(S x)
	{
		// Defined for use in recursive vector functions
		return std::abs(x);
	}
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr typename vector_traits<Vec>::element_t Length(Vec const& v)
	{
		using S = typename vector_traits<Vec>::element_t;
		return Sqrt<S>(LengthSq(v));
	}

	// Normalise a vector
	template <TensorType Vec> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	constexpr Vec Normalise(Vec const& v)
	{
		return v / Length(v);
	}
	template <TensorType Vec> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t>)
	constexpr Vec Normalise(Vec const& v, Vec const& value_if_zero_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		auto len = Length(v);
		return len > Tiny<S>() ? v / len : value_if_zero_length;
	}
	template <TensorType Vec, typename IfZeroFactory> requires (IsRank1<Vec> && std::floating_point<typename vector_traits<Vec>::element_t> && requires (IfZeroFactory f) { { f() } -> std::convertible_to<Vec>; })
	constexpr Vec Normalise(Vec const& v, IfZeroFactory value_if_zero_length)
	{
		using S = typename vector_traits<Vec>::element_t;
		auto len = Length(v);
		return len > Tiny<S>() ? v / len : value_if_zero_length();
	}

	// Return true if 'mat' is an orthonormal matrix
	template <TensorType Vec> requires (IsRank1<Vec>)
	constexpr bool IsNormalised(Vec const& v, typename vector_traits<Vec>::element_t tol = Tiny<typename vector_traits<Vec>::element_t>())
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
	constexpr Vec operator * (Mat const& a2b, Vec const& v)
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
	constexpr Mat operator * (Mat const& b2c, Mat const& a2b)
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

	#if 0
	// Create a quaternion from the rotation part of a matrix
	constexpr quaternion RotationFrom(float4x4 const& mat)
	{
		assert("Only orientation matrices can be converted into quaternions" && IsOrthonormal(mat));
		constexpr auto Rsqrt = [](float x) { return 1.0f / Sqrt(x); };

		quaternion q = {};
		if (mat.x.x + mat.y.y + mat.z.z >= 0)
		{
			auto s = 0.5f * Rsqrt(1.f + mat.x.x + mat.y.y + mat.z.z);
			q.x = (mat.y.z - mat.z.y) * s;
			q.y = (mat.z.x - mat.x.z) * s;
			q.z = (mat.x.y - mat.y.x) * s;
			q.w = (0.25f / s);
		}
		else if (mat.x.x > mat.y.y && mat.x.x > mat.z.z)
		{
			auto s = 0.5f * Rsqrt(1.f + mat.x.x - mat.y.y - mat.z.z);
			q.x = (0.25f / s);
			q.y = (mat.x.y + mat.y.x) * s;
			q.z = (mat.z.x + mat.x.z) * s;
			q.w = (mat.y.z - mat.z.y) * s;
		}
		else if (mat.y.y > mat.z.z)
		{
			auto s = 0.5f * Rsqrt(1.f - mat.x.x + mat.y.y - mat.z.z);
			q.x = (mat.x.y + mat.y.x) * s;
			q.y = (0.25f / s);
			q.z = (mat.y.z + mat.z.y) * s;
			q.w = (mat.z.x - mat.x.z) * s;
		}
		else
		{
			auto s = 0.5f * Rsqrt(1.f - mat.x.x - mat.y.y + mat.z.z);
			q.x = (mat.z.x + mat.x.z) * s;
			q.y = (mat.y.z + mat.z.y) * s;
			q.z = (0.25f / s);
			q.w = (mat.x.y - mat.y.x) * s;
		}
		return q;
	}
	#endif

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

	#if 0
	// Decompose a quaternion into axis (normalised) and angle (radians)
	template <QuaternionType Quat>
	auto AxisAngle(Quat const& quat)
	{
		// Trig:
		//' cos^2(x) = 0.5 * (1 + cos(2x))
		//' w == cos(x/2)
		//' w^2 == cos^2(x/2) == 0.5 * (1 + cos(x))
		//' 2w^2 - 1 == cos(x)
		using S = typename vector_traits<Quat>::element_t;
		struct AA { float3 axis; float angle; };

		assert(IsNormalised(quat) && "quaternion isn't normalised");

		// The axis is arbitrary for identity rotations
		if (LengthSq(quat) < TinySq<S>())
			return AA{ .axis = float3{0, 0, 1}, .angle = 0.0f };

		auto axis = Normalise(quat.xyz);
		auto cos_angle = std::clamp(S(2) * Pow<S>(vec(quat).w, 2) - S(1), -S(1), +S(1)); // The cosine of the angle of rotation about the axis

		return AA{ .axis = axis, .angle = std::acosf(cos_angle) };
	}
	#endif


	// Rotate a vector by a quaternion
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	constexpr Vec Rotate(Quat const& lhs, Vec const& rhs)
	{
		using vt = vector_traits<Vec>;

		// This is an optimised version of: 'r = q*v*conj(q) for when v.w == 0'
		auto xx = vec(lhs).x * vec(lhs).x, xy = vec(lhs).x * vec(lhs).y, xz = vec(lhs).x * vec(lhs).z, xw = vec(lhs).x * vec(lhs).w;
		auto yy = vec(lhs).y * vec(lhs).y, yz = vec(lhs).y * vec(lhs).z, yw = vec(lhs).y * vec(lhs).w;
		auto zz = vec(lhs).z * vec(lhs).z, zw = vec(lhs).z * vec(lhs).w;
		auto ww = vec(lhs).w * vec(lhs).w;

		Vec res = {};
		vec(res).x = ww * vec(rhs).x + 2 * yw * vec(rhs).z - 2 * zw * vec(rhs).y + xx * vec(rhs).x + 2 * xy * vec(rhs).y + 2 * xz * vec(rhs).z - zz * vec(rhs).x - yy * vec(rhs).x;
		vec(res).y = 2 * xy * vec(rhs).x + yy * vec(rhs).y + 2 * yz * vec(rhs).z + 2 * zw * vec(rhs).x - zz * vec(rhs).y + ww * vec(rhs).y - 2 * xw * vec(rhs).z - xx * vec(rhs).y;
		vec(res).z = 2 * xz * vec(rhs).x + 2 * yz * vec(rhs).y + zz * vec(rhs).z - 2 * yw * vec(rhs).x - yy * vec(rhs).z + 2 * xw * vec(rhs).y - xx * vec(rhs).z + ww * vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(rhs).w;
		return res;
	}

	// Logarithm map of quaternion to tangent space at identity
	template <VectorType Vec, QuaternionType Quat>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	Vec LogMap(Quat const& q)
	{
		using S = typename vector_traits<Vec>::element_t;

		// Quat = [u.Sin(A/2), Cos(A/2)]
		auto cos_half_ang = std::clamp<double>(vec(q).w, -1.0, +1.0); // [0, tau]
		auto sin_half_ang = std::sqrt(Square(vec(q).x) + Square(vec(q).y) + Square(vec(q).z)); // Don't use 'sqrt(1 - w*w)', it's not float noise accurate enough when w ~= +/-1
		auto ang_by_2 = std::acos(cos_half_ang); // By convention, log space uses Length = A/2
		return std::abs(sin_half_ang) > Tiny<S>()
			? Vec{ vec(q).x, vec(q).y, vec(q).z } * static_cast<S>(ang_by_2 / sin_half_ang)
			: Vec{ vec(q).x, vec(q).y, vec(q).z };
	}
	
	// Exponential map of tangent space at identity to quaternion
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	Quat ExpMap(Vec const& v)
	{
		using S = typename vector_traits<Vec>::element_t;

		// Vec = (+/-)A * (-/+)u.
		auto ang_by_2 = Length(v); // By convention, log space uses Length = A/2
		auto cos_half_ang = std::cos(ang_by_2);
		auto sin_half_ang = std::sin(ang_by_2); // != sqrt(1 - cos_half_ang²) when ang_by_2 > tau/2
		auto s = ang_by_2 > Tiny<S>() ? static_cast<S>(sin_half_ang / ang_by_2) : S(1);
		return { vec(v).x * s, vec(v).y * s, vec(v).z * s, static_cast<S>(cos_half_ang) };
	}
}
