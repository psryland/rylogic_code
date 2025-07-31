#pragma once
#include <string_view>
#include <windows.h>

namespace pr::threads
{
	// Static thread local storage for the thread name
	inline static thread_local char thread_name[32];

	// Get the assigned name for the current thread (must call 'SetCurrentThreadName' first)
	inline char const* GetCurrentThreadName()
	{
		return &thread_name[0];
	}

	// Sets a name for the current thread
	inline void SetCurrentThreadName(std::string_view name)
	{
		// Save the name to the thread local storage
		memset(&thread_name[0], 0, sizeof(thread_name));
		memcpy(&thread_name[0], name.data(), std::min(name.size(), sizeof(thread_name) - 1));
		thread_name[_countof(thread_name) - 1] = 0;

		// Call 'SetThreadDescription'. 'SetThreadDescription' only exists on >= Win10
		auto kernal32 = ::LoadLibraryW(L"kernel32.dll");
		if (kernal32 != nullptr)
		{
			#pragma warning(push)
			#pragma warning(disable: 4191) // 'reinterpret_cast': unsafe conversion from 'FARPROC' to function pointer
			using SetThreadDescriptionFn = HRESULT(__stdcall*)(HANDLE hThread, PCWSTR lpThreadDescription);
			auto set_thread_desc = reinterpret_cast<SetThreadDescriptionFn>(GetProcAddress(kernal32, "SetThreadDescription"));
			#pragma warning(pop)

			if (set_thread_desc != nullptr)
			{
				wchar_t wname[32];
				MultiByteToWideChar(CP_ACP, MB_PRECOMPOSED, &thread_name[0], -1, &wname[0], _countof(wname));
				set_thread_desc(GetCurrentThread(), &wname[0]);
			}
			::FreeLibrary(kernal32);
			return;
		}
		// Fall back to the old method
		#if _WIN32_WINNT < _WIN32_WINNT_WIN10
		{
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
			info.szName = thread_name;
			info.dwThreadID = (DWORD)-1;
			info.dwFlags = 0;
			__try { RaiseException(MS_VC_EXCEPTION, 0, sizeof(info) / sizeof(ULONG_PTR), (ULONG_PTR*)&info); }
			__except (EXCEPTION_EXECUTE_HANDLER) {}
		}
		#endif
	}
}
