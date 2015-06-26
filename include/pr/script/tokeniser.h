//**********************************
// Script tokeniser
//  Copyright (c) Rylogic Ltd 2011
//**********************************
#ifndef PR_SCRIPT_TOKENISER_H
#define PR_SCRIPT_TOKENISER_H
	
#include "pr/script/script_core.h"
#include "pr/script/token.h"
	
namespace pr
{
	namespace script
	{
		// An interface/base class for a source of tokens
		struct TokenSrc
		{
			virtual ~TokenSrc() {}
			
			// Pointer-like interface
			Token const& operator * () const     { return const_cast<TokenSrc*>(this)->peek(); }
			TokenSrc&    operator ++()           { if (**this) next(); return *this; }
			TokenSrc&    operator +=(int count)  { while (count--) ++(*this); return *this; }
			
		protected:
			virtual void         next() = 0;
			virtual Token const& peek() = 0;
		};
		
		// C Tokeniser
		struct Tokeniser :TokenSrc
		{
			Buffer<> m_buf;                       // The character stream to read from
			Token    m_tok;                       // The token last read from the stream
			bool     m_cached;                    // True when 'm_tok' represents the next token
			
			Tokeniser(Src& src)
			:m_buf(src)
			,m_tok()
			,m_cached(false)
			{}
			
		protected:
			void next() { m_cached = false; }
			Token const& peek()
			{
				if (m_cached) return m_tok;
				
				// line space does not generate tokens
				Eat::LineSpace(m_buf);
				switch (*m_buf)
				{
				default:
					throw Exception(EResult::SyntaxError, m_buf.loc(), "tokeniser failed to understand code starting here");
				case 0:
					m_tok = Token(EToken::EndOfStream, "");
					break;
				case '\n':
					m_tok = Token(ESymbol::NewLine, m_buf.loc().m_line+1); // save the index of the line following this '\n'
					++m_buf;
					break;
				case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': case 'g': case 'h': case 'i':
				case 'j': case 'k': case 'l': case 'm': case 'n': case 'o': case 'p': case 'q': case 'r':
				case 's': case 't': case 'u': case 'v': case 'w': case 'x': case 'y': case 'z':
				case 'A': case 'B': case 'C': case 'D': case 'E': case 'F': case 'G': case 'H': case 'I':
				case 'J': case 'K':           case 'M': case 'N': case 'O': case 'P': case 'Q': case 'R':
				case 'S': case 'T': case 'U': case 'V': case 'W': case 'X': case 'Y': case 'Z':
				case '_':
					// extract an identifier that might be a keyword
					tokeniser_extract_identitier:
					{ 
						string id;
						pr::str::ExtractIdentifier(id, m_buf);
						pr::hash::HashValue hash = Hash::String(id);
						switch (hash)
						{
						default:                 m_tok = Token(EToken::Identifier, id, hash); break;
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
					}break;
					
				case '0': case '1': case '2': case '3': case '4':
				case '5': case '6': case '7': case '8': case '9':
					// Extract a numeric constant
					tokeniser_extract_constant:
					{
						int64 ivalue; double fvalue; bool fp;
						if (!pr::str::ExtractNumber(ivalue, fvalue, fp, m_buf))
							throw Exception(EResult::SyntaxError, m_buf.loc(), "invalid numeric constant");
						m_tok = Token(fp ? EConstant::FloatingPoint : EConstant::Integral, fvalue, ivalue);
					}break;
					
				case '\'':
				case '\"':
					// Extract a literal c-string (possibly prefixed with 'L')
					tokeniser_extract_str_literal:
					{
						bool wide = (*m_buf == 'L');
						if (wide) ++m_buf;
						bool is_char = (*m_buf == '\'');
						m_tok = Token(EConstant::Integral, 0.0, 0);
						if (wide) { if (!pr::str::ExtractString(m_tok.m_wvalue, m_buf, '\\')) throw Exception(EResult::SyntaxError, m_buf.loc(), "invalid wide literal constant"); }
						else      { if (!pr::str::ExtractString(m_tok.m_avalue, m_buf, '\\')) throw Exception(EResult::SyntaxError, m_buf.loc(), "invalid literal constant"); }
						if (is_char) m_tok.m_ivalue   = wide ? m_tok.m_wvalue[0]         : m_tok.m_avalue[0]; // char literals are actually integral constants
						else         m_tok.m_constant = wide ? EConstant::WStringLiteral : EConstant::StringLiteral;
					}break;
					
				case 'L': // might be a char literal, string literal, or an identifier
					m_buf.buffer(2);
					if (m_buf[1] == '\'' || m_buf[1] == '\"') goto tokeniser_extract_str_literal;
					goto tokeniser_extract_identitier;
					
				case '.':
					m_buf.buffer(3);
					if (m_buf[1] == '.' && m_buf[2] == '.') { m_tok = Token(ESymbol::Ellipsis); m_buf += 3; break; }
					if (pr::str::IsDigit(m_buf[1])) goto tokeniser_extract_constant; // '.' can be the start of a number
					m_tok = Token(ESymbol::Dot); ++m_buf;
					break;
					
				case '<':
					m_buf.buffer(3);
					if (m_buf[1] == '<' && m_buf[2] == '=') { m_tok = Token(ESymbol::ShiftLAssign); m_buf += 3; break; }
					if (m_buf[1] == '<') { m_tok = Token(ESymbol::ShiftL); m_buf += 2; break; }
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::LessEql); m_buf += 2; break; }
					m_tok = Token(ESymbol::LessThan); ++m_buf;
					break;
					
				case '>':
					m_buf.buffer(3);
					if (m_buf[1] == '>' && m_buf[2] == '=') { m_tok = Token(ESymbol::ShiftRAssign); m_buf += 3; break; }
					if (m_buf[1] == '>') { m_tok = Token(ESymbol::ShiftR); m_buf += 2; break; }
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::GtrEql); m_buf += 2; break; }
					m_tok = Token(ESymbol::GtrThan); ++m_buf;
					break;
					
