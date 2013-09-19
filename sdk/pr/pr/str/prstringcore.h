//***********************************************************************
// Core String functions
//  Copyright © Rylogic Ltd 2008
//***********************************************************************
// String types:
//	std::string, std::wstring, etc
//	char[], wchar_t[], etc
//	char*, wchar_t*, etc

#ifndef PR_STR_STRING_CORE_H
#define PR_STR_STRING_CORE_H

#include <cstddef>
#include <ctype.h>
#include <functional>

//"pr/common/assert.h" should be included prior to this for pr asserts
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif

#pragma warning (push)
#pragma warning (disable: 4351) // warning C4351: new behavior: elements of array will be default initialized

namespace pr
{
	namespace str
	{
		// Helper object for converting a fixed buffer to an stl-like container
		template <typename Char> struct fixed_buffer
		{
			typedef Char value_type;

			size_t m_cap;
			size_t m_len;
			Char* m_buf;

			fixed_buffer(Char* buf, size_t capacity) :m_cap(capacity-1) ,m_len(0) ,m_buf(buf) { m_buf[0] = 0; }
			Char const* begin() const       { return m_buf; }
			Char*       begin()             { return m_buf; }
			Char const* end() const         { return m_buf + m_len; }
			Char*       end()               { return m_buf + m_len; }
			size_t      size() const        { return m_len; }
			bool        empty() const       { return m_len == 0; }
			void        clear()             { return m_len = 0; }
			void        push_back(Char ch)  { if (m_len != m_cap) { m_buf[m_len++] = ch; m_buf[m_len] = 0; } }
			Char const* c_str() const       { return m_buf; }
		};

		// Helper for buffering characters read from an iterator
		template <typename Iter, typename tchar, size_t tlen> struct iter_buffer
		{
			Iter&  m_iter;
			tchar  m_buf[tlen];
			size_t m_count;

			explicit iter_buffer(Iter& iter) :m_iter(iter) ,m_buf() ,m_count(0) {}
			iter_buffer(iter_buffer const&);
			iter_buffer&  operator =(iter_buffer const&);
			bool          empty() const            { return m_count == 0; }
			bool          full() const             { return m_count == tlen; }
			tchar         operator * () const      { return tchar(*m_iter); }
			iter_buffer&  operator ++()            { if (m_count < tlen) {m_buf[m_count++] = **this;} ++m_iter; return *this; }
			tchar         operator [](int i) const { return tchar(m_iter[i]); }
		};

		// Traits for getting the iterator type for a string
		template <typename Type> struct Traits
		{
			typedef typename Type::const_iterator citer;
			typedef typename Type::iterator       iter;
			typedef typename Type::value_type     value_type;
		};
		template <typename Type> struct Traits<Type*>
		{
			typedef Type const* citer;
			typedef Type*       iter;
			typedef Type        value_type;
		};
		template <typename Type> struct Traits<Type* const>
		{
			typedef Type const* citer;
			typedef Type*       iter;
			typedef Type        value_type;
		};
		template <typename Type, size_t Len> struct Traits<Type[Len]>
		{
			typedef Type const* citer;
			typedef Type*       iter;
			typedef Type        value_type;
		};

		// Character classes
		template <typename Char> inline bool IsNewLine(Char ch)                 { return ch == Char('\n'); }
		template <typename Char> inline bool IsLineSpace(Char ch)               { return ch == Char(' ') || ch == Char('\t') || ch == Char('\r'); }
		template <typename Char> inline bool IsWhiteSpace(Char ch)              { return IsLineSpace(ch) || IsNewLine(ch) || ch == Char('\v') || ch == Char('\f'); }
		template <typename Char> inline bool IsDigit(Char ch)                   { return IsDecDigit(ch); }
		template <typename Char> inline bool IsAlpha(Char ch)                   { return (ch >= Char('a') && ch <= Char('z')) || (ch >= Char('A') && ch <= Char('Z')); }
		template <typename Char> inline bool IsDecDigit(Char ch)                { return (ch >= Char('0') && ch <= Char('9')); }
		template <typename Char> inline bool IsBinDigit(Char ch)                { return (ch >= Char('0') && ch <= Char('1')); }
		template <typename Char> inline bool IsOctDigit(Char ch)                { return (ch >= Char('0') && ch <= Char('7')); }
		template <typename Char> inline bool IsHexDigit(Char ch)                { return IsDecDigit(ch) || (ch >= Char('a') && ch <= Char('f')) || (ch >= Char('A') && ch <= Char('F')); }
		template <typename Char> inline bool IsIdentifier(Char ch, bool first)  { return ch == Char('_') || IsAlpha(ch) || (!first && IsDigit(ch)); }

