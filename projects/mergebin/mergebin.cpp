//*******************************************************
// mergebin
// Written by Robert Simpson (robert@blackcastlesoft.com)
//
// Released to the public domain, use at your own risk!
//*******************************************************

#include "mergebin/forward.h"
#include "mergebin/meta_data.h"
#include "mergebin/meta_data_tables.h"
#include "mergebin/table_data.h"

struct EXTRA_STUFF
{
	DWORD dwNativeEntryPoint;
};

// Find 'find' in 'buffer' of length 'size'
inline BYTE* memstr(BYTE* buffer, char const* find, DWORD size)
{
	auto findsize = lstrlenA(find);
	for (auto p = buffer; p <= (buffer - findsize + size); p++)
	{
		if (memcmp(p, find, findsize) == 0)
			return p; // found
	}
	return nullptr;
}

// Applies a work around to CE binaries
void FixObjFile(LPCTSTR pszFile)
{
	auto hFile = CreateFile(pszFile, GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
	if (hFile == INVALID_HANDLE_VALUE)
		return;

	auto dwSize = GetFileSize(hFile, nullptr);
	auto hMap = CreateFileMapping(hFile, nullptr, PAGE_READWRITE, 0, 0, nullptr);
	if (hMap)
	{
		auto p = (BYTE*)MapViewOfFile(hMap, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, 0);
		if (p)
		{
			auto section = reinterpret_cast<IMAGE_SECTION_HEADER*>(memstr(p, ".bss", dwSize));
			if (section)
			{
				section->Characteristics &= ~IMAGE_SCN_CNT_UNINITIALIZED_DATA;
				section->Characteristics |= IMAGE_SCN_CNT_INITIALIZED_DATA;
			}
			UnmapViewOfFile(p);
		}
		CloseHandle(hMap);
	}
	CloseHandle(hFile);
}

bool GetMinMaxCOR20RVA(pr::PEFile& file, DWORD& dwMin, DWORD& dwMax)
{
	dwMin = MAXDWORD;
	dwMax = 0;

	auto pCor = (IMAGE_COR20_HEADER*)file;
	if (!pCor)
		return false;

	if (pCor->MetaData               .Size) dwMin = std::min(dwMin, pCor->MetaData               .VirtualAddress);
	if (pCor->Resources              .Size) dwMin = std::min(dwMin, pCor->Resources              .VirtualAddress);
	if (pCor->StrongNameSignature    .Size) dwMin = std::min(dwMin, pCor->StrongNameSignature    .VirtualAddress);
	if (pCor->CodeManagerTable       .Size) dwMin = std::min(dwMin, pCor->CodeManagerTable       .VirtualAddress);
	if (pCor->VTableFixups           .Size) dwMin = std::min(dwMin, pCor->VTableFixups           .VirtualAddress);
	if (pCor->ExportAddressTableJumps.Size) dwMin = std::min(dwMin, pCor->ExportAddressTableJumps.VirtualAddress);
	if (pCor->ManagedNativeHeader    .Size) dwMin = std::min(dwMin, pCor->ManagedNativeHeader    .VirtualAddress);
	dwMax = std::max(dwMax, pCor->MetaData               .VirtualAddress + pCor->MetaData               .Size);
	dwMax = std::max(dwMax, pCor->Resources              .VirtualAddress + pCor->Resources              .Size);
	dwMax = std::max(dwMax, pCor->StrongNameSignature    .VirtualAddress + pCor->StrongNameSignature    .Size);
	dwMax = std::max(dwMax, pCor->CodeManagerTable       .VirtualAddress + pCor->CodeManagerTable       .Size);
	dwMax = std::max(dwMax, pCor->VTableFixups           .VirtualAddress + pCor->VTableFixups           .Size);
	dwMax = std::max(dwMax, pCor->ExportAddressTableJumps.VirtualAddress + pCor->ExportAddressTableJumps.Size);
	dwMax = std::max(dwMax, pCor->ManagedNativeHeader    .VirtualAddress + pCor->ManagedNativeHeader    .Size);

	CMetadata meta(file);
	CMetadataTables tables(meta);
	for (auto tt : {ETableTypes::MethodDef, ETableTypes::FieldRVA})
	{
		auto p = tables.GetTable(tt);
		if (p)
		{
			for (UINT r = 0, rend = p->GetRowCount(); r != rend; ++r)
			{
				auto pdwRVA = reinterpret_cast<DWORD*>(p->Column(r, 0U));
				if (*pdwRVA)
					dwMin = std::min(dwMin, (*pdwRVA));
			}
		}
	}
	return true;
}

// Output the required space needed to embedded the .NET assembly in a native assembly
void DumpCLRInfo(TCHAR const* pszFile)
{
	pr::PEFile peFile(pszFile);

	DWORD dwMinRVA, dwMaxRVA;
	if (!GetMinMaxCOR20RVA(peFile, dwMinRVA, dwMaxRVA))
		_tprintf(_T("Unable to retrieve .NET assembly information for file %s\n"), pszFile);
	else
		_tprintf(_T("%d Bytes required to merge %s\n"), int((dwMaxRVA - dwMinRVA) + ((PIMAGE_COR20_HEADER)peFile)->cb + sizeof(EXTRA_STUFF)), pszFile);
}

// Output the generated code for including in the native dll
void DumpCLRPragma(LPCTSTR assembly_filepath, LPCTSTR section_name)
{
	pr::PEFile peFile(assembly_filepath);
	section_name = section_name ? section_name : _T(".clr");

	DWORD dwMinRVA, dwMaxRVA;
	if (!GetMinMaxCOR20RVA(peFile, dwMinRVA, dwMaxRVA))
		printf("// Unable to retrieve .NET assembly information for file %s\n", assembly_filepath);
	else
		printf(R"(
// This code was automatically generated from assembly
// %s

#include <windef.h>

#pragma data_seg(push, clrseg, "%s")
#pragma comment(linker, "/SECTION:%s,ER")
char __ph[%d] = {0}; // The number of bytes to reserve
#pragma data_seg(pop, clrseg)

typedef BOOL (WINAPI *DLLMAIN)(HANDLE, DWORD, LPVOID);
struct EXTRA_STUFF
{
	DWORD dwNativeEntryPoint;
};

__declspec(dllexport) BOOL WINAPI _CorDllMainStub(HANDLE hModule, DWORD dwReason, LPVOID pvReserved)
{
	DLLMAIN proc;

	auto hMod = GetModuleHandleW(L"mscoree");
	if (hMod)
	{
		proc = (DLLMAIN)GetProcAddress(hMod, "_CorDllMain");
	}
	else
	{
		MEMORY_BASIC_INFORMATION mbi;
		VirtualQuery(_CorDllMainStub, &mbi, sizeof(mbi));
		auto pExtra = (EXTRA_STUFF*)__ph;
		proc = (DLLMAIN)(pExtra->dwNativeEntryPoint + (DWORD)mbi.AllocationBase);
	}
	return proc(hModule, dwReason, pvReserved);
})"
			,assembly_filepath
			,section_name
			,section_name
			,int((dwMaxRVA - dwMinRVA) + ((PIMAGE_COR20_HEADER)peFile)->cb + sizeof(EXTRA_STUFF)));
}

