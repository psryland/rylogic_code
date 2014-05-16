//***********************************************************************
// String extract functions
//  Copyright (c) Rylogic Ltd 2008
//***********************************************************************

#ifndef PR_STR_STRING_EXTRACT_H
#define PR_STR_STRING_EXTRACT_H

#include <cstdlib>
#include <cerrno>
#include "pr/str/prstringcore.h"

namespace pr
{
	namespace str
	{
		// Notes:
		//  - 'tstr' is the string to output
		//  - 'src' must always be an iterator to a null terminated string.
		//  - only use pre-increment on 'src'
		//  - never seek backwards on 'src' only forwards
		//  - 'end' is only set when the extract function returns true;
		
		// String to numeric overloads
		template <typename Value>                Value as(char const* str, char** end_ptr, int radix);
		template <typename Value>                Value as(char const* str, char** end_ptr);
		template <typename Value>                Value as(wchar_t const* str, wchar_t** end_ptr, int radix);
		template <typename Value>                Value as(wchar_t const* str, wchar_t** end_ptr);
		template <typename Value, typename tstr> Value as(tstr const& str, char** end_ptr, int radix)      { return as<Value>(str.c_str(), end_ptr, radix); }
		template <typename Value, typename tstr> Value as(tstr const& str, char** end_ptr)                 { return as<Value>(str.c_str(), end_ptr); }
		template <> inline long               as(char const* str, char** end_ptr, int radix) { return static_cast<long>               (::strtol(str, end_ptr, radix)); }
		template <> inline int                as(char const* str, char** end_ptr, int radix) { return static_cast<int>                (::strtol(str, end_ptr, radix)); }
		template <> inline short              as(char const* str, char** end_ptr, int radix) { return static_cast<short>              (::strtol(str, end_ptr, radix)); }
		template <> inline char               as(char const* str, char** end_ptr, int radix) { return static_cast<char>               (::strtol(str, end_ptr, radix)); }
		template <> inline wchar_t            as(char const* str, char** end_ptr, int radix) { return static_cast<wchar_t>            (::strtol(str, end_ptr, radix)); }
		template <> inline float              as(char const* str, char** end_ptr, int radix) { return static_cast<float>              (::strtol(str, end_ptr, radix)); }
		template <> inline double             as(char const* str, char** end_ptr, int radix) { return static_cast<double>             (::strtol(str, end_ptr, radix)); }
		template <> inline unsigned long      as(char const* str, char** end_ptr, int radix) { return static_cast<unsigned long>      (::strtoul(str, end_ptr, radix)); }
		template <> inline unsigned int       as(char const* str, char** end_ptr, int radix) { return static_cast<unsigned int>       (::strtoul(str, end_ptr, radix)); }
		template <> inline unsigned short     as(char const* str, char** end_ptr, int radix) { return static_cast<unsigned short>     (::strtoul(str, end_ptr, radix)); }
		template <> inline unsigned char      as(char const* str, char** end_ptr, int radix) { return static_cast<unsigned char>      (::strtoul(str, end_ptr, radix)); }
		template <> inline long long          as(char const* str, char** end_ptr, int radix) { return static_cast<long long>          (::_strtoi64(str, end_ptr, radix)); }
		template <> inline unsigned long long as(char const* str, char** end_ptr, int radix) { return static_cast<unsigned long long> (::_strtoui64(str, end_ptr, radix)); }
		template <> inline int                as(char const* str, char** end_ptr)            { return static_cast<int>                (::strtod(str, end_ptr)); }
		template <> inline float              as(char const* str, char** end_ptr)            { return static_cast<float>              (::strtod(str, end_ptr)); }
		template <> inline double             as(char const* str, char** end_ptr)            { return static_cast<double>             (::strtod(str, end_ptr)); }
		
