//***********************************************************************
// Utility String functions
//  Copyright © Rylogic Ltd 2008
//***********************************************************************
	
#ifndef PR_STR_STRING_UTILITY_H
#define PR_STR_STRING_UTILITY_H
	
#include "pr/common/crc.h"
#include "pr/str/prstringcore.h"
	
namespace pr
{
	namespace str
	{
		// Ensure 'str' has a newline character at its end
		template <typename tstr> inline tstr& EnsureNewline(tstr& str)
		{
			if( !Empty(str) && *(--End(str)) != '\n' ) str.push_back('\n');
			return str;
		}
		template <typename tstr> inline tstr EnsureNewline(tstr const& str)
		{
			tstr s = str;
			return EnsureNewline(s);
		}
		
		// Return true if 'src' contains 'what'
		template <typename tstr1, typename tstr2> inline bool Contains(tstr1 const& src, tstr2 const& what)
		{
			return FindStr(Begin(src), End(src), what) != End(src);
		}
		template <typename tstr1, typename tstr2> inline bool ContainsNoCase(tstr1 const& src, tstr2 const& what)
		{
			return FindStrNoCase(Begin(src), End(src), what) != End(src);
		}
		
		struct Pred_Compare       { template <typename L, typename R> int  operator() (L lhs, R rhs) const { return (lhs > rhs) - (rhs > lhs); } };
		struct Pred_CompareNoCase { template <typename L, typename R> int  operator() (L lhs, R rhs) const { lhs = static_cast<L>(tolower(lhs)); rhs = static_cast<R>(tolower(rhs)); return (lhs > rhs) - (rhs > lhs); } };
		
		// Return the lexicographical comparison of two strings.
		// Returns 0 if equal, -1 if lhs < rhs, or +1 if lhs > rhs
		template <typename tstr1, typename tstr2, typename Pred> inline int Compare(tstr1 const& lhs, tstr2 const& rhs, Pred pred)
		{
			typename Traits<tstr1>::citer l = Begin(lhs), l_end = End(lhs);
			typename Traits<tstr2>::citer r = Begin(rhs), r_end = End(rhs);
			for (; l != l_end && r != r_end; ++l, ++r)
			{
				int comp = pred(*l, *r);
				if (comp < 0) { l = l_end; break; }
				if (comp > 0) { r = r_end; break; }
			}
			return (r == r_end) - (l == l_end);
		}
		template <typename tstr1, typename tstr2> inline int Compare(tstr1 const& lhs, tstr2 const& rhs)
		{
			return Compare(lhs, rhs, Pred_Compare());
		}
		template <typename tstr1, typename tstr2> inline int CompareNoCase(tstr1 const& lhs, tstr2 const& rhs)
		{
			return Compare(lhs, rhs, Pred_CompareNoCase());
		}
		
		// Return the number of occurances of 'what' in 'str'
		template <typename tstr1, typename tstr2> size_t Count(tstr1 const& str, tstr2 const& what)
		{
			typedef typename Traits<tstr1>::citer citer;
			size_t count = 0;
			size_t what_len = Length(what);
			citer str_end = EndC(str);
			citer pos = FindStr(BeginC(str), str_end, what);
			while( pos != str_end )
			{
				++count;
				pos = FindStr(pos + what_len, str_end, what);
			}
			return count;
		}
		
		// Replace blocks of delimiter characters with a single delimiter
		// The first delimiter of the block is kept unless a '\n' is found
		// in which case the '\n' is used as the single delimiter.
		template <typename tstr1, typename tstr2, typename tchar> void CompressWhiteSpace(tstr1& src, tstr2* delim, tchar ws_char, bool preserve_newlines)
		{
			typedef typename Traits<tstr1>::value_type tchar;
			if (Empty(src)) return;
			
			tchar* in = &src[0], *out = in;
			in = FindFirstNotOf(in, delim);
			while( *in )
			{
				bool return_found = false;
				for (; *in && *FindChar(delim, *in) == 0; ++in, ++out) { *out = *in; }
				for (; *in && *FindChar(delim, *in) != 0; ++in)        { return_found |= *in == '\n'; }
				if (*in) *out++ = (preserve_newlines && return_found) ? '\n' : ws_char;
			}
			Resize(src, static_cast<size_t>(out - &src[0]));
		}
		template <typename tstr, typename tchar> inline void CompressWhiteSpace(tstr& src, tchar ws_char, bool preserve_newlines)
		{
			typedef typename Traits<tstr>::value_type value_type;
			return CompressWhiteSpace(src, Delim(static_cast<value_type const*>(0)), ws_char, preserve_newlines);
		}
		
