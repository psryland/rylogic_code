//***********************************************************************
//
//	Stack Dump
//
//***********************************************************************
// Note: This implementation is based on an unreleased Boost library,
// which can be found here: http://groups.yahoo.com/group/boost/files/StackTraces/
// This implementation requires the dbghelp.dll.
// It should be placed somewhere where it can be found by the executable.
//
// Usage:
//	Use StackDump() to produce the addresses at the time StackDump() is called
//	Use GetCallSource() to convert addresses into file/line numbers
//	If you only get addresses printed out you need to make "DbgHelp.Dll" available for the exe
//	 get the debugging tools from: http://www.microsoft.com/whdc/devtools/debugging/installx86.mspx
#ifndef PR_STACK_DUMP_H
#define PR_STACK_DUMP_H

#if _WIN32_WINNT <= 0x0500
#	error "_WIN32_WINNT version 0x0600 or greater required"
#endif

#pragma warning(push, 1)
#include "pr/common/min_max_fix.h"
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
#include "pr/common/assert.h"
#include "pr/common/singleton.h"
#include "pr/common/fmt.h"
#include "pr/common/stdstring.h"
#include "pr/str/wstring.h"
#include "pr/macros/link.h"
#pragma warning(pop)

// Required lib: Dbghelp.lib

namespace pr
{
	// Interface ***************************
	struct CallAddress
	{
		void* m_address;
	};
	struct CallSource
	{
		std::string  m_filename;
		unsigned int m_line;
	};
	CallSource GetCallSource(CallAddress);
	template <typename Output> Output& StackDump(Output& out);
	// Interface ***************************

