//**********************************
// Extract
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <type_traits>
#include <cerrno>
#include "pr/str/string_core.h"

namespace pr
{
	namespace str
	{
		// Notes:
		// Functions ending with 'C' don't advance the character source pointer
		// errno is used for wcstoul,etc conversions. errno is thread local so is thread safe

		#pragma region Extract Utility Functions

		// A helper type that takes a pointer to a char stream and outputs a wchar_t stream
		template <typename Ptr> struct wchar_ptr
		{
			Ptr& m_ptr;
			wchar_ptr(Ptr& ptr) :m_ptr(ptr) {}
			wchar_ptr(wchar_ptr const&) = delete;
			wchar_ptr& operator =(wchar_ptr const&) = delete;
			wchar_t    operator *() const       { return wchar_t(*m_ptr); }
			wchar_t    operator [](int i) const { return wchar_t(m_ptr[i]); }
			wchar_ptr& operator ++()            { ++m_ptr; return *this; }
			wchar_ptr  operator ++(int)         { auto this_ = *this; ++m_ptr; return this_; }
			wchar_ptr& operator +=(int count)   { m_ptr += count; return *this; }
		};

		// Advance 'src' while 'pred' is true
		// Returns true if the function returned due to 'pred' returning false, false if *src == 0
		template <typename Ptr, typename Pred> inline bool Advance(Ptr& src, Pred pred)
		{
			// Find the first non-delimiter
			for (;*src && pred(*src); ++src) {}
			return *src != 0;
		}

		// Advance 'src' to the next delimiter character
		// Returns false if *src == 0
		template <typename Ptr, typename Char = char_type_t<Ptr>> inline bool AdvanceToDelim(Ptr& src, Char const* delim)
		{
			// Advance while *src does not point to a delimiter
			return Advance(src, [=](Char ch){ return *FindChar(delim, ch) == 0; });
		}

		// Advance 'src' to the next non-delimiter character
		// Returns false if *src == 0
		template <typename Ptr, typename Char = char_type_t<Ptr>> inline bool AdvanceToNonDelim(Ptr& src, Char const* delim)
		{
			// Advance while *src points to a delimiter
			return Advance(src, [=](Char ch){ return *FindChar(delim, ch) != 0; });
		}
		#pragma endregion

		#pragma region Extract Line
		// Extract a contiguous block of characters upto (and possibly including) a new line character
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractLine(Str& line, Ptr& src, bool inc_cr, Char const* newline = nullptr)
		{
			if (newline == nullptr) newline = PR_STRLITERAL(Char,"\n");
			auto len = Length(line);
			for (;*src && *FindChar(newline, *src) == 0; Append(line, len++, *src), ++src) {}
			if (*src && inc_cr) { Append(line, len++, *src); ++src; }
			return true;
		}
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractLineC(Str& line, Ptr src, bool inc_cr, Char const* newline = nullptr)
		{
			return ExtractLine(line, src, inc_cr, newline);
		}
		#pragma endregion

		#pragma region Extract Token
		// Extract a contiguous block of non-delimiter characters from 'src'
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractToken(Str& token, Ptr& src, Char const* delim = nullptr)
		{
			delim = Delim(delim);

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// Copy up to the next delimiter
			auto len = Length(token);
			for (Append(token, len++, *src), ++src; *src && *FindChar(delim, *src) == 0; Append(token, len++, *src), ++src) {}
			return true;
		}
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractTokenC(Str& token, Ptr src, Char const* delim = nullptr)
		{
			return ExtractToken(token, src, delim);
		}
		#pragma endregion

		#pragma region Extract Identifier
		// Extract a contiguous block of identifier characers from 'src' incrementing 'src'
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractIdentifier(Str& id, Ptr& src, Char const* delim = nullptr)
		{
			delim = Delim(delim);

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// If the first non-delimiter is not a valid identifier character, then we can't extract an identifier
			if (!IsIdentifier(*src, true))
				return false;

			// Copy up to the first non-identifier character
			auto len = Length(id);
			for (Append(id, len++, *src), ++src; *src && IsIdentifier(*src, false); Append(id, len++, *src), ++src) {}
			return true;
		}
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractIdentifierC(Str& id, Ptr src, Char const* delim = nullptr)
		{
			return ExtractIdentifier(id, src, delim);
		}
		#pragma endregion

