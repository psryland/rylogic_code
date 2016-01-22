//***********************************************************
// PR AutoExp
// Copyright (c) Rylogic Ltd 2002
//***********************************************************

#ifndef PR_EXPANSIONS_H
#define PR_EXPANSIONS_H

struct DbgHelper
{
	DWORD dwVersion;
	BOOL      (WINAPI *ReadDebuggeeMemory  )(DbgHelper* pThis, DWORD dwAddr, DWORD nWant, VOID* pWhere, DWORD *nGot);
	DWORDLONG (WINAPI *GetRealAddress      )(DbgHelper* pThis); // from here only when dwVersion >= 0x20000
	BOOL      (WINAPI *ReadDebuggeeMemoryEx)(DbgHelper* pThis, DWORDLONG qwAddr, DWORD nWant, VOID* pWhere, DWORD *nGot);
	int       (WINAPI *GetProcessorType    )(DbgHelper* pThis);

	// Read debugger memory from the base address associated with this debug helper
	HRESULT Read(void* obj, size_t size, size_t ofs = 0)
	{
		DWORD bytes_got;
		HRESULT r = ReadDebuggeeMemoryEx(this, GetRealAddress(this) + ofs, DWORD(size), obj, &bytes_got);
		return (r == S_OK && (size_t)bytes_got == size) ? S_OK : E_FAIL;
	}

	// Read debugger memory from an address
	HRESULT Read(void* obj, size_t size, void* address)
	{
		DWORD bytes_got;
		HRESULT r = ReadDebuggeeMemoryEx(this, *(DWORDLONG*)address, DWORD(size), obj, &bytes_got);
		return (r == S_OK && (size_t)bytes_got == size) ? S_OK : E_FAIL;
	}

	// VC 6.0 version
	HRESULT ReadVC6(void* obj, size_t size, DWORD dwAddress)
	{
		DWORD bytes_got;
		HRESULT r = ReadDebuggeeMemory(this, dwAddress, DWORD(size), obj, &bytes_got);
		return (r == S_OK && (size_t)bytes_got == size) ? S_OK : E_FAIL;
	}

	// Template specialisations
	template <typename Type> HRESULT Read(Type& type, size_t ofs = 0) { return Read(&type, sizeof(type), ofs); }
	template <>              HRESULT Read(std::string& str, size_t ofs);
	template <>              HRESULT Read(std::wstring& str, size_t ofs);
};

#define ADDIN_API __declspec(dllexport)
typedef HRESULT (WINAPI *AddIn_Function)(DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);

#if 0
// To use these expansions, edit your autoexp.dat file:
#include "c:\program files\microsoft visual studio 9.0\common7\packages\debugger\autoexp.dat"
template =$ADDIN(q:\sdk\pr\lib\prautoexp.win32.release.dll,AddIn_template)
#endif

	// Exported expansion functions
extern "C" ADDIN_API HRESULT WINAPI AddIn_v2                 (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_v3                 (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_v4                 (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_iv4                (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_i64v4              (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_m3x4               (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_m4x4               (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_MAXMatrix3         (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_stdvector          (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_stdstring          (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_stdstringstream    (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_stdifstream        (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_stdofstream        (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_Quaternion         (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_MD5                (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_LargeInt           (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_QuaternionAsMatrix (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_phShape            (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_LuaState           (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
extern "C" ADDIN_API HRESULT WINAPI AddIn_DateTime           (DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);

#endif
