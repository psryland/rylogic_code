//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct ShFileOp :ICex
	{
		SHFILEOPSTRUCTA m_fo;
		std::string m_src, m_dst, m_title;

		ShFileOp();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override;
		int Run() override;
	};
}
