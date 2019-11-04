//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/common/to.h"
#include "pr/common/fmt.h"
#include "pr/maths/maths.h"
#include "pr/maths/bit_fields.h"
#include "pr/str/string.h"
#include "pr/str/to_string.h"

namespace pr
{
	namespace convert
	{
		// Vector/Matrix to string conversion
		template <typename Str>
		struct VMToString
		{
			using Char = typename string_traits<Str>::value_type;

			template <typename T> static Str To(Vec2<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g"), x.x, x.y);
			}
			template <typename T> static Str To(Vec3<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g"), x.x, x.y, x.z);
			}
			template <typename T> static Str To(Vec4<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g %g"), x.x, x.y, x.z, x.w);
			}
			template <typename T> static Str To(Vec8<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g %g  %g %g %g %g"), x.ang.x, x.ang.y, x.ang.z, x.ang.w, x.lin.x, x.lin.y, x.lin.z, x.lin.w);
			}
			template <typename T> static Str To(IVec2<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%d %d"), x.x, x.y);
			}
			template <typename T> static Str To(IVec4<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%d %d %d %d"), x.x, x.y, x.z, x,w);
			}
			template <typename A, typename B> static Str To(Mat2x2<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To(m.x)+_+To(m.y);
			}
			template <typename A, typename B> static Str To(Mat3x4<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To(m.x.xyz)+_+To(m.y.xyz)+_+To(m.z.xyz);
			}
			template <typename A, typename B> static Str To(Mat4x4<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To(m.x)+_+To(m.y)+_+To(m.z)+_+To(m.w);
			}
			template <typename A, typename B> static Str To(Mat6x8<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To(m[0])+_+To(m[1])+_+To(m[2])+_+To(m[3])+_+To(m[4])+_+To(m[5]);
			}
		};

		// Whatever to vector/matrix conversion
		struct ToV2
		{
			// String to v2
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v2 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				if (end) *end = e;
				return v2{x,y};
			}