		#pragma region Extract String
		// Extract a quoted (") string
		// if 'escape' is not 0, it is treated as the escape character
		// if 'quote' is not nullptr, it is treated as the accepted quote characters
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractString(Str& str, Ptr& src, char escape = 0, Char const* quotes = nullptr, Char const* delim = nullptr)
		{
			delim = Delim(delim);

			// Set the accepted quote characters
			if (quotes == nullptr)
			{
				static Char const default_quotes[] = {'\"', '\'', 0};
				quotes = default_quotes;
			}

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// If the next character is not an acceptable quote, then this isn't a string
			auto quote = *src;
			if (FindChar(quotes, quote) != 0) ++src; else return false;

			// Copy the string
			auto len = Length(str);
			if (auto esc_chr = Char(escape))
			{
				// Copy to the closing ", allowing for the escape character
				for (; *src && *src != quote; ++src)
				{
					if (*src == esc_chr)
					{
						switch (*++src)
						{
						default: break;
						case 'a':  Append(str, len++, '\a'); break;
						case 'b':  Append(str, len++, '\b'); break;
						case 'f':  Append(str, len++, '\f'); break;
						case 'n':  Append(str, len++, '\n'); break;
						case 'r':  Append(str, len++, '\r'); break;
						case 't':  Append(str, len++, '\t'); break;
						case 'v':  Append(str, len++, '\v'); break;
						case '\'': Append(str, len++, '\''); break;
						case '\"': Append(str, len++, '\"'); break;
						case '\\': Append(str, len++, '\\'); break;
						case '\?': Append(str, len++, '\?'); break;
						case '0':
						case '1':
						case '2':
						case '3':
							{
								// ascii character in octal
								wchar_t oct[9] = {};
								for (int i = 0; i != 8 && IsOctDigit(*src); ++i, ++src) oct[i] = wchar_t(*src);
								Append(str, len++, Char(::wcstoul(oct, nullptr, 8)));
								break;
							}
						case 'x':
							{
								// ascii or unicode character in hex
								wchar_t hex[9] = {};
								for (int i = 0; i != 8 && IsHexDigit(*src); ++i, ++src) hex[i] = wchar_t(*src);
								Append(str, len++, Char(::wcstoul(hex, nullptr, 16)));
								break;
							}
						}
					}
					else
					{
						Append(str, len++, *src);
					}
				}
			}
			else
			{
				// Copy to the next "
				for (; *src && *src != quote; ++src)
					Append(str, len++, *src);
			}

			// If the string doesn't end with a ", then it's not a valid string
			if (*src == quote) ++src; else return false;
			return true;
		}
		template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractStringC(Str& str, Ptr src, char escape = 0, Char const* delim = nullptr)
		{
			return ExtractString(str, src, escape, delim);
		}
		#pragma endregion

		#pragma region Extract Bool
		// Extract a boolean from 'src'
		// Expects 'src' to point to a string of the following form:
		// [delim]{0|1|true|false}
		// The first character that does not fit this form stops the scan.
		// '0','1' must be followed by a non-identifier character
		// 'true', 'false' can have any case
		template <typename Bool, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractBool(Bool& bool_, Ptr& src, Char const* delim = nullptr)
		{
			delim = Delim(delim);

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// Convert a char to a lower case wchar_t
			auto lwr = [](Char ch){ return char_traits<wchar_t>::lwr(wchar_t(ch)); };

			// Extract the boolean
			switch (lwr(*src))
			{
			default : return false;
			case L'0': bool_ = static_cast<Bool>(false); return !IsIdentifier(*++src, false);
			case L'1': bool_ = static_cast<Bool>(true ); return !IsIdentifier(*++src, false);
			case L't': bool_ = static_cast<Bool>(true ); return lwr(*++src) == L'r' && lwr(*++src) == L'u' && lwr(*++src) == L'e'                        && !IsIdentifier(*++src, false);
			case L'f': bool_ = static_cast<Bool>(false); return lwr(*++src) == L'a' && lwr(*++src) == L'l' && lwr(*++src) == L's' && lwr(*++src) == L'e' && !IsIdentifier(*++src, false);
			}
		}
		template <typename Bool, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractBoolC(Bool& bool_, Ptr src, Char const* delim = nullptr)
		{
			return ExtractBool(bool_, src, delim);
		}
		template <typename Bool, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractBoolArray(Bool* bool_, size_t count, Ptr& src, Char const* delim = nullptr)
		{
			while (count--) if (!ExtractBool(*bool_++, src, delim)) return false;
			return true;
		}
		template <typename Bool, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractBoolArrayC(Bool* bool_, size_t count, Ptr src, Char const* delim = nullptr)
		{
			return ExtractBoolArray(bool_, count, src, delim);
		}
		#pragma endregion

