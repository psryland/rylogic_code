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

	template <typename T> concept HasFlagsEnumField = std::is_enum_v<T> && requires(T t) { T::_flags_enum; };
	template <typename T> concept HasArithEnumField = std::is_enum_v<T> && requires(T t) { T::_arith_enum; };

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
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(~static_cast<UT>(lhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum operator | (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) | static_cast<UT>(rhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum operator & (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) & static_cast<UT>(rhs));
	}
	template <FlagsEnum TEnum> constexpr TEnum operator ^ (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) ^ static_cast<UT>(rhs));
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
	template <FlagsEnum TEnum, std::integral T> constexpr TEnum operator << (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) << rhs);
	}
	template <FlagsEnum TEnum, std::integral T> constexpr TEnum operator >> (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) >> rhs);
	}
	template <FlagsEnum TEnum, std::integral T> constexpr TEnum& operator <<= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs << rhs);
	}
	template <FlagsEnum TEnum, std::integral T> constexpr TEnum& operator >>= (TEnum& lhs, T rhs)
	{
		return lhs = (lhs >> rhs);
	}
	template <FlagsEnum TEnum, std::integral T> constexpr bool operator == (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<UT>(lhs) == static_cast<UT>(rhs);
	}
	template <FlagsEnum TEnum, std::integral T> constexpr bool operator == (T lhs, TEnum rhs)
	{
		return rhs == lhs;
	}
	template <FlagsEnum TEnum, std::integral T> constexpr bool operator != (TEnum lhs, T rhs)
	{
		return !(lhs == rhs);
	}
	template <FlagsEnum TEnum, std::integral T> constexpr bool operator != (T lhs, TEnum rhs)
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
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(-static_cast<UT>(lhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator + (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) + static_cast<UT>(rhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator - (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) - static_cast<UT>(rhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator * (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) * static_cast<UT>(rhs));
	}
	template <ArithEnum TEnum> constexpr TEnum operator / (TEnum lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) / static_cast<UT>(rhs));
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator + (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) + rhs);
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator + (T lhs, TEnum rhs)
	{
		return rhs + lhs;
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator - (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) - rhs);
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator - (T lhs, TEnum rhs)
	{
		return -(rhs - lhs);
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator * (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) * rhs);
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator * (T lhs, TEnum rhs)
	{
		return rhs * lhs;
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator / (TEnum lhs, T rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(static_cast<UT>(lhs) / rhs);
	}
	template <ArithEnum TEnum, std::integral T> constexpr TEnum operator / (T lhs, TEnum rhs)
	{
		using UT = std::underlying_type_t<TEnum>;
		return static_cast<TEnum>(lhs / static_cast<UT>(rhs));
	}

#endif

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::common
{
	PRUnitTestClass(FlagsEnumTests)
	{
		enum class NotFlags
		{
			One = 1,
			Two = 2,
		};
		static_assert(is_flags_enum_v<NotFlags> == false);

		enum class Flags : uint32_t // Flags should be unsigned types
		{
			One = 1 << 0,
			Two = 1 << 1,

			_flags_enum = 0,
		};
		static_assert(FlagsEnum<Flags>);

		enum class Numbers
		{
			Zero = 0,
			Two = 2,
			One = 1,
			Six = 6,
			Three = 3,
			MinusTwo = -2,

			_arith_enum = 0,
		};
		static_assert(ArithEnum<Numbers>);

		PRUnitTestMethod(Bitwise)
		{
			using E = Flags;
			//using E = NotFlags; // Uncomment to test not-compiling-ness

			auto a = E::One | E::Two;
			auto b = E::One & E::Two;
			auto c = E::One ^ E::Two;
			auto f = ~E::One;

			PR_EXPECT((int)a == 3);
			PR_EXPECT((int)b == 0);
			PR_EXPECT((int)c == 3);
			PR_EXPECT((int)f == -2);

			a |= E::Two;
			b &= E::Two;
			c ^= E::Two;

			PR_EXPECT((int)a == 3);
			PR_EXPECT((int)b == 0);
			PR_EXPECT((int)c == 1);
		}
		PRUnitTestMethod(Arithmetic)
		{
			using E = Numbers;
			static_assert(ArithEnum<E>);

			PR_EXPECT(+E::One == E::One);
			PR_EXPECT(-E::Two == E::MinusTwo);

			PR_EXPECT(E::One + E::Two == E::Three);
			PR_EXPECT(E::Six - E::Three == E::Three);
			PR_EXPECT(E::Two * E::Three == E::Six);
			PR_EXPECT(E::Six / E::Two == E::Three);

			PR_EXPECT(-2 + E::Three == E::One);
			PR_EXPECT(E::Six - 5 == E::One);
			PR_EXPECT(1 - E::Zero == E::One);
			PR_EXPECT(E::MinusTwo * -3 == E::Six);
			PR_EXPECT(-1 * E::Two == E::MinusTwo);
			PR_EXPECT(E::Two / 2 == E::One);
			PR_EXPECT(6 / E::Two == E::Three);
		}
	};
}
#endif
