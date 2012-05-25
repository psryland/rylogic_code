//**************************************************************************
//
//	Header File Data
//
//**************************************************************************
// Converts a file into a byte list or literal string

#include "pr/common/guid.h"
#include "pr/common/fmt.h"
#include "pr/common/command_line.h"
#include "pr/str/prstring.h"
#include "pr/filesys/fileex.h"
#include "pr/storage/nugget_file/nuggetfile.h"

using namespace pr;
using namespace pr::cmdline;

struct Main : IOptionReceiver
{
	std::string	m_filename;
	std::string m_output_filename;
	bool		m_binary;
	bool		m_verbose;

	Main()
	:m_filename			("")
	,m_output_filename	("")
	,m_binary			(true)
	,m_verbose			(false)
	{}

	bool CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
	{
		if( str::EqualI(option, "-F") && arg != arg_end )	{ m_filename = *arg++;			return true; }
		if( str::EqualI(option, "-O") && arg != arg_end )	{ m_output_filename = *arg++;	return true; }
		if( str::EqualI(option, "-T") )					{ m_binary = false;				return true; }
		if( str::EqualI(option, "-V") )					{ m_verbose = true;				return true; }
		printf("Unknown command line option: '%s'\n", option.c_str());
		ShowHelp();
		return false;
	}
	void ShowHelp()
	{
		printf(	"\n"
				"*********************************************************\n"
				" --- Header File Data - Copyright © Rylogic 2005 ---\n"
				"*********************************************************\n"
				"\n"
				" Syntax:\n"
				"   HeaderFileData -F \"filename\" -O \"output_filename.h\" -T\n"
				"   -F : the filename to dump into the header\n"
				"   -O : the name of the header file to create\n"
				"   -T : output text data into the header. Default binary output\n"
				"   -V : verbose output\n"
			   );
	}

	//*****
	// Main program run
	int run(int argc, char* argv[])
	{
		if( !EnumCommandLine(argc, argv, *this) )				{ ShowHelp(); return -1; }
		if( m_filename.empty() || m_output_filename.empty() )	{ printf("In/Out filenames not provided\n"); ShowHelp(); return -1; }
		
		// Open the source file
		pr::Handle in_file = FileOpen(m_filename.c_str(), EFileOpen::Reading);
		if (in_file == INVALID_HANDLE_VALUE)					{ printf("Failed to open the source file: '%s'\n", m_filename.c_str()); return -1; }

		// Open the output file
		pr::Handle out_file = FileOpen(m_output_filename.c_str(), EFileOpen::Writing);
		if (out_file == INVALID_HANDLE_VALUE)					{ printf("Failed to open the output file: '%s'\n", m_output_filename.c_str()); return -1; }

		if( m_binary )
		{
			WriteBinary(in_file, out_file);
			if( m_verbose ) printf("Output binary header data: '%s'\n", m_output_filename.c_str());
		}
		else
		{
			WriteText(in_file, out_file);
			if( m_verbose ) printf("Output text header data: '%s'\n", m_output_filename.c_str());
		}		
		return 0;
	}

	// Write out binary header file data
	void WriteBinary(pr::Handle& in_file, pr::Handle& out_file)
	{
		// Write the data
		const uint BytesPerLine = 16;
		unsigned char buffer[BytesPerLine + 1];
		DWORD i, bytes_read;
		do
		{
			std::string line;
			FileRead(in_file, buffer, BytesPerLine, &bytes_read);
			for( i = 0; i != bytes_read; ++i )
			{
				line += Fmt("0x%2.2x, ", buffer[i]);
				if( (i % 4) == 3 ) line += " ";
				if( (i % 8) == 7 ) line += " ";

				// Convert the buffer element to a character
				if( !isalnum(buffer[i]) ) { buffer[i] = '.'; }
			}
			buffer[i] = 0;

			// Add the comments
			if( !line.empty() ) line += Fmt("// %s\n", reinterpret_cast<char*>(buffer));

			// Write the line
			FileWrite(out_file, line.c_str());
		}
		while( bytes_read == BytesPerLine );
	}

	// Write out text header file data
	void WriteText(pr::Handle& in_file, pr::Handle& out_file)
	{
		const uint BlockReadSize = 4096;
		char buffer[BlockReadSize];
		DWORD bytes_read;
		do
		{
			std::string line = "\"";
			FileRead(in_file, buffer, BlockReadSize, &bytes_read);
			for( char *c = buffer, *c_end = buffer + bytes_read; c != c_end; ++c )
			{
				switch( *c )
				{
				case '\a':	line += "\\a"; break;
				case '\b':	line += "\\b"; break;
				case '\f':	line += "\\f"; break;
				case '\n':	line += "\\n\"\n\""; break;
				case '\r':	line += "\\r"; break;
				case '\t':	line += "\\t"; break;
				case '\v':	line += "\\v"; break;
				case '\\':	line += "\\\\"; break;
				case '\?':	line += "\\?"; break;
				case '\'':	line += "\\'"; break;
				case '"':	line += "\\\""; break;
				default:	line += *c;
				}
			}
			line += "\"";

			// Write the line
			FileWrite(out_file, line.c_str());
		}
		while( bytes_read == BlockReadSize );
		FileWrite(out_file, ";");
	}
};

int main(int argc, char* argv[])
{
	Main m;
	return m.run(argc, argv);
}
