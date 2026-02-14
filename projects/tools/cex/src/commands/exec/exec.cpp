//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "pr/str/string_util.h"

namespace cex
{
	struct Cmd_Exec
	{
		void ShowHelp() const
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
		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::wstring process;
			std::wstring params;
			if (args.count("p") != 0)
			{
				auto const& arg = args("p");
				process = pr::Widen(arg.as<std::string>());
				for (int i = 1; i < arg.num_values(); ++i)
				{
					if (!params.empty()) params.push_back(' ');
					params.append(pr::Widen(arg.as<std::string>(i)));
				}
			}
			std::wstring working_dir;
			if (args.count("cwd") != 0)
			{
				working_dir = pr::Widen(args("cwd").as<std::string>());
			}
			bool m_async = false;
			if (args.count("async") != 0)
			{
				m_async = true;
			}
		
			if (process.empty())
				return -1;

			// Start the child process
			pr::Process proc;
			proc.Start(
				process.c_str(),
				!params.empty() ? params.c_str() : nullptr,
				!working_dir.empty() ? working_dir.c_str() : nullptr
			);

			// Return immediately if async, otherwise block and return the exit code
			return m_async ? 0 : proc.BlockTillExit();
		}
	};

	int Exec(pr::CmdLine const& args)
	{
		Cmd_Exec cmd;
		return cmd.Run(args);
	}
}
