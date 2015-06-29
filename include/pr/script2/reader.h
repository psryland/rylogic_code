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
			typename Source = Preprocessor<ThrowOnFailure>,
			typename FailPolicy = ThrowOnFailure
		>
		struct Reader
		{
		private:

			Source m_src;
			wchar_t const* m_delim;
			bool m_ignore_kw_case;

			// Advance 'm_src' to the next non-delimiter
			void EatWhiteSpace()
			{
				for (; *pr::str::FindChar(m_delim, *m_src) != 0; ++m_src) {}
			}

		public:

			Reader(wchar_t const* delim = L" \t\r\n\v,;", bool ignore_kw_case = false)
				:m_src()
				,m_delim(delim)
				,m_ignore_kw_case(ignore_kw_case)
			{}
			Reader(Reader&& rhs)
				:m_src(rhs.m_src)
				,m_delim(rhs.m_delim)
				,m_ignore_kw_case(rhs.m_ignore_kw_case)
			{}
			Reader(Reader const&) = delete;
			Reader& operator = (Reader const&) = delete;

			// Construct the reader with the source to read from
			template <typename Ptr> Reader(Ptr& ptr)
				:Reader()
			{
				AddSource(ptr);
			}

			// Push a source onto the input stack
			void AddSource(Src& src)
			{
				m_src.Push(src);
			}

			// Get/Set delimiter characters
			wchar_t const* Delimiters() const
			{
				return m_delim;
			}
			void Delimiters(wchar_t const* delim)
			{
				m_delim = delim;
			}

			// Returns true if the next non-whitespace character is the start/end of a section
			bool IsSectionStart()
			{
				EatWhiteSpace();
				return *m_src == L'{';
			}
			bool IsSectionEnd()
			{
				EatWhiteSpace();
				return *m_src == L'}';
			}

			// Move to the start/end of a section and then one past it
			bool SectionStart()
			{
				if (IsSectionStart()) { ++m_src; return true; }
				return FailPolicy::Fail(EResult::TokenNotFound, m_src.loc(), "expected '{'");
			}
			bool SectionEnd()
			{
				if (IsSectionEnd()) { ++m_src; return true; }
				return FailPolicy::Fail(EResult::TokenNotFound, m_src.loc(), "expected '}'");
			}

			// Read an identifier from the source.
			// An identifier is one of (A-Z,a-z,'_') followed by (A-Z,a-z,'_',0-9) in a contiguous block
			template <typename StrType> bool Identifier(StrType& word)
			{
				pr::str::Resize(word, 0);
				if (pr::str::ExtractIdentifier(word, m_src, m_delim)) return true;
				return FailPolicy::Fail(EResult::TokenNotFound, m_src.loc(), "identifier expected");
			}
			template <typename StrType> bool IdentifierS(StrType& word)
			{
				return SectionStart() && Identifier(word) && SectionEnd();
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

			char const* src = R"(
				// Line comment
				*Identifier ident
				)";

			Reader<> reader(src);
			reader.Identifier();
		}
#pragma region
#if 0
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
#endif
#pragma endregion
	}
}
#endif
