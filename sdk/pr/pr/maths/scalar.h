//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#if !defined(PR_MATHS_SCALER_H)
#define PR_MATHS_SCALER_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"

namespace pr
{
	// Get the 'X','Y','Z', or 'W' value from a scaler
	template <typename T> inline T GetX(T const& x) { return x; }
	template <typename T> inline T GetY(T const& x) { return x; }
	template <typename T> inline T GetZ(T const& x) { return x; }
	template <typename T> inline T GetW(T const& x) { return x; }
	
	template <typename T> inline int   AsInt (T const& x)  { return static_cast<int>(x); }
	template <typename T> inline float AsReal(T const& x) { return static_cast<float>(x); }
	
	template <typename T> inline float GetXf(T const& x) { return AsReal(GetX(x)); }
	template <typename T> inline float GetYf(T const& x) { return AsReal(GetY(x)); }
	template <typename T> inline float GetZf(T const& x) { return AsReal(GetZ(x)); }
	template <typename T> inline float GetWf(T const& x) { return AsReal(GetW(x)); }
	template <typename T> inline int   GetXi(T const& x) { return AsInt(GetX(x)); }
	template <typename T> inline int   GetYi(T const& x) { return AsInt(GetY(x)); }
	template <typename T> inline int   GetZi(T const& x) { return AsInt(GetZ(x)); }
	template <typename T> inline int   GetWi(T const& x) { return AsInt(GetW(x)); }
	
	template <typename FromType, typename ToType> inline ToType To(FromType const& from) { return static_cast<ToType const&>(from); }
	
	inline float   Ceil(float x)                              { return ceilf(x); }
	inline float   Floor(float x)                             { return floorf(x); }
	inline float   Sin(float x)                               { return sinf(x); }
	inline float   Cos(float x)                               { return cosf(x); }
	inline float   Tan(float x)                               { return tanf(x); }
	inline float   ASin(float x)                              { return asinf(x); }
	inline float   ACos(float x)                              { return acosf(x); }
	inline float   ATan(float x)                              { return atanf(x); }
	inline float   ATan2(float y, float x)                    { return atan2f(y, x); }
	inline float   ATan2Positive(float y, float x)            { float a = atan2f(y, x); return (a >= 0.0f)*a + (a < 0.0f)*(maths::tau + a); }
	inline float   Sinh(float x)                              { return sinhf(x); }
	inline float   Cosh(float x)                              { return coshf(x); }
	inline float   Tanh(float x)                              { return tanhf(x); }
	inline float   Pow(float x, float y)                      { return powf(x, y); }
	inline int     Pow2(int n)                                { return 1 << n; }
	inline float   Fmod(float x, float y)                     { return fmodf(x, y); }
	inline float   Exp(float x)                               { return expf(x); }
	inline float   Log10(float x)                             { return log10f(x); }
	inline float   Log(float x)                               { return logf(x); }
	inline double  Abs(double x)                              { return fabs(x); }
	inline float   Abs(float x)                               { return fabsf(x); }
	inline int     Abs(int x)                                 { return abs(x); }
	inline long    Abs(long x)                                { return labs(x); }
	inline __int64 Abs(__int64 x)                             { return _abs64(x); }
	inline uint    Abs(uint x)                                { return x; }
	inline ulong   Abs(ulong x)                               { return x; }
	inline uint64  Abs(uint64 x)                              { return x; }
	inline float   Trunc(float x)                             { return static_cast<float>(static_cast<int>(x)); }
	inline double  Trunc(double x)                            { return static_cast<double>(static_cast<int>(x)); }
	inline float   Frac(float x)                              { float n; return modff(x, &n); }
	inline double  Frac(double x)                             { double n; return modf(x, &n); }
	inline bool    IsFinite(double value)                     { return _finite(value) != 0; }
	inline bool    IsFinite(double value, double max_value)   { return IsFinite(value) && Abs(value) < max_value; }
	inline bool    IsFinite(float value)                      { return _finite(value) != 0; }
	inline bool    IsFinite(float value, float max_value)     { return IsFinite(value) && Abs(value) < max_value; }
	inline bool    IsFinite(int value)                        { value; return true; }
	inline bool    IsFinite(int value, int max_value)         { return IsFinite(value) && Abs(value) < max_value; }
	inline bool    IsFinite(__int64 value)                    { value; return true; }
	inline bool    IsFinite(__int64 value, __int64 max_value) { return IsFinite(value) && Abs(value) < max_value; }

