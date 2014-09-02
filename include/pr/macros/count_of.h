//*************************************
// Count Of macro
//  Copyright (c) Rylogic Ltd 2009
//*************************************
// Use:
//	char arr[256];
//	for (int i = 0; i != PR_COUNTOF(arr); ++i) {...}
//
// Prevents accidental misuse such as:
//	char arr[256], *ptr = arr;
//	for (int i = 0; i != PR_COUNTOF(ptr); ++i) {...}

#pragma once

namespace pr
{
	namespace impl
	{
		template <typename T, size_t N>
		char (&countofhelper(T(&)[N]))[N];
	}
}

#define PR_COUNTOF(arr) sizeof(pr::impl::countofhelper(arr))
