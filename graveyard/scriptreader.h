//*****************************************
// ScriptReader
//	(c)opyright Paul Ryland 2009
//*****************************************
// Example:
//	#define{Macro}{value}
//	#include "include_file"
//	*Keyword
//	{// Section begin
//		// Line comment
//		/* Block comment */
//		#eval{1+2}
//		#def{Macro}
//	}// Section end
//	#undef{Macro}
//	#ifdef{Macro}
//	#elif{Macro}
//	#else
//	#endif
//	#lit anything #endlit

// Script keyword declarations
#ifndef PR_SCRIPT_RESULT
#define PR_SCRIPT_RESULT(name, value)
#endif
PR_SCRIPT_RESULT(Success                         ,= 1)
PR_SCRIPT_RESULT(Failed                          ,= 0x80000000)
PR_SCRIPT_RESULT(SectionStartNotFound            ,)
PR_SCRIPT_RESULT(SectionEndNotFound              ,)
PR_SCRIPT_RESULT(IncompleteString                ,)
PR_SCRIPT_RESULT(DefSymbolNotDefined             ,)
PR_SCRIPT_RESULT(UnknownPreprocessorCommand      ,)
PR_SCRIPT_RESULT(UnmatchedPreprocessorCommand    ,)
PR_SCRIPT_RESULT(EvalSyntaxError                 ,)
PR_SCRIPT_RESULT(StringNotFound                  ,)
PR_SCRIPT_RESULT(IncludeFileMissing              ,)
PR_SCRIPT_RESULT(FailedToLoadFile                ,)
PR_SCRIPT_RESULT(FailedToReadFile                ,)
PR_SCRIPT_RESULT(UnknownKeyword                  ,)
PR_SCRIPT_RESULT(UnknownValue                    ,)
PR_SCRIPT_RESULT(InvalidValue                    ,)
PR_SCRIPT_RESULT(InvalidLuaCode                  ,)
PR_SCRIPT_RESULT(UserErrorCode                   ,= 0x81000000)
#undef PR_SCRIPT_RESULT

#ifndef PR_SCRIPT_TOKEN
#define PR_SCRIPT_TOKEN(name, string_name)
#endif
PR_SCRIPT_TOKEN(Unknown                ,"unknown"               )
PR_SCRIPT_TOKEN(Keyword                ,"keyword"               )
PR_SCRIPT_TOKEN(PreprocessorCommand    ,"preprocessor command"  )
PR_SCRIPT_TOKEN(SectionStart           ,"section start"         )
PR_SCRIPT_TOKEN(SectionEnd             ,"section end"           )
PR_SCRIPT_TOKEN(Section                ,"section"               )
PR_SCRIPT_TOKEN(NewLine                ,"new line"              )
PR_SCRIPT_TOKEN(Value                  ,"value"                 )
PR_SCRIPT_TOKEN(EndOfStream            ,"end of stream"         )
PR_SCRIPT_TOKEN(Identifier             ,"identifier"            )
PR_SCRIPT_TOKEN(String                 ,"string"                )
PR_SCRIPT_TOKEN(Bool                   ,"boolean"               )
PR_SCRIPT_TOKEN(Integral               ,"integer"               )
PR_SCRIPT_TOKEN(Real                   ,"real"                  )
PR_SCRIPT_TOKEN(Plus                   ,"plus sign"             )
PR_SCRIPT_TOKEN(Minus                  ,"minus sign"            )
#undef PR_SCRIPT_TOKEN

#ifndef PR_SCRIPT_PPKEYWORD
#define PR_SCRIPT_PPKEYWORD(name, text, hash_value)
#endif
PR_SCRIPT_PPKEYWORD(Define             ,"define"               ,0x22A5C100)
PR_SCRIPT_PPKEYWORD(Undef              ,"undef"                ,0x66B12803)
PR_SCRIPT_PPKEYWORD(Ifdef              ,"ifdef"                ,0x06157A6F)
PR_SCRIPT_PPKEYWORD(Ifndef             ,"ifndef"               ,0x6E0A9BC3)
PR_SCRIPT_PPKEYWORD(Else               ,"else"                 ,0x0B007568)
PR_SCRIPT_PPKEYWORD(Elif               ,"elif"                 ,0x2224DE09)
PR_SCRIPT_PPKEYWORD(Endif              ,"endif"                ,0x2AE4F08D)
PR_SCRIPT_PPKEYWORD(Include            ,"include"              ,0x123E4C0A)
PR_SCRIPT_PPKEYWORD(Def                ,"def"                  ,0x0C7AC78C)
PR_SCRIPT_PPKEYWORD(Eval               ,"eval"                 ,0x1B999BB9)
PR_SCRIPT_PPKEYWORD(Lit                ,"lit"                  ,0x5D65A870)
PR_SCRIPT_PPKEYWORD(Lua                ,"lua"                  ,0x56CF11C6)
PR_SCRIPT_PPKEYWORD(End                ,"end"                  ,0x0042155C)
#undef PR_SCRIPT_PPKEYWORD

#ifndef PR_SCRIPT_READER_H
#define PR_SCRIPT_READER_H

#include <map>
#include <vector>
#include <string>
#include <sstream>
#include <fstream>
#include <memory>
#include <algorithm>
#include "pr/common/fmt.h"
#include "pr/str/prstringextract.h"
#include "pr/common/expr_eval.h"
#include "pr/common/hash.h"
#include "pr/common/stack.h"
#include "pr/common/queue.h"
#include "pr/common/prarray.h"
#include "pr/str/prstring.h"
#include "pr/filesys/filesys.h"
#include "pr/maths/maths.h"

#pragma warning(push)
#pragma warning(disable: 4351) // new behavior: elements of array will be default initialized

// Enable this to check hash values for script preprocessor keywords and tokens
#ifndef PR_SCRIPT_CHECK_HASH_VALUES
#	define PR_SCRIPT_CHECK_HASH_VALUES 1
#endif

namespace pr
{
	namespace script
	{
		namespace EResult
		{
			enum Type
			{
				#define PR_SCRIPT_RESULT(name, value) name value,
				#include "scriptreader.h"
			};
			inline char const* ToString(Type result)
			{
				switch (result)
				{
				default: return "";
				#define PR_SCRIPT_RESULT(name, value) case name: return #name;
				#include "scriptreader.h"
				}
			}
		}
		namespace EToken
		{
			enum Type
			{
				#define PR_SCRIPT_TOKEN(name, string_name) name,
				#include "scriptreader.h"
			};
			inline char const* ToString(Type token)
			{
				switch (token)
				{
				default: return "";
				#define PR_SCRIPT_TOKEN(name, string_name) case name: return string_name;
				#include "scriptreader.h"
				}
			}
		}
		namespace EPPKeyword
		{
			enum Type
			{
				#define PR_SCRIPT_PPKEYWORD(name, text, hash) name = hash,
				#include "scriptreader.h"
			};
			inline char const* ToString(Type token)
			{
				switch (token)
				{
				default: return "";
				#define PR_SCRIPT_PPKEYWORD(name, text, hash) case name: return text;
				#include "scriptreader.h"
				}
			}
		}

