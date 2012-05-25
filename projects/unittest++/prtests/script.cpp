//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/maths/maths.h"
#include "pr/filesys/filesys.h"
#include "pr/script/reader.h"
#include "pr/script/embedded_lua.h"

using namespace pr;
using namespace pr::script;

SUITE(Script)
{
	TEST(KeywordHashcodes)
	{
		CHECK(ValidateKeywordHashcodes());
	}
	TEST(CharStream_StringSrc)
	{
		char const* str = "This is a stream of characters\n";
		PtrSrc src(str);
		for (;*str; ++str, ++src)
			CHECK_EQUAL(*str, *src);
		CHECK_EQUAL(0, *src);
	}
	TEST(CharStream_FileSrc)
	{
		char const* str = "This is a stream of characters\n";
		char const* filepath = "test_file_source.pr_script";
		{
			std::ofstream file(filepath);
			file << str;
		}
		{
			FileSrc src(filepath);
			for (; *str; ++str, ++src)
				CHECK_EQUAL(*str, *src);
			CHECK_EQUAL(0, *src);
		}
		{
			pr::filesys::EraseFile<std::string>(filepath);
			CHECK(!pr::filesys::FileExists(filepath));
		}
	}
	TEST(SrcStack)
	{
		char const* str1 = "one";
		char const* str2 = "two";
		PtrSrc src1(str1);
		PtrSrc src2(str2);
		SrcStack stack(src1);
		
		for (int i = 0; i != 2; ++i, ++stack)
			CHECK_EQUAL(str1[i], *stack);
		
		stack.push(src2);
		
		for (int i = 0; i != 3; ++i, ++stack)
			CHECK_EQUAL(str2[i], *stack);
		
		for (int i = 2; i != 3; ++i, ++stack)
			CHECK_EQUAL(str1[i], *stack);
		
		CHECK_EQUAL(0, *stack);
	}
	TEST(Buf8)
	{
		Buf8 _123("123");
		Buf8 _12345678("12345678");
		Buf8 _678("678");
	
		Buf8 buf;
		CHECK_EQUAL(0, buf.m_len);
		buf.push_back('1');
		buf.push_back('2');
		buf.push_back('3');
		CHECK_EQUAL(3, buf.size());
		CHECK(buf == _123);
		CHECK(buf != _12345678);
		buf.push_back('4');
		buf.push_back('5');
		buf.push_back('6');
		buf.push_back('7');
		buf.push_back('8');
		CHECK(buf == _12345678);
		CHECK_EQUAL('1', buf[0]); buf.pop_front();
		CHECK_EQUAL('2', buf[0]); buf.pop_front();
		CHECK_EQUAL('3', buf[0]); buf.pop_front();
		CHECK_EQUAL('4', buf[0]); buf.pop_front();
		CHECK_EQUAL('5', buf[0]); buf.pop_front();
		CHECK(buf == _678);
	}
	TEST(Buffer)
	{
		char const* str1 = "1234567890";
		PtrSrc src(str1);
		
		Buffer<> buf(src);             CHECK(buf.empty());
		CHECK_EQUAL('1', *buf);        CHECK(buf.empty());
		CHECK_EQUAL('1', buf[0]);      CHECK_EQUAL(1U, buf.size());
		CHECK_EQUAL('2', buf[1]);      CHECK_EQUAL(2U, buf.size());
		CHECK(buf.match(str1, 4));     CHECK_EQUAL(4u, buf.size());

		++buf;                         CHECK_EQUAL(3U, buf.size());
		CHECK_EQUAL('2', *buf);        CHECK_EQUAL(3U, buf.size());
		CHECK_EQUAL('2', buf[0]);      CHECK_EQUAL(3U, buf.size());
		CHECK_EQUAL('3', buf[1]);      CHECK_EQUAL(3U, buf.size());
		CHECK(buf.match(&str1[1], 4)); CHECK_EQUAL(4U, buf.size());
		CHECK(!buf.match("235"));      CHECK_EQUAL(4U, buf.size());

		buf += 4;                      CHECK(buf.empty());
		CHECK(!buf.match("6780"));     CHECK_EQUAL(4U, buf.size());
	}
	TEST(History)
	{
		char const* str_in = "12345678";
		PtrSrc src(str_in);
		History<4> hist(src);   CHECK(pr::str::Equal(hist.history(), ""));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "1"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "12"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "123"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "1234"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "2345"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "3456"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "4567"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "5678"));
		++hist;                 CHECK(pr::str::Equal(hist.history(), "5678")); // ++hist doesn't call next(), so the history isn't changed
	}
	TEST(TxfmSrc)
	{
		char const* str_in = "CaMeLCasE";
		char const* str_lwr = "camelcase";
		char const* str_upr = "CAMELCASE";
		{ // no change
			PtrSrc src(str_in);
			TxfmSrc nch(src);
			for (char const* out = str_in; *out; ++nch, ++out)
				CHECK_EQUAL(*out, *nch);
			CHECK_EQUAL(0, *nch);
		}
		{ // lower case
			PtrSrc src(str_in);
			TxfmSrc lwr(src, ::tolower);
			for (char const* out = str_lwr; *out; ++lwr, ++out)
				CHECK_EQUAL(*out, *lwr);
			CHECK_EQUAL(0, *lwr);
		}
		{ // upper case
			PtrSrc src(str_in);
			TxfmSrc upr(src);
			upr.set_transform(::toupper);
			for (char const* out = str_upr; *out; ++upr, ++out)
				CHECK_EQUAL(*out, *upr);
			CHECK_EQUAL(0, *upr);
		}
	}
	TEST(CommentStrip)
	{
		char const* str_in = 
			"123// comment         \n"
			"456/* block */789     \n"
			"// many               \n"
			"// lines              \n"
			"// \"string\"         \n"
			"/* \"string\" */      \n"
			"\"string \\\" /*a*/ //b\"  \n"
			"/not a comment\n"
			"/*\n"
			"  more lines\n"
			"*/\n";
		char const* str_out = 
			"123\n"
			"456789     \n"
			"\n"
			"\n"
			"\n"
			"      \n"
			"\"string \\\" /*a*/ //b\"  \n"
			"/not a comment\n"
			"\n";
		PtrSrc src(str_in);
		CommentStrip strip(src);
		for (;*str_out; ++strip, ++str_out)
			CHECK_EQUAL(*str_out, *strip);
		CHECK_EQUAL(0, *strip);
	}
	TEST(Preprocessor)
	{
		{// ignored stuff
			char const* str_in =
				"\"#if ignore #define this stuff\"\n"
				;
			char const* str_out =
				"\"#if ignore #define this stuff\"\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// simple macros
			char const* str_in =
				"#  define ONE 1 // ignore me \n"
				"#  define NOT_ONE (!ONE) /*and me*/ \n"
				"ONE\n"
				"NOT_ONE\n"
				;
			char const* str_out =
				"1\n"
				"(!1)\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// simple macro functions
			char const* str_in =
				"#\tdefine PLUS(x,y) \\\n"
				" (x)+(y) xx 0x _0x  \n"
				"PLUS  (1,(2,3))\n"
				;
			char const* str_out =
				"(1)+((2,3)) xx 01 _0x\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// recursive macros
			char const* str_in =
				"#define C(x) A(x) B(x) C(x)\n"
				"#define B(x) C(x)\n"
				"#define A(x) B(x)\n"
				"A(1)\n"
				;
			char const* str_out =
				"A(1) B(1) C(1)\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// #eval
			char const* str_in =
				"#eval{1+#eval{1+1}}\n"
				;
			char const* str_out =
				"3\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// recursive macros/evals
			char const* str_in =
				"#define X 3.0\n"
				"#define Y 4.0\n"
				"#define Len2 #eval{len2(X,Y)}\n"
				"#eval{X + Len2}\n";
			char const* str_out =
				"8\n";
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// includes
			char const* str_in =
				"#  define ONE 1 // ignore me \n"
				"#include \"inc\"\n"
				;
			char const* str_out =
				"included 1\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			StrIncludes includes; includes.m_strings["inc"] = "included ONE";
			Preprocessor pp(src, &macros, &includes, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// #if/#else/#etc
			char const* str_in =
				"#  define ONE 1 // ignore me \n"
				"#  define NOT_ONE (!ONE) /*and me*/ \n"
				"#\tdefine PLUS(x,y) (x)+(y) xx 0x _0x  \n"
				"#ifdef ZERO\n"
				"#if NESTED\n"
				"  not output \"ignore #else\" \n"
				"#endif\n"
				"#elif (!NOT_ONE) && defined(PLUS)\n"
				"  output\n"
				"#else\n"
				"  not output\n"
				"#endif\n"
				"#ifndef ZERO\n"
				"#if defined(ZERO) || defined(PLUS)\n"
				"  output this\n"
				"#else\n"
				"  but not this\n"
				"#endif\n"
				"#endif\n"
				"#undef ONE\n"
				"#ifdef ONE\n"
				"  don't output\n"
				"#endif\n"
				"#define TWO\n"
				"#ifdef TWO\n"
				"  two defined\n"
				"#endif\n"
				;
			char const* str_out =
				"  output\n"
				"  output this\n"
				"  two defined\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			Preprocessor pp(src, &macros, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// miscellaneous
			char const* str_in =
				"\"#error this would throw an error\"\n"
				"#pragma ignore this\n"
				"#line ignore this\n"
				"#warning ignore this\n"
				"lastword"
				"#define ONE 1\n"
				"#eval{ONE+2-4+len2(3,4)}\n"
				"#define EVAL(x) #eval{x+1}\n"
				"EVAL(1)\n"
				"#lit Any old ch*rac#ers #if I {feel} #include --cheese like #en#end\n"
				"// #if 1 comments \n"
				"/*should pass thru #else*/\n"
				"#embedded(lua) --lua code\n return \"hello world\" #end\n"
				;
			char const* str_out =
				"\"#error this would throw an error\"\n"
				"lastword"
				"4\n"
				"2\n"
				"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
				"// #if 1 comments \n"
				"/*should pass thru #else*/\n"
				"hello world\n"
				;
			PtrSrc src(str_in);
			PPMacroDB macros;
			StrIncludes includes; includes.m_strings["inc"] = "included ONE";
			EmbeddedLua lua_handler;
			Preprocessor pp(src, &macros, &includes, &lua_handler);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
		{// Preprocessor with no macro or include handler
			char const* str_in =
				"\t      \n"
				"\"#if ignore #define this stuff\"\n"
				"#  define ONE 1     \n"
				"#  define NOT_ONE (!ONE)  \n"
				"#\tdefine PLUS(x,y) \\\n"
				" (x)+(y) xx 0x _0x  \n"
				"ONE\n"
				"PLUS  (1,(2,3))\n"
				"#define C(x) A(x) B(x) C(x)\n"
				"#define B(x) C(x)\n"
				"#define A(x) B(x)\n"
				"A(1)\n"
				"#include \"inc\"\n"
				"#ifdef ZERO\n"
				"#if 0\n"
				"  not output \"ignore #else\" \n"
				"#endif\n"
				"#elif (!0) && defined(PLUS)\n"
				"  output\n"
				"#else\n"
				"  not output\n"
				"#endif\n"
				"#ifndef ZERO\n"
				"#if defined(ZERO) || defined(PLUS)\n"
				"  output this\n"
				"#else\n"
				"  but not this\n"
				"#endif\n"
				"#endif\n"
				"#undef ONE\n"
				"#ifdef ONE\n"
				"  don't output\n"
				"#endif\n"
				"\"#error this would throw an error\"\n"
				"#pragma ignore this\n"
				"#line ignore this\n"
				"#warning ignore this\n"
				"lastword"
				"#define ONE 1\n"
				"#eval{ONE+2-4+len2(3,4)}\n"
				"#lit Any old ch*rac#ers #if I {feel} #include --cheese like #en#end\n"
				"// #if 1 comments \n"
				"/*should pass thru #else*/\n"
				//"#lua --lua code\n return \"hello world\" #end\n"
				;
			char const* str_out =
				"\t      \n"
				"\"#if ignore #define this stuff\"\n"
				"ONE\n"
				"PLUS  (1,(2,3))\n"
				"A(1)\n"
				"\n"
				"  not output\n"
				"\"#error this would throw an error\"\n"
				"lastword0\n"
				"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
				"// #if 1 comments \n"
				"/*should pass thru #else*/\n"
				//"#lua --lua code\n return \"hello world\" #end\n"
			;
			PtrSrc src(str_in);
			Preprocessor pp(src, 0, 0, 0);
			for (;*str_out; ++pp, ++str_out)
				CHECK_EQUAL(*str_out, *pp);
			CHECK_EQUAL(0, *pp);
		}
	}
	TEST(Tokeniser)
	{
		char const* str_in =
			"auto double int struct break else long switch case enum register typedef "
			"char extern return union const float short unsigned continue for signed "
			"void default goto sizeof volatile do if static while"
			" \n = ; ~ ! * & + - / % < > | ^ , ? { } [ ] ( ) . : # $ @ ++ -- << >> <= "
			">= == != && || <<= >>= &= |= ^= += -= *= /= %= ..."
			;
		PtrSrc src(str_in);
		Tokeniser tkr(src);
		CHECK(*tkr == EKeyword::Auto     ); ++tkr;
		CHECK(*tkr == EKeyword::Double   ); ++tkr;
		CHECK(*tkr == EKeyword::Int      ); ++tkr;
		CHECK(*tkr == EKeyword::Struct   ); ++tkr;
		CHECK(*tkr == EKeyword::Break    ); ++tkr;
		CHECK(*tkr == EKeyword::Else     ); ++tkr;
		CHECK(*tkr == EKeyword::Long     ); ++tkr;
		CHECK(*tkr == EKeyword::Switch   ); ++tkr;
		CHECK(*tkr == EKeyword::Case     ); ++tkr;
		CHECK(*tkr == EKeyword::Enum     ); ++tkr;
		CHECK(*tkr == EKeyword::Register ); ++tkr;
		CHECK(*tkr == EKeyword::Typedef  ); ++tkr;
		CHECK(*tkr == EKeyword::Char     ); ++tkr;
		CHECK(*tkr == EKeyword::Extern   ); ++tkr;
		CHECK(*tkr == EKeyword::Return   ); ++tkr;
		CHECK(*tkr == EKeyword::Union    ); ++tkr;
		CHECK(*tkr == EKeyword::Const    ); ++tkr;
		CHECK(*tkr == EKeyword::Float    ); ++tkr;
		CHECK(*tkr == EKeyword::Short    ); ++tkr;
		CHECK(*tkr == EKeyword::Unsigned ); ++tkr;
		CHECK(*tkr == EKeyword::Continue ); ++tkr;
		CHECK(*tkr == EKeyword::For      ); ++tkr;
		CHECK(*tkr == EKeyword::Signed   ); ++tkr;
		CHECK(*tkr == EKeyword::Void     ); ++tkr;
		CHECK(*tkr == EKeyword::Default  ); ++tkr;
		CHECK(*tkr == EKeyword::Goto     ); ++tkr;
		CHECK(*tkr == EKeyword::Sizeof   ); ++tkr;
		CHECK(*tkr == EKeyword::Volatile ); ++tkr;
		CHECK(*tkr == EKeyword::Do       ); ++tkr;
		CHECK(*tkr == EKeyword::If       ); ++tkr;
		CHECK(*tkr == EKeyword::Static   ); ++tkr;
		CHECK(*tkr == EKeyword::While    ); ++tkr;
		
		CHECK(*tkr == ESymbol::NewLine      ); ++tkr;
		CHECK(*tkr == ESymbol::Assign       ); ++tkr;
		CHECK(*tkr == ESymbol::SemiColon    ); ++tkr;
		CHECK(*tkr == ESymbol::Complement   ); ++tkr;
		CHECK(*tkr == ESymbol::Not          ); ++tkr;
		CHECK(*tkr == ESymbol::Ptr          ); ++tkr;
		CHECK(*tkr == ESymbol::AddressOf    ); ++tkr;
		CHECK(*tkr == ESymbol::Plus         ); ++tkr;
		CHECK(*tkr == ESymbol::Minus        ); ++tkr;
		CHECK(*tkr == ESymbol::Divide       ); ++tkr;
		CHECK(*tkr == ESymbol::Modulus      ); ++tkr;
		CHECK(*tkr == ESymbol::LessThan     ); ++tkr;
		CHECK(*tkr == ESymbol::GtrThan      ); ++tkr;
		CHECK(*tkr == ESymbol::BitOr        ); ++tkr;
		CHECK(*tkr == ESymbol::BitXor       ); ++tkr;
		CHECK(*tkr == ESymbol::Comma        ); ++tkr;
		CHECK(*tkr == ESymbol::Conditional  ); ++tkr;
		CHECK(*tkr == ESymbol::BraceOpen    ); ++tkr;
		CHECK(*tkr == ESymbol::BraceClose   ); ++tkr;
		CHECK(*tkr == ESymbol::BracketOpen  ); ++tkr;
		CHECK(*tkr == ESymbol::BracketClose ); ++tkr;
		CHECK(*tkr == ESymbol::ParenthOpen  ); ++tkr;
		CHECK(*tkr == ESymbol::ParenthClose ); ++tkr;
		CHECK(*tkr == ESymbol::Dot          ); ++tkr;
		CHECK(*tkr == ESymbol::Colon        ); ++tkr;
		CHECK(*tkr == ESymbol::Hash         ); ++tkr;
		CHECK(*tkr == ESymbol::Dollar       ); ++tkr;
		CHECK(*tkr == ESymbol::At           ); ++tkr;
		CHECK(*tkr == ESymbol::Increment    ); ++tkr;
		CHECK(*tkr == ESymbol::Decrement    ); ++tkr;
		CHECK(*tkr == ESymbol::ShiftL       ); ++tkr;
		CHECK(*tkr == ESymbol::ShiftR       ); ++tkr;
		CHECK(*tkr == ESymbol::LessEql      ); ++tkr;
		CHECK(*tkr == ESymbol::GtrEql       ); ++tkr;
		CHECK(*tkr == ESymbol::Equal        ); ++tkr;
		CHECK(*tkr == ESymbol::NotEqual     ); ++tkr;
		CHECK(*tkr == ESymbol::LogicalAnd   ); ++tkr;
		CHECK(*tkr == ESymbol::LogicalOr    ); ++tkr;
		CHECK(*tkr == ESymbol::ShiftLAssign ); ++tkr;
		CHECK(*tkr == ESymbol::ShiftRAssign ); ++tkr;
		CHECK(*tkr == ESymbol::BitAndAssign ); ++tkr;
		CHECK(*tkr == ESymbol::BitOrAssign  ); ++tkr;
		CHECK(*tkr == ESymbol::BitXorAssign ); ++tkr;
		CHECK(*tkr == ESymbol::AddAssign    ); ++tkr;
		CHECK(*tkr == ESymbol::SubAssign    ); ++tkr;
		CHECK(*tkr == ESymbol::MulAssign    ); ++tkr;
		CHECK(*tkr == ESymbol::DivAssign    ); ++tkr;
		CHECK(*tkr == ESymbol::ModAssign    ); ++tkr;
		CHECK(*tkr == ESymbol::Ellipsis     ); ++tkr;
		CHECK(*tkr == EToken::EndOfStream   ); ++tkr;
		CHECK(*tkr == EToken::EndOfStream   ); ++tkr;
	}
	TEST(Reader)
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
		pr::m3x3 mat3;
		pr::m4x4 mat4;
		
		{// basic extract methods
			pr::script::Loc    loc;
			pr::script::PtrSrc ptr(src, &loc);
			pr::script::Reader reader;
			reader.CaseSensitiveKeywords(true);
			reader.AddSource(ptr);
			
			CHECK(reader.CaseSensitiveKeywords());
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Identifier"                  ,std::string(kw) );
			CHECK(reader.ExtractIdentifier(str));       CHECK_EQUAL("ident"                       ,str             );
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("String"                      ,std::string(kw) );
			CHECK(reader.ExtractString(str));           CHECK_EQUAL("simple string"               ,str             );
			CHECK(reader.NextKeywordH(hashed_kw));      CHECK_EQUAL(reader.HashKeyword("CString") ,hashed_kw       );
			CHECK(reader.ExtractCString(str));          CHECK_EQUAL("C:\\Path\\Filename.txt"      ,str             );
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Bool"                        ,std::string(kw) );
			CHECK(reader.ExtractBool(bval));            CHECK_EQUAL(true                          ,bval            );
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Intg"                        ,std::string(kw) );
			CHECK(reader.ExtractInt(ival, 10));         CHECK_EQUAL(-23                           ,ival            );
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Intg16"                      ,std::string(kw) );
			CHECK(reader.ExtractInt(uival, 16));        CHECK_EQUAL(0xABCDEF00                    ,uival           );
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Real"                        ,std::string(kw) );
			CHECK(reader.ExtractReal(fval));            CHECK_EQUAL(-2.3e+3                       ,fval            );
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("BoolArray"                   ,std::string(kw) );
			CHECK(reader.ExtractBoolArray(barray, 4));
			CHECK_EQUAL(true , barray[0]);
			CHECK_EQUAL(false, barray[1]);
			CHECK_EQUAL(true , barray[2]);
			CHECK_EQUAL(false, barray[3]);
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("IntArray"                    ,std::string(kw) );
			CHECK(reader.ExtractIntArray(iarray, 4, 10));
			CHECK_EQUAL(-3, iarray[0]);
			CHECK_EQUAL(+2, iarray[1]);
			CHECK_EQUAL(+1, iarray[2]);
			CHECK_EQUAL(-0, iarray[3]);
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("RealArray"                   ,std::string(kw) );
			CHECK(reader.ExtractRealArray(farray, 4));
			CHECK_EQUAL(2.3f    , farray[0]);
			CHECK_EQUAL(-1.0e-1f, farray[1]);
			CHECK_EQUAL(+2.0f   , farray[2]);
			CHECK_EQUAL(-0.2f   , farray[3]);
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Vector3"                     ,std::string(kw) );
			CHECK(reader.ExtractVector3(vec,-1.0f));    CHECK(pr::FEql4(vec, pr::v4::make(1.0f, 2.0f, 3.0f,-1.0f)));
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Vector4"                     ,std::string(kw) );
			CHECK(reader.ExtractVector4(vec));          CHECK(pr::FEql4(vec, pr::v4::make(4.0f, 3.0f, 2.0f, 1.0f)));
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Quaternion"                  ,std::string(kw) );
			CHECK(reader.ExtractQuaternion(quat));      CHECK(pr::FEql4(quat, pr::Quat::make(0.0f, -1.0f, -2.0f, -3.0f)));
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("M3x3"                        ,std::string(kw) );
			CHECK(reader.ExtractMatrix3x3(mat3));       CHECK(pr::FEql(mat3, pr::m3x3Identity));
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("M4x4"                        ,std::string(kw) );
			CHECK(reader.ExtractMatrix4x4(mat4));       CHECK(pr::FEql(mat4, pr::m4x4Identity));
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("Data"                        ,std::string(kw) );
			CHECK(reader.ExtractData(kw, 16));          CHECK_EQUAL("ABCDEFGHIJKLMNO"             ,std::string(kw) );
			CHECK(reader.FindNextKeyword("Section"));   str.resize(0);
			CHECK(reader.ExtractSection(str, false));   CHECK_EQUAL("*SubSection { *Data \n 23 \"With a }\\\"string\\\"{ in it\" }" ,str);
			CHECK(reader.FindNextKeyword("Section"));   str.resize(0);
			CHECK(reader.NextKeywordS(kw));             CHECK_EQUAL("LastThing"                   ,std::string(kw) );
			CHECK(!reader.IsKeyword());
			CHECK(!reader.IsSectionStart());
			CHECK(!reader.IsSectionEnd());
			CHECK(reader.IsSourceEnd());
		}
		{
			char const* src = 
				"A.B\n"
				"a.b.c\n"
				"A.B.C.D\n"
				;
		
			pr::script::Loc    loc;
			pr::script::PtrSrc ptr(src, &loc);
			pr::script::Reader reader;
			reader.CaseSensitiveKeywords(true);
			reader.AddSource(ptr);
			
			std::string s0,s1,s2,s3;
			reader.ExtractIdentifier(s0,s1,'.');        CHECK(s0 == "A" && s1 == "B");
			reader.ExtractIdentifier(s0,s1,s2,'.');     CHECK(s0 == "a" && s1 == "b" && s2 == "c");
			reader.ExtractIdentifier(s0,s1,s2,s3,'.');  CHECK(s0 == "A" && s1 == "B" && s2 == "C" && s3 == "D");
		}
	}
}
