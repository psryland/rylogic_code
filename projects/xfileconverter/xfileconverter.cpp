//************************************************************************
// XFile Converter
//  Copyright © Rylogic Ltd 2010
//************************************************************************
// A command line tool for converting xfiles to/from text to binary

#include "pr/common/assert.h"
#include "pr/common/command_line.h"
#include "pr/str/prstring.h"
#include "pr/storage/xfile/xfile.h"

using namespace pr;
using namespace pr::xfile;
using namespace pr::cmdline;

struct Options : IOptionReceiver
{
	EConvert::Type m_convert;
	std::string    m_input_filename;
	std::string    m_output_filename;
	bool           m_silent;

	Options()
	:m_convert(EConvert::Bin)
	,m_input_filename("")
	,m_output_filename("")
	,m_silent(false)
	{}
	bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
	{
		if (str::EqualI(option, "-I") && arg != arg_end) { m_input_filename  = *arg++;          return true; }
		if (str::EqualI(option, "-O") && arg != arg_end) { m_output_filename = *arg++;          return true; }
		if (str::EqualI(option, "-bin"))                 { m_convert = EConvert::Bin;           return true; }
		if (str::EqualI(option, "-txt"))                 { m_convert = EConvert::Txt;           return true; }
		if (str::EqualI(option, "-compressedbin"))       { m_convert = EConvert::CompressedBin; return true; }
		if (str::EqualI(option, "-S"))                   { m_silent = true;                     return true; }
		printf("Error: Unknown option '%s'\n", option.c_str());
		ShowHelp();
		return false;
	}
	void ShowHelp()
	{
		printf("\n"
			   "***************************************************\n"
			   " --- XFile Converter - Copyright © Rylogic 2005 ---\n"
			   "***************************************************\n"
			   "\n"
			   "   Syntax: XFileConverter -bin|-txt|-compressedbin -I 'Filename' -O 'Filename' [-V]\n"
			   "    -bin : Convert x files to binary format\n"
			   "    -txt : Convert x files to text format\n"
			   "    -compressedbin : Convert x files to compressed binary format\n"
			   "    -I : Input x file filename\n"
			   "    -O : Output x file filename (cannot be the same as the input filename)\n"
			   "    -S : Silent\n"
			   );
	}
	bool Valid() const
	{
		return !m_input_filename.empty() && !m_output_filename.empty();
	}
};

//*****
int main(int argc, char* argv[])
{
	Options options;
	if( !EnumCommandLine(argc, argv, options) || !options.Valid() )
	{
		options.ShowHelp();
		return -1;
	}

	xfile::EResult result = xfile::Convert(options.m_input_filename.c_str(), options.m_output_filename.c_str(), options.m_convert);
	if( Failed(result) )
	{
		if( !options.m_silent ) { printf("%s -> %s failed.\n", options.m_input_filename.c_str(), options.m_output_filename.c_str()); }
		return -1;
	}
	if( !options.m_silent ) { printf("%s -> %s successful.\n", options.m_input_filename.c_str(), options.m_output_filename.c_str()); }
	return 0;
}