		// Convert a string into an array of tokens
		template <typename tstr1, typename tstr_cont, typename tstr2> void Tokenise(tstr1 const& src, tstr_cont& tokens, tstr2 const* delim)
		{
			typedef typename Traits<tstr1>::citer citer;
			typedef typename tstr_cont::value_type tstr3;
			for (citer i = Begin(src), i_end = End(src); i != i_end; ++i)
			{
				if (*i == '"') // Extract whole strings
				{
					tstr3 tok; tok.reserve(256);
					for (++i; i != i_end && *i != '"'; ++i) { tok.push_back(*i); }
					tokens.push_back(tok);
					if (i == i_end) return;
				}
				else if (*FindChar(delim, *i) == 0)
				{
					tstr3 tok; tok.reserve(256);
					for (; i != i_end && *FindChar(delim, *i) == 0; ++i) { tok.push_back(*i); }
					tokens.push_back(tok);
					if (i == i_end) return;
				}
			}
		}
		template <typename tstr, typename tstr_cont> inline void Tokenise(tstr const& src, tstr_cont& tokens)
		{
			typedef typename Traits<tstr>::value_type value_type;
			return Tokenise(src, tokens, Delim(static_cast<value_type const*>(0)));
		}
		
		// Strip sections from a string
		// Pass null to 'block_start','block_end' or 'line' to ignore that section type
		template <typename tstr1, typename tstr2> tstr1& Strip(tstr1& src, tstr2* block_start, tstr2* block_end, tstr2* line)
		{
			typedef typename Traits<tstr1>::value_type tchar;
			if (Empty(src)) return src;
			
			bool block = block_start != 0 && block_end != 0;
			size_t lc_len  = line  ? Length(line)        : 0;
			size_t bcs_len = block ? Length(block_start) : 0;
			size_t bce_len = block ? Length(block_end)   : 0;
			tchar* out = &src[0];
			for (tchar const *s = &src[0]; *s;)
			{
				if (line && EqualN(line, s, lc_len))
				{
					while( *s && *s != '\n' && *s != '\r' ) {++s;}
					while( *s && (*s == '\n'||*s == '\r') ) {++s;}
				}
				else if( block && EqualN(block_start, s, bcs_len) )
				{
					while( *s && !EqualN(block_end, s, bce_len) )	{++s;}
					s += bce_len;
				}
				else
				{
					*out++ = *s++;
				}
			}
			Resize(src, static_cast<size_t>(out - &src[0]));
			return src;
		}
		
		// Strip C++ style comments from a string
		template <typename tstr> inline tstr& StripCppComments(tstr& src)
		{
			return Strip(src, "/*", "*/", "//");
		}
		
		// Replace instances of 'what' with 'with'
		template <typename tstr1, typename tstr2, typename tstr3> size_t Replace(tstr1& src, tstr2 const& what, tstr3 const& with)
		{
			typedef typename Traits<tstr1>::value_type tchar;
			if (Empty(src)) return 0;
			size_t src_len  = Length(src);
			size_t what_len = Length(what);
			size_t with_len = Length(with);
			size_t count = 0;
			if (what_len >= with_len)
			{
				tchar* out = &src[0];
				for (tchar const* s = &src[0]; *s;)
				{
					if (!EqualN(s, what, what_len)) { *out++ = *s++; continue; }
					for (size_t i = 0; i != with_len; ++i) { out[i] = with[i]; }
					out += with_len;
					s += what_len;
					++count;
				}
				
				size_t new_size = src_len - what_len*count + with_len*count;
				Resize(src, new_size);
			}
			else
			{
				count = Count(src, what);
				size_t new_size = src_len - what_len*count + with_len*count;
				Resize(src, new_size);
				
				tchar* out = &src[0] + new_size;
				for (tchar const* s = &src[0] + src_len; s != out;)
				{
					if (!EqualN(out, what, what_len)) { *(--out) = *(--s); continue; }
					out -= with_len - what_len;
					for (size_t i = 0; i != with_len; ++i) { out[i] = with[i]; }
				}
			}
			return count;
		}
		template <typename tstr1, typename tstr2, typename tstr3> size_t Replace(tstr1 const& src, tstr1& dst, tstr2 const& what, tstr3 const& with)
		{
			dst = src;
			return Replace(dst, what, with);
		}

		// Hash the contents of a string using crc32
		template <typename tstr> inline size_t Hash(tstr const& src, size_t initial_crc = size_t(~0))
		{
			if (Empty(src)) return initial_crc;
			void const* data = reinterpret_cast<void const*>(&src[0]);
			size_t size = Length(src) * sizeof(Traits<tstr>::value_type);
			return Crc(data, size);
		}
		
