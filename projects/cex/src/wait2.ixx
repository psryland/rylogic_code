//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"
export module wait;

namespace cex
{
	export struct Wait2 :ICex
	{
		unsigned int m_seconds; // Time to wait in seconds
		std::string m_message; // Message to display while waiting
		
		Wait2()
			:m_seconds(1)
			,m_message()
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"Wait: Wait for a specified length of time\n"
				" Syntax: Cex -wait 5 -msg \"Message to display\"\n";
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-wait") && arg != arg_end) { return pr::str::ExtractIntC(m_seconds, 10, (*arg++).c_str()); }
			if (pr::str::EqualI(option, "-msg" ) && arg != arg_end) { m_message = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		int Run() override
		{
			if (!m_message.empty())
				std::cout << m_message << "\n(Waiting " << m_seconds << " seconds)\n";

			Sleep(m_seconds * 1000);
			return 0;
		}
	};
}
