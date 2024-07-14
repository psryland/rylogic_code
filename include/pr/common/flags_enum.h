//******************************************************************************
// Flags Enum
//  Copyright (c) Rylogic Ltd 2014
//******************************************************************************
// Add '_flags_enum = 0' to your enum for bitwise operators
// Add '_arith_enum = 0' to your enum for arithmetic operators
// The '= 0' prevents a warning about forgetting to assign a value
#pragma once

// These are in the global namespace so that they work in any namespace

#ifdef __cplusplus // C does not require operators
#include <type_traits>

	template <typename T> concept HasFlagsEnumField = requires(T t) { std::is_enum_v<T>; T::_flags_enum; };
	template <typename T> concept HasArithEnumField = requires(T t) { std::is_enum_v<T>; T::_arith_enum; };

	// Traits
	template <typename T> struct is_flags_enum : std::false_type {};
	template <HasFlagsEnumField T> struct is_flags_enum<T> :std::true_type {};
	template <typename T> constexpr bool is_flags_enum_v = is_flags_enum<T>::value;

	template <typename T> struct is_arith_enum :std::false_type {};
	template <HasArithEnumField T> struct is_arith_enum<T> :std::true_type {};
	template <typename T> constexpr bool is_arith_enum_v = is_arith_enum<T>::value;

	// Concepts
	template <typename T>
	concept FlagsEnum = is_flags_enum_v<T>;
	template <typename T>
	concept ArithEnum = is_arith_enum_v<T>;

	// Define the bitwise operators
	template <FlagsEnum TEnum> constexpr TEnum operator ~ (TEnum lhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(~static_cast<ut>(lhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum operator | (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) | ut(rhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum operator & (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) & ut(rhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum operator ^ (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) ^ ut(rhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum& operator |= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs | rhs);
	}
	template <FlagsEnum TEnum> constexpr TEnum& operator &= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs & rhs);
	}
	template <FlagsEnum TEnum> constexpr TEnum& operator ^= (TEnum& lhs, TEnum rhs)
	{
		return lhs = (lhs ^ rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr TEnum operator << (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) << rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr TEnum operator >> (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(ut(lhs) >> rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr TEnum& operator <<= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs << rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr TEnum& operator >>= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs >> rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr bool operator == (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return ut(lhs) == ut(rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr bool operator == (T lhs, TEnum rhs)
	{
		return rhs == lhs;
	}
	template <FlagsEnum TEnum, typename T> constexpr bool operator != (TEnum lhs, T rhs)
	{
		return !(lhs == rhs);
	}
	template <FlagsEnum TEnum, typename T> constexpr bool operator != (T lhs, TEnum rhs)
	{
		return rhs != lhs;
	}

	// Define the arithmetic operators
	template <ArithEnum TEnum> inline TEnum& operator ++ (TEnum& lhs)
	{
		return lhs = static_cast<TEnum>(lhs + 1);
	}
	template <ArithEnum TEnum> constexpr TEnum operator + (TEnum lhs)
	{
		return lhs;
	}
	template <ArithEnum TEnum> constexpr TEnum operator - (TEnum lhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(-static_cast<ut>(lhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator + (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) + static_cast<ut>(rhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator - (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) - static_cast<ut>(rhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator * (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) * static_cast<ut>(rhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator / (TEnum lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) / static_cast<ut>(rhs));
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator + (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) + rhs);
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator + (T lhs, TEnum rhs)
	{
		return rhs + lhs;
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator - (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) - rhs);
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator - (T lhs, TEnum rhs)
	{
		return -(rhs - lhs);
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator * (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) * rhs);
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator * (T lhs, TEnum rhs)
	{
		return rhs * lhs;
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator / (TEnum lhs, T rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(static_cast<ut>(lhs) / rhs);
	}
	template <ArithEnum TEnum, typename T> constexpr TEnum operator / (T lhs, TEnum rhs)
	{
		using ut = typename std::underlying_type<TEnum>::type;
		return TEnum(lhs / static_cast<ut>(rhs));
	}

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
		static_assert(is_flags_enum_v<NotFlags> == false, "");
			
		enum class Flags
		{
			One   = 1 << 0,
			Two   = 1 << 1,

			_flags_enum = 0,
		};
		static_assert(is_flags_enum_v<Flags> == true, "");

		enum class Numbers
		{
			Zero     = 0,
			Two      = 2,
			One      = 1,
			Six      = 6,
			Three    = 3,
			MinusTwo = -2,

			_arith_enum = 0,
		};
		static_assert(is_arith_enum_v<Numbers> == true, "");
	}
	PRUnitTest(FlagsEnumTests)
	{
		using namespace unittests::flag_enum;

		{// Bitwise
			using E = Flags;
			//using E = NotFlags; // Uncomment to test not-compiling-ness

			auto a =  E::One | E::Two;
			auto b =  E::One & E::Two;
			auto c =  E::One ^ E::Two;
			auto f = ~E::One;

			PR_CHECK((int)a, 3);
			PR_CHECK((int)b, 0);
			PR_CHECK((int)c, 3);
			PR_CHECK((int)f, -2);

			a |= E::Two;
			b &= E::Two;
			c ^= E::Two;

			PR_CHECK((int)a, 3);
			PR_CHECK((int)b, 0);
			PR_CHECK((int)c, 1);
		}

		{// Arithmetic
			using E = Numbers;

			PR_CHECK(+E::One == E::One, true);
			PR_CHECK(-E::Two == E::MinusTwo, true);

			PR_CHECK(E::One + E::Two == E::Three, true);
			PR_CHECK(E::Six - E::Three == E::Three, true);
			PR_CHECK(E::Two * E::Three == E::Six, true);
			PR_CHECK(E::Six / E::Two == E::Three, true);

			PR_CHECK(-2 + E::Three == E::One, true);
			PR_CHECK(E::Six - 5 == E::One, true);
			PR_CHECK(1 - E::Zero == E::One, true);
			PR_CHECK(E::MinusTwo * -3 == E::Six, true);
			PR_CHECK(-1 * E::Two == E::MinusTwo, true);
			PR_CHECK(E::Two / 2 == E::One, true);
			PR_CHECK(6 / E::Two == E::Three, true);
		}
	}
}
#endif
