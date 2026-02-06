#pragma once
#include "src/forward.h"

namespace gltf_cmd
{
	// Base class for gltf-cmd
	struct ICommand :pr::cmdline::IOptionReceiver<>
	{
		~ICommand() = default;

		// A title banner for gltf-cmd
		static char const* Title()
		{
			return "\n"
				"-------------------------------------------------------------\n"
				"  glTF Tools\n" 
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

			std::cerr << "Error: Unknown option '" << option << "' or incomplete parameters provided\nSee help for syntax information\n";
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
				freopen("CONIN$", "r", stdin);
				freopen("CONOUT$", "w", stdout);
				freopen("CONOUT$", "w", stderr);

				std::wcout.clear();
				std::cout.clear();
				std::wcerr.clear();
				std::cerr.clear();
				std::wcin.clear();
				std::cin.clear();
			}
		}

		// Show command help
		virtual void ShowHelp() const {}

		// Execute the command
		virtual int Run() { return -1; }
	};
}
