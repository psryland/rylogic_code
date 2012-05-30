//***********************************************************************
//
//  Stack Dump
//
//***********************************************************************
// Note: This implementation is based on an unreleased Boost library,
// which can be found here: http://groups.yahoo.com/group/boost/files/StackTraces/
// This implementation requires the dbghelp.dll.
// It should be placed somewhere where it can be found by the executable.
//
// Usage:
//  Use StackDump() to produce the addresses at the time StackDump() is called
//  Use GetCallSource() to convert addresses into file/line numbers
//  If you only get addresses printed out you need to make "DbgHelp.Dll" available for the exe
//   get the debugging tools from: http://www.microsoft.com/whdc/devtools/debugging/installx86.mspx
// e.g.
//   void SomeHelperTraceFunc()
//   {
//       // 1 = skip this function, and output the next 5 on the stack
//       pr::StackDump(1, 5, [](std::string const& filepath, int line)
//       {
//          printf(..);
//       }
//   }

#pragma once
#ifndef PR_STACK_DUMP_H
#define PR_STACK_DUMP_H

#if _WIN32_WINNT <= 0x0500
#   error "_WIN32_WINNT version 0x0600 or greater required"
#endif

#pragma warning(push, 1)
//#include "pr/common/min_max_fix.h"
#include <windows.h>
// <dbghelp.h> has conflicts with <imagehlp.h>, so it cannot be #include'd
// <imagehlp.h> contains everything we would need from <dbghelp.h> anyway
#include <imagehlp.h>
#include <tlhelp32.h>
#include <psapi.h>
#include <malloc.h>
#include <cstdlib>
#include <cstring>
#include <string>
#include <functional>
//#include "pr/common/assert.h"
//#include "pr/common/fmt.h"
//#include "pr/str/wstring.h"
//#include "pr/macros/link.h"
#pragma warning(pop)

#pragma comment(lib, "dbghelp.lib")
// Required lib: Dbghelp.lib

namespace pr
{
	struct CallAddress
	{
		void* m_address;
	};
	struct CallSource
	{
		std::string  m_filepath;
		unsigned int m_line;
	};
	
	namespace impl
	{
		// Win32/64 helper macros (defined by system header files or the compiler):
		//  _WIN64 64-bit
		//  _WIN32 64-bit or 32-bit
		//
		//  _M_IX86 Intel 32-bit
		//  _M_IA64 Intel 64-bit
		//  _M_AMD64 AMD 64-bit
		//
		// Occasionally, a reinterpret_cast is used in these functions.  These casts
		//  are safe; just keep in mind the following:
		//   void * - 32 bits on Win32, 64 bits on Win64
		//   DWORD64 - 64 bits on Win32, 64 bits on Win64
		//   DWORD - 32 bits on Win32, unspecified on Win64 (probably 32 bits)
		//   SIZE_T - 32 bits on Win32, and 64 bits on Win64 (unsigned)
		
		// Simple class that closes a handle on scope exit
		struct ScopedFile
		{
			HANDLE m_handle;
			ScopedFile(HANDLE handle) : m_handle(handle) {}
			~ScopedFile() { CloseHandle(m_handle); }
		};
		
		// Scoped critical section class
		class ScopedLock
		{
			struct CritSect
			{
				CRITICAL_SECTION m_cs;
				CritSect()    { InitializeCriticalSection(&m_cs); }
				~CritSect()   { DeleteCriticalSection(&m_cs); }
				void lock()   { EnterCriticalSection(&m_cs); }
				void unlock() { LeaveCriticalSection(&m_cs); }
			};
			static CritSect& cs() { static CritSect s_cs; return s_cs; }

		public:
			ScopedLock()  { cs().lock(); }
			~ScopedLock() { cs().unlock(); }
		};
		
		// DLL functions and classes **********************************************************************
		
		// This is a class that represents a DLL that has been Loaded into memory
		// Loads and unloads the named DLL into memory
		// If the DLL is not successfully Loaded, module() will return 0.
		class DllBase
		{
			HMODULE m_module;
			
