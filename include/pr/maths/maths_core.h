//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"

namespace pr
{
	#pragma region Traits
	namespace maths
	{
		template <typename T, int N> struct is_vec<T[N]> :is_vec_cp<T>
		{
			using value_type = T;
			static int const dim = N;
		};
		static_assert(!is_vec<char* >::value, "");
		static_assert(!is_vec<wchar_t* >::value, "");
		static_assert(!is_vec<float* >::value, "");
		static_assert(!is_vec<int*  >::value, "");
		static_assert(is_vec<float[2]>::value, "");
		static_assert(is_vec<int[2] >::value, "");
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

	// Equality
	template <typename T, typename = maths::enable_if_vec_cp<T>> inline bool Equal(T lhs, T rhs)
	{
		return lhs == rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool Equal(T const& lhs, T const& rhs)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && Equal(lhs[i], rhs[i]); ++i) {}
		return i == iend;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline bool Equal2(T const& lhs, T const& rhs)
	{
		return
			x_cp(lhs) == x_cp(rhs) &&
			y_cp(lhs) == y_cp(rhs);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline bool Equal3(T const& lhs, T const& rhs)
	{
		return
			x_cp(lhs) == x_cp(rhs) &&
			y_cp(lhs) == y_cp(rhs) &&
			z_cp(lhs) == z_cp(rhs);
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline bool Equal4(T const& lhs, T const& rhs)
	{
		return
			x_cp(lhs) == x_cp(rhs) &&
			y_cp(lhs) == y_cp(rhs) &&
			z_cp(lhs) == z_cp(rhs) &&
			w_cp(lhs) == w_cp(rhs);
	}

	// Floating point comparisons
	inline bool FGtr(float a, float b, float tol = maths::tiny)
	{
		return a - b > tol;
	}
	inline bool FGtrEql(float a, float b, float tol = maths::tiny)
	{
		return a - b > -tol;
	}
	inline bool FLess(float a, float b, float tol = maths::tiny)
	{
		return !FGtrEql(a, b, tol);
	}
	inline bool FLessEql(float a, float b, float tol = maths::tiny)
	{
		return !FGtr(a, b, tol);
	}
	inline bool FEql(float a, float b, float tol = maths::tiny)
	{
		return !FGtr(a, b, tol) && !FLess(a, b, tol);
	}
	inline bool FGtr(double a, double b, double tol = maths::tiny)
	{
		return a - b > tol;
	}
	inline bool FGtrEql(double a, double b, double tol = maths::tiny)
	{
		return a - b > -tol;
	}
	inline bool FLess(double a, double b, double tol = maths::tiny)
	{
		return !FGtrEql(a, b, tol);
	}
	inline bool FLessEql(double a, double b, double tol = maths::tiny)
	{
		return !FGtr(a, b, tol);
	}
	inline bool FEql(double a, double b, double tol = maths::tiny)
	{
		return !FGtr(a, b, tol) && !FLess(a, b, tol);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool FEql(T const& a, T const& b, float tol = maths::tiny)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && FEql(a[i],b[i],tol); ++i) {}
		return i == iend;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline bool FEql2(T const& lhs, T const& rhs, float tol = maths::tiny)
	{
		return
			FEql(x_as<float>(lhs), x_as<float>(rhs), tol) &&
			FEql(y_as<float>(lhs), y_as<float>(rhs), tol);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline bool FEql3(T const& lhs, T const& rhs, float tol = maths::tiny)
	{
		return
			FEql(x_as<float>(lhs), x_as<float>(rhs), tol) &&
			FEql(y_as<float>(lhs), y_as<float>(rhs), tol) &&
			FEql(z_as<float>(lhs), z_as<float>(rhs), tol);
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline bool FEql4(T const& lhs, T const& rhs, float tol = maths::tiny)
	{
		return
			FEql(x_as<float>(lhs), x_as<float>(rhs), tol) &&
			FEql(y_as<float>(lhs), y_as<float>(rhs), tol) &&
			FEql(z_as<float>(lhs), z_as<float>(rhs), tol) &&
			FEql(w_as<float>(lhs), w_as<float>(rhs), tol);
	}

	// Zero test
	template <typename T, typename = maths::enable_if_vec_cp<T>> inline bool IsZero(T x)
	{
		return x == T();
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsZero(T const& v)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && IsZero(v[i]); ++i) {}
		return i == iend;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline bool IsZero2(T const& v)
	{
		return x_cp(v) == 0 && y_cp(v) == 0;
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline bool IsZero3(T const& v)
	{
		return x_cp(v) == 0 && y_cp(v) == 0 && z_cp(v) == 0;
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline bool IsZero4(T const& v)
	{
		return x_cp(v) == 0 && y_cp(v) == 0 && z_cp(v) == 0 && w_cp(v) == 0;
	}

	// Finite test
	inline bool IsFinite(float value)
	{
		return _finite(value) != 0;
	}
	inline bool IsFinite(double value)
	{
		return _finite(value) != 0;
	}
	inline bool IsFinite(float value, float max_value)
	{
		return IsFinite(value) && fabs(value) < max_value;
	}
	inline bool IsFinite(double value, double max_value)
	{
		return IsFinite(value) && fabs(value) < max_value;
	}
	template <typename T, typename = std::enable_if<std::is_arithmetic<T>::value>::type> inline bool IsFinite(T value)
	{
		return value >= limits<T>::lowest() && value <= limits<T>::max();
	}
	template <typename T, typename = std::enable_if<std::is_arithmetic<T>::value>::type> inline bool IsFinite(T value, T max_value)
	{
		return IsFinite(value) && Abs(value) < max_value;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsFinite(T const& value)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && IsFinite(value[i]); ++i) {}
		return i == iend;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsFinite(T const& value, typename maths::is_vec<T>::cp_type max_value)
	{
		int i = 0, iend = maths::is_vec<T>::dim;
		for (; i != iend && IsFinite(value[i], max_value); ++i) {}
		return i == iend;
	}

	// Absolute value
	inline float Abs(float x)
	{
		return fabsf(x);
	}
	inline double Abs(double x)
	{
		return fabs(x);
	}
	inline int Abs(int x)
	{
		return abs(x);
	}
	inline long Abs(long x)
	{
		return labs(x);
	}
	inline int64 Abs(int64 x)
	{
		return _abs64(x);
	}
	inline uint Abs(uint x)
	{
		return x;
	}
	inline ulong Abs(ulong x)
	{
		return x;
	}
	inline uint64 Abs(uint64 x)
	{
		return x;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Abs(T const& v)
	{
		// Note: arrays as vectors cannot use this function because arrays cannot be returned by value
		T r = {};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Abs(v[i]);
		return r;
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline T Abs4(T const& v)
	{
		auto r = v;
		r[0] = Abs(x_cp(v));
		r[1] = Abs(y_cp(v));
		r[2] = Abs(z_cp(v));
		r[3] = Abs(w_cp(v));
		return r;
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline T Abs3(T const& v)
	{
		auto r = v;
		r[0] = Abs(x_cp(v));
		r[1] = Abs(y_cp(v));
		r[2] = Abs(z_cp(v));
		return r;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline T Abs2(T const& v)
	{
		auto r = v;
		r[0] = Abs(x_cp(v));
		r[1] = Abs(y_cp(v));
		return r;
	}

	// Sign, returns +1 if positive, -1 if negative, 0 if zero or a mixture of +1,-1
	template <typename T, typename = maths::enable_if_vec_cp<T>> inline T Sign(T x)
	{
		return x > 0 ? T(+1) : x < 0 ? T(-1) : T(0);
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline int Sign2(T const& v)
	{
		int p = (x_cp(v) > 0) + (y_cp(v) > 0);
		int n = (x_cp(v) < 0) + (y_cp(v) < 0);
		return (p == 2) - (n == 2);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline int Sign3(T const& v)
	{
		int p = (x_cp(v) > 0) + (y_cp(v) > 0) + (z_cp(v) > 0);
		int n = (x_cp(v) < 0) + (y_cp(v) < 0) + (z_cp(v) < 0);
		return (p == 3) - (n == 3);
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline int Sign4(T const& v)
	{
		int n = (x_cp(v) < 0) + (y_cp(v) < 0) + (z_cp(v) < 0) + (w_cp(v) < 0);
		int p = (x_cp(v) > 0) + (y_cp(v) > 0) + (z_cp(v) > 0) + (w_cp(v) > 0);
		return (p == 4) - (n == 4);
	}
	template <typename T> inline T Sign(bool positive) // Converts bool to +1,-1 (note: no 0 value)
	{
		return positive ? static_cast<T>(1) : static_cast<T>(-1);
	}

	// Truncate value
	enum class ETruncType { TowardZero, ToNearest };
	inline float Trunc(float x, ETruncType ty = ETruncType::TowardZero)
	{
		switch (ty)
		{
		default: assert("Unknown truncation type" && false); return x;
		case ETruncType::ToNearest:  return static_cast<float>(static_cast<int>(x + Sign(x)*0.5f));
		case ETruncType::TowardZero: return static_cast<float>(static_cast<int>(x));
		}
	}
	inline double Trunc(double x, ETruncType ty = ETruncType::TowardZero)
	{
		switch (ty)
		{
		default: assert("Unknown truncation type" && false); return x;
		case ETruncType::ToNearest:  return static_cast<double>(static_cast<long long>(x + Sign(x)*0.5f));
		case ETruncType::TowardZero: return static_cast<double>(static_cast<long long>(x));
		}
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Trunc(T const& x, ETruncType ty = ETruncType::TowardZero)
	{
		T r = {};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
			r[i] = Trunc(x[i], ty);
		return r;
	}

	// Fractional part
	inline float Frac(float x)
	{
		float n;
		return modff(x, &n);
	}
	inline double Frac(double x)
	{
		double n;
		return modf(x, &n);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Frac(T const& x)
	{
		T r = {};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
			r[i] = Frac(x[i]);
		return r;
	}

	// Square a value
	inline float Sqr(float x)
	{
		return x * x;
	}
	inline double Sqr(double x)
	{
		return x * x;
	}
	inline long Sqr(long x)
	{
		assert("Overflow" && Abs(x) <= 46340L);
		return x * x;
	}
	inline long long Sqr(long long x)
	{
		assert("Overflow" && Abs(x) <= 3037000499LL);
		return x * x;
	}
	template <typename T> inline T Sqr(T const& x)
	{
		return x * x;
	}

	// Square root - if X*X = Y, then Sqrt(Y) = X. i.e. the inverse T::operator *().
	inline float Sqrt(float x)
	{
		assert("Sqrt of negative or undefined value" && x >= 0 && IsFinite(x));
		return sqrtf(x);
	}
	inline float Sqrt(int x)
	{
		return Sqrt(static_cast<float>(x));
	}
	inline float Sqrt(long x)
	{
		return Sqrt(static_cast<float>(x));
	}
	inline double Sqrt(double x)
	{
		assert("Sqrt of negative or undefined value" && x >= 0 && IsFinite(x));
		return sqrt(x);
	}
	inline double Sqrt(int64 x)
	{
		return Sqrt(static_cast<double>(x));
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Sqrt(T const& x)
	{
		// Sqrt is ill-defined for non-square vectors
		// Matrices have an overload that finds the matrix whose product is 'x'.
		static_assert(false, "Sqrt is not defined for general vector types");
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T SqrtCmp(T const& x) // Component Sqrt
	{
		T r = {};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Sqrt(x[i]);
		return r;
	}

	// Scalar functions
	inline float DegreesToRadians(float degrees)
	{
		return degrees * 1.74532e-2f;
	}
	inline float RadiansToDegrees(float radians)
	{
		return radians * 5.72957e+1f;
	}
	inline float Ceil(float x)
	{
		return ceilf(x);
	}
	inline float Floor(float x)
	{
		return floorf(x);
	}
	inline float Sin(float x)
	{
		return sinf(x);
	}
	inline float Cos(float x)
	{
		return cosf(x);
	}
	inline float Tan(float x)
	{
		return tanf(x);
	}
	inline float ASin(float x)
	{
		return asinf(x);
	}
	inline float ACos(float x)
	{
		return acosf(x);
	}
	inline float ATan(float x)
	{
		return atanf(x);
	}
	inline float ATan2(float y, float x)
	{
		return atan2f(y, x);
	}
	inline float ATan2Positive(float y, float x)
	{
		float a = atan2f(y, x);
		return a < 0.0f ? a += maths::tau : a;
	}
	inline float Sinh(float x)
	{
		return sinhf(x);
	}
	inline float Cosh(float x)
	{
		return coshf(x);
	}
	inline float Tanh(float x)
	{
		return tanhf(x);
	}
	inline float Pow(float x, float y)
	{
		return powf(x, y);
	}
	inline int Pow2(int n)
	{
		return 1 << n;
	}
	inline float Fmod(float x, float y)
	{
		return fmodf(x, y);
	}
	inline float Exp(float x)
	{
		return expf(x);
	}
	inline float Log10(float x)
	{
		return log10f(x);
	}
	inline float Log(float x)
	{
		return logf(x);
	}

	// Test all components pass 'Pred'
	template <typename T, typename Pred, typename = maths::enable_if_v2<T>> inline bool Any2(T const& v, Pred pred)
	{
		return
			pred(x_cp(v)) ||
			pred(y_cp(v));
	}
	template <typename T, typename Pred, typename = maths::enable_if_v3<T>> inline bool Any3(T const& v, Pred pred)
	{
		return
			pred(x_cp(v)) ||
			pred(y_cp(v)) ||
			pred(z_cp(v));
	}
	template <typename T, typename Pred, typename = maths::enable_if_v4<T>> inline bool Any4(T const& v, Pred pred)
	{
		return
			pred(x_cp(v)) ||
			pred(y_cp(v)) ||
			pred(z_cp(v)) ||
			pred(w_cp(v));
	}
	template <typename T, typename Pred, typename = maths::enable_if_v2<T>> inline bool All2(T const& v, Pred pred)
	{
		return
			pred(x_cp(v)) &&
			pred(y_cp(v));
	}
	template <typename T, typename Pred, typename = maths::enable_if_v3<T>> inline bool All3(T const& v, Pred pred)
	{
		return
			pred(x_cp(v)) &&
			pred(y_cp(v)) &&
			pred(z_cp(v));
	}
	template <typename T, typename Pred, typename = maths::enable_if_v4<T>> inline bool All4(T const& v, Pred pred)
	{
		return
			pred(x_cp(v)) &&
			pred(y_cp(v)) &&
			pred(z_cp(v)) &&
			pred(w_cp(v));
	}

	// Lengths
	template <typename T> inline T Len2Sq(T x, T y)
	{
		return Sqr(x) + Sqr(y);
	}
	template <typename T> inline T Len3Sq(T x, T y, T z)
	{
		return Sqr(x) + Sqr(y) + Sqr(z);
	}
	template <typename T> inline T Len4Sq(T x, T y, T z, T w)
	{
		return Sqr(x) + Sqr(y) + Sqr(z) + Sqr(w);
	}
	template <typename T> inline auto Len2(T x, T y) -> decltype(Sqrt(T()))
	{
		return Sqrt(Len2Sq(x, y));
	}
	template <typename T> inline auto Len3(T x, T y, T z) -> decltype(Sqrt(T()))
	{
		return Sqrt(Len3Sq(x, y, z));
	}
	template <typename T> inline auto Len4(T x, T y, T z, T w) -> decltype(Sqrt(T()))
	{
		return Sqrt(Len4Sq(x, y, z, w));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> inline V LengthSq(T const& x)
	{
		auto r = V{};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r += Sqr(x[i]);
		return r;
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_v2<T>> inline V Length2Sq(T const& x)
	{
		return Len2Sq(x_cp(x), y_cp(x));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_v3<T>> inline V Length3Sq(T const& x)
	{
		return Len3Sq(x_cp(x), y_cp(x), z_cp(x));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_v4<T>> inline V Length4Sq(T const& x)
	{
		return Len4Sq(x_cp(x), y_cp(x), z_cp(x), w_cp(x));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> inline auto Length(T const& x) -> decltype(Sqrt(V()))
	{
		return Sqrt(LengthSq(x));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_v2<T>> inline auto Length2(T const& x) -> decltype(Sqrt(V()))
	{
		return Sqrt(Length2Sq(x));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_v3<T>> inline auto Length3(T const& x) -> decltype(Sqrt(V()))
	{
		return Sqrt(Length3Sq(x));
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_v4<T>> inline auto Length4(T const& x) -> decltype(Sqrt(V()))
	{
		return Sqrt(Length4Sq(x));
	}

	// Min/Max/Clamp
	template <typename T, typename = maths::enable_if_not_vN<T>> inline T Min(T x, T y)
	{
		return (x > y) ? y : x;
	}
	template <typename T, typename = maths::enable_if_not_vN<T>> inline T Max(T x, T y)
	{
		return (x > y) ? x : y;
	}
	template <typename T, typename = maths::enable_if_not_vN<T>> inline T Clamp(T x, T mn, T mx)
	{
		assert("[min,max] must be a positive range" && mn <= mx);
		return (mx < x) ? mx : (x < mn) ? mn : x;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Min(T const& x, T const& y)
	{
		auto r = T{};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Min(x[i], y[i]);
		return r;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Max(T const& x, T const& y)
	{
		auto r = T{};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Max(x[i], y[i]);
		return r;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Clamp(T const& x, T const& mn, T const& mx)
	{
		auto r = T{};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Clamp(x[i], mn[i], mx[i]);
		return r;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Clamp(T const& x, typename maths::is_vec<T>::cp_type mn, typename maths::is_vec<T>::cp_type mx)
	{
		auto r = T{};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Clamp(x[i], mn, mx);
		return r;
	}
	template <typename T, typename... A> inline T Min(T const& x, T const& y, A&&... a)
	{
		return Min(Min(x,y), std::forward<A>(a)...);
	}
	template <typename T, typename... A> inline T Max(T const& x, T const& y, A&&... a)
	{
		return Max(Max(x,y), std::forward<A>(a)...);
	}

	// Operators
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
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator + (T const& lhs, T const& rhs)
	{
		auto v = lhs;
		return v += rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator - (T const& lhs, T const& rhs)
	{
		auto v = lhs;
		return v -= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator * (T const& lhs, T const& rhs)
	{
		auto v = lhs;
		return v *= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator / (T const& lhs, T const& rhs)
	{
		auto v = lhs;
		return v /= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator % (T const& lhs, T const& rhs)
	{
		auto v = lhs;
		return v %= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator * (T const& lhs, typename maths::is_vec<T>::cp_type rhs)
	{
		auto v = lhs;
		return v *= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator / (T const& lhs, typename maths::is_vec<T>::cp_type rhs)
	{
		auto v = lhs;
		return v /= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator % (T const& lhs, typename maths::is_vec<T>::cp_type rhs)
	{
		auto v = lhs;
		return v %= rhs;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T operator * (typename maths::is_vec<T>::cp_type lhs, T const& rhs)
	{
		return rhs * lhs;
	}

	// Normalise - 2,3,4 variants scale all elements in the vector (consistent with DirectX)
	template <typename T, typename = maths::enable_if_vN<T>> inline T Normalise(T const& v)
	{
		return v / Length(v);
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline T Normalise(T const& v, T const& def)
	{
		return !IsZero(v) ? Normalise(v) : def;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline T Normalise2(T const& v)
	{
		return v / Length2(v);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline T Normalise3(T const& v)
	{
		return v / Length3(v);
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline T Normalise4(T const& v)
	{
		return v / Length4(v);
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline T Normalise2(T const& v, T const& def)
	{
		return !IsZero2(v) ? Normalise2(v) : def;
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline T Normalise3(T const& v, T const& def)
	{
		return !IsZero3(v) ? Normalise3(v) : def;
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline T Normalise4(T const& v, T const& def)
	{
		return !IsZero4(v) ? Normalise4(v) : def;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline bool IsNormal(T const& v)
	{
		return FEql(LengthSq(v), 1.0f);
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline bool IsNormal2(T const& v)
	{
		return FEql(Length2Sq(v), 1.0f);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline bool IsNormal3(T const& v)
	{
		return FEql(Length3Sq(v), 1.0f);
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline bool IsNormal4(T const& v)
	{
		return FEql(Length4Sq(v), 1.0f);
	}

	// Smallest/Largest element
	template <typename T, typename = maths::enable_if_vN<T>> inline int SmallestElement(T const& v)
	{
		int idx = 0;
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
		{
			if (v[i] >= v[idx]) continue;
			idx = i;
		}
		return idx;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline int SmallestElement2(T const& v)
	{
		return x_cp(v) > y_cp(v);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline int SmallestElement3(T const& v)
	{
		int i = (y_cp(v) > z_cp(v)) + 1;
		return (x_cp(v) > v[i]) * i;
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline int SmallestElement4(T const& v)
	{
		int i = (x_cp(v) > y_cp(v)) + 0;
		int j = (z_cp(v) > w_cp(v)) + 2;
		return (v[i] > v[j]) ? j : i;
	}
	template <typename T, typename = maths::enable_if_vN<T>> inline int LargestElement(T const& v)
	{
		int idx = 0;
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
		{
			if (v[i] <= v[idx]) continue;
			idx = i;
		}
		return idx;
	}
	template <typename T, typename = maths::enable_if_v2<T>> inline int LargestElement2(T const& v)
	{
		return x_cp(v) < y_cp(v);
	}
	template <typename T, typename = maths::enable_if_v3<T>> inline int LargestElement3(T const& v)
	{
		int i = (y_cp(v) < z_cp(v)) + 1;
		return (x_cp(v) < v[i]) * i;
	}
	template <typename T, typename = maths::enable_if_v4<T>> inline int LargestElement4(T const& v)
	{
		int i = (x_cp(v) < y_cp(v)) + 0;
		int j = (z_cp(v) < w_cp(v)) + 2;
		return (v[i] < v[j]) ? j : i;
	}

	// Dot product
	inline float Dot(float a, float b)
	{
		return a * b;
	}
	inline int Dot(int a, int b)
	{
		return a * b;
	}
	template <typename T, typename V = maths::is_vec<T>::elem_type, typename = maths::enable_if_vN<T>> inline V Dot(T const& a, T const& b)
	{
		auto r = V{}
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i) r[i] = Dot(a[i], b[i]);
		return r;
	}
	
	// Return the normalised fraction that 'x' is in the range ['min', 'max']
	template <typename T, typename = maths::enable_if_vec_cp<T>> inline float Frac(T min, T x, T max)
	{
		assert("Positive definite interval required for 'Frac'" && Abs(max - min) > 0);
		return float(x - min) / (max - min);
	}

	// Linearly interpolate from 'lhs' to 'rhs'
	template <typename T> inline T Lerp(T const& lhs, T const& rhs, float frac)
	{
		return lhs + frac * (rhs - lhs);
	}

	// Spherical linear interpolation from 'a' to 'b' for t=[0,1]
	template <typename T, typename = maths::enable_if_vN<T>> inline T Slerp(T const& a, T const& b, float t)
	{
		assert("Cannot spherically interpolate to/from the zero vector" && !IsZero(a) && !IsZero(b));

		auto a_len = Length(a);
		auto b_len = Length(b);
		auto len = Lerp(a_len, b_len, t);
		auto vec = Normalise(((1-t)/a_len) * a  + (t/b_len) * b);
		return len * vec;
	}

	// Quantise a value to a power of two. 'scale' should be a power of 2, i.e. 256, 1024, 2048, etc
	inline float Quantise(float x, int scale)
	{
		return static_cast<int>(x*scale) / static_cast<float>(scale);
	}
	template <typename T, typename = maths::enable_if_fp_vec<T>> inline T Quantise(T const& x, int scale)
	{
		auto r = T{};
		for (int i = 0, iend = maths::is_vec<T>::dim; i != iend; ++i)
			r[i] = Quantise(x[i], scale);
		return r;
	}

	// Return the cosine of the angle of the triangle apex opposite 'opp'
	inline float CosAngle(float adj0, float adj1, float opp)
	{
		assert("Angle undefined an when adjacent length is zero" && !FEql(adj0,0) && !FEql(adj1,0));
		return Clamp((adj0*adj0 + adj1*adj1 - opp*opp) / (2.0f * adj0 * adj1), -1.0f, 1.0f);
	}

	// Return the cosine of the angle between two vectors
	template <typename T, typename = maths::enable_if_vN<T>> inline float CosAngle(T const& lhs, T const& rhs)
	{
		assert("CosAngle undefined for zero vectors" && !IsZero(lhs) && !IsZero(rhs));
		return Clamp(Dot(lhs,rhs) / Sqrt(LengthSq(lhs)*LengthSq(rhs)), -1.0f, 1.0f);
	}

	// Return the angle (in radians) of the triangle apex opposite 'opp'
	inline float Angle(float adj0, float adj1, float opp)
	{
		return ACos(CosAngle(adj0, adj1, opp));
	}

	// Return the angle between two vectors
	template <typename T, typename = maths::enable_if_vN<T>> inline float Angle(T const& lhs, T const& rhs)
	{
		return ACos(CosAngle(lhs, rhs));
	}

	// Return the length of a triangle side given by two adjacent side lengths and an angle between them
	inline float Length(float adj0, float adj1, float angle)
	{
		float len_sq = adj0*adj0 + adj1*adj1 - 2.0f * adj0 * adj1 * Cos(angle);
		return len_sq > 0.0f ? Sqrt(len_sq) : 0.0f;
	}

	// Returns 1.0f if 'hi' is > 'lo' otherwise 0.0f
	inline float Step(float lo, float hi)
	{
		return lo <= hi ? 0.0f : 1.0f;
	}

	// Returns the 'Hermite' interpolation (3t² - 2t³) between 'lo' and 'hi' for t=[0,1]
	inline float SmoothStep(float lo, float hi, float t)
	{
		if (lo == hi) return lo;
		t = Clamp((t - lo)/(hi - lo), 0.0f, 1.0f);
		return t*t*(3 - 2*t);
	}

	// Returns a fifth-order 'Perlin' interpolation (6t^5 - 15t^4 + 10t^3) between 'lo' and 'hi' for t=[0,1]
	inline float SmoothStep2(float lo, float hi, float t)
	{
		if (lo == hi) return lo;
		t = Clamp((t - lo)/(hi - lo), 0.0f, 1.0f);
		return t*t*t*(t*(t*6 - 15) + 10);
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
		union { float f; uint32 i; } as;
		bool flip_sign = x < 0.0f;
		if (flip_sign)  x = -x;
		if (x == 0.0f) return x;
		as.f = x;
		uint32 bits = as.i;

		bits = (bits + (uint32)2 * 0x3f800000) / 3;

		as.i = bits;
		float guess = as.f;

		x *= 1.0f / 3.0f;
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
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
	template <typename T, typename = std::enable_if<std::is_integral<T>::value>::type> inline T GreatestCommonFactor(T a, T b)
	{
		// Uses the Euclidean algorithm. If the greatest common factor is 1, then 'a' and 'b' are co-prime
		while (b) { auto t = b; b = a % b; a = t; }
		return a;
	}

	// Return the least common multiple between 'a' and 'b'
	template <typename T, typename = std::enable_if<std::is_integral<T>::value>::type> inline T LeastCommonMultiple(T a, T b)
	{
		return (a*b) / GreatestCommonFactor(a,b);
	}

	// Returns the number to add to pad 'size' up to 'alignment'
	template <typename T> inline T Pad(T size, T alignment)
	{
		return (alignment - (size % alignment)) % alignment;
	}

	// Function object for generating an arithmetic sequence
	template <typename Type> struct ArithmeticSequence
	{
		Type m_value;
		Type m_step;

		ArithmeticSequence(Type initial_value = 0, Type step = 0)
			:m_value(initial_value)
			,m_step(step)
		{}
		Type operator()()
		{
			Type v = m_value;
			m_value = static_cast<Type>(m_value + m_step);
			return v;
		}
	};

	// Function object for generating an geometric sequence
	template <typename Type> struct GeometricSequence
	{
		Type m_value;
		Type m_ratio;

		GeometricSequence(Type initial_value = 0, Type ratio = Type(1))
			:m_value(initial_value)
			,m_ratio(ratio)
		{}
		Type operator()()
		{
			Type v = m_value;
			m_value = static_cast<Type>(m_value * m_ratio);
			return v;
		}
	};
}

#if PR_UNITTESTS
#include "pr/maths/maths.h"
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_maths_core)
		{
			{// Floating point compare
				float arr0[] = {1,2,3,4};
				float arr1[] = {1,2,3,5};
				static_assert(maths::is_vec<decltype(arr0)>::value, "");
				static_assert(maths::is_vec<decltype(arr1)>::value, "");

				PR_CHECK(!Equal(arr0, arr1), true);
				PR_CHECK( Equal2(arr0, arr1), true);
				PR_CHECK( Equal3(arr0, arr1), true);
				PR_CHECK(!Equal4(arr0, arr1), true);

				PR_CHECK( FGtr    (+1.0f, +0.9f) && !FGtr    (+1.0f, +1.0f) && !FGtr    (+0.9f, +1.0f) &&  FGtr    (-0.9f, -1.0f) && !FGtr    (-1.0f, -1.0f) && !FGtr    (-1.0f, -0.9f), true);
				PR_CHECK( FGtrEql (+1.0f, +0.9f) &&  FGtrEql (+1.0f, +1.0f) && !FGtrEql (+0.9f, +1.0f) &&  FGtrEql (-0.9f, -1.0f) &&  FGtrEql (-1.0f, -1.0f) && !FGtrEql (-1.0f, -0.9f), true);
				PR_CHECK(!FLess   (+1.0f, +0.9f) && !FLess   (+1.0f, +1.0f) &&  FLess   (+0.9f, +1.0f) && !FLess   (-0.9f, -1.0f) && !FLess   (-1.0f, -1.0f) &&  FLess   (-1.0f, -0.9f), true);
				PR_CHECK(!FLessEql(+1.0f, +0.9f) &&  FLessEql(+1.0f, +1.0f) &&  FLessEql(+0.9f, +1.0f) && !FLessEql(-0.9f, -1.0f) &&  FLessEql(-1.0f, -1.0f) &&  FLessEql(-1.0f, -0.9f), true);
				PR_CHECK(!FEql    (+1.0f, +0.9f) &&  FEql    (+1.0f, +1.0f) && !FEql    (+0.9f, +1.0f) && !FEql    (-0.9f, -1.0f) &&  FEql    (-1.0f, -1.0f) && !FEql    (-1.0f, -0.9f), true);

				auto t0 = 0.0f;
				auto t1 = maths::tiny * 0.5f;
				auto t2 = maths::tiny * 1.5f;
				float arr2[] = {1.0f + t0, 2.0f + t0, 3.0f + t0, 5.0f + t0};
				float arr3[] = {1.0f + t1, 2.0f + t1, 3.0f + t1, 4.0f + t1};
				float arr4[] = {1.0f + t2, 2.0f + t2, 3.0f + t2, 4.0f + t2};
				PR_CHECK( FEql2(arr2, arr3) && !FEql2(arr2, arr4), true);
				PR_CHECK( FEql3(arr2, arr3) && !FEql3(arr2, arr4), true);
				PR_CHECK(!FEql4(arr2, arr3) && !FEql4(arr2, arr4), true);

				float arr5[] = {t0, t0, t0, t0};
				float arr6[] = {t0, t0, t1, t1};
				PR_CHECK( IsZero(arr5), true);
				PR_CHECK(!IsZero(arr6), true);
				PR_CHECK( IsZero2(arr5), true);
				PR_CHECK( IsZero2(arr6), true);
				PR_CHECK( IsZero3(arr5), true);
				PR_CHECK(!IsZero3(arr6), true);
				PR_CHECK( IsZero4(arr5), true);
				PR_CHECK(!IsZero4(arr6), true);
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
				PR_CHECK(!IsFinite(arr0, 5.0f), true);

				m4x4 arr2(arr0,arr0,arr0,arr0);
				m4x4 arr3(arr1,arr1,arr1,arr1);
				PR_CHECK( IsFinite(arr2), true);
				PR_CHECK(!IsFinite(arr3), true);
				PR_CHECK(!IsFinite(arr2, 5.0f), true);

				iv2 arr4(10,1);
				PR_CHECK( IsFinite(arr4), true);
				PR_CHECK(!IsFinite(arr4, 5), true);
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

				PR_CHECK(!Any2(arr0, are_zero), true);
				PR_CHECK( Any3(arr0, are_zero), true);
				PR_CHECK( Any4(arr0, are_zero), true);
				PR_CHECK(!All2(arr0, are_zero), true);
				PR_CHECK(!All3(arr0, are_zero), true);
				PR_CHECK(!All4(arr0, are_zero), true);

				PR_CHECK( Any2(arr0, not_zero), true);
				PR_CHECK( Any3(arr0, not_zero), true);
				PR_CHECK( Any4(arr0, not_zero), true);
				PR_CHECK( All2(arr0, not_zero), true);
				PR_CHECK(!All3(arr0, not_zero), true);
				PR_CHECK(!All4(arr0, not_zero), true);
			}
			{// Lengths
				PR_CHECK(Len2Sq(3,4) == 25, true);
				PR_CHECK(Len3Sq(3,4,5) == 50, true);
				PR_CHECK(Len4Sq(3,4,5,6) == 86, true);
				PR_CHECK(FEql(Len2(3,4), 5.0f), true);
				PR_CHECK(FEql(Len3(3,4,5), 7.0710678f), true);
				PR_CHECK(FEql(Len4(3,4,5,6), 9.2736185f), true);

				auto arr0 = v4(3,4,5,6);
				PR_CHECK(FEql(Length2(arr0), 5.0f), true);
				PR_CHECK(FEql(Length3(arr0), 7.0710678f), true);
				PR_CHECK(FEql(Length4(arr0), 9.2736185f), true);
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
				PR_CHECK(FEql(Normalise2(arr0), v4(0.4472136f, 0.8944272f, 1.3416407f, 1.7888543f)), true);
				PR_CHECK(FEql(Normalise3(arr0), v4(0.2672612f, 0.5345225f, 0.8017837f, 1.0690449f)), true);
				PR_CHECK(FEql(Normalise4(arr0), v4(0.1825742f, 0.3651484f, 0.5477226f, 0.7302967f)), true);

				auto arr1 = v2(1,2);
				PR_CHECK(FEql(Normalise(v2Zero, arr1), arr1), true);
				PR_CHECK(FEql(Normalise2(arr1), v2(0.4472136f, 0.8944272f)), true);

				PR_CHECK(IsNormal(Normalise(arr0)), true);
				PR_CHECK(IsNormal2(Normalise2(arr0)), true);
				PR_CHECK(IsNormal3(Normalise3(arr0)), true);
				PR_CHECK(IsNormal4(Normalise4(arr0)), true);
			}
			{// Smallest/Largest element
				int arr0[] = {1,2,3,4,5};
				int arr1[] = {2,1,3,4,5};
				int arr2[] = {2,3,1,4,5};
				int arr3[] = {2,3,4,1,5};
				int arr4[] = {2,3,4,5,1};

				PR_CHECK(SmallestElement(arr0) == 0, true);
				PR_CHECK(SmallestElement(arr1) == 1, true);
				PR_CHECK(SmallestElement(arr2) == 2, true);
				PR_CHECK(SmallestElement(arr3) == 3, true);
				PR_CHECK(SmallestElement(arr4) == 4, true);
				PR_CHECK(SmallestElement2(arr1) == 1, true);
				PR_CHECK(SmallestElement3(arr3) == 0, true);
				PR_CHECK(SmallestElement4(arr4) == 0, true);

				float arr5[] = {1,2,3,4,5};
				float arr6[] = {1,2,3,5,4};
				float arr7[] = {2,3,5,1,4};
				float arr8[] = {2,5,3,4,1};
				float arr9[] = {5,2,3,4,1};
				PR_CHECK(LargestElement(arr5) == 4, true);
				PR_CHECK(LargestElement(arr6) == 3, true);
				PR_CHECK(LargestElement(arr7) == 2, true);
				PR_CHECK(LargestElement(arr8) == 1, true);
				PR_CHECK(LargestElement(arr9) == 0, true);
				PR_CHECK(LargestElement2(arr5) == 1, true);
				PR_CHECK(LargestElement3(arr5) == 2, true);
				PR_CHECK(LargestElement4(arr5) == 3, true);
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
				PR_CHECK(FEql(Slerp(v4XAxis, 2.0f*v4YAxis, 0.5f), 1.5f*v4::Normal4(0.5f,0.5f,0,0)), true);
			}
			{// Quantise
				v4 arr0(1.0f/3.0f, 0.0f, 2.0f, maths::tau);
				PR_CHECK(FEql(Quantise(arr0, 1024), v4(0.333f, 0.0f, 2.0f, 6.28222f)), true);
			}
			{// CosAngle
				v2 arr0(1,0);
				v2 arr1(0,1);
				PR_CHECK(FEql(CosAngle(1,1,1.4142135f), Cos(DegreesToRadians(90.0f))), true);
				PR_CHECK(FEql(CosAngle(arr0, arr1), Cos(DegreesToRadians(90.0f))), true);
				PR_CHECK(FEql(Angle(1,1,1.4142135f), DegreesToRadians(90.0f)), true);
				PR_CHECK(FEql(Angle(arr0, arr1), DegreesToRadians(90.0f)), true);
				PR_CHECK(FEql(Length(1.0f, 1.0f, DegreesToRadians(90.0f)), 1.4142135f), true);
			}
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