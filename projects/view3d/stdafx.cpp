#include "stdafx.h"

CAppModule g_module;

#ifdef _MANAGED
#pragma managed(push, off)
#endif
BOOL APIENTRY DllMain(HMODULE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	case DLL_PROCESS_ATTACH: g_module.Init(0, hInstance); break;
	case DLL_PROCESS_DETACH: g_module.Term(); break;
	}
	return TRUE;
}
#ifdef _MANAGED
#pragma managed(pop)
#endif