		protected:
			// Returns the address of the specified procedure, or 0 if not found.
			void* ProcAddress(const char* proc_name) { return ::GetProcAddress(m_module, proc_name); }
		
		public:
			explicit DllBase(const char* name) :m_module(LoadLibraryA(name)) {}
			~DllBase() { if (m_module != 0) {FreeLibrary(m_module);} }
			HMODULE module() const { return m_module; }			
		};
		
		// This class represents kernel32.dll and the toolhelp functions from it
		struct ToolHelp_Dll: public DllBase
		{
			typedef HANDLE(__stdcall* CreateToolhelp32Snapshot_type)(DWORD , DWORD);
			typedef BOOL  (__stdcall* Module32First_type           )(HANDLE, LPMODULEENTRY32);
			typedef BOOL  (__stdcall* Module32Next_type            )(HANDLE, LPMODULEENTRY32);
			CreateToolhelp32Snapshot_type CreateToolhelp32Snapshot;
			Module32First_type            Module32First;
			Module32Next_type             Module32Next;
			
			ToolHelp_Dll()
			:DllBase("kernel32.dll")
			,CreateToolhelp32Snapshot((CreateToolhelp32Snapshot_type) ProcAddress("CreateToolhelp32Snapshot"))
			,Module32First           ((Module32First_type           ) ProcAddress("Module32First"))
			,Module32Next            ((Module32Next_type            ) ProcAddress("Module32Next"))
			{}
			
			// Returns true if all required functions were found
			bool Loaded() const { return CreateToolhelp32Snapshot && Module32First && Module32Next; }
			
		private:
			ToolHelp_Dll(const ToolHelp_Dll&);
			ToolHelp_Dll& operator=(const ToolHelp_Dll&);
		};
		
		// This class represents psapi.dll and the functions that we use from it
		struct PSAPI_Dll: public DllBase
		{
			typedef BOOL (__stdcall* EnumProcessModules_type  )(HANDLE, HMODULE*, DWORD, LPDWORD);
			typedef BOOL (__stdcall* GetModuleInformation_type)(HANDLE, HMODULE, LPMODULEINFO, DWORD);
			EnumProcessModules_type   EnumProcessModules;
			GetModuleInformation_type GetModuleInformation;
			
			PSAPI_Dll()
			:DllBase("psapi.dll")
			,EnumProcessModules  ((EnumProcessModules_type  )   ProcAddress("EnumProcessModules"))
			,GetModuleInformation((GetModuleInformation_type) ProcAddress("GetModuleInformation"))
			{}
			
			// Returns true if all required functions were found
			bool Loaded() const { return (EnumProcessModules && GetModuleInformation); }
			
		private:
			PSAPI_Dll(const PSAPI_Dll&);
			PSAPI_Dll& operator=(const PSAPI_Dll&);
		};
		
		// This class represents dbghelp.dll and the functions that we use from it
		// It also keeps track of the dll's initialized state and will call Cleanup
		//  if necessary before it unloads
		class DbgHelp_Dll: public DllBase
		{
			bool m_loaded; // This is true if the DLL was found, loaded, all required functions found, and initalized without error
			
			DbgHelp_Dll(DbgHelp_Dll const&); // no copying
			DbgHelp_Dll& operator=(DbgHelp_Dll const&);
			
			// Helper function for getting the full path and file name for a module. Returns std::string() on error
			static std::string GetModuleFilename(const HMODULE module)
			{
				char result[_MAX_PATH+1];
				if (!GetModuleFileNameA(module, result, _MAX_PATH+1)) result[0] = 0;
				return result;
			}
			
			// Load all the modules in this Process, using the PSAPI
			void LoadModules(PSAPI_Dll const& psapi)
			{
				DWORD bytes_needed;
				psapi.EnumProcessModules(GetCurrentProcess(), (HMODULE*) &bytes_needed, 0, &bytes_needed);
				SetLastError(0);
				
				HMODULE* modules = reinterpret_cast<HMODULE*>(_alloca(bytes_needed));
				if (!psapi.EnumProcessModules(GetCurrentProcess(), modules, bytes_needed, &bytes_needed))
					return;
					
				typedef HMODULE* HMODULEptr;
				for (HMODULEptr i = modules, end = modules + bytes_needed / sizeof(HMODULE); i != end;)
				{
					HMODULE module = *i++;
					MODULEINFO info;
					if (psapi.GetModuleInformation(GetCurrentProcess(), module, &info, sizeof(info)))
						SymLoadModule(Process(), 0, const_cast<char*>(GetModuleFilename(module).c_str()), 0, reinterpret_cast<DWORD64>(info.lpBaseOfDll), info.SizeOfImage);
				}
			}
			
