//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"

namespace pr
{
	#pragma region Operators
	template <typename T, typename = maths::enable_if_vN<T>> inline bool operator == (T const& lhs, T const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) == 0;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool operator != (T const& lhs, T const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) != 0;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool operator <  (T const& lhs, T const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) < 0;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool operator >  (T const& lhs, T const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) > 0;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool operator <= (T const& lhs, T const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool operator >= (T const& lhs, T const& rhs)
	{
		return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator += (T& lhs, T const& rhs)
	{
		return lhs = lhs + rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator -= (T& lhs, T const& rhs)
	{
		return lhs = lhs - rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator *= (T& lhs, T const& rhs)
	{
		return lhs = lhs * rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator /= (T& lhs, T const& rhs)
	{
		return lhs = lhs / rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator %= (T& lhs, T const& rhs)
	{
		return lhs = lhs % rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator *= (T& lhs, typename maths::is_vec<T>::cp_type rhs)
	{
		return lhs = lhs * rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator /= (T& lhs, typename maths::is_vec<T>::cp_type rhs)
	{
		return lhs = lhs / rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T& operator %= (T& lhs, typename maths::is_vec<T>::cp_type rhs)
	{
		return lhs = lhs % rhs;
	}
	#pragma endregion

	// Component access to arrays
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A x_cp(A const* ptr) { return ptr[0]; }
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A y_cp(A const* ptr) { return ptr[1]; }
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A z_cp(A const* ptr) { return ptr[2]; }
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A w_cp(A const* ptr) { return ptr[3]; }

	// Component access to initialiser lists
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A x_cp(std::initializer_list<A> l) { return l.begin()[0]; }
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A y_cp(std::initializer_list<A> l) { return l.begin()[1]; }
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A z_cp(std::initializer_list<A> l) { return l.begin()[2]; }
	template <typename A, typename = maths::enable_if_vec_cp<A>> inline A w_cp(std::initializer_list<A> l) { return l.begin()[3]; }

	// Casting component accessors
	template <typename R, typename A> inline R x_as(A const& x) { return static_cast<R>(x_cp(x)); }
	template <typename R, typename A> inline R y_as(A const& x) { return static_cast<R>(y_cp(x)); }
	template <typename R, typename A> inline R z_as(A const& x) { return static_cast<R>(z_cp(x)); }
	template <typename R, typename A> inline R w_as(A const& x) { return static_cast<R>(w_cp(x)); }

	// Compile time function for applying 'op' to each component of a vector
	template <typename T, typename Op, typename = maths::enable_if_vN<T>> constexpr T CompOp(T const& a, Op op)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = op(a[i]);
		return r;
	}
	template <typename T, typename Op, typename = maths::enable_if_vN<T>> constexpr T CompOp(T const& a, T const& b, Op op)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = op(a[i], b[i]);
		return r;
	}
	template <typename T, typename Op, typename = maths::enable_if_vN<T>> constexpr T CompOp(T const& a, T const& b, T const& c, Op op)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = op(a[i], b[i], c[i]);
		return r;
	}
	template <typename T, typename Op, typename = maths::enable_if_vN<T>> constexpr T CompOp(T const& a, T const& b, T const& c, T const& d, Op op)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = op(a[i], b[i], c[i], d[i]);
		return r;
	}

	// Return true if any element satisfies 'Pred'
	template <typename T, typename Pred, typename = maths::enable_if_arith<T>> constexpr bool Any(T value, Pred pred)
	{
		return pred(value);
	}
	template <typename T, typename Pred, typename = maths::enable_if_vN<T>> constexpr bool Any(T const& v, Pred pred)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && !Any(v[i], pred); ++i) {}
		return i != iend;
	}

	// Return true if all elements satisfy 'Pred'
	template <typename T, typename Pred, typename = maths::enable_if_arith<T>> constexpr bool All(T value, Pred pred)
	{
		return pred(value);
	}
	template <typename T, typename Pred, typename = maths::enable_if_vN<T>> constexpr bool All(T const& v, Pred pred)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && All(v[i], pred); ++i) {}
		return i == iend;
	}

	// Equality
	template <typename T, typename = maths::enable_if_vec_cp<T>> constexpr bool Equal(T lhs, T rhs)
	{
		return lhs == rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr bool Equal(T const& lhs, T const& rhs)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && Equal(lhs[i], rhs[i]); ++i) {}
		return i == iend;
	}

	// Absolute value
	constexpr float Abs(float x)
	{
		return x >= 0 ? x : -x;
	}
	constexpr double Abs(double x)
	{
		return x >= 0 ? x : -x;
	}
	constexpr int Abs(int x)
	{
		return x >= 0 ? x : -x;
	}
	constexpr long Abs(long x)
	{
		return x >= 0 ? x : -x;
	}
	constexpr long long Abs(long long x)
	{
		return x >= 0 ? x : -x;
	}
	constexpr unsigned int Abs(unsigned int x)
	{
		return x;
	}
	constexpr unsigned long Abs(unsigned long x)
	{
		return x;
	}
	constexpr unsigned long long Abs(unsigned long long x)
	{
		return x;
	}
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T Abs(T x)
	{
		return x >= 0 ? x : -x;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Abs(T const& v)
	{
		return CompOp(v, [](auto x) { return Abs(x); });
	}
	template <typename T, int N> constexpr std::array<std::decay_t<T>, N> Abs(T(&v)[N])
	{
		std::array<std::decay_t<T>, N> r = {};
		for (int i = 0, iend = N; i != iend; ++i) r[i] = Abs(v[i]);
		return r;
	}

	// Min/Max/Clamp
	template <typename T, typename = maths::enable_if_not_vN<T>> constexpr T Min(T x, T y)
	{
		return (x > y) ? y : x;
	}
	template <typename T, typename = maths::enable_if_not_vN<T>> constexpr T Max(T x, T y)
	{
		return (x > y) ? x : y;
	}
	template <typename T, typename = maths::enable_if_not_vN<T>> constexpr T Clamp(T x, T mn, T mx)
	{
		assert("[min,max] must be a positive range" && mn <= mx);
		return (mx < x) ? mx : (x < mn) ? mn : x;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Min(T const& x, T const& y)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = Min(x[i], y[i]);
		return r;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Max(T const& x, T const& y)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = Max(x[i], y[i]);
		return r;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Clamp(T const& x, T const& mn, T const& mx)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = Clamp(x[i], mn[i], mx[i]);
		return r;
	}
	template <typename T, typename CP = maths::is_vec<T>::cp_type, typename = maths::enable_if_vN<T>> constexpr T Clamp(T const& x, CP mn, CP mx)
	{
		T r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = Clamp(x[i], mn, mx);
		return r;
	}
	template <typename T, typename... A> constexpr T Min(T const& x, T const& y, A&&... a)
	{
		return Min(Min(x, y), std::forward<A>(a)...);
	}
	template <typename T, typename... A> constexpr T Max(T const& x, T const& y, A&&... a)
	{
		return Max(Max(x, y), std::forward<A>(a)...);
	}

	#pragma warning (disable:4756) // Constant overflow in floating point arithmetic

	// Floating point comparisons. *WARNING* 'tol' is an absolute tolerance. Returns true if a is in the range (b-tol,b+tol)
	template <typename = void> constexpr bool FEqlAbsolute(float a, float b, float tol)
	{
		// When float operations are performed at compile time, the compiler warnings about 'inf'
		assert(isnan(tol) || tol >= 0); // NaN is not an error, comparisons with NaN are defined to always be false
		return Abs(a - b) < tol;
	}
	template <typename = void> constexpr bool FEqlAbsolute(double a, double b, double tol)
	{
		// When float operations are performed at compile time, the compiler warnings about 'inf'
		assert(isnan(tol) || tol >= 0); // NaN is not an error, comparisons with NaN are defined to always be false
		return Abs(a - b) < tol;
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> constexpr bool FEqlAbsolute(T const& a, T const& b, V tol)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && FEqlAbsolute(a[i], b[i], tol); ++i) {}
		return i == iend;
	}
	template <typename T> inline bool FEqlAbsolute(std::span<T> const& a, std::span<T> const& b, T tol)
	{
		if (a.size() != b.size())
			return false;

		int i = 0, iend = int(a.size());
		for (; i != iend && FEqlAbsolute(a[i], b[i], tol); ++i) {}
		return i == iend;
	}

