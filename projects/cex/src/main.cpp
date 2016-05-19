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
#include "cex/src/dll_proxy.h"
#include "cex/src/new_lines.h"
//#include "cex/src/NEW_COMMAND.h"

namespace cex
{
	struct Main :cex::ICex
	{
		pr::InitComEx m_com;
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
				"    -shdelete : Delete files using the explorer shell\n"
				"    -clip     : Clip text to the system clipboard\n"
				"    -hash     : Generate a hash of the given text input\n"
				"    -guid     : Generate a guid\n"
				"    -hdata    : Convert a file to C/C++ header file data\n"
				"    -p3d      : P3d model file format converter\n"
				"    -dllproxy : Generate a proxy dll\n"
				"    -newlines : Add or remove newlines from a text file\n"
				// NEW_COMMAND - add a help string
				"\n"
				"  Type Cex -command -help for help on a particular command\n"
				"\n"
				"  Cex can be used as a proxy application. Rename cex.exe to whatever application\n"
				"  name you like, and create an XML file with the same name in the same directory.\n"
				"  In the XML file, put:\n"
				"    <root>\n"
				"        <process>some process full path</process>\n"
				"        <startdir>some directory path</startdir>\n"
				"        <arg>first argument</arg>\n"
				"        <arg>next argument</arg>\n"
				"        <arg>...</arg>\n"
				"    </root>\n"
				"  When the renamed Cex is run, it will look for the XML file and launch whatever\n"
				"  process is specified. Note: you must specify the 'startdir' as well as the 'process'.\n"
				"\n"
				"  Alternatively, if no XML file is found, Cex runs as though the command line was:\n"
				"     cex.exe -<name_that_cex_was_renamed_to>\n"
				"  e.g.\n"
				"     if the cex.exe is renamed to clip.exe, executing it is the same as executing\n"
				"     cex.exe -clip\n"
				;
		}

		// Main program run
		int Run(std::wstring args)
		{
			// Get the name of this executable
			auto exepath = pr::Win32<wchar_t>::ModuleFileName(nullptr);
			auto path = pr::str::LowerCaseC(pr::filesys::GetDirectory(exepath));
			auto name = pr::str::LowerCaseC(pr::filesys::GetFiletitle(exepath));
			auto extn = pr::str::LowerCaseC(pr::filesys::GetExtension(exepath));

			// Look for an XML file with the same name as this program in the local directory
			auto config = path + L"\\" + name + L".xml";
			if (pr::filesys::FileExists(config))
				return RunFromXml(config, args);

			// If the name of the exe is not 'cex', assume an implicit -exename as the first command line argument
			if (name != L"cex")
				args.insert(0, pr::FmtS(L"-%s", name.c_str()));

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
				if (!pr::EnumCommandLine(pr::Narrow(args).c_str(), *this))
				{
					ShowConsole();

					if (m_command)
						return m_command->ShowHelp(), -1;
				
					ShowHelp();
					return -1;
				}
				if (m_command)
					m_command->ValidateInput();
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
		bool CmdLineOption(std::string const& option, TArgIter& arg ,TArgIter arg_end) override
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
				if (pr::str::EqualI(option, "-dllproxy" )) { m_command = std::make_unique<cex::DllProxy >(); break; }
				if (pr::str::EqualI(option, "-newlines" )) { m_command = std::make_unique<cex::NewLines >(); break; }
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
		bool CmdLineData(TArgIter& arg, TArgIter arg_end) override
		{
			for (;;)
			{
				if (m_command) break;
				return ICex::CmdLineData(arg, arg_end);
			}
			return m_command->CmdLineData(arg, arg_end);
		}

		// Read 'config' and execute
		int RunFromXml(std::wstring config, std::wstring args)
		{
			// Load the XML file
			pr::xml::Node root;
			try { root = pr::xml::Load(config.c_str()); }
			catch (std::exception const& ex)
			{
				std::wcout << "Failed to load " << config << std::endl << ex.what() << std::endl;
				return -1;
			}

			// Read elements from the xml file
			std::wstring process, startdir;
			for (auto& child : root.m_child)
			{
				if      (child == L"process" ) process  = child.as<std::wstring>();
				else if (child == L"startdir") startdir = child.as<std::wstring>();
				else if (child == L"arg"     ) args.append(args.empty() ? L"" : L" ").append(child.as<std::wstring>());
			}

			// If a process name was given, execute it, take that virus scanner :)
			if (!process.empty())
			{
				pr::Process proc;
				if (proc.Start(process.c_str(), args.c_str(), startdir.c_str()))
					return proc.BlockTillExit();

				// Copy the error message before any other calls to Succeeded overwrite it
				auto err = pr::Reason();

				ShowConsole();
				std::wcout << "Failed to start process: " << process.c_str() << "\n" << err.c_str() << "\n";
				return -1;
			}

			return -1;
		}
	};
}

// Run as a windows program so that the console window is not shown
int __stdcall wWinMain(HINSTANCE,HINSTANCE,LPWSTR lpCmdLine,int)
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

int __cdecl wmain(int argc, wchar_t* argv[])
{
	cex::Main m;
	std::wstring args;
	for (int i = 1; i < argc; ++i) args.append(argv[i]).append(L" ");
	return m.Run(args);
}