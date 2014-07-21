//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct Hash :ICex
	{
		std::string m_text;

		void ShowHelp() const override
		{
			std::cout <<
				"Hash the given stdin data\n"
				" Syntax: Cex -hash data_to_hash...\n"
				;
		}

		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-hash")) { return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		
		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter) override
		{
			m_text += *arg++;
			return true;
		}

		int Run() override
		{
			printf("%08X", pr::hash::HashC(m_text.c_str()));
			return 0;
		}
	};
}
