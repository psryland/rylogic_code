//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

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

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-guid")) { return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		bool CmdLineData(TArgIter& arg, TArgIter arg_end) override
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
