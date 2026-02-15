//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// Automate: a scripting command for batched mouse/keyboard input.
// Reads a line-based script from stdin and executes each command sequentially.
#include "src/forward.h"
#include "src/commands/process_util.h"

#include <charconv>
#include <cmath>

namespace cex
{
	namespace
	{
		double constexpr tau = 6.283185307179586476925286766559;
		double constexpr deg_to_rad = tau / 360.0;
	}

	struct Cmd_Automate
	{
		void ShowHelp() const
		{
			std::cout <<
				"Automate: Execute a script of mouse/keyboard commands\n"
				" Syntax: Cex -automate -p <process-name> [-w <window-name>] [-f <script-file>]\n"
				"  -p : Name (or partial name) of the target process\n"
				"  -w : Title (or partial title) of the target window (default: largest)\n"
				"  -f : Script file to read (default: stdin)\n"
				"\n"
				"  Reads commands from stdin, one per line. Lines starting with '#' are comments.\n"
				"  All coordinates are relative to the window's client area.\n"
				"\n"
				"  Mouse commands:\n"
				"    move x,y               Move cursor to client coordinates\n"
				"    click x,y [button]      Click at position (default: left)\n"
				"    down x,y [button]       Press button down at position\n"
				"    up [button]             Release button\n"
				"    drag x1,y1 x2,y2 [N]   Drag from A to B in N steps (default: 20)\n"
				"\n"
				"  Drawing primitives (executed as mouse drags):\n"
				"    line x1,y1 x2,y2       Draw a straight line\n"
				"    circle cx,cy r [N]      Draw a circle (default N=80)\n"
				"    arc cx,cy r a0 a1 [N]   Draw an arc (angles in degrees, default N=60)\n"
				"    fill_circle cx,cy r [N] Draw concentric circles to fill a dot\n"
				"\n"
				"  Keyboard commands:\n"
				"    type text...            Send unicode text (rest of line)\n"
				"    key combo               Key combo: ctrl+a, shift+delete, enter, f5, etc.\n"
				"\n"
				"  Timing:\n"
				"    delay ms                Pause for N milliseconds\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string process_name;
			if (args.count("p") != 0) { process_name = args("p").as<std::string>(); }

			std::string window_name;
			if (args.count("w") != 0) { window_name = args("w").as<std::string>(); }

			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }

			auto hwnd = FindWindow(process_name, window_name);
			if (!hwnd)
			{
				auto target = window_name.empty() ? process_name : std::format("{}:{}", process_name, window_name);
				std::cerr << std::format("No window found for '{}'\n", target);
				return -1;
			}

			// Open the script source (file or stdin)
			std::ifstream file_stream;
			if (args.count("f") != 0)
			{
				auto path = args("f").as<std::string>();
				file_stream.open(path);
				if (!file_stream) { std::cerr << std::format("Cannot open script file '{}'\n", path); return -1; }
			}
			auto& input = file_stream.is_open() ? static_cast<std::istream&>(file_stream) : std::cin;

			std::cout << std::format("Automating '{}'\n", GetWindowTitle(hwnd));
			std::cout.flush();

			// Bring the target window to the foreground once
			BringToForeground(hwnd);

			// Read and execute script lines
			std::string line;
			int line_num = 0;
			while (std::getline(input, line))
			{
				++line_num;

				// Trim whitespace
				auto start = line.find_first_not_of(" \t\r");
				if (start == std::string::npos) continue;
				line = line.substr(start);

				// Skip comments and empty lines
				if (line.empty() || line[0] == '#') continue;

				auto result = ExecuteLine(hwnd, line);
				if (result != 0)
				{
					std::cerr << std::format("Error on line {}: '{}'\n", line_num, line);
					return result;
				}
			}

			std::cout << std::format("Script complete ({} lines)\n", line_num);
			return 0;
		}

	private:

		// The last known cursor position (for 'up' without coordinates)
		POINT m_last_abs = {};

