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
#include "pr/str/string_filter.h"

namespace pr::str
{
	// Ensure 'str' has a newline character at its end
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str& EnsureNewline(Str& str)
	{
		if (!Empty(str) && *(End(str) - 1) != '\n') Append(str, '\n');
		return str;
	}
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str EnsureNewline(Str const& str)
	{
		auto s = str;
		return EnsureNewline(s);
	}

	// Return true if 'src' contains 'what'
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline bool Contains(Str1 const& str, Str2 const& what)
	{
		return FindStr(Begin(str), End(str), what) != End(str);
	}
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline bool ContainsI(Str1 const& str, Str2 const& what)
	{
		return FindStrI(Begin(str), End(str), what) != End(str);
	}

	// Returns 0 if equal, -1 if lhs < rhs, or +1 if lhs > rhs
	template <typename Str1, typename Str2, typename Pred, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline int Compare(Str1 const& lhs, Str2 const& rhs, Pred pred)
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
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline int Compare(Str1 const& lhs, Str2 const& rhs)
	{
		return Compare(lhs, rhs, [](auto lhs, auto rhs)
		{
			return (lhs > rhs) - (rhs > lhs);
		});
	}
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline int CompareI(Str1 const& lhs, Str2 const& rhs)
	{
		return Compare(lhs, rhs, [](auto lhs, auto rhs)
		{
			auto l = string_traits<Str1 const>::lwr(lhs);
			auto r = string_traits<Str2 const>::lwr(rhs);
			return (l > r) - (r > l);
		});
	}

	// Return the number of occurrences of 'what' in 'str'
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline size_t Count(Str1 const& str, Str2 const& what)
	{
		auto count = 0;
		auto i = Begin(str);
		auto iend = End(str);
		auto what_len = Size(what);
		for (i = FindStr(i, iend, what); i != iend; i = FindStr(i + what_len, iend, what), ++count) {}
		return count;
	}

	// Replace blocks of delimiter characters with a single delimiter 'ws_char'
	// 'preserve_newlines' if true, and '\n' is a delimiter, then a newline is added if 1 or more '\n' characters are found in a block
	template <typename Str1, typename Char, typename = std::enable_if_t<is_string_v<Str1>>>
	void CompressDelimiters(Str1& str, Char const* delim, Char ws_char, bool preserve_newlines)
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
			for (; beg != end && *FindChar(delim, *beg) != 0; ++beg) { nl |= *beg == '\n'; }