// When merged, the native DLL's entrypoint must go to _CorDllMain in MSCOREE.DLL.
// In order to do this, we need to change the DLL's entrypoint to "something" that will
// call CorDllMain.  Since its too much hassle to add imports to the DLL and make drastic
// changes to it, we rely on the native DLL to export a function that we can call which will
// forward to CorDllMain.  Exported functions are easy to identify and get an RVA for.
// The exported function must have the same calling conventions and parameters as DllMain,
// and must contain the letters "CORDLLMAIN" in the name.  The search is case-insensitive.
DWORD GetExportedCorDllMainRVA(pr::PEFile& file)
{
	DWORD exports_rva_beg, exports_rva_end;
	if (IMAGE_NT_HEADERS32* hdr32 = file)
	{
		exports_rva_beg = hdr32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
		exports_rva_end = hdr32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].Size + exports_rva_beg;
	}
	else if (IMAGE_NT_HEADERS64* hdr64 = file)
	{
		exports_rva_beg = hdr64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
		exports_rva_end = hdr64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].Size + exports_rva_beg;
	}
	else
	{
		throw std::exception("NT image header missing");
	}

	auto header = file.EnclosingSectionHeader(exports_rva_beg);
	if (!header)
		return 0;

	auto pExportDir   = (IMAGE_EXPORT_DIRECTORY*)file.PtrFromRVA(exports_rva_beg);
	auto pdwFunctions = (DWORD*                 )file.PtrFromRVA(pExportDir->AddressOfFunctions);
	auto pwOrdinals   = (WORD*                  )file.PtrFromRVA(pExportDir->AddressOfNameOrdinals);
	auto pszFuncNames = (DWORD*                 )file.PtrFromRVA(pExportDir->AddressOfNames);
	for (DWORD i = 0; i != pExportDir->NumberOfFunctions; ++i, ++pdwFunctions)
	{
		auto entryPointRVA = *pdwFunctions;
		if (entryPointRVA == 0)
			continue;

		for (UINT j = 0; j < pExportDir->NumberOfNames; j++)
		{
			if (pwOrdinals[j] == i)
			{
				char szName[MAX_PATH + 1] = {};
				lstrcpynA(szName, (char const*)file.PtrFromRVA(pszFuncNames[j]), MAX_PATH);
				CharUpper(szName);
				if (strstr(szName, "CORDLLMAIN") != 0)
					return entryPointRVA;
			}
		}
	}
	return 0;
}