		template <> inline long               as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<long>               (::wcstol(str, end_ptr, radix)); }
		template <> inline int                as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<int>                (::wcstol(str, end_ptr, radix)); }
		template <> inline short              as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<short>              (::wcstol(str, end_ptr, radix)); }
		template <> inline char               as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<char>               (::wcstol(str, end_ptr, radix)); }
		template <> inline wchar_t            as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<wchar_t>            (::wcstol(str, end_ptr, radix)); }
		template <> inline float              as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<float>              (::wcstol(str, end_ptr, radix)); }
		template <> inline double             as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<double>             (::wcstol(str, end_ptr, radix)); }
		template <> inline unsigned long      as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<unsigned long>      (::wcstoul(str, end_ptr, radix)); }
		template <> inline unsigned int       as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<unsigned int>       (::wcstoul(str, end_ptr, radix)); }
		template <> inline unsigned short     as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<unsigned short>     (::wcstoul(str, end_ptr, radix)); }
		template <> inline unsigned char      as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<unsigned char>      (::wcstoul(str, end_ptr, radix)); }
		template <> inline long long          as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<long long>          (::_wcstoi64(str, end_ptr, radix)); }
		template <> inline unsigned long long as(wchar_t const* str, wchar_t** end_ptr, int radix) { return static_cast<unsigned long long> (::_wcstoui64(str, end_ptr, radix)); }
		template <> inline int                as(wchar_t const* str, wchar_t** end_ptr)            { return static_cast<int>                (::wcstod(str, end_ptr)); }
		template <> inline float              as(wchar_t const* str, wchar_t** end_ptr)            { return static_cast<float>              (::wcstod(str, end_ptr)); }
		template <> inline double             as(wchar_t const* str, wchar_t** end_ptr)            { return static_cast<double>             (::wcstod(str, end_ptr)); }
		
		// Supported radii, supporting a few non-C constant types
		namespace NumType
		{
			enum Type { Dec = 10, Hex = 16, Oct = 8, Bin = 2, FP = 0 };
		}
		
		// Extract a contiguous block of characters upto (and possibly including) a new line character
		namespace impl
		{
			template <typename Str, typename Char, typename Iter> inline bool ExtractLine(Str& line, Iter& src, bool inc_cr, char const* newline = 0)
			{
				if (newline == 0) newline = "\n";
				for (;*src && *str::FindChar(newline, *src) == 0; line.push_back(Char(*src)), ++src) {}
				if (*src && inc_cr) { line.push_back(Char(*src)); ++src; }
				return true;
			}
		}
		template <typename Str, typename Iter> inline bool ExtractLine(Str& line, Iter& src, bool inc_cr, char const* newline = 0)
		{
			line.clear();
			return impl::ExtractLine<Str, Str::value_type, Iter>(line, src, inc_cr, newline);
		}
		template <typename Char, typename Iter> inline bool ExtractLine(Char* line, size_t len, Iter& src, bool inc_cr, char const* newline = 0)
		{
			fixed_buffer<Char> buf(line, len);
			return impl::ExtractLine<fixed_buffer<Char>, Char, Iter>(buf, src, inc_cr, newline);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractLine(Char (&line)[Len], Iter& src, bool inc_cr, char const* newline = 0)
		{
			fixed_buffer<Char> buf(line, Len);
			return impl::ExtractLine<fixed_buffer<Char>, Char, Iter>(buf, src, inc_cr, newline);
		}
		template <typename Str, typename Iter> inline bool ExtractLineC(Str& line, Iter src, bool inc_cr, char const* newline = 0)
		{
			line.clear();
			return impl::ExtractLine<Str, Str::value_type, Iter>(line, src, inc_cr, newline);
		}
		template <typename Char, typename Iter> inline bool ExtractLineC(Char* line, size_t len, Iter src, bool inc_cr, char const* newline = 0)
		{
			fixed_buffer<Char> buf(line, len);
			return impl::ExtractLine<fixed_buffer<Char>, Char, Iter>(buf, src, inc_cr, newline);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractLineC(Char (&line)[Len], Iter src, bool inc_cr, char const* newline = 0)
		{
			fixed_buffer<Char> buf(line, Len);
			return impl::ExtractLine<fixed_buffer<Char>, Char, Iter>(buf, src, inc_cr, newline);
		}
		
		// Extract a contiguous block of identifier characers from 'src' incrementing 'src'
		namespace impl
		{
			template <typename Str, typename Char, typename Iter> bool ExtractIdentifier(Str& id, Iter& src, char const* delim = 0)
			{
				delim = Delim(delim);
				FindFirstNotOfAdv(src, delim);
				if (!IsIdentifier(*src, true)) return false;
				for (id.push_back(Char(*src)), ++src; IsIdentifier(*src, false); id.push_back(Char(*src)), ++src) {}
				return true;
			}
		}
		template <typename Str, typename Iter> inline bool ExtractIdentifier(Str& identifier, Iter& src, char const* delim = 0)
		{
			identifier.clear();
			return impl::ExtractIdentifier<Str, Str::value_type, Iter>(identifier, src, delim);
		}
		template <typename Char, typename Iter> inline bool ExtractIdentifier(Char* identifier, size_t len, Iter& src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(identifier, len);
			return impl::ExtractIdentifier<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractIdentifier(Char (&identifier)[Len], Iter& src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(identifier, Len);
			return impl::ExtractIdentifier<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Str, typename Iter> inline bool ExtractIdentifierC(Str& identifier, Iter src, char const* delim = 0)
		{
			identifier.clear();
			return impl::ExtractIdentifier<Str, Str::value_type, Iter>(identifier, src, delim);
		}
		template <typename Char, typename Iter> inline bool ExtractIdentifierC(Char* identifier, size_t len, Iter src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(identifier, len);
			return impl::ExtractIdentifier<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractIdentifierC(Char (&identifier)[Len], Iter src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(identifier, Len);
			return impl::ExtractIdentifier<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		
		// Extract a string from 'src'
		namespace impl
		{
			template <typename Str, typename Char, typename Iter> bool ExtractString(Str& str, Iter& src, char const* delim = 0)
			{
				delim = Delim(delim);
				FindFirstNotOfAdv(src, delim);
				if (*src == '"') ++src; else return false;
				size_t len;
				for (len = 0; *src && *src != '"'; ++src, ++len) { str.push_back(Char(*src)); }
				if (*src == '"') ++src; else return false;
				return true;
			}
		}
		template <typename Str, typename Iter> inline bool ExtractString(Str& str, Iter& src, char const* delim = 0)
		{
			str.clear();
			return impl::ExtractString<Str, Str::value_type, Iter>(str, src, delim);
		}
		template <typename Char, typename Iter> inline bool ExtractString(Char* str, size_t len, Iter& src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, len);
			return impl::ExtractString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractString(Char (&str)[Len], Iter& src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, Len);
			return impl::ExtractString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Str, typename Iter> inline bool ExtractStringC(Str& str, Iter src, char const* delim = 0)
		{
			str.clear();
			return impl::ExtractString<Str, Str::value_type, Iter>(str, src, delim);
		}
		template <typename Char, typename Iter> inline bool ExtractStringC(Char* str, size_t len, Iter src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, len);
			return impl::ExtractString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractStringC(Char (&str)[Len], Iter src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, Len);
			return impl::ExtractString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		
		// Extract a C string from 'src'
		// Also handles literal characters, e.g. 'A' or '\n'
		namespace impl
		{
			template <typename Str, typename Char, typename Iter> bool ExtractCString(Str& str, Iter& src, char const* delim = 0)
			{
				FindFirstNotOfAdv(src, Delim(delim));
				
				Char end = *src;
				bool is_str  = *src == '\"';
				bool is_char = *src == '\'';
				if (is_char || is_str) ++src; else return false;
				if (is_char && *src == end) return false; // literal characters cannot be empty, i.e. ''
				for (; *src && *src != end; ++src)
				{
					if (*src == '\\')
					{
						switch (*++src)
						{
						default: break; // invalid escape sequence...
						case 'a':  str.push_back('\a'); break;
						case 'b':  str.push_back('\b'); break;
						case 'f':  str.push_back('\f'); break;
						case 'n':  str.push_back('\n'); break;
						case 'r':  str.push_back('\r'); break;
						case 't':  str.push_back('\t'); break;
						case 'v':  str.push_back('\v'); break;
						case '\'': str.push_back('\''); break;
						case '\"': str.push_back('\"'); break;
						case '\\': str.push_back('\\'); break;
						case '\?': str.push_back('\?'); break;
						case '0': case '1': case '2': case '3':{// ascii character in octal
							char oct[9] = {};
							for (int i = 0; i != 8 && IsOctDigit(*src); ++i, ++src) oct[i] = *src;
							str.push_back(as<Char>(oct, 0, 8));
							}break;
						case 'x':{// ascii or unicode character in hex
							char hex[9] = {};
							for (int i = 0; i != 8 && IsHexDigit(*src); ++i, ++src) hex[i] = *src;
							str.push_back(as<Char>(hex, 0, 16));
							}break;
						}
					}
					else
					{
						str.push_back(*src);
					}
					if (end == '\'') { ++src; break; }
				}
				if (*src == end) ++src; else return false;
				return true;
			}
		}
		template <typename Str, typename Iter> inline bool ExtractCString(Str& str, Iter& src, char const* delim = 0)
		{
			str.clear();
			return impl::ExtractCString<Str, Str::value_type, Iter>(str, src, delim);
		}
		template <typename Char, typename Iter> inline bool ExtractCString(Char* str, size_t len, Iter& src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, len);
			return impl::ExtractCString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractCString(Char (&str)[Len], Iter& src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, Len);
			return impl::ExtractCString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Str, typename Iter> inline bool ExtractCStringC(Str& str, Iter src, char const* delim = 0)
		{
			str.clear();
			return impl::ExtractCString<Str, Str::value_type, Iter>(str, src, delim);
		}
		template <typename Char, typename Iter> inline bool ExtractCStringC(Char* str, size_t len, Iter src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, len);
			return impl::ExtractCString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		template <typename Char, size_t Len, typename Iter> inline bool ExtractCStringC(Char (&str)[Len], Iter src, char const* delim = 0)
		{
			fixed_buffer<Char> buf(str, Len);
			return impl::ExtractCString<fixed_buffer<Char>, Char, Iter>(buf, src, delim);
		}
		
		// Extract a boolean from 'src'
		// Expects 'src' to point to a string of the following form:
		// [delim]{0|1|true|false}
		// The first character that does not fit this form stops the scan.
		// '0','1' must be followed by a non-identifier character
		// 'true', 'false' can have any case
		template <typename Bool, typename Iter> bool ExtractBool(Bool& bool_, Iter& src, char const* delim = 0)
		{
			delim = Delim(delim);
			FindFirstNotOfAdv(src, delim);
			switch (tolower(*src))
			{
			default : return false;
			case '0': bool_ = false; return !IsIdentifier(*++src, false);
			case '1': bool_ = true;  return !IsIdentifier(*++src, false);
			case 't': bool_ = true;  return tolower(*++src)=='r' && tolower(*++src)=='u' && tolower(*++src)=='e'                         && !IsIdentifier(*++src, false);
			case 'f': bool_ = false; return tolower(*++src)=='a' && tolower(*++src)=='l' && tolower(*++src)=='s' && tolower(*++src)=='e' && !IsIdentifier(*++src, false);
			}
		}
		template <typename Bool, typename Iter> inline bool ExtractBoolC(Bool& bool_, Iter src, char const* delim = 0)
		{
			return ExtractBool(bool_, src, delim);
		}
		
		// Extract an integral number from 'src' (basically strtol)
		// Expects 'src' to point to a string of the following form:
		// [delim] [{+|–}][0[{x|X}]][digits]
		// The first character that does not fit this form stops the scan.
		// If 'radix' is between 2 and 36, then it is used as the base of the number.
		// If 'radix' is 0, the initial characters of the string are used to determine the base.
		// If the first character is 0 and the second character is not 'x' or 'X', the string is interpreted as an octal integer;
		// otherwise, it is interpreted as a decimal number. If the first character is '0' and the second character is 'x' or 'X',
		// the string is interpreted as a hexadecimal integer. If the first character is '1' through '9', the string is interpreted
		// as a decimal integer. The letters 'a' through 'z' (or 'A' through 'Z') are assigned the values 10 through 35; only letters
		// whose assigned values are less than 'radix' are permitted.
		template <typename Int, typename Iter> bool ExtractInt(Int& intg, int radix, Iter& src, char const* delim = 0)
		{
			delim = Delim(delim);
			FindFirstNotOfAdv(src, delim);
			char str[512] = {}, *ptr; int i = 0;
			if (*src == '+' || *src == '-') { str[i++] = *src; ++src; }
			if (radix == 0)
			{
				if      (*src == '0')                { radix = 8; ++src; if (*src == 'x' || *src == 'X') {radix = 16; ++src;} }
				else if (*src >= '1' && *src <= '9') { radix = 10; }
				else return false;
			}
			for (; i < sizeof(str) && *src; ++src)
			{
				int ch = ::toupper(*src);
				int dec_ch = ch - '0', hex_ch = ch - 'A';
				if (dec_ch >= 0 && dec_ch <= 9  && (dec_ch     ) < radix) { str[i++] = static_cast<char>(ch); continue; }
				if (hex_ch >= 0 && hex_ch <= 25 && (hex_ch + 10) < radix) { str[i++] = static_cast<char>(ch); continue; }
				break;
			}
			if (i == sizeof(str)) return false;

			// Careful here. if you're reading a number larger than the max value for 'Int' you'll get MAX_VALUE
			// i.e. reading a hex value greater than 0x7FFFFFFF into an int will return 0x7FFFFFFF
			intg = as<Int>(str, &ptr, radix);
			return ptr != str;
		}
		template <typename Int, typename Iter> inline bool ExtractIntC(Int& intg, int radix, Iter src, char const* delim = 0)
		{
			return ExtractInt(intg, radix, src, delim);
		}

		// This is basically a convenience wrapper around ExtractInt
		template <typename Enum, typename Iter> bool ExtractEnumValue(Enum& enum_, Iter& src, char const* delim = 0)
		{
			long val;
			if (!ExtractInt(val, 10, src, delim)) return false;
			enum_ = static_cast<Enum>(val);
			return true;
		}
		template <typename Enum, typename Iter> bool ExtractEnumValueC(Enum& enum_, Iter src, char const* delim = 0)
		{
			return ExtractEnum(enum_, src, delim);
		}

		// Extracts an enum by its string name. For use with 'PR_ENUM' defined enums
		template <typename Enum, typename Iter> bool ExtractEnum(Enum& enum_, Iter& src, char const* delim = 0)
		{
			decltype(*src) val[512];
			if (!ExtractIdentifier(val, src, delim)) return false;
			enum_ = Enum::Parse(val);
			return true;
		}
		template <typename Enum, typename Iter> bool ExtractEnumC(Enum& enum_, Iter src, char const* delim = 0)
		{
			return ExtractEnum(enum_, src, delim);
		}

		// Extract a floating point number from 'src'
		// Expects 'src' to point to a string of the following form:
		// [delim] [{+|-}][digits][.digits][{d|D|e|E}[{+|-}]digits]
		// The first character that does not fit this form stops the scan.
		// If no digits appear before the '.' character, at least one must appear after the '.' character.
		// The decimal digits can be followed by an exponent, which consists of an introductory letter (d, D, e, or E) and an optionally signed integer.
		// If neither an exponent part nor a '.' character appears, a '.' character is assumed to follow the last digit in the string.
		template <typename Real, typename Iter> bool ExtractReal(Real& real, Iter& src, char const* delim = 0)
		{
			delim = Delim(delim);
			FindFirstNotOfAdv(src, delim);
			char str[512] = {}, *ptr; int i = 0;
			if (*src == '+' || *src == '-') { str[i++] = *src; ++src; }
			for (; i < sizeof(str) && IsDecDigit(*src); ++src) { str[i++] = *src; }
			if (i < sizeof(str) && *src == '.')
			{
				str[i++] = *src; ++src;
				for (; i < sizeof(str) && IsDecDigit(*src); ++src) { str[i++] = *src; }
			}
			if (i < sizeof(str) && (*src == 'd' || *src == 'D' || *src == 'e' || *src == 'E'))
			{
				str[i++] = *src; ++src;
				if (*src == '+' || *src == '-') { str[i++] = *src; ++src; }
				for (; IsDecDigit(*src); ++src) { str[i++] = *src; }
			}
			if (i == sizeof(str)) return false;
			real = as<Real>(str, &ptr);
			return ptr != str;
		}
		template <typename Real, typename Iter> inline bool ExtractRealC(Real& real, Iter src, char const* delim = 0)
		{
			return ExtractReal(real, src, delim);
		}

		// Extract an array of bools from 'src' template <typename Bool, typename Iter>
		template <typename Bool, typename Iter> inline bool ExtractBoolArray(Bool* bool_, size_t count, Iter& src, char const* delim = 0)
		{
			while (count--) if (!ExtractBool(*bool_++, src, delim)) return false;
			return true;
		}
		template <typename Bool, typename Iter> inline bool ExtractBoolArrayC(Bool* bool_, size_t count, Iter src, char const* delim = 0)
		{
			return ExtractBoolArray(bool_, count, src, delim);
		}
		
		// Extract an array of integral numbers from 'src'
		template <typename Int, typename Iter> inline bool ExtractIntArray(Int* intg, size_t count, int radix, Iter& src, char const* delim = 0)
		{
			while (count--) if (!ExtractInt(*intg++, radix, src, delim)) return false;
			return true;
		}
		template <typename Int, typename Iter> inline bool ExtractIntArrayC(Int* intg, size_t count, int radix, Iter src, char const* delim = 0)
		{
			return ExtractIntArray(intg, count, radix, src, delim); 
		}
		
		// Extract an array of real numbers from 'src'
		template <typename Real, typename Iter> inline bool ExtractRealArray(Real* real, size_t count, Iter& src, char const* delim = 0)
		{
			while (count--) if (!ExtractReal(*real++, src, delim)) return false;
			return true;
		}
		template <typename Real, typename Iter> inline bool ExtractRealArrayC(Real* real, size_t count, Iter src, char const* delim = 0)
		{
			return ExtractRealArray(real, count, src, delim);
		}
		
		// Read from 'src' to the end of a numeric constant
		// e.g. -3.12e+03F, 0x1234abcd, 077, 0b1011011, 3.14, etc
		// Returns:
		//  -an iterator to the first non-numeric constant character
		//  -the type of constant
		//  -whether the value is an unsigned value
		//  -whether the value is a 64bit type
		template <typename Iter> inline size_t ParseNumber(Iter& src, NumType::Type& type, bool& unsignd, bool& longlong)
		{
			type = NumType::Dec;
			longlong = false;
			unsignd = false;
			size_t count = 0;
			bool exp = false;
			
			// Optional sign character
			if (*src == '+' || *src == '-')
			{
				// Use operator [] to prevent advancing 'src' for invalid numbers
				// This won't work for pure streams, however buffered stream types
				// can make use of this.
				if (!IsDecDigit(src[1])) return count;
				++src; ++count;
			}
			// Numeric constants all begin with a digit
			else
			{
				if (!IsDecDigit(*src)) return count;
			}
			
			// If the first digit is zero, then the number may be of a different base
			if (*src == '0')
			{
				type = NumType::Oct; ++src; ++count;
				if      (*src == 'x' || *src == 'X') { type = NumType::Hex; ++src; ++count; }
				else if (*src == 'b' || *src == 'B') { type = NumType::Bin; ++src; ++count; }
				else if (*src == '.')                { type = NumType::FP;  ++src; ++count; }
			}
			
			// Accept a string of digits
			for (;;)
			{
				if      (type == NumType::Dec) { while (IsDecDigit(*src)) {++src; ++count;} }
				else if (type == NumType::FP ) { while (IsDecDigit(*src)) {++src; ++count;} }
				else if (type == NumType::Hex) { while (IsHexDigit(*src)) {++src; ++count;} }
				else if (type == NumType::Oct) { while (IsOctDigit(*src)) {++src; ++count;} }
				else if (type == NumType::Bin) { while (IsBinDigit(*src)) {++src; ++count;} }
				else break;
				
				if      ((*src == '.'               ) && (type == NumType::Dec                       )        ) { type = NumType::FP; ++src; ++count; }
				else if ((*src == 'e' || *src == 'E') && (type == NumType::Dec || type == NumType::FP) && !exp) { type = NumType::FP; ++src; ++count; exp = true; if (*src == '+' || *src == '-') {++src;++count;} }
				else break;
			}
			
			// Read the number suffix
			if ((*src == 'f' || *src == 'F') && (type == NumType::Dec || type == NumType::FP)) { type = NumType::FP; ++src; ++count; return count; }
			if ((*src == 'u' || *src == 'U') && (                        type != NumType::FP)) { unsignd = true; ++src; ++count; }
			if ((*src == 'l' || *src == 'L') && (                        type != NumType::FP)) { ++src; ++count; longlong = (*src=='l'||*src=='L'); if (longlong) {++src;++count;} }
			return count;
		}
		template <typename Iter> inline size_t ParseNumberC(Iter src, NumType::Type& type, bool& unsignd, bool& longlong)
		{
			return ParseNumber(src, type, unsignd, longlong);
		}
		
		// Extract a numeric constant. On return, 'fp' will indicate what type of constant was extracted
		// (integral for floating point) and one of 'ivalue' or 'fvalue' will contain the corresponding value
		// the other value will be unchanged (this allows unions to work)
		template <typename Int, typename Real, typename Iter> bool ExtractNumber(Int& ivalue, Real& fvalue, bool& fp, Iter& src, char const* delim = 0)
		{
			delim = Delim(delim);
			FindFirstNotOfAdv(src, delim);
			
			// Buffer the number
			iter_buffer<Iter, char, 256> buf(src);
			NumType::Type type; bool usign; bool llong;
			ParseNumber(buf, type, usign, llong);
			if (buf.empty() || buf.full()) return false;
			
			fp = type == NumType::FP;
			
			// Convert the string to a value
			if (fp) { fvalue = as<Real>(buf.m_buf, 0); }
			else if (usign)
			{
				errno = 0;
				if (!llong)                    ivalue = static_cast<Int>(as<unsigned long     >(buf.m_buf, 0, type));
				if ( llong || errno == ERANGE) ivalue = static_cast<Int>(as<unsigned long long>(buf.m_buf, 0, type));
			}
			else
			{
				errno = 0;
				if (!llong)                    ivalue = static_cast<Int>(as<long     >(buf.m_buf, 0, type));
				if ( llong || errno == ERANGE) ivalue = static_cast<Int>(as<long long>(buf.m_buf, 0, type));
			}
			return true;
		}
		template <typename Int, typename Real, typename Iter> inline bool ExtractNumberC(Int& ivalue, Real& fvalue, bool& fp, Iter src, char const* delim = 0)
		{
			return ExtractNumber(ivalue, fvalue, fp, src, delim);
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_str_prstringextract)
		{
			using namespace pr;
			using namespace pr::str;

			{//ExtractLine
				wchar_t const* src = L"abcefg\n";
				char line[10];
				PR_CHECK(ExtractLineC(line, src, false), true); PR_CHECK(Equal(line, "abcefg")   ,true);
				PR_CHECK(ExtractLineC(line, src, true) , true); PR_CHECK(Equal(line, "abcefg\n") ,true);
			}
			{//ExtractIdentifier
				wchar_t const* src = L"\t\n\r Ident { 10.9 }";
				wchar_t const* s = src;
				char identifier[10];
				PR_CHECK(ExtractIdentifier(identifier, s) ,true);
				PR_CHECK(Equal(identifier, "Ident")       ,true);
			}
			{//ExtractString
				std::wstring src = L"\n \"String String\" ";
				wchar_t const* s = src.c_str();
				char string[20];
				PR_CHECK(ExtractString(string, s)       ,true);
				PR_CHECK(Equal(string, "String String") ,true);
			}
			{//ExtractCString
				std::wstring wstr;
				PR_CHECK(ExtractCStringC(wstr, "  \" \\\\\\b\\f\\n\\r\\t\\v\\?\\'\\\" \" ") ,true);
				PR_CHECK(Equal(wstr, " \\\b\f\n\r\t\v\?\'\" ")                              ,true);

				char narr[2];
				PR_CHECK(ExtractCStringC(narr, "  '\\n'  ") ,true);
				PR_CHECK(Equal(narr, "\n")                  ,true);
				PR_CHECK(ExtractCStringC(narr, "  'a'  ")   ,true);
				PR_CHECK(Equal(narr, "a")                   ,true);
			}
			{//ExtractBool
				char  src[] = "true false 1";
				char const* s = src;
				bool  bbool = 0;
				int   ibool = 0;
				float fbool = 0;
				PR_CHECK(ExtractBool(bbool, s) ,true); PR_CHECK(bbool ,true);
				PR_CHECK(ExtractBool(ibool, s) ,true); PR_CHECK(ibool ,0   );
				PR_CHECK(ExtractBool(fbool, s) ,true); PR_CHECK(fbool ,1.0f);
			}
			{//ExtractInt
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
			}
			{// ExtractReal
				float f = 0; double d = 0; int i = 0;
				{
					char src[] = "\n 3.14 ";
					PR_CHECK(ExtractRealC(f ,src) ,true); PR_CLOSE(f, 3.14f, 0.00001f);
					PR_CHECK(ExtractRealC(d ,src) ,true); PR_CLOSE(d, 3.14 , 0.00001);
					PR_CHECK(ExtractRealC(i ,src) ,true); PR_CHECK(i, 3);
				}
				{
					char src[] = "-1.25e-4Z", *ptr = src;
					PR_CHECK(ExtractReal(d, ptr) ,true);
					PR_CHECK(d ,-1.25e-4);
					PR_CHECK(*ptr, 'Z');
				}
			}
			{//ExtractBoolArray
				char src[] = "\n true 1 TRUE ";
				float f[3] = {0,0,0};
				PR_CHECK(ExtractBoolArrayC(f, 3, src), true);
				PR_CHECK(f[0], 1.0f);
				PR_CHECK(f[1], 1.0f);
				PR_CHECK(f[2], 1.0f);
			}
			{//ExtractRealArray
				char src[] = "\n 3.14\t3.14e0\n-3.14 ";
				float  f[3] = {0,0,0};
				double d[3] = {0,0,0};
				int    i[3] = {0,0,0};
				PR_CHECK(ExtractRealArrayC(f, 3, src) ,true);    PR_CLOSE(f[0], 3.14f, 0.00001f); PR_CLOSE(f[1], 3.14f, 0.00001f); PR_CLOSE(f[2], -3.14f, 0.00001f);
				PR_CHECK(ExtractRealArrayC(d, 3, src) ,true);    PR_CLOSE(d[0], 3.14 , 0.00001);  PR_CLOSE(d[1], 3.14 , 0.00001);  PR_CLOSE(d[2], -3.14 , 0.00001);
				PR_CHECK(ExtractRealArrayC(i, 3, src) ,true);    PR_CHECK(i[0], 3);               PR_CHECK(i[1] ,3);               PR_CHECK(i[2], -3);
			}
			{//ExtractIntArray
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
			{//ExtractNumber
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

#endif

