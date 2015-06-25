//**********************************
// String Core
//  Copyright (c) Rylogic Ltd 2015
//**********************************
// Fundamental string functions that operate on:
//   std::string, std::wstring, pr::string<>, etc
//   char[], wchar_t[], etc
//   char*, wchar_t*, etc
// Note: char-array strings are not handled as special cases because there is no
// guarantee that the entire buffer is filled by the string, the null terminator
// may be midway through the buffer

#pragma once

#include <cstring>
#include <type_traits>
#include <locale>

namespace pr
{
	namespace str2
	{
		#pragma region Locale

		// A static instance of the locale, because this thing takes ages to construct
		inline std::locale const& locale()
		{
			static std::locale s_locale("");
			return s_locale;
		}

		// Narrow
		inline std::string Narrow(char const* from, std::size_t len = 0)
		{
			if (!from) return std::string();
			if (len == 0) len = strlen(from);
			return std::string(from, from+len);
		}
		inline std::string Narrow(wchar_t const* from, std::size_t len = 0)
		{
			if (!from) return std::string();
			if (len == 0) len = wcslen(from);
			std::vector<char> buffer(len + 1);
			std::use_facet<std::ctype<wchar_t>>(locale()).narrow(from, from + len, '_', &buffer[0]);
			return std::string(&buffer[0], &buffer[len]);
		}
		inline std::string Narrow(std::string const& from)  { return from; }
		inline std::string Narrow(std::wstring const& from) { return Narrow(from.c_str(), from.size()); }

		// Widen
		inline std::wstring Widen(wchar_t const* from, std::size_t len = 0)
		{
			if (!from) return std::wstring();
			if (len == 0) len = wcslen(from);
			return std::wstring(from, from+len);
		}
		inline std::wstring Widen(char const* from, std::size_t len = 0)
		{
			if (!from) return std::wstring();
			if (len == 0) len = strlen(from);
			std::vector<wchar_t> buffer(len + 1);
			std::use_facet<std::ctype<wchar_t>>(locale()).widen(from, from + len, &buffer[0]);
			return std::wstring(&buffer[0], &buffer[len]);
		}
		inline std::wstring Widen(std::wstring const& from) { return from; }
		inline std::wstring Widen(std::string const& from)  { return Widen(from.c_str(), from.size()); }

		#pragma endregion

		#pragma region Char Traits
		// char/wchar_t traits
		template <typename TChar> struct char_traits;
		template <> struct char_traits<char>
		{
			static char lwr(char ch) { return static_cast<char>(::tolower(ch)); }
			static char upr(char ch) { return static_cast<char>(::toupper(ch)); }
		};
		template <> struct char_traits<wchar_t>
		{
			static wchar_t lwr(wchar_t ch) { return static_cast<wchar_t>(towlower(ch)); }
			static wchar_t upr(wchar_t ch) { return static_cast<wchar_t>(towupper(ch)); }
		};
		template <> struct char_traits<char const> :char_traits<char>
		{};
		template <> struct char_traits<wchar_t const> :char_traits<wchar_t>
		{};
		#pragma endregion

		#pragma region String Traits
		template <typename Str> struct traits
		{
			using citer      = typename Str::const_iterator;
			using iter       = typename Str::iterator;
			using value_type = typename Str::value_type;
		};
		template <typename Char> struct traits<Char const*>
		{
			using citer      = Char const*;
			using iter       = Char*;
			using value_type = Char const;
		};
		template <typename Char> struct traits<Char*>
		{
			using citer      = Char const*;
			using iter       = Char*;
			using value_type = Char;
		};
		template <typename Char> struct traits<Char const* const> :traits<Char const*>
		{};
		template <typename Char> struct traits<Char* const> :traits<Char*>
		{};
		template <typename Char, size_t Len> struct traits<Char const[Len]> :traits<Char const*>
		{};
		template <typename Char, size_t Len> struct traits<Char[Len]> :traits<Char*>
		{};
		#pragma endregion

		#pragma region Character classes
		template <typename Char> inline bool IsNewLine(Char ch)                 { return ch == Char('\n'); }
		template <typename Char> inline bool IsLineSpace(Char ch)               { return ch == Char(' ') || ch == Char('\t') || ch == Char('\r'); }
		template <typename Char> inline bool IsWhiteSpace(Char ch)              { return IsLineSpace(ch) || IsNewLine(ch) || ch == Char('\v') || ch == Char('\f'); }
		template <typename Char> inline bool IsDecDigit(Char ch)                { return (ch >= Char('0') && ch <= Char('9')); }
		template <typename Char> inline bool IsBinDigit(Char ch)                { return (ch >= Char('0') && ch <= Char('1')); }
		template <typename Char> inline bool IsOctDigit(Char ch)                { return (ch >= Char('0') && ch <= Char('7')); }
		template <typename Char> inline bool IsHexDigit(Char ch)                { return IsDecDigit(ch) || (ch >= Char('a') && ch <= Char('f')) || (ch >= Char('A') && ch <= Char('F')); }
		template <typename Char> inline bool IsDigit(Char ch)                   { return IsDecDigit(ch); }
		template <typename Char> inline bool IsAlpha(Char ch)                   { return (ch >= Char('a') && ch <= Char('z')) || (ch >= Char('A') && ch <= Char('Z')); }
		template <typename Char> inline bool IsIdentifier(Char ch, bool first)  { return ch == Char('_') || IsAlpha(ch) || (!first && IsDigit(ch)); }
		#pragma endregion