			// If 1 or more newlines were detected, add a '\n'
			if (beg != end)
				*out++ = preserve_newlines && nl ? '\n' : ws_char;
		}

		// Shrink the string
		Resize(str, size_t(out - Begin(str)));
	}
	template <typename Str, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
	inline void CompressDelimiters(Str& str, Char ws_char, bool preserve_newlines)
	{
		return CompressDelimiters(str, Delim<Char>(nullptr), ws_char, preserve_newlines);
	}

	// Convert a string to tokens, returning each token via 'token_cb'. Sig: token_cb(char const* s, char const* e);
	template <typename Str, typename TokenCB, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
	void TokeniseCB(Str const& str, TokenCB token_cb, Char const* delim, bool remove_quotes = true)
	{
		auto s = BeginC(str);
		auto end = EndC(str);
		for (; s != end; s += int(s != end))
		{
			// Extract whole strings
			if (*s == '\"' || *s == '\'')
			{
				InLiteral lit;

				auto e = s;
				auto quote = *s;
				lit.WithinLiteralString(*s);
				for (++e; e != end && lit.WithinLiteralString(*e); ++e) {}
				if (*(e - 1) != quote) throw std::runtime_error("Incomplete string/character literal");
				token_cb(s + remove_quotes, e - remove_quotes);
				s = e;
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
	template <typename Str, typename StrCont, typename Char, typename = std::enable_if_t<is_string_v<Str>>>
	void Tokenise(Str const& str, StrCont& tokens, Char const* delim, bool remove_quotes = true)
	{
		using StrOut = typename StrCont::value_type;
		TokeniseCB(str, [&](Char const* s, Char const* e)
		{
			tokens.push_back(StrOut(s, e));
		}, delim, remove_quotes);
	}
	template <typename Str, typename StrCont>
	inline void Tokenise(Str const& str, StrCont& tokens, bool remove_quotes = true)
	{
		return Tokenise(str, tokens, Delim<typename string_traits<Str>::value_type>(), remove_quotes);
	}

	// Strip sections from a string
	// Pass null to 'block_start','block_end' or 'line' to ignore that section type
	template <typename Str, typename Char = typename string_traits<Str>::value_type, typename = std::enable_if_t<is_string_v<Str>>>
	Str& Strip(Str& str, Char const* block_start, Char const* block_end, Char const* line)
	{
		// TODO: replace this with the string_filter.h implementation

		if (Empty(str))
			return str;

		// Find the length of the block start/end and line section markers
		auto block = block_start != 0 && block_end != 0;
		auto lc_len  = line  ? Size(line)        : 0;
		auto bcs_len = block ? Size(block_start) : 0;
		auto bce_len = block ? Size(block_end)   : 0;

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
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str& StripCppComments(Str& str)
	{
		using Char = typename string_traits<Str>::value_type;
		Char const bs[] = {'/','*',0};
		Char const be[] = {'*','/',0};
		Char const ln[] = {'/','/',0};
		return Strip(str, bs, be, ln);
	}

	// Replace instances of 'what' with 'with' in-place
	// Returns the number of instances of 'what' that where replaced
	template <typename Str1, typename Str2, typename Str3, typename Pred, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2> && is_string_v<Str3>>>
	size_t Replace(Str1& str, Str2 const& what, Str3 const& with, Pred cmp)
	{
		if (Empty(str))
			return 0;

		auto str_len  = Size(str);
		auto what_len = Size(what);
		auto with_len = Size(with);

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
	template <typename Str1, typename Str2, typename Str3, typename Pred, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2> && is_string_v<Str3>>>
	size_t Replace(Str1 const& src, Str1& dst, Str2 const& what, Str3 const& with, Pred cmp)
	{
		Assign(dst, BeginC(src), EndC(src));
		return Replace(dst, what, with, cmp);
	}

	// Replace all instances of 'what' with 'with' (case sensitive)
	template <typename Str1, typename Str2, typename Str3, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2> && is_string_v<Str3>>>
	size_t Replace(Str1& str, Str2 const& what, Str3 const& with)
	{
		using Char1 = typename string_traits<Str1>::value_type;
		return Replace(str, what, with, EqualN<Char1 const*, Str2>);
	}
	template <typename Str1, typename Str2, typename Str3, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2> && is_string_v<Str3>>>
	size_t Replace(Str1 const& src, Str1& dst, Str2 const& what, Str3 const& with)
	{
		using Char1 = typename string_traits<Str1>::value_type;
		Assign(dst, Begin(src), End(src));
		return Replace(dst, what, with, EqualN<Char1 const*, Str2>);
	}

	// Replace all instances of 'what' with 'with' (case insensitive)
	template <typename Str1, typename Str2, typename Str3, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2> && is_string_v<Str3>>>
	size_t ReplaceI(Str1& str, Str2 const& what, Str3 const& with)
	{
		using Char1 = typename string_traits<Str1>::value_type;
		return Replace(str, what, with, EqualNI<Char1 const*, Str2>);
	}
	template <typename Str1, typename Str2, typename Str3, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2> && is_string_v<Str3>>>
	size_t ReplaceI(Str1 const& src, Str1& dst, Str2 const& what, Str3 const& with)
	{
		using Char1 = typename string_traits<Str1>::value_type;
		Assign(dst, Begin(src), End(src));
		return Replace(dst, what, with, EqualNI<Char1 const*, Str2>);
	}

	// Convert a normal string into a C-style string
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str StringToCString(Str const& src)
	{
		if (Empty(src))
			return src;

		Str dst = {};
		size_t len = 0;
		Escape<typename string_traits<Str>::value_type> esc;
		for (auto const* s = &src[0]; *s; ++s)
			esc.Translate(*s, dst, len);

		return dst;
	}

	// Convert a C-style string into a normal string
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str CStringToString(Str const& src)
	{
		using traits = string_traits<Str>;
		using Char = typename string_traits<Str>::value_type;

		if (Empty(src)) 
			return src;

		Str dst = {};
		size_t len = 0;
		Unescape<typename string_traits<Str>::value_type> unesc;
		for (auto const* s = &src[0]; *s; ++s)
			unesc.Translate(*s, dst, len);

		return dst;
	}

	// Look for 'identifier' within the range [ofs, ofs+count) of 'src'.
	// Returns the index of it's position or ofs+count if not found.
	// Identifier will be a complete identifier based on the character class IsIdentifier()
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline size_t FindIdentifier(Str1 const& src, Str2 const& identifier, size_t ofs, size_t count)
	{
		auto beg = Begin(src) + ofs;
		auto end = beg + count;
		auto len = Size(identifier);
		for (auto iter = beg; iter != end;)
		{
			// Find the next instance of 'identifier'
			auto ptr = FindStr(iter, end, identifier);
			if (ptr == end) break;
			iter = ptr + len;

			// Check for characters after. i.e. don't return "bobble" if "bob" is the identifier
			auto j = ptr + len;
			if (j != end && IsIdentifier(*j, false))
				continue;

			// Look for any characters before. i.e. don't return "plumbob" if "bob" is the identifier
			// This has to be a search, consider: ' 1token', ' _1token', and ' _1111token'.
			for (j = ptr; j != beg && IsIdentifier(*(j - 1), false); --j) {}
			if (j != ptr && IsIdentifier(*j, true))
				continue;

			// Found one
			return size_t(ptr - Begin(src));
		}
		return ofs + count;
	}
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline size_t FindIdentifier(Str1 const& src, Str2 const& identifier, size_t ofs)
	{
		return FindIdentifier(src, identifier, ofs, Size(src) - ofs);
	}
	template <typename Str1, typename Str2, typename = std::enable_if_t<is_string_v<Str1> && is_string_v<Str2>>>
	inline size_t FindIdentifier(Str1 const& src, Str2 const& identifier)
	{
		return FindIdentifier(src, identifier, 0);
	}

	// Return the next identifier in 'src', within the range [ofs, ofs + count)
	// Returns the range of the next identifier, or an empty range if no more identifiers are found.
	namespace impl
	{
		template <typename Char>
		std::basic_string_view<Char> NextIdentifier(std::basic_string_view<Char> src, size_t ofs, size_t count)
		{
			count = std::min(count, src.size() - ofs);
			if (ofs > src.size() || ofs + count > src.size())
				throw std::invalid_argument("'ofs' and/or 'count' exceed the range of 'src'");

			auto i = ofs;
			auto iend = ofs + count;

			// Find the start of the next identifier
			InLiteral lit;
			for (; i != iend; ++i)
			{
				// Do not find identifiers within literal strings
				if (lit.WithinLiteralString(src[i]))
					continue;

				// Identifiers cannot have digits preceeding them
				if (IsIdentifier(src[i], true) && (i == 0 || !IsIdentifier(src[i - 1], false)))
					break;
			}
			if (i == iend)
				return src.substr(src.size(), 0);

			auto b = i;

			// Find the range of the identifier
			for (; i != iend && IsIdentifier(src[i], false); ++i) {}

			// Return the found identifier
			return src.substr(b, i - b);
		}
	}
	inline std::string_view NextIdentifier(std::string_view src, size_t ofs, size_t count = ~0ULL)
	{
		return impl::NextIdentifier<char>(src, ofs, count);
	}
	inline std::wstring_view NextIdentifier(std::wstring_view src, size_t ofs, size_t count = ~0ULL)
	{
		return impl::NextIdentifier<wchar_t>(src, ofs, count);
	}

	// Add/Remove quotes from a string if it doesn't already have them
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str& Quotes(Str& str, bool add)
	{
		size_t i, len = Size(str);
		auto begin = Begin(str);
		if ( add && (len >= 2 && *begin == '\"' && *(begin + len - 1) == '\"')) return str; // already quoted
		if (!add && (len <  2 || *begin != '\"' || *(begin + len - 1) != '\"')) return str; // already not quoted
		if (add)
		{
			Resize(str, len+2, '\"');
			for (i = len; i-- != 0;) str[i+1] = str[i];
			str[0] = '\"';
		}
		else
		{
			for (i = 0; i != len-1; ++i) str[i] = str[i+1];
			Resize(str, len-2);
		}
		return str;
	}
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	inline Str Quotes(Str const& str, bool add)
	{
		Str s(str);
		return Quotes(s, add);
	}

	// Convert a size in bytes to a 'pretty' size in KB,MB,GB,etc
	// 'bytes' - the input data size
	// 'si' - true to use 1000bytes = 1kb, false for 1024bytes = 1kb
	// 'dp' - number of decimal places to use
	template <typename Str = std::string, typename = std::enable_if_t<is_string_v<Str>>>
	Str PrettyBytes(long long bytes, bool si, int dp)
	{
		using Char = typename string_traits<Str>::value_type;

		auto unit = si ? 1000 : 1024;
		if (bytes < unit)
		{
			Char dst[17] = {};
			sprintf(&dst[0], _countof(dst) - 1, "%lld%s", bytes, si?"B":"iB");
			return dst;
		}

		auto exp = int(::log(bytes) / ::log(unit));
		auto pretty_size = bytes/::pow(unit, exp);
		char prefix = "KMGTPE"[exp-1];

		Char fmt[33] = {};
		if (sprintf(&fmt[0], _countof(fmt) - 1, "%%1.%df%%c%%s", dp) < 0)
			throw std::runtime_error("PrettySize failed");
			
		Char buf[129] = {};
		auto len = sprintf(&buf[0], _countof(buf) - 1, fmt, pretty_size, prefix, si?"B":"iB");
		if (len < 0 || len >= _countof(buf) - 1)
			throw std::runtime_error("PrettySize failed");

		return buf;
	}

	// Convert a number into a 'pretty' number
	// e.g. 1.234e10 = 12,340,000,000
	// 'num' should be a number in base units
	// 'decade' is the power of 10 to round to
	// 'dp' is the number of decimal places to write
	// 'sep' is the separator character to use
	template <typename Str = std::string, typename = std::enable_if_t<is_string_v<Str>>>
	Str PrettyNumber(double num, long decade, int dp, char sep = ',')
	{
		using Char = typename string_traits<Str>::value_type;

		auto unit = ::pow(10, decade);
		auto pretty_size = num / unit;

		// Create the format string for the next sprintf
		Char fmt[33] = {};
		if (sprintf(&fmt[0], _countof(fmt) - 1, "%%1.%df", dp) < 0)
			throw std::runtime_error("PrettyNumber failed");

		// Printf the number into buf
		Char buf[129] = {};
		auto len = sprintf(&buf[0], _countof(buf) - 1, fmt, pretty_size);
		if (len < 0 || len >= _countof(buf) - 1)
			throw std::runtime_error("PrettyNumber failed");

		// Insert separators into buf
		if (sep != 0)
		{
			// Calculate the new string length
			// length, minus decimal point, minus digits after dp,
			// shifted by 1 because 'sep' inserted at zero-based character
			// index 3,6,9, div 3 digits per separator
			auto sep_count = (len - 1 - dp - 1) / 3;
			auto new_length = len + sep_count;
			if (new_length >= _countof(buf))
				throw std::runtime_error("PrettyNumber failed, number too long for internal buffer");

			if (new_length >= 0)
			{
				buf[new_length] = 0;

				// Expand buf, inserting separators
				for (int in = len, out = new_length, i = -dp - 1; in != out; ++i)
				{
					buf[--out] = buf[--in];
					if ((i%3) == 2)
						buf[--out] = sep;
				}
			}
		}
		return buf;
	}

	// Remove leading white space and trailing tabs around '\n' characters.
	template <typename Str, typename = std::enable_if_t<is_string_v<Str>>>
	Str& ProcessIndentedNewlines(Str& str)
	{
		// Allow new lines in simple strings. 
		size_t w = 0, c = 0;
		for (int i = 0, iend = static_cast<int>(Size(str)); i != iend; )
		{
			// If this is a newline
			if (str[i] == '\n')
			{
				// Write the newline just after the last non-whitespace character
				str[c++] = str[i];

				// Move the write pointer to just after the new line
				w = c;

				// Skip over trailing tab characters
				for (++i; i != iend && str[i] == '\t'; ++i) {}
			}
			else
			{
				// Copy from the read position to the write position
				str[w++] = str[i++];

				// Save the location of the last non whitespace character written
				if (!IsWhiteSpace(str[w-1]))
					c = w;
			}
		}
		Resize(str, w);
		return str;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::str
{
	PRUnitTest(StringUtilTests)
	{
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
			char const src[] = "tok0 tok1 tok2 \"tok3 and tok3\" tok4 'tok5 & tok6'";
			std::vector<std::string> tokens;
			Tokenise(src, tokens);
			PR_CHECK(tokens.size(), 6U);
			PR_CHECK(tokens[0].c_str(), "tok0"          );
			PR_CHECK(tokens[1].c_str(), "tok1"          );
			PR_CHECK(tokens[2].c_str(), "tok2"          );
			PR_CHECK(tokens[3].c_str(), "tok3 and tok3" );
			PR_CHECK(tokens[4].c_str(), "tok4"          );
			PR_CHECK(tokens[5].c_str(), "tok5 & tok6"   );
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
			PR_CHECK(2  == FindIdentifier(" 1token"        , "token"), true);
			PR_CHECK(2  == FindIdentifier(" 1token"        , "token", 2), true);
			PR_CHECK(8  == FindIdentifier(" 1token2"       , "token"), true);
			PR_CHECK(9  == FindIdentifier(" 1token2 token ", "token"), true);
			PR_CHECK(2  == FindIdentifier(" 1token2"       , "token", 2, 5), true);
			PR_CHECK(8  == FindIdentifier(" _1token"       , "token"), true);
			PR_CHECK(3  == FindIdentifier(" _1token"       , "token", 3), true);
			PR_CHECK(9  == FindIdentifier(" _1token2"      , "token", 3), true);
			PR_CHECK(3  == FindIdentifier(" _1token2"      , "token", 3, 5), true);
			PR_CHECK(8  == FindIdentifier(" _1token2"      , "token", 0, 8), true);
			PR_CHECK(11 == FindIdentifier(" _1111token"    , "token"), true);
			PR_CHECK(6  == FindIdentifier(" _1111token"    , "token", 2), true);
		}
		{// NextIdentifier
			char const str[] = "_1st,;=2nd\n_3rd";
			size_t idx = 0;

			auto ident = NextIdentifier(str, idx);
			PR_CHECK(ident == "_1st", true);
			idx = ident.size() + ident.data() - &str[0];

			ident = NextIdentifier(str, idx);
			PR_CHECK(ident == "_3rd", true);
			idx = ident.size() + ident.data() - &str[0];

			ident = NextIdentifier(str, idx);
			PR_CHECK(ident.empty(), true);
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
		{// ProcessIndentedNewlines
			std::string str = "\nwords    and     \n\t\t\tmore  words  \n\t\t and more\nwords\n";
			ProcessIndentedNewlines(str);
			PR_CHECK(str, "\nwords    and\nmore  words\n and more\nwords\n");
		}
	}
}
#endif


