//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"
#include "src/commands/commands.h"

using namespace pr;

namespace cex
{
	struct Main
	{
		InitCom m_com;

		Main()
			:m_com(COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE)
		{}

		int Run(CmdLine& cmd_line)
		{
			// Get the name of this executable
			auto exepath = win32::ExePath();
			auto path = exepath.parent_path();
			auto name = exepath.stem();
			auto extn = exepath.extension();

			// Look for a JSON file with the same name as this program in the local directory
			auto config = path / name.replace_extension(L".json");
			if (std::filesystem::exists(config))
				return RunFromJson(config, cmd_line);

			// If the name of the exe is not 'cex', assume an implicit -exename as the first command line argument
			if (name != "cex")
				cmd_line.args.insert(begin(cmd_line.args), { std::format("{}", name.string()) });

			// Parse the command line
			auto IsOption = [&cmd_line](std::string_view options) -> bool
			{
				for (; !options.empty(); )
				{
					auto pos = options.find(',');
					if (cmd_line.count(std::string(options.substr(0, pos))) != 0)
						return true;
					if (pos == std::string::npos)
						break;
					options = options.substr(pos + 1);
				}
				return false;
			};

			// Forward to the appropriate command
			{
				#define CEX_CMD_OPTIONS(options, description, func) if (IsOption(options)) { return func(cmd_line); }
				CEX_CMD(CEX_CMD_OPTIONS);
				#undef CEX_CMD_OPTIONS
			}

			// If no commands given, display the command line help message
			std::cout << "\n"
				"-------------------------------------------------------------\n"
				"  Console EXtensions \n" 
				"   Copyright (c) Rylogic 2004 \n"
				"   Version: v1.3\n"
				"-------------------------------------------------------------\n"
				"\n"
				" Syntex is: cex --command [parameters]\n"
				"\n"
				"  Cex can be used as a proxy application. Rename cex.exe to whatever application\n"
				"  name you like, and create a JSON file with the same name in the same directory.\n"
				"  In the file, put:\n"
				"    {\n"
				"        process: \"some process full path\"\n"
				"        startdir: \"some directory path\"\n"
				"        args: [\"first argument\", \"next argument\", ...]\n"
				"    }\n"
				"  When the renamed Cex is run, it will look for the JSON file and launch whatever\n"
				"  process is specified. Note: you must specify the 'startdir' as well as the 'process'.\n"
				"\n"
				"  Alternatively, if no file is found, Cex runs as though the command line was:\n"
				"     cex.exe --<name_that_cex_was_renamed_to>\n"
				"  e.g.\n"
				"     if the cex.exe is renamed to clip.exe, executing it is the same as executing\n"
				"     cex.exe --clip\n"
				"\n"
				" Options:\n"
				"\n";
			{
				#define CEX_CMD_OPTIONS(options, description, func) std::cout << "   " << options << " : " << description << "\n";
				CEX_CMD(CEX_CMD_OPTIONS);
				#undef CEX_CMD_OPTIONS
			}
			std::cout << "\n";
			return 0;
		}

		// Read 'config' and execute
		int RunFromJson(std::filesystem::path const& filepath, CmdLine& cmd_line)
		{
			try
			{
				// Load the file
				auto doc = json::Read(filepath, json::Options{ .AllowComments = true });
				auto root = doc.to_object();

				std::wstring process, startdir;

				// Read elements from the file
				if (auto jprocess = root.find("process"))
				{
					process = Widen(jprocess->to<std::string>());
				}
				if (auto jstartdir = root.find("startdir"))
				{
					startdir = Widen(jstartdir->to<std::string>());
				}
				if (auto jargs = root.find("args"))
				{
					for (auto& arg : jargs->to_array())
						cmd_line.args.push_back({ arg.to<std::string>() });
				}

				if (process.empty() || startdir.empty())
					throw std::runtime_error(std::format("JSON file '{}' must contain 'process' and 'startdir' elements", filepath.string()));

				// If a process name was given, execute it
				std::wstring args;
				for (auto& arg : cmd_line.args)
				{
					auto warg = str::Quotes<std::wstring>(Widen(arg.key), true);
					args.append(warg).append(L" ");
				}

				Process proc;
				if (proc.Start(process.c_str(), args.c_str(), startdir.c_str()))
					return proc.BlockTillExit();

				// Copy the error message before any other calls to Succeeded overwrite it
				auto err = Reason();

				ShowConsole();
				std::wcout << "Failed to start process: " << process.c_str() << "\n" << err.c_str() << "\n";
				return -1;
			}
			catch (std::exception const& ex)
			{
				std::wcout << "Failed to load " << filepath << std::endl << ex.what() << std::endl;
				return -1;
			}
		}
	};