	inline bool    FGtr    (float a, float b, float tol = pr::maths::tiny)                      { return a - b > tol; }
	inline bool    FGtrEql (float a, float b, float tol = pr::maths::tiny)                      { return a - b > -tol; }
	inline bool    FLess   (float a, float b, float tol = pr::maths::tiny)                      { return !FGtrEql(a,b,tol); }
	inline bool    FLessEql(float a, float b, float tol = pr::maths::tiny)                      { return !FGtr(a,b,tol); }
	inline bool    FEql    (float a, float b, float tol = pr::maths::tiny)                      { return !FGtr(a,b,tol) && !FLess(a,b,tol); }
	inline bool    FEqlZero(float a, float tol = pr::maths::tiny)                               { return Abs(a) <= tol; }
	inline bool    FGtr    (double a, double b, double tol = pr::maths::tiny)                   { return a - b > tol; }
	inline bool    FGtrEql (double a, double b, double tol = pr::maths::tiny)                   { return a - b > -tol; }
	inline bool    FLess   (double a, double b, double tol = pr::maths::tiny)                   { return !FGtrEql(a,b,tol); }
	inline bool    FLessEql(double a, double b, double tol = pr::maths::tiny)                   { return !FGtr(a,b,tol); }
	inline bool    FEql    (double a, double b, double tol = pr::maths::tiny)                   { return !FGtr(a,b,tol) && !FLess(a,b,tol); }
	inline bool    FEqlZero(double a, double tol = pr::maths::tiny)                             { return Abs(a) <= tol; }
	
	template <typename T> inline bool   Equal2(T const& lhs, T const& rhs)                      { return GetX(lhs) == GetX(rhs) && GetY(lhs) == GetY(rhs); }
	template <typename T> inline bool   Equal3(T const& lhs, T const& rhs)                      { return Equal2(lhs,rhs) && GetZ(lhs) == GetZ(rhs); }
	template <typename T> inline bool   Equal4(T const& lhs, T const& rhs)                      { return Equal3(lhs,rhs) && GetW(lhs) == GetW(rhs); }
	template <typename T> inline bool   IsZero2(T const& v)                                     { return GetX(v) == 0 && GetY(v) == 0; }
	template <typename T> inline bool   IsZero3(T const& v)                                     { return IsZero2(v) && GetZ(v) == 0; }
	template <typename T> inline bool   IsZero4(T const& v)                                     { return IsZero3(v) && GetW(v) == 0; }

	template <typename T> inline bool   FEql2    (T const& lhs, T const& rhs, float tol = pr::maths::tiny) { return FEql(GetXf(lhs), GetXf(rhs), tol) && FEql(GetYf(lhs), GetYf(rhs), tol); }
	template <typename T> inline bool   FEql3    (T const& lhs, T const& rhs, float tol = pr::maths::tiny) { return FEql2(lhs, rhs, tol) && FEql(GetZf(lhs), GetZf(rhs), tol); }
	template <typename T> inline bool   FEql4    (T const& lhs, T const& rhs, float tol = pr::maths::tiny) { return FEql3(lhs, rhs, tol) && FEql(GetWf(lhs), GetWf(rhs), tol); }
	template <typename T> inline bool   FEqlZero2(T const& lhs, float tol = pr::maths::tiny)               { return Length2Sq(lhs) < Sqr(tol); }
	template <typename T> inline bool   FEqlZero3(T const& lhs, float tol = pr::maths::tiny)               { return Length3Sq(lhs) < Sqr(tol); }
	template <typename T> inline bool   FEqlZero4(T const& lhs, float tol = pr::maths::tiny)               { return Length4Sq(lhs) < Sqr(tol); }
	
	template <typename T, typename Pred> inline bool Any2(T const& v, Pred pred)                { return pred(GetX(v)) || pred(GetY(v)); }
	template <typename T, typename Pred> inline bool Any3(T const& v, Pred pred)                { return Any2(v, pred) || pred(GetZ(v)); }
	template <typename T, typename Pred> inline bool Any4(T const& v, Pred pred)                { return Any3(v, pred) || pred(GetW(v)); }
	template <typename T, typename Pred> inline bool All2(T const& v, Pred pred)                { return pred(GetX(v)) && pred(GetY(v)); }
	template <typename T, typename Pred> inline bool All3(T const& v, Pred pred)                { return All2(v, pred) || pred(GetZ(v)); }
	template <typename T, typename Pred> inline bool All4(T const& v, Pred pred)                { return All3(v, pred) || pred(GetW(v)); }
	
	template <typename T> inline T      Sign(bool positive)                                     { return static_cast<T>(2 * positive - 1); }
	template <typename T> inline T      Sign(T sign)                                            { return static_cast<T>(2 * (sign >= 0) - 1); }
	template <typename T> inline void   Swap(T& x, T& y)                                        { T tmp = x; x = y; y = tmp; }
	
