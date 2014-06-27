//**********************************
// Script Reader
//  Copyright (c) Rylogic Ltd 2007
//**********************************
// Usage Notes:
// - Programmatically Resolved Symbols:
//   Implement using the IPPMacroDB interface. The default implementation
//   'PPMacroDB' has a 'm_resolver' pointer setup for this already.
// - Embedded Lua Code:
//   Use an 'EmbeddedLua' char stream

#pragma once
#ifndef PR_SCRIPT_READER_H
#define PR_SCRIPT_READER_H

#include "pr/maths/maths.h"
#include "pr/script/script.h"

namespace pr
{
	namespace script
	{
		// Error handling interface - includes default implementation
		// Clients can either implement the 'IErrorHandler_Error' method or just the
		// 'ScriptReader_ShowMessage' method to output the standard formatted error message.
		// e.g.
		//   struct ErrorHandler :pr::script::IErrorHandler
		//   {
		//       void ShowMessage(char const* str) { ::MessageBoxA(::GetFocus(), str, "Script Error", MB_OK); }
		//   };
		struct IErrorHandler
		{
			virtual ~IErrorHandler() {}
			virtual void IErrorHandler_ShowMessage(char const*) {}
			virtual void IErrorHandler_Error(pr::script::EResult result, char const* error_msg, pr::script::Loc const& loc)
			{
				// Report a basic error message.
				// To implement script history, users should use a history char stream and include that in reported messages.
				IErrorHandler_ShowMessage(pr::script::ErrMsg(result, error_msg, loc).c_str());
				throw pr::script::Exception(result, loc, error_msg);
			}
		};

		// pr script reader
		struct Reader
		{
		private:
			PPMacroDB       m_dft_macros;
			FileIncludes    m_dft_includes;
			IErrorHandler   m_dft_errors;
			Preprocessor    m_pp;
			CommentStrip    m_strip;
			Src&            m_src;
			IErrorHandler*  m_error_handler;
			char const*     m_delim;
			bool            m_case_sensitive_keywords;

			// Notes:
			// - There is no default embedded code handler because typically you'd want to share
			//   the code handling object across many instances of the script reader

			Reader(Reader const&); // no copying
			Reader& operator=(Reader const&);
			void EatWhiteSpace() { for (; *pr::str::FindChar(m_delim, *m_src) != 0; ++m_src) {} }

		public:
			Reader(bool case_sensitive_keywords = false)
				:m_dft_macros()
				,m_dft_includes()
				,m_dft_errors()
				,m_pp(&m_dft_macros, &m_dft_includes, 0)
				,m_strip(m_pp)
				,m_src(m_strip)
				,m_error_handler(&m_dft_errors)
				,m_delim(" \t\r\n\v,;")
				,m_case_sensitive_keywords(case_sensitive_keywords)
			{}
			Reader(Src& src, bool case_sensitive_keywords = false) :Reader(case_sensitive_keywords)
			{
				AddSource(src);
			}
			Reader(Src* src, bool delete_on_pop, bool case_sensitive_keywords = false) :Reader(case_sensitive_keywords)
			{
				AddSource(src, delete_on_pop);
			}

			// Interface pointers
			// Set these for non-default handling of macros, includes, embedded code, or error reporting
			IPPMacroDB*&    MacroHandler()   { return m_pp.m_macros; }
			IIncludes*&     IncludeHandler() { return m_pp.m_includes; }
			IEmbeddedCode*& CodeHandler()    { return m_pp.m_embedded; }
			IErrorHandler*& ErrorHandler()   { return m_error_handler; }

			// User delimiter characters
			char const*& Delimiters() { return m_delim; }

			// Push a source onto the input stack
			// Note: specific overloads that add strings or files are not included
			// as these require allocation. The client should create and control
			// instances of sources derived from 'Src'
			void AddSource(Src* src, bool delete_on_pop)
			{
				m_pp.push(src, delete_on_pop);
			}
			void AddSource(Src& src)
			{
				AddSource(&src, false);
			}

