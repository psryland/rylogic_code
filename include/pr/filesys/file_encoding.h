//**********************************************
// File path/File system operations
//  Copyright (c) Rylogic Ltd 2009
//**********************************************
#pragma once
#include <fstream>
#include <filesystem>
#include "pr/str/encoding.h"

namespace pr::filesys
{
	// Examines 'filepath' to guess at the file data encoding (assumes 'filepath' is a text file)
	// On return 'bom_size' is the length of the byte order mask.
	// Returns 'UTF-8' if unknown, since UTF-8 recommends not using BOMs
	inline EEncoding DetectFileEncoding(std::filesystem::path const& filepath, int& bom_size)
	{
		std::ifstream file(filepath, std::ios::binary);

		unsigned char bom[3];
		auto read = file.good() ? file.read(reinterpret_cast<char*>(&bom[0]), sizeof(bom)).gcount() : 0;
		if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
		{
			bom_size = 3;
			return EEncoding::utf8;
		}
		if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
		{
			bom_size = 2;
			return EEncoding::utf16_be;
		}
		if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
		{
			bom_size = 2;
			return EEncoding::utf16_le;
		}

		// Assume UTF-8 unless we find some invalid UTF-8 sequences
		bom_size = 0;
		auto char_ofs = 0;

		// Scan the file (or some of it) to look for invalid UTF-8 sequences
		unsigned char buf[4096];
		constexpr int jend = 0x100000 / sizeof(buf);
		for (int j = 0, margin = 0; j != jend && file.good(); ++j)
		{
			// Read a chunk of the file.
			auto count = file.read(reinterpret_cast<char*>(&buf[margin]), sizeof(buf) - margin).gcount();

			// Scan up to sizeof(buf) - 8 to handle multi-byte characters than span the buffer boundary.
			std::streamsize i = 0, iend = std::min<std::streamsize>(count, sizeof(buf) - 8);
			for (;i < iend; ++i, ++char_ofs)
			{
				auto c =
					(buf[i] & 0b10000000) == 0          ? 0 : // ASCII character, 0 continuation bytes
					(buf[i] & 0b11100000) == 0b11000000 ? 1 : // 2-byte UTF-8 character, 1 continuation byte
					(buf[i] & 0b11110000) == 0b11100000 ? 2 : // 3-byte UTF-8 character, 2 continuation bytes
					(buf[i] & 0b11111000) == 0b11110000 ? 3 : // 4-byte UTF-8 character, 3 continuation bytes
					-1;                                       // Not a valid UTF-8 character

				if (c == 0)
					continue;

				// Continuation bytes should be 0b10xxxxxx for valid UTF-8
				for (; c > 0 && (buf[++i] & 0b11000000) == 0b10000000; --c) {}

				// Not valid UTF-8, assume extended ASCII
				if (c != 0)
					return EEncoding::ascii_extended;
			}

			// Reached the end of the file?
			if (count != static_cast<std::streamsize>(sizeof(buf) - margin))
				break;

			// Copy the non-scanned bytes to the start of buf and go round again
			margin = static_cast<int>(sizeof(buf) - i);
			memmove(&buf[0], &buf[i], margin);
		}
		return EEncoding::utf8;
	}
	inline EEncoding DetectFileEncoding(std::filesystem::path const& filepath)
	{
		int bom_size;
		return DetectFileEncoding(filepath, bom_size);
	}
}