		#pragma region Defaults
		// Return a pointer to delimiters, either the ones provided or the default ones
		template <typename Char> inline Char* Delim(Char* delim = nullptr)
		{
			static Char const default_delim[] = {Char(' '), Char('\t'), Char('\n'), Char('\r'), 0};
			return delim ? delim : default_delim;
		}
		#pragma endregion

		#pragma region Empty
		// Return true if 'str' is an empty string
		template <typename Str, typename Char = Str::value_type> inline bool Empty(Str const& str)
		{
			return str.empty();
		}
		template <typename Char> inline bool Empty(Char const* str)
		{
			return *str == 0;
		}
		#pragma endregion

		#pragma region Length
		// Return the length of the string, excluding the null terminator (same as strlen)
		template <typename Str, typename Char = Str::value_type> inline size_t Length(Str const& str)
		{
			return str.size();
		}
		template <typename Char> inline size_t Length(Char const* str)
		{
			auto s = str;
			for (;*s;++s){}
			return size_t(s - str);
		}
		inline size_t Length(char const* str)
		{
			return ::strlen(str);
		}
		inline size_t Length(wchar_t const* str)
		{
			return ::wcslen(str);
		}
		#pragma endregion

		#pragma region Range
		// Return a pointer to the start of the string
		template <typename Str, typename Char = Str::value_type> inline Char* Begin(Str& str)
		{
			return const_cast<Char*>(str.c_str());
		}
		template <typename Char> inline Char* Begin(Char* str)
		{
			return str;
		}

		// Return a pointer to the end of the string
		template <typename Str, typename Char = Str::value_type> inline Char* End(Str& str)
		{
			return Begin(str) + Length(str);
		}
		template <typename Char> inline Char* End(Char* str)
		{
			return Begin(str) + Length(str);
		}

		// Return a pointer to the 'N'th character or the end of the string, whichever is less
		template <typename Str, typename Char = Str::value_type> inline Char* End(Str& str, size_t N)
		{
			auto n = str.size() < N ? str.size() : N;
			return Begin(str) + n;
		}
		template <typename Char> inline Char* End(Char* str, size_t N)
		{
			auto s = str;
			for (;N-- != 0 && *s; ++s) {}
			return s;
		}
		#pragma endregion

		#pragma region Equal
		// Return true if lhs and rhs are equal
		template <typename Str1, typename Str2, typename Pred>
		inline bool Equal(Str1 const& str1, Str2 const& str2, Pred pred)
		{
			auto i = Begin(str1); auto i_end = End(str1);
			auto j = Begin(str2); auto j_end = End(str2);
			for (; !(i == i_end) && !(j == j_end) && pred(*i, *j); ++i, ++j) {}
			return i == i_end && j == j_end;
		}
		template <typename Char1, typename Char2, typename Pred>
		inline bool Equal(Char1 const* str1, Char2 const* str2, Pred pred)
		{
			auto i = Begin(str1);
			auto j = Begin(str2);
			for (; *i != 0 && *j != 0 && pred(*i, *j); ++i, ++j) {}
			return *i == 0 && *j == 0;
		}
		template <typename Str1, typename Str2>
		inline bool Equal(Str1 const& str1, Str2 const& str2)
		{
			using Char1 = traits<Str1>::value_type;
			using Char2 = traits<Str2>::value_type;
			return Equal(str1, str2, [](Char1 lhs, Char2 rhs){ return lhs == rhs; });
		}
		inline bool Equal(char const* str1, char const* str2)
		{
			return ::strcmp(str1, str2) == 0;
		}
		inline bool Equal(wchar_t const* str1, wchar_t const* str2)
		{
			return ::wcscmp(str1, str2) == 0;
		}
		#pragma endregion

		#pragma region EqualI
		// Return true if lhs and rhs are equal, ignoring case
		template <typename Str1, typename Str2>
		inline bool EqualI(Str1 const& str1, Str2 const& str2)
		{
			using Char1 = traits<Str1>::value_type;
			using Char2 = traits<Str2>::value_type;
			using CTraits1 = char_traits<Char1>;
			using CTraits2 = char_traits<Char2>;
			return Equal(str1, str2, [](Char1 lhs, Char2 rhs){ return CTraits1::lwr(lhs) == CTraits2::lwr(rhs); });
		}
		inline bool EqualI(char const* str1, char const* str2)
		{
			return ::_stricmp(str1, str2) == 0;
		}
		inline bool EqualI(wchar_t const* str1, wchar_t const* str2)
		{
			return ::_wcsicmp(str1, str2) == 0;
		}
		#pragma endregion

		#pragma region EqualN
		// Return true if lhs and rhs are equal, up to the given length
		template <typename Str1, typename Str2, typename Pred>
		inline bool EqualN(Str1 const& str1, Str2 const& str2, size_t length, Pred pred)
		{
			auto i = Begin(str1); auto i_end = End(str1, length);
			auto j = Begin(str2); auto j_end = End(str2, length);
			for (; length-- != 0 && !(i == i_end) && !(j == j_end) && pred(*i, *j); ++i, ++j) {}
			return length == size_t(-1) || (i == i_end && j == j_end);
		}
		template <typename Char1, typename Char2, typename Pred>
		inline bool EqualN(Char1 const* str1, Char2 const* str2, size_t length, Pred pred)
		{
			auto i = Begin(str1);
			auto j = Begin(str2);
			for (; length-- != 0 && *i != 0 && *j != 0 && pred(*i, *j); ++i, ++j) {}
			return length == size_t(-1) || (*i == 0 && *j == 0);
		}
		template <typename Str1, typename Str2>
		inline bool EqualN(Str1 const& str1, Str2 const& str2, size_t length)
		{
			using Char1 = traits<Str1>::value_type;
			using Char2 = traits<Str2>::value_type;
			return EqualN(str1, str2, length, [](Char1 lhs, Char2 rhs){ return lhs == rhs; });
		}
		inline bool EqualN(char const* str1, char const* str2, size_t length)
		{
			return ::strncmp(str1, str2, length) == 0;
		}
		inline bool EqualN(wchar_t const* str1, wchar_t const* str2, size_t length)
		{
			return ::wcsncmp(str1, str2, length) == 0;
		}
		#pragma endregion

