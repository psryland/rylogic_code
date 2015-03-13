
#include "cex/src/forward.h"
#include "cex/src/icex.h"
#include "cex/src/dir_path.h"
#include "cex/src/msg_box.h"
#include "cex/src/wait.h"
#include "cex/src/open_vs.h"
#include "cex/src/lower.h"
#include "cex/src/exec.h"
#include "cex/src/shell_file_op.h"
#include "cex/src/clip.h"
#include "cex/src/hash.h"
#include "cex/src/guid.h"
#include "cex/src/data_header_gen.h"
#include "cex/src/p3d.h"
//#include "cex/src/NEW_COMMAND.h"

namespace cex
{
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
				ICex::Title() <<
				"  Syntax: Cex -command [parameters]\n"
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
				"    -p3d      : P3d model file format converter\n"
				// NEW_COMMAND - add a help string
				"\n"
				"  Type Cex -command -help for help on a particular command\n"
				;
		}

		// Main program run
		int Run(std::string args)
		{
			// Get the name of this executable
			char exepath_[1024]; GetModuleFileNameA(0, exepath_, sizeof(exepath_));
			std::string exepath = exepath_;
			std::string path = pr::str::LowerCase(pr::filesys::GetDirectory(exepath));
			std::string name = pr::str::LowerCase(pr::filesys::GetFiletitle(exepath));
			std::string extn = pr::str::LowerCase(pr::filesys::GetExtension(exepath));

			// Look for an xml file with the same name as this program in the local directory
			std::string config = path + "\\" + name + ".xml";
			if (pr::filesys::FileExists(config))
				return RunFromXml(config, args);

			// If the name of the exe is not 'cex', assume an implicit -exename as the first command line argument
			if (name != "cex")
				args.insert(0, pr::FmtS("-%s", name.c_str()));

			//NEW_COMMAND - Test the new command
			//if (!args.empty()) printf("warning: debugging overriding arguments");
			//args = R("-input blah -msg "Type a value> ")";
			//args = R("-shcopy "c:/deleteme/SQ.bin,c:/deleteme/TheList.txt" "c:/deleteme/cexitime/" -title "Testing shcopy")";
			//args = R"(-clip -lwr -bkslash "C:/blah" "Boris" "F:\\Jef/wan")";
			//args = R"(-p3d -fi "P:\dump\test.3ds" -verbosity 5 -remove_degenerates 4096 -gen_normals 40)";
			//args = R"(-p3d -fi R:\localrepo\PC\vrex\res\pelvis.3ds -verbosity 3 -remove_degenerates 16384 -gen_normals 40)";

			// Parse the command line, show help if invalid
			try
			{
				if (!pr::EnumCommandLine(args.c_str(), *this))
				{
					ShowConsole();

					if (m_command)
						return m_command->ShowHelp(), -1;
				
					ShowHelp();
					return -1;
				}
			}
			catch (std::exception const& ex)
			{
				ShowConsole();
				std::cerr << "Command line error" << std::endl << ex.what() << std::endl;
				return -1;
			}

			// Run the command
			// It's the commands decision whether to display the console or not
			if (m_command)
				return m_command->Run(); // Note the returned value is accessed using %errorlevel% in batch files

			// Assume error messages have been displayed already
			return 0;
		}

		// Read the option passed to Cex
		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg ,pr::cmdline::TArgIter arg_end) override
		{
			for (;;)
			{
				if (m_command) break;
				if (pr::str::EqualI(option, "-dirpath"  )) { m_command = std::make_unique<cex::DirPath  >(); break; }
				if (pr::str::EqualI(option, "-msgbox"   )) { m_command = std::make_unique<cex::MsgBox   >(); break; }
				if (pr::str::EqualI(option, "-wait"     )) { m_command = std::make_unique<cex::Wait     >(); break; }
				if (pr::str::EqualI(option, "-openvs"   )) { m_command = std::make_unique<cex::OpenVS   >(); break; }
				if (pr::str::EqualI(option, "-lower"    )) { m_command = std::make_unique<cex::ToLower  >(); break; }
				if (pr::str::EqualI(option, "-exec"     )) { m_command = std::make_unique<cex::Exec     >(); break; }
				if (pr::str::EqualI(option, "-shcopy"   )) { m_command = std::make_unique<cex::ShFileOp >(); break; }
				if (pr::str::EqualI(option, "-shmove"   )) { m_command = std::make_unique<cex::ShFileOp >(); break; }
				if (pr::str::EqualI(option, "-shrename" )) { m_command = std::make_unique<cex::ShFileOp >(); break; }
				if (pr::str::EqualI(option, "-shdelete" )) { m_command = std::make_unique<cex::ShFileOp >(); break; }
				if (pr::str::EqualI(option, "-clip"     )) { m_command = std::make_unique<cex::Clip     >(); break; }
				if (pr::str::EqualI(option, "-hash"     )) { m_command = std::make_unique<cex::Hash     >(); break; }
				if (pr::str::EqualI(option, "-guid"     )) { m_command = std::make_unique<cex::Guid     >(); break; }
				if (pr::str::EqualI(option, "-hdata"    )) { m_command = std::make_unique<cex::HData    >(); break; }
				if (pr::str::EqualI(option, "-p3d"      )) { m_command = std::make_unique<cex::P3d      >(); break; }
				// NEW_COMMAND - handle the command
				return ICex::CmdLineOption(option, arg, arg_end);
			}
			if (arg != arg_end && pr::str::EqualI(*arg, "-help"))
			{
				return false; // no more command line please
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

		// Read 'config' and execute
		int RunFromXml(std::string config, std::string args)
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

			return -1;
		}
	};
}

// Run as a windows program so that the console window is not shown
int __stdcall WinMain(HINSTANCE,HINSTANCE,LPSTR lpCmdLine,int)
{
	try
	{
		//MessageBox(0, "Paws'd", "Cex", MB_OK);
		cex::Main m;
		return m.Run(lpCmdLine);
	}
	catch (std::exception const& ex)
	{
		std::cout << ex.what() << std::endl;
		return -1;
	}
}

int main(int argc, char* argv[])
{
	cex::Main m;
	std::string args;
	for (int i = 1; i < argc; ++i) args.append(argv[i]).append(" ");
	return m.Run(args);
}