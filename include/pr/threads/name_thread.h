#pragma once
#include <windows.h>

namespace pr::threads
{
	// Static thread local storage for the thread name
	inline static thread_local char thread_name[32];

	// Sets a name for the current thread
	inline void SetCurrentThreadName(char const* name)
	{
		strncpy(&thread_name[0], name, _countof(thread_name) - 1);
		thread_name[_countof(thread_name)-1] = 0;

		// This will fail on old windows versions
		wchar_t wname[32];
		MultiByteToWideChar(CP_ACP, MB_PRECOMPOSED, name, -1, &wname[0], _countof(wname));
		SetThreadDescription(GetCurrentThread(), &wname[0]);

		// Fallback to the old method
		#if _WIN32_WINNT < _WIN32_WINNT_WIN10
		constexpr DWORD MS_VC_EXCEPTION = 0x406D1388;

		#pragma pack(push,8)
		struct MS_THREADNAME_INFO
		{
			DWORD dwType;     // Must be 0x1000.
			LPCSTR szName;    // Pointer to name (in user addr space).
			DWORD dwThreadID; // Thread ID (-1=caller thread).
			DWORD dwFlags;    // Reserved for future use, must be zero.
		};
		#pragma pack(pop)

		MS_THREADNAME_INFO info;
		info.dwType = 0x1000;
		info.szName = name;
		info.dwThreadID = (DWORD)-1;
		info.dwFlags = 0;
		__try { RaiseException(MS_VC_EXCEPTION, 0, sizeof(info)/sizeof(ULONG_PTR), (ULONG_PTR*)&info); }
		__except(EXCEPTION_EXECUTE_HANDLER) {}
		#endif
	}

	// Get the assigned name for the current thread (must call 'SetCurrentThreadName' first)
	inline char const* GetCurrentThreadName()
	{
		return &thread_name[0];
	}
}
