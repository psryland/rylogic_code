//**********************************
// EEncoding
//  Copyright (c) Rylogic Ltd 2019
//**********************************
#pragma once

namespace pr
{
	// String encodings
	enum class EEncoding
	{
		// Notes:
		// - The ISO standard, Unicode 6.0, says that the latest code point is 0x10FFFF
		// - The means char32_t encoding is fixed with because each all characters fit within an i32.
		// - UCS2 and UTF-16 are the same on ranges [0,0xD800) and [0xE000,0xFFFE). Values in the range
		//   [0xD800,0xDE00) are high surrogates, values in the range [0xDC00,0xE000) are low surrogates.
		//   UCS2 surrogate pairs are invalid UTF-16 encodings.
		// - UCS4 and UTF-32 are the same thing.

		// No encoding
		none,
		binary = none,

		// Values in the range [0, 128)
		ascii,

		// 0b0xxxxxxx (1-byte sequence)
		// 0b110xxxxx, 0b10xxxxxx (2-byte sequence)
		// 0b1110xxxx, 0b10xxxxxx, 0b10xxxxxx (3-byte sequence)
		// 0b11110xxx, 0b10xxxxxx, 0b10xxxxxx, 0b10xxxxxx (4-byte sequence)
		utf8,

		// 0bxxxxxxxx_xxxxxxxx (2-byte sequence) (excluding surrogates)
		// 0b11011xxx_xxxxxxxx, 0b110111xx_xxxxxxxx (4-byte sequence)
		utf16_le,
		utf16_be,

		// Values in the range [0, 0x10FFFF]
		utf32,

		// Values in the range [0, 0xFFFF]
		// Legacy, avoid if possible
		ucs2_le,
		ucs2_be,

		// Used with files, detect the encoding from the BOM
		auto_detect,

		// Used to allow pass through of encoding
		already_decoded,
	};
}
