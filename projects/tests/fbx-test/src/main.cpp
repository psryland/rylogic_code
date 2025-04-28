#include <iostream>
//#include <eigen/Eigen/Core>
#include "src/fbx.h"
#include "pr/win32/win32.h"

using namespace pr;

#if 0
// Helper for setting the DLL search path before main()
extern "C" void __cdecl InitDllDirectory()
{
	// Platform based on pointer size
	constexpr wchar_t const* platform =
		sizeof(void*) == 8 ? L"x64" :
		sizeof(void*) == 4 ? L"x86" :
		L"";

	// NDEBUG is unreliable. Seems it's not always defined in release
	#if defined(_DEBUG) || defined(DEBUG)
	constexpr wchar_t const* config = L"debug";
	#else
	constexpr wchar_t const* config = L"release";
	#endif

	OutputDebugStringA("InitDll Called"); // Sends it to the debugger output window

	__debugbreak();

	// This function runs very, very early — before global/static initializers
	if (!SetDllDirectoryW(std::wstring(L"lib\\").append(platform).append(L"\\").append(config).c_str()))
	{
		DWORD err = GetLastError();

		// Log the error somewhere safe
		// You can't use fancy logging here yet — CRT is not fully ready
		char buf[256];
		snprintf(buf, sizeof(buf), "SetDllDirectoryW failed with error %lu\n", err);

		OutputDebugStringA(buf); // Sends it to the debugger output window
		// (Optional) printf to console if available
		// printf("%s", buf);
	}
}

// Force the linker to keep the symbol, even if it thinks it's unused
#ifdef _M_X64
#pragma comment(linker, "/include:InitDllDirectory")
#else
#pragma comment(linker, "/include:_InitDllDirectory")
#endif

// Mark the function to run during CRT init
#pragma section(".CRT$XCT", read)
__declspec(allocate(".CRT$XCT")) void (__cdecl* pInitDllDirectory)(void) = &InitDllDirectory;
#endif

//int Init()
//{
//	win32::LoadDll<struct FbxSdk>("libfbxsdk.dll");
//	return 1;
//}
//
//static int g_setup = Init();

int main()
{

	fbx::Manager fbx;
	fbx::Settings settings(fbx);
	fbx::Importer imp(fbx, settings);
	auto scene = imp.Import("E:\\Games\\Epic\\UE_5.5\\Engine\\Content\\FbxEditorAutomation\\AnimatedCharacter.fbx");
	WriteContent(scene, std::cout);
	return 0;
}
