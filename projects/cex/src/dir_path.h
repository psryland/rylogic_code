//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"

namespace cex
{
	struct DirPath :ICex
	{
		std::string m_message; // Message to display before reading input
		std::string m_env_var; // Name of the environment variable to set

		void ShowHelp() const override
		{
			std::cout <<
				"DirPath : Open a dialog window for finding a path.\n"
				"          Path name is stored into an environment variable\n"
				" Syntax: Cex -dirpath environment_variable_name [-msg \"Message\"]\n";
		}

		bool CmdLineOption(std::string const& option, TArgIter& arg , TArgIter arg_end) override
		{
			if (pr::str::EqualI(option, "-dirpath") && arg != arg_end) { m_env_var = *arg++; return true; }
			if (pr::str::EqualI(option, "-msg"    ) && arg != arg_end) { m_message = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}

		int Run() override
		{
			char display_name[MAX_PATH];
			BROWSEINFOA browse_info;
			browse_info.hwndOwner = GetConsoleWindow();
			browse_info.pidlRoot = 0;
			browse_info.pszDisplayName = display_name;
			browse_info.lpszTitle = m_message.c_str();
			browse_info.ulFlags = BIF_NEWDIALOGSTYLE | BIF_RETURNONLYFSDIRS;
			browse_info.lpfn = 0;
			browse_info.lParam = 0;
			browse_info.iImage = 0;
			LPITEMIDLIST p = SHBrowseForFolderA(&browse_info);

			std::string dir_path(MAX_PATH, 0);
			SHGetPathFromIDListA(p, &dir_path[0]);

			SetEnvVar(m_env_var, dir_path);
			return 0;
		}
	};
}
