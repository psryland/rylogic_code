//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// WaitWindow: Wait for a window matching a title to appear.
#include "src/forward.h"
#include "src/commands/process_util.h"

namespace cex
{
	struct Cmd_WaitWindow
	{
		void ShowHelp() const
		{
			std::cout <<
				"WaitWindow: Wait for a window to appear\n"
				" Syntax: Cex -wait_window -p <process-name> [-w <window-name>] [-timeout <ms>]\n"
				"  -p       : Name (or partial name) of the target process\n"
				"  -w       : Title (or partial title) to wait for (default: any window)\n"
				"  -timeout : Maximum time to wait in milliseconds (default: 30000)\n"
				"\n"
				"  Polls for a window matching the criteria. Returns 0 when found,\n"
				"  or 1 on timeout. Useful for waiting for app startup or dialogs.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			int timeout_ms = 30000;
			if (args.count("timeout") != 0) { timeout_ms = args("timeout").as<int>(); }

			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }

			auto start = GetTickCount64();
			auto deadline = start + static_cast<ULONGLONG>(timeout_ms);

			std::cout << std::format("Waiting for '{}'{} (timeout: {}ms)\n",
				process_name,
				window_name.empty() ? "" : std::format(" window '{}'", window_name),
				timeout_ms);
			std::cout.flush();

			for (;;)
			{
				auto hwnd = FindWindow(process_name, window_name);
				if (hwnd)
				{
					auto elapsed = GetTickCount64() - start;
					auto title = GetWindowTitle(hwnd);

					RECT rc;
					GetWindowRect(hwnd, &rc);
					auto w = rc.right - rc.left;
					auto h = rc.bottom - rc.top;

					std::cout << std::format("Found '{}' ({}x{}) after {}ms\n", title, w, h, elapsed);
					return 0;
				}

				if (GetTickCount64() >= deadline)
				{
					std::cerr << std::format("Timeout: no window found after {}ms\n", timeout_ms);
					return 1;
				}

				Sleep(250);
			}
		}
	};

	int WaitWindow(pr::CmdLine const& args)
	{
		Cmd_WaitWindow cmd;
		return cmd.Run(args);
	}
}