		// Iterator access to strings
		template <typename tstr> inline typename Traits<tstr>::citer  Begin (tstr const& str)             { return str.begin(); }
		template <typename tstr> inline typename Traits<tstr>::citer  BeginC(tstr& str)                   { return const_cast<tstr const&>(str).begin(); }
		template <typename tstr> inline typename Traits<tstr>::iter   Begin (tstr& str)                   { return str.begin(); }
		template <typename tstr> inline typename Traits<tstr>::citer  End   (tstr const& str)             { return str.end(); }
		template <typename tstr> inline typename Traits<tstr>::citer  EndC  (tstr& str)                   { return const_cast<tstr const&>(str).end(); }
		template <typename tstr> inline typename Traits<tstr>::iter   End   (tstr& str)                   { return str.end(); }
		template <typename tstr> inline typename Traits<tstr*>::citer Begin (tstr const* str)             { return str; }
		template <typename tstr> inline typename Traits<tstr*>::citer BeginC(tstr* str)                   { return const_cast<tstr const*>(str); }
		template <typename tstr> inline typename Traits<tstr*>::iter  Begin (tstr* str)                   { return str; }
		template <typename tstr> inline typename Traits<tstr*>::citer End   (tstr const* str)             { for (; *str; ++str){} return str; }
		template <typename tstr> inline typename Traits<tstr*>::citer EndC  (tstr* str)                   { for (; *str; ++str){} return const_cast<tstr const*>(str); }
		template <typename tstr> inline typename Traits<tstr*>::iter  End   (tstr* str)                   { for (; *str; ++str){} return str; }
		template <typename tstr> inline typename Traits<tstr>::citer  End   (tstr const& str , size_t)    { return End (str); }
		template <typename tstr> inline typename Traits<tstr>::citer  EndC  (tstr& str       , size_t)    { return EndC(str); }
		template <typename tstr> inline typename Traits<tstr>::iter   End   (tstr& str       , size_t)    { return End (str); }
		template <typename tstr> inline typename Traits<tstr*>::citer End   (tstr const* str , size_t N)  { for (; N-- && *str; ++str){} return str; }
		template <typename tstr> inline typename Traits<tstr*>::citer EndC  (tstr const* str , size_t N)  { for (; N-- && *str; ++str){} return const_cast<tstr const*>(str); }
		template <typename tstr> inline typename Traits<tstr*>::iter  End   (tstr* str       , size_t N)  { for (; N-- && *str; ++str){} return str; }
		inline char const* End      (char const* str)              { return str + strlen(str); }
		inline char const* EndC     (char* str)                    { return str + strlen(str); }
		inline char*       End      (char* str)                    { return str + strlen(str); }
		inline wchar_t const* End   (wchar_t const* str)           { return str + wcslen(str); }
		inline wchar_t const* EndC  (wchar_t* str)                 { return str + wcslen(str); }
		inline wchar_t*       End   (wchar_t* str)                 { return str + wcslen(str); }
		
		// Return true if 'str' is a zero length string
		template <typename tstr> inline bool Empty(tstr const& str)
		{
			return Begin(str) == End(str, 1);
		}

		// Return the length of a string. This version is for std::vector-like strings
		template <typename tstr> inline size_t Length(tstr const& str)
		{
			return static_cast<size_t>(str.size());
		}
		template <typename tstr> inline size_t Length(tstr const* str)
		{
			return static_cast<size_t>(End(str) - Begin(str)); // Note: Excluding the null terminator (same as strlen)
		}
		template <typename tstr> inline size_t Length(tstr* str)
		{
			return Length(const_cast<tstr const*>(str));
		}

		// Lower/upper case
		template <typename tchar> inline tchar Lwr(tchar ch) { return (ch >= 'A' && ch <= 'Z') ? ch + ('a'-'A') : ch; }
		template <typename tchar> inline tchar Upr(tchar ch) { return (ch >= 'a' && ch <= 'z') ? ch - ('a'-'A') : ch; }

		// Predicates for char comparisons
		struct Pred_Equal       { template <typename L, typename R> bool operator()(L lhs, R rhs) const { return lhs == rhs; }               };
		struct Pred_EqualNoCase { template <typename L, typename R> bool operator()(L lhs, R rhs) const { return Lwr(lhs) == Lwr(rhs); } };

		// Test strings for equality
		inline bool Equal(char const* str1, char const* str2)
		{
			return ::strcmp(str1, str2) == 0;
		}
		inline bool EqualI(char const* str1, char const* str2)
		{
			return ::_stricmp(str1, str2) == 0;
		}
		template <typename tstr1, typename tstr2, typename Pred> inline bool Equal(tstr1 const& str1, tstr2 const& str2, Pred pred)
		{
			typename Traits<tstr1>::citer i = Begin(str1), i_end = End(str1);
			typename Traits<tstr2>::citer j = Begin(str2), j_end = End(str2);
			for( ; !(i == i_end) && !(j == j_end) && pred(*i, *j); ++i, ++j ) {}
			return i == i_end && j == j_end;
		}
		template <typename tstr1, typename tstr2> inline bool Equal(tstr1 const& str1, tstr2 const& str2)
		{
			return Equal(str1, str2, Pred_Equal());
		}
		template <typename tstr1, typename tstr2> inline bool EqualI(tstr1 const& str1, tstr2 const& str2)
		{
			return Equal(str1, str2, Pred_EqualNoCase());
		}

		// Test strings for equality with fixed length
		inline bool EqualN(char const* str1, char const* str2, size_t length)
		{
			return ::memcmp(str1, str2, length) == 0;
		}
		inline bool EqualNI(char const* str1, char const* str2, size_t length)
		{
			return ::_strnicmp(str1, str2, length) == 0;
		}
		template <typename tstr1, typename tstr2, typename Pred> inline bool EqualN(tstr1 const& str1, tstr2 const& str2, size_t length, Pred pred)
		{
			typename Traits<tstr1>::citer i = Begin(str1), i_end = End(str1, length);
			typename Traits<tstr2>::citer j = Begin(str2), j_end = End(str2, length);
			for( ; length-- && !(i == i_end) && !(j == j_end) && pred(*i, *j); ++i, ++j ) {}
			return length == size_t(-1) || (i == i_end && j == j_end);
		}
		template <typename tstr1, typename tstr2> inline bool EqualN(tstr1 const& str1, tstr2 const& str2, size_t length)
		{
			return EqualN(str1, str2, length, Pred_Equal());
		}
		template <typename tstr1, typename tstr2> inline bool EqualNI(tstr1 const& str1, tstr2 const& str2, size_t length)
		{
			return EqualN(str1, str2, length, Pred_EqualNoCase());
		}
		template <typename tstr1, typename tchar, size_t Len> inline bool EqualN(tstr1 const& str1, tchar (&str2)[Len])
		{
			return EqualN(str1, str2, Len, Pred_Equal());
		}
		template <typename tstr1, typename tchar, size_t Len> inline bool EqualNI(tstr1 const& str1, tchar (&str2)[Len])
		{
			return EqualN(str1, str2, Len, Pred_EqualNoCase());
		}

