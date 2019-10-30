//*************************************************************
// Expression Evaluator
//  Copyright (c) Rylogic Ltd 2008
//*************************************************************

#pragma once

#include <cmath>
#include <cstring>
#include <array>
#include <string_view>
#include <algorithm>
#include <exception>
#include <type_traits>
#include <cassert>
#include <cerrno>
#include <charconv>
#include "pr/common/hash.h"
#include "pr/container/span.h"

namespace pr
{
	namespace eval
	{
		// Expression tokens
		enum class ETok
		{
			None,
			If,
			Else,
			Comma,
			LogOR,
			LogAND,
			BitOR,
			BitXOR,
			BitAND,
			LogEql,
			LogNEql,
			LogLT,
			LogLTEql,
			LogGT,
			LogGTEql,
			LeftShift,
			RightShift,
			Add,
			Sub,
			Mul,
			Div,
			Mod,
			UnaryPlus,
			UnaryMinus,
			Comp,
			Not,
			Fmod,
			Abs,
			Ceil,
			Floor,
			Round,
			Min,
			Max,
			Clamp,
			Sin,
			Cos,
			Tan,
			ASin,
			ACos,
			ATan,
			ATan2,
			SinH,
			CosH,
			TanH,
			Exp,
			Log,
			Log10,
			Pow,
			Sqr,
			Sqrt,
			Len2,
			Len3,
			Len4,
			Deg,
			Rad,
			HashCT,
			OpenParenthesis,
			CloseParenthesis,
			Value
		};

		// Constants
		constexpr double const TAU = double(6.283185307179586476925286766559);
		constexpr double const PHI = double(1.618033988749894848204586834);

		// Convert a string into a character stream
		template <typename Char> struct char_range
		{
			Char const* m_ptr;
			Char const* m_end;

			char_range(Char const* ptr, Char const* end)
				:m_ptr(ptr)
				,m_end(end)
			{}
			char_range(Char const* ptr)
				:char_range(ptr, ptr + std::char_traits<Char>::length(ptr))
			{}
			char_range(std::basic_string_view<Char> sv)
				:char_range(sv.data(), sv.data() + sv.size())
			{}
			operator bool() const
			{
				return m_ptr != m_end;
			}
			Char operator *() const
			{
				return m_ptr != m_end ? *m_ptr : '\0';
			}
			char_range& operator ++()
			{
				m_ptr += int(m_ptr != m_end);
				return *this;
			}
			char_range& operator += (int n)
			{
				m_ptr += std::min(n, static_cast<int>(m_end - m_ptr));
				return *this;
			}
			char_range operator + (int n) const
			{
				return char_range(m_ptr + std::min(n, static_cast<int>(m_end - m_ptr)), m_end);
			}
			size_t size() const
			{
				return m_end - m_ptr;
			}
			std::basic_string<Char> str() const
			{
				return std::basic_string<Char>(m_ptr, m_end);
			}
		};

		// An integral or floating point value
		struct Val
		{
			enum class EType { Unknown, Intg, Real, };

			// The value
			union
			{
				long long m_ll;
				double m_db;
			};
			EType m_ty; // True if 'db' should be used, false if 'll'

