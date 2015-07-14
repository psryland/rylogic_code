//*************************************************************
// Expression Evaluator
//  Copyright (c) Rylogic Ltd 2008
//*************************************************************

#pragma once

#include <string.h>
#include <math.h>
#include <exception>
#include <errno.h>
#include <cassert>

namespace pr
{
	namespace eval_impl
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
			UnaryAdd,
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
			OpenParenthesis,
			CloseParenthesis,
			Value
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
		inline int Precedence(ETok tok)
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
			case ETok::UnaryAdd        : return 140;
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
			case ETok::OpenParenthesis : return 300;
			case ETok::CloseParenthesis: return 300;
			case ETok::Value           : return 1000;
			}
		}

		template <typename Char> struct traits_base;
		template <> struct traits_base<char>
		{
			static double             strtod(char const* str, char** end)                   { return ::strtod(str, end); }
			static long               strtol(char const* str, char** end, int radix)        { return ::strtol(str, end, radix); }
			static unsigned long      strtoul(char const* str, char** end, int radix)       { return ::strtoul(str, end, radix); }
			static long long          strtoi64(char const* str, char** end, int radix)      { return ::_strtoi64(str, end, radix); }
			static unsigned long long strtoui64(char const* str, char** end, int radix)     { return ::_strtoui64(str, end, radix); }
			static int                strnicmp(char const* lhs, char const* rhs, int count) { return ::_strnicmp(lhs, rhs, count); }
			static char const*        str(char const* str, wchar_t const*)                  { return str; }
		};
		template <> struct traits_base<wchar_t>
		{
			static double             strtod(wchar_t const* str, wchar_t** end)                   { return ::wcstod(str, end); }
			static long               strtol(wchar_t const* str, wchar_t** end, int radix)        { return ::wcstol(str, end, radix); }
			static long long          strtoi64(wchar_t const* str, wchar_t** end, int radix)      { return ::_wcstoi64(str, end, radix); }
			static unsigned long      strtoul(wchar_t const* str, wchar_t** end, int radix)       { return ::wcstoul(str, end, radix); }
			static unsigned long long strtoui64(wchar_t const* str, wchar_t** end, int radix)     { return ::_wcstoui64(str, end, radix); }
			static int                strnicmp(wchar_t const* lhs, wchar_t const* rhs, int count) { return ::_wcsnicmp(lhs, rhs, count); }
			static wchar_t const*     str(char const*, wchar_t const* str)                        { return str; }
		};

		// An integral or floating point value
		struct Val
		{
			union { unsigned long long m_ul; long long m_ll; double m_db; };  // The value
			bool m_fp; // True if 'db' should be used, false if 'll'

			double             db() const { return m_fp ? m_db : m_ll; }
			long long          ll() const { return m_fp ? static_cast<long long>(m_db) : m_ll; }
			unsigned long long ul() const { return m_fp ? static_cast<unsigned long long>(m_db) : static_cast<unsigned long long>(m_ll); }

			Val() :m_ll() ,m_fp() {}
			explicit Val(long long ll) :m_ll(ll) ,m_fp(false) {}
			explicit Val(double    db) :m_db(db) ,m_fp(true)  {}
			Val& operator = (float                v) { m_db = v; m_fp = true; return *this; }
			Val& operator = (double               v) { m_db = v; m_fp = true; return *this; }
			Val& operator = (unsigned long long   v) { m_ul = v; m_fp = false; return *this; }
			Val& operator = (unsigned long        v) { m_ul = v; m_fp = false; return *this; }
			Val& operator = (long long            v) { m_ll = v; m_fp = false; return *this; }
			Val& operator = (long                 v) { m_ll = v; m_fp = false; return *this; }
			Val& operator = (bool                 v) { m_ll = v; m_fp = false; return *this; }

			// Read a value (greedily) from 'expr'
			template <typename Char> bool read(Char const*& expr)
			{
				using traits = traits_base<Char>;

				// Read as a floating point number
				Char* endf; errno = 0; double db = traits::strtod(expr, &endf);

				// Read as a 32-bit int
				Char* endi; errno = 0; long long ll = traits::strtol(expr, &endi, 0);

				// If more chars are read as a float, then assume a float
				if (endi == expr && endf == expr) return false;
				if (endf > endi) { expr += (endf - expr); *this = db; return true; }

				// Otherwise assume an integral type
				// If an integral type, check for a type suffix
				bool usign = *endi == 'u' || *endi == 'U'; endi += usign;
				bool llong = *endi == 'l' || *endi == 'L'; endi += llong;
				llong &=     *endi == 'l' || *endi == 'L'; endi += llong;
				if (usign)
				{
					errno = 0;
					if (!llong)                    ll = traits::strtoul(expr, nullptr, 0);
					if ( llong || errno == ERANGE) ll = traits::strtoui64(expr, nullptr, 0);
				}
				else
				{
					if ( llong || errno == ERANGE) ll = traits::strtoi64(expr, nullptr, 0);
				}
				expr += (endi - expr);
				*this = ll;
				return true;
			}
		};
		inline Val  operator +  (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? Val(lhs.db() + rhs.db()) : Val(lhs.ll() + rhs.ll()); }
		inline Val  operator -  (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? Val(lhs.db() - rhs.db()) : Val(lhs.ll() - rhs.ll()); }
		inline Val  operator *  (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? Val(lhs.db() * rhs.db()) : Val(lhs.ll() * rhs.ll()); }
		inline Val  operator /  (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? Val(lhs.db() / rhs.db()) : Val(lhs.ll() / rhs.ll()); }
		inline bool operator == (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? lhs.db() == rhs.db() : lhs.ll() == rhs.ll(); }
		inline bool operator <  (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? lhs.db() <  rhs.db() : lhs.ll() <  rhs.ll(); }
		inline bool operator <= (Val const& lhs, Val const& rhs) { return (lhs.m_fp || rhs.m_fp) ? lhs.db() <= rhs.db() : lhs.ll() <= rhs.ll(); }

		// Extract a token from 'expr'
		// If the token is a value then 'expr' is advanced past the value
		// if it's an operator it isn't. This is so that operator precedense works
		// 'follows_value' should be true if the preceeding expression evaluates to a value
		template <typename Char> ETok Token(Char const*& expr, Val& val, bool follows_value)
		{
			using traits = traits_base<Char>;
			auto cmp = traits::strnicmp;
			#define str(s) traits::str(s, L##s)
			static std::locale s_locale(""); // A static instance of the locale, because this thing takes ages to construct

			// Skip any leading whitespace
			while (*expr && std::isspace(*expr, s_locale)) { ++expr; }

			// Look for an operator
			switch (std::tolower(*expr, s_locale))
			{
			default: break;
			// Convert Add/Sub to unary plus/minus by looking at the previous expression
			// If the previous expression evaluates to a value then Add/Sub are binary expressions
			case '+': return follows_value ? ETok::Add : ETok::UnaryAdd;
			case '-': return follows_value ? ETok::Sub : ETok::UnaryMinus;
			case '*': return ETok::Mul;
			case '/': return ETok::Div;
			case '%': return ETok::Mod;
			case '~': return ETok::Comp;
			case ',': return ETok::Comma;
			case '^': return ETok::BitXOR;
			case '(': return ETok::OpenParenthesis;
			case ')': return ETok::CloseParenthesis;
			case '?': return ETok::If;
			case ':': return ETok::Else;
			case '<':
				if      (cmp(expr, str("<<")    ,2) == 0) return ETok::LeftShift;
				else if (cmp(expr, str("<=")    ,2) == 0) return ETok::LogLTEql;
				else return ETok::LogLT;
			case '>':
				if      (cmp(expr, str(">>")    ,2) == 0) return ETok::RightShift;
				else if (cmp(expr, str(">=")    ,2) == 0) return ETok::LogGTEql;
				else return ETok::LogGT;
			case '|':
				if      (cmp(expr, str("||")    ,2) == 0) return ETok::LogOR;
				else return ETok::BitOR;
			case '&':
				if      (cmp(expr, str("&&")    ,2) == 0) return ETok::LogAND;
				else return ETok::BitAND;
			case '=':
				if      (cmp(expr, str("==")    ,2) == 0) return ETok::LogEql;
				else break;
			case '!':
				if      (cmp(expr, str("!=")    ,2) == 0) return ETok::LogNEql;
				else return ETok::Not;
			case 'a':
				if      (cmp(expr, str("abs")   ,3) == 0) return ETok::Abs;
				else if (cmp(expr, str("asin")  ,4) == 0) return ETok::ASin;
				else if (cmp(expr, str("acos")  ,4) == 0) return ETok::ACos;
				else if (cmp(expr, str("atan2") ,5) == 0) return ETok::ATan2;
				else if (cmp(expr, str("atan")  ,4) == 0) return ETok::ATan;
				else break;
			case 'c':
				if      (cmp(expr, str("clamp") ,5) == 0) return ETok::Clamp;
				else if (cmp(expr, str("ceil")  ,4) == 0) return ETok::Ceil;
				else if (cmp(expr, str("cosh")  ,4) == 0) return ETok::CosH;
				else if (cmp(expr, str("cos")   ,3) == 0) return ETok::Cos;
				else break;
			case 'd':
				if      (cmp(expr, str("deg")   ,3) == 0) return ETok::Deg;
				else break;
			case 'e':
				if      (cmp(expr, str("exp")   ,3) == 0) return ETok::Exp;
				else break;
			case 'f':
				if      (cmp(expr, str("floor") ,5) == 0) return ETok::Floor;
				else if (cmp(expr, str("fmod")  ,3) == 0) return ETok::Fmod;
				else if (cmp(expr, str("false") ,5) == 0) { expr += 5; val = 0.0; return ETok::Value; }
				else break;
			case 'l':
				if      (cmp(expr, str("log10") ,5) == 0) return ETok::Log10;
				else if (cmp(expr, str("log")   ,3) == 0) return ETok::Log;
				else if (cmp(expr, str("len2")  ,4) == 0) return ETok::Len2;
				else if (cmp(expr, str("len3")  ,4) == 0) return ETok::Len3;
				else if (cmp(expr, str("len4")  ,4) == 0) return ETok::Len4;
				else break;
			case 'm':
				if      (cmp(expr, str("min")   ,3) == 0) return ETok::Min;
				else if (cmp(expr, str("max")   ,3) == 0) return ETok::Max;
				else break;
			case 'p':
				if      (cmp(expr, str("pow")   ,3) == 0) return ETok::Pow;
				else if (cmp(expr, str("phi")   ,3) == 0) { expr += 3; val = 1.618033988749894848204586834; return ETok::Value; }
				else if (cmp(expr, str("pi")    ,2) == 0) { expr += 2; val = 3.1415926535897932384626433832795; return ETok::Value; }
				else break;
			case 'r':
				if      (cmp(expr, str("round") ,5) == 0) return ETok::Round;
				else if (cmp(expr, str("rad")   ,3) == 0) return ETok::Rad;
				else break;
			case 's':
				if      (cmp(expr, str("sinh")  ,4) == 0) return ETok::SinH;
				else if (cmp(expr, str("sin")   ,3) == 0) return ETok::Sin;
				else if (cmp(expr, str("sqrt")  ,4) == 0) return ETok::Sqrt;
				else if (cmp(expr, str("sqr")   ,3) == 0) return ETok::Sqr;
				else break;
			case 't':
				if      (cmp(expr, str("tanh")  ,4) == 0) return ETok::TanH;
				else if (cmp(expr, str("tan")   ,3) == 0) return ETok::Tan;
				else if (cmp(expr, str("tau")   ,3) == 0) { expr += 3; val = 6.283185307179586476925286766559; return ETok::Value; }
				else if (cmp(expr, str("true")  ,4) == 0) { expr += 4; val = 1.0; return ETok::Value; }
				else break;
			}
			#undef str

			// If it's not an operator, try extracting an operand
			return val.read(expr) ? ETok::Value : ETok::None;
		}

		// Evaluate an expression.
		// Called recursively for each operation within an expression.
		// 'parent_op' is used to determine precedence order.
		template <typename Char> bool Eval(Char const*& expr, Val* result, int rmax, int& ridx, ETok parent_op, bool l2r = true)
		{
			if (ridx >= rmax)
				throw std::exception("too many results");

			// Each time round the while loop should result in a value.
			// Operation tokens result in recursive calls.
			bool follows_value = false;
			while (*expr)
			{
				Val lhs, rhs;
				ETok tok = Token(expr, lhs, follows_value);
				follows_value = true;

				// If the next token has lower precedence than the parent operation
				// then return to allow the parent op to evaluate
				auto prec0 = Precedence(tok);
				auto prec1 = Precedence(parent_op);
				if (prec0 < prec1)
					return true;
				if (prec0 == prec1 && l2r)
					return true;
				
				int idx = 0;
				switch (tok)
				{
				default:
					throw std::exception("unknown expression token");
				case ETok::None:
					return *expr == 0;
				case ETok::Value:
					result[ridx] = lhs;
					break;
				case ETok::Add:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] + rhs;
					break;
				case ETok::Sub:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] - rhs;
					break;
				case ETok::Mul:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] * rhs;
					break;
				case ETok::Div:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] / rhs;
					break;
				case ETok::Mod:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() % rhs.ll();
					break;
				case ETok::UnaryAdd:
					if (!Eval(++expr, &rhs, 1, idx, tok, false)) return false;
					result[ridx] = rhs;
					break;
				case ETok::UnaryMinus:
					if (!Eval(++expr, &rhs, 1, idx, tok, false)) return false;
					if (rhs.m_fp) result[ridx] = -rhs.db(); // don't convert to ?: as the result will always be double
					else          result[ridx] = -rhs.ll(); // result[ridx].ll() will get promoted to double
					break;
				case ETok::Comp:
					if (!Eval(++expr, &rhs, 1, idx, tok, false)) return false;
					if (rhs.ll() < 0) result[ridx] = ~rhs.ll();
					else              result[ridx] = ~rhs.ul();
					break;
				case ETok::Not:
					if (!Eval(++expr, &rhs, 1, idx, tok, false)) return false;
					result[ridx] = !rhs.ll();
					break;
				case ETok::LogOR:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() || rhs.ll();
					break;
				case ETok::LogAND:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() && rhs.ll();
					break;
				case ETok::BitOR:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() | rhs.ul();
					break;
				case ETok::BitXOR:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() ^ rhs.ul();
					break;
				case ETok::BitAND:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() & rhs.ul();
					break;
				case ETok::LogEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] == rhs;
					break;
				case ETok::LogNEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = !(result[ridx] == rhs);
					break;
				case ETok::LogLT:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() < rhs.ll();
					break;
				case ETok::LogLTEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() <= rhs.ll();
					break;
				case ETok::LogGT:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() > rhs.ll();
					break;
				case ETok::LogGTEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() >= rhs.ll();
					break;
				case ETok::LeftShift:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() << rhs.ll();
					break;
				case ETok::RightShift:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() >> rhs.ll();
					break;
				case ETok::Fmod:
					{
						Val args[2];
						if (!Eval(expr += 4, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'mod'");
						result[ridx] = ::fmod(args[0].db(), args[1].db());
					}break;
				case ETok::Ceil:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::ceil(rhs.db());
					break;
				case ETok::Floor:
					if (!Eval(expr += 5, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::floor(rhs.db());
					break;
				case ETok::Round:
					if (!Eval(expr += 5, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::floor(rhs.db() + 0.5);
					break;
				case ETok::Min:
					{
						Val args[2];
						if (!Eval(expr += 3, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'min'");
						result[ridx] = args[0] < args[1] ? args[0] : args[1];
					}break;
				case ETok::Max:
					{
						Val args[2];
						if (!Eval(expr += 3, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'max'");
						result[ridx] = args[0] < args[1] ? args[1] : args[0];
					}break;
				case ETok::Clamp:
					{
						Val args[3];
						if (!Eval(expr += 5, args, 3, idx, tok)) return false;
						if (idx+1 != 3) throw std::exception("insufficient parameters for 'clamp'");
						result[ridx] = args[0] < args[1] ? args[1] : args[2] < args[0] ? args[2] : args[0];
					}break;
				case ETok::Abs:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::abs(rhs.m_fp ? rhs.db() : rhs.ll());
					break;
				case ETok::Sin:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::sin(rhs.db());
					break;
				case ETok::Cos:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::cos(rhs.db());
					break;
				case ETok::Tan:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::tan(rhs.db());
					break;
				case ETok::ASin:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::asin(rhs.db());
					break;
				case ETok::ACos:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::acos(rhs.db());
					break;
				case ETok::ATan:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::atan(rhs.db());
					break;
				case ETok::ATan2:
					{
						Val args[2];
						if (!Eval(expr += 5, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'atan2'");
						result[ridx] = ::atan2(args[0].db(), args[1].db());
					}break;
				case ETok::SinH:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::sinh(rhs.db());
					break;
				case ETok::CosH:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::cosh(rhs.db());
					break;
				case ETok::TanH:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::tanh(rhs.db());
					break;
				case ETok::Exp:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::exp(rhs.db());
					break;
				case ETok::Log:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::log(rhs.db());
					break;
				case ETok::Log10:
					if (!Eval(expr += 5, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::log10(rhs.db());
					break;
				case ETok::Pow:
					{
						Val args[2];
						if (!Eval(expr += 3, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'pow'");
						result[ridx] = ::pow(args[0].db(), args[1].db());
					}break;
				case ETok::Sqr:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = rhs * rhs;
					break;
				case ETok::Sqrt:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ::sqrt(rhs.db());
					break;
				case ETok::Len2:
					{
						Val args[2];
						if (!Eval(expr += 4, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'len2'");
						result[ridx] = ::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db());
					}break;
				case ETok::Len3:
					{
						Val args[3];
						if (!Eval(expr += 4, args, 3, idx, tok)) return false;
						if (idx+1 != 3) throw std::exception("insufficient parameters for 'len3'");
						result[ridx] = ::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db());
					}break;
				case ETok::Len4:
					{
						Val args[4];
						if (!Eval(expr += 4, args, 4, idx, tok)) return false;
						if (idx+1 != 4) throw std::exception("insufficient parameters for 'len4'");
						result[ridx] = ::sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db() + args[3].db()*args[3].db());
					}break;
				case ETok::Deg:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = rhs.db() * 5.729578e+1F;
					break;
				case ETok::Rad:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = rhs.db() * 1.745329e-2F;
					break;
				case ETok::Comma:
					if (ridx + 1 == rmax) throw std::exception("too many parameters");
					++expr;
					++ridx;
					break;
				case ETok::OpenParenthesis:
					if (!Eval(++expr, result, rmax, ridx, ETok::None)) return false;
					break;
				case ETok::CloseParenthesis:
					if (parent_op == ETok::None) ++expr;
					return true;
				case ETok::If:
					{
						Val vals[2]; int valc = 0;
						if (!Eval(++expr, vals, 2, valc, ETok::None)) return false;
						if (valc != 2) throw std::exception("incomplete if-else");
						result[ridx] = result[ridx].ll() != 0 ? vals[0] : vals[1];
					}break;
				case ETok::Else:
					if (!Eval(++expr, result, rmax, ++ridx, ETok::Else)) return false;
					++ridx;
					return true;
				}
			}
			return true;
		}
	}

	// Evaluate a floating point expression
	template <typename ResType, typename Char> inline ResType Evaluate(Char const* expr)
	{
		using namespace eval_impl;
		Val result; int ridx = 0;
		Eval(expr, &result, 1, ridx, ETok::None);
		return static_cast<ResType>(result.db());
	}

	// Evaluate an integral expression
	template <typename ResType, typename Char> inline ResType EvaluateI(Char const* expr)
	{
		using namespace eval_impl;
		Val result; int ridx = 0;
		Eval(expr, &result, 1, ridx, ETok::None);
		return static_cast<ResType>(result.ll());
	}

	// Helper overloads
	template <typename Char> inline bool Evaluate (Char const* expr, double&             out) { try {out = Evaluate <double             >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool Evaluate (Char const* expr, float&              out) { try {out = Evaluate <float              >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, unsigned long long& out) { try {out = EvaluateI<unsigned long long >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, unsigned long&      out) { try {out = EvaluateI<unsigned long      >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, unsigned int&       out) { try {out = EvaluateI<unsigned int       >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, long long&          out) { try {out = EvaluateI<long long          >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, long&               out) { try {out = EvaluateI<long               >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, int&                out) { try {out = EvaluateI<int                >(expr); return true;} catch (std::exception const&) {return false;} }
	template <typename Char> inline bool EvaluateI(Char const* expr, bool&               out) { try {out=!!EvaluateI<int                >(expr); return true;} catch (std::exception const&) {return false;} }
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr
{
	namespace unittests
	{
		template <typename ResType> bool Expr(char const* expr, ResType result);
		template <> bool Expr(char const* expr, double     result) { double     val; return pr::Evaluate (expr, val) && pr::FEql(val, result); }
		template <> bool Expr(char const* expr, float      result) { float      val; return pr::Evaluate (expr, val) && pr::FEql(val, result); }
		template <> bool Expr(char const* expr, pr::uint64 result) { pr::uint64 val; return pr::EvaluateI(expr, val) && val == result; }
		template <> bool Expr(char const* expr, pr::int64  result) { pr::int64  val; return pr::EvaluateI(expr, val) && val == result; }
		template <> bool Expr(char const* expr, pr::ulong  result) { pr::ulong  val; return pr::EvaluateI(expr, val) && val == result; }
		template <> bool Expr(char const* expr, long       result) { long       val; return pr::EvaluateI(expr, val) && val == result; }
		template <> bool Expr(char const* expr, pr::uint   result) { pr::uint   val; return pr::EvaluateI(expr, val) && val == result; }
		template <> bool Expr(char const* expr, int        result) { int        val; return pr::EvaluateI(expr, val) && val == result; }
		template <> bool Expr(char const* expr, bool       result) { bool       val; return pr::EvaluateI(expr, val) && val == result; }

		#define EXPR(exp) Expr(#exp, (exp))

		bool Val(char const* expr, double     result) { pr::eval_impl::Val val; return val.read(expr) && result == val.db(); }
		bool Val(char const* expr, long long  result) { pr::eval_impl::Val val; return val.read(expr) && result == val.ll(); }
		bool Val(char const* expr, int        result) { pr::eval_impl::Val val; return val.read(expr) && result == val.ll(); }
		bool Val(char const* expr, pr::uint   result) { pr::eval_impl::Val val; return val.read(expr) && result == val.ll(); }
		bool Val(char const* expr, pr::ulong  result) { pr::eval_impl::Val val; return val.read(expr) && result == val.ll(); }
		bool Val(char const* expr, pr::uint64 result) { pr::eval_impl::Val val; return val.read(expr) && result == (pr::uint64)val.ll(); }

		#define VAL(exp) Val(#exp, (exp))

		PRUnitTest(pr_common_expr_eval)
		{
			float const TAU = pr::maths::tau;
			float const PHI = pr::maths::phi;

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
			PR_CHECK(Expr("sin(1.0 + 2.0)"          ,::sin(1.0 + 2.0))  ,true);
			PR_CHECK(Expr("cos(TAU)"                ,::cos(TAU))        ,true);
			PR_CHECK(Expr("tan(PHI)"                ,::tan(PHI))        ,true);
			PR_CHECK(Expr("abs( 1.0)"               ,::abs( 1.0))       ,true);
			PR_CHECK(Expr("abs(-1.0)"               ,::abs(-1.0))       ,true);
			PR_CHECK(EXPR(11 % 3), true);
			PR_CHECK(Expr("fmod(11.3, 3.1)"         ,::fmod(11.3, 3.1)) ,true);
			PR_CHECK(EXPR(3.0*fmod(17.3, 2.1)), true);
			PR_CHECK(EXPR(1 << 10), true);
			PR_CHECK(EXPR(1024 >> 3), true);
			PR_CHECK(Expr("ceil(3.4)"                ,::ceil(3.4))                                    ,true);
			PR_CHECK(Expr("ceil(-3.4)"               ,::ceil(-3.4))                                   ,true);
			PR_CHECK(Expr("floor(3.4)"               ,::floor(3.4))                                   ,true);
			PR_CHECK(Expr("floor(-3.4)"              ,::floor(-3.4))                                  ,true);
			PR_CHECK(Expr("asin(-0.8)"               ,::asin(-0.8))                                   ,true);
			PR_CHECK(Expr("acos(0.2)"                ,::acos(0.2))                                    ,true);
			PR_CHECK(Expr("atan(2.3/12.9)"           ,::atan(2.3/12.9))                               ,true);
			PR_CHECK(Expr("atan2(2.3,-3.9)"          ,::atan2(2.3,-3.9))                              ,true);
			PR_CHECK(Expr("sinh(0.8)"                ,::sinh(0.8))                                    ,true);
			PR_CHECK(Expr("cosh(0.2)"                ,::cosh(0.2))                                    ,true);
			PR_CHECK(Expr("tanh(2.3)"                ,::tanh(2.3))                                    ,true);
			PR_CHECK(Expr("exp(2.3)"                 ,::exp(2.3))                                     ,true);
			PR_CHECK(Expr("log(209.3)"               ,::log(209.3))                                   ,true);
			PR_CHECK(Expr("log10(209.3)"             ,::log10(209.3))                                 ,true);
			PR_CHECK(Expr("pow(2.3, -1.3)"           ,::pow(2.3, -1.3))                               ,true);
			PR_CHECK(Expr("sqrt(2.3)"                ,::sqrt(2.3))                                    ,true);
			PR_CHECK(Expr("sqr(-2.3)"                ,pr::Sqr(-2.3))                                  ,true);
			PR_CHECK(Expr("len2(3,4)"                ,::sqrt(3.0*3.0 + 4.0*4.0))                      ,true);
			PR_CHECK(Expr("len3(3,4,5)"              ,::sqrt(3.0*3.0 + 4.0*4.0 + 5.0*5.0))            ,true);
			PR_CHECK(Expr("len4(3,4,5,6)"            ,::sqrt(3.0*3.0 + 4.0*4.0 + 5.0*5.0 + 6.0*6.0))  ,true);
			PR_CHECK(Expr("deg(-1.24)"               ,-1.24 * pr::maths::E60_by_tau)                  ,true);
			PR_CHECK(Expr("rad(241.32)"              ,241.32 * pr::maths::tau_by_360)                 ,true);
			PR_CHECK(Expr("round( 3.5)"              ,::floor(3.5 + 0.5))                             ,true);
			PR_CHECK(Expr("round(-3.5)"              ,::floor(-3.5 + 0.5))                            ,true);
			PR_CHECK(Expr("round( 3.2)"              ,::floor(3.2 + 0.5))                             ,true);
			PR_CHECK(Expr("round(-3.2)"              ,::floor(-3.2 + 0.5))                            ,true);
			PR_CHECK(Expr("min(-3.2, -3.4)"          ,pr::Min(-3.2, -3.4))                            ,true);
			PR_CHECK(Expr("max(-3.2, -3.4)"          ,pr::Max(-3.2, -3.4))                            ,true);
			PR_CHECK(Expr("clamp(10.0, -3.4, -3.2)"  ,pr::Clamp(10.0, -3.4, -3.2))                    ,true);
			PR_CHECK(Expr("sqr(sqrt(2.3)*-abs(4%2)/15.0-tan(TAU/-6))", pr::Sqr(::sqrt(2.3)*-::abs(4%2)/15.0-::tan(TAU/-6))), true);
			{
				long long v1 = 0, v0 = 123456789000000LL / 2;
				PR_CHECK(pr::EvaluateI("123456789000000 / 2", v1), true);
				PR_CHECK(v0 == v1, true);
			}
			PR_CHECK(Expr("1 != 2 ? 5 : 6", 5), true);
			PR_CHECK(Expr("1 == 2 ? 5 : 6", 6), true);
			PR_CHECK(Expr("true ? 5 : 6 + 1", 5), true);
			PR_CHECK(Expr("false ? 5 : 6 + 1", 7), true);
			PR_CHECK(Expr("sqr(-2) ? (1+2) : max(-2,-3)", 3), true);
			PR_CHECK(Expr("-+1", -1),  true);
			PR_CHECK(Expr("-++-1", 1),  true);
			PR_CHECK(Expr("!!true", 1),  true);
			PR_CHECK(Expr("-!!!false", -1),  true);
			PR_CHECK(Expr("10 - 3 - 2", 5), true);
		}
	}
}
#endif
