//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#pragma once

#include <iostream>
#include <string>
#include "pr/common/command_line.h"
#include "pr/str/prstring.h"

namespace cex
{
	// Base class for ICex commands
	struct ICex :pr::cmdline::IOptionReceiver
	{
		static char const* Title()
		{
			return "\n"
				"-------------------------------------------------------------\n"
				"  Commandline Extensions \n" 
				"   Copyright (c) Rylogic 2004 \n"
				"   Version: v1.2\n"
				"-------------------------------------------------------------\n"
				"\n";
		}

		// Command line callbacks
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
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
		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter) override
		{
			ShowConsole();

			std::cerr << "Error: Unknown option '" << *arg << "'\n";
			return false;
		}

		// Called after command line parsing, allows derived types to set default params
		virtual void ValidatedInput()
		{}

		// Show the console for this process
		void ShowConsole() const
		{
			// Attach to the current console
			if (AttachConsole((DWORD)-1) || AllocConsole())
			{
				FILE *fp;
				int h;

				// redirect unbuffered STDOUT to the console
				h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_OUTPUT_HANDLE)), _O_TEXT);
				fp = _fdopen(h, "w");
				*stdout = *fp;
				setvbuf(stdout, NULL, _IONBF, 0);

				// redirect unbuffered STDIN to the console
				h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_INPUT_HANDLE )), _O_TEXT);
				fp = _fdopen(h, "r");
				*stdin = *fp;
				setvbuf(stdin, NULL, _IONBF, 0);

				// redirect unbuffered STDERR to the console
				h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_ERROR_HANDLE )), _O_TEXT);
				fp = _fdopen(h, "w");
				*stderr = *fp;
				setvbuf(stderr, NULL, _IONBF, 0);

				// make cout, wcout, cin, wcin, wcerr, cerr, wclog and clog
				// point to console as well
				std::ios::sync_with_stdio();
			}
		}

		// Show command help
		virtual void ShowHelp() const = 0;

		// Execute the command
		virtual int Run() = 0;
	};
}
