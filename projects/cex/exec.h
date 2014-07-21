//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"

namespace cex
{
	struct Exec :ICex
	{
		std::string m_process;
		std::string m_args;
		std::string m_startdir;
		bool m_async;

		Exec()
			:m_process()
			,m_args()
			,m_startdir()
			,m_async(false)
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"Exec: execute another process\n"
				" Syntax: Cex -exec -p exe_path args [-async] [-startdir start_path]\n"
				" -p exe_path args : run the process given by the following path and\n"
				"     arguments. The first parameters after the -p is the path to the\n"
				"     exe, any further parameters up to the next option or end of the\n"
				"     argument list are treated as arguments for 'exe_path'.\n"
				" -async : Optional parameter that if set causes Cex to return immediately\n"
				"     By default, Cex will block until the process has completed.\n"
				" -startdir start_path : sets the starting path for the process.\n"
				"     By default this is the current directory\n";
		}

		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-exec"    ) && arg != arg_end) { m_args.append(!m_args.empty() ? " " : "").append(*arg++); return true; }
			if (pr::str::EqualI(option, "-p"       ) && arg != arg_end) { m_process = *arg++; return true; }
			if (pr::str::EqualI(option, "-async"   ))                   { m_async = true; return true; }
			if (pr::str::EqualI(option, "-startdir") && arg != arg_end) { m_startdir = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		int Run() override
		{
			if (!m_process.empty())
			{
				pr::Process proc;
				proc.Start(m_process.c_str(), m_args.empty() ? 0 : m_args.c_str(), m_startdir.empty() ? 0 : m_startdir.c_str());
				return proc.BlockTillExit();
			}
			return -1;
		}
	};
}
