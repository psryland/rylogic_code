//******************************************
// Value Cast
//  Copyright © Rylogic Ltd 2008
//******************************************
// Use to range check between casts to values of different bit lengths

#ifndef PR_VALUE_CAST_H
#define PR_VALUE_CAST_H

#include "pr/common/assert.h"

namespace pr
{
	// A casting function that checks for value overflow/underflow
	template <typename ToType, typename FromType>
	inline ToType value_cast(FromType value)
	{
		PR_ASSERT(PR_DBG, static_cast<FromType>(static_cast<ToType>(value)) == value, "data lost in cast");
		return static_cast<ToType>(value);
	}

	// No need to check when the types are the same
	template <typename Type>
	inline Type value_cast(Type value)
	{
		return value;
	}
}

#endif