			Val() noexcept
				:m_ll()
				,m_ty(EType::Unknown)
			{}
			explicit Val(long long ll) noexcept
				:m_ll(ll)
				,m_ty(EType::Intg)
			{}
			explicit Val(double db) noexcept
				:m_db(db)
				,m_ty(EType::Real)
			{}
			Val& operator = (unsigned long long v) noexcept
			{
				m_ll = static_cast<long long>(v);
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (long long v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (double v) noexcept
			{
				m_db = v;
				m_ty = EType::Real;
				return *this;
			}
			Val& operator = (wchar_t v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (char v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}
			Val& operator = (bool v) noexcept
			{
				m_ll = v;
				m_ty = EType::Intg;
				return *this;
			}

			// Read the value as a double or long long
			double db() const
			{
				return
					m_ty == EType::Real ? m_db :
					m_ty == EType::Intg ? static_cast<double>(m_ll) :
					throw std::runtime_error("Value not given. Value type is unknown");
			}
			long long ll() const
			{
				return
					m_ty == EType::Intg ? m_ll :
					m_ty == EType::Real ? static_cast<long long>(m_db) :
					throw std::runtime_error("Value not given. Value type is unknown");
			}

			// True if a valid value has been assigned
			bool valid() const
			{
				return m_ty != EType::Unknown;
			}

			// Operators
			friend Val  operator +  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() + rhs.db()) : Val(lhs.ll() + rhs.ll()); }
			friend Val  operator -  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() - rhs.db()) : Val(lhs.ll() - rhs.ll()); }
			friend Val  operator *  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() * rhs.db()) : Val(lhs.ll() * rhs.ll()); }
			friend Val  operator /  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? Val(lhs.db() / rhs.db()) : Val(lhs.ll() / rhs.ll()); }
			friend bool operator == (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? lhs.db() == rhs.db() : lhs.ll() == rhs.ll(); }
			friend bool operator <  (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? lhs.db() <  rhs.db() : lhs.ll() <  rhs.ll(); }
			friend bool operator <= (Val const& lhs, Val const& rhs) { return (lhs.m_ty == EType::Real || rhs.m_ty == EType::Real) ? lhs.db() <= rhs.db() : lhs.ll() <= rhs.ll(); }
		};

		// Returns the precedence of a token
		// How to work out precedence:
		//   NewOp = the op whose precedence you want to know
		//   RhsOp = an op in this list
		// Ask, "If I encounter RhsOp next after NewOp, should NewOp go on hold while
		//  RhsOp is evaluated, or should I stop and evaluate up to NewOp before carrying on".
		//  Also the visa versa case.
		// If NewOp should go on hold, then it has lower precedence (i.e. NewOp < RhsOp)
		// If NewOp needs evaluating, then RhsOp has lower precedence (i.e. RhsOp > NewOp)
		template <typename = void> int Precedence(ETok tok)
		{
			switch (tok)
			{
			default: throw std::exception("Unknown token");
			case ETok::None            : return   0;
			case ETok::Comma           : return  20;
			case ETok::If              : return  30;
			case ETok::Else            : return  30;
			case ETok::LogOR           : return  40;
			case ETok::LogAND          : return  50;
			case ETok::BitOR           : return  60;
			case ETok::BitXOR          : return  70;
			case ETok::BitAND          : return  80;
			case ETok::LogEql          : return  90;
			case ETok::LogNEql         : return  90;
			case ETok::LogLT           : return 100;
			case ETok::LogLTEql        : return 100;
			case ETok::LogGT           : return 100;
			case ETok::LogGTEql        : return 100;
			case ETok::LeftShift       : return 110;
			case ETok::RightShift      : return 110;
			case ETok::Add             : return 120;
			case ETok::Sub             : return 120;
			case ETok::Mul             : return 130;
			case ETok::Div             : return 130;
			case ETok::Mod             : return 130;
			case ETok::UnaryPlus       : return 140;
			case ETok::UnaryMinus      : return 140;
			case ETok::Comp            : return 140;
			case ETok::Not             : return 140;
			case ETok::Fmod            : return 200;
			case ETok::Abs             : return 200;
			case ETok::Ceil            : return 200;
			case ETok::Floor           : return 200;
			case ETok::Round           : return 200;
			case ETok::Min             : return 200;
			case ETok::Max             : return 200;
			case ETok::Clamp           : return 200;
			case ETok::Sin             : return 200;
			case ETok::Cos             : return 200;
			case ETok::Tan             : return 200;
			case ETok::ASin            : return 200;
			case ETok::ACos            : return 200;
			case ETok::ATan            : return 200;
			case ETok::ATan2           : return 200;
			case ETok::SinH            : return 200;
			case ETok::CosH            : return 200;
			case ETok::TanH            : return 200;
			case ETok::Exp             : return 200;
			case ETok::Log             : return 200;
			case ETok::Log10           : return 200;
			case ETok::Pow             : return 200;
			case ETok::Sqr             : return 200;
			case ETok::Sqrt            : return 200;
			case ETok::Len2            : return 200;
			case ETok::Len3            : return 200;
			case ETok::Len4            : return 200;
			case ETok::Deg             : return 200;
			case ETok::Rad             : return 200;
			case ETok::HashCT          : return 200;
			case ETok::OpenParenthesis : return 300;
			case ETok::CloseParenthesis: return 300;
			case ETok::Value           : return 1000;
			}
		}

		// Advance 'expr' to the next non-whitespace character
		template <typename Char> char_range<Char> EatWS(char_range<Char>& expr)
		{
			for (; expr && std::isspace(*expr); ++expr) {}
			return expr;
		}

		// Read a value (greedily) from 'expr'
		template <typename Char> bool ReadValue(char_range<Char>& expr, Val& out)
		{
			// Conversion functions
			auto ReadReal = [](char_range<Char> src, Val& v) -> size_t
			{
				try
				{
					size_t len;
					v = std::stod(src.str(), &len);
					return len;
				}
				catch (std::invalid_argument const&)
				{
					return 0;
				}
			};
			auto RealIntegral = [](char_range<Char> src, Val& v) -> size_t
			{
				Val sval; size_t len0;
				try { sval = std::stoll(src.str(), &len0, 0); }
				catch (std::out_of_range const&) { len0 = 0; }
				catch (std::invalid_argument const&) { len0 = 0; }
				
				Val uval; size_t len1;
				try { uval = std::stoull(src.str(), &len1, 0); }
				catch (std::out_of_range const&) { len1 = 0; }
				catch (std::invalid_argument const&) { len1 = 0; }

				auto len = std::max(len0, len1);
				if (len == 0)
					return 0;

				// Ignore the optional suffix
				src += static_cast<int>(len);
				if (*src == 'u' || *src == 'U') { ++src; ++len; }
				if (*src == 'l' || *src == 'L') { ++src; ++len; }
				if (*src == 'l' || *src == 'L') { ++src; ++len; }

				// Return the integral value
				v = len0 != 0 ? sval :
					len1 != 0 ? uval :
					throw std::runtime_error("at least one is supposed to be valid");
				return len;
			};
			auto ReadLiteral = [](char_range<Char> src, Val& v) -> size_t
			{
				auto beg = src.m_ptr;

				if (*src != '\'')
					return 0;

				// Allow for escaped characters
				if (*++src == '\\')
				{
					switch (*++src)
					{
					default: break;
					case 'a':  v = '\a'; ++src; break;
					case 'b':  v = '\b'; ++src; break;
					case 'f':  v = '\f'; ++src; break;
					case 'n':  v = '\n'; ++src; break;
					case 'r':  v = '\r'; ++src; break;
					case 't':  v = '\t'; ++src; break;
					case 'v':  v = '\v'; ++src; break;
					case '\'': v = '\''; ++src; break;
					case '\"': v = '\"'; ++src; break;
					case '\\': v = '\\'; ++src; break;
					case '\?': v = '\?'; ++src; break;
					case '0':
					case '1':
					case '2':
					case '3':
						{
							// ASCII character in octal
							std::basic_string<Char> oct;
							for (; *src && *src >= '0' && *src <= '7'; ++src) oct.append(1, *src);
							try { v = std::stoll(oct, nullptr, 8); }
							catch (std::out_of_range const&) { return 0; }
							break;
						}
					case 'x':
						{
							// ASCII or UNICODE character in hex
							std::basic_string<Char> hex;
							for (; *src && std::isxdigit(*src); ++src) hex.append(1, *src);
							try { v = std::stoll(hex, nullptr, 16); }
							catch (std::out_of_range const&) { return 0; }
							break;
						}
					}
				}
				else
				{
					v = *src;
					++src;
				}
				
				if (*src != '\'')
					return 0;
					
				++src;
				return src.m_ptr - beg;
			};

			// Greedy read, whichever consumes the most characters is the interpretted value
			Val v; size_t len = 0;
			if (auto l = ReadReal    (expr, v); l > len) { len = l; out = v; }
			if (auto l = RealIntegral(expr, v); l > len) { len = l; out = v; }
			if (auto l = ReadLiteral (expr, v); l > len) { len = l; out = v; }
			if (len == 0)
				return false;

			expr += static_cast<int>(len);
			return true;
		}

		// Extract a token from 'expr'
		// If the token is a value then 'expr' is advanced past the value
		// if it's an operator it isn't. This is so that operator precedence works
		// 'follows_value' should be true if the preceding expression evaluates to a value
		template <typename Char> ETok Token(char_range<Char>& expr, Val& val, bool follows_value)
		{
			// Case insensitive string compare
			auto cmp = [](char_range<Char> s, char const* pattern)
			{
				for (; s && *pattern == std::tolower(*s); ++s, ++pattern) {}
				return *pattern == 0;
			};

			// Skip any leading whitespace
			if (!EatWS(expr))
				return ETok::None;

			// Look for an operator
			// Convert Add/Sub to unary plus/minus by looking at the previous expression
			// If the previous expression evaluates to a value then Add/Sub are binary expressions
			switch (std::tolower(*expr))
			{
			default: break;
			case '+': 
				{
					return follows_value ? ETok::Add : ETok::UnaryPlus;
				}
			case '-': 
				{
					return follows_value ? ETok::Sub : ETok::UnaryMinus;
				}
			case '*': 
				{
					return ETok::Mul;
				}
			case '/': 
				{
					return ETok::Div;
				}
			case '%':
				{
					return ETok::Mod;
				}
			case '~':
				{
					return ETok::Comp;
				}
			case ',':
				{
					return ETok::Comma;
				}
			case '^':
				{
					return ETok::BitXOR;
				}
			case '(':
				{
					return ETok::OpenParenthesis;
				}
			case ')': 
				{
					return ETok::CloseParenthesis;
				}
			case '?':
				{
					return ETok::If;
				}
			case ':':
				{
					return ETok::Else;
				}
			case '<':
				{
					if (cmp(expr, "<<")) return ETok::LeftShift;
					if (cmp(expr, "<=")) return ETok::LogLTEql;
					return ETok::LogLT;
				}
			case '>':
				{
					if (cmp(expr, ">>")) return ETok::RightShift;
					if (cmp(expr, ">=")) return ETok::LogGTEql;
					return ETok::LogGT;
				}
			case '|':
				{
					if (cmp(expr, "||")) return ETok::LogOR;
					return ETok::BitOR;
				}
			case '&':
				{
					if (cmp(expr, "&&")) return ETok::LogAND;
					return ETok::BitAND;
				}
			case '=':
				{
					if (cmp(expr, "==")) return ETok::LogEql;
					break;
				}
			case '!':
				{
					if (cmp(expr, "!=")) return ETok::LogNEql;
					return ETok::Not;
				}
			case 'a':
				{
					if (cmp(expr, "abs")) return ETok::Abs;
					if (cmp(expr, "asin")) return ETok::ASin;
					if (cmp(expr, "acos")) return ETok::ACos;
					if (cmp(expr, "atan2")) return ETok::ATan2;
					if (cmp(expr, "atan")) return ETok::ATan;
					break;
				}
			case 'c':
				{
					if (cmp(expr, "clamp")) return ETok::Clamp;
					if (cmp(expr, "ceil")) return ETok::Ceil;
					if (cmp(expr, "cosh")) return ETok::CosH;
					if (cmp(expr, "cos")) return ETok::Cos;
					break;
				}
			case 'd':
				{
					if (cmp(expr, "deg")) return ETok::Deg;
					break;
				}
			case 'e':
				{
					if (cmp(expr, "exp")) return ETok::Exp;
					break;
				}
			case 'f':
				{
					if (cmp(expr, "floor")) return ETok::Floor;
					if (cmp(expr, "fmod")) return ETok::Fmod;
					if (cmp(expr, "false")) { expr += 5; val = 0.0; return ETok::Value; }
					break;
				}
			case 'h':
				{
					if (cmp(expr, "hashct")) return ETok::HashCT;
					break;
				}
			case 'l':
				{
					if (cmp(expr, "log10")) return ETok::Log10;
					if (cmp(expr, "log")) return ETok::Log;
					if (cmp(expr, "len2")) return ETok::Len2;
					if (cmp(expr, "len3")) return ETok::Len3;
					if (cmp(expr, "len4")) return ETok::Len4;
					break;
				}
			case 'm':
				{
					if (cmp(expr, "min")) return ETok::Min;
					if (cmp(expr, "max")) return ETok::Max;
					break;
				}
			case 'p':
				{
					if (cmp(expr, "pow")) return ETok::Pow;
					if (cmp(expr, "phi")) { expr += 3; val = PHI; return ETok::Value; }
					if (cmp(expr, "pi")) { expr += 2; val = TAU / 2.0; return ETok::Value; }
					break;
				}
			case 'r':
				{
					if (cmp(expr, "round")) return ETok::Round;
					if (cmp(expr, "rad")) return ETok::Rad;
					break;
				}
			case 's':
				{
					if (cmp(expr, "sinh")) return ETok::SinH;
					if (cmp(expr, "sin")) return ETok::Sin;
					if (cmp(expr, "sqrt")) return ETok::Sqrt;
					if (cmp(expr, "sqr")) return ETok::Sqr;
					break;
				}
			case 't':
				{
					if (cmp(expr, "tanh")) return ETok::TanH;
					if (cmp(expr, "tan")) return ETok::Tan;
					if (cmp(expr, "tau")) { expr += 3; val = TAU; return ETok::Value; }
					if (cmp(expr, "true")) { expr += 4; val = 1.0; return ETok::Value; }
					break;
				}
			}

			// If it's not an operator, try extracting an operand
			return ReadValue(expr, val) ? ETok::Value : ETok::None;
		}

		// Evaluate an expression.
		// Called recursively for each operation within an expression.
		// 'parent_op' is used to determine precedence order.
		template <typename Char> bool Eval(char_range<Char>& expr, std::span<Val> result, int ridx, ETok parent_op, bool l2r = true)
		{
			// Each time round the while loop should result in a value.
			// Operation tokens result in recursive calls.
			bool follows_value = false;
			for (; expr; )
			{
				Val val;
				auto tok = Token(expr, val, follows_value);
				follows_value = true;

				// If the next token has lower precedence than the parent operation
				// then return to allow the parent op to evaluate
				auto prec0 = Precedence(tok);
				auto prec1 = Precedence(parent_op);
				if (prec0 < prec1)
					return true;
				if (prec0 == prec1 && l2r)
					return true;
				
				switch (tok)
				{
				case ETok::None:
					{
						return expr;
					}
				case ETok::Value:
					{
						result[ridx] = val;
						break;
					}
				case ETok::Add:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] + rhs[0];
						break;
					}
				case ETok::Sub:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] - rhs[0];
						break;
					}
				case ETok::Mul:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] * rhs[0];
						break;
					}
				case ETok::Div:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] / rhs[0];
						break;
					}
				case ETok::Mod:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() % rhs[0].ll();
						break;
					}
				case ETok::UnaryPlus:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok, false)) return false;
						result[ridx] = rhs[0];
						break;
					}
				case ETok::UnaryMinus:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok, false)) return false;
						switch (rhs[0].m_ty) {
						case Val::EType::Real: result[ridx] = -rhs[0].db(); break; // don't use ?: as the result will always be double
						case Val::EType::Intg: result[ridx] = -rhs[0].ll(); break; // result[ridx].ll() will get promoted to double
						default: throw std::runtime_error("Unknown result type for unary minus");
						}
						break;
					}
				case ETok::Comp:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok, false)) return false;
						result[ridx] = ~rhs[0].ll();
						break;
					}
				case ETok::Not:
					{
						std::array<Val, 1> rhs;
						if (!Eval(++expr, rhs, 0, tok, false)) return false;
						result[ridx] = !rhs[0].ll();
						break;
					}
				case ETok::LogOR:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() || rhs[0].ll();
						break;
					}
				case ETok::LogAND:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() && rhs[0].ll();
						break;
					}
				case ETok::BitOR:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() | rhs[0].ll();
						break;
					}
				case ETok::BitXOR:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() ^ rhs[0].ll();
						break;
					}
				case ETok::BitAND:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() & rhs[0].ll();
						break;
					}
				case ETok::LogEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx] == rhs[0];
						break;
					}
				case ETok::LogNEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = !(result[ridx] == rhs[0]);
						break;
					}
				case ETok::LogLT:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() < rhs[0].ll();
						break;
					}
				case ETok::LogLTEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() <= rhs[0].ll();
						break;
					}
				case ETok::LogGT:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() > rhs[0].ll();
						break;
					}
				case ETok::LogGTEql:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = result[ridx].ll() >= rhs[0].ll();
						break;
					}
				case ETok::LeftShift:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = static_cast<int64_t>(static_cast<uint64_t>(result[ridx].ll()) << rhs[0].ll());
						break;
					}
				case ETok::RightShift:
					{
						std::array<Val, 1> rhs;
						if (!Eval(expr += 2, rhs, 0, tok)) return false;
						result[ridx] = static_cast<int64_t>(static_cast<uint64_t>(result[ridx].ll()) >> rhs[0].ll());
						break;
					}
				case ETok::Fmod:
					{
						std::array<Val, 2> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::fmod(args[0].db(), args[1].db());
						break;
					}
				case ETok::Ceil:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::ceil(args[0].db());
						break;
					}
				case ETok::Floor:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 5, args, 0, tok)) return false;
						result[ridx] = std::floor(args[0].db());
						break;
					}
				case ETok::Round:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 5, args, 0, tok)) return false;
						result[ridx] = std::round(args[0].db());
						break;
					}
				case ETok::Min:
					{
						std::array<Val, 2> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = args[0] < args[1] ? args[0] : args[1];
						break;
					}
				case ETok::Max:
					{
						std::array<Val, 2> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = args[0] < args[1] ? args[1] : args[0];
						break;
					}
				case ETok::Clamp:
					{
						std::array<Val, 3> args;
						if (!Eval(expr += 5, args, 0, tok)) return false;
						result[ridx] = args[0] < args[1] ? args[1] : args[2] < args[0] ? args[2] : args[0];
						break;
					}
				case ETok::Abs:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = 
							args[0].m_ty == Val::EType::Intg ? std::abs(args[0].ll()) :
							args[0].m_ty == Val::EType::Real ? std::abs(args[0].db()) :
							throw std::runtime_error("Invalid argument for 'abs'");
						break;
					}
				case ETok::Sin:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = std::sin(args[0].db());
						break;
					}
				case ETok::Cos:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = std::cos(args[0].db());
						break;
					}
				case ETok::Tan:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = std::tan(args[0].db());
						break;
					}
				case ETok::ASin:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::asin(args[0].db());
						break;
					}
				case ETok::ACos:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::acos(args[0].db());
						break;
					}
				case ETok::ATan:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::atan(args[0].db());
						break;
					}
				case ETok::ATan2:
					{
						std::array<Val, 2> args;
						if (!Eval(expr += 5, args, 0, tok)) return false;
						result[ridx] = std::atan2(args[0].db(), args[1].db());
						break;
					}
				case ETok::SinH:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::sinh(args[0].db());
						break;
					}
				case ETok::CosH:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::cosh(args[0].db());
						break;
					}
				case ETok::TanH:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::tanh(args[0].db());
						break;
					}
				case ETok::Exp:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = std::exp(args[0].db());
						break;
					}
				case ETok::Log:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = std::log(args[0].db());
						break;
					}
				case ETok::Log10:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 5, args, 0, tok)) return false;
						result[ridx] = std::log10(args[0].db());
						break;
					}
				case ETok::Pow:
					{
						std::array<Val, 2> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = std::pow(args[0].db(), args[1].db());
						break;
					}
				case ETok::Sqr:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = args[0] * args[0];
						break;
					}
				case ETok::Sqrt:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::sqrt(args[0].db());
						break;
					}
				case ETok::Len2:
					{
						std::array<Val, 2> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db());
						break;
					}
				case ETok::Len3:
					{
						std::array<Val, 3> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db());
						break;
					}
				case ETok::Len4:
					{
						std::array<Val, 4> args;
						if (!Eval(expr += 4, args, 0, tok)) return false;
						result[ridx] = std::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db() + args[3].db()*args[3].db());
						break;
					}
				case ETok::Deg:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = args[0].db() * (360.0 / TAU);
						break;
					}
				case ETok::Rad:
					{
						std::array<Val, 1> args;
						if (!Eval(expr += 3, args, 0, tok)) return false;
						result[ridx] = args[0].db() * (TAU / 360.0);
						break;
					}
				case ETok::HashCT:
					{
						std::basic_string<Char> str;
						expr += 6;
						EatWS(expr);
						if (*expr == '(') ++expr; else return false;
						EatWS(expr);
						if (*expr == '"') ++expr; else return false;
						for (bool esc = false; expr && (esc || *expr != '\"'); esc = !esc && *expr == '\\', ++expr) str.append(1, *expr);
						if (*expr == '"') ++expr; else return false;
						result[ridx] = static_cast<long long>(hash::HashCT(str.data(), str.data() + str.size()));
						break;
					}
				case ETok::Comma:
					{
						if (ridx + 1 == static_cast<int>(result.size()))
							throw std::runtime_error("too many parameters");

						++expr;
						++ridx;
						follows_value = false;
						break;
					}
				case ETok::OpenParenthesis:
					{
						// Parent op is 'None' because it has the lowest precedence
						if (!Eval(++expr, result, ridx, ETok::None)) return false;
						break;
					}
				case ETok::CloseParenthesis:
					{
						// Wait for the parent op to be the 'Open Parenthesis'
						if (parent_op == ETok::None) ++expr;
						return true;
					}
				case ETok::If:
					{
						std::array<Val, 2> args;
						if (!Eval(++expr, args, 0, ETok::None)) return false;
						result[ridx] = result[ridx].ll() != 0 ? args[0] : args[1];
						break;
					}
				case ETok::Else:
					{
						if (ridx + 1 == static_cast<int>(result.size()))
							throw std::runtime_error("result buffer too small");

						if (!Eval(++expr, result, ++ridx, ETok::Else)) return false;
						++ridx;
						return true;
					}
				default:
					{
						throw std::runtime_error("unknown expression token");
					}
				}
			}
			return true;
		}
	
		// Evaluate an expression.
		template <typename Char>
		inline Val Evaluate(char_range<Char> expr)
		{
			std::array<Val, 1> result;
			Eval(expr, result, 0, ETok::None);
			return result[0];
		}
		template <typename Char, typename ResType>
		inline bool Evaluate(char_range<Char> expr, ResType& out)
		{
			try
			{
				auto val = Evaluate<Char>(expr);
				if constexpr(std::is_integral_v<ResType>)
					out = static_cast<ResType>(val.ll());
				else if constexpr(std::is_floating_point_v<ResType>)
					out = static_cast<ResType>(val.db());
				else
					static_assert(false, "Unsupported result type");

				return true;
			}
			catch (std::exception const&)
			{
				return false;
			}
		}
	}

	// The following functions are not templated on character type because
	// type deduction cannot convert string types to 'basic_string_view<Char>'.

	// Evaluate an expression. Throws on syntax error.
	inline eval::Val Evaluate(std::string_view expr)
	{
		return eval::Evaluate<char>(expr);
	}
	inline eval::Val Evaluate(std::wstring_view expr)
	{
		return eval::Evaluate<wchar_t>(expr);
	}

	// Try evaluate an expression.
	template <typename ResType>
	inline bool Evaluate(std::string_view expr, ResType& out)
	{
		return eval::Evaluate<char, ResType>(expr, out);
	}
	template <typename ResType>
	inline bool Evaluate(std::wstring_view expr, ResType& out)
	{
		return eval::Evaluate<wchar_t, ResType>(expr, out);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::common
{
	namespace unitests::expr_eval
	{
		template <typename ResType> bool Expr(std::string_view expr, ResType result) { static_assert(false); }
		template <> bool Expr(std::string_view expr, double                  result) { double        val; return Evaluate(expr, val) && FEql(val, result); }
		template <> bool Expr(std::string_view expr, float                   result) { float         val; return Evaluate(expr, val) && FEql(val, result); }
		template <> bool Expr(std::string_view expr, uint64_t                result) { uint64_t      val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, int64_t                 result) { int64_t       val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, unsigned long           result) { unsigned long val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, long                    result) { long          val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, uint32_t                result) { uint32_t      val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, int                     result) { int           val; return Evaluate(expr, val) && val == result; }
		template <> bool Expr(std::string_view expr, bool                    result) { bool          val; return Evaluate(expr, val) && val == result; }

		template <typename ValueType>
		bool ReadValue(pr::eval::char_range<char> expr, ValueType expected_result)
		{
			pr::eval::Val val;
			if (!pr::eval::ReadValue(expr, val))
				return false;

			if constexpr (std::is_integral_v<ValueType> && std::is_unsigned_v<ValueType>)
				return static_cast<uint64_t>(expected_result) == static_cast<uint64_t>(val.ll());
			else if constexpr (std::is_integral_v<ValueType>)
				return expected_result == static_cast<ValueType>(val.ll());
			else if constexpr (std::is_floating_point_v<ValueType>)
				return expected_result == static_cast<ValueType>(val.db());
			else
				static_assert(false);
		}
	}

	PRUnitTest(ExprEvalTests)
	{
		using namespace unitests::expr_eval;
		{
			#define VAL(exp) ReadValue(#exp, (exp))
			PR_CHECK(VAL(1), true);
			PR_CHECK(VAL(1.0), true);
			PR_CHECK(VAL(-1), true);
			PR_CHECK(VAL(-1.0), true);
			PR_CHECK(VAL(10U), true);
			PR_CHECK(VAL(100L), true);
			PR_CHECK(VAL(-100L), true);
			PR_CHECK(VAL(0x1000UL), true);
			PR_CHECK(VAL(0x7FFFFFFF), true);
			PR_CHECK(VAL(0x80000000), true);
			PR_CHECK(VAL(0xFFFFFFFF), true);
			PR_CHECK(VAL(0xFFFFFFFFU), true);
			PR_CHECK(VAL(0xFFFFFFFFULL), true);
			PR_CHECK(VAL(0x7FFFFFFFFFFFFFFFLL), true);
			PR_CHECK(VAL(0xFFFFFFFFFFFFFFFFULL), true);
			#undef VAL
		}
		{
			#define EXPR(exp) Expr(#exp, (exp))
			PR_CHECK(EXPR(1.0), true);
			PR_CHECK(EXPR(+1.0), true);
			PR_CHECK(EXPR(-1.0), true);
			PR_CHECK(EXPR(-(1.0 + 2.0)), true);
			PR_CHECK(EXPR(8.0 * -1.0), true);
			PR_CHECK(EXPR(4.0 * -1.0 + 2.0), true);
			PR_CHECK(EXPR(1.0 + +2.0), true);
			PR_CHECK(EXPR(1.0 - -2.0), true);
			PR_CHECK(EXPR(1.0 - 2.0 - 3.0 + 4.0), true);
			PR_CHECK(EXPR(1.0 * +2.0), true);
			PR_CHECK(EXPR(1 / 2), true);
			PR_CHECK(EXPR(1.0 / 2.0), true);
			PR_CHECK(EXPR(1.0 / 2.0 + 3.0), true);
			PR_CHECK(EXPR(1.0 / 2.0 * 3.0), true);
			PR_CHECK(EXPR((1 || 0) && 2), true);
			PR_CHECK(EXPR(((13 ^ 7) | 6) & 14), true);
			PR_CHECK(EXPR((8 < 9) + (3 <= 3) + (8 > 9) + (2 >= 2) + (1 != 2) + (2 == 2)), true);
			PR_CHECK(EXPR(1.0 + 2.0 * 3.0 - 4.0), true);
			PR_CHECK(EXPR(2.0 * 3.0 + 1.0 - 4.0), true);
			PR_CHECK(EXPR(1.0 - 4.0 + 2.0 * 3.0), true);
			PR_CHECK(EXPR((1.0 + 2.0) * 3.0 - 4.0), true);
			PR_CHECK(EXPR(1.0 + 2.0 * -(3.0 - 4.0)), true);
			PR_CHECK(EXPR(1.0 + (2.0 * (3.0 - 4.0))), true);
			PR_CHECK(EXPR((1.0 + 2.0) * (3.0 - 4.0)), true);
			PR_CHECK(EXPR(~37 & ~0), true);
			PR_CHECK(EXPR(!37 | !0), true);
			PR_CHECK(EXPR(~(0xFFFFFFFF >> 2)), true);
			PR_CHECK(EXPR(~(4294967295 >> 2)), true);
			PR_CHECK(EXPR(~(0xFFFFFFFFLL >> 2)), true);
			PR_CHECK(EXPR(~(4294967295LL >> 2)), true);
			PR_CHECK(Expr("sin(1.0 + 2.0)", std::sin(1.0 + 2.0)), true);
			PR_CHECK(Expr("cos(TAU)", std::cos(eval::TAU)), true);
			PR_CHECK(Expr("tan(PHI)", std::tan(eval::PHI)), true);
			PR_CHECK(Expr("abs( 1.0)", std::abs(1.0)), true);
			PR_CHECK(Expr("abs(-1.0)", std::abs(-1.0)), true);
			PR_CHECK(EXPR(11 % 3), true);
			PR_CHECK(Expr("fmod(11.3, 3.1)", std::fmod(11.3, 3.1)), true);
			PR_CHECK(Expr("3.0 * fmod(17.3, 2.1)", 3.0 * std::fmod(17.3, 2.1)), true);
			PR_CHECK(EXPR(1 << 10), true);
			PR_CHECK(EXPR(1024 >> 3), true);
			PR_CHECK(Expr("ceil(3.4)", std::ceil(3.4)), true);
			PR_CHECK(Expr("ceil(-3.4)", std::ceil(-3.4)), true);
			PR_CHECK(Expr("floor(3.4)", std::floor(3.4)), true);
			PR_CHECK(Expr("floor(-3.4)", std::floor(-3.4)), true);
			PR_CHECK(Expr("asin(-0.8)", std::asin(-0.8)), true);
			PR_CHECK(Expr("acos(0.2)", std::acos(0.2)), true);
			PR_CHECK(Expr("atan(2.3/12.9)", std::atan(2.3 / 12.9)), true);
			PR_CHECK(Expr("atan2(2.3,-3.9)", std::atan2(2.3, -3.9)), true);
			PR_CHECK(Expr("sinh(0.8)", std::sinh(0.8)), true);
			PR_CHECK(Expr("cosh(0.2)", std::cosh(0.2)), true);
			PR_CHECK(Expr("tanh(2.3)", std::tanh(2.3)), true);
			PR_CHECK(Expr("exp(2.3)", std::exp(2.3)), true);
			PR_CHECK(Expr("log(209.3)", std::log(209.3)), true);
			PR_CHECK(Expr("log10(209.3)", std::log10(209.3)), true);
			PR_CHECK(Expr("pow(2.3, -1.3)", std::pow(2.3, -1.3)), true);
			PR_CHECK(Expr("sqrt(2.3)", std::sqrt(2.3)), true);
			PR_CHECK(Expr("sqr(-2.3)", pr::Sqr(-2.3)), true);
			PR_CHECK(Expr("len2(3,4)", std::sqrt(3.0 * 3.0 + 4.0 * 4.0)), true);
			PR_CHECK(Expr("len3(3,4,5)", std::sqrt(3.0 * 3.0 + 4.0 * 4.0 + 5.0 * 5.0)), true);
			PR_CHECK(Expr("len4(3,4,5,6)", std::sqrt(3.0 * 3.0 + 4.0 * 4.0 + 5.0 * 5.0 + 6.0 * 6.0)), true);
			PR_CHECK(Expr("deg(-1.24)", -1.24 * (360.0 / eval::TAU)), true);
			PR_CHECK(Expr("rad(241.32)", 241.32 * (eval::TAU / 360.0)), true);
			PR_CHECK(Expr("round( 3.5)", std::round(3.5)), true);
			PR_CHECK(Expr("round(-3.5)", std::round(-3.5)), true);
			PR_CHECK(Expr("round( 3.2)", std::round(3.2)), true);
			PR_CHECK(Expr("round(-3.2)", std::round(-3.2)), true);
			PR_CHECK(Expr("min(-3.2, -3.4)", std::min(-3.2, -3.4)), true);
			PR_CHECK(Expr("max(-3.2, -3.4)", std::max(-3.2, -3.4)), true);
			PR_CHECK(Expr("clamp(10.0, -3.4, -3.2)", pr::Clamp(10.0, -3.4, -3.2)), true);
			PR_CHECK(Expr("hashct(\"A String\")", hash::HashCT("A String")), true);
			PR_CHECK(Expr("sqr(sqrt(2.3)*-abs(4%2)/15.0-tan(TAU/-6))", pr::Sqr(std::sqrt(2.3) * -std::abs(4 % 2) / 15.0 - std::tan(eval::TAU / -6))), true);
			{
				long long v1 = 0, v0 = 123456789000000LL / 2;
				PR_CHECK(Evaluate("123456789000000 / 2", v1), true);
				PR_CHECK(v0 == v1, true);
			}
			PR_CHECK(Expr("1 != 2 ? 5 : 6", 5), true);
			PR_CHECK(Expr("1 == 2 ? 5 : 6", 6), true);
			PR_CHECK(Expr("true ? 5 : 6 + 1", 5), true);
			PR_CHECK(Expr("false ? 5 : 6 + 1", 7), true);
			PR_CHECK(Expr("sqr(-2) ? (1+2) : max(-2,-3)", 3), true);
			PR_CHECK(Expr("-+1", -1), true);
			PR_CHECK(Expr("-++-1", 1), true);
			PR_CHECK(Expr("!!true", 1), true);
			PR_CHECK(Expr("-!!!false", -1), true);
			PR_CHECK(Expr("10 - 3 - 2", 5), true);
			PR_CHECK(Expr("'1' + '2'", '1' + '2'), true);
			#undef EXPR
		}
	}
}
#endif