		// Convert a normal string into a C-style string
		// This is the std::string -esk version. char* version not implemented yet...
		template <typename tstr2, typename tstr1> inline tstr2 StringToCString(tstr1 const& src)
		{
			typedef typename Traits<tstr1>::value_type tchar;
			if (Empty(src)) return src;
			tstr2 dst;
			for (tchar const* s = &src[0]; *s; ++s)
			{
				switch (*s)
				{
				default:   dst.push_back(*s); break;
				case '\a': dst.push_back('\\'); dst.push_back('a'); break;
				case '\b': dst.push_back('\\'); dst.push_back('b'); break;
				case '\f': dst.push_back('\\'); dst.push_back('f'); break;
				case '\n': dst.push_back('\\'); dst.push_back('n'); break;
				case '\r': dst.push_back('\\'); dst.push_back('r'); break;
				case '\t': dst.push_back('\\'); dst.push_back('t'); break;
				case '\v': dst.push_back('\\'); dst.push_back('v'); break;
				case '\\': dst.push_back('\\'); dst.push_back('\\'); break;
				case '\?': dst.push_back('\\'); dst.push_back('?');  break;
				case '\'': dst.push_back('\\'); dst.push_back('\''); break;
				case '\"': dst.push_back('\\'); dst.push_back('"');  break;
				}
			}
			return dst;
		}
		
		// Convert a C-style string into a normal string
		// This is the std::string -esk version. char* version not implemented yet...
		template <typename tstr2, typename tstr1> inline tstr2 CStringToString(tstr1 const& src)
		{
			typedef typename Traits<tstr1>::value_type tchar;
			if (Empty(src)) return src;
			
			tstr1 dst;
			for (tchar const* s = &src[0]; *s; ++s)
			{
				if (*s == '\\')
				{
					switch (*(++s))
					{
					default: break; // might be '0'
					case 'a':  dst.push_back('\a'); break;
					case 'b':  dst.push_back('\b'); break;
					case 'f':  dst.push_back('\f'); break;
					case 'n':  dst.push_back('\n'); break;
					case 'r':  dst.push_back('\r'); break;
					case 't':  dst.push_back('\t'); break;
					case 'v':  dst.push_back('\v'); break;
					case '\\': dst.push_back('\\'); break;
					case '?':  dst.push_back('\?'); break;
					case '\'': dst.push_back('\''); break;
					case '"':  dst.push_back('\"'); break;
					}
				}
				else
				{
					dst.push_back(*s);
				}
			}
			return dst;
		}
		
		// Look for 'identifier' within the range [ofs, ofs+count) of 'src'
		// returning the index of it's position or ofs+count if not found.
		// identifier will be a complete identifier based on the char class IsIdentifier()
		template <typename tstr1, typename tstr2> inline size_t FindIdentifier(tstr1 const& src, tstr2 const& identifier, size_t ofs, size_t count)
		{
			Traits<tstr1>::citer begin = BeginC(src);
			Traits<tstr1>::citer iter  = begin + ofs;
			Traits<tstr1>::citer end   = iter + count;
			size_t id_len = Length(identifier);
			for(;;++iter)
			{
				iter = FindStr(iter, end, identifier);
				if (iter == end) return ofs + count;
				
				// Look for any identifier characters after iter + id_len
				Traits<tstr1>::citer j = iter + id_len;
				if (j != end && IsIdentifier(*j, false)) continue;
				
				// Look for any identifier characters before iter
				for (j = iter; j > begin && IsIdentifier(*(--j), false);) {}
				for (        ; j != iter && !IsIdentifier(*j, true); ++j) {}
				if  (j != iter) continue;
				
				return static_cast<size_t>(iter - begin);
			}
		}
		template <typename tstr1, typename tstr2> inline size_t FindIdentifier(tstr1 const& src, tstr2 const& identifier, size_t ofs)
		{
			return FindIdentifier(src, identifier, ofs, Length(src) - ofs);
		}
		template <typename tstr1, typename tstr2> inline size_t FindIdentifier(tstr1 const& src, tstr2 const& identifier)
		{
			return FindIdentifier(src, identifier, 0);
		}
		
		// Add/Remove quotes from a string if it doesn't already have them
		template <typename tstr> inline tstr& Quotes(tstr& str, bool add)
		{
			typedef Traits<tstr>::value_type tchar;

			size_t i,len = Length(str);
			Traits<tstr>::citer begin = BeginC(str);
			if ( add && (len >= 2 && *begin == tchar('\"') && *(begin + len - 1) == tchar('\"'))) return str; // already quoted
			if (!add && (len <  2 || *begin != tchar('\"') || *(begin + len - 1) != tchar('\"'))) return str; // already not quoted
			if (add)
			{
				Resize(str, len+2);
				str[len+1] = tchar('\"');
				for (i = len; i-- != 0;) str[i+1] = str[i];
				str[0] = tchar('\"');
			}
			else
			{
				for (i = 0; i != len-1; ++i) str[i] = str[i+1];
				Resize(str, len-2);
			}
			return str;
		}
		template <typename tstr> inline tstr  Quotes(tstr const& str, bool add)
		{
			tstr s = str;
			return Quotes(s, add);
		}
	}
}
	
#endif
