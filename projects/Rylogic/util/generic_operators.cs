using System;
using System.Linq.Expressions;
using System.Reflection;

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
			#region MaxValue
			{
				var mi = typeof(T).GetProperty("MaxValue", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy)?.GetGetMethod();
				var fi = typeof(T).GetField("MaxValue", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy);
				if (mi != null)
					m_max_value = () => (T)mi.Invoke(null, null);
				else if (fi != null)
					m_max_value = () => (T)fi.GetValue(null);
				else
					m_max_value = () => { throw new Exception($"Type {typeof(T).Name} has no static property or field named 'MaxValue'"); };
			}
			#endregion
			#region MinValue
			{
				var mi = typeof(T).GetProperty("MinValue", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy)?.GetGetMethod();
				var fi = typeof(T).GetField("MinValue", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy);
				if (mi != null)
					m_min_value = () => (T)mi.Invoke(null, null);
				else if (fi != null)
					m_min_value = () => (T)fi.GetValue(null);
				else
					m_min_value = () => { throw new Exception($"Type {typeof(T).Name} has no static property or field named 'MinValue'"); };
			}
			#endregion
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
			#region ToString
			{
				var mi1 = typeof(T).GetMethod("ToString", new[]{typeof(IFormatProvider)});
				var mi2 = typeof(T).GetMethod("ToString", new[]{typeof(string)});
				var mi3 = typeof(T).GetMethod("ToString", new[]{typeof(string), typeof(IFormatProvider)});
				if (mi1 != null) m_tostring1 = (a,fp) => (string)mi1.Invoke(a, new object[]{ fp });
				else             m_tostring1 = (a,fp) => { throw new Exception($"Type {typeof(T).Name} has no ToString(IFormatProvider) overload"); };
				if (mi2 != null) m_tostring2 = (a,fmt) => (string)mi2.Invoke(a, new object[]{ fmt });
				else             m_tostring2 = (a,fmt) => { throw new Exception($"Type {typeof(T).Name} has no ToString(string) overload"); };
				if (mi3 != null) m_tostring3 = (a,fmt,fp) => (string)mi3.Invoke(a, new object[]{ fmt, fp });
				else             m_tostring3 = (a,fmt,fp) => { throw new Exception($"Type {typeof(T).Name} has no ToString(string, IFormatProvider) overload"); };
			}
			#endregion
		}

		/// <summary>Return the maximum value</summary>
		public static T MaxValue { get { return m_max_value(); } }
		private static Func<T> m_max_value;

		/// <summary>Return the maximum value</summary>
		public static T MinValue { get { return m_min_value(); } }
		private static Func<T> m_min_value;

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

		/// <summary>ToString with formatting</summary>
		public static string ToString(T a)                                 { return a.ToString(); }
		public static string ToString(T a, IFormatProvider fp)             { return m_tostring1(a, fp); }
		public static string ToString(T a, string fmt)                     { return m_tostring2(a, fmt); }
		public static string ToString(T a, string fmt, IFormatProvider fp) { return m_tostring3(a, fmt, fp); }
		private static Func<T,IFormatProvider,string>        m_tostring1;
		private static Func<T,string,string>                 m_tostring2;
		private static Func<T,string,IFormatProvider,string> m_tostring3;
	}
	public static class Operators<T,U>
	{
		static Operators()
		{
			#region Multiply
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(U), "b");
				var body = Expression.Multiply(paramA, Expression.ConvertChecked(paramB, typeof(T)));
				m_mul = Expression.Lambda<Func<T, U, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Divide
			{
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(U), "b");
				var body = Expression.Divide(paramA, Expression.ConvertChecked(paramB, typeof(T)));
				m_div = Expression.Lambda<Func<T, U, T>>(body, paramA, paramB).Compile();
			}
			#endregion
		}

		/// <summary>a * b</summary>
		public static T Mul(T a, U b) { return m_mul(a,b); }
		private static Func<T,U,T> m_mul;

		/// <summary>a / b</summary>
		public static T Div(T a, U b) { return m_div(a,b); }
		private static Func<T,U,T> m_div;
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

			Assert.AreEqual(Operators<decimal>.MaxValue, decimal.MaxValue);
			Assert.AreEqual(Operators<decimal>.MinValue, decimal.MinValue);

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

			Assert.AreEqual(Operators<float,int>.Mul(1.23f, 45), 1.23f * 45);
			Assert.AreEqual(Operators<float,int>.Div(1.23f, 45), 1.23f / 45);

			Assert.AreEqual(Operators<float>.ToString(1.23f, "C") , 1.23f.ToString("C"));
		}
	}
}
#endif