﻿//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************
#pragma once
#include <exception>
#include <memory>
#include <cuchar>
#include <string>
#include <string_view>
#include <array>
#include <unordered_map>
#include <fstream>
#include <type_traits>
#include <filesystem>
#include <functional>
#include <regex>
#include <locale>
#include <cassert>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"
#include "pr/common/hash.h"
#include "pr/common/cast.h"
#include "pr/common/scope.h"
#include "pr/common/flags_enum.h"
#include "pr/common/expr_eval.h"
#include "pr/container/vector.h"
#include "pr/container/stack.h"
#include "pr/maths/bit_fields.h"
#include "pr/macros/enum.h"
#include "pr/str/string_core.h"
#include "pr/str/string_util.h"
#include "pr/str/string_filter.h"
#include "pr/str/extract.h"
#include "pr/filesys/file_encoding.h"

namespace pr::script
{
	using char_t = wchar_t;
	using string_t = std::basic_string<char_t>;
	using string_view_t = std::basic_string_view<char_t>;
	using InLiteral = str::InLiteral;
	using InComment = str::InComment;

	// Script result codes
	enum class EResult
	{
		#define PR_ENUM(x)\
		x(Success                       )\
		x(Failed                        )\
		x(FileNotFound                  )\
		x(WrongEncoding                 )\
		x(InvalidIdentifier             )\
		x(InvalidString                 )\
		x(InvalidValue                  )\
		x(ParameterCountMismatch        )\
		x(UnexpectedEndOfFile           )\
		x(UnknownPreprocessorCommand    )\
		x(InvalidMacroDefinition        )\
		x(MacroNotDefined               )\
		x(MacroAlreadyDefined           )\
		x(IncludesNotSupported          )\
		x(InvalidInclude                )\
		x(MissingInclude                )\
		x(InvalidPreprocessorDirective  )\
		x(UnmatchedPreprocessorDirective)\
		x(PreprocessError               )\
		x(SyntaxError                   )\
		x(ExpressionSyntaxError         )\
		x(EmbeddedCodeNotSupported      )\
		x(EmbeddedCodeError             )\
		x(KeywordNotFound               )\
		x(TokenNotFound                 )\
		x(ValueNotFound                 )\
		x(UnknownKeyword                )\
		x(UnknownToken                  )\
		x(UnknownValue                  )
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(EResult, PR_ENUM);
	#undef PR_ENUM

	// Script token types
	enum class EToken
	{
		#define PR_ENUM(x)\
		x(Invalid    ) /* Unknown */\
		x(EndOfStream) /* The end of the input stream */\
		x(Identifier ) /* An identifier */\
		x(Keyword    ) /* A script keyword */\
		x(Symbol     ) /* An operator or punctuation, e.g *, ->, +, ;, {, }, etc */\
		x(Constant   ) /* A literal constant  */
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(EToken, PR_ENUM);
	#undef PR_ENUM

	// C/C++ keywords
	enum class EKeyword
	{
		#define PR_ENUM(x)\
		x(Invalid      ,""              ,= pr::hash::HashCT(""             ))\
		x(Auto         ,"auto"          ,= pr::hash::HashCT("auto"         ))\
		x(Double       ,"double"        ,= pr::hash::HashCT("double"       ))\
		x(Int          ,"int"           ,= pr::hash::HashCT("int"          ))\
		x(Struct       ,"struct"        ,= pr::hash::HashCT("struct"       ))\
		x(Break        ,"break"         ,= pr::hash::HashCT("break"        ))\
		x(Else         ,"else"          ,= pr::hash::HashCT("else"         ))\
		x(Long         ,"long"          ,= pr::hash::HashCT("long"         ))\
		x(Switch       ,"switch"        ,= pr::hash::HashCT("switch"       ))\
		x(Case         ,"case"          ,= pr::hash::HashCT("case"         ))\
		x(Enum         ,"enum"          ,= pr::hash::HashCT("enum"         ))\
		x(Register     ,"register"      ,= pr::hash::HashCT("register"     ))\
		x(Typedef      ,"typedef"       ,= pr::hash::HashCT("typedef"      ))\
		x(Char         ,"char"          ,= pr::hash::HashCT("char"         ))\
		x(Extern       ,"extern"        ,= pr::hash::HashCT("extern"       ))\
		x(Return       ,"return"        ,= pr::hash::HashCT("return"       ))\
		x(Union        ,"union"         ,= pr::hash::HashCT("union"        ))\
		x(Const        ,"const"         ,= pr::hash::HashCT("const"        ))\
		x(Float        ,"float"         ,= pr::hash::HashCT("float"        ))\
		x(Short        ,"short"         ,= pr::hash::HashCT("short"        ))\
		x(Unsigned     ,"unsigned"      ,= pr::hash::HashCT("unsigned"     ))\
		x(Continue     ,"continue"      ,= pr::hash::HashCT("continue"     ))\
		x(For          ,"for"           ,= pr::hash::HashCT("for"          ))\
		x(Signed       ,"signed"        ,= pr::hash::HashCT("signed"       ))\
		x(Void         ,"void"          ,= pr::hash::HashCT("void"         ))\
		x(Default      ,"default"       ,= pr::hash::HashCT("default"      ))\
		x(Goto         ,"goto"          ,= pr::hash::HashCT("goto"         ))\
		x(Sizeof       ,"sizeof"        ,= pr::hash::HashCT("sizeof"       ))\
		x(Volatile     ,"volatile"      ,= pr::hash::HashCT("volatile"     ))\
		x(Do           ,"do"            ,= pr::hash::HashCT("do"           ))\
		x(While        ,"while"         ,= pr::hash::HashCT("while"        ))\
		x(If           ,"if"            ,= pr::hash::HashCT("if"           ))\
		x(Static       ,"static"        ,= pr::hash::HashCT("static"       ))\
		x(StaticAssert ,"static_assert" ,= pr::hash::HashCT("static_assert"))
		PR_ENUM_MEMBERS3(PR_ENUM)
	};
	PR_ENUM_REFLECTION3(EKeyword, PR_ENUM);
	#undef PR_ENUM

