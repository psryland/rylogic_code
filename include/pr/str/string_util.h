//**********************************
// String Util
//  Copyright (c) Rylogic Ltd 2015
//**********************************
// Utility string functions that operate on:
//   std::string, std::wstring, pr::string<>, etc
//   char[], wchar_t[], etc
//   char*, wchar_t*, etc
// Note: char-array strings are not handled as special cases because there is no
// guarantee that the entire buffer is filled by the string, the null terminator
// may be midway through the buffer

#pragma once

#include <cmath>
#include "pr/str/string_core.h"

namespace pr
{
	namespace str
	{
		// Ensure 'str' has a newline character at its end
		template <typename Str, typename Char = traits<Str>::value_type> inline Str& EnsureNewline(Str& str)
		{
			if (!Empty(str) && *(End(str) - 1) != Char('\n')) Append(str, '\n');
			return str;
		}
		template <typename Str, typename Char = Str::value_type> inline Str EnsureNewline(Str const& str)
		{
			auto s = str;
			return EnsureNewline(s);
		}

		// Return true if 'src' contains 'what'
		template <typename Str1, typename Str2> inline bool Contains(Str1& str, Str2& what)
		{
			return FindStr(Begin(str), End(str), what) != EndC(str);
		}
		template <typename Str1, typename Str2> inline bool ContainsI(Str1& str, Str2& what)
		{
			return FindStrI(Begin(str), End(str), what) != End(str);
		}

		// Return the lexicographical comparison of two strings.
		// Returns 0 if equal, -1 if lhs < rhs, or +1 if lhs > rhs
		template <typename Str1, typename Str2, typename Pred> inline int Compare(Str1& lhs, Str2& rhs, Pred pred)
		{
			auto l = Begin(lhs); auto lend = End(lhs);
			auto r = Begin(rhs); auto rend = End(rhs);
			for (; l != lend && r != rend; ++l, ++r)
			{
				int comp = pred(*l, *r);
				if (comp < 0) { l = lend; break; }
				if (comp > 0) { r = rend; break; }
			}
			return (r == rend) - (l == lend);
		}
		template <typename Str1, typename Str2> inline int Compare(Str1& lhs, Str2& rhs)
		{
			using Char1 = traits<Str1>::value_type;
			using Char2 = traits<Str2>::value_type;
			return Compare(lhs, rhs, [](Char1 lhs, Char2 rhs){ return (lhs > rhs) - (rhs > lhs); });
		}
		template <typename Str1, typename Str2> inline int CompareI(Str1& lhs, Str2& rhs)
		{
			using Char1 = traits<Str1>::value_type;
			using Char2 = traits<Str2>::value_type;
			return Compare(lhs, rhs, [](Char1 lhs, Char2 rhs)
				{
					auto l = char_traits<Char1>::lwr(lhs);
					auto r = char_traits<Char2>::lwr(rhs);
					return (l > r) - (r > l);
				});
		}

		// Return the number of occurrences of 'what' in 'str'
		template <typename Str1, typename Str2> size_t Count(Str1& str, Str2& what)
		{
			auto count = 0;
			auto what_len = Length(what);
			auto i = Begin(str); auto iend = End(str);
			for (i = FindStr(i, iend, what); i != iend; i = FindStr(i + what_len, iend, what), ++count) {}
			return count;
		}

