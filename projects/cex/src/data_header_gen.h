//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

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
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override;
		bool CmdLineData(TArgIter& arg, TArgIter arg_end) override;
		int Run() override;
	};
}
