//********************************
// Ldraw Script Text Serialiser
//  Copyright (c) Rylogic Ltd 2025
//********************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"
#include "pr/view3d-12/ldraw/ldraw_parsing.h"

namespace pr::rdr12::ldraw
{
	#pragma region Read

#if 0 // TODO
	struct TextReader : IReader
	{
		// Return the current location in the source
		virtual Location Loc() const = 0;

		// Move into a nested section
		virtual void PushSection() = 0;

		// Leave the current nested section
		virtual void PopSection() = 0;

		// Get the next keyword within the current section.
		// Returns false if at the end of the section
		virtual bool NextKeyword(ldraw::EKeyword& kw) = 0;

		// Search the current section, from the current position, for the given keyword.
		// Does not affect the 'current' position used by 'NextKeyword'
		virtual bool FindKeyword(ldraw::EKeyword kw) = 0;

		// True when the current position has reached the end of the current section
		virtual bool IsSectionEnd() = 0;

		// Read a utf8 string from the current section.
		// If 'has_length' is false, assume the whole section is the string.
		// If 'has_length' is true, assume the string is prefixed by its length.
		virtual string32 String(bool has_length = false) = 0;

		// Read an integral value from the current section
		virtual int64_t Int(int byte_count, int radix) = 0;

		// Read a floating point value from the current section
		virtual double Real(int byte_count) = 0;

		// Open a byte stream corresponding to 'path'
		virtual std::unique_ptr<std::istream> OpenStream(std::filesystem::path const& path) = 0;
	};
#endif
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::rdr12::ldraw
{
	PRUnitTest(LDrawTextSerialiserTests)
	{
	}
}
#endif
