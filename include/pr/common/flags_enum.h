//******************************************************************************
// Flags Enum
//  Copyright (c) Rylogic Ltd 2014
//******************************************************************************
#pragma once

#include <type_traits>

#ifdef __cplusplus

	// True (true_type) if 'T' has '_bitwise_operations_allowed' as a static member
	template <typename T> struct has_bitwise_operations_allowed
	{
	private:
		template <typename U> static std::true_type  check(decltype(U::_bitwise_operations_allowed)*);
		template <typename>   static std::false_type check(...);
	public:
		using type = decltype(check<T>(0));
		static bool const value = type::value;
	};
	template <typename T> struct support_bitwise_operators :has_bitwise_operations_allowed<T>::type {};
	template <typename T, typename = std::enable_if_t<support_bitwise_operators<T>::value>> struct bitwise_operators_enabled;

	// Define the operators
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator ~ (TEnum lhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(~static_cast<ut>(lhs));
	}
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator | (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) | ut(rhs));
	}
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator & (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) & ut(rhs));
	}
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator ^ (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) ^ ut(rhs));
	}
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum& operator |= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs | rhs);
	}
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum& operator &= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs & rhs);
	}
	template <typename TEnum, typename = bitwise_operators_enabled<TEnum>> inline TEnum& operator ^= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs ^ rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator << (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) << rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline TEnum operator >> (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) >> rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline TEnum& operator <<= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs << rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline TEnum& operator >>= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs >> rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline bool operator == (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return ut(lhs) == ut(rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline bool operator == (T lhs, TEnum rhs)
	{
		return rhs == lhs;
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline bool operator != (TEnum lhs, T rhs)
	{
		return !(lhs == rhs);
	}
	template <typename TEnum, typename T, typename = bitwise_operators_enabled<TEnum>> inline bool operator != (T lhs, TEnum rhs)
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
			static_assert(support_bitwise_operators<NotFlags>::value == false, "");
			
			enum class Flags
			{
				One   = 1 << 0,
				Two   = 1 << 1,
				_bitwise_operations_allowed,
			};
			static_assert(support_bitwise_operators<Flags>::value == true, "");
		}

		PRUnitTest(pr_macros_flags_enum)
		{
			using namespace pr::unittests::flag_enum;
			{
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