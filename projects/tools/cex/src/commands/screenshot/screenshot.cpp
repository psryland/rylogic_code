//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/commands/process_util.h"
#include "pr/gui/gdiplus.h"

#include <map>

namespace cex
{
	struct Cmd_Screenshot
	{
		void ShowHelp() const
		{
			std::cout <<
				"Screenshot: Capture windows of a process\n"
				" Syntax: Cex -screenshot -p <process-name> -o <output-directory> [-all] [-bitblt] [-scale N]\n"
				"  -p      : Name (or partial name) of the process to capture\n"
				"  -o      : Output directory for captured PNG images\n"
				"  -all    : Also capture hidden/minimised windows\n"
				"  -bitblt : Capture from the screen DC instead of using PrintWindow.\n"
				"            Works for GPU-rendered apps (e.g. Electron/Chromium) but\n"
				"            requires the window to be visible and in the foreground.\n"
				"  -scale  : Scale factor for the output image (e.g. 0.25 for quarter size)\n"
				"\n"
				"  Output files are named <process-name>.<window-title>.png\n"
				"  Duplicate names are suffixed with -1, -2, etc.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			// Process name to search for
			std::string process_name;
			if (args.count("p") != 0)
			{
				process_name = args("p").as<std::string>();
			}

			// Output directory for screenshots
			std::filesystem::path outdir;
			if (args.count("o") != 0)
			{
				outdir = args("o").as<std::string>();
			}

			if (process_name.empty()) { std::cerr << "No process name provided (-p)\n"; return ShowHelp(), -1; }
			if (outdir.empty())       { std::cerr << "No output directory provided (-o)\n"; return ShowHelp(), -1; }

			auto include_hidden = args.count("all") != 0;
			auto use_bitblt     = args.count("bitblt") != 0;
			auto scale          = args.count("scale") != 0 ? args("scale").as<double>() : 1.0;

			// Ensure the output directory exists
			std::filesystem::create_directories(outdir);

			// Find all process IDs matching the name
			auto pids = FindProcesses(process_name);
			if (pids.empty())
			{
				std::cerr << std::format("No running process found matching '{}'\n", process_name);
				return -1;
			}

			// Enumerate windows for these processes
			auto windows = FindWindows(pids, include_hidden);
			if (windows.empty())
			{
				std::cerr << std::format("No {} windows found for '{}'\n", include_hidden ? "capturable" : "visible", process_name);
				return -1;
			}

			// Initialise GDI+ for PNG encoding
			pr::GdiPlus gdiplus;

			// Capture each window
			std::map<std::string, int> name_counts;
			int captured = 0;
			for (auto hwnd : windows)
			{
				auto title = GetWindowTitle(hwnd);
				auto safe_title = SanitiseFilename(title.empty() ? "untitled" : title);
				auto safe_pname = SanitiseFilename(process_name);

				// Build the base name and handle duplicates
				auto base = std::format("{}.{}", safe_pname, safe_title);
				auto& count = name_counts[base];
				auto filename = count == 0
					? std::format("{}.png", base)
					: std::format("{}-{}.png", base, count);
				++count;

				auto filepath = outdir / filename;
				if (CaptureWindow(hwnd, filepath, use_bitblt, scale))
				{
					std::cout << std::format("Captured: {}\n", filename);
					++captured;
				}
				else
				{
					std::cerr << std::format("Failed to capture: {}\n", filename);
				}
			}

			std::cout << std::format("{} window(s) captured\n", captured);
			return captured > 0 ? 0 : -1;
		}

	private:

		// Replace characters that are invalid in filenames
		static std::string SanitiseFilename(std::string name)
		{
			for (auto& ch : name)
			{
				switch (ch)
				{
					case '<': case '>': case ':': case '"':
					case '/': case '\\': case '|': case '?': case '*':
						ch = '_';
						break;
				}
			}

			// Trim trailing dots and spaces (invalid on Windows)
			while (!name.empty() && (name.back() == '.' || name.back() == ' '))
				name.pop_back();

			return name.empty() ? std::string("_") : name;
		}

		// Capture a window to a PNG file
		static bool CaptureWindow(HWND hwnd, std::filesystem::path const& filepath, bool use_bitblt, double scale)
		{
			RECT rc;
			if (!GetWindowRect(hwnd, &rc))
				return false;

			auto w = rc.right  - rc.left;
			auto h = rc.bottom - rc.top;
			if (w <= 0 || h <= 0)
				return false;

			// Create a memory DC and bitmap for the full-size capture
			auto hdc_screen = GetDC(nullptr);
			auto hdc_mem = CreateCompatibleDC(hdc_screen);
			auto hbm = CreateCompatibleBitmap(hdc_screen, w, h);
			auto old_bm = SelectObject(hdc_mem, hbm);

			bool captured = false;
			if (use_bitblt)
			{
				// BitBlt from the screen DC. Captures the composited output from DWM,
				// which works for GPU-rendered apps (Electron, Chromium, etc.). Requires
				// the window to be visible, unoccluded, and in the foreground.
				captured = BitBlt(hdc_mem, 0, 0, w, h, hdc_screen, rc.left, rc.top, SRCCOPY) != 0;
			}
			else
			{
				// Use PrintWindow to capture the window content (works even if partially occluded)
				captured = PrintWindow(hwnd, hdc_mem, PW_RENDERFULLCONTENT) != 0;
				if (!captured)
				{
					// Fallback to BitBlt from the window DC
					auto hdc_wnd = GetDC(hwnd);
					captured = BitBlt(hdc_mem, 0, 0, w, h, hdc_wnd, 0, 0, SRCCOPY) != 0;
					ReleaseDC(hwnd, hdc_wnd);
				}
			}

			bool saved = false;
			if (captured)
			{
				// Scale the image if needed
				if (scale > 0.0 && scale != 1.0)
				{
					auto sw = std::max(1, static_cast<int>(w * scale));
					auto sh = std::max(1, static_cast<int>(h * scale));

					auto hdc_scaled = CreateCompatibleDC(hdc_screen);
					auto hbm_scaled = CreateCompatibleBitmap(hdc_screen, sw, sh);
					auto old_scaled = SelectObject(hdc_scaled, hbm_scaled);

					SetStretchBltMode(hdc_scaled, HALFTONE);
					SetBrushOrgEx(hdc_scaled, 0, 0, nullptr);
					StretchBlt(hdc_scaled, 0, 0, sw, sh, hdc_mem, 0, 0, w, h, SRCCOPY);

					Gdiplus::Bitmap bmp(hbm_scaled, nullptr);
					saved = Gdiplus::Save(bmp, filepath) == Gdiplus::Status::Ok;

					SelectObject(hdc_scaled, old_scaled);
					DeleteObject(hbm_scaled);
					DeleteDC(hdc_scaled);
				}
				else
				{
					Gdiplus::Bitmap bmp(hbm, nullptr);
					saved = Gdiplus::Save(bmp, filepath) == Gdiplus::Status::Ok;
				}
			}

			// Clean up GDI resources
			SelectObject(hdc_mem, old_bm);
			DeleteObject(hbm);
			DeleteDC(hdc_mem);
			ReleaseDC(nullptr, hdc_screen);

			return saved;
		}
	};

	int Screenshot(pr::CmdLine const& args)
	{
		Cmd_Screenshot cmd;
		return cmd.Run(args);
	}
}
