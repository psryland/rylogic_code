//***********************************************************************
// Command line parser
//  Copyright (c) Rylogic Ltd 2006
//***********************************************************************
// Usage:
//	struct Thing : pr::cmdline::IOptionReceiver
//	{
//		std::string m_input_filename;
//		std::string m_output_filename;
//		bool CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end) { ++arg; return true; }
//		bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
//		{
//			if      (pr::str::EqualI(option, "-I") && arg != arg_end) { m_input_filename  = *arg++; return true; }
//			else if (pr::str::EqualI(option, "-O") && arg != arg_end) { m_output_filename = *arg++; return true; }
//			else if (pr::str::EqualI(option, "-h"))                   { ShowHelp(); return true; }
//			printf("Error: Unknown option '%s'\n", option.c_str());
//			return false;
//		}
//		void ShowHelp() { printf("help\n"); }
//	}
//	Thing thing;
//	if (!pr::EnumCommandLine(argc, argv, thing)) return false;
//	//or
//	if (!pr::EnumCommandLine(GetCommandLine(), thing)) return false;
//
#pragma once
#ifndef PR_COMMAND_LINE_H
#define PR_COMMAND_LINE_H

#include <string>
#include <vector>
#include "pr/str/string_util.h" // for pr::str::Tokenise()

namespace pr
{
	namespace cmdline
	{
		typedef std::vector<std::string> TArgs;
		typedef TArgs::const_iterator TArgIter;

		// Helper to test if 'str' is of the form '-xyz'
		inline bool IsOption(std::string const& str) { return str.size() >= 2 && str[0] == '-'; }

		// Interface for receiving command line options
		// Return true for more options
		struct IOptionReceiver
		{
			// Called for anything not preceded by '-'.
			// The caller should advance 'arg' for each argument read.
			// Return true to continue parsing, false to abort parsing, or
			// set arg = arg_end to end parsing and have true returned.
			virtual bool CmdLineData(TArgIter& arg, TArgIter /*arg_end*/) { ++arg; return true; }

			// Called when an option is found. An option is anything preceded by a '-'.
			// 'option' is the name of the option, including the '-'.
			// 'arg' is an iterator to the next command line value after 'option'
			// 'arg_end' is the end of the argument vector
			// e.g. -Option arg0 arg1 arg2
			// The caller should advance 'arg' for each argument read.
			// Return true to continue parsing, false to abort parsing, or
			// set arg = arg_end to end parsing and have true returned.
			virtual bool CmdLineOption(std::string const& /*option*/, TArgIter& /*arg*/, TArgIter /*arg_end*/) { return true; }
		};
	}
	
	// Parse a range of command line arguments
	// Returns true if all command line parameters were parsed
	inline bool EnumCommandLine(pr::cmdline::TArgIter arg, pr::cmdline::TArgIter arg_end, pr::cmdline::IOptionReceiver& receiver)
	{
		for (;arg != arg_end;)
		{
			if (pr::cmdline::IsOption(*arg))
			{
				pr::cmdline::TArgIter option = arg++;
				if (!receiver.CmdLineOption(*option, arg, arg_end))
					return false;
			}
			else
			{
				if (!receiver.CmdLineData(arg, arg_end))
					return false;
			}
		}
		return true;
	}
	
	// Parse console program style command line arguments
	// Returns true if all command line parameters were parsed
	inline bool EnumCommandLine(int argc, char* argv[], pr::cmdline::IOptionReceiver& receiver)
	{
		// Note: ignoring the argv[0] parameter to make both versions of 'EnumCommandLine'
		// consistent. Programs that want the executable name should use:
		//  char exepath[1024]; GetModuleFileName(0, exepath, sizeof(exepath));
		pr::cmdline::TArgs args;
		for (int i = 1; i < argc; ++i) { args.push_back(argv[i]); }
		return argc == 0 || EnumCommandLine(args.begin(), args.end(), receiver);
	}
	
	// Parse windows program style command line arguments
	// Returns true if all command line parameters were parsed
	inline bool EnumCommandLine(char const* cmd_line, pr::cmdline::IOptionReceiver& receiver)
	{
		pr::cmdline::TArgs args;
		pr::str::Tokenise(cmd_line, args, " \t\r\n\v");
		return args.empty() || EnumCommandLine(args.begin(), args.end(), receiver);
	}
}

#endif

