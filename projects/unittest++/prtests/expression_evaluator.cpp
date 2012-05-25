//*************************************************************
// Unit Test for PRString
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/maths/maths.h"
#include "pr/common/expr_eval.h"
#include <cerrno>
SUITE(ExpressionEvaluator)
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
	
#	define EXPR(exp) Expr(#exp, (exp))
	
	bool Val(char const* expr, double     result) { pr::impl::Val val; return val.read(expr) && result == val.db(); }
	bool Val(char const* expr, long long  result) { pr::impl::Val val; return val.read(expr) && result == val.ll(); }
	bool Val(char const* expr, int        result) { pr::impl::Val val; return val.read(expr) && result == val.ll(); }
	bool Val(char const* expr, pr::uint   result) { pr::impl::Val val; return val.read(expr) && result == val.ll(); }
	bool Val(char const* expr, pr::ulong  result) { pr::impl::Val val; return val.read(expr) && result == val.ll(); }
	bool Val(char const* expr, pr::uint64 result) { pr::impl::Val val; return val.read(expr) && result == (pr::uint64)val.ll(); }
	
#	define VAL(exp) Val(#exp, (exp))
	
	TEST(Expr)
	{
		float const TAU = pr::maths::tau;
		float const PHI = pr::maths::phi;
		
		CHECK(VAL(1));
		CHECK(VAL(1.0));
		CHECK(VAL(-1));
		CHECK(VAL(-1.0));
		CHECK(VAL(10U));
		CHECK(VAL(100L));
		CHECK(VAL(-100L));
		CHECK(VAL(0x1000UL));
		CHECK(VAL(0x7FFFFFFF));
		CHECK(VAL(0x80000000));
		CHECK(VAL(0xFFFFFFFF));
		CHECK(VAL(0xFFFFFFFFU));
		CHECK(VAL(0xFFFFFFFFULL));
		CHECK(VAL(0x7FFFFFFFFFFFFFFFLL));
		CHECK(VAL(0xFFFFFFFFFFFFFFFFULL));
		
		CHECK(EXPR(1.0));
		CHECK(EXPR(+1.0));
		CHECK(EXPR(-1.0));
		CHECK(EXPR(8.0 * -1.0));
		CHECK(EXPR(1.0 + +2.0));
		CHECK(EXPR(1.0 - 2.0));
		CHECK(EXPR(1.0 * +2.0));
		CHECK(EXPR(1 / 2));
		CHECK(EXPR(1.0 / 2.0));
		CHECK(EXPR(1.0 / 2.0 + 3.0));
		CHECK(EXPR(1.0 / 2.0 * 3.0));
		CHECK(EXPR((1 || 0) && 2));
		CHECK(EXPR(((13 ^ 7) | 6) & 14));
		CHECK(EXPR((8 < 9) + (3 <= 3) + (8 > 9) + (2 >= 2) + (1 != 2) + (2 == 2)));
		CHECK(EXPR(1.0 + 2.0 * 3.0 - 4.0));
		CHECK(EXPR(2.0 * 3.0 + 1.0 - 4.0));
		CHECK(EXPR(1.0 - 4.0 + 2.0 * 3.0));
		CHECK(EXPR((1.0 + 2.0) * 3.0 - 4.0));
		CHECK(EXPR(1.0 + 2.0 * -(3.0 - 4.0)));
		CHECK(EXPR(1.0 + (2.0 * (3.0 - 4.0))));
		CHECK(EXPR((1.0 + 2.0) * (3.0 - 4.0)));
		CHECK(EXPR(~37 & ~0));
		CHECK(EXPR(!37 | !0));
		CHECK(EXPR(~(0xFFFFFFFF >> 2)));
		CHECK(EXPR(~(4294967295 >> 2)));
		CHECK(EXPR(~(0xFFFFFFFFLL >> 2)));
		CHECK(EXPR(~(4294967295LL >> 2)));
		CHECK(EXPR(sin(1.0 + 2.0)));
		CHECK(EXPR(cos(TAU)));
		CHECK(EXPR(tan(PHI)));
		CHECK(EXPR(abs( 1.0)));
		CHECK(EXPR(abs(-1.0)));
		CHECK(EXPR(11 % 3));
		CHECK(EXPR(fmod(11.3, 3.1)));
		CHECK(EXPR(3.0*fmod(17.3, 2.1)));
		CHECK(EXPR(1 << 10));
		CHECK(EXPR(1024 >> 3));
		CHECK(EXPR(ceil(3.4)));
		CHECK(EXPR(ceil(-3.4)));
		CHECK(EXPR(floor(3.4)));
		CHECK(EXPR(floor(-3.4)));
		CHECK(EXPR(asin(-0.8)));
		CHECK(EXPR(acos(0.2)));
		CHECK(EXPR(atan(2.3/12.9)));
		CHECK(EXPR(atan2(2.3,-3.9)));
		CHECK(EXPR(sinh(0.8)));
		CHECK(EXPR(cosh(0.2)));
		CHECK(EXPR(tanh(2.3)));
		CHECK(EXPR(exp(2.3)));
		CHECK(EXPR(log(209.3)));
		CHECK(EXPR(log10(209.3)));
		CHECK(EXPR(pow(2.3, -1.3)));
		CHECK(EXPR(sqrt(2.3)));
		CHECK(Expr("sqr(-2.3)", pr::Sqr(-2.3)));
		CHECK(Expr("len2(3,4)", sqrt(3.0*3.0 + 4.0*4.0)));
		CHECK(Expr("len3(3,4,5)", sqrt(3.0*3.0 + 4.0*4.0 + 5.0*5.0)));
		CHECK(Expr("len4(3,4,5,6)", sqrt(3.0*3.0 + 4.0*4.0 + 5.0*5.0 + 6.0*6.0)));
		CHECK(Expr("deg(-1.24)", -1.24 * pr::maths::E60_by_tau));
		CHECK(Expr("rad(241.32)", 241.32 * pr::maths::tau_by_360));
		CHECK(Expr("round( 3.5)", floor(3.5 + 0.5)));
		CHECK(Expr("round(-3.5)", floor(-3.5 + 0.5)));
		CHECK(Expr("round( 3.2)", floor(3.2 + 0.5)));
		CHECK(Expr("round(-3.2)", floor(-3.2 + 0.5)));
		CHECK(Expr("min(-3.2, -3.4)", pr::Min(-3.2, -3.4)));
		CHECK(Expr("max(-3.2, -3.4)", pr::Max(-3.2, -3.4)));
		CHECK(Expr("clamp(10.0, -3.4, -3.2)", pr::Clamp(10.0, -3.4, -3.2)));
		
		{
			using namespace pr;
			CHECK(EXPR(Sqr(sqrt(2.3)*-abs(4%2)/15.0-tan(TAU/-6))));
		}
		{
			long long v1 = 0, v0 = 123456789000000LL / 2;
			CHECK(pr::EvaluateI("123456789000000 / 2", v1));
			CHECK_EQUAL(v0, v1);
		}
	}
}
