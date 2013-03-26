using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace pr.extn
{
	public static class ObjectExtensions
	{
		/// <summary>Returns a string containing a description of this object and its member values</summary>
		public static string Dump(this object obj)
		{
			if (obj == null) return "null";
			var type = obj.GetType();
			return
				"{0}:\n".Fmt(type.Name) +
				string.Join("\n", type.GetProperties().Select(p => p.Name + ":  " + p.GetValue(obj, null)))+
				"\n";
		}

		/// <summary>Helper class for generating compiled lambda expressions</summary>
		private static class MethodGenerator<T>
		{
			/// <summary>Returns a shallow copy of 'obj' as a new instance</summary>
			public static object Clone(object obj) { return m_func_clone((T)obj); }
			private static readonly Func<T,T> m_func_clone = CloneFunc();
			private static Func<T,T> CloneFunc()
			{
				var p = Expression.Parameter(typeof(T), "obj");
				var bindings = typeof(T).AllFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
					.Select(x => (MemberBinding)Expression.Bind(x, Expression.Field(p,x)));
				Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
				return Expression.Lambda<Func<T,T>>(body, p).Compile();
			}

			///// <summary>Test two instances of 'T' for having equal fields</summary>
			//public static bool Equal(object lhs, object rhs) { return m_func_equal((T)lhs, (T)rhs); }
			//private static readonly Func<T,T,bool> m_func_equal = EqualFunc();
			//private static Func<T,T,bool> EqualFunc()
			//{
			//    var lhs = Expression.Parameter(typeof(T), "lhs");
			//    var rhs = Expression.Parameter(typeof(T), "rhs");
			//    Expression body = Expression.Constant(true);
			//    foreach (var f in typeof(T).AllFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
			//        body = Expression.AndAlso(Expression.Equal(Expression.Field(lhs, f), Expression.Field(rhs, f)), body);
			//    return Expression.Lambda<Func<T,T,bool>>(body, lhs, rhs).Compile();
			//}
		}

		/// <summary>Returns a shallow copy of this instance</summary>
		public static object ShallowCopy(this object obj)
		{
			MethodInfo clone = typeof(MethodGenerator<>).MakeGenericType(obj.GetType()).GetMethod("Clone", BindingFlags.Static|BindingFlags.Public);
			return clone.Invoke(null, new[]{obj});
		}
	}
}
#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using extn;
	
	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestObjectExtensions
		{
			public class Thing
			{
				private readonly int PrivateField;
				protected int ProtectedField;
				internal int  InternalField;
				public string PublicField;
				private int   PrivateAutoProp { get; set; }
				protected int ProtectedAutoProp { get; set; }
				internal int  InternalAutoProp { get; set; }
				public string PublicAutoProp { get; set; }
			
				public Thing() {}
				public Thing(int i)
				{
					PrivateField      = i;
					ProtectedField    = i;
					InternalField     = i;
					PublicField       = i.ToString();
					PrivateAutoProp   = i;
					ProtectedAutoProp = i;
					InternalAutoProp  = i;
					PublicAutoProp    = i.ToString();
				}
				public bool Equals(Thing other)
				{
					return
					PrivateField      == other.PrivateField      &&
					ProtectedField    == other.ProtectedField    &&
					InternalField     == other.InternalField     &&
					PublicField       == other.PublicField       &&
					PrivateAutoProp   == other.PrivateAutoProp   &&
					ProtectedAutoProp == other.ProtectedAutoProp &&
					InternalAutoProp  == other.InternalAutoProp  &&
					PublicAutoProp    == other.PublicAutoProp    ;
				}
			}
			[Test] public static void ShallowCopy()
			{
				//var t0 = new Thing(1);
				//var t1 = t0.ShallowCopy();
				//Assert.IsTrue(t0.Equals(t1));

				//object t2 = t0;
				//object t3 = t2.ShallowCopy();
				//Assert.IsTrue(((Thing)t2).Equals((Thing)t3));
			}
		}
	}
}
#endif