		// Typedef the string type to use.
		// 'pr::string<>' uses the short string optimisation
		typedef pr::string<char, 64> string;

		// Error handling interface - includes default implementation
		// Clients can either implement the 'ScriptReader_TokenNotFound' and 'ScriptReader_Error' methods
		// or just the 'ScriptReader_ShowMessage' method to output the standard formatted error message
		// e.g.
		//  struct ErrorHandler :pr::script::IErrorHandler {void ShowMessage(char const* str) {::MessageBoxA(::GetFocus(),str,"Script Error",MB_OK);}} error_handler;
		struct IErrorHandler
		{
			virtual void ScriptReader_ShowMessage(char const*) {}
			virtual bool ScriptReader_TokenNotFound(pr::script::EToken::Type token, char const* src, int line, int column, char const* history)
			{
				ScriptReader_ShowMessage(pr::FmtS(
					"Script error:\n"
					"  Missing token: '%s'\n"
					"  Source: '%s'\n"
					"  Line: %d\n"
					"  Column: %d\n"
					"\n--Script History--\n"
					"%s"
					"\n--Script History--\n"
					,pr::script::EToken::ToString(token)
					,src
					,line
					,column
					,history
					));
				throw token;
			}
			virtual void ScriptReader_Error(pr::script::EResult::Type result, char const* error_msg, char const* src, int line, int column, char const* history)
			{
				ScriptReader_ShowMessage(pr::FmtS(
					"Script error:\n"
					"  Error Code: %s\n"
					"  Message: %s\n"
					"  Source: %s\n"
					"  Line: %d\n"
					"  Column: %d\n"
					"\n--Script History--\n"
					"%s"
					"\n--Script History--\n"
					,pr::script::EResult::ToString(result)
					,error_msg
					,src
					,line
					,column
					,history
					));
				throw result;
			}
		};

		// Interface for converting unknown #def{symbol} into a value
		struct ISymbolResolver
		{
			virtual bool ScriptReader_GetSymbol(pr::script::string const& symbol, pr::script::string& value) = 0;
			//{
			//	if (symbol == "PurpleMonkeyDishwasher")	{ value = "42"; return true; }
			//	return false;
			//}
		};

		// Interface for parsing lua code and converting it to ldr script
		struct ILuaCodeHandler
		{
			virtual bool ScriptReader_LuaCode(pr::script::string const& lua_code, pr::script::string& syntax_error_msg, pr::script::string& result) = 0;
			//{
			//	// Record the number of items on the stack
			//	int base = lua_gettop(m_lua);

			//	// Convert the lua code to a compiled chunk
			//	if (pr::lua::PushLuaChunk(m_lua, lua_code, syntax_error_msg) != pr::lua::EResult::Success)
			//		return false;

			//	// Execute the chunk
			//	if (!pr::lua::CallLuaChunk(m_lua, 0, false))
			//		return false;

			//	// If there's something still on the stack, copy it to result
			//	if (lua_gettop(m_lua) != base && !lua_isnil(m_lua, -1))
			//	{
			//		result = lua_tostring(m_lua, -1);
			//		lua_pop(m_lua, 1);
			//	}

			//	// Ensure the stack does not grow
			//	if (lua_gettop(m_lua) != base)
			//	{
			//		PR_ASSERT_STR(PR_DBG_LDR, false, "lua stack height not constant");
			//		lua_settop(m_lua, base);
			//	}

			//	return true;
			//}
		};

		// A struct for pairing a result with extra string info
		struct ResultEx
		{
			EResult::Type m_result;
			string        m_info;
			ResultEx() :m_result(EResult::Success) ,m_info() {}
			ResultEx(EResult::Type result, string info) :m_result(result) ,m_info(info) {}
		};

		// Interface to a stream of characters
		struct Src
		{
		protected:
			char m_ch;										// The last char read from the source
			int m_line, m_column;							// The position in the input source
		
			// Methods implemented by instances of char stream sources
			virtual char get() = 0;							// Get the next character from the stream
			virtual char peek() const = 0;					// Return the next character from the stream without removing it from the stream
			
		public:
			// Implementers need to call "m_ch = get();" this can't be done in this base
			// class because the implementing class will not have constructed char source yet.
			virtual ~Src() {}
			Src() :m_ch(1) ,m_line(1) ,m_column(0)			{}

			char operator *() const							{ return m_ch; }
			Src& operator ++()								{ if (m_ch != 0) {m_ch = get(); if (m_ch == '\n') {++m_line; m_column = 1;} else {++m_column;}} return *this; }
			Src& operator += (int count)					{ while (count--) ++(*this); return *this; }

			char next() const								{ return peek(); }
			int line() const								{ return m_line; }
			int column() const								{ return m_column; }
			virtual char const* name() const				{ return ""; }
			virtual string path() const						{ return ""; }	// Returns a directory associated with this source			
		};
		
		// A null terminated string char source
		struct StringSrc :Src
		{
			char const* m_ptr;
			explicit StringSrc(char const* str) :Src() ,m_ptr(str)	{ m_ch = get(); }
			char get()										{ return *m_ptr++; }
			char peek() const								{ return *m_ptr; }
			char const* name() const						{ return "string source"; }
		};

		// A file char source
		struct FileSrc :Src
		{
			string m_filename;
			mutable std::ifstream m_file;
			explicit FileSrc(char const* filename) :m_filename(filename) ,m_file(filename) { m_ch = get(); } // set the eof flag for empty files
			bool is_open() const							{ return m_file.is_open(); }
			char get()										{ int ch = m_file.get (); return m_file.good() ? char(ch) : 0; }
			char peek() const								{ int ch = m_file.peek(); return m_file.good() ? char(ch) : 0; }
			char const* name() const						{ return m_filename.c_str(); }
			string path() const								{ return pr::filesys::GetDirectory(m_filename); }
		};

		// A null terminated buffered string char source
		template <typename StringBuf> struct StringBufSrc :Src
		{
			StringBuf m_buf;
			char const* m_ptr;
			explicit StringBufSrc() :Src() ,m_buf() ,m_ptr() {}
			explicit StringBufSrc(StringBuf const& str) :Src() ,m_buf(str) { buffer_ready(); }
			void buffer_ready()                             { m_ptr = m_buf.c_str(); m_ch = get(); }
			char get()										{ return *m_ptr++; }
			char peek() const								{ return *m_ptr; }
			char const* name() const						{ return "buffered string source"; }
		};

