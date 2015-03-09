//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct P3d :ICex
	{
		struct Impl;
		std::shared_ptr<Impl> m_ptr;

		P3d();
		void ShowHelp() const override;
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override;
		int Run() override;
	};
}