		// Resize a string
		template <typename tstr, typename tchar> inline void Resize(tstr& str, size_t new_size, tchar ch)
		{
			str.resize(new_size, ch);
		}
		template <typename tstr, typename tchar> inline void Resize(tstr* str, size_t new_size, tchar ch)
		{
			typename Traits<tstr*>::value_type c = ch;
			for (tstr* i = End(str,new_size), *iend = str + new_size; i != iend; ++i) *i = c;
			str[new_size] = 0;
		}
		template <typename tstr> inline void Resize(tstr& str, size_t new_size)
		{
			str.resize(new_size);
		}
		template <typename tstr> inline void Resize(tstr* str, size_t new_size)
		{
			str[new_size] = 0;
			// Should really assert this:
			// ASSERT(End(str) - Begin(str) >= new_size);
			// but can't guarantee 'str' has been initialised
		}

		// Assign a range of characters to a string.
		template <typename iter, typename tstr> void Assign(iter first, iter last, size_t offset, tstr& dest, size_t max_dest_size)
		{
			// The number of characters to be copied to 'dest'
			size_t size = static_cast<size_t>(last - first);

			// Clamp this by the max size of 'dest'
			if (offset + size > max_dest_size) size = (max_dest_size - offset) * int(max_dest_size > offset);

			// Set 'dest' to be the correct size
			Resize(dest, offset + size);

			// Assign the characters
			for (typename Traits<tstr>::iter out = Begin(dest) + offset; size--; ++out, ++first)
				*out = static_cast<typename Traits<tstr>::value_type>(*first);
		}

		// Simple character arrays are null terminated by these functions
		// Note: 'max_dest_size' is assumed to be the length of the 'dest' array
		// so 'dest[max_dest_size-1] == 0' is the null terminator. This means
		// 'End(dest) - Begin(dest)' == Length(dest) is at most max_dest_size-1.
		template <typename iter, typename tstr> void Assign(iter first, iter last, size_t offset, tstr* dest, size_t max_dest_size)
		{
			// The number of characters to be copied to 'dest'
			size_t size = static_cast<size_t>(last - first);

			// Clamp this by the max size of 'dest' (allow for null terminator)
			if (offset + size + 1 > max_dest_size) size = (max_dest_size - offset - 1) * int(max_dest_size > offset);

			// Assign the characters
			dest[offset + size] = 0;
			for (tstr* in = dest + offset; size--; ++in, ++first)
				*in = static_cast<tstr>(*first);
		}

		// Specialisation for char* to take advantage of memcpy intrinsic
		inline void Assign(char const* first, char const* last, size_t offset, char* dest, size_t max_dest_size)
		{
			// The number of characters to be copied to 'dest'
			size_t size = static_cast<size_t>(last - first);

			// Clamp this by the max size of 'dest' (allow for null terminator)
			if (offset + size + 1 > max_dest_size) size = (max_dest_size - offset - 1) * int(max_dest_size > offset);

			// Assign the characters and terminate
			memcpy(dest + offset, first, size);
			dest[offset + size] = 0;
		}

		// This is needed to prevent calls to Assign with non-const char going to the template version
		inline void Assign(char* first, char* last, size_t offset, char* dest, size_t max_dest_size)
		{
			Assign(const_cast<char const*>(first), const_cast<char const*>(last), offset, dest, max_dest_size);
		}

		// Assign a number of characters from an iterator to 'dest'
		template <typename iter, typename tstr> void Assign(iter first, size_t count, size_t offset, tstr& dest, size_t max_dest_size)
		{
			// Clamp to the max size of 'dest'
			if (count + offset > max_dest_size) count = (max_dest_size - offset) * int(max_dest_size > offset);

			// Set 'dest' to the correct size
			Resize(dest, offset + count);

			// Assign the characters
			for (typename Traits<tstr>::iter out = Begin(dest) + offset; count--; ++out, ++first)
				*out = static_cast<typename Traits<tstr>::value_type>(*first);
		}

		// Assign a number of characters from an iterator to 'dest'
		// Note: 'max_dest_size' is assumed to be the length of the 'dest' array
		// so 'dest[max_dest_size-1] == 0' is the null terminator. This means
		// 'End(dest) - Begin(dest)' == Length(dest) is at most max_dest_size-1.
		template <typename iter, typename tstr> void Assign(iter first, size_t count, size_t offset, tstr* dest, size_t max_dest_size)
		{
			// Clamp to the max size of 'dest'
			if (count + offset + 1 > max_dest_size) count = (max_dest_size - offset - 1) * int(max_dest_size > offset);

			// Assign the characters
			dest[offset + count] = 0;
			for (tstr* in = dest + offset; count--; ++in, ++first)
				*in = static_cast<tstr>(*first);
		}

		// Specialisation for char* to take advantage of memcpy intrinsic
		inline void Assign(char const* first, size_t count, size_t offset, char* dest, size_t max_dest_size)
		{
			// Clamp to the max size of 'dest'
			if (count + offset + 1 > max_dest_size) count = (max_dest_size - offset - 1) * int(max_dest_size > offset);
			memcpy(dest + offset, first, count);
			dest[offset + count] = 0;
		}

		// This is needed to prevent calls to Assign with non-const char going to the template version
		inline void Assign(char* first, size_t count, size_t offset, char* dest, size_t max_dest_size)
		{
			Assign(const_cast<char const*>(first), count, offset, dest, max_dest_size);
		}
		template <typename iter, typename tstr> inline void Assign(iter first, iter last, size_t offset, tstr& dest)
		{
			Assign(first, last, offset, dest, size_t(~0));
		}
		template <typename iter, typename tstr> inline void Assign(iter first, iter last, tstr& dest)
		{
			Assign(first, last, 0, dest, size_t(~0));
		}
		template <typename tstr1, typename tstr2> inline void Assign(tstr1* src, tstr2& dest, size_t max_dest_size)
		{
			Assign(Begin(src), End(src, max_dest_size), 0, dest, max_dest_size);
		}

