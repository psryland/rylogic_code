//**********************************
// Script tokens
//  Copyright (c) Rylogic Ltd 2011
//**********************************
#pragma once
#ifndef PR_SCRIPT_TOKEN_H
#define PR_SCRIPT_TOKEN_H

#include "pr/script/script_core.h"

namespace pr
{
	namespace script
	{
		// An indivisible script element
		struct Token
		{
			EToken    m_type;
			EKeyword  m_keyword;
			ESymbol   m_symbol;
			EConstant m_constant;
			string    m_avalue;
			wstring   m_wvalue;
			int64     m_ivalue;
			double    m_fvalue;
			
			Token() :m_type(EToken::Invalid) ,m_keyword(EKeyword::Invalid) ,m_avalue() ,m_wvalue() ,m_ivalue() ,m_fvalue() {}
			explicit Token(EToken    t, string const& avalue  ,int64 ivalue = 0) :m_type(t                ) ,m_keyword  ( ) ,m_avalue(avalue) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EKeyword  k                        ,int64 ivalue = 0) :m_type(EToken::Keyword  ) ,m_keyword  (k) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(ESymbol   s                        ,int64 ivalue = 0) :m_type(EToken::Symbol   ) ,m_symbol   (s) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,string  const& avalue ,int64 ivalue = 0) :m_type(EToken::Constant ) ,m_constant (c) ,m_avalue(avalue) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,wstring const& wvalue ,int64 ivalue = 0) :m_type(EToken::Constant ) ,m_constant (c) ,m_avalue(      ) ,m_wvalue(wvalue) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,double fvalue         ,int64 ivalue = 0) :m_type(EToken::Constant ) ,m_constant (c) ,m_avalue(      ) ,m_wvalue(      ) ,m_ivalue(ivalue) ,m_fvalue(fvalue) {}
			
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
		inline string ToString(Token const& token)
		{
			switch (token.m_type)
			{
			default: return "";
			case EToken::Invalid:     return fmt("Invalid"); break;
			case EToken::EndOfStream: return fmt("EndOfStream"); break;
			case EToken::Keyword:     return fmt("%s %s"    ,pr::ToString(token.m_type) ,pr::ToString(token.m_keyword)); break;
			case EToken::Identifier:  return fmt("%s %s"    ,pr::ToString(token.m_type) ,token.m_avalue.c_str()       ); break;
			case EToken::Symbol:      return fmt("%s %s"    ,pr::ToString(token.m_type) ,pr::ToString(token.m_symbol) ); break;
			case EToken::Constant:
				switch (token.m_constant)
				{
				default: return "";
				case EConstant::Invalid:        return fmt("Invalid"); break;
				case EConstant::StringLiteral:  return fmt("%s %s %s" ,pr::ToString(token.m_type) ,pr::ToString(token.m_constant) ,token.m_avalue.c_str()); break;
				case EConstant::WStringLiteral: return fmt("%s %s "   ,pr::ToString(token.m_type) ,pr::ToString(token.m_constant) ); break;//,token.value().c_str()); break;
				case EConstant::Integral:       return fmt("%s %s %d" ,pr::ToString(token.m_type) ,pr::ToString(token.m_constant) ,token.m_ivalue); break;
				case EConstant::FloatingPoint:  return fmt("%s %s %f" ,pr::ToString(token.m_type) ,pr::ToString(token.m_constant) ,token.m_fvalue); break;
				}break;
			}
		}
		
		// Convert a container of tokens into a string
		template <typename Cont> inline string ToString(Cont const& tokens)
		{
			string out;
			for (TokenCont::const_iterator i = tokens.begin(), iend = tokens.end(); i != iend; ++i)
				out.append(ToString(*i)).append("\n");
			return out;
		}
	}
}

#endif
