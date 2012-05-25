//*************************************
// Count Of macro
//  Copyright © Rylogic Ltd 2009
//*************************************
// Use:
//	char arr[256];
//	for (int i = 0; i != PR_COUNTOF(arr); ++i) {...}
//
// Prevents accidental misuse such as:
//	char arr[256], *ptr = arr;
//	for (int i = 0; i != PR_COUNTOF(ptr); ++i) {...}

#pragma once
#ifndef PR_MACRO_COUNT_OF_H
#define PR_MACRO_COUNT_OF_H

namespace pr
{
	namespace impl
	{
		template <typename T, size_t N>
		char (&countofhelper(T(&)[N]))[N];
	}
}

#define PR_COUNTOF(arr) sizeof(pr::impl::countofhelper(arr))

#endif