//******************************************************************************
// Flags Enum
//  Copyright (c) Rylogic Ltd 2014
//******************************************************************************
// Add '_bitwise_operators_allowed' to your enum for bitwise operators
// Add '_arithmetic_operators_allowed' to your enum for arithmetic operators
#pragma once

#ifdef __cplusplus // C does not require operators
#include <type_traits>

	// These are in the global namespace so that they work in any namespace

	// True (true_type) if 'T' has '_bitwise_operators_allowed' as a static member
	template <typename T> struct has_bitops_allowed
	{
		template <typename U> static std::true_type  check(decltype(U::_bitwise_operators_allowed)*);
		template <typename>   static std::false_type check(...);
		static constexpr bool value = decltype(check<T>(nullptr))::value;
	};
	template <typename T> constexpr bool has_bitops_allowed_v = has_bitops_allowed<T>::value;
	template <typename T> using enable_if_has_bitops_t = typename std::enable_if_t<has_bitops_allowed_v<T>, void>;

	// Define the bitwise operators
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum operator ~ (TEnum lhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(~static_cast<ut>(lhs));
	}
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum operator | (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) | ut(rhs));
	}
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum operator & (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) & ut(rhs));
	}
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum operator ^ (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) ^ ut(rhs));
	}
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum& operator |= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs | rhs);
	}
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum& operator &= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs & rhs);
	}
	template <typename TEnum, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum& operator ^= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs ^ rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum operator << (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) << rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum operator >> (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) >> rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum& operator <<= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs << rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr TEnum& operator >>= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs >> rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr bool operator == (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return ut(lhs) == ut(rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr bool operator == (T lhs, TEnum rhs)
	{
		return rhs == lhs;
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr bool operator != (TEnum lhs, T rhs)
	{
		return !(lhs == rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_bitops_t<TEnum>> constexpr bool operator != (T lhs, TEnum rhs)
	{
		return rhs != lhs;
	}


	// True (true_type) if 'T' has '_arithmetic_operators_allowed' as a static member
	template <typename T> struct has_arithops_allowed
	{
		template <typename U> static std::true_type  check(decltype(U::_arithmetic_operators_allowed)*);
		template <typename>   static std::false_type check(...);
		static constexpr bool value = decltype(check<T>(nullptr))::value;
	};
	template <typename T> struct has_number_of
	{
		template <typename U> static std::true_type  check(decltype(U::NumberOf)*);
		template <typename>   static std::false_type check(...);
		static constexpr bool value = decltype(check<T>(nullptr))::value;
	};
	template <typename T> constexpr bool has_arithops_allowed_v = has_arithops_allowed<T>::value;
	template <typename T> constexpr bool has_number_of_v = has_number_of<T>::value;
	template <typename T> using enable_if_has_arith_t = typename std::enable_if_t<has_arithops_allowed_v<T> || has_number_of_v<T>>;

	// Define the arithmetic operators
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> inline TEnum& operator ++ (TEnum& lhs)
	{
		return lhs = static_cast<TEnum>(lhs + 1);
	}
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator + (TEnum lhs)
	{
		return lhs;
	}
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator - (TEnum lhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(-static_cast<ut>(lhs));
	}
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator + (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) + static_cast<ut>(rhs));
	}
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator - (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) - static_cast<ut>(rhs));
	}
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator * (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) * static_cast<ut>(rhs));
	}
	template <typename TEnum, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator / (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) / static_cast<ut>(rhs));
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator + (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) + rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator + (T lhs, TEnum rhs)
	{
		return rhs + lhs;
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator - (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) - rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator - (T lhs, TEnum rhs)
	{
		return -(rhs - lhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator * (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) * rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator * (T lhs, TEnum rhs)
	{
		return rhs * lhs;
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator / (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) / rhs);
	}
	template <typename TEnum, typename T, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator / (T lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(lhs / static_cast<ut>(rhs));
	}
	//template <typename TEnum, typename ut = typename std::underlying_type<TEnum>::type, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator == (TEnum lhs, ut rhs)
	//{
	//	return static_cast<ut>(lhs) == rhs;
	//}
	//template <typename TEnum, typename ut = typename std::underlying_type<TEnum>::type, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator != (TEnum lhs, ut rhs)
	//{
	//	return !(lhs == rhs);
	//}
	//template <typename TEnum, typename ut = typename std::underlying_type<TEnum>::type, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator <  (TEnum lhs, ut rhs)
	//{
	//	return static_cast<ut>(lhs) < rhs;
	//}
	//template <typename TEnum, typename ut = typename std::underlying_type<TEnum>::type, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator >  (TEnum lhs, ut rhs)
	//{
	//	return static_cast<ut>(lhs) > rhs;
	//}
	//template <typename TEnum, typename ut = typename std::underlying_type<TEnum>::type, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator <= (TEnum lhs, ut rhs)
	//{
	//	return static_cast<ut>(lhs) <= rhs;
	//}
	//template <typename TEnum, typename ut = typename std::underlying_type<TEnum>::type, typename = enable_if_has_arith_t<TEnum>> constexpr TEnum operator >= (TEnum lhs, ut rhs)
	//{
	//	return static_cast<ut>(lhs) >= rhs;
	//}

#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	namespace unittests::flag_enum
	{
		enum class NotFlags
		{
			One   = 1,
			Two   = 2,
		};
		static_assert(has_bitops_allowed_v<NotFlags> == false, "");
			
		enum class Flags
		{
			One   = 1 << 0,
			Two   = 1 << 1,
			_bitwise_operators_allowed,
		};
		static_assert(has_bitops_allowed_v<Flags> == true, "");

		enum class Numbers
		{
			Zero     = 0,
			Two      = 2,
			One      = 1,
			Six      = 6,
			Three    = 3,
			MinusTwo = -2,

			_arithmetic_operators_allowed,
		};
		static_assert(has_arithops_allowed_v<Numbers> == true, "");
	}
	PRUnitTest(FlagsEnumTests)
	{
		using namespace unittests::flag_enum;

		{// Bitwise
			using Enum = Flags;
			//using Enum = NotFlags; // Uncomment to test not-compiling-ness

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

		// Doesn't work for some weird reason :(
		#define PR_ENABLE_ENUM_ARITHMETIC_OPERATORS 0
		#if PR_ENABLE_ENUM_ARITHMETIC_OPERATORS 
		{// Arithmetic
			using Enum = Numbers;

			PR_CHECK(+Enum::One == Enum::One, true);
			PR_CHECK(-Enum::Two == Enum::MinusTwo, true);

			PR_CHECK(Enum::One + Enum::Two == Enum::Three, true);
			PR_CHECK(Enum::Six - Enum::Three == Enum::Three, true);
			PR_CHECK(Enum::Two * Enum::Three == Enum::Six, true);
			PR_CHECK(Enum::Six / Enum::Two == Enum::Three, true);

			PR_CHECK(-2 + Enum::Three == Enum::One, true);
			PR_CHECK(Enum::Six - 5 == Enum::One, true);
			PR_CHECK(1 - Enum::Zero == Enum::One, true);
			PR_CHECK(Enum::MinusTwo * -3 == Enum::Six, true);
			PR_CHECK(-1 * Enum::Two == Enum::MinusTwo, true);
			PR_CHECK(Enum::Two / 2 == Enum::One, true);
			PR_CHECK(6 / Enum::Two == Enum::Three, true);
		}
		#endif
	}
}
#endif