		// Parse a comma-separated coordinate pair "x,y"
		static bool ParseXY(std::string const& s, int& x, int& y)
		{
			auto comma = s.find(',');
			if (comma == std::string::npos) return false;
			auto rx = std::from_chars(s.data(), s.data() + comma, x);
			auto ry = std::from_chars(s.data() + comma + 1, s.data() + s.size(), y);
			return rx.ec == std::errc{} && ry.ec == std::errc{};
		}

		// Parse a single number
		static bool ParseNum(std::string const& s, int& v)
		{
			auto r = std::from_chars(s.data(), s.data() + s.size(), v);
			return r.ec == std::errc{};
		}
		static bool ParseNum(std::string const& s, double& v)
		{
			auto r = std::from_chars(s.data(), s.data() + s.size(), v);
			return r.ec == std::errc{};
		}

		// Split a line into whitespace-separated tokens
		static std::vector<std::string> Tokenize(std::string const& line)
		{
			std::vector<std::string> tokens;
			std::istringstream iss(line);
			std::string token;
			while (iss >> token)
				tokens.push_back(std::move(token));
			return tokens;
		}

		// Resolve a button name to MOUSEEVENTF flags (down and up)
		static bool ResolveButton(std::string const& name, DWORD& down_flag, DWORD& up_flag)
		{
			auto lower = name;
			std::transform(lower.begin(), lower.end(), lower.begin(), [](char c) { return static_cast<char>(tolower(c)); });

			if      (lower == "left"   || lower.empty()) { down_flag = MOUSEEVENTF_LEFTDOWN;   up_flag = MOUSEEVENTF_LEFTUP;   }
			else if (lower == "right")                   { down_flag = MOUSEEVENTF_RIGHTDOWN;  up_flag = MOUSEEVENTF_RIGHTUP;  }
			else if (lower == "middle")                  { down_flag = MOUSEEVENTF_MIDDLEDOWN; up_flag = MOUSEEVENTF_MIDDLEUP; }
			else return false;
			return true;
		}

		// Send a mouse input event at absolute screen coordinates
		void SendMouse(POINT abs, DWORD flags)
		{
			INPUT input = {};
			input.type = INPUT_MOUSE;
			input.mi.dx = abs.x;
			input.mi.dy = abs.y;
			input.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK | flags;
			::SendInput(1, &input, sizeof(INPUT));
			m_last_abs = abs;
		}

		// Move the cursor and drag through a sequence of client-area points
		void DragPath(HWND hwnd, std::vector<std::pair<double, double>> const& points)
		{
			if (points.empty()) return;

			// Move to start and press
			auto abs = ClientToAbsScreen(hwnd, static_cast<int>(points[0].first), static_cast<int>(points[0].second));
			SendMouse(abs, MOUSEEVENTF_LEFTDOWN);
			Sleep(10);

			// Move through intermediate points
			for (size_t i = 1; i != points.size(); ++i)
			{
				abs = ClientToAbsScreen(hwnd, static_cast<int>(points[i].first), static_cast<int>(points[i].second));
				SendMouse(abs, 0);
				Sleep(2);
			}

			// Release
			SendMouse(abs, MOUSEEVENTF_LEFTUP);
			Sleep(30);
		}

		// Generate arc points in client coordinates
		static std::vector<std::pair<double, double>> ArcPoints(double cx, double cy, double r, double a0_deg, double a1_deg, int steps)
		{
			std::vector<std::pair<double, double>> pts;
			auto a0 = a0_deg * deg_to_rad;
			auto a1 = a1_deg * deg_to_rad;
			for (int i = 0; i != steps + 1; ++i)
			{
				auto t = a0 + (a1 - a0) * i / steps;
				pts.emplace_back(cx + r * std::cos(t), cy + r * std::sin(t));
			}
			return pts;
		}

