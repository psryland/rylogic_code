//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// ShutdownProcess: Gracefully close a process by sending WM_CLOSE to its windows.
#include "src/forward.h"
#include "src/commands/process_util.h"

namespace cex
{
	struct Cmd_ShutdownProcess
	{
		void ShowHelp() const
		{
			std::cout <<
				"ShutdownProcess: Gracefully shut down a process\n"
				" Syntax: Cex -shutdown_process -p <process-name> [-w <window-name>] [-timeout <ms>]\n"
				"  -p       : Name (or partial name) of the target process\n"
				"  -w       : Title (or partial title) of a specific window to close (default: all)\n"
				"  -timeout : Time in milliseconds to wait for the process to exit (default: 5000)\n"
				"             If the process doesn't exit within the timeout it is left running.\n"
				"             Use -timeout 0 to send WM_CLOSE without waiting.\n"
				"\n"
				"  Sends WM_CLOSE to the process's windows, giving it a chance to save state\n"
				"  and clean up. This is equivalent to clicking the window's close button.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			int timeout_ms = 5000;
			if (args.count("timeout") != 0) { timeout_ms = args("timeout").as<int>(); }

			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }

			auto pids = FindProcesses(process_name);
			if (pids.empty())
			{
				std::cerr << std::format("No running process found matching '{}'\n", process_name);
				return -1;
			}

			// Collect windows to close
			auto windows = FindWindows(pids, true);
			if (windows.empty())
			{
				std::cerr << std::format("No windows found for '{}'\n", process_name);
				return -1;
			}

			// Optionally filter by window name
			if (!window_name.empty())
			{
				auto iwname = window_name;
				std::transform(iwname.begin(), iwname.end(), iwname.begin(), [](char c) { return static_cast<char>(tolower(c)); });

				std::erase_if(windows, [&](HWND hwnd)
				{
					auto ititle = GetWindowTitle(hwnd);
					std::transform(ititle.begin(), ititle.end(), ititle.begin(), [](char c) { return static_cast<char>(tolower(c)); });
					return ititle.find(iwname) == std::string::npos;
				});

				if (windows.empty())
				{
					std::cerr << std::format("No windows matching '{}' found for '{}'\n", window_name, process_name);
					return -1;
				}
			}

			// Collect the owning PIDs so we can wait for them
			std::vector<HANDLE> handles;
			std::vector<DWORD> target_pids;
			for (auto hwnd : windows)
			{
				DWORD pid = 0;
				GetWindowThreadProcessId(hwnd, &pid);
				if (pid != 0 && std::find(target_pids.begin(), target_pids.end(), pid) == target_pids.end())
				{
					auto h = OpenProcess(SYNCHRONIZE, FALSE, pid);
					if (h)
					{
						handles.push_back(h);
						target_pids.push_back(pid);
					}
				}
			}

			// Send WM_CLOSE to each window
			for (auto hwnd : windows)
			{
				auto title = GetWindowTitle(hwnd);
				std::cout << std::format("Closing '{}'\n", title.empty() ? "(untitled)" : title);
				PostMessage(hwnd, WM_CLOSE, 0, 0);
			}

			// Wait for the processes to exit
			if (timeout_ms > 0 && !handles.empty())
			{
				std::cout << std::format("Waiting up to {}ms for {} process(es) to exit...\n", timeout_ms, handles.size());
				auto result = WaitForMultipleObjects(
					static_cast<DWORD>(handles.size()),
					handles.data(),
					TRUE,
					static_cast<DWORD>(timeout_ms));

				if (result == WAIT_TIMEOUT)
				{
					std::cerr << "Timeout: process(es) did not exit\n";
					for (auto h : handles) CloseHandle(h);
					return 1;
				}

				std::cout << "Process(es) exited\n";
			}

			for (auto h : handles) CloseHandle(h);
			return 0;
		}
	};

	int ShutdownProcess(pr::CmdLine const& args)
	{
		Cmd_ShutdownProcess cmd;
		return cmd.Run(args);
	}
}
