//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/script_core.h"
#include "pr/script2/preprocessor.h"
#include "pr/script2/fail_policy.h"

namespace pr
{
	namespace script2
	{
		// Reader - Reads pr script from a stream of wchar_t's
		template
		<
			typename FailPolicy = ThrowOnFailure,
			typename Source = Preprocessor<FailPolicy>
		>
		struct Reader
		{
		private:

			//Source
			Preprocessor<> m_src;
			char const* m_delim;
			bool m_case_sensitive;

		public:

			explicit Reader(bool case_sensitive = true)
				:m_src()
				,m_delim(" \t\r\n\v,;")
				,m_case_sensitive(case_sensitive)
			{}
			explicit Reader(Src& src, bool case_sensitive = true)
				:Reader(case_sensitive)
			{
				AddSource(src);
			}
			explicit Reader(Src* src, bool delete_on_pop, bool case_sensitive = true)
				:Reader(case_sensitive)
			{
				AddSource(src, delete_on_pop);
			}
			explicit Reader(char const* ptr, bool case_sensitive = true)
				:Reader(case_sensitive)
			{
				auto src = std::make_unique<PtrA<>>(ptr);
				AddSource(src.get(), true);
				src.release();
			}
			explicit Reader(wchar_t const* ptr, bool case_sensitive = true)
				:Reader(case_sensitive)
			{
				auto src = std::make_unique<PtrW<>>(ptr);
				AddSource(src.get(), true);
				src.release();
			}

			Reader(Reader&& rhs)
				:m_src(std::move(rhs.m_src))
				,m_delim(rhs.m_delim)
				,m_case_sensitive(rhs.m_case_sensitive)
			{}
			Reader(Reader const& rhs)
				:m_src(rhs.m_src)
				,m_delim(rhs.m_delim)
				,m_case_sensitive(rhs.m_case_sensitive)
			{}
			Reader& operator = (Reader&& rhs)
			{
				if (this != &rhs)
				{
					m_src = std::move(rhs.m_src);
					m_delim = rhs.m_delim;
					m_case_sensitive = rhs.m_case_sensitive;
				}
				return *this;
			}
			Reader& operator = (Reader const& rhs)
			{
				if (this != &rhs)
				{
					m_src = rhs.m_rhs;
					m_delim = rhs.m_delim;
					m_case_sensitive = rhs.m_case_sensitive;
				}
			}

			// Push a source onto the input stack
			void AddSource(Src& src)
			{
				m_src.Push(src);
			}
			void AddSource(Src* src, bool delete_on_pop)
			{
				m_src.Push(src, delete_on_pop);
			}

			// Get/Set delimiter characters
			char const* Delimiters() const
			{
				return m_delim;
			}
			void Delimiters(char const* delim)
			{
				m_delim = delim;
			}

			// Get/Set case sensitive keywords on/off
			bool CaseSensitive() const { return m_case_sensitive; }
			void CaseSensitive(bool cs) { m_case_sensitive = cs; }

			// Return the hash of a keyword using the current reader settings
			static HashValue HashKeyword(wchar_t const* keyword, bool case_sensitive)
			{
				auto kw = case_sensitive ? Hash(keyword) : HashLwr(keyword);
				return kw;
				//return (kw & 0x7fffffff); // mask off msb so that enum's show up in the debugger
			}
			HashValue HashKeyword(wchar_t const* keyword) const
			{
				return HashKeyword(keyword, m_case_sensitive);
			}

			// Return true if the end of the source has been reached
			bool IsSourceEnd()
			{
				EatWhiteSpace(m_src, 0, 0);
				return *m_src == 0;
			}

			// Return true if the next token is a keyword
			bool IsKeyword()
			{
				EatWhiteSpace(m_src, 0, 0);
				return *m_src == '*';
			}

			// Returns true if the next non-whitespace character is the start/end of a section
			bool IsSectionStart()
			{
				EatDelimiters(m_src, m_delim);
				return *m_src == L'{';
			}
			bool IsSectionEnd()
			{
				EatDelimiters(m_src, m_delim);
				return *m_src == L'}';
			}

			// Move to the start/end of a section and then one past it
			bool SectionStart()
			{
				if (IsSectionStart()) { ++m_src; return true; }
				return FailPolicy::Fail(EResult::TokenNotFound, m_src.Loc(), "expected '{'");
			}
			bool SectionEnd()
			{
				if (IsSectionEnd()) { ++m_src; return true; }
				return FailPolicy::Fail(EResult::TokenNotFound, m_src.Loc(), "expected '}'");
			}