	// Preprocessor keywords
	enum class EPPKeyword
	{
		#define PR_ENUM(x)\
		x(Invalid       ,""               ,= pr::hash::HashCT(""              ))\
		x(Include       ,"include"        ,= pr::hash::HashCT("include"       ))\
		x(IncludePath   ,"include_path"   ,= pr::hash::HashCT("include_path"  ))\
		x(IgnoreMissing ,"ignore_missing" ,= pr::hash::HashCT("ignore_missing"))\
		x(Depend        ,"depend"         ,= pr::hash::HashCT("depend"        ))\
		x(Define        ,"define"         ,= pr::hash::HashCT("define"        ))\
		x(Undef         ,"undef"          ,= pr::hash::HashCT("undef"         ))\
		x(Defifndef     ,"defifndef"      ,= pr::hash::HashCT("defifndef"     ))\
		x(If            ,"if"             ,= pr::hash::HashCT("if"            ))\
		x(Ifdef         ,"ifdef"          ,= pr::hash::HashCT("ifdef"         ))\
		x(Ifndef        ,"ifndef"         ,= pr::hash::HashCT("ifndef"        ))\
		x(End           ,"end"            ,= pr::hash::HashCT("end"           ))\
		x(Elif          ,"elif"           ,= pr::hash::HashCT("elif"          ))\
		x(Else          ,"else"           ,= pr::hash::HashCT("else"          ))\
		x(Endif         ,"endif"          ,= pr::hash::HashCT("endif"         ))\
		x(Pragma        ,"pragma"         ,= pr::hash::HashCT("pragma"        ))\
		x(Line          ,"line"           ,= pr::hash::HashCT("line"          ))\
		x(Error         ,"error"          ,= pr::hash::HashCT("error"         ))\
		x(Warning       ,"warning"        ,= pr::hash::HashCT("warning"       ))\
		x(Defined       ,"defined"        ,= pr::hash::HashCT("defined"       ))\
		x(Eval          ,"eval"           ,= pr::hash::HashCT("eval"          ))\
		x(Lit           ,"lit"            ,= pr::hash::HashCT("lit"           ))\
		x(Embedded      ,"embedded"       ,= pr::hash::HashCT("embedded"      ))
		PR_ENUM_MEMBERS3(PR_ENUM)
	};
	PR_ENUM_REFLECTION3(EPPKeyword, PR_ENUM);
	#undef PR_ENUM

