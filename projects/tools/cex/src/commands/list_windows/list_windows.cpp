//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// ListWindows: List all windows belonging to a process.
#include "src/forward.h"
#include "src/commands/process_util.h"

namespace cex
{
	struct Cmd_ListWindows
	{
		void ShowHelp() const
		{
			std::cout <<
				"ListWindows: List all windows of a process\n"
				" Syntax: Cex -list_windows -p <process-name> [-all]\n"
				"  -p   : Name (or partial name) of the target process\n"
				"  -all : Include hidden/minimised windows\n"
				"\n"
				"  Outputs one line per window: HWND, size, visibility, and title.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }

			auto include_hidden = args.count("all") != 0;

			auto pids = FindProcesses(process_name);
			if (pids.empty())
			{
				std::cerr << std::format("No running process found matching '{}'\n", process_name);
				return -1;
			}

			auto windows = FindWindows(pids, include_hidden);
			if (windows.empty())
			{
				std::cerr << std::format("No {} windows found for '{}'\n", include_hidden ? "" : "visible", process_name);
				return -1;
			}

			std::cout << std::format("{} window(s) found:\n", windows.size());
			for (auto hwnd : windows)
			{
				auto title = GetWindowTitle(hwnd);
				auto visible = IsWindowVisible(hwnd) != 0;
				auto iconic = IsIconic(hwnd) != 0;

				RECT rc;
				GetWindowRect(hwnd, &rc);
				auto w = rc.right - rc.left;
				auto h = rc.bottom - rc.top;

				RECT crc;
				GetClientRect(hwnd, &crc);
				auto cw = crc.right - crc.left;
				auto ch = crc.bottom - crc.top;

				auto state = iconic ? "minimised" : (visible ? "visible" : "hidden");

				std::cout << std::format("  HWND={:#010x}  {}x{} (client {}x{})  [{}]  '{}'\n",
					reinterpret_cast<uintptr_t>(hwnd), w, h, cw, ch, state,
					title.empty() ? "(untitled)" : title);
			}
			return 0;
		}
	};

	int ListWindows(pr::CmdLine const& args)
	{
		Cmd_ListWindows cmd;
		return cmd.Run(args);
	}
}
