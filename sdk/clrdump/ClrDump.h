///////////////////////////////////////////////////////////////////////////////
//
// ClrDump.h 
//
// Author: Oleg Starodumov (www.debuginfo.com) 
//
//


#ifndef ClrDump_h
#define ClrDump_h

#define CLRDUMP_API __declspec(dllimport) __stdcall


///////////////////////////////////////////////////////////////////////////////
// Flags 
//

	// Filter options 
		// Call default exception handler after creating the minidump 
		// (that is, return EXCEPTION_CONTINUE_SEARCH from the custom filter) 
#define CLRDMP_OPT_CALLDEFAULTHANDLER   0x00000001


///////////////////////////////////////////////////////////////////////////////
// Exported functions 
//

	// CreateDump
	// 
	// This function creates a minidump of the target process 
	// 
	// Parameters: 
	// 
	//   ProcessId:    Process id of the target process 
	//   pFileName:    Name of the minidump file 
	//   DumpType:     Minidump type (MiniDumpNormal, or a combination of other 
	//                 MiniDump* constants) 
	//   ExcThreadId:  Thread id of the thread that generated the exception 
	//                 (optional, can be null if no exception information is specified)
	//   pExtPtrs:     Pointer to EXCEPTION_POINTERS structure that describes 
	//                 the current exception (optional, can be null) 
	// 
	// Return value: TRUE if succeeded, FALSE if failed. If the function fails, 
	//   GetLastError() can be used to obtain the error code. 
	//
BOOL CLRDUMP_API CreateDump( DWORD ProcessId, LPCWSTR pFileName, DWORD DumpType, 
                             DWORD ExcThreadId, EXCEPTION_POINTERS* pExcPtrs );

	// RegisterFilter 
	// 
	// Registers a custom filter for unhandled exceptions, which creates a minidump 
	// with the specified name and type if an unhandled exception occurs in the process
	// 
	// Parameters: 
	// 
	//   pDumpFileName:  Name of the minidump file (which will be created in case 
	//                   of unhandled exception)
	//   DumpType:       Minidump type (MiniDumpNormal, or a combination of other 
	//                   MiniDump* constants) 
	// 
	// Return value: TRUE if succeeded, FALSE if failed. If the function fails, 
	//   GetLastError() can be used to obtain the error code. 
	// 
BOOL CLRDUMP_API RegisterFilter( LPCWSTR pDumpFileName, DWORD DumpType );

	// UnregisterFilter 
	// 
	// Unregisters the previously registered custom filter for unhandled exceptions 
	// 
	// Return value: TRUE if succeeded, FALSE if failed. If the function fails, 
	//   GetLastError() can be used to obtain the error code. 
	// 
BOOL CLRDUMP_API UnregisterFilter();

	// SetFilterOptions 
	//
	// Sets configuration options that allow to customize the behavior 
	// of the custom filter for unhanled exceptions 
	//
	// Available options: 
	//   CLRDMP_OPT_CALLDEFAULTHANDLER: After creating the minidump, 
	//     pass control to the default (system-provided) exception handler. 
	//     Usually it will result in just-in-time debugger launched and 
	//     attached to the application, or Windows Error Reporting dialog 
	//     will be shown. By default, the application will be silently 
	//     terminated after the dump has been created.
	//
	// Return value: Previously active options 
	//
DWORD CLRDUMP_API SetFilterOptions( DWORD Options );


#endif // ClrDump_h

