//**********************************
// Script
//  Copyright (c) Rylogic Ltd 2015
//**********************************

#pragma once

#include "pr/script/forward.h"
#include "pr/script/fail_policy.h"
#include "pr/script/token.h"
#include "pr/script/script_core.h"

namespace pr::script
{
	// An interface/base class for a source of tokens
	struct TokenSrc
	{
		virtual ~TokenSrc() {}
			
		// Pointer-like interface
		virtual Token const& operator * () const = 0;
		virtual TokenSrc& operator ++() = 0;
		TokenSrc& operator += (int n)
		{
			for (;n--;) ++*this;
			return *this;
		}
	};
		
	// C Tokeniser
	struct Tokeniser :TokenSrc
	{
		using Buffer = Buffer<pr::deque<wchar_t>>;

		Buffer    m_buf;  // The character stream to read from
		EmitCount m_emit; // The read position in 'm_buf'
		Token     m_tok;  // The token last read from the stream

		Tokeniser(Src& src)
			:m_buf(src)
			,m_emit()
			,m_tok()
		{
			seek();
		}
		Tokeniser(Tokeniser&& rhs)
			:TokenSrc(std::move(rhs))
			,m_buf(std::move(rhs.m_buf))
			,m_emit(std::move(rhs.m_emit))
			,m_tok(std::move(rhs.m_tok))
		{}
		Tokeniser(Tokeniser const& rhs)
			:TokenSrc(rhs)
			,m_buf(rhs.m_buf)
			,m_emit(rhs.m_emit)
			,m_tok(rhs.m_tok)
		{}

		// Pointer-like interface
		Token const& operator * () const override
		{
			return m_tok;
		}
		TokenSrc& operator ++() override
		{
			seek();
			return *this;
		}