			// Load all the modules in this Process, using the ToolHelp API
			void LoadModules(ToolHelp_Dll const& toolhelp)
			{
				const HANDLE snapshot = toolhelp.CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, GetCurrentProcessId());
				if (snapshot == INVALID_HANDLE_VALUE) return;
				
				ScopedFile snapshot_guard(snapshot);
				MODULEENTRY32 module = {};
				module.dwSize = sizeof(module);
				for (BOOL ok = toolhelp.Module32First(snapshot, &module); ok; ok = toolhelp.Module32Next(snapshot, &module))
				{
					std::string exe_path = pr::str::ToAString<std::string>(module.szExePath);
					std::string mod_name = pr::str::ToAString<std::string>(module.szModule);
					SymLoadModule64(Process(), 0, (PSTR)exe_path.c_str(), (PSTR)mod_name.c_str(), reinterpret_cast<DWORD64>(module.modBaseAddr), module.modBaseSize);
				}
			}
			
			// This function will try anything to enumerate and load all of this Process' modules
			void LoadModules()
			{
				// Try the ToolHelp API first
				const ToolHelp_Dll toolhelp;
				if (toolhelp.Loaded()) { LoadModules(toolhelp); return; }
				
				// Try PSAPI if ToolHelp isn't present
				const PSAPI_Dll psapi;
				if (psapi.Loaded()) { LoadModules(psapi); return; }
				
				// One of the two above should work.  The only possible case where they wouldn't is
				//  if an NT user deleted psapi.dll...
			}
			
			// Retrieves the current search path, or std::string() on error
			std::string GetSearchPath()
			{
				char result[_MAX_PATH+1];
				if (!SymGetSearchPath(Process(), result, _MAX_PATH+1)) result[0] = 0;
				return result;
			}
			
		public:
			typedef BOOL   (__stdcall* SymInitialize_type         )(HANDLE, PSTR, BOOL);
			typedef BOOL   (__stdcall* SymCleanup_type            )(HANDLE);
			typedef DWORD  (__stdcall* SymSetOptions_type         )(DWORD);
			typedef PVOID  (__stdcall* SymFunctionTableAccess_type)(HANDLE, DWORD64);
			typedef DWORD64(__stdcall* SymGetModuleBase_type      )(HANDLE, DWORD64);
			typedef BOOL   (__stdcall* StackWalk_type             )(DWORD, HANDLE, HANDLE, LPSTACKFRAME64, PVOID, PREAD_PROCESS_MEMORY_ROUTINE64, PFUNCTION_TABLE_ACCESS_ROUTINE64, PGET_MODULE_BASE_ROUTINE64, PTRANSLATE_ADDRESS_ROUTINE64);
			typedef DWORD64(__stdcall* SymLoadModule_type         )(HANDLE, HANDLE, PSTR, PSTR, DWORD64, DWORD);
			typedef BOOL   (__stdcall* SymGetLineFromAddr_type    )(HANDLE, DWORD64, PDWORD, PIMAGEHLP_LINE64);
			typedef BOOL   (__stdcall* SymGetSearchPath_type      )(HANDLE, PSTR, DWORD);
			
			// The following are functions that we absolutely require in order to do stack traces
			SymInitialize_type          SymInitialize;
			SymCleanup_type             SymCleanup;
			SymSetOptions_type          SymSetOptions;
			SymFunctionTableAccess_type SymFunctionTableAccess;
			SymGetModuleBase_type       SymGetModuleBase;
			StackWalk_type              StackWalk;
			
