//********************************
// Ldraw Script Binary Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#include "pr/view3d-12/ldraw/ldraw_serialiser_binary.h"

namespace pr::rdr12::ldraw
{
	ByteReader::ByteReader(std::istream& src, std::filesystem::path src_filepath)
		: m_src(src)
		, m_section()
		, m_src_filepath(src_filepath)
	{}

	// Return the current location in the source
	Location ByteReader::Loc() const
	{
		return Location{
			.m_filepath = m_src_filepath,
			.m_offset = m_src.tellg(),
		};
	}

	// Move into a nested section
	void ByteReader::PushSection()
	{
	}

	// Leave the current nested section
	void ByteReader::PopSection()
	{
	}

	// Get the next keyword within the current section.
	// Returns false if at the end of the section
	bool ByteReader::NextKeyword(ldraw::EKeyword& kw)
	{
	}

	// Search the current section, from the current position, for the given keyword.
	// Does not affect the 'current' position used by 'NextKeyword'
	bool ByteReader::FindKeyword(ldraw::EKeyword kw)
	{
	}

	// True when the current position has reached the end of the current section
	bool ByteReader::IsSectionEnd()
	{
	}

	// Read a utf8 string from the current section.
	// If 'has_length' is false, assume the whole section is the string.
	// If 'has_length' is true, assume the string is prefixed by its length.
	string32 ByteReader::String(bool has_length = false)
	{
	}

	// Read an integral value from the current section
	int64_t ByteReader::Int(int byte_count, int radix)
	{
	}

	// Read a floating point value from the current section
	double ByteReader::Real(int byte_count)
	{
	}

	// Open a byte stream corresponding to 'path'
	std::unique_ptr<std::istream> ByteReader::OpenStream(std::filesystem::path const& path)
	{
	}

}