				case '&':
					m_buf.buffer(2);
					if (m_buf[1] == '&') { m_tok = Token(ESymbol::LogicalAnd); m_buf += 2; break; }
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::BitAndAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::AddressOf); ++m_buf;
					break;
					
				case '|':
					m_buf.buffer(2);
					if (m_buf[1] == '|') { m_tok = Token(ESymbol::LogicalOr); m_buf += 2; break; }
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::BitOrAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::BitOr); ++m_buf;
					break;
					
				case '^':
					m_buf.buffer(2);
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::BitXorAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::BitXor); ++m_buf;
					break;
					
				case '!':
					m_buf.buffer(2);
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::NotEqual); m_buf += 2; break; }
					m_tok = Token(ESymbol::Not); ++m_buf;
					break;
					
				case '=':
					m_buf.buffer(2);
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::Equal); m_buf += 2; break; }
					m_tok = Token(ESymbol::Assign); ++m_buf;
					break;
				
				case '+':
					m_buf.buffer(2);
					if (m_buf[1] == '+') { m_tok = Token(ESymbol::Increment); m_buf += 2; break; }
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::AddAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::Plus); ++m_buf;
					break;
					
				case '-':
					m_buf.buffer(2);
					if (m_buf[1] == '-') { m_tok = Token(ESymbol::Decrement); m_buf += 2; break; }
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::SubAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::Minus); ++m_buf;
					break;
					
				case '*':
					m_buf.buffer(2);
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::MulAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::Ptr); ++m_buf;
					break;
					
				case '%':
					m_buf.buffer(2);
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::ModAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::Modulus); ++m_buf;
					break;
					
				case '/':
					m_buf.buffer(2);
					if (m_buf[1] == '=') { m_tok = Token(ESymbol::DivAssign); m_buf += 2; break; }
					m_tok = Token(ESymbol::Divide); ++m_buf;
					break;
					
				case '(': m_tok = Token(ESymbol::ParenthOpen  ); ++m_buf; break;
				case ')': m_tok = Token(ESymbol::ParenthClose ); ++m_buf; break;
				case '[': m_tok = Token(ESymbol::BracketOpen  ); ++m_buf; break;
				case ']': m_tok = Token(ESymbol::BracketClose ); ++m_buf; break;
				case '{': m_tok = Token(ESymbol::BraceOpen    ); ++m_buf; break;
				case '}': m_tok = Token(ESymbol::BraceClose   ); ++m_buf; break;
				case ',': m_tok = Token(ESymbol::Comma        ); ++m_buf; break;
				case ';': m_tok = Token(ESymbol::SemiColon    ); ++m_buf; break;
				case ':': m_tok = Token(ESymbol::Colon        ); ++m_buf; break;
				case '?': m_tok = Token(ESymbol::Conditional  ); ++m_buf; break;
				case '~': m_tok = Token(ESymbol::Complement   ); ++m_buf; break;
				case '#': m_tok = Token(ESymbol::Hash         ); ++m_buf; break;
				case '$': m_tok = Token(ESymbol::Dollar       ); ++m_buf; break;
				case '@': m_tok = Token(ESymbol::At           ); ++m_buf; break;
				}
				m_cached = true;
				return m_tok;
			}
			
		private: // no copying
			Tokeniser(Tokeniser const&);
			Tokeniser& operator=(Tokeniser const&);
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_script_tokeniser)
		{
			using namespace pr;
			using namespace pr::script;

			char const* str_in =
				"auto double int struct break else long switch case enum register typedef "
				"char extern return union const float short unsigned continue for signed "
				"void default goto sizeof volatile do if static while"
				" \n = ; ~ ! * & + - / % < > | ^ , ? { } [ ] ( ) . : # $ @ ++ -- << >> <= "
				">= == != && || <<= >>= &= |= ^= += -= *= /= %= ..."
				;
			PtrSrc src(str_in);
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
}
#endif

#endif