	// Symbol characters
	enum class ESymbol
	{
		#define PR_ENUM(x)\
		x(Invalid        ,""    ,=   0 )\
		x(WhiteSpace     ," "   ,= ' ' )\
		x(NewLine        ,"\n"  ,= '\n')\
		x(Assign         ,"="   ,= '=' )\
		x(SemiColon      ,";"   ,= ';' )\
		x(Complement     ,"~"   ,= '~' )\
		x(Not            ,"!"   ,= '!' )\
		x(Ptr            ,"*"   ,= '*' )\
		x(AddressOf      ,"&"   ,= '&' )\
		x(Plus           ,"+"   ,= '+' )\
		x(Minus          ,"-"   ,= '-' )\
		x(Divide         ,"/"   ,= '/' )\
		x(Modulus        ,"%"   ,= '%' )\
		x(LessThan       ,"<"   ,= '<' )\
		x(GtrThan        ,">"   ,= '>' )\
		x(BitOr          ,"|"   ,= '|' )\
		x(BitXor         ,"^"   ,= '^' )\
		x(Comma          ,","   ,= ',' )\
		x(Conditional    ,"?"   ,= '?' )\
		x(BraceOpen      ,"{"   ,= '{' )\
		x(BraceClose     ,"}"   ,= '}' )\
		x(BracketOpen    ,"["   ,= '[' )\
		x(BracketClose   ,"]"   ,= ']' )\
		x(ParenthOpen    ,"("   ,= '(' )\
		x(ParenthClose   ,")"   ,= ')' )\
		x(Dot            ,"."   ,= '.' )\
		x(Colon          ,":"   ,= ':' )\
		x(Hash           ,"#"   ,= '#' )\
		x(Dollar         ,"$"   ,= '$' )\
		x(At             ,"@"   ,= '@' )\
		x(Increment      ,"++"  ,= 128 )\
		x(Decrement      ,"--"  ,= 129 )\
		x(ShiftL         ,"<<"  ,= 130 )\
		x(ShiftR         ,">>"  ,= 131 )\
		x(LessEql        ,"<="  ,= 132 )\
		x(GtrEql         ,">="  ,= 133 )\
		x(Equal          ,"=="  ,= 134 )\
		x(NotEqual       ,"!="  ,= 135 )\
		x(LogicalAnd     ,"&&"  ,= 136 )\
		x(LogicalOr      ,"||"  ,= 137 )\
		x(ShiftLAssign   ,"<<=" ,= 138 )\
		x(ShiftRAssign   ,">>=" ,= 139 )\
		x(BitAndAssign   ,"&="  ,= 140 )\
		x(BitOrAssign    ,"|="  ,= 141 )\
		x(BitXorAssign   ,"^="  ,= 142 )\
		x(AddAssign      ,"+="  ,= 143 )\
		x(SubAssign      ,"-="  ,= 144 )\
		x(MulAssign      ,"*="  ,= 145 )\
		x(DivAssign      ,"/="  ,= 146 )\
		x(ModAssign      ,"%="  ,= 147 )\
		x(Ellipsis       ,"..." ,= 148 )
		PR_ENUM_MEMBERS3(PR_ENUM)
	};
	PR_ENUM_REFLECTION3(ESymbol, PR_ENUM);
	#undef PR_ENUM

	// Constant literals
	enum class EConstant
	{
		#define PR_ENUM(x)\
		x(Invalid       )\
		x(StringLiteral )\
		x(WStringLiteral)\
		x(Integral      )\
		x(FloatingPoint )
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(EConstant, PR_ENUM);
	#undef PR_ENUM

	// Transform keywords
	enum class ETransformKeyword
	{
		#define PR_ENUM(x)\
		x(NonAffine      ,= pr::hash::HashICT("NonAffine"     ))\
		x(M4x4           ,= pr::hash::HashICT("M4x4"          ))\
		x(M3x3           ,= pr::hash::HashICT("M3x3"          ))\
		x(Pos            ,= pr::hash::HashICT("Pos"           ))\
		x(Align          ,= pr::hash::HashICT("Align"         ))\
		x(Quat           ,= pr::hash::HashICT("Quat"          ))\
		x(QuatPos        ,= pr::hash::HashICT("QuatPos"       ))\
		x(Rand4x4        ,= pr::hash::HashICT("Rand4x4"       ))\
		x(RandPos        ,= pr::hash::HashICT("RandPos"       ))\
		x(RandOri        ,= pr::hash::HashICT("RandOri"       ))\
		x(Euler          ,= pr::hash::HashICT("Euler"         ))\
		x(Scale          ,= pr::hash::HashICT("Scale"         ))\
		x(Transpose      ,= pr::hash::HashICT("Transpose"     ))\
		x(Inverse        ,= pr::hash::HashICT("Inverse"       ))\
		x(Normalise      ,= pr::hash::HashICT("Normalise"     ))\
		x(Orthonormalise ,= pr::hash::HashICT("Orthonormalise"))
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(ETransformKeyword, PR_ENUM);
	#undef PR_ENUM
}
