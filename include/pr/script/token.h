//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/script_core.h"

namespace pr
{
	namespace script
	{
		// An indivisible source element
		struct Token
		{
			using wstring = pr::string<wchar_t>;
			using int64 = long long;

			EToken    m_type;
			EKeyword  m_keyword;
			ESymbol   m_symbol;
			EConstant m_constant;
			wstring   m_svalue;
			int64     m_ivalue;
			double    m_fvalue;

			Token() :m_type(EToken::Invalid) ,m_keyword(EKeyword::Invalid) ,m_svalue() ,m_ivalue() ,m_fvalue() {}
			explicit Token(EToken    t                                         ) :m_type(t               ) ,m_keyword ( ) ,m_svalue(      ) ,m_ivalue(      ) ,m_fvalue(      ) {}
			explicit Token(EToken    t, wstring const& svalue ,int64 ivalue = 0) :m_type(t               ) ,m_keyword ( ) ,m_svalue(svalue) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EKeyword  k                        ,int64 ivalue = 0) :m_type(EToken::Keyword ) ,m_keyword (k) ,m_svalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(ESymbol   s                        ,int64 ivalue = 0) :m_type(EToken::Symbol  ) ,m_symbol  (s) ,m_svalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c                        ,int64 ivalue = 0) :m_type(EToken::Constant) ,m_constant(c) ,m_svalue(      ) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,wstring const& svalue ,int64 ivalue = 0) :m_type(EToken::Constant) ,m_constant(c) ,m_svalue(svalue) ,m_ivalue(ivalue) ,m_fvalue(      ) {}
			explicit Token(EConstant c ,double fvalue         ,int64 ivalue = 0) :m_type(EToken::Constant) ,m_constant(c) ,m_svalue(      ) ,m_ivalue(ivalue) ,m_fvalue(fvalue) {}

			// All tokens except the EndOfStream token return true
			operator bool() const { return m_type != EToken::EndOfStream; }
		};

		// Operators
		inline bool operator == (Token const& tok, EToken    type) { return tok.m_type == type; }
		inline bool operator == (Token const& tok, EKeyword  type) { return tok.m_type == EToken::Keyword  && tok.m_keyword  == type; }
		inline bool operator == (Token const& tok, ESymbol   type) { return tok.m_type == EToken::Symbol   && tok.m_symbol   == type; }
		inline bool operator == (Token const& tok, EConstant type) { return tok.m_type == EToken::Constant && tok.m_constant == type; }
		inline bool operator != (Token const& tok, EToken    type) { return !(tok == type); }
		inline bool operator != (Token const& tok, EKeyword  type) { return !(tok == type); }
		inline bool operator != (Token const& tok, ESymbol   type) { return !(tok == type); }
		inline bool operator != (Token const& tok, EConstant type) { return !(tok == type); }

		// Convert a token to a string description of the token
		inline pr::string<wchar_t> ToStringW(Token const& token)
		{
			switch (token.m_type)
			{
			default: return L"";
			case EToken::Invalid:     return pr::FmtS(L"Invalid"); break;
			case EToken::EndOfStream: return pr::FmtS(L"EndOfStream"); break;
			case EToken::Keyword:     return pr::FmtS(L"%s %s"    ,ToStringW(token.m_type) ,ToStringW(token.m_keyword)); break;
			case EToken::Identifier:  return pr::FmtS(L"%s %s"    ,ToStringW(token.m_type) ,token.m_svalue.c_str()); break;
			case EToken::Symbol:      return pr::FmtS(L"%s %s"    ,ToStringW(token.m_type) ,ToStringW(token.m_symbol)); break;
			case EToken::Constant:
				switch (token.m_constant)
				{
				default: return L"";
				case EConstant::Invalid:        return pr::FmtS(L"Invalid"); break;
				case EConstant::StringLiteral:  return pr::FmtS(L"%s %s %s" ,ToStringW(token.m_type) ,ToStringW(token.m_constant) ,token.m_svalue.c_str()); break;
				case EConstant::WStringLiteral: return pr::FmtS(L"%s %s "   ,ToStringW(token.m_type) ,ToStringW(token.m_constant)); break;
				case EConstant::Integral:       return pr::FmtS(L"%s %s %d" ,ToStringW(token.m_type) ,ToStringW(token.m_constant) ,token.m_ivalue); break;
				case EConstant::FloatingPoint:  return pr::FmtS(L"%s %s %f" ,ToStringW(token.m_type) ,ToStringW(token.m_constant) ,token.m_fvalue); break;
				}
				break;
			}
		}

		// Convert a container of tokens into a string
		template <typename Cont> inline pr::string<wchar_t> ToStringW(Cont const& tokens)
		{
			pr::string<wchar_t> out;
			for (for tok : tokens)
				out.append(ToString(tok)).append(L"\n");
			return out;
		}
	}
}

