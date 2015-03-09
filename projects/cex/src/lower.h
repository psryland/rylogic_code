//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct ToLower :ICex
	{
		std::string m_str; // string to lower

		ToLower()
			:m_str()
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"ToLower: Convert a string to lower case\n"
				" Syntax: Cex -lwr \"Message to lower\"\n";
		}
		
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-lwr") && arg != arg_end) { m_str.append(!m_str.empty() ? " " : "").append(*arg++); return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		
		int Run() override
		{
			std::transform(m_str.begin(), m_str.end(), m_str.begin(), tolower);
			if (!m_str.empty()) std::cout << m_str;
			return 0;
		}
	};
}
