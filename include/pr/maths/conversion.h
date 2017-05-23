//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/common/to.h"
#include "pr/common/fmt.h"
#include "pr/str/to_string.h"
#include "pr/maths/maths.h"

namespace pr
{
	namespace convert
	{
		template <typename Str, typename Char = typename Str::value_type>
		struct VMToString
		{
			// To String
			static Str To(v2 const& x) { return pr::Fmt(PR_STRLITERAL(Char, "%g %g"), x.x, x.y); }
			static Str To(v3 const& x) { return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g"), x.x, x.y, x.z); }
			static Str To(v4 const& x) { return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g %g"), x.x, x.y, x.z, x.w); }
			static Str To(iv2 const& x) { return pr::Fmt(PR_STRLITERAL(Char, "%d %d"), x.x, x.y); }
			static Str To(iv4 const& x) { return pr::Fmt(PR_STRLITERAL(Char, "%d %d %d %d"), x.x, x.y, x.z, x,w); }
			static Str To(m2x2 const& m) { Char const _[] = {' ','\0'}; return To(m.x) + _ + To(m.y); }
			static Str To(m3x4 const& m) { Char const _[] = {' ','\0'}; return To(m.x.xyz) + _ + To(m.y.xyz) + _ + To(m.z.xyz); }
			static Str To(m4x4 const& m) { Char const _[] = {' ','\0'}; return To(m.x) + _ + To(m.y) + _ + To(m.z) + _ + To(m.w); }
		};
		struct ToV2
		{
			template <typename Char> static v2 To(Char const* s, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				if (end) *end = e;
				return v2::make(x,y);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static v2 To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
			#ifdef _WINDEF_
			static v2 To(POINT const& x)
			{
				return v2(float(x.x), float(x.y));
			}
			#endif
		};
		struct ToV3
		{
			template <typename Char> static v3 To(Char const* s, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				if (end) *end = e;
				return v3(x,y,z);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static v3 To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
		};
		struct ToV4
		{
			template <typename Char> static v4 To(Char const* s, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				auto w = pr::To<float>(e, &e);
				if (end) *end = e;
				return v4(x,y,z,w);
			}
			template <typename Char> static v4 To(Char const* s, float w, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				if (end) *end = e;
				return v4(x,y,z,w);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static v4 To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static v4 To(Str const& s, float w, Char** end = nullptr)
			{
				return To(s.c_str(), w, end);
			}
		};
		struct ToIV2
		{
			template <typename Char> static iv2 To(Char const* s, int radix = 10, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv2::make(x,y);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static iv2 To(Str const& s, Char** end = nullptr, int radix = 10)
			{
				return To(s.c_str(), end, radix);
			}

			static iv2 To(IRect const& x)
			{
				return iv2(x.SizeX(), x.SizeY());
			}

			#ifdef _WINDEF_
			static iv2 To(POINT const& x)
			{
				return iv2(x.x, x.y);
			}
			static iv2 To(RECT const& x)
			{
				return iv2(x.right - x.left, x.bottom - x.top);
			}
			static iv2 To(SIZE const& x)
			{
				return iv2(x.cx, x.cy);
			}
			#endif
			#ifdef _GDIPLUS_H
			static iv2 To(Gdiplus::Rect const& x)
			{
				return iv2(x.Width, x.Height);
			}
			static iv2 To(Gdiplus::RectF const& x)
			{
				return iv2(int(x.Width), int(x.Height));
			}
			#endif
		};
		struct ToIV4
		{
			template <typename Char> static iv4 To(Char const* s, int radix = 10, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				auto z = pr::To<int>(e, radix, &e);
				auto w = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv4::make(x,y,z,w);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static iv4 To(Str const& s, int radix = 10, Char** end = nullptr)
			{
				return To(s.c_str(), radix, end);
			}
		};
		struct ToM2X2
		{
			template <typename Char> static m2x2 To(Char const* s, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<v2>(s, &e);
				auto y = pr::To<v2>(e, &e);
				if (end) *end = e;
				return m2x2::make(x,y);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static m2x2 To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
		};
		struct ToM3X4
		{
			template <typename Char> static m3x4 To(Char const* s, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<v4>(s, &e);
				auto y = pr::To<v4>(e, &e);
				auto z = pr::To<v4>(e, &e);
				if (end) *end = e;
				return m3x4::make(x,y,z);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static m3x4 To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
		};
		struct ToM4X4
		{
			template <typename Char> static m4x4 To(Char const* s, Char** end = nullptr)
			{
				Char* e;
				auto x = pr::To<v4>(s, &e);
				auto y = pr::To<v4>(e, &e);
				auto z = pr::To<v4>(e, &e);
				auto w = pr::To<v4>(e, &e);
				if (end) *end = e;
				return m4x4::make(x,y,z,w);
			}
			template <typename Str, typename Char = Str::value_type, typename = enable_if_str_class<Str>>
			static m4x4 To(Str const& s, Char** end = nullptr)
			{
				return To(s.c_str(), end);
			}
		};
		struct ToIRect
		{
			static IRect To(iv2 const& x)
			{
				return IRect(0, 0, x.x, x.y);
			}
			#ifdef _WINDEF_
			static IRect To(RECT const& x)
			{
				return IRect(x.left, x.top, x.right, x.bottom);
			}
			static IRect To(SIZE const& x)
			{
				return IRect(0, 0, x.cx, x.cy);
			}
			#endif
		};
		#ifdef _WINDEF_
		struct ToSIZE
		{
			static SIZE To(IRect const& x)
			{
				return SIZE{x.SizeX(), x.SizeY()};
			}
			static SIZE To(RECT const& x)
			{
				return SIZE{x.right - x.left, x.bottom - x.top};
			}
		};
		struct ToRECT
		{
			static RECT To(IRect const& x)
			{
				return RECT{x.m_min.x, x.m_min.y, x.m_max.x, x.m_max.y};
			}
			static RECT To(SIZE const& x)
			{
				return RECT{0, 0, x.cx, x.cy};
			}
		};
		#endif
	}
	template <typename Char>                struct Convert<std::basic_string<Char>, v2  > :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, v3  > :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, v4  > :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, iv2 > :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, iv4 > :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, m2x2> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, m3x4> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char>                struct Convert<std::basic_string<Char>, m4x4> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    v2  > :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    v3  > :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    v4  > :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    iv2 > :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    iv4 > :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    m2x2> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    m3x4> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F> struct Convert<pr::string<Char,L,F>,    m4x4> :convert::VMToString<pr::string<Char,L,F>> {};

	template <typename TFrom> struct Convert<v2   , TFrom> :convert::ToV2 {};
	template <typename TFrom> struct Convert<v3   , TFrom> :convert::ToV3 {};
	template <typename TFrom> struct Convert<v4   , TFrom> :convert::ToV4 {};
	template <typename TFrom> struct Convert<iv2  , TFrom> :convert::ToIV2 {};
	template <typename TFrom> struct Convert<iv4  , TFrom> :convert::ToIV4 {};
	template <typename TFrom> struct Convert<m2x2 , TFrom> :convert::ToM2X2 {};
	template <typename TFrom> struct Convert<m3x4 , TFrom> :convert::ToM3X4 {};
	template <typename TFrom> struct Convert<m4x4 , TFrom> :convert::ToM4X4 {};
	template <typename TFrom> struct Convert<IRect, TFrom> :convert::ToIRect {};

	#ifdef _WINDEF_
	template <typename TFrom> struct Convert<SIZE, TFrom> :convert::ToSIZE {};
	template <typename TFrom> struct Convert<RECT, TFrom> :convert::ToRECT {};
	#endif

	// Write a vector to a stream
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, v2 const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, v3 const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, v4 const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, iv2 const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, iv4 const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, m2x2 const& mat)
	{
		return out << mat.x << " " << mat.y;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, m3x4 const& mat)
	{
		return out << mat.x << " " << mat.y << " " << mat.z;
	}
	template <typename Char> inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, m4x4 const& mat)
	{
		return out << mat.x << " " << mat.y << " " << mat.z << " " << mat.w;
	}
}




	//template <typename ToType> inline ToType To(v3 const& v)   { static_assert(false, "No conversion from to this type available"); }
	//template <typename ToType> inline ToType To(v4 const& v)   { static_assert(false, "No conversion from to this type available"); }
	//template <typename ToType> inline ToType To(m3x4 const& m) { static_assert(false, "No conversion from to this type available"); }
	//template <typename ToType> inline ToType To(m4x4 const& m) { static_assert(false, "No conversion from to this type available"); }
	//template <>                inline std::string To<std::string>(v3 const& v)   { return To<std::string>(v.x)       + " " + To<std::string>(v.y)       + " " + To<std::string>(v.z); }
	//template <>                inline std::string To<std::string>(v4 const& v)   { return To<std::string>(v.xyz())   + " " + To<std::string>(v.w); }
	//template <>                inline std::string To<std::string>(m3x4 const& m) { return To<std::string>(m.x.xyz()) + " " + To<std::string>(m.y.xyz()) + " " + To<std::string>(m.z.xyz()); }
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