		// Copy characters from src to dest while pred(src) is true
		// Advances 'src' in the copying process
		template <typename iter, typename tstr, typename pred> inline size_t AssignAdv(iter& src, tstr* dest, size_t max_dest_size, pred const& pred)
		{
			size_t count = 0;
			for (; count != max_dest_size && pred(src); ++count, ++src, ++dest) *dest = *src;
			return count;
		}

		// Return a pointer to delimiters, either the ones provided or the default ones
		template <typename tstr> inline tstr* Delim(tstr* delim)
		{
			static tstr const default_delim[] = {' ', '\t', '\n', '\r', 0};
			return delim ? delim : default_delim;
		}

		// Find a single character in a string
		template <typename iter, typename tchar> inline iter FindChar(iter first, iter last, tchar ch)
		{
			for(;first != last && *first != ch; ++first) {}
			return first;
		}
		template <typename tstr, typename tchar> inline tstr* FindChar(tstr* str, tchar ch)
		{
			for (;*str && *str != ch; ++str) {}
			return str;
		}

		// Predicates for testing whether a char is in or not in a set of delimiters
		template <typename tchar = char> struct NotOneOf
		{
			tchar const* m_delim;
			NotOneOf(tchar const* delim) :m_delim(delim) {}
			template <typename tchar2> bool operator()(tchar2 ch) const { return *FindChar(m_delim, ch) == 0; }
		};
		template <typename tchar = char> struct IsOneOf
		{
			tchar const* m_delim;
			IsOneOf(tchar const* delim) :m_delim(delim) {}
			template <typename tchar2> bool operator()(tchar2 ch) const { return *FindChar(m_delim, ch) != 0; }
		};
		template <typename tpred> struct Not
		{
			tpred m_pred;
			Not(tpred pred) :m_pred(pred) {}
			template <typename tchar> bool operator()(tchar ch) const { return !m_pred(ch); }
		};

		struct Pred_Find       { template <typename tstr1, typename tstr2> bool operator() (tstr1 const* src, tstr2 const* what, size_t len) const {return EqualN (src, what, len);} };
		struct Pred_FindNoCase { template <typename tstr1, typename tstr2> bool operator() (tstr1 const* src, tstr2 const* what, size_t len) const {return EqualNI(src, what, len);} };

		// Find the sub string 'what' in the range of characters provided.
		// Returns an iterator to the sub string or to the end of the range.
		template <typename iter, typename tstr, typename Pred> inline iter FindStrIf(iter src, iter last, tstr const& what, Pred pred)
		{
			if (Empty(what)) return last;
			size_t what_len = Length(what);
			for (; src != last && !pred(&*src, BeginC(what), what_len); ++src) {}
			return src;
		}
		template <typename iter, typename tstr, typename Pred> inline iter FindStrIf(iter src, tstr const& what, Pred pred)
		{
			if (Empty(what)) return End(src);
			size_t what_len = Length(what);
			for (; *src && !pred(&*src, BeginC(what), what_len); ++src) {}
			return src;
		}
		template <typename iter, typename tstr> inline iter FindStr(iter src, iter last, tstr const& what)
		{
			return FindStrIf(src, last, what, Pred_Find());
		}
		template <typename iter, typename tstr> inline iter FindStr(iter src, tstr const& what)
		{
			return FindStrIf(src, what, Pred_Find());
		}
		template <typename iter, typename tstr> inline iter FindStrNoCase(iter src, iter last, tstr const& what)
		{
			return FindStrIf(src, last, what, Pred_FindNoCase());
		}
		template <typename iter, typename tstr> inline iter FindStrNoCase(iter src, tstr const& what)
		{
			return FindStrIf(src, what, Pred_FindNoCase());
		}

		// Find using a predicate
		template <typename iter, typename tpred> inline iter FindFirst(iter src, iter last, tpred pred)
		{
			for (; src != last && !pred(*src); ++src) {}
			return src;
		}
		template <typename iter, typename tpred> inline iter FindFirst(iter src, tpred pred)
		{
			for (; *src && !pred(*src); ++src) {}
			return src;
		}
		template <typename iter, typename tpred> inline iter FindLast(iter src, iter last, tpred pred)
		{
			for (iter i = last; i != src;) { if (pred(*--i)) return i; }
			return last;
		}
		template <typename iter, typename tpred> inline iter FindLast(iter src, tpred pred)
		{
			iter last; for (last = src; *last; ++last) {}
			for (iter i = last; i != src;) { if (pred(*--i)) return i; }
			return last;
		}

		// Find the first occurance of one of the chars in 'delim'
		template <typename iter, typename tchar> inline iter FindFirstOf(iter src, iter last, tchar const* delim)
		{
			for (; src != last && *FindChar(delim, *src) == 0; ++src) {}
			return src;
		}
		template <typename iter, typename tchar> inline iter FindFirstOf(iter src, tchar const* delim)
		{
			for (; *src && *FindChar(delim, *src) == 0; ++src) {}
			return src;
		}
		template <typename iter, typename tchar> inline size_t FindFirstOfAdv(iter& src, tchar const* delim)
		{
			size_t count = 0;
			for (; *src && *FindChar(delim, *src) == 0; ++src, ++count) {}
			return count;
		}

