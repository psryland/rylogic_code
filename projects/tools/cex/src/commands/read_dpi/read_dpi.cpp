//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// ReadDpi: Report the DPI scaling for a monitor.
#include "src/forward.h"
#include <shellscalingapi.h>
#pragma comment(lib, "Shcore.lib")

namespace cex
{
	struct Cmd_ReadDpi
	{
		void ShowHelp() const
		{
			std::cout <<
				"ReadDpi: Report the DPI scaling for a monitor\n"
				" Syntax: Cex -read_dpi [-monitor <index>]\n"
				"  -monitor : Zero-based monitor index (default: primary monitor)\n"
				"\n"
				"  Outputs: dpi_x dpi_y scale_percent\n"
				"  Example output: 144 144 150\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			// Optional monitor index
			int monitor_index = -1; // -1 means primary
			if (args.count("monitor") != 0)
				monitor_index = args("monitor").as<int>();

			// Enumerate monitors to find the target
			struct EnumData
			{
				int target_index;
				int current_index;
				HMONITOR hmonitor;
			};
			EnumData data = { monitor_index, 0, nullptr };

			if (monitor_index < 0)
			{
				// Use the primary monitor
				POINT pt = { 0, 0 };
				data.hmonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTOPRIMARY);
			}
			else
			{
				// Enumerate monitors to find the one at the given index
				EnumDisplayMonitors(nullptr, nullptr, [](HMONITOR hmon, HDC, LPRECT, LPARAM lparam) -> BOOL
				{
					auto& d = *reinterpret_cast<EnumData*>(lparam);
					if (d.current_index == d.target_index)
					{
						d.hmonitor = hmon;
						return FALSE; // Stop enumerating
					}
					d.current_index++;
					return TRUE;
				}, reinterpret_cast<LPARAM>(&data));
			}

			if (data.hmonitor == nullptr)
			{
				std::cerr << std::format("Monitor index {} not found\n", monitor_index);
				return -1;
			}

			// Query the effective DPI for this monitor
			UINT dpi_x = 0, dpi_y = 0;
			auto hr = GetDpiForMonitor(data.hmonitor, MDT_EFFECTIVE_DPI, &dpi_x, &dpi_y);
			if (FAILED(hr))
			{
				std::cerr << std::format("Failed to get DPI for monitor (HRESULT: {:#010x})\n", static_cast<unsigned>(hr));
				return -1;
			}

			auto scale_percent = static_cast<int>(dpi_x * 100 / 96);
			std::cout << std::format("{} {} {}\n", dpi_x, dpi_y, scale_percent);
			return 0;
		}
	};

	int ReadDpi(pr::CmdLine const& args)
	{
		Cmd_ReadDpi cmd;
		return cmd.Run(args);
	}
}
