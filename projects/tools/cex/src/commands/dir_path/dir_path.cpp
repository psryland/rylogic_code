//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"

namespace cex
{
	struct Cmd_DirPath
	{
		void ShowHelp() const
		{
			std::cout <<
				"DirPath : Open a dialog window for finding a path.\n"
				"          Path name is stored into an environment variable\n"
				" Syntax: Cex -dirpath environment_variable_name [-msg \"Message\"]\n";
		}
		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::string env_var; // Name of the environment variable to set
			if (args.count("dirpath") != 0)
			{
				env_var = args("dirpath").as<std::string>();
			}
			std::string message; // Message to display before reading input
			if (args.count("msg") != 0)
			{
				message = args("msg").as<std::string>();
			}

			char display_name[MAX_PATH];
			BROWSEINFOA browse_info = {
				.hwndOwner = GetConsoleWindow(),
				.pidlRoot = 0,
				.pszDisplayName = display_name,
				.lpszTitle = message.c_str(),
				.ulFlags = BIF_NEWDIALOGSTYLE | BIF_RETURNONLYFSDIRS,
				.lpfn = 0,
				.lParam = 0,
				.iImage = 0,
			};
			LPITEMIDLIST p = SHBrowseForFolderA(&browse_info);

			std::string dir_path(MAX_PATH, 0);
			SHGetPathFromIDListA(p, &dir_path[0]);

			SetEnvVar(env_var, dir_path);
			return 0;
		}
	};

	int DirPath(pr::CmdLine const& args)
	{
		Cmd_DirPath cmd;
		return cmd.Run(args);
	}
}
