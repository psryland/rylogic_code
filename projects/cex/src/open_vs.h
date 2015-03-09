//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct OpenVS :ICex
	{
		std::string  m_file;   // File to open
		unsigned int m_line;   // Line number to go to

		OpenVS();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override;
		int Run() override;
	};
}