		#pragma region EqualNI
		// Return true if lhs and rhs are equal, up to the given length
		template <typename Str1, typename Str2>
		inline bool EqualNI(Str1 const& str1, Str2 const& str2, size_t length)
		{
			using Char1 = traits<Str1>::value_type;
			using Char2 = traits<Str2>::value_type;
			using CTraits1 = char_traits<Char1>;
			using CTraits2 = char_traits<Char2>;
			return EqualN(str1, str2, length, [](Char1 lhs, Char2 rhs){ return CTraits1::lwr(lhs) == CTraits2::lwr(rhs); });
		}
		inline bool EqualNI(char const* str1, char const* str2, size_t length)
		{
			return ::_strnicmp(str1, str2, length) == 0;
		}
		inline bool EqualNI(wchar_t const* str1, wchar_t const* str2, size_t length)
		{
			return ::_wcsnicmp(str1, str2, length) == 0;
		}
		#pragma endregion

		#pragma region FindChar
		// Return a pointer to the first occurrance of 'ch' in a string or the string null terminator
		template <typename Char1, typename Char2> inline Char1* FindChar(Char1* str, Char2 ch)
		{
			for (;*str && *str != ch; ++str) {}
			return str;
		}
		template <typename Str, typename Char2, typename Char1 = Str::value_type> inline Char1* FindChar(Str& str, Char2 ch)
		{
			return FindChar(const_cast<Char1*>(str.c_str()), ch);
		}

		// Return a pointer to the first occurrange of 'ch' in a string or the string null terminator or the 'length' character
		template <typename Char1, typename Char2> inline Char1* FindChar(Char1* str, Char2 ch, size_t length)
		{
			for (;*str && length-- && *str != ch; ++str) {}
			return str;
		}
		template <typename Str, typename Char2, typename Char1 = Str::value_type> inline Char1* FindChar(Str const& str, Char2 ch, size_t length)
		{
			return FindChar(str.c_str(), ch, length);
		}
		#pragma endregion

		#pragma region FindStr
		// Find the sub string 'what' in the range of characters provided.
		// Returns an iterator to the sub string or to the end of the range.
		template <typename Char1, typename Char2, typename Pred> inline Char1* FindStr(Char1* str, Char2 const* what, Pred pred)
		{
			if (Empty(what)) return End(str);
			auto what_len = Length(what);
			auto s = str; for (; *s != 0 && !pred(s, what, what_len); ++s) {}
			return s;
		}
		template <typename Str, typename Char2, typename Pred, typename Char1 = Str::value_type> inline Char1* FindStr(Str& str, Char2 const* what, Pred pred)
		{
			return FindStr(str.c_str(), what, pred);
		}
		template <typename Char1, typename Char2> inline Char1* FindStr(Char1* str, Char2 const* what)
		{
			return FindStr(str, what, [](Char1 const* lhs, Char2 const* rhs, size_t length){ return EqualN(lhs,rhs,length); });
		}
		template <typename Str, typename Char2, typename Char1 = Str::value_type> inline Char1* FindStr(Str& str, Char2 const* what)
		{
			return FindStr(&*str.begin(), what);
		}
		#pragma endregion

		#pragma region Find
		// Returns a pointer to the first character in 'str[offset, offset+count)' that satisfies 'pred', or a pointer to the end of the string or &str[offset+count]
		template <typename Str, typename Pred, typename Char = traits<Str>::value_type> inline Char* FindFirst(Str& str, size_t offset, size_t count, Pred pred)
		{
			auto p = Begin(str) + offset;
			auto pend = End(str, offset + count);
			for (; count-- != 0 && p != pend && !pred(*p); ++p) {}
			return p;
		}
		template <typename Str, typename Pred, typename Char = traits<Str>::value_type> inline Char* FindFirst(Str& str, Pred pred)
		{
			return FindFirst(str, 0, ~0U, pred);
		}

		// Returns a pointer to the last character in 'str[offset, offset+count)' that satisfies 'pred', or a pointer to the beginning of the string or &str[offset]
		template <typename Str, typename Pred, typename Char = traits<Str>::value_type> inline Char* FindLast(Str& str, size_t offset, size_t count, Pred pred)
		{
			auto p = End(str, offset + count);
			auto pend = Begin(str) + offset;
			for (; count-- != 0 && p != pend && !pred(*--p);) {}
			return p;
		}
		template <typename Str, typename Pred, typename Char = traits<Str>::value_type> inline Char* FindLast(Str& str, Pred pred)
		{
			return FindLast(str, 0, ~0U, pred);
		}
		#pragma endregion

		#pragma region Resize
		// Resize a string. For pointers to fixed buffers, it's the callers responsibilty to ensure sufficient space
		template <typename Str, typename Char2 = char, typename Char1 = Str::value_type> inline void Resize(Str& str, size_t new_size, Char2 ch = 0)
		{
			str.resize(new_size, Char1(ch));
		}

		// Resize 'str', initialising with 'ch'
		template <typename Char1, typename Char2 = char> inline void Resize(Char1* str, size_t new_size, Char2 ch)
		{
			// Should really assert this: assert(End(str) - Begin(str) >= new_size);
			// but can't guarantee 'str' has been initialised
			auto c = Char1(ch);
			auto i = End(str, new_size); auto iend = str + new_size;
			for (; i < iend; ++i) *i = c;
			str[new_size] = 0;
		}

