//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct HData :ICex
	{
		std::string m_src;
		std::string m_dst;
		bool m_binary;
		bool m_verbose;

		HData();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override;
		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override;
		int Run() override;
	};
}