		// Replace blocks of delimiter characters with a single delimiter 'ws_char'
		// 'preserve_newlines' if true, and '\n' is a delimiter, then a newline is added if 1 or more '\n' characters are found in a block
		template <typename Str1, typename Char> void CompressDelimiters(Str1& str, Char const* delim, Char ws_char, bool preserve_newlines)
		{
			auto beg = Begin(str);
			auto end = End(str);
			auto out = Begin(str);
			auto nl = false;

			// Skip initial delimiters
			for (; beg != end && *FindChar(delim, *beg) != 0; ++beg) {}

			for (; beg != end; )
			{
				// While *beg is not a delimiter, copy to 'out'
				for (; beg != end && *FindChar(delim, *beg) == 0; ++beg) { *out++ = *beg; }

				// While *beg is a delimiter, advance 'beg'
				for (; beg != end && *FindChar(delim, *beg) != 0; ++beg) { nl |= *beg == Char('\n'); }

				// If 1 or more newlines were detected, add a '\n'
				if (beg != end)
					*out++ = preserve_newlines && nl ? Char('\n') : ws_char;
			}

			// Shrink the string
			Resize(str, size_t(out - Begin(str)));
		}
		template <typename Str, typename Char> inline void CompressDelimiters(Str& str, Char ws_char, bool preserve_newlines)
		{
			return CompressDelimiters(str, Delim<traits<Str>::value_type>(nullptr), ws_char, preserve_newlines);
		}

		// Convert a string to tokens, returned each token via 'token_cb'.
		// token_cb(char const* s, char const* e);
		template <typename Str, typename TokenCB, typename Char = traits<Str>::value_type> void TokeniseCB(Str const& str, TokenCB token_cb, Char const* delim, bool remove_quotes = true)
		{
			auto s = BeginC(str);
			auto end = EndC(str);
			for (; s != end; ++s)
			{
				// Extract whole strings
				if (*s == '"')
				{
					auto e = s;
					if (remove_quotes) ++s;
					for (++e; e != end && *e != '"'; ++e) {}
					if (*e != '"') throw std::exception("Partial literal string");
					token_cb(s, e + !remove_quotes);
					s = ++e;
					continue;
				}

				// Extract blocks of non-delimiters
				if (*FindChar(delim, *s) == 0)
				{
					auto e = s;
					for (; e != end && *FindChar(delim, *e) == 0; ++e) {}
					token_cb(s, e);
					s = --e;
					continue;
				}
			}
		}
		template <typename Str, typename StrCont, typename Char = traits<Str>::value_type> void Tokenise(Str const& str, StrCont& tokens, Char const* delim, bool remove_quotes = true)
		{
			using StrOut = StrCont::value_type;
			TokeniseCB(str, [&](Char const* s, Char const* e){ tokens.push_back(StrOut(s, e)); }, delim, remove_quotes);
		}
		template <typename Str, typename StrCont> inline void Tokenise(Str const& str, StrCont& tokens, bool remove_quotes = true)
		{
			return Tokenise(str, tokens, Delim<traits<Str>::value_type>(), remove_quotes);
		}

		// Strip sections from a string
		// Pass null to 'block_start','block_end' or 'line' to ignore that section type
		template <typename Str, typename Char = traits<Str>::value_type> Str& Strip(Str& str, Char const* block_start, Char const* block_end, Char const* line)
		{
			if (Empty(str))
				return str;

			// Find the length of the block start/end and line section markers
			auto block = block_start != 0 && block_end != 0;
			auto lc_len  = line  ? Length(line)        : 0;
			auto bcs_len = block ? Length(block_start) : 0;
			auto bce_len = block ? Length(block_end)   : 0;

			// Compress 'str' by stripping out the characters within the marked sections
			Char* out = &str[0];
			for (Char const *s = &str[0]; *s;)
			{
				if (line && EqualN(line, s, lc_len))
				{
					for (; *s && (*s != '\n' && *s != '\r'); ++s) {}
					for (; *s && (*s == '\n' || *s == '\r'); ++s) {}
				}
				else if (block && EqualN(block_start, s, bcs_len))
				{
					for (; *s && !EqualN(block_end, s, bce_len); ++s) {}
					s += bce_len;
				}
				else
				{
					*out++ = *s++;
				}
			}
			Resize(str, size_t(out - &str[0]));
			return str;
		}

		// Strip C++ style comments from a string
		template <typename Str> inline Str& StripCppComments(Str& str)
		{
			using Char = traits<Str>::value_type;
			Char const bs[] = {'/','*',0};
			Char const be[] = {'*','/',0};
			Char const ln[] = {'/','/',0};
			return Strip(str, bs, be, ln);
		}

