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
#include <filesystem>
#include "pr/str/encoding.h"

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

	// Examines 'filepath' to guess at the file data encoding (assumes 'filepath' is a text file)
	// On return 'bom_length' is the length of the byte order mask.
	// Returns 'UTF-8' if unknown, since UTF-8 recommends not using BOMs
	inline EEncoding DetectFileEncoding(std::filesystem::path const& filepath, int& bom_length)
	{
		std::ifstream file(filepath, std::ios::binary);

		EEncoding enc;
		unsigned char bom[3];
		auto read = file.good() ? file.read(reinterpret_cast<char*>(&bom[0]), sizeof(bom)).gcount() : 0;
		if      (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) { enc = EEncoding::utf8;     bom_length = 3; }
		else if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)                   { enc = EEncoding::utf16_be; bom_length = 2; }
		else if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)                   { enc = EEncoding::utf16_le; bom_length = 2; }
		else                                                                      { enc = EEncoding::utf8;     bom_length = 0; }
		return enc;
	}
	inline EEncoding DetectFileEncoding(std::filesystem::path const& filepath)
	{
		int bom_length;
		return DetectFileEncoding(filepath, bom_length);
	}
	
	// Read the contents of a file into 'buf'
	// 'buf_enc' is how to convert the file data. The encoding in the file is automatically detected and converted to 'buf_enc'
	// 'file_enc' is used to say what the file encoding is. If equal to Text then the encoding is automatically detected
	template <typename Buf>
	inline bool FileToBuffer(std::filesystem::path const& filepath, Buf& buf, EEncoding buf_enc = EEncoding::binary, EEncoding file_enc = EEncoding::binary)
	{
		using Elem = std::decay_t<decltype(buf[0])>;

		// Ensure the file exists
		if (!std::filesystem::exists(filepath))
			return false;

		// Determine the file size in bytes
		auto size = static_cast<size_t>(std::filesystem::file_size(filepath));

		// If we need to do a text encoding conversion...
		if (buf_enc != EEncoding::binary)
		{
			// Detect the file encoding
			int bom_length = 0;
			file_enc = (file_enc == EEncoding::binary || file_enc == EEncoding::auto_detect) ? DetectFileEncoding(filepath, bom_length) : file_enc;

			// Open the file stream
			std::basic_ifstream<Elem> file(filepath, std::ios::binary);
			if (!file.good())
				return false;

			// Imbue the file if the file encoding doesn't match the buffer encoding
			switch (file_enc)
			{
			case EEncoding::ascii:
			case EEncoding::utf8:
				{
					if (buf_enc == EEncoding::ascii || buf_enc == EEncoding::utf8) {}
					else if (buf_enc == EEncoding::utf16_le)
					{
						using cvt_t = std::codecvt_utf8_utf16<wchar_t>;
						file.imbue(std::locale(file.getloc(), new cvt_t));
					}
					else if (buf_enc == EEncoding::ucs2_le)
					{
						using cvt_t = std::codecvt_utf8<wchar_t>;
						file.imbue(std::locale(file.getloc(), new cvt_t));
					}
					else
					{
						throw std::exception("todo");
					}
					break;
				}
			case EEncoding::utf16_le:
			case EEncoding::ucs2_le:
				{
					// Imbue the file if the file encoding doesn't match the buffer encoding
					if (false) {}
					else if (buf_enc == EEncoding::utf8 || buf_enc == EEncoding::ascii)
					{
						// There doesn't seem to be a codecvt instance that handles utf16 -> utf8
						// Have to read utf16 into a string then convert it to utf8
						std::wifstream wfile(filepath, std::ios::binary);
						if (!wfile.good())
							return false;

						using cvt_t = std::codecvt_utf16<wchar_t, 0x10ffff, std::codecvt_mode::little_endian>;
						wfile.imbue(std::locale(wfile.getloc(), new cvt_t));

						// Skip the BOM
						wfile.seekg(bom_length, std::ios::beg);
						size -= bom_length;

						// Read with formatting
						if (size != 0)
						{
							std::wstring tmp(size, 0);
							auto read = wfile.read(tmp.data(), size).gcount();
							tmp.resize(static_cast<size_t>(read));

							std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>, wchar_t> converter;
							auto u8 = converter.to_bytes(tmp);
							auto ptr = reinterpret_cast<Elem const*>(u8.data());
							assert(sizeof(Elem) == sizeof(char));
							buf.insert(end(buf), ptr, ptr + u8.size());
						}
						return true;

						// Tried:
						//using cvt_t = std::codecvt_utf8_utf16<wchar_t>;
						//using cvt_t = std::codecvt_utf8_utf16<char>;
						//using cvt_t = std::codecvt<char16_t, Elem, std::mbstate_t>;
						//using cvt_t = std::codecvt_utf8<wchar_t>;
						//using cvt_t = std::codecvt_utf16<wchar_t, 0x10ffff, std::codecvt_mode::little_endian>;
						//using cvt_t = std::codecvt_utf16<char16_t, 0x10ffff, std::codecvt_mode::little_endian>;
						//file.imbue(std::locale(file.getloc(), new cvt_t));
					}
					else if (buf_enc == EEncoding::utf16_le || buf_enc == EEncoding::ucs2_le)
					{
						using cvt_t = std::codecvt_utf16<Elem, 0x10ffff, std::codecvt_mode::little_endian>;
						file.imbue(std::locale(file.getloc(), new cvt_t));
					}
					else if (buf_enc == EEncoding::utf16_be || buf_enc == EEncoding::ucs2_be)
					{
						using cvt_t = std::codecvt_utf16<Elem>;
						file.imbue(std::locale(file.getloc(), new cvt_t));
					}
					else
					{
						throw std::exception("todo");
					}
					break;
				}
			default:
				{
					throw std::exception("todo");
				}
			}

			// Skip the BOM
			file.seekg(bom_length, std::ios::beg);
			size -= bom_length;

			// Read with formatting
			if (size != 0)
			{
				auto initial = buf.size();
				buf.resize(size_t(initial + size));
				auto read = file.read(reinterpret_cast<Elem*>(&buf[initial]), size).gcount();
				buf.resize(size_t(initial + read));
			}
		}
		else
		{
			// Open the file stream
			std::ifstream file(filepath, std::ios::binary);
			if (!file.good())
				return false;

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
	template <typename Buf>
	inline Buf FileToBuffer(std::filesystem::path const& filepath, EEncoding buf_enc = EEncoding::binary, EEncoding file_enc = EEncoding::binary)
	{
		Buf buf;
		if (!FileToBuffer(filepath, buf, buf_enc, file_enc)) throw std::exception("Failed to read file");
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
	inline bool BufferToFile(void const* buf, size_t size, std::filesystem::path const& filepath, EEncoding file_enc = EEncoding::binary, EEncoding buf_enc = EEncoding::binary, bool append = false, bool add_bom = false)
	{
		// Open the output file stream
		std::ofstream file(filepath, std::ios::binary|(append ? std::ios::app : 0));
		if (!file.good())
			return false;

		if (file_enc != EEncoding::binary)
		{
			// Add the byte order mask
			if (add_bom && file_enc != EEncoding::auto_detect)
			{
				unsigned char bom[3] = {}; int bom_length = 0;
				if      (file_enc == EEncoding::utf8)     { bom[0] = 0xEF; bom[1] = 0xBB; bom[2] = 0xBF; bom_length = 3; }
				else if (file_enc == EEncoding::utf16_le) { bom[0] = 0xFF; bom[1] = 0xFE; bom_length = 2; }
				else if (file_enc == EEncoding::utf16_be) { bom[0] = 0xFE; bom[1] = 0xFF; bom_length = 2; }
				else throw std::exception("Cannot write the byte order mask for an unknown text encoding");
				if (!file.write(reinterpret_cast<char const*>(&bom[0]), bom_length).good())
					throw std::exception("Failed to write the byte order mask");
			}

			// Imbue the file stream if the buffer encoding doesn't match the file encoding
			if (file_enc != buf_enc)
			{
				switch (file_enc)
				{
				case EEncoding::utf8:
					{
						if (false) {}
						else if (buf_enc == EEncoding::utf16_le)
						{
							using cvt_t = std::codecvt<char16_t, char, std::mbstate_t>;
							file.imbue(std::locale(file.getloc(), new cvt_t));
						}
						else
						{
							throw std::exception("todo");
						}
						break;
					}
				case EEncoding::utf16_le:
					{
						if (false) {}
						else if (buf_enc == EEncoding::utf8)
						{
							using cvt_t = std::codecvt_utf16<wchar_t, 0x10ffff, std::codecvt_mode::little_endian>;
							file.imbue(std::locale(file.getloc(), new cvt_t));
						}
						else
						{
							throw std::exception("todo");
						}
					}
				default:
					{
						throw std::exception("todo");
					}
				}
			}
		}

		// Write the data to the file
		file.write(static_cast<char const*>(buf), size);
		return true;
	}

	// Write a buffer to a file.
	// 'ofs' and 'count' are the sub-range to write (in units of 'Elem')
	template <typename Elem>
	inline bool BufferToFile(Elem const* buf, size_t ofs, size_t count, std::filesystem::path const& filepath, EEncoding file_enc = EEncoding::binary, EEncoding buf_enc = EEncoding::binary, bool append = false, bool add_bom = false)
	{
		return BufferToFile(buf+ofs, count*sizeof(Elem), filepath, file_enc, buf_enc, append, add_bom);
	}

	// Write a buffer to a file.
	// 'ofs' and 'count' are the sub-range to write (in units of 'Elem')
	template <typename Buf, typename Elem = Buf::value_type>
	inline bool BufferToFile(Buf const& buf, size_t ofs, size_t len, std::filesystem::path const& filepath, EEncoding file_enc = EEncoding::binary, EEncoding buf_enc = EEncoding::binary, bool append = false, bool add_bom = false)
	{
		return buf.empty()
			? BufferToFile((void const*)nullptr, 0U, filepath, buf_enc, file_enc, append, add_bom)
			: BufferToFile(&buf[0], ofs, len, filepath, file_enc, buf_enc, append, add_bom);
	}
	template <typename Buf, typename = Buf::value_type>
	inline bool BufferToFile(Buf const& buf, std::filesystem::path const& filepath, EEncoding file_enc = EEncoding::binary, EEncoding buf_enc = EEncoding::binary, bool append = false, bool add_bom = false)
	{
		return BufferToFile(buf, 0, buf.size(), filepath, file_enc, buf_enc, append, add_bom);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/common/scope.h"
#include "pr/filesys/filesys.h"
namespace pr::filesys
{
	PRUnitTest(FileTests)
	{
		std::string filepath = "test.txt";
		auto cleanup = pr::CreateScope([]{}, [&]{ pr::filesys::EraseFile(filepath); });

		{// Write binary - Read binary
			{// write bytes
				unsigned char const data[] = {'0','1','2','3','4','5'};
				BufferToFile(data, 0, _countof(data), filepath, EEncoding::binary);

				{// Read binary data into 'std::vector<byte>', no conversion
					auto read = FileToBuffer<std::vector<unsigned char>>(filepath, EEncoding::binary);
					PR_CHECK(read.size() == sizeof(data), true);
					PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
				}
				{// Read binary data into 'std::wstring', no conversion
					auto read = FileToBuffer<std::wstring>(filepath, EEncoding::binary);
					PR_CHECK(read.size() == (sizeof(data) + 1)/2, true);
					PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
				}
			}
			{// write !bytes
				unsigned short const data[] = {'0','1','2','3','4','5'};
				BufferToFile(data, 0, _countof(data), filepath, EEncoding::binary);

				{// Read binary data into 'std::vector<byte>', no conversion
					auto read = FileToBuffer<std::vector<unsigned char>>(filepath, EEncoding::binary);
					PR_CHECK(read.size() == sizeof(data), true);
					PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
				}
				{// Read binary data into 'std::wstring', no conversion
					auto read = FileToBuffer<std::wstring>(filepath, EEncoding::binary);
					PR_CHECK(read.size() == sizeof(data)/2, true);
					PR_CHECK(memcmp(&read[0], data, sizeof(data)) == 0, true);
				}
			}
		}
		{// Write UTF-8 text
			unsigned char const utf8[] = { 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd, '\n', 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd }; // 'ni hao'
			wchar_t const utf16[] = { 0x4f60, 0x597d, '\n', 0x4f60, 0x597d };

			BufferToFile(utf8, 0, _countof(utf8), filepath, EEncoding::utf8, EEncoding::utf8, false, true);
			PR_CHECK(DetectFileEncoding(filepath) == EEncoding::utf8, true);

			{// Read UTF-8 - BOM automatically stripped
				auto read = FileToBuffer<std::string>(filepath, EEncoding::utf8);
				PR_CHECK(read.size() == _countof(utf8), true);
				PR_CHECK(memcmp(&read[0], utf8, sizeof(utf8)) == 0, true);
			}
			{// Read UTF-8 to UTF-16 - BOM automatically stripped
				auto read = FileToBuffer<std::wstring>(filepath, EEncoding::utf16_le);
				PR_CHECK(read.size() == _countof(utf16), true);
				PR_CHECK(memcmp(&read[0], utf16, sizeof(utf16)) == 0, true);
			}
			{// Read UTF-8 to UCS2 - BOM automatically stripped
				auto read = FileToBuffer<std::wstring>(filepath, EEncoding::ucs2_le);
				PR_CHECK(read.size() == _countof(utf16), true);
				PR_CHECK(memcmp(&read[0], utf16, sizeof(utf16)) == 0, true);
			}
		}
		{// Write UTF-16 text
			unsigned char const utf8[] = { 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd, '\n', 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd }; // 'ni hao'
			wchar_t const utf16[] = { 0x4f60, 0x597d, '\n', 0x4f60, 0x597d };
			wchar_t const utf16be[] = { 0x604f, 0x7d59, 0x0A00, 0x604f, 0x7d59 };

			BufferToFile(utf16, 0, _countof(utf16), filepath, EEncoding::utf16_le, EEncoding::utf16_le, false, true);
			PR_CHECK(DetectFileEncoding(filepath) == EEncoding::utf16_le, true);

			{// Read UTF-16 to UTF-16 - BOM automatically stripped
				auto read = FileToBuffer<std::wstring>(filepath, EEncoding::utf16_le);
				PR_CHECK(read.size() == _countof(utf16), true);
				PR_CHECK(memcmp(&read[0], utf16, sizeof(utf16)) == 0, true);
			}
			{// Read UTF-16 to UTF-16be - BOM automatically stripped
				auto read = FileToBuffer<std::wstring>(filepath, EEncoding::utf16_be);
				PR_CHECK(read.size() == _countof(utf16be), true);
				PR_CHECK(memcmp(&read[0], utf16be, sizeof(utf16be)) == 0, true);
			}
			{// Read UTF-16 to UTF-8 - BOM automatically stripped
				auto read = FileToBuffer<std::string>(filepath, EEncoding::utf8);
				PR_CHECK(read.size() == _countof(utf8), true);
				PR_CHECK(memcmp(&read[0], utf8, sizeof(utf8)) == 0, true);
			}
		}
		{// todo...
		}
	}
}
#endif
