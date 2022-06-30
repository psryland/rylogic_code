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

// Todo - This needs updating to VecX<S,T>

namespace pr
{
	namespace convert
	{
		// Vector/Matrix to string conversion
		template <typename Str>
		struct VMToString
		{
			using Char = typename string_traits<Str>::value_type;

			template <typename T> static Str To_(Vec2f<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g"), x.x, x.y);
			}
			template <typename T> static Str To_(Vec3f<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g"), x.x, x.y, x.z);
			}
			template <typename T> static Str To_(Vec4f<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g %g"), x.x, x.y, x.z, x.w);
			}
			template <typename T> static Str To_(Vec8f<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%g %g %g %g  %g %g %g %g"), x.ang.x, x.ang.y, x.ang.z, x.ang.w, x.lin.x, x.lin.y, x.lin.z, x.lin.w);
			}
			template <typename T> static Str To_(Vec2i<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%d %d"), x.x, x.y);
			}
			template <typename T> static Str To_(Vec3i<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%d %d %d"), x.x, x.y, x.z);
			}
			template <typename T> static Str To_(Vec4i<T> const& x)
			{
				return pr::Fmt(PR_STRLITERAL(Char, "%d %d %d %d"), x.x, x.y, x.z, x.w);
			}
			template <typename A, typename B> static Str To_(Mat2x2f<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To_(m.x)+_+To_(m.y);
			}
			template <typename A, typename B> static Str To_(Mat3x4f<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To_(m.x.xyz)+_+To_(m.y.xyz)+_+To_(m.z.xyz);
			}
			template <typename A, typename B> static Str To_(Mat4x4f<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To_(m.x)+_+To_(m.y)+_+To_(m.z)+_+To_(m.w);
			}
			template <typename A, typename B> static Str To_(Mat6x8f<A,B> const& m)
			{
				Char const _[] = {' ','\0'};
				return To_(m[0])+_+To_(m[1])+_+To_(m[2])+_+To_(m[3])+_+To_(m[4])+_+To_(m[5]);
			}
		};

		// Whatever to vector/matrix conversion
		struct ToV2f
		{
			// iv2 to v2
			static v2 To_(iv2 const& v)
			{
				return v2(static_cast<float>(v.x), static_cast<float>(v.y));
			}

			// String to v2
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v2 To_(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				if (end) *end = e;
				return v2{x,y};
			}

			// POINT to v2
			#ifdef _WINDEF_
			static v2 To_(POINT const& x)
			{
				return v2(static_cast<float>(x.x), static_cast<float>(x.y));
			}
			#endif
		};
		struct ToV3f
		{
			// iv3 to v3
			static v3 To_(iv3 const& v)
			{
				return v3(static_cast<float>(v.x), static_cast<float>(v.y), static_cast<float>(v.z));
			}

			// String to v3
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v3 To_(Str const& s, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				if (end) *end = e;
				return v3(x,y,z);
			}
		};
		struct ToV4f
		{
			// iv4 to v4
			static v4 To_(iv4 const& v)
			{
				return v4(static_cast<float>(v.x), static_cast<float>(v.y), static_cast<float>(v.z), static_cast<float>(v.w));
			}

			// String to v4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v4 To_(Str const& s, Char const** end = nullptr)
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
			static v4 To_(Str const& s, float w, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<float>(s, &e);
				auto y = pr::To<float>(e, &e);
				auto z = pr::To<float>(e, &e);
				if (end) *end = e;
				return v4(x,y,z,w);
			}
		};
		struct ToV8f
		{
			// String to v8
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static v8 To_(Str const& s, Char const** end = nullptr)
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
		struct ToV2i
		{
			// String to iv2
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static iv2 To_(Str const& s, int radix = 10, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv2(x,y);
			}

			// v2 to iv2
			static iv2 To_(v2 const& v)
			{
				return iv2(int(v.x), int(v.y));
			}

			// IRect to iv2
			static iv2 To_(IRect const& x)
			{
				return iv2(x.SizeX(), x.SizeY());
			}

