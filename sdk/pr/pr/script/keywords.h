//**********************************
// Script keywords
//  Copyright © Rylogic Ltd 2007
//**********************************
	
// Script exception values/return codes
#ifndef PR_SCRIPT_RESULT
#define PR_SCRIPT_RESULT(name, value)
#endif
PR_SCRIPT_RESULT(Success                         ,= 0)
PR_SCRIPT_RESULT(Failed                          ,= 0x80000000)
PR_SCRIPT_RESULT(InvalidIdentifier               ,)
PR_SCRIPT_RESULT(InvalidString                   ,)
PR_SCRIPT_RESULT(ParameterCountMismatch          ,)
PR_SCRIPT_RESULT(UnexpectedEndOfFile             ,)
PR_SCRIPT_RESULT(UnknownPreprocessorCommand      ,)
PR_SCRIPT_RESULT(InvalidMacroDefinition          ,)
PR_SCRIPT_RESULT(MacroNotDefined                 ,)
PR_SCRIPT_RESULT(MacroAlreadyDefined             ,)
PR_SCRIPT_RESULT(InvalidInclude                  ,)
PR_SCRIPT_RESULT(MissingInclude                  ,)
PR_SCRIPT_RESULT(InvalidPreprocessorDirective    ,)
PR_SCRIPT_RESULT(UnmatchedPreprocessorDirective  ,)
PR_SCRIPT_RESULT(PreprocessError                 ,)
PR_SCRIPT_RESULT(SyntaxError                     ,)
PR_SCRIPT_RESULT(ExpressionSyntaxError           ,)
PR_SCRIPT_RESULT(EmbeddedCodeSyntaxError         ,)
PR_SCRIPT_RESULT(TokenNotFound                   ,)
PR_SCRIPT_RESULT(UnknownKeyword                  ,)
PR_SCRIPT_RESULT(UnknownToken                    ,)
PR_SCRIPT_RESULT(UnknownValue                    ,)
PR_SCRIPT_RESULT(ValueNotFound                   ,)
#undef PR_SCRIPT_RESULT
	
// Token type
#ifndef PR_SCRIPT_TOKEN
#define PR_SCRIPT_TOKEN(name)
#endif
PR_SCRIPT_TOKEN(Invalid           ) // Unknown
PR_SCRIPT_TOKEN(EndOfStream       ) // The end of the input stream
PR_SCRIPT_TOKEN(Identifier        ) // An identifier
PR_SCRIPT_TOKEN(Keyword           ) // A script keyword
PR_SCRIPT_TOKEN(Symbol            ) // An operator or punctuator, e.g *, ->, +, ;, {, }, etc 
PR_SCRIPT_TOKEN(Constant          ) // A literal constant
#undef PR_SCRIPT_TOKEN
	
// Preprocessor keywords
#ifndef PR_SCRIPT_PP_KEYWORD
#define PR_SCRIPT_PP_KEYWORD(name, text, hashcode)
#endif
PR_SCRIPT_PP_KEYWORD(Invalid      ,""         ,0xffffffff)
PR_SCRIPT_PP_KEYWORD(Include      ,"include"  ,0x0a5f3fce)
PR_SCRIPT_PP_KEYWORD(Define       ,"define"   ,0x0d22697a)
PR_SCRIPT_PP_KEYWORD(Undef        ,"undef"    ,0x1450e770)
PR_SCRIPT_PP_KEYWORD(If           ,"if"       ,0x1dfa87fc)
PR_SCRIPT_PP_KEYWORD(Ifdef        ,"ifdef"    ,0x11fac604)
PR_SCRIPT_PP_KEYWORD(Ifndef       ,"ifndef"   ,0x1fb3e42d)
PR_SCRIPT_PP_KEYWORD(Elif         ,"elif"     ,0x02414bd3)
PR_SCRIPT_PP_KEYWORD(Else         ,"else"     ,0x1d237859)
PR_SCRIPT_PP_KEYWORD(Endif        ,"endif"    ,0x15632e04)
PR_SCRIPT_PP_KEYWORD(Pragma       ,"pragma"   ,0x1ec9d08d)
PR_SCRIPT_PP_KEYWORD(Line         ,"line"     ,0x10d28008)
PR_SCRIPT_PP_KEYWORD(Error        ,"error"    ,0x0158fc8d)
PR_SCRIPT_PP_KEYWORD(Warning      ,"warning"  ,0x051535cd)
PR_SCRIPT_PP_KEYWORD(Defined      ,"defined"  ,0x019b9520)
PR_SCRIPT_PP_KEYWORD(Eval         ,"eval"     ,0x1531ec3d)
PR_SCRIPT_PP_KEYWORD(Lit          ,"lit"      ,0x15df8629)
PR_SCRIPT_PP_KEYWORD(Embedded     ,"embedded" ,0x0e5b2cfa)
#undef PR_SCRIPT_PP_KEYWORD
	