	// Show the console for this process
	void ShowConsole()
	{
		// Attach to the current console
		if (AttachConsole((DWORD)-1) || AllocConsole())
		{
			// Redirect the CRT standard input, output, and error handles to the console
			freopen("CONIN$", "r", stdin);
			freopen("CONOUT$", "w", stdout);
			freopen("CONOUT$", "w", stderr);

			// Clear the error state for each of the C++ standard stream objects. We need to do this, as
			// attempts to access the standard streams before they refer to a valid target will cause the
			// 'iostream' objects to enter an error state. In versions of Visual Studio after 2005, this seems
			// to always occur during startup regardless of whether anything has been read from or written to
			// the console or not.
			std::wcout.clear();
			std::cout.clear();
			std::wcerr.clear();
			std::cerr.clear();
			std::wcin.clear();
			std::cin.clear();

			//FILE *fp;
			//int h;

			//// redirect unbuffered STDOUT to the console
			//h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_OUTPUT_HANDLE)), _O_TEXT);
			//fp = _fdopen(h, "w");
			//*stdout = *fp;
			//setvbuf(stdout, NULL, _IONBF, 0);

			//// redirect unbuffered STDIN to the console
			//h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_INPUT_HANDLE )), _O_TEXT);
			//fp = _fdopen(h, "r");
			//*stdin = *fp;
			//setvbuf(stdin, NULL, _IONBF, 0);

			//// redirect unbuffered STDERR to the console
			//h = _open_osfhandle(reinterpret_cast<intptr_t>(GetStdHandle(STD_ERROR_HANDLE )), _O_TEXT);
			//fp = _fdopen(h, "w");
			//*stderr = *fp;
			//setvbuf(stderr, NULL, _IONBF, 0);

			//// make cout, wcout, cin, wcin, wcerr, cerr, wclog and clog point to console as well
			//std::ios::sync_with_stdio();
		}
	}

	void SetEnvVar(std::string_view env_var, std::string_view value)
	{
		try
		{
			std::ofstream file("~cex.bat");
			file << std::format("@echo off\nset {}={}\n", env_var, value); //"DEL /Q ~cex.bat\n"
		}
		catch (std::exception const& ex)
		{
			std::cerr << "Failed to create '~cex.bat' file\n" << ex.what();
		}
	}
}

// Run as a windows program so that the console window is not shown
int __stdcall wWinMain(HINSTANCE,HINSTANCE,LPWSTR lpCmdLine,int)
{
	try
	{
		//MessageBox(0, "Paws'd", "Cex", MB_OK);

		// lpCmdLine doesn't include the program name, but CmdLine expects argv[0] to be the exe path
		auto cl = std::format("{} {}", win32::ExePath().string(), Narrow(lpCmdLine));
		CmdLine cmd_line(cl);

		cex::Main m;
		return m.Run(cmd_line);
	}
	catch (std::exception const& ex)
	{
		std::cout << ex.what() << std::endl;
		return -1;
	}
}
int __cdecl main(int argc, char* argv[])
{
	try
	{
		CmdLine cmd_line(argc, argv);

		cex::Main m;
		return m.Run(cmd_line);
	}
	catch (std::exception const& ex)
	{
		std::cout << ex.what() << std::endl;
		return -1;
	}
}