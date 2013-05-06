//**********************************************
// Command line extensions
//  Copyright © Rylogic Ltd 2004
//**********************************************
// To add new commands add code at the NEW_COMMAND comments

#define _WIN32_WINNT 0x0500
#include <windows.h>
#include <iostream>
#include <iterator>
#include <process.h>
#include <shlobj.h>
#include <atlbase.h>
#include <fcntl.h>
#include <io.h>
#include <shellapi.h>
#include "pr/common/assert.h"
#include "pr/common/command_line.h"
#include "pr/common/fmt.h"
#include "pr/common/hresult.h"
#include "pr/common/windows_com.h"
#include "pr/common/clipboard.h"
#include "pr/str/prstring.h"
#include "pr/str/tostring.h"
#include "pr/threads/process.h"
#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"
#include "pr/storage/xml.h"

// import EnvDTE
#pragma warning(disable : 4278)
#pragma warning(disable : 4146)
#import "libid:80cc9f66-e7d8-4ddd-85b6-d9e6cd0e93e2" version("8.0") lcid("0") raw_interfaces_only named_guids
#pragma warning(default : 4146)
#pragma warning(default : 4278)

using namespace pr;
using namespace pr::cmdline;

namespace cex
{
	char const VersionString[] = "v1.1";
	
	inline void SetEnvVar(std::string const& env_var, std::string const& value)
	{
		pr::Handle fp = pr::FileOpen("~cex.bat", pr::EFileOpen::Writing);
		if (!fp) { std::cerr << "Failed to create '~cex.bat' file\n"; return; }
		pr::FileWrite(fp, pr::FmtS("@echo off\nset %s=%s\n" ,env_var.c_str() ,value.c_str())); //"DEL /Q ~cex.bat\n"
	}
	
