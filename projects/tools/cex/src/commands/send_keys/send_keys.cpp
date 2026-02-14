//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/commands/process_util.h"

namespace cex
{
	struct Cmd_SendKeys
	{
		void ShowHelp() const
		{
			std::cout <<
				"SendKeys: Send key presses to a window\n"
				" Syntax: Cex -send_keys \"text\" -p <process-name> [-w <window-name>] [-rate <keys-per-second>]\n"
				"  -p    : Name (or partial name) of the target process\n"
				"  -w    : Title (or partial title) of the target window (default: largest)\n"
				"  -rate : Key press rate in keys per second (default: 10)\n"
				"\n"
				"  Brings the window to the foreground and uses SendInput for\n"
				"  hardware-level key simulation. Works with all applications.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string text;
			if (args.count("send_keys") != 0)
			{
				for (auto const& v : args("send_keys").values)
					text.append(v);
			}

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			double rate = 10.0;
			if (args.count("rate") != 0) { rate = args("rate").as<double>(); }

			if (text.empty())         { std::cerr << "No text to send\n"; return ShowHelp(), -1; }
			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }
			if (rate <= 0)            { std::cerr << "Rate must be positive\n"; return -1; }

			auto hwnd = FindWindow(process_name, window_name);
			if (!hwnd)
			{
				auto target = window_name.empty() ? process_name : std::format("{}:{}", process_name, window_name);
				std::cerr << std::format("No window found for '{}'\n", target);
				return -1;
			}

			auto delay_ms = static_cast<DWORD>(1000.0 / rate);

			// Print status before bringing the target to the foreground,
			// otherwise writing to the console can steal focus back.
			std::cout << std::format("Sending {} key(s) to '{}' at {:.0f} keys/sec\n", text.size(), GetWindowTitle(hwnd), rate);
			std::cout.flush();

			// Bring the target window to the foreground. Click the client area to
			// ensure keyboard focus is inside the window's content control.
			BringToForeground(hwnd, true);

			for (auto ch : text)
			{
				// Use KEYEVENTF_UNICODE to send the character directly as a unicode scancode.
				// This works regardless of the target's internal window hierarchy.
				INPUT inputs[2] = {};
				inputs[0].type = INPUT_KEYBOARD;
				inputs[0].ki.wScan = static_cast<WORD>(ch);
				inputs[0].ki.dwFlags = KEYEVENTF_UNICODE;

				inputs[1].type = INPUT_KEYBOARD;
				inputs[1].ki.wScan = static_cast<WORD>(ch);
				inputs[1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

				SendInput(2, inputs, sizeof(INPUT));
				Sleep(delay_ms);
			}

			return 0;
		}
	};

	int SendKeys(pr::CmdLine const& args)
	{
		Cmd_SendKeys cmd;
		return cmd.Run(args);
	}
}
