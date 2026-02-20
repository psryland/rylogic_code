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
				"ListWindows: List all windows of a process (or all processes)\n"
				" Syntax: Cex -list_windows [-p <process-name>] [-all]\n"
				"  -p   : Name (or partial name) of the target process (optional)\n"
				"  -all : Include hidden/minimised windows\n"
				"\n"
				"  If -p is omitted, lists windows for all processes.\n"
				"  Outputs one line per window: HWND, size, visibility, process, and title.\n";
		}

		// Build a map of PID -> process name
		static std::unordered_map<DWORD, std::string> GetProcessNames()
		{
			std::unordered_map<DWORD, std::string> names;
			auto snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
			if (snap == INVALID_HANDLE_VALUE)
				return names;

			PROCESSENTRY32 pe = { .dwSize = sizeof(pe) };
			for (auto ok = Process32First(snap, &pe); ok; ok = Process32Next(snap, &pe))
				names[pe.th32ProcessID] = pe.szExeFile;

			CloseHandle(snap);
			return names;
		}

		// Enumerate all visible (or all) top-level windows
		static std::vector<HWND> EnumAllWindows(bool include_hidden)
		{
			struct EnumData
			{
				std::vector<HWND> windows;
				bool include_hidden;
			};
			EnumData data = { {}, include_hidden };

			EnumWindows([](HWND hwnd, LPARAM lparam) -> BOOL
			{
				auto& d = *reinterpret_cast<EnumData*>(lparam);

				if (!d.include_hidden && !IsWindowVisible(hwnd))
					return TRUE;

				RECT rc;
				if (!GetWindowRect(hwnd, &rc))
					return TRUE;
				if (rc.right - rc.left <= 0 || rc.bottom - rc.top <= 0)
					return TRUE;

				d.windows.push_back(hwnd);
				return TRUE;
			}, reinterpret_cast<LPARAM>(&data));

			return data.windows;
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			auto include_hidden = args.count("all") != 0;

			// Determine the set of windows to list
			std::vector<HWND> windows;
			bool show_process = false;

			if (args.count("p") != 0)
			{
				auto process_name = args("p").as<std::string>();
				auto pids = FindProcesses(process_name);
				if (pids.empty())
				{
					std::cerr << std::format("No running process found matching '{}'\n", process_name);
					return -1;
				}
				windows = FindWindows(pids, include_hidden);
			}
			else
			{
				// No process filter — list all windows
				windows = EnumAllWindows(include_hidden);
				show_process = true;
			}

			if (windows.empty())
			{
				std::cerr << std::format("No {} windows found\n", include_hidden ? "" : "visible");
				return -1;
			}

			// Build PID -> process name map when showing all windows
			auto proc_names = show_process ? GetProcessNames() : std::unordered_map<DWORD, std::string>{};

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

				// Include process name when listing all windows
				std::string proc_info;
				if (show_process)
				{
					DWORD pid = 0;
					GetWindowThreadProcessId(hwnd, &pid);
					auto it = proc_names.find(pid);
					proc_info = std::format("  [{}]", it != proc_names.end() ? it->second : std::format("PID:{}", pid));
				}

				std::cout << std::format("  HWND={:#010x}  {}x{} (client {}x{})  [{}]{}  '{}'\n",
					reinterpret_cast<uintptr_t>(hwnd), w, h, cw, ch, state,
					proc_info, title.empty() ? "(untitled)" : title);
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
