//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"

namespace cex
{
	struct Cmd_Wait
	{
		unsigned int m_seconds; // Time to wait in seconds
		std::string  m_message; // Message to display while waiting
		
		Cmd_Wait()
			:m_seconds(1)
			,m_message()
		{}

		void ShowHelp() const
		{
			std::cout <<
				"Wait: Wait for a specified length of time\n"
				" Syntax: Cex -wait 5 -msg \"Message to display\"\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			m_seconds = args("wait").as<unsigned int>();
			if (args.count("msg") != 0)
			{
				for (auto const& msg : args("msg").values)
					std::cout << msg << " ";
			}
			std::cout << "\n(Waiting " << m_seconds << " seconds)\n";

			Sleep(m_seconds * 1000);
			return 0;
		}
	};

	int Wait(pr::CmdLine const& args)
	{
		Cmd_Wait cmd;
		return cmd.Run(args);
	}
}
