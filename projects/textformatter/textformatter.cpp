//**********************************************************
// A command line tool for formatting text
//  (c)opyright 2002 Paul Ryland
//**********************************************************
#include <memory>
#include <string>
#include <exception>
#include <iostream>
#include "pr/common/command_line.h"
#include "pr/common/prtypes.h"
#include "pr/maths/maths.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/filesys.h"
#include "pr/str/prstring.h"
#include "pr/script/char_stream.h"

typedef std::unique_ptr<pr::script::Src> SrcPtr;

// Strip/Insert new lines.
struct Newlines :pr::script::Src
{
	SrcPtr m_src;
	pr::script::Buffer<> m_buf;
	pr::uint m_lines_min;
	pr::uint m_lines_max;

	Newlines(Src* src, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
		:m_src(src)
		,m_buf(*m_src)
		,m_lines_min(0)
		,m_lines_max(0xFFFFFFFF)
	{
		if (arg_end - arg < 2) throw std::exception("<newlines> insufficient arguments");
		m_lines_min = pr::str::as<pr::uint>(*arg++, 0, 10);
		m_lines_max = pr::str::as<pr::uint>(*arg++, 0, 10);
	}

protected:
	char peek() const { return *m_buf; }
	void next()       { ++m_buf; }
	void seek()
	{
		for (;;)
		{
			if (!m_buf.empty() || *m_buf != '\n') break;

			// Look for consecutive lines that contain only whitespace characters
			// If the number of lines is less than 'm_lines_min' add lines up to 'm_lines_min'
			// If the number of lines is greater than 'm_lines_max' delete lines back to 'm_lines_max'
			pr::uint line_count = 0;
			for (; *m_buf.m_src;)
			{
				if (*m_buf.m_src == '\n')               { m_buf.clear(); ++line_count; ++m_buf.m_src; }
				else if (pr::str::IsLineSpace(*m_buf.m_src)) { m_buf.buffer(); }
				else break;
			}
			for (line_count = pr::Clamp(line_count, m_lines_min, m_lines_max); line_count; --line_count)
			{
				m_buf.push_front('\n');
			}
			break;
		}
	}
};

//
struct Main :pr::cmdline::IOptionReceiver
{
	std::string  m_in_file;
	std::string  m_out_file;
	SrcPtr       m_src;

	Main()
		:m_in_file()
		,m_out_file()
		,m_src()
	{}

	// Show the main help
	void ShowHelp()
	{
		printf(
			"\n"
			"***************************************************\n"
			" --- Text Formatter - Copyright © Rylogic 2011 --- \n"
			"***************************************************\n"
			"\n"
			"  Syntax: TextFormatter -f 'FileToFormat' [-h] [-o 'OutputFilename'] [-command0 -command1 ...]\n"
			"    -f : The file to format\n"
			"    -o : Output filename\n"
			"    -h : Display this help text\n"
			"\n"
			"  note: the -f option must be given before any commands. Commands are applied in the order given\n"
			"\n"
			"  Commands:\n"
			"    -newlines min max   : Set limits on the number of successive new lines\n"
			"    -lineends CRLF      : Replace line ends with CR, LF, CRLF, or LFCR\n"
			//"    -no_hungarian       : Replace hundarian variable names with lower case variable names\n"
			// NEW_COMMAND - add a help string
		);
	}

