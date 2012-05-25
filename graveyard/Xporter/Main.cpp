//*********************************************************************************
//
// DLL entry point
//
//*********************************************************************************

#include "Headers.h"

HINSTANCE g_Instance;
BOOL APIENTRY DllMain(HINSTANCE module, DWORD  reason, LPVOID reserved)
{
	switch( reason )
	{
		case DLL_PROCESS_ATTACH:
			g_Instance = module;
			return TRUE;

		case DLL_THREAD_ATTACH:
		case DLL_THREAD_DETACH:
			return TRUE;

		case DLL_PROCESS_DETACH:
			return TRUE;
	}
	return FALSE;
}

extern "C"
{

_declspec(dllexport) int LibNumberClasses()
{
	return 0;
}

_declspec(dllexport) ClassDesc *LibClassDesc( int nClass )
{
	return NULL;
}

_declspec(dllexport) const TCHAR *LibDescription()
{
	return _T("X Exporter - Paul Ryland");
}

_declspec(dllexport) ULONG LibVersion()
{
	return VERSION_3DSMAX;
}

} // extern "C"