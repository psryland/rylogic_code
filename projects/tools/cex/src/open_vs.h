//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/icex.h"

namespace cex
{
	struct OpenVS :ICex
	{
		std::string  m_file;   // File to open
		unsigned int m_line;   // Line number to go to

		OpenVS();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override;
		int Run() override;
	};
}
