//***********************************************************************
//  Stack Dump
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

#if _WIN32_WINNT <= 0x0500
#error "_WIN32_WINNT version 0x0600 or greater required"
#endif

#include <malloc.h>
#include <cstdlib>
#include <cstring>
#include <string>
#include <functional>
#include <mutex>

#include <windows.h>

// <dbghelp.h> has conflicts with <imagehlp.h>, so it cannot be #include'd
// <imagehlp.h> contains everything we would need from <dbghelp.h> anyway
#include <imagehlp.h>
#include <tlhelp32.h>
#include <psapi.h>

#pragma comment(lib, "dbghelp.lib")

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
		std::string  m_sym_name;
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

			ScopedFile(HANDLE handle) :m_handle(handle) {}
			~ScopedFile() { CloseHandle(m_handle); }

		private:
			ScopedFile(ScopedFile const&);
			ScopedFile& operator=(ScopedFile const&);
		};

		// A global mutex to sync access to 'GetCallSource()'
		class StackdumpLock
		{
			static std::recursive_mutex& cs() { static std::recursive_mutex s_cs; return s_cs; }
			std::lock_guard<std::recursive_mutex> m_lock;

			StackdumpLock(StackdumpLock const&);
			StackdumpLock& operator=(StackdumpLock const&);

		public:
			StackdumpLock() :m_lock(cs()) {}
		};

		// Wraps and initialises a SYMBOL_INFO object
		struct SymInfo :SYMBOL_INFO
		{
			char sym_name[1024];
			SymInfo() :SYMBOL_INFO() ,sym_name()
			{
				SizeOfStruct = sizeof(SYMBOL_INFO);
				MaxNameLen = sizeof(sym_name);
			}
		};

		// Wraps and initialises a IMAGEHLP_LINE64 object
		struct Line64 :IMAGEHLP_LINE64
		{
			Line64() :IMAGEHLP_LINE64()
			{
				SizeOfStruct = sizeof(IMAGEHLP_LINE64);
			}
		};

		// DLL functions and classes **********************************************************************

		// This is a class that represents a DLL that has been Loaded into memory
		// Loads and unloads the named DLL into memory
		// If the DLL is not successfully Loaded, module() will return 0.
		struct DllBase
		{
		protected:

			HMODULE m_module;

			// Returns the address of the specified procedure, or 0 if not found.
			void* ProcAddress(const char* proc_name) { return ::GetProcAddress(m_module, proc_name); }

			explicit DllBase(const char* name)
				:m_module(LoadLibraryA(name))
			{}
			~DllBase()
			{
				if (m_module == 0) return;
				FreeLibrary(m_module);
			}
			HMODULE module() const
			{
				return m_module;
			}
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
				,EnumProcessModules  ((EnumProcessModules_type  ) ProcAddress("EnumProcessModules"))
				,GetModuleInformation((GetModuleInformation_type) ProcAddress("GetModuleInformation"))
			{}

			// Returns true if all required functions were found
			bool Loaded() const { return EnumProcessModules && GetModuleInformation; }

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
						SymLoadModule64(Process(), 0, const_cast<char*>(GetModuleFilename(module).c_str()), 0, reinterpret_cast<DWORD64>(info.lpBaseOfDll), info.SizeOfImage);
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
					std::string exe_path = module.szExePath;
					std::string mod_name = module.szModule;
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

			DbgHelp_Dll(DbgHelp_Dll const&); // no copying
			DbgHelp_Dll& operator=(DbgHelp_Dll const&);

		public:
			typedef BOOL   (WINAPI* SymInitialize_type         )(HANDLE, PSTR, BOOL);
			typedef BOOL   (WINAPI* SymCleanup_type            )(HANDLE);
			typedef DWORD  (WINAPI* SymSetOptions_type         )(DWORD);
			typedef PVOID  (WINAPI* SymFunctionTableAccess_type)(HANDLE, DWORD64);
			typedef DWORD64(WINAPI* SymGetModuleBase_type      )(HANDLE, DWORD64);
			typedef BOOL   (WINAPI* StackWalk_type             )(DWORD, HANDLE, HANDLE, LPSTACKFRAME64, PVOID, PREAD_PROCESS_MEMORY_ROUTINE64, PFUNCTION_TABLE_ACCESS_ROUTINE64, PGET_MODULE_BASE_ROUTINE64, PTRANSLATE_ADDRESS_ROUTINE64);
			typedef DWORD64(WINAPI* SymLoadModule_type         )(HANDLE, HANDLE, PSTR, PSTR, DWORD64, DWORD);
			typedef BOOL   (WINAPI* SymGetLineFromAddr_type    )(HANDLE, DWORD64, PDWORD, PIMAGEHLP_LINE64);
			typedef BOOL   (WINAPI* SymGetSearchPath_type      )(HANDLE, PSTR, DWORD);
			typedef BOOL   (WINAPI* SymFromAddr_type           )(HANDLE, DWORD64, PDWORD64, PSYMBOL_INFO);

			// The following are functions that we absolutely required in order to do stack traces
			SymInitialize_type          SymInitialize;
			SymCleanup_type             SymCleanup;
			SymSetOptions_type          SymSetOptions;
			SymFunctionTableAccess_type SymFunctionTableAccess64;
			SymGetModuleBase_type       SymGetModuleBase64;
			StackWalk_type              StackWalk64;

			// The following are functions that we may be able to do without, but they are always
			// provided if the functions above are provided (so we assume they will be present)
			SymLoadModule_type      SymLoadModule64;
			SymGetLineFromAddr_type SymGetLineFromAddr64;
			SymGetSearchPath_type   SymGetSearchPath;
			SymFromAddr_type        SymFromAddr;

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
				,SymInitialize            ((SymInitialize_type         ) ProcAddress("SymInitialize"           ))
				,SymCleanup               ((SymCleanup_type            ) ProcAddress("SymCleanup"              ))
				,SymSetOptions            ((SymSetOptions_type         ) ProcAddress("SymSetOptions"           ))
				,SymFunctionTableAccess64 ((SymFunctionTableAccess_type) ProcAddress("SymFunctionTableAccess64"))
				,SymGetModuleBase64       ((SymGetModuleBase_type      ) ProcAddress("SymGetModuleBase64"      ))
				,StackWalk64              ((StackWalk_type             ) ProcAddress("StackWalk64"             ))
				,SymLoadModule64          ((SymLoadModule_type         ) ProcAddress("SymLoadModule64"         ))
				,SymGetLineFromAddr64     ((SymGetLineFromAddr_type    ) ProcAddress("SymGetLineFromAddr64"    ))
				,SymGetSearchPath         ((SymGetSearchPath_type      ) ProcAddress("SymGetSearchPath"        ))
				,SymFromAddr              ((SymFromAddr_type           ) ProcAddress("SymFromAddr"             ))
			{
				// If any critical functions (or the DLL itself) are missing, give up
				if (!SymInitialize || !SymCleanup || !SymSetOptions || !SymFunctionTableAccess64 ||
					!SymGetModuleBase64 || !StackWalk64 || !SymLoadModule64 ||
					!SymGetLineFromAddr64 || !SymGetSearchPath || !SymFromAddr)
					throw std::exception("failed to load dbghelp.dll for stack dump");

				SymSetOptions(SYMOPT_DEFERRED_LOADS | SYMOPT_LOAD_LINES | SYMOPT_UNDNAME);
				if (!SymInitialize(Process(), 0, FALSE)) // Initialize dbghelp
					throw std::exception("failed to initialise dbghelp.dll for stack dump");

				LoadModules();   // Enumerate and load all Process modules
			}
			~DbgHelp_Dll()
			{
				SymCleanup(Process());
			}
		};

		template <typename T = int> struct StackDumpImpl
		{
			static CallSource GetCallSource(CallAddress p)
			{
				StackdumpLock lock;                        // Acquire the thread lock
				DbgHelp_Dll& dbghelp = DbgHelp_Dll::get(); // Ensure dbghelp.dll is Loaded

				CallSource result;

				Line64 line;
				DWORD displacement;
				if (dbghelp.SymGetLineFromAddr64(dbghelp.Process(), reinterpret_cast<DWORD64>(p.m_address), &displacement, &line))
				{
					result.m_filepath = line.FileName;
					result.m_line     = line.LineNumber;
				}
				else
				{
					DWORD e = GetLastError(); (void)e;
					char s_filename[MAX_PATH];
					_snprintf(s_filename, MAX_PATH, "0x%p", p.m_address);
					result.m_filepath = s_filename;
					result.m_line     = 0;
				}

				SymInfo info;
				DWORD64 displacement64;
				if (dbghelp.SymFromAddr(dbghelp.Process(), reinterpret_cast<DWORD64>(p.m_address), &displacement64, &info))
				{
					result.m_sym_name = std::string(&info.Name[0], &info.Name[info.NameLen]);
				}
				else
				{
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
	}

	// The stack dump function
	// 'out' is a call back function called for each stack frame
	// 'skip' is the number of initial stack frames to not call 'func' for
	// 'count' is the number of stack frames to call 'func' for before stopping
	// 'TOut(std::string sym, std::string file, int line)
	template <typename TOut> void StackDump(TOut out, size_t count = 256, size_t skip = 0)
	{
		size_t const max_frames = 256;
		CallAddress frames[max_frames];
		if (count > max_frames) count = max_frames;
		count = CaptureStackBackTrace(DWORD(skip), DWORD(count), reinterpret_cast<void**>(&frames[0]), nullptr);

		// Get symbol info and output the frames
		for (size_t i = 0; i != count; ++i)
		{
			CallSource src = impl::StackDumpImpl<>::GetCallSource(frames[i]);
			out(src.m_sym_name, src.m_filepath, src.m_line);
		}
	}
	#if 0 // Ye Olde Way...
	template <typename TOut> void StackDump2(TOut out, int count = 100, int skip = 0)
	{
		impl::StackdumpLock lock;                              // Acquire the thread lock
		impl::DbgHelp_Dll& dbghelp = impl::DbgHelp_Dll::get(); // Ensure dbghelp.dll is Loaded

		// Get the context record directly
		// You're really not supposed to do this...
		CONTEXT ctx;
		RtlCaptureContext(&ctx);

		// Ignore the msdn docs, see: http://www.codeproject.com/Articles/11132/Walking-the-callstack
		STACKFRAME64 frame = {};
		#if defined(_M_IX86)
		const DWORD image_file_type = IMAGE_FILE_MACHINE_I386;
		frame.AddrPC.Mode           = AddrModeFlat;
		frame.AddrPC.Offset         = ctx.Eip;
		frame.AddrFrame.Mode        = AddrModeFlat;
		frame.AddrFrame.Offset      = ctx.Ebp;
		frame.AddrStack.Mode        = AddrModeFlat;
		frame.AddrStack.Offset      = ctx.Esp;
		#elif defined(_M_IA64)
		const DWORD image_file_type = IMAGE_FILE_MACHINE_IA64;
		frame.AddrPC.Mode           = AddrModeFlat;
		frame.AddrPC                = ctx.StIIP;
		frame.AddrFrame.Mode        = AddrModeFlat;
		frame.AddrFrame.Offset      = ctx.RsBSP;
		frame.AddrStack.Mode        = AddrModeFlat;
		frame.AddrStack.Offset      = ctx.IntSp;
		frame.AddrBStore.Mode       = AddrModeFlat;
		frame.AddrBStore            = ctx.RsBSP;
		#elif defined(_M_AMD64)
		const DWORD image_file_type = IMAGE_FILE_MACHINE_AMD64;
		frame.AddrPC.Mode           = AddrModeFlat;
		frame.AddrPC.Offset         = ctx.Rip;
		frame.AddrFrame.Mode        = AddrModeFlat;
		frame.AddrFrame.Offset      = ctx.Rsp;
		frame.AddrStack.Mode        = AddrModeFlat;
		frame.AddrStack.Offset      = ctx.Rsp;
		#else
		#error Unknown machine type!
		#endif

		size_t fcount = 0;
		size_t const max_fcount = 100;
		CallAddress frames[max_fcount];
		while (dbghelp.StackWalk64(
			image_file_type,
			dbghelp.Process(),
			GetCurrentThread(),
			&frame,
			&ctx,
			&impl::StackDumpImpl<>::DoReadProcessMemory,
			dbghelp.SymFunctionTableAccess64,
			dbghelp.SymGetModuleBase64,
			0))
		{
			// Sometimes StackWalk will leave the last error set, which causes us to report an (incorrect) error after this loop.
			SetLastError(0);
			if (skip > 0) --skip;
			else if (count-- == 0) break;
			else if (fcount == max_fcount) break;
			else frames[fcount++] = reinterpret_cast<CallAddress const&>(frame.AddrPC.Offset);
			if (frame.AddrReturn.Offset == 0)
				break;
		}

		// Get symbol info and output the frames
		for (size_t i = 0; i != fcount; ++i)
		{
			CallSource src = impl::StackDumpImpl<>::GetCallSource(frames[i]);
			out(src.m_sym_name, src.m_filepath, src.m_line);
		}
	}
	#endif

	// Return the call source from an arbitrary memory address
	inline CallSource GetCallSource(void* address)
	{
		return impl::StackDumpImpl<>::GetCallSource(reinterpret_cast<CallAddress const&>(address));
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <sstream>

namespace pr
{
	namespace unittests
	{
		struct StackDumpTest
		{
			template <typename TOut> static __declspec(noinline) void Func1(TOut out) { Func2(out); }
			template <typename TOut> static __declspec(noinline) void Func2(TOut out) { Func3(out); }
			template <typename TOut> static __declspec(noinline) void Func3(TOut out) { StackDump(out); }
		};

		PRUnitTest(pr_common_stackdump)
		{
			std::stringstream out;
			size_t fcount = 0;
			StackDumpTest::Func1([&](std::string sym, std::string file, int line)
			{
				out << file << "(" << line << "): " << sym << std::endl;
				++fcount;
			});

			PR_CHECK(fcount > 3, true);

			// Requires debug symbols...
			#if !defined(NDEBUG)
			auto s = out.str();
			std::string::size_type ofs = 0U;
			PR_CHECK((ofs = s.find("pr::unittests::StackDumpTest::Func3", ofs)) != std::string::npos, true);
			PR_CHECK((ofs = s.find("pr::unittests::StackDumpTest::Func2", ofs)) != std::string::npos, true);
			PR_CHECK((ofs = s.find("pr::unittests::StackDumpTest::Func1", ofs)) != std::string::npos, true);
			#endif
		}
	}
}
#endif
