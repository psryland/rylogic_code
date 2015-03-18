//******************************************************************************
// Flags Enum
//  Copyright (c) Rylogic Ltd 2014
//******************************************************************************
#pragma once

#include <type_traits>

#define PR_ENUM_FLAG_OPERATORS(TEnum)\
	inline TEnum operator ~ (TEnum lhs)\
	{\
		return TEnum(~static_cast<std::underlying_type<TEnum>::type>(lhs));\
	}\
	inline TEnum operator | (TEnum lhs, TEnum rhs)\
	{\
		return TEnum(std::underlying_type<TEnum>::type(lhs) | std::underlying_type<TEnum>::type(rhs));\
	}\
	inline TEnum operator & (TEnum lhs, TEnum rhs)\
	{\
		return TEnum(std::underlying_type<TEnum>::type(lhs) & std::underlying_type<TEnum>::type(rhs));\
	}\
	inline TEnum operator ^ (TEnum lhs, TEnum rhs)\
	{\
		return TEnum(std::underlying_type<TEnum>::type(lhs) ^ std::underlying_type<TEnum>::type(rhs));\
	}\
	inline TEnum& operator |= (TEnum& lhs, TEnum rhs)\
	{\
		return lhs = (lhs | rhs);\
	}\
	inline TEnum& operator &= (TEnum& lhs, TEnum rhs)\
	{\
		return lhs = (lhs & rhs);\
	}\
	inline TEnum& operator ^= (TEnum& lhs, TEnum rhs)\
	{\
		return lhs = (lhs ^ rhs);\
	}\
	inline bool operator == (TEnum lhs, std::underlying_type<TEnum>::type rhs)\
	{\
		return std::underlying_type<TEnum>::type(lhs) == rhs;\
	}\
	inline bool operator == (std::underlying_type<TEnum>::type lhs, TEnum rhs)\
	{\
		return rhs == lhs;\
	}\
	inline bool operator != (TEnum lhs, std::underlying_type<TEnum>::type rhs)\
	{\
		return !(lhs == rhs);\
	}\
	inline bool operator != (std::underlying_type<TEnum>::type lhs, TEnum rhs)\
	{\
		return rhs != lhs;\
	}

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
			PR_ENUM_FLAG_OPERATORS(Flags);
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
