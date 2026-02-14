//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/commands/process_util.h"

namespace cex
{
	struct Cmd_SendMouse
	{
		void ShowHelp() const
		{
			std::cout <<
				"SendMouse: Send mouse events to a window\n"
				" Syntax: Cex -send_mouse x,y -b <button-action> -p <process-name> [-w <window-name>]\n"
				"  -p : Name (or partial name) of the target process\n"
				"  -w : Title (or partial title) of the target window (default: largest)\n"
				"  -b : Button action. One of:\n"
				"       LeftDown, LeftUp, LeftClick\n"
				"       RightDown, RightUp, RightClick\n"
				"       MiddleDown, MiddleUp, MiddleClick\n"
				"       Move\n"
				"\n"
				"  Brings the window to the foreground and uses SendInput for\n"
				"  hardware-level mouse simulation.\n"
				"  x,y are coordinates relative to the window's client area.\n"
				"  'Click' actions send a button-down followed by a button-up.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			// Parse the x,y coordinates from the value of send_mouse
			int x = 0, y = 0;
			if (args.count("send_mouse") != 0)
			{
				auto pos_str = args("send_mouse").as<std::string>();
				if (!ParseCoords(pos_str, x, y))
				{
					std::cerr << std::format("Invalid coordinates '{}'. Expected format: x,y\n", pos_str);
					return -1;
				}
			}

			std::string button_action;
			if (args.count("b") != 0) { button_action = args("b").as<std::string>(); }

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			if (button_action.empty()) { std::cerr << "No button action provided (-b)\n"; return ShowHelp(), -1; }
			if (process_name.empty())  { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }

			auto hwnd = FindWindow(process_name, window_name);
			if (!hwnd)
			{
				auto target = window_name.empty() ? process_name : std::format("{}:{}", process_name, window_name);
				std::cerr << std::format("No window found for '{}'\n", target);
				return -1;
			}

			// Convert client coordinates to absolute screen coordinates for SendInput
			auto abs = ClientToAbsScreen(hwnd, x, y);

			// Lowercase the action for case-insensitive matching
			auto action = button_action;
			std::transform(action.begin(), action.end(), action.begin(), [](char c) { return static_cast<char>(tolower(c)); });

			// Print status before bringing the target to the foreground,
			// otherwise writing to the console can steal focus back.
			std::cout << std::format("Sending mouse {} at ({},{}) to '{}'\n", button_action, x, y, GetWindowTitle(hwnd));
			std::cout.flush();

			// Bring the target window to the foreground
			BringToForeground(hwnd);

			if      (action == "leftdown")    { SendMouseInput(abs, MOUSEEVENTF_LEFTDOWN); }
			else if (action == "leftup")      { SendMouseInput(abs, MOUSEEVENTF_LEFTUP); }
			else if (action == "leftclick")   { SendMouseInput(abs, MOUSEEVENTF_LEFTDOWN);
			                                    SendMouseInput(abs, MOUSEEVENTF_LEFTUP); }
			else if (action == "rightdown")   { SendMouseInput(abs, MOUSEEVENTF_RIGHTDOWN); }
			else if (action == "rightup")     { SendMouseInput(abs, MOUSEEVENTF_RIGHTUP); }
			else if (action == "rightclick")  { SendMouseInput(abs, MOUSEEVENTF_RIGHTDOWN);
			                                    SendMouseInput(abs, MOUSEEVENTF_RIGHTUP); }
			else if (action == "middledown")  { SendMouseInput(abs, MOUSEEVENTF_MIDDLEDOWN); }
			else if (action == "middleup")    { SendMouseInput(abs, MOUSEEVENTF_MIDDLEUP); }
			else if (action == "middleclick") { SendMouseInput(abs, MOUSEEVENTF_MIDDLEDOWN);
			                                    SendMouseInput(abs, MOUSEEVENTF_MIDDLEUP); }
			else if (action == "move")        { SendMouseInput(abs, 0); }
			else
			{
				std::cerr << std::format("Unknown button action '{}'\n", button_action);
				return ShowHelp(), -1;
			}

			return 0;
		}

	private:

		// Parse "x,y" coordinate string
		static bool ParseCoords(std::string const& str, int& x, int& y)
		{
			auto comma = str.find(',');
			if (comma == std::string::npos)
				return false;

			try
			{
				x = std::stoi(str.substr(0, comma));
				y = std::stoi(str.substr(comma + 1));
				return true;
			}
			catch (...)
			{
				return false;
			}
		}

		// Send a mouse input event at absolute screen coordinates
		static void SendMouseInput(POINT abs, DWORD button_flags)
		{
			INPUT input = {};
			input.type = INPUT_MOUSE;
			input.mi.dx = abs.x;
			input.mi.dy = abs.y;
			input.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK | button_flags;
			SendInput(1, &input, sizeof(INPUT));
		}
	};

	int SendMouse(pr::CmdLine const& args)
	{
		Cmd_SendMouse cmd;
		return cmd.Run(args);
	}
}
