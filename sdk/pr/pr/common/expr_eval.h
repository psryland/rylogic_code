//*************************************************************
// Expression Evaluator
//  Copyright © Rylogic Ltd 2008
//*************************************************************
	
#ifndef PR_EXPR_EVAL_H
#define PR_EXPR_EVAL_H
	
#include <string.h>
#include <math.h>
#include <exception>
#include <errno.h>
	
#ifndef PR_ASSERT
#   define PR_ASSERT_STR_DEFINED
#   define PR_ASSERT(grp, exp, str)
#endif
	
namespace pr
{
	namespace impl
	{
		// These must be in precedence order (last value is highest precedence)
		enum ETok
		{
			ETok_Unknown,
			ETok_Value,
			ETok_Comma,
			ETok_LogOR,
			ETok_LogAND,
			ETok_BitOR,
			ETok_BitXOR,
			ETok_BitAND,
			ETok_LogEql,
			ETok_LogNEql,
			ETok_LogLT,
			ETok_LogLTEql,
			ETok_LogGT,
			ETok_LogGTEql,
			ETok_LeftShift,
			ETok_RightShift,
			ETok_Add,
			ETok_Sub,
			ETok_Mul,
			ETok_Div,
			ETok_Fmod,
			ETok_Mod,
			ETok_Comp,
			ETok_Not,
			ETok_Abs,
			ETok_Ceil,
			ETok_Floor,
			ETok_Round,
			ETok_Min,
			ETok_Max,
			ETok_Clamp,
			ETok_Sin,
			ETok_Cos,
			ETok_Tan,
			ETok_ASin,
			ETok_ACos,
			ETok_ATan,
			ETok_ATan2,
			ETok_SinH,
			ETok_CosH,
			ETok_TanH,
			ETok_Exp,
			ETok_Log,
			ETok_Log10,
			ETok_Pow,
			ETok_Sqr,
			ETok_Sqrt,
			ETok_Len2,
			ETok_Len3,
			ETok_Len4,
			ETok_Deg,
			ETok_Rad,
			ETok_OpenParenthesis,
			ETok_CloseParenthesis,
			ETok_NumberOf
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
			bool read(char const*& expr)
			{
				// Read as a floating point number
				char* endf; errno = 0; double db = ::strtod(expr, &endf);
				
				// Read as a 32-bit int
				char* endi; errno = 0; long long ll = ::strtol(expr, &endi, 0);
				
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
					if (!llong)                    ll = ::strtoul(expr, 0, 0);
					if ( llong || errno == ERANGE) ll = ::_strtoui64(expr, 0, 0);
				}
				else
				{
					if ( llong || errno == ERANGE) ll = ::_strtoi64(expr, 0, 0);
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
		inline ETok Token(char const*& expr, Val& val)
		{
			// Skip any leading whitespace
			while (*expr && isspace(*expr)) { ++expr; }
			
			// Look for an operator
			switch (tolower(*expr))
			{
			default: break;
			case '+': return ETok_Add;
			case '-': return ETok_Sub;
			case '*': return ETok_Mul;
			case '/': return ETok_Div;
			case '%': return ETok_Mod;
			case '~': return ETok_Comp;
			case ',': return ETok_Comma;
			case '^': return ETok_BitXOR;
			case '(': return ETok_OpenParenthesis;
			case ')': return ETok_CloseParenthesis;
			case '<':
				if      (_strnicmp(expr, "<<"    ,2) == 0) return ETok_LeftShift;
				else if (_strnicmp(expr, "<="    ,2) == 0) return ETok_LogLTEql;
				else return ETok_LogLT;
			case '>':
				if      (_strnicmp(expr, ">>"    ,2) == 0) return ETok_RightShift;
				else if (_strnicmp(expr, ">="    ,2) == 0) return ETok_LogGTEql;
				else return ETok_LogGT;
			case '|':
				if      (_strnicmp(expr, "||"    ,2) == 0) return ETok_LogOR;
				else return ETok_BitOR;
			case '&':
				if      (_strnicmp(expr, "&&"    ,2) == 0) return ETok_LogAND;
				else return ETok_BitAND;
			case '=':
				if      (_strnicmp(expr, "=="    ,2) == 0) return ETok_LogEql;
				else break;
			case '!':
				if      (_strnicmp(expr, "!="    ,2) == 0) return ETok_LogNEql;
				else return ETok_Not;
			case 'a':
				if      (_strnicmp(expr, "abs"   ,3) == 0) return ETok_Abs;
				else if (_strnicmp(expr, "asin"  ,4) == 0) return ETok_ASin;
				else if (_strnicmp(expr, "acos"  ,4) == 0) return ETok_ACos;
				else if (_strnicmp(expr, "atan2" ,5) == 0) return ETok_ATan2;
				else if (_strnicmp(expr, "atan"  ,4) == 0) return ETok_ATan;
				else break;
			case 'c':
				if      (_strnicmp(expr, "clamp" ,5) == 0) return ETok_Clamp;
				else if (_strnicmp(expr, "ceil"  ,4) == 0) return ETok_Ceil;
				else if (_strnicmp(expr, "cosh"  ,4) == 0) return ETok_CosH;
				else if (_strnicmp(expr, "cos"   ,3) == 0) return ETok_Cos;
				else break;
			case 'd':
				if      (_strnicmp(expr, "deg"   ,3) == 0) return ETok_Deg;
				else break;
			case 'e':
				if      (_strnicmp(expr, "exp"   ,3) == 0) return ETok_Exp;
				else break;
			case 'f':
				if      (_strnicmp(expr, "floor" ,5) == 0) return ETok_Floor;
				else if (_strnicmp(expr, "fmod"  ,3) == 0) return ETok_Fmod;
				else break;
			case 'l':
				if      (_strnicmp(expr, "log10" ,5) == 0) return ETok_Log10;
				else if (_strnicmp(expr, "log"   ,3) == 0) return ETok_Log;
				else if (_strnicmp(expr, "len2"  ,4) == 0) return ETok_Len2;
				else if (_strnicmp(expr, "len3"  ,4) == 0) return ETok_Len3;
				else if (_strnicmp(expr, "len4"  ,4) == 0) return ETok_Len4;
				else break;
			case 'm':
				if      (_strnicmp(expr, "min"   ,3) == 0) return ETok_Min;
				else if (_strnicmp(expr, "max"   ,3) == 0) return ETok_Max;
				else break;
			case 'p':
				if      (_strnicmp(expr, "pow"   ,3) == 0) return ETok_Pow;
				else if (_strnicmp(expr, "phi"   ,3) == 0) { expr += 3; val = 1.618034e+0F; return ETok_Value; }
				else if (_strnicmp(expr, "pi"    ,2) == 0) { expr += 2; val = 3.141592e+0F; return ETok_Value; }
				else break;
			case 'r':
				if      (_strnicmp(expr, "round" ,5) == 0) return ETok_Round;
				else if (_strnicmp(expr, "rad"   ,3) == 0) return ETok_Rad;
				else break;
			case 's':
				if      (_strnicmp(expr, "sinh"  ,4) == 0) return ETok_SinH;
				else if (_strnicmp(expr, "sin"   ,3) == 0) return ETok_Sin;
				else if (_strnicmp(expr, "sqrt"  ,4) == 0) return ETok_Sqrt;
				else if (_strnicmp(expr, "sqr"   ,3) == 0) return ETok_Sqr;
				else break;
			case 't':
				if      (_strnicmp(expr, "tanh"  ,4) == 0) return ETok_TanH;
				else if (_strnicmp(expr, "tan"   ,3) == 0) return ETok_Tan;
				else if (_strnicmp(expr, "tau"   ,3) == 0) { expr += 3; val = 6.283185e+0F; return ETok_Value; }
				else break;
			}
			
			// If it's not an operator, try extracting an operand
			return  val.read(expr) ? ETok_Value : ETok_Unknown;
		}
		
		// Evaluate an expression.
		inline bool Eval(char const*& expr, Val* result, int rmax, int& ridx, ETok prev_op)
		{
			if (ridx >= rmax) throw std::exception("too many results");
			ETok prev_tok = prev_op;
			while (*expr)
			{
				int idx = 0, sign = 1; Val lhs, rhs;  //ArgType lhs, rhs = 0, sign = ArgType(1);
				ETok tok = Token(expr, lhs);
				if      (tok == ETok_Add && prev_tok != ETok_Value) { sign =  1; tok = Token(++expr, lhs); }
				else if (tok == ETok_Sub && prev_tok != ETok_Value) { sign = -1; tok = Token(++expr, lhs); }
				else if (tok > ETok_Value && tok < prev_op) return true;
				prev_tok = tok;
				
				switch (tok)
				{
				default:
					throw std::exception("unknown expression token");
				case ETok_Unknown:
					return *expr == 0;
				case ETok_Value:
					result[ridx] = lhs;
					break;
				case ETok_Add:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] + rhs;
					break;
				case ETok_Sub:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] - rhs;
					break;
				case ETok_Mul:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] * rhs;
					break;
				case ETok_Div:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] / rhs;
					break;
				case ETok_Mod:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() % rhs.ll();
					break;
				case ETok_Comp:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					if (rhs.ll() < 0) result[ridx] = ~rhs.ll();
					else              result[ridx] = ~rhs.ul();
					break;
				case ETok_Not:
					if (!Eval(++expr, &rhs, 1, idx, tok)) return false;
					result[ridx] = !rhs.ll();
					break;
				case ETok_LogOR:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() || rhs.ll();
					break;
				case ETok_LogAND:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() && rhs.ll();
					break;
				case ETok_BitOR:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() | rhs.ul();
					break;
				case ETok_BitXOR:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() ^ rhs.ul();
					break;
				case ETok_BitAND:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() & rhs.ul();
					break;
				case ETok_LogEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx] == rhs;
					break;
				case ETok_LogNEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = !(result[ridx] == rhs);
					break;
				case ETok_LogLT:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() < rhs.ll();
					break;
				case ETok_LogLTEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() <= rhs.ll();
					break;
				case ETok_LogGT:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() > rhs.ll();
					break;
				case ETok_LogGTEql:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ll() >= rhs.ll();
					break;
				case ETok_LeftShift:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() << rhs.ll();
					break;
				case ETok_RightShift:
					if (!Eval(expr += 2, &rhs, 1, idx, tok)) return false;
					result[ridx] = result[ridx].ul() >> rhs.ll();
					break;
				case ETok_Fmod:
					{
						Val args[2];
						if (!Eval(expr += 4, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'mod'");
						result[ridx] = fmod(args[0].db(), args[1].db());
					}break;
				case ETok_Ceil:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = ceil(rhs.db());
					break;
				case ETok_Floor:
					if (!Eval(expr += 5, &rhs, 1, idx, tok)) return false;
					result[ridx] = floor(rhs.db());
					break;
				case ETok_Round:
					if (!Eval(expr += 5, &rhs, 1, idx, tok)) return false;
					result[ridx] = floor(rhs.db() + 0.5);
					break;
				case ETok_Min:
					{
						Val args[2];
						if (!Eval(expr += 3, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'min'");
						result[ridx] = args[0] < args[1] ? args[0] : args[1];
					}break;
				case ETok_Max:
					{
						Val args[2];
						if (!Eval(expr += 3, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'max'");
						result[ridx] = args[0] < args[1] ? args[1] : args[0];
					}break;
				case ETok_Clamp:
					{
						Val args[3];
						if (!Eval(expr += 5, args, 3, idx, tok)) return false;
						if (idx+1 != 3) throw std::exception("insufficient parameters for 'clamp'");
						result[ridx] = args[0] < args[1] ? args[1] : args[2] < args[0] ? args[2] : args[0];
					}break;
				case ETok_Abs:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = abs(rhs.m_fp ? rhs.db() : rhs.ll());
					break;
				case ETok_Sin:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = sin(rhs.db());
					break;
				case ETok_Cos:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = cos(rhs.db());
					break;
				case ETok_Tan:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = tan(rhs.db());
					break;
				case ETok_ASin:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = asin(rhs.db());
					break;
				case ETok_ACos:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = acos(rhs.db());
					break;
				case ETok_ATan:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = atan(rhs.db());
					break;
				case ETok_ATan2:
					{
						Val args[2];
						if (!Eval(expr += 5, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'atan2'");
						result[ridx] = atan2(args[0].db(), args[1].db());
					}break;
				case ETok_SinH:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = sinh(rhs.db());
					break;
				case ETok_CosH:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = cosh(rhs.db());
					break;
				case ETok_TanH:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = tanh(rhs.db());
					break;
				case ETok_Exp:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = exp(rhs.db());
					break;
				case ETok_Log:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = log(rhs.db());
					break;
				case ETok_Log10:
					if (!Eval(expr += 5, &rhs, 1, idx, tok)) return false;
					result[ridx] = log10(rhs.db());
					break;
				case ETok_Pow:
					{
						Val args[2];
						if (!Eval(expr += 3, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'pow'");
						result[ridx] = pow(args[0].db(), args[1].db());
					}break;
				case ETok_Sqr:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = rhs * rhs;
					break;
				case ETok_Sqrt:
					if (!Eval(expr += 4, &rhs, 1, idx, tok)) return false;
					result[ridx] = sqrt(rhs.db());
					break;
				case ETok_Len2:
					{
						Val args[2];
						if (!Eval(expr += 4, args, 2, idx, tok)) return false;
						if (idx+1 != 2) throw std::exception("insufficient parameters for 'len2'");
						result[ridx] = sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db());
					}break;
				case ETok_Len3:
					{
						Val args[3];
						if (!Eval(expr += 4, args, 3, idx, tok)) return false;
						if (idx+1 != 3) throw std::exception("insufficient parameters for 'len3'");
						result[ridx] = sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db());
					}break;
				case ETok_Len4:
					{
						Val args[4];
						if (!Eval(expr += 4, args, 4, idx, tok)) return false;
						if (idx+1 != 4) throw std::exception("insufficient parameters for 'len4'");
						result[ridx] = sqrt(0.0 + args[0].db()*args[0].db() + args[1].db()*args[1].db() + args[2].db()*args[2].db() + args[3].db()*args[3].db());
					}break;
				case ETok_Deg:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = rhs.db() * 5.729578e+1F;
					break;
				case ETok_Rad:
					if (!Eval(expr += 3, &rhs, 1, idx, tok)) return false;
					result[ridx] = rhs.db() * 1.745329e-2F;
					break;
				case ETok_Comma:
					if (ridx + 1 == rmax) throw std::exception("too many parameters");
					++expr;
					++ridx;
					break;
				case ETok_OpenParenthesis:
					if (!Eval(++expr, result, rmax, ridx, ETok_Unknown)) return false;
					break;
				case ETok_CloseParenthesis:
					if (prev_op == ETok_Unknown) ++expr;
					return true;
				}
				if (result[ridx].m_fp) result[ridx] = sign * result[ridx].db();  // don't convert to ?: as the result will always be double
				else                   result[ridx] = sign * result[ridx].ll(); // result[ridx].ll() will get promoted to double
				prev_tok = ETok_Value;
			}
			return true;
		}
	}
	
	// Evaluate a floating point expression
	template <typename ResType> inline ResType Evaluate(char const* expr)
	{
		impl::Val result; int ridx = 0;
		impl::Eval(expr, &result, 1, ridx, impl::ETok_Unknown);
		return static_cast<ResType>(result.db());
	}
	
	// Evaluate an integral expression
	template <typename ResType> inline ResType EvaluateI(char const* expr)
	{
		impl::Val result; int ridx = 0;
		impl::Eval(expr, &result, 1, ridx, impl::ETok_Unknown);
		return static_cast<ResType>(result.ll());
	}
	
	// Helper overloads
	inline bool Evaluate (char const* expr, double&             out) { try {out = Evaluate <double             >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool Evaluate (char const* expr, float&              out) { try {out = Evaluate <float              >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, unsigned long long& out) { try {out = EvaluateI<unsigned long long >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, unsigned long&      out) { try {out = EvaluateI<unsigned long      >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, unsigned int&       out) { try {out = EvaluateI<unsigned int       >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, long long&          out) { try {out = EvaluateI<long long          >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, long&               out) { try {out = EvaluateI<long               >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, int&                out) { try {out = EvaluateI<int                >(expr); return true;} catch (std::exception const&) {return false;} }
	inline bool EvaluateI(char const* expr, bool&               out) { try {out=!!EvaluateI<int                >(expr); return true;} catch (std::exception const&) {return false;} }
}
	
#ifdef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT_STR_DEFINED
#   undef PR_ASSERT
#endif
	
#endif
