using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Rylogic.Extn
{
	public static class Object_
	{
		/// <summary>Overload ToString with options for transforming the string</summary>
		[DebuggerStepThrough] public static string ToString(this object obj, Str.ECapitalise word_start = Str.ECapitalise.DontChange, Str.ECapitalise word_case = Str.ECapitalise.DontChange, Str.ESeparate word_sep = Str.ESeparate.DontChange, string sep = " ")
		{
			return obj.ToString().Txfm(word_start, word_case, word_sep, sep);
		}

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

		/// <summary>Copies all of the fields of this object into 'to'. Returns 'to' for method chaining</summary>
		public static object ShallowCopy(this object from, object to)
		{
			var type = from.GetType();
			if (type != to.GetType()) throw new ArgumentException("'from' and 'to' must be of the same type");
			foreach (var x in type.AllFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
				x.SetValue(to, x.GetValue(from));
			return to;
		}

		/// <summary>Creates a new instance of this object with all fields copied. Returns 'to' for method chaining</summary>
		public static object ShallowCopy(this object from)
		{
			return ShallowCopy(from, Activator.CreateInstance(from.GetType(), true));
		}

		/// <summary>Copies all of the fields of this object into 'to'. Returns 'to' for method chaining</summary>
		public static T ShallowCopy<T>(this T from, T to)
		{
			return (T)ShallowCopy((object)from, to);
		}

		/// <summary>Creates a new instance of this object with all fields copied. Returns 'to' for method chaining</summary>
		public static T ShallowCopy<T>(this T from)
		{
			return ShallowCopy(from, (T)Activator.CreateInstance(from.GetType(), true));
		}

		/// <summary>
		/// Recursively copies the fields of this object to 'to'. Returns 'to' for method chaining.
		/// Careful with this method, consider:<para/>
		/// class C { object o1, o2; C() { o1 = o2 = new Thing(); } }<para/>
		/// A deep copy of an instance of this class will cause o1 != o2</summary>
		public static object DeepCopy(this object from, object to)
		{
			var type = from.GetType();
			if (type != to.GetType()) throw new ArgumentException("'from' and 'to' must be of the same type");
			foreach (var x in type.AllFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
			{
				var v = x.GetValue(from);
				if (!x.FieldType.IsClass || x.FieldType == typeof(string) || v == null)
				{
					x.SetValue(to, v);
					continue;
				}
				if (x.FieldType.IsArray)
				{
					Array arr0 = (Array)v;
					Array arr1;
					var elem_type = arr0.GetType().GetElementType();
					if (!elem_type.IsClass || elem_type == typeof(string))
					{
						arr1 = (Array)arr0.Clone(); // Clone is good enough for non-ref types
					}
					else
					{	arr1 = Array.CreateInstance(arr0.GetType().GetElementType(), arr0.Length); // shallow copy
						var sz  = Enumerable.Range(0,arr0.Rank).Select(arr0.GetLength).ToArray();
						var idx = new int[sz.Length];
						for (;;)
						{
							arr1.SetValue(DeepCopy(arr0.GetValue(idx)), idx); // deep copy the elements
							int r; for (r = arr0.Rank; --r != 0 && idx[r] == sz[r]; ++r) {}
							if (r == -1) break;
							for (; r != arr0.Rank && ++idx[r] == sz[r]; idx[r] = 0, ++r) {}
							if (r == arr0.Rank) break;
						}
					}
					x.SetValue(to, arr1);
					continue;
				}
				if (x.FieldType.IsClass)
				{
					x.SetValue(to, DeepCopy(v));
					continue;
				}
				throw new NotImplementedException("Unknown class of member");
			}
			return to;
		}

		/// <summary>
		/// Creates a new instance of this object with all fields copied (recursively).
		/// Returns 'to' for method chaining.<para/>
		/// Careful with this method, consider:<para/>
		/// class C { object o1, o2; C() { o1 = o2 = new Thing(); } }<para/>
		/// A deep copy of an instance of this class will cause o1 != o2</summary>
		public static object DeepCopy(this object from)
		{
			return DeepCopy(from, Activator.CreateInstance(from.GetType(), true));
		}

		/// <summary>Copies all of the fields of this object into 'to' (recursively). Returns 'to' for method chaining.<para/>
		/// Careful with this method, consider:<para/>
		/// class C { object o1, o2; C() { o1 = o2 = new Thing(); } }<para/>
		/// A deep copy of an instance of this class will cause o1 != o2</summary>
		public static T DeepCopy<T>(this T from, T to)
		{
			return (T)DeepCopy((object)from, to);
		}

		/// <summary>Creates a new instance of this object with all fields copied (recursively). Returns 'to' for method chaining.<para/>
		/// Careful with this method, consider:<para/>
		/// class C { object o1, o2; C() { o1 = o2 = new Thing(); } }<para/>
		/// A deep copy of an instance of this class will cause o1 != o2</summary>
		public static T DeepCopy<T>(this T from)
		{
			return DeepCopy(from, (T)Activator.CreateInstance(from.GetType(), true));
		}

		/// <summary>Helper class for generating compiled lambda expressions</summary>
		private static class MethodGenerator<T>
		{
			// Doesn't work.
			// Can't use the generated method because it could 'destabilize the runtime'
			///// <summary>Returns a shallow copy of 'obj' as a new instance</summary>
			//public static T Clone(T obj) { return m_func_clone(obj); }
			//private static readonly Func<T,T> m_func_clone = CloneFunc();
			//private static Func<T,T> CloneFunc()
			//{
			//    var p = Expression.Parameter(typeof(T), "obj");
			//    var bindings = typeof(T)
			//        .AllFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
			//        .Select(x => (MemberBinding)Expression.Bind(x, Expression.Field(p,x)));
			//    Expression body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
			//    return Expression.Lambda<Func<T,T>>(body, p).Compile();
			//}

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
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture] public class TestObjectExtns
	{
		public class Cloner
		{
			public int Level { get { return m_level; } }
			private readonly int m_level;
			public string m_string;
			public object m_object { get; set; }
			public Cloner m_child;
			public List<DateTime> m_list;
			public Dictionary<string, object> m_dict;

			protected Cloner() :this(0) {}
			public Cloner(int level)
			{
				m_level = level;
				if (m_level > 0)
					m_child = new Cloner(level - 1);
				m_string = level.ToString(CultureInfo.InvariantCulture);
				if ((level % 3) == 0) m_object = null;
				if ((level % 3) == 1) m_object = m_child;
				if ((level % 3) == 2) m_object = m_level;
				m_list = new List<DateTime>();
				m_list.Add(DateTime.MaxValue);
				m_list.Add(DateTime.Now);
				m_dict = new Dictionary<string, object>();
				m_dict.Add(m_string, m_object);
			}
			public virtual bool Equal(Cloner rhs)
			{
				if (rhs == null) return false;
				if (m_level != rhs.m_level) return false;
				if (m_string != rhs.m_string) return false;
				if ((m_object == null) != (rhs.m_object == null)) return false;
				if (m_object is Cloner && !((Cloner)m_object).Equal(rhs.m_object as Cloner)) return false;
				if (m_object is int && (!(rhs.m_object is int) || (int)m_object != (int)rhs.m_object)) return false;
				if ((m_child == null) != (rhs.m_child == null)) return false;
				if (m_child != null && !m_child.Equal(rhs.m_child)) return false;
				if (!m_list.SequenceEqual(rhs.m_list)) return false;
				if (!m_dict.SequenceEqual(rhs.m_dict)) return false;
				return true;
			}
		}
		public class DerivedCloner :Cloner
		{
			public int m_derived_field;
			protected DerivedCloner() {}
			public DerivedCloner(int level) :base(level) { m_derived_field = level; }
			public override bool Equal(Cloner rhs)
			{
				var d = rhs as DerivedCloner;
				if (d == null || d.m_derived_field != m_derived_field) return false;
				return base.Equal(rhs);
			}
		}
		[Test] public void ShallowCopy()
		{
			Action<Cloner,Cloner> CheckShallowEqual = (lhs,rhs) =>
			{
				Assert.True(lhs.GetType() == rhs.GetType());
				Assert.True(!ReferenceEquals(lhs,rhs));
				Assert.True(lhs.Level == rhs.Level);
				Assert.True(lhs.m_string == rhs.m_string);
				Assert.True(ReferenceEquals(lhs.m_object,rhs.m_object));
				Assert.True(ReferenceEquals(lhs.m_child,rhs.m_child));
				Assert.True(ReferenceEquals(lhs.m_list,rhs.m_list));
				Assert.True(ReferenceEquals(lhs.m_dict,rhs.m_dict));
			};

			var c1 = new Cloner(5);
			var c2 = c1.ShallowCopy();
			CheckShallowEqual(c1,c2);

			var d1 = (Cloner)new DerivedCloner(3);
			var d2 = d1.ShallowCopy();
			CheckShallowEqual(d1, d2);
		}
		[Test] public void DeepCopy()
		{
			Action<Cloner,Cloner> CheckDeepEqual = (lhs,rhs) =>
				{
					Assert.True(!ReferenceEquals(lhs,rhs));
					Assert.True(lhs.Level == rhs.Level);
					Assert.True(lhs.m_string == rhs.m_string);
					Assert.True((lhs.m_object == null && rhs.m_object == null) || !ReferenceEquals(lhs.m_object,rhs.m_object));
					Assert.True((lhs.m_child == null && rhs.m_child == null) || !ReferenceEquals(lhs.m_child,rhs.m_child));
					Assert.True((lhs.m_list == null && rhs.m_list == null) || !ReferenceEquals(lhs.m_list,rhs.m_list));
					Assert.True((lhs.m_dict == null && rhs.m_dict == null) || !ReferenceEquals(lhs.m_dict,rhs.m_dict));
					Assert.True(lhs.Equal(rhs));
				};

			var c1 = new Cloner(5);
			var c2 = c1.DeepCopy();
			CheckDeepEqual(c1,c2);

			var d1 = (Cloner)new DerivedCloner(3);
			var d2 = d1.DeepCopy();
			CheckDeepEqual(d1, d2);
		}
	}
}
#endif