	template <typename T> inline T      Sqr(T x)                                                { return x * x; }
	template <typename T> inline T      Sqrt(T x)                                               { PR_ASSERT(PR_DBG_MATHS, x >= 0 && IsFinite(x), ""); return static_cast<T>(sqrt(static_cast<double>(x))); }
	
	template <typename T> inline T      Len2Sq(T x, T y)                                        { return Sqr(x) + Sqr(y); }
	template <typename T> inline T      Len3Sq(T x, T y, T z)                                   { return Sqr(x) + Sqr(y) + Sqr(z); }
	template <typename T> inline T      Len4Sq(T x, T y, T z, T w)                              { return Sqr(x) + Sqr(y) + Sqr(z) + Sqr(w); }
	template <typename T> inline T      Len2(T x, T y)                                          { return Sqrt(Len2Sq(x, y)); }
	template <typename T> inline T      Len3(T x, T y, T z)                                     { return Sqrt(Len3Sq(x, y, z)); }
	template <typename T> inline T      Len4(T x, T y, T z, T w)                                { return Sqrt(Len4Sq(x, y, z, w)); }
	template <typename T> inline float  Length2Sq(T const& x)                                   { return AsReal(Len2Sq(GetX(x), GetY(x))); }
	template <typename T> inline float  Length3Sq(T const& x)                                   { return AsReal(Len3Sq(GetX(x), GetY(x), GetZ(x))); }
	template <typename T> inline float  Length4Sq(T const& x)                                   { return AsReal(Len4Sq(GetX(x), GetY(x), GetZ(x), GetW(x))); }
	template <typename T> inline float  Length2(T const& x)                                     { return Sqrt(Length2Sq(x)); }
	template <typename T> inline float  Length3(T const& x)                                     { return Sqrt(Length3Sq(x)); }
	template <typename T> inline float  Length4(T const& x)                                     { return Sqrt(Length4Sq(x)); }
	
	template <typename T> inline T      Max  (T const& x, T const& y)                           { return (x > y) ? x : y; }
	template <typename T> inline T      Min  (T const& x, T const& y)                           { return (x > y) ? y : x; }
	template <typename T> inline T      Clamp(T const& x, T const& mn, T const& mx)             { PR_ASSERT(PR_DBG_MATHS, mn <= mx   , ""); return (mx < x) ? mx : (x < mn) ? mn : x; }
	template <>           inline float  Max  (float const& x, float const& y)                   { PR_ASSERT(PR_DBG_MATHS, IsFinite(x), ""); return (x > y) ? x : y; }
	template <>           inline float  Min  (float const& x, float const& y)                   { PR_ASSERT(PR_DBG_MATHS, IsFinite(x), ""); return (x > y) ? y : x; }
	template <>           inline float  Clamp(float const& x, float const& mn, float const& mx) { PR_ASSERT(PR_DBG_MATHS, IsFinite(x) && mn <= mx, ""); return (mx < x) ? mx : (x < mn) ? mn : x; }
	
	template <typename T> inline T&     Normalise2(T& v)                                        { return v /= Length2(v); }
	template <typename T> inline T&     Normalise3(T& v)                                        { return v /= Length3(v); }
	template <typename T> inline T&     Normalise4(T& v)                                        { return v /= Length4(v); }
	template <typename T> inline T      GetNormal2(T const& v)                                  { T x = v; return Normalise2(x); }
	template <typename T> inline T      GetNormal3(T const& v)                                  { T x = v; return Normalise3(x); }
	template <typename T> inline T      GetNormal4(T const& v)                                  { T x = v; return Normalise4(x); }
	template <typename T> inline T&     Normalise2IfNonZero(T& v)                               { return IsZero2(v) ? v : Normalise2(v); }
	template <typename T> inline T&     Normalise3IfNonZero(T& v)                               { return IsZero3(v) ? v : Normalise3(v); }
	template <typename T> inline T&     Normalise4IfNonZero(T& v)                               { return IsZero4(v) ? v : Normalise4(v); }
	template <typename T> inline T      GetNormal2IfNonZero(T const& v)                         { T x = v; return Normalise2IfNonZero(x); }
	template <typename T> inline T      GetNormal3IfNonZero(T const& v)                         { T x = v; return Normalise3IfNonZero(x); }
	template <typename T> inline T      GetNormal4IfNonZero(T const& v)                         { T x = v; return Normalise4IfNonZero(x); }
	template <typename T> inline bool   IsNormal2(T const& v)                                   { return FEql(Length2Sq(v), 1.0f); }
	template <typename T> inline bool   IsNormal3(T const& v)                                   { return FEql(Length3Sq(v), 1.0f); }
	template <typename T> inline bool   IsNormal4(T const& v)                                   { return FEql(Length4Sq(v), 1.0f); }
	