			// POINT to v2
			#ifdef _WINDEF_
			static v2 To(POINT const& x)
			{
				return v2(float(x.x), float(x.y));
			}
			#endif
		};
		struct ToV3
		{
			// String to v3
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v3 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				if (end) *end = e;
				return v3(x,y,z);
			}
		};
		struct ToV4
		{
			// String to v4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v4 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				auto w = pr::To<float>(e, &e);
				if (end) *end = e;
				return v4(x,y,z,w);
			}
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v4 To(Str const& s, float w, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				if (end) *end = e;
				return v4(x,y,z,w);
			}
		};
		struct ToV8
		{
			// String to v8
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v8 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto angx = pr::To<float>(s, &e);
				auto angy = pr::To<float>(e, &e);
				auto angz = pr::To<float>(e, &e);
				auto angw = pr::To<float>(e, &e);
				auto linx = pr::To<float>(e, &e);
				auto liny = pr::To<float>(e, &e);
				auto linz = pr::To<float>(e, &e);
				auto linw = pr::To<float>(e, &e);
				if (end) *end = e;
				return v8(angx, angy, angz, angw, linx, liny, linz, linw);
			}
		};
		struct ToIV2
		{
			// String to iv2
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static iv2 To(Str const& s, int radix = 10, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv2(x,y);
			}

			// v2 to iv2
			static iv2 To(v2 const& v)
			{
				return iv2(int(v.x), int(v.y));
			}

			// IRect to iv2
			static iv2 To(IRect const& x)
			{
				return iv2(x.SizeX(), x.SizeY());
			}

			// Win32 primitive types to iv2
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

			// GDI+ types to iv2
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
			// String to iv4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static iv4 To(Str const& s, int radix = 10, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				auto z = pr::To<int>(e, radix, &e);
				auto w = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv4{x,y,z,w};
			}
		};
		struct ToM2X2
		{
			// String to m2x2
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static m2x2 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<v2>(s, &e);
				auto y = pr::To<v2>(e, &e);
				if (end) *end = e;
				return m2x2{x,y};
			}
		};
		struct ToM3X4
		{
			// String to m3x4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static m3x4 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<v4>(s, &e);
				auto y = pr::To<v4>(e, &e);
				auto z = pr::To<v4>(e, &e);
				if (end) *end = e;
				return m3x4{x,y,z};
			}
		};
		struct ToM4X4
		{
			// String to m4x4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static m4x4 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<v4>(s, &e);
				auto y = pr::To<v4>(e, &e);
				auto z = pr::To<v4>(e, &e);
				auto w = pr::To<v4>(e, &e);
				if (end) *end = e;
				return m4x4{x,y,z,w};
			}
		};
		struct ToM6X8
		{
			// String to m6x8
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static m6x8 To(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<v8>(s, &e);
				auto y = pr::To<v8>(e, &e);
				auto z = pr::To<v8>(e, &e);
				auto u = pr::To<v8>(e, &e);
				auto v = pr::To<v8>(e, &e);
				auto w = pr::To<v8>(e, &e);
				if (end) *end = e;
				return m6x8{x,y,z,u,v,w};
			}
		};

		// Whatever to IRect conversion
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
		// Whatever to SIZE conversion
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

		// Whatever to RECT conversion
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

	// Vector/Matrix to std::basic_string
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec2<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec3<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec4<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec8<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, IVec2<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, IVec4<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat2x2<A,B>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat3x4<A,B>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat4x4<A,B>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat6x8<A,B>> :convert::VMToString<std::basic_string<Char>> {};

	// Vector/Matrix to pr::string
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec2<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec3<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec4<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec8<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    IVec2<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    IVec4<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat2x2<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat3x4<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat4x4<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat6x8<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};

	// Whatever to vector/matrix
	template <typename T, typename TFrom> struct Convert<Vec2<T>  , TFrom> :convert::ToV2 {};
	template <typename T, typename TFrom> struct Convert<Vec3<T>  , TFrom> :convert::ToV3 {};
	template <typename T, typename TFrom> struct Convert<Vec4<T>  , TFrom> :convert::ToV4 {};
	template <typename T, typename TFrom> struct Convert<Vec8<T>  , TFrom> :convert::ToV8 {};
	template <typename T, typename TFrom> struct Convert<IVec2<T> , TFrom> :convert::ToIV2 {};
	template <typename T, typename TFrom> struct Convert<IVec4<T> , TFrom> :convert::ToIV4 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat2x2<A,B> , TFrom> :convert::ToM2X2 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat3x4<A,B> , TFrom> :convert::ToM3X4 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat4x4<A,B> , TFrom> :convert::ToM4X4 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat6x8<A,B> , TFrom> :convert::ToM6X8 {};

	// Whatever to IRect
	template <typename TFrom> struct Convert<IRect, TFrom> :convert::ToIRect {};

	#ifdef _WINDEF_
	// Whatever to SIZE
	template <typename TFrom> struct Convert<SIZE, TFrom> :convert::ToSIZE {};
	
	// Whatever to RECT
	template <typename TFrom> struct Convert<RECT, TFrom> :convert::ToRECT {};
	#endif

	// Write a vector to a stream
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec2<T> const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec3<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec4<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec8<T> const& vec)
	{
		return out << vec.ang << " " << vec.lin;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, IVec2<T> const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, IVec4<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat2x2<A,B> const& mat)
	{
		return out << mat.x << " " << mat.y;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat3x4<A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << m.x << sep << m.y << sep << m.z;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat4x4<A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << m.x << sep << m.y << sep << m.z << sep << m.w;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat6x8<A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << mat[0] << sep << mat[1] << sep << mat[2] << sep << mat[3] << sep << mat[4] << sep << mat[5];
	}

	// Convert an integer to a string of 0s and 1s
	template <typename Str, typename Int, typename = std::enable_if_t<std::is_integral_v<Int>>>
	inline Str ToBinary(Int n)
	{
		int const bits = sizeof(Int) * 8;
		Str str;
		string_traits<Str>::resize(str, bits);
		for (int i = 0, j = bits; i != bits;)
			str[i++] = (n & Bit64(--j)) ? '1' : '0';

		return str;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(ToStringTests)
	{
		PR_CHECK(pr::To<std::string>(v4(1,2,3,4)), "1 2 3 4");

		PR_CHECK(pr::To<v2>("2 3"), v2(2,3));
		PR_CHECK(pr::To<v4>("1 2 3 4"), v4(1,2,3,4));

		PR_CHECK(pr::ToBinary<std::string>(uint8_t(0b11001010)), "11001010");
	}
}
#endif