		// Find the last occurance of one of the chars in 'delim'
		template <typename iter, typename tchar> inline iter FindLastOf(iter src, iter last, tchar const* delim)
		{
			for (iter i = last; i != src;) { if (*FindChar(delim, *--i) != 0) return i; }
			return last;
		}
		template <typename iter, typename tchar> inline iter FindLastOf(iter src, tchar const* delim)
		{
			iter last; for (last = src; *last; ++last) {}
			for (iter i = last; i != src;) { if (*FindChar(delim, *--i) != 0) return i; }
			return last;
		}

		// Find the first character not in the set 'delim'
		template <typename iter, typename tchar> inline iter FindFirstNotOf(iter src, iter last, tchar const* delim)
		{
			for (; src != last && *FindChar(delim, *src) != 0; ++src) {}
			return src;
		}
		template <typename iter, typename tchar> inline iter FindFirstNotOf(iter src, tchar const* delim)
		{
			for (; *src && *FindChar(delim, *src) != 0; ++src) {}
			return src;
		}
		template <typename iter, typename tchar> inline size_t FindFirstNotOfAdv(iter& src, tchar const* delim)
		{
			size_t count = 0;
			for (; *src && *FindChar(delim, *src) != 0; ++src, ++count) {}
			return count;
		}

		// Find the last character not in the set 'delim'
		template <typename iter, typename tchar> inline iter FindLastNotOf(iter src, iter last, tchar const* delim)
		{
			for (iter i = last; i != src;) { if (*FindChar(delim, *--i) == 0) return i; }
			return last;
		}
		template <typename iter, typename tchar> inline iter FindLastNotOf(iter src, tchar const* delim)
		{
			iter last; for (last = src; *last; ++last) {}
			for (iter i = last; i != src;) { if (*FindChar(delim, *--i) == 0) return i; }
			return last;
		}

		// Convert a string to upper case
		template <typename tstr> inline tstr& UpperCase(tstr& str)
		{
			for (typename Traits<tstr>::iter i = Begin(str), i_end = End(str); i != i_end; ++i)
				*i = static_cast<typename Traits<tstr>::value_type>(toupper(*i));
			return str;
		}
		template <typename tstr> inline tstr  UpperCase(tstr const& src)
		{
			tstr dest; Assign(Begin(src), End(src), 0, dest);
			return UpperCase(dest);
		}
		template <typename tstr> inline tstr* UpperCase(tstr* str)
		{
			for (tstr* i = str; *i; ++i) {*i = static_cast<tstr>(toupper(*i));}
			return str;
		}
		template <typename tstr1, typename tstr2> inline tstr2& UpperCase(tstr1 const& src, tstr2& dest, size_t max_dest_size = size_t(~0))
		{
			Assign(Begin(src), End(src), 0, dest, max_dest_size);
			return UpperCase(dest);
		}
		template <typename tstr1, typename tstr2> inline tstr1* UpperCase(tstr1 const* src, tstr2* dest, size_t max_dest_size = size_t(~0))
		{
			Assign(src, dest, max_dest_size);
			return UpperCase(dest);
		}

		// Convert a string to lower case
		template <typename tstr> inline tstr& LowerCase(tstr& str)
		{
			for (typename Traits<tstr>::iter i = Begin(str), i_end = End(str); i != i_end; ++i)
				*i = static_cast<typename Traits<tstr>::value_type>(tolower(*i));
			return str;
		}
		template <typename tstr> inline tstr  LowerCase(tstr const& src)
		{
			tstr dest; Assign(Begin(src), End(src), 0, dest);
			return LowerCase(dest);
		}
		template <typename tstr> inline tstr* LowerCase(tstr* str)
		{
			for( tstr* i = str; *i; ++i ) {*i = static_cast<tstr>(tolower(*i));}
			return str;
		}
		template <typename tstr1, typename tstr2> inline tstr2& LowerCase(tstr1 const& src, tstr2& dest, size_t max_dest_size = size_t(~0))
		{
			Assign(Begin(src), End(src), 0, dest, max_dest_size);
			return LowerCase(dest);
		}
		template <typename tstr1, typename tstr2> inline tstr1* LowerCase(tstr1 const* src, tstr2* dest, size_t max_dest_size = size_t(~0))
		{
			Assign(src, dest, max_dest_size);
			return LowerCase(dest);
		}

		// Copy a substring from within 'src' to 'out'
		template <typename tstr1, typename tstr2> inline void SubStr(tstr1 const& src, size_t index, size_t count, tstr2& out)
		{
			typename Traits<tstr1>::citer s = BeginC(src) + index;
			Assign(s, s + count, 0, out, count);
		}
		template <typename tstr1, typename tstr2> inline void SubStr(tstr1 const& src, size_t index, size_t count, tstr2* out)
		{
			typename Traits<tstr1>::citer s = BeginC(src) + index;
			Assign(s, s + count, 0, out, count+1);
		}

		// Split a string at 'delims' outputting each sub string to 'out'
		template <typename tstr1, typename tchar, typename tout> inline void Split(tstr1 const& src, tchar const* delims, tout out)
		{
			size_t i = 0, j = 0, jend = Length(src);
			for (; j != jend; ++j)
			{
				if (*FindChar(delims, src[j]) == 0) continue;
				out(src, i, j);
				i = j + 1;
			}
			if (i != j)
				out(src, i, j);
		}