	// Helper Output Iterators ***************************
	namespace stack_dump
	{
		struct OutputDbgStr
		{
			void operator()(CallAddress const& address)
			{
				CallSource source = GetCallSource(address);
				PR_OUTPUT_MSG(Fmt("%s(%d)\n", source.m_filename.c_str(), source.m_line).c_str());
			}
		};
		struct Printf
		{
			void operator()(CallAddress const& address)
			{
				CallSource source = GetCallSource(address);
				printf("%s(%d)\n", source.m_filename.c_str(), source.m_line);
			}
		};
	}//namespace stack_dump
	// Helper Output Iterators ***************************

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
			ScopedFile(HANDLE handle) : m_handle(handle)	{}
			~ScopedFile()									{ CloseHandle(m_handle); }
			HANDLE m_handle;
		};

		// Scoped critical section class
		struct ScopedLock
		{
			struct CriticalSection : pr::Singleton<CriticalSection>
			{
				CriticalSection()	{ InitializeCriticalSection	(&m_critical_section); }
				~CriticalSection()	{ DeleteCriticalSection		(&m_critical_section); }
				void lock  ()		{ EnterCriticalSection		(&m_critical_section); }
				void unlock()		{ LeaveCriticalSection		(&m_critical_section); }
				CRITICAL_SECTION m_critical_section;
			};

			ScopedLock()	{ CriticalSection::Get().lock  (); }
			~ScopedLock()	{ CriticalSection::Get().unlock(); }
		};

		// DLL functions and classes **********************************************************************

		// This is a class that represents a DLL that has been Loaded into memory
		// Loads and unloads the named DLL into memory
		// If the DLL is not successfully Loaded, module() will return 0.
		class DllBase
		{
		public:
			explicit DllBase(const char* name) : m_module(LoadLibraryA(name))	{}
			~DllBase()															{ if( m_module != 0 ) {FreeLibrary(m_module);} }
			HMODULE module() const												{ return m_module; }

		protected:
			// Returns the address of the specified procedure, or 0 if not found.
			void* ProcAddress(const char* proc_name)							{ return ::GetProcAddress(m_module, proc_name); }

		private:
			HMODULE m_module;
		};

		// This class represents kernel32.dll and the toolhelp functions from it
		struct ToolHelp_Dll: public DllBase
		{
			typedef HANDLE (__stdcall * CreateToolhelp32Snapshot_type)(DWORD , DWORD          );
			typedef BOOL   (__stdcall * Module32First_type           )(HANDLE, LPMODULEENTRY32);
			typedef BOOL   (__stdcall * Module32Next_type            )(HANDLE, LPMODULEENTRY32);

			ToolHelp_Dll()
			:DllBase("kernel32.dll")
			,CreateToolhelp32Snapshot((CreateToolhelp32Snapshot_type) ProcAddress("CreateToolhelp32Snapshot"))
			,Module32First           ((Module32First_type           ) ProcAddress("Module32First"           ))
			,Module32Next            ((Module32Next_type            ) ProcAddress("Module32Next"            ))
			{}

			CreateToolhelp32Snapshot_type CreateToolhelp32Snapshot;
			Module32First_type            Module32First;
			Module32Next_type             Module32Next;

			// Returns true if all required functions were found
			bool Loaded() const										{ return CreateToolhelp32Snapshot && Module32First && Module32Next; }

		private:
			ToolHelp_Dll(const ToolHelp_Dll&);
			ToolHelp_Dll& operator=(const ToolHelp_Dll&);
		};

		// This class represents psapi.dll and the functions that we use from it
		struct PSAPI_Dll: public DllBase
		{
			typedef BOOL (__stdcall * EnumProcessModules_type  )(HANDLE, HMODULE*, DWORD, LPDWORD);
			typedef BOOL (__stdcall * GetModuleInformation_type)(HANDLE, HMODULE, LPMODULEINFO, DWORD);

			PSAPI_Dll()
			:DllBase("psapi.dll")
			,EnumProcessModules  ((EnumProcessModules_type)   ProcAddress("EnumProcessModules"))
			,GetModuleInformation((GetModuleInformation_type) ProcAddress("GetModuleInformation"))
			{}

			EnumProcessModules_type EnumProcessModules;
			GetModuleInformation_type GetModuleInformation;

			// Returns true if all required functions were found
			bool Loaded() const										{ return (EnumProcessModules && GetModuleInformation); }

		private:
			PSAPI_Dll(const PSAPI_Dll&);
			PSAPI_Dll& operator=(const PSAPI_Dll&);
		};

		// This class represents dbghelp.dll and the functions that we use from it
		// It also keeps track of the dll's initialized state and will call Cleanup
		//  if necessary before it unloads
		class DbgHelp_Dll: public DllBase, public pr::Singleton<DbgHelp_Dll>
		{
		public:
			typedef BOOL     (__stdcall * SymInitialize_type         )(HANDLE, PSTR, BOOL);
			typedef BOOL     (__stdcall * SymCleanup_type            )(HANDLE);
			typedef DWORD    (__stdcall * SymSetOptions_type         )(DWORD);
			typedef PVOID    (__stdcall * SymFunctionTableAccess_type)(HANDLE, DWORD64);
			typedef DWORD64  (__stdcall * SymGetModuleBase_type      )(HANDLE, DWORD64);
			typedef BOOL     (__stdcall * StackWalk_type             )(DWORD, HANDLE, HANDLE, LPSTACKFRAME64, PVOID, PREAD_PROCESS_MEMORY_ROUTINE64, PFUNCTION_TABLE_ACCESS_ROUTINE64, PGET_MODULE_BASE_ROUTINE64, PTRANSLATE_ADDRESS_ROUTINE64);
			typedef DWORD64  (__stdcall * SymLoadModule_type         )(HANDLE, HANDLE, PSTR, PSTR, DWORD64, DWORD);
			typedef BOOL     (__stdcall * SymGetLineFromAddr_type    )(HANDLE, DWORD64, PDWORD, PIMAGEHLP_LINE64);
			typedef BOOL     (__stdcall * SymGetSearchPath_type      )(HANDLE, PSTR, DWORD);

		public:
			DbgHelp_Dll()
			:DllBase               ("dbghelp.dll")
			,m_loaded              (false)
			,SymInitialize         ((SymInitialize_type         ) ProcAddress("SymInitialize"           ))
			,SymCleanup            ((SymCleanup_type            ) ProcAddress("SymCleanup"              ))
			,SymSetOptions         ((SymSetOptions_type         ) ProcAddress("SymSetOptions"           ))
			,SymFunctionTableAccess((SymFunctionTableAccess_type) ProcAddress("SymFunctionTableAccess64"))
			,SymGetModuleBase      ((SymGetModuleBase_type      ) ProcAddress("SymGetModuleBase64"      ))
			,StackWalk             ((StackWalk_type             ) ProcAddress("StackWalk64"             ))
			,SymLoadModule         ((SymLoadModule_type         ) ProcAddress("SymLoadModule64"         ))
			,SymGetLineFromAddr    ((SymGetLineFromAddr_type    ) ProcAddress("SymGetLineFromAddr64"    ))
			,SymGetSearchPath      ((SymGetSearchPath_type      ) ProcAddress("SymGetSearchPath"        ))
			{
				// If any critical functions (or the DLL itself) are missing, give up
				if( SymInitialize && SymCleanup && SymSetOptions && SymFunctionTableAccess && SymGetModuleBase && StackWalk && SymLoadModule && SymGetLineFromAddr && SymGetSearchPath )
				{
					// Set the options
					SymSetOptions(SYMOPT_DEFERRED_LOADS | SYMOPT_LOAD_LINES);

					// Initialize dbghelp
					if( SymInitialize(Process(), 0, FALSE) )
					{
						// dbghelp.dll has been Initialized
						m_loaded = true;

						// Enumerate and load all Process modules
						LoadModules();
					}
				}
			}
			~DbgHelp_Dll()
			{
				if( m_loaded )
					SymCleanup(Process());
			}

			// Returns true if all required functions were found and the init was OK
			bool Loaded() const { return m_loaded; }

			// The value used to initialize dbghelp.dll
			// We always use the Process id here, so it will work on 95/98/Me
			static HANDLE Process()
			{
				DWORD id = GetCurrentProcessId();
				return reinterpret_cast<const HANDLE&>(id);
				//return GetCurrentProcess();
			}

		private:
			DbgHelp_Dll(const DbgHelp_Dll&);
			DbgHelp_Dll& operator=(const DbgHelp_Dll&);

			// Helper function for getting the full path and file name for a module
			// Returns std::string() on error
			static std::string GetModuleFilename(const HMODULE module)
			{
				char result[_MAX_PATH+1];
				if( !GetModuleFileNameA(module, result, _MAX_PATH+1) ) result[0] = 0;
				return result;
			}

			// Load all the modules in this Process, using the PSAPI
			void LoadModules(const PSAPI_Dll& psapi)
			{
				DWORD bytes_needed;
				psapi.EnumProcessModules(GetCurrentProcess(), (HMODULE *) &bytes_needed, 0, &bytes_needed);
				SetLastError(0);

				HMODULE* modules = reinterpret_cast<HMODULE*>(_alloca(bytes_needed));
				if( !psapi.EnumProcessModules(GetCurrentProcess(), modules, bytes_needed, &bytes_needed) )
					return;

				typedef HMODULE* HMODULEptr;
				for (HMODULEptr i = modules, end = modules + bytes_needed / sizeof(HMODULE); i != end; )
				{
					HMODULE module = *i++;
					MODULEINFO info;
					if( psapi.GetModuleInformation(GetCurrentProcess(), module, &info, sizeof(info)) )
						SymLoadModule(Process(), 0, const_cast<char *>(GetModuleFilename(module).c_str()), 0, reinterpret_cast<DWORD64>(info.lpBaseOfDll), info.SizeOfImage);
				}
			}

			// Load all the modules in this Process, using the ToolHelp API
			void LoadModules(const ToolHelp_Dll& toolhelp)
			{
				const HANDLE snapshot = toolhelp.CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, GetCurrentProcessId());
				if( snapshot == INVALID_HANDLE_VALUE )
					return;
				ScopedFile snapshot_guard(snapshot);

				MODULEENTRY32 module;
				std::memset(&module, 0, sizeof(module));
				module.dwSize = sizeof(module);
				BOOL ok = toolhelp.Module32First(snapshot, &module);
				while (ok)
				{
					SymLoadModule64(Process(), 0, (PSTR)pr::str::ConvertToAString(module.szExePath).c_str(), (PSTR)pr::str::ConvertToAString(module.szModule).c_str(), reinterpret_cast<DWORD64>(module.modBaseAddr), module.modBaseSize);
					ok = toolhelp.Module32Next(snapshot, &module);
				}
			}

			// This function will try anything to enumerate and load all of this Process' modules
			void LoadModules()
			{
				// Try the ToolHelp API first
				const ToolHelp_Dll toolhelp;
				if( toolhelp.Loaded() )
				{
					LoadModules(toolhelp);
					return;
				}

				// Try PSAPI if ToolHelp isn't present
				const PSAPI_Dll psapi;
				if( psapi.Loaded() )
				{
					LoadModules(psapi);
					return;
				}

				// One of the two above should work.  The only possible case where they wouldn't is
				//  if an NT user deleted psapi.dll...
			}

			// Retrieves the current search path, or std::string() on error
			std::string GetSearchPath()
			{
				char result[_MAX_PATH+1];
				if( !SymGetSearchPath(Process(), result, _MAX_PATH+1) ) result[0] = 0;
				return result;
			}

		public:
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

		private:
			// This is true if the DLL was found, Loaded, all required functions found, and initalized without error
			bool m_loaded;
		};

		// This function is placed inside a template, so that it can safely be put
		// inside a header without requiring an extra .cpp file, lib or inlining
		template <typename T = int>
		struct dummy
		{
			static CallSource GetCallSource(CallAddress);
			static BOOL CALLBACK DoReadProcessMemory(HANDLE, DWORD64, void*, DWORD, DWORD*);
		};

		template <typename T>
		CallSource dummy<T>::GetCallSource(CallAddress p)
		{
			// Acquire the thread lock
			ScopedLock lock;

			// Ensure dbghelp.dll is Loaded
			DbgHelp_Dll& dbghelp = DbgHelp_Dll::Get();

			CallSource result;

			DWORD displacement;
			IMAGEHLP_LINE64 line;
			std::memset(&line, 0, sizeof(line));
			line.SizeOfStruct = sizeof(line);
			if( dbghelp.SymGetLineFromAddr(dbghelp.Process(), reinterpret_cast<DWORD64>(p.m_address), &displacement, &line) )
			{
				result.m_filename = line.FileName;
				result.m_line     = line.LineNumber;
			}
			else
			{
				DWORD e = GetLastError(); e;
				static char s_filename[MAX_PATH];
				_snprintf(s_filename, MAX_PATH, "0x%p", p.m_address);
				result.m_filename = s_filename;
				result.m_line     = 0;
			}

			return result;
		}

		// This is used as the callback that StackWalk uses to read Process memory
		// We pass it through to ReadProcessMemory, after making the call 64-bit safe,
		//  and make a note of any errors returned.
		template <typename T>
		BOOL CALLBACK dummy<T>::DoReadProcessMemory(HANDLE, DWORD64 address, void* buffer, DWORD size, DWORD* bytes_read)
		{
			// On Win32, only 32 bits of 'address' is used
			// We use a separate SIZE_T variable for the bytes_read result value,
			//  because it is 64 bits on Win64
			SIZE_T bytes_read_;
			const BOOL ret = ReadProcessMemory(GetCurrentProcess(), reinterpret_cast<const void *>(address), buffer, size, &bytes_read_);
			*bytes_read = static_cast<DWORD>(bytes_read_);
			return ret;
		}
	}//namespace impl

	// The stack dump function
	template <typename Output>
	Output& StackDump(Output& out)
	{
		// Acquire the thread lock
		impl::ScopedLock lock;

		// Ensure dbghelp.dll is Loaded
		impl::DbgHelp_Dll& dbghelp = impl::DbgHelp_Dll::Get();
		if( !dbghelp.Loaded() ) return out;

		// Get the context record directly

		// You're really not supposed to do this...
		CONTEXT ctx;
		RtlCaptureContext(&ctx);

		STACKFRAME64 frame;
		std::memset(&frame, 0, sizeof(frame));

		#if defined(_M_IX86)
			const DWORD image_file_type = IMAGE_FILE_MACHINE_I386;
			frame.AddrPC.Mode           = AddrModeFlat;
			frame.AddrPC.Offset         = ctx.Eip;
			frame.AddrFrame.Mode        = AddrModeFlat;
			frame.AddrFrame.Offset      = ctx.Ebp;
			frame.AddrStack.Mode		= AddrModeFlat;
			frame.AddrStack.Offset		= ctx.Esp;
			//frame.AddrPC.Mode			= AddrModeFlat;
			//frame.AddrPC.Offset		= ctx.Eip;
			//frame.AddrFrame.Mode		= AddrModeFlat;
			//frame.AddrFrame.Offset	= ctx.Ebp;
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

		while( dbghelp.StackWalk(image_file_type, dbghelp.Process(), GetCurrentThread(),
				&frame, &ctx, &impl::dummy<>::DoReadProcessMemory, dbghelp.SymFunctionTableAccess,
				dbghelp.SymGetModuleBase, 0) )
		{
			// Sometimes StackWalk will leave the last error set, which causes us to report an
			//  (incorrect) error after this loop.
			SetLastError(0);
			out(reinterpret_cast<const CallAddress&>(frame.AddrPC.Offset));
			if( frame.AddrReturn.Offset == 0 )
			break;
		}
		return out;
	}

	inline CallSource GetCallSource(CallAddress p)		{ return impl::dummy<>::GetCallSource(p); }
	inline CallSource GetCallSource(void* address)		{ return impl::dummy<>::GetCallSource(reinterpret_cast<const CallAddress&>(address)); }
}//namespace pr

#endif//PR_STACK_DUMP_H
