//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct Input :ICex
	{
		std::string m_message; // Message to display before reading input
		std::string m_env_var; // Name of the environment variable to set

		void ShowHelp() const override
		{
			std::cout <<
				"Input : Read user input into an environment variable\n"
				" Syntax: Cex -input environment_variable_name [-msg \"Message\"]\n";
		}

		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg , pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-input") && arg != arg_end) { m_env_var = *arg++; return true; }
			if (pr::str::EqualI(option, "-msg"  ) && arg != arg_end) { m_message = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		int Run() override
		{
			if (!m_message.empty()) { std::cout << m_message; }
			std::string value;
			std::getline(std::cin, value);
			SetEnvVar(m_env_var, value);
			return 0;
		}
	};
}