			// Move to the start of the next line
			bool NewLine()
			{
				EatLine(m_src, 0, 0);
				if (pr::str::IsNewLine(*m_src)) ++m_src; else return false;
				return true;
			}

			// Advance the source to the next '{' within the current scope
			// On return the current position should be a section start character
			// or the end of the current section or end of the input stream if not found
			bool FindSectionStart()
			{
				for (;*m_src && *m_src != L'{' && *m_src != L'}';)
				{
					if (*m_src == L'\"') { EatLiteralString(m_src); continue; }
					else ++m_src;
				}
				return *m_src == L'{';
			}

			// Advance the source to the end of the current section
			// On return the current position should be the section end character
			// or the end of the input stream (if called from file scope).
			bool FindSectionEnd()
			{
				for (int nest = IsSectionStart() ? 0 : 1; *m_src;)
				{
					if (*m_src == L'\"') { EatLiteralString(m_src); continue; }
					nest += int(*m_src == L'{');
					nest -= int(*m_src == L'}');
					if (nest == 0) break;
					++m_src;
				}
				return *m_src == L'}';
			}

			// Scans forward until a keyword identifier is found within the current scope.
			// Non-keyword tokens are skipped. If a section is found it is skipped.
			// If a keyword is found, the source is position at the next character after the keyword
			// Returns true if a keyword is found, false otherwise.
			template <typename StrType> bool NextKeywordS(StrType& kw)
			{
				for (;*m_src && *m_src != L'}' && *m_src != L'*';)
				{
					if (*m_src == L'\"') { EatLiteralString(m_src); continue; }
					if (*m_src == L'{')  { m_src += FindSectionEnd(); continue; }
					++m_src;
				}
				if (*m_src == L'*') ++m_src; else return false;
				pr::str::Resize(kw, 0);
				if (!pr::str::ExtractIdentifier(kw, m_src, m_delim)) return false;
				if (!m_case_sensitive) pr::str::LowerCase(kw);
				return true;
			}

			// As above except the hash of the keyword is returned instead (converted to an enum value)
			template <typename Enum> bool NextKeywordH(Enum& enum_kw)
			{
				pr::string<wchar_t> kw;
				if (!NextKeywordS(kw)) return false;
				enum_kw = static_cast<Enum>(HashKeyword(kw.c_str()));
				return true;
			}

			// As above an error is reported if the next token is not a keyword
			HashValue NextKeywordH()
			{
				HashValue kw = 0;
				if (!NextKeywordH(kw)) ReportError(EResult::TokenNotFound, m_src.Loc(), "keyword expected");
				return kw;
			}
			template <typename Enum> Enum NextKeywordH()
			{
				return Enum(NextKeywordH());
			}

			// Scans forward until a keyword matching 'named_kw' is found within the current scope.
			// Returns false if the named keyword is not found, true if it is
			bool FindNextKeyword(wchar_t const* named_kw)
			{
				auto kw_hashed = HashValue();
				auto named_kw_hashed = HashKeyword(named_kw);
				for (; NextKeywordH(kw_hashed) && kw_hashed != named_kw_hashed; ) {}
				return named_kw_hashed == kw_hashed;
			}

