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
	namespace script
	{
		using HashValue = int;
		using string = pr::string<wchar_t>;

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
			x(Auto     ,"auto"     ,= 0x187C3F6C)\
			x(Double   ,"double"   ,= 0x8FFA9612)\
			x(Int      ,"int"      ,= 0x5CE21D70)\
			x(Struct   ,"struct"   ,= 0xA98D0AFE)\
			x(Break    ,"break"    ,= 0xFDDA6A72)\
			x(Else     ,"else"     ,= 0x5A7F7A9E)\
			x(Long     ,"long"     ,= 0x241D4609)\
			x(Switch   ,"switch"   ,= 0x3776015F)\
			x(Case     ,"case"     ,= 0x0F909A23)\
			x(Enum     ,"enum"     ,= 0x795FE4E6)\
			x(Register ,"register" ,= 0xD2E90C52)\
			x(Typedef  ,"typedef"  ,= 0x5C019616)\
			x(Char     ,"char"     ,= 0x10C1DF07)\
			x(Extern   ,"extern"   ,= 0xEC53F561)\
			x(Return   ,"return"   ,= 0x5FBA9DDD)\
			x(Union    ,"union"    ,= 0xBD3984B6)\
			x(Const    ,"const"    ,= 0xAF749E3A)\
			x(Float    ,"float"    ,= 0x906E736B)\
			x(Short    ,"short"    ,= 0x0E57FE17)\
			x(Unsigned ,"unsigned" ,= 0xD0208C58)\
			x(Continue ,"continue" ,= 0x29BDDE56)\
			x(For      ,"for"      ,= 0xB8CCEE6A)\
			x(Signed   ,"signed"   ,= 0x0AF75B8B)\
			x(Void     ,"void"     ,= 0xB2D22281)\
			x(Default  ,"default"  ,= 0x96B0DEA8)\
			x(Goto     ,"goto"     ,= 0xC278F5C4)\
			x(Sizeof   ,"sizeof"   ,= 0x7693BDE7)\
			x(Volatile ,"volatile" ,= 0xB225EF47)\
			x(Do       ,"do"       ,= 0x6E06602A)\
			x(If       ,"if"       ,= 0x64F9860C)\
			x(Static   ,"static"   ,= 0x3D26EFB5)\
			x(While    ,"while"    ,= 0x8D56B330)
		PR_DEFINE_ENUM3(EKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Preprocessor keywords
		#define PR_ENUM(x)\
			x(Invalid      ,""             ,= 0xffffffff)\
			x(Include      ,"include"      ,= 0x1D27DEED)\
			x(IncludePath  ,"include_path" ,= 0xB541601D)\
			x(Define       ,"define"       ,= 0x3E7EDBEC)\
			x(Undef        ,"undef"        ,= 0xA6236687)\
			x(Defifndef    ,"defifndef"    ,= 0x48F0434C)\
			x(If           ,"if"           ,= 0x64F9860C)\
			x(Ifdef        ,"ifdef"        ,= 0x17E02913)\
			x(Ifndef       ,"ifndef"       ,= 0xB1F1232D)\
			x(Elif         ,"elif"         ,= 0x7970A0E1)\
			x(Else         ,"else"         ,= 0x5A7F7A9E)\
			x(Endif        ,"endif"        ,= 0x8774DC3B)\
			x(Pragma       ,"pragma"       ,= 0x7BE590B7)\
			x(Line         ,"line"         ,= 0x8A9A6F5D)\
			x(Error        ,"error"        ,= 0x221BB06F)\
			x(Warning      ,"warning"      ,= 0xBDC00819)\
			x(Defined      ,"defined"      ,= 0xE9B39718)\
			x(Eval         ,"eval"         ,= 0xAE926225)\
			x(Lit          ,"lit"          ,= 0x14C22A9C)\
			x(Embedded     ,"embedded"     ,= 0x82A3D0E5)
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

		// Helper for a generic character pointer
		union SrcConstPtr
		{
			wchar_t const* wptr;
			char    const* aptr;
			SrcConstPtr() :wptr() {}
			SrcConstPtr(wchar_t const* p) :wptr(p) {}
			SrcConstPtr(char const* p) :aptr(p) {}
		};

	}
}


