	struct ICex
	{
		virtual void ShowHelp() const = 0;
		virtual bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "/?") ||
				str::EqualI(option, "-h") ||
				str::EqualI(option, "-help"))
			{ ShowHelp(); arg = arg_end; return true; }
			std::cerr << "Error: Unknown option '" << option << "'\n";
			return false;
		}
		virtual bool CmdLineData(TArgIter& arg, TArgIter)
		{
			std::cerr << "Error: Unknown option '" << *arg << "'\n";
			return false;
		}
		virtual int Run() = 0;
	};

	// Input ****************************************************************************
	struct Input :ICex
	{
		std::string m_message; // Message to display before reading input
		std::string m_env_var; // Name of the environment variable to set
		void ShowHelp() const
		{
			std::cout <<
				"Input : Read user input into an environment variable\n"
				" Syntax: Cex -input environment_variable_name [-msg \"Message\"]\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg , TArgIter arg_end)
		{
			if (str::EqualI(option, "-input") && arg != arg_end) { m_env_var = *arg++; return true; }
			if (str::EqualI(option, "-msg"  ) && arg != arg_end) { m_message = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			if (!m_message.empty()) { std::cout << m_message; }
			std::string value;
			std::getline(std::cin, value);
			SetEnvVar(m_env_var, value);
			return 0;
		}
	};
	// DirPath ****************************************************************************
	struct DirPath :ICex
	{
		std::string m_message; // Message to display before reading input
		std::string m_env_var; // Name of the environment variable to set
		void ShowHelp() const
		{
			std::cout <<
				"DirPath : Open a dialog window for finding a path.\n"
				"          Path name is stored into an environment variable\n"
				" Syntax: Cex -dirpath environment_variable_name [-msg \"Message\"]\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg , TArgIter arg_end)
		{
			if (str::EqualI(option, "-dirpath") && arg != arg_end) { m_env_var = *arg++; return true; }
			if (str::EqualI(option, "-msg"    ) && arg != arg_end) { m_message = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			char display_name[MAX_PATH];
			BROWSEINFO browse_info;
			browse_info.hwndOwner = GetConsoleWindow(); 
			browse_info.pidlRoot = 0;
			browse_info.pszDisplayName = display_name;
			browse_info.lpszTitle = m_message.c_str();
			browse_info.ulFlags = BIF_NEWDIALOGSTYLE | BIF_RETURNONLYFSDIRS;
			browse_info.lpfn = 0;
			browse_info.lParam = 0;
			browse_info.iImage = 0;
			LPITEMIDLIST p = SHBrowseForFolder(&browse_info);
 
			std::string dir_path(MAX_PATH, 0);
			SHGetPathFromIDList(p, &dir_path[0]);
		
			SetEnvVar(m_env_var, dir_path);
			return 0;
		}
	};
	// Message Box ****************************************************************************
	struct MsgBox :ICex
	{
		std::string m_title;    // Title of the message box
		std::string m_text;     // Body text of the message box
		unsigned int m_style;   // Message box style
		MsgBox() :m_title("Message") ,m_text("") ,m_style(0) {}
		void ShowHelp() const
		{
			std::cout <<
				"MsgBox : Display a message box.\n"
				" Syntax: Cex -msgbox -title \"title text\" -body \"body text\" -style style_id\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "-msgbox")) { return true; }
			if (str::EqualI(option, "-title") && arg != arg_end) { m_title = *arg++; return true; }
			if (str::EqualI(option, "-body" ) && arg != arg_end) { m_text  = *arg++; return true; }
			if (str::EqualI(option, "-style") && arg != arg_end) { m_style = (unsigned int)strtoul(arg->c_str(), 0, 10); ++arg; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			return MessageBox(0, m_text.c_str(), m_title.c_str(), m_style);
		}
	};
	// Wait ****************************************************************************
	struct Wait :ICex
	{
		unsigned int m_seconds; // Time to wait in seconds
		std::string  m_message; // Message to display while waiting
		Wait() :m_seconds(1) ,m_message() {}
		void ShowHelp() const
		{
			std::cout <<
				"Wait: Wait for a specified length of time\n"
				" Syntax: Cex -wait 5 -msg \"Message to display\"\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "-wait") && arg != arg_end) { return str::ExtractIntC(m_seconds, 10, (*arg++).c_str()); }
			if (str::EqualI(option, "-msg" ) && arg != arg_end) { m_message = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			if (!m_message.empty()) std::cout << m_message << "\n(Waiting " << m_seconds << " seconds)\n";
			Sleep(m_seconds * 1000);
			return 0;
		}
	};
	// OpenVS ****************************************************************************
	struct OpenVS :ICex
	{
		std::string  m_file;   // File to open
		unsigned int m_line;   // Line number to go to
		OpenVS() :m_file() ,m_line(0) {}
		void ShowHelp() const
		{
			std::cout <<
				"OpenVS: Open a file in an existing instance of visual studio\n"
				" Syntax: Cex -openvs \"filename\":line_number\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "-openvs"))
			{
				std::size_t end = arg->find_last_of(':');
				if (end != 1 && end != std::string::npos) // not a drive colon
				{
					m_file = arg->substr(0, end);
					m_line = strtoul(arg->substr(end+1).c_str(), 0, 10);
				}
				else
				{
					m_file = *arg;
				}
				arg = arg_end;
				return true;
			}
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			HRESULT result;
			CLSID clsid;
			for (;;)
			{
				result = ::CLSIDFromProgID(L"VisualStudio.DTE.8.0", &clsid);
				if (FAILED(result)) break;

				CComPtr<IUnknown> punk;
				result = ::GetActiveObject(clsid, NULL, &punk);
				if (FAILED(result)) break;

				CComPtr<EnvDTE::_DTE> DTE;
				DTE = punk;

				CComPtr<EnvDTE::ItemOperations> item_ops;
				result = DTE->get_ItemOperations(&item_ops);
				if (FAILED(result)) break;

				CComBSTR bstrFileName(m_file.c_str());
				CComBSTR bstrKind(EnvDTE::vsViewKindTextView);
				CComPtr<EnvDTE::Window> window;
				result = item_ops->OpenFile(bstrFileName, bstrKind, &window);
				if (FAILED(result)) break;

				CComPtr<EnvDTE::Document> doc;
				result = DTE->get_ActiveDocument(&doc);
				if (FAILED(result)) break;

				CComPtr<IDispatch> selection_dispatch;
				result = doc->get_Selection(&selection_dispatch);
				if (FAILED(result)) break;

				CComPtr<EnvDTE::TextSelection> selection;
				result = selection_dispatch->QueryInterface(&selection);
				if (FAILED(result)) break;

				result = selection->GotoLine(m_line, TRUE);
				if (FAILED(result)) break;
			
				return 0;
			}
			std::cerr << "Failed to open file in VS.\nReason: " << pr::ToString(result) << "\n";
			return -1;
		}
	};
	// ToLower ****************************************************************************
	struct ToLower :ICex
	{
		std::string m_str; // string to lower
		ToLower() :m_str() {}
		void ShowHelp() const
		{
			std::cout <<
				"ToLower: Convert a string to lower case\n"
				" Syntax: Cex -lwr \"Message to lower\"\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "-lwr") && arg != arg_end) { m_str.append(!m_str.empty() ? " " : "").append(*arg++); return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			std::transform(m_str.begin(), m_str.end(), m_str.begin(), tolower);
			if (!m_str.empty()) std::cout << m_str;
			return 0;
		}
	};
	// Exec ****************************************************************************
	struct Exec :ICex
	{
		std::string m_process;
		std::string m_args;
		std::string m_startdir;
		bool m_async;
		Exec() :m_process() ,m_args() ,m_startdir() ,m_async(false) {}
		void ShowHelp() const
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
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "-exec"    ) && arg != arg_end) { m_args.append(!m_args.empty() ? " " : "").append(*arg++); return true; }
			if (str::EqualI(option, "-p"       ) && arg != arg_end) { m_process = *arg++; return true; }
			if (str::EqualI(option, "-async"   ))                   { m_async = true; return true; }
			if (str::EqualI(option, "-startdir") && arg != arg_end) { m_startdir = *arg++; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
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
	// ShFileOp ****************************************************************************
	struct ShFileOp :ICex
	{
		SHFILEOPSTRUCT m_fo;
		std::string m_src, m_dst, m_title;
		ShFileOp::ShFileOp() :m_fo() ,m_src() ,m_dst() {}
		void ShowHelp() const
		{
			std::cout <<
				"Shell File Operation : Perform a file operation using the windows explorer shell\n"
				" Syntax: Cex -shcopy|-shmove|-shrename|-shdelete [options]\n"
				"  -shcopy   src_file1,src_file2,... dst_path0,dst_path1,... [-flags flag0,flag1] [-title title]\n"
				"  -shmove   src_file1,src_file2,... dst_path0,dst_path1,... [-flags flag0,flag1] [-title title]\n"
				"  -shrename src_file1,src_file2,... dst_path0,dst_path1,... [-flags flag0,flag1] [-title title]\n"
				"  -shdelete src_file1,src_file2,... [-flags flag0,flag1] [-title title]\n"
				"     src_files : Standard MS-DOS wildcard characters, such as '*', are permitted\n"
				"                 only in the file-name position. Using a wildcard character elsewhere\n"
				"                 in the string will lead to unpredictable results.\n"
				"     dst_path  : Wildcard characters are not supported.\n"
				"                 Copy and Move operations can specify destination directories that do\n"
				"                 not exist. In those cases, the system attempts to create them and normally\n"
				"                 displays a dialog box to ask the user if they want to create the new directory.\n"
				"                 To suppress this dialog box and have the directories created silently, set the\n"
				"                 NoConfirmMkDir flag in -flags.\n"
				"                 For Copy and Move operations, the buffer can contain multiple destination file\n"
				"                 names if the -flags member specifies MultiDestFiles.\n"
				"     flags     : AllowUndo - Preserve undo information, if possible.\n"
				"                 FilesOnly - Perform the operation only on files (not on folders) if a wildcard\n"
				"                             file name (*.*) is specified.\n"
				"                 MultiDestFiles - The dst_path list specifies multiple destination files (one for\n"
				"                             each source file in src_files) rather than one directory where\n"
				"                             all source files are to be deposited.\n"
				"                 NoConfirmation - Respond with Yes to All for any dialog box that is displayed.\n"
				"                 NoConfirmMkDir - Do not ask the user to confirm the creation of a new directory\n"
				"                             if the operation requires one to be created.\n"
				"                 NoConnectedElements - WinVer 5.0. Do not move connected files as a group.\n"
				"                             Only move the specified files.\n"
				"                 NoCopySecurityAttribs - WinVer 4.71. Do not copy the security attributes of the file.\n"
				"                             The destination file receives the security attributes of its new folder.\n"
				"                 NoErrorUI - Do not display a dialog to the user if an error occurs.\n"
				"                 NoRecursion - Only perform the operation in the local directory. Do not operate\n"
				"                             recursively into subdirectories, which is the default behavior.\n"
				"                 NoUI - WinVer 6.0.6060 (Windows Vista). Perform the operation silently, presenting\n"
				"                             no UI to the user. This is equivalent to Silent,NoConfirmation,NoErrorUI,NoConfirmMkDir.\n"
				"                 RenameOnCollision - Give the file being operated on a new name in a move, copy, or rename\n"
				"                             operation if a file with the target name already exists at the destination.\n"
				"                 Silent - Do not display a progress dialog box.\n"
				"                 SimpleProgress - Display a progress dialog box but do not show individual file names\n"
				"                             as they are operated on.\n"
				"                 WantNukeWarning - WinVer 5.0. Send a warning if a file is being permanently destroyed\n"
				"                             during a delete operation rather than recycled. This flag partially overrides NoConfirmation.\n"
				"     title     : A title to display on progress dialogs\n";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			struct Read
			{
				static char* Paths(std::string const& arg, std::string& var)
				{
					var.clear();
					std::vector<std::string> paths;
					pr::str::Split(arg, ",", std::back_inserter(paths));
					for (std::vector<std::string>::const_iterator i = paths.begin(), iend = paths.end(); i != iend; ++i) var.append(pr::filesys::GetFullPath(*i)).push_back('\0');
					var.push_back('\0');
					return &var[0];
				}
				static void Flags(std::string const& arg, FILEOP_FLAGS& var)
				{
					var = 0;
					std::vector<std::string> flags;
					pr::str::Split(arg, ",", std::back_inserter(flags));
					for (std::vector<std::string>::const_iterator i = flags.begin(), iend = flags.end(); i != iend; ++i)
					{
						if (str::EqualI(*i, "AllowUndo"             )) { var |= FOF_ALLOWUNDO;             continue; }
						if (str::EqualI(*i, "FilesOnly"             )) { var |= FOF_FILESONLY;             continue; }
						if (str::EqualI(*i, "MultiDestFiles"        )) { var |= FOF_MULTIDESTFILES;        continue; }
						if (str::EqualI(*i, "NoConfirmation"        )) { var |= FOF_NOCONFIRMATION;        continue; }
						if (str::EqualI(*i, "NoConfirmMkDir"        )) { var |= FOF_NOCONFIRMMKDIR;        continue; }
						if (str::EqualI(*i, "NoConnectedElements"   )) { var |= FOF_NO_CONNECTED_ELEMENTS; continue; }
						if (str::EqualI(*i, "NoCopySecurityAttribs" )) { var |= FOF_NOCOPYSECURITYATTRIBS; continue; }
						if (str::EqualI(*i, "NoErrorUI"             )) { var |= FOF_NOERRORUI;             continue; }
						if (str::EqualI(*i, "NoRecursion"           )) { var |= FOF_NORECURSION;           continue; }
						if (str::EqualI(*i, "NoUI"                  )) { var |= FOF_NO_UI;                 continue; }
						if (str::EqualI(*i, "RenameOnCollision"     )) { var |= FOF_RENAMEONCOLLISION;     continue; }
						if (str::EqualI(*i, "Silent"                )) { var |= FOF_SILENT;                continue; }
						if (str::EqualI(*i, "SimpleProgress"        )) { var |= FOF_SIMPLEPROGRESS;        continue; }
						if (str::EqualI(*i, "WantNukeWarning"       )) { var |= FOF_WANTNUKEWARNING;       continue; }
					}
				}
			};

			if (str::EqualI(option, "-shcopy"  ) && arg_end - arg >= 2) { m_fo.wFunc = FO_COPY;   m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = Read::Paths(*(arg+1), m_dst); arg += 2; return true; }
			if (str::EqualI(option, "-shmove"  ) && arg_end - arg >= 2) { m_fo.wFunc = FO_MOVE;   m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = Read::Paths(*(arg+1), m_dst); arg += 2; return true; }
			if (str::EqualI(option, "-shrename") && arg_end - arg >= 2) { m_fo.wFunc = FO_RENAME; m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = Read::Paths(*(arg+1), m_dst); arg += 2; return true; }
			if (str::EqualI(option, "-shdelete") && arg_end - arg >= 1) { m_fo.wFunc = FO_DELETE; m_fo.pFrom = Read::Paths(*arg, m_src); m_fo.pTo = 0;                            arg += 1; return true; }
			if (str::EqualI(option, "-flags"   ) && arg_end - arg >= 1) { Read::Flags(*arg, m_fo.fFlags); arg += 1; return true; }
			if (str::EqualI(option, "-title"   ) && arg_end - arg >= 1) { m_title = *arg++; m_fo.lpszProgressTitle = m_title.c_str(); return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		int Run()
		{
			// Returns 0 for success, 1 for aborted, or an error code
			// Note: This is not a GetLastError() error code. See the docs for SHFileOperation()
			int res = SHFileOperation(&m_fo);
			if (res == 0) res = m_fo.fAnyOperationsAborted;
			return res;
		}
	};
	// Clip ****************************************************************************
	struct Clip :ICex
	{
		std::string m_text;
		bool m_lwr, m_upr, m_fwdslash, m_bkslash, m_cstr, m_dopaste;
		std::string m_newline;
		Clip() :m_text() ,m_lwr(false) ,m_upr(false) ,m_fwdslash(false) ,m_bkslash(false) ,m_cstr(false) ,m_dopaste(false) ,m_newline() {}
		void ShowHelp() const
		{
			std::cout <<
				"Clip text to the system clipboard\n"
				" Syntax: Cex -clip [-lwr][-upr][-fwdslash][-bkslash][-cstr] [-crlf|cr|lf] text_to_copy ...\n"
				"  -lwr : converts copied text to lower case\n"
				"  -upr : converts copied text to upper case\n"
				"  -fwdslash : converts any directory marks to forward slashes\n"
				"  -bkslash : converts any directory marks to back slashes\n"
				"  -cstr : converts the copied text to a C\\C++ style string by adding escape characters\n"
				"  -crlf|cr|lf : convert newlines to the dos,mac,linux format\n"
				"\n"
				" Syntax: Cex -clip -paste\n"
				"   Paste the clipboard contents to stdout\n"
				;
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			if (str::EqualI(option, "-clip"    )) { return true; }
			if (str::EqualI(option, "-lwr"     )) { m_lwr = true; return true; }
			if (str::EqualI(option, "-upr"     )) { m_upr = true; return true; }
			if (str::EqualI(option, "-fwdslash")) { m_fwdslash = true; return true; }
			if (str::EqualI(option, "-bkslash" )) { m_bkslash = true; return true; }
			if (str::EqualI(option, "-cstr"    )) { m_cstr = true; return true; }
			if (str::EqualI(option, "-crlf"    )) { m_newline = "\r\n"; return true; }
			if (str::EqualI(option, "-cr"      )) { m_newline = "\r"; return true; }
			if (str::EqualI(option, "-lf"      )) { m_newline = "\n"; return true; }
			if (str::EqualI(option, "-paste"   )) { m_dopaste = true; return true; }
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		bool CmdLineData(TArgIter& arg, TArgIter)
		{
			if (!m_text.empty()) m_text += "\r\n";
			m_text += *arg++;
			return true;
		}
		int Run()
		{
			if (m_dopaste)
			{
				std::string output;
				if (!pr::GetClipBoardText(GetConsoleWindow(), output)) return -1;
				std::cout << output;
				return 0;
			}

			// Perform optional conversions
			if (m_lwr)              { pr::str::LowerCase(m_text); }
			if (m_upr)              { pr::str::UpperCase(m_text); }
			if (m_fwdslash)         { pr::str::Replace(m_text, "\\\\", "/");  pr::str::Replace(m_text, "\\", "/"); }
			if (m_bkslash)          { pr::str::Replace(m_text, "\\\\", "\\"); pr::str::Replace(m_text, "/", "\\"); }
			if (!m_newline.empty()) { pr::str::Replace(m_text, "\r\n", "\n"); pr::str::Replace(m_text, "\r", "\n"); pr::str::Replace(m_text, "\n", m_newline.c_str()); }
			if (m_cstr)             { m_text = pr::str::StringToCString<std::string>(m_text); }
			return pr::SetClipBoardText(GetConsoleWindow(), m_text) ? 0 : -1;
		}
	};
	// NEW_COMMAND ****************************************************************************
	struct NEW_COMMAND :ICex
	{
		NEW_COMMAND() {}
		void ShowHelp() const
		{
			std::cout <<
				"";
		}
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
		{
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		bool CmdLineData(TArgIter& arg, TArgIter arg_end)
		{
			return ICex::CmdLineData(arg, arg_end);
		}
		int Run()
		{
			return 0;
		}
	};
}
	
// Main *********************************************************************************************
class Main :public pr::cmdline::IOptionReceiver ,public cex::ICex
{
	pr::InitCom m_com;
	cex::ICex* m_command;

	int Run() { return -1; }
public:
	Main()
	:m_com(COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE)
	,m_command(0)
	{}
	~Main()
	{
		delete m_command;
	}
	
	// Show the main help
	void ShowHelp() const
	{
		std::cout <<
			"\n"
			"***********************************************************\n"
			" --- Commandline Extensions - Copyright © Rylogic 2006 --- \n"
			"***********************************************************\n"
			" Version: " << cex::VersionString << "\n"
			"  Syntax: Cex -command [parameters]\n"
			"    -input    : Read user input into an environment variable\n"
			"    -dirpath  : Read a directory path into an environment variable\n"
			"    -msgbox   : Display a message box\n"
			"    -wait     : Wait for a specified length of time\n"
			"    -openvs   : Open a file in an existing instance of visual studio at a line\n"
			"    -lower    : Return the lower case version of a given string\n"
			"    -exec     : Execute another process\n"
			"    -shcopy   : Copy files using the explorer shell\n"
			"    -shmove   : Move files using the explorer shell\n"
			"    -shrename : Rename files using the explorer shell\n"
			"    -shdelete : Delete filesusing the explorer shell\n"
			"    -clip     : Clip text to the system clipboard\n"
			// NEW_COMMAND - add a help string
			"\n"
			"  Type Cex -command -help for help on a particular command\n"
			//"  Remember to add the following to your batch file immediately\n"
			//"   after this command:\n"
			//"     call ~cex.bat\n"
			//"     del  ~cex.bat\n"
			;
	}
	
	// Main program run
	int Run(std::string args)
	{
		// Get the name of this executable
		char exepath_[1024]; GetModuleFileName(0, exepath_, sizeof(exepath_));
		std::string exepath = exepath_;
		std::string path = pr::filesys::GetDirectory(exepath);
		std::string name = pr::filesys::GetFiletitle(exepath);
		std::string extn = pr::filesys::GetExtension(exepath);

		// Look for an xml file with the same name as this program in the local directory
		std::string config = path + "\\" + name + ".xml";
		if (pr::filesys::FileExists(config))
		{
			// Load the xml file
			pr::xml::Node root;
			if (pr::Failed(pr::xml::Load(config.c_str(), root)))
			{
				std::cout << "Failed to load " << config << "\n";
				return -1;
			}
		
			// Read elements from the xml file
			std::string process, startdir;
			for (pr::xml::NodeVec::const_iterator i = root.m_child.begin(), iend = root.m_child.end(); i != iend; ++i)
			{
				if      (*i == L"process" ) process  = i->as<std::string>();
				else if (*i == L"startdir") startdir = i->as<std::string>();
				else if (*i == L"arg"     ) args.append(args.empty() ? "" : " ").append(i->as<std::string>());
			}
		
			// If a process name was given, execute it, take that virus scanner :)
			if (!process.empty())
			{
				pr::Process proc;
				proc.Start(process.c_str(), args.c_str(), startdir.c_str());
				return proc.BlockTillExit();
			}
		}

		// If we get to here, run as the normal command line extension program

		// Attach to the current console
		if (AttachConsole((DWORD)-1) || AllocConsole())
		{
			FILE *fp;
			int h;

			// redirect unbuffered STDOUT to the console
			h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_OUTPUT_HANDLE)), _O_TEXT);
			fp = _fdopen(h, "w");
			*stdout = *fp;
			setvbuf(stdout, NULL, _IONBF, 0);

			// redirect unbuffered STDIN to the console
			h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_INPUT_HANDLE )), _O_TEXT);
			fp = _fdopen(h, "r");
			*stdin = *fp;
			setvbuf(stdin, NULL, _IONBF, 0);

			// redirect unbuffered STDERR to the console
			h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_ERROR_HANDLE )), _O_TEXT);
			fp = _fdopen(h, "w");
			*stderr = *fp;
			setvbuf(stderr, NULL, _IONBF, 0);

			// make cout, wcout, cin, wcin, wcerr, cerr, wclog and clog
			// point to console as well
			std::ios::sync_with_stdio();
		}

		//NEW_COMMAND - Test the new command
		//args = "-shcopy \"c:/deleteme/SQ.bin,c:/deleteme/TheList.txt\" \"c:/deleteme/cexitime/\" -title \"Testing shcopy\"";
		//args = "-clip -lwr -bkslash \"C:/blah\" \"Boris\" \"F:\\\\Jef/wan\"";
		
		if (args.empty()) { ShowHelp(); return -1; }
		if (!EnumCommandLine(args.c_str(), *this)) { if (!m_command) { ShowHelp(); } return -1; }
		if (!m_command) { ShowHelp(); return -1; }
		return m_command->Run(); // Note the returned value is accessed using %errorlevel% in batch files
	}
	
	// Read the option passed to Cex
	bool CmdLineOption(std::string const& option, TArgIter& arg , TArgIter arg_end)
	{
		for (;;)
		{
			if (m_command) break;
			if (str::EqualI(option, "-input"    )) { m_command = new cex::Input;    break; }
			if (str::EqualI(option, "-dirpath"  )) { m_command = new cex::DirPath;  break; }
			if (str::EqualI(option, "-msgbox"   )) { m_command = new cex::MsgBox;   break; }
			if (str::EqualI(option, "-wait"     )) { m_command = new cex::Wait;     break; }
			if (str::EqualI(option, "-openvs"   )) { m_command = new cex::OpenVS;   break; }
			if (str::EqualI(option, "-lower"    )) { m_command = new cex::ToLower;  break; }
			if (str::EqualI(option, "-exec"     )) { m_command = new cex::Exec;     break; }
			if (str::EqualI(option, "-shcopy"   )) { m_command = new cex::ShFileOp; break; }
			if (str::EqualI(option, "-shmove"   )) { m_command = new cex::ShFileOp; break; }
			if (str::EqualI(option, "-shrename" )) { m_command = new cex::ShFileOp; break; }
			if (str::EqualI(option, "-shdelete" )) { m_command = new cex::ShFileOp; break; }
			if (str::EqualI(option, "-clip"     )) { m_command = new cex::Clip;     break; }
			// NEW_COMMAND - handle the command
			return ICex::CmdLineOption(option, arg, arg_end);
		}
		return m_command->CmdLineOption(option, arg, arg_end);
	}
	
	// Forward arg to the command
	bool CmdLineData(TArgIter& arg, TArgIter arg_end)
	{
		for (;;)
		{
			if (m_command) break;
			return ICex::CmdLineData(arg, arg_end);
		}
		return m_command->CmdLineData(arg, arg_end);
	}
};
	
// Entry point *********************************************************************************************
	
// Run as a windows program so that the console window is not shown
int __stdcall WinMain(HINSTANCE,HINSTANCE,LPSTR lpCmdLine,int)
{
	//MessageBox(0, "Paws'd", "Cex", MB_OK);
	Main m;
	return m.Run(lpCmdLine);
}
	
int main(int argc, char* argv[])
{
	Main m;
	std::string args;
	for (int i = 1; i < argc; ++i) args.append(argv[i]).append(" ");
	return m.Run(args);
}