		// Resize 'str' without initialisation
		template <typename Char1, typename Char2 = char> inline void Resize(Char1* str, size_t new_size)
		{
			// Should really assert this: assert(End(str) - Begin(str) >= new_size);
			// but can't guarantee 'str' has been initialised
			str[new_size] = 0;
		}
		#pragma endregion

		#pragma region Assign
		// Assign a range of characters to a subrange within a string.
		// 'dest' is the string to be assigned to
		// 'offset' is the index position of where to start copying to
		// 'count' is the maximum number of characters to copy. The capacity of dest *must* be >= offset + count
		// 'first','last' the range of characters to assign to 'dest'
		// On return, dest will be resized to 'offset + (last - first)' or 'offset + count', whichever is less.
		template <typename Str, typename Char2> void Assign(Str& dest, size_t offset, size_t count, Char2 const* first, Char2 const* last)
		{
			// The number of characters to be copied to 'dest'
			auto size = static_cast<size_t>(last - first);

			// Clamp this by 'count'
			if (size > count)
				size = count;

			// Set 'dest' to be the correct size
			Resize(dest, offset + size);

			// Assign the characters
			using Char1 = traits<Str>::value_type;
			for (auto out = Begin(dest) + offset; size--; ++out, ++first)
				*out = Char1(*first);
		}
		template <typename Str, typename Char2> void Assign(Str& dest, Char2 const* first, Char2 const* last)
		{
			// The number of characters to be copied to 'dest'
			auto size = static_cast<size_t>(last - first);

			// Assume 'dest' can be resized to 'size'
			return Assign(dest, 0, size, first, last);
		}
		#pragma endregion

		#pragma region Split
		// Split a string at 'delims' outputting each sub string to 'out'
		// 'out' should have the signature out(tstr1 const& s, size_t i, size_t j)
		// where [i,j) is the range in 's' containing the substring
		template <typename Str, typename Char1, typename Out> inline void Split(Str const& str, Char1 const* delims, Out out)
		{
			size_t i = 0, j = 0, jend = Length(str);
			for (; j != jend; ++j)
			{
				if (*FindChar(delims, str[j]) == 0) continue;
				out(str, i, j);
				i = j + 1;
			}
			if (i != j)
				out(str, i, j);
		}
		#pragma endregion

		#pragma region Trim
		// Trim characters from a string
		// 'str' is the string to be trimmed
		// 'pred' should return true if the character should be trimmed
		// Returns 'str' for method chaining
		template <typename Str, typename Pred, typename Char = Str::value_type> inline Str& Trim(Str& str, Pred pred, bool front, bool back)
		{
			auto len   = int(Length(str));
			auto first = front ? FindFirst(str, 0    , len        , [&](Char ch){ return !pred(ch); }) - Begin(str) : 0;
			auto last  = back  ? FindLast (str, first, len - first, [&](Char ch){ return !pred(ch); }) - Begin(str) : len - 1;
			last += int(last != first);

			// Move the non-trimed characters to the front of the string and trim the tail
			auto count = last - first;
			for (Char* p = Begin(str), *s = p + first; count-- != 0; ++p, ++s) { *p = *s; }
			Resize(str, last - first);
			return str;
		}
		template <typename Str, typename Pred, typename Char = Str::value_type> inline Str Trim(Str const& str, Pred pred, bool front, bool back)
		{
			auto s = str;
			return Trim(s, pred, front, back);
		}
		template <typename Char, typename Pred> inline Char* Trim(Char* str, Pred pred, bool front, bool back)
		{
			return Trim<Char*,Pred,Char>(str, pred, front, back);
		}
		template <typename Char, typename Pred> inline std::basic_string<Char> Trim(Char const* str, Pred pred, bool front, bool back)
		{
			auto s = std::basic_string<Char>(str);
			return Trim(s, pred, front, back);
		}
		
