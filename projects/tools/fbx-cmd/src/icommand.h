#pragma once
#include "src/forward.h"

namespace fbx_cmd
{
	// Base class for fbx-cmd
	struct ICommand :pr::cmdline::IOptionReceiver<>
	{
		~ICommand() = default;

		// A title banner for cex.exe
		static char const* Title()
		{
			return "\n"
				"-------------------------------------------------------------\n"
				"  FBX Tools\n" 
				"   Copyright (c) Rylogic 2025 \n"
				"   Version: v1.0\n"
				"-------------------------------------------------------------\n"
				"\n";
		}

		// Command line callbacks
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			ShowConsole();

			if (pr::str::EqualI(option, "/?") ||
				pr::str::EqualI(option, "-h") ||
				pr::str::EqualI(option, "-help"))
			{
				ShowHelp();
				arg = arg_end;
				return true;
			}

			std::cerr << "Error: Unknown  option '" << option << "' or incomplete parameters provided\nSee help for syntax information\n";
			return false;
		}
		bool CmdLineData(TArgIter& arg, TArgIter) override
		{
			ShowConsole();

			std::cerr << "Error: Unknown option '" << *arg << "'\n";
			return false;
		}

		// Called after command line parsing, allows derived types to set default params
		virtual void ValidateInput()
		{}

		// Show the console for this process
		void ShowConsole()
		{
			// Attach to the current console
			if (AttachConsole((DWORD)-1) || AllocConsole())
			{
				// Redirect the CRT standard input, output, and error handles to the console
				freopen("CONIN$", "r", stdin);
				freopen("CONOUT$", "w", stdout);
				freopen("CONOUT$", "w", stderr);

				// Clear the error state for each of the C++ standard stream objects. We need to do this, as
				// attempts to access the standard streams before they refer to a valid target will cause the
				// 'iostream' objects to enter an error state. In versions of Visual Studio after 2005, this seems
				// to always occur during startup regardless of whether anything has been read from or written to
				// the console or not.
				std::wcout.clear();
				std::cout.clear();
				std::wcerr.clear();
				std::cerr.clear();
				std::wcin.clear();
				std::cin.clear();
			}
		}

		// Show command help
		virtual void ShowHelp() const = 0;

		// Execute the command
		virtual int Run() = 0;
	};
}