		// Resolve a key name to a virtual key code
		static WORD ResolveVKey(std::string const& name)
		{
			auto lower = name;
			std::transform(lower.begin(), lower.end(), lower.begin(), [](char c) { return static_cast<char>(tolower(c)); });

			if (lower == "ctrl" || lower == "control") return VK_CONTROL;
			if (lower == "alt")                        return VK_MENU;
			if (lower == "shift")                      return VK_SHIFT;
			if (lower == "win")                        return VK_LWIN;
			if (lower == "enter" || lower == "return")  return VK_RETURN;
			if (lower == "tab")                        return VK_TAB;
			if (lower == "esc" || lower == "escape")   return VK_ESCAPE;
			if (lower == "backspace" || lower == "bs") return VK_BACK;
			if (lower == "delete" || lower == "del")   return VK_DELETE;
			if (lower == "insert" || lower == "ins")   return VK_INSERT;
			if (lower == "home")                       return VK_HOME;
			if (lower == "end")                        return VK_END;
			if (lower == "pageup" || lower == "pgup")  return VK_PRIOR;
			if (lower == "pagedown" || lower == "pgdn") return VK_NEXT;
			if (lower == "up")                         return VK_UP;
			if (lower == "down")                       return VK_DOWN;
			if (lower == "left")                       return VK_LEFT;
			if (lower == "right")                      return VK_RIGHT;
			if (lower == "space")                      return VK_SPACE;

			// Function keys F1–F24
			if (lower.size() >= 2 && lower[0] == 'f')
			{
				int n = 0;
				auto r = std::from_chars(lower.data() + 1, lower.data() + lower.size(), n);
				if (r.ec == std::errc{} && n >= 1 && n <= 24)
					return static_cast<WORD>(VK_F1 + n - 1);
			}

			// Single character a-z → VK code (uppercase ASCII)
			if (lower.size() == 1 && lower[0] >= 'a' && lower[0] <= 'z')
				return static_cast<WORD>(lower[0] - 'a' + 'A');

			// Single digit 0-9
			if (lower.size() == 1 && lower[0] >= '0' && lower[0] <= '9')
				return static_cast<WORD>(lower[0]);

			return 0;
		}

