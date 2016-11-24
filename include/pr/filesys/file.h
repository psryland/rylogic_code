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
#include <type_traits>

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
		Ucs2,     // wide wchar_t text
	};

	// Examines 'filepath' to guess at the file data encoding (assumes 'filepath' is a text file)
	// On return 'bom_length' is the length of the byte order mask.
	// Returns 'UTF-8' if unknown, since UTF-8 recommends not using BOMs
	template <typename String> inline EFileData DetectFileEncoding(String const& filepath, int& bom_length)
	{
		std::ifstream file(filepath, std::ios::binary);

		EFileData enc;
		unsigned char bom[3];
		auto read = file.good() ? file.read(reinterpret_cast<char*>(&bom[0]), sizeof(bom)).gcount() : 0;
		if      (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) { enc = EFileData::Utf8;     bom_length = 3; }
		else if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)                   { enc = EFileData::Utf16_be; bom_length = 2; }
		else if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)                   { enc = EFileData::Utf16;    bom_length = 2; }
		else                                                                      { enc = EFileData::Utf8;     bom_length = 0; }
		return enc;
	}
	template <typename String> inline EFileData DetectFileEncoding(String const& filepath)
	{
		int bom_length;
		return DetectFileEncoding(filepath, bom_length);
	}
	
	// Read the contents of a file into 'buf'
	// 'buf_enc' is how to convert the file data. The encoding in the file is automatically detected and converted to 'buf_enc'
	// 'file_enc' is used to say what the file encoding is. If equal to Text then the encoding is automatically detected
	// 'ofs' and 'len' are in units of 'Buf::value_type'
	template <typename Buf, typename String> inline bool FileToBuffer(String const& filepath, Buf& buf, EFileData buf_enc = EFileData::Binary, EFileData file_enc = EFileData::Binary, size_t ofs = 0, size_t len = ~size_t())
	{
		using Elem = std::remove_reference_t<decltype(buf[0])>;

		// If we need to do a text encoding conversion...
		if (buf_enc != EFileData::Binary)
		{
			// Detect the file encoding
			int bom_length = 0;
			file_enc = (file_enc == EFileData::Binary || file_enc == EFileData::Text) ? DetectFileEncoding(filepath, bom_length) : file_enc;

			std::locale* loc = nullptr;
			static std::locale global_locale;
			switch (file_enc)
			{
			case EFileData::Utf8:
				{
					if      (buf_enc == EFileData::Ucs2)     { static std::locale locale(global_locale, new std::codecvt_utf8<Elem>); loc = &locale; }
					else if (buf_enc == EFileData::Utf16)    { throw std::exception("todo"); }
					else if (buf_enc == EFileData::Utf16_be) { throw std::exception("todo"); }
					break;
				}
			case EFileData::Utf16:
				{
					 throw std::exception("todo");
				}
			case EFileData::Utf16_be:
				{
					 throw std::exception("todo");
				}
			case EFileData::Ucs2:
				{
					if      (buf_enc == EFileData::Utf8)     { static std::locale locale(global_locale, new std::codecvt_utf8<Elem>); loc = &locale; }
					else if (buf_enc == EFileData::Utf16)    { static std::locale locale(global_locale, new std::codecvt_utf16<Elem, 0x10ffff, std::little_endian>); loc = &locale; }
					else if (buf_enc == EFileData::Utf16_be) { static std::locale locale(global_locale, new std::codecvt_utf16<Elem>); loc = &locale; }
					break;
				}
			}

			// Open the file stream
			std::basic_ifstream<Elem> file(filepath, std::ios::binary|std::ios::ate);
			if (!file.good())
				return false;

			// Determine the file size in bytes
			auto size = std::min(size_t(file.tellg()) - ofs, len);
			file.seekg(ofs, std::ios::beg);
			file.seekg(bom_length, std::ios::cur); // skip the BOM
			size -= bom_length;

			// Read with formatting
			if (size != 0)
			{
				if (loc) file.imbue(*loc);
				auto initial = buf.size();
				buf.resize(size_t(initial + size + 1)); // Reads n-1 chars then adds the null terminator
				auto read = file.read(reinterpret_cast<Elem*>(&buf[initial]), size).gcount();
				buf.resize(size_t(initial + read));
			}
		}
		else
		{
			// Open the file stream
			std::ifstream file(filepath, std::ios::binary|std::ios::ate);
			if (!file.good())
				return false;

			// Determine the file size in bytes
			auto size = std::min(size_t(file.tellg()) - ofs, len);
			file.seekg(ofs, std::ios::beg);

			// Read unformatted
			if (size != 0)
			{
				auto initial = buf.size();
				buf.resize(size_t(initial + (size + sizeof(Elem) - 1) / sizeof(Elem)));
				auto read = file.read(reinterpret_cast<char*>(&buf[initial]), size).gcount();
				buf.resize(size_t(initial + (read + sizeof(Elem) - 1) / sizeof(Elem)));
			}
		}
		return true;
	}
	template <typename Buf, typename String> inline Buf FileToBuffer(String const& filepath, EFileData buf_enc = EFileData::Binary, EFileData file_enc = EFileData::Binary, size_t ofs = 0, size_t len = ~size_t())
	{
		Buf buf;
		if (!FileToBuffer(filepath, buf, buf_enc, file_enc, ofs, len)) throw std::exception("Failed to read file");
		return std::move(buf);
	}
	
	// Write a buffer to a file.
	// 'buf' points to the contiguous block of data to write
	// 'size' is the length to write (in bytes)
	// 'filepath' is the name of the file to create
	// 'file_enc' describes the encoding to be written to the file.
	// 'buf_enc' describes the encoding used in 'buf'
	// 'append' is true if the file should be appended to
	// 'add_bom' is true if a byte order mask should be written to the file (applies to text encoding only, prefer not for UTF-8)
	template <typename String> inline bool BufferToFile(void const* buf, size_t size, String const& filepath, EFileData file_enc = EFileData::Binary, EFileData buf_enc = EFileData::Binary, bool append = false, bool add_bom = false)
	{
		// Open the output file stream
		std::ofstream file(filepath, std::ios::binary|(append ? std::ios::app : 0));
		if (!file.good())
			return false;

		if (file_enc != EFileData::Binary)
		{
			// Add the byte order mask
			if (add_bom && file_enc != EFileData::Text)
			{
				unsigned char bom[3] = {}; int bom_length = 0;
				if      (file_enc == EFileData::Utf8)     { bom[0] = 0xEF; bom[1] = 0xBB; bom[2] = 0xBF; bom_length = 3; }
				else if (file_enc == EFileData::Utf16_be) { bom[0] = 0xFE; bom[1] = 0xFF; bom_length = 2; }
				else if (file_enc == EFileData::Utf16)    { bom[0] = 0xFF; bom[1] = 0xFE; bom_length = 2; }
				else throw std::exception("Cannot write the byte order mask for an unknown text encoding");
				if (!file.write(reinterpret_cast<char const*>(&bom[0]), bom_length).good())
					throw std::exception("Failed to write the byte order mask");
			}

			std::locale* loc = nullptr;
			static std::locale global_locale;
			switch (file_enc)
			{
			case EFileData::Utf8:
				{
					if      (buf_enc == EFileData::Ucs2)     { static std::locale locale(global_locale, new std::codecvt_utf8<char>); loc = &locale; }
					else if (buf_enc == EFileData::Utf16_be) { throw std::exception("todo"); }
					else if (buf_enc == EFileData::Utf16)    { throw std::exception("todo"); }
					break;
				}
			case EFileData::Utf16:
			case EFileData::Utf16_be:
			case EFileData::Ucs2:
				{
					throw std::exception("todo");
				}
			}
			if (loc) file.imbue(*loc);
		}

		// Write the data to the file
		file.write(static_cast<char const*>(buf), size);
		return true;
	}

	// Write a buffer to a file.
	// 'ofs' and 'count' are the sub-range to write (in units of 'Elem')
	template <typename Elem, typename String>
	inline bool BufferToFile(Elem const* buf, size_t ofs, size_t count, String const& filepath, EFileData file_enc = EFileData::Binary, EFileData buf_enc = EFileData::Binary, bool append = false, bool add_bom = false)
	{
		return BufferToFile(buf+ofs, count*sizeof(Elem), filepath, file_enc, buf_enc, append, add_bom);
	}

	// Write a buffer to a file.
	// 'ofs' and 'count' are the sub-range to write (in units of 'Elem')
	template <typename Buf, typename String, typename Elem = Buf::value_type>
	inline bool BufferToFile(Buf const& buf, size_t ofs, size_t len, String const& filepath, EFileData file_enc = EFileData::Binary, EFileData buf_enc = EFileData::Binary, bool append = false, bool add_bom = false)
	{
		return buf.empty()
			? BufferToFile((void const*)nullptr, 0U, filepath, buf_enc, file_enc, append, add_bom)
			: BufferToFile(&buf[0], ofs, len, filepath, file_enc, buf_enc, append, add_bom);
	}
	template <typename Buf, typename String, typename = Buf::value_type>
	inline bool BufferToFile(Buf const& buf, String const& filepath, EFileData file_enc = EFileData::Binary, EFileData buf_enc = EFileData::Binary, bool append = false, bool add_bom = false)
	{
		return BufferToFile(buf, 0, buf.size(), filepath, file_enc, buf_enc, append, add_bom);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/scope.h"
#include "pr/filesys/filesys.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_filesys_file)
		{
			std::string filepath = "test.txt";
			auto cleanup = pr::CreateScope([]{}, [&]{ pr::filesys::EraseFile(filepath); });

			{// Write binary - Read binary
				{// write bytes
					unsigned char data[] = {'0','1','2','3','4','5'};
					BufferToFile(data, 0, _countof(data), filepath, EFileData::Binary);

					{// Read binary data into 'std::vector<byte>', no conversion
						auto read = FileToBuffer<std::vector<unsigned char>>(filepath, EFileData::Binary);
						PR_CHECK(read.size() == sizeof(data), true);
						PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
					}
					{// Read binary data into 'std::wstring', no conversion
						auto read = FileToBuffer<std::wstring>(filepath, EFileData::Binary);
						PR_CHECK(read.size() == (sizeof(data) + 1)/2, true);
						PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
					}
				}
				{// write !bytes
					unsigned short data[] = {'0','1','2','3','4','5'};
					BufferToFile(data, 0, _countof(data), filepath, EFileData::Binary);

					{// Read binary data into 'std::vector<byte>', no conversion
						auto read = FileToBuffer<std::vector<unsigned char>>(filepath, EFileData::Binary);
						PR_CHECK(read.size() == sizeof(data), true);
						PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
					}
					{// Read binary data into 'std::wstring', no conversion
						auto read = FileToBuffer<std::wstring>(filepath, EFileData::Binary);
						PR_CHECK(read.size() == sizeof(data)/2, true);
						PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
					}
				}
			}
			{// Write UTF-8 text
				unsigned char utf8[] = {0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd, '\n', 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd}; // 'ni hao (chinesse)'
				wchar_t ucs2[] = {0x4f60, 0x597d, '\n', 0x4f60, 0x597d};

				BufferToFile(utf8, 0, _countof(utf8), filepath, EFileData::Utf8, EFileData::Utf8, false, true);
				PR_CHECK(DetectFileEncoding(filepath) == EFileData::Utf8, true);

				{// Read UTF-8 - BOM automatically stripped
					auto read = FileToBuffer<std::string>(filepath, EFileData::Utf8);
					PR_CHECK(read.size() == _countof(utf8), true);
					PR_CHECK(memcmp(&read[0], utf8, sizeof(utf8)) == 0, true);
				}
				{// Read UTF-8 to UCS2 - BOM automatically stripped
					auto read = FileToBuffer<std::wstring>(filepath, EFileData::Ucs2);
					PR_CHECK(read.size() == _countof(ucs2), true);
					PR_CHECK(memcmp(&read[0], ucs2, sizeof(ucs2)) == 0, true);
				}

				//todo
				BufferToFile(ucs2, 0, _countof(ucs2), filepath, EFileData::Utf8, EFileData::Ucs2, false, false);
				PR_CHECK(DetectFileEncoding(filepath) == EFileData::Utf8, true);

				//{// Read UTF-8 - BOM automatically stripped
				//	auto read = FileToBuffer<std::string>(filepath, EFileData::Utf8);
				//	PR_CHECK(read.size() == _countof(utf8), true);
				//	PR_CHECK(memcmp(&read[0], utf8, sizeof(utf8)) == 0, true);
				//}
				//{// Read UTF-8 to UCS2 - BOM automatically stripped
				//	auto read = FileToBuffer<std::wstring>(filepath, EFileData::Ucs2);
				//	PR_CHECK(read.size() == _countof(ucs2), true);
				//	PR_CHECK(memcmp(&read[0], ucs2, sizeof(ucs2)) == 0, true);
				//}

			}
			{// todo...
			}
		}
	}
}
#endif