			// The following are functions that we may be able to do without, but they are always
			//  provided if the functions above are provided (so we assume they will be present)
			SymLoadModule_type      SymLoadModule;
			SymGetLineFromAddr_type SymGetLineFromAddr;
			SymGetSearchPath_type   SymGetSearchPath;
			
			// Singleton accessor
			static DbgHelp_Dll& get() { static DbgHelp_Dll me; return me; }
			
			// The value used to initialize dbghelp.dll. We always use the Process id here, so it will work on 95/98/Me
			static HANDLE Process()
			{
				DWORD id = GetCurrentProcessId();
				return reinterpret_cast<const HANDLE&>(id);
			}
			
			DbgHelp_Dll()
			:DllBase("dbghelp.dll")
			,m_loaded(false)
			,SymInitialize         ((SymInitialize_type         ) ProcAddress("SymInitialize"))
			,SymCleanup            ((SymCleanup_type            ) ProcAddress("SymCleanup"))
			,SymSetOptions         ((SymSetOptions_type         ) ProcAddress("SymSetOptions"))
			,SymFunctionTableAccess((SymFunctionTableAccess_type) ProcAddress("SymFunctionTableAccess64"))
			,SymGetModuleBase      ((SymGetModuleBase_type      ) ProcAddress("SymGetModuleBase64"))
			,StackWalk             ((StackWalk_type             ) ProcAddress("StackWalk64"))
			,SymLoadModule         ((SymLoadModule_type         ) ProcAddress("SymLoadModule64"))
			,SymGetLineFromAddr    ((SymGetLineFromAddr_type    ) ProcAddress("SymGetLineFromAddr64"))
			,SymGetSearchPath      ((SymGetSearchPath_type      ) ProcAddress("SymGetSearchPath"))
			{
				// If any critical functions (or the DLL itself) are missing, give up
				if (SymInitialize && SymCleanup && SymSetOptions && SymFunctionTableAccess && SymGetModuleBase && StackWalk && SymLoadModule && SymGetLineFromAddr && SymGetSearchPath)
				{
					SymSetOptions(SYMOPT_DEFERRED_LOADS | SYMOPT_LOAD_LINES);
					if (SymInitialize(Process(), 0, FALSE)) // Initialize dbghelp
					{
						m_loaded = true; // dbghelp.dll has been Initialized
						LoadModules();   // Enumerate and load all Process modules
					}
				}
			}
			~DbgHelp_Dll()
			{
				if (m_loaded)
					SymCleanup(Process());
			}
			
			// Returns true if all required functions were found and the init was OK
			bool Loaded() const { return m_loaded; }
		};
		
		// This function is placed inside a template, so that it can safely be put
		// inside a header without requiring an extra .cpp file, lib or inlining
		template <typename T = int> struct StackDumpImpl
		{
			static CallSource GetCallSource(CallAddress p)
			{
				ScopedLock lock;                           // Acquire the thread lock
				DbgHelp_Dll& dbghelp = DbgHelp_Dll::get(); // Ensure dbghelp.dll is Loaded
				
				CallSource result;
				DWORD displacement;
				IMAGEHLP_LINE64 line = {}; line.SizeOfStruct = sizeof(line);
				if (dbghelp.SymGetLineFromAddr(dbghelp.Process(), reinterpret_cast<DWORD64>(p.m_address), &displacement, &line))
				{
					result.m_filepath = line.FileName;
					result.m_line     = line.LineNumber;
				}
				else
				{
					DWORD e = GetLastError(); e;
					static char s_filename[MAX_PATH];
					_snprintf(s_filename, MAX_PATH, "0x%p", p.m_address);
					result.m_filepath = s_filename;
					result.m_line     = 0;
				}
				return result;
			}
			
			// This is used as the callback that StackWalk uses to read Process memory
			// We pass it through to ReadProcessMemory, after making the call 64-bit safe, and make a note of any errors returned.
			static BOOL CALLBACK DoReadProcessMemory(HANDLE, DWORD64 address, void* buffer, DWORD size, DWORD* bytes_read)
			{
				// On Win32, only 32 bits of 'address' is used
				// We use a separate SIZE_T variable for the bytes_read result value, because it is 64 bits on Win64
				SIZE_T bytes_read_;
				const BOOL ret = ReadProcessMemory(GetCurrentProcess(), reinterpret_cast<const void*>(address), buffer, size, &bytes_read_);
				*bytes_read = static_cast<DWORD>(bytes_read_);
				return ret;
			}
		};
		
