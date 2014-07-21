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
	struct ICex
		:pr::cmdline::IOptionReceiver
	{
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "/?") ||
				pr::str::EqualI(option, "-h") ||
				pr::str::EqualI(option, "-help"))
			{
				ShowHelp();
				arg = arg_end;
				return true;
			}

			std::cerr << "Error: Unknown option '" << option << "'\n";
			return false;
		}
		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter) override
		{
			std::cerr << "Error: Unknown option '" << *arg << "'\n";
			return false;
		}

		virtual void ShowHelp() const = 0;
		virtual int Run() = 0;
	};
}
