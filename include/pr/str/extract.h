//**********************************
// Extract
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <type_traits>
#include <cerrno>
#include "pr/common/number.h"
#include "pr/common/flags_enum.h"
#include "pr/str/string_core.h"

namespace pr::str
{
	// Notes:
	//  - Functions ending with 'C' don't advance the character source pointer.
	//  - 'errno' is used for 'wcstoul', etc conversions. 'errno' is thread local so is thread safe

	#pragma region Extract Utility Functions

	// A helper type that takes a pointer to a char stream and outputs a 'wchar_t' stream
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

	// A string wrapper that provides a null-termination interface
	template <typename Char> struct basic_stringz_t
	{
		Char const* m_ptr;
		Char const* m_end;
		inline static Char const m_null = 0;

		basic_stringz_t(Char const* str)
			:m_ptr(str)
			,m_end(nullptr)
		{}
		basic_stringz_t(std::basic_string_view<Char> str)
			:m_ptr(str.data())
			,m_end(str.data() + str.size())
		{}
		basic_stringz_t(std::basic_string<Char> const& str)
			:m_ptr(str.data())
			,m_end(str.data() + str.size())
		{}

		Char operator *() const
		{
			return *m_ptr;
		}
		basic_stringz_t& operator ++()
		{
			if (++m_ptr == m_end) m_ptr = &m_null;
			return *this;
		}
		basic_stringz_t operator ++(int)
		{
			auto me = *this;
			++*this;
			return me;
		}
		basic_stringz_t& operator +=(int n)
		{
			if ((m_ptr += n) == m_end) m_ptr = &m_null;
			return *this;
		}
	};
	using stringz_t = basic_stringz_t<char>;
	using wstringz_t = basic_stringz_t<wchar_t>;

	// Get the character type from a pointer or iterator
	template <typename Iter, typename Char = std::remove_reference_t<decltype(*std::declval<Iter>())>>
	using char_type_t = Char;

	// Checks that 'tstr' is an std::string-like type
	template <typename tstr, typename = decltype(std::declval<tstr>().size())> using valid_str_t = void;

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

	// Used to filter the accepted characters when extracting number strings
	enum class ENumType
	{
		Int = 1 << 0,
		FP  = 1 << 1,
		Any = Int|FP,
		_bitwise_operators_allowed,
	};