		// Trim characters from a string
		template <typename tstr, typename tpred> inline tstr& Trim(tstr& src, tpred pred, bool front, bool back)
		{
			typename Traits<tstr>::iter begin = Begin(src);
			typename Traits<tstr>::iter end   = End(src);
			typename Traits<tstr>::iter first = begin;
			typename Traits<tstr>::iter last  = end;
			if (front) first = FindFirst(first, last, Not<tpred>(pred));
			if (back)  last  = FindLast (first, last, Not<tpred>(pred));
			if (last != end) ++last;
			size_t len = static_cast<size_t>(last - first);
			for (; first != last; ++first, ++begin) { *begin = *first; }
			Resize(src, len);
			return src;
		}
		template <typename tstr, typename tpred> inline tstr  Trim(tstr const& src, tpred pred, bool front, bool back)
		{
			tstr s = src;
			return Trim(s, pred, front, back)
		}
		template <typename tstr, typename tchar> inline tstr& TrimChars(tstr& src, tchar const* chars, bool front, bool back)
		{
			return Trim(src, IsOneOf<tchar>(chars), front, back);
		}
		template <typename tstr, typename tchar> inline tstr  TrimChars(tstr const& src, tchar const* chars, bool front, bool back)
		{
			tstr s = src;
			return Trim(s, IsOneOf<tchar>(chars), front, back);
		}

		//// Character adaptors - doesn't work
		//template <typename titer> struct AdpLower
		//{
		//	titer& m_src;
		//	AdpLower(titer src) :m_src(src) {}
		//	AdpLower& operator = (char ch)  { *m_src = ch; return *this; }
		//	AdpLower& operator ++()         { ++m_src; return *this; }
		//	AdpLower& operator --()         { --m_src; return *this; }
		//	char      operator * () const   { return static_cast<char>(::tolower(*m_src)); }
		//private:
		//	AdpLower(AdpLower const&); // no copying
		//	AdpLower& operator = (AdpLower const&); // no copying
		//};
		//template <typename titer>           inline AdpLower<titer>  Lower(titer src)       { return AdpLower<titer>(src); }
		//template <typename tchar, size_t N> inline AdpLower<tchar*> Lower(tchar (&src)[N]) { return AdpLower<tchar*>(src); }
	}
}