// C keywords
#ifndef PR_SCRIPT_KEYWORD
#define PR_SCRIPT_KEYWORD(name, text, hashcode)
#endif
PR_SCRIPT_KEYWORD(Invalid         ,""          ,0xffffffff)
PR_SCRIPT_KEYWORD(Auto            ,"auto"      ,0x112746e9)
PR_SCRIPT_KEYWORD(Double          ,"double"    ,0x1840d9ce)
PR_SCRIPT_KEYWORD(Int             ,"int"       ,0x164a43dd)
PR_SCRIPT_KEYWORD(Struct          ,"struct"    ,0x0f408d2a)
PR_SCRIPT_KEYWORD(Break           ,"break"     ,0x1ac013ec)
PR_SCRIPT_KEYWORD(Else            ,"else"      ,0x1d237859)
PR_SCRIPT_KEYWORD(Long            ,"long"      ,0x14ef7164)
PR_SCRIPT_KEYWORD(Switch          ,"switch"    ,0x13c0233f)
PR_SCRIPT_KEYWORD(Case            ,"case"      ,0x18ea7f00)
PR_SCRIPT_KEYWORD(Enum            ,"enum"      ,0x113f6121)
PR_SCRIPT_KEYWORD(Register        ,"register"  ,0x1a14aae9)
PR_SCRIPT_KEYWORD(Typedef         ,"typedef"   ,0x1b494818)
PR_SCRIPT_KEYWORD(Char            ,"char"      ,0x1e5760f8)
PR_SCRIPT_KEYWORD(Extern          ,"extern"    ,0x16497b3b)
PR_SCRIPT_KEYWORD(Return          ,"return"    ,0x0a01f36e)
PR_SCRIPT_KEYWORD(Union           ,"union"     ,0x1e57f369)
PR_SCRIPT_KEYWORD(Const           ,"const"     ,0x036f03e1)
PR_SCRIPT_KEYWORD(Float           ,"float"     ,0x176b5be3)
PR_SCRIPT_KEYWORD(Short           ,"short"     ,0x1edc8c0f)
PR_SCRIPT_KEYWORD(Unsigned        ,"unsigned"  ,0x186a2b87)
PR_SCRIPT_KEYWORD(Continue        ,"continue"  ,0x1e46a876)
PR_SCRIPT_KEYWORD(For             ,"for"       ,0x0e37a24a)
PR_SCRIPT_KEYWORD(Signed          ,"signed"    ,0x00bf0c54)
PR_SCRIPT_KEYWORD(Void            ,"void"      ,0x1a9b029d)
PR_SCRIPT_KEYWORD(Default         ,"default"   ,0x1c8cdd40)
PR_SCRIPT_KEYWORD(Goto            ,"goto"      ,0x04d53061)
PR_SCRIPT_KEYWORD(Sizeof          ,"sizeof"    ,0x1429164b)
PR_SCRIPT_KEYWORD(Volatile        ,"volatile"  ,0x18afc4c2)
PR_SCRIPT_KEYWORD(Do              ,"do"        ,0x1d8b5fef)
PR_SCRIPT_KEYWORD(If              ,"if"        ,0x1dfa87fc)
PR_SCRIPT_KEYWORD(Static          ,"static"    ,0x16150ce7)
PR_SCRIPT_KEYWORD(While           ,"while"     ,0x0b4669dc)
#undef PR_SCRIPT_KEYWORD
	