	// Parse the command line
	bool CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		try
		{
			if (pr::str::EqualI(option, "-f") && arg != arg_end) { m_in_file  = *arg++; m_src.reset(new pr::script::FileSrc(m_in_file.c_str())); return true; }
			if (pr::str::EqualI(option, "-o") && arg != arg_end) { m_out_file = *arg++; return true; }
			if (pr::str::EqualI(option, "-h")) { ShowHelp(); return false; }
			if (!m_src) { printf("Error: the -f option must be given before any commands\n"); return false; }
			if (pr::str::EqualI(option, "-newlines")) { m_src.reset(new Newlines(m_src.release(), arg, arg_end)); return true; }
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
		if (!pr::filesys::FileExists(m_in_file)) { printf("Source file doesn't exist\n"); return -1; }

		bool replace_infile = m_out_file.empty();
		if (replace_infile) m_out_file = m_in_file + ".tmp";

		// Run the formatters over the input file
		{
			std::cout << "Running formatting...";
			std::ofstream ofile(m_out_file.c_str());
			if (ofile.bad()) { printf("Failed to create output file '%s'\n", m_out_file.c_str()); return -1; }
			for (pr::script::Src& src(*m_src); *src; ++src)
				ofile << *src;
			m_src.reset();
		} std::cout << "done\n";

		if (replace_infile && !pr::filesys::ReplaceFile(m_out_file, m_in_file))
		{
			DWORD last = GetLastError();
			std::cout << "Failed to replace " << m_in_file << " with " << m_out_file << ". Error code: " << last << "\n";
		}

		return 0;
	}
};

// Entry point *********************************************************************************************

// Run as a windows program so that the console window is not shown
int __stdcall WinMain(HINSTANCE,HINSTANCE,LPSTR lpCmdLine,int)
{
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

/// Strip consecutive newlines
//struct NewlineStrip :IFormatter
//{
//  uint m_max_new_lines;
//
//  NewlineStrip(FormatterPtr src, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
//  {
//  }
//
//  void Do(std::istream& src, std::ostream& dst)
//  {
//  max_new_lines = strtoul(arg->c_str(), 0, 10); ++arg
//  }
//};

//// Convert hungarian notation to something readable
//struct Hungarian :IFormatter
//{
//  void Do(std::istream& src, std::ostream& dst) {}
//  //void AddTabs(std::string& text_out, uint num_tabs)
//  //{
//  //  for( uint i = 0; i < num_tabs; ++i ) text_out.push_back('\t');
//  //}
//
//  //// Format text
//  //void Format(const std::string& text_in, std::string& text_out)
//  //{
//  //  bool add_white_space = false;
//  //  //bool add_newline_false;
//  //  bool on_newline_start = true;
//  //  int tab_depth = 0;
//  //  for( std::string::const_iterator c = text_in.begin(), c_end = text_in.end(); c != c_end; ++c )
//  //  {
//  //      // Newline? tab in to the correct depth
//  //      if( *c == '\n' )
//  //      {
//  //          if( !on_newline_start )
//  //          {
//  //              text_out.push_back('\n');
//  //              AddTabs(text_out, tab_depth);
//  //              on_newline_start = true;
//  //          }
//  //      }
//  //      else
//  //      // White space? Remember that whitespace is needed
//  //      if( isspace(*c) )
//  //      {
//  //          add_white_space = true;
//  //      }
//  //      else
//  //      if( *c == '{' )
//  //      {
//  //          ++tab_depth;
//
//  //          if( !on_newline_start )
//  //          {
//  //              text_out.push_back('\n');
//  //              AddTabs(text_out, tab_depth - 1);
//  //          }
//  //          text_out.push_back('{');
//  //          text_out.push_back('\n');
//  //          AddTabs(text_out, tab_depth);
//  //          add_white_space = false;
//  //          on_newline_start = true;
//  //      }
//  //      else
//  //      if( *c == '}' )
//  //      {
//  //          if( --tab_depth < 0 ) tab_depth = 0;
//
//  //          if( !on_newline_start )
//  //          {
//  //              text_out.push_back('\n');
//  //              AddTabs(text_out, tab_depth);
//  //          }
//  //          else if( !text_out.empty() )
//  //          {
//  //              text_out.resize(text_out.size() - 1);
//  //          }
//  //          text_out.push_back('}');
//  //          text_out.push_back('\n');
//  //          AddTabs(text_out, tab_depth);
//  //          add_white_space = false;
//  //          on_newline_start = true;
//  //      }
//  //      else
//  //      {
//  //          if( add_white_space ) text_out.push_back(' ');
//  //          text_out.push_back(*c);
//  //          add_white_space = false;
//  //          on_newline_start = false;
//  //      }
//  //  }
//  //}
//};
