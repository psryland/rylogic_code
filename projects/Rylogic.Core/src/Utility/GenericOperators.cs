using System;
using System.Linq.Expressions;
using System.Reflection;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Utility
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
					m_max_value = () => (T)mi.Invoke(null, null)!;
				else if (fi != null)
					m_max_value = () => (T)fi.GetValue(null)!;
				else
					m_max_value = () => { throw new Exception($"Type {typeof(T).Name} has no static property or field named 'MaxValue'"); };
			}
			#endregion
			#region MinValue
			{
				var mi = typeof(T).GetProperty("MinValue", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy)?.GetGetMethod();
				var fi = typeof(T).GetField("MinValue", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy);
				if (mi != null)
					m_min_value = () => (T)mi.Invoke(null, null)!;
				else if (fi != null)
					m_min_value = () => (T)fi.GetValue(null)!;
				else
					m_min_value = () => { throw new Exception($"Type {typeof(T).Name} has no static property or field named 'MinValue'"); };
			}
			#endregion
			#region Plus
			{
				if (typeof(T).IsEnum)
				{
					m_plus = x => { throw new Exception($"Type {typeof(T).Name} does not define the Unary Plus operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var body = Expression.UnaryPlus(paramA);
					m_plus = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
				}
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
				else if (typeof(T).IsEnum)
				{
					m_neg = x => { throw new Exception($"Type {typeof(T).Name} does not define the Unary Minus operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var body = Expression.Negate(paramA);
					m_neg = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
				}
			}
			#endregion
			#region Ones complement
			{
				if (typeof(T).IsEnum)
				{
					if (typeof(T).HasAttribute<FlagsAttribute>())
					{
						var paramA = Expression.Parameter(typeof(T), "a");
						var cast0 = Expression.Convert(paramA, Enum.GetUnderlyingType(typeof(T)));
						var comp = Expression.OnesComplement(cast0);
						var body = Expression.Convert(comp, typeof(T));
						m_ones_comp = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
					}
					else
					{
						m_ones_comp = x => { throw new Exception($"Type {typeof(T).Name} does not define the Ones Complement operator"); };
					}
				}
				else if (typeof(T).IsPrimitive && typeof(T) != typeof(float) && typeof(T) != typeof(double) && typeof(T) != typeof(decimal))
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var body = Expression.OnesComplement(paramA);
					m_ones_comp = Expression.Lambda<Func<T, T>>(body, paramA).Compile();
				}
				else
				{
					m_ones_comp = x => { throw new Exception($"Type {typeof(T).Name} does not define the Ones Complement operator"); };
				}
			}
			#endregion
			#region Add
			{
				if (typeof(T).IsEnum)
				{
					m_add = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Add operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.Add(paramA, paramB);
					m_add = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
				}
			}
			#endregion
			#region Sub
			{
				if (typeof(T).IsEnum)
				{
					m_sub = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Subtraction operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.Subtract(paramA, paramB);
					m_sub = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
				}
			}
			#endregion
			#region Multiply
			{
				if (typeof(T).IsEnum)
				{
					m_mul = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Multiplication operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.Multiply(paramA, paramB);
					m_mul = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
				}
			}
			#endregion
			#region Divide
			{
				if (typeof(T).IsEnum ||
					Math_.IsVecMatType(typeof(T)))
				{
					m_div = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Division operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.Divide(paramA, paramB);
					m_div = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
				}
			}
			#endregion
			#region Bitwise OR
			{
				if (typeof(T).IsEnum)
				{
					if (typeof(T).HasAttribute<FlagsAttribute>())
					{
						var paramA   = Expression.Parameter(typeof(T), "a");
						var paramB   = Expression.Parameter(typeof(T), "a");
						var castA0   = Expression.Convert(paramA, Enum.GetUnderlyingType(typeof(T)));
						var castB0   = Expression.Convert(paramB, Enum.GetUnderlyingType(typeof(T)));
						var or       = Expression.Or(castA0, castB0);
						var body     = Expression.Convert(or, typeof(T));
						m_bitwise_or = Expression.Lambda<Func<T,T,T>>(body, paramA, paramB).Compile();
					}
					else
					{
						m_bitwise_or = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Bitwise OR operator"); };
					}
				}
				else if (typeof(T).IsPrimitive && typeof(T) != typeof(float) && typeof(T) != typeof(double) && typeof(T) != typeof(decimal))
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.Or(paramA, paramB);
					m_bitwise_or = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
				}
				else
				{
					m_bitwise_or = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Bitwise OR operator"); };
				}
			}
			#endregion
			#region Bitwise AND
			{
				if (typeof(T).IsEnum)
				{
					if (typeof(T).HasAttribute<FlagsAttribute>())
					{
						var paramA    = Expression.Parameter(typeof(T), "a");
						var paramB    = Expression.Parameter(typeof(T), "a");
						var castA0    = Expression.Convert(paramA, Enum.GetUnderlyingType(typeof(T)));
						var castB0    = Expression.Convert(paramB, Enum.GetUnderlyingType(typeof(T)));
						var and       = Expression.And(castA0, castB0);
						var body      = Expression.Convert(and, typeof(T));
						m_bitwise_and = Expression.Lambda<Func<T,T,T>>(body, paramA, paramB).Compile();
					}
					else
					{
						m_bitwise_and = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Bitwise AND operator"); };
					}
				}
				else if (typeof(T).IsPrimitive && typeof(T) != typeof(float) && typeof(T) != typeof(double) && typeof(T) != typeof(decimal))
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.And(paramA, paramB);
					m_bitwise_and = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
				}
				else
				{
					m_bitwise_and = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not define the Bitwise AND operator"); };
				}
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
				if (typeof(T).IsEnum)
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var castA0 = Expression.Convert(paramA, Enum.GetUnderlyingType(typeof(T)));
					var castB0 = Expression.Convert(paramB, Enum.GetUnderlyingType(typeof(T)));
					var body = Expression.LessThan(castA0, castB0);
					m_less = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();

				}
				else if (Math_.IsVecMatType(typeof(T)))
				{
					m_less = (a, b) => { throw new Exception($"Type {typeof(T).Name} does not define the LessThan operator"); };
				}
				else
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(T), "b");
					var body = Expression.LessThan(paramA, paramB);
					m_less = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
				}
			}
			#endregion
			#region ToString
			{
				var mi1 = typeof(T).GetMethod("ToString", new[]{typeof(IFormatProvider)});
				var mi2 = typeof(T).GetMethod("ToString", new[]{typeof(string)});
				var mi3 = typeof(T).GetMethod("ToString", new[]{typeof(string), typeof(IFormatProvider)});
				if (mi1 != null) m_tostring1 = (a,fp) => (string?)mi1.Invoke(a, new object[]{ fp });
				else             m_tostring1 = (a,fp) => { throw new Exception($"Type {typeof(T).Name} has no ToString(IFormatProvider) overload"); };
				if (mi2 != null) m_tostring2 = (a,fmt) => (string?)mi2.Invoke(a, new object[]{ fmt });
				else             m_tostring2 = (a,fmt) => { throw new Exception($"Type {typeof(T).Name} has no ToString(string) overload"); };
				if (mi3 != null) m_tostring3 = (a,fmt,fp) => (string?)mi3.Invoke(a, new object[]{ fmt, fp });
				else             m_tostring3 = (a,fmt,fp) => { throw new Exception($"Type {typeof(T).Name} has no ToString(string, IFormatProvider) overload"); };
			}
			#endregion
			#region Parse
			{
				var mi = typeof(T).GetMethod("Parse", BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy, null, new Type[]{ typeof(string) }, null);
				if (mi != null)
					m_parse = s => (T)mi.Invoke(null, new object[]{ s })!;
				else
					m_parse = s => { throw new Exception($"Type {typeof(T).Name} has no static method named 'Parse'"); };
			}
			#endregion
		}

		/// <summary>Return the maximum value</summary>
		public static T MaxValue { get { return m_max_value(); } }
		private static Func<T> m_max_value;

		/// <summary>Return the maximum value</summary>
		public static T MinValue { get { return m_min_value(); } }
		private static Func<T> m_min_value;

		/// <summary>Cast from type U to type T</summary>
		public static T Cast<U>(U a) { return Operators<T, U>.Cast(a); }

		/// <summary>+a</summary>
		public static T Plus(T a) { return m_plus(a); }
		private static Func<T,T> m_plus;

		/// <summary>-a</summary>
		public static T Neg(T a) { return m_neg(a); }
		private static Func<T,T> m_neg;

		/// <summary>~a</summary>
		public static T OnesComp(T a) { return m_ones_comp(a); }
		private static Func<T,T> m_ones_comp;

		/// <summary>a + b</summary>
		public static T Add(T a, T b) { return m_add(a,b); }
		private static Func<T,T,T> m_add;

		/// <summary>a - b</summary>
		public static T Sub(T a, T b) { return m_sub(a,b); }
		private static Func<T,T,T> m_sub;

		/// <summary>a * b</summary>
		public static T Mul(T a, T b) { return m_mul(a,b); }
		private static Func<T,T,T> m_mul;

		/// <summary>a / b</summary>
		public static T Div(T a, T b) { return m_div(a,b); }
		private static Func<T,T,T> m_div;

		/// <summary>a | b</summary>
		public static T BitwiseOR(T a, T b) { return m_bitwise_or(a,b); }
		private static Func<T,T,T> m_bitwise_or;

		/// <summary>a & b</summary>
		public static T BitwiseAND(T a, T b) { return m_bitwise_and(a,b); }
		private static Func<T,T,T> m_bitwise_and;

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
		public static string ToString(T a)                                 { return a?.ToString() ?? string.Empty; }
		public static string ToString(T a, IFormatProvider fp)             { return m_tostring1(a, fp) ?? string.Empty; }
		public static string ToString(T a, string fmt)                     { return m_tostring2(a, fmt) ?? string.Empty; }
		public static string ToString(T a, string fmt, IFormatProvider fp) { return m_tostring3(a, fmt, fp) ?? string.Empty; }
		private static Func<T,IFormatProvider,string?>        m_tostring1;
		private static Func<T,string,string?>                 m_tostring2;
		private static Func<T,string,IFormatProvider,string?> m_tostring3;

		/// <summary>Parse</summary>
		public static bool TryParse(string str, out T val)
		{
			try   { val = Parse(str); return true; }
			catch { val = default!; return false; }
		}
		public static T Parse(string str) { return m_parse(str); }
		private static Func<string, T> m_parse;
	}
	public static class Operators<T,U>
	{
		static Operators()
		{
			#region Cast
			{
				try
				{
					var paramA = Expression.Parameter(typeof(U), "a");
					var body = Expression.Convert(paramA, typeof(T));
					m_cast = Expression.Lambda<Func<U, T>>(body, paramA).Compile();
				}
				catch
				{
					m_cast = x => throw new Exception($"No coercion between {typeof(U).Name} and {typeof(T).Name}");
				}
			}
			#endregion
			#region Multiply
			{
				// Can't cast 'b' to 'T' because of 'v4 * double' cases.
				// 'double' isn't castable to 'v4' but the operator v4 * double is defined.
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(U), "b");
				var body = typeof(T).IsScalar()
					? Expression.Multiply(paramA, Expression.ConvertChecked(paramB, typeof(T)))
					: Expression.Multiply(paramA, paramB);
				m_mul = Expression.Lambda<Func<T, U, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Divide
			{
				// Can't cast 'b' to 'T' because of 'v4 / double' cases.
				// 'double' isn't castable to 'v4' but the operator v4 / double is defined.
				var paramA = Expression.Parameter(typeof(T), "a");
				var paramB = Expression.Parameter(typeof(U), "b");
				var body = typeof(T).IsScalar()
					? Expression.Divide(paramA, Expression.ConvertChecked(paramB, typeof(T)))
					: Expression.Divide(paramA, paramB);
				m_div = Expression.Lambda<Func<T, U, T>>(body, paramA, paramB).Compile();
			}
			#endregion
			#region Power
			{
				//if (typeof(T).IsPrimitive && typeof(T) == typeof(double))
				if (typeof(T) == typeof(double))
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(U), "b");
					var body = Expression.Power(paramA, Expression.ConvertChecked(paramB, typeof(double)));
					m_pow = Expression.Lambda<Func<T, U, T>>(body, paramA, paramB).Compile();
				}
				else if (typeof(T).IsPrimitive)
				{
					var paramA = Expression.Parameter(typeof(T), "a");
					var paramB = Expression.Parameter(typeof(U), "b");
					var body = Expression.ConvertChecked(Expression.Power(Expression.ConvertChecked(paramA, typeof(double)), Expression.ConvertChecked(paramB, typeof(double))), typeof(T));
					m_pow = Expression.Lambda<Func<T, U, T>>(body, paramA, paramB).Compile();
				}
				else
				{
					m_pow = (a,b) => { throw new Exception($"Type {typeof(T).Name} does not support the Pow operator"); };
				}
			}
			#endregion
		}

		/// <summary>Cast a value to type 'T'</summary>
		public static T Cast(U a) { return m_cast(a); }
		private static Func<U, T> m_cast;

		/// <summary>a * b</summary>
		public static T Mul(T a, U b) { return m_mul(a,b); }
		private static Func<T,U,T> m_mul;

		/// <summary>a / b</summary>
		public static T Div(T a, U b) { return m_div(a,b); }
		private static Func<T,U,T> m_div;

		/// <summary>a ^ b</summary>
		public static T Pow(T a, U b) { return m_pow(a, b); }
		private static Func<T, U, T> m_pow;
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture] public class TestOperators
	{
		private enum Enum0
		{
			One   = 1,
			Two   = 2,
			Three = 3,
		}
		[Flags] private enum Enum1
		{
			One  = 1 << 0,
			Two  = 1 << 1,
			Four = 1 << 2,
			Seven = One | Two | Four,
		}

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

			Assert.Equal(Operators<decimal>.MaxValue, decimal.MaxValue);
			Assert.Equal(Operators<decimal>.MinValue, decimal.MinValue);
			Assert.Equal(Operators<decimal>.Cast(1.0).GetType(), typeof(decimal));

			Assert.Equal(Operators<short  >.Plus(412)       , (short)+412 );
			Assert.Equal(Operators<long   >.Neg(512)        , -512L       );
			Assert.Equal(Operators<double >.Add(0.1, 0.3)   , 0.1 + 0.3   );
			Assert.Equal(Operators<decimal>.Sub(0.5m, 0.8m) , 0.5m - 0.8m );
			Assert.Equal(Operators<int    >.Mul(3, 7)       , 3 * 7       );
			Assert.Equal(Operators<float  >.Div(1.2f, 3.4f) , 1.2f / 3.4f );

			Assert.Equal(Operators<float  >.Eql(1.23f, 1.24f)        , 1.23f == 1.24f );
			Assert.Equal(Operators<float  >.Eql(1.23f, 1.23f)        , 1.23f == 1.23f );
			Assert.Equal(Operators<float  >.NEql(1.23f, 1.24f)       , 1.23f != 1.24f );
			Assert.Equal(Operators<float  >.NEql(1.23f, 1.23f)       , 1.23f != 1.23f );
			Assert.Equal(Operators<float  >.Less(1.23f, 1.24f)       , 1.23f <  1.24f );
			Assert.Equal(Operators<float  >.Less(1.23f, 1.23f)       , 1.23f <  1.23f );
			Assert.Equal(Operators<float  >.Less(1.23f, 1.22f)       , 1.23f <  1.22f );
			Assert.Equal(Operators<float  >.LessEql(1.23f, 1.24f)    , 1.23f <= 1.24f );
			Assert.Equal(Operators<float  >.LessEql(1.23f, 1.23f)    , 1.23f <= 1.23f );
			Assert.Equal(Operators<float  >.LessEql(1.23f, 1.22f)    , 1.23f <= 1.22f );
			Assert.Equal(Operators<float  >.Greater(1.23f, 1.24f)    , 1.23f >  1.24f );
			Assert.Equal(Operators<float  >.Greater(1.23f, 1.23f)    , 1.23f >  1.23f );
			Assert.Equal(Operators<float  >.Greater(1.23f, 1.22f)    , 1.23f >  1.22f );
			Assert.Equal(Operators<float  >.GreaterEql(1.23f, 1.24f) , 1.23f >= 1.24f );
			Assert.Equal(Operators<float  >.GreaterEql(1.23f, 1.23f) , 1.23f >= 1.23f );
			Assert.Equal(Operators<float  >.GreaterEql(1.23f, 1.22f) , 1.23f >= 1.22f );

			Assert.Equal(Operators<float,int>.Mul(1.23f, 45), 1.23f * 45);
			Assert.Equal(Operators<float,int>.Div(1.23f, 45), 1.23f / 45);

			Assert.Equal(Operators<float>.ToString(1.23f, "C") , 1.23f.ToString("C"));
		}
		[Test] public void EnumUse()
		{
			// Non-flags
			Assert.Equal(Operators<Enum0>.Less(Enum0.One, Enum0.Three), true);

			// Flags
			Assert.Equal(Operators<Enum1>.BitwiseOR(Enum1.One, Enum1.Four), Enum1.One|Enum1.Four);
			Assert.Equal(Operators<Enum1>.BitwiseAND(Enum1.Seven, Enum1.Two), Enum1.Two);
			Assert.Equal(Operators<Enum1>.OnesComp(Enum1.Two), ~Enum1.Two);
			Assert.Equal(Operators<Enum1>.Less(Enum1.One, Enum1.Four), true);
		}
	}
}
#endif