			// Win32 primitive types to iv2
			#ifdef _WINDEF_
			static iv2 To_(POINT const& x)
			{
				return iv2(x.x, x.y);
			}
			static iv2 To_(RECT const& x)
			{
				return iv2(x.right - x.left, x.bottom - x.top);
			}
			static iv2 To_(SIZE const& x)
			{
				return iv2(x.cx, x.cy);
			}
			#endif

			// GDI+ types to iv2
			#ifdef _GDIPLUS_H
			static iv2 To_(Gdiplus::Rect const& x)
			{
				return iv2(x.Width, x.Height);
			}
			static iv2 To_(Gdiplus::RectF const& x)
			{
				return iv2(int(x.Width), int(x.Height));
			}
			#endif
		};
		struct ToV3i
		{
			// String to iv4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static iv3 To_(Str const& s, int radix = 10, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				auto z = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv3{x,y,z};
			}
		};
		struct ToV4i
		{
			// String to iv4
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static iv4 To_(Str const& s, int radix = 10, Char const** end = nullptr)
			{
				Char const* e;
				auto x = pr::To<int>(s, radix, &e);
				auto y = pr::To<int>(e, radix, &e);
				auto z = pr::To<int>(e, radix, &e);
				auto w = pr::To<int>(e, radix, &e);
				if (end) *end = e;
				return iv4{x,y,z,w};
			}

			// Vec4f to Vec4i
			template <typename T>
			static Vec4i<T> To_(v4_cref<T> v)
			{
				return Vec4i<T> {
					static_cast<int>(v.x),
					static_cast<int>(v.y),
					static_cast<int>(v.z),
					static_cast<int>(v.w)};
			}
		};
		struct ToM2X2
		{
			// String to m2x2
			template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
			static m2x2 To_(Str const& s, Char const** end = nullptr)
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
			static m3x4 To_(Str const& s, Char const** end = nullptr)
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
			static m4x4 To_(Str const& s, Char const** end = nullptr)
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
			static m6x8 To_(Str const& s, Char const** end = nullptr)
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
			static IRect To_(iv2 const& x)
			{
				return IRect(0, 0, x.x, x.y);
			}
			static IRect To_(FRect const& x)
			{
				return IRect(static_cast<int>(x.m_min.x), static_cast<int>(x.m_min.y), static_cast<int>(x.m_max.x), static_cast<int>(x.m_max.y));
			}

			#ifdef _WINDEF_
			static IRect To_(RECT const& x)
			{
				return IRect(x.left, x.top, x.right, x.bottom);
			}
			static IRect To_(SIZE const& x)
			{
				return IRect(0, 0, x.cx, x.cy);
			}
			#endif
		};

		// Whatever to FRect conversion
		struct ToFRect
		{
			static FRect To_(v2 const& x)
			{
				return FRect(0, 0, x.x, x.y);
			}
			static FRect To_(IRect const& x)
			{
				return FRect(static_cast<float>(x.m_min.x), static_cast<float>(x.m_min.y), static_cast<float>(x.m_max.x), static_cast<float>(x.m_max.y));
			}

			#ifdef _WINDEF_
			static FRect To_(RECT const& x)
			{
				return FRect(static_cast<float>(x.left), static_cast<float>(x.top), static_cast<float>(x.right), static_cast<float>(x.bottom));
			}
			static FRect To_(SIZE const& x)
			{
				return FRect(0, 0, static_cast<float>(x.cx), static_cast<float>(x.cy));
			}
			#endif
		};

		#ifdef _WINDEF_
		// Whatever to SIZE conversion
		struct ToSIZE
		{
			static SIZE To_(IRect const& x)
			{
				return SIZE{x.SizeX(), x.SizeY()};
			}
			static SIZE To_(FRect const& x)
			{
				return SIZE{ static_cast<int>(x.SizeX()), static_cast<int>(x.SizeY()) };
			}
			static SIZE To_(RECT const& x)
			{
				return SIZE{x.right - x.left, x.bottom - x.top};
			}
		};

