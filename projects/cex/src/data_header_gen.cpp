//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"
#include "cex/src/data_header_gen.h"

namespace cex
{
	HData::HData()
		:m_src()
		,m_dst()
		,m_binary(true)
		,m_verbose(false)
	{}

	void HData::ShowHelp() const
	{
		std::cout <<
			"Convert a source file into a C/C++ compatible header file\n"
			" Syntax: Cex -hdata -f src_file -o output_header_file [-t] [-v]\n"
			"  -f   : the input file to be converted\n"
			"  -o   : the output header file to generate\n"
			"  -t   : output text data in the header (instead of binary data)\n"
			"  -v   : verbose output\n";
	}

	bool HData::CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		if (pr::str::EqualI(option, "-f") && arg != arg_end) { m_src = *arg++; return true; }
		if (pr::str::EqualI(option, "-o") && arg != arg_end) { m_dst = *arg++; return true; }
		if (pr::str::EqualI(option, "-t")) { m_binary = false; return true; }
		if (pr::str::EqualI(option, "-v")) { m_verbose = true; return true; }
		return ICex::CmdLineOption(option, arg, arg_end);
	}

	bool HData::CmdLineData(pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		return ICex::CmdLineData(arg, arg_end);
	}

	// Write out binary header file data
	void WriteBinary(pr::Handle& in_file, pr::Handle& out_file)
	{
		// Write the data
		pr::uint const BytesPerLine = 16;
		unsigned char buffer[BytesPerLine + 1];
		DWORD i, bytes_read;
		do
		{
			std::string line;
			pr::FileRead(in_file, buffer, BytesPerLine, &bytes_read);
			for (i = 0; i != bytes_read; ++i)
			{
				line += pr::Fmt("0x%2.2x, ", buffer[i]);
				if ((i % 4) == 3) line += " ";
				if ((i % 8) == 7) line += " ";

				// Convert the buffer element to a character
				if (!isalnum(buffer[i])) { buffer[i] = '.'; }
			}
			buffer[i] = 0;

			// Add the comments
			if (!line.empty()) line += pr::Fmt("// %s\n", reinterpret_cast<char*>(buffer));

			// Write the line
			pr::FileWrite(out_file, line.c_str());
		}
		while (bytes_read == BytesPerLine);
	}

	// Write out text header file data
	void WriteText(pr::Handle& in_file, pr::Handle& out_file)
	{
		pr::uint const BlockReadSize = 4096;
		char buffer[BlockReadSize];
		DWORD bytes_read;
		do
		{
			std::string line = "\"";
			pr::FileRead(in_file, buffer, BlockReadSize, &bytes_read);
			for (char *c = buffer, *c_end = buffer + bytes_read; c != c_end; ++c)
			{
				switch (*c)
				{
				default: line += *c;
				case '\a': line += "\\a"; break;
				case '\b': line += "\\b"; break;
				case '\f': line += "\\f"; break;
				case '\n': line += "\\n\"\n\""; break;
				case '\r': line += "\\r"; break;
				case '\t': line += "\\t"; break;
				case '\v': line += "\\v"; break;
				case '\\': line += "\\\\"; break;
				case '\?': line += "\\?"; break;
				case '\'': line += "\\'"; break;
				case  '"': line += "\\\""; break;
				}
			}
			line += "\"";

			// Write the line
			pr::FileWrite(out_file, line.c_str());
		}
		while (bytes_read == BlockReadSize);
		pr::FileWrite(out_file, ";");
	}

	int HData::Run()
	{
		if (m_src.empty()) { printf("No source filepath provided\n"); return -1; }
		if (m_dst.empty()) { printf("No output filepath provided\n"); return -1; }

		// Open the source file
		pr::Handle in_file = pr::FileOpen(m_src.c_str(), pr::EFileOpen::Reading);
		if (in_file == INVALID_HANDLE_VALUE) { printf("Failed to open the source file: '%s'\n", m_src.c_str()); return -1; }

		// Open the output file
		pr::Handle out_file = pr::FileOpen(m_dst.c_str(), pr::EFileOpen::Writing);
		if (out_file == INVALID_HANDLE_VALUE) { printf("Failed to open the output file: '%s'\n", m_dst.c_str()); return -1; }

		if (m_binary)
		{
			WriteBinary(in_file, out_file);
			if (m_verbose) printf("Output binary header data: '%s'\n", m_dst.c_str());
		}
		else
		{
			WriteText(in_file, out_file);
			if (m_verbose) printf("Output text header data: '%s'\n", m_dst.c_str());
		}
		return 0;
	}
}
