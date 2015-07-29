//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

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

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-hash")) { return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		
		bool CmdLineData(TArgIter& arg, TArgIter) override
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