// Merges a pure .NET assembly with a native DLL, inserting it into the specified section
void MergeModules(LPCTSTR assembly_filepath, LPCTSTR native_filepath, LPCTSTR section_name, DWORD dwAdjust)
{
	// Open the .NET assembly
	pr::PEFile peFile(assembly_filepath);

	// Scan the .NET assembly and find the block of .NET code specified in the .NET metadata
	DWORD dwMinRVA, dwMaxRVA;
	if (!GetMinMaxCOR20RVA(peFile, dwMinRVA, dwMaxRVA))
	{
		printf("Unable to retrieve .NET assembly information for file %s\n", assembly_filepath);
		return;
	}

	// Open the destination file for readwrite access
	pr::PEFile peDest(native_filepath, false);

	// Make sure it has the section specified in the command-line
	auto pSection = peDest.SectionHeader(section_name);
	if (!pSection)
	{
		printf("Unable to find section %s in file\n", section_name);
		return;
	}

	// Total number of bytes of the block of .NET code we're going to merge
	auto dwSize = (dwMaxRVA - dwMinRVA) + ((PIMAGE_COR20_HEADER)peFile)->cb;
	BYTE* pSrc, *pDest;

	// If the section isn't large enough, tell the user how large it needs to be
	if (pSection->Misc.VirtualSize < dwSize + sizeof(EXTRA_STUFF))
	{
		printf("Not enough room in section for data.  Need %d bytes\n", int(dwSize + sizeof(EXTRA_STUFF)));
		return;
	}

	// Find a new entrypoint to use for the DLL.  The old entrypoint is written into the .NET header
	auto dwNewEntrypoint = GetExportedCorDllMainRVA(peDest);
	if (!dwNewEntrypoint)
	{
		printf("Native DLL must export a function that calls _CorDllMain, and its name must contain the word \"CorDllMain\".\n");
		return;
	}

	// Change this section's flags
	pSection->Characteristics = IMAGE_SCN_CNT_CODE | IMAGE_SCN_MEM_EXECUTE | IMAGE_SCN_MEM_READ;

	auto dwDestRVA = pSection->VirtualAddress;
	auto pExtra = (EXTRA_STUFF*)peDest.PtrFromRVA(dwDestRVA);
	dwDestRVA += sizeof(EXTRA_STUFF);

	// If the native DLL has been merged with an assembly beforehand, we need to strip the .NET stuff and restore the entrypoint
	auto pCor = (IMAGE_COR20_HEADER*)peDest;
	if (pCor)
	{
		if (pCor->Flags & 0x10)
		{
			if      (IMAGE_NT_HEADERS32* hdr32 = peDest) hdr32->OptionalHeader.AddressOfEntryPoint = pCor->EntryPointToken;
			else if (IMAGE_NT_HEADERS64* hdr64 = peDest) hdr64->OptionalHeader.AddressOfEntryPoint = pCor->EntryPointToken;
			else throw std::exception("NT image header missing");
		}
	}

	// Copy the assembly's .NET header into the section
	dwSize = ((IMAGE_COR20_HEADER*)peFile)->cb;
	pSrc   = (BYTE*)(PIMAGE_COR20_HEADER)peFile;
	pDest  = (BYTE*)peDest.PtrFromRVA(dwDestRVA);
	memcpy(pDest, pSrc, dwSize);

	// Fixup the NT header on the native DLL to include the new .NET header
	if (IMAGE_NT_HEADERS32* hdr32 = peDest)
	{
		pExtra->dwNativeEntryPoint = hdr32->OptionalHeader.AddressOfEntryPoint;
		hdr32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress = dwDestRVA;
		hdr32->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size = dwSize;
	}
	else if (IMAGE_NT_HEADERS64* hdr64 = peDest)
	{
		pExtra->dwNativeEntryPoint = hdr64->OptionalHeader.AddressOfEntryPoint;
		hdr64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress = dwDestRVA;
		hdr64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size = dwSize;
	}
	dwDestRVA += dwSize;
	if (dwDestRVA % 4) dwDestRVA += (4 - (dwDestRVA % 4));

	// Copy the .NET block of code and metadata into the section, after the header
	dwSize = dwMaxRVA - dwMinRVA;
	pSrc  = (BYTE*)peFile.PtrFromRVA(dwMinRVA);
	pDest = (BYTE*)peDest.PtrFromRVA(dwDestRVA);
	memcpy(pDest, pSrc, dwSize);

	// Figure out by how much we need to change the RVA's to compensate for the relocation
	auto diffRVA = dwDestRVA - dwMinRVA;
	pCor = (IMAGE_COR20_HEADER*)peDest;

	// Fixup the DLL entrypoints
	if (IMAGE_NT_HEADERS32* hdr32 = peDest)
	{
		hdr32->OptionalHeader.MajorOperatingSystemVersion = 4;
		hdr32->OptionalHeader.MajorSubsystemVersion       = 4;
		if (hdr32->OptionalHeader.AddressOfEntryPoint != dwNewEntrypoint)
		{
			pCor->EntryPointToken = hdr32->OptionalHeader.AddressOfEntryPoint;
			hdr32->OptionalHeader.AddressOfEntryPoint = dwNewEntrypoint;
		}
	}
	else if (IMAGE_NT_HEADERS64* hdr64 = peDest)
	{
		if (hdr64->OptionalHeader.AddressOfEntryPoint != dwNewEntrypoint)
		{
			pCor->EntryPointToken = hdr64->OptionalHeader.AddressOfEntryPoint;
			hdr64->OptionalHeader.AddressOfEntryPoint = dwNewEntrypoint;
		}
	}

	// Adjust the .NET headers to indicate we're a mixed DLL
	pCor->Flags = (pCor->Flags & 0xFFFE) | 0x10;

	// Fixup the metadata header RVA's
	if (pCor->MetaData               .VirtualAddress) pCor->MetaData               .VirtualAddress += diffRVA;
	if (pCor->Resources              .VirtualAddress) pCor->Resources              .VirtualAddress += diffRVA;
	if (pCor->StrongNameSignature    .VirtualAddress) pCor->StrongNameSignature    .VirtualAddress += diffRVA;
	if (pCor->CodeManagerTable       .VirtualAddress) pCor->CodeManagerTable       .VirtualAddress += diffRVA;
	if (pCor->VTableFixups           .VirtualAddress) pCor->VTableFixups           .VirtualAddress += diffRVA;
	if (pCor->ExportAddressTableJumps.VirtualAddress) pCor->ExportAddressTableJumps.VirtualAddress += diffRVA;
	if (pCor->ManagedNativeHeader    .VirtualAddress) pCor->ManagedNativeHeader    .VirtualAddress += diffRVA;

	CMetadata meta(peDest);
	CMetadataTables tables(meta);

	// Fixup all the RVA's for methods and fields that have them in the .NET code
	for (auto n : {ETableTypes::MethodDef, ETableTypes::FieldRVA})
	{
		auto p = tables.GetTable(n);
		if (p)
		{
			for (UINT r = 0, rend = p->GetRowCount(); r != rend; ++r)
			{
				auto pdwRVA = (DWORD*)p->Column(r, 0U);
				if (*pdwRVA)
					*pdwRVA = *pdwRVA + diffRVA;
			}
		}
	}

	// If this is a CE file, then change the processor to x86
	if (IMAGE_NT_HEADERS32* hdr = peDest)
	{
		if (hdr->OptionalHeader.Subsystem == IMAGE_SUBSYSTEM_WINDOWS_CE_GUI || hdr->FileHeader.Machine == IMAGE_FILE_MACHINE_ARM)
		{
			hdr->FileHeader.Machine = IMAGE_FILE_MACHINE_I386;
			hdr->OptionalHeader.Subsystem = IMAGE_SUBSYSTEM_WINDOWS_CUI;
		}
		if (hdr->OptionalHeader.Subsystem == IMAGE_SUBSYSTEM_WINDOWS_CUI && (pCor->Flags & 0x08))
		{
			auto section = IMAGE_FIRST_SECTION(hdr);
			for (UINT i = 0; i < hdr->FileHeader.NumberOfSections; ++i, ++section)
			{
				if (strcmp((char const*)section->Name, ".bss") == 0)
				{
					strncpy((char*)section->Name, ".idata", _countof(section->Name));
					//section->Characteristics &= ~IMAGE_SCN_CNT_INITIALIZED_DATA;
					//section->Characteristics |= IMAGE_SCN_CNT_UNINITIALIZED_DATA;
					
					auto dwBSSRVA = section->VirtualAddress;
					auto pBSS = (BYTE*)peDest.PtrFromRVA(dwBSSRVA);
					for (DWORD u = 0; u != section->SizeOfRawData; ++u)
						pBSS[u] = 0;
				}
				if (section->SizeOfRawData < section->Misc.VirtualSize)
				{
					if (strcmp((char const*)section->Name, ".data") == 0 && dwAdjust > 0)
					{
						printf("\nWARNING: %s section has a RawData size of %d, less than its VirtualSize of %d, adjusting VirtualSize to %d\n", section->Name, section->SizeOfRawData, section->Misc.VirtualSize, dwAdjust);
						section->Misc.VirtualSize = dwAdjust;
					}
					else
					{
						printf("\nWARNING: %s section has a RawData size of %d and a VirtualSize of %d, strong named image may not run on Windows CE\n", section->Name, section->SizeOfRawData, section->Misc.VirtualSize);
					}
				}
			}
		}
	}

	if (pCor->Flags & 0x08)
		printf("\nWARNING: %s must be re-signed before it can be used!\n", native_filepath);

	printf("Success!\n");
}

