using System;
using System.Linq.Expressions;

namespace pr.util
{
	/// <summary>Operator generator</summary>
	public static class Operators<T>
	{
		// Notes:
		//  This class does not support integer types smaller than 'int' because the function
		//  signatures for 'Plus', 'Neg' are not the same as for types >= 'int' (i.e. +(byte)1 returns an int)
		static Operators()
		{
			#region Plus
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var body = Expression.UnaryPlus(paramA);
				m_plus = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
			}
			#endregion
			#region Neg
			{
				// Unsigned types don't define unary minus. Following the MS convention of -a == 2^N - a, where N is the number of bits
				if (typeof(T) == typeof(uint))
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var body = Expression.Subtract(Expression.Constant(0U), paramA);
					m_neg = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
				}
				else if (typeof(T) == typeof(ulong))
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var body = Expression.Subtract(Expression.Constant(0UL), paramA);
					m_neg = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var body = Expression.Negate(paramA);
					m_neg = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
				}
			}
			#endregion
			#region Add
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(T), "b");
				var body = Expression.Add(paramA, paramB);
				m_add = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Sub
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(T), "b");
				var body = Expression.Subtract(paramA, paramB);
				m_sub = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Multiply
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(T), "b");
				var body = Expression.Multiply(paramA, paramB);
				m_mul = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Divide
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(T), "b");
				var body = Expression.Divide(paramA, paramB);
				m_div = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Equal
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(T), "b");
				var body = Expression.Equal(paramA, paramB);
				m_eql = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Less Than
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(T), "b");
				var body = Expression.LessThan(paramA, paramB);
				m_less = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
			}
			#endregion
		}

		/// <summary>+a</summary>
		public static T Plus(T a) { return m_plus(a); }
		private static Func<T,T> m_plus;

		/// <summary>-a</summary>
		public static T Neg(T a) { return m_neg(a); }
		private static Func<T,T> m_neg;

		/// <summary>a + b</summary>
		public static T Add(T a, T b) { return m_add(a,b); }
		private static Func<T,T,T> m_add;

		/// <summary>a - b</summary>
		public static T Sub(T a, T b) { return m_sub(a,b); }
		private static Func<T,T,T> m_sub;

		/// <summary>a * b</summary>
		public static T Mul(T a, T b) { return m_mul(a,b); }
		private static Func<T,T,T> m_mul;

		/// <summary>a * b</summary>
		public static T Div(T a, T b) { return m_div(a,b); }
		private static Func<T,T,T> m_div;

		/// <summary>a == b</summary>
		public static bool Eql(T a, T b) { return m_eql(a,b); }
		private static Func<T,T,bool> m_eql;

		/// <summary>a != b</summary>
		public static bool NEql(T a, T b) { return !Eql(a,b); }

		/// <summary>a < b</summary>
		public static bool Less(T a, T b) { return m_less(a,b); }
		private static Func<T,T,bool> m_less;

		/// <summary>a &lt;= b</summary>
		public static bool LessEql(T a, T b) { return !Less(b,a); }

		/// <summary>a > b</summary>
		public static bool Greater(T a, T b) { return Less(b,a); }

		/// <summary>a >= b</summary>
		public static bool GreaterEql(T a, T b) { return !Less(a,b); }
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using util;

	[TestFixture] public class TestOperators
	{
		[Test] public void DefaultUse()
		{
			// Force instantiation for all supported basic types (i.e. not < 'int')
			Operators<int    >.Eql(123, 124);
			Operators<uint   >.Eql(123, 124);
			Operators<long   >.Eql(123, 124);
			Operators<ulong  >.Eql(123, 124);
			Operators<float  >.Eql(123, 124);
			Operators<double >.Eql(123, 124);
			Operators<decimal>.Eql(123, 124);

			Assert.AreEqual(Operators<short  >.Plus(412)       , (short)+412 );
			Assert.AreEqual(Operators<long   >.Neg(512)        , -512L       );
			Assert.AreEqual(Operators<double >.Add(0.1, 0.3)   , 0.1 + 0.3   );
			Assert.AreEqual(Operators<decimal>.Sub(0.5m, 0.8m) , 0.5m - 0.8m );
			Assert.AreEqual(Operators<int    >.Mul(3, 7)       , 3 * 7       );
			Assert.AreEqual(Operators<float  >.Div(1.2f, 3.4f) , 1.2f / 3.4f );

			Assert.AreEqual(Operators<float  >.Eql(1.23f, 1.24f)        , 1.23f == 1.24f );
			Assert.AreEqual(Operators<float  >.Eql(1.23f, 1.23f)        , 1.23f == 1.23f );
			Assert.AreEqual(Operators<float  >.NEql(1.23f, 1.24f)       , 1.23f != 1.24f );
			Assert.AreEqual(Operators<float  >.NEql(1.23f, 1.23f)       , 1.23f != 1.23f );
			Assert.AreEqual(Operators<float  >.Less(1.23f, 1.24f)       , 1.23f <  1.24f );
			Assert.AreEqual(Operators<float  >.Less(1.23f, 1.23f)       , 1.23f <  1.23f );
			Assert.AreEqual(Operators<float  >.Less(1.23f, 1.22f)       , 1.23f <  1.22f );
			Assert.AreEqual(Operators<float  >.LessEql(1.23f, 1.24f)    , 1.23f <= 1.24f );
			Assert.AreEqual(Operators<float  >.LessEql(1.23f, 1.23f)    , 1.23f <= 1.23f );
			Assert.AreEqual(Operators<float  >.LessEql(1.23f, 1.22f)    , 1.23f <= 1.22f );
			Assert.AreEqual(Operators<float  >.Greater(1.23f, 1.24f)    , 1.23f >  1.24f );
			Assert.AreEqual(Operators<float  >.Greater(1.23f, 1.23f)    , 1.23f >  1.23f );
			Assert.AreEqual(Operators<float  >.Greater(1.23f, 1.22f)    , 1.23f >  1.22f );
			Assert.AreEqual(Operators<float  >.GreaterEql(1.23f, 1.24f) , 1.23f >= 1.24f );
			Assert.AreEqual(Operators<float  >.GreaterEql(1.23f, 1.23f) , 1.23f >= 1.23f );
			Assert.AreEqual(Operators<float  >.GreaterEql(1.23f, 1.22f) , 1.23f >= 1.22f );
		}
	}
}
#endif