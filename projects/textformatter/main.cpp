//**********************************************************
// TextFormatter
//  Copyright © Paul Ryland 2012
//**********************************************************
//#include <memory>
#include <string>
//#include <exception>
#include <iostream>
//#include "pr/common/autoptr.h"
#include "pr/common/command_line.h"
//#include "pr/common/prtypes.h"
//#include "pr/maths/maths.h"
//#include "pr/filesys/fileex.h"
//#include "pr/filesys/filesys.h"
//#include "pr/str/prstring.h"
//#include "pr/script/char_stream.h"
/*
struct Main :pr::cmdline::IOptionReceiver
{
	std::string  m_in_file;
	std::string  m_out_file;
	
	Main()
	:m_in_file()
	,m_out_file()
	{}
	
	// Show the main help
	void ShowHelp()
	{
		//printf(
		//	"\n"
		//	"***************************************************\n"
		//	" --- Text Formatter - Copyright © Rylogic 2011 --- \n"
		//	"***************************************************\n"
		//	"\n"
		//	"  Syntax: TextFormatter -f 'FileToFormat' [-h] [-o 'OutputFilename'] [-command0 -command1 ...]\n"
		//	"    -f : The file to format\n"
		//	"    -o : Output filename\n"
		//	"    -h : Display this help text\n"
		//	"\n"
		//	"  note: the -f option must be given before any commands. Commands are applied in the order given\n"
		//	"\n"
		//	"  Commands:\n"
		//	"    -newlines min max   : Set limits on the number of successive new lines\n"
		//	"    -lineends CRLF      : Replace line ends with CR, LF, CRLF, or LFCR\n"
		//	//"    -no_hungarian       : Replace hundarian variable names with lower case variable names\n"
		//	// NEW_COMMAND - add a help string
		//	);
	}
	
	// Parse the command line
	bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		try
		{
			if (pr::str::EqualI(option, "-f") && arg != arg_end) { m_in_file  = *arg++; m_src = new pr::script::FileSrc(m_in_file.c_str()); return true; }
			if (pr::str::EqualI(option, "-o") && arg != arg_end) { m_out_file = *arg++; return true; }
			if (pr::str::EqualI(option, "-h")                  ) { ShowHelp(); return false; }
			if (!m_src) { printf("Error: the -f option must be given before any commands\n"); return false; }
			if (pr::str::EqualI(option, "-newlines"  )) { m_src = new Newlines(m_src, arg, arg_end); return true; }
			// NEW_COMMAND - add option
			
			printf("Error: Unknown option '%s'\n", option.c_str());
			return false;
		}
		catch (std::exception const& e)
		{
			printf("Error: %s", e.what());
			return false;
		}
	}
	
	// Entry point
	int Run(std::string args)
	{
		// Parse the command line
		if (!pr::EnumCommandLine(args.c_str(), *this)) { return -1; }
		if (!m_src) { printf("No source file given\n"); return -1; }
		
		bool replace_infile = m_out_file.empty();
		if (replace_infile) m_out_file = m_in_file + ".tmp";
		
		// Run the formatters over the input file
		{std::cout << "Running formatting...";
			std::ofstream ofile(m_out_file.c_str());
			if (ofile.bad()) { printf("Failed to create output file '%s'\n", m_out_file.c_str()); return -1; }
			for (pr::script::Src& src(*m_src); *src; ++src)
				ofile << *src;
			m_src.reset();
		}std::cout << "done\n";
		
		if (replace_infile && !pr::filesys::ReplaceFile(m_out_file, m_in_file))
		{
			DWORD last = GetLastError();
			std::cout << "Failed to replace " << m_in_file << " with " << m_out_file << ". Error code: " << last << "\n";
		}
		
		return 0;
	}
};
*/
// Entry point *********************************************************************************************
	
//// Run as a windows program so that the console window is not shown
//int __stdcall WinMain(HINSTANCE,HINSTANCE,LPSTR lpCmdLine,int)
//{
//	Main m;
//	return m.Run(lpCmdLine);
//}
//	
//int main(int argc, char* argv[])
//{
//	Main m;
//	std::string args;
//	for (int i = 1; i < argc; ++i) args.append(argv[i]).append(" ");
//	return m.Run(args);
//}
