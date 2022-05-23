//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************
#pragma once

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
#include "pr/script/script_core.h"
#include "pr/script/embedded_lua.h"
#include "pr/win32/win32.h"

namespace pr::script
{
	PRUnitTest(CoreTests)
	{
		using namespace pr::str;

		std::filesystem::path const script_utf = L"script_utf.txt";
		auto cleanup = pr::Scope<void>([]{}, [&]{ std::filesystem::remove(script_utf); });

		{// Simple buffering
			char const str[] = "123abc";
			StringSrc ptr(str);

			PR_CHECK(*ptr  , L'1');
			PR_CHECK(ptr[5], L'c');
			PR_CHECK(ptr[0], L'1');

			PR_CHECK(*(++ptr)   , L'2');
			PR_CHECK(*(ptr += 3), L'b');
			PR_CHECK(*(++ptr)   , L'c');

			PR_CHECK(*(++ptr),  0);
		}
		{// Limited source
			char const str[] = "1234567890";
			StringSrc ptr(str);
			ptr.Limit(3);

			PR_CHECK(ptr[0], L'1');
			PR_CHECK(ptr[1], L'2');
			PR_CHECK(ptr[2], L'3');
			PR_CHECK(ptr[3], L'\0');
			PR_CHECK(ptr[4], L'\0');

			PR_CHECK(*ptr, L'1'); ++ptr;
			PR_CHECK(*ptr, L'2'); ++ptr;
			PR_CHECK(*ptr, L'3'); ++ptr;
			PR_CHECK(*ptr, L'\0'); ++ptr;
			PR_CHECK(*ptr, L'\0'); ++ptr;

			ptr.Limit(2);

			PR_CHECK(ptr[0], L'4');
			PR_CHECK(ptr[1], L'5');
			PR_CHECK(ptr[2], L'\0');
			PR_CHECK(ptr[3], L'\0');

			PR_CHECK(*ptr, L'4'); ++ptr;
			PR_CHECK(*ptr, L'5'); ++ptr;
			PR_CHECK(*ptr, L'\0'); ++ptr;
			PR_CHECK(*ptr, L'\0'); ++ptr;

			ptr.Limit(5);

			auto len0 = ptr.ReadAhead(5);
			PR_CHECK(len0, 5);
			PR_CHECK(ptr.Buffer().size(), 5ULL);

			ptr.Limit(3);

			// Setting the limit after characters have been buffered does not change the buffer.
			// Because of this, Buffer().size() should not be used to determine the available characters
			// after a call to ReadAhead()
			auto len1 = ptr.ReadAhead(5);
			PR_CHECK(len1, 3);
			PR_CHECK(ptr.Buffer().size(), 5ULL);
		}
		{// Matching
			wchar_t const str[] = L"0123456789";
			StringSrc ptr(str);

			PR_CHECK(ptr.Match(L"0123"), true);
			PR_CHECK(ptr.Match(L"012345678910"), false);
			ptr += 5;
			PR_CHECK(ptr.Match(L"567"), true);
		}
		{// UTF8 File source

			// UTF-8 data
			unsigned char data[] = {0xef, 0xbb, 0xbf, 0xe4, 0xbd, 0xa0, 0xe5, 0xa5, 0xbd}; //' ni hao
			wchar_t str[] = {0x4f60, 0x597d};

			{// Create the file
				std::ofstream fout(script_utf, std::ios::binary);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}

			FileSrc file(script_utf);
			PR_CHECK(*file, str[0]); ++file;
			PR_CHECK(*file, str[1]); ++file;

			// Buffering a file in a string
			FileSrc file2(script_utf);
			StringSrc str2 = file2.ToStringSrc();
			PR_CHECK(*file2, *str2); ++file2; ++str2;
			PR_CHECK(*file2, *str2); ++file2; ++str2;
			PR_CHECK(*file2, *str2); ++file2; ++str2;
		}
		{// UTF 16 little endian File source

			// UTF-16le data (if host system is little-endian)
			unsigned short data[] = {0xfeff, 0x4f60, 0x597d}; //' ni hao
			wchar_t str[] = {0x4f60, 0x597d};

			{// Create the file
				std::ofstream fout(script_utf, std::ios::binary);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}

			FileSrc file(script_utf);
			PR_CHECK(*file, str[0]); ++file;
			PR_CHECK(*file, str[1]); ++file;
		}
		{// UTF 16 big endian File source

			// UTF-16be data (if host system is little-endian)
			unsigned short data[] = {0xfffe, 0x604f, 0x7d59}; //' ni hao
			wchar_t str[] = {0x4f60, 0x597d};

			{// Create the file
				std::ofstream fout(script_utf, std::ios::binary);
				fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
			}

			FileSrc file(script_utf);
			PR_CHECK(*file, str[0]); ++file;
			PR_CHECK(*file, str[1]); ++file;
		}
		{ // Eat functions
			{
				StringSrc src(" \t\n,Text");
				EatDelimiters(src, "\n\t ,");
				PR_CHECK(*src, 'T');
			}
			{
				StringSrc src("991239Text");
				Eat(src, 2, 1, [](auto& s) { return *s < '5'; });
				PR_CHECK(*src, 'T');
			}
			{
				StringSrc src0("01 \t \t \r\n");
				EatLineSpace(src0, 2, 0);
				PR_CHECK(*src0, '\n');

				StringSrc src1("01 \t \t \r");
				EatLineSpace(src1, 2, 1);
				PR_CHECK(*src1, '\0');
			}
			{
				StringSrc src("01 \t \t \rA");
				EatWhiteSpace(src, 2, 0);
				PR_CHECK(*src, 'A');
			}
			{
				StringSrc src0("0123456\r\nABC");
				EatLine(src0, 0, 2, false);
				PR_CHECK(*src0, 'A');

				StringSrc src1("0123456");
				EatLine(src1, 0, 0, true);
				PR_CHECK(*src1, '\0');
			}
			{
				StringSrc src("{{ block }}#");
				EatBlock(src, L"{{", L"}}");
				PR_CHECK(*src, '#');
			}
			{
				StringSrc src0("\"A \\\"string\\\" within a string\"#");
				EatLiteral(src0);
				PR_CHECK(*src0, '#');

				StringSrc src1("\"A \\\\\"#  \"@ ");
				EatLiteral(src1);
				PR_CHECK(*src1, '#');

				StringSrc src2("\"\\\"\"#");
				EatLiteral(src2);
				PR_CHECK(*src2, '#');
			}
			{
				StringSrc src(";comment \r\n#");
				EatLineComment(src, L";");
				PR_CHECK(*src, '\r');
			}
			{
				StringSrc src("<!-- comment \r\n -->#");
				EatBlockComment(src, L"<!--", L"-->");
				PR_CHECK(*src, '#');
			}
		}
		{// Buffer functions
			{
				auto len = 0;
				StringSrc src("_123abc#");
				PR_CHECK(BufferIdentifier(src, 0, &len), true);
				PR_CHECK(len, 7);
				PR_CHECK(src.ReadN(len), L"_123abc");
			}
			{
				auto len = 5;
				StringSrc src("123abc#");
				PR_CHECK(BufferIdentifier(src, 0, &len), false);
				PR_CHECK(len, 0);
			}
			{
				auto len = 0;
				StringSrc src("  \"Lit\\\"er\\\"al\" ");
				PR_CHECK(BufferLiteral(src, 2, &len), true);
				PR_CHECK(len, 15);
				src += 2; len -= 2;
				PR_CHECK(src.ReadN(len), L"\"Lit\\\"er\\\"al\"");
			}
			{
				auto len = 0;
				StringSrc src("\"\\\\\"   \"");
				PR_CHECK(BufferLiteral(src, 0, &len), true);
				PR_CHECK(len, 4);
				PR_CHECK(src.ReadN(len), L"\"\\\\\"");
			}
			{
				auto len = 0;
				StringSrc src("abc\ndef");
				PR_CHECK(BufferLine(src, true, 0, &len), true);
				PR_CHECK(len, 4);
				PR_CHECK(src.ReadN(len), L"abc\n");
			}
			{
				auto len = 0;
				StringSrc src("  abc\ndef");
				PR_CHECK(BufferLine(src, false, 2, &len), true);
				PR_CHECK(len, 5);
				src += 2; len -= 2;
				PR_CHECK(src.ReadN(len), L"abc");
			}
			{
				auto len = 0;
				StringSrc src("a b\tc\nd,end;f");
				PR_CHECK(BufferTo(src, L"end", true, 0, &len), true);
				PR_CHECK(len, 11);
				PR_CHECK(src.ReadN(len), L"a b\tc\nd,end");
			}
			{
				auto len = 0;
				StringSrc src("a b\tc\nd,");
				PR_CHECK(BufferTo(src, L"end", false, 0, &len), false);
				PR_CHECK(len, 8);
				PR_CHECK(src.ReadN(len), L"a b\tc\nd,");
			}
			{
				auto len = 0;
				StringSrc src("a b\tc\nd,");
				PR_CHECK(BufferWhile(src, [](Src& s, int i) { return !s.Match(L"\tc\n", i); }, 0, &len), true);
				PR_CHECK(len, 3);
				PR_CHECK(src.ReadN(len), L"a b");
			}
			{
				auto len = 0;
				StringSrc src("abcde");
				PR_CHECK(BufferWhile(src, [](Src& s, int i) { return s[i] != 'f'; }, 0, &len), false);
				PR_CHECK(len, 5);
				PR_CHECK(src.ReadN(len), L"abcde");
			}
			{
				auto len = 0;
				StringSrc src("a_b_c_d");
				PR_CHECK(BufferWhile(src, [](Src& s, int i) { return s[i] != '_' ? 2 : 0; }, 0, &len), false);
				PR_CHECK(len, 7);
				PR_CHECK(src.ReadN(len), L"a_b_c_d");
			}
		}
	}
	PRUnitTest(BufTests)
	{
		using namespace pr::str;

		{// BufW2
			wchar_t const data[] = L"0123456789";
			wchar_t const* const src = &data[0];
			Buf<2, wchar_t> buf(src);
			PR_CHECK(buf[0], L'0');
			PR_CHECK(buf[1], L'1');
			PR_CHECK(*src, L'0');

		}
		{// BufW4
			wchar_t const data[] = L"0123456789";
			wchar_t const* src = &data[0];
			Buf<4, wchar_t> buf(src);
			PR_CHECK(*src, L'4');
			PR_CHECK(buf[0], L'0');
			PR_CHECK(buf[1], L'1');
			PR_CHECK(buf[2], L'2');
			PR_CHECK(buf[3], L'3');
			buf.shift(*src++);
			PR_CHECK(buf[0], L'1');
			PR_CHECK(buf[1], L'2');
			PR_CHECK(buf[2], L'3');
			PR_CHECK(buf[3], L'4');
		}
		{// BufW8
			using BufW8 = Buf<8, wchar_t>;
			wchar_t const src[] = L"0123456";
			PR_CHECK(Equal(BufW8(src).c_str(), src), true);
			PR_CHECK(BufW8(L"Paul"       ).match(BufW8(L"PaulWasHere")), true);
			PR_CHECK(BufW8(L"PaulWasHere").match(BufW8(L"Paul"       )), false);
			PR_CHECK(BufW8(L"ABC") == BufW8(L"ABC"), true);
		}
		{// Src
			script::StringSrc src("0123456789");
			Buf<4,wchar_t> buf(src);
			PR_CHECK(*src, L'4');
			PR_CHECK(buf[0], L'0');
			PR_CHECK(buf[1], L'1');
			PR_CHECK(buf[2], L'2');
			PR_CHECK(buf[3], L'3');
			buf.shift(*src);
			PR_CHECK(buf[0], L'1');
			PR_CHECK(buf[1], L'2');
			PR_CHECK(buf[2], L'3');
			PR_CHECK(buf[3], L'4');
		}
	}
	PRUnitTest(LocationTests)
	{
		char const* str =
			"123\n"
			"abc\n"
			"\tx";

		Loc loc(L"", 0, 0, 1, 1, true, 4);
		for (auto s = str; *s; ++s)
			loc.inc(*s);

		PR_CHECK(loc.Line(), 3);
		PR_CHECK(loc.Col(), 6);
	}
	PRUnitTest(SrcStackTests)
	{
		char const str1[] = "one";
		char const str2[] = "two";
		StringSrc src1(str1);
		StringSrc src2(str2);
		SrcStack stack(src1);

		for (int i = 0; i != 2; ++i, ++stack)
			PR_CHECK(*stack, str1[i]);

		stack.Push(src2);

		for (int i = 0; i != 3; ++i, ++stack)
			PR_CHECK(*stack, str2[i]);

		for (int i = 2; i != 3; ++i, ++stack)
			PR_CHECK(*stack, str1[i]);

		PR_CHECK(*stack, '\0');
	}
	PRUnitTest(FilterTests)
	{
		using namespace pr::str;
		{// StripLineContinuations
			char const str_in[] = "Li\
				on";
			char const str_out[] = "Li				on";

			StringSrc src(str_in);
			StripLineContinuations strip(src);

			auto out = &str_out[0];
			for (; *strip; ++strip, ++out)
			{
				if (*strip == *out) continue;
				PR_CHECK(*strip, *out);
			}
			PR_CHECK(*out, 0);
		}
		{// StripComments
			char const str_in[] = 
				"123// comment         \n"
				"456/* blo/ck */789\n"
				"// many               \n"
				"// lines              \n"
				"// \"string\"         \n"
				"/* \"string\" */      \n"
				"\"string \\\" /*a*/ //b\"  \n"
				"/not a comment\n"
				"/*\n"
				"  more lines\n"
				"*/\n"
				"// multi\\\n"
				" line\\\n"
				" comment\n"
				"/*/ comment */\n"
				"/*back to*//*back*/ comment\n";
			char const str_out[] = 
				"123\n"
				"456789\n"
				"\n"
				"\n"
				"\n"
				"      \n"
				"\"string \\\" /*a*/ //b\"  \n"
				"/not a comment\n"
				"\n"
				"\n"
				"\n"
				" comment\n";

			StringSrc src0(str_in);
			StripLineContinuations src1(src0);
			StripComments strip(src1);

			auto out = &str_out[0];
			for (;*strip; ++strip, ++out)
			{
				if (*strip == *out) continue;
				PR_CHECK(*strip, *out);
			}
			PR_CHECK(*out, 0);
		}
		{// Strip ASM comments
			char const str_in[] =
				"; asm comments start with a ; character\r\n"
				"mov 43 2\r\n"
				"ldr $a 2 ; imaginary asm";
			char const str_out[] =
				"\r\n"
				"mov 43 2\r\n"
				"ldr $a 2 ";

			StringSrc src0(str_in);
			StripComments strip(src0, InComment::Patterns(L";", L"\r\n", L"", L""));

			auto out = &str_out[0];
			for (; *strip; ++strip, ++out)
			{
				if (*strip == *out) continue;
				PR_CHECK(*strip, *out);
			}
			PR_CHECK(*out, 0);
		}
		{// StripNewLines
			char const str_in[] =
				"  \n"
				"      \n"
				"   \n"
				"  \" multi-line \n"
				"\n"
				"\n"
				"string \"     \n"
				"         \n"
				"     \n"
				"abc  \n"
				"\n"
				"\n"
				"";

			{// min 0, max 0 lines
				char const str_out[] =
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     abc  ";

				StringSrc src0(str_in);
				StripNewLines strip(src0, 0, 0);

				auto out = &str_out[0];
				for (; *strip; ++strip, ++out)
				{
					if (*strip == *out) continue;
					PR_CHECK(*strip, *out);
				}
				PR_CHECK(*out, 0);
			}
			{// min 0, max 1 lines
				char const str_out[] =
					"\n"
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"abc  \n"
					"";

				StringSrc src0(str_in);
				StripNewLines strip(src0, 0, 1);

				auto out = &str_out[0];
				for (; *strip; ++strip, ++out)
				{
					if (*strip == *out) continue;
					PR_CHECK(*strip, *out);
				}
				PR_CHECK(*out, 0);
			}
			{// min 2, max 2 lines
				char const str_out[] =
					"\n"
					"\n"
					"  \" multi-line \n"
					"\n"
					"\n"
					"string \"     \n"
					"\n"
					"abc  \n"
					"\n"
					"";

				StringSrc src0(str_in);
				StripNewLines strip(src0,2,2);

				auto out = &str_out[0];
				for (; *strip; ++strip, ++out)
				{
					if (*strip == *out) continue;
					PR_CHECK(*strip, *out);
				}
				PR_CHECK(*out, 0);
			}
		}
	}
	PRUnitTest(MacroTests)
	{
		MacroDB macros;

		{
			Macro macro1(L"One", L"OneExpanded");
			Macro macro2(L"Two", L"TwoExpanded x y", { L"x", L"y" });
			macros.Add(macro1);
			macros.Add(macro2);

			// Macros are copied into the DB
			PR_CHECK(macros.Find(L"One") != &macro1, true);
			PR_CHECK(macros.Find(L"Two") != &macro2, true);
			PR_CHECK(*macros.Find(L"One") == macro1, true);
			PR_CHECK(*macros.Find(L"Two") == macro2, true);
		}

		PR_CHECK(macros.Find(L"One") != nullptr, true);
		PR_CHECK(macros.Find(L"Two") != nullptr, true);
		PR_CHECK(macros.Find(L"Three") == nullptr, true);

		string_t result;
		macros.Find(L"One")->Expand(result, {}, Loc());
		PR_CHECK(result, L"OneExpanded");

		macros.Find(L"Two")->Expand(result, { L"A", L"B" }, Loc());
		PR_CHECK(result, L"TwoExpanded A B");
	}
	PRUnitTest(IncludesTests)
	{
		using namespace pr::str;
		using string = pr::string<wchar_t>;

		char data[] = "Included";
		auto script_include = temp_dir / L"script_include.txt";
		auto cleanup = Scope<void>([]{}, [=] { std::filesystem::remove(script_include); });

		{// Create the file
			std::ofstream fout(script_include);
			fout.write(reinterpret_cast<char const*>(&data[0]), sizeof(data));
		}

		{
			Includes inc;

			inc.AddSearchPath(win32::ExePath().parent_path());
			inc.AddSearchPath(std::filesystem::current_path());

			auto src_ptr = inc.Open(script_include, EIncludeFlags::None);
			auto& src = *src_ptr;

			std::wstring r; for (;*src; ++src) r.push_back(*src);
			PR_CHECK(pr::str::Equal(r, data), true);
		}
	}
	PRUnitTest(TokeniserTests)
	{
		char const str_in[] =
			"auto double int struct break else long switch case enum register typedef "
			"char extern return union const float short unsigned continue for signed "
			"void default goto sizeof volatile do if static while"
			" \n = ; ~ ! * & + - / % < > | ^ , ? { } [ ] ( ) . : # $ @ ++ -- << >> <= "
			">= == != && || <<= >>= &= |= ^= += -= *= /= %= ..."
			;

		StringSrc src(str_in);
		Tokeniser tkr(src);
		PR_CHECK(*tkr == EKeyword::Auto     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Double   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Int      , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Struct   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Break    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Else     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Long     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Switch   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Case     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Enum     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Register , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Typedef  , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Char     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Extern   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Return   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Union    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Const    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Float    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Short    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Unsigned , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Continue , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::For      , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Signed   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Void     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Default  , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Goto     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Sizeof   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Volatile , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Do       , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::If       , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Static   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::While    , true); ++tkr;

		PR_CHECK(*tkr == ESymbol::NewLine      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Assign       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::SemiColon    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Complement   , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Not          , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Ptr          , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::AddressOf    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Plus         , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Minus        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Divide       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Modulus      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LessThan     , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::GtrThan      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitOr        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitXor       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Comma        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Conditional  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BraceOpen    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BraceClose   , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BracketOpen  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BracketClose , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ParenthOpen  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ParenthClose , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Dot          , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Colon        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Hash         , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Dollar       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::At           , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Increment    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Decrement    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftL       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftR       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LessEql      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::GtrEql       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Equal        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::NotEqual     , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LogicalAnd   , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LogicalOr    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftLAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftRAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitAndAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitOrAssign  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitXorAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::AddAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::SubAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::MulAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::DivAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ModAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Ellipsis     , true); ++tkr;
		PR_CHECK(*tkr == EToken::EndOfStream   , true); ++tkr;
		PR_CHECK(*tkr == EToken::EndOfStream   , true); ++tkr;
	}
	PRUnitTest(InputStackTests)
	{
		using namespace pr::str;

		char const* src1 = "abcd";
		wchar_t const* src2 = L"123";
		pr::string<wchar_t> str1;

		Preprocessor pp(src1);
		str1.push_back(*pp); ++pp;
		str1.push_back(*pp); ++pp;
		pp.Push(src2);
		str1.push_back(*pp); ++pp;
		str1.push_back(*pp); ++pp;
		str1.push_back(*pp); ++pp;
		str1.push_back(*pp); ++pp;
		str1.push_back(*pp); ++pp;
		PR_CHECK(Equal(str1, L"ab123cd"), true);
		PR_CHECK(*pp, 0);
	}
	PRUnitTest(PreprocessorTests)
	{
		{// Consecutive strings
			char const* str_in = 
				"\"consecutive \"  \t\"string\""
				;
			char const* str_out =
				"\"consecutive string\""
				;
			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// ignored stuff
			char const* str_in =
				"\"#if ignore #define this stuff\"\n"
				;
			char const* str_out =
				"\"#if ignore #define this stuff\"\n"
				;

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// Line continuation tests line endings
			char const* str_in =
				"#define BLAH(x)\\\r\n"
				"   \\\r\n"
				"	(x + 1)\r\n"
				"BLAH(5)\r\n"
				"#define BOB\\\r\n"
				"	bob\r\n"
				"BLAH(bob)\r\n";
			char const* str_out =
				"(5 + 1)\r\n"
				"(bob + 1)\r\n"
			;

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// simple macros
			char const* str_in =
				"#  define ONE 1 // ignore me \n"
				"# define    ONE  1\n" // same definition, allowed
				"#  define NOT_ONE (!ONE) /*and me*/ \n"
				"#define TWO\\\n"
				"   2\n"
				"ONE\n"
				"NOT_ONE\n"
				"TWO\n"
				;
			char const* str_out =
				"1\n"
				"(!1)\n"
				"2\n"
				;

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// Multi-line preprocessor
			char const* str_in =
				"#define ml\\\n"
				"  MULTI\\\n"
				"LINE\n"
				"ml";
			char const* str_out =
				"MULTILINE";

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
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

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
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

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// #eval
			char const* str_in =
				"#eval{1+#eval{1+1}}\n"
				;
			char const* str_out =
				"3\n"
				;

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// recursive macros/evals
			char const* str_in =
				"#define X 3.0\n"
				"#define Y 4.0\n"
				"#define Len2 #eval{len2(X,Y)}\n"
				"#eval{X + Len2}\n";
			char const* str_out =
				"8\n";

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// #if/#else/#etc
			char const* str_in =
				"#  define ONE 1 // ignore me \n"
				"#  define NOT_ONE (!ONE) /*and me*/ \n"
				"#\tdefine PLUS(x,y) (x)+(y) xx 0x _0x  \n"
				"#ifdef ZERO\n"
				"	#if NESTED\n"
				"		not output \"ignore #else\" \n"
				"	#endif\n"
				"#elif (!NOT_ONE) && defined(PLUS)\n"
				"	output\n"
				"#else\n"
				"	not output\n"
				"#endif\n"
				"#ifndef ZERO\n"
				"	#if defined(ZERO) || defined(PLUS)\n"
				"		output this\n"
				"	#else\n"
				"		but not this\n"
				"	#endif\n"
				"#endif\n"
				"#undef ONE\n"
				"#ifdef ONE\n"
				"	don't output\n"
				"#endif\n"
				"#define TWO\n"
				"#ifdef TWO\n"
				"	two defined\n"
				"#endif\n"
				"#defifndef ONE 1\n"
				"#defifndef ONE 2\n"
				"ONE\n"
				"#if 0\n"
				"\"string \\\n" // This unclosed string doesn't matter when skipping inactive blocks
				"#endif\n" // this is part of the previous line, so is ignored
				"#endif\n"
				;
			char const* str_out =
				"	output\n"
				"	"//#if defined(ZERO) || ...
				"		output this\n"
				"	"//#else\n
				"	two defined\n"
				"1\n"
				;

			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// includes
			char const* str_in =
				"#  define ONE 1 // ignore me \n"
				"#include \"inc\"\n"
				"#depend \"dep\"\n"
				;
			char const* str_out =
				"included 1\n\n"
				;

			Includes inc;
			inc.AddString(L"inc", "included ONE");
			inc.AddString(L"dep", "Anything");
			StringSrc src(str_in);
			Preprocessor pp(&src, false, &inc);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// miscellaneous
			char const* str_in =
				"\"#error this would throw an error\"\n"
				"#pragma ignore this\n"
				"#line ignore this\n"
				"#warning ignore this\n"
				"#include_path \"some_path\"\n"
				"lastword"
				"#define ONE 1\n"
				"#eval{ONE+2-4+len2(3,4)}\n"
				"#define EVAL(x) #eval{x+1}\n"
				"EVAL(1)\n"
				"#lit Any old ch*rac#ers #if I {feel} #include --cheese like #en#end\n"
				"#embedded(lua) --lua code\n return \"hello world\" #end\n"
				;
			char const* str_out =
				"\"#error this would throw an error\"\n"
				"\n"
				"lastword"
				"4\n"
				"2\n"
				"Any old ch*rac#ers #if I {feel} #include --cheese like #en\n"
				"hello world\n"
				;

			Includes inc;
			Preprocessor pp(str_in, &inc, [](auto){ return std::make_unique<EmbeddedLua>(); });
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// Using a preloaded buffer
			std::string str_in =
				"#define BOB(x) #x\n"
				"BOB(this is a string)\n"
				;
			char const* str_out =
				"\"this is a string\"\n"
				;

			StringSrc src(str_in, StringSrc::EFlags::BufferLocally); str_in.clear();
			Preprocessor pp(&src, false);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
		{// X Macros
			char const* str_in =
				"#define LINE(x) x = #x\n"
				"#define DEFINE(values) values(LINE)\n"
				"#define Thing(x)\\\n"
				"	x(One)\\\n"
				"	x(Two)\\\n"
				"	x(Three)\n"
				"DEFINE(Thing)\n"
				"#undef Thing\n"
				;
			char const* str_out =
				"One = \"One\"	Two = \"Two\"	Three = \"Three\"\n"
				;
			Preprocessor pp(str_in);
			for (;*pp && *str_out; ++pp, ++str_out)
			{
				if (*pp == *str_out) continue;
				PR_CHECK(*pp, *str_out);
			}
			PR_CHECK(*str_out == 0 && *pp == 0, true);
		}
	}
}
#endif