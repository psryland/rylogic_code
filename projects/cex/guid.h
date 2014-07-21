//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct Guid :ICex
	{
		void ShowHelp() const override
		{
			std::cout <<
				"Generate a new GUID\n"
				" Syntax: Cex -guid\n";
		}

		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-guid")) { return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			return ICex::CmdLineData(arg, arg_end);
		}

		int Run() override
		{
			std::cout << pr::To<std::string>(pr::GenerateGUID());
			return 0;
		}
	};
}
