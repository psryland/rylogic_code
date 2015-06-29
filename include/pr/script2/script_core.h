//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include <cassert>
#include "pr/common/exception.h"
#include "pr/common/fmt.h"
#include "pr/common/scope.h"
#include "pr/container/vector.h"
#include "pr/macros/enum.h"
#include "pr/str/string.h"
#include "pr/str/string_core.h"
#include "pr/script2/location.h"

namespace pr
{
	namespace script2
	{
		#pragma region Enumerations

		// Token type
		#define PR_ENUM(x) /*
			*/x(Invalid    ) /* Unknown
			*/x(EndOfStream) /* The end of the input stream
			*/x(Identifier ) /* An identifier
			*/x(Keyword    ) /* A script keyword
			*/x(Symbol     ) /* An operator or punctuator, e.g *, ->, +, ;, {, }, etc
			*/x(Constant   ) // A literal constant
		PR_DEFINE_ENUM1(EToken, PR_ENUM);
		#undef PR_ENUM

		// C keywords
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

		// Preprocessor keywords
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

		// Script exception values/return codes
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
			x(InvalidInclude                  ,)\
			x(MissingInclude                  ,)\
			x(InvalidPreprocessorDirective    ,)\
			x(UnmatchedPreprocessorDirective  ,)\
			x(PreprocessError                 ,)\
			x(SyntaxError                     ,)\
			x(ExpressionSyntaxError           ,)\
			x(EmbeddedCodeNotSupported        ,)\
			x(EmbeddedCodeSyntaxError         ,)\
			x(TokenNotFound                   ,)\
			x(UnknownKeyword                  ,)\
			x(UnknownToken                    ,)\
			x(UnknownValue                    ,)\
			x(ValueNotFound                   ,)
		PR_DEFINE_ENUM2(EResult, PR_ENUM);
		#undef PR_ENUM

		// Symbols
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

		#define PR_ENUM(x)\
			x(Invalid       )\
			x(StringLiteral )\
			x(WStringLiteral)\
			x(Integral      )\
			x(FloatingPoint )
		PR_DEFINE_ENUM1(EConstant, PR_ENUM);
		#undef PR_ENUM

		// Source types, mainly used for debugging
		enum class ESrcType
		{
			Unknown,
			Pointer,
			Range,
			Buffered,
			File,
			Macro,
			Preprocessor,
		};

		#pragma endregion

		// Script exception
		struct Exception :pr::Exception<EResult>
		{
			Loc const* m_loc;
			Exception(EResult result, Loc const& loc, std::string msg)
				:pr::Exception<EResult>(result, msg)
				,m_loc(&loc)
			{}
		};

		// Interface to a stream of wchar_t's
		// Essentually a pointer-like interface
		struct Src
		{
			virtual ~Src() {}
			virtual ESrcType Type() const { return m_type; }

			// Pointer-like interface
			virtual wchar_t operator * () const = 0;
			virtual Src&    operator ++() = 0;

			// Get/Set the source file/line/column location
			virtual Loc const& loc() const
			{
				static Loc s_null_loc;
				return s_null_loc;
			}

			// Convenience methods
			Src& operator +=(int n)
			{
				for (;n--;) ++*this;
				return *this;
			}

		protected:
			ESrcType m_type;
			Src(ESrcType type)
				:m_type(type)
			{}
		};

		// Allow any type that implements a pointer to implement 'Src'
		template <typename TPtr, typename TLoc> struct Ptr :Src
		{
			TPtr m_ptr;
			TLoc m_loc;

			Ptr(TPtr ptr, ESrcType src_type = ESrcType::Pointer)
				:Src(src_type)
				,m_ptr(ptr)
				,m_loc()
			{}

			// Pointer-like interface
			wchar_t operator * () const override
			{
				return wchar_t(*m_ptr);
			}
			Ptr& operator ++() override
			{
				if (*m_ptr) ++m_ptr;
				m_loc.inc(*m_ptr);
				return *this;
			}

			// Get/Set the source file/line/column location
			TLoc const& loc() const override
			{
				return m_loc;
			}
			void loc(TLoc& l)
			{
				m_loc = l;
			}
		};
	}
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/str/string_core.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script2_enums)
		{
			using namespace pr::script2;

			//auto hash   = [](char const* str){ return pr::hash::HashC(str); };
			//auto onfail = [](char const* msg){ PR_FAIL(msg); };
			//pr::CheckHashEnum<EKeyword>  (hash, onfail);
			//pr::CheckHashEnum<EPPKeyword>(hash, onfail);
		}
		PRUnitTest(pr_script2_script_core)
		{
			using namespace pr::str;
			using namespace pr::script2;

		}
	}
}
#endif
