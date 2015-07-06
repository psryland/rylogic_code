//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <string>
#include <deque>
#include <unordered_map>
#include <type_traits>
#include <locale>
#include <codecvt>
#include <cassert>
#include "pr/common/exception.h"
#include "pr/common/fmt.h"
#include "pr/common/scope.h"
#include "pr/common/expr_eval.h"
#include "pr/container/vector.h"
#include "pr/container/deque.h"
#include "pr/container/stack.h"
#include "pr/macros/enum.h"
#include "pr/str/string.h"
#include "pr/str/string_core.h"
#include "pr/str/string_util.h"
#include "pr/str/extract.h"

namespace pr
{
	namespace script2
	{
		#pragma region Enumerations

		#pragma region Tokens
		#define PR_ENUM(x) /*
			*/x(Invalid    ) /* Unknown
			*/x(EndOfStream) /* The end of the input stream
			*/x(Identifier ) /* An identifier
			*/x(Keyword    ) /* A script keyword
			*/x(Symbol     ) /* An operator or punctuator, e.g *, ->, +, ;, {, }, etc
			*/x(Constant   ) // A literal constant
		PR_DEFINE_ENUM1(EToken, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region C keywords
		#define PR_ENUM(x)\
			x(Invalid  ,""         ,= 0xffffffff)\
			x(Auto     ,"auto"     ,= 0x112746E9)\
			x(Double   ,"double"   ,= 0x1840D9CE)\
			x(Int      ,"int"      ,= 0x164A43DD)\
			x(Struct   ,"struct"   ,= 0x0F408D2A)\
			x(Break    ,"break"    ,= 0x1AC013EC)\
			x(Else     ,"else"     ,= 0x1D237859)\
			x(Long     ,"long"     ,= 0x14EF7164)\
			x(Switch   ,"switch"   ,= 0x13C0233F)\
			x(Case     ,"case"     ,= 0x18EA7F00)\
			x(Enum     ,"enum"     ,= 0x113F6121)\
			x(Register ,"register" ,= 0x1A14AAE9)\
			x(Typedef  ,"typedef"  ,= 0x1B494818)\
			x(Char     ,"char"     ,= 0x1E5760F8)\
			x(Extern   ,"extern"   ,= 0x16497B3B)\
			x(Return   ,"return"   ,= 0x0A01F36E)\
			x(Union    ,"union"    ,= 0x1E57F369)\
			x(Const    ,"const"    ,= 0x036F03E1)\
			x(Float    ,"float"    ,= 0x176B5BE3)\
			x(Short    ,"short"    ,= 0x1EDC8C0F)\
			x(Unsigned ,"unsigned" ,= 0x186A2B87)\
			x(Continue ,"continue" ,= 0x1E46A876)\
			x(For      ,"for"      ,= 0x0E37A24A)\
			x(Signed   ,"signed"   ,= 0x00BF0C54)\
			x(Void     ,"void"     ,= 0x1A9B029D)\
			x(Default  ,"default"  ,= 0x1C8CDD40)\
			x(Goto     ,"goto"     ,= 0x04D53061)\
			x(Sizeof   ,"sizeof"   ,= 0x1429164B)\
			x(Volatile ,"volatile" ,= 0x18AFC4C2)\
			x(Do       ,"do"       ,= 0x1D8B5FEF)\
			x(If       ,"if"       ,= 0x1DFA87FC)\
			x(Static   ,"static"   ,= 0x16150CE7)\
			x(While    ,"while"    ,= 0x0B4669DC)
		PR_DEFINE_ENUM3(EKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Preprocessor keywords
		#define PR_ENUM(x)\
			x(Invalid      ,""             ,= 0xffffffff)\
			x(Include      ,"include"      ,= 0x0A5F3FCE)\
			x(IncludePath  ,"include_path" ,= 0x1789F136)\
			x(Define       ,"define"       ,= 0x0D22697A)\
			x(Undef        ,"undef"        ,= 0x1450E770)\
			x(Defifndef    ,"defifndef"    ,= 0x1169dadd)\
			x(If           ,"if"           ,= 0x1DFA87FC)\
			x(Ifdef        ,"ifdef"        ,= 0x11FAC604)\
			x(Ifndef       ,"ifndef"       ,= 0x1FB3E42D)\
			x(Elif         ,"elif"         ,= 0x02414BD3)\
			x(Else         ,"else"         ,= 0x1D237859)\
			x(Endif        ,"endif"        ,= 0x15632E04)\
			x(Pragma       ,"pragma"       ,= 0x1EC9D08D)\
			x(Line         ,"line"         ,= 0x10D28008)\
			x(Error        ,"error"        ,= 0x0158FC8D)\
			x(Warning      ,"warning"      ,= 0x051535CD)\
			x(Defined      ,"defined"      ,= 0x019B9520)\
			x(Eval         ,"eval"         ,= 0x1531EC3D)\
			x(Lit          ,"lit"          ,= 0x15DF8629)\
			x(Embedded     ,"embedded"     ,= 0x0E5B2CFA)
		PR_DEFINE_ENUM3(EPPKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Script exception values/return codes
		#define PR_ENUM(x)\
			x(Success                         ,= 0)\
			x(Failed                          ,= 0x80000000)\
			x(InvalidIdentifier               ,)\
			x(InvalidString                   ,)\
			x(ParameterCountMismatch          ,)\
			x(UnexpectedEndOfFile             ,)\
			x(UnknownPreprocessorCommand      ,)\
			x(InvalidMacroDefinition          ,)\
			x(MacroNotDefined                 ,)\
			x(MacroAlreadyDefined             ,)\
			x(IncludesNotSupported            ,)\
			x(InvalidInclude                  ,)\
			x(MissingInclude                  ,)\
			x(InvalidPreprocessorDirective    ,)\
			x(UnmatchedPreprocessorDirective  ,)\
			x(PreprocessError                 ,)\
			x(SyntaxError                     ,)\
			x(ExpressionSyntaxError           ,)\
			x(EmbeddedCodeNotSupported        ,)\
			x(EmbeddedCodeSyntaxError         ,)\
			x(EmbeddedCodeExecutionFailed     ,)\
			x(TokenNotFound                   ,)\
			x(UnknownKeyword                  ,)\
			x(UnknownToken                    ,)\
			x(UnknownValue                    ,)\
			x(ValueNotFound                   ,)
		PR_DEFINE_ENUM2(EResult, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Symbols
		#define PR_ENUM(x)/*
			*/x(Invalid        ,""    ,=   0 ) /*
			*/x(WhiteSpace     ," "   ,= ' ' ) /* ' ', '\t', etc
			*/x(NewLine        ,"\n"  ,= '\n') /* '\n'
			*/x(Assign         ,"="   ,= '=' ) /* assign
			*/x(SemiColon      ,";"   ,= ';' ) /*
			*/x(Complement     ,"~"   ,= '~' ) /*
			*/x(Not            ,"!"   ,= '!' ) /*
			*/x(Ptr            ,"*"   ,= '*' ) /* pointer, dereference, or multiply
			*/x(AddressOf      ,"&"   ,= '&' ) /* address of, or bitwise-AND
			*/x(Plus           ,"+"   ,= '+' ) /* unary plus, or add
			*/x(Minus          ,"-"   ,= '-' ) /* unary negate, or subtract
			*/x(Divide         ,"/"   ,= '/' ) /* /
			*/x(Modulus        ,"%"   ,= '%' ) /* %
			*/x(LessThan       ,"<"   ,= '<' ) /* <
			*/x(GtrThan        ,">"   ,= '>' ) /* >
			*/x(BitOr          ,"|"   ,= '|' ) /* |
			*/x(BitXor         ,"^"   ,= '^' ) /* ^
			*/x(Comma          ,","   ,= ',' ) /* ,
			*/x(Conditional    ,"?"   ,= '?' ) /* ? (as in (bool) ? (statement) : (statement)
			*/x(BraceOpen      ,"{"   ,= '{' ) /* {
			*/x(BraceClose     ,"}"   ,= '}' ) /* }
			*/x(BracketOpen    ,"["   ,= '[' ) /* [
			*/x(BracketClose   ,"]"   ,= ']' ) /* ]
			*/x(ParenthOpen    ,"("   ,= '(' ) /* (
			*/x(ParenthClose   ,")"   ,= ')' ) /* )
			*/x(Dot            ,"."   ,= '.' ) /* .
			*/x(Colon          ,":"   ,= ':' ) /* :
			*/x(Hash           ,"#"   ,= '#' ) /* #
			*/x(Dollar         ,"$"   ,= '$' ) /* $
			*/x(At             ,"@"   ,= '@' ) /* @
			*/x(Increment      ,"++"  ,= 128 ) /* ++
			*/x(Decrement      ,"--"  ,= 129 ) /* --
			*/x(ShiftL         ,"<<"  ,= 130 ) /* <<
			*/x(ShiftR         ,">>"  ,= 131 ) /* >>
			*/x(LessEql        ,"<="  ,= 132 ) /* <=
			*/x(GtrEql         ,">="  ,= 133 ) /* >=
			*/x(Equal          ,"=="  ,= 134 ) /* ==
			*/x(NotEqual       ,"!="  ,= 135 ) /* !=
			*/x(LogicalAnd     ,"&&"  ,= 136 ) /* &&
			*/x(LogicalOr      ,"||"  ,= 137 ) /* ||
			*/x(ShiftLAssign   ,"<<=" ,= 138 ) /* <<=
			*/x(ShiftRAssign   ,">>=" ,= 139 ) /* >>=
			*/x(BitAndAssign   ,"&="  ,= 140 ) /* &=
			*/x(BitOrAssign    ,"|="  ,= 141 ) /* |=
			*/x(BitXorAssign   ,"^="  ,= 142 ) /* ^=
			*/x(AddAssign      ,"+="  ,= 143 ) /* +=
			*/x(SubAssign      ,"-="  ,= 144 ) /* -=
			*/x(MulAssign      ,"*="  ,= 145 ) /* *=
			*/x(DivAssign      ,"/="  ,= 146 ) /* /=
			*/x(ModAssign      ,"%="  ,= 147 ) /* %=
			*/x(Ellipsis       ,"..." ,= 148 ) // ...
		PR_DEFINE_ENUM3(ESymbol, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Literal types
		#define PR_ENUM(x)\
			x(Invalid       )\
			x(StringLiteral )\
			x(WStringLiteral)\
			x(Integral      )\
			x(FloatingPoint )
		PR_DEFINE_ENUM1(EConstant, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Source types, mainly used for debugging
		enum class ESrcType
		{
			Unknown,
			Null,
			Pointer,
			Range,
			Buffered,
			File,
			Eval,
			EmbeddedCode,
			Macro,
			Preprocessor,
		};
		#pragma endregion

		#pragma endregion
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_forward)
		{
			using namespace pr::script2;

			//auto hash   = [](char const* str){ return pr::hash::HashC(str); };
			//auto onfail = [](char const* msg){ PR_FAIL(msg); };
			//pr::CheckHashEnum<EKeyword>  (hash, onfail);
			//pr::CheckHashEnum<EPPKeyword>(hash, onfail);
		}
	}
}
#endif