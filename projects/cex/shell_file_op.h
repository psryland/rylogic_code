//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct ShFileOp :ICex
	{
		SHFILEOPSTRUCTA m_fo;
		std::string m_src, m_dst, m_title;

		ShFileOp();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override;
		int Run() override;
	};
}
