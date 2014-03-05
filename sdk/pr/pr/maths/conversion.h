//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_CONVERSION_H
#define PR_MATHS_CONVERSION_H

#include "pr/common/to.h"
#include "pr/common/fmt.h"
#include "pr/str/tostring.h"
#include "pr/maths/maths.h"

namespace pr
{
	// To<std::string>
	template <> struct Convert<std::string,v2> { static std::string To(v2 const& x) { return pr::Fmt("%f %f",x.x,x.y); } };
	template <> struct Convert<std::string,v4> { static std::string To(v4 const& x) { return pr::Fmt("%f %f %f %f",x.x,x.y,x.z,x.w); } };

	// To<double>
	template <typename TFrom> struct Convert<double,TFrom>
	{
		static double To(char const* s) { char* end; return strtod(s, &end); }
	};

	// To<float>
	template <typename TFrom> struct Convert<float,TFrom>
	{
		static float To(char const* s) { return static_cast<float>(pr::To<double>(s)); }
	};

	// To<v2>
	template <typename TFrom> struct Convert<v2,TFrom>
	{
		#ifdef _WINDEF_
		static v2 To(POINT const& x) { return pr::v2::make(float(x.x), float(x.y)); }
		#endif
	};

	// From<v3>
	template <typename TTo> struct Convert<TTo,v3>
	{
		static TTo To(v3&& from) { return pr::To<TTo>(v.x) + " " + pr::To<TTo>(v.y) + " " + pr::To<TTo>(v.z); }
	};

	// From<v4>
	template <typename TTo> struct Convert<TTo,v4>
	{
		static TTo To(v4&& from) { return pr::To<TTo>(v.x) + " " + pr::To<TTo>(v.y) + " " + pr::To<TTo>(v.z) + " " + pr::To<TTo>(v.w); }
	};

	// From<m3x3>
	template <typename TTo> struct Convert<TTo,m3x3>
	{
		static TTo To(m3x3&& from) { return pr::To<TTo>(v.x.xyz()) + " " + pr::To<TTo>(v.y.xyz()) + " " + pr::To<TTo>(v.z.xyz()); }
	};

	// From<m4x4>
	template <typename TTo> struct Convert<TTo,m4x4>
	{
		static TTo To(m4x4&& from) { return pr::To<TTo>(v.x) + " " + pr::To<TTo>(v.y) + " " + pr::<TTo>(v.z) + " " + pr::To<TTo>(v.w); }
	};

	// To<iv2>
	template <typename TFrom> struct Convert<iv2, TFrom>
	{
		static iv2 To(IRect const& x) { return iv2::make(x.SizeX(), x.SizeY()); }
		#ifdef _WINDEF_
		static iv2 To(RECT const& x) { return iv2::make(x.right - x.left, x.bottom - x.top); }
		static iv2 To(SIZE const& x) { return iv2::make(x.cx, x.cy); }
		#endif
		#ifdef _GDIPLUS_H
		static iv2 To(Gdiplus::Rect  const& x) { return iv2::make(x.Width, x.Height); }
		static iv2 To(Gdiplus::RectF const& x) { return iv2::make(int(x.Width), int(x.Height)); }
		#endif
	};

	// To<IRect>
	template <typename TFrom> struct Convert<IRect,TFrom>
	{
		static IRect To(iv2 const& x) { return IRect::make(0, 0, x.x, x.y); }
		#ifdef _WINDEF_
		static IRect To(RECT const& x) { return IRect::make(x.left, x.top, x.right, x.bottom); }
		static IRect To(SIZE const& x) { return IRect::make(0, 0, x.cx, x.cy); }
		#endif
	};

	#ifdef _WINDEF_

	// To<SIZE>
	template <typename TFrom> struct Convert<SIZE,TFrom>
	{
		static SIZE To(IRect const& x) { SIZE s = {x.width(), x.height()}; return s; }
		static SIZE To(RECT const& x)  { SIZE s = {x.right - x.left, x.bottom - x.top}; return s; }
	};