// Symbols
#ifndef PR_SCRIPT_SYMBOL
#define PR_SCRIPT_SYMBOL(name, text, hashcode)
#endif
PR_SCRIPT_SYMBOL(Invalid        ,""    ,  0) //
PR_SCRIPT_SYMBOL(WhiteSpace     ," "   ,' ') // ' ', '\t', etc
PR_SCRIPT_SYMBOL(NewLine        ,"\n"  ,'\n')// '\n'
PR_SCRIPT_SYMBOL(Assign         ,"="   ,'=') // assign
PR_SCRIPT_SYMBOL(SemiColon      ,";"   ,';') //
PR_SCRIPT_SYMBOL(Complement     ,"~"   ,'~') //
PR_SCRIPT_SYMBOL(Not            ,"!"   ,'!') //
PR_SCRIPT_SYMBOL(Ptr            ,"*"   ,'*') // pointer, dereference, or multiply
PR_SCRIPT_SYMBOL(AddressOf      ,"&"   ,'&') // address of, or bitwise-AND
PR_SCRIPT_SYMBOL(Plus           ,"+"   ,'+') // unary plus, or add
PR_SCRIPT_SYMBOL(Minus          ,"-"   ,'-') // unary negate, or subtract
PR_SCRIPT_SYMBOL(Divide         ,"/"   ,'/') // /
PR_SCRIPT_SYMBOL(Modulus        ,"%"   ,'%') // %
PR_SCRIPT_SYMBOL(LessThan       ,"<"   ,'<') // <
PR_SCRIPT_SYMBOL(GtrThan        ,">"   ,'>') // >
PR_SCRIPT_SYMBOL(BitOr          ,"|"   ,'|') // |
PR_SCRIPT_SYMBOL(BitXor         ,"^"   ,'^') // ^
PR_SCRIPT_SYMBOL(Comma          ,","   ,',') // ,
PR_SCRIPT_SYMBOL(Conditional    ,"?"   ,'?') // ? (as in (bool) ? (statement) : (statement)
PR_SCRIPT_SYMBOL(BraceOpen      ,"{"   ,'{') // {
PR_SCRIPT_SYMBOL(BraceClose     ,"}"   ,'}') // }
PR_SCRIPT_SYMBOL(BracketOpen    ,"["   ,'[') // [
PR_SCRIPT_SYMBOL(BracketClose   ,"]"   ,']') // ]
PR_SCRIPT_SYMBOL(ParenthOpen    ,"("   ,'(') // (
PR_SCRIPT_SYMBOL(ParenthClose   ,")"   ,')') // )
PR_SCRIPT_SYMBOL(Dot            ,"."   ,'.') // .
PR_SCRIPT_SYMBOL(Colon          ,":"   ,':') // :
PR_SCRIPT_SYMBOL(Hash           ,"#"   ,'#') // #
PR_SCRIPT_SYMBOL(Dollar         ,"$"   ,'$') // $
PR_SCRIPT_SYMBOL(At             ,"@"   ,'@') // @
PR_SCRIPT_SYMBOL(Increment      ,"++"  ,128) // ++
PR_SCRIPT_SYMBOL(Decrement      ,"--"  ,129) // --
PR_SCRIPT_SYMBOL(ShiftL         ,"<<"  ,130) // <<
PR_SCRIPT_SYMBOL(ShiftR         ,">>"  ,131) // >>
PR_SCRIPT_SYMBOL(LessEql        ,"<="  ,132) // <=
PR_SCRIPT_SYMBOL(GtrEql         ,">="  ,133) // >=
PR_SCRIPT_SYMBOL(Equal          ,"=="  ,134) // ==
PR_SCRIPT_SYMBOL(NotEqual       ,"!="  ,135) // !=
PR_SCRIPT_SYMBOL(LogicalAnd     ,"&&"  ,136) // &&
PR_SCRIPT_SYMBOL(LogicalOr      ,"||"  ,137) // ||
PR_SCRIPT_SYMBOL(ShiftLAssign   ,"<<=" ,138) // <<=
PR_SCRIPT_SYMBOL(ShiftRAssign   ,">>=" ,139) // >>=
PR_SCRIPT_SYMBOL(BitAndAssign   ,"&="  ,140) // &=
PR_SCRIPT_SYMBOL(BitOrAssign    ,"|="  ,141) // |=
PR_SCRIPT_SYMBOL(BitXorAssign   ,"^="  ,142) // ^=
PR_SCRIPT_SYMBOL(AddAssign      ,"+="  ,143) // +=
PR_SCRIPT_SYMBOL(SubAssign      ,"-="  ,144) // -=
PR_SCRIPT_SYMBOL(MulAssign      ,"*="  ,145) // *=
PR_SCRIPT_SYMBOL(DivAssign      ,"/="  ,146) // /=
PR_SCRIPT_SYMBOL(ModAssign      ,"%="  ,147) // %=
PR_SCRIPT_SYMBOL(Ellipsis       ,"..." ,148) // ...
#undef PR_SCRIPT_SYMBOL
	