		// Characters and words with special meaning for the script
		struct Keywords
		{
			char  m_keyword;
			char  m_preprocessor;
			char  m_section_start;
			char  m_section_end;
			char  m_new_line;
			pr::string<char, 16> m_delim;
			pr::string<char, 16> m_whitespace;

			Keywords()
			:m_keyword       ('*')
			,m_preprocessor  ('#')
			,m_section_start ('{')
			,m_section_end   ('}')
			,m_new_line      ('\n')
			,m_delim         ("{}*#+- \t\n\r,;")
			,m_whitespace    (" \t\n\r,;")
			{}
			static Keywords& Default()
			{
				static Keywords default_keywords;
				return default_keywords;
			}
			char const* delim() const      { return m_delim.c_str(); }
			char const* whitespace() const { return m_whitespace.c_str(); }
		};

		// Helper macro for non-copyable types
#		define PR_NON_COPYABLE(type)\
			private:\
				type(type const&);\
				type& operator =(type const&)

		// A stack of character stream sources
		struct InputStack
		{
			struct SrcStackItem
			{
				Src* m_src; bool m_cleanup;
				SrcStackItem(Src* src, bool cleanup) :m_src(src) ,m_cleanup(cleanup) {}
			};
			typedef pr::Array<SrcStackItem, 16> SrcStack;
			typedef pr::Array<string, 16> IncFilesCont;
			typedef pr::Queue<char, 256> History;

			SrcStack m_stack;
			Keywords m_keywords;
			bool m_record_includes;
			IncFilesCont m_inc_files;
			mutable History m_history;

			InputStack() :m_stack() ,m_keywords(Keywords::Default())		{ m_history.push_back(0); }
			explicit InputStack(Src& src) :m_stack()						{ m_history.push_back(0); push(src, false); }
			explicit InputStack(Src& src, bool delete_on_pop) :m_stack()	{ m_history.push_back(0); push(src, delete_on_pop); }
			~InputStack()													{ while (!m_stack.empty()) pop(); }
			void CopyState(InputStack const& input)							{ m_keywords = input.m_keywords; m_record_includes = input.m_record_includes; m_inc_files = input.m_inc_files; }
			void push(Src& src)												{ push(src, false); }
			void push(Src& src, bool delete_on_pop)							{ m_stack.push_back(SrcStackItem(&src, delete_on_pop)); read(); }
			void pop()														{ if (m_stack.back().m_cleanup) { delete m_stack.back().m_src; } m_stack.pop_back(); }
			InputStack& operator ++()										{ if   (!m_stack.empty()) {++*m_stack.back().m_src; read();} return *this; }
			char operator *() const											{ return m_stack.empty() ? 0 : **m_stack.back().m_src; }
			char next() const												{ return m_stack.empty() ? 0 : m_stack.back().m_src->next(); }
			string path() const												{ return m_stack.empty() ? "": m_stack.back().m_src->path(); }
			char const* name() const										{ return m_stack.empty() ? "": m_stack.back().m_src->name(); }
			int line() const												{ return m_stack.empty() ? 0 : m_stack.back().m_src->line(); }
			int column() const												{ return m_stack.empty() ? 0 : m_stack.back().m_src->column(); }
			void read()
			{
				for (;;)
				{
					while (!m_stack.empty() && **m_stack.back().m_src == 0) { pop(); }
					if (m_stack.empty()) return;

					Src& src = *m_stack.back().m_src;

					// Strip comments
					if (*src == '/' && src.next() == '/') // Line comment
					{
						for (++src; *src && *src != m_keywords.m_new_line; ++src) {} ++src;
						continue;
					}
					if (*src == '/' && src.next() == '*') // Block comment
					{
						bool star = false;
						for (++src; *src && !(star && *src == '/'); star = *src == '*', ++src) {} ++src;
						continue;
					}

					m_history.back() = *src;
					m_history.push_back_overwrite(0);
					break;
				}
			}
			void include_dependency(string filepath)
			{
				if (!m_record_includes) return;
				pr::filesys::Canonicalise(filepath);
				IncFilesCont::iterator iter = std::find(m_inc_files.begin(), m_inc_files.end(), filepath);
				if (iter == m_inc_files.end()) m_inc_files.push_back(filepath);
			}
			char const* history() const
			{
				m_history.canonicalise();
				return &m_history[0];
			}
			PR_NON_COPYABLE(InputStack);
		};

		// A single token from the input stream
		struct Token
		{
			EToken::Type m_type;
			string       m_str;
			Token() :m_type(EToken::Unknown) ,m_str() {}
			Token(EToken::Type type, char const* str) :m_type(type) ,m_str(str) {}
			void clear()                                 { m_type = EToken::Unknown; m_str.resize(0); }
			void lower_case()                            { for (string::iterator i = m_str.begin(), iend = m_str.end(); i != iend; ++i) {*i = static_cast<string::value_type>(tolower(*i));} }
			void quote()                                 { m_str.insert(m_str.begin(), '"'); m_str.insert(m_str.end(), '"'); }
			char const* str() const                      { return m_str.c_str(); }
			pr::hash::HashValue hash() const             { return pr::hash::HashC(str()); }
			Token& operator << (char ch)                 { m_str.push_back(ch); return *this; }
			Token& operator << (Token const& tok)        { m_type = EToken::Unknown; m_str += tok.m_str; return *this; }
			bool operator == (char const* str) const     { return m_str == str; }
			bool operator == (EToken::Type tok) const    { return m_type == tok; }
			bool operator != (EToken::Type tok) const    { return m_type != tok; }
		};

		// Implementation detail
		namespace impl
		{
			// An 8-character shift register for doing short string matches
			template <typename Src> struct Buf8
			{
				union { uint64 m_ui; char m_ch[9]; };
				Src* m_src;
				int m_size;
				
				Buf8(Src* src, int size) :m_ch() ,m_src(src) ,m_size(size)          { for (int i = 0; i != m_size; ++i, ++*m_src) {m_ch[i] = **m_src;} }
				char operator *() const                                             { return m_ch[0]; }
				Buf8& operator ++()                                                 { m_ui >>= 8; m_ch[m_size-1] = **m_src; ++*m_src; return *this; }
				Buf8& operator << (char ch)                                         { m_ui >>= 8; m_ch[m_size-1] = ch; return *this; }
				template <typename Src2> bool match(Buf8<Src2> const& buf) const    { return m_ui != 0 && (m_ui & buf.m_ui) == m_ui; }
			};

			// An 8-character shift register for a constant string
			template <> struct Buf8<char const*>
			{
				union { uint64 m_ui; char m_ch[9]; };
				int m_size;
				Buf8(char const* str) :m_ch() ,m_size(0)                            { for (; m_size != 8 && *str != 0; ++m_size, ++str) {m_ch[m_size] = *str;} }
				int size() const                                                    { return m_size; }
				Buf8& operator << (char ch)                                         { m_ui >>= 8; m_ch[m_size-1] = ch; return *this; }
				template <typename Src2> bool match(Buf8<Src2> const& buf) const    { return m_ui != 0 && (m_ui & buf.m_ui) == m_ui; }
			};

