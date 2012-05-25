//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/common/assert.h"
#include "pr/str/prstring.h"
#include "pr/str/wstring.h"
#include "pr/str/prstdstring.h"
#include <string>

using namespace pr;
using namespace pr::str;

SUITE(PRStringTests)
{
	// Core *******************************************************************
	TEST(Empty)
	{
		char            narr[] = "";
		std::wstring    wstr   = L"str";
		CHECK(Empty(narr));
		CHECK(!Empty(wstr));
	}
	TEST(Length)
	{
		char            narr[] = "length7";
		wchar_t         wide[] = L"length7";
		std::string     cstr   = "length7";
		std::wstring    wstr   = L"length7";
		CHECK_EQUAL(size_t(7), Length(narr));
		CHECK_EQUAL(size_t(7), Length(wide));
		CHECK_EQUAL(size_t(7), Length(cstr));
		CHECK_EQUAL(size_t(7), Length(wstr));
	}
	TEST(Equal)
	{
		char            narr[] = "str";
		wchar_t         wide[] = L"str";
		std::string     cstr   = "str1";
		std::wstring    wstr   = L"str";
		CHECK(Equal(narr, narr) &&  Equal(narr, wide) && !Equal(narr, cstr) &&  Equal(narr, wstr));
		CHECK(Equal(wide, narr) &&  Equal(wide, wide) && !Equal(wide, cstr) &&  Equal(wide, wstr));
		CHECK(!Equal(cstr, narr) && !Equal(cstr, wide) &&  Equal(cstr, cstr) && !Equal(cstr, wstr));
		CHECK(Equal(wstr, narr) &&  Equal(wstr, wide) && !Equal(wstr, cstr) &&  Equal(wstr, wstr));
	}
	TEST(EqualI)
	{
		char            narr[] = "StR";
		wchar_t         wide[] = L"sTr";
		std::string     cstr   = "sTR";
		std::wstring    wstr   = L"STr";
		CHECK(EqualI(narr, narr) && EqualI(narr, wide) && EqualI(narr, cstr) && EqualI(narr, wstr));
		CHECK(EqualI(wide, narr) && EqualI(wide, wide) && EqualI(wide, cstr) && EqualI(wide, wstr));
		CHECK(EqualI(cstr, narr) && EqualI(cstr, wide) && EqualI(cstr, cstr) && EqualI(cstr, wstr));
		CHECK(EqualI(wstr, narr) && EqualI(wstr, wide) && EqualI(wstr, cstr) && EqualI(wstr, wstr));
	}
	TEST(EqualN)
	{
		char            narr[] =  "str0";
		wchar_t         wide[] = L"str1";
		std::string     cstr   =  "str2";
		std::wstring    wstr   = L"str3";
		CHECK(EqualN(narr, narr, 3) &&  EqualN(narr, narr, 4) &&  EqualN(narr, narr, 5));
		CHECK(EqualN(narr, wide, 3) && !EqualN(narr, wide, 4) && !EqualN(narr, wide, 5));
		CHECK(EqualN(narr, cstr, 3) && !EqualN(narr, cstr, 4) && !EqualN(narr, cstr, 5));
		CHECK(EqualN(narr, wstr, 3) && !EqualN(narr, wstr, 4) && !EqualN(narr, wstr, 5));
		CHECK(EqualN(narr,  "str0") && !EqualN(narr,  "str"));
		CHECK(EqualN(wide, L"str1") && !EqualN(wide, L"str"));
		CHECK(EqualN(cstr,  "str2") && !EqualN(cstr,  "str"));
		CHECK(EqualN(wstr, L"str3") && !EqualN(wstr, L"str"));
	}
	TEST(EqualNI)
	{
		char            narr[] = "sTr0";
		wchar_t         wide[] = L"Str1";
		std::string     cstr   = "stR2";
		std::wstring    wstr   = L"sTR3";
		CHECK(EqualNI(narr, narr, 3) &&  EqualNI(narr, narr, 4) &&  EqualNI(narr, narr, 5));
		CHECK(EqualNI(narr, wide, 3) && !EqualNI(narr, wide, 4) && !EqualNI(narr, wide, 5));
		CHECK(EqualNI(narr, cstr, 3) && !EqualNI(narr, cstr, 4) && !EqualNI(narr, cstr, 5));
		CHECK(EqualNI(narr, wstr, 3) && !EqualNI(narr, wstr, 4) && !EqualNI(narr, wstr, 5));
		CHECK(EqualNI(narr,  "str0") && !EqualNI(narr,  "str"));
		CHECK(EqualNI(wide, L"str1") && !EqualNI(wide, L"str"));
		CHECK(EqualNI(cstr,  "str2") && !EqualNI(cstr,  "str"));
		CHECK(EqualNI(wstr, L"str3") && !EqualNI(wstr, L"str"));
	}
	TEST(Resize)
	{
		char            narr[4] = {'a','a','a','a'};
		wchar_t         wide[4] = {L'a',L'a',L'a',L'a'};
		std::string     cstr    = "aaaa";
		std::wstring    wstr    = L"aaaa";
		Resize(narr, 2); Resize(narr, 3, 'b'); CHECK(Equal(narr, "aab"));
		Resize(wide, 2); Resize(wide, 3, 'b'); CHECK(Equal(wide, "aab"));
		Resize(cstr, 2); Resize(cstr, 3, 'b'); CHECK(Equal(cstr, "aab"));
		Resize(wstr, 2); Resize(wstr, 3, 'b'); CHECK(Equal(wstr, "aab"));
	}
	TEST(Assign)
	{
		char*           src0 = "str";
		std::string     src1 = "str";
		char            narr[4];
		wchar_t         wide[4];
		std::string     cstr;
		std::wstring    wstr;
		Assign(src0, src0 + 3, 0, narr, 4);             CHECK(Equal(narr, "str"));
		Assign(src1.begin(), src1.end(), 0, wide, 4);   CHECK(Equal(wide, "str"));
		Assign(src0, src0 + 3, 0, cstr);                CHECK(Equal(cstr, "str"));
		Assign(src0, src0 + 3, 0, wstr);                CHECK(Equal(wstr, "str"));
	}
	TEST(FindChar)
	{
		std::string src = "str";
		wchar_t ch = L't';
		CHECK(*FindChar(src.begin(), src.end(), ch) == 't');
		CHECK(*FindChar(src.c_str(), ch) == 't');
	}
	TEST(FindStr)
	{
		char src[] = "string";
		CHECK(FindStr(src, src+sizeof(src), "in") == &src[3]);
		CHECK(FindStr(src, "in") == &src[3]);
	}
	TEST(FindFirst)
	{
		char         narr[] = "AaBbAaBb";
		wchar_t      wide[] = L"AaBbAaBb";
		std::string  cstr   = "AaBbAaBb";
		std::wstring wstr   = L"AaBbAaBb";
		CHECK(Equal(FindFirst(narr, IsOneOf<>("bB")), "BbAaBb"));
		CHECK(Equal(FindFirst(wide, NotOneOf<>("AaB")), "bAaBb"));
		CHECK(*FindFirst(narr, IsOneOf<>("c")) == 0);
		CHECK_EQUAL(3, FindFirst(cstr.begin(), cstr.end(), IsOneOf<>("b")) - cstr.begin());
		CHECK_EQUAL(2, FindFirst(wstr.begin(), wstr.end(), NotOneOf<>("Aab")) - wstr.begin());
		CHECK(wstr.end() == FindFirst(wstr.begin(), wstr.end(), NotOneOf<>("AabB")));
	}
	TEST(FindLast)
	{
		char         narr[] = "AaBbAaBb";
		wchar_t      wide[] = L"AaBbAaBb";
		std::string  cstr   = "AaBbAaBb";
		std::wstring wstr   = L"AaBbAaBb";
		CHECK(Equal(FindLast(narr, IsOneOf<>("bB")), "b"));
		CHECK(Equal(FindLast(wide, NotOneOf<>("ABb")), "aBb"));
		CHECK(*FindLast(narr, IsOneOf<>("c")) == 0);
		CHECK_EQUAL(6, FindLast(cstr.begin(), cstr.end(), IsOneOf<>("B")) - cstr.begin());
		CHECK_EQUAL(4, FindLast(wstr.begin(), wstr.end(), NotOneOf<>("Bab")) - wstr.begin());
		CHECK(wstr.end() == FindLast(wstr.begin(), wstr.end(), NotOneOf<>("AabB")));
	}
	TEST(FindFirstOf)
	{
		char         narr[] = "AaAaAa";
		wchar_t      wide[] = L"AaAaAa";
		std::string  cstr   = "AaAaAa";
		std::wstring wstr   = L"AaAaAa";
		CHECK(Equal(FindFirstOf(narr, "A"), "AaAaAa"));
		CHECK(Equal(FindFirstOf(wide, "a"), "aAaAa"));
		CHECK(*FindFirstOf(wide, "B") == 0);
		CHECK_EQUAL(0, FindFirstOf(cstr.begin(), cstr.end(), "A") - cstr.begin());
		CHECK_EQUAL(1, FindFirstOf(wstr.begin(), wstr.end(), "a") - wstr.begin());
		CHECK(wstr.end() == FindFirstOf(wstr.begin(), wstr.end(), "B"));
	}
	TEST(FindLastOf)
	{
		char         narr[] = "AaAaAa";
		wchar_t      wide[] = L"AaAaa";
		std::string  cstr   = "AaAaaa";
		std::wstring wstr   = L"Aaaaa";
		CHECK(Equal(FindLastOf(narr, L"A"), "Aa"));
		CHECK(Equal(FindLastOf(wide, L"A"), "Aaa"));
		CHECK(*FindLastOf(wide, L"B") == 0);
		CHECK_EQUAL(2, FindLastOf(cstr.begin(), cstr.end(), "A") - cstr.begin());
		CHECK_EQUAL(0, FindLastOf(wstr.begin(), wstr.end(), "A") - wstr.begin());
		CHECK(wstr.end() == FindLastOf(wstr.begin(), wstr.end(), "B"));
	}
	TEST(FindFirstNotOf)
	{
		char         narr[] = "junk_str_junk";
		wchar_t      wide[] = L"junk_str_junk";
		std::string  cstr   = "junk_str_junk";
		std::wstring wstr   = L"junk_str_junk";
		CHECK(Equal(FindFirstNotOf(narr, "_knuj"), "str_junk"));
		CHECK(Equal(FindFirstNotOf(wide, "_knuj"), "str_junk"));
		CHECK(*FindFirstNotOf(wide, "_knujstr") == 0);
		CHECK_EQUAL(5, FindFirstNotOf(cstr.begin(), cstr.end(), "_knuj") - cstr.begin());
		CHECK_EQUAL(5, FindFirstNotOf(wstr.begin(), wstr.end(), "_knuj") - wstr.begin());
		CHECK(wstr.end() == FindFirstNotOf(wstr.begin(), wstr.end(), "_knujstr"));
	}
	TEST(FindLastNotOf)
	{
		char         narr[] = "junk_str_junk";
		wchar_t      wide[] = L"junk_str_junk";
		std::string  cstr   = "junk_str_junk";
		std::wstring wstr   = L"junk_str_junk";
		CHECK(Equal(FindLastNotOf(narr, "_knuj"), "r_junk"));
		CHECK(Equal(FindLastNotOf(wide, "_knuj"), "r_junk"));
		CHECK(*FindLastNotOf(wide, "_knujstr") == 0);
		CHECK_EQUAL(7, FindLastNotOf(cstr.begin(), cstr.end(), "_knuj") - cstr.begin());
		CHECK_EQUAL(7, FindLastNotOf(wstr.begin(), wstr.end(), "_knuj") - wstr.begin());
		CHECK(wstr.end() == FindLastNotOf(wstr.begin(), wstr.end(), "_knujstr"));
	}
	TEST(UpperCase)
	{
		wchar_t src0[] = L"caSe";
		std::string dest0;
		CHECK(Equal(UpperCase(src0, dest0), L"CASE"));
		CHECK(Equal(UpperCase(src0), L"CASE"));
		
		wchar_t src1[] = L"caSe";
		wchar_t dest1[5];
		CHECK(Equal(UpperCase(src1, dest1, 5), L"CASE"));
		CHECK(Equal(UpperCase(src1), L"CASE"));
	}
	TEST(LowerCase)
	{
		wchar_t src0[] = L"caSe";
		std::string dest0;
		CHECK(Equal(LowerCase(src0, dest0), L"case"));
		CHECK(Equal(LowerCase(src0), L"case"));
		
		wchar_t src1[] = L"caSe";
		wchar_t dest1[5];
		CHECK(Equal(LowerCase(src1, dest1, 5), L"case"));
		CHECK(Equal(LowerCase(src1), L"case"));
	}
	TEST(SubStr)
	{
		char    narr[] = "SubstringExtract";
		wchar_t wide[] = L"SubstringExtract";
		
		std::string out0;
		SubStr(narr, 3, 6, out0);
		CHECK(Equal(out0, "string"));
		
		char out1[7];
		SubStr(wide, 3, 6, out1);
		CHECK(Equal(out1, "string"));
	}
	TEST(Split)
	{
		typedef std::vector<std::string> StrVec;
		char str[] = "1,,2,3,4";
		char res[][2] = {"1","","2","3","4"};
		
		StrVec buf;
		pr::str::Split(str, ",", std::back_inserter(buf));
		for (StrVec::const_iterator i = buf.begin(), iend = buf.end(); i != iend; ++i)
			CHECK(Equal(*i, res[i - buf.begin()]));
	}
	TEST(Trim)
	{
		char         narr[] = " \t,1234\n";
		wchar_t      wide[] = L" \t,1234\n";
		std::string  cstr   = " \t,1234\n";
		std::wstring wstr   = L" \t,1234\n";
		CHECK(Equal(Trim(narr, IsWhiteSpace<char>    ,true, true), ",1234"));
		CHECK(Equal(Trim(wide, IsWhiteSpace<wchar_t> ,true, true), L",1234"));
		CHECK(Equal(Trim(cstr, IsWhiteSpace<char>    ,true, false), ",1234\n"));
		CHECK(Equal(Trim(wstr, IsWhiteSpace<wchar_t> ,false, true), " \t,1234"));
	}
	TEST(TrimChars)
	{
		char         narr[] = " \t,1234\n";
		wchar_t      wide[] = L" \t,1234\n";
		std::string  cstr   = " \t,1234\n";
		std::wstring wstr   = L" \t,1234\n";
		CHECK(Equal(TrimChars(narr,  " \t,\n" ,true  ,true),  "1234"));
		CHECK(Equal(TrimChars(wide, L" \t,\n" ,true  ,true), L"1234"));
		CHECK(Equal(TrimChars(cstr,  " \t,\n" ,true  ,false),  "1234\n"));
		CHECK(Equal(TrimChars(wstr, L" \t,\n" ,false ,true), L" \t,1234"));
	}
	// Extract *****************************************************************************
	TEST(ExtractLine)
	{
		wchar_t const* src = L"abcefg\n";
		char line[10];
		CHECK(ExtractLineC(line, src, false)); CHECK(Equal(line, "abcefg"));
		CHECK(ExtractLineC(line, src, true));  CHECK(Equal(line, "abcefg\n"));
	}
	TEST(ExtractIdentifier)
	{
		wchar_t const* src = L"\t\n\r Ident { 10.9 }";
		wchar_t const* s = src;
		char identifier[10];
		CHECK(ExtractIdentifier(identifier, s));
		CHECK(Equal(identifier, "Ident"));
	}
	TEST(ExtractString)
	{
		std::wstring src = L"\n \"String String\" ";
		wchar_t const* s = src.c_str();
		char string[20];
		CHECK(ExtractString(string, s));
		CHECK(Equal(string, "String String"));
	}
	TEST(ExtractCString)
	{
		std::wstring wstr;
		CHECK(ExtractCStringC(wstr, "  \" \\\\\\b\\f\\n\\r\\t\\v\\?\\'\\\" \" "));
		CHECK(Equal(wstr, " \\\b\f\n\r\t\v\?\'\" "));
		
		char narr[2];
		CHECK(ExtractCStringC(narr, "  '\\n'  "));
		CHECK(Equal(narr, "\n"));
		CHECK(ExtractCStringC(narr, "  'a'  "));
		CHECK(Equal(narr, "a"));
	}
	TEST(ExtractBool)
	{
		char  src[] = "true false 1";
		char const* s = src;
		bool  bbool = 0;
		int   ibool = 0;
		float fbool = 0;
		CHECK(ExtractBool(bbool, s)); CHECK_EQUAL(true, bbool);
		CHECK(ExtractBool(ibool, s)); CHECK_EQUAL(0   , ibool);
		CHECK(ExtractBool(fbool, s)); CHECK_EQUAL(1.0f, fbool);
	}
	TEST(ExtractInt)
	{
		char  c = 0;      unsigned char  uc = 0;
		short s = 0;      unsigned short us = 0;
		int   i = 0;      unsigned int   ui = 0;
		long  l = 0;      unsigned long  ul = 0;
		long long ll = 0; unsigned long long ull = 0;
		float  f = 0;     double d = 0;
		{
			char src[] = "\n -1.14 ";
			CHECK(ExtractIntC(c  ,10 ,src));   CHECK_EQUAL(-1, c);
			CHECK(ExtractIntC(uc ,10 ,src));   CHECK_EQUAL(0xff, uc);
			CHECK(ExtractIntC(s  ,10 ,src));   CHECK_EQUAL(-1, s);
			CHECK(ExtractIntC(us ,10 ,src));   CHECK_EQUAL(0xffff, us);
			CHECK(ExtractIntC(i  ,10 ,src));   CHECK_EQUAL(-1, i);
			CHECK(ExtractIntC(ui ,10 ,src));   CHECK_EQUAL(0xffffffff, ui);
			CHECK(ExtractIntC(l  ,10 ,src));   CHECK_EQUAL(-1, l);
			CHECK(ExtractIntC(ul ,10 ,src));   CHECK_EQUAL(0xffffffff, ul);
			CHECK(ExtractIntC(ll ,10 ,src));   CHECK_EQUAL(-1, ll);
			CHECK(ExtractIntC(ull,10 ,src));   CHECK_EQUAL(0xffffffffffffffffL, ull);
			CHECK(ExtractIntC(f  ,10 ,src));   CHECK_EQUAL(-1.0f, f);
			CHECK(ExtractIntC(d  ,10 ,src));   CHECK_EQUAL(-1.0, d);
		}
		{
			char src[] = "0x1abcZ", *ptr = src;
			CHECK(ExtractInt(i,0,ptr));
			CHECK_EQUAL(0x1abc, i);
			CHECK_EQUAL('Z', *ptr);
		}
	}
	TEST(ExtractReal)
	{
		float f = 0; double d = 0; int i = 0;
		{
			char src[] = "\n 3.14 ";
			CHECK(ExtractRealC(f  ,src));   CHECK_CLOSE(3.14, f, 0.00001f);
			CHECK(ExtractRealC(d  ,src));   CHECK_CLOSE(3.14, d, 0.00001f);
			CHECK(ExtractRealC(i ,src));    CHECK_EQUAL(3, i);
		}
		{
			char src[] = "-1.25e-4Z", *ptr = src;
			CHECK(ExtractReal(d, ptr));
			CHECK_EQUAL(-1.25e-4, d);
			CHECK_EQUAL('Z', *ptr);
		}
	}
	TEST(ExtractBoolArray)
	{
		char src[] = "\n true 1 TRUE ";
		float f[3] = {0,0,0};
		CHECK(ExtractBoolArrayC(f, 3, src));
		CHECK_EQUAL(1.0f, f[0]);
		CHECK_EQUAL(1.0f, f[1]);
		CHECK_EQUAL(1.0f, f[2]);
	}
	TEST(ExtractRealArray)
	{
		char src[] = "\n 3.14\t3.14e0\n-3.14 ";
		float  f[3] = {0,0,0};
		double d[3] = {0,0,0};
		int    i[3] = {0,0,0};
		CHECK(ExtractRealArrayC(f, 3, src));    CHECK_CLOSE(3.14f, f[0], 0.00001f); CHECK_CLOSE(3.14f, f[1], 0.00001f); CHECK_CLOSE(-3.14f, f[2], 0.00001f);
		CHECK(ExtractRealArrayC(d, 3, src));    CHECK_CLOSE(3.14 , d[0], 0.00001);  CHECK_CLOSE(3.14 , d[1], 0.00001);  CHECK_CLOSE(-3.14 , d[2], 0.00001);
		CHECK(ExtractRealArrayC(i, 3, src));    CHECK_EQUAL(3, i[0]);               CHECK_EQUAL(3, i[1]);               CHECK_EQUAL(-3, i[2]);
	}
	TEST(ExtractIntArray)
	{
		char src[] = "\n \t3  1 \n -2\t ";
		int i[3] = {0,0,0};
		unsigned int u[3] = {0,0,0};
		float        f[3] = {0,0,0};
		double       d[3] = {0,0,0};
		CHECK(ExtractIntArrayC(i, 3, 10, src)); CHECK_EQUAL(3, i[0]);               CHECK_EQUAL(1, i[1]);               CHECK_EQUAL(-2, i[2]);
		CHECK(ExtractIntArrayC(u, 3, 10, src)); CHECK_EQUAL(3, i[0]);               CHECK_EQUAL(1, i[1]);               CHECK_EQUAL(-2, i[2]);
		CHECK(ExtractIntArrayC(f, 3, 10, src)); CHECK_CLOSE(3.f, f[0], 0.00001f);   CHECK_CLOSE(1.f, f[1], 0.00001f);   CHECK_CLOSE(-2.f, f[2], 0.00001f);
		CHECK(ExtractIntArrayC(d, 3, 10, src)); CHECK_CLOSE(3.0, d[0], 0.00001);    CHECK_CLOSE(1.0, d[1], 0.00001);    CHECK_CLOSE(-2.0, d[2], 0.00001);
	}
	TEST(ExtractNumber)
	{
		char    src0[] =  "-3.24e-39f";
		wchar_t src1[] = L"0x123abcUL";
		char    src2[] =  "01234567";
		wchar_t src3[] = L"-34567L";
		
		float f = 0; int i = 0; bool fp = false;
		CHECK(ExtractNumberC(i,f,fp,src0)); CHECK(fp);  CHECK_EQUAL(-3.24e-39f, f);
		CHECK(ExtractNumberC(i,f,fp,src1)); CHECK(!fp); CHECK_EQUAL(0x123abcUL, (unsigned long)i);
		CHECK(ExtractNumberC(i,f,fp,src2)); CHECK(!fp); CHECK_EQUAL(01234567, i);
		CHECK(ExtractNumberC(i,f,fp,src3)); CHECK(!fp); CHECK_EQUAL(-34567L, (long)i);
	}
	// Utility *****************************************************************************
	TEST(EnsureNewline)
	{
		std::string thems_without   = "without";
		std::wstring thems_with     = L"with\n";
		EnsureNewline(thems_without);
		EnsureNewline(thems_with);
		CHECK_EQUAL('\n', *(--End(thems_without)));
		CHECK_EQUAL('\n', *(--End(thems_with)));
	}
	TEST(Contains)
	{
		std::string src = "string";
		CHECK(Contains(src, "in"));
		CHECK(Contains(src, "ing"));
		CHECK(ContainsNoCase(src, "iNg"));
		CHECK(ContainsNoCase(src, "inG"));
	}
	TEST(Compare)
	{
		std::string src = "string1";
		CHECK_EQUAL(-1, Compare(src, "string2"));
		CHECK_EQUAL(0, Compare(src, "string1"));
		CHECK_EQUAL(1, Compare(src, "string0"));
		CHECK_EQUAL(-1, Compare(src, "string11"));
		CHECK_EQUAL(1, Compare(src, "string"));
		CHECK_EQUAL(-1, CompareNoCase(src, "striNg2"));
		CHECK_EQUAL(0, CompareNoCase(src, "stRIng1"));
		CHECK_EQUAL(1, CompareNoCase(src, "strinG0"));
		CHECK_EQUAL(-1, CompareNoCase(src, "string11"));
		CHECK_EQUAL(1, CompareNoCase(src, "strinG"));
	}
	TEST(Count)
	{
		char            narr[] = "s0tr0";
		wchar_t         wide[] = L"s0tr0";
		std::string     cstr   = "s0tr0";
		std::wstring    wstr   = L"s0tr0";
		CHECK(Count(narr, "0t") == 1);
		CHECK(Count(wide, "0")  == 2);
		CHECK(Count(cstr, "0")  == 2);
		CHECK(Count(wstr, "0t") == 1);
	}
	TEST(CompressWhiteSpace)
	{
		char src[] = "\n\nstuff     with  \n  white\n   space   \n in   ";
		CompressWhiteSpace(src, " \n", ' ', true);
		CHECK_EQUAL("stuff with\nwhite\nspace\nin", src);
	}
	TEST(Tokenise)
	{
		char src[] = "tok0 tok1 tok2 \"tok3 and tok3\" tok4";
		std::vector<std::string> tokens;
		Tokenise(src, tokens);
		CHECK_EQUAL((int)5, (int)tokens.size());
		CHECK_EQUAL("tok0"          ,tokens[0]);
		CHECK_EQUAL("tok1"          ,tokens[1]);
		CHECK_EQUAL("tok2"          ,tokens[2]);
		CHECK_EQUAL("tok3 and tok3" ,tokens[3]);
		CHECK_EQUAL("tok4"          ,tokens[4]);
	}
	TEST(StripComments)
	{
		char src[] =    "//Line Comment\n"
						"Not a comment\n"
						"/* multi\n"
						"-line comment*/";
		CHECK_EQUAL("Not a comment\n", StripCppComments(src));
	}
	TEST(Replace)
	{
		char src[] = "Bite my shiny donkey metal donkey";
		CHECK_EQUAL(size_t(2), Replace(src, "donkey", "arse"));
		CHECK_EQUAL("Bite my shiny arse metal arse", src);
		CHECK_EQUAL(size_t(2), Replace(src, "arse", "donkey"));
		CHECK_EQUAL("Bite my shiny donkey metal donkey", src);
	}
	TEST(ConvertToCString)
	{
		char str[] = "Not a \"Cstring\". \a \b \f \n \r \t \v \\ \? \' ";
		char res[] = "Not a \\\"Cstring\\\". \\a \\b \\f \\n \\r \\t \\v \\\\ \\? \\\' ";
		
		std::string cstr1 = StringToCString<std::string>(str);
		CHECK(str::Equal(cstr1, res));
		
		std::string str1 = CStringToString<std::string>(cstr1);
		CHECK(str::Equal(str1, str));
	}
	TEST(FindIdentifier)
	{
		char str[] = "aid id iid    id aiden";
		wchar_t id[] = L"id";
		
		size_t idx = FindIdentifier(str, id);    CHECK(idx == 4);
		idx = FindIdentifier(str, id, idx+1, 3); CHECK(idx == 8);
		idx = FindIdentifier(str, id, idx+1);    CHECK(idx == 14);
		idx = FindIdentifier(str, id, idx+1);    CHECK(idx == 22);
	}
	TEST(Quotes)
	{
		char empty[3] = "";
		wchar_t one[4] = L"1";
		std::string two = "\"two\"";
		std::wstring three = L"three";
		
		CHECK(pr::str::Equal("\"\""       ,Quotes(empty ,true)));
		CHECK(pr::str::Equal("\"1\""      ,Quotes(one   ,true)));
		CHECK(pr::str::Equal("\"two\""    ,Quotes(two   ,true)));
		CHECK(pr::str::Equal(L"\"three\"" ,Quotes(three ,true)));
		
		CHECK(pr::str::Equal(""       ,Quotes(empty ,false)));
		CHECK(pr::str::Equal("1"      ,Quotes(one   ,false)));
		CHECK(pr::str::Equal("two"    ,Quotes(two   ,false)));
		CHECK(pr::str::Equal(L"three" ,Quotes(three ,false)));
	}
	TEST(ParseNumber)
	{
		char const* str = "-3.12e+03F,0x1234abcd,077,1ULL,";
		pr::str::NumType::Type type; bool unsignd; bool ll;
		char const* s = str;
		size_t count;
		
		count = pr::str::ParseNumber(s, type, unsignd, ll);
		CHECK_EQUAL(10, (int)count);
		CHECK_EQUAL(pr::str::NumType::FP, type);
		CHECK_EQUAL(false, unsignd);
		CHECK_EQUAL(false, ll);
		
		s += 1;
		count = pr::str::ParseNumber(s, type, unsignd, ll);
		CHECK_EQUAL(10, (int)count);
		CHECK_EQUAL(pr::str::NumType::Hex, type);
		CHECK_EQUAL(false, unsignd);
		CHECK_EQUAL(false, ll);
		
		s += 1;
		count = pr::str::ParseNumber(s, type, unsignd, ll);
		CHECK_EQUAL(3, (int)count);
		CHECK_EQUAL(pr::str::NumType::Oct, type);
		CHECK_EQUAL(false, unsignd);
		CHECK_EQUAL(false, ll);
		
		s += 1;
		count = pr::str::ParseNumber(s, type, unsignd, ll);
		CHECK_EQUAL(4, (int)count);
		CHECK_EQUAL(pr::str::NumType::Dec, type);
		CHECK_EQUAL(true, unsignd);
		CHECK_EQUAL(true, ll);
	}
	TEST(ConvertAWString)
	{
		char         narr[] =  "junk_str_junk";
		wchar_t      wide[] = L"junk_str_junk";
		std::string  cstr   =  "junk_str_junk";
		std::wstring wstr   = L"junk_str_junk";
		pr::string<> pstr   =  "junk_str_junk";
		
		CHECK(pr::str::ToWString<std::wstring>(narr) == wstr);
		CHECK(pr::str::ToWString<std::wstring>(wide) == wstr);
		CHECK(pr::str::ToWString<std::wstring>(cstr) == wstr);
		CHECK(pr::str::ToWString<std::wstring>(wstr) == wstr);
		CHECK(pr::str::ToWString<std::wstring>(pstr) == wstr);
		
		CHECK(pr::str::ToAString<std::string>(narr) == cstr);
		CHECK(pr::str::ToAString<std::string>(wide) == cstr);
		CHECK(pr::str::ToAString<std::string>(cstr) == cstr);
		CHECK(pr::str::ToAString<std::string>(wstr) == cstr);
		CHECK(pr::str::ToAString<std::string>(pstr) == cstr);
	}
	// PrStdString *****************************************************************************
	TEST(PrStdString)
	{
		char const*    src = "abcdefghij";
		wchar_t const* wsrc = L"abcdefghij";
		std::string s0 = "std::string";
		
		pr::string<> str0;              CHECK(str0.empty());
		pr::string<> str1 = "Test1";    CHECK(str1 == "Test1");
		pr::string<> str2 = str1;       CHECK(str2 == str1);
		CHECK(str2.c_str() != str1.c_str());
		
		pr::string<> str3(str1, 2, pr::string<>::npos);
		CHECK(str3.compare("st1") == 0);
		
		pr::string<> str4 = s0;
		CHECK(str4 == pr::string<>(s0));

		pr::string<wchar_t> wstr0 = wsrc;
		CHECK(wstr0.compare(wsrc) == 0);
		
		str0.assign(10, 'A');                               CHECK(str0 == "AAAAAAAAAA");
		str1.assign(s0);                                    CHECK(str1 == "std::string");
		str2.assign("Test2");                               CHECK(str2 == "Test2");
		str4.assign(src, src + 6);                          CHECK(str4 == "abcdef");
		str4.assign(s0.begin(), s0.begin() + 5);            CHECK(str4 == "std::");
		
		str0.append(str1, 0, 3);                            CHECK(str0 == "AAAAAAAAAAstd");
		str1.append(str2);                                  CHECK(str1 == "std::stringTest2");
		str2.append(3, 'B');                                CHECK(str2 == "Test2BBB");
		str0.append("Hello", 4);                            CHECK(str0 == "AAAAAAAAAAstdHell");
		str0.append("o");                                   CHECK(str0 == "AAAAAAAAAAstdHello");
		str4.append(s0.begin()+7, s0.end());                CHECK(str4 == "std::ring");
		wstr0.append(4, L'x');                              CHECK(wstr0 == L"abcdefghijxxxx");
		
		str0.insert(2, 3, 'C');                             CHECK(str0 == "AACCCAAAAAAAAstdHello");
		str1.insert(str1.begin(), 'D');                     CHECK(str1 == "Dstd::stringTest2");
		str2.insert(str2.begin());                          CHECK(str2[0] == 0 && !str2.empty());
		str3.insert(2, pr::string<>("and"));                CHECK(str3 == "stand1");
		
		str0.erase(0, 13);                                  CHECK(str0 == "stdHello");
		str2.erase(0, 1);                                   CHECK(str2 == "Test2BBB");
		str2.erase(str2.begin()+4);                         CHECK(str2 == "TestBBB");
		str2.erase(str2.begin()+4, str2.begin()+7);         CHECK(str2 == "Test");
		str2 += "2BBB";
		
		CHECK(str0.compare(1, 2, "te", 2) < 0);
		CHECK(str1.compare(1, 5, pr::string<>("Dstd::"), 1, 5) == 0);
		CHECK(str2.compare(pr::string<>("Test2BBB")) == 0);
		CHECK(str0.compare(0, 2, pr::string<>("sr")) > 0);
		CHECK(str1.compare("Dstd::string") > 0);
		CHECK(str2.compare(5, 3, "BBB") == 0);
		
		str0.clear();                                       CHECK(str0.empty() && str0.capacity() == str0.LocalLength - 1);
		CHECK(::size_t(str1.end() - str1.begin()) == str1.size());
		str1.resize(0);                                     CHECK(str1.empty());
		str1.push_back('E');                                CHECK(str1.size() == 1 && str1[0] == 'E');
		
		str0 = pr::string<>("Test0");                       CHECK(str0 == "Test0");
		str1 = "Test1";                                     CHECK(str1 == "Test1");
		str2 = 'F';                                         CHECK(str2 == "F");
		
		str0 += pr::string<>("Pass");                       CHECK(str0 == "Test0Pass");
		str1 += "Pass";                                     CHECK(str1 == "Test1Pass");
		str2 += 'G';                                        CHECK(str2 == "FG");
		
		str0 = pr::string<>("Jin") + pr::string<>("Jang");  CHECK(str0 == "JinJang");
		str1 = pr::string<>("Purple") + "Monkey";           CHECK(str1 == "PurpleMonkey");
		str2 = pr::string<>("H") + 'I';                     CHECK(str2 == "HI");
		
		wstr0 = L"A";                                       CHECK(wstr0 == L"A");
		wstr0 += L'b';                                      CHECK(wstr0 == L"Ab");

		CHECK(pr::string<>("A") == pr::string<>("A"));
		CHECK(pr::string<>("A") != pr::string<>("B"));
		CHECK(pr::string<>("A")  < pr::string<>("B"));
		CHECK(pr::string<>("B")  > pr::string<>("A"));
		CHECK(pr::string<>("A") <= pr::string<>("AB"));
		CHECK(pr::string<>("B") >= pr::string<>("B"));
		
		CHECK(str0.find("Jang", 1, 4) == 3);
		CHECK(str0.find(pr::string<>("ang"), 2) == 4);
		CHECK(str0.find_first_of(pr::string<>("n"), 0) == 2);
		CHECK(str0.find_first_of("J", 1, 1) == 3);
		CHECK(str0.find_first_of("J", 0) == 0);
		CHECK(str0.find_first_of('n', 3) == 5);
		CHECK(str0.find_last_of(pr::string<>("n"), pr::string<>::npos) == 5);
		CHECK(str0.find_last_of("J", 3, 1) == 3);
		CHECK(str0.find_last_of("J", pr::string<>::npos) == 3);
		CHECK(str0.find_last_of('a', pr::string<>::npos) == 4);
		CHECK(str0.find_first_not_of(pr::string<>("Jin"), 0) == 4);
		CHECK(str0.find_first_not_of("ing", 1, 3) == 3);
		CHECK(str0.find_first_not_of("inJ", 0) == 4);
		CHECK(str0.find_first_not_of('J', 1) == 1);
		CHECK(str0.find_last_not_of(pr::string<>("Jang"), pr::string<>::npos) == 1);
		CHECK(str0.find_last_not_of("Jang", 4, 4) == 1);
		CHECK(str0.find_last_not_of("an", 5) == 3);
		CHECK(str0.find_last_not_of('n', 5) == 4);
		
		CHECK(str1.substr(6, 4) == "Monk");
		
		str0.resize(0);
		for (int i = 0; i != 500; ++i)
		{
			str0.insert(str0.begin() ,'A'+(i%24));
			str0.insert(str0.end()   ,'A'+(i%24));
			CHECK((int)str0.size() == (1+i) * 2);
		}
		
		str4 = "abcdef";
		std::string stdstr = str4;     CHECK(pr::str::Equal(stdstr, str4));
		stdstr = str3;                 CHECK(pr::str::Equal(stdstr, str3));
		
		std::string str5 = "ABCDEFG";
		str5.replace(1, 3, "bcde", 2);
		CHECK(str5.size() == 6);
		
		pr::string<> str6 = "abcdefghij";
		str6.replace(0, 3, pr::string<>("AB"));              CHECK(str6 == "ABdefghij");
		str6.replace(3, 3, pr::string<>("DEFGHI"), 1, 3);    CHECK(str6 == "ABdEFGhij");
		str6.replace(1, pr::string<>::npos, "bcdefghi", 4);  CHECK(str6 == "Abcde");
		str6.replace(1, pr::string<>::npos, "bcdefghi");     CHECK(str6 == "Abcdefghi");
		str6.replace(4, 20, 3, 'X');                         CHECK(str6 == "AbcdXXX");
		
		// Test move constructor/assignment
#if _MSC_VER >= 1600
		pr::string<> str7 = "my_string";
		pr::string<> str8 = std::move(str7);
		CHECK(str7.empty());
		CHECK(str8 == "my_string");
		
		pr::string<char,4> str9 = "very long string that has been allocated";
		pr::string<char,8> str10 = "a different very long string that's been allocated";
		str10 = std::move(str9);
		CHECK(str9.empty());
		CHECK(str10 == "very long string that has been allocated");
		CHECK(str9.c_str() != str10.c_str());
#endif
	}
}
