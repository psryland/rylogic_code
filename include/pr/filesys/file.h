//***********************************
//  File
//  Copyright (c) Rylogic Ltd 2003
//***********************************
// This header uses standard libraries only

#pragma once

#include <stdio.h>
#include <fstream>
#include <locale>
#include <codecvt>

namespace pr
{
	// Scoped C-style file pointer wrapper
	struct FilePtr
	{
		FILE* m_fp;

		FilePtr(FILE* fp) :m_fp(fp)   {}
		~FilePtr()                    { if (m_fp) fclose(m_fp); } 
		operator FILE*&()             { return m_fp; }
		operator FILE* const&() const { return m_fp; }
	};

	// How to interpret the data in a file when reading it
	enum class EFileData
	{
		Binary,   // binary data, read all bytes as is
		Text,     // A text encoding, attempts to auto detect
		Utf8,     // UTF-8 text
		Utf16,    // UTF-16 text
		Utf16_be, // UTF-16 Big Endian text
	};

	// Read the contents of a file into 'buf'
	template <typename Buf, typename String> inline bool FileToBuffer(String const& filepath, Buf& buf, size_t ofs = 0, size_t len = ~size_t())
	{
		// Open the file stream
		std::ifstream file(filepath, std::ios::binary|std::ios::ate);
		if (!file.good())
			return false;

		// Determine the file size in bytes
		auto size = std::min(size_t(file.tellg()) - ofs, len);
		file.seekg(ofs, std::ios::beg);

		// Read the file contents into the buffer and return it
		if (size != 0)
		{
			using Elem = std::remove_reference_t<decltype(buf[0])>;
			auto size_in_elems = (size + sizeof(Elem) - 1) / sizeof(Elem);

			auto initial = buf.size();
			buf.resize(initial + size_in_elems);
			file.read(reinterpret_cast<char*>(&buf[initial]), size);
		}
		return true;
	}
	template <typename Buf, typename String> inline Buf FileToBuffer(String const& filepath, size_t ofs = 0, size_t len = ~size_t())
	{
		Buf buf;
		if (!FileToBuffer(filepath, buf, ofs, len)) throw std::exception("Failed to read file");
		return std::move(buf);
	}
	
	// Write a buffer to a file.
	// 'ofs' and 'len' are in units of bytes
	template <typename String> inline bool BufferToFile(void const* buf, size_t ofs, size_t len, String const& filepath, bool append = false)
	{
		// Open the output file stream
		std::ofstream file(filepath, std::ios::binary|(append ? std::ios::app : 0));
		if (!file.good())
			return false;

		// Write the data to the file
		file.write(static_cast<char const*>(buf) + ofs, len);
		return true;
	}

	// Write a buffer to a file
	// 'ofs' and 'len' are in units of 'Elem'
	template <typename Buf, typename String, typename Elem = Buf::value_type>
	inline bool BufferToFile(Buf const& buf, size_t ofs, size_t len, String const& filepath, bool append = false)
	{
		return buf.empty()
			? BufferToFile(nullptr, 0U, 0U, filepath, append)
			: BufferToFile(&buf[0], ofs * sizeof(Elem), len * sizeof(Elem), filepath, append);
	}

	// Write a buffer to a file
	template <typename Buf, typename String, typename Elem = Buf::value_type>
	inline bool BufferToFile(Buf const& buf, String const& filepath, bool append = false)
	{
		return BufferToFile(buf, 0, buf.size(), filepath, append);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_filesys_file)
		{
		}
	}
}
#endif
