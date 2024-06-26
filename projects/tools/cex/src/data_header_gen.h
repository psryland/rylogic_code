//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/icex.h"

namespace cex
{
	struct HData :ICex
	{
		std::filesystem::path m_src;
		std::filesystem::path m_dst;
		bool m_binary;
		bool m_verbose;

		HData();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override;
		bool CmdLineData(TArgIter& arg, TArgIter arg_end) override;
		int Run() override;
	};
}