	// To<RECT>
	template <typename TFrom> struct Convert<RECT,TFrom>
	{
		static RECT To(IRect const& x) { RECT r = {x.left, x.top, x.right, x.bottom}; return r; }
		static RECT To(SIZE const& x)  { RECT r = {0, 0, x.cx, x.cy}; return r; }
	};

	#endif

	#ifdef _GDIPLUS_H

	// To<Gdiplus::Rect>
	template <typename TFrom> struct Convert<Gdiplus::Rect,TFrom>
	{
		static Gdiplus::Rect  To(RECT const& x) { return Gdiplus::Rect(x.left, x.top, x.right - x.left, x.bottom - x.top); }
	};

	// To<Gdiplus::RectF>
	template <typename TFrom> struct Convert<Gdiplus::RectF,TFrom>
	{
		static Gdiplus::RectF To(RECT const& x) { return Gdiplus::RectF(float(rect.left), float(rect.top), float(rect.right - rect.left), float(rect.bottom - rect.top)); }
	};

	#endif


	//template <typename ToType> inline ToType To(v3 const& v)   { static_assert(false, "No conversion from to this type available"); }
	//template <typename ToType> inline ToType To(v4 const& v)   { static_assert(false, "No conversion from to this type available"); }
	//template <typename ToType> inline ToType To(m3x3 const& m) { static_assert(false, "No conversion from to this type available"); }
	//template <typename ToType> inline ToType To(m4x4 const& m) { static_assert(false, "No conversion from to this type available"); }
	//template <>                inline std::string To<std::string>(v3 const& v)   { return To<std::string>(v.x)       + " " + To<std::string>(v.y)       + " " + To<std::string>(v.z); }
	//template <>                inline std::string To<std::string>(v4 const& v)   { return To<std::string>(v.xyz())   + " " + To<std::string>(v.w); }
	//template <>                inline std::string To<std::string>(m3x3 const& m) { return To<std::string>(m.x.xyz()) + " " + To<std::string>(m.y.xyz()) + " " + To<std::string>(m.z.xyz()); }
	//template <>                inline std::string To<std::string>(m4x4 const& m) { return To<std::string>(m.x)       + " " + To<std::string>(m.y)       + " " + To<std::string>(m.z)        + " " + To<std::string>(m.w); }
	//
	//template <typename ToType> inline ToType      To             (char const* s, char const** end, int radix = 10) { static_assert(false, "No conversion from to this type available"); } 
	//template <>                inline double      To<double>     (char const* s, char const** end, int)            { return                       strtod (s, (char**)end); }
	//template <>                inline double      To<double>     (char const* s)                                   { return To<double>(s, 0); }
	//template <>                inline float       To<float>      (char const* s, char const** end, int)            { return static_cast<float>   (strtod (s, (char**)end)); }
	//template <>                inline float       To<float>      (char const* s)                                   { return To<float>(s, 0); }
	//template <>                inline int         To<int>        (char const* s, char const** end, int radix)      { return static_cast<int>     (strtol (s, (char**)end, radix)); }
	//template <>                inline pr::uint    To<pr::uint>   (char const* s, char const** end, int radix)      { return static_cast<pr::uint>(strtoul(s, (char**)end, radix)); }
	//template <> inline pr::v4 To<pr::v4>     (char const* s, char const** end, int)
	//{
	//	pr::v4 v;  float* f = v.ToArray();  char const* str = s;
	//	for (int i = 0; i != 4; ++i) *f++ = To<float>(str, &str);
	//	if (end) *end = str;
	//	return v;
	//}
	//template <> inline pr::m4x4 To<pr::m4x4>   (char const* s, char const** end, int)
	//{
	//	pr::m4x4 m;  pr::v4* v = m.ToArray();  char const* str = s;
	//	for (int i = 0; i != 4; ++i) *v++ = To<pr::v4>(str, &str);
	//	if (end) *end = str;
	//	return m;
	//}

//
//#include "pr/common/min_max_fix.h"
//#include <windows.h>
//#include <gdiplus.h>
//#include "pr/maths/maths.h"
//
//namespace pr
//{
//	inline long GetX(POINT const& pt) { return pt.x; }
//	inline long GetY(POINT const& pt) { return pt.y; }
//	inline long GetX(SIZE const& sz) { return sz.cx; }
//	inline long GetY(SIZE const& sz) { return sz.cy; }
//	
//	// Convert from pr::v2
//	template <typename ToType> inline ToType    To         (pr::v2 const& pt);
//	template <>                inline POINT     To<POINT>  (pr::v2 const& pt) { POINT p = {int(pt.x), int(pt.y)}; return p; }
//	
//	// Convert from pr::iv2
//	template <typename ToType> inline ToType    To         (pr::iv2 const& pt);
//	template <>                inline POINT     To<POINT>  (pr::iv2 const& pt) { POINT p = {pt.x, pt.y}; return p; }
//	template <>                inline SIZE      To<SIZE>   (pr::iv2 const& sz) { SIZE  s = {sz.x, sz.y}; return s; }
//	
//	// Convert from pr::IRect
//	template <typename ToType> inline ToType    To         (pr::IRect const& rect);
//	template <>                inline RECT      To<RECT>   (pr::IRect const& rect) { RECT r = {rect.m_min.x, rect.m_min.y, rect.m_max.x, rect.m_max.y}; return r; }
//	
//	// Convert from POINT
//	template <typename ToType> inline ToType    To         (POINT const& pt);
//	template <>                inline pr::iv2   To<pr::iv2>(POINT const& pt) { return pr::iv2::make(pt.x, pt.y); }
//	template <>                inline SIZE      To<SIZE>   (POINT const& pt) { SIZE s = {pt.x, pt.y}; return s; }
//	
//	// Convert from SIZE
//	template <typename ToType> inline ToType    To         (SIZE const& pt);
//	template <>                inline pr::iv2   To<pr::iv2>(SIZE const& sz) { return pr::iv2::make(sz.cx, sz.cy); }
//	template <>                inline pr::v2    To<pr::v2> (SIZE const& sz) { return pr::v2::make(float(sz.cx), float(sz.cy)); }
//	template <>                inline RECT      To<RECT>   (SIZE const& sz) { RECT r = {0, 0, sz.cx, sz.cy}; return r; }
//	
//	// Convert from RECT
//	template <typename ToType> inline ToType         To                 (RECT const& rect);
//	template <>                inline pr::IRect      To<pr::IRect>      (RECT const& rect) { return pr::IRect::make(rect.left, rect.top, rect.right, rect.bottom); }
//	template <>                inline SIZE           To<SIZE>           (RECT const& rect) { SIZE s = {rect.right - rect.left, rect.bottom - rect.top}; return s; }
//	template <>                inline Gdiplus::Rect  To<Gdiplus::Rect>  (RECT const& rect) { return Gdiplus::Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top); }
//	template <>                inline Gdiplus::RectF To<Gdiplus::RectF> (RECT const& rect) { return Gdiplus::RectF(float(rect.left), float(rect.top), float(rect.right - rect.left), float(rect.bottom - rect.top)); }
//
//	// Convert from Gdiplus::Rect
//	template <typename ToType> inline ToType    To       (Gdiplus::Rect const& rect);
//	template <>                inline RECT      To<RECT> (Gdiplus::Rect const& rect) { RECT r = {rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height}; return r; }
//}


	// Convert an integer to a string of binary
	template <typename T> inline std::string ToBinary(T n)
	{
		int const bits = sizeof(T) * 8;
		std::string str; str.reserve(bits);
		for (int i = bits; i-- != 0;)
			str.push_back((n & Bit64(i)) ? '1' : '0');
		return str;
	}
	
	// Write a vector to a stream
	template <typename Stream> inline Stream& operator << (Stream& out, pr::v2 const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Stream> inline Stream& operator << (Stream& out, pr::v4 const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
}

#endif