			typedef Buf8<char const*> CBuf8;

			// Returns true if 'ch' is an identifier or value character
			// We can't tell the difference between identifiers and values, so don't treat them as different tokens
			// e.g. Deadbeef <- identifier or hex value?
			// This means identifiers can be "Identifier.with.points_in" and values can be "1.0e-10"
			// Note, leading '-','+' signs are different tokens and must be concatenated
			inline bool is_value_first_char(char ch)	{ return isalnum(ch) || ch == '_' || ch == '.'; }
			inline bool is_value_char(char ch)			{ return isalnum(ch) || ch == '_' || ch == '.' || ch == '+' || ch == '-'; }
			
			// Searches for 'ch' in 'str' and returns the pointer to it in the string if found, or a pointer to '\0'
			inline char const* find_char(char const* str, char ch)
			{
				for (;*str && *str != ch; ++str) {}
				return str;
			}

			// Combine two tokens. On return dst = dst + src
			inline Token& concatenate_tokens(Token& dst, Token const& src, Keywords const& keywords)
			{
				if (!dst.m_str.empty())					dst << ' ';
				if (src == EToken::Keyword)				dst << keywords.m_keyword;
				if (src == EToken::PreprocessorCommand)	dst << keywords.m_preprocessor;
				if (src == EToken::String)				dst << '"';
				dst << src;
				if (src == EToken::String)				dst << '"';
				return dst;
			}

			// Skip all tokens between matched '{' and '}' tokens
			template <typename TokenSrc> inline void skip_section(TokenSrc& tokens, Keywords const& keywords)
			{
				for (int nest = 1;;)
				{
					Token tok = tokens.get(keywords);
					nest += int(tok == EToken::SectionStart);
					nest -= int(tok == EToken::SectionEnd);
					if (nest == 0 || tok == EToken::EndOfStream) break;
				}
			}
			
			// Concatenate all tokens between matched '{' and '}' tokens
			template <typename TokenSrc> inline void copy_section(TokenSrc& tokens, Token& token, Keywords const& keywords)
			{
				if (tokens.peek(keywords) == EToken::SectionStart) tokens.get(keywords); else throw EResult::SectionStartNotFound;
				for (int nest = 1;;)
				{
					Token tok = tokens.peek(keywords);
					nest += int(tok == EToken::SectionStart);
					nest -= int(tok == EToken::SectionEnd);
					if (nest == 0 || tok == EToken::EndOfStream) break;
					concatenate_tokens(token, tokens.get(keywords), keywords);
				}
				if (tokens.peek(keywords) == EToken::SectionEnd) tokens.get(keywords); else throw EResult::SectionEndNotFound;
			}

			// Eat tokens up to the matching #elif, #else, or #endif for a previous #ifdef, #ifndef, or #elif
			template <typename TokenSrc> inline void skip_preprocessor_block(TokenSrc& tokens, Keywords const& keywords)
			{
				for (int nest = 1;;)
				{
					Token tok = tokens.peek(keywords);
					if (tok == EToken::PreprocessorCommand)
					{
						pr::hash::HashValue ppkeyword = tok.hash();
						nest += int(ppkeyword == EPPKeyword::Ifdef || ppkeyword == EPPKeyword::Ifndef);
						nest -= int(ppkeyword == EPPKeyword::Endif || (nest == 1 && (ppkeyword == EPPKeyword::Elif || ppkeyword == EPPKeyword::Else)));
					}
					if (nest == 0 || tok == EToken::EndOfStream) break;
					tokens.get(keywords);
				}
			}

			// Advance 'src' past a string. Ignores '"' characters escaped with '\'
			template <typename SrcType> inline void skip_string(SrcType& src)
			{
				for (; *src && *src != '"'; ++src) {}													// find the first "
				if (*src == '"') ++src; else throw EResult::StringNotFound;								// Confirm '"'
				for (bool esc = false; *src && (esc || *src != '"'); esc = *src=='\\', ++src) {}		// skip the string
				if (*src == '"') ++src; else throw EResult::StringNotFound;								// Confirm '"'
			}

			// Copies from 'src' into 'str'. Ignores '"' characters escaped with '\'
			template <typename SrcType> inline void copy_string(SrcType& src, string& str, bool include_quotes)
			{
				for (; *src && *src != '"'; ++src) {}																	// find the first "
				if (*src == '"') ++src; else throw EResult::StringNotFound;												// Confirm '"'
				if (include_quotes) str.push_back('"');
				for (bool esc = false; *src && (esc || *src != '"'); esc = *src=='\\', ++src) { str.push_back(*src); }	// copy the string
				if (include_quotes) str.push_back('"');
				if (*src == '"') ++src; else throw EResult::StringNotFound;												// Confirm '"'
			}

			// Copy all characters between matched '{' and '}' tokens into 'str'.
			template <typename SrcType, typename Keywords> inline void copy_section(SrcType& src, string& str, Keywords const& keywords)
			{
				for (; *src && *src != keywords.m_section_start; ++src) {}								// find section start
				if (*src == keywords.m_section_start) ++src; else throw EResult::SectionStartNotFound;	// confirm '{'
				for (int nest = 1; *src; ++src)															// copy the section
				{
					while (*src == '"') copy_string(src, str, true);
					nest += int(*src == keywords.m_section_start);
					nest -= int(*src == keywords.m_section_end);
					if (nest == 0) break;
					str.push_back(*src);
				}
				if (*src == keywords.m_section_end) ++src; else throw EResult::SectionEndNotFound;		// confirm '}'
			}

			// Copy tokens up to a matching #end token into 'str'
			template <typename SrcType> inline void copy_literal_section(SrcType& src, string& str)
			{
				CBuf8 end("#end");
				Buf8<SrcType> buf(&src, end.size());

				// '#lit','#lua',etc must be followed by a delimiter so that the keyword
				// is identified. Don't include this whitespace in the section.
				if (*buf != '#') ++buf;

				// copy everying from 'buf' into 'str' until '#end' is found
				for (; *buf && !end.match(buf); ++buf)
					str.push_back(*buf);
			}
		}

		// A layer that converts a stream of characters into tokens
		struct Tokeniser
		{
			InputStack& m_src;
			Token       m_token;
			bool        m_token_valid;

			explicit Tokeniser(InputStack& src)
			:m_src(src)
			,m_token()
			,m_token_valid(false)
			{}

			// Copy settings, keywords, macros, etc from an existing reader
			void CopyState(Tokeniser const&)
			{}
			
			// Get the next token from the stream
			Token get(Keywords const& keywords)
			{
				peek(keywords);
				m_token_valid = false;
				return m_token;
			}
			
