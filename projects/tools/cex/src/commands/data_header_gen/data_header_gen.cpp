//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
#include "src/forward.h"

namespace cex
{
	struct Cmd_HData
	{
		void ShowHelp() const
		{
			std::cout <<
				"Convert a source file into a C/C++ compatible header file\n"
				" Syntax: Cex -hdata -f src_file -o output_header_file [-t] [-v]\n"
				"  -f   : the input file to be converted\n"
				"  -o   : the output header file to generate\n"
				"  -t   : output text data in the header (instead of binary data)\n"
				"  -v   : verbose output\n";
		}
		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::filesystem::path src;
			if (args.count("f") != 0)
			{
				src = args("f").as<std::string>();
			}
			std::filesystem::path dst;
			if (args.count("o") != 0)
			{
				dst = args("o").as<std::string>();
			}
			bool binary = true;
			if (args.count("t") != 0)
			{
				binary = false;
			}
			bool verbose = false;
			if (args.count("v") != 0)
			{
				verbose = true;
			}

			if (src.empty()) { std::cerr << "No source filepath provided\n"; return -1; }
			if (dst.empty()) { std::cerr << "No output filepath provided\n"; return -1; }

			// Open the source file
			std::ifstream in_file(src);
			if (!in_file)
				throw std::runtime_error(std::format("Failed to open the source file: '{}'\n", src.string()));

			// Open the output file
			std::ofstream out_file(dst);
			if (!out_file)
				throw std::runtime_error(std::format("Failed to open the output file: '{}'\n", dst.string()));

			if (binary)
			{
				WriteBinary(in_file, out_file);
				if (verbose) std::cout << "Output binary header data: '" << dst << "'\n";
			}
			else
			{
				WriteText(in_file, out_file);
				if (verbose) std::cout << "Output text header data: '" << dst << "'\n";
			}
			return 0;
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
					line += std::format("0x{:02x}, ", buffer[i]);
					if ((i % 4) == 3) line += " ";
					if ((i % 8) == 7) line += " ";

					// Convert the buffer element to a character
					if (!isalnum(buffer[i]))
						buffer[i] = '.';
				}
				buffer[i] = 0;

				// Add the comments
				if (!line.empty())
					line += std::format("// {}\n", reinterpret_cast<char*>(buffer));

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
				for (char* c = buffer.data(), *c_end = c + bytes_read; c != c_end; ++c)
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
	};

	int HData(pr::CmdLine const& args)
	{
		Cmd_HData cmd;
		return cmd.Run(args);
	}
}
