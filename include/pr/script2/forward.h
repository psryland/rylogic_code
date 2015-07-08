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
		using HashValue = int;

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
			x(Auto     ,"auto"     ,= 0x21b616f0)\
			x(Double   ,"double"   ,= 0xb572894a)\
			x(Int      ,"int"      ,= 0xf4771206)\
			x(Struct   ,"struct"   ,= 0x25040de0)\
			x(Break    ,"break"    ,= 0xc3d29d82)\
			x(Else     ,"else"     ,= 0xdfcb6468)\
			x(Long     ,"long"     ,= 0x1544fe25)\
			x(Switch   ,"switch"   ,= 0x9893195)\
			x(Case     ,"case"     ,= 0x4a7c751b)\
			x(Enum     ,"enum"     ,= 0xa49a8a94)\
			x(Register ,"register" ,= 0x87f3e726)\
			x(Typedef  ,"typedef"  ,= 0x6be3d212)\
			x(Char     ,"char"     ,= 0xfccf20b7)\
			x(Extern   ,"extern"   ,= 0x94447857)\
			x(Return   ,"return"   ,= 0xe5511245)\
			x(Union    ,"union"    ,= 0xe2af7b0e)\
			x(Const    ,"const"    ,= 0x5a686410)\
			x(Float    ,"float"    ,= 0x86ed7e65)\
			x(Short    ,"short"    ,= 0x690dea7f)\
			x(Unsigned ,"unsigned" ,= 0xd1a5b19e)\
			x(Continue ,"continue" ,= 0x37b892be)\
			x(For      ,"for"      ,= 0x6c00786)\
			x(Signed   ,"signed"   ,= 0xab373275)\
			x(Void     ,"void"     ,= 0xf545fcd3)\
			x(Default  ,"default"  ,= 0x27ab006e)\
			x(Goto     ,"goto"     ,= 0xae8e15fc)\
			x(Sizeof   ,"sizeof"   ,= 0xd9bf6823)\
			x(Volatile ,"volatile" ,= 0x69d6188f)\
			x(Do       ,"do"       ,= 0xc003cebc)\
			x(If       ,"if"       ,= 0xe0f53580)\
			x(Static   ,"static"   ,= 0xcd88d6df)\
			x(While    ,"while"    ,= 0xe63f6e2a)
		PR_DEFINE_ENUM3(EKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Preprocessor keywords
		#define PR_ENUM(x)\
			x(Invalid      ,""             ,= 0xffffffff)\
			x(Include      ,"include"      ,= 0xdd4bbe11)\
			x(IncludePath  ,"include_path" ,= 0xd1a75ca1)\
			x(Define       ,"define"       ,= 0x1d8988c2)\
			x(Undef        ,"undef"        ,= 0x588f8a99)\
			x(Defifndef    ,"defifndef"    ,= 0x9b9ddb8c)\
			x(If           ,"if"           ,= 0xe0f53580)\
			x(Ifdef        ,"ifdef"        ,= 0xad2966dd)\
			x(Ifndef       ,"ifndef"       ,= 0x80d54379)\
			x(Elif         ,"elif"         ,= 0xf89ba339)\
			x(Else         ,"else"         ,= 0xdfcb6468)\
			x(Endif        ,"endif"        ,= 0xc610b415)\
			x(Pragma       ,"pragma"       ,= 0x943a2877)\
			x(Line         ,"line"         ,= 0xff066c61)\
			x(Error        ,"error"        ,= 0xf325c97d)\
			x(Warning      ,"warning"      ,= 0x869371af)\
			x(Defined      ,"defined"      ,= 0x7337d7bc)\
			x(Eval         ,"eval"         ,= 0xa4d87301)\
			x(Lit          ,"lit"          ,= 0xfcf70a8c)\
			x(Embedded     ,"embedded"     ,= 0x9bd1cba1)
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