			// Get the next token without removing it from the stream
			Token peek(Keywords const& keywords)
			{
				// If 'm_token' is invalid get it from the stream
				if (!m_token_valid)
				{
					m_token.clear();

					// Find the first non-whitespace character
					for (; *m_src && *impl::find_char(keywords.whitespace(), *m_src) != 0; ++m_src) {}

					// Check the next character in the stream and then read
					// further characters till we reach the end of the token
					if (*m_src == 0)
					{
						m_token.m_type = EToken::EndOfStream;
					}
					else if (*m_src == keywords.m_keyword)
					{
						// Keyword token -> read to the first non-alphanumeric character
						m_token.m_type = EToken::Keyword;
						for (++m_src; *m_src && impl::is_value_char(*m_src); ++m_src) m_token << *m_src;
					}
					else if (*m_src == keywords.m_preprocessor)
					{
						// Preprocessor token -> read to the first non-alphanumeric character
						m_token.m_type = EToken::PreprocessorCommand;
						for (++m_src; *m_src && isspace(*m_src); ++m_src) {} // Allow whitespace between # and the command
						for (       ; *m_src && isalnum(*m_src); ++m_src) { m_token << *m_src; }
					}
					else if (*m_src == keywords.m_section_start)
					{
						// Section start
						m_token.m_type = EToken::SectionStart;
						m_token << *m_src;
						++m_src;
					}
					else if (*m_src == keywords.m_section_end)
					{
						// Section end
						m_token.m_type = EToken::SectionEnd;
						m_token << *m_src;
						++m_src;
					}
					else if (*m_src == '"')
					{
						// String token, read to the next non-escaped '"' character
						m_token.m_type = EToken::String;
						bool esc = false;
						for (++m_src; *m_src && (esc || *m_src != '"'); esc = *m_src=='\\', ++m_src) m_token << *m_src;
						if (*m_src != 0) ++m_src; else throw EResult::IncompleteString;
					}
					else if (*m_src == '-')
					{
						m_token.m_type = EToken::Minus;
						m_token << *m_src;
						++m_src;
					}
					else if (*m_src == '+')
					{
						m_token.m_type = EToken::Plus;
						m_token << *m_src;
						++m_src;
					}
					else if (impl::is_value_first_char(*m_src))
					{
						// A numeric value
						m_token.m_type = EToken::Value;
						m_token << *m_src;
						for (++m_src; *m_src && impl::is_value_char(*m_src); ++m_src) m_token << *m_src;
					}
					else
					{
						// Unknown token
						m_token.m_type = EToken::Unknown;
						m_token << *m_src;
						++m_src;
					}
				}
				m_token_valid = true;
				return m_token;
			}
			PR_NON_COPYABLE(Tokeniser);
		};

		// A layer that performs preprocessing on a stream of tokens and outputs tokens
		struct Preprocessor
		{
			typedef std::map<string, string> MacroCont;
			typedef std::vector<string> Paths;
			typedef pr::BitStack<> PPStack;	// Proprocessor #if/endif stack
			typedef string EvalResult;
			
			Tokeniser&			m_tokens;					// The input source of tokens
			MacroCont			m_macros;					// Container of macro definitions
			PPStack				m_pp_stack;					// A stack recording the state of #if/#endif blocks
			EvalResult			m_eval_result;				// The result of the last evaluated expression
			Paths				m_paths;					// Include paths
			ISymbolResolver*	m_get_symbol;				// Symbol resolver interface
			ILuaCodeHandler*    m_lua_handler;
			bool				m_ignore_missing_includes;
			Token				m_token;
			bool				m_token_valid;

			explicit Preprocessor(Tokeniser& tokeniser)
			:m_tokens(tokeniser)
			,m_macros()
			,m_pp_stack()
			,m_eval_result()
			,m_paths()
			,m_get_symbol(0)
			,m_lua_handler(0)
			,m_ignore_missing_includes(false)
			,m_token()
			,m_token_valid(false)
			{}

			// Copy settings, keywords, macros, etc from an existing reader
			void CopyState(Preprocessor const& pp)
			{
				m_macros					= pp.m_macros;
				m_paths						= pp.m_paths;
				m_get_symbol				= pp.m_get_symbol;
				m_ignore_missing_includes	= pp.m_ignore_missing_includes;
			}

			// Get the next token from the stream
			Token get(Keywords const& keywords)
			{
				peek(keywords);
				m_token_valid = false;
				return m_token;
			}