// Show the command line help
void ShowHelp()
{
	printf(R"(
MERGEBIN - Merges a pure .NET assembly with a native DLL
Syntax: MERGEBIN [/I:assembly] [/S:sectionname assembly nativedll]
   /I:assembly    - Returns the number of bytes required to consume the assembly
   /S:sectionname - The name of the section in the nativedll to insert the CLR data
   /P:assembly    - Outputs the C++ pragma code that can be used as additional input
                    to a C++ app to reserve a section block large enough for the managed code.
   /B:objectfile  - Windows CE workaround, changes the attributes of the .BSS section
                    of an object file to generate a DLL that doesn't have a .bss section
                    whose virtual size is larger than the rawdata size.

The native DLL must have an unused section in it, into which the .NET assembly will be inserted. 
You can do this with the following code:
    #pragma data_seg(".clr")
    #pragma comment(linker, "/SECTION:.clr,ER")
    char __ph[92316] = {0}; // 92316 is the number of bytes to reserve
    #pragma data_seg()

You would then specify /SECTION:.CLR in the command-line for the location to
insert the .NET assembly.  The number of bytes reserved in the section needs
to be equal to or more than the number of bytes returned by the /I parameter.

The native DLL must also export a function that calls _CorDllMain in 
MSCOREE.DLL.  This function must have the same parameters and calling
conventions as DllMain, and its name must have the word "CORDLLMAIN"
in it.)");
}

