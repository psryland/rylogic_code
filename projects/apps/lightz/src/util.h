#pragma once
#include "forward.h"

namespace lightz
{
	// Case insensitive string comparison
	template <int N>
	inline bool StartsWith(String const& str, char const(&match)[N])
	{
		return str.length() >= N - 1 && lwip_strnicmp(str.c_str(), &match[0], N - 1) == 0;
	}
}