		// templated to prevent inlining
		template <typename T> void StackDump(int skip, int count, std::function<void(std::string const& file, int line)> out)
		{
			impl::ScopedLock lock; // Acquire the thread lock
			impl::DbgHelp_Dll& dbghelp = impl::DbgHelp_Dll::get(); // Ensure dbghelp.dll is Loaded
			if (!dbghelp.Loaded()) return;
		
			// Get the context record directly
			// You're really not supposed to do this...
			CONTEXT ctx;
			RtlCaptureContext(&ctx);
		
			STACKFRAME64 frame = {};
			#if defined(_M_IX86)
			const DWORD image_file_type = IMAGE_FILE_MACHINE_I386;
			frame.AddrPC.Mode           = AddrModeFlat;
			frame.AddrPC.Offset         = ctx.Eip;
			frame.AddrFrame.Mode        = AddrModeFlat;
			frame.AddrFrame.Offset      = ctx.Ebp;
			frame.AddrStack.Mode        = AddrModeFlat;
			frame.AddrStack.Offset      = ctx.Esp;
			//frame.AddrPC.Mode         = AddrModeFlat;
			//frame.AddrPC.Offset       = ctx.Eip;
			//frame.AddrFrame.Mode      = AddrModeFlat;
			//frame.AddrFrame.Offset    = ctx.Ebp;
			#elif defined(_M_IA64)
			const DWORD image_file_type = IMAGE_FILE_MACHINE_IA64;
			frame.AddrPC.Mode           = AddrModeFlat;
			frame.AddrPC                = ctx.StIIP;
			frame.AddrBStore.Mode       = AddrModeFlat;
			frame.AddrBStore            = ctx.RsBSP;
			#elif defined(_M_AMD64)
			const DWORD image_file_type = IMAGE_FILE_MACHINE_AMD64;
			frame.AddrPC.Mode           = AddrModeFlat;
			frame.AddrPC.Offset         = ctx.Rip;
			frame.AddrFrame.Mode        = AddrModeFlat;
			frame.AddrFrame.Offset      = ctx.Rbp;
			#else
			#error Unknown machine type!
			#endif
			while (dbghelp.StackWalk(
				image_file_type,
				dbghelp.Process(),
				GetCurrentThread(),
				&frame,
				&ctx,
				&impl::StackDumpImpl<>::DoReadProcessMemory,
				dbghelp.SymFunctionTableAccess,
				dbghelp.SymGetModuleBase,
				0))
			{
				// Sometimes StackWalk will leave the last error set, which causes us to report an (incorrect) error after this loop.
				SetLastError(0);
				if (skip != 0) --skip;
				else if (count-- == 0) break;
				else
				{
					CallAddress const& addr = reinterpret_cast<CallAddress const&>(frame.AddrPC.Offset);
					CallSource src = impl::StackDumpImpl<>::GetCallSource(addr);
					out(src.m_filepath, src.m_line);
				}
				if (frame.AddrReturn.Offset == 0) break;
			}
		}
	}
	
	// The stack dump function
	// func is a call back function called for each stack frame
	// 'skip' is the number of initial stack frames to not call 'func' for
	// 'count' is the number of stack frames to call 'func' for before stopping
	inline void StackDump(int skip, int count, std::function<void(std::string const& file, int line)> out)
	{
		impl::StackDump<void>(skip, count, out);
	}
	inline void StackDump(std::function<void(std::string const& file, int line)> out)
	{
		impl::StackDump<void>(0, 0x7FFFFFFF, out);
	}
	
	// Return the call source from an arbitrary memory address
	inline CallSource GetCallSource(void* address)
	{
		return impl::StackDumpImpl<>::GetCallSource(reinterpret_cast<CallAddress const&>(address));
	}
}

#endif