			// Extract a token from the source.
			// A token is a contiguous block of non-separater characters
			template <typename StrType> bool Token(StrType& token)
			{
				pr::str::Resize(token, 0);
				if (pr::str::ExtractToken(token, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "token expected");
			}
			template <typename StrType> bool TokenS(StrType& token)
			{
				return SectionStart() && Token(token) && SectionEnd();
			}

			// Extract a token using additional delimiters
			template <typename StrType> bool Token(StrType& token, char const* delim)
			{
				pr::str::Resize(token, 0);
				if (pr::str::ExtractToken(token, m_src, std::string(m_delim).append(delim).c_str())) return true;
				return ReportError(EResult::TokenNotFound, "token expected");
			}
			template <typename StrType> bool TokenS(StrType& token, char const* delim)
			{
				return SectionStart() && Token(token, delim) && SectionEnd();
			}

			// Read an identifier from the source.
			// An identifier is one of (A-Z,a-z,'_') followed by (A-Z,a-z,'_',0-9) in a contiguous block
			template <typename StrType> bool Identifier(StrType& word)
			{
				pr::str::Resize(word, 0);
				if (pr::str::ExtractIdentifier(word, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "identifier expected");
			}
			template <typename StrType> bool IdentifierS(StrType& word)
			{
				return SectionStart() && Identifier(word) && SectionEnd();
			}

			// Extract identifiers from the source separated by 'sep'
			template <typename StrType> bool Identifiers(char sep, StrType& word)
			{
				(void)sep;
				pr::str::Resize(word, 0);
				if (pr::str::ExtractIdentifier(word, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "identifier expected");
			}
			template <typename StrType, typename... StrTypes> bool Identifiers(char sep, StrType& word, StrTypes&&... words)
			{
				pr::str::Resize(word, 0);
				if (!pr::str::ExtractIdentifier(word, m_src, m_delim)) return ReportError(EResult::TokenNotFound, "identifier expected");
				if (*m_src == sep) ++m_src; else return ReportError(EResult::TokenNotFound, "identifier separator expected");
				return Identifiers(sep, std::forward<StrTypes>(words)...);
			}
			template <typename StrType, typename... StrTypes> bool IdentifiersS(char sep, StrType& word, StrTypes&&... words)
			{
				return SectionStart() && Identifiers(sep,word,std::forward<StrTypes>(words)...) && SectionEnd();
			}

			// Extract a string from the source.
			// A string is a sequence of characters between quotes.
			template <typename StrType> bool String(StrType& string)
			{
				pr::str::Resize(string, 0);
				if (pr::str::ExtractString(string, m_src, 0, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "string expected");
			}
			template <typename StrType> bool StringS(StrType& string)
			{
				return SectionStart() && String(string) && SectionEnd();
			}

			// Extract a C-style string from the source.
			template <typename StrType> bool CString(StrType& cstring)
			{
				pr::str::Resize(cstring, 0);
				if (pr::str::ExtractString(cstring, m_src, '\\', m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "cstring expected");
			}
			template <typename StrType> bool CStringS(StrType& cstring)
			{
				return SectionStart() && CString(cstring) && SectionEnd();
			}

			// Extract a bool from the source.
			template <typename Boolean> bool Bool(Boolean& bool_)
			{
				if (pr::str::ExtractBool(bool_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "bool expected");
			}
			template <typename Boolean> bool BoolS(Boolean& bool_)
			{
				return SectionStart() && Bool(bool_) && SectionEnd();
			}
			template <typename Boolean> bool Bool(Boolean* bools, std::size_t num_bools)
			{
				while (num_bools--) if (!Bool(*bools++)) return false;
				return true;
			}
			template <typename Boolean> bool BoolS(Boolean* bools, std::size_t num_bools)
			{
				return SectionStart() && Bool(bools, num_bools) && SectionEnd();
			}

			// Extract an integral type from the source.
			template <typename Intg> bool Int(Intg& int_, int radix)
			{
				if (pr::str::ExtractInt(int_, radix, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "integral expected");
			}
			template <typename Intg> bool IntS(Intg& int_, int radix)
			{
				return SectionStart() && Int(int_, radix) && SectionEnd();
			}
			template <typename Intg> bool Int(Intg* ints, std::size_t num_ints, int radix)
			{
				while (num_ints--) if (!Int(*ints++, radix)) return false;
				return true;
			}
			template <typename Intg> bool IntS(Intg* ints, std::size_t num_ints, int radix)
			{
				return SectionStart() && Int(ints, num_ints, radix) && SectionEnd();
			}

			// Extract a real from the source.
			template <typename Float> bool Real(Float& real_)
			{
				if (pr::str::ExtractReal(real_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "real expected");
			}
			template <typename Float> bool RealS(Float& real_)
			{
				return SectionStart() && Real(real_) && SectionEnd();
			}
			template <typename Float> bool Real(Float* reals, std::size_t num_reals)
			{
				while (num_reals--) if (!Real(*reals++)) return false;
				return true;
			}
			template <typename Float> bool RealS(Float* reals, std::size_t num_reals)
			{
				return SectionStart() && Real(reals, num_reals) && SectionEnd();
			}

			// Extract an enum value from the source.
			template <typename Enum> bool EnumValue(Enum& enum_)
			{
				if (pr::str::ExtractEnumValue(enum_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "enum integral value expected");
			}
			template <typename Enum> bool EnumValueS(Enum& enum_)
			{
				return SectionStart() && EnumValue(enum_) && SectionEnd();
			}

			// Extract an enum identifier from the source.
			template <typename Enum> bool Enum(Enum& enum_)
			{
				if (pr::str::ExtractEnum(enum_, m_src, m_delim)) return true;
				return ReportError(EResult::TokenNotFound, "enum member string name expected");
			}
			template <typename Enum> bool EnumS(Enum& enum_)
			{
				return SectionStart() && Enum(enum_) && SectionEnd();
			}

			// Extract a vector from the source
			bool Vector2(pr::v2& vector)
			{
				return Real(vector.x) && Real(vector.y);
			}
			bool Vector2S(pr::v2& vector)
			{
				return SectionStart() && Vector2(vector) && SectionEnd();
			}

			// Extract a vector from the source
			bool Vector3(pr::v4& vector, float w)
			{
				vector.w = w;
				return Real(vector.x) && Real(vector.y) && Real(vector.z);
			}
			bool Vector3S(pr::v4& vector, float w)
			{
				return SectionStart() && Vector3(vector, w) && SectionEnd();
			}

			// Extract a vector from the source
			bool Vector4(pr::v4& vector)
			{
				return Real(vector.x) && Real(vector.y) && Real(vector.z) && Real(vector.w);
			}
			bool Vector4S(pr::v4& vector)
			{
				return SectionStart() && Vector4(vector) && SectionEnd();
			}

			// Extract a quaternion from the source
			bool Quaternion(pr::Quat& quaternion)
			{
				return Real(quaternion.x) && Real(quaternion.y) && Real(quaternion.z) && Real(quaternion.w);
			}
			bool QuaternionS(pr::Quat& quaternion)
			{
				return SectionStart() && Quaternion(quaternion) && SectionEnd();
			}

			// Extract a 3x3 matrix from the source
			bool Matrix3x3(pr::m3x4& transform)
			{
				return Vector3(transform.x,0.0f) && Vector3(transform.y,0.0f) && Vector3(transform.z,0.0f);
			}
			bool Matrix3x3S(pr::m3x4& transform)
			{
				return SectionStart() && Matrix3x3(transform) && SectionEnd();
			}

			// Extract a 4x4 matrix from the source
			bool Matrix4x4(pr::m4x4& transform)
			{
				return Vector4(transform.x) && Vector4(transform.y) && Vector4(transform.z) && Vector4(transform.w);
			}
			bool Matrix4x4S(pr::m4x4& transform)
			{
				return SectionStart() && Matrix4x4(transform) && SectionEnd();
			}

			// Extract a byte array
			bool Data(void* data, std::size_t length)
			{
				return Int(static_cast<unsigned char*>(data), length, 16);
			}
			bool DataS(void* data, std::size_t length)
			{
				return SectionStart() && Data(data, length) && SectionEnd();
			}

			// Extract a complete section as a preprocessed string.
			// Note: To embed arbitrary text in a script use #lit/#end and then Section()
			template <typename String> bool Section(String& str, bool include_braces)
			{
				// Do not str.resize(0) here, that's the callers decision
				if (IsSectionStart()) ++m_src; else return ReportError(EResult::TokenNotFound, "expected '{'");
				if (include_braces) pr::str::Append(str, L'{');
				for (int nest = 1; *m_src; ++m_src)
				{
					// While the preprocessor has characters buffered we shouldn't be testing for '}'.
					// The buffered characters will be those within a literal string, literal char, or #lit/#end section.
					if (m_src.src_buffered()) { pr::str::Append(str, *m_src); continue; }
					nest += int(*m_src == '{');
					nest -= int(*m_src == '}');
					if (nest == 0) break;
					pr::str::Append(str, *m_src);
				}
				if (include_braces) pr::str::Append(str, '}');
				if (IsSectionEnd()) ++m_src; else return ReportError(EResult::TokenNotFound, "expected '}'");
				return true;
			}

			// Allow extension methods. e.g:
			// template <> bool pr::script::Reader::Extract<MyType>(MyType& my_type) { return Int(my_type.int, 10); // etc }
			template <typename Type> bool Extract(Type& type);
			template <typename Type> bool ExtractS(Type& type)
			{
				return SectionStart() && ExtractS(type) && SectionEnd();
			}

			// Allow subclasses report errors
			virtual bool ReportError(EResult result, Location const& loc, char const* msg)
			{
				return FailPolicy::Fail(result, loc, msg), false;
			}
			bool ReportError(EResult result, char const* msg)
			{
				return FailPolicy::Fail(result, m_src.Loc(), msg), false;
			}
			bool ReportError(EResult result)
			{
				return FailPolicy::Fail(result, m_src.Loc(), msg.ToStringA()), false;
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
		PRUnitTest(pr_script2_reader)
		{
			using namespace pr::script2;

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
			HashValue hashed_kw = 0;
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
				Reader<> reader(src, true);
				PR_CHECK(reader.CaseSensitive()         ,true);
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Identifier"                  );
				PR_CHECK(reader.Identifier(str)         ,true); PR_CHECK(str             , "ident"                       );
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "String"                      );
				PR_CHECK(reader.String(str)             ,true); PR_CHECK(str             , "simple string"               );
				PR_CHECK(reader.NextKeywordH(hashed_kw) ,true); PR_CHECK(hashed_kw       , reader.HashKeyword(L"CString"));
				PR_CHECK(reader.CString(str)            ,true); PR_CHECK(str             , "C:\\Path\\Filename.txt"      );
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Bool"                        );
				PR_CHECK(reader.Bool(bval)              ,true); PR_CHECK(bval            , true                          );
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Intg"                        );
				PR_CHECK(reader.Int(ival, 10)           ,true); PR_CHECK(ival            , -23                           );
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Intg16"                      );
				PR_CHECK(reader.Int(uival, 16)          ,true); PR_CHECK(uival           , 0xABCDEF00                    );
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "Real"                        );
				PR_CHECK(reader.Real(fval)              ,true); PR_CHECK(fval            , -2.3e+3                       );
				PR_CHECK(reader.NextKeywordS(kw)        ,true); PR_CHECK(std::string(kw) , "BoolArray"                   );
				PR_CHECK(reader.Bool(barray, 4)         ,true);
				PR_CHECK(barray[0], true );
				PR_CHECK(barray[1], false);
				PR_CHECK(barray[2], true );
				PR_CHECK(barray[3], false);
				PR_CHECK(reader.NextKeywordS(kw)   ,true); PR_CHECK(std::string(kw) , "IntArray");
				PR_CHECK(reader.Int(iarray, 4, 10) ,true);
				PR_CHECK(iarray[0], -3);
				PR_CHECK(iarray[1], +2);
				PR_CHECK(iarray[2], +1);
				PR_CHECK(iarray[3], -0);
				PR_CHECK(reader.NextKeywordS(kw) ,true); PR_CHECK(std::string(kw) , "RealArray");
				PR_CHECK(reader.Real(farray, 4)  ,true);
				PR_CHECK(farray[0], 2.3f    );
				PR_CHECK(farray[1], -1.0e-1f);
				PR_CHECK(farray[2], +2.0f   );
				PR_CHECK(farray[3], -0.2f   );
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Vector3");
				PR_CHECK(reader.Vector3(vec,-1.0f)          ,true); PR_CHECK(pr::FEql4(vec, pr::v4::make(1.0f, 2.0f, 3.0f,-1.0f)), true);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Vector4");
				PR_CHECK(reader.Vector4(vec)                ,true); PR_CHECK(pr::FEql4(vec, pr::v4::make(4.0f, 3.0f, 2.0f, 1.0f)), true);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Quaternion");
				PR_CHECK(reader.Quaternion(quat)            ,true); PR_CHECK(pr::FEql4(quat, pr::Quat::make(0.0f, -1.0f, -2.0f, -3.0f)), true);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "M3x3");
				PR_CHECK(reader.Matrix3x3(mat3)             ,true); PR_CHECK(pr::FEql(mat3, pr::m3x4Identity), true);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "M4x4");
				PR_CHECK(reader.Matrix4x4(mat4)             ,true); PR_CHECK(pr::FEql(mat4, pr::m4x4Identity), true);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Data");
				PR_CHECK(reader.Data(kw, 16)                ,true); PR_CHECK(std::string(kw) , "ABCDEFGHIJKLMNO");
				PR_CHECK(reader.FindNextKeyword(L"Section") ,true); str.resize(0);
				PR_CHECK(reader.Section(str, false)         ,true); PR_CHECK(str, "*SubSection { *Data \n 23 \"With a }\\\"string\\\"{ in it\" }");
				PR_CHECK(reader.FindNextKeyword(L"Section") ,true); str.resize(0);
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "Token");
				PR_CHECK(reader.Token(str)                  ,true); PR_CHECK(str, "123token");
				PR_CHECK(reader.NextKeywordS(kw)            ,true); PR_CHECK(std::string(kw) , "LastThing");
				PR_CHECK(!reader.IsKeyword()                ,true);
				PR_CHECK(!reader.IsSectionStart()           ,true);
				PR_CHECK(!reader.IsSectionEnd()             ,true);
				PR_CHECK(reader.IsSourceEnd()               ,true);
			}
			{
				char const* src =
					"A.B\n"
					"a.b.c\n"
					"A.B.C.D\n"
					;
				std::string s0,s1,s2,s3;

				Reader<> reader(src);
				reader.Identifiers('.',s0,s1);        PR_CHECK(s0 == "A" && s1 == "B", true);
				reader.Identifiers('.',s0,s1,s2);     PR_CHECK(s0 == "a" && s1 == "b" && s2 == "c", true);
				reader.Identifiers('.',s0,s1,s2,s3);  PR_CHECK(s0 == "A" && s1 == "B" && s2 == "C" && s3 == "D", true);
			}
		}
	}
}
#endif
