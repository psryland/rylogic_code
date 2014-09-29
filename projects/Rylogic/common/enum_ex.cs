//***************************************************
// EnumEx
//  Copyright (c) Rylogic Ltd 2013
//***************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace pr.common
{
	[DebuggerDisplay("{m_value} ({m_name})")]
	public abstract class EnumEx<TValue, TDerived> :IEquatable<TDerived>, IComparable<TDerived>, IComparable, IComparer<TDerived>
		where TValue : struct, IComparable<TValue>, IEquatable<TValue>
		where TDerived :EnumEx<TValue, TDerived>
	{
		/// <summary>Reverse lookup to convert values back to local instances</summary>
		private static readonly SortedList<TValue, TDerived> m_values = new SortedList<TValue, TDerived>();

		/// <summary>The value of the enum item</summary>
		private readonly TValue m_value;

		/// <summary>The public field name, determined from reflection</summary>
		private string m_name;

		protected static void Init()
		{
			var fields = typeof(TDerived)
				.GetFields(BindingFlags.Static | BindingFlags.GetField | BindingFlags.Public)
				.Where(t => t.FieldType == typeof(TDerived));

			foreach (var field in fields)
			{
				TDerived instance = (TDerived)field.GetValue(null);
				instance.m_name = field.Name;
			}
		}

		protected EnumEx(TValue value)
		{
			m_value = value;
			m_values.Add(value, (TDerived)this);
		}

		public override string ToString() { return m_name; }

		public static TDerived Convert(TValue value) { return m_values[value]; }
		public static bool TryConvert(TValue value, out TDerived result) { return m_values.TryGetValue(value, out result); }

		public static implicit operator TDerived(EnumEx<TValue, TDerived> value) { return value; }
		public static implicit operator TValue(EnumEx<TValue, TDerived> value) { return value.m_value; }
		public static implicit operator EnumEx<TValue, TDerived>(TValue value) { return m_values[value]; } 

		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				if (obj is TValue)
					return m_value.Equals((TValue)obj);

				if (obj is TDerived)
					return m_value.Equals(((TDerived)obj).m_value);
			}
			return false;
		}

		bool IEquatable<TDerived>.Equals(TDerived other) { return m_value.Equals(other.m_value); }
		public override int GetHashCode() { return m_value.GetHashCode(); } 

		int IComparable.CompareTo(object obj)
		{
			if (obj != null)
			{
				if (obj is TValue)
					return m_value.CompareTo((TValue)obj);

				if (obj is TDerived)
					return m_value.CompareTo(((TDerived)obj).m_value);
			}
			return -1;
		}
		int IComparable<TDerived>.CompareTo(TDerived other)     { return m_value.CompareTo(other.m_value); }
		int IComparer<TDerived>.Compare(TDerived x, TDerived y) { return (x == null) ? -1 : (y == null) ? 1 : x.m_value.CompareTo(y.m_value); }

		public static IEnumerable<TDerived> Values { get { return m_values.Values; } }

		public static TDerived Parse(string name)
		{
			foreach (TDerived value in Values)
				if (0 == String.Compare(value.m_name, name, StringComparison.OrdinalIgnoreCase))
					return value;
			return null;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using common;

	[TestFixture] public class TestEnumEx
	{
		public sealed class MyEnum :EnumEx<int, MyEnum>
		{
			public static readonly MyEnum One   = new MyEnum(1 << 0);
			public static readonly MyEnum Two   = new MyEnum(1 << 1);
			public static readonly MyEnum Three = new MyEnum(1 << 2);
			public static readonly MyEnum Four  = new MyEnum(1 << 3);
				
			static MyEnum() { Init(); }
			private MyEnum(int value) :base(value) {}
			public static implicit operator MyEnum(int value) { return Convert(value); }
		}
		[Test] public void Func()
		{
			MyEnum e = MyEnum.One;
			Assert.AreEqual(e, 1 << 0);
		}
	}
}
#endif
