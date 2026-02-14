//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "pr/common/guid.h"

namespace cex
{
	struct Cmd_Guid
	{
		void ShowHelp() const
		{
			std::cout <<
				"Generate a new GUID\n"
				" Syntax: Cex -guid\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::cout << pr::To<std::string>(pr::GenerateGUID());
			return 0;
		}
	};

	int Guid(pr::CmdLine const& args)
	{
		Cmd_Guid cmd;
		return cmd.Run(args);
	}
}