		// Replace instances of 'what' with 'with' in-place
		// Returns the number of instances of 'what' that where replaced
		template <typename Str1, typename Str2, typename Str3, typename Pred, typename Char1 = traits<Str1>::value_type>
		size_t Replace(Str1& str, Str2 const& what, Str3 const& with, Pred cmp)
		{
			if (Empty(str))
				return 0;

			auto str_len  = Length(str);
			auto what_len = Length(what);
			auto with_len = Length(with);

			// This in an in place substitution so we need to copy from the
			// start or end depending on whether 'with' is longer or shorter than 'what'
			auto count = size_t();
			if (what_len >= with_len)
			{
				auto out = &str[0];
				for (auto s = &str[0]; *s;)
				{
					if (!cmp(s, what, what_len))           { *out++ = *s++; continue; }
					for (size_t i = 0; i != with_len; ++i) { *out++ = with[i]; }
					s += what_len;
					++count;
				}

				auto new_size = str_len - what_len*count + with_len*count;
				Resize(str, new_size);
			}
			else
			{
				// Note that if count == 0, then 's != out' in the for will be false immediately
				count = Count(str, what);
				auto new_size = str_len + count * (with_len - what_len);

				// Grow 'str' to allow for count * (with_len - what_len) extra characters
				Resize(str, new_size);

				// Back fill
				auto out = &str[0] + new_size;
				for (auto s = &str[0] + str_len; s != out;)
				{
					// Back fill chars until s - what_len points to 'what'
					// Note: s - what_len >= &str[0] because there is >0 instances of 'what' in str
					if (!cmp(s - what_len, what, what_len)) { *(--out) = *(--s); continue; }
					for (auto i = with_len; i-- != 0;)      { *(--out) = with[i]; } // Insert 'with' into 'str'
					s -= what_len;
				}
			}
			return count;
		}
		template <typename Str1, typename Str2, typename Str3, typename Pred>
		size_t Replace(Str1 const& src, Str1& dst, Str2 const& what, Str3 const& with, Pred cmp)
		{
			Assign(dst, BeginC(src), EndC(src));
			return Replace(dst, what, with, cmp);
		}

		// Replace all instances of 'what' with 'with' (case sensitive)
		template <typename Str1, typename Str2, typename Str3, typename Char1 = traits<Str1>::value_type>
		size_t Replace(Str1& str, Str2 const& what, Str3 const& with)
		{
			return Replace(str, what, with, EqualN<Char1 const*, Str2>);
		}
		template <typename Str1, typename Str2, typename Str3>
		size_t Replace(Str1 const& src, Str1& dst, Str2 const& what, Str3 const& with)
		{
			Assign(dst, BeginC(src), EndC(src));
			return Replace(dst, what, with, EqualN<Char1 const*, Str2>);
		}

		// Replace all instances of 'what' with 'with' (case insensitive)
		template <typename Str1, typename Str2, typename Str3, typename Char1 = traits<Str1>::value_type>
		size_t ReplaceI(Str1& str, Str2 const& what, Str3 const& with)
		{
			return Replace(str, what, with, EqualNI<Char1 const*, Str2>);
		}
		template <typename Str1, typename Str2, typename Str3>
		size_t ReplaceI(Str1 const& src, Str1& dst, Str2 const& what, Str3 const& with)
		{
			Assign(dst, BeginC(src), EndC(src));
			return Replace(dst, what, with, EqualNI<Char1 const*, Str2>);
		}

		//// Hash the contents of a string using crc32
		//template <typename Str, typename Char = traits<Str>::value_type> inline size_t Hash(Str const& src, size_t initial_crc = size_t(~0))
		//{
		//	if (Empty(src))
		//		return initial_crc;

		//	auto data = reinterpret_cast<void const*>(&src[0]);
		//	auto size = size_t(Length(src) * sizeof(Char));
		//	return Crc(data, size);
		//}

