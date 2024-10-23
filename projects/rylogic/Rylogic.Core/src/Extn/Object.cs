using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Rylogic.Extn
{
	public static class Object_
	{
		/// <summary>Overload ToString with options for transforming the string</summary>
		[DebuggerStepThrough]
		public static string ToString(this object obj, Str_.ECapitalise word_start = Str_.ECapitalise.DontChange, Str_.ECapitalise word_case = Str_.ECapitalise.DontChange, Str_.ESeparate word_sep = Str_.ESeparate.DontChange, string sep = " ")
		{
			return obj.ToString()?.Txfm(word_start, word_case, word_sep, sep) ?? string.Empty;
		}

		/// <summary>Returns a string containing a description of this object and its member values</summary>
		public static string Dump(this object obj)
		{
			if (obj == null) return "null";
			var type = obj.GetType();

			m_dump ??= new StringBuilder();
			try
			{
				m_dump.Append(type.Name).AppendLine(":");
				foreach (var pi in type.GetProperties())
				{
					// Does the property take parameters? (If so, it's probably an indexer)
					if (pi.GetIndexParameters() is ParameterInfo[] parms && parms.Length != 0)
					{
						m_dump.Append(pi.Name).Append(": {collection}");
					}
					else
					{
						var val = pi.GetValue(obj, null);
						m_dump.Append(pi.Name).Append(": ").AppendLine(val ?? string.Empty);
					}
				}
				foreach (var fi in type.GetFields())
				{
					var val = fi.GetValue(obj);
					m_dump.Append(fi.Name).Append(": ").AppendLine(val ?? string.Empty);
				}
				return m_dump.ToString();
			}
			finally { m_dump.Length = 0; }
		}
		[ThreadStatic] private static StringBuilder? m_dump;

		/// <summary>Invoke a method on 'obj' if it has it. Returns true if found and invoked</summary>
		public static bool TryInvoke(this object obj, string name, params object[] args) => TryInvoke(obj, name, BindingFlags.Instance | BindingFlags.NonPublic, args);
		public static bool TryInvoke(this object obj, string name, BindingFlags binding_flags, params object[] args)
		{
			var ty = obj.GetType();
			if (ty.GetMethod(name, binding_flags) is MethodInfo mi)
			{
				mi.Invoke(obj, args);
				return true;
			}
			return false;
		}

		/// <summary>Invoke a property or parameterless method on 'obj' if it has it. Returns true if found and invoked</summary>
		public static (bool, T) TryInvoke<T>(this object obj, string name) => TryInvoke<T>(obj, name, BindingFlags.Instance | BindingFlags.NonPublic);
		public static (bool, T) TryInvoke<T>(this object obj, string name, BindingFlags binding_flags)
		{
			var ty = obj.GetType();
			if (ty.GetProperty(name, binding_flags) is PropertyInfo pi)
			{
				var result = (T?)pi.GetGetMethod()?.Invoke(obj, null) ?? throw new NullReferenceException();
				return (true, result);
			}
			if (ty.GetMethod(name, binding_flags) is MethodInfo mi)
			{
				var result = (T?)mi.Invoke(obj, null) ?? throw new NullReferenceException();
				return (true, result);
			}
			return (false, default!);
		}

		/// <summary>Invoke a method that returns a value if it has it. Returns true if found and invoked</summary>
		public static (bool, T) TryInvoke<T>(this object obj, string name, params object[] args) => TryInvoke<T>(obj, name, BindingFlags.Instance | BindingFlags.NonPublic, args);
		public static (bool, T) TryInvoke<T>(this object obj, string name, BindingFlags binding_flags, params object[] args)
		{
			var ty = obj.GetType();
			if (ty.GetMethod(name, binding_flags) is MethodInfo mi)
			{
				var result = (T?)mi.Invoke(obj, null) ?? throw new NullReferenceException();
				return (true, result);
			}
			return (false, default!);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	[TestFixture] public class TestObjectExtns
	{
		//public class Cloner
		//{
		//	public int Level { get { return m_level; } }
		//	private readonly int m_level;
		//	public string? m_string;
		//	public object? m_object { get; set; }
		//	public Cloner? m_child;
		//	public List<DateTime> m_list;
		//	public Dictionary<string, object?> m_dict;

		//	protected Cloner() :this(0) {}
		//	public Cloner(int level)
		//	{
		//		m_level = level;
		//		if (m_level > 0)
		//			m_child = new Cloner(level - 1);
		//		m_string = level.ToString(CultureInfo.InvariantCulture);
		//		if ((level % 3) == 0) m_object = null;
		//		if ((level % 3) == 1) m_object = m_child;
		//		if ((level % 3) == 2) m_object = m_level;
		//		m_list = new List<DateTime>();
		//		m_list.Add(DateTime.MaxValue);
		//		m_list.Add(DateTime.Now);
		//		m_dict = new Dictionary<string, object?>();
		//		m_dict.Add(m_string, m_object);
		//	}
		//	public virtual bool Equal(Cloner? rhs)
		//	{
		//		if (rhs == null) return false;
		//		if (m_level != rhs.m_level) return false;
		//		if (m_string != rhs.m_string) return false;
		//		if ((m_object == null) != (rhs.m_object == null)) return false;
		//		if (m_object is Cloner && !((Cloner)m_object).Equal(rhs.m_object as Cloner)) return false;
		//		if (m_object is int && (!(rhs.m_object is int) || (int)m_object != (int)rhs.m_object)) return false;
		//		if ((m_child == null) != (rhs.m_child == null)) return false;
		//		if (m_child != null && !m_child.Equal(rhs.m_child)) return false;
		//		if (!m_list.SequenceEqual(rhs.m_list)) return false;
		//		if (!m_dict.SequenceEqual(rhs.m_dict)) return false;
		//		return true;
		//	}
		//}
		//public class DerivedCloner :Cloner
		//{
		//	public int m_derived_field;
		//	protected DerivedCloner() {}
		//	public DerivedCloner(int level) :base(level) { m_derived_field = level; }
		//	public override bool Equal(Cloner? rhs)
		//	{
		//		var d = rhs as DerivedCloner;
		//		if (d == null || d.m_derived_field != m_derived_field) return false;
		//		return base.Equal(rhs);
		//	}
		//}
		//[Test] public void ShallowCopy()
		//{
		//	Action<Cloner,Cloner> CheckShallowEqual = (lhs,rhs) =>
		//	{
		//		Assert.True(lhs.GetType() == rhs.GetType());
		//		Assert.True(!ReferenceEquals(lhs,rhs));
		//		Assert.True(lhs.Level == rhs.Level);
		//		Assert.True(lhs.m_string == rhs.m_string);
		//		Assert.True(ReferenceEquals(lhs.m_object,rhs.m_object));
		//		Assert.True(ReferenceEquals(lhs.m_child,rhs.m_child));
		//		Assert.True(ReferenceEquals(lhs.m_list,rhs.m_list));
		//		Assert.True(ReferenceEquals(lhs.m_dict,rhs.m_dict));
		//	};

		//	var c1 = new Cloner(5);
		//	var c2 = c1.ShallowCopy();
		//	CheckShallowEqual(c1,c2);

		//	var d1 = (Cloner)new DerivedCloner(3);
		//	var d2 = d1.ShallowCopy();
		//	CheckShallowEqual(d1, d2);
		//}
		//[Test] public void DeepCopy()
		//{
		//	Action<Cloner,Cloner> CheckDeepEqual = (lhs,rhs) =>
		//		{
		//			Assert.True(!ReferenceEquals(lhs,rhs));
		//			Assert.True(lhs.Level == rhs.Level);
		//			Assert.True(lhs.m_string == rhs.m_string);
		//			Assert.True((lhs.m_object == null && rhs.m_object == null) || !ReferenceEquals(lhs.m_object,rhs.m_object));
		//			Assert.True((lhs.m_child == null && rhs.m_child == null) || !ReferenceEquals(lhs.m_child,rhs.m_child));
		//			Assert.True((lhs.m_list == null && rhs.m_list == null) || !ReferenceEquals(lhs.m_list,rhs.m_list));
		//			Assert.True((lhs.m_dict == null && rhs.m_dict == null) || !ReferenceEquals(lhs.m_dict,rhs.m_dict));
		//			Assert.True(lhs.Equal(rhs));
		//		};

		//	var c1 = new Cloner(5);
		//	var c2 = c1.DeepCopy();
		//	CheckDeepEqual(c1,c2);

		//	var d1 = (Cloner)new DerivedCloner(3);
		//	var d2 = d1.DeepCopy();
		//	CheckDeepEqual(d1, d2);
		//}
	}
}
#endif