		// Trim leading or trailing characters in 'chars' from 'str'.
		// Returns 'str' for method chaining
		template <typename Str, typename Char2, typename Char1 = Str::value_type> inline Str& TrimChars(Str& str, Char2 const* chars, bool front, bool back)
		{
			return Trim(str, [&](Char1 ch){ return *FindChar(chars, ch) != 0; }, front, back);
		}
		template <typename Str, typename Char2, typename Char1 = Str::value_type> inline Str TrimChars(Str const& str, Char2 const* chars, bool front, bool back)
		{
			auto s = str;
			return TrimChars(s, chars, front, back);
		}
		template <typename Char1, typename Char2> inline Char1* TrimChars(Char1* str, Char2 const* chars, bool front, bool back)
		{
			return TrimChars<Char1*, Char2, Char1>(str, chars, front, back);
		}
		template <typename Char1, typename Char2> inline std::basic_string<Char1> TrimChars(Char1 const* str, Char2 const* chars, bool front, bool back)
		{
			auto s = std::basic_string<Char1>(str);
			return TrimChars(s, chars, front, back);
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
		PRUnitTest(pr_str2_stringcore)
		{
			using namespace pr;
			using namespace pr::str2;

			{// Empty
				char const*     aptr   = "full";
				char            aarr[] = "";
				std::string     astr   = "";
				wchar_t const*  wptr   = L"";
				wchar_t         warr[] = L"full";
				std::wstring    wstr   = L"full";

				PR_CHECK(!Empty(aptr), true);
				PR_CHECK( Empty(aarr), true);
				PR_CHECK( Empty(astr), true);
				PR_CHECK( Empty(wptr), true);
				PR_CHECK(!Empty(warr), true);
				PR_CHECK(!Empty(wstr), true);
			}
			{// Length
				char const*     aptr   = "length7";
				char            aarr[] = "length7";
				std::string     astr   = "length7";
				wchar_t const*  wptr   = L"length7";
				wchar_t         warr[] = L"length7";
				std::wstring    wstr   = L"length7";

				PR_CHECK(Length(aptr), size_t(7));
				PR_CHECK(Length(aarr), size_t(7));
				PR_CHECK(Length(astr), size_t(7));
				PR_CHECK(Length(wptr), size_t(7));
				PR_CHECK(Length(warr), size_t(7));
				PR_CHECK(Length(wstr), size_t(7));
			}
			{// Range
				char const*     aptr   =  "range";
				char            aarr[] =  "range";
				std::string     astr   =  "range";
				wchar_t const*  wptr   = L"range";
				wchar_t         warr[] = L"range";
				std::wstring    wstr   = L"range";

				PR_CHECK(*Begin(aptr) ==  'r' && *End(aptr) == 0 && *(End(aptr)-1) ==  'e', true);
				PR_CHECK(*Begin(aarr) ==  'r' && *End(aarr) == 0 && *(End(aarr)-1) ==  'e', true);
				PR_CHECK(*Begin(astr) ==  'r' && *End(astr) == 0 && *(End(astr)-1) ==  'e', true);
				PR_CHECK(*Begin(wptr) == L'r' && *End(wptr) == 0 && *(End(wptr)-1) == L'e', true);
				PR_CHECK(*Begin(warr) == L'r' && *End(warr) == 0 && *(End(warr)-1) == L'e', true);
				PR_CHECK(*Begin(wstr) == L'r' && *End(wstr) == 0 && *(End(wstr)-1) == L'e', true);
			}
			{// Equal
				char const*     aptr   =  "equal";
				char            aarr[] =  "equal";
				std::string     astr   =  "equal";
				wchar_t const*  wptr   = L"equal";
				wchar_t         warr[] = L"equal";
				std::wstring    wstr   = L"equal";

				PR_CHECK(Equal(aptr, aptr) && Equal(aptr, aarr) && Equal(aptr, astr) && Equal(aptr, wptr) && Equal(aptr, warr) && Equal(aptr, wstr), true);
				PR_CHECK(Equal(aarr, aptr) && Equal(aarr, aarr) && Equal(aarr, astr) && Equal(aarr, wptr) && Equal(aarr, warr) && Equal(aarr, wstr), true);
				PR_CHECK(Equal(astr, aptr) && Equal(astr, aarr) && Equal(astr, astr) && Equal(astr, wptr) && Equal(astr, warr) && Equal(astr, wstr), true);
				PR_CHECK(Equal(wptr, aptr) && Equal(wptr, aarr) && Equal(wptr, astr) && Equal(wptr, wptr) && Equal(wptr, warr) && Equal(wptr, wstr), true);
				PR_CHECK(Equal(warr, aptr) && Equal(warr, aarr) && Equal(warr, astr) && Equal(warr, wptr) && Equal(warr, warr) && Equal(warr, wstr), true);
				PR_CHECK(Equal(wstr, aptr) && Equal(wstr, aarr) && Equal(wstr, astr) && Equal(wstr, wptr) && Equal(wstr, warr) && Equal(wstr, wstr), true);
				PR_CHECK(Equal(aptr, "equal!"), false);
				PR_CHECK(Equal(aarr, "equal!"), false);
				PR_CHECK(Equal(astr, "equal!"), false);
				PR_CHECK(Equal(wptr, "equal!"), false);
				PR_CHECK(Equal(warr, "equal!"), false);
				PR_CHECK(Equal(wstr, "equal!"), false);
			}
			{// EqualI
				char const*     aptr   =  "Equal";
				char            aarr[] =  "eQual";
				std::string     astr   =  "eqUal";
				wchar_t const*  wptr   = L"equAl";
				wchar_t         warr[] = L"equaL";
				std::wstring    wstr   = L"EQUAL";

				PR_CHECK(EqualI(aptr, aptr) && EqualI(aptr, aarr) && EqualI(aptr, astr) && EqualI(aptr, wptr) && EqualI(aptr, warr) && EqualI(aptr, wstr), true);
				PR_CHECK(EqualI(aarr, aptr) && EqualI(aarr, aarr) && EqualI(aarr, astr) && EqualI(aarr, wptr) && EqualI(aarr, warr) && EqualI(aarr, wstr), true);
				PR_CHECK(EqualI(astr, aptr) && EqualI(astr, aarr) && EqualI(astr, astr) && EqualI(astr, wptr) && EqualI(astr, warr) && EqualI(astr, wstr), true);
				PR_CHECK(EqualI(wptr, aptr) && EqualI(wptr, aarr) && EqualI(wptr, astr) && EqualI(wptr, wptr) && EqualI(wptr, warr) && EqualI(wptr, wstr), true);
				PR_CHECK(EqualI(warr, aptr) && EqualI(warr, aarr) && EqualI(warr, astr) && EqualI(warr, wptr) && EqualI(warr, warr) && EqualI(warr, wstr), true);
				PR_CHECK(EqualI(wstr, aptr) && EqualI(wstr, aarr) && EqualI(wstr, astr) && EqualI(wstr, wptr) && EqualI(wstr, warr) && EqualI(wstr, wstr), true);
				PR_CHECK(EqualI(aptr, "equal!"), false);
				PR_CHECK(EqualI(aarr, "equal!"), false);
				PR_CHECK(EqualI(astr, "equal!"), false);
				PR_CHECK(EqualI(wptr, "equal!"), false);
				PR_CHECK(EqualI(warr, "equal!"), false);
				PR_CHECK(EqualI(wstr, "equal!"), false);
			}
			{// EqualN
				char const*     aptr   =  "equal1";
				char            aarr[] =  "equal2";
				std::string     astr   =  "equal3";
				wchar_t const*  wptr   = L"equal4";
				wchar_t         warr[] = L"equal5";
				std::wstring    wstr   = L"equal6";

				PR_CHECK(EqualN(aptr, aptr, 5) && EqualN(aptr, aarr, 5) && EqualN(aptr, astr, 5) && EqualN(aptr, wptr, 5) && EqualN(aptr, warr, 5) && EqualN(aptr, wstr, 5), true);
				PR_CHECK(EqualN(aarr, aptr, 5) && EqualN(aarr, aarr, 5) && EqualN(aarr, astr, 5) && EqualN(aarr, wptr, 5) && EqualN(aarr, warr, 5) && EqualN(aarr, wstr, 5), true);
				PR_CHECK(EqualN(astr, aptr, 5) && EqualN(astr, aarr, 5) && EqualN(astr, astr, 5) && EqualN(astr, wptr, 5) && EqualN(astr, warr, 5) && EqualN(astr, wstr, 5), true);
				PR_CHECK(EqualN(wptr, aptr, 5) && EqualN(wptr, aarr, 5) && EqualN(wptr, astr, 5) && EqualN(wptr, wptr, 5) && EqualN(wptr, warr, 5) && EqualN(wptr, wstr, 5), true);
				PR_CHECK(EqualN(warr, aptr, 5) && EqualN(warr, aarr, 5) && EqualN(warr, astr, 5) && EqualN(warr, wptr, 5) && EqualN(warr, warr, 5) && EqualN(warr, wstr, 5), true);
				PR_CHECK(EqualN(wstr, aptr, 5) && EqualN(wstr, aarr, 5) && EqualN(wstr, astr, 5) && EqualN(wstr, wptr, 5) && EqualN(wstr, warr, 5) && EqualN(wstr, wstr, 5), true);
				PR_CHECK(EqualN(aptr, "equal!", 6), false);
				PR_CHECK(EqualN(aarr, "equal!", 6), false);
				PR_CHECK(EqualN(astr, "equal!", 6), false);
				PR_CHECK(EqualN(wptr, "equal!", 6), false);
				PR_CHECK(EqualN(warr, "equal!", 6), false);
				PR_CHECK(EqualN(wstr, "equal!", 6), false);
			}
			{// EqualNI
				char const*     aptr   =  "Equal1";
				char            aarr[] =  "eQual2";
				std::string     astr   =  "eqUal3";
				wchar_t const*  wptr   = L"equAl4";
				wchar_t         warr[] = L"equaL5";
				std::wstring    wstr   = L"EQUAL6";

				PR_CHECK(EqualNI(aptr, aptr, 5) && EqualNI(aptr, aarr, 5) && EqualNI(aptr, astr, 5) && EqualNI(aptr, wptr, 5) && EqualNI(aptr, warr, 5) && EqualNI(aptr, wstr, 5), true);
				PR_CHECK(EqualNI(aarr, aptr, 5) && EqualNI(aarr, aarr, 5) && EqualNI(aarr, astr, 5) && EqualNI(aarr, wptr, 5) && EqualNI(aarr, warr, 5) && EqualNI(aarr, wstr, 5), true);
				PR_CHECK(EqualNI(astr, aptr, 5) && EqualNI(astr, aarr, 5) && EqualNI(astr, astr, 5) && EqualNI(astr, wptr, 5) && EqualNI(astr, warr, 5) && EqualNI(astr, wstr, 5), true);
				PR_CHECK(EqualNI(wptr, aptr, 5) && EqualNI(wptr, aarr, 5) && EqualNI(wptr, astr, 5) && EqualNI(wptr, wptr, 5) && EqualNI(wptr, warr, 5) && EqualNI(wptr, wstr, 5), true);
				PR_CHECK(EqualNI(warr, aptr, 5) && EqualNI(warr, aarr, 5) && EqualNI(warr, astr, 5) && EqualNI(warr, wptr, 5) && EqualNI(warr, warr, 5) && EqualNI(warr, wstr, 5), true);
				PR_CHECK(EqualNI(wstr, aptr, 5) && EqualNI(wstr, aarr, 5) && EqualNI(wstr, astr, 5) && EqualNI(wstr, wptr, 5) && EqualNI(wstr, warr, 5) && EqualNI(wstr, wstr, 5), true);
				PR_CHECK(EqualNI(aptr, "equal!", 6), false);
				PR_CHECK(EqualNI(aarr, "equal!", 6), false);
				PR_CHECK(EqualNI(astr, "equal!", 6), false);
				PR_CHECK(EqualNI(wptr, "equal!", 6), false);
				PR_CHECK(EqualNI(warr, "equal!", 6), false);
				PR_CHECK(EqualNI(wstr, "equal!", 6), false);
			}
			{// FindChar
				char const*     aptr   =  "find char";
				char            aarr[] =  "find char";
				std::string     astr   =  "find char";
				wchar_t const*  wptr   = L"find char";
				wchar_t         warr[] = L"find char";
				std::wstring    wstr   = L"find char";

				PR_CHECK(*FindChar(aptr, 'i') ==  'i' && *FindChar(aptr,  'b') == 0, true);
				PR_CHECK(*FindChar(aarr,L'i') ==  'i' && *FindChar(aarr, L'b') == 0, true);
				PR_CHECK(*FindChar(astr, 'i') ==  'i' && *FindChar(astr,  'b') == 0, true);
				PR_CHECK(*FindChar(wptr, 'i') == L'i' && *FindChar(wptr, L'b') == 0, true);
				PR_CHECK(*FindChar(warr,L'i') == L'i' && *FindChar(warr,  'b') == 0, true);
				PR_CHECK(*FindChar(wstr, 'i') == L'i' && *FindChar(wstr, L'b') == 0, true);
			}
			{// FindStr
				char const*     aptr   =  "find str";
				char            aarr[] =  "find str";
				std::string     astr   =  "find str";
				wchar_t const*  wptr   = L"find str";
				wchar_t         warr[] = L"find str";
				std::wstring    wstr   = L"find str";

				PR_CHECK(*FindStr(aptr, "str") ==  's' && *FindStr(aptr,  "bob") == 0, true);
				PR_CHECK(*FindStr(aarr,L"str") ==  's' && *FindStr(aarr, L"bob") == 0, true);
				PR_CHECK(*FindStr(astr, "str") ==  's' && *FindStr(astr,  "bob") == 0, true);
				PR_CHECK(*FindStr(wptr, "str") == L's' && *FindStr(wptr, L"bob") == 0, true);
				PR_CHECK(*FindStr(warr,L"str") == L's' && *FindStr(warr,  "bob") == 0, true);
				PR_CHECK(*FindStr(wstr, "str") == L's' && *FindStr(wstr, L"bob") == 0, true);
			}
			{// FindFirst
				char const*     aptr   =  "find first";
				char            aarr[] =  "find first";
				std::string     astr   =  "find first";
				wchar_t const*  wptr   = L"find first";
				wchar_t         warr[] = L"find first";
				std::wstring    wstr   = L"find first";

				PR_CHECK(Equal(FindFirst(aptr, [](char ch){ return ch == 'i'; }), "ind first"), true);
				PR_CHECK(Equal(FindFirst(aarr, [](char ch){ return ch == 'i'; }), "ind first"), true);
				PR_CHECK(Equal(FindFirst(astr, [](char ch){ return ch == 'i'; }), "ind first"), true);
				PR_CHECK(Equal(FindFirst(wptr, [](wchar_t ch){ return ch == L'i'; }), "ind first"), true);
				PR_CHECK(Equal(FindFirst(warr, [](wchar_t ch){ return ch == L'i'; }), "ind first"), true);
				PR_CHECK(Equal(FindFirst(wstr, [](wchar_t ch){ return ch == L'i'; }), "ind first"), true);

				PR_CHECK(Equal(FindFirst(aptr, [](char ch){ return ch == 'x'; }), ""), true);
				PR_CHECK(Equal(FindFirst(aarr, [](char ch){ return ch == 'x'; }), ""), true);
				PR_CHECK(Equal(FindFirst(astr, [](char ch){ return ch == 'x'; }), ""), true);
				PR_CHECK(Equal(FindFirst(wptr, [](wchar_t ch){ return ch == L'x'; }), ""), true);
				PR_CHECK(Equal(FindFirst(warr, [](wchar_t ch){ return ch == L'x'; }), ""), true);
				PR_CHECK(Equal(FindFirst(wstr, [](wchar_t ch){ return ch == L'x'; }), ""), true);
			}
			{//FindLast
				char const*     aptr   =  "find flast";
				char            aarr[] =  "find flast";
				std::string     astr   =  "find flast";
				wchar_t const*  wptr   = L"find flast";
				wchar_t         warr[] = L"find flast";
				std::wstring    wstr   = L"find flast";

				PR_CHECK(Equal(FindLast(aptr, [](char ch){ return ch == 'f'; }), "flast"), true);
				PR_CHECK(Equal(FindLast(aarr, [](char ch){ return ch == 'f'; }), "flast"), true);
				PR_CHECK(Equal(FindLast(astr, [](char ch){ return ch == 'f'; }), "flast"), true);
				PR_CHECK(Equal(FindLast(wptr, [](wchar_t ch){ return ch == L'f'; }), "flast"), true);
				PR_CHECK(Equal(FindLast(warr, [](wchar_t ch){ return ch == L'f'; }), "flast"), true);
				PR_CHECK(Equal(FindLast(wstr, [](wchar_t ch){ return ch == L'f'; }), "flast"), true);

				PR_CHECK(Equal(FindLast(aptr, [](char ch){ return ch == 'x'; }), "find flast"), true);
				PR_CHECK(Equal(FindLast(aarr, [](char ch){ return ch == 'x'; }), "find flast"), true);
				PR_CHECK(Equal(FindLast(astr, [](char ch){ return ch == 'x'; }), "find flast"), true);
				PR_CHECK(Equal(FindLast(wptr, [](wchar_t ch){ return ch == L'x'; }), "find flast"), true);
				PR_CHECK(Equal(FindLast(warr, [](wchar_t ch){ return ch == L'x'; }), "find flast"), true);
				PR_CHECK(Equal(FindLast(wstr, [](wchar_t ch){ return ch == L'x'; }), "find flast"), true);
			}
			{// Resize
				char            aarr[] = {'a','a','a','a'};
				wchar_t         warr[] = {L'a',L'a',L'a',L'a'};
				std::string     astr   = "aaaa";
				std::wstring    wstr   = L"aaaa";

				Resize(aarr, 2); PR_CHECK(Equal(aarr, "aa"), true);
				Resize(warr, 2); PR_CHECK(Equal(warr, "aa"), true);
				Resize(astr, 2); PR_CHECK(Equal(astr, "aa"), true);
				Resize(wstr, 2); PR_CHECK(Equal(wstr, "aa"), true);

				Resize(aarr, 3, 'b'); PR_CHECK(Equal(aarr, "aab"), true);
				Resize(warr, 3, 'b'); PR_CHECK(Equal(warr, "aab"), true);
				Resize(astr, 3, 'b'); PR_CHECK(Equal(astr, "aab"), true);
				Resize(wstr, 3, 'b'); PR_CHECK(Equal(wstr, "aab"), true);
			}
			{// Assign
				char const*     asrc = "string";
				wchar_t const*  wsrc = L"string";

				char            aarr[5];
				wchar_t         warr[5];
				std::string     astr;
				std::wstring    wstr;

				Assign(aarr, asrc, asrc+3); PR_CHECK(Equal(aarr, "str"), true);
				Assign(aarr, wsrc, wsrc+3); PR_CHECK(Equal(aarr, "str"), true);
				Assign(warr, asrc, asrc+3); PR_CHECK(Equal(warr, "str"), true);
				Assign(warr, wsrc, wsrc+3); PR_CHECK(Equal(warr, "str"), true);
				Assign(astr, asrc, asrc+3); PR_CHECK(Equal(astr, "str"), true);
				Assign(astr, wsrc, wsrc+3); PR_CHECK(Equal(astr, "str"), true);
				Assign(wstr, asrc, asrc+3); PR_CHECK(Equal(wstr, "str"), true);
				Assign(wstr, wsrc, wsrc+3); PR_CHECK(Equal(wstr, "str"), true);

				Assign(aarr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(aarr, "stst"), true);
				Assign(aarr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(aarr, "stst"), true);
				Assign(warr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(warr, "stst"), true);
				Assign(warr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(warr, "stst"), true);
				Assign(astr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(astr, "stst"), true);
				Assign(astr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(astr, "stst"), true);
				Assign(wstr, 2, 2, asrc, asrc+3); PR_CHECK(Equal(wstr, "stst"), true);
				Assign(wstr, 2, 2, wsrc, wsrc+3); PR_CHECK(Equal(wstr, "stst"), true);

				Assign(astr, 2, ~0U, asrc, asrc+5); PR_CHECK(Equal(astr, "ststrin"), true);
				Assign(astr, 2, ~0U, wsrc, wsrc+5); PR_CHECK(Equal(astr, "ststrin"), true);
				Assign(wstr, 2, ~0U, asrc, asrc+5); PR_CHECK(Equal(wstr, "ststrin"), true);
				Assign(wstr, 2, ~0U, wsrc, wsrc+5); PR_CHECK(Equal(wstr, "ststrin"), true);
			}
			{// Split
				char    astr[] = "1,,2,3,4";
				wchar_t wstr[] = L"1,,2,3,4";
				char    res[][2] = {"1","","2","3","4"};
				int i;

				std::vector<std::string> abuf;
				Split(astr, L",", [&](char const* s, size_t i, size_t iend)
					{
						abuf.push_back(std::string(s+i, s+iend));
					});
				i = 0; for (auto& s : abuf)
					PR_CHECK(Equal(s, res[i++]), true);

				std::vector<std::wstring> wbuf;
				Split(wstr, ",", [&](wchar_t const* s, size_t i, size_t iend)
					{
						wbuf.push_back(std::wstring(s+i, s+iend));
					});
				i = 0; for (auto& s : wbuf)
					PR_CHECK(Equal(s, res[i++]), true);
			}
			{// Trim
				char            aarr[] =  " \t,trim\n";
				std::string     astr   =  " \t,trim\n";
				wchar_t         warr[] = L" \t,trim\n";
				std::wstring    wstr   = L" \t,trim\n";
				auto aws = IsWhiteSpace<char>;
				auto wws = IsWhiteSpace<wchar_t>;

				PR_CHECK(Equal(Trim(aarr, aws, true, true), ",trim"), true);
				PR_CHECK(Equal(Trim(astr, aws, true, true), ",trim"), true);
				PR_CHECK(Equal(Trim(warr, wws, true, true), ",trim"), true);
				PR_CHECK(Equal(Trim(wstr, wws, true, true), ",trim"), true);

				PR_CHECK(Equal(Trim( " \t,trim\n", aws, true, false), ",trim\n") , true);
				PR_CHECK(Equal(Trim( " \t,trim\n", aws, true, false), ",trim\n") , true);
				PR_CHECK(Equal(Trim(L" \t,trim\n", wws, true, false), ",trim\n") , true);
				PR_CHECK(Equal(Trim(L" \t,trim\n", wws, true, false), ",trim\n") , true);

				PR_CHECK(Equal(Trim( " \t,trim\n", aws, false, true), " \t,trim"), true);
				PR_CHECK(Equal(Trim( " \t,trim\n", aws, false, true), " \t,trim"), true);
				PR_CHECK(Equal(Trim(L" \t,trim\n", wws, false, true), " \t,trim"), true);
				PR_CHECK(Equal(Trim(L" \t,trim\n", wws, false, true), " \t,trim"), true);

				PR_CHECK(Equal(TrimChars( " \t,trim\n",  " \t,\n" ,true  ,true) ,  "trim"    ), true);
				PR_CHECK(Equal(TrimChars( " \t,trim\n", L" \t,\n" ,true  ,true) , L"trim"    ), true);
				PR_CHECK(Equal(TrimChars(L" \t,trim\n",  " \t,\n" ,true  ,false),  "trim\n"  ), true);
				PR_CHECK(Equal(TrimChars(L" \t,trim\n", L" \t,\n" ,false ,true) , L" \t,trim"), true);
			}
		}
	}
}
#endif


