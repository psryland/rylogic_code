//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/icex.h"

namespace cex
{
	struct NEW_COMMAND :ICex
	{
		NEW_COMMAND() {}

		void ShowHelp() const override
		{
			std::cout <<
				"";
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		bool CmdLineData(TArgIter& arg, TArgIter arg_end) override
		{
			return ICex::CmdLineData(arg, arg_end);
		}

		int Run() override
		{
			return 0;
		}
	};
}