	// Buffer characters for a number (real or int) from 'src'
	// Format: [delim][{+|-}][0[{x|X|b|B}]][digits][.digits][{d|D|e|E|p|P}[{+|-}]digits][U][L][L]
	// [out] 'num' = the extracted value
	// [in] 'radix' = the base of the number to read.
	// [in] 'type' = the number style to read.
	// [in] 'src' = the forward only input stream
	// [in] 'delim' = token delimiter characters.
	// Returns false if a valid number could not be read, or 'str' is too small
	template <typename Ptr, typename Char = char_type_t<Ptr>> void BufferNumber(Ptr& src, wchar_t (&str)[256], int& len, int& radix, ENumType type = ENumType::Any, Char const* delim = nullptr)
	{
		// Notes:
		//  - This duplicates the BufferNumber function in pr::script :-/
		//    The pr::script version does not consume characters but instead buffers them within
		//    the source. I don't want pr::str to depend on pr::script and I don't want to change
		//    the behaviour of the pr::script version, so duplication is the only option.

		len = 0;
		delim = Delim(delim);

		// Find the first non-delimiter
		if (!AdvanceToNonDelim(src, delim))
			return;

		// Promote the character stream to 'wchar_t's.
		wchar_ptr<Ptr> wsrc(src);
		auto append = [&](wchar_t ch)
		{
			if (len == _countof(str)) return false;
			str[len++] = ch;
			return true;
		};
		auto digit = [](wchar_t ch)
		{
			if (ch >= '0' && ch <= '9') return ch - '0';
			if (ch >= 'a' && ch <= 'z') return 10 + ch - 'a';
			if (ch >= 'A' && ch <= 'Z') return 10 + ch - 'A';
			return std::numeric_limits<int>::max();
		};

		auto allow_fp = (type & ENumType::FP) == ENumType::FP;
		auto fp = false;

		// Look for the optional sign character
		// Ideally we'd prefer not to advance 'src' passed the '+' or '-' if the next
		// character is not the start of a number. However doing so means 'src' can't
		// be a forward only input stream. Therefore, I'm pushing the responsibility
		// back to the caller, they need to check that if *src is a '+' or '-' then
		// the following char is a decimal digit.
		if (*wsrc == '+' || *wsrc == '-')
		{
			if (!append(*wsrc)) return;
			++wsrc;
		}

		// Look for a radix prefix on the number, this overrides 'radix'.
		// If the first digit is zero, then the number may have a radix prefix.
		// '0x' or '0b' must have at least one digit following the prefix
		// Adding 'o' for octal, in addition to standard C literal syntax
		if (*wsrc == '0')
		{
			++wsrc;
			auto digit_required = false;
			if      (*wsrc == 'x' || *wsrc == 'X') { radix = 16; ++wsrc; digit_required = true; }
			else if (*wsrc == 'o' || *wsrc == 'O') { radix =  8; ++wsrc; digit_required = true; }
			else if (*wsrc == 'b' || *wsrc == 'B') { radix =  2; ++wsrc; digit_required = true; }

			// Check for the required integer
			if (digit_required)
			{
				if (digit(*wsrc) >= radix)
				{
					len = 0;
					return;
				}
				else if (radix == 16)
				{
					if (!append('0')) return;
					if (!append('x')) return;
				}
				else if (radix == 8)
				{
					if (!append('0')) return;
				}
			}
			else
			{
				// If no radix is given, then assume octal (for conformance with C)
				if (radix == 0) radix = 8;

				// Add '0' to the string because we're not skipping over a prefix.
				// This is needed to handle the "0" case.
				if (!append('0')) return;
			}
		}
		else if (radix == 0)
		{
			radix = 10;
		}

		// Read digits up to a delimiter, decimal point, or digit >= radix.
		auto assumed_fp_len = 0; // the length of 'str' when we first assumed a FP number.
		for (; *wsrc; ++wsrc)
		{
			// If the character is greater than the radix, then assume a FP number.
			// e.g. 09.1 could be an invalid octal number or a FP number.
			// 019 is assumed to be FP, 
			auto d = digit(*wsrc);
			if (d < radix)
			{
				if (!append(*wsrc)) return;
				continue;
			}
			else if (radix == 8 && allow_fp && d < 10)
			{
				if (assumed_fp_len == 0) assumed_fp_len = len;
				if (!append(*wsrc)) return;
				continue;
			}
			break;
		}

		// If we're assuming this is a FP number but no decimal point is found,
		// then truncate the string at the last valid character given 'radix'.
		// If a decimal point is found, change the radix to base 10.
		if (assumed_fp_len != 0)
		{
			if (*wsrc == '.') radix = 10;
			else len = assumed_fp_len;
		}

		// FP numbers can be in dec or hex, but not anything else...
		allow_fp &= radix == 10 || radix == 16;

		// If floating point is allowed, read a decimal point followed by more digits, and an optional exponent
		if (allow_fp && *wsrc == '.' && IsDecDigit(*(++wsrc)))
		{
			fp = true;
			if (!append('.')) return;

			// Read decimal digits up to a delimiter, sign, or exponent
			for (; IsDecDigit(*wsrc); ++wsrc)
				if (!append(*wsrc)) return;
		}

		// Read an optional exponent
		if (allow_fp && (*wsrc == 'e' || *wsrc == 'E' || *wsrc == 'd' || *wsrc == 'D' || (*wsrc == 'p' && radix == 16) || (*wsrc == 'P' && radix == 16)))
		{
			if (!append(*wsrc)) return;
			++wsrc;

			// Read the optional exponent sign
			if (*wsrc == '+' || *wsrc == '-')
			{
				if (!append(*wsrc)) return;
				++wsrc;
			}

			// Read decimal digits up to a delimiter, or suffix
			for (; IsDecDigit(*wsrc); ++wsrc)
				if (!append(*wsrc)) return;
		}

		// Read the optional number suffixes
		if (allow_fp && (*wsrc == 'f' || *wsrc == 'F'))
		{
			fp = true;
			++wsrc;
		}
		if (!fp && (*wsrc == 'u' || *wsrc == 'U'))
		{
			++wsrc;
		}
		if (!fp && (*wsrc == 'l' || *wsrc == 'L'))
		{
			++wsrc;
			auto ll = *wsrc == 'l' || *wsrc == 'L';
			if (ll) ++wsrc;
		}
	}

