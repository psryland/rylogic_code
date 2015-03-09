//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"
#include "cex/src/shell_file_op.h"

namespace cex
{
	ShFileOp::ShFileOp()
		:m_fo()
		,m_src()
		,m_dst()
	{}

	void ShFileOp::ShowHelp() const
	{
		std::cout <<
R"(Shell File Operation : Perform a file operation using the windows explorer shell
 Syntax: Cex -shcopy|-shmove|-shrename|-shdelete [options]
  -shcopy   src_file1,src_file2,... dst_path0,dst_path1,... [-flags flag0,flag1] [-title title]
  -shmove   src_file1,src_file2,... dst_path0,dst_path1,... [-flags flag0,flag1] [-title title]
  -shrename src_file1,src_file2,... dst_path0,dst_path1,... [-flags flag0,flag1] [-title title]
  -shdelete src_file1,src_file2,... [-flags flag0,flag1] [-title title]
     src_files : Standard MS-DOS wildcard characters, such as '*', are permitted
                 only in the file-name position. Using a wildcard character elsewhere
                 in the string will lead to unpredictable results.
     dst_path  : Wildcard characters are not supported.
                 Copy and Move operations can specify destination directories that do
                 not exist. In those cases, the system attempts to create them and normally
                 displays a dialog box to ask the user if they want to create the new directory.
                 To suppress this dialog box and have the directories created silently, set the
                 NoConfirmMkDir flag in -flags.
                 For Copy and Move operations, the buffer can contain multiple destination file
                 names if the -flags member specifies MultiDestFiles.
     flags     : AllowUndo - Preserve undo information, if possible.
                 FilesOnly - Perform the operation only on files (not on folders) if a wildcard
                             file name (*.*) is specified.
                 MultiDestFiles - The dst_path list specifies multiple destination files (one for
                             each source file in src_files) rather than one directory where
                             all source files are to be deposited.
                 NoConfirmation - Respond with Yes to All for any dialog box that is displayed.
                 NoConfirmMkDir - Do not ask the user to confirm the creation of a new directory
                             if the operation requires one to be created.
                 NoConnectedElements - WinVer 5.0. Do not move connected files as a group.
                             Only move the specified files.
                 NoCopySecurityAttribs - WinVer 4.71. Do not copy the security attributes of the file.
                             The destination file receives the security attributes of its new folder.
                 NoErrorUI - Do not display a dialog to the user if an error occurs.
                 NoRecursion - Only perform the operation in the local directory. Do not operate
                             recursively into subdirectories, which is the default behavior.
                 NoUI - WinVer 6.0.6060 (Windows Vista). Perform the operation silently, presenting
                             no UI to the user. This is equivalent to Silent,NoConfirmation,NoErrorUI,NoConfirmMkDir.
                 RenameOnCollision - Give the file being operated on a new name in a move, copy, or rename
                             operation if a file with the target name already exists at the destination.
                 Silent - Do not display a progress dialog box.
                 SimpleProgress - Display a progress dialog box but do not show individual file names
                             as they are operated on.
                 WantNukeWarning - WinVer 5.0. Send a warning if a file is being permanently destroyed
                             during a delete operation rather than recycled. This flag partially overrides NoConfirmation.
     title     : A title to display on progress dialogs
)";
	}

	bool ShFileOp::CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		struct Read
		{
			static char* Paths(std::string const& arg, std::string& var)
			{
				var.clear();
				std::vector<std::string> paths;
				pr::str::Split(arg, ",", [&](std::string const& arg, size_t i, size_t iend){ paths.push_back(arg.substr(i, iend-i)); });
				for (std::vector<std::string>::const_iterator i = paths.begin(), iend = paths.end(); i != iend; ++i) var.append(pr::filesys::GetFullPath(*i)).push_back('\0');
				var.push_back('\0');
				return &var[0];
			}
			static void Flags(std::string const& arg, FILEOP_FLAGS& var)
			{
				var = 0;
				std::vector<std::string> flags;
				pr::str::Split(arg, ",", [&](std::string const& arg, size_t i, size_t iend){ flags.push_back(arg.substr(i,iend-i)); });
				for (std::vector<std::string>::const_iterator i = flags.begin(), iend = flags.end(); i != iend; ++i)
				{
					if (pr::str::EqualI(*i, "AllowUndo"             )) { var |= FOF_ALLOWUNDO;             continue; }
					if (pr::str::EqualI(*i, "FilesOnly"             )) { var |= FOF_FILESONLY;             continue; }
					if (pr::str::EqualI(*i, "MultiDestFiles"        )) { var |= FOF_MULTIDESTFILES;        continue; }
					if (pr::str::EqualI(*i, "NoConfirmation"        )) { var |= FOF_NOCONFIRMATION;        continue; }
					if (pr::str::EqualI(*i, "NoConfirmMkDir"        )) { var |= FOF_NOCONFIRMMKDIR;        continue; }
					if (pr::str::EqualI(*i, "NoConnectedElements"   )) { var |= FOF_NO_CONNECTED_ELEMENTS; continue; }
					if (pr::str::EqualI(*i, "NoCopySecurityAttribs" )) { var |= FOF_NOCOPYSECURITYATTRIBS; continue; }
					if (pr::str::EqualI(*i, "NoErrorUI"             )) { var |= FOF_NOERRORUI;             continue; }
					if (pr::str::EqualI(*i, "NoRecursion"           )) { var |= FOF_NORECURSION;           continue; }
					if (pr::str::EqualI(*i, "NoUI"                  )) { var |= FOF_NO_UI;                 continue; }
					if (pr::str::EqualI(*i, "RenameOnCollision"     )) { var |= FOF_RENAMEONCOLLISION;     continue; }
					if (pr::str::EqualI(*i, "Silent"                )) { var |= FOF_SILENT;                continue; }
					if (pr::str::EqualI(*i, "SimpleProgress"        )) { var |= FOF_SIMPLEPROGRESS;        continue; }
					if (pr::str::EqualI(*i, "WantNukeWarning"       )) { var |= FOF_WANTNUKEWARNING;       continue; }
				}
			}
		};

		if (pr::str::EqualI(option, "-shcopy"  ) && arg_end - arg >= 2) { m_fo.wFunc = FO_COPY;   m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = Read::Paths(*(arg+1), m_dst); arg += 2; return true; }
		if (pr::str::EqualI(option, "-shmove"  ) && arg_end - arg >= 2) { m_fo.wFunc = FO_MOVE;   m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = Read::Paths(*(arg+1), m_dst); arg += 2; return true; }
		if (pr::str::EqualI(option, "-shrename") && arg_end - arg >= 2) { m_fo.wFunc = FO_RENAME; m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = Read::Paths(*(arg+1), m_dst); arg += 2; return true; }
		if (pr::str::EqualI(option, "-shdelete") && arg_end - arg >= 1) { m_fo.wFunc = FO_DELETE; m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = 0;                            arg += 1; return true; }
		if (pr::str::EqualI(option, "-flags"   ) && arg_end - arg >= 1) { Read::Flags(*arg, m_fo.fFlags); arg += 1; return true; }
		if (pr::str::EqualI(option, "-title"   ) && arg_end - arg >= 1) { m_title = *arg++; m_fo.lpszProgressTitle = m_title.c_str(); return true; }
		return ICex::CmdLineOption(option, arg, arg_end);
	}

	int ShFileOp::Run()
	{
		// Returns 0 for success, 1 for aborted, or an error code
		// Note: This is not a GetLastError() error code. See the docs for SHFileOperation()
		int res = SHFileOperationA(&m_fo);
		if (res == 0) res = m_fo.fAnyOperationsAborted;
		return res;
	}
}
