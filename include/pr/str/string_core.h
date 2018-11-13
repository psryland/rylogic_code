//**********************************
// String Core
//  Copyright (c) Rylogic Ltd 2015
//**********************************
// Fundamental string functions that operate on:
//'   std::string, std::wstring, pr::string<>, etc
//'   char[], wchar_t[], etc
//'   char*, wchar_t*, etc
// Note: char-array strings are not handled as special cases because there is no
// guarantee that the entire buffer is filled by the string, the null terminator
// may be midway through the buffer

#pragma once

#include <string>
#include <vector>
#include <functional>
#include <type_traits>
#include <locale>
#include <cstring>
#include <cstdarg>
#include <cstdlib>
#include <cassert>

// Use this define to declare a string literal in a function templated on 'tchar'
#ifndef PR_STRLITERAL
#define PR_STRLITERAL(tchar,s)  pr::str::char_traits<tchar>::str(s, L##s)
#endif

namespace pr
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

	namespace str
	{
		#pragma region Char Traits
		// Is 'T' a char or wchar_t
		template <typename T> struct is_char :std::false_type {};
		template <> struct is_char<char> :std::true_type {};
		template <> struct is_char<wchar_t> :std::true_type {};
		template <typename T> using enable_if_char_t = typename std::enable_if<is_char<T>::value>::type;

		// char traits - Did you know about std::char_traits<>... 
		template <typename TChar> struct char_traits;
		
		// char traits
		template <> struct char_traits<char>
		{
			static char const* str(char const* str, wchar_t const*) { return str; }

			static char lwr(char ch) { return static_cast<char>(::tolower(ch)); }
			static char upr(char ch) { return static_cast<char>(::toupper(ch)); }

			static size_t strlen(char const* str)                               { return ::strlen(str); }
			static size_t strnlen(char const* str, size_t max_count)            { return ::strnlen(str, max_count); }

			static int strcmp(char const* lhs, char const* rhs)                     { return ::strcmp(lhs, rhs); }
			static int strncmp(char const* lhs, char const* rhs, size_t max_count)  { return ::strncmp(lhs, rhs, max_count); }
			static int strnicmp(char const* lhs, char const* rhs, size_t max_count) { return ::_strnicmp(lhs, rhs, max_count); }

			static double             strtod(char const* str, char** end)                   { return ::strtod(str, end); }
			static long               strtol(char const* str, char** end, int radix)        { return ::strtol(str, end, radix); }
			static unsigned long      strtoul(char const* str, char** end, int radix)       { return ::strtoul(str, end, radix); }
			static long long          strtoi64(char const* str, char** end, int radix)      { return ::_strtoi64(str, end, radix); }
			static unsigned long long strtoui64(char const* str, char** end, int radix)     { return ::_strtoui64(str, end, radix); }

			static bool eq(char lhs, char rhs)                                       { return lhs == rhs; }
			static void fill(char* ptr, size_t count, char ch)                       { ::memset(ptr, ch, count); }
			static void copy(char* dst, char const* src, size_t count)               { ::memcpy(dst, src, count); }
			static void move(char* dst, char const* src, size_t count)               { ::memmove(dst, src, count); }
			static int compare(char const* first1, char const* first2, size_t count) { return ::strncmp(first1, first2, count); }
			static char const* find(char const* first, size_t count, char ch)
			{
				for (; 0 < count; --count, ++first)
					if (eq(*first, ch)) return first;
				return nullptr;
			}
		};
		template <> struct char_traits<char&      > :char_traits<char> {};
		template <> struct char_traits<char const > :char_traits<char> {};
		template <> struct char_traits<char const&> :char_traits<char const> {};

		// wchar_t traits
		template <> struct char_traits<wchar_t>
		{
			static wchar_t const* str(char const*, wchar_t const* str) { return str; }

			static wchar_t lwr(wchar_t ch) { return static_cast<wchar_t>(towlower(ch)); }
			static wchar_t upr(wchar_t ch) { return static_cast<wchar_t>(towupper(ch)); }

			static size_t strlen(wchar_t const* str)                    { return ::wcslen(str); }
			static size_t strnlen(wchar_t const* str, size_t max_count) { return ::wcsnlen(str, max_count); }

			static int strcmp(wchar_t const* lhs, wchar_t const* rhs)                     { return ::wcscmp(lhs, rhs); }
			static int strncmp(wchar_t const* lhs, wchar_t const* rhs, size_t max_count)  { return ::wcsncmp(lhs, rhs, max_count); }
			static int strnicmp(wchar_t const* lhs, wchar_t const* rhs, size_t max_count) { return ::_wcsnicmp(lhs, rhs, max_count); }

			static double             strtod(wchar_t const* str, wchar_t** end)                   { return ::wcstod(str, end); }
			static long               strtol(wchar_t const* str, wchar_t** end, int radix)        { return ::wcstol(str, end, radix); }
			static long long          strtoi64(wchar_t const* str, wchar_t** end, int radix)      { return ::_wcstoi64(str, end, radix); }
			static unsigned long      strtoul(wchar_t const* str, wchar_t** end, int radix)       { return ::wcstoul(str, end, radix); }
			static unsigned long long strtoui64(wchar_t const* str, wchar_t** end, int radix)     { return ::_wcstoui64(str, end, radix); }
			
			static bool eq(wchar_t lhs, wchar_t rhs)                                       { return lhs == rhs; }
			static void fill(wchar_t* ptr, size_t count, wchar_t ch)                       { for (;count--;) *ptr++ = ch; }
			static void copy(wchar_t* dst, wchar_t const* src, size_t count)               { ::memcpy(dst, src, count * sizeof(wchar_t)); }
			static void move(wchar_t* dst, wchar_t const* src, size_t count)               { ::memmove(dst, src, count * sizeof(wchar_t)); }
			static int compare(wchar_t const* first1, wchar_t const* first2, size_t count) { return ::wcsncmp(first1, first2, count); }
			static wchar_t const* find(wchar_t const* first, size_t count, wchar_t ch)
			{
				for (; 0 < count; --count, ++first)
					if (eq(*first, ch)) return first;
				return nullptr;
			}
		};
		template <> struct char_traits<wchar_t&      > :char_traits<wchar_t> {};
		template <> struct char_traits<wchar_t const > :char_traits<wchar_t> {};
		template <> struct char_traits<wchar_t const&> :char_traits<wchar_t const> {};

		#pragma endregion

		#pragma region String Traits
		template <typename Str> struct traits :char_traits<typename Str::value_type>
		{
			using citer      = typename Str::const_iterator;
			using miter      = typename Str::iterator;
			using iter       = typename Str::iterator;
			using value_type = typename Str::value_type;

			static value_type const* c_str(Str const& str) { return str.c_str(); }
			static bool empty(Str const& str)              { return str.empty(); }
			static size_t size(Str const& str)             { return str.size(); }
		};
		template <typename Str> struct traits<Str const> :char_traits<typename Str::value_type>
		{
			using citer      = typename Str::const_iterator;
			using miter      = typename Str::iterator;
			using iter       = typename Str::const_iterator;
			using value_type = typename Str::value_type;

			static value_type const* c_str(Str const& str) { return str.c_str(); }
			static bool empty(Str const& str)              { return str.empty(); }
			static size_t size(Str const& str)             { return str.size(); }
		};
		template <typename Char> struct traits<Char const*> :char_traits<Char>
		{
			using citer      = Char const*;
			using miter      = Char*;
			using iter       = Char const*;
			using value_type = Char;

			static value_type const* c_str(Char const* str) { return str; }
			static bool empty(Char const* str)              { return *str == 0; }
			static size_t size(Char const* str)             { return strlen(str); }
		};
		template <typename Char> struct traits<Char*> :char_traits<Char>
		{
			using citer      = Char const*;
			using miter      = Char*;
			using iter       = Char*;
			using value_type = Char;

			static value_type const* c_str(Char const* str) { return str; }
			static bool empty(Char const* str)              { return *str == 0; }
			static size_t size(Char const* str)             { return strlen(str); }
		};
		template <typename Char> struct traits<Char const* const> :traits<Char const*>
		{};
		template <typename Char> struct traits<Char* const> :traits<Char*>
		{};
		template <typename Char, size_t Len> struct traits<Char const[Len]> :traits<Char const*>
		{};
		template <typename Char, size_t Len> struct traits<Char[Len]> :traits<Char*>
		{};

		//template <typename tchar> using valid_char_t = typename std::enable_if<std::is_same<Type, typename std::remove_reference<tchar>::type>::value>::type;
		//template <typename tarr> using valid_arr_t = valid_char_t<decltype(std::declval<tarr>()[0])>;
		//template <typename tptr> using valid_ptr_t = valid_char_t<decltype(std::declval<tptr>()[0])>;
		//template <typename tstr, typename = decltype(std::declval<tstr>().size())> using valid_str_t = valid_arr_t<tstr>;

		// Get the character type from a pointer or iterator
		template <typename Iter, typename Char = std::remove_reference_t<decltype(*std::declval<Iter>())>>
		using char_type_t = Char;

		// Checks that 'tstr' is an std::string-like type
		template <typename tstr, typename = decltype(std::declval<tstr>().size())> using valid_str_t = void;
		#pragma endregion

		#pragma region Standard Library Functions
		// Variable arg 'sprintf', overloaded for all char type combinations
		inline int vsprintf(char* buf, size_t buf_size_in_bytes, char const* format, va_list args)
		{
			return vsprintf_s(buf, buf_size_in_bytes, format, args);
		}
		inline int vsprintf(char* buf, size_t buf_size_in_bytes, wchar_t const* format, va_list args)
		{
			return vsprintf_s(buf, buf_size_in_bytes, Narrow(format).c_str(), args);
		}
		inline int vsprintf(wchar_t* buf, size_t buf_size_in_words, wchar_t const* format, va_list args)
		{
			return vswprintf_s(buf, buf_size_in_words, format, args);
		}
		inline int vsprintf(wchar_t* buf, size_t buf_size_in_words, char const* format, va_list args)
		{
			return vswprintf_s(buf, buf_size_in_words, Widen(format).c_str(), args);
		}

		// 'sprintf' accepting any char type
		template <typename Char1, typename Char2> inline int sprintf(Char1* buf, size_t buf_size_in_words, Char2 const* format, ...)
		{
			va_list arg_list;
			va_start(arg_list, format);
			auto r = vsprintf(buf, buf_size_in_words, format, arg_list);
			va_end(arg_list);
			return r;
		}
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
		template <typename Char> inline Char const* Delim(Char const* delim = nullptr)
		{
			static Char const default_delim[] = {Char(' '), Char('\t'), Char('\n'), Char('\r'), 0};
			return delim ? delim : default_delim;
		}
		#pragma endregion

		#pragma region c_str
		// Return a char const* for any string type
		template <typename Str, typename Char = Str::value_type> inline Char const* c_str(Str const& str)
		{
			return str.c_str();
		}
		template <typename Char> inline Char const* c_str(Char const* str)
		{
			return str;
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
		// Return the length of the string, excluding the null terminator (same as 'strlen')
		template <typename Str, typename = Str::value_type> inline size_t Length(Str const& str)
		{
			return str.size();
		}
		template <typename Str, typename = std::enable_if_t<std::is_pointer<Str>::value>> inline size_t Length(Str str)
		{
			using Char = traits<Str>::value_type;
			return char_traits<Char>::strlen(str);
		}
		template <typename Char, size_t N> inline size_t Length(Char (&str)[N])
		{
			return char_traits<Char>::strnlen(str, N);
		}
		#pragma endregion

		#pragma region Range
		// Return a pointer to the start of the string
		template <typename Str, typename Iter = traits<Str>::iter, typename = Str::value_type> inline Iter Begin(Str& str)
		{
			return str.begin();
		}
		template <typename Str, typename Iter = traits<Str>::citer, typename = Str::value_type> inline Iter BeginC(Str const& str)
		{
			return Begin<Str const>(str);
		}
		template <typename Char> inline Char* Begin(Char* str)
		{
			return str;
		}
		template <typename Char> inline Char const* BeginC(Char const* str)
		{
			return Begin(str);
		}

		// Return a pointer to the end of the string
		template <typename Str, typename Iter = traits<Str>::iter, typename = Str::value_type> inline Iter End(Str& str)
		{
			return Begin(str) + Length(str);
		}
		template <typename Str, typename Iter = traits<Str>::citer, typename = Str::value_type> inline Iter EndC(Str const& str)
		{
			return End<Str const>(str);
		}
		template <typename Char> inline Char* End(Char* str)
		{
			return Begin(str) + Length(str);
		}
		template <typename Char> inline Char const* EndC(Char const* str)
		{
			return End(str);
		}

		// Return a pointer to the 'N'th character or the end of the string, whichever is less
		template <typename Str, typename Iter = traits<Str>::iter, typename = Str::value_type> inline Iter End(Str& str, size_t N)
		{
			return Begin(str) + std::min(N, Length(str));
		}
		template <typename Str, typename Iter = traits<Str>::citer, typename = Str::value_type> inline Iter EndC(Str const& str, size_t N)
		{
			return End<Str const>(str, N);
		}
		template <typename Char> inline Char* End(Char* str, size_t N)
		{
			return Begin(str) + std::min(N, Length(str));
		}
		template <typename Char> inline Char const* EndC(Char const* str, size_t N)
		{
			return End(str, N);
		}
		#pragma endregion

		#pragma region Equal
		// Return true if the ranges '[i,iend)' and '[j,jend)' are equal
		template <typename Iter1, typename Iter2, typename Pred> inline bool Equal(Iter1 i, Iter1 iend, Iter2 j, Iter2 jend, Pred pred)
		{
			for (; !(i == iend) && !(j == jend) && pred(*i, *j); ++i, ++j) {}
			return i == iend && j == jend;
		}
		template <typename Iter1, typename Iter2> inline bool Equal(Iter1 i, Iter1 iend, Iter2 j, Iter2 jend)
		{
			using Char1 = decltype(*i);
			using Char2 = decltype(*j);
			return Equal(i, iend, j, jend, [](Char1 l, Char2 r){ return l == r; });
		}

		// Return true if str1 and str2 are equal
		template <typename Str1, typename Str2, typename Pred> inline bool Equal(Str1 const& str1, Str2 const& str2, Pred pred)
		{
			return Equal(BeginC(str1), EndC(str1), BeginC(str2), EndC(str2), pred);
		}
		template <typename Str1, typename Str2> inline bool Equal(Str1 const& str1, Str2 const& str2)
		{
			return Equal(BeginC(str1), EndC(str1), BeginC(str2), EndC(str2));
		}

		// Return true if str1 and str2 are equal
		template <typename Char1, typename Char2, typename Pred> inline bool Equal(Char1 const* str1, Char2 const* str2, Pred pred)
		{
			auto i = Begin(str1);
			auto j = Begin(str2);
			for (; *i != 0 && *j != 0 && pred(*i, *j); ++i, ++j) {}
			return *i == 0 && *j == 0;
		}
		template <typename Char1, typename Char2> inline bool Equal(Char1 const* str1, Char2 const* str2)
		{
			return Equal(str1, str2, [](Char1 l, Char2 r){ return l == r; });
		}

		// Specialisations for char/wchar_t
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
		// Return true if the ranges '[i,iend)' and '[j,jend)' are equal, ignoring case
		template <typename Iter1, typename Iter2, typename Char1 = decltype(*Iter1()), typename Char2 = decltype(*Iter2())> inline bool EqualI(Iter1 i, Iter1 iend, Iter2 j, Iter2 jend)
		{
			return Equal(i, iend, j, jend, [](Char1 lhs, Char2 rhs)
				{
					auto l = char_traits<Char1>::lwr(lhs);
					auto r = char_traits<Char2>::lwr(rhs);
					return l == r;
				});
		}

		// Return true if lhs and rhs are equal, ignoring case
		template <typename Str1, typename Str2> inline bool EqualI(Str1 const& str1, Str2 const& str2)
		{
			return EqualI(BeginC(str1), EndC(str1), BeginC(str2), EndC(str2));
		}

		// Specialisations for char/wchar_t
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
		template <typename Str1, typename Str2, typename Pred> inline bool EqualN(Str1 const& str1, Str2 const& str2, size_t length, Pred pred)
		{
			return Equal(BeginC(str1), EndC(str1, length), BeginC(str2), EndC(str2, length), pred);
		}
		template <typename Str1, typename Str2> inline bool EqualN(Str1 const& str1, Str2 const& str2, size_t length)
		{
			return Equal(BeginC(str1), EndC(str1, length), BeginC(str2), EndC(str2, length));
		}
		template <typename Char1, typename Char2, typename Pred> inline bool EqualN(Char1 const* str1, Char2 const* str2, size_t length, Pred pred)
		{
			auto i = Begin(str1);
			auto j = Begin(str2);
			for (; length-- != 0 && *i != 0 && *j != 0 && pred(*i, *j); ++i, ++j) {}
			return length == size_t(-1) || (*i == 0 && *j == 0);
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
		template <typename Str1, typename Str2, typename Char1 = traits<Str1>::value_type, typename Char2 = traits<Str2>::value_type> inline bool EqualNI(Str1 const& str1, Str2 const& str2, size_t length)
		{
			return EqualN(str1, str2, length, [](Char1 lhs, Char2 rhs)
				{
					auto l = char_traits<Char1>::lwr(lhs);
					auto r = char_traits<Char2>::lwr(rhs);
					return l == r; 
				});
		}
		inline bool EqualNI(char const* str1, char const* str2, size_t length)
		{
			return ::_strnicmp(str1, str2, length) == 0;
		}
		inline bool EqualNI(wchar_t const* str1, wchar_t const* str2, size_t length)
		{
			return ::_wcsnicmp(str1, str2, length) == 0;
		}
		template <typename Str, typename Char, int N> inline bool EqualNI(Str const& str, Char const (&str2)[N])
		{
			return EqualNI(str, str2, N);
		}
		#pragma endregion

		#pragma region FindChar
		// Return a pointer to the first occurrence of 'ch' in a string or the string null terminator
		template <typename Char1, typename Char2> inline Char1* FindChar(Char1* str, Char2 ch)
		{
			for (;*str && *str != ch; ++str) {}
			return str;
		}
		template <typename Str, typename Char2, typename Char1 = Str::value_type> inline Char1* FindChar(Str& str, Char2 ch)
		{
			return FindChar(const_cast<Char1*>(str.c_str()), ch);
		}

		// Return a pointer to the first occurrence of 'ch' in a string or the string null terminator or the 'length' character
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
		// Find the sub string 'what' in the given range of characters.
		// Returns an iterator to the sub string or to the end of the range.
		template <typename Iter, typename Str, typename Pred> inline Iter FindStr(Iter first, Iter last, Str const& what, Pred pred)
		{
			if (Empty(what)) return last;
			auto what_len = int(Length(what));
			auto what_beg = BeginC(what);
			for (; last - first >= what_len; ++first)
				if (pred(first, first + what_len, what_beg, what_beg + what_len))
					return first;

			return last;
		}
		template <typename Iter, typename Str> inline Iter FindStr(Iter first, Iter last, Str const& what)
		{
			using Iter2 = traits<Str>::citer;
			return FindStr(first, last, what, [](Iter i, Iter iend, Iter2 j, Iter2 jend){ return Equal(i, iend, j, jend); });
		}
		template <typename Str1, typename Str2, typename Iter = traits<Str1>::iter> inline Iter FindStr(Str1& str, Str2 const& what)
		{
			return FindStr(Begin(str), End(str), what);
		}

		// Find the sub string 'what' in the range of characters provided (no case).
		// Returns an iterator to the sub string or to the end of the range.
		template <typename Iter, typename Str> inline Iter FindStrI(Iter first, Iter last, Str const& what)
		{
			using Iter2 = traits<Str>::citer;
			return FindStr(first, last, what, [](Iter i, Iter iend, Iter2 j, Iter2 jend){ return EqualI(i, iend, j, jend); });
		}
		template <typename Str1, typename Str2, typename Iter = traits<Str1>::iter> inline Iter FindStrI(Str1& str, Str2 const& what)
		{
			return FindStr(Begin(str), End(str), what);
		}
		#pragma endregion

		#pragma region Find

		// Return an iterator to the first position that satisfies 'pred'
		template <typename Iter, typename Pred> inline Iter FindFirst(Iter beg, Iter end, Pred pred)
		{
			for (;beg != end && !pred(*beg); ++beg) {}
			return beg;
		}

		// Returns a pointer to the first character in '[offset, offset+count)' that satisfies 'pred', or a pointer to the end of the string or &str[offset+count]
		template <typename Str, typename Pred, typename Iter = traits<Str>::iter> inline Iter FindFirst(Str& str, size_t offset, size_t count, Pred pred)
		{
			return FindFirst(Begin(str) + offset, End(str, offset + count), pred);
		}

		// Returns a pointer to the first character in 'str' that satisfies 'pred', or a pointer to the end of the string
		template <typename Str, typename Pred, typename Iter = traits<Str>::iter> inline Iter FindFirst(Str& str, Pred pred)
		{
			return FindFirst(str, 0, ~size_t(), pred);
		}

		// Return an iterator to *one past* the last position that satisfies 'pred' or a pointer to the beginning of the string. Intended to be used to form a range with FindFirst/FindLast.
		template <typename Iter, typename Pred> inline Iter FindLast(Iter beg, Iter end, Pred pred)
		{
			for (; end != beg && !pred(*(end-1)); --end) {}
			return end;
		}

		// Returns a pointer to *one past* the last character in '[offset, offset+count)' that satisfies 'pred', or a pointer to the beginning of the string or &str[offset]
		template <typename Str, typename Pred, typename Iter = traits<Str>::iter> inline Iter FindLast(Str& str, size_t offset, size_t count, Pred pred)
		{
			return FindLast(Begin(str) + offset, End(str, offset + count), pred);
		}

		// Returns a pointer to *one past* the last character in 'str' that satisfies 'pred', or a pointer to the beginning of the string
		template <typename Str, typename Pred, typename Iter = traits<Str>::iter> inline Iter FindLast(Str& str, Pred pred)
		{
			return FindLast(str, 0, ~size_t(), pred);
		}

		// Find the first occurrence of one of the chars in 'delim'
		template <typename Iter, typename Char> inline Iter FindFirstOf(Iter beg, Iter end, Char const* delim)
		{
			for (; beg != end && *FindChar(delim, *beg) == 0; ++beg) {}
			return beg;
		}
		template <typename Str, typename Char, typename Iter = traits<Str>::iter> inline Iter FindFirstOf(Str& str, Char const* delim)
		{
			return FindFirstOf(Begin(str), End(str), delim);
		}
		template <typename Iter, typename Char> inline size_t FindFirstOfAdv(Iter& str, Char const* delim)
		{
			auto count = size_t();
			for (; *str && *FindChar(delim, *str) == 0; ++str, ++count) {}
			return count;
		}

		// Return a pointer to *one past* the last occurrence of one of the chars in 'delim'. Intended to be use with FindFirstOf to for a range
		template <typename Iter, typename Char> inline Iter FindLastOf(Iter beg, Iter end, Char const* delim)
		{
			for (;end != beg && *FindChar(delim, *(end-1)) == 0; --end) {}
			return end;
		}
		template <typename Str, typename Char, typename Iter = traits<Str>::iter> inline Iter FindLastOf(Str& str, Char const* delim)
		{
			return FindLastOf(Begin(str), End(str), delim);
		}

		// Find the first character not in the set 'delim'
		template <typename Iter, typename Char> inline Iter FindFirstNotOf(Iter beg, Iter end, Char const* delim)
		{
			for (; beg != end && *FindChar(delim, *beg) != 0; ++beg) {}
			return beg;
		}
		template <typename Str, typename Char, typename Iter = traits<Str>::iter> inline Iter FindFirstNotOf(Str& str, Char const* delim)
		{
			return FindFirstNotOf(Begin(str), End(str), delim);
		}
		template <typename Iter, typename Char> inline size_t FindFirstNotOfAdv(Iter& str, Char const* delim)
		{
			auto count = size_t();
			for (; *str && *FindChar(delim, *str) != 0; ++str, ++count) {}
			return count;
		}

		// Return a pointer to *one past* the last character not in the set 'delim'
		template <typename Iter, typename Char> inline Iter FindLastNotOf(Iter beg, Iter end, Char const* delim)
		{
			for (;end != beg && *FindChar(delim, *(end-1)) != 0; --end) {}
			return end;
		}
		template <typename Str, typename Char, typename Iter = traits<Str>::iter> inline Iter FindLastNotOf(Str& str, Char const* delim)
		{
			return FindLastNotOf(Begin(str), End(str), delim);
		}
		#pragma endregion

		#pragma region Resize
		// Resize a string. For pointers to fixed buffers, it's the callers responsibility to ensure sufficient space
		template <typename Str, typename Char2 = char, typename Char1 = Str::value_type> inline void Resize(Str& str, size_t new_size, Char2 ch = 0)
		{
			str.resize(new_size, Char1(ch));
		}
		template <typename Str, typename Char2 = char, typename = std::enable_if_t<std::is_pointer<Str>::value>> inline void Resize(Str str, size_t new_size, Char2 ch)
		{
			using Char1 = traits<Str>::value_type;
			auto i = End(str, new_size);
			auto iend = str + new_size;
			for (; i < iend; ++i) *i = Char1(ch);
			str[new_size] = 0;
		}
		template <typename Str, typename = std::enable_if_t<std::is_pointer<Str>::value>> inline void Resize(Str str, size_t new_size)
		{
			str[new_size] = 0;
		}
		template <typename Char1, size_t N, typename Char2 = char> inline void Resize(Char1 (&str)[N], size_t new_size, Char2 ch)
		{
			if (new_size >= N) throw std::exception("Fixed array buffer exceeded");
			Resize(&str[0], new_size, ch);
		}
		template <typename Char1, size_t N> inline void Resize(Char1 (&str)[N], size_t new_size)
		{
			if (new_size >= N) throw std::exception("Fixed array buffer exceeded");
			Resize(&str[0], new_size);
		}
		#pragma endregion

		#pragma region Append
		// Append a char to the end of 'str'
		// 'len' is an optimisation for pointer-like strings, so that Length() doesn't need to be called for each Append
		// Returns 'str' for method chaining
		template <typename Str, typename Char2, typename Char1 = traits<Str>::value_type> Str& Append(Str& str, size_t len, Char2 ch)
		{
			Resize(str, len+1);
			str[len] = Char1(ch);
			return str;
		}
		template <typename Str, typename Char2, typename Char1 = traits<Str>::value_type> Str& Append(Str& str, Char2 ch)
		{
			return Append(str, Length(str), ch);
		}
		#pragma endregion

		#pragma region Assign
		// Assign a range of characters to a sub-range within a string.
		// 'dest' is the string to be assigned to
		// 'offset' is the index position of where to start copying to
		// 'count' is the maximum number of characters to copy. The capacity of dest *must* be >= offset + count
		// 'first','last' the range of characters to assign to 'dest'
		// On return, dest will be resized to 'offset + min(count, last-first)'.
		template <typename Str, typename Iter, typename Char = traits<Str>::value_type> void Assign(Str& dest, size_t offset, size_t count, Iter first, Iter last)
		{
			// The number of characters to be copied to 'dest', clamped by 'count'
			auto size = std::min(count, size_t(last - first));

			// Set 'dest' to be the correct size
			// Assume 'dest' can be resized to 'offset + size'
			Resize(dest, offset + size);

			// Assign the characters
			for (auto out = Begin(dest) + offset; size--; ++out, ++first)
				*out = Char(*first);
		}
		template <typename Str, typename Iter> void Assign(Str& dest, Iter first, Iter last)
		{
			Assign(dest, 0, size_t(last - first), first, last);
		}

		// Assign a null terminated string to 'dest'
		template <typename Str, typename Char> void Assign(Str& dest, size_t offset, size_t count, Char const* first)
		{
			Resize(dest, offset);
			for (;count-- != 0 && *first; ++first)
				Append(dest, *first);
		}
		template <typename Str, typename Char> void Assign(Str& dest, Char const* first)
		{
			return Assign(dest, 0, ~size_t(), first);
		}
		#pragma endregion

		#pragma region Upper Case
		// Convert a string to upper case
		template <typename Str, typename Char = traits<Str>::value_type> inline Str& UpperCase(Str& str)
		{
			auto i = Begin(str); auto iend = End(str);
			for (; i != iend; ++i) *i = char_traits<Char>::upr(*i);
			return str;
		}
		template <typename Str, typename Char = Str::value_type> inline Str UpperCaseC(Str const& str)
		{
			auto s = str;
			return UpperCase(s);
		}
		template <typename Char> inline std::basic_string<Char> UpperCaseC(Char const* str)
		{
			auto s = std::basic_string<Char>(str);
			return UpperCase(s);
		}
		#pragma endregion

		#pragma region Lower Case
		// Convert a string to lower case
		template <typename Str, typename Char = traits<Str>::value_type> inline Str& LowerCase(Str& str)
		{
			auto i = Begin(str); auto iend = End(str);
			for (; i != iend; ++i) *i = char_traits<Char>::lwr(*i);
			return str;
		}
		template <typename Str, typename Char = Str::value_type> inline Str LowerCaseC(Str const& str)
		{
			auto s = str;
			return LowerCase(s);
		}
		template <typename Char> inline std::basic_string<Char> LowerCaseC(Char const* str)
		{
			auto s = std::basic_string<Char>(str);
			return LowerCase(s);
		}
		#pragma endregion

		#pragma region SubStr
		// Copy a substring from within 'src' to 'out'
		template <typename Str1, typename Str2> inline void SubStr(Str1 const& src, size_t offset, size_t count, Str2& out)
		{
			auto s = Begin(src) + offset;
			Assign(out, 0, count, s, s + count);
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
			auto beg = Begin(str);
			auto end = End(str);
			auto first = front ? FindFirst(beg  , end, [&](Char ch){ return !pred(ch); }) : beg;
			auto last  = back  ? FindLast (first, end, [&](Char ch){ return !pred(ch); }) : end;

			// Move the non-trimmed characters to the front of the string and trim the tail
			auto out = beg;
			for (; first != last; ++first, ++out) { *out = *first; }
			Resize(str, out - beg);
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
namespace pr::str
{
	PRUnitTest(StringCoreTests)
	{
		{// Str
			char const*     aptr   = "full";
			char            aarr[] = "";
			std::string     astr   = "";
			wchar_t const*  wptr   = L"";
			wchar_t         warr[] = L"full";
			std::wstring    wstr   = L"full";

			PR_CHECK(c_str(aptr), aptr);
			PR_CHECK(c_str(aarr), &aarr[0]);
			PR_CHECK(c_str(astr), astr.c_str());
			PR_CHECK(c_str(wptr), wptr);
			PR_CHECK(c_str(warr), &warr[0]);
			PR_CHECK(c_str(wstr), wstr.c_str());
		}
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

			PR_CHECK(*Begin(aptr) ==  'r' && *(End(aptr)-1) ==  'e', true);
			PR_CHECK(*Begin(aarr) ==  'r' && *(End(aarr)-1) ==  'e', true);
			PR_CHECK(*Begin(astr) ==  'r' && *(End(astr)-1) ==  'e', true);
			PR_CHECK(*Begin(wptr) == L'r' && *(End(wptr)-1) == L'e', true);
			PR_CHECK(*Begin(warr) == L'r' && *(End(warr)-1) == L'e', true);
			PR_CHECK(*Begin(wstr) == L'r' && *(End(wstr)-1) == L'e', true);
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
			char const*     aptr   =  "find in str";
			char            aarr[] =  "find in str";
			std::string     astr   =  "find in str";
			wchar_t const*  wptr   = L"find in str";
			wchar_t         warr[] = L"find in str";
			std::wstring    wstr   = L"find in str";

			PR_CHECK(*FindStr(aptr, "str") ==  's' && FindStr(aptr,  "bob") == End(aptr), true);
			PR_CHECK(*FindStr(aarr,L"str") ==  's' && FindStr(aarr, L"bob") == End(aarr), true);
			PR_CHECK(*FindStr(astr, "str") ==  's' && FindStr(astr,  "bob") == End(astr), true);
			PR_CHECK(*FindStr(wptr, "str") == L's' && FindStr(wptr, L"bob") == End(wptr), true);
			PR_CHECK(*FindStr(warr,L"str") == L's' && FindStr(warr,  "bob") == End(warr), true);
			PR_CHECK(*FindStr(wstr, "str") == L's' && FindStr(wstr, L"bob") == End(wstr), true);

			PR_CHECK(FindStr(aptr + 2, aptr + 9, "in") - Begin(aptr) == 5, true);
			PR_CHECK(FindStr(wptr + 2, wptr + 9, "in") - Begin(wptr) == 5, true);
		}
		{// FindFirst                  0123456789
			char const*     aptr   =  "find first";
			char            aarr[] =  "find first";
			std::string     astr   =  "find first";
			wchar_t const*  wptr   = L"find first";
			wchar_t         warr[] = L"find first";
			std::wstring    wstr   = L"find first";

			PR_CHECK(FindFirst(aptr, [](char    ch){ return ch ==  'i'; }) == &aptr[0] + 1        , true);
			PR_CHECK(FindFirst(aarr, [](char    ch){ return ch ==  'i'; }) == &aarr[0] + 1        , true);
			PR_CHECK(FindFirst(astr, [](char    ch){ return ch ==  'i'; }) == std::begin(astr) + 1, true);
			PR_CHECK(FindFirst(wptr, [](wchar_t ch){ return ch == L'i'; }) == &wptr[0] + 1        , true);
			PR_CHECK(FindFirst(warr, [](wchar_t ch){ return ch == L'i'; }) == &warr[0] + 1        , true);
			PR_CHECK(FindFirst(wstr, [](wchar_t ch){ return ch == L'i'; }) == std::begin(wstr) + 1, true);

			PR_CHECK(FindFirst(aptr, [](char    ch){ return ch ==  'x'; }) == &aptr[0] + 10        , true);
			PR_CHECK(FindFirst(aarr, [](char    ch){ return ch ==  'x'; }) == &aarr[0] + 10        , true);
			PR_CHECK(FindFirst(astr, [](char    ch){ return ch ==  'x'; }) == std::begin(astr) + 10, true);
			PR_CHECK(FindFirst(wptr, [](wchar_t ch){ return ch == L'x'; }) == &wptr[0] + 10        , true);
			PR_CHECK(FindFirst(warr, [](wchar_t ch){ return ch == L'x'; }) == &warr[0] + 10        , true);
			PR_CHECK(FindFirst(wstr, [](wchar_t ch){ return ch == L'x'; }) == std::begin(wstr) + 10, true);

			PR_CHECK(FindFirst(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  'i'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindFirst(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  't'; }) == &aptr[0] + 8, true);

			PR_CHECK(FindFirst(aptr, 2, 6, [](char ch){ return ch ==  'i'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindFirst(aptr, 2, 6, [](char ch){ return ch ==  't'; }) == &aptr[0] + 8, true);
		}
		{//FindLast                    0123456789
			char const*     aptr   =  "find flast";
			char            aarr[] =  "find flast";
			std::string     astr   =  "find flast";
			wchar_t const*  wptr   = L"find flast";
			wchar_t         warr[] = L"find flast";
			std::wstring    wstr   = L"find flast";

			PR_CHECK(FindLast(aptr, [](char    ch){ return ch ==  'f'; }) == &aptr[0] + 6        , true);
			PR_CHECK(FindLast(aarr, [](char    ch){ return ch ==  'f'; }) == &aarr[0] + 6        , true);
			PR_CHECK(FindLast(astr, [](char    ch){ return ch ==  'f'; }) == std::begin(astr) + 6, true);
			PR_CHECK(FindLast(wptr, [](wchar_t ch){ return ch == L'f'; }) == &wptr[0] + 6        , true);
			PR_CHECK(FindLast(warr, [](wchar_t ch){ return ch == L'f'; }) == &warr[0] + 6        , true);
			PR_CHECK(FindLast(wstr, [](wchar_t ch){ return ch == L'f'; }) == std::begin(wstr) + 6, true);

			PR_CHECK(FindLast(aptr, [](char    ch){ return ch ==  'x'; }) == &aptr[0] + 0        , true);
			PR_CHECK(FindLast(aarr, [](char    ch){ return ch ==  'x'; }) == &aarr[0] + 0        , true);
			PR_CHECK(FindLast(astr, [](char    ch){ return ch ==  'x'; }) == std::begin(astr) + 0, true);
			PR_CHECK(FindLast(wptr, [](wchar_t ch){ return ch == L'x'; }) == &wptr[0] + 0        , true);
			PR_CHECK(FindLast(warr, [](wchar_t ch){ return ch == L'x'; }) == &warr[0] + 0        , true);
			PR_CHECK(FindLast(wstr, [](wchar_t ch){ return ch == L'x'; }) == std::begin(wstr) + 0, true);

			PR_CHECK(FindLast(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  'f'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindLast(&aptr[0] + 2, &aptr[0] + 8, [](char ch){ return ch ==  't'; }) == &aptr[0] + 2, true);

			PR_CHECK(FindLast(aptr, 2, 6, [](char ch){ return ch ==  'f'; }) == &aptr[0] + 6, true);
			PR_CHECK(FindLast(aptr, 2, 6, [](char ch){ return ch ==  't'; }) == &aptr[0] + 2, true);
		}
		{// FindFirstOf             0123456
			char         aarr[] =  "AaAaAa";
			wchar_t      warr[] = L"AaAaAa";
			std::string  astr   =  "AaAaAa";
			std::wstring wstr   = L"AaAaAa";

			PR_CHECK(FindFirstOf(aarr, "A") == &aarr[0] + 0        , true);
			PR_CHECK(FindFirstOf(warr, "a") == &warr[0] + 1        , true);
			PR_CHECK(FindFirstOf(astr, "B") == std::begin(astr) + 6, true);
			PR_CHECK(FindFirstOf(wstr, "B") == std::begin(wstr) + 6, true);
		}
		{//FindLastOf               0123456
			char         aarr[] =  "AaAaAa";
			wchar_t      warr[] = L"AaAaaa";
			std::string  astr   =  "AaAaaa";
			std::wstring wstr   = L"Aaaaaa";
			PR_CHECK(FindLastOf(aarr, L"A") == &aarr[0] + 5    , true);
			PR_CHECK(FindLastOf(warr, L"A") == &warr[0] + 3    , true);
			PR_CHECK(FindLastOf(astr, L"B") == std::begin(astr), true);
			PR_CHECK(FindLastOf(wstr, L"B") == std::begin(wstr), true);
		}
		{//FindFirstNotOf           01234567890123
			char         aarr[] =  "junk_str_junk";
			wchar_t      warr[] = L"junk_str_junk";
			std::string  astr   =  "junk_str_junk";
			std::wstring wstr   = L"junk_str_junk";
			PR_CHECK(FindFirstNotOf(aarr, "_knuj"   ) == &aarr[0] + 5         , true);
			PR_CHECK(FindFirstNotOf(warr, "_knuj"   ) == &warr[0] + 5         , true);
			PR_CHECK(FindFirstNotOf(astr, "_knujstr") == std::begin(astr) + 13, true);
			PR_CHECK(FindFirstNotOf(wstr, "_knujstr") == std::begin(wstr) + 13, true);
		}
		{//FindLastNotOf            01234567890123
			char         aarr[] =  "junk_str_junk";
			wchar_t      warr[] = L"junk_str_junk";
			std::string  astr   =  "junk_str_junk";
			std::wstring wstr   = L"junk_str_junk";
			PR_CHECK(FindLastNotOf(aarr, "_knuj"   ) == &aarr[0] + 8        , true);
			PR_CHECK(FindLastNotOf(warr, "_knuj"   ) == &warr[0] + 8        , true);
			PR_CHECK(FindLastNotOf(astr, "_knujstr") == std::begin(astr) + 0, true);
			PR_CHECK(FindLastNotOf(wstr, "_knujstr") == std::begin(wstr) + 0, true);
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
		{// Append
			char         aarr[5] = {};
			wchar_t      warr[5] = {};
			std::string  astr;
			std::wstring wstr;

			Append(aarr, 'a'); Append(aarr, L'b'); Append(aarr, 'c'); PR_CHECK(Equal(aarr, "abc"), true);
			Append(warr, 'a'); Append(warr, L'b'); Append(warr, 'c'); PR_CHECK(Equal(warr, "abc"), true);
			Append(astr, 'a'); Append(astr, L'b'); Append(astr, 'c'); PR_CHECK(Equal(astr, "abc"), true);
			Append(wstr, 'a'); Append(wstr, L'b'); Append(wstr, 'c'); PR_CHECK(Equal(wstr, "abc"), true);
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

			Assign(astr, 2, ~size_t(), asrc, asrc+5); PR_CHECK(Equal(astr, "ststrin"), true);
			Assign(astr, 2, ~size_t(), wsrc, wsrc+5); PR_CHECK(Equal(astr, "ststrin"), true);
			Assign(wstr, 2, ~size_t(), asrc, asrc+5); PR_CHECK(Equal(wstr, "ststrin"), true);
			Assign(wstr, 2, ~size_t(), wsrc, wsrc+5); PR_CHECK(Equal(wstr, "ststrin"), true);

			Assign(astr, 2, ~size_t(), "ab"); PR_CHECK(Equal(astr, "stab"), true);
			Assign(astr, 2, ~size_t(), "ab"); PR_CHECK(Equal(astr, "stab"), true);
			Assign(wstr, 2, ~size_t(), "ab"); PR_CHECK(Equal(wstr, "stab"), true);
			Assign(wstr, 2, ~size_t(), "ab"); PR_CHECK(Equal(wstr, "stab"), true);

			Assign(astr, "done"); PR_CHECK(Equal(astr, "done"), true);
			Assign(astr, "done"); PR_CHECK(Equal(astr, "done"), true);
			Assign(wstr, "done"); PR_CHECK(Equal(wstr, "done"), true);
			Assign(wstr, "done"); PR_CHECK(Equal(wstr, "done"), true);
		}
		{//UpperCase
			char         aarr[5] =  "CaSe";
			wchar_t      warr[5] = L"CaSe";
			std::string  astr    =  "CaSe";
			std::wstring wstr    = L"CaSe";

			PR_CHECK(Equal(UpperCaseC(aarr), L"CASE"), true); PR_CHECK(Equal(aarr, "CaSe"), true);
			PR_CHECK(Equal(UpperCase (warr), L"CASE"), true); PR_CHECK(Equal(warr, "CASE"), true);
			PR_CHECK(Equal(UpperCase (astr), L"CASE"), true); PR_CHECK(Equal(astr, "CASE"), true);
			PR_CHECK(Equal(UpperCaseC(wstr), L"CASE"), true); PR_CHECK(Equal(wstr, "CaSe"), true);
		}
		{//LowerCase
			char         aarr[5] =  "CaSe";
			wchar_t      warr[5] = L"CaSe";
			std::string  astr    =  "CaSe";
			std::wstring wstr    = L"CaSe";

			PR_CHECK(Equal(LowerCaseC(aarr), L"case"), true); PR_CHECK(Equal(aarr, "CaSe"), true);
			PR_CHECK(Equal(LowerCase (warr), L"case"), true); PR_CHECK(Equal(warr, "case"), true);
			PR_CHECK(Equal(LowerCase (astr), L"case"), true); PR_CHECK(Equal(astr, "case"), true);
			PR_CHECK(Equal(LowerCaseC(wstr), L"case"), true); PR_CHECK(Equal(wstr, "CaSe"), true);
		}
		{//SubStr
			char    asrc[] =  "SubstringExtract";
			wchar_t wsrc[] = L"SubstringExtract";

			char         aarr[10] = {};
			wchar_t      warr[10] = {};
			std::string  astr;
			std::wstring wstr;

			SubStr(asrc, 3, 6, aarr); PR_CHECK(Equal(aarr, "string"), true);
			SubStr(asrc, 3, 6, warr); PR_CHECK(Equal(warr, "string"), true);
			SubStr(asrc, 3, 6, astr); PR_CHECK(Equal(astr, "string"), true);
			SubStr(asrc, 3, 6, wstr); PR_CHECK(Equal(wstr, "string"), true);

			SubStr(wsrc, 3, 6, aarr); PR_CHECK(Equal(aarr, "string"), true);
			SubStr(wsrc, 3, 6, warr); PR_CHECK(Equal(warr, "string"), true);
			SubStr(wsrc, 3, 6, astr); PR_CHECK(Equal(astr, "string"), true);
			SubStr(wsrc, 3, 6, wstr); PR_CHECK(Equal(wstr, "string"), true);
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

			PR_CHECK(Equal(Trim(" \t ", aws, false, true), ""), true);
		}
	}
}
#endif