		#pragma region Extract Int
		// Extract an integral number from 'src' (basically strtol)
		// Expects 'src' to point to a string of the following form:
		// [delim] [{+|–}][0[{x|X|b|B}]][digits]
		// The first character that does not fit this form stops the scan.
		// If 'radix' is between 2 and 36, then it is used as the base of the number.
		// If 'radix' is 0, the initial characters of the string are used to determine the base.
		// If the first character is 0 and the second character is not 'x' or 'X', the string is interpreted as an octal integer;
		// otherwise, it is interpreted as a decimal number. If the first character is '0' and the second character is 'x' or 'X',
		// the string is interpreted as a hexadecimal integer. If the first character is '1' through '9', the string is interpreted
		// as a decimal integer. The letters 'a' through 'z' (or 'A' through 'Z') are assigned the values 10 through 35; only letters
		// whose assigned values are less than 'radix' are permitted.
		template <typename Int, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractInt(Int& intg, int radix, Ptr& src, Char const* delim = nullptr)
		{
			errno = 0;
			delim = Delim(delim);

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			wchar_t str[256] = {};
			int i = 0;

			// Read the character stream as wchars.
			// I'm casting up to wchar_t because, if the stream has multibyte chars,
			// casting down to char could wrap and produce a silently accepted value.
			wchar_ptr<Ptr> wsrc(src);

			// Read the optional sign
			if (*wsrc == L'+' || *wsrc == L'-')
			{
				str[i++] = *wsrc;
				++wsrc;
			}

			// Look for radix identifiers prefixing the number
			// If the radix if given, allow the number to have the associated radix prefix
			if (radix == 0)
			{
				if (*wsrc == L'0')
				{
					++wsrc; // Don't need to add leading zeros
					if      (*wsrc == L'x' || *wsrc == L'X') { radix = 16; ++wsrc; }
					else if (*wsrc == L'b' || *wsrc == L'B') { radix =  2; ++wsrc; }
					else                                     { radix =  8; }
				}
				else if (*wsrc >= L'1' && *wsrc <= L'9')
				{
					radix = 10;
				}
				else
				{
					return false;
				}
			}
			else if (radix == 2)
			{
				if (*wsrc == L'0') { ++wsrc; wsrc += int(*wsrc == L'b' || *wsrc == L'B'); }
			}
			else if (radix == 16)
			{
				if (*wsrc == '0') { ++wsrc; wsrc += int(*wsrc == L'x' || *wsrc == L'X'); }
			}

			// Read the digits
			for (; *wsrc; ++wsrc)
			{
				int ch = ::towupper(*wsrc);
				int dec_ch = ch - L'0', hex_ch = ch - L'A';
				if (i == _countof(str)) return false; // Out of local buffer space
				if (dec_ch >= 0 && dec_ch <= 9  && dec_ch      < radix) { str[i++] = wchar_t(ch); continue; }
				if (hex_ch >= 0 && hex_ch <= 25 && hex_ch + 10 < radix) { str[i++] = wchar_t(ch); continue; }
				break;
			}

			// Convert the string to an integer.
			// Careful here. if you're reading a number larger than the max value for 'Int' you'll get MAX_VALUE
			// i.e. reading a hex value greater than 0x7FFFFFFF into an int will return 0x7FFFFFFF
			errno = 0;
			wchar_t* end;
			intg = sizeof(Int) == sizeof(long long)
				? std::is_unsigned<Int>::value
					? Int(::_wcstoui64(str, &end, radix))
					: Int(::_wcstoi64(str, &end, radix))
				: std::is_unsigned<Int>::value
					? Int(::wcstoul(str, &end, radix))
					: Int(::wcstol(str, &end, radix));

			// Check all of the string was used in the conversion and there wasn't an overflow
			return end - &str[0] == i && errno != ERANGE;
		}
		template <typename Int, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractIntC(Int& intg, int radix, Ptr src, Char const* delim = nullptr)
		{
			return ExtractInt(intg, radix, src, delim);
		}
		template <typename Int, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractIntArray(Int* intg, size_t count, int radix, Ptr& src, Char const* delim = nullptr)
		{
			while (count--) if (!ExtractInt(*intg++, radix, src, delim)) return false;
			return true;
		}
		template <typename Int, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractIntArrayC(Int* intg, size_t count, int radix, Ptr src, Char const* delim = nullptr)
		{
			return ExtractIntArray(intg, count, radix, src, delim); 
		}
		#pragma endregion

