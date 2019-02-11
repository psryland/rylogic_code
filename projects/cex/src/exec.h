//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct Exec :ICex
	{
		std::wstring m_process;
		std::wstring m_args;
		std::wstring m_working_dir;
		bool m_async;

		Exec()
			:m_process()
			,m_args()
			,m_working_dir()
			,m_async(false)
		{}

		void ShowHelp() const override
		{
			std::cout <<
				"Exec: execute another process\n"
				" Syntax: Cex -exec [-async] [-cwd working_dir] -p exe_path args ... \n"
				" -p exe_path args : run the process given by the following path and\n"
				"     arguments. The first parameter after the -p is the executable path,\n"
				"     any further parameters up to the end of the argument list are treated\n"
				"     as arguments for 'exe_path'.\n"
				" -async : Optional parameter that causes Cex to return immediately\n"
				"     By default, Cex will block until the process has completed.\n"
				" -cwd working_dir : sets the working directory for the process.\n"
				"     By default this is the current directory\n"
				;
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-exec")) { return true; }
			if (pr::str::EqualI(option, "-cwd") && arg != arg_end)
			{
				m_working_dir = pr::Widen(*arg++);
				return true;
			}
			if (pr::str::EqualI(option, "-async"))
			{
				m_async = true;
				return true;
			}
			if (pr::str::EqualI(option, "-p") && arg != arg_end)
			{
				m_process = pr::Widen(*arg++);
				for (; arg != arg_end; ++arg)
				{
					if (!m_args.empty()) m_args.push_back(' ');
					m_args.append(pr::Widen(*arg));
				}
				return true;
			}
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		int Run() override
		{
			if (!m_process.empty())
			{
				// Start the child process
				pr::Process proc;
				auto args = m_args.empty() ? nullptr : m_args.c_str();
				auto cwd = m_working_dir.empty() ? nullptr : m_working_dir.c_str();
				proc.Start(m_process.c_str(), args, cwd);

				// Return immediately if async, otherwise block and return the exit code
				return m_async ? 0 : proc.BlockTillExit();
			}
			return -1;
		}
	};
}
