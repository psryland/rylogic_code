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

	bool HData::CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
	{
		if (pr::str::EqualI(option, "-f") && arg != arg_end) { m_src = *arg++; return true; }
		if (pr::str::EqualI(option, "-o") && arg != arg_end) { m_dst = *arg++; return true; }
		if (pr::str::EqualI(option, "-t")) { m_binary = false; return true; }
		if (pr::str::EqualI(option, "-v")) { m_verbose = true; return true; }
		return ICex::CmdLineOption(option, arg, arg_end);
	}

	bool HData::CmdLineData(TArgIter& arg, TArgIter arg_end)
	{
		return ICex::CmdLineData(arg, arg_end);
	}

	// Write out binary header file data
	void WriteBinary(std::ifstream& in_file, std::ofstream& out_file)
	{
		// Write the data
		int const BytesPerLine = 16;
		unsigned char buffer[BytesPerLine + 1];
		DWORD i;
		for (;;)
		{
			auto bytes_read = in_file.read(pr::char_ptr(&buffer[0]), BytesPerLine).gcount();
			if (bytes_read == 0)
				break;

			std::string line;
			for (i = 0; i != bytes_read; ++i)
			{
				line += pr::Fmt("0x%2.2x, ", buffer[i]);
				if ((i % 4) == 3) line += " ";
				if ((i % 8) == 7) line += " ";

				// Convert the buffer element to a character
				if (!isalnum(buffer[i]))
					buffer[i] = '.';
			}
			buffer[i] = 0;

			// Add the comments
			if (!line.empty())
				line += pr::Fmt("// %s\n", reinterpret_cast<char*>(buffer));

			// Write the line
			out_file.write(line.data(), line.size());
		}
	}

	// Write out text header file data
	void WriteText(std::ifstream& in_file, std::ofstream& out_file)
	{
		int const BlockReadSize = 4096;
		std::array<char, BlockReadSize> buffer;

		for (;;)
		{
			auto bytes_read = in_file.read(buffer.data(), BlockReadSize).gcount();
			if (bytes_read == 0)
				break;

			std::string line = "\"";
			for (char *c = buffer.data(), *c_end = c + bytes_read; c != c_end; ++c)
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
			out_file.write(line.data(), line.size());
		}
		out_file.write(";", 1);
	}

	int HData::Run()
	{
		if (m_src.empty()) { printf("No source filepath provided\n"); return -1; }
		if (m_dst.empty()) { printf("No output filepath provided\n"); return -1; }

		// Open the source file
		std::ifstream in_file(m_src);
		if (!in_file)
			throw std::runtime_error(pr::FmtS("Failed to open the source file: '%S'\n", m_src.c_str()));

		// Open the output file
		std::ofstream out_file(m_dst);
		if (!out_file)
			throw std::runtime_error(pr::FmtS("Failed to open the output file: '%S'\n", m_dst.c_str()));

		if (m_binary)
		{
			WriteBinary(in_file, out_file);
			if (m_verbose) printf("Output binary header data: '%S'\n", m_dst.c_str());
		}
		else
		{
			WriteText(in_file, out_file);
			if (m_verbose) printf("Output text header data: '%S'\n", m_dst.c_str());
		}
		return 0;
	}
}