	#pragma endregion

	#pragma region Extract Line
	// Extract a contiguous block of characters up to (and possibly including) a new line character
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractLine(Str& line, Ptr& src, bool inc_cr, Char const* newline = nullptr)
	{
		if (newline == nullptr) newline = PR_STRLITERAL(Char,"\n");
		auto len = Size(line);
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
		auto len = Size(token);
		for (Append(token, len++, *src), ++src; *src && *FindChar(delim, *src) == 0; Append(token, len++, *src), ++src) {}
		return true;
	}
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractTokenC(Str& token, Ptr src, Char const* delim = nullptr)
	{
		return ExtractToken(token, src, delim);
	}
	#pragma endregion

	#pragma region Extract Identifier
	// Extract a contiguous block of identifier characters from 'src' incrementing 'src'
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
		auto len = 0;//Length(id); don't append existing values in 'id'
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
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractString(Str& str, Ptr& src, Char const* delim = nullptr)
	{
		return ExtractString(str, src, Char(0), nullptr, delim);
	}
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractString(Str& str, Ptr& src, Char escape, std::nullptr_t quotes, Char const* delim = nullptr)
	{
		return ExtractString(str, src, escape, static_cast<Char const*>(quotes), delim);
	}
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractString(Str& str, Ptr& src, Char escape, Char const* quotes, Char const* delim = nullptr)
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
		auto len = 0;//Length(str); don't append existing values in 'str'
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
							// ASCII character in octal
							wchar_t oct[9] = {};
							for (int i = 0; i != 8 && IsOctDigit(*src); ++i, ++src) oct[i] = wchar_t(*src);
							Append(str, len++, Char(::wcstoul(oct, nullptr, 8)));
							break;
						}
					case 'x':
						{
							// ASCII or UNICODE character in hex
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
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractStringC(Str& str, Ptr src, Char const* delim = nullptr)
	{
		return ExtractStringC(str, src, Char(0), nullptr, delim);
	}
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractStringC(Str& str, Ptr src, Char escape, std::nullptr_t quotes, Char const* delim = nullptr)
	{
		return ExtractString(str, src, escape, static_cast<Char const*>(quotes), delim);
	}
	template <typename Str, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractStringC(Str& str, Ptr src, Char escape, Char const* quotes, Char const* delim = nullptr)
	{
		return ExtractString(str, src, escape, quotes, delim);
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

		// Convert a char to a lower case 'wchar_t'
		auto lwr = [](Char ch){ return char_traits<wchar_t>::lwr(wchar_t(ch)); };

		// Extract the boolean
		switch (lwr(*src))
		{
		default : return false;
		case '0': bool_ = static_cast<Bool>(false); return !IsIdentifier(*++src, false);
		case '1': bool_ = static_cast<Bool>(true ); return !IsIdentifier(*++src, false);
		case 't': bool_ = static_cast<Bool>(true ); return lwr(*++src) == 'r' && lwr(*++src) == 'u' && lwr(*++src) == 'e'                       && !IsIdentifier(*++src, false);
		case 'f': bool_ = static_cast<Bool>(false); return lwr(*++src) == 'a' && lwr(*++src) == 'l' && lwr(*++src) == 's' && lwr(*++src) == 'e' && !IsIdentifier(*++src, false);
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
	// Extract an integral number from 'src' (basically 'strtol')
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
		wchar_t str[256] = {}; int len = 0;
		BufferNumber(src, str, len, radix, ENumType::Int, delim);
		if (len == 0 || len == _countof(str)) return false;
		str[len] = 0;

		errno = 0;
		wchar_t* end;
		intg = std::is_unsigned<Int>::value
			? static_cast<Int>(::_wcstoui64(str, &end, radix))
			: static_cast<Int>(::_wcstoi64(str, &end, radix));

		// Check all of the string was used in the conversion and there wasn't an overflow
		return end - &str[0] == len && errno != ERANGE;
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
		int radix = 10;
		wchar_t str[256] = {}; int len = 0;
		BufferNumber(src, str, len, radix, ENumType::FP, delim);
		if (len == 0 || len == _countof(str)) return false;
		str[len] = 0;

		errno = 0;
		wchar_t* end;
		real = static_cast<Real>(::wcstod(str, &end));

		// Check all of the string was used in the conversion and there wasn't an overflow
		return end - &str[0] == len && errno != ERANGE;
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

	#pragma region Extract Number
	// Extract a number (real or int) from 'src'
	// Format: [delim][{+|-}][0[{x|X|b|B}]][digits][.digits][{d|D|e|E}[{+|-}]digits][U][L][L]
	// [out] 'num' = the extracted value
	// [in] 'radix' = the base of the number to read.
	// [in] 'type' = the number style to read.
	// [in] 'src' = the forward only input stream
	// [in] 'delim' = token delimiter characters.
	// Returns false if a valid number could not be read, or 'str' is too small
	template <typename Ptr, typename Char = char_type_t<Ptr>> bool ExtractNumber(Number& num, Ptr& src, int radix = 0, Char const* delim = nullptr)
	{
		wchar_t str[256] = {}; int len = 0;
		BufferNumber(src, str, len, radix, ENumType::Any, delim);
		if (len == 0 || len == _countof(str)) return false;
		str[len] = 0;

		errno = 0;
		wchar_t* end;
		num = Number::From(str, &end, radix);

		// Check all of the string was used in the conversion and there wasn't an overflow
		return end - &str[0] == len && errno != ERANGE;
	}
	template <typename Ptr, typename Char = char_type_t<Ptr>> bool ExtractNumberC(Number& num, Ptr src, int radix = 0, Char const* delim = nullptr)
	{
		return ExtractNumber(num, src, radix, delim);
	}
	template <typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractNumberArray(Real* real, size_t count, Ptr& src, int radix = 0, Char const* delim = nullptr)
	{
		while (count--) if (!ExtractNumber(*num++, src, radix, delim)) return false;
		return true;
	}
	template <typename Real, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractNumberArrayC(Number* num, size_t count, Ptr src, int radix = 0, Char const* delim = nullptr)
	{
		return ExtractNumberArray(num, count, src, radix, delim);
	}
	#pragma endregion

	#pragma region Extract Enum
	// This is basically a convenience wrapper around ExtractInt
	template <typename TEnum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnumValue(TEnum& enum_, int radix, Ptr& src, Char const* delim = nullptr)
	{
		std::underlying_type<TEnum>::type val;
		if (!ExtractInt(val, radix, src, delim)) return false;
		enum_ = static_cast<TEnum>(val);
		return true;
	}
	template <typename TEnum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnumValueC(TEnum& enum_, int radix, Ptr src, Char const* delim = nullptr)
	{
		return ExtractEnum(enum_, radix, src, delim);
	}

	// Extracts an enum by its string name. For use with 'PR_ENUM' defined enums
	template <typename TEnum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnum(TEnum& enum_, Ptr& src, Char const* delim = nullptr)
	{
		decltype(*src) val[512] = {};
		if (!ExtractIdentifier(val, src, delim)) return false;
		return Enum<TEnum>::TryParse(enum_, val, false);
	}
	template <typename TEnum, typename Ptr, typename Char = char_type_t<Ptr>> inline bool ExtractEnumC(TEnum& enum_, Ptr src, Char const* delim = nullptr)
	{
		return ExtractEnum(enum_, src, delim);
	}
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::str
{
	PRUnitTest(ExtractTests)
	{
		{// Line
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
			PR_CHECK(ExtractIdentifierC(aarr, ++s) && Equal(aarr, "ident2") && *s == L'i', true);

			s = src;
			PR_CHECK(ExtractIdentifier(warr, s) && Equal(warr, "_ident") && *s == L' ', true);
			PR_CHECK(ExtractIdentifierC(warr, ++s) && Equal(warr, "ident2") && *s == L'i', true);

			s = src;
			PR_CHECK(ExtractIdentifier(astr, s) && Equal(astr, "_ident") && *s == L' ', true);
			PR_CHECK(ExtractIdentifierC(astr, ++s) && Equal(astr, "ident2") && *s == L'i', true);

			s = src;
			PR_CHECK(ExtractIdentifier(wstr, s) && Equal(wstr, "_ident") && *s == L' ', true);
			PR_CHECK(ExtractIdentifierC(wstr, ++s) && Equal(wstr, "ident2") && *s == L'i', true);
		}
		{// String
			using namespace pr::str;

			wchar_t const* src = LR"("string1" "str\"i\\ng2")", *s;
			char         aarr[20] = {};
			wchar_t      warr[20] = {};
			std::string  astr;
			std::wstring wstr;

			s = src;
			PR_CHECK(ExtractString(aarr, s, L'\\', nullptr) && Equal(aarr, "string1") && *s == L' ', true);
			PR_CHECK(ExtractStringC(aarr, ++s, L'\\', nullptr) && Equal(aarr, R"(str"i\ng2)") && *s == L'"', true);

			s = src;
			PR_CHECK(ExtractString(warr, s) && Equal(warr, "string1") && *s == L' ', true);
			PR_CHECK(ExtractStringC(warr, ++s) && Equal(warr, R"(str\)") && *s == L'"', true);

			s = src;
			PR_CHECK(ExtractString(astr, s, L'\\', nullptr) && Equal(astr, "string1") && *s == L' ', true);
			PR_CHECK(ExtractStringC(astr, ++s, L'\\', nullptr) && Equal(astr, R"(str"i\ng2)") && *s == L'"', true);

			s = src;
			PR_CHECK(ExtractString(wstr, s) && Equal(wstr, "string1") && *s == L' ', true);
			PR_CHECK(ExtractStringC(wstr, ++s) && Equal(wstr, R"(str\)") && *s == L'"', true);
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
				char src[] = "0";
				PR_CHECK(ExtractIntC(i,  0, src), true); PR_CHECK(i, (int)0);
				PR_CHECK(ExtractIntC(i,  8, src), true); PR_CHECK(i, (int)0);
				PR_CHECK(ExtractIntC(i, 10, src), true); PR_CHECK(i, (int)0);
				PR_CHECK(ExtractIntC(i, 16, src), true); PR_CHECK(i, (int)0);
			}
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
				PR_CHECK(ExtractRealC(f ,src) ,true); PR_CHECK(FEql(f, 6.28f), true);
				PR_CHECK(ExtractRealC(d ,src) ,true); PR_CHECK(FEql(d, 6.28), true);
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
				PR_CHECK(ExtractIntArrayC(i, 3, 10, src), true); PR_CHECK(i[0], 3); PR_CHECK(i[1], 1); PR_CHECK(i[2], -2);
				PR_CHECK(ExtractIntArrayC(u, 3, 10, src), true); PR_CHECK(i[0], 3); PR_CHECK(i[1], 1); PR_CHECK(i[2], -2);
				PR_CHECK(ExtractIntArrayC(f, 3, 10, src), true); PR_CHECK(FEql(f[0], 3.f), true); PR_CHECK(FEql(f[1], 1.f), true); PR_CHECK(FEql(f[2], -2.f), true);
				PR_CHECK(ExtractIntArrayC(d, 3, 10, src), true); PR_CHECK(FEql(d[0], 3.0), true); PR_CHECK(FEql(d[1], 1.0), true); PR_CHECK(FEql(d[2], -2.0), true);
			}
			{// Real Array
				char src[] = "\n 6.28\t6.28e0\n-6.28 ";
				float  f[3] = {0,0,0};
				double d[3] = {0,0,0};
				int    i[3] = {0,0,0};
				PR_CHECK(ExtractRealArrayC(f, 3, src) ,true); PR_CHECK(FEql(f[0], 6.28f), true); PR_CHECK(FEql(f[1], 6.28f), true); PR_CHECK(FEql(f[2], -6.28f), true);
				PR_CHECK(ExtractRealArrayC(d, 3, src) ,true); PR_CHECK(FEql(d[0], 6.280), true); PR_CHECK(FEql(d[1], 6.280), true); PR_CHECK(FEql(d[2], -6.280), true);
				PR_CHECK(ExtractRealArrayC(i, 3, src) ,true); PR_CHECK(i[0], 6); PR_CHECK(i[1] ,6); PR_CHECK(i[2], -6);
			}
		}
		{// Number
			using namespace pr::str;
				
			Number num;
			PR_CHECK(ExtractNumberC(num, "0"       ), true); PR_CHECK(num.ll(), 0);
			PR_CHECK(ExtractNumberC(num, "+0"      ), true); PR_CHECK(num.ll(), 0);
			PR_CHECK(ExtractNumberC(num, "-0"      ), true); PR_CHECK(num.ll(), 0);
			PR_CHECK(ExtractNumberC(num, "+.0f"    ), true); PR_CHECK(FEql(num.db(), +0.0), true);
			PR_CHECK(ExtractNumberC(num, "-.1f"    ), true); PR_CHECK(FEql(num.db(), -0.1), true);
			PR_CHECK(ExtractNumberC(num, "1F"      ), true); PR_CHECK(FEql(num.db(), 1.0 ), true);
			PR_CHECK(ExtractNumberC(num, "3E-05F"  ), true); PR_CHECK(FEql(num.db(), 3E-05), true);
			PR_CHECK(ExtractNumberC(num, "12three" ), true); PR_CHECK(num.ll(), 12        );
			PR_CHECK(ExtractNumberC(num, "0x123"   ), true); PR_CHECK(num.ll(), 0x123     );
			PR_CHECK(ExtractNumberC(num, "0x123ULL"), true); PR_CHECK(num.ul(), 0x123ULL  );
			PR_CHECK(ExtractNumberC(num, "0x123L"  ), true); PR_CHECK(num.ll(), 0x123LL   );
			PR_CHECK(ExtractNumberC(num, "0x123LL" ), true); PR_CHECK(num.ll(), 0x123LL   );

			PR_CHECK(ExtractNumberC(num, "0b101010"), true); PR_CHECK(num.ll(), 0b101010LL);
			PR_CHECK(ExtractNumberC(num, "0923.0"  ), true); PR_CHECK(FEql(num.db(), 0923.0), true);
			PR_CHECK(ExtractNumberC(num, "0199"    ), true); PR_CHECK(num.ll(), 01); // because it's octal
			PR_CHECK(ExtractNumberC(num, "0199", 10), true); PR_CHECK(num.ll(), 199);
			PR_CHECK(ExtractNumberC(num, "0x1.0p1" ), true); PR_CHECK(FEql(num.db(), 0x1.0p1), true);

			PR_CHECK(ExtractNumberC(num, "0x.0"), false);
			PR_CHECK(ExtractNumberC(num, ".x0"), false);
			PR_CHECK(ExtractNumberC(num, "-x.0"), false);

			char    src0[] =  "-3.24e-39f";
			wchar_t src1[] = L"0x123abcUL";
			char    src2[] =  "01234567";
			wchar_t src3[] = L"-34567L";
		
			PR_CHECK(ExtractNumberC(num, src0) ,true); PR_CHECK(num.m_type == Number::EType::FP , true); PR_CHECK(FEql(num.db(),-3.24e-39), true);
			PR_CHECK(ExtractNumberC(num, src1) ,true); PR_CHECK(num.m_type == Number::EType::Int, true); PR_CHECK(num.ul(), 0x123abcULL);
			PR_CHECK(ExtractNumberC(num, src2) ,true); PR_CHECK(num.m_type == Number::EType::Int, true); PR_CHECK(num.ll(), 01234567);
			PR_CHECK(ExtractNumberC(num, src3) ,true); PR_CHECK(num.m_type == Number::EType::Int, true); PR_CHECK(num.ll(), -34567LL);
		}
		{// Null terminated string
			std::string str = "6.28 Ident";
			stringz_t sz = str;

			double val0;
			PR_CHECK(ExtractReal(val0, sz), true);
			PR_CHECK(val0, 6.28);
			PR_CHECK(*sz, ' ');

			std::string ident;
			PR_CHECK(ExtractIdentifier(ident, sz), true);
			PR_CHECK(ident, "Ident");
			PR_CHECK(*sz, '\0');
		}
	}
}
#endif
