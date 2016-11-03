//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <string>
#include <fstream>
#include <unordered_map>
#include <type_traits>
#include <locale>
#include <codecvt>
#include <cassert>
#include "pr/common/exception.h"
#include "pr/common/fmt.h"
#include "pr/common/hash.h"
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
			x(Invalid  ,""         ,= pr::hash::Hash(""         ))\
			x(Auto     ,"auto"     ,= pr::hash::Hash("auto"     ))\
			x(Double   ,"double"   ,= pr::hash::Hash("double"   ))\
			x(Int      ,"int"      ,= pr::hash::Hash("int"      ))\
			x(Struct   ,"struct"   ,= pr::hash::Hash("struct"   ))\
			x(Break    ,"break"    ,= pr::hash::Hash("break"    ))\
			x(Else     ,"else"     ,= pr::hash::Hash("else"     ))\
			x(Long     ,"long"     ,= pr::hash::Hash("long"     ))\
			x(Switch   ,"switch"   ,= pr::hash::Hash("switch"   ))\
			x(Case     ,"case"     ,= pr::hash::Hash("case"     ))\
			x(Enum     ,"enum"     ,= pr::hash::Hash("enum"     ))\
			x(Register ,"register" ,= pr::hash::Hash("register" ))\
			x(Typedef  ,"typedef"  ,= pr::hash::Hash("typedef"  ))\
			x(Char     ,"char"     ,= pr::hash::Hash("char"     ))\
			x(Extern   ,"extern"   ,= pr::hash::Hash("extern"   ))\
			x(Return   ,"return"   ,= pr::hash::Hash("return"   ))\
			x(Union    ,"union"    ,= pr::hash::Hash("union"    ))\
			x(Const    ,"const"    ,= pr::hash::Hash("const"    ))\
			x(Float    ,"float"    ,= pr::hash::Hash("float"    ))\
			x(Short    ,"short"    ,= pr::hash::Hash("short"    ))\
			x(Unsigned ,"unsigned" ,= pr::hash::Hash("unsigned" ))\
			x(Continue ,"continue" ,= pr::hash::Hash("continue" ))\
			x(For      ,"for"      ,= pr::hash::Hash("for"      ))\
			x(Signed   ,"signed"   ,= pr::hash::Hash("signed"   ))\
			x(Void     ,"void"     ,= pr::hash::Hash("void"     ))\
			x(Default  ,"default"  ,= pr::hash::Hash("default"  ))\
			x(Goto     ,"goto"     ,= pr::hash::Hash("goto"     ))\
			x(Sizeof   ,"sizeof"   ,= pr::hash::Hash("sizeof"   ))\
			x(Volatile ,"volatile" ,= pr::hash::Hash("volatile" ))\
			x(Do       ,"do"       ,= pr::hash::Hash("do"       ))\
			x(If       ,"if"       ,= pr::hash::Hash("if"       ))\
			x(Static   ,"static"   ,= pr::hash::Hash("static"   ))\
			x(While    ,"while"    ,= pr::hash::Hash("while"    ))
		PR_DEFINE_ENUM3(EKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Preprocessor keywords
		#define PR_ENUM(x)\
			x(Invalid      ,""             ,= pr::hash::Hash(""            ))\
			x(Include      ,"include"      ,= pr::hash::Hash("include"     ))\
			x(IncludePath  ,"include_path" ,= pr::hash::Hash("include_path"))\
			x(Define       ,"define"       ,= pr::hash::Hash("define"      ))\
			x(Undef        ,"undef"        ,= pr::hash::Hash("undef"       ))\
			x(Defifndef    ,"defifndef"    ,= pr::hash::Hash("defifndef"   ))\
			x(If           ,"if"           ,= pr::hash::Hash("if"          ))\
			x(Ifdef        ,"ifdef"        ,= pr::hash::Hash("ifdef"       ))\
			x(Ifndef       ,"ifndef"       ,= pr::hash::Hash("ifndef"      ))\
			x(Elif         ,"elif"         ,= pr::hash::Hash("elif"        ))\
			x(Else         ,"else"         ,= pr::hash::Hash("else"        ))\
			x(Endif        ,"endif"        ,= pr::hash::Hash("endif"       ))\
			x(Pragma       ,"pragma"       ,= pr::hash::Hash("pragma"      ))\
			x(Line         ,"line"         ,= pr::hash::Hash("line"        ))\
			x(Error        ,"error"        ,= pr::hash::Hash("error"       ))\
			x(Warning      ,"warning"      ,= pr::hash::Hash("warning"     ))\
			x(Defined      ,"defined"      ,= pr::hash::Hash("defined"     ))\
			x(Eval         ,"eval"         ,= pr::hash::Hash("eval"        ))\
			x(Lit          ,"lit"          ,= pr::hash::Hash("lit"         ))\
			x(Embedded     ,"embedded"     ,= pr::hash::Hash("embedded"    ))
		PR_DEFINE_ENUM3(EPPKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Script exception values/return codes
		#define PR_ENUM(x)\
			x(Success                         ,= 0)\
			x(Failed                          ,= 0x80000000)\
			x(InvalidIdentifier               ,)\
			x(InvalidString                   ,)\
			x(InvalidValue                    ,)\
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


