			// Get the next token without removing it from the stream
			Token peek(Keywords const& keywords)
			{
				if (!m_token_valid)
				{
					m_token = m_tokens.get(keywords);
					while (m_token == EToken::PreprocessorCommand)
					{
						switch (m_token.hash())
						{
						default:
							{
								throw EResult::UnknownPreprocessorCommand;
							}
						case EPPKeyword::Ifdef:
							{
								string ident;
								impl::copy_section(m_tokens.m_src, ident, keywords);
								if (m_macros.find(ident) == m_macros.end()) { m_pp_stack.push(false); impl::skip_preprocessor_block(m_tokens, keywords); }
								else                                        { m_pp_stack.push(true); }
							}break;
						case EPPKeyword::Ifndef:
							{
								string ident;
								impl::copy_section(m_tokens.m_src, ident, keywords);
								if (m_macros.find(ident) != m_macros.end()) { m_pp_stack.push(false); impl::skip_preprocessor_block(m_tokens, keywords); }
								else                                        { m_pp_stack.push(true); }
							}break;
						case EPPKeyword::Elif:
							{
								if (m_pp_stack.empty()) throw EResult::UnmatchedPreprocessorCommand;
								string ident;
								impl::copy_section(m_tokens.m_src, ident, keywords);
								if (m_pp_stack.top())                       { impl::skip_preprocessor_block(m_tokens, keywords); break; }
								if (m_macros.find(ident) == m_macros.end()) { impl::skip_preprocessor_block(m_tokens, keywords); break; }
								m_pp_stack.top() = true;
							}break;
						case EPPKeyword::Else:
							{
								if (m_pp_stack.empty()) throw EResult::UnmatchedPreprocessorCommand;
								if (m_pp_stack.top())								{ impl::skip_preprocessor_block(m_tokens, keywords); break; }
								m_pp_stack.top() = true;
							}break;
						case EPPKeyword::Endif:
							{
								if (m_pp_stack.empty()) throw EResult::UnmatchedPreprocessorCommand;
								m_pp_stack.pop();
							}break;
						case EPPKeyword::Define:
							{
								string ident, value;
								impl::copy_section(m_tokens.m_src, ident, keywords);
								impl::copy_section(m_tokens.m_src, value, keywords);
								m_macros[ident] = value;
							}break;
						case EPPKeyword::Undef:
							{
								string ident;
								impl::copy_section(m_tokens.m_src, ident, keywords);
								MacroCont::iterator iter = m_macros.find(ident);
								if (iter != m_macros.end()) m_macros.erase(iter);
							}break;
						case EPPKeyword::Def:
							{
								string ident;
								impl::copy_section(m_tokens.m_src, ident, keywords);
								MacroCont::const_iterator iter = m_macros.find(ident);
								if (iter != m_macros.end())
									m_tokens.m_src.push(*new StringSrc(iter->second.c_str()), true);
								else
								{
									string value;
									if (m_get_symbol && m_get_symbol->ScriptReader_GetSymbol(ident, value))
										m_tokens.m_src.push(*new StringBufSrc<string>(value), true);
									else
										throw EResult::DefSymbolNotDefined;
								}
							}break;
						case EPPKeyword::Eval:
							{
								// We want to copy the whole section including whitespace for expressions
								Keywords kws;
								kws.m_whitespace.clear();
								kws.m_delim.clear();

								Token tok;
								impl::copy_section(*this, tok, kws);
								
								double value;
								if (!pr::Evaluate(tok.str(), value)) throw EResult::EvalSyntaxError;

								std::stringstream result; result << value;
								m_token.m_type = EToken::Value;
								m_token.m_str = result.str();
								m_token_valid = true;
							}break;
						case EPPKeyword::Lit:
							{
								m_token.clear();
								m_token.m_type = EToken::Section;
								impl::copy_literal_section(m_tokens.m_src, m_token.m_str);
								m_token_valid = true;
							}break;
						case EPPKeyword::Lua:
							{
								string lua_code, err_msg;
								impl::copy_literal_section(m_tokens.m_src, lua_code);
								if (m_lua_handler)
								{
									std::auto_ptr< StringBufSrc<string> > result(new StringBufSrc<string>());
									if (!m_lua_handler->ScriptReader_LuaCode(lua_code, err_msg, result->m_buf))
										throw ResultEx(EResult::InvalidLuaCode, err_msg);
									result->buffer_ready();
									m_tokens.m_src.push(*result.release(), true);
								}
							}break;
						case EPPKeyword::End:
							{
								throw EResult::UnmatchedPreprocessorCommand;
							}break;
						case EPPKeyword::Include:
							{
								// The next token should be an include filename
								Token file = m_tokens.get(keywords);
								if (file != EToken::String) throw EResult::IncludeFileMissing;

								// Get the full path for the file
								if (!get_full_path(file.m_str))
								{
									if (m_ignore_missing_includes) break;
									throw EResult::IncludeFileMissing;
								}

								// Push the include file onto the stack
								m_tokens.m_src.push(*new FileSrc(file.str()), true);
								m_tokens.m_src.include_dependency(file.str());
							}break;
						}

						// If the above code has not created a valid token, get the next one
						if (!m_token_valid)
							m_token = m_tokens.get(keywords);
					}
				}
				m_token_valid = true;
				return m_token;
			}

			// Convert a partial path into a full path using the provided include directories
			bool get_full_path(string& file)
			{
				// If 'file' is complete already, then just use it
				if (pr::filesys::FileExists(file)) return true;
				
				// Look in the current directory of the top item on the input stack
				string path = pr::filesys::Make(m_tokens.m_src.path(), file);
				if (pr::filesys::FileExists(path)) { file = path; return true; }
				
				// Look in the provided include paths
				for (Paths::const_reverse_iterator i = m_paths.rbegin(), i_end = m_paths.rend(); i != i_end; ++i)
				{
					path = pr::filesys::Make(*i, file);
					if (pr::filesys::FileExists(path)) { file = path; return true; }
				}
				return false;
			}
			PR_NON_COPYABLE(Preprocessor);
		};


		// The script reader
		class Reader
		{
			InputStack     m_input;
			Tokeniser      m_tokeniser;
			Preprocessor   m_preprocessor;
			Preprocessor&  m_src;
			IErrorHandler* m_error_handler;
			bool           m_case_sensitive;

			Token get()
			{
				try { return m_src.get(m_input.m_keywords); }
				catch (EResult::Type r) { ReportError(r); return Token(); }
				catch (ResultEx r)      { ReportError(r.m_result, r.m_info.c_str()); return Token(); }
			}
			Token peek()
			{
				try { return m_src.peek(m_input.m_keywords); }
				catch (EResult::Type r) { ReportError(r); return Token(); }
				catch (ResultEx r)      { ReportError(r.m_result, r.m_info.c_str()); return Token(); }
			}
			PR_NON_COPYABLE(Reader);

