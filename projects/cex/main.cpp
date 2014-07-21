
#include "cex/forward.h"
#include "cex/icex.h"
#include "cex/input.h"
#include "cex/dir_path.h"
#include "cex/msg_box.h"
#include "cex/wait.h"
#include "cex/open_vs.h"
#include "cex/lower.h"
#include "cex/exec.h"
#include "cex/shell_file_op.h"
#include "cex/clip.h"
#include "cex/hash.h"
#include "cex/guid.h"
#include "cex/data_header_gen.h"

namespace cex
{
	char const VersionString[] = "v1.2";

	struct Main :cex::ICex
	{
		pr::InitCom m_com;
		std::unique_ptr<cex::ICex> m_command;

		Main()
			:m_com(COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE)
			,m_command()
		{}

		int Run() override
		{
			return -1;
		}

		// Show the main help
		void ShowHelp() const
		{
			std::cout <<
				"\n"
				"***********************************************************\n"
				" --- Commandline Extensions - Copyright (c) Rylogic 2004 --- \n"
				"***********************************************************\n"
				" Version: " << cex::VersionString << "\n"
				"  Syntax: Cex -command [parameters]\n";
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
				"    -hash     : Generate a hash of the given text input\n"
				"    -guid     : Generate a guid\n"
				"    -hdata    : Convert a file to C/C++ header file data\n"
				// NEW_COMMAND - add a help string
				"\n"
				"  Type Cex -command -help for help on a particular command\n"
				"  Remember to add the following to your batch file immediately\n"
				"   after this command:\n"
				"     call ~cex.bat\n"
				"     del  ~cex.bat\n"
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
				try { pr::xml::Load(config.c_str(), root); }
				catch (std::exception const& ex)
				{
					std::cout << "Failed to load " << config << std::endl << ex.what() << std::endl;
					return -1;
				}

				// Read elements from the xml file
				std::string process, startdir;
				for (auto& child : root.m_child)
				{
					if      (child == L"process" ) process  = child.as<std::string>();
					else if (child == L"startdir") startdir = child.as<std::string>();
					else if (child == L"arg"     ) args.append(args.empty() ? "" : " ").append(child.as<std::string>());
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

			if (args.empty())
			{
				ShowHelp();
				return -1;
			}
			
			if (!pr::EnumCommandLine(args.c_str(), *this))
			{
				if (!m_command)
					ShowHelp();

				return -1;
			}
			if (!m_command)
			{
				ShowHelp();
				return -1;
			}
			return m_command->Run(); // Note the returned value is accessed using %errorlevel% in batch files
		}

		// Read the option passed to Cex
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg ,pr::cmdline::TArgIter arg_end) override
		{
			for (;;)
			{
				if (m_command)
					break;

				if (pr::str::EqualI(option, "-input"    )) { m_command = std::make_unique<cex::Input   >(); break; }
				if (pr::str::EqualI(option, "-dirpath"  )) { m_command = std::make_unique<cex::DirPath >(); break; }
				if (pr::str::EqualI(option, "-msgbox"   )) { m_command = std::make_unique<cex::MsgBox  >(); break; }
				if (pr::str::EqualI(option, "-wait"     )) { m_command = std::make_unique<cex::Wait    >(); break; }
				if (pr::str::EqualI(option, "-openvs"   )) { m_command = std::make_unique<cex::OpenVS  >(); break; }
				if (pr::str::EqualI(option, "-lower"    )) { m_command = std::make_unique<cex::ToLower >(); break; }
				if (pr::str::EqualI(option, "-exec"     )) { m_command = std::make_unique<cex::Exec    >(); break; }
				if (pr::str::EqualI(option, "-shcopy"   )) { m_command = std::make_unique<cex::ShFileOp>(); break; }
				if (pr::str::EqualI(option, "-shmove"   )) { m_command = std::make_unique<cex::ShFileOp>(); break; }
				if (pr::str::EqualI(option, "-shrename" )) { m_command = std::make_unique<cex::ShFileOp>(); break; }
				if (pr::str::EqualI(option, "-shdelete" )) { m_command = std::make_unique<cex::ShFileOp>(); break; }
				if (pr::str::EqualI(option, "-clip"     )) { m_command = std::make_unique<cex::Clip    >(); break; }
				if (pr::str::EqualI(option, "-hash"     )) { m_command = std::make_unique<cex::Hash    >(); break; }
				if (pr::str::EqualI(option, "-guid"     )) { m_command = std::make_unique<cex::Guid    >(); break; }
				if (pr::str::EqualI(option, "-hdata"    )) { m_command = std::make_unique<cex::HData   >(); break; }
				// NEW_COMMAND - handle the command
				return ICex::CmdLineOption(option, arg, arg_end);
			}
			return m_command->CmdLineOption(option, arg, arg_end);
		}

		// Forward arg to the command
		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) override
		{
			for (;;)
			{
				if (m_command) break;
				return ICex::CmdLineData(arg, arg_end);
			}
			return m_command->CmdLineData(arg, arg_end);
		}
	};
}

// Run as a windows program so that the console window is not shown
int __stdcall WinMain(HINSTANCE,HINSTANCE,LPSTR lpCmdLine,int)
{
	//MessageBox(0, "Paws'd", "Cex", MB_OK);
	cex::Main m;
	return m.Run(lpCmdLine);
}

int main(int argc, char* argv[])
{
	cex::Main m;
	std::string args;
	for (int i = 1; i < argc; ++i) args.append(argv[i]).append(" ");
	return m.Run(args);
}