		#pragma region Extract Real
		// Extract a floating point number from 'src'
		// Expects 'src' to point to a string of the following form:
		// [delim] [{+|-}][digits][.digits][{d|D|e|E}[{+|-}]digits]
		// The first character that does not fit this form stops the scan.
		// If no digits appear before the '.' character, at least one must appear after the '.' character.
		// The decimal digits can be followed by an exponent, which consists of an introductory letter (d, D, e, or E) and an optionally signed integer.
		// If neither an exponent part nor a '.' character appears, a '.' character is assumed to follow the last digit in the string.
		template <typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractReal(Real& real, Ptr& src, Char const* delim = nullptr)
		{
			errno = 0;
			delim = Delim(delim);

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			wchar_t str[256] = {};
			int i = 0;

			// Read the character stream as wchars.
			// I'm casting up to wchar_t because, if the stream has multibyte chars,
			// casting down to char could wrap and produce a silently accepted value.
			wchar_ptr<Ptr> wsrc(src);

			// Read the optional sign
			if (*wsrc == L'+' || *wsrc == L'-')
			{
				str[i++] = *wsrc;
				++wsrc;
			}

			// Read digits up to the '.'
			for (; IsDecDigit(*wsrc); ++wsrc)
			{
				if (i == _countof(str)) return false; // Out of local buffer space
				str[i++] = *wsrc;
			}

			// If the next char is the decimal point, add it and keep reading
			if (*wsrc == L'.')
			{
				if (i == _countof(str)) return false; // Out of local buffer space
				str[i++] = *wsrc;
				++wsrc;

				// Read more digits
				for (; IsDecDigit(*wsrc); ++wsrc)
				{
					if (i == _countof(str)) return false; // Out of local buffer space
					str[i++] = *wsrc;
				}
			}

			// Look for the optional exponent character
			if (*wsrc == L'd' || *wsrc == L'D' || *wsrc == L'e' || *wsrc == L'E')
			{
				// Add the exponent char
				if (i == _countof(str)) return false; // Out of local buffer space
				str[i++] = *wsrc;
				++wsrc;

				// Read the optional exponent sign
				if (*wsrc == L'+' || *wsrc == L'-')
				{
					if (i == _countof(str)) return false; // Out of local buffer space
					str[i++] = *wsrc;
					++wsrc;
				}

				// Read the exponent digits
				for (; IsDecDigit(*wsrc); ++wsrc)
				{
					if (i == _countof(str)) return false; // Out of local buffer space
					str[i++] = *wsrc;
				}
			}

			// Convert the string to a real
			errno = 0;
			wchar_t* end;
			real = Real(::wcstod(str, &end));

			// Check all of the string was used in the conversion and there wasn't an overflow
			return end - &str[0] == i && errno != ERANGE;
		}
		template <typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractRealC(Real& real, Ptr src, Char const* delim = nullptr)
		{
			return ExtractReal(real, src, delim);
		}
		template <typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractRealArray(Real* real, size_t count, Ptr& src, Char const* delim = nullptr)
		{
			while (count--) if (!ExtractReal(*real++, src, delim)) return false;
			return true;
		}
		template <typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractRealArrayC(Real* real, size_t count, Ptr src, Char const* delim = nullptr)
		{
			return ExtractRealArray(real, count, src, delim);
		}
		#pragma endregion

		#pragma region Extract Enum
		// This is basically a convenience wrapper around ExtractInt
		template <typename Enum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnumValue(Enum& enum_, int radix, Ptr& src, Char const* delim = nullptr)
		{
			std::underlying_type<Enum>::type val;
			if (!ExtractInt(val, radix, src, delim)) return false;
			enum_ = static_cast<Enum>(val);
			return true;
		}
		template <typename Enum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnumValueC(Enum& enum_, int radix, Ptr src, Char const* delim = nullptr)
		{
			return ExtractEnum(enum_, radix, src, delim);
		}

		// Extracts an enum by its string name. For use with 'PR_ENUM' defined enums
		template <typename Enum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnum(Enum& enum_, Ptr& src, Char const* delim = nullptr)
		{
			decltype(*src) val[512] = {};
			if (!ExtractIdentifier(val, src, delim)) return false;
			enum_ = Enum::Parse(val);
			return true;
		}
		template <typename Enum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnumC(Enum& enum_, Ptr src, Char const* delim = nullptr)
		{
			return ExtractEnum(enum_, src, delim);
		}
		#pragma endregion

		#pragma region Extract Number
		// Supported radii, supporting a few non-C constant types
		enum class ENumType { Dec = 10, Hex = 16, Oct = 8, Bin = 2, FP = 0 };