			// Get/Set whether keywords are case sensitive
			// If false (default), then all keywords are returned as lower case
			bool CaseSensitiveKeywords() const
			{
				return m_case_sensitive_keywords;
			}
			void CaseSensitiveKeywords(bool case_sensitive)
			{
				m_case_sensitive_keywords = case_sensitive;
			}

			// Return the hash of a keyword
			static pr::hash::HashValue HashKeyword(char const* keyword, bool case_sensitive_keywords)
			{
				pr::hash::HashValue kw = case_sensitive_keywords ? pr::hash::HashC(keyword) : pr::hash::HashLwr(keyword);
				return (kw & 0x7fffffff); // mask off msb so that enum's show up in the debugger
			}

			// Return the hash of a keyword using the current reader settings
			pr::hash::HashValue HashKeyword(char const* keyword) const
			{
				return HashKeyword(keyword, m_case_sensitive_keywords);
			}

			// Return true if the end of the source has been reached
			bool IsSourceEnd()
			{
				EatWhiteSpace();
				return *m_src == 0;
			}

			// Return true if the next token is a keyword
			bool IsKeyword()
			{
				EatWhiteSpace();
				return *m_src == '*';
			}

			// Return true if the next non-whitespace character is the start/end of a section
			bool IsSectionStart()
			{
				EatWhiteSpace();
				return *m_src == '{';
			}
			bool IsSectionEnd()
			{
				EatWhiteSpace();
				return *m_src == '}';
			}

			// Move to the start/end of a section and then one past it
			bool SectionStart()
			{
				if (IsSectionStart()) { ++m_src; return true; }
				return ReportError(EResult::TokenNotFound, "expected '{'");
			}
			bool SectionEnd()
			{
				if (IsSectionEnd()) { ++m_src; return true; }
				return ReportError(EResult::TokenNotFound, "expected '}'");
			}

			// Move to the start of the next line
			bool NewLine()
			{
				Eat::Line(m_src, false);
				if (pr::str::IsNewLine(*m_src)) ++m_src; else return false;
				return true;
			}

			// Advance the source to the next '{' within the current scope
			// On return the current position should be a section start character
			// or the end of the current section or end of the input stream if not found
			bool FindSectionStart()
			{
				for (;*m_src && *m_src != '{' && *m_src != '}';)
				{
					if (*m_src == '\"') { Eat::LiteralString(m_src); continue; }
					else ++m_src;
				}
				return *m_src == '{';
			}

			// Advance the source to the end of the current section
			// On return the current position should be the section end character
			// or the end of the input stream (if called from file scope).
			bool FindSectionEnd()
			{
				for (int nest = IsSectionStart() ? 0 : 1; *m_src;)
				{
					if (*m_src == '\"') { Eat::LiteralString(m_src); continue; }
					nest += int(*m_src == '{');
					nest -= int(*m_src == '}');
					if (nest == 0) break;
					++m_src;
				}
				return *m_src == '}';
			}

			// Scans forward until a keyword identifier is found within the current scope.
			// Non-keyword tokens are skipped. If a section is found it is skipped.
			// If a keyword is found, the source is position at the next character after the keyword
			// Returns true if a keyword is found, false otherwise.
			template <typename StrType> bool NextKeywordS(StrType& kw)
			{
				for (;*m_src && *m_src != '}' && *m_src != '*';)
				{
					if (*m_src == '\"') { Eat::LiteralString(m_src); continue; }
					if (*m_src == '{')  { m_src += FindSectionEnd(); continue; }
					++m_src;
				}
				if (*m_src == '*') ++m_src; else return false;
				if (m_case_sensitive_keywords) return pr::str::ExtractIdentifier(kw, m_src, m_delim);
				else { TxfmSrc src(m_src, ::tolower); return pr::str::ExtractIdentifier(kw, src, m_delim); }
			}