#ifndef PR_SCRIPT_CONSTANT
#define PR_SCRIPT_CONSTANT(name)
#endif
PR_SCRIPT_CONSTANT(Invalid        )
PR_SCRIPT_CONSTANT(StringLiteral  )
PR_SCRIPT_CONSTANT(WStringLiteral )
PR_SCRIPT_CONSTANT(Integral       )
PR_SCRIPT_CONSTANT(FloatingPoint  )
#undef PR_SCRIPT_CONSTANT
	
// Main Include
#ifndef PR_SCRIPT_KEYWORDS_H
#define PR_SCRIPT_KEYWORDS_H
	
namespace pr
{
	namespace script
	{
		namespace EResult
		{
			enum Type
			{
#			define PR_SCRIPT_RESULT(name, value) name value,
#			include "keywords.h"
			};
		}
		namespace EToken
		{
			enum Type
			{
#			define PR_SCRIPT_TOKEN(name) name,
#			include "keywords.h"
			};
		}
		namespace EPPKeyword
		{
			enum Type
			{
#			define PR_SCRIPT_PP_KEYWORD(name, text, hashcode) name = hashcode,
#			include "keywords.h"
			};
		}
		namespace EKeyword
		{
			enum Type
			{
#			define PR_SCRIPT_KEYWORD(name, text, hashcode) name = hashcode,
#			include "keywords.h"
			};
		}
		namespace ESymbol
		{
			enum Type
			{
#			define PR_SCRIPT_SYMBOL(name, text, hashcode) name = hashcode,
#			include "keywords.h"
			};
		}
		namespace EConstant
		{
			enum Type
			{
#			define PR_SCRIPT_CONSTANT(name) name,
#			include "keywords.h"
			};
		}
		
		// String helpers
		inline char const* ToString(EResult::Type type)
		{
			switch (type) {
			default: return "";
#			define PR_SCRIPT_RESULT(name, value) case EResult::name: return #name;
#			include "keywords.h"
			}
		}
		inline char const* ToString(EToken::Type type)
		{
			switch (type) {
			default: return "";
#			define PR_SCRIPT_TOKEN(name) case EToken::name: return #name;
#			include "keywords.h"
			}
		}
		inline char const* ToString(EPPKeyword::Type type)
		{
			switch (type) {
			default: return "";
#			define PR_SCRIPT_PP_KEYWORD(name,text,hashcode) case EPPKeyword::name: return text;
#			include "keywords.h"
			}
		}
		inline char const* ToString(EKeyword::Type type)
		{
			switch (type) {
			default: return "";
#			define PR_SCRIPT_KEYWORD(name,text,hashcode) case EKeyword::name: return text;
#			include "keywords.h"
			}
		}
		inline char const* ToString(ESymbol::Type type)
		{
			switch (type) {
			default: return "";
#			define PR_SCRIPT_SYMBOL(name,text,hashcode) case ESymbol::name: return text;
#			include "keywords.h"
			}
		}
		inline char const* ToString(EConstant::Type type)
		{
			switch (type) {
			default: return "";
#			define PR_SCRIPT_CONSTANT(name) case EConstant::name: return #name;
#			include "keywords.h"
			}
		}
		
		//// Characters and words with special meaning for the script
		//struct Keywords
		//{
		//	char m_keyword;
		//	char m_preprocessor;
		//	char m_section_start;
		//	char m_section_end;
		//	char m_new_line;
		//	pr::string<char, 16> m_delim;
		//	pr::string<char, 16> m_whitespace;

		//	Keywords()
		//	:m_keyword       ('*')
		//	,m_preprocessor  ('#')
		//	,m_section_start ('{')
		//	,m_section_end   ('}')
		//	,m_new_line      ('\n')
		//	,m_delim         ("{}*#+- \t\n\r,;")
		//	,m_whitespace    (" \t\n\r,;")
		//	{}
		//	static Keywords& Default()
		//	{
		//		static Keywords default_keywords;
		//		return default_keywords;
		//	}
		//	char const* delim() const      { return m_delim.c_str(); }
		//	char const* whitespace() const { return m_whitespace.c_str(); }
		//};
	}
}

#endif