		// Whatever to RECT conversion
		struct ToRECT
		{
			static RECT To_(IRect const& x)
			{
				return RECT{x.m_min.x, x.m_min.y, x.m_max.x, x.m_max.y};
			}
			static RECT To_(FRect const& x)
			{
				return RECT{ static_cast<int>(x.m_min.x), static_cast<int>(x.m_min.y), static_cast<int>(x.m_max.x), static_cast<int>(x.m_max.y) };
			}
			static RECT To_(SIZE const& x)
			{
				return RECT{0, 0, x.cx, x.cy};
			}
		};
		#endif
	}

	// Vector/Matrix to std::basic_string
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec2f<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec3f<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec4f<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec8f<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec2i<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec3i<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename T>                struct Convert<std::basic_string<Char>, Vec4i<T>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat2x2f<A,B>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat3x4f<A,B>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat4x4f<A,B>> :convert::VMToString<std::basic_string<Char>> {};
	template <typename Char, typename A, typename B>    struct Convert<std::basic_string<Char>, Mat6x8f<A,B>> :convert::VMToString<std::basic_string<Char>> {};

	// Vector/Matrix to pr::string
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec2f<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec3f<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec4f<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec8f<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec2i<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec3i<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename T> struct Convert<pr::string<Char,L,F>,    Vec4i<T>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat2x2f<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat3x4f<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat4x4f<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};
	template <typename Char, int L, bool F, typename A, typename B> struct Convert<pr::string<Char,L,F>, Mat6x8f<A,B>> :convert::VMToString<pr::string<Char,L,F>> {};

	// Whatever to vector/matrix
	template <typename T, typename TFrom> struct Convert<Vec2f<T>  , TFrom> :convert::ToV2f {};
	template <typename T, typename TFrom> struct Convert<Vec3f<T>  , TFrom> :convert::ToV3f {};
	template <typename T, typename TFrom> struct Convert<Vec4f<T>  , TFrom> :convert::ToV4f {};
	template <typename T, typename TFrom> struct Convert<Vec8f<T>  , TFrom> :convert::ToV8f {};
	template <typename T, typename TFrom> struct Convert<Vec2i<T> , TFrom> :convert::ToV2i {};
	template <typename T, typename TFrom> struct Convert<Vec3i<T> , TFrom> :convert::ToV3i {};
	template <typename T, typename TFrom> struct Convert<Vec4i<T> , TFrom> :convert::ToV4i {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat2x2f<A,B> , TFrom> :convert::ToM2X2 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat3x4f<A,B> , TFrom> :convert::ToM3X4 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat4x4f<A,B> , TFrom> :convert::ToM4X4 {};
	template <typename A, typename B, typename TFrom> struct Convert<Mat6x8f<A,B> , TFrom> :convert::ToM6X8 {};

	// Whatever to IRect
	template <typename TFrom> struct Convert<IRect, TFrom> :convert::ToIRect {};

	// Whatever to FRect
	template <typename TFrom> struct Convert<FRect, TFrom> :convert::ToFRect {};

	#ifdef _WINDEF_
	// Whatever to SIZE
	template <typename TFrom> struct Convert<SIZE, TFrom> :convert::ToSIZE {};
	
	// Whatever to RECT
	template <typename TFrom> struct Convert<RECT, TFrom> :convert::ToRECT {};
	#endif

	// Write a vector to a stream
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec2f<T> const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec3f<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec4f<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec8f<T> const& vec)
	{
		return out << vec.ang << " " << vec.lin;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec2i<T> const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec3i<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z;
	}
	template <typename Char, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec4i<T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat2x2f<A,B> const& mat)
	{
		return out << mat.x << " " << mat.y;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat3x4f<A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << m.x << sep << m.y << sep << m.z;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat4x4f<A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << m.x << sep << m.y << sep << m.z << sep << m.w;
	}
	template <typename Char, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat6x8f<A,B> const& mat)
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