		public:
			Reader()
			:m_input()
			,m_tokeniser(m_input)
			,m_preprocessor(m_tokeniser)
			,m_src(m_preprocessor)
			,m_error_handler(0)
			,m_case_sensitive(false)
			{
				#if PR_SCRIPT_CHECK_HASH_VALUES
				#define PR_SCRIPT_PPKEYWORD(name, text, hash_value)\
						PR_ASSERT_STR(1, EPPKeyword::name == pr::hash::HashC(text), pr::FmtS("Hash value for EPPKeyword::"#name" should be 0x%08X\n", pr::hash::HashC(text)));
				#include "scriptreader.h"
				#endif
			}
	
			// Get/Set whether keywords are case sensitive
			// If false (default), then all keywords are returned as lower case
			bool CaseSensitiveKeywords() const
			{
				return m_case_sensitive;
			}
			void CaseSensitiveKeywords(bool case_sensitive)
			{
				m_case_sensitive = case_sensitive;
			}
	
			// Get/Set whether missing includes are ignored
			bool IgnoreMissingIncludes() const
			{
				return m_preprocessor.m_ignore_missing_includes;
			}
			void IgnoreMissingIncludes(bool ignore)
			{
				m_preprocessor.m_ignore_missing_includes = ignore;
			}

			// Set whether to record the names of included files found while parsing the script
			void RecordIncludeDependencies(bool yes)
			{
				m_input.m_record_includes = yes;
			}
			typedef InputStack::IncFilesCont StrCont;
			StrCont const& GetIncludeDependencies() const
			{
				return m_input.m_inc_files;
			}
	
			// Set the interface to a script error handler
			void SetErrorHandler(IErrorHandler* error_handler)
			{
				m_error_handler = error_handler;
			}
	
			// Set the interface for converting undefined symbols into values
			void SetSymbolResolver(ISymbolResolver* get_symbol)
			{
				m_preprocessor.m_get_symbol = get_symbol;
			}

			// Set the interface for handling lua code
			void SetLuaCodeHandler(ILuaCodeHandler* lua_handler)
			{
				m_preprocessor.m_lua_handler = lua_handler; 
			}
	
			// Copy settings, keywords, macros, etc from an existing reader
			void CopyState(Reader const& reader)
			{
				m_input          .CopyState(reader.m_input);
				m_tokeniser      .CopyState(reader.m_tokeniser);			
				m_preprocessor   .CopyState(reader.m_preprocessor);
				m_error_handler  = reader.m_error_handler;
				m_case_sensitive = reader.m_case_sensitive;
			}
	
			// Return the hash of a keyword using the current reader settings
			pr::hash::HashValue HashKeyword(char const* keyword) const
			{
				Token tok(EToken::Keyword, keyword);
				if (!m_case_sensitive) tok.lower_case();
				return tok.hash();
			}
	
			// Check the hash value of a keyword using the current reader settings.
			// Returns true if the hash of 'keyword' matches 'hashvalue'.
			// 'expected' returns the expected hash value of 'keyword'.
			bool CheckKeywordValue(char const* keyword, pr::hash::HashValue hashvalue, pr::hash::HashValue& expected) const
			{
				expected = HashKeyword(keyword);
				return hashvalue == expected;
			}
	
			// Generate a string containing the entire source preprocessed
			template <typename StrType> StrType PreprocessOutput()
			{
				Token tok;
				while (!IsSourceEnd())
					impl::concatenate_tokens(tok, get(), m_input.m_keywords);
				return StrType(tok.m_str.c_str());
			}
	
			// Allow users to report errors via the internal error handler
			bool ReportNotFound(EToken::Type token)
			{
				return m_error_handler ? m_error_handler->ScriptReader_TokenNotFound(token, m_input.name(), m_input.line(), m_input.column(), m_input.history()) : false;
			}
			void ReportError(EResult::Type result)
			{
				if (m_error_handler) m_error_handler->ScriptReader_Error(result, EResult::ToString(result), m_input.name(), m_input.line(), m_input.column(), m_input.history());
			}
			void ReportError(EResult::Type result, char const* msg)
			{
				if (m_error_handler) m_error_handler->ScriptReader_Error(result, msg, m_input.name(), m_input.line(), m_input.column(), m_input.history());
			}
			void ReportError(char const* msg)
			{
				if (m_error_handler) m_error_handler->ScriptReader_Error(pr::script::EResult::Failed, msg, m_input.name(), m_input.line(), m_input.column(), m_input.history());
			}
	
			// Push a source onto the input stack
			void AddSource(Src& src, bool delete_on_pop)
			{
				m_input.push(src, delete_on_pop);
			}
	
			// Push a string script source onto the input stack
			void AddString(char const* str)
			{
				AddSource(*new StringSrc(str), true);
			}
	
			// Push a file script source onto the input stack
			bool AddFile(char const* filename)
			{
				FileSrc* file_src = new FileSrc(filename);
				AddSource(*file_src, true);
				if (file_src->is_open()) return true;
				ReportError(EResult::FailedToLoadFile);
				return false;
			}
	
			// Return true if the end of the source has been reached
			bool IsSourceEnd()
			{
				return peek() == EToken::EndOfStream;
			}
	
			// Return true if the next token is a keyword
			bool IsKeyword()
			{
				return peek() == EToken::Keyword;
			}
	
			// Return true if the next non-whitespace character is the start/end of a section
			bool IsSectionStart()
			{
				return peek() == EToken::SectionStart;
			}
			bool IsSectionEnd()
			{
				return peek() == EToken::SectionEnd;
			}
	
			// Move to the start/end of a section and then one past it
			bool SectionStart()
			{
				if (IsSectionStart()) { get(); return true; }
				else return ReportNotFound(EToken::SectionStart);
			}
			bool SectionEnd()
			{
				if (IsSectionEnd()) { get(); return true; }
				else return ReportNotFound(EToken::SectionEnd);
			}
	
			// Move to the start of the next line
			bool NewLine()
			{
				for (; *m_input && *m_input != '\n'; ++m_input) {}
				if (*m_input == '\n' ) { ++m_input; return true; }
				return ReportNotFound(EToken::NewLine);
			}
	
			// Advance the source to the next '{'
			// On return the current position should be a section start character
			// or the end of the current section or end of the input stream if not found
			bool FindSectionStart()
			{
				for (;;)
				{
					Token tok = peek();
					if (tok == EToken::SectionStart) return true;
					if (tok == EToken::SectionEnd || tok == EToken::EndOfStream) return false;
					get();
				}
			}
	
			// Advance the source to the end of the current section
			// On return the current position should be the section end character
			// or the end of the input stream (if called from file scope).
			bool FindSectionEnd()
			{
				for (int nest = 1;;) // Assume we're starting within a section
				{
					Token tok = peek();
					if (tok == EToken::EndOfStream) return false;
					nest += int(tok == EToken::SectionStart);
					nest -= int(tok == EToken::SectionEnd);
					if (nest == 0) return true;
					get();
				}
			}
	
			// Return the hash of the next keyword
			pr::hash::HashValue GetKeyword()
			{
				Token tok;
				for (tok = get(); tok != EToken::Keyword && tok != EToken::SectionEnd && tok != EToken::EndOfStream; tok = get())
				{
					if (tok == EToken::SectionStart)
						impl::skip_section(m_src, m_input.m_keywords);
				}
				if (tok != EToken::Keyword) { ReportNotFound(EToken::Keyword); return 0; }
				if (!m_case_sensitive) tok.lower_case();
				return tok.hash();
			}
	
			// Scan forward until a keyword identifier is found. 
			// If a section is found it is skipped.
			template <typename StrType> bool GetKeyword(StrType& kw)
			{
				Token tok;
				for (tok = get(); tok != EToken::Keyword && tok != EToken::SectionEnd && tok != EToken::EndOfStream; tok = get())
				{
					if (tok == EToken::SectionStart)
						impl::skip_section(m_src, m_input.m_keywords);
				}
				if (tok != EToken::Keyword) return ReportNotFound(EToken::Keyword);
				if (!m_case_sensitive) tok.lower_case();
				return pr::str::ExtractIdentifierC(kw, tok.str());
			}
	
			// Read the next keyword from the source, expecting it to be 'keyword'
			// Reports an error if it isn't
			bool Keyword(char const* keyword)
			{
				if (GetKeyword() == HashKeyword(keyword)) return true;
				return ReportNotFound(EToken::Keyword);
			}
	
			// Scan forward until a keyword identifier is found that matches 'kw'.
			// Only looks in the current scope. Source position is advanced to one
			// past the keyword if found. Does not call NotFound if not found
			// so that this function can be used to find keywords that mightn't be there.
			template <typename StrType> bool FindKeyword(StrType const& kw, std::size_t length, bool match_case = true)
			{
				Token tok;
				for (tok = get(); tok != EToken::EndOfStream && tok != EToken::SectionEnd; tok = get())
				{
					if (tok == EToken::Keyword)
					{
						if ( match_case && pr::str::EqualN (kw, tok.str(), length)) return true;
						if (!match_case && pr::str::EqualNI(kw, tok.str(), length)) return true;
					}
					else if (tok == EToken::SectionStart)
					{
						impl::skip_section(m_src, m_input.m_keywords);
					}
				}
				return false;
			}
			template <typename CharType, int Len> bool FindKeyword(CharType (&kw)[Len], bool match_case = true)
			{
				return FindKeyword(kw, Len - 1, match_case);
			}
			bool FindKeyword(pr::hash::HashValue kw)
			{
				Token tok;
				for (tok = get(); tok != EToken::EndOfStream && tok != EToken::SectionEnd; tok = get())
				{
					if (tok == EToken::Keyword)
					{
						if (!m_case_sensitive) tok.lower_case();
						if (tok.hash() == kw) return true;
					}
					else if (tok == EToken::SectionStart)
					{
						impl::skip_section(m_src, m_input.m_keywords);
					}
				}
				return false;
			}
	
			// Extract an identifier from the source.
			// An identifier is a block of non-white space characters.
			template <typename StrType>	bool ExtractIdentifier(StrType& word)
			{
				Token tok = get();
				if (pr::str::ExtractIdentifierC(word, tok.str())) return true;
				return ReportNotFound(EToken::Identifier);
			}
	
			// Extract a string from the source.
			// A string is a sequence of characters between quotes.
			template <typename StrType>	bool ExtractString(StrType& string)
			{
				Token tok = get();
				if (tok == EToken::String) tok.quote(); else return ReportNotFound(EToken::String);
				return pr::str::ExtractStringC(string, tok.str());
			}
	
			// Extract a C-style string from the source.
			template <typename StrType>	bool ExtractCString(StrType& cstring)
			{
				Token tok = get();
				if (tok == EToken::String) tok.quote(); else return ReportNotFound(EToken::String);
				return pr::str::ExtractCStringC(cstring, tok.str());
			}
	
			// Extract a bool from the source.
			template <typename Bool> bool ExtractBool(Bool& bool_)
			{
				Token tok = get();
				if (pr::str::ExtractBoolC(bool_, tok.str())) return true;
				return ReportNotFound(EToken::Bool);
			}
	
			// Extract an integral type from the source.
			template <typename Int> bool ExtractInt(Int& int_, int radix)
			{
				Token tok = get();
				if (tok == EToken::Minus || tok == EToken::Plus) tok << get();
				if (pr::str::ExtractIntC(int_, radix, tok.str())) return true;
				return ReportNotFound(EToken::Integral);
			}
	
			// Extract a real from the source.
			template <typename Real> bool ExtractReal(Real& real_)
			{
				Token tok = get();
				if (tok == EToken::Minus || tok == EToken::Plus) tok << get();
				if (pr::str::ExtractRealC(real_, tok.str())) return true;
				return ReportNotFound(EToken::Real);
			}
	
			// Extract an array of bools from the source.
			template <typename Bool> bool ExtractBoolArray(Bool* bools, std::size_t num_bools)
			{
				while (num_bools--) if (!ExtractBool(*bools++)) return false;
				return true;	
			}
	
			// Extract an array of integral types from the source.
			template <typename Int> bool ExtractIntArray(Int* ints, std::size_t num_ints, int radix)
			{
				while (num_ints--) if (!ExtractInt(*ints++, radix)) return false;
				return true;
			}
	
			// Extract an array of reals from the source.
			template <typename Real> bool ExtractRealArray(Real* reals, std::size_t num_reals)
			{
				while (num_reals--) if (!ExtractReal(*reals++)) return false;
				return true;	
			}
	
			// Extract a vector from the source
			bool ExtractVector2(v2& vector)
			{
				return ExtractReal(vector.x) && ExtractReal(vector.y);
			}
	
			// Extract a vector from the source
			bool ExtractVector3(v4& vector, float w)
			{
				vector.w = w;
				return ExtractReal(vector.x) && ExtractReal(vector.y) && ExtractReal(vector.z);
			}
	
			// Extract a vector from the source
			bool ExtractVector4(v4& vector)
			{
				return ExtractReal(vector.x) && ExtractReal(vector.y) && ExtractReal(vector.z) && ExtractReal(vector.w);
			}
	
			// Extract a quaternion from the source
			bool ExtractQuaternion(Quat& quaternion)
			{
				return ExtractReal(quaternion.x) && ExtractReal(quaternion.y) && ExtractReal(quaternion.z) && ExtractReal(quaternion.w);
			}
	
			// Extract a 3x3 matrix from the source
			bool ExtractMatrix3x3(m3x3& transform)
			{
				return ExtractVector3(transform.x,0.0f) && ExtractVector3(transform.y,0.0f) && ExtractVector3(transform.z,0.0f);
			}
	
			// Extract a 4x4 matrix from the source
			bool ExtractMatrix4x4(m4x4& transform)
			{
				return ExtractVector4(transform.x) && ExtractVector4(transform.y) && ExtractVector4(transform.z) && ExtractVector4(transform.w);
			}
	
			// Extract a byte array
			bool ExtractData(void* data, std::size_t length)
			{
				return ExtractIntArray(static_cast<unsigned char*>(data), length, 16);
			}
	
			// Extract a complete section as a preprocessed string.
			// Note: To embed arbitrary text in a script use #lit #endlit and then
			// ExtractSection rather than ExtractLiteralSection
			template <typename String> bool ExtractSection(String& str, bool include_braces)
			{
				try
				{
					Token dst;
					if (include_braces) dst << '{';
					impl::copy_section(m_src, dst, m_input.m_keywords);
					if (include_braces) dst << '}';
					str = dst.m_str.c_str();
					return true;
				}
				catch (EResult::Type r)
				{
					if (r == EResult::SectionStartNotFound) return ReportNotFound(EToken::SectionStart);
					if (r == EResult::SectionEndNotFound)	return ReportNotFound(EToken::SectionEnd);
					ReportError(r);
					return false;
				}
			}
	
			// Extract a complete section as a non-preprocessed string.
			// Note: To embed arbitrary text in a script use #lit #endlit and then
			// ExtractSection rather than ExtractLiteralSection
			template <typename String> bool ExtractLiteralSection(String& str, bool include_braces)
			{
				try
				{
					string buf;
					if (include_braces) buf.push_back('{');
					impl::copy_section(m_input, buf, m_input.m_keywords);
					if (include_braces) buf.push_back('}');
					str = buf.c_str();
					return true;
				}
				catch (EResult::Type r)
				{
					if (r == EResult::SectionStartNotFound) return ReportNotFound(EToken::SectionStart);
					if (r == EResult::SectionEndNotFound)	return ReportNotFound(EToken::SectionEnd);
					ReportError(r);
					return false;
				}
			}
	
			// Allow extension methods
			// Specialise like this:
			// template <> bool pr::script::Reader::Extract<MyType>(MyType& my_type)
			//{
			//	return ExtractInt(my_type.int, 10); // etc
			//}
			template <typename Type> bool Extract(Type& type);
		};

		#undef PR_NON_COPYABLE
	}
}

#pragma warning(pop)
#endif

