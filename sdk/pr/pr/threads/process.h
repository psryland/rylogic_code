//**********************************************************
// Process
//  Copyright (c) Rylogic Ltd 2011
//**********************************************************
// Usage:
//  pr::Process proc;
//  proc.Start("c:\program files\program_name.exe", "-args");
//  <do stuff>
//  int exit_code = proc.BlockTillExit();
//  <or>
//  int exit_code = proc.Stop();

#ifndef PR_PROCESS_H
#define PR_PROCESS_H

#include <string>
#include <windows.h>
#include "pr/common/assert.h"
#include "pr/str/prstring.h"

namespace pr
{
	class Process
	{
		STARTUPINFO         m_startup_info;
		PROCESS_INFORMATION m_process_info;

		// Reset things so that a process can be started
		void Reset()
		{
			m_startup_info = STARTUPINFO();
			m_startup_info.cb = sizeof(m_startup_info);
			m_process_info = PROCESS_INFORMATION();
			m_process_info.hProcess = INVALID_HANDLE_VALUE;
			m_process_info.hThread	= INVALID_HANDLE_VALUE;
		}

	public:
		Process() { Reset(); }
		~Process() { Stop(); }

		// Start the process
		bool Start(char const* exe_path, char const* args, char const* startdir)
		{
			if (m_process_info.hProcess != INVALID_HANDLE_VALUE) Stop();
			std::string cmdline = pr::str::Quotes<std::string>(exe_path, true); cmdline.append(" ").append(args);
			return CreateProcess(exe_path, &cmdline[0], 0, 0, TRUE, 0, 0, startdir, &m_startup_info, &m_process_info) == TRUE;
		}
		bool Start(char const* exe_path, char const* args)
		{
			return Start(exe_path, args, 0);
		}
		bool Start(char const* exe_path)
		{
			return Start(exe_path, 0);
		}

		// Shutdown the process
		int Stop()
		{
			PostThreadMessage(m_process_info.dwThreadId, WM_QUIT, 0, 0);
			int exit_code = BlockTillExit();
			if (m_process_info.hProcess != INVALID_HANDLE_VALUE) CloseHandle(m_process_info.hProcess);
			if (m_process_info.hThread  != INVALID_HANDLE_VALUE) CloseHandle(m_process_info.hThread);
			Reset();
			return exit_code;
		}

		// Block the calling thread until the child process exits
		int BlockTillExit()
		{
			WaitForSingleObject(m_process_info.hProcess, INFINITE);
			
			DWORD exit_code;
			if (!GetExitCodeProcess(m_process_info.hProcess, &exit_code)) { PR_ASSERT(PR_DBG, false, "Process exit code not available"); return -1; }
			return static_cast<int>(exit_code);
		}

		// Returns true if the process is running
		bool IsActive() const
		{
			PR_ASSERT(PR_DBG,(m_process_info.hProcess == INVALID_HANDLE_VALUE) == (m_process_info.hThread == INVALID_HANDLE_VALUE), "");
			return m_process_info.hProcess != INVALID_HANDLE_VALUE;
		}
	};
}

#endif