		// Convert a normal string into a C-style string
		template <typename Str1>
		inline Str1 StringToCString(Str1 const& src)
		{
			// This instance means 'Str2' has to be a std::string-like string
			Str1 dst;
			if (Empty(src)) return src;
			for (auto const* s = &src[0]; *s; ++s)
			{
				switch (*s)
				{
				default:   Append(dst,   *s); break;
				case '\a': Append(dst, '\\'); Append(dst, 'a' ); break;
				case '\b': Append(dst, '\\'); Append(dst, 'b' ); break;
				case '\f': Append(dst, '\\'); Append(dst, 'f' ); break;
				case '\n': Append(dst, '\\'); Append(dst, 'n' ); break;
				case '\r': Append(dst, '\\'); Append(dst, 'r' ); break;
				case '\t': Append(dst, '\\'); Append(dst, 't' ); break;
				case '\v': Append(dst, '\\'); Append(dst, 'v' ); break;
				case '\\': Append(dst, '\\'); Append(dst, '\\'); break;
				case '\?': Append(dst, '\\'); Append(dst, '?' ); break;
				case '\'': Append(dst, '\\'); Append(dst, '\''); break;
				case '\"': Append(dst, '\\'); Append(dst, '"' ); break;
				}
			}
			return dst;
		}

		// Convert a C-style string into a normal string
		// This is the std::string -esk version. char* version not implemented yet...
		template <typename Str1, typename Char = traits<Str1>::value_type>
		inline Str1 CStringToString(Str1 const& src)
		{
			// This instance means 'Str2' has to be a std::string-like string
			Str1 dst;
			if (Empty(src)) return dst;
			auto len = 0;
			for (auto const* s = &src[0]; *s; ++s)
			{
				if (*s == '\\')
				{
					switch (*++s)
					{
					default: break; // might be '0'
					case 'a':  Append(dst, len++, '\a'); break;
					case 'b':  Append(dst, len++, '\b'); break;
					case 'f':  Append(dst, len++, '\f'); break;
					case 'n':  Append(dst, len++, '\n'); break;
					case 'r':  Append(dst, len++, '\r'); break;
					case 't':  Append(dst, len++, '\t'); break;
					case 'v':  Append(dst, len++, '\v'); break;
					case '\'': Append(dst, len++, '\''); break;
					case '\"': Append(dst, len++, '\"'); break;
					case '\\': Append(dst, len++, '\\'); break;
					case '?':  Append(dst, len++, '\?'); break;
					case '0':
					case '1':
					case '2':
					case '3':
						{
							// ascii character in octal
							Char oct[9] = {};
							for (int i = 0; i != 8 && IsOctDigit(*s); ++i, ++s) oct[i] = Char(*s);
							Append(dst, len++, Char(char_traits<Char>::strtoul(oct, nullptr, 8)));
							break;
						}
					case 'x':
						{
							// ascii or unicode character in hex
							Char hex[9] = {};
							for (int i = 0; i != 8 && IsHexDigit(*s); ++i, ++s) hex[i] = Char(*s);
							Append(dst, len++, Char(char_traits<Char>::strtoul(hex, nullptr, 16)));
							break;
						}
					}
				}
				else
				{
					Append(dst, len++, *s);
				}
			}
			return dst;
		}

		// Look for 'identifier' within the range [ofs, ofs+count) of 'src'.
		// Returns the index of it's position or ofs+count if not found.
		// Identifier will be a complete identifier based on the character class IsIdentifier()
		template <typename Str1, typename Str2>
		inline size_t FindIdentifier(Str1 const& src, Str2 const& identifier, size_t ofs, size_t count)
		{
			auto begin  = BeginC(src);
			auto iter   = begin + ofs;
			auto end    = iter + count;
			auto id_len = Length(identifier);

			for (;;++iter)
			{
				// Find the next instance of 'identifier'
				iter = FindStr(iter, end, identifier);
				if (iter == end)
					return ofs + count;

				// Check for identifier characters after iter + id_len
				// i.e. don't return "bobble" if "bob" is the identifier
				auto j = iter + id_len;
				if (j != end && IsIdentifier(*j, false))
					continue;

				// Look for any identifier characters before iter
				// i.e. don't return "plumbob" if "bob" is the identifier
				for (j = iter; j > begin && IsIdentifier(*--j, false);) {}
				for (        ; j != iter && !IsIdentifier(*j, true); ++j) {}
				if  (j != iter)
					continue;

				// Found one
				return size_t(iter - begin);
			}
		}
		template <typename Str1, typename Str2> inline size_t FindIdentifier(Str1 const& src, Str2 const& identifier, size_t ofs)
		{
			return FindIdentifier(src, identifier, ofs, Length(src) - ofs);
		}
		template <typename Str1, typename Str2> inline size_t FindIdentifier(Str1 const& src, Str2 const& identifier)
		{
			return FindIdentifier(src, identifier, 0);
		}