		// Extract a numeric constant of unknown type.
		// Format: [delim][{+|-}][0[{x|X|b|B}]][digits][.digits][{d|D|e|E}[{+|-}]digits][U][L][L]
		// On return:
		//  'fp' indicates what type of constant was extracted (integral or floating point)
		//  'unsignd' indicates whether the number was signed or unsigned (if false is returned check errno = ERANGE)
		//  'llong' indicates whether the number was a long long (if false is returned check errno = ERANGE)
		//  'ivalue','fvalue' will contain the corresponding value and the
		//    other value will be unchanged (this allows unions to work)
		template <typename Int, typename Real, typename Ptr, typename Char = char_type_t<Ptr>> bool ExtractNumber(Int& ivalue, Real& fvalue, bool& fp, Ptr& src, bool* unsignd = nullptr, bool* llong = nullptr, Char const* delim = nullptr)
		{
			errno = 0;
			delim = Delim(delim);

			// Find the first non-delimiter
			if (!AdvanceToNonDelim(src, delim))
				return false;

			// Read the character stream as wchars.
			// I'm casting up to wchar_t because, if the stream has multibyte chars,
			// casting down to char could wrap and produce a silently accepted value.
			wchar_ptr<Ptr> wsrc(src);

			// Buffer the number string locally so that we can first read the
			// entire number then convert it appropriately
			wchar_t str[256] = {}; int i = 0;
			bool us_buf, ll_buf;
			auto& us = unsignd ? *unsignd : us_buf;
			auto& ll = llong   ? *llong   : ll_buf;

			// Initialise defaults
			fp = false;
			us = false;
			ll = false;

			// Look for the optional sign character
			// Ideally we don't want to advance 'src' passed the '+' or '-' if the next
			// character is not the start of a number. However doing so means 'src' can't
			// be a forward only input stream. Therefore, I'm pushing the responsibility
			// back to the caller, they need to check that if the next char is a '+' or '-'
			// then the following char is a decimal digit
			if (*wsrc == L'+' || *wsrc == L'-')
			{
				str[i++] = *wsrc;
				++wsrc;
			}

			// If the first digit is zero, then the number may be of a different base
			int radix = 10;
			if (*wsrc == L'0')
			{
				++wsrc; // Leading zeros don't need to be added to 'str'
				if      (*wsrc == L'.')                  { str[i++] = *wsrc; ++wsrc; fp = true; }
				else if (*wsrc == L'x' || *wsrc == L'X') { radix = 16; ++wsrc; }
				else if (*wsrc == L'b' || *wsrc == L'B') { radix =  2; ++wsrc; }
				else                                     { radix =  8; }
			}
			else if (!IsDecDigit(*wsrc))
			{
				// Numeric constants all begin with a digit
				return false;
			}

			// Read a string of digits
			for (bool dp = false, exp = false;;)
			{
				if (fp)
				{
					// Read decimal digits
					for (; *wsrc && IsDecDigit(*wsrc); ++wsrc)
					{
						if (i == _countof(str)) return false; // Out of local buffer space
						str[i++] = *wsrc;
					}
				}
				else
				{
					// Read digits with values less that 'radix'
					for (; *wsrc; ++wsrc)
					{
						int ch = ::towupper(*wsrc);
						int dec_ch = ch - L'0', hex_ch = ch - L'A';
						if (i == _countof(str)) return false; // Out of local buffer space
						if (dec_ch >= 0 && dec_ch <= 9  && dec_ch      < radix) { str[i++] = wchar_t(ch); continue; }
						if (hex_ch >= 0 && hex_ch <= 25 && hex_ch + 10 < radix) { str[i++] = wchar_t(ch); continue; }
						break;
					}
				}

				// If the next char is a decimal point, and it's the first one we've seen, add it and go round again
				if (!dp && *wsrc == L'.' && radix == 10) // may not know that it's fp yet
				{
					if (i == _countof(str)) return false; // Out of local buffer space
					str[i++] = *wsrc;
					++wsrc;
					dp = true;
					fp = true;
				}

				// If the next char is the exponent char, and it's the first one we've seen, add it and go round again
				else if (!exp && (*wsrc == L'e' || *wsrc == L'E' || *wsrc == L'd' || *wsrc == L'D') && radix == 10) // may not know that it's fp yet
				{
					if (i == _countof(str)) return false; // Out of local buffer space
					str[i++] = *wsrc;
					++wsrc;
					exp = true;
					fp = true;

					// Read the optional exponent sign
					if (*wsrc == L'+' || *wsrc == L'-')
					{
						if (i == _countof(str)) return false; // Out of local buffer space
						str[i++] = *wsrc;
						++wsrc;
					}
				}
				else
				{
					break;
				}
			}

			// Read the optional number suffixes
			if ((fp || radix == 10) && (*wsrc == L'f' || *wsrc == L'F'))
			{
				fp = true;
				++wsrc;
			}
			if (!fp && (*wsrc == L'u' || *wsrc == L'U'))
			{
				us = true;
				++wsrc;
			}
			if (!fp && (*wsrc == L'l' || *wsrc == L'L'))
			{
				++wsrc;
				ll = *wsrc == L'l' || *wsrc == L'L';
				if (ll) ++wsrc;
			}

			// Convert the string to a value
			errno = 0;
			wchar_t* end;
			if (fp)
			{
				fvalue = Real(::wcstod(str, &end));
			}
			else
			{
				ivalue = ll
					? us
						? Int(::_wcstoui64(str, &end, radix))
						: Int(::_wcstoi64(str, &end, radix))
					: us
						? Int(::wcstoul(str, &end, radix))
						: Int(::wcstol(str, &end, radix));
			}

			// Check all of the string was used in the conversion and there wasn't an overflow
			return end - &str[0] == i && errno != ERANGE;
		}
		template <typename Int, typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractNumberC(Int& ivalue, Real& fvalue, bool& fp, Ptr src, bool* unsignd = nullptr, bool* llong = nullptr, Char const* delim = nullptr)
		{
			return ExtractNumber(ivalue, fvalue, fp, src, unsignd, llong, delim);
		}

		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_str_extract)
		{
			{// Line
				using namespace pr::str;

				wchar_t const* src = L"abcefg\nhijk\nlmnop", *s;
				char         aarr[16] = {};
				wchar_t      warr[16] = {};
				std::string  astr;
				std::wstring wstr;

				s = src;
				PR_CHECK(ExtractLine(aarr, s, false) && Equal(aarr, "abcefg") && *s == L'\n', true);
				PR_CHECK(ExtractLineC(aarr, ++s, true) && Equal(aarr, "abcefghijk\n") && *s == L'h', true);

				s = src;
				PR_CHECK(ExtractLine(warr, s, false) && Equal(warr, "abcefg") && *s == L'\n', true);
				PR_CHECK(ExtractLineC(warr, ++s, true) && Equal(warr, "abcefghijk\n") && *s == L'h', true);

				s = src;
				PR_CHECK(ExtractLine(astr, s, false) && Equal(astr, "abcefg") && *s == L'\n', true);
				PR_CHECK(ExtractLineC(astr, ++s, true) && Equal(astr, "abcefghijk\n") && *s == L'h', true);

				s = src;
				PR_CHECK(ExtractLine(wstr, s, false) && Equal(wstr, "abcefg") && *s == L'\n', true);
				PR_CHECK(ExtractLineC(wstr, ++s, true) && Equal(wstr, "abcefghijk\n") && *s == L'h', true);
			}
			{// Token
				using namespace pr::str;

				wchar_t const* src = L"token1 token2:token3", *s;
				char         aarr[16] = {};
				wchar_t      warr[16] = {};
				std::string  astr;
				std::wstring wstr;

				s = src;
				PR_CHECK(ExtractToken(aarr, s) && Equal(aarr, "token1") && *s == L' ', true);
				PR_CHECK(ExtractTokenC(aarr, ++s, L" \n:") && Equal(aarr, "token1token2") && *s == L't', true);

				s = src;
				PR_CHECK(ExtractToken(warr, s) && Equal(warr, "token1") && *s == L' ', true);
				PR_CHECK(ExtractTokenC(warr, ++s, L" \n:") && Equal(warr, "token1token2") && *s == L't', true);

				s = src;
				PR_CHECK(ExtractToken(astr, s) && Equal(astr, "token1") && *s == L' ', true);
				PR_CHECK(ExtractTokenC(astr, ++s, L" \n:") && Equal(astr, "token1token2") && *s == L't', true);

				s = src;
				PR_CHECK(ExtractToken(wstr, s) && Equal(wstr, "token1") && *s == L' ', true);
				PR_CHECK(ExtractTokenC(wstr, ++s, L" \n:") && Equal(wstr, "token1token2") && *s == L't', true);
			}
			{// Identifier
				using namespace pr::str;

				wchar_t const* src = L"_ident ident2:token3", *s;
				char         aarr[16] = {};
				wchar_t      warr[16] = {};
				std::string  astr;
				std::wstring wstr;

				s = src;
				PR_CHECK(ExtractIdentifier(aarr, s) && Equal(aarr, "_ident") && *s == L' ', true);
				PR_CHECK(ExtractIdentifierC(aarr, ++s) && Equal(aarr, "_identident2") && *s == L'i', true);

				s = src;
				PR_CHECK(ExtractIdentifier(warr, s) && Equal(warr, "_ident") && *s == L' ', true);
				PR_CHECK(ExtractIdentifierC(warr, ++s) && Equal(warr, "_identident2") && *s == L'i', true);

				s = src;
				PR_CHECK(ExtractIdentifier(astr, s) && Equal(astr, "_ident") && *s == L' ', true);
				PR_CHECK(ExtractIdentifierC(astr, ++s) && Equal(astr, "_identident2") && *s == L'i', true);

				s = src;
				PR_CHECK(ExtractIdentifier(wstr, s) && Equal(wstr, "_ident") && *s == L' ', true);
				PR_CHECK(ExtractIdentifierC(wstr, ++s) && Equal(wstr, "_identident2") && *s == L'i', true);
			}
			{// String
				using namespace pr::str;

				wchar_t const* src = LR"("string1" "str\"i\\ng2")", *s;
				char         aarr[20] = {};
				wchar_t      warr[20] = {};
				std::string  astr;
				std::wstring wstr;

				s = src;
				PR_CHECK(ExtractString(aarr, s, '\\') && Equal(aarr, "string1") && *s == L' ', true);
				PR_CHECK(ExtractStringC(aarr, ++s, '\\') && Equal(aarr, R"(string1str"i\ng2)") && *s == L'"', true);

				s = src;
				PR_CHECK(ExtractString(warr, s) && Equal(warr, "string1") && *s == L' ', true);
				PR_CHECK(ExtractStringC(warr, ++s) && Equal(warr, R"(string1str\)") && *s == L'"', true);

				s = src;
				PR_CHECK(ExtractString(astr, s, '\\') && Equal(astr, "string1") && *s == L' ', true);
				PR_CHECK(ExtractStringC(astr, ++s, '\\') && Equal(astr, R"(string1str"i\ng2)") && *s == L'"', true);

				s = src;
				PR_CHECK(ExtractString(wstr, s) && Equal(wstr, "string1") && *s == L' ', true);
				PR_CHECK(ExtractStringC(wstr, ++s) && Equal(wstr, R"(string1str\)") && *s == L'"', true);
			}
			{// Bool
				using namespace pr::str;

				char  src[] = "true false 1", *s = src;
				bool  bbool = 0;
				int   ibool = 0;
				float fbool = 0;

				PR_CHECK(ExtractBool(bbool, s) ,true); PR_CHECK(bbool ,true);
				PR_CHECK(ExtractBool(ibool, s) ,true); PR_CHECK(ibool ,0   );
				PR_CHECK(ExtractBool(fbool, s) ,true); PR_CHECK(fbool ,1.0f);
			}
			{// Int
				using namespace pr::str;

				char  c = 0;      unsigned char  uc = 0;
				short s = 0;      unsigned short us = 0;
				int   i = 0;      unsigned int   ui = 0;
				long  l = 0;      unsigned long  ul = 0;
				long long ll = 0; unsigned long long ull = 0;
				float  f = 0;     double d = 0;
				{
					char src[] = "\n -1.14 ";
					PR_CHECK(ExtractIntC(c  ,10 ,src) ,true);   PR_CHECK(c  ,(char)-1);
					PR_CHECK(ExtractIntC(uc ,10 ,src) ,true);   PR_CHECK(uc ,(unsigned char)0xff);
					PR_CHECK(ExtractIntC(s  ,10 ,src) ,true);   PR_CHECK(s  ,(short)-1);
					PR_CHECK(ExtractIntC(us ,10 ,src) ,true);   PR_CHECK(us ,(unsigned short)0xffff);
					PR_CHECK(ExtractIntC(i  ,10 ,src) ,true);   PR_CHECK(i  ,(int)-1);
					PR_CHECK(ExtractIntC(ui ,10 ,src) ,true);   PR_CHECK(ui ,(unsigned int)0xffffffff);
					PR_CHECK(ExtractIntC(l  ,10 ,src) ,true);   PR_CHECK(l  ,(long)-1);
					PR_CHECK(ExtractIntC(ul ,10 ,src) ,true);   PR_CHECK(ul ,(unsigned long)0xffffffff);
					PR_CHECK(ExtractIntC(ll ,10 ,src) ,true);   PR_CHECK(ll ,(long long)-1);
					PR_CHECK(ExtractIntC(ull,10 ,src) ,true);   PR_CHECK(ull,(unsigned long long)0xffffffffffffffffL);
					PR_CHECK(ExtractIntC(f  ,10 ,src) ,true);   PR_CHECK(f  ,(float)-1.0f);
					PR_CHECK(ExtractIntC(d  ,10 ,src) ,true);   PR_CHECK(d  ,(double)-1.0);
				}
				{
					char src[] = "0x1abcZ", *ptr = src;
					PR_CHECK(ExtractInt(i,0,ptr), true);
					PR_CHECK(i    ,0x1abc);
					PR_CHECK(*ptr ,'Z');
				}
				{
					char src[] = "0xdeadBeaf";
					PR_CHECK(ExtractIntC(ll, 16, src), true);
					PR_CHECK(ll, 0xdeadBeaf);
				}
			}
			{// Real
				using namespace pr::str;

				float f = 0; double d = 0; int i = 0;
				{
					char src[] = "\n 6.28 ";
					PR_CHECK(ExtractRealC(f ,src) ,true); PR_CLOSE(f, 6.28f, 0.00001f);
					PR_CHECK(ExtractRealC(d ,src) ,true); PR_CLOSE(d, 6.28 , 0.00001);
					PR_CHECK(ExtractRealC(i ,src) ,true); PR_CHECK(i, 6);
				}
				{
					char src[] = "-1.25e-4Z", *ptr = src;
					PR_CHECK(ExtractReal(d, ptr) ,true);
					PR_CHECK(d ,-1.25e-4);
					PR_CHECK(*ptr, 'Z');
				}
			}
			{// Arrays
				using namespace pr::str;

				{// Bool Array
					char src[] = "\n true 1 TRUE ";
					float f[3] = {0,0,0};
					PR_CHECK(ExtractBoolArrayC(f, 3, src), true);
					PR_CHECK(f[0], 1.0f);
					PR_CHECK(f[1], 1.0f);
					PR_CHECK(f[2], 1.0f);
				}
				{// Int Array
					char src[] = "\n \t3  1 \n -2\t ";
					int i[3] = {0,0,0};
					unsigned int u[3] = {0,0,0};
					float        f[3] = {0,0,0};
					double       d[3] = {0,0,0};
					PR_CHECK(ExtractIntArrayC(i, 3, 10, src), true); PR_CHECK(i[0], 3);               PR_CHECK(i[1], 1);               PR_CHECK(i[2], -2);
					PR_CHECK(ExtractIntArrayC(u, 3, 10, src), true); PR_CHECK(i[0], 3);               PR_CHECK(i[1], 1);               PR_CHECK(i[2], -2);
					PR_CHECK(ExtractIntArrayC(f, 3, 10, src), true); PR_CLOSE(f[0], 3.f, 0.00001f);   PR_CLOSE(f[1], 1.f, 0.00001f);   PR_CLOSE(f[2], -2.f, 0.00001f);
					PR_CHECK(ExtractIntArrayC(d, 3, 10, src), true); PR_CLOSE(d[0], 3.0, 0.00001);    PR_CLOSE(d[1], 1.0, 0.00001);    PR_CLOSE(d[2], -2.0, 0.00001);
				}
				{// Real Array
					char src[] = "\n 6.28\t6.28e0\n-6.28 ";
					float  f[3] = {0,0,0};
					double d[3] = {0,0,0};
					int    i[3] = {0,0,0};
					PR_CHECK(ExtractRealArrayC(f, 3, src) ,true);    PR_CLOSE(f[0], 6.28f, 0.00001f); PR_CLOSE(f[1], 6.28f, 0.00001f); PR_CLOSE(f[2], -6.28f, 0.00001f);
					PR_CHECK(ExtractRealArrayC(d, 3, src) ,true);    PR_CLOSE(d[0], 6.28 , 0.00001);  PR_CLOSE(d[1], 6.28 , 0.00001);  PR_CLOSE(d[2], -6.28 , 0.00001);
					PR_CHECK(ExtractRealArrayC(i, 3, src) ,true);    PR_CHECK(i[0], 6);               PR_CHECK(i[1] ,6);               PR_CHECK(i[2], -6);
				}
			}
			{// Number
				using namespace pr::str;

				char    src0[] =  "-3.24e-39f";
				wchar_t src1[] = L"0x123abcUL";
				char    src2[] =  "01234567";
				wchar_t src3[] = L"-34567L";
		
				float f = 0; int i = 0; bool fp = false;
				PR_CHECK(ExtractNumberC(i,f,fp,src0) ,true); PR_CHECK( fp, true); PR_CHECK(f ,-3.24e-39f);
				PR_CHECK(ExtractNumberC(i,f,fp,src1) ,true); PR_CHECK(!fp, true); PR_CHECK((unsigned long)i, 0x123abcUL);
				PR_CHECK(ExtractNumberC(i,f,fp,src2) ,true); PR_CHECK(!fp, true); PR_CHECK(i ,01234567);
				PR_CHECK(ExtractNumberC(i,f,fp,src3) ,true); PR_CHECK(!fp, true); PR_CHECK((long)i ,-34567L);
			}
		}
	}
}
#endif
