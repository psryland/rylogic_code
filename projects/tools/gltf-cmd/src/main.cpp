#include "src/forward.h"
#include "src/icommand.h"
#include "src/commands/cmd_dump.h"

using namespace pr;
using namespace pr::geometry;

namespace gltf_cmd
{
	struct Main :ICommand
	{
		using CmdPtr = std::unique_ptr<ICommand>;
		CmdPtr m_command = {};

		// Main program run
		int Run(std::wstring args)
		{
			try
			{
				// Parse the command line, show help if invalid
				if (!EnumCommandLine(Narrow(args).c_str(), *this))
				{
					ShowConsole();
					if (m_command)
						return m_command->ShowHelp(), -1;

					ShowHelp();
					return -1;
				}

				// Run the command
				if (m_command)
				{
					m_command->ValidateInput();
					return m_command->Run();
				}

				return 0;
			}
			catch (std::exception const& ex)
			{
				ShowConsole();
				std::cerr << "Unhandled error" << std::endl << ex.what() << std::endl;
				return -1;
			}
		}

		// Show the main help
		void ShowHelp() const
		{
			std::cout <<
				ICommand::Title() <<
				"  Syntax: gltf-cmd -command [parameters]\n"
				"    -dump : Dump the structure of a glTF file\n"
				"\n"
				"  Type gltf-cmd -command -help for help on a particular command\n"
				"\n";
		}

		// Read the option passed to gltf-cmd
		bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end) override
		{
			for (;;)
			{
				if (m_command) break;
				if (pr::str::EqualI(option, "-dump")) { m_command = CmdPtr{ new DumpGltf }; break; }
				return ICommand::CmdLineOption(option, arg, arg_end);
			}
			if (arg != arg_end && pr::str::EqualI(*arg, "-help"))
			{
				return false;
			}
			return m_command->CmdLineOption(option, arg, arg_end);
		}

		// Forward arg to the command
		bool CmdLineData(TArgIter& arg, TArgIter arg_end) override
		{
			for (;;)
			{
				if (m_command) break;
				return ICommand::CmdLineData(arg, arg_end);
			}
			return m_command->CmdLineData(arg, arg_end);
		}
	};
}

// Run as a windows program so that the console window is not shown
int __stdcall wWinMain(HINSTANCE, HINSTANCE, LPWSTR lpCmdLine, int)
{
	try
	{
		gltf_cmd::Main m;
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
	gltf_cmd::Main m;
	std::wstring args;
	for (int i = 1; i < argc; ++i) args.append(argv[i]).append(L" ");
	return m.Run(args);
}