		// Add/Remove quotes from a string if it doesn't already have them
		template <typename Str, typename Char = traits<Str>::value_type> inline Str& Quotes(Str& str, bool add)
		{
			size_t i, len = Length(str);
			auto begin = BeginC(str);
			if ( add && (len >= 2 && *begin == Char('\"') && *(begin + len - 1) == Char('\"'))) return str; // already quoted
			if (!add && (len <  2 || *begin != Char('\"') || *(begin + len - 1) != Char('\"'))) return str; // already not quoted
			if (add)
			{
				Resize(str, len+2);
				str[len+1] = Char('\"');
				for (i = len; i-- != 0;) str[i+1] = str[i];
				str[0] = Char('\"');
			}
			else
			{
				for (i = 0; i != len-1; ++i) str[i] = str[i+1];
				Resize(str, len-2);
			}
			return str;
		}
		template <typename Str> inline Str Quotes(Str const& str, bool add)
		{
			Str s(str);
			return Quotes(s, add);
		}

		// Convert a size in bytes to a 'pretty' size in KB,MB,GB,etc
		// 'bytes' - the input data size
		// 'si' - true to use 1000bytes = 1kb, false for 1024bytes = 1kb
		// 'dp' - number of decimal places to use
		template <typename Str = std::string, typename Char = traits<Str>::value_type> Str PrettyBytes(long long bytes, bool si, int dp)
		{
			auto unit = si ? 1000 : 1024;
			if (bytes < unit)
			{
				Char dst[16];
				sprintf(&dst[0], _countof(dst), "%lld%s", bytes, si?"B":"iB");
				return dst;
			}

			auto exp = int(::log(bytes) / ::log(unit));
			auto pretty_size = bytes/::pow(unit, exp);
			char prefix = "KMGTPE"[exp-1];

			Char fmt[32];
			if (sprintf(&fmt[0], _countof(fmt), "%%1.%df%%c%%s", dp) < 0)
				throw std::exception("PrettySize failed");
			
			Char buf[128];
			auto len = sprintf(&buf[0], _countof(buf), fmt, pretty_size, prefix, si?"B":"iB");
			if (len < 0 || len >= _countof(buf))
				throw std::exception("PrettySize failed");

			return buf;
		}