int _tmain(int argc, _TCHAR* argv[])
{
	if (argc == 1)
	{
		ShowHelp();
		return 0;
	}

	LPTSTR assembly_filepath = nullptr;
	LPTSTR native_filepath = nullptr;
	LPTSTR section_name = nullptr;
	BOOL bDoPragma = FALSE;
	BOOL bDoObj = FALSE;
	DWORD dwAdjust = 0;

	for (int n = 1; n < argc; n++)
	{
		if (argv[n][0] != '-' && argv[n][0] != '/')
		{
			if (assembly_filepath == nullptr)
				assembly_filepath = argv[n];
			else if (native_filepath == nullptr)
				native_filepath = argv[n];
			else
			{
				_tprintf(_T("Too many files specified\n"));
				return 0;
			}
			continue;
		}

		switch (argv[n][1])
		{
		case 'I':
		case 'i':
			assembly_filepath = &argv[n][3];
			if (argv[n][2] != ':' || lstrlen(assembly_filepath) == 0)
			{
				_tprintf(_T("/I requires an assembly name\n"));
				return 0;
			}
			DumpCLRInfo(assembly_filepath);
			return 0;
			break;
		case 'P':
		case 'p':
			assembly_filepath = &argv[n][3];
			if (argv[n][2] != ':' || lstrlen(assembly_filepath) == 0)
			{
				_tprintf(_T("/P requires an assembly name\n"));
				return 0;
			}
			bDoPragma = TRUE;
			break;
		case 'S':
		case 's':
			section_name = &argv[n][3];
			if (argv[n][2] != ':' || lstrlen(section_name) == 0)
			{
				_tprintf(_T("/S requires a section name\n"));
				return 0;
			}
			break;
			//case 'A':
			//case 'a':
			//  if (argv[n][2] != ':')
			//  {
			//    _tprintf(_T("A parameter requires a numeric value\n"));
			//    return 0;
			//  }
			//  dwAdjust = _ttol(&argv[n][3]);
			//  break;
		case 'B':
		case 'b':
			assembly_filepath = &argv[n][3];
			if (argv[n][2] != ':' || lstrlen(assembly_filepath) == 0)
			{
				_tprintf(_T("/B requires an object file name\n"));
				return 0;
			}
			bDoObj = TRUE;
			break;
		}
	}

	if (assembly_filepath && bDoObj)
		FixObjFile(assembly_filepath);
	else if (assembly_filepath && native_filepath && section_name && !bDoPragma)
		MergeModules(assembly_filepath, native_filepath, section_name, dwAdjust);
	else if (assembly_filepath && bDoPragma)
		DumpCLRPragma(assembly_filepath, section_name);

	return 0;
}

