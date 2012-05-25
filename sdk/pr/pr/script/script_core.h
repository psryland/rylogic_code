//**********************************
// Script charactor source
//  Copyright © Rylogic Ltd 2007
//**********************************
#ifndef PR_SCRIPT_SCRIPT_CORE_H
#define PR_SCRIPT_SCRIPT_CORE_H

#include <string>
#include <vector>
#include <deque>
#include <stdarg.h>
#include <exception>
#include "pr/common/assert.h"
#include "pr/common/array.h"
#include "pr/common/stack.h"
#include "pr/common/queue.h"
#include "pr/common/hash.h"
#include "pr/common/fmt.h"
#include "pr/common/expr_eval.h"
#include "pr/common/exception.h"
#include "pr/str/prstring.h"
#include "pr/script/keywords.h"

#pragma warning (push)
#pragma warning (disable: 4351) // warning C4351: new behavior: elements of array will be default initialized

namespace pr
{
	namespace script
	{
		// The string type to use.
		// 'pr::string<>' uses the short string optimisation
		typedef pr::string<char, 32> string;
		
		// Use the stl for wide strings
		typedef std::wstring wstring;
		
		typedef unsigned int uint;
		typedef long long int64;
		typedef unsigned long long uint64;
		
		inline int const*  As32(int64 const& val) { return reinterpret_cast<int const*>(&val); }
		inline int*        As32(int64&       val) { return reinterpret_cast<int*>      (&val); }
		
		// A location within a source file
		struct Loc
		{
			string m_file;
			size_t m_line, m_col;
			Loc() :m_file() ,m_line() ,m_col() {}
			Loc(string file, size_t line, size_t col) :m_file(file) ,m_line(line) ,m_col(col) {}
			char inc(char ch) { if (ch == '\n') { ++m_line; m_col = 0; } else if (ch != '\0') { ++m_col; } return ch; }
		};
		inline bool operator == (Loc const& lhs, Loc const& rhs) { return lhs.m_file == rhs.m_file && lhs.m_line == rhs.m_line && lhs.m_col == rhs.m_col; }
		inline bool operator != (Loc const& lhs, Loc const& rhs) { return !(lhs == rhs); }
		inline bool operator <  (Loc const& lhs, Loc const& rhs)
		{
			if (lhs.m_file != rhs.m_file) return lhs.m_file < rhs.m_file;
			if (lhs.m_line != rhs.m_line) return lhs.m_line < rhs.m_line;
			return lhs.m_col <= rhs.m_col;
		}
		
		// An 8-character buffer for doing short string matches
		struct Buf8
		{
			union { uint64 m_ui; char m_ch[8]; };
			int m_len;
			
			Buf8() :m_ch() ,m_len(0) {}
			explicit Buf8(char const* src, int n = -1) :m_ch() ,m_len(0) { for (;*src && n--; ++src) push_back(*src); }
			template <typename Iter> explicit Buf8(Iter& src, int n = -1) :m_ch() ,m_len(0) { for (;*src && n--; ++src) push_back(*src); }
			bool empty() const             { return m_len == 0; }
			int  max_size() const          { return 8; }
			int  size() const              { return m_len; }
			void clear()                   { m_ui = 0; m_len = 0; }
			char* begin()                  { return m_ch; }
			char* end()                    { return m_ch + m_len; }
			void shift(char ch)            { PR_ASSERT(PR_DBG, m_len > 0, ""); m_ui >>= 8; m_ch[m_len-1] = ch; }
			char front() const             { PR_ASSERT(PR_DBG, m_len > 0, ""); return m_ch[0]; }
			char back() const              { PR_ASSERT(PR_DBG, m_len > 0, ""); return m_ch[m_len-1]; }
			void push_back(char ch)        { PR_ASSERT(PR_DBG, m_len < 8, ""); m_ch[m_len++] = ch; }
			void pop_front()               { PR_ASSERT(PR_DBG, m_len > 0, ""); m_ui >>= 8; --m_len; }
			char  operator [](int i) const { PR_ASSERT(PR_DBG, i < m_len, ""); return m_ch[i]; }
			char& operator [](int i)       { PR_ASSERT(PR_DBG, i < m_len, ""); return m_ch[i]; }
			
			// This returns true if 'buf' _contains_ 'this'. i.e. buf1.match(buf2) != buf2.match(buf1) generally
			//bool match(Buf8 const& buf) const { return m_ui != 0 && (m_ui & buf.m_ui) == m_ui; }
		};
		inline bool operator == (Buf8 const& lhs, Buf8 const& rhs) { return lhs.m_ui == rhs.m_ui; }
		inline bool operator != (Buf8 const& lhs, Buf8 const& rhs) { return lhs.m_ui != rhs.m_ui; }
		