		// Advance to the next token to output
		void seek()
		{
			auto& src = m_buf;
			auto& emit = m_emit;

			// Line space does not generate tokens
			EatLineSpace(src, 0, 0);
			switch (*src)
			{
			default:
			throw Exception(EResult::SyntaxError, src.Loc(), "Tokeniser failed to understand code starting here");

			case 0:
				m_tok = Token(EToken::EndOfStream);
				break;

			case '\n':
				m_tok = Token(ESymbol::NewLine, src.Loc().Line()); // save the index of the line following this '\n'
				++src;
				break;

			case '_':
			case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': case 'g': case 'h': case 'i':
			case 'j': case 'k': case 'l': case 'm': case 'n': case 'o': case 'p': case 'q': case 'r':
			case 's': case 't': case 'u': case 'v': case 'w': case 'x': case 'y': case 'z':
			case 'A': case 'B': case 'C': case 'D': case 'E': case 'F': case 'G': case 'H': case 'I':
			case 'J': case 'K':           case 'M': case 'N': case 'O': case 'P': case 'Q': case 'R':
			case 'S': case 'T': case 'U': case 'V': case 'W': case 'X': case 'Y': case 'Z':
				// extract an identifier that might be a keyword
				tokeniser_extract_identitier:
				{
					// Read the identifier
					BufferIdentifier(src, emit);
					auto hash = Hash(src, 0, emit);
					switch (hash)
					{
					default:                 m_tok = Token(EToken::Identifier, src.str(0, emit), hash); break;
					case EKeyword::Auto:     m_tok = Token(EKeyword::Auto     ); break;
					case EKeyword::Double:   m_tok = Token(EKeyword::Double   ); break;
					case EKeyword::Int:      m_tok = Token(EKeyword::Int      ); break;
					case EKeyword::Struct:   m_tok = Token(EKeyword::Struct   ); break;
					case EKeyword::Break:    m_tok = Token(EKeyword::Break    ); break;
					case EKeyword::Else:     m_tok = Token(EKeyword::Else     ); break;
					case EKeyword::Long:     m_tok = Token(EKeyword::Long     ); break;
					case EKeyword::Switch:   m_tok = Token(EKeyword::Switch   ); break;
					case EKeyword::Case:     m_tok = Token(EKeyword::Case     ); break;
					case EKeyword::Enum:     m_tok = Token(EKeyword::Enum     ); break;
					case EKeyword::Register: m_tok = Token(EKeyword::Register ); break;
					case EKeyword::Typedef:  m_tok = Token(EKeyword::Typedef  ); break;
					case EKeyword::Char:     m_tok = Token(EKeyword::Char     ); break;
					case EKeyword::Extern:   m_tok = Token(EKeyword::Extern   ); break;
					case EKeyword::Return:   m_tok = Token(EKeyword::Return   ); break;
					case EKeyword::Union:    m_tok = Token(EKeyword::Union    ); break;
					case EKeyword::Const:    m_tok = Token(EKeyword::Const    ); break;
					case EKeyword::Float:    m_tok = Token(EKeyword::Float    ); break;
					case EKeyword::Short:    m_tok = Token(EKeyword::Short    ); break;
					case EKeyword::Unsigned: m_tok = Token(EKeyword::Unsigned ); break;
					case EKeyword::Continue: m_tok = Token(EKeyword::Continue ); break;
					case EKeyword::For:      m_tok = Token(EKeyword::For      ); break;
					case EKeyword::Signed:   m_tok = Token(EKeyword::Signed   ); break;
					case EKeyword::Void:     m_tok = Token(EKeyword::Void     ); break;
					case EKeyword::Default:  m_tok = Token(EKeyword::Default  ); break;
					case EKeyword::Goto:     m_tok = Token(EKeyword::Goto     ); break;
					case EKeyword::Sizeof:   m_tok = Token(EKeyword::Sizeof   ); break;
					case EKeyword::Volatile: m_tok = Token(EKeyword::Volatile ); break;
					case EKeyword::Do:       m_tok = Token(EKeyword::Do       ); break;
					case EKeyword::If:       m_tok = Token(EKeyword::If       ); break;
					case EKeyword::Static:   m_tok = Token(EKeyword::Static   ); break;
					case EKeyword::While:    m_tok = Token(EKeyword::While    ); break;
					}
					src.pop_front(emit);
					emit = 0;
					break;
				}
			case 'L':
				// might be a char literal, string literal, or an identifier
				{
					if (src[1] == '\'' || src[1] == '\"') goto tokeniser_extract_str_literal;
					goto tokeniser_extract_identitier;
				}

			case '0': case '1': case '2': case '3': case '4':
			case '5': case '6': case '7': case '8': case '9':
				// Extract a numeric constant
				tokeniser_extract_constant:
				{
					pr::Number num;
					if (!pr::str::ExtractNumber(num, src))
						throw Exception(EResult::SyntaxError, src.Loc(), "Invalid numeric constant");

					m_tok = num.m_type == Number::EType::FP ? Token(EConstant::FloatingPoint, num.db()) : Token(EConstant::Integral, num.ll());
					break;
				}

			case '\'':
			case '\"':
				// Extract a literal c-string (possibly prefixed with 'L')
				tokeniser_extract_str_literal:
				{
					auto is_wide = *src == 'L' ? ++src,true : false;
					auto is_char = *src == '\'';
						
					string str;
					if (!pr::str::ExtractString(str, src, L'\\', nullptr)) throw Exception(EResult::SyntaxError, src.Loc(), "Invalid literal constant");
					if      (is_char) m_tok = Token(EConstant::Integral, Token::int64(str[0])); // char literals are actually integral constants
					else if (is_wide) m_tok = Token(EConstant::WStringLiteral, str);
					else              m_tok = Token(EConstant::StringLiteral, str);
					break;
				}

			case '.':
				{
					if (src[1] == '.' && src[2] == '.') { m_tok = Token(ESymbol::Ellipsis); src += 3; break; }
					if (pr::str::IsDigit(src[1])) goto tokeniser_extract_constant; // '.' can be the start of a number
					m_tok = Token(ESymbol::Dot); ++src;
					break;
				}

			case '<':
				{
					if (src[1] == '<' && src[2] == '=') { m_tok = Token(ESymbol::ShiftLAssign); src += 3; break; }
					if (src[1] == '<') { m_tok = Token(ESymbol::ShiftL); src += 2; break; }
					if (src[1] == '=') { m_tok = Token(ESymbol::LessEql); src += 2; break; }
					m_tok = Token(ESymbol::LessThan); ++src;
					break;
				}

			case '>':
				{
					if (src[1] == '>' && src[2] == '=') { m_tok = Token(ESymbol::ShiftRAssign); src += 3; break; }
					if (src[1] == '>') { m_tok = Token(ESymbol::ShiftR); src += 2; break; }
					if (src[1] == '=') { m_tok = Token(ESymbol::GtrEql); src += 2; break; }
					m_tok = Token(ESymbol::GtrThan); ++src;
					break;
				}

			case '&':
				{
					if (src[1] == '&') { m_tok = Token(ESymbol::LogicalAnd); src += 2; break; }
					if (src[1] == '=') { m_tok = Token(ESymbol::BitAndAssign); src += 2; break; }
					m_tok = Token(ESymbol::AddressOf); ++src;
					break;
				}

			case '|':
				{
					if (src[1] == '|') { m_tok = Token(ESymbol::LogicalOr); src += 2; break; }
					if (src[1] == '=') { m_tok = Token(ESymbol::BitOrAssign); src += 2; break; }
					m_tok = Token(ESymbol::BitOr); ++src;
					break;
				}

			case '^':
				{
					if (src[1] == '=') { m_tok = Token(ESymbol::BitXorAssign); src += 2; break; }
					m_tok = Token(ESymbol::BitXor); ++src;
					break;
				}

			case '!':
				{
					if (src[1] == '=') { m_tok = Token(ESymbol::NotEqual); src += 2; break; }
					m_tok = Token(ESymbol::Not); ++src;
					break;
				}

			case '=':
				{
					if (src[1] == '=') { m_tok = Token(ESymbol::Equal); src += 2; break; }
					m_tok = Token(ESymbol::Assign); ++src;
					break;
				}

			case '+':
				{
					if (src[1] == '+') { m_tok = Token(ESymbol::Increment); src += 2; break; }
					if (src[1] == '=') { m_tok = Token(ESymbol::AddAssign); src += 2; break; }
					m_tok = Token(ESymbol::Plus); ++src;
					break;
				}

			case '-':
				{
					if (src[1] == '-') { m_tok = Token(ESymbol::Decrement); src += 2; break; }
					if (src[1] == '=') { m_tok = Token(ESymbol::SubAssign); src += 2; break; }
					m_tok = Token(ESymbol::Minus); ++src;
					break;
				}

			case '*':
				{
					if (src[1] == '=') { m_tok = Token(ESymbol::MulAssign); src += 2; break; }
					m_tok = Token(ESymbol::Ptr); ++src;
					break;
				}

			case '%':
				{
					if (src[1] == '=') { m_tok = Token(ESymbol::ModAssign); src += 2; break; }
					m_tok = Token(ESymbol::Modulus); ++src;
					break;
				}

			case '/':
				{
					if (src[1] == '=') { m_tok = Token(ESymbol::DivAssign); src += 2; break; }
					m_tok = Token(ESymbol::Divide); ++src;
					break;
				}

			case '(': m_tok = Token(ESymbol::ParenthOpen  ); ++src; break;
			case ')': m_tok = Token(ESymbol::ParenthClose ); ++src; break;
			case '[': m_tok = Token(ESymbol::BracketOpen  ); ++src; break;
			case ']': m_tok = Token(ESymbol::BracketClose ); ++src; break;
			case '{': m_tok = Token(ESymbol::BraceOpen    ); ++src; break;
			case '}': m_tok = Token(ESymbol::BraceClose   ); ++src; break;
			case ',': m_tok = Token(ESymbol::Comma        ); ++src; break;
			case ';': m_tok = Token(ESymbol::SemiColon    ); ++src; break;
			case ':': m_tok = Token(ESymbol::Colon        ); ++src; break;
			case '?': m_tok = Token(ESymbol::Conditional  ); ++src; break;
			case '~': m_tok = Token(ESymbol::Complement   ); ++src; break;
			case '#': m_tok = Token(ESymbol::Hash         ); ++src; break;
			case '$': m_tok = Token(ESymbol::Dollar       ); ++src; break;
			case '@': m_tok = Token(ESymbol::At           ); ++src; break;
			}

			// Clear the buffer now that we have the token
			src.pop_front(emit);
			emit = 0;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::script
{
	PRUnitTest(TokeniserTests)
	{
		using namespace pr;

		char const* str_in =
			"auto double int struct break else long switch case enum register typedef "
			"char extern return union const float short unsigned continue for signed "
			"void default goto sizeof volatile do if static while"
			" \n = ; ~ ! * & + - / % < > | ^ , ? { } [ ] ( ) . : # $ @ ++ -- << >> <= "
			">= == != && || <<= >>= &= |= ^= += -= *= /= %= ..."
			;

		PtrA src(str_in);
		Tokeniser tkr(src);
		PR_CHECK(*tkr == EKeyword::Auto     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Double   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Int      , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Struct   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Break    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Else     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Long     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Switch   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Case     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Enum     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Register , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Typedef  , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Char     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Extern   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Return   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Union    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Const    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Float    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Short    , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Unsigned , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Continue , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::For      , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Signed   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Void     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Default  , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Goto     , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Sizeof   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Volatile , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Do       , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::If       , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::Static   , true); ++tkr;
		PR_CHECK(*tkr == EKeyword::While    , true); ++tkr;

		PR_CHECK(*tkr == ESymbol::NewLine      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Assign       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::SemiColon    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Complement   , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Not          , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Ptr          , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::AddressOf    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Plus         , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Minus        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Divide       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Modulus      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LessThan     , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::GtrThan      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitOr        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitXor       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Comma        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Conditional  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BraceOpen    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BraceClose   , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BracketOpen  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BracketClose , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ParenthOpen  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ParenthClose , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Dot          , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Colon        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Hash         , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Dollar       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::At           , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Increment    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Decrement    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftL       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftR       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LessEql      , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::GtrEql       , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Equal        , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::NotEqual     , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LogicalAnd   , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::LogicalOr    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftLAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ShiftRAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitAndAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitOrAssign  , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::BitXorAssign , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::AddAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::SubAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::MulAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::DivAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::ModAssign    , true); ++tkr;
		PR_CHECK(*tkr == ESymbol::Ellipsis     , true); ++tkr;
		PR_CHECK(*tkr == EToken::EndOfStream   , true); ++tkr;
		PR_CHECK(*tkr == EToken::EndOfStream   , true); ++tkr;
	}
}
#endif