		// Execute a single script line
		int ExecuteLine(HWND hwnd, std::string const& line)
		{
			auto tokens = Tokenize(line);
			if (tokens.empty()) return 0;

			auto cmd = tokens[0];
			std::transform(cmd.begin(), cmd.end(), cmd.begin(), [](char c) { return static_cast<char>(tolower(c)); });

			// ── move x,y ──
			if (cmd == "move")
			{
				if (tokens.size() < 2) { std::cerr << "move: expected x,y\n"; return -1; }
				int x, y;
				if (!ParseXY(tokens[1], x, y)) { std::cerr << std::format("move: invalid coords '{}'\n", tokens[1]); return -1; }
				auto abs = ClientToAbsScreen(hwnd, x, y);
				SendMouse(abs, 0);
				return 0;
			}

			// ── click x,y [button] ──
			if (cmd == "click")
			{
				if (tokens.size() < 2) { std::cerr << "click: expected x,y\n"; return -1; }
				int x, y;
				if (!ParseXY(tokens[1], x, y)) { std::cerr << std::format("click: invalid coords '{}'\n", tokens[1]); return -1; }
				auto button = tokens.size() >= 3 ? tokens[2] : std::string("left");
				DWORD down_flag, up_flag;
				if (!ResolveButton(button, down_flag, up_flag)) { std::cerr << std::format("click: unknown button '{}'\n", button); return -1; }
				auto abs = ClientToAbsScreen(hwnd, x, y);
				SendMouse(abs, down_flag);
				Sleep(10);
				SendMouse(abs, up_flag);
				Sleep(30);
				return 0;
			}

			// ── down x,y [button] ──
			if (cmd == "down")
			{
				if (tokens.size() < 2) { std::cerr << "down: expected x,y\n"; return -1; }
				int x, y;
				if (!ParseXY(tokens[1], x, y)) { std::cerr << std::format("down: invalid coords '{}'\n", tokens[1]); return -1; }
				auto button = tokens.size() >= 3 ? tokens[2] : std::string("left");
				DWORD down_flag, up_flag;
				if (!ResolveButton(button, down_flag, up_flag)) { std::cerr << std::format("down: unknown button '{}'\n", button); return -1; }
				auto abs = ClientToAbsScreen(hwnd, x, y);
				SendMouse(abs, down_flag);
				return 0;
			}

			// ── up [button] ──
			if (cmd == "up")
			{
				auto button = tokens.size() >= 2 ? tokens[1] : std::string("left");
				DWORD down_flag, up_flag;
				if (!ResolveButton(button, down_flag, up_flag)) { std::cerr << std::format("up: unknown button '{}'\n", button); return -1; }

				// Send at last known position
				SendMouse(m_last_abs, up_flag);
				return 0;
			}

			// ── drag x1,y1 x2,y2 [N] ──
			if (cmd == "drag")
			{
				if (tokens.size() < 3) { std::cerr << "drag: expected x1,y1 x2,y2\n"; return -1; }
				int x0, y0, x1, y1;
				if (!ParseXY(tokens[1], x0, y0)) { std::cerr << std::format("drag: invalid coords '{}'\n", tokens[1]); return -1; }
				if (!ParseXY(tokens[2], x1, y1)) { std::cerr << std::format("drag: invalid coords '{}'\n", tokens[2]); return -1; }
				int steps = 20;
				if (tokens.size() >= 4) ParseNum(tokens[3], steps);

				std::vector<std::pair<double, double>> pts;
				for (int i = 0; i != steps + 1; ++i)
				{
					auto t = static_cast<double>(i) / steps;
					pts.emplace_back(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t);
				}
				DragPath(hwnd, pts);
				return 0;
			}

			// ── line x1,y1 x2,y2 ──
			if (cmd == "line")
			{
				if (tokens.size() < 3) { std::cerr << "line: expected x1,y1 x2,y2\n"; return -1; }
				int x0, y0, x1, y1;
				if (!ParseXY(tokens[1], x0, y0)) { std::cerr << std::format("line: invalid coords '{}'\n", tokens[1]); return -1; }
				if (!ParseXY(tokens[2], x1, y1)) { std::cerr << std::format("line: invalid coords '{}'\n", tokens[2]); return -1; }
				auto dx = x1 - x0, dy = y1 - y0;
				auto len = std::sqrt(static_cast<double>(dx * dx + dy * dy));
				auto steps = std::max(2, static_cast<int>(len / 2.0));

				std::vector<std::pair<double, double>> pts;
				for (int i = 0; i != steps + 1; ++i)
				{
					auto t = static_cast<double>(i) / steps;
					pts.emplace_back(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t);
				}
				DragPath(hwnd, pts);
				return 0;
			}

			// ── circle cx,cy r [N] ──
			if (cmd == "circle")
			{
				if (tokens.size() < 3) { std::cerr << "circle: expected cx,cy r\n"; return -1; }
				int cx, cy;
				double r;
				if (!ParseXY(tokens[1], cx, cy)) { std::cerr << std::format("circle: invalid coords '{}'\n", tokens[1]); return -1; }
				if (!ParseNum(tokens[2], r))      { std::cerr << std::format("circle: invalid radius '{}'\n", tokens[2]); return -1; }
				int steps = 80;
				if (tokens.size() >= 4) ParseNum(tokens[3], steps);

				auto pts = ArcPoints(cx, cy, r, 0, 360, steps);
				DragPath(hwnd, pts);
				return 0;
			}

			// ── arc cx,cy r a0 a1 [N] ──
			if (cmd == "arc")
			{
				if (tokens.size() < 5) { std::cerr << "arc: expected cx,cy r a0 a1\n"; return -1; }
				int cx, cy;
				double r, a0, a1;
				if (!ParseXY(tokens[1], cx, cy)) { std::cerr << std::format("arc: invalid coords '{}'\n", tokens[1]); return -1; }
				if (!ParseNum(tokens[2], r))      { std::cerr << std::format("arc: invalid radius '{}'\n", tokens[2]); return -1; }
				if (!ParseNum(tokens[3], a0))     { std::cerr << std::format("arc: invalid angle '{}'\n", tokens[3]); return -1; }
				if (!ParseNum(tokens[4], a1))     { std::cerr << std::format("arc: invalid angle '{}'\n", tokens[4]); return -1; }
				int steps = 60;
				if (tokens.size() >= 6) ParseNum(tokens[5], steps);

				auto pts = ArcPoints(cx, cy, r, a0, a1, steps);
				DragPath(hwnd, pts);
				return 0;
			}

			// ── fill_circle cx,cy r [N] ──
			if (cmd == "fill_circle")
			{
				if (tokens.size() < 3) { std::cerr << "fill_circle: expected cx,cy r\n"; return -1; }
				int cx, cy;
				double r;
				if (!ParseXY(tokens[1], cx, cy)) { std::cerr << std::format("fill_circle: invalid coords '{}'\n", tokens[1]); return -1; }
				if (!ParseNum(tokens[2], r))      { std::cerr << std::format("fill_circle: invalid radius '{}'\n", tokens[2]); return -1; }
				int steps = 20;
				if (tokens.size() >= 4) ParseNum(tokens[3], steps);

				// Draw concentric circles from inside out
				for (double ri = 1.0; ri <= r; ri += 2.0)
				{
					auto pts = ArcPoints(cx, cy, ri, 0, 360, steps);
					DragPath(hwnd, pts);
				}
				return 0;
			}

			// ── type text... ──
			if (cmd == "type")
			{
				if (tokens.size() < 2) { std::cerr << "type: expected text\n"; return -1; }

				// Everything after "type " is the text to send
				auto text_start = line.find_first_not_of(" \t", 4);
				if (text_start == std::string::npos) { std::cerr << "type: expected text\n"; return -1; }
				auto text = line.substr(text_start);

				for (auto ch : text)
				{
					INPUT inputs[2] = {};
					inputs[0].type = INPUT_KEYBOARD;
					inputs[0].ki.wScan = static_cast<WORD>(ch);
					inputs[0].ki.dwFlags = KEYEVENTF_UNICODE;
					inputs[1].type = INPUT_KEYBOARD;
					inputs[1].ki.wScan = static_cast<WORD>(ch);
					inputs[1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
					::SendInput(2, inputs, sizeof(INPUT));
					Sleep(10);
				}
				return 0;
			}

			// ── key combo ──
			if (cmd == "key")
			{
				if (tokens.size() < 2) { std::cerr << "key: expected combo\n"; return -1; }
				auto combo = tokens[1];

				// Split on '+' to get modifier+key parts
				std::vector<std::string> parts;
				std::istringstream ss(combo);
				std::string part;
				while (std::getline(ss, part, '+'))
					parts.push_back(part);

				// Resolve all virtual keys
				std::vector<WORD> vkeys;
				for (auto& p : parts)
				{
					auto vk = ResolveVKey(p);
					if (vk == 0) { std::cerr << std::format("key: unknown key '{}'\n", p); return -1; }
					vkeys.push_back(vk);
				}

				// Press all keys down, then release in reverse order
				for (auto vk : vkeys)
				{
					INPUT inp = {};
					inp.type = INPUT_KEYBOARD;
					inp.ki.wVk = vk;
					::SendInput(1, &inp, sizeof(INPUT));
				}
				for (auto it = vkeys.rbegin(); it != vkeys.rend(); ++it)
				{
					INPUT inp = {};
					inp.type = INPUT_KEYBOARD;
					inp.ki.wVk = *it;
					inp.ki.dwFlags = KEYEVENTF_KEYUP;
					::SendInput(1, &inp, sizeof(INPUT));
				}
				Sleep(30);
				return 0;
			}

			// ── delay ms ──
			if (cmd == "delay")
			{
				if (tokens.size() < 2) { std::cerr << "delay: expected milliseconds\n"; return -1; }
				int ms;
				if (!ParseNum(tokens[1], ms)) { std::cerr << std::format("delay: invalid value '{}'\n", tokens[1]); return -1; }
				Sleep(static_cast<DWORD>(ms));
				return 0;
			}

			std::cerr << std::format("Unknown command: '{}'\n", cmd);
			return -1;
		}
	};

	int Automate(pr::CmdLine const& args)
	{
		Cmd_Automate cmd;
		return cmd.Run(args);
	}
}