		// Text consumer helpers
		struct Eat
		{
			template <typename Ptr> static void NewLines(Ptr& ptr)          { for (; pr::str::IsNewLine(*ptr); ++ptr) {} }
			template <typename Ptr> static void WhiteSpace(Ptr& ptr)        { for (; pr::str::IsWhiteSpace(*ptr); ++ptr) {} }
			template <typename Ptr> static void LineSpace(Ptr& ptr)         { for (; *ptr &&  pr::str::IsLineSpace(*ptr); ++ptr) {} }
			template <typename Ptr> static void Line(Ptr& ptr, bool eat_cr) { for (; *ptr && !pr::str::IsNewLine(*ptr); ++ptr) {} if (eat_cr && *ptr) ++ptr; }
			template <typename Ptr> static void LiteralString(Ptr& ptr)     { for (bool esc = true; *ptr && (*ptr != '\"' || esc); esc = (*ptr=='\\'), ++ptr) {} if (*ptr) ++ptr; }
			template <typename Ptr> static void LiteralChar(Ptr& ptr)       { for (bool esc = true; *ptr && (*ptr != '\'' || esc); esc = (*ptr=='\\'), ++ptr) {} if (*ptr) ++ptr; }
			template <typename Ptr> static void BlockComment(Ptr& ptr)      { for (char prev = 0; *ptr && !(prev == '*' && *ptr == '/'); prev = *ptr, ++ptr) {} if (*ptr) ++ptr; }
			template <typename Ptr> static void LineComment(Ptr& ptr)       { Line(ptr, false); }
		};
		
		// Hash helpers
		struct Hash
		{
			// Terminator that returns true when 'ch' isn't a valid identifier charactor
			struct NotIdChar
			{
				mutable uint i; NotIdChar() :i(0) {}
				bool operator()(char ch) const { return !pr::str::IsIdentifier(ch, 0==i++); }
			};
			
			// Generate the hash of the contents of a buffer
			template <typename TBuf> static pr::hash::HashValue Buffer(TBuf const& buf)
			{
				return pr::hash::HashC(buf.begin(), buf.end());
			}
			
			// Generate the hash of a string
			static pr::hash::HashValue String(char const* str)
			{
				return pr::hash::HashC(str);
			}
			template <typename Str> static pr::hash::HashValue String(Str const& str)
			{
				return String(str.c_str());
			}
		};
		
		// Stick a formatted string in place of a normal string
		// Use this instead of pr::FmtS() because its thread safe
		inline string fmt(const char* format, ...)
		{
			char str[1024] = {};
			va_list args;
			va_start(args, format);
			if (_vsprintf_p(str, 1023, format, args) == -1) { PR_ASSERT(PR_DBG, false, "String truncated"); }
			va_end(args);
			return str;
		}
		
		// Convert a result with details message and location into a string
		inline string ErrMsg(pr::script::EResult::Type result, char const* details, Loc const& loc)
		{
			return fmt(
					"Error:\n"
					"  Error Code: %s\n"
					"  Message: %s\n"
					"  Source: %s\n"
					"  Line: %d\n"
					"  Column: %d\n"
					,pr::script::ToString(result)
					,details
					,loc.m_file.c_str()
					,loc.m_line+1
					,loc.m_col+1);
		}
		
		// Script exceptions
		struct Exception :pr::Exception<pr::script::EResult::Type>
		{
			Loc m_loc;
			virtual Loc const& loc() const { return m_loc; }
			string msg() const { return ErrMsg(code(), what(), loc()); }
			Exception() :pr::Exception<EResult::Type>() ,m_loc() {}
			Exception(EResult::Type result, Loc const& loc, char const* msg) :pr::Exception<EResult::Type>(result, msg) ,m_loc(loc) {}
			Exception(EResult::Type result, Loc const& loc, string const& msg) :pr::Exception<EResult::Type>(result, msg.c_str()) ,m_loc(loc) {}
		};
		
		// Debugging function to check the keyword hashcodes
		inline bool ValidateKeywordHashcodes()
		{
#			define PR_SCRIPT_KEYWORD(name,text,hashcode)    PR_ASSERT(PR_DBG, Hash::String(text) == hashcode, fmt("Hash value for "#name" is incorrect. Should be: 0x%08x\n", Hash::String(text)).c_str());
#			define PR_SCRIPT_PP_KEYWORD(name,text,hashcode) PR_ASSERT(PR_DBG, Hash::String(text) == hashcode, fmt("Hash value for "#name" is incorrect. Should be: 0x%08x\n", Hash::String(text)).c_str());
#			include "pr/script/keywords.h"
			return true;
		}
	}
}

#pragma warning (pop)

#endif
