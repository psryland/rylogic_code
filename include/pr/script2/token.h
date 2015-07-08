//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script2/forward.h"
#include "pr/script2/script_core.h"

namespace pr
{
	namespace script2
	{
		// An indivisible source element
		struct Token
		{
			using astring = pr::string<char>;
			using wstring = pr::string<wchar_t>;

			EToken    m_type;
			EKeyword  m_keyword;
			ESymbol   m_symbol;
			EConstant m_constant;
			astring   m_avalue;
			wstring   m_wvalue;
			int64     m_ivalue;
			double    m_fvalue;

			Token() :m_type(EToken::Invalid) ,m_keyword(EKeyword::Invalid) ,m_avalue() ,m_wvalue() ,m_ivalue() ,m_fvalue() {}
			explicit Token(EToken    t                                         ) :m_type(t               ) ,m_keyword ( ) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(      ) ,m_fvalue(      ) {}
			explicit Token(EToken    t, wstring const& wvalue ,int64 ivalue = 0) :m_type(t               ) ,m_keyword ( ) ,m_avalue(      ) ,m_wvalue(wvalue) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EKeyword  k                        ,int64 ivalue = 0) :m_type(EToken::Keyword ) ,m_keyword (k) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(ESymbol   s                        ,int64 ivalue = 0) :m_type(EToken::Symbol  ) ,m_symbol  (s) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,astring const& avalue ,int64 ivalue = 0) :m_type(EToken::Constant) ,m_constant(c) ,m_avalue(avalue) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,wstring const& wvalue ,int64 ivalue = 0) :m_type(EToken::Constant) ,m_constant(c) ,m_avalue(      ) ,m_wvalue(wvalue) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,double fvalue         ,int64 ivalue = 0) :m_type(EToken::Constant) ,m_constant(c) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(fvalue) {}

			// All tokens except the EndOfStream token return true
			operator bool() const { return m_type != EToken::EndOfStream; }
		};

		// Operators
		inline bool operator == (Token const& tok, EToken   ::Enum_ type) { return tok.m_type == type; }
		inline bool operator == (Token const& tok, EKeyword ::Enum_ type) { return tok.m_type == EToken::Keyword  && tok.m_keyword  == type; }
		inline bool operator == (Token const& tok, ESymbol  ::Enum_ type) { return tok.m_type == EToken::Symbol   && tok.m_symbol   == type; }
		inline bool operator == (Token const& tok, EConstant::Enum_ type) { return tok.m_type == EToken::Constant && tok.m_constant == type; }
		inline bool operator != (Token const& tok, EToken   ::Enum_ type) { return !(tok == type); }
		inline bool operator != (Token const& tok, EKeyword ::Enum_ type) { return !(tok == type); }
		inline bool operator != (Token const& tok, ESymbol  ::Enum_ type) { return !(tok == type); }
		inline bool operator != (Token const& tok, EConstant::Enum_ type) { return !(tok == type); }

		// Convert a token to a string description of the token
		inline pr::string<char> ToString(Token const& token)
		{
			switch (token.m_type)
			{
			default: return "";
			case EToken::Invalid:     return pr::FmtS("Invalid"); break;
			case EToken::EndOfStream: return pr::FmtS("EndOfStream"); break;
			case EToken::Keyword:     return pr::FmtS("%s %s"    ,token.m_type.ToStringA() ,token.m_keyword.ToStringA()); break;
			case EToken::Identifier:  return pr::FmtS("%s %s"    ,token.m_type.ToStringA() ,token.m_avalue.c_str()    ); break;
			case EToken::Symbol:      return pr::FmtS("%s %s"    ,token.m_type.ToStringA() ,token.m_symbol.ToStringA() ); break;
			case EToken::Constant:
				switch (token.m_constant)
				{
				default: return "";
				case EConstant::Invalid:        return pr::FmtS("Invalid"); break;
				case EConstant::StringLiteral:  return pr::FmtS("%s %s %s" ,token.m_type.ToStringA() ,token.m_constant.ToStringA() ,token.m_avalue.c_str()); break;
				case EConstant::WStringLiteral: return pr::FmtS("%s %s "   ,token.m_type.ToStringA() ,token.m_constant.ToStringA() ); break;
				case EConstant::Integral:       return pr::FmtS("%s %s %d" ,token.m_type.ToStringA() ,token.m_constant.ToStringA() ,token.m_ivalue); break;
				case EConstant::FloatingPoint:  return pr::FmtS("%s %s %f" ,token.m_type.ToStringA() ,token.m_constant.ToStringA() ,token.m_fvalue); break;
				}
				break;
			}
		}

		// Convert a container of tokens into a string
		template <typename Cont> inline pr::string<char> ToString(Cont const& tokens)
		{
			pr::string<char> out;
			for (for tok : tokens)
				out.append(ToString(tok)).append("\n");
			return out;
		}
	}
}

