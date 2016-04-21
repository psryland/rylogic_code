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

#pragma once

#include <string>
#include <windows.h>
#include <cassert>
#include "pr/str/string_util.h"
#include "pr/common/hresult.h"

namespace pr
{
	class Process
	{
		STARTUPINFOW        m_startup_info;
		PROCESS_INFORMATION m_process_info;

		// Reset things so that a process can be started
		void Reset()
		{
			m_startup_info = STARTUPINFOW{sizeof(STARTUPINFOW)};
			m_process_info = PROCESS_INFORMATION();
			m_process_info.hProcess = nullptr;
			m_process_info.hThread	= nullptr;
		}

	public:
		Process() { Reset(); }
		~Process() { Stop(); }

		// Start the process
		bool Start(wchar_t const* exe_path, wchar_t const* args, wchar_t const* startdir)
		{
			if (m_process_info.hProcess != nullptr) Stop();
			auto cmdline = pr::str::Quotes<std::wstring>(exe_path, true).append(L" ").append(args);
			return Succeeded(CreateProcessW(exe_path, &cmdline[0], 0, 0, TRUE, 0, 0, startdir, &m_startup_info, &m_process_info));
		}
		bool Start(wchar_t const* exe_path, wchar_t const* args)
		{
			return Start(exe_path, args, nullptr);
		}
		bool Start(wchar_t const* exe_path)
		{
			return Start(exe_path, nullptr);
		}

		// Shutdown the process
		int Stop()
		{
			int exit_code = -1;
			if (m_process_info.hProcess != nullptr)
			{
				PostThreadMessageW(m_process_info.dwThreadId, WM_QUIT, 0, 0);
				exit_code = BlockTillExit();
			}
			if (m_process_info.hProcess != nullptr) CloseHandle(m_process_info.hProcess);
			if (m_process_info.hThread  != nullptr) CloseHandle(m_process_info.hThread);
			Reset();
			return exit_code;
		}

		// Block the calling thread until the child process exits
		int BlockTillExit()
		{
			if (m_process_info.hProcess == nullptr)
				throw std::exception("Process not running");

			WaitForSingleObject(m_process_info.hProcess, INFINITE);
			
			DWORD exit_code;
			pr::Throw(GetExitCodeProcess(m_process_info.hProcess, &exit_code), "Process exit code not available");
			return static_cast<int>(exit_code);
		}

		// Returns true if the process is running
		bool IsActive() const
		{
			assert((m_process_info.hProcess == nullptr) == (m_process_info.hThread == nullptr));
			return m_process_info.hProcess != nullptr;
		}
	};
}