	// *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
	template <typename = void> constexpr bool FEqlRelative(float a, float b, float tol)
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
		return FEqlAbsolute(a, b, tol * Max(Abs(a), Abs(b)));
	}
	template <typename = void> constexpr bool FEqlRelative(double a, double b, double tol)
	{
		if (b == 0) return Abs(a) < tol;
		if (a == 0) return Abs(b) < tol;
		if (a == b) return true;
		auto abs_max_element = Max(Abs(a), Abs(b));
		return FEqlAbsolute(a, b, tol * abs_max_element);
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> constexpr bool FEqlRelative(T const& a, T const& b, V tol)
	{
		auto max_a = MaxElementAbs(a);
		auto max_b = MaxElementAbs(b);
		if (max_b == 0) return max_a < tol;
		if (max_a == 0) return max_b < tol;
		auto abs_max_element = Max(max_a, max_b);
		return FEqlAbsolute(a, b, tol * abs_max_element);
	}
	template <typename T> inline bool FEqlRelative(std::span<T> const& a, std::span<T> const& b, T tol)
	{
		if (a.size() != b.size()) return false;
		auto max_a = MaxElementAbs(a);
		auto max_b = MaxElementAbs(b);
		if (max_b == 0) return max_a < tol;
		if (max_a == 0) return max_b < tol;
		auto abs_max_element = Max(max_a, max_b);
		return FEqlAbsolute<T>(a, b, tol * abs_max_element);
	}

	// FEqlRelative using 'tinyf'. Returns true if a in the range (b - max(a,b)*tiny, b + max(a,b)*tiny)
	constexpr bool FEql(float a, float b)
	{
		// Don't add a 'tol' parameter because it looks like the function should perform a == b +- tol, which isn't what it does.
		return FEqlRelative(a, b, maths::tinyf);
	}
	constexpr bool FEql(double a, double b)
	{
		return FEqlRelative(a, b, maths::tinyd);
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr bool FEql(T const& a, T const& b)
	{
		return FEqlRelative(a, b, maths::tinyf);
	}
	template <typename T> inline bool FEql(std::span<T> const& a, std::span<T> const& b)
	{
		return FEqlRelative<T>(a, b, maths::tinyf);
	}

	//#pragma warning (default:4756)

	// NaN test
	inline bool IsNaN(float value)
	{
		return std::isnan(value);
	}
	inline bool IsNaN(double value)
	{
		return std::isnan(value);
	}
	template <typename T, typename = typename maths::enable_if_arith<T>> inline bool IsNaN(T value)
	{
		return false;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsNaN(T const& value, bool any = true) // false = all
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		if (any)
		{
			for (; i != iend && !IsNaN(value[i]); ++i) {}
			return i != iend;
		}
		else
		{
			for (; i != iend && IsNaN(value[i]); ++i) {}
			return i == iend;
		}
	}

	// Finite test
	inline bool IsFinite(float value)
	{
		return std::isfinite(value);
	}
	inline bool IsFinite(double value)
	{
		return std::isfinite(value);
	}
	inline bool IsFinite(float value, float max_value)
	{
		return IsFinite(value) && std::fabs(value) < max_value;
	}
	inline bool IsFinite(double value, double max_value)
	{
		return IsFinite(value) && std::fabs(value) < max_value;
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline bool IsFinite(T value)
	{
		return value >= limits<T>::lowest() && value <= limits<T>::max();
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline bool IsFinite(T value, T max_value)
	{
		return IsFinite(value) && Abs(value) < max_value;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsFinite(T const& value, bool any = false)
	{
		return any
			? Any(value, [](auto x) { return IsFinite(x); })
			: All(value, [](auto x) { return IsFinite(x); });
	}

	// Ceil/Floor/Round/Fmod
	template <typename T, typename = maths::enable_if_arith<T>> inline T Ceil(T x)
	{
		return static_cast<T>(std::ceil(x));
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Floor(T x)
	{
		return static_cast<T>(std::floor(x));
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Round(T x)
	{
		return static_cast<T>(std::round(x));
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Fmod(T x, T y)
	{
		return std::fmod(x, y);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Ceil(T const& v)
	{
		// Note: arrays as vectors cannot use this function because arrays cannot be returned by value
		return CompOp(v, [](auto x) { return Ceil(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Floor(T const& v)
	{
		// Note: arrays as vectors cannot use this function because arrays cannot be returned by value
		return CompOp(v, [](auto x) { return Floor(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Round(T const& v)
	{
		// Note: arrays as vectors cannot use this function because arrays cannot be returned by value
		return CompOp(v, [](auto x) { return Round(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Fmod(T const& x, T const& y)
	{
		return CompOp(x, y, [](auto x, auto y) { return Fmod(x, y); });
	}

	// Converts bool to +1,-1 (note: no 0 value)
	constexpr int Sign(bool positive)
	{
		return positive ? +1 : -1;
	}
	constexpr int SignI(bool positive)
	{
		return positive ? +1 : -1;
	}
	constexpr float SignF(bool positive)
	{
		return positive ? +1.0f : -1.0f;
	}

	// Sign, returns +1 if x >= 0 otherwise -1. If 'zero_is_positive' is false, then 0 in gives 0 out.
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T Sign(T x, bool zero_is_positive = true)
	{
		if constexpr (std::is_unsigned_v<T>)
			return x > 0 ? +1 : static_cast<T>(zero_is_positive);
		else
			return x > 0 ? +1 : x < 0 ? -1 : static_cast<T>(zero_is_positive);
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Sign(T const& v, bool zero_is_positive = true)
	{
		return CompOp(v, [=](auto x) { return Sign(v[i], zero_is_positive); });
	}

	// Divide 'a' by 'b' if 'b' is not equal to zero, otherwise return 'def'
	template <typename T> constexpr T Div(T a, T b, T def = T())
	{
		return b != T() ? a / b : def;
	}

	// Truncate value
	enum class ETruncType { TowardZero, ToNearest };
	constexpr float Trunc(float x, ETruncType ty = ETruncType::TowardZero)
	{
		switch (ty)
		{
		default: assert("Unknown truncation type" && false); return x;
		case ETruncType::ToNearest:  return static_cast<float>(static_cast<int>(x + Sign(x) * 0.5f));
		case ETruncType::TowardZero: return static_cast<float>(static_cast<int>(x));
		}
	}
	constexpr double Trunc(double x, ETruncType ty = ETruncType::TowardZero)
	{
		switch (ty)
		{
		default: assert("Unknown truncation type" && false); return x;
		case ETruncType::ToNearest:  return static_cast<double>(static_cast<long long>(x + Sign(x) * 0.5f));
		case ETruncType::TowardZero: return static_cast<double>(static_cast<long long>(x));
		}
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Trunc(T const& v, ETruncType ty = ETruncType::TowardZero)
	{
		return CompOp(v, [=](auto x) { return Trunc(x, ty); });
	}

	// Fractional part
	inline float Frac(float x)
	{
		float n;
		return std::modf(x, &n);
	}
	inline double Frac(double x)
	{
		double n;
		return std::modf(x, &n);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Frac(T const& v)
	{
		return CompOp(v, [](auto x) { return Frac(x); });
	}

	// Square a value
	constexpr float Sqr(float x)
	{
		return x * x;
	}
	constexpr double Sqr(double x)
	{
		return x * x;
	}
	constexpr long Sqr(long x)
	{
		assert("Overflow" && Abs(x) <= 46340L);
		return x * x;
	}
	constexpr long long Sqr(long long x)
	{
		assert("Overflow" && Abs(x) <= 3037000499LL);
		return x * x;
	}
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T Sqr(T x)
	{
		return x * x;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr T Sqr(T const& v)
	{
		return CompOp(v, [](auto x) { return x * x; });
	}

	// Cube a value
	constexpr float Cube(float x)
	{
		return x * x * x;
	}
	constexpr double Cube(double x)
	{
		return x * x * x;
	}
	constexpr long Cube(long x)
	{
		assert("Overflow" && Abs(x) <= 1625L);
		return x * x * x;
	}
	constexpr long long Cube(long long x)
	{
		assert("Overflow" && Abs(x) <= 2642245LL);
		return x * x * x;
	}
	template <typename T> constexpr T Cube(T const& x)
	{
		return x * x * x;
	}

	// Square root
	inline float Sqrt(float x)
	{
		assert("Sqrt of negative or undefined value" && x >= 0 && IsFinite(x));
		return std::sqrt(x);
	}
	inline double Sqrt(double x)
	{
		assert("Sqrt of negative or undefined value" && x >= 0 && IsFinite(x));
		return std::sqrt(x);
	}
	inline float Sqrt(int x)
	{
		return Sqrt(static_cast<float>(x));
	}
	inline float Sqrt(long x)
	{
		return Sqrt(static_cast<float>(x));
	}
	inline double Sqrt(long long x)
	{
		return Sqrt(static_cast<double>(x));
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Sqrt(T const&)
	{
		// Sqrt is ill-defined for non-square vectors
		// Matrices have an overload that finds the matrix whose product is 'x'.
		static_assert(std::is_same_v<T, std::false_type>, "Sqrt is not defined for general vector types");
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T CompSqrt(T x) // Component Sqrt
	{
		return Sqrt(x);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T CompSqrt(T const& v) // Component Sqrt
	{
		return CompOp(v, [](auto x) { return CompSqrt(x); });
	}
	constexpr double SqrtCT(double x)
	{
		// Compile time version of the square root.
		//   - For a finite and non-negative value of "x", returns an approximation for the square root of "x"
		//   - Otherwise, returns NaN
		struct L
		{
			constexpr static double NewtonRaphson(double x, double curr, double prev)
			{
				return curr == prev ? curr
					: NewtonRaphson(x, 0.5 * (curr + x / curr), curr);
			}
		};

		return x >= 0 && x < limits<double>::infinity()
			? L::NewtonRaphson(x, x, 0)
			: limits<double>::quiet_NaN();
	}

	// Signed Sqr
	constexpr float SignedSqr(float x)
	{
		return x >= 0 ? Sqr(x) : -Sqr(x);
	}
	constexpr double SignedSqr(double x)
	{
		return x >= 0 ? Sqr(x) : -Sqr(x);
	}
	constexpr long SignedSqr(long x)
	{
		return x >= 0 ? Sqr(x) : -Sqr(x);
	}
	constexpr long long SignedSqr(long long x)
	{
		return x >= 0 ? Sqr(x) : -Sqr(x);
	}
	template <typename T> constexpr T SignedSqr(T const& x)
	{
		return x >= 0 ? Sqr(x) : -Sqr(x);
	}

	// Signed Sqrt
	inline float SignedSqrt(float x)
	{
		return x >= 0 ? Sqrt(x) : -Sqrt(-x);
	}
	inline double SignedSqrt(double x)
	{
		return x >= 0 ? Sqrt(x) : -Sqrt(-x);
	}
	inline float SignedSqrt(long x)
	{
		return x >= 0 ? Sqrt(x) : -Sqrt(-x);
	}
	inline double SignedSqrt(long long x)
	{
		return x >= 0 ? Sqrt(x) : -Sqrt(-x);
	}
	template <typename T> inline T SignedSqrt(T const& x)
	{
		return x >= T() ? Sqrt(x) : -Sqrt(-x);
	}

	// Angles
	template <typename T> constexpr inline T DegreesToRadians(T degrees)
	{
		return T(degrees * maths::tau_by_360);
	}
	template <typename T> constexpr inline T RadiansToDegrees(T radians)
	{
		return T(radians * maths::E60_by_tau);
	}

	// Trig functions
	template <typename T, typename = maths::enable_if_arith<T>> inline T Sin(T x)
	{
		return std::sin(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Cos(T x)
	{
		return std::cos(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Tan(T x)
	{
		return std::tan(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T ASin(T x)
	{
		return std::asin(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T ACos(T x)
	{
		return std::acos(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T ATan(T x)
	{
		return std::atan(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T ATan2(T y, T x)
	{
		return std::atan2(y, x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T ATan2Positive(T y, T x)
	{
		auto a = std::atan2(y, x);
		return a < 0 ? a + static_cast<T>(maths::tau) : a;
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Sinh(T x)
	{
		return std::sinh(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Cosh(T x)
	{
		return std::cosh(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Tanh(T x)
	{
		return std::tanh(x);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Sin(T const& v)
	{
		return CompOp(v, [](auto x) { return Sin(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Cos(T const& v)
	{
		return CompOp(v, [](auto x) { return Cos(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Tan(T const& v)
	{
		return CompOp(v, [](auto x) { return Tan(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T ASin(T const& v)
	{
		return CompOp(v, [](auto x) { return ASin(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T ACos(T const& v)
	{
		return CompOp(v, [](auto x) { return ACos(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T ATan(T const& v)
	{
		return CompOp(v, [](auto x) { return ATan(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T ATan2(T const& y, T const& x)
	{
		return CompOp(y, x, [](auto y, auto x) { return ATan2(y, x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T ATan2Positive(T const& y, T const& x)
	{
		return CompOp(y, x, [](auto y, auto x) { return ATan2Positive(y, x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Sinh(T const& v)
	{
		return CompOp(v, [](auto x) { return Sinh(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Cosh(T const& v)
	{
		return CompOp(v, [](auto x) { return Cosh(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Tanh(T const& v)
	{
		return CompOp(v, [](auto x) { return Tanh(x); });
	}

	// Power/Exponent/Log
	constexpr int Pow2(int n)
	{
		return 1 << n;
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Pow(T x, T y)
	{
		return std::pow(x, y);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Exp(T x)
	{
		return std::exp(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Log10(T x)
	{
		return std::log10(x);
	}
	template <typename T, typename = maths::enable_if_arith<T>> inline T Log(T x)
	{
		return std::log(x);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Pow(T const& x, T const& y)
	{
		return CompOp(x, y, [](auto x, auto y) { return Pow(x, y); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Exp(T const& x)
	{
		return CompOp(x, [](auto x) { return Exp(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Log10(T const& x)
	{
		return CompOp(x, [](auto x) { return Log10(x); });
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Log(T const& x)
	{
		return CompOp(x, [](auto x) { return Log(x); });
	}

	// Lengths
	template <typename T> constexpr T LenSq(T x, T y)
	{
		return Sqr(x) + Sqr(y);
	}
	template <typename T> constexpr T LenSq(T x, T y, T z)
	{
		return Sqr(x) + Sqr(y) + Sqr(z);
	}
	template <typename T> constexpr T LenSq(T x, T y, T z, T w)
	{
		return Sqr(x) + Sqr(y) + Sqr(z) + Sqr(w);
	}
	template <typename T> inline auto Len(T x, T y) -> decltype(Sqrt(T()))
	{
		return Sqrt(LenSq(x, y));
	}
	template <typename T> inline auto Len(T x, T y, T z) -> decltype(Sqrt(T()))
	{
		return Sqrt(LenSq(x, y, z));
	}
	template <typename T> inline auto Len(T x, T y, T z, T w) -> decltype(Sqrt(T()))
	{
		return Sqrt(LenSq(x, y, z, w));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> constexpr auto LengthSq(T const& x) -> V
	{
		V r = 0;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r += Sqr(x[i]);
		return r;
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> constexpr auto Length(T const& x) -> decltype(Sqrt(V()))
	{
		return Sqrt(LengthSq(x));
	}
	template <typename T> inline T LengthSq(std::complex<T> const& x)
	{
		return Sqr(x.real()) + Sqr(x.imag());
	}
	template <typename T> inline T Length(std::complex<T> const& x)
	{
		return Sqrt(LengthSq(x));
	}

	// Normalise - 2,3,4 variants scale all elements in the vector (consistent with DirectX)
	// Note, due to FP rounding, normalising non-zero vectors can create zero vectors
	template <typename T, typename = maths::enable_if_vN<T>> inline T Normalise(T const& v)
	{
		return v / Length(v);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Normalise(T const& v, T const& def)
	{
		using elem_t = typename maths::is_vec<T>::elem_type;
		if (All(v, [](auto x) { return x == elem_t{}; })) return def;
		return Normalise(v);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsNormal(T const& v)
	{
		return FEql(LengthSq(v), 1.0f);
	}

	// Min/Max element (i.e. nearest to -inf/+inf)
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T MinElement(T v)
	{
		return v;
	}
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T MaxElement(T v)
	{
		return v;
	}
	template <typename T, typename V = maths::is_vec<T>::cp_type, typename = maths::enable_if_vN<T>> constexpr V MinElement(T const& v)
	{
		auto min = MinElement(v[0]);
		int i = 1, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) min = Min(min, MinElement(v[i]));
		return min;
	}
	template <typename T, typename V = maths::is_vec<T>::cp_type, typename = maths::enable_if_vN<T>> constexpr V MaxElement(T const& v)
	{
		auto max = MaxElement(v[0]);
		int i = 1, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) max = Max(max, MaxElement(v[i]));
		return max;
	}
	template <typename T> inline T MinElement(std::span<T> const& a)
	{
		if (a.empty()) throw std::runtime_error("minimum undefined on zero length span");
		auto min = MinElement(a[0]);
		int i = 1, iend = static_cast<int>(a.size());
		for (; i != iend; ++i) min = Min(min, MinElement(a[i]));
		return min;
	}
	template <typename T> inline T MaxElement(std::span<T> const& a)
	{
		if (a.empty()) throw std::runtime_error("maximum undefined on zero length span");
		auto max = MaxElement(a[0]);
		int i = 1, iend = static_cast<int>(a.size());
		for (; i != iend; ++i) max = Max(max, MaxElement(a[i]));
		return max;
	}

	// Min/Max absolute element (i.e. nearest to 0/+inf) (needed when T is a large array)
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T MinElementAbs(T v)
	{
		return Abs(v);
	}
	template <typename T, typename = maths::enable_if_arith<T>> constexpr T MaxElementAbs(T v)
	{
		return Abs(v);
	}
	template <typename T, typename V = maths::is_vec<T>::cp_type, typename = maths::enable_if_vN<T>> constexpr V MinElementAbs(T const& v)
	{
		auto min = MinElementAbs(v[0]);
		int i = 1, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) min = Min(min, MinElementAbs(v[i]));
		return min;
	}
	template <typename T, typename V = maths::is_vec<T>::cp_type, typename = maths::enable_if_vN<T>> constexpr V MaxElementAbs(T const& v)
	{
		auto max = MaxElementAbs(v[0]);
		int i = 1, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) max = Max(max, MaxElementAbs(v[i]));
		return max;
	}
	template <typename T> inline T MinElementAbs(std::span<T> const& a)
	{
		if (a.empty()) throw std::runtime_error("minimum undefined on zero length span");
		auto min = MinElementAbs(a[0]);
		int i = 1, iend = static_cast<int>(a.size());
		for (; i != iend; ++i) min = Min(min, MinElementAbs(a[i]));
		return min;
	}
	template <typename T> inline T MaxElementAbs(std::span<T> const& a)
	{
		if (a.empty()) throw std::runtime_error("maximum undefined on zero length span");
		auto max = MaxElementAbs(a[0]);
		int i = 1, iend = static_cast<int>(a.size());
		for (; i != iend; ++i) max = Max(max, MaxElementAbs(a[i]));
		return max;
	}

	// Smallest/Largest element index. Returns the index of the first min/max element if elements are equal.
	template <typename T, typename = maths::enable_if_vN<T>> constexpr int MinElementIndex(T const& v)
	{
		auto idx = 0;
		int i = 1, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i)
		{
			if (v[i] >= v[idx]) continue;
			idx = i;
		}
		return idx;
	}
	template <typename T, typename = maths::enable_if_vN<T>> constexpr int MaxElementIndex(T const& v)
	{
		auto idx = 0;
		int i = 1, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i)
		{
			if (v[i] <= v[idx]) continue;
			idx = i;
		}
		return idx;
	}

	// Sum the elements in a vector
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> inline V Sum(T const& v)
	{
		V r;
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r += v[i];
		return r;
	}

	// Sum the result of applying 'pred' to each element in a vector
	template <typename T, typename Pred, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> inline int Sum(T const& v, Pred pred)
	{
		int r = 0;
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r += pred(v[i]);
		return r;
	}

	// Dot product
	inline int Dot(int a, int b)
	{
		return a * b;
	}
	inline float Dot(float a, float b)
	{
		return a * b;
	}
	inline double Dot(double a, double b)
	{
		return a * b;
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> inline V Dot(T const& a, T const& b)
	{
		V r;
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend; ++i) r[i] = Dot(a[i], b[i]);
		return r;
	}
	
	// Return the normalised fraction that 'x' is in the range ['min', 'max']
	template <typename T, typename = maths::enable_if_vec_cp<T>> inline float Frac(T min, T x, T max)
	{
		assert("Positive definite interval required for 'Frac'" && Abs(max - min) > 0);
		return float(x - min) / (max - min);
	}

	// Linearly interpolate from 'lhs' to 'rhs'
	inline float Lerp(float lhs, float rhs, float frac)
	{
		return lhs + frac * (rhs - lhs);
	}
	inline double Lerp(double lhs, double rhs, double frac)
	{
		return lhs + frac * (rhs - lhs);
	}
	template <typename T, typename U> inline T Lerp(T const& lhs, T const& rhs, U frac)
	{
		return static_cast<T>(lhs + frac * (rhs - lhs));
	}

	// Spherical linear interpolation from 'a' to 'b' for t=[0,1]
	template <typename T, typename = maths::enable_if_vN<T>> inline T Slerp(T const& a, T const& b, float t)
	{
		assert("Cannot spherically interpolate to/from the zero vector" && a != T{} && b != T{});

		auto a_len = Length(a);
		auto b_len = Length(b);
		auto len = Lerp(a_len, b_len, t);
		auto vec = Normalise(((1-t)/a_len) * a  + (t/b_len) * b);
		return len * vec;
	}

	// Quantise a value to a power of two. 'scale' should be a power of 2, i.e. 256, 1024, 2048, etc
	inline float Quantise(float x, int scale)
	{
		return static_cast<int>(x*scale) / float(scale);
	}
	inline double Quantise(double x, int scale)
	{
		return static_cast<int>(x*scale) / double(scale);
	}
	template <typename T, typename = maths::enable_if_fp_vec<T>> inline T Quantise(T const& x, int scale)
	{
		T r;
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
			r[i] = Quantise(x[i], scale);
		return r;
	}

	// Return the cosine of the angle of the triangle apex opposite 'opp'
	template <typename T> inline T CosAngle(T adj0, T adj1, T opp)
	{
		assert("Angle undefined an when adjacent length is zero" && !FEql(adj0,0) && !FEql(adj1,0));
		return Clamp<T>((adj0*adj0 + adj1*adj1 - opp*opp) / (2 * adj0 * adj1), -1, 1);
	}

	// Return the cosine of the angle between two vectors
	template <typename T, typename = maths::enable_if_vN<T>> inline float CosAngle(T const& lhs, T const& rhs)
	{
		assert("CosAngle undefined for zero vectors" && lhs != T{} && rhs != T{});
		return Clamp(Dot(lhs,rhs) / Sqrt(LengthSq(lhs)*LengthSq(rhs)), -1.0f, 1.0f);
	}

	// Return the angle (in radians) of the triangle apex opposite 'opp'
	template <typename T> inline T Angle(T adj0, T adj1, T opp)
	{
		return ACos(CosAngle(adj0, adj1, opp));
	}

	// Return the angle between two vectors
	template <typename T, typename = maths::enable_if_vN<T>> inline float Angle(T const& lhs, T const& rhs)
	{
		return ACos(CosAngle(lhs, rhs));
	}

	// Return the length of a triangle side given by two adjacent side lengths and an angle between them
	template <typename T> inline T Length(T adj0, T adj1, T angle)
	{
		auto len_sq = adj0*adj0 + adj1*adj1 - 2 * adj0 * adj1 * Cos(angle);
		return len_sq > 0 ? Sqrt(len_sq) : 0;
	}

	// Returns 1 if 'hi' is > 'lo' otherwise 0
	template <typename T> T Step(T lo, T hi)
	{
		return lo <= hi ? T(0) : T(1);
	}

	// Returns the 'Hermite' interpolation (3t² - 2t³) between 'lo' and 'hi' for t=[0,1]
	template <typename T> inline T SmoothStep(T lo, T hi, T t)
	{
		if (lo == hi) return lo;
		t = Clamp((t - lo)/(hi - lo), T(0), T(1));
		return t*t*(3 - 2*t);
	}

	// Returns a fifth-order 'Perlin' interpolation (6t^5 - 15t^4 + 10t^3) between 'lo' and 'hi' for t=[0,1]
	template <typename T> inline T SmoothStep2(T lo, T hi, T t)
	{
		if (lo == hi) return lo;
		t = Clamp((t - lo)/(hi - lo), T(0), T(1));
		return t*t*t*(t*(t*6 - 15) + 10);
	}

	// Scale a value on the range [-inf,+inf] to within the range [-1,+1].
	// 'n' is a horizontal scaling factor.
	// If n = 1, [-1,+1] maps to [-0.5, +0.5]
	// If n = 10, [-10,+10] maps to [-0.5, +0.5], etc
	template <typename T> inline T Sigmoid(T x, T n = T(1))
	{
		return T(ATan(x/n) / maths::tau_by_4);
	}

	// Scale a value on the range [0,1] such that f(0) = 0, f(1) = 1, and df(0.5) = 0
	// This is used to weight values so that values near 0.5 are favoured
	inline float UnitCubic(float x)
	{
		return 4.0f * Cube(x - 0.5f) + 0.5f;
	}
	inline double UnitCubic(double x)
	{
		return 4.0 * Cube(x - 0.5) + 0.5;
	}
	template <typename T> inline T UnitCubic(T x)
	{
		return 4.0f * Cube(x - 0.5f) + 0.5f;
	}

	// Low precision reciprocal square root
	inline float Rsqrt0(float x)
	{
		float r;
		#if PR_MATHS_USE_INTRINSICS
		__m128 r0;
		r0 = _mm_load_ss(&x);
		r0 = _mm_rsqrt_ss(r0);
		_mm_store_ss(&r, r0);
		#else
		r = 1.0f / Sqrt(x);
		#endif
		return r;
	}

	// High(er) precision reciprocal square root
	inline float Rsqrt1(float x)
	{
		float r;
		#if PR_MATHS_USE_INTRINSICS
		static const float c0 = 3.0f, c1 = -0.5f;
		__m128 r0,r1;
		r0 = _mm_load_ss(&x);
		r1 = _mm_rsqrt_ss(r0);
		r0 = _mm_mul_ss(r0, r1); // The general 'Newton-Raphson' reciprocal square root recurrence:
		r0 = _mm_mul_ss(r0, r1); // (3 - b * X * X) * (X / 2)
		r0 = _mm_sub_ss(r0, _mm_load_ss(&c0));
		r1 = _mm_mul_ss(r1, _mm_load_ss(&c1));
		r0 = _mm_mul_ss(r0, r1);
		_mm_store_ss(&r, r0);
		#else
		r = 1.0f / Sqrt(x);
		#endif
		return r;
	}

	// Cube root
	inline float Cubert(float x)
	{
		// This works because the integer interpretation of an IEEE 754 float
		// is approximately the log2(x) scaled by 2^23. The basic idea is to
		// use the log2(x) value as the initial guess then do some Newton-Raphson
		// iterations to find the actual root.
		
		if (x == 0)
			return x;

		auto flip_sign = x < 0.0f;
		if (flip_sign) x = -x;
		
		union { float f; unsigned long i; } as;
		as.f = x;
		as.i = (as.i + 2U * 0x3f800000) / 3U;
		auto guess = as.f;

		x *= 1.0f / 3.0f;
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
		return (flip_sign ? -guess : guess);
	}
	inline double Cubert(double x)
	{
		if (x == 0)
			return x;

		auto flip_sign = x < 0.0f;
		if (flip_sign)  x = -x;

		union { double f; unsigned long long i; } as;
		as.f = x;
		as.i = (as.i + 2ULL * 0x3FF0000000000000ULL) / 3ULL;
		auto guess = as.f;

		x *= 1.0 / 3.0;
		guess = (x / (guess*guess) + guess * (2.0 / 3.0));
		guess = (x / (guess*guess) + guess * (2.0 / 3.0));
		guess = (x / (guess*guess) + guess * (2.0 / 3.0));
		guess = (x / (guess*guess) + guess * (2.0 / 3.0));
		guess = (x / (guess*guess) + guess * (2.0 / 3.0));
		return (flip_sign ? -guess : guess);
	}

	// Fast hash
	inline uint Hash(float value, uint max_value)
	{
		const uint32 h = 0x8da6b343; // Arbitrary prime
		int n = static_cast<int>(h * value);
		n = n % max_value;
		if (n < 0) n += max_value;
		return static_cast<uint>(n);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline uint Hash(T const& value, uint max_value)
	{
		const uint32 h[] = { 0x8da6b343, 0xd8163841, 0xcb1ab31f }; // Arbitrary Primes

		int n = 0;
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
			n += static_cast<int>(h[i%_countof(h)] * value[i]);

		n = n % max_value;
		if (n < 0) n += max_value;
		return static_cast<uint>(n);
	}

	// Return the greatest common factor between 'a' and 'b'
	template <typename T, typename = maths::enable_if_intg<T>> inline T GreatestCommonFactor(T a, T b)
	{
		// Uses the Euclidean algorithm. If the greatest common factor is 1, then 'a' and 'b' are co-prime
		while (b) { auto t = b; b = a % b; a = t; }
		return a;
	}

	// Return the least common multiple between 'a' and 'b'
	template <typename T, typename = maths::enable_if_intg<T>> inline T LeastCommonMultiple(T a, T b)
	{
		return (a*b) / GreatestCommonFactor(a,b);
	}

	// Returns the number to add to pad 'size' up to 'alignment'
	template <typename T, typename = maths::enable_if_intg<T>> constexpr T Pad(T size, T alignment)
	{
		return (alignment - (size % alignment)) % alignment;
	}

	// Returns 'size' increased to a multiple of 'alignment'
	template <typename T, typename = maths::enable_if_intg<T>> constexpr T PadTo(T size, T alignment)
	{
		return size + Pad<T>(size, alignment);
	}

	// Function object for generating an arithmetic sequence
	template <typename Type> struct ArithmeticSequence
	{
		// A sequence defined by:
		//  an = a0 + n * step
		//  Sn = (n + 1) * (a0 + an) / 2
		Type a0, step, a;

		ArithmeticSequence(Type initial_value = 0, Type step = 0)
			:a0(initial_value)
			,step(step)
			,a(a0)
		{}
		Type operator()(int n) const
		{
			return static_cast<Type>(a0 + (n - 1) * step);
		}
		Type operator()()
		{
			auto v = a;
			a = static_cast<Type>(a + step);
			return v;
		}
		Type sum(int n) const
		{
			return ArithmeticSum(a0, step, n);
		}
	};
	template <typename Type> constexpr Type ArithmeticSum(Type a0, Type step, int n)
	{
		auto an = a0 + n * step;
		return static_cast<Type>((n + 1) * (a0 + an) / 2);
	}

	// Function object for generating an geometric sequence
	template <typename Type> struct GeometricSequence
	{
		// A sequence defined by:
		//  an = am * r = a0 * r^n, (where m = n - 1)
		//  Sn = a0.(1 - r^(n+1)) / (1 - r)
		Type a0, ratio, a;

		GeometricSequence(Type initial_value = 0, Type ratio = Type(1))
			:a0(initial_value)
			,ratio(ratio)
			,a(a0)
		{}
		Type operator()(int n) const
		{
			return static_cast<Type>(a0 * Pow<double>(ratio, n + 1));
		}
		Type operator()()
		{
			auto v = a;
			a = static_cast<Type>(a * ratio);
			return v;
		}
		Type sum(int n) const
		{
			return GeometricSum(a0, ratio, n);
		}
	};
	template <typename Type> constexpr Type GeometricSum(Type a0, Type ratio, int n)
	{
		auto rn = Pow<double>(ratio, n + 1);
		return static_cast<Type>(a0 * (1 - rn) / (1 - ratio));
	}
}

// Specialise ::std
namespace std
{
	template <typename T, typename = pr::maths::enable_if_vN<T>> inline T min(T const& lhs, T const& rhs)
	{
		T v;
		for (int i = 0, iend = pr::maths::is_vec<T>::dim; i != iend; ++i) { v[i] = min(lhs[i], rhs[i]); }
		return v;
	}
	template <typename T, typename = pr::maths::enable_if_vN<T>> inline T max(T const& lhs, T const& rhs)
	{
		T v;
		for (int i = 0, iend = pr::maths::is_vec<T>::dim; i != iend; ++i) { v[i] = max(lhs[i], rhs[i]); }
		return v;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::maths
{
	PRUnitTest(MathsCoreTests)
	{
		{// Floating point compare
			float const _6dp = 1.000000111e-6f;

			// Regular large numbers - generally not problematic
			PR_CHECK(FEqlRelative(1000000.0f, 1000001.0f, _6dp), true);
			PR_CHECK(FEqlRelative(1000001.0f, 1000000.0f, _6dp), true);
			PR_CHECK(FEqlRelative(1000000.0f, 1000010.0f, _6dp), false);
			PR_CHECK(FEqlRelative(1000010.0f, 1000000.0f, _6dp), false);

			// Negative large numbers
			PR_CHECK(FEqlRelative(-1000000.0f, -1000001.0f, _6dp), true);
			PR_CHECK(FEqlRelative(-1000001.0f, -1000000.0f, _6dp), true);
			PR_CHECK(FEqlRelative(-1000000.0f, -1000010.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-1000010.0f, -1000000.0f, _6dp), false);

			// Numbers around 1
			PR_CHECK(FEqlRelative(1.0000001f, 1.0000002f, _6dp), true);
			PR_CHECK(FEqlRelative(1.0000002f, 1.0000001f, _6dp), true);
			PR_CHECK(FEqlRelative(1.0000020f, 1.0000010f, _6dp), false);
			PR_CHECK(FEqlRelative(1.0000010f, 1.0000020f, _6dp), false);

			// Numbers around -1
			PR_CHECK(FEqlRelative(-1.0000001f, -1.0000002f, _6dp), true);
			PR_CHECK(FEqlRelative(-1.0000002f, -1.0000001f, _6dp), true);
			PR_CHECK(FEqlRelative(-1.0000010f, -1.0000020f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.0000020f, -1.0000010f, _6dp), false);

			// Numbers between 1 and 0
			PR_CHECK(FEqlRelative(0.000000001000001f, 0.000000001000002f, _6dp), true);
			PR_CHECK(FEqlRelative(0.000000001000002f, 0.000000001000001f, _6dp), true);
			PR_CHECK(FEqlRelative(0.000000000100002f, 0.000000000100001f, _6dp), false);
			PR_CHECK(FEqlRelative(0.000000000100001f, 0.000000000100002f, _6dp), false);

			// Numbers between -1 and 0
			PR_CHECK(FEqlRelative(-0.0000000010000001f, -0.0000000010000002f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.0000000010000002f, -0.0000000010000001f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.0000000001000002f, -0.0000000001000001f, _6dp), false);
			PR_CHECK(FEqlRelative(-0.0000000001000001f, -0.0000000001000002f, _6dp), false);

			// Comparisons involving zero
			PR_CHECK(FEqlRelative(+0.0f, +0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.0f, -0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.0f, -0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.000001f, +0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.0f, +0.000001f, _6dp), true);
			PR_CHECK(FEqlRelative(-0.000001f, +0.0f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.0f, -0.000001f, _6dp), true);
			PR_CHECK(FEqlRelative(+0.00001f, +0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+0.0f, +0.00001f, _6dp), false);
			PR_CHECK(FEqlRelative(-0.00001f, +0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+0.0f, -0.00001f, _6dp), false);

			// Comparisons involving extreme values (overflow potential)
			auto float_hi = maths::float_max;
			auto float_lo = maths::float_lowest;
			PR_CHECK(FEqlRelative(float_hi, float_hi, _6dp), true);
			PR_CHECK(FEqlRelative(float_hi, float_lo, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_hi, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_lo, _6dp), true);
			PR_CHECK(FEqlRelative(float_hi, float_hi / 2, _6dp), false);
			PR_CHECK(FEqlRelative(float_hi, float_lo / 2, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_hi / 2, _6dp), false);
			PR_CHECK(FEqlRelative(float_lo, float_lo / 2, _6dp), false);

			// Comparisons involving infinities
			PR_CHECK(FEqlRelative(+maths::float_inf, +maths::float_inf, _6dp), true);
			PR_CHECK(FEqlRelative(-maths::float_inf, -maths::float_inf, _6dp), true);
			PR_CHECK(FEqlRelative(-maths::float_inf, +maths::float_inf, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_inf, +maths::float_max, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_inf, -maths::float_max, _6dp), false);

			// Comparisons involving NaN values
			PR_CHECK(FEqlRelative(maths::float_nan, maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative(maths::float_nan, +0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-0.0f, maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative(maths::float_nan, -0.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+0.0f, maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, +maths::float_inf, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_inf,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, -maths::float_inf, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_inf,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, +maths::float_max, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_max,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, -maths::float_max, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_max,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, +maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_min,  maths::float_nan, _6dp), false);
			PR_CHECK(FEqlRelative( maths::float_nan, -maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_min,  maths::float_nan, _6dp), false);

			// Comparisons of numbers on opposite sides of 0
			PR_CHECK(FEqlRelative(+1.0f, -1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.0f, +1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+1.000000001f, -1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.0f, +1.000000001f, _6dp), false);
			PR_CHECK(FEqlRelative(-1.000000001f, +1.0f, _6dp), false);
			PR_CHECK(FEqlRelative(+1.0f, -1.000000001f, _6dp), false);
			PR_CHECK(FEqlRelative(2 * maths::float_min, 0, _6dp), true);
			PR_CHECK(FEqlRelative(maths::float_min, -maths::float_min, _6dp), false);

			// The really tricky part - comparisons of numbers very close to zero.
			PR_CHECK(FEqlRelative(+maths::float_min, +maths::float_min, _6dp), true);
			PR_CHECK(FEqlRelative(+maths::float_min, -maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_min, +maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_min, 0, _6dp), true);
			PR_CHECK(FEqlRelative(0, +maths::float_min, _6dp), true);
			PR_CHECK(FEqlRelative(-maths::float_min, 0, _6dp), true);
			PR_CHECK(FEqlRelative(0, -maths::float_min, _6dp), true);

			PR_CHECK(FEqlRelative(0.000000001f, -maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(0.000000001f, +maths::float_min, _6dp), false);
			PR_CHECK(FEqlRelative(+maths::float_min, 0.000000001f, _6dp), false);
			PR_CHECK(FEqlRelative(-maths::float_min, 0.000000001f, _6dp), false);
		}
		{// Floating point vector compare
			float arr0[] = {1,2,3,4};
			float arr1[] = {1,2,3,5};
			static_assert(maths::is_vec<decltype(arr0)>::value, "");
			static_assert(maths::is_vec<decltype(arr1)>::value, "");

			PR_CHECK(!Equal(arr0, arr1), true);
		}
		{// FEql arrays
			auto t0 = 0.0f;
			auto t1 = maths::tinyf * 0.5f;
			auto t2 = maths::tinyf * 1.5f;
			float arr0[] = {t0, 0, maths::tinyf, -1};
			float arr1[] = {t1, 0, maths::tinyf, -1};
			float arr2[] = {t2, 0, maths::tinyf, -1};

			PR_CHECK(FEql(arr0, arr1), true ); // Different by 1.000005%
			PR_CHECK(FEql(arr0, arr2), false); // Different by 1.000015%
		}
		{// Finite
			volatile auto f0 = 0.0f;
			volatile auto d0 = 0.0;
			PR_CHECK( IsFinite(1.0f), true);
			PR_CHECK( IsFinite(limits<int>::max()), true);
			PR_CHECK(!IsFinite(1.0f/f0), true);
			PR_CHECK(!IsFinite(0.0/d0), true);
			PR_CHECK(!IsFinite(11, 10), true);

			v4 arr0(0.0f, 1.0f, 10.0f, 1.0f);
			v4 arr1(0.0f, 1.0f, 1.0f/f0, 0.0f/f0);
			PR_CHECK( IsFinite(arr0), true);
			PR_CHECK(!IsFinite(arr1), true);
			PR_CHECK(!All(arr0, [](float x){ return x < 5.0f; }), true);
			PR_CHECK( Any(arr0, [](float x){ return x < 5.0f; }), true);

			m4x4 arr2(arr0,arr0,arr0,arr0);
			m4x4 arr3(arr1,arr1,arr1,arr1);
			PR_CHECK( IsFinite(arr2), true);
			PR_CHECK(!IsFinite(arr3), true);
			PR_CHECK(!All(arr2, [](float x){ return x < 5.0f; }), true);
			PR_CHECK( Any(arr2, [](float x){ return x < 5.0f; }), true);

			iv2 arr4(10,1);
			PR_CHECK( IsFinite(arr4), true);
			PR_CHECK(!All(arr4, [](int x){ return x < 5; }), true);
			PR_CHECK( Any(arr4, [](int x){ return x < 5; }), true);
		}
		{// Abs
			PR_CHECK(Abs(-1.0f) == Abs(-1.0f), true);
			PR_CHECK(Abs(-1.0f) == Abs(+1.0f), true);
			PR_CHECK(Abs(+1.0f) == Abs(+1.0f), true);

			v4 arr0 = {+1,-2,+3,-4};
			v4 arr1 = {-1,+2,-3,+4};
			v4 arr2 = {+1,+2,+3,+4};
			PR_CHECK(Abs(arr0) == Abs(arr1), true);
			PR_CHECK(Abs(arr0) == Abs(arr2), true);
			PR_CHECK(Abs(arr1) == Abs(arr2), true);

			float arr3[] = {+1,-2,+3,-4};
			float arr4[] = {+1,+2,+3,+4};
			auto arr5 = Abs(arr3);
			std::span<float const> span0(arr5);
			std::span<float const> span1(arr4);
			PR_CHECK(FEql(span0, span1), true);

			std::array<float, 5> const arr6 = { 1, 2, 3, 4, 5 };
			std::span<float const> span5(arr6);
		}
		{// Truncate
			v4 arr0 = {+1.1f, -1.2f, +2.8f, -2.9f};
			v4 arr1 = {+1.0f, -1.0f, +2.0f, -2.0f};
			v4 arr2 = {+1.0f, -1.0f, +3.0f, -3.0f};
			v4 arr3 = {+0.1f, -0.2f, +0.8f, -0.9f};

			PR_CHECK(Trunc(1.9f) == 1.0f, true);
			PR_CHECK(Trunc(10000000000000.9) == 10000000000000.0, true);
			PR_CHECK(Trunc(arr0, ETruncType::TowardZero) == arr1, true);
			PR_CHECK(Trunc(arr0, ETruncType::ToNearest ) == arr2, true);
			PR_CHECK(FEql(Frac(arr0), arr3), true);
		}
		{// Any/All
			float arr0[] = {1.0f, 2.0f, 0.0f, -4.0f};
			auto are_zero = [](float x) { return x == 0.0f; };
			auto not_zero = [](float x) { return x != 0.0f; };

			PR_CHECK(!All(arr0, are_zero), true);
			PR_CHECK(!All(arr0, not_zero), true);
			PR_CHECK( Any(arr0, not_zero), true);
			PR_CHECK( Any(arr0, are_zero), true);
		}
		{// Lengths
			PR_CHECK(LenSq(3,4) == 25, true);
			PR_CHECK(LenSq(3,4,5) == 50, true);
			PR_CHECK(LenSq(3,4,5,6) == 86, true);
			PR_CHECK(FEql(Len(3,4), 5.0f), true);
			PR_CHECK(FEql(Len(3,4,5), 7.0710678f), true);
			PR_CHECK(FEql(Len(3,4,5,6), 9.2736185f), true);

			auto arr0 = v4(3,4,5,6);
			PR_CHECK(FEql(Length(arr0.xy), 5.0f), true);
			PR_CHECK(FEql(Length(arr0.xyz), 7.0710678f), true);
			PR_CHECK(FEql(Length(arr0), 9.2736185f), true);
		}
		{// Min/Max/Clamp
			PR_CHECK(Min(1,2,-3,4,-5) == -5, true);
			PR_CHECK(Max(1,2,-3,4,-5) == 4, true);
			PR_CHECK(Clamp(-1,0,10) == 0, true);
			PR_CHECK(Clamp(3,0,10) == 3, true);
			PR_CHECK(Clamp(12,0,10) == 10, true);

			auto arr0 = v4(+1,-2,+3,-4);
			auto arr1 = v4(-1,+2,-3,+4);
			auto arr2 = v4(+0,+0,+0,+0);
			auto arr3 = v4(+2,+2,+2,+2);
			PR_CHECK(Min(arr0, arr1, arr2, arr3) == v4(-1,-2,-3,-4), true);
			PR_CHECK(Max(arr0, arr1, arr2, arr3) == v4(+2,+2,+3,+4), true);
			PR_CHECK(Clamp(arr0, arr2, arr3) == v4(+1,+0,+2,+0), true);
		}
		{// Operators
			auto arr0 = v4(+1,-2,+3,-4);
			auto arr1 = v4(-1,+2,-3,+4);
			PR_CHECK((arr0 == arr1) == !(arr0 != arr1), true);
			PR_CHECK((arr0 != arr1) == !(arr0 == arr1), true);
			PR_CHECK((arr0 <  arr1) == !(arr0 >= arr1), true);
			PR_CHECK((arr0 >  arr1) == !(arr0 <= arr1), true);
			PR_CHECK((arr0 <= arr1) == !(arr0 >  arr1), true);
			PR_CHECK((arr0 >= arr1) == !(arr0 <  arr1), true);

			auto arr2 = v4(+3,+4,+5,+6);
			auto arr3 = v4(+1,+2,+3,+4);
			PR_CHECK(FEql(arr2 + arr3, v4(4,6,8,10)), true);
			PR_CHECK(FEql(arr2 - arr3, v4(2,2,2,2)), true);
			PR_CHECK(FEql(arr2 * 2.0f, v4(6,8,10,12)), true);
			PR_CHECK(FEql(2.0f * arr2, v4(6,8,10,12)), true);
			PR_CHECK(FEql(arr2 / 2.0f, v4(1.5f,2,2.5f,3)), true);
			PR_CHECK(FEql(arr2 % 3.0f, v4(0,1,2,0)), true);
		}
		{// Normalise
			auto arr0 = v4(1,2,3,4);
			PR_CHECK(FEql(Normalise(v4Zero, arr0), arr0), true);
			PR_CHECK(FEql(Normalise(arr0), v4(0.1825742f, 0.3651484f, 0.5477226f, 0.7302967f)), true);

			auto arr1 = v2(1,2);
			PR_CHECK(FEql(Normalise(v2Zero, arr1), arr1), true);
			PR_CHECK(FEql(Normalise(arr1), v2(0.4472136f, 0.8944272f)), true);

			PR_CHECK(IsNormal(Normalise(arr0)), true);
		}
		{// Smallest/Largest element
			int arr0[] = {1,2,3,4,5};
			int arr1[] = {2,1,3,4,5};
			int arr2[] = {2,3,1,4,5};
			int arr3[] = {2,3,4,1,5};
			int arr4[] = {2,3,4,5,1};
			static_assert(std::is_same<maths::is_vec<int[5]>::elem_type, int>::value, "");

			PR_CHECK(MinElement(arr0 ) == 1, true);
			PR_CHECK(MinElement(arr1 ) == 1, true);
			PR_CHECK(MinElement(arr2 ) == 1, true);
			PR_CHECK(MinElement(arr3 ) == 1, true);
			PR_CHECK(MinElement(arr4 ) == 1, true);

			float arr5[] = {1,2,3,4,5};
			float arr6[] = {1,2,3,5,4};
			float arr7[] = {2,3,5,1,4};
			float arr8[] = {2,5,3,4,1};
			float arr9[] = {5,2,3,4,1};
			PR_CHECK(MaxElement(arr5) == 5, true);
			PR_CHECK(MaxElement(arr6) == 5, true);
			PR_CHECK(MaxElement(arr7) == 5, true);
			PR_CHECK(MaxElement(arr8) == 5, true);
			PR_CHECK(MaxElement(arr9) == 5, true);
		}
		{// Smallest/Largest element index
			int arr0[] = {1,2,3,4,5};
			int arr1[] = {2,1,3,4,5};
			int arr2[] = {2,3,1,4,5};
			int arr3[] = {2,3,4,1,5};
			int arr4[] = {2,3,4,5,1};

			PR_CHECK(MinElementIndex(arr0) == 0, true);
			PR_CHECK(MinElementIndex(arr1) == 1, true);
			PR_CHECK(MinElementIndex(arr2) == 2, true);
			PR_CHECK(MinElementIndex(arr3) == 3, true);
			PR_CHECK(MinElementIndex(arr4) == 4, true);

			float arr5[] = {1,2,3,4,5};
			float arr6[] = {1,2,3,5,4};
			float arr7[] = {2,3,5,1,4};
			float arr8[] = {2,5,3,4,1};
			float arr9[] = {5,2,3,4,1};
			PR_CHECK(MaxElementIndex(arr5) == 4, true);
			PR_CHECK(MaxElementIndex(arr6) == 3, true);
			PR_CHECK(MaxElementIndex(arr7) == 2, true);
			PR_CHECK(MaxElementIndex(arr8) == 1, true);
			PR_CHECK(MaxElementIndex(arr9) == 0, true);
		}
		{// Dot
			v3 arr0(1,2,3);
			v3 arr1(2,3,4);
			iv2 arr2(1,2);
			iv2 arr3(3,4);
			PR_CHECK(FEql(Dot(arr0, arr1), 20), true);
			PR_CHECK(Dot(arr2, arr3) == 11, true);
		}
		{// Fraction
			PR_CHECK(FEql(Frac(-5, 2, 5), 7.0f/10.0f), true);
		}
		{// Linear interpolate
			v4 arr0(1,10,100,1000);
			v4 arr1(2,20,200,2000);
			PR_CHECK(FEql(Lerp(arr0, arr1, 0.7f), v4(1.7f, 17, 170, 1700)), true);
		}
		{// Spherical linear interpolate
			PR_CHECK(FEql(Slerp(v4XAxis, 2.0f*v4YAxis, 0.5f), 1.5f*v4::Normal(0.5f,0.5f,0,0)), true);
		}
		{// Quantise
			v4 arr0(1.0f/3.0f, 0.0f, 2.0f, float(maths::tau));
			PR_CHECK(FEql(Quantise(arr0, 1024), v4(0.333f, 0.0f, 2.0f, 6.28222f)), true);
		}
		{// CosAngle
			v2 arr0(1,0);
			v2 arr1(0,1);
			PR_CHECK(FEql(CosAngle(1.0,1.0,maths::root2) - Cos(DegreesToRadians(90.0)), 0), true);
			PR_CHECK(FEql(CosAngle(arr0, arr1)           - Cos(DegreesToRadians(90.0f)), 0), true);
			PR_CHECK(FEql(Angle(1.0,1.0,maths::root2), DegreesToRadians(90.0)), true);
			PR_CHECK(FEql(Angle(arr0, arr1),           DegreesToRadians(90.0f)), true);
			PR_CHECK(FEql(Length(1.0f, 1.0f, DegreesToRadians(90.0f)), float(maths::root2)), true);
		}
		{// Cube Root (32bit)
			auto a = 1.23456789123456789f;
			auto b = Cubert(a * a * a);
			PR_CHECK(FEqlRelative(a,b,0.000001f), true);
		}
		{// Cube Root (64bit)
			auto a = 1.23456789123456789;
			auto b = Cubert(a * a * a);
			PR_CHECK(FEqlRelative(a,b,0.000000000001), true);
		}
		{// Arithmetic sequence
			ArithmeticSequence a(2, 5);
			PR_CHECK(a(), 2);
			PR_CHECK(a(), 7);
			PR_CHECK(a(), 12);
			PR_CHECK(a(), 17);

			PR_CHECK(ArithmeticSum(0, 2, 4), 20);
			PR_CHECK(ArithmeticSum(4, 2, 2), 18);
			PR_CHECK(ArithmeticSum(1, 2, 0), 1);
			PR_CHECK(ArithmeticSum(1, 2, 5), 36);
		}
		{// Geometric sequence
			GeometricSequence g(2, 5);
			PR_CHECK(g(), 2);
			PR_CHECK(g(), 10);
			PR_CHECK(g(), 50);
			PR_CHECK(g(), 250);

			PR_CHECK(GeometricSum(1, 2, 4), 31);
			PR_CHECK(GeometricSum(4, 2, 2), 28);
			PR_CHECK(GeometricSum(1, 3, 0), 1);
			PR_CHECK(GeometricSum(1, 3, 5), 364);
		}
	}
}
#endif

	//inline uint32  High32(uint64 const& i)                                           { return reinterpret_cast<uint32 const*>(&i)[0]; }
	//inline uint32  Low32 (uint64 const& i)                                           { return reinterpret_cast<uint32 const*>(&i)[1]; }
	//inline uint32& High32(uint64& i)                                                 { return reinterpret_cast<uint32*>(&i)[0]; }
	//inline uint32& Low32 (uint64& i)                                                 { return reinterpret_cast<uint32*>(&i)[1]; }
	//inline uint16  High16(uint32 const& i)                                           { return reinterpret_cast<uint16 const*>(&i)[0]; }
	//inline uint16  Low16 (uint32 const& i)                                           { return reinterpret_cast<uint16 const*>(&i)[1]; }
	//inline uint16& High16(uint32& i)                                                 { return reinterpret_cast<uint16*>(&i)[0]; }
	//inline uint16& Low16 (uint32& i)                                                 { return reinterpret_cast<uint16*>(&i)[1]; }
	//inline uint8   High8 (uint16 const& i)                                           { return reinterpret_cast<uint8 const*>(&i)[0]; }
	//inline uint8   Low8  (uint16 const& i)                                           { return reinterpret_cast<uint8 const*>(&i)[1]; }
	//inline uint8&  High8 (uint16& i)                                                 { return reinterpret_cast<uint8*>(&i)[0]; }
	//inline uint8&  Low8  (uint16& i)                                                 { return reinterpret_cast<uint8*>(&i)[1]; }
	//// Predicates
	//namespace maths
	//{
	//	template <typename T> inline bool IsZero   (T const& value) { return value == T(); }
	//	template <typename T> inline bool IsNonZero(T const& value) { return value != T(); }
	//	template <typename T=float> struct Eql    { bool operator()(T const& value) const {return value == x;}  T x; Eql    (T const& x_):x(x_){}};
	//	template <typename T=float> struct NotEql { bool operator()(T const& value) const {return value != x;}  T x; NotEql (T const& x_):x(x_){}};
	//	template <typename T=float> struct Gtr    { bool operator()(T const& value) const {return value >  x;}  T x; Gtr    (T const& x_):x(x_){}};
	//	template <typename T=float> struct Less   { bool operator()(T const& value) const {return value <  x;}  T x; Less   (T const& x_):x(x_){}};
	//	template <typename T=float> struct GtrEq  { bool operator()(T const& value) const {return value >= x;}  T x; GtrEq  (T const& x_):x(x_){}};
	//	template <typename T=float> struct LessEq { bool operator()(T const& value) const {return value <= x;}  T x; LessEq (T const& x_):x(x_){}};
	//}