			// As above except the hash of the keyword is returned instead (converted to an enum value)
			template <typename Enum> bool NextKeywordH(Enum& enum_kw)
			{
				string kw;
				if (!NextKeywordS(kw)) return false;
				enum_kw = static_cast<Enum>(HashKeyword(kw.c_str()));
				return true;
			}

			// As above an error is reported if the next token is not a keyword
			pr::hash::HashValue NextKeywordH()
			{
				pr::hash::HashValue kw = 0;
				if (!NextKeywordH(kw)) ReportError(EResult::TokenNotFound, "keyword expected");
				return kw;
			}

			// As above an error is reported if the next token is not a keyword
			template <typename Enum> Enum NextKeywordH()
			{
				return static_cast<Enum>(NextKeywordH());
			}

			// Scans forward until a keyword matching 'named_kw' is found within the current scope.
			// Returns false if the named keyword is not found, true if it is
			bool FindNextKeyword(char const* named_kw)
			{
				pr::hash::HashValue kw_hashed, named_kw_hashed = HashKeyword(named_kw);
				for (;NextKeywordH(kw_hashed) && kw_hashed != named_kw_hashed;) {}
				return named_kw_hashed == kw_hashed;
			}

			// Extract a token from the source.
			// A token is a contiguous block of non-separater characters
			template <typename StrType> bool ExtractToken(StrType& token)
			{
				if (pr::str::ExtractToken(token, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "token expected");
			}
			template <typename StrType> bool ExtractTokenS(StrType& token)
			{
				return SectionStart() && ExtractToken(token) && SectionEnd();
			}
			template <typename StrType> bool ExtractToken(StrType& token, char const* delim)
			{
				std::string sep = m_delim; sep += delim;
				if (pr::str::ExtractToken(token, m_src, sep.c_str())) return true;
				return ReportError(EResult::TokenNotFound, "token expected");
			}
			template <typename StrType> bool ExtractTokenS(StrType& token, char const* delim)
			{
				return SectionStart() && ExtractToken(token, delim) && SectionEnd();
			}

			// Extract an identifier from the source.
			// An identifier is one of (A-Z,a-z,'_') followed by (A-Z,a-z,'_',0-9) in a contiguous block
			template <typename StrType> bool ExtractIdentifier(StrType& word)
			{
				if (pr::str::ExtractIdentifier(word, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "identifier expected");
			}
			template <typename StrType> bool ExtractIdentifierS(StrType& word)
			{
				return SectionStart() && ExtractIdentifier(word) && SectionEnd();
			}

			// Extract identifiers from the source separated by 'sep'
			template <typename StrType> bool ExtractIdentifiers(char, StrType& word)
			{
				if (pr::str::ExtractIdentifier(word, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "identifier expected");
			}
			template <typename StrType, typename... StrTypes> bool ExtractIdentifiers(char sep, StrType& word, StrTypes&&... words)
			{
				if (!pr::str::ExtractIdentifier(word, m_src, m_delim)) return ReportError(EResult::TokenNotFound, "identifier expected");
				if (*m_src == sep) ++m_src; else return ReportError(EResult::TokenNotFound, "identifier separator expected");
				return ExtractIdentifiers(sep, std::forward<StrTypes>(words)...);
			}
			template <typename StrType, typename... StrTypes> bool ExtractIdentifiersS(char sep, StrType& word, StrTypes&&... words)
			{
				return SectionStart() && ExtractIdentifiers(sep,word,std::forward<StrTypes>(words)...) && SectionEnd();
			}

			// Extract a string from the source.
			// A string is a sequence of characters between quotes.
			template <typename StrType> bool ExtractString(StrType& string)
			{
				if (pr::str::ExtractString(string, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "string expected");
			}
			template <typename StrType> bool ExtractStringS(StrType& string)
			{
				return SectionStart() && ExtractString(string) && SectionEnd();
			}

			// Extract a C-style string from the source.
			template <typename StrType> bool ExtractCString(StrType& cstring)
			{
				if (pr::str::ExtractCString(cstring, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "cstring expected");
			}
			template <typename StrType> bool ExtractCStringS(StrType& cstring)
			{
				return SectionStart() && ExtractCString(cstring) && SectionEnd();
			}

			// Extract a bool from the source.
			template <typename Bool> bool ExtractBool(Bool& bool_)
			{
				if (pr::str::ExtractBool(bool_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "bool expected");
			}
			template <typename Bool> bool ExtractBoolS(Bool& bool_)
			{
				return SectionStart() && ExtractBool(bool_) && SectionEnd();
			}

			// Extract an integral type from the source.
			template <typename Int> bool ExtractInt(Int& int_, int radix)
			{
				if (pr::str::ExtractInt(int_, radix, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "integral expected");
			}
			template <typename Int> bool ExtractIntS(Int& int_, int radix)
			{
				return SectionStart() && ExtractInt(int_, radix) && SectionEnd();
			}

			// Extract an enum value from the source.
			template <typename Enum> bool ExtractEnumValue(Enum& enum_)
			{
				if (pr::str::ExtractEnumValue(enum_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "enum integral value expected");
			}
			template <typename Enum> bool ExtractEnumValueS(Enum& enum_)
			{
				return SectionStart() && ExtractEnum(enum_) && SectionEnd();
			}

			// Extract an enum identifier from the source.
			template <typename Enum> bool ExtractEnum(Enum& enum_)
			{
				if (pr::str::ExtractEnum(enum_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "enum member string name expected");
			}
			template <typename Enum> bool ExtractEnumS(Enum& enum_)
			{
				return SectionStart() && ExtractEnum(enum_) && SectionEnd();
			}

			// Extract a real from the source.
			template <typename Real> bool ExtractReal(Real& real_)
			{
				if (pr::str::ExtractReal(real_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "real expected");
			}
			template <typename Real> bool ExtractRealS(Real& real_)
			{
				return SectionStart() && ExtractReal(real_) && SectionEnd();
			}

			// Extract an array of bools from the source.
			template <typename Bool> bool ExtractBoolArray(Bool* bools, std::size_t num_bools)
			{
				while (num_bools--) if (!ExtractBool(*bools++)) return false;
				return true;
			}
			template <typename Bool> bool ExtractBoolArrayS(Bool* bools, std::size_t num_bools)
			{
				return SectionStart() && ExtractBoolArray(bools, num_bools) && SectionEnd();
			}

			// Extract an array of integral types from the source.
			template <typename Int> bool ExtractIntArray(Int* ints, std::size_t num_ints, int radix)
			{
				while (num_ints--) if (!ExtractInt(*ints++, radix)) return false;
				return true;
			}
			template <typename Int> bool ExtractIntArrayS(Int* ints, std::size_t num_ints, int radix)
			{
				return SectionStart() && ExtractIntArray(ints, num_ints, radix) && SectionEnd();
			}

			// Extract an array of reals from the source.
			template <typename Real> bool ExtractRealArray(Real* reals, std::size_t num_reals)
			{
				while (num_reals--) if (!ExtractReal(*reals++)) return false;
				return true;
			}
			template <typename Real> bool ExtractRealArrayS(Real* reals, std::size_t num_reals)
			{
				return SectionStart() && ExtractRealArray(reals, num_reals) && SectionEnd();
			}

			// Extract a vector from the source
			bool ExtractVector2(pr::v2& vector)
			{
				return ExtractReal(vector.x) && ExtractReal(vector.y);
			}
			bool ExtractVector2S(pr::v2& vector)
			{
				return SectionStart() && ExtractVector2(vector) && SectionEnd();
			}

			// Extract a vector from the source
			bool ExtractVector3(pr::v4& vector, float w)
			{
				vector.w = w;
				return ExtractReal(vector.x) && ExtractReal(vector.y) && ExtractReal(vector.z);
			}
			bool ExtractVector3S(pr::v4& vector, float w)
			{
				return SectionStart() && ExtractVector3(vector, w) && SectionEnd();
			}

			// Extract a vector from the source
			bool ExtractVector4(pr::v4& vector)
			{
				return ExtractReal(vector.x) && ExtractReal(vector.y) && ExtractReal(vector.z) && ExtractReal(vector.w);
			}
			bool ExtractVector4S(pr::v4& vector)
			{
				return SectionStart() && ExtractVector4(vector) && SectionEnd();
			}

			// Extract a quaternion from the source
			bool ExtractQuaternion(pr::Quat& quaternion)
			{
				return ExtractReal(quaternion.x) && ExtractReal(quaternion.y) && ExtractReal(quaternion.z) && ExtractReal(quaternion.w);
			}
			bool ExtractQuaternionS(pr::Quat& quaternion)
			{
				return SectionStart() && ExtractQuaternion(quaternion) && SectionEnd();
			}

			// Extract a 3x3 matrix from the source
			bool ExtractMatrix3x3(pr::m3x4& transform)
			{
				return ExtractVector3(transform.x,0.0f) && ExtractVector3(transform.y,0.0f) && ExtractVector3(transform.z,0.0f);
			}
			bool ExtractMatrix3x3S(pr::m3x4& transform)
			{
				return SectionStart() && ExtractMatrix3x3(transform) && SectionEnd();
			}

			// Extract a 4x4 matrix from the source
			bool ExtractMatrix4x4(pr::m4x4& transform)
			{
				return ExtractVector4(transform.x) && ExtractVector4(transform.y) && ExtractVector4(transform.z) && ExtractVector4(transform.w);
			}
			bool ExtractMatrix4x4S(pr::m4x4& transform)
			{
				return SectionStart() && ExtractMatrix4x4(transform) && SectionEnd();
			}

			// Extract a byte array
			bool ExtractData(void* data, std::size_t length)
			{
				return ExtractIntArray(static_cast<unsigned char*>(data), length, 16);
			}
			bool ExtractDataS(void* data, std::size_t length)
			{
				return SectionStart() && ExtractData(data, length) && SectionEnd();
			}

			// Extract a complete section as a preprocessed string.
			// Note: To embed arbitrary text in a script use #lit/#end and then
			// ExtractSection rather than ExtractLiteralSection.
			// Assumes an stl-like string interface, use 'pr::str::fixed_buffer' if necessary
			template <typename String> bool ExtractSection(String& str, bool include_braces)
			{
				str.resize(0);
				if (IsSectionStart()) ++m_src; else return ReportError(EResult::TokenNotFound, "expected '{'");
				if (include_braces) str.push_back('{');
				for (int nest = 1; *m_src; ++m_src)
				{
					// While the preprocessor has characters buffered we shouldn't be testing for '}'.
					// The buffered characters will be those within a literal string, literal char, or #lit/#end section.
					if (m_pp.input_buffered()) { str.push_back(*m_src); continue; }
					nest += int(*m_src == '{');
					nest -= int(*m_src == '}');
					if (nest == 0) break;
					str.push_back(*m_src);
				}
				if (include_braces) str.push_back('}');
				if (IsSectionEnd()) ++m_src; else return ReportError(EResult::TokenNotFound, "expected '}'");
				return true;
			}

			// Allow extension methods. e.g:
			// template <> bool pr::script::Reader::Extract<MyType>(MyType& my_type)
			// { return ExtractInt(my_type.int, 10); // etc }
			template <typename Type> bool Extract(Type& type);
			template <typename Type> bool ExtractS(Type& type)
			{
				return SectionStart() && ExtractS(type) && SectionEnd();
			}

			// Allow users to report errors via the internal error handler
			bool ReportError(EResult result)
			{
				if (m_error_handler) m_error_handler->IErrorHandler_Error(result, pr::ToString(result), m_src.loc());
				return false;
			}
			bool ReportError(EResult result, char const* msg)
			{
				if (m_error_handler) m_error_handler->IErrorHandler_Error(result, msg, m_src.loc());
				return false;
			}
			bool ReportError(char const* msg)
			{
				if (m_error_handler) m_error_handler->IErrorHandler_Error(EResult::Failed, msg, m_src.loc());
				return false;
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_reader)
		{
			char const* src =
				"#define NUM 23\n"
				"*Identifier ident\n"
				"*String \"simple string\"\n"
				"*CString \"C:\\\\Path\\\\Filename.txt\"\n"
				"*Bool true\n"
				"*Intg -NUM\n"
				"*Intg16 ABCDEF00\n"
				"*Real -2.3e+3\n"
				"*BoolArray 1 0 true false\n"
				"*IntArray -3 2 +1 -0\n"
				"*RealArray 2.3 -1.0e-1 2 -0.2\n"
				"*Vector3 1.0 2.0 3.0\n"
				"*Vector4 4.0 3.0 2.0 1.0\n"
				"*Quaternion 0.0 -1.0 -2.0 -3.0\n"
				"*M3x3 1.0 0.0 0.0  0.0 1.0 0.0  0.0 0.0 1.0\n"
				"*M4x4 1.0 0.0 0.0 0.0  0.0 1.0 0.0 0.0  0.0 0.0 1.0 0.0  0.0 0.0 0.0 1.0\n"
				"*Data 41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 00\n"
				"*Junk\n"
				"*Section {*SubSection { *Data \n NUM \"With a }\\\"string\\\"{ in it\" }}    \n"
				"*Section {*SubSection { *Data \n NUM \"With a }\\\"string\\\"{ in it\" }}    \n"
				"*Token 123token\n"
				"*LastThing";

			char kw[50];
			pr::hash::HashValue hashed_kw = 0;
			std::string str;
			bool bval = false, barray[4];
			int ival = 0, iarray[4];
			unsigned int uival = 0;
			float fval = 0.0f, farray[4];
			pr::v4 vec = pr::v4Zero;
			pr::Quat quat = pr::QuatIdentity;
			pr::m3x4 mat3;
			pr::m4x4 mat4;

			{// basic extract methods
				pr::script::Loc    loc;
				pr::script::PtrSrc ptr(src, &loc);
				pr::script::Reader reader(ptr, true);

				PR_CHECK(reader.CaseSensitiveKeywords()     ,true);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Identifier"                  );
				PR_CHECK(reader.ExtractIdentifier(str)      ,true); PR_CHECK(str             , "ident"                       );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "String"                      );
				PR_CHECK(reader.ExtractString(str)          ,true); PR_CHECK(str             , "simple string"               );
				PR_CHECK(reader.NextKeywordH(hashed_kw)     ,true); PR_CHECK(hashed_kw       , reader.HashKeyword("CString") );
				PR_CHECK(reader.ExtractCString(str)         ,true); PR_CHECK(str             , "C:\\Path\\Filename.txt"      );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Bool"                        );
				PR_CHECK(reader.ExtractBool(bval)           ,true); PR_CHECK(bval            , true                          );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Intg"                        );
				PR_CHECK(reader.ExtractInt(ival, 10)        ,true); PR_CHECK(ival            , -23                           );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Intg16"                      );
				PR_CHECK(reader.ExtractInt(uival, 16)       ,true); PR_CHECK(uival           , 0xABCDEF00                    );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Real"                        );
				PR_CHECK(reader.ExtractReal(fval)           ,true); PR_CHECK(fval            , -2.3e+3                       );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "BoolArray"                   );
				PR_CHECK(reader.ExtractBoolArray(barray, 4) ,true);
				PR_CHECK(barray[0], true );
				PR_CHECK(barray[1], false);
				PR_CHECK(barray[2], true );
				PR_CHECK(barray[3], false);
				PR_CHECK(reader.NextKeywordS(kw), true);             PR_CHECK(std::string(kw) , "IntArray");
				PR_CHECK(reader.ExtractIntArray(iarray, 4, 10), true);
				PR_CHECK(iarray[0], -3);
				PR_CHECK(iarray[1], +2);
				PR_CHECK(iarray[2], +1);
				PR_CHECK(iarray[3], -0);
				PR_CHECK(reader.NextKeywordS(kw), true);             PR_CHECK(std::string(kw) , "RealArray");
				PR_CHECK(reader.ExtractRealArray(farray, 4), true);
				PR_CHECK(farray[0], 2.3f    );
				PR_CHECK(farray[1], -1.0e-1f);
				PR_CHECK(farray[2], +2.0f   );
				PR_CHECK(farray[3], -0.2f   );
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "Vector3");
				PR_CHECK(reader.ExtractVector3(vec,-1.0f) , true); PR_CHECK(pr::FEql4(vec, pr::v4::make(1.0f, 2.0f, 3.0f,-1.0f)), true);
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "Vector4");
				PR_CHECK(reader.ExtractVector4(vec)       , true); PR_CHECK(pr::FEql4(vec, pr::v4::make(4.0f, 3.0f, 2.0f, 1.0f)), true);
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "Quaternion");
				PR_CHECK(reader.ExtractQuaternion(quat)   , true); PR_CHECK(pr::FEql4(quat, pr::Quat::make(0.0f, -1.0f, -2.0f, -3.0f)), true);
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "M3x3");
				PR_CHECK(reader.ExtractMatrix3x3(mat3)    , true); PR_CHECK(pr::FEql(mat3, pr::m3x4Identity), true);
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "M4x4");
				PR_CHECK(reader.ExtractMatrix4x4(mat4)    , true); PR_CHECK(pr::FEql(mat4, pr::m4x4Identity), true);
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "Data");
				PR_CHECK(reader.ExtractData(kw, 16)       , true); PR_CHECK(std::string(kw) , "ABCDEFGHIJKLMNO");
				PR_CHECK(reader.FindNextKeyword("Section"), true); str.resize(0);
				PR_CHECK(reader.ExtractSection(str, false), true); PR_CHECK(str, "*SubSection { *Data \n 23 \"With a }\\\"string\\\"{ in it\" }");
				PR_CHECK(reader.FindNextKeyword("Section"), true); str.resize(0);
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "Token");
				PR_CHECK(reader.ExtractToken(str)         , true); PR_CHECK(str, "123token");
				PR_CHECK(reader.NextKeywordS(kw)          , true); PR_CHECK(std::string(kw) , "LastThing");
				PR_CHECK(!reader.IsKeyword()              , true);
				PR_CHECK(!reader.IsSectionStart()         , true);
				PR_CHECK(!reader.IsSectionEnd()           , true);
				PR_CHECK(reader.IsSourceEnd()             , true);
			}
			{
				char const* src =
					"A.B\n"
					"a.b.c\n"
					"A.B.C.D\n"
					;

				pr::script::Loc    loc;
				pr::script::PtrSrc ptr(src, &loc);
				pr::script::Reader reader(ptr, true);

				std::string s0,s1,s2,s3;
				reader.ExtractIdentifiers('.',s0,s1);        PR_CHECK(s0 == "A" && s1 == "B", true);
				reader.ExtractIdentifiers('.',s0,s1,s2);     PR_CHECK(s0 == "a" && s1 == "b" && s2 == "c", true);
				reader.ExtractIdentifiers('.',s0,s1,s2,s3);  PR_CHECK(s0 == "A" && s1 == "B" && s2 == "C" && s3 == "D", true);
			}
		}
	}
}
#endif

#endif
