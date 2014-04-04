//******************************************************************************
// Flags Enum
//  Copyright © Rylogic Ltd 2014
//******************************************************************************
// Bitwise operators for enums with the flags_enum type_trait
#pragma once

#include <type_traits>

namespace pr
{
	namespace meta
	{
		template <typename TEnum> struct is_flags_enum
		{
			enum { value = false };
		};
	}
}

#define PR_DEFINE_ENUM_FLAG_BINARYOP(op)\
	template <typename TEnum>\
	inline typename std::enable_if<pr::meta::is_flags_enum<TEnum>::value, TEnum>::type\
	operator op (TEnum lhs, TEnum rhs)\
	{\
		auto lhs_ = static_cast<std::underlying_type<TEnum>::type>(lhs);\
		auto rhs_ = static_cast<std::underlying_type<TEnum>::type>(rhs);\
		return static_cast<TEnum>(lhs_ op rhs_);\
	}\
	template <typename TEnum>\
	inline typename std::enable_if<pr::meta::is_flags_enum<TEnum>::value, TEnum>::type&\
	operator op##= (TEnum& lhs, TEnum rhs)\
	{\
		return lhs = (lhs op rhs);\
	}

#define PR_DEFINE_ENUM_FLAG_UNARYOP(op)\
	template <typename TEnum>\
	inline typename std::enable_if<pr::meta::is_flags_enum<TEnum>::value, TEnum>::type\
	operator op (TEnum val)\
	{\
		auto val_ = static_cast<std::underlying_type<TEnum>::type>(val);\
		return static_cast<TEnum>(op##val_);\
	}

PR_DEFINE_ENUM_FLAG_BINARYOP(|)
PR_DEFINE_ENUM_FLAG_BINARYOP(&)
PR_DEFINE_ENUM_FLAG_BINARYOP(^)
PR_DEFINE_ENUM_FLAG_UNARYOP(~)

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
		}
	}
}

template <> struct ::pr::meta::is_flags_enum<pr::unittests::flag_enum::Flags> { enum { value = true }; };

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_common_flags_enum)
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
