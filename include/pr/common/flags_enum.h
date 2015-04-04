//******************************************************************************
// Flags Enum
//  Copyright (c) Rylogic Ltd 2014
//******************************************************************************
#pragma once

#include <type_traits>

#ifdef __cplusplus
	template <typename T, bool = std::is_enum<T>::value> struct is_flags_enum;
	template <typename T> struct is_flags_enum<T, true> :std::false_type {};

	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum operator ~ (TEnum lhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(~static_cast<ut>(lhs));
	}
	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum operator | (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) | ut(rhs));
	}
	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum operator & (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) & ut(rhs));
	}
	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum operator ^ (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) ^ ut(rhs));
	}
	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum& operator |= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs | rhs);
	}
	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum& operator &= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs & rhs);
	}
	template <typename TEnum, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum& operator ^= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs ^ rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum operator << (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) << rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum operator >> (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) >> rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum& operator <<= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs << rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline TEnum& operator >>= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs >> rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline bool operator == (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return ut(lhs) == ut(rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline bool operator == (T lhs, TEnum rhs)
	{
		return rhs == lhs;
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline bool operator != (TEnum lhs, T rhs)
	{
		return !(lhs == rhs);
	}
	template <typename TEnum, typename T, typename std::enable_if<is_flags_enum<TEnum>::value>::type* = nullptr> inline bool operator != (T lhs, TEnum rhs)
	{
		return rhs != lhs;
	}
#else
// C does not require operators
#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr
{
	namespace unittests
	{
		namespace flag_enum
		{
			enum class NotFlags
			{
				One   = 1,
				Two   = 2,
			};
			enum class Flags
			{
				One   = 1 << 0,
				Two   = 1 << 1,
			};
			template <> struct is_flags_enum<Flags> :std::true_type {};
		}

		PRUnitTest(pr_macros_flags_enum)
		{
			using namespace pr::unittests::flag_enum;

			typedef Flags Enum;
			//typedef NotFlags Enum; // Uncomment to test not-compiling-ness

			Enum a =  Enum::One | Enum::Two;
			Enum b =  Enum::One & Enum::Two;
			Enum c =  Enum::One ^ Enum::Two;
			Enum f = ~Enum::One;

			PR_CHECK((int)a, 3);
			PR_CHECK((int)b, 0);
			PR_CHECK((int)c, 3);
			PR_CHECK((int)f, -2);

			a |= Enum::Two;
			b &= Enum::Two;
			c ^= Enum::Two;

			PR_CHECK((int)a, 3);
			PR_CHECK((int)b, 0);
			PR_CHECK((int)c, 1);
		}
	}
}
#endif

//#define PR_ENUM_FLAG_OPERATORS(TEnum)\
//	inline TEnum operator ~ (TEnum lhs)\
//	{\
//		return TEnum(~static_cast<std::underlying_type<TEnum>::type>(lhs));\
//	}\
//	inline TEnum operator | (TEnum lhs, TEnum rhs)\
//	{\
//		return TEnum(std::underlying_type<TEnum>::type(lhs) | std::underlying_type<TEnum>::type(rhs));\
//	}\
//	inline TEnum operator & (TEnum lhs, TEnum rhs)\
//	{\
//		return TEnum(std::underlying_type<TEnum>::type(lhs) & std::underlying_type<TEnum>::type(rhs));\
//	}\
//	inline TEnum operator ^ (TEnum lhs, TEnum rhs)\
//	{\
//		return TEnum(std::underlying_type<TEnum>::type(lhs) ^ std::underlying_type<TEnum>::type(rhs));\
//	}\
//	inline TEnum operator << (TEnum lhs, std::underlying_type<TEnum>::type rhs)\
//	{\
//		return TEnum(std::underlying_type<TEnum>::type(lhs) << rhs);\
//	}\
//	inline TEnum operator >> (TEnum lhs, std::underlying_type<TEnum>::type rhs)\
//	{\
//		return TEnum(std::underlying_type<TEnum>::type(lhs) >> rhs);\
//	}\
//	inline TEnum& operator |= (TEnum& lhs, TEnum rhs)\
//	{\
//		return lhs = (lhs | rhs);\
//	}\
//	inline TEnum& operator &= (TEnum& lhs, TEnum rhs)\
//	{\
//		return lhs = (lhs & rhs);\
//	}\
//	inline TEnum& operator ^= (TEnum& lhs, TEnum rhs)\
//	{\
//		return lhs = (lhs ^ rhs);\
//	}\
//	inline TEnum& operator <<= (TEnum& lhs, std::underlying_type<TEnum>::type rhs)\
//	{\
//		return lhs = (lhs << rhs);\
//	}\
//	inline TEnum& operator >>= (TEnum& lhs, std::underlying_type<TEnum>::type rhs)\
//	{\
//		return lhs = (lhs >> rhs);\
//	}\
//	inline bool operator == (TEnum lhs, std::underlying_type<TEnum>::type rhs)\
//	{\
//		return std::underlying_type<TEnum>::type(lhs) == rhs;\
//	}\
//	inline bool operator == (std::underlying_type<TEnum>::type lhs, TEnum rhs)\
//	{\
//		return rhs == lhs;\
//	}\
//	inline bool operator != (TEnum lhs, std::underlying_type<TEnum>::type rhs)\
//	{\
//		return !(lhs == rhs);\
//	}\
//	inline bool operator != (std::underlying_type<TEnum>::type lhs, TEnum rhs)\
//	{\
//		return rhs != lhs;\
//	}