//**********************************************
// Win32 Memory Check
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#pragma once
#include <cassert>
#include <cstdint>

#ifdef _MSC_VER
#include <Windows.h>
#else
#include <unistd.h>
#include <sys/resource.h>
#endif

namespace pr
{
	// Returns true if 'ptr' is likely a pointer into the stack
	inline bool IsStackPointer(void const* ptr)
	{
		// Use fiber local storage to get stack base
		#ifdef _MSC_VER
		void* stack_low = nullptr;
		void* stack_high = nullptr;
		GetCurrentThreadStackLimits((ULONG_PTR*)&stack_low, (ULONG_PTR*)&stack_high);
		return ptr >= stack_low && ptr < stack_high;
		#else
		// POSIX fall back — not perfect, but close
		rlimit stack_limit;
		getrlimit(RLIMIT_STACK, &stack_limit);
		uintptr_t stack_top = reinterpret_cast<uintptr_t>(&ptr);
		uintptr_t stack_base = stack_top - stack_limit.rlim_cur;
		return reinterpret_cast<uintptr_t>(ptr) >= stack_base && reinterpret_cast<uintptr_t>(ptr) < stack_top;
		#endif
	}
}