		// Convert a number into a 'pretty' number
		// e.g. 1.234e10 = 12,340,000,000
		// 'num' should be a number in base units
		// 'decade' is the power of 10 to round to
		// 'dp' is the number of decimal places to write
		// 'sep' is the separator character to use
		template <typename Str = std::string, typename Char = traits<Str>::value_type> Str PrettyNumber(double num, long decade, int dp, char sep = ',')
		{
			auto unit = ::pow(10, decade);
			auto pretty_size = num / unit;

			// Create the format string for the next sprintf
			Char fmt[32];
			if (sprintf(&fmt[0], _countof(fmt), "%%1.%df", dp) < 0)
				throw std::exception("PrettyNumber failed");

			// Printf the number into buf
			Char buf[128];
			auto len = sprintf(&buf[0], _countof(buf), fmt, pretty_size);
			if (len < 0 || len >= _countof(buf))
				throw std::exception("PrettyNumber failed");

			// Insert separators into buf
			if (sep != 0)
			{
				// length, minus decimal point, minus digits after dp,
				// shifted by 1 because 'sep' inserted at zero-based character
				// index 3,6,9, div 3 digits per separator
				auto sep_count = (len - 1 - dp - 1) / 3;
				buf[len + sep_count] = 0;

				// Expand buf, inserting separators
				for (int in = len, out = len + sep_count, i = -dp - 1; in != out; ++i)
				{
					buf[--out] = buf[--in];
					if ((i%3) == 2)
						buf[--out] = sep;
				}
			}
			return buf;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_str_string_util)
		{
			using namespace pr;
			using namespace pr::str;

			{// EnsureNewline
				std::string thems_without   = "without";
				std::wstring thems_with     = L"with\n";
				EnsureNewline(thems_without);
				EnsureNewline(thems_with);
				PR_CHECK(*(End(thems_without) - 1), '\n');
				PR_CHECK(*(End(thems_with)    - 1),L'\n');
			}
			{//Contains
				std::string src = "string";
				PR_CHECK(Contains(src, "in") , true);
				PR_CHECK(Contains(src, "ing"), true);
				PR_CHECK(ContainsI(src, "iNg"), true);
				PR_CHECK(ContainsI(src, "inG"), true);
			}
			{//Compare
				std::string src = "string1";
				PR_CHECK(Compare(src, "string2") , -1);
				PR_CHECK(Compare(src, "string1") ,  0);
				PR_CHECK(Compare(src, "string0") ,  1);
				PR_CHECK(Compare(src, "string11"), -1);
				PR_CHECK(Compare(src, "string")  ,  1);
				PR_CHECK(CompareI(src, "striNg2" ), -1);
				PR_CHECK(CompareI(src, "stRIng1" ),  0);
				PR_CHECK(CompareI(src, "strinG0" ),  1);
				PR_CHECK(CompareI(src, "string11"), -1);
				PR_CHECK(CompareI(src, "strinG"  ),  1);
			}
			{//Count
				char            aarr[] =  "s0tr0";
				wchar_t         warr[] = L"s0tr0";
				std::string     astr   =  "s0tr0";
				std::wstring    wstr   = L"s0tr0";
				PR_CHECK(Count(aarr, "0t"), 1U);
				PR_CHECK(Count(warr, "0") , 2U);
				PR_CHECK(Count(astr, "0") , 2U);
				PR_CHECK(Count(wstr, "0t"), 1U);
			}
			{//CompressDelimiters
				char src[] = "\n\nstuff     with  \n  white\n   space   \n in   ";
				char res[] = "stuff with\nwhite\nspace\nin";
				CompressDelimiters(src, " \n", ' ', true);
				PR_CHECK(src, res);
			}
			{//Tokenise
				char const src[] = "tok0 tok1 tok2 \"tok3 and tok3\" tok4";
				std::vector<std::string> tokens;
				Tokenise(src, tokens);
				PR_CHECK(tokens.size(), 5U);
				PR_CHECK(tokens[0].c_str(), "tok0"          );
				PR_CHECK(tokens[1].c_str(), "tok1"          );
				PR_CHECK(tokens[2].c_str(), "tok2"          );
				PR_CHECK(tokens[3].c_str(), "tok3 and tok3" );
				PR_CHECK(tokens[4].c_str(), "tok4"          );
			}
			{//StripComments
				char src[] =
					"//Line Comment\n"
					"Not a comment\n"
					"/* multi\n"
					"-line comment*/";
				char res[] = "Not a comment\n";
				PR_CHECK(StripCppComments(src), res);
			}
			{//Replace
				char src[] = "Bite my shiny donkey metal donkey";
				char res1[] = "Bite my shiny arse metal arse";
				char res2[] = "Bite my shiny donkey metal donkey";
				PR_CHECK(Replace(src, "donkey", "arse"), size_t(2));
				PR_CHECK(src, res1);
				PR_CHECK(Replace(src, "arse", "donkey"), size_t(2));
				PR_CHECK(src, res2);
				PR_CHECK(ReplaceI(src, "DONKEY", "arse"), size_t(2));
				PR_CHECK(src, res1);
			}
			{//ConvertToCString
				char const str[] = "Not a \"Cstring\". \a \b \f \n \r \t \v \\ \? \' ";
				char const res[] = "Not a \\\"Cstring\\\". \\a \\b \\f \\n \\r \\t \\v \\\\ \\? \\\' ";
				
				auto cstr1 = StringToCString<std::string>(str);
				auto str1  = CStringToString(cstr1);
				PR_CHECK(Equal(cstr1, res), true);
				PR_CHECK(Equal(str1, str), true);
			}
			{//FindIdentifier
				char const str[] = "aid id iid    id aiden";
				wchar_t id[] = L"id";
				size_t idx;
				idx = FindIdentifier(str, id);           PR_CHECK(idx, 4U);
				idx = FindIdentifier(str, id, idx+1, 3); PR_CHECK(idx, 8U);
				idx = FindIdentifier(str, id, idx+1);    PR_CHECK(idx, 14U);
				idx = FindIdentifier(str, id, idx+1);    PR_CHECK(idx, 22U);
			}
			{//Quotes
				char empty[3] = "";
				wchar_t one[4] = L"1";
				std::string two = "\"two\"";
				std::wstring three = L"three";
				PR_CHECK(str::Equal("\"\""       ,Quotes(empty ,true)), true);
				PR_CHECK(str::Equal("\"1\""      ,Quotes(one   ,true)), true);
				PR_CHECK(str::Equal("\"two\""    ,Quotes(two   ,true)), true);
				PR_CHECK(str::Equal(L"\"three\"" ,Quotes(three ,true)), true);
				PR_CHECK(str::Equal(""       ,Quotes(empty ,false)), true);
				PR_CHECK(str::Equal("1"      ,Quotes(one   ,false)), true);
				PR_CHECK(str::Equal("two"    ,Quotes(two   ,false)), true);
				PR_CHECK(str::Equal(L"three" ,Quotes(three ,false)), true);
			}
			{// Pretty Bytes
				auto pretty = [](long long bytes)
				{
					return PrettyBytes<>(bytes, true, 1) + " " + PrettyBytes<>(bytes, false, 1);
				};
				PR_CHECK(pretty(0)                   ,      "0B 0iB"      );
				PR_CHECK(pretty(27)                  ,     "27B 27iB"     );
				PR_CHECK(pretty(999)                 ,    "999B 999iB"    );
				PR_CHECK(pretty(1000)                ,   "1.0KB 1000iB"   );
				PR_CHECK(pretty(1023)                ,   "1.0KB 1023iB"   );
				PR_CHECK(pretty(1024)                ,   "1.0KB 1.0KiB"   );
				PR_CHECK(pretty(1728)                ,   "1.7KB 1.7KiB"   );
				PR_CHECK(pretty(110592)              , "110.6KB 108.0KiB" );
				PR_CHECK(pretty(7077888)             ,   "7.1MB 6.8MiB"   );
				PR_CHECK(pretty(452984832)           , "453.0MB 432.0MiB" );
				PR_CHECK(pretty(28991029248)         ,  "29.0GB 27.0GiB"  );
				PR_CHECK(pretty(1855425871872)       ,   "1.9TB 1.7TiB"   );
				PR_CHECK(pretty(9223372036854775807) ,   "9.2EB 8.0EiB"   );
			}
			{// Pretty Number
				PR_CHECK(PrettyNumber<std::wstring>(1.234e10, 6, 3)    , L"12,340.000"    );
				PR_CHECK(PrettyNumber<std::wstring>(1.234e10, 3, 3)    , L"12,340,000.000");
				PR_CHECK(PrettyNumber<std::wstring>(1.234e-10, -3, 3)  , L"0.000"         );
				PR_CHECK(PrettyNumber<std::wstring>(1.234e-10, -12, 3) , L"123.400"       );
			}
		}
	}
}
#endif