	inline float   DegreesToRadians(float degrees)                                   { return degrees * 1.74532e-2f; }
	inline float   RadiansToDegrees(float radians)                                   { return radians * 5.72957e+1f; }
	inline uint32  High32(uint64 const& i)                                           { return reinterpret_cast<uint32 const*>(&i)[0]; }
	inline uint32  Low32 (uint64 const& i)                                           { return reinterpret_cast<uint32 const*>(&i)[1]; }
	inline uint32& High32(uint64& i)                                                 { return reinterpret_cast<uint32*>(&i)[0]; }
	inline uint32& Low32 (uint64& i)                                                 { return reinterpret_cast<uint32*>(&i)[1]; }
	inline uint16  High16(uint32 const& i)                                           { return reinterpret_cast<uint16 const*>(&i)[0]; }
	inline uint16  Low16 (uint32 const& i)                                           { return reinterpret_cast<uint16 const*>(&i)[1]; }
	inline uint16& High16(uint32& i)                                                 { return reinterpret_cast<uint16*>(&i)[0]; }
	inline uint16& Low16 (uint32& i)                                                 { return reinterpret_cast<uint16*>(&i)[1]; }
	inline uint8   High8 (uint16 const& i)                                           { return reinterpret_cast<uint8 const*>(&i)[0]; }
	inline uint8   Low8  (uint16 const& i)                                           { return reinterpret_cast<uint8 const*>(&i)[1]; }
	inline uint8&  High8 (uint16& i)                                                 { return reinterpret_cast<uint8*>(&i)[0]; }
	inline uint8&  Low8  (uint16& i)                                                 { return reinterpret_cast<uint8*>(&i)[1]; }
	template <typename T> inline T Lerp(T const& src, T const& dest, float frac)     { return static_cast<T>(src + frac * (dest - src)); }
	
	v2      Slerp(const v2& src, const v2& dest, float frac);
	v3      Slerp(const v3& src, const v3& dest, float frac);
	v4      Slerp3(const v4& src, const v4& dest, float frac);
	v4      Slerp4(const v4& src, const v4& dest, float frac);
	float   Rsqrt0(float x);
	float   Rsqrt1(float x);
	float   Cubert(float x);
	uint    Hash(float value, uint max_value);
	uint    Hash(const v4& value, uint max_value);
	float   Quantise(float x, int scale);
	float   CosAngle(float adj0, float adj1, float opp);
	float   Angle(float adj0, float adj1, float opp);
	float   Length(float adj0, float adj1, float angle);
	
	// Function objects for generating sequences
	template <typename Type> struct ArithmeticSequence
	{
		Type m_value, m_step;
		ArithmeticSequence(Type initial_value = 0, Type step = 0) :m_value(initial_value) ,m_step(step) {}
		Type operator()() { Type v = m_value; m_value = static_cast<Type>(m_value + m_step); return v; }
	};
	template <typename Type> struct GeometricSequence
	{
		Type m_value, m_ratio;
		GeometricSequence(Type initial_value = 0, Type ratio = Type(1)) :m_value(initial_value) ,m_ratio(ratio) {}
		Type operator()() { Type v = m_value; m_value = static_cast<Type>(m_value * m_ratio); return v; }
	};
	
	// Return the greatest common factor between 'a' and 'b'
	// Uses the Euclidean algorithm. If the greatest common factor is 1, then 'a' and 'b' are co-prime
	inline int GreatestCommonFactor(int a, int b)
	{
		while (b) { int t = b; b = a % b; a = t; }
		return a;
	}
	
	// Predicates
	namespace maths
	{
		template <typename T> inline bool  Zero   (T const& value)                {return value == T();}
		template <typename T> inline bool  NonZero(T const& value)                {return value != T();}
		template <typename T=float> struct Eql    { bool operator()(T const& value) const {return value == x;}  T x; Eql    (T const& x_):x(x_){}};
		template <typename T=float> struct NotEql { bool operator()(T const& value) const {return value != x;}  T x; NotEql (T const& x_):x(x_){}};
		template <typename T=float> struct Gtr    { bool operator()(T const& value) const {return value >  x;}  T x; Gtr    (T const& x_):x(x_){}};
		template <typename T=float> struct Less   { bool operator()(T const& value) const {return value <  x;}  T x; Less   (T const& x_):x(x_){}};
		template <typename T=float> struct GtrEq  { bool operator()(T const& value) const {return value >= x;}  T x; GtrEq  (T const& x_):x(x_){}};
		template <typename T=float> struct LessEq { bool operator()(T const& value) const {return value <= x;}  T x; LessEq (T const& x_):x(x_){}};
	}
}

#endif
