//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"

namespace cex
{
	struct Cmd_Lower
	{
		std::string m_str;

		Cmd_Lower()
			:m_str()
		{}

		void ShowHelp() const
		{
			std::cout <<
				"ToLower: Convert a string to lower case\n"
				" Syntax: Cex -lwr \"Message to lower\"\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			for (auto const& text : args("lwr").values)
			{
				if (!m_str.empty()) m_str.append(" ");
				m_str.append(text);
			}

			std::transform(m_str.begin(), m_str.end(), m_str.begin(), [](char ch) { return static_cast<char>(tolower(ch)); });
			if (!m_str.empty()) std::cout << m_str;
			return 0;
		}
	};

	int Lower(pr::CmdLine const& args)
	{
		Cmd_Lower cmd;
		return cmd.Run(args);
	}
}
