#pragma once

#include <string>
#include <windows.h>

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
	template <typename Type>
	HRESULT Read(Type& type, size_t ofs = 0)
	{
		return Read(&type, sizeof(type), ofs);
	}
	//template <>              HRESULT Read(std::string& str, size_t ofs);
	//template <>              HRESULT Read(std::wstring& str, size_t ofs);
};

using AddIn_Function = HRESULT (WINAPI *)(DWORD dwAddress, DbgHelper* pHelper, int nBase, BOOL bUniStrings, char *pResult, size_t max, DWORD reserved);
