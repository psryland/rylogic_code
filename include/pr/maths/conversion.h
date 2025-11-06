//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include <format>
#include "pr/common/to.h"
#include "pr/common/cast.h"
#include "pr/common/fmt.h"
#include "pr/maths/maths.h"
#include "pr/maths/bit_fields.h"
#include "pr/str/string.h"
#include "pr/str/to_string.h"

namespace pr
{
	// Vec2
	template <StringTypeDynamic Str, Scalar S, typename T>
	struct Convert<Str, Vec2<S, T>>
	{
		static Str Func(Vec2<S, T> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			if constexpr (std::is_same_v<Char, char>)
				return std::format("{} {}", x.x, x.y);
			else if constexpr (std::is_same_v<Char, wchar_t>)
				return std::format(L"{} {}", x.x, x.y);
		}
	};
	template <Scalar S, typename T, typename TFrom>
	struct Convert<Vec2<S, T>, TFrom>
	{
		// Vec2 to Vec2
		template <Scalar SS, typename TT> static Vec2<S,T> Func(Vec2<SS,TT> const& v)
		{
			return Vec2<S,T>(s_cast<S>(v.x), s_cast<S>(v.y));
		}

		// String to Vec2<floating_point>
		template <StringType Str>
		static Vec2<S,T> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr) requires std::floating_point<S>
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, &e);
			auto y = To<S>(e, &e);
			if (end) *end = e;
			return Vec2<S,T>(x, y);
		}

		// String to Vec2<integral>
		template <StringType Str>
		static Vec2<S,T> Func(Str const& s, int radix = 10, string_traits<Str>::value_type const** end = nullptr) requires std::integral<S>
		{
			using Char = typename string_traits<Str>::value_type;
			
			Char const* e = {};
			auto x = To<S>(s, radix, &e);
			auto y = To<S>(e, radix, &e);
			if (end) *end = e;
			return Vec2<S,T>(x, y);
		}

		// Rect to Vec2
		static Vec2<S, T> Func(FRect const& x)
		{
			return Vec2<S, T>(s_cast<S>(x.SizeX()), s_cast<S>(x.SizeY()));
		}
		static Vec2<S,T> Func(IRect const& x)
		{
			return Vec2<S, T>(s_cast<S>(x.SizeX()), s_cast<S>(x.SizeY()));
		}

		// Win32 primitive types to Vec2
		#ifdef _WINDEF_
		static Vec2<S,T> Func(POINT const& x)
		{
			return Vec2<S, T>(s_cast<S>(x.x), s_cast<S>(x.y));
		}
		static Vec2<S,T> Func(SIZE const& x)
		{
			return Vec2<S,T>(s_cast<S>(x.cx), s_cast<S>(x.cy));
		}
		static Vec2<S,T> Func(RECT const& x)
		{
			return Vec2<S, T>(s_cast<S>(x.right - x.left), s_cast<S>(x.bottom - x.top));
		}
		#endif

		// GDI+ types to Vec2
		#ifdef _GDIPLUS_H
		static Vec2<S,T> Func(Gdiplus::Rect const& x)
		{
			return Vec2<S,T>(s_cast<S>(x.Width), s_cast<S>(x.Height));
		}
		static Vec2<S, T> Func(Gdiplus::RectF const& x)
		{
			return Vec2<S, T>(s_cast<S>(x.Width), s_cast<S>(x.Height));
		}
		#endif
	};

	// Vec3
	template <StringTypeDynamic Str, Scalar S, typename T>
	struct Convert<Str, Vec3<S, T>>
	{
		static Str Func(Vec3<S, T> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			if constexpr (std::is_same_v<Char, char>)
				return std::format("{} {} {}", x.x, x.y, x.z);
			else if constexpr (std::is_same_v<Char, wchar_t>)
				return std::format(L"{} {} {}", x.x, x.y, x.z);
		}
	};
	template <Scalar S, typename T, typename TFrom>
	struct Convert<Vec3<S, T>, TFrom>
	{
		// Vec3 to Vec3
		template <Scalar SS, typename TT> static Vec3<S, T> Func(Vec3<SS, TT> const& v)
		{
			return Vec3<S, T>(s_cast<S>(v.x), s_cast<S>(v.y), s_cast<S>(v.z));
		}

		// String to Vec3
		template <StringType Str>
		static Vec3<S, T> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, &e);
			auto y = To<S>(e, &e);
			auto z = To<S>(e, &e);
			if (end) *end = e;
			return Vec3<S, T>(x, y, z);
		}
		template <typename Str>
		static Vec3<S, T> Func(Str const& s, int radix = 10, string_traits<Str>::value_type const** end = nullptr) requires std::integral<S>
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, radix, &e);
			auto y = To<S>(e, radix, &e);
			auto z = To<S>(e, radix, &e);
			if (end) *end = e;
			return Vec3<S,T>(x, y, z);
		}
	};

	// Vec4
	template <StringTypeDynamic Str, Scalar S, typename T>
	struct Convert<Str, Vec4<S, T>>
	{
		static Str Func(Vec4<S, T> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			if constexpr (std::is_same_v<Char, char>)
				return std::format("{} {} {} {}", x.x, x.y, x.z, x.w);
			else if constexpr (std::is_same_v<Char, wchar_t>)
				return std::format(L"{} {} {} {}", x.x, x.y, x.z, x.w);
		}
	};
	template <Scalar S, typename T, typename TFrom>
	struct Convert<Vec4<S, T>, TFrom>
	{
		// Vec4 to Vec4
		template <Scalar SS, typename TT> static Vec4<S, T> Func(Vec4<SS, TT> const& v)
		{
			return Vec4<S, T>(s_cast<S>(v.x), s_cast<S>(v.y), s_cast<S>(v.z), s_cast<S>(v.w));
		}

		// String to Vec4
		template <StringType Str>
		static Vec4<S, T> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, &e);
			auto y = To<S>(e, &e);
			auto z = To<S>(e, &e);
			auto w = To<S>(e, &e);
			if (end) *end = e;
			return Vec4<S, T>(x, y, z, w);
		}
		template <StringType Str>
		static Vec4<S, T> Func(Str const& s, S w, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, &e);
			auto y = To<S>(e, &e);
			auto z = To<S>(e, &e);
			if (end) *end = e;
			return Vec4<S, T>(x, y, z, w);
		}
		template <StringType Str>
		static Vec4<S, T> Func(Str const& s, int radix = 10, string_traits<Str>::value_type const** end = nullptr) requires std::integral<S>
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, radix, &e);
			auto y = To<S>(e, radix, &e);
			auto z = To<S>(e, radix, &e);
			auto w = To<S>(e, radix, &e);
			if (end) *end = e;
			return Vec4<S, T>(x, y, z, w);
		}
		template <StringType Str>
		static Vec4<S, T> Func(Str const& s, S w, int radix = 10, string_traits<Str>::value_type const** end = nullptr) requires std::integral<S>
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<S>(s, radix, &e);
			auto y = To<S>(e, radix, &e);
			auto z = To<S>(e, radix, &e);
			if (end) *end = e;
			return Vec4<S, T>(x, y, z, w);
		}
	};

	// Vec8
	template <StringTypeDynamic Str, Scalar S, typename T>
	struct Convert<Str, Vec8<S, T>>
	{
		static Str Func(Vec8<S, T> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			if constexpr (std::is_same_v<Char, char>)
				return std::format("{} {} {} {}  {} {} {} {}", x.ang.x, x.ang.y, x.ang.z, x.ang.w, x.lin.x, x.lin.y, x.lin.z, x.lin.w);
			else if constexpr (std::is_same_v<Char, wchar_t>)
				return std::format(L"{} {} {} {}  {} {} {} {}", x.ang.x, x.ang.y, x.ang.z, x.ang.w, x.lin.x, x.lin.y, x.lin.z, x.lin.w);
		}
	};
	template <Scalar S, typename T, typename TFrom>
	struct Convert<Vec8<S, T>, TFrom>
	{
		// Vec8 to Vec8
		template <Scalar SS, typename TT> static Vec8<S, T> Func(Vec8<SS, TT> const& v)
		{
			return Vec8<S, T>(To<Vec4<S,T>>(v.ang), To<Vec4<S,T>>(v.lin));
		}

		// String to Vec8
		template <StringType Str>
		static Vec8<S, T> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr) requires std::floating_point<S>
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto ang_x = To<S>(s, &e);
			auto ang_y = To<S>(e, &e);
			auto ang_z = To<S>(e, &e);
			auto ang_w = To<S>(e, &e);
			auto lin_x = To<S>(e, &e);
			auto lin_y = To<S>(e, &e);
			auto lin_z = To<S>(e, &e);
			auto lin_w = To<S>(e, &e);
			if (end) *end = e;
			return Vec8<S, T>(
				Vec4<S, T>(ang_x, ang_y, ang_z, ang_w),
				Vec4<S, T>(lin_x, lin_y, lin_z, lin_w));
		}
		template <StringType Str>
		static Vec8<S, T> Func(Str const& s, int radix = 10, string_traits<Str>::value_type const** end = nullptr) requires std::integral<S>
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto ang_x = To<S>(s, radix, &e);
			auto ang_y = To<S>(e, radix, &e);
			auto ang_z = To<S>(e, radix, &e);
			auto ang_w = To<S>(e, radix, &e);
			auto lin_x = To<S>(e, radix, &e);
			auto lin_y = To<S>(e, radix, &e);
			auto lin_z = To<S>(e, radix, &e);
			auto lin_w = To<S>(e, radix, &e);
			if (end) *end = e;
			return Vec8<S, T>(
				Vec4<S, T>(ang_x, ang_y, ang_z, ang_w),
				Vec4<S, T>(lin_x, lin_y, lin_z, lin_w));
		}
	};

	// Mat2x2
	template <StringTypeDynamic Str, Scalar S, typename A, typename B>
	struct Convert<Str, Mat2x2<S,A,B>>
	{
		static Str Func(Mat2x2<S,A,B> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			return
				To<Str>(x.x) + Char(' ') +
				To<Str>(x.y);
		}
	};
	template <Scalar S, typename A, typename B, typename TFrom>
	struct Convert<Mat2x2<S, A, B>, TFrom>
	{
		// Mat2x2 to Mat2x2
		template <Scalar SS, typename AA, typename BB> static Mat2x2<S, A, B> Func(Mat2x2<SS, AA, BB> const& v)
		{
			return Mat2x2<S, A, B>(
				To<Vec2<S, void>>(v.x),
				To<Vec2<S, void>>(v.y));
		}

		// String to Mat2x2
		template <StringType Str>
		static Mat2x2<S, A, B> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<Vec2<S, void>>(s, &e);
			auto y = To<Vec2<S, void>>(e, &e);
			if (end) *end = e;
			return m2x2(x, y);
		}
	};

	// Mat3x4
	template <StringTypeDynamic Str, Scalar S, typename A, typename B>
	struct Convert<Str, Mat3x4<S, A, B>>
	{
		static Str Func(Mat3x4<S, A, B> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			return
				To<Str>(x.x) + Char(' ') +
				To<Str>(x.y) + Char(' ') +
				To<Str>(x.z);
		}
	};
	template <Scalar S, typename A, typename B, typename TFrom>
	struct Convert<Mat3x4<S, A, B>, TFrom>
	{
		// Mat3x4 to Mat3x4
		template <Scalar SS, typename AA, typename BB> static Mat3x4<S, A, B> Func(Mat3x4<SS, AA, BB> const& v)
		{
			return Mat3x4<S, A, B>(
				To<Vec3<S, void>>(v.x),
				To<Vec3<S, void>>(v.y),
				To<Vec3<S, void>>(v.z));
		}

		// String to Mat3x4
		template <StringType Str>
		static Mat3x4<S, A, B> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<Vec3<S, void>>(s, &e);
			auto y = To<Vec3<S, void>>(e, &e);
			auto z = To<Vec3<S, void>>(e, &e);
			if (end) *end = e;
			return Mat3x4<S, A, B>(x, y, z);
		}
	};

	// Mat4x4
	template <StringTypeDynamic Str, Scalar S, typename A, typename B>
	struct Convert<Str, Mat4x4<S, A, B>>
	{
		static Str Func(Mat4x4<S, A, B> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			return
				To<Str>(x.x) + Char(' ') +
				To<Str>(x.y) + Char(' ') +
				To<Str>(x.z) + Char(' ') +
				To<Str>(x.w);
		}
	};
	template <Scalar S, typename A, typename B, typename TFrom>
	struct Convert<Mat4x4<S, A, B>, TFrom>
	{
		// Mat4x4 to Mat4x4
		template <Scalar SS, typename AA, typename BB> static Mat4x4<S, A, B> Func(Mat4x4<SS, AA, BB> const& v)
		{
			return Mat4x4<S, A, B>(
				To<Vec4<S, void>>(v.x),
				To<Vec4<S, void>>(v.y),
				To<Vec4<S, void>>(v.z),
				To<Vec4<S, void>>(v.w));
		}

		// String to Mat4x4
		template <StringType Str>
		static Mat4x4<S, A, B> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<Vec4<S, void>>(s, &e);
			auto y = To<Vec4<S, void>>(e, &e);
			auto z = To<Vec4<S, void>>(e, &e);
			auto w = To<Vec4<S, void>>(e, &e);
			if (end) *end = e;
			return Mat4x4<S, A, B>(x, y, z, w);
		}
	};

	// Mat6x8
	template <StringTypeDynamic Str, Scalar S, typename A, typename B>
	struct Convert<Str, Mat6x8<S, A, B>>
	{
		static Str Func(Mat6x8<S, A, B> const& x)
		{
			using Char = typename string_traits<Str>::value_type;
			return
				To<Str>(x[0]) + Char(' ') +
				To<Str>(x[1]) + Char(' ') +
				To<Str>(x[2]) + Char(' ') +
				To<Str>(x[3]) + Char(' ') +
				To<Str>(x[4]) + Char(' ') +
				To<Str>(x[5]);
		}
	};
	template <Scalar S, typename A, typename B, typename TFrom>
	struct Convert<Mat6x8<S, A, B>, TFrom>
	{
		// Mat6x8 to Mat6x8
		template <Scalar SS, typename AA, typename BB> static Mat6x8<S, A, B> Func(Mat6x8<SS, AA, BB> const& v)
		{
			return Mat6x8<S, A, B>(
				To<Mat3x4<S, void>>(v.m00),
				To<Mat3x4<S, void>>(v.m01),
				To<Mat3x4<S, void>>(v.m10),
				To<Mat3x4<S, void>>(v.m11));
		}

		// String to Mat6x8
		template <StringType Str>
		static Mat6x8<S, A, B> Func(Str const& s, string_traits<Str>::value_type const** end = nullptr)
		{
			using Char = typename string_traits<Str>::value_type;

			Char const* e = {};
			auto x = To<Vec8<S, void>>(s, &e);
			auto y = To<Vec8<S, void>>(e, &e);
			auto z = To<Vec8<S, void>>(e, &e);
			auto u = To<Vec8<S, void>>(e, &e);
			auto v = To<Vec8<S, void>>(e, &e);
			auto w = To<Vec8<S, void>>(e, &e);
			if (end) *end = e;
			return Mat6x8(x, y, z, u, v, w);
		}
	};

	// Rect
	template <typename TFrom>
	struct Convert<IRect, TFrom>
	{
		template <Scalar S, typename T>
		static IRect Func(Vec2<S,T> const& x)
		{
			return IRect(
				Vec2<int, void>(0, 0),
				To<Vec2<int, void>>(x));
		}
		static IRect Func(FRect const& x)
		{
			return IRect(
				Vec2<int, void>(s_cast<int>(x.m_min.x), s_cast<int>(x.m_min.y)),
				Vec2<int, void>(s_cast<int>(x.m_max.x), s_cast<int>(x.m_max.y)));
		}

		#ifdef _WINDEF_
		static IRect Func(RECT const& x)
		{
			return IRect(x.left, x.top, x.right, x.bottom);
		}
		static IRect Func(SIZE const& x)
		{
			return IRect(0, 0, x.cx, x.cy);
		}
		#endif
	};
	template <typename TFrom>
	struct Convert<FRect, TFrom>
	{
		template <Scalar S, typename T>
		static FRect Func(Vec2<S,T> const& x)
		{
			return FRect(
				Vec2<float, void>(0, 0),
				To<Vec2<float, void>>(x));
		}
		static FRect Func(IRect const& x)
		{
			return FRect(
				To<Vec2<float, void>>(x.m_min),
				To<Vec2<float, void>>(x.m_max));
		}

		#ifdef _WINDEF_
		static FRect Func(RECT const& x)
		{
			return FRect(
				s_cast<float>(x.left),
				s_cast<float>(x.top),
				s_cast<float>(x.right),
				s_cast<float>(x.bottom));
		}
		static FRect Func(SIZE const& x)
		{
			return FRect(0, 0, s_cast<float>(x.cx), s_cast<float>(x.cy));
		}
		#endif
	};

	// Write a vector to a stream
	template <typename Char, Scalar S, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec2<S, T> const& vec)
	{
		return out << vec.x << " " << vec.y;
	}
	template <typename Char, Scalar S, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec3<S, T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z;
	}
	template <typename Char, Scalar S, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec4<S, T> const& vec)
	{
		return out << vec.x << " " << vec.y << " " << vec.z << " " << vec.w;
	}
	template <typename Char, Scalar S, typename T>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Vec8<S, T> const& vec)
	{
		return out << vec.ang << " " << vec.lin;
	}
	template <typename Char, Scalar S, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Quat<S, A, B> const& quat)
	{
		return out << quat.xyzw;
	}
	template <typename Char, Scalar S, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat2x2<S,A,B> const& mat)
	{
		return out << mat.x << " " << mat.y;
	}
	template <typename Char, Scalar S, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat3x4<S,A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << m.x << sep << m.y << sep << m.z;
	}
	template <typename Char, Scalar S, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat4x4<S,A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << m.x << sep << m.y << sep << m.z << sep << m.w;
	}
	template <typename Char, Scalar S, typename A, typename B>
	inline std::basic_ostream<Char>& operator << (std::basic_ostream<Char>& out, Mat6x8<S,A,B> const& mat)
	{
		auto m = mat;
		auto sep = " ";
		return out << mat[0] << sep << mat[1] << sep << mat[2] << sep << mat[3] << sep << mat[4] << sep << mat[5];
	}

	// Convert an integer to a string of 0s and 1s
	template <StringTypeDynamic Str, std::integral Int>
	inline Str ToBinary(Int n)
	{
		int const bits = sizeof(Int) * 8;
		Str str = {};
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
		{// Vec2
			PR_CHECK(To<std::string>(Vec2<float, void>(2.125f, -4.75f)), "2.125 -4.75");
			PR_CHECK(To<std::string>(Vec2<double, void>(2.125, -4.75)), "2.125 -4.75");
			PR_CHECK(To<std::string>(Vec2<int, void>(12, -34)), "12 -34");
			PR_CHECK(To<std::string>(Vec2<long long, void>(12, -34)), "12 -34");

			PR_CHECK((To<Vec2<double, void>>("1.2 3.4")), (Vec2<double, void>(1.2, 3.4)));
			PR_CHECK((To<Vec2<int, void>>("1 -2")      ), (Vec2<int, void>(1, -2)));
			PR_CHECK((To<Vec2<int, void>>("AA 55", 16) ), (Vec2<int, void>(170, 85)));
		}
		{// Vec3
			PR_CHECK(To<std::string>(Vec3<float, void>(2.125f, 4.75f, -1.375f)), "2.125 4.75 -1.375");
			PR_CHECK(To<std::string>(Vec3<double, void>(2.125, 4.75, -1.375)), "2.125 4.75 -1.375");
			PR_CHECK(To<std::string>(Vec3<int, void>(12, -34, 56)), "12 -34 56");
			PR_CHECK(To<std::string>(Vec3<long long, void>(12, -34, 56)), "12 -34 56");
		}
		{// Vec4
			PR_CHECK(To<std::string>(Vec4<float, void>(2.125f, 4.75f, -1.375f, -0.825f)), "2.125 4.75 -1.375 -0.825");
			PR_CHECK(To<std::string>(Vec4<double, void>(2.125, 4.75, -1.375, -0.825)), "2.125 4.75 -1.375 -0.825");
			PR_CHECK(To<std::string>(Vec4<int, void>(12, -34, 56, -78)), "12 -34 56 -78");
			PR_CHECK(To<std::string>(Vec4<long long, void>(12, -34, 56, -78)), "12 -34 56 -78");
		}
		{// Vec8
			PR_CHECK(To<std::string>(Vec8<float, void>(-1.125f, 2.25f, -3.375f, 4.4f, -5.5f, 6.675f)), "-1.125 2.25 -3.375 0  4.4 -5.5 6.675 0");
			PR_CHECK(To<std::string>(Vec8<double, void>(-1.125, 2.25, -3.375, 4.4, -5.5, 6.675)), "-1.125 2.25 -3.375 0  4.4 -5.5 6.675 0");
			PR_CHECK(To<std::string>(Vec8<int, void>(12, -34, 56, -78, 90, -11)), "12 -34 56 0  -78 90 -11 0");
			PR_CHECK(To<std::string>(Vec8<long long, void>(12, -34, 56, -78, 90, -11)), "12 -34 56 0  -78 90 -11 0");

			PR_CHECK((To<Vec8<double, void>>("-1.125 2.25 -3.375 0  4.4 -5.5 6.675 0")), (Vec8<double, void>(-1.125, 2.25, -3.375, 0.0, 4.4, -5.5, 6.675, 0.0)));
			PR_CHECK((To<Vec8<int, void>>("12 -34 56 0  -78 90 -11 0")), (Vec8<int, void>(12, -34, 56, 0, -78, 90, -11, 0)));
			PR_CHECK((To<Vec8<int, void>>("10 11 12 13  14 15 16 17", 16)), (Vec8<int, void>(16, 17, 18, 19, 20, 21, 22, 23)));
		}
		{// Mat2x2
			PR_CHECK(To<std::string>(Mat2x2<float, void, void>(
				2.125f, -4.75f,
				-1.375f, -0.825f)),
				"2.125 -4.75 "
				"-1.375 -0.825");
			PR_CHECK(To<std::string>(Mat2x2<double, void, void>(
				2.125, -4.75,
				-1.375, -0.825)),
				"2.125 -4.75 "
				"-1.375 -0.825");
			PR_CHECK(To<std::string>(Mat2x2<int, void, void>(
				12, -34,
				56, -78)),
				"12 -34 "
				"56 -78");
			PR_CHECK(To<std::string>(Mat2x2<long long, void, void>(
				12, -34,
				56, -78)),
				"12 -34 "
				"56 -78");
		}
		{// Mat3x4
			PR_CHECK(To<std::string>(Mat3x4<float, void, void>(
				Vec4<float,void>(1.2f, 2.4f, -4.8f, -8.16f),
				Vec4<float,void>(2.1f, 4.2f, -8.4f, -1.68f),
				Vec4<float,void>(1.1f, 2.2f, -3.3f, -4.44f))),
				"1.2 2.4 -4.8 -8.16 "
				"2.1 4.2 -8.4 -1.68 "
				"1.1 2.2 -3.3 -4.44");
			PR_CHECK(To<std::string>(Mat3x4<double, void, void>(
				Vec4<double, void>(1.2, 2.4, -4.8, -8.16),
				Vec4<double, void>(2.1, 4.2, -8.4, -1.68),
				Vec4<double, void>(1.1, 2.2, -3.3, -4.44))),
				"1.2 2.4 -4.8 -8.16 "
				"2.1 4.2 -8.4 -1.68 "
				"1.1 2.2 -3.3 -4.44");
			PR_CHECK(To<std::string>(Mat3x4<int, void, void>(
				Vec4<int, void>(1, 2, -3, -4),
				Vec4<int, void>(5, 6, -7, -8),
				Vec4<int, void>(9, 0, -1, -2))),
				"1 2 -3 -4 "
				"5 6 -7 -8 "
				"9 0 -1 -2");
			PR_CHECK(To<std::string>(Mat3x4<long long, void, void>(
				Vec4<long long, void>(1, 2, -3, -4),
				Vec4<long long, void>(5, 6, -7, -8),
				Vec4<long long, void>(9, 0, -1, -2))),
				"1 2 -3 -4 "
				"5 6 -7 -8 "
				"9 0 -1 -2");
		}
		{// Mat4x4
			PR_CHECK(To<std::string>(Mat4x4<float, void, void>(
				Vec4<float, void>(1.2f, 2.4f, -4.8f, -8.16f),
				Vec4<float, void>(2.1f, 4.2f, -8.4f, -1.68f),
				Vec4<float, void>(1.1f, 2.2f, -3.3f, -4.44f),
				Vec4<float, void>(0.2f, 0.5f, -0.1f, -0.12f))),
				"1.2 2.4 -4.8 -8.16 "
				"2.1 4.2 -8.4 -1.68 "
				"1.1 2.2 -3.3 -4.44 "
				"0.2 0.5 -0.1 -0.12");
			PR_CHECK(To<std::string>(Mat4x4<double, void, void>(
				Vec4<double, void>(1.2, 2.4, -4.8, -8.16),
				Vec4<double, void>(2.1, 4.2, -8.4, -1.68),
				Vec4<double, void>(1.1, 2.2, -3.3, -4.44),
				Vec4<double, void>(0.2, 0.5, -0.1, -0.12))),
				"1.2 2.4 -4.8 -8.16 "
				"2.1 4.2 -8.4 -1.68 "
				"1.1 2.2 -3.3 -4.44 "
				"0.2 0.5 -0.1 -0.12");
			PR_CHECK(To<std::string>(Mat4x4<int, void, void>(
				Vec4<int, void>(1, 2, -3, -4),
				Vec4<int, void>(5, 6, -7, -8),
				Vec4<int, void>(9, 0, -1, -2),
				Vec4<int, void>(3, 4, -5, -6))),
				"1 2 -3 -4 "
				"5 6 -7 -8 "
				"9 0 -1 -2 "
				"3 4 -5 -6");
			PR_CHECK(To<std::string>(Mat4x4<long long, void, void>(
				Vec4<long long, void>(1, 2, -3, -4),
				Vec4<long long, void>(5, 6, -7, -8),
				Vec4<long long, void>(9, 0, -1, -2),
				Vec4<long long, void>(3, 4, -5, -6))),
				"1 2 -3 -4 "
				"5 6 -7 -8 "
				"9 0 -1 -2 "
				"3 4 -5 -6");
		}
		{// Mat6x8
			PR_CHECK(To<std::string>(Mat6x8<float, void, void>(
				Vec8<float, void>(1.1f, 2.2f, -3.25f, 4.5f, 5.25f, -6.125f),
				Vec8<float, void>(1.1f, 2.2f, -3.25f, 4.5f, 5.25f, -6.125f),
				Vec8<float, void>(1.1f, 2.2f, -3.25f, 4.5f, 5.25f, -6.125f),
				Vec8<float, void>(1.1f, 2.2f, -3.25f, 4.5f, 5.25f, -6.125f),
				Vec8<float, void>(1.1f, 2.2f, -3.25f, 4.5f, 5.25f, -6.125f),
				Vec8<float, void>(1.1f, 2.2f, -3.25f, 4.5f, 5.25f, -6.125f))),
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0");
			PR_CHECK(To<std::string>(Mat6x8<double, void, void>(
				Vec8<double, void>(1.1, 2.2, -3.25, 4.5, 5.25, -6.125),
				Vec8<double, void>(1.1, 2.2, -3.25, 4.5, 5.25, -6.125),
				Vec8<double, void>(1.1, 2.2, -3.25, 4.5, 5.25, -6.125),
				Vec8<double, void>(1.1, 2.2, -3.25, 4.5, 5.25, -6.125),
				Vec8<double, void>(1.1, 2.2, -3.25, 4.5, 5.25, -6.125),
				Vec8<double, void>(1.1, 2.2, -3.25, 4.5, 5.25, -6.125))),
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0 "
				"1.1 2.2 -3.25 0  4.5 5.25 -6.125 0");
			PR_CHECK(To<std::string>(Mat6x8<int, void, void>(
				Vec8<int, void>(1, -2, 3, -4, 5, -6),
				Vec8<int, void>(1, -2, 3, -4, 5, -6),
				Vec8<int, void>(1, -2, 3, -4, 5, -6),
				Vec8<int, void>(1, -2, 3, -4, 5, -6),
				Vec8<int, void>(1, -2, 3, -4, 5, -6),
				Vec8<int, void>(1, -2, 3, -4, 5, -6))),
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0");
			PR_CHECK(To<std::string>(Mat6x8<long long, void, void>(
				Vec8<long long, void>(1, -2, 3, -4, 5, -6),
				Vec8<long long, void>(1, -2, 3, -4, 5, -6),
				Vec8<long long, void>(1, -2, 3, -4, 5, -6),
				Vec8<long long, void>(1, -2, 3, -4, 5, -6),
				Vec8<long long, void>(1, -2, 3, -4, 5, -6),
				Vec8<long long, void>(1, -2, 3, -4, 5, -6))),
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0 "
				"1 -2 3 0  -4 5 -6 0");
		}

		{
			PR_CHECK(pr::To<std::string>(v4(1, 2, 3, 4)), "1 2 3 4");

			PR_CHECK(pr::To<v2>("2 3"), v2(2, 3));
			PR_CHECK(pr::To<v4>("1 2 3 4"), v4(1, 2, 3, 4));

			PR_CHECK(pr::ToBinary<std::string>(uint8_t(0b11001010)), "11001010");
		}
	}
}
#endif