#pragma warning (pop)

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_str_prstringcore)
		{
			using namespace pr;
			using namespace pr::str;

			{// Empty
				char            narr[] = "";
				std::wstring    wstr   = L"str";
				PR_CHECK(Empty(narr), true);
				PR_CHECK(Empty(wstr), false);
			}
			{//Length
				char            narr[] = "length7";
				wchar_t         wide[] = L"length7";
				std::string     cstr   = "length7";
				std::wstring    wstr   = L"length7";
				PR_CHECK(Length(narr), size_t(7));
				PR_CHECK(Length(wide), size_t(7));
				PR_CHECK(Length(cstr), size_t(7));
				PR_CHECK(Length(wstr), size_t(7));
			}
			{//Equal
				char            narr[] = "str";
				wchar_t         wide[] = L"str";
				std::string     cstr   = "str1";
				std::wstring    wstr   = L"str";
				PR_CHECK( Equal(narr, narr) &&  Equal(narr, wide) && !Equal(narr, cstr) &&  Equal(narr, wstr), true);
				PR_CHECK( Equal(wide, narr) &&  Equal(wide, wide) && !Equal(wide, cstr) &&  Equal(wide, wstr), true);
				PR_CHECK(!Equal(cstr, narr) && !Equal(cstr, wide) &&  Equal(cstr, cstr) && !Equal(cstr, wstr), true);
				PR_CHECK( Equal(wstr, narr) &&  Equal(wstr, wide) && !Equal(wstr, cstr) &&  Equal(wstr, wstr), true);
			}
			{//EqualI
				char            narr[] = "StR";
				wchar_t         wide[] = L"sTr";
				std::string     cstr   = "sTR";
				std::wstring    wstr   = L"STr";
				PR_CHECK(EqualI(narr, narr) && EqualI(narr, wide) && EqualI(narr, cstr) && EqualI(narr, wstr), true);
				PR_CHECK(EqualI(wide, narr) && EqualI(wide, wide) && EqualI(wide, cstr) && EqualI(wide, wstr), true);
				PR_CHECK(EqualI(cstr, narr) && EqualI(cstr, wide) && EqualI(cstr, cstr) && EqualI(cstr, wstr), true);
				PR_CHECK(EqualI(wstr, narr) && EqualI(wstr, wide) && EqualI(wstr, cstr) && EqualI(wstr, wstr), true);
			}
			{//EqualN
				char            narr[] =  "str0";
				wchar_t         wide[] = L"str1";
				std::string     cstr   =  "str2";
				std::wstring    wstr   = L"str3";
				PR_CHECK(EqualN(narr, narr, 3) &&  EqualN(narr, narr, 4) &&  EqualN(narr, narr, 5), true);
				PR_CHECK(EqualN(narr, wide, 3) && !EqualN(narr, wide, 4) && !EqualN(narr, wide, 5), true);
				PR_CHECK(EqualN(narr, cstr, 3) && !EqualN(narr, cstr, 4) && !EqualN(narr, cstr, 5), true);
				PR_CHECK(EqualN(narr, wstr, 3) && !EqualN(narr, wstr, 4) && !EqualN(narr, wstr, 5), true);
				PR_CHECK(EqualN(narr,  "str0") && !EqualN(narr,  "str"), true);
				PR_CHECK(EqualN(wide, L"str1") && !EqualN(wide, L"str"), true);
				PR_CHECK(EqualN(cstr,  "str2") && !EqualN(cstr,  "str"), true);
				PR_CHECK(EqualN(wstr, L"str3") && !EqualN(wstr, L"str"), true);
			}
			{//EqualNI
				char            narr[] = "sTr0";
				wchar_t         wide[] = L"Str1";
				std::string     cstr   = "stR2";
				std::wstring    wstr   = L"sTR3";
				PR_CHECK(EqualNI(narr, narr, 3) &&  EqualNI(narr, narr, 4) &&  EqualNI(narr, narr, 5), true);
				PR_CHECK(EqualNI(narr, wide, 3) && !EqualNI(narr, wide, 4) && !EqualNI(narr, wide, 5), true);
				PR_CHECK(EqualNI(narr, cstr, 3) && !EqualNI(narr, cstr, 4) && !EqualNI(narr, cstr, 5), true);
				PR_CHECK(EqualNI(narr, wstr, 3) && !EqualNI(narr, wstr, 4) && !EqualNI(narr, wstr, 5), true);
				PR_CHECK(EqualNI(narr,  "str0") && !EqualNI(narr,  "str"), true);
				PR_CHECK(EqualNI(wide, L"str1") && !EqualNI(wide, L"str"), true);
				PR_CHECK(EqualNI(cstr,  "str2") && !EqualNI(cstr,  "str"), true);
				PR_CHECK(EqualNI(wstr, L"str3") && !EqualNI(wstr, L"str"), true);
			}
			{//Resize
				char            narr[4] = {'a','a','a','a'};
				wchar_t         wide[4] = {L'a',L'a',L'a',L'a'};
				std::string     cstr    = "aaaa";
				std::wstring    wstr    = L"aaaa";
				Resize(narr, 2); Resize(narr, 3, 'b'); PR_CHECK(Equal(narr, "aab"), true);
				Resize(wide, 2); Resize(wide, 3, 'b'); PR_CHECK(Equal(wide, "aab"), true);
				Resize(cstr, 2); Resize(cstr, 3, 'b'); PR_CHECK(Equal(cstr, "aab"), true);
				Resize(wstr, 2); Resize(wstr, 3, 'b'); PR_CHECK(Equal(wstr, "aab"), true);
			}
			{//Assign
				char*           src0 = "str";
				std::string     src1 = "str";
				char            narr[4];
				wchar_t         wide[4];
				std::string     cstr;
				std::wstring    wstr;
				Assign(src0, src0 + 3, 0, narr, 4);             PR_CHECK(Equal(narr, "str"), true);
				Assign(src1.begin(), src1.end(), 0, wide, 4);   PR_CHECK(Equal(wide, "str"), true);
				Assign(src0, src0 + 3, 0, cstr);                PR_CHECK(Equal(cstr, "str"), true);
				Assign(src0, src0 + 3, 0, wstr);                PR_CHECK(Equal(wstr, "str"), true);
			}
			{//FindChar
				std::string src = "str";
				wchar_t ch = L't';
				PR_CHECK(*FindChar(src.begin(), src.end(), ch), 't');
				PR_CHECK(*FindChar(src.c_str(), ch)           , 't');
			}
			{//FindStr
				char src[] = "string";
				PR_CHECK(FindStr(src, src+sizeof(src), "in") , &src[3]);
				PR_CHECK(FindStr(src, "in")                  , &src[3]);
			}
			{//FindFirst
				char         narr[] = "AaBbAaBb";
				wchar_t      wide[] = L"AaBbAaBb";
				std::string  cstr   = "AaBbAaBb";
				std::wstring wstr   = L"AaBbAaBb";
				PR_CHECK(Equal(FindFirst(narr, IsOneOf<>("bB")), "BbAaBb"), true);
				PR_CHECK(Equal(FindFirst(wide, NotOneOf<>("AaB")), "bAaBb"), true);
				PR_CHECK(*FindFirst(narr, IsOneOf<>("c")) == 0, true);
				PR_CHECK(FindFirst(cstr.begin(), cstr.end(), IsOneOf<>("b")) - cstr.begin()   , 3);
				PR_CHECK(FindFirst(wstr.begin(), wstr.end(), NotOneOf<>("Aab")) - wstr.begin(), 2);
				PR_CHECK(wstr.end() == FindFirst(wstr.begin(), wstr.end(), NotOneOf<>("AabB")), true);
			}
			{//FindLast
				char         narr[] = "AaBbAaBb";
				wchar_t      wide[] = L"AaBbAaBb";
				std::string  cstr   = "AaBbAaBb";
				std::wstring wstr   = L"AaBbAaBb";
				PR_CHECK(Equal(FindLast(narr, IsOneOf<>("bB")), "b"), true);
				PR_CHECK(Equal(FindLast(wide, NotOneOf<>("ABb")), "aBb"), true);
				PR_CHECK(*FindLast(narr, IsOneOf<>("c")) == 0, true);
				PR_CHECK(FindLast(cstr.begin(), cstr.end(), IsOneOf<>("B")) - cstr.begin()    ,6);
				PR_CHECK(FindLast(wstr.begin(), wstr.end(), NotOneOf<>("Bab")) - wstr.begin() ,4);
				PR_CHECK(wstr.end() == FindLast(wstr.begin(), wstr.end(), NotOneOf<>("AabB")), true);
			}
			{//FindFirstOf
				char         narr[] = "AaAaAa";
				wchar_t      wide[] = L"AaAaAa";
				std::string  cstr   = "AaAaAa";
				std::wstring wstr   = L"AaAaAa";
				PR_CHECK(Equal(FindFirstOf(narr, "A"), "AaAaAa"), true);
				PR_CHECK(Equal(FindFirstOf(wide, "a"), "aAaAa"), true);
				PR_CHECK(*FindFirstOf(wide, "B") == 0, true);
				PR_CHECK(FindFirstOf(cstr.begin(), cstr.end(), "A") - cstr.begin(), 0);
				PR_CHECK(FindFirstOf(wstr.begin(), wstr.end(), "a") - wstr.begin(), 1);
				PR_CHECK(wstr.end() == FindFirstOf(wstr.begin(), wstr.end(), "B"), true);
			}
			{//FindLastOf
				char         narr[] = "AaAaAa";
				wchar_t      wide[] = L"AaAaa";
				std::string  cstr   = "AaAaaa";
				std::wstring wstr   = L"Aaaaa";
				PR_CHECK(Equal(FindLastOf(narr, L"A"), "Aa"), true);
				PR_CHECK(Equal(FindLastOf(wide, L"A"), "Aaa"), true);
				PR_CHECK(*FindLastOf(wide, L"B") == 0, true);
				PR_CHECK(FindLastOf(cstr.begin(), cstr.end(), "A") - cstr.begin(), 2);
				PR_CHECK(FindLastOf(wstr.begin(), wstr.end(), "A") - wstr.begin(), 0);
				PR_CHECK(wstr.end() == FindLastOf(wstr.begin(), wstr.end(), "B"), true);
			}
			{//FindFirstNotOf
				char         narr[] = "junk_str_junk";
				wchar_t      wide[] = L"junk_str_junk";
				std::string  cstr   = "junk_str_junk";
				std::wstring wstr   = L"junk_str_junk";
				PR_CHECK(Equal(FindFirstNotOf(narr, "_knuj"), "str_junk"), true);
				PR_CHECK(Equal(FindFirstNotOf(wide, "_knuj"), "str_junk"), true);
				PR_CHECK(*FindFirstNotOf(wide, "_knujstr") == 0, true);
				PR_CHECK(FindFirstNotOf(cstr.begin(), cstr.end(), "_knuj") - cstr.begin(), 5);
				PR_CHECK(FindFirstNotOf(wstr.begin(), wstr.end(), "_knuj") - wstr.begin(), 5);
				PR_CHECK(wstr.end() == FindFirstNotOf(wstr.begin(), wstr.end(), "_knujstr"), true);
			}
			{//FindLastNotOf
				char         narr[] = "junk_str_junk";
				wchar_t      wide[] = L"junk_str_junk";
				std::string  cstr   = "junk_str_junk";
				std::wstring wstr   = L"junk_str_junk";
				PR_CHECK(Equal(FindLastNotOf(narr, "_knuj"), "r_junk"), true);
				PR_CHECK(Equal(FindLastNotOf(wide, "_knuj"), "r_junk"), true);
				PR_CHECK(*FindLastNotOf(wide, "_knujstr") == 0, true);
				PR_CHECK(FindLastNotOf(cstr.begin(), cstr.end(), "_knuj") - cstr.begin() ,7);
				PR_CHECK(FindLastNotOf(wstr.begin(), wstr.end(), "_knuj") - wstr.begin() ,7);
				PR_CHECK(wstr.end() == FindLastNotOf(wstr.begin(), wstr.end(), "_knujstr"), true);
			}
			{//UpperCase
				wchar_t src0[] = L"caSe";
				std::string dest0;
				PR_CHECK(Equal(UpperCase(src0, dest0), L"CASE"), true);
				PR_CHECK(Equal(UpperCase(src0), L"CASE"), true);

				wchar_t src1[] = L"caSe";
				wchar_t dest1[5];
				PR_CHECK(Equal(UpperCase(src1, dest1, 5), L"CASE"), true);
				PR_CHECK(Equal(UpperCase(src1), L"CASE"), true);
			}
			{//LowerCase
				wchar_t src0[] = L"caSe";
				std::string dest0;
				PR_CHECK(Equal(LowerCase(src0, dest0), L"case"), true);
				PR_CHECK(Equal(LowerCase(src0), L"case"), true);

				wchar_t src1[] = L"caSe";
				wchar_t dest1[5];
				PR_CHECK(Equal(LowerCase(src1, dest1, 5), L"case"), true);
				PR_CHECK(Equal(LowerCase(src1), L"case"), true);
			}
			{//SubStr
				char    narr[] = "SubstringExtract";
				wchar_t wide[] = L"SubstringExtract";

				std::string out0;
				SubStr(narr, 3, 6, out0);
				PR_CHECK(Equal(out0, "string"), true);

				char out1[7];
				SubStr(wide, 3, 6, out1);
				PR_CHECK(Equal(out1, "string"), true);
			}
			{//Split
				typedef std::vector<std::string> StrVec;
				char str[] = "1,,2,3,4";
				char res[][2] = {"1","","2","3","4"};

				StrVec buf;
				pr::str::Split(str, ",", [&](char const* s, size_t i, size_t iend){ buf.push_back(std::string(s+i, s+iend)); });
				for (StrVec::const_iterator i = buf.begin(), iend = buf.end(); i != iend; ++i)
					PR_CHECK(Equal(*i, res[i - buf.begin()]), true);
			}
			{//Trim
				char         narr[] = " \t,1234\n";
				wchar_t      wide[] = L" \t,1234\n";
				std::string  cstr   = " \t,1234\n";
				std::wstring wstr   = L" \t,1234\n";
				PR_CHECK(Equal(Trim(narr, IsWhiteSpace<char>    ,true, true), ",1234"), true);
				PR_CHECK(Equal(Trim(wide, IsWhiteSpace<wchar_t> ,true, true), L",1234"), true);
				PR_CHECK(Equal(Trim(cstr, IsWhiteSpace<char>    ,true, false), ",1234\n"), true);
				PR_CHECK(Equal(Trim(wstr, IsWhiteSpace<wchar_t> ,false, true), " \t,1234"), true);
			}
			{//TrimChars
				char         narr[] = " \t,1234\n";
				wchar_t      wide[] = L" \t,1234\n";
				std::string  cstr   = " \t,1234\n";
				std::wstring wstr   = L" \t,1234\n";
				PR_CHECK(Equal(TrimChars(narr,  " \t,\n" ,true  ,true),  "1234"), true);
				PR_CHECK(Equal(TrimChars(wide, L" \t,\n" ,true  ,true), L"1234"), true);
				PR_CHECK(Equal(TrimChars(cstr,  " \t,\n" ,true  ,false),  "1234\n"), true);
				PR_CHECK(Equal(TrimChars(wstr, L" \t,\n" ,false ,true), L" \t,1234"), true);
			}
		}
	}
}
#endif

#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif

#endif
