//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "UnitTest++/1.3/Src/UnitTest++.h"
#include "UnitTest++/1.3/Src/ReportAssert.h"
#include <vector>
#include "pr/common/PRString.h"
#include "pr/common/Stack.h"
#include "pr/common/ScriptParser.h"
#include "pr/maths/maths.h"

struct ScriptString
{
	std::string m_str[5];
	ScriptString()
	{
		m_str[0] =	"*Keyword #eval{2*sin(0.5)}\n"
					"*Section { /*block comment*/ }\n"
					"*LineComment // comments here\n";
		m_str[1] =	"*Identifier\n"
					"*String \"simple string\"\n"
					"*CString \"C:\\\\Path\\\\Filename.txt\"\n"
					"*Bool true\n"
					"*Intg -23\n"
					"*Real -2.3e+3\n"
					"*BoolArray 1 0 true false\n"
					"*IntArray -3 2 +1 -0\n"
					"*RealArray 2.3 -1.0e-1 2 -0.2\n"
					"*Vector3 1.0 2.0 3.0\n"
					"*Vector4 4.0 3.0 2.0 1.0\n"
					"*Quaternion 0.0 -1.0 -2.0 -3.0\n"
					"*M3x3 1.0 0.0 0.0  0.0 1.0 0.0  0.0 0.0 1.0\n"
					"*M4x4 1.0 0.0 0.0 0.0  0.0 1.0 0.0 0.0  0.0 0.0 1.0 0.0  0.0 0.0 0.0 1.0\n"
					"*Junk\n"
					"*Section { *SubSection {} }\n";
		m_str[2] =	"#include \"0\"\n"
					"#include \"1\"\n";
	}
	char const* operator [](int i) const { return m_str[i].c_str(); }
} g_script_string;
	
struct TestIncludeHandler
{
	bool Load(char const* include_string, char const*& iter)
	{
		int index; pr::str::ExtractInt(index, 10, include_string);
		iter = g_script_string[index];
		return true;
	}
};

using namespace pr;

typedef pr::ScriptParser
<
	char const*,
	pr::script::StubFailPolicy,
	TestIncludeHandler
> SScriptParser;

//namespace {
SUITE(PRScript)
{
	TEST(TestScript)
	{
		bool bval = false, barray[4];
		int ival = 0, iarray[4];
		float fval = 0.0f, farray[4];
		v4 vec;
		Quat quat;
		m3x3 mat3;
		m4x4 mat4;
		std::string kw, str;
		SScriptParser parser;

		parser.SetSource(g_script_string[0]);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Keyword"));
		CHECK(parser.ExtractReal(fval));		CHECK_CLOSE(0.958851f, fval, 0.00001f);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Section"));
		CHECK(parser.FindSectionStart());		CHECK(parser.FindSectionEnd());
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "LineComment"));

		parser.SetSource(g_script_string[2]);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Keyword"));
		CHECK(parser.ExtractReal(fval));		CHECK_CLOSE(0.958851f, fval, 0.00001f);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Section"));
		CHECK(parser.FindSectionStart());		CHECK(parser.FindSectionEnd());
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "LineComment"));

		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Identifier"));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "String"));
		CHECK(parser.ExtractString(str));		CHECK(str::Equal(str, "simple string"));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "CString"));
		CHECK(parser.ExtractCString(str));		CHECK(str::Equal(str, "C:\\Path\\Filename.txt"));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Bool"));
		CHECK(parser.ExtractBool(bval));		CHECK_EQUAL(true, bval);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Intg"));
		CHECK(parser.ExtractInt(ival, 10));		CHECK_EQUAL(-23, ival);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Real"));
		CHECK(parser.ExtractReal(fval));		CHECK_CLOSE(-2.3e+3, fval, 0.00001f);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "BoolArray"));
		CHECK(parser.ExtractBoolArray(barray, 4));
		CHECK_EQUAL(true , barray[0]);
		CHECK_EQUAL(false, barray[1]);
		CHECK_EQUAL(true , barray[2]);
		CHECK_EQUAL(false, barray[3]);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "IntArray"));
		CHECK(parser.ExtractIntArray(iarray, 4, 10));
		CHECK_EQUAL(-3, iarray[0]);
		CHECK_EQUAL(+2, iarray[1]);
		CHECK_EQUAL(+1, iarray[2]);
		CHECK_EQUAL(-0, iarray[3]);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "RealArray"));
		CHECK(parser.ExtractRealArray(farray, 4));
		CHECK_EQUAL(2.3f    , farray[0]);
		CHECK_EQUAL(-1.0e-1f, farray[1]);
		CHECK_EQUAL(+2.0f   , farray[2]);
		CHECK_EQUAL(-0.2f   , farray[3]);
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Vector3"));
		CHECK(parser.ExtractVector3(vec,-1.0f));CHECK(FEql4(vec, v4::make(1.0f, 2.0f, 3.0f,-1.0f)));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Vector4"));
		CHECK(parser.ExtractVector4(vec));		CHECK(FEql4(vec, v4::make(4.0f, 3.0f, 2.0f, 1.0f)));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "Quaternion"));
		CHECK(parser.ExtractQuaternion(quat));	CHECK(FEql(quat, Quat::make(0.0f, -1.0f, -2.0f, -3.0f)));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "M3x3"));
		CHECK(parser.Extractm3x3(mat3));		CHECK(FEql(mat3, m3x3Identity));
		CHECK(parser.GetKeyword(kw));			CHECK(str::Equal(kw, "M4x4"));
		CHECK(parser.Extractm4x4(mat4));		CHECK(FEql(mat4, m4x4Identity));
		CHECK(parser.FindKeyword("Section", 7, true));
		CHECK(parser.ExtractSection(str));		CHECK(str::Equal(str, " *SubSection {} "));
		CHECK(!parser.IsKeyword());
		CHECK(!parser.IsSectionStart());
		CHECK(!parser.IsSectionEnd());
		CHECK(parser.IsSourceEnd());
	}
}//SUITE(PRScript)
