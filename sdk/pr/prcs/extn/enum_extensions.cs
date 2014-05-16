//***************************************************
// Enum Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using pr.util;

namespace pr.extn
{
	public static class Enum<T> where T : struct, IConvertible
	{
		// Some of this came from:
		//  http://www.codeproject.com/KB/dotnet/enum.aspx
		//  Author: Boris Dongarov (ideafixxxer)
		private static readonly EnumConverter Converter;

		#region ToString Converters

		#region EnumConverter
		private abstract class EnumConverter
		{
			public abstract string ToStringInternal(long value);
			public abstract long   ParseInternal(string value, bool ignoreCase, bool parseNumber);
			public abstract bool   TryParseInternal(string value, bool ignore_case, bool parse_number, out long result);
		}
		#endregion

		#region ArrayEnumConverter
		class ArrayEnumConverter :EnumConverter
		{
			private readonly string[] m_names = Enum.GetNames(typeof(T));

			public ArrayEnumConverter(string[] names)
			{
				m_names = names;
			}
			public override string ToStringInternal(long value)
			{
				return value >= 0 && value < m_names.Length ? m_names[value] : value.ToString(CultureInfo.InvariantCulture);
			}
			public override long ParseInternal(string value, bool ignoreCase, bool parseNumber)
			{
				if (value == null) throw new ArgumentNullException("value");
				if (value.Length == 0) throw new ArgumentException("Value is empty", "value");

				var f = value[0];
				if (parseNumber && (Char.IsDigit(f) || f == '+' || f == '-'))
					return Int32.Parse(value);

				var string_comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				for (var i = 0; i < m_names.Length; i++)
					if (m_names[i].Equals(value, string_comparison))
						return i;

				throw new ArgumentException("Enum value wasn't found", "value");
			}
			public override bool TryParseInternal(string value, bool ignore_case, bool parse_number, out long result)
			{
				result = 0;
				if (string.IsNullOrEmpty(value))
					return false;

				var f = value[0];
				if (parse_number && (Char.IsDigit(f) || f == '+' || f == '-'))
				{
					int i;
					if (Int32.TryParse(value, out i))
					{
						result = i;
						return true;
					}
					return false;
				}
				var string_comparison = ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				for (var i = 0; i < m_names.Length; i++)
				{
					if (!m_names[i].Equals(value,string_comparison)) continue;
					result = i;
					return true;
				}
				return false;
			}
		}
		#endregion

		#region DictionaryEnumCovnerter
		private class DictionaryEnumConverter : EnumConverter
		{
			protected readonly Dictionary<long, string> m_dic;

			public DictionaryEnumConverter(string[] names, T[] values)
			{
				m_dic = new Dictionary<long, string>(names.Length);
				for (var j = 0; j < names.Length; j++)
					m_dic.Add(Convert.ToInt64(values[j], null), names[j]);
			}
			public override string ToStringInternal(long value)
			{
				string n;
				return m_dic.TryGetValue(value, out n) ? n : value.ToString(CultureInfo.InvariantCulture);
			}
			public override long ParseInternal(string value, bool ignoreCase, bool parseNumber)
			{
				if (value == null) throw new ArgumentNullException("value");
				if (value.Length == 0) throw new ArgumentException("Value is empty", "value");

				var f = value[0];
				if (parseNumber && (Char.IsDigit(f) || f == '+' || f == '-'))
					return Int32.Parse(value);

				var string_comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				foreach (var pair in m_dic)
				{
					if (pair.Value.Equals(value, string_comparison))
						return pair.Key;
				}
				throw new ArgumentException("Enum value wasn't found", "value");
			}
			public override bool TryParseInternal(string value, bool ignore_case, bool parse_number, out long result)
			{
				result = 0;
				if (string.IsNullOrEmpty(value))
					return false;

				var f = value[0];
				if (parse_number && (Char.IsDigit(f) || f == '+' || f == '-'))
				{
					int i;
					if (Int32.TryParse(value, out i))
					{
						result = i;
						return true;
					}
					return false;
				}
				var string_comparison = ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
				foreach (var pair in m_dic)
				{
					if (!pair.Value.Equals(value,string_comparison)) continue;
					result = pair.Key;
					return true;
				}
				return false;
			}
		}
		#endregion

		#region FlagsEnumConverter
		private class FlagsEnumConverter : DictionaryEnumConverter
		{
			private readonly long[] m_values;

			public FlagsEnumConverter(string[] names, T[] values) :base(names, values)
			{
				m_values = new long[values.Length];
				for (var i = 0; i < values.Length; i++)
					m_values[i] = values[i].ToInt64(null);
			}
			public override string ToStringInternal(long value)
			{
				string n;
				if (m_dic.TryGetValue(value, out n)) return n;
				var sb = new StringBuilder();
				const string sep = ", ";
				for (var i = m_values.Length - 1; i >= 0; i--)
				{
					var v = m_values[i];
					if (v == 0) continue;
					if ((v & value) != v) continue;
					value &= ~v;
					sb.Insert(0, sep).Insert(0, m_dic[(int)v]);
				}
				return value == 0 && sb.Length > sep.Length ? sb.ToString(0, sb.Length - sep.Length) : value.ToString(CultureInfo.InvariantCulture);
			}
			public override long ParseInternal(string value, bool ignoreCase, bool parseNumber)
			{
				var parts = value.Split(new[]{"|"}, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 1) return base.ParseInternal(value, ignoreCase, parseNumber);
				var val = 0L;
				foreach (var part in parts)
				{
					var t = base.ParseInternal(part.Trim(), ignoreCase, parseNumber);
					val |= t;
				}
				return val;
			}
			public override bool TryParseInternal(string value, bool ignore_case, bool parse_number, out long result)
			{
				var parts = value.Split(new[]{"|"}, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 1) return base.TryParseInternal(value, ignore_case, parse_number, out result);
				var val = 0L;
				foreach (var part in parts)
				{
					long t;
					if (!base.TryParseInternal(part.Trim(), ignore_case, parse_number, out t))
					{
						result = 0;
						return false;
					}
					val |= t;
				}
				result = val;
				return true;
			}
		}
		#endregion

		#endregion

		/// <summary>Converts enum value to string</summary>
		/// <returns>If <paramref name="value"/> is defined, the enum member name; otherwise the string representation of the <paramref name="value"/>.
		/// If <see cref="FlagsAttribute"/> is applied, can return a delimited list of values</returns>
		public static string ToString(long value)
		{
			return Converter.ToStringInternal(value);
		}

		/// <summary>Converts enum value to string</summary>
		/// <returns>If <paramref name="value"/> is defined, the enum member name; otherwise the string representation of the <paramref name="value"/>.
		/// If <see cref="FlagsAttribute"/> is applied, can return comma-separated list of values</returns>
		public static string ToString(T value)
		{
			return Converter.ToStringInternal(value.ToInt64(null));
		}

		public static T Parse(string value, bool ignore_case = false, bool parse_numeric = true)
		{
			return (T) Enum.ToObject(typeof(T), Converter.ParseInternal(value, ignore_case, parse_numeric));
		}

		public static bool TryParse(string value, bool ignoreCase, bool parseNumeric, out T result)
		{
			long ir;
			var b = Converter.TryParseInternal(value, ignoreCase, parseNumeric, out ir);
			result = (T) Enum.ToObject(typeof(T), ir);
			return b;
		}

		public static bool TryParse(string value, bool ignoreCase, out T result)
		{
			long ir;
			var b = Converter.TryParseInternal(value, ignoreCase, true, out ir);
			result = (T)Enum.ToObject(typeof(T), ir);
			return b;
		}

		public static bool TryParse(string value, out T result)
		{
			long ir;
			bool b = Converter.TryParseInternal(value, false, true, out ir);
			result = (T)Enum.ToObject(typeof(T), ir);
			return b;
		}

		/// <summary>Returns an indication whether a constant with a specified value exists in a specified enumeration.</summary>
		public static bool IsDefined(object value)
		{
			return Enum.IsDefined(typeof(T), value);
		}

		/// <summary>Returns the next enum value after 'value'. Note: this is really enum abuse. Use sparingly</summary>
		public static T Cycle(T src)
		{
			var arr = (T[])Enum.GetValues(typeof(T));
			int i = Array.IndexOf(arr, src) + 1;
			return (i >= 0 && i < arr.Length) ? arr[i] : arr[0];
		}

		/// <summary>Returns all enum field names as a collection</summary>
		public static IEnumerable<string> Names
		{
			get { return Enum.GetNames(typeof(T)); }
		}

		/// <summary>Returns all values of the enum as a collection</summary>
		public static IEnumerable<T> Values
		{
			get { return Enum.GetValues(typeof(T)).Cast<T>(); }
		}

		/// <summary>Returns the number of names or values in the enumeration</summary>
		public static int Count
		{
			get { return Enum.GetValues(typeof(T)).Length; }
		}

		static Enum()
		{
			var type = typeof(T);
			if (!type.IsEnum)
				throw new ArgumentException("Generic Enum type works only with enums");

			var names = Enum.GetNames(type);
			var values = (T[])Enum.GetValues(type);
			if (type.GetCustomAttributes(typeof(FlagsAttribute), false).Length != 0)
			{
				Converter = new FlagsEnumConverter(names, values);
			}
			else
			{
				Converter = values.Where((t, i) => Convert.ToInt32(t) != i).Any()
					? new DictionaryEnumConverter(names, values)
					: (EnumConverter)new ArrayEnumConverter(names);
			}
		}
	}

	public static class EnumExtensions
	{
		/// <summary>Convert the enum value to a string containing spaces</summary>
		public static string ToPrettyString<TEnum>(this TEnum e) where TEnum :struct ,IConvertible
		{
			return StrTxfm.Apply(e.ToStringFast(), StrTxfm.ECapitalise.UpperCase, StrTxfm.ECapitalise.DontChange, StrTxfm.ESeparate.Add, " ");
		}

		/// <summary>A faster overload of ToString</summary>
		public static string ToStringFast<TEnum>(this TEnum e) where TEnum :struct ,IConvertible
		{
			return Enum<TEnum>.ToString(e);
		}

		/// <summary>Return the index of this enum value within the enum (for non-sequential enums)
		/// Use: (MyEnum)Enum.GetValues(typeof(MyEnum)).GetValue(i) for the reverse of this method</summary>
		public static int ToIndex(this Enum e)
		{
			int i = 0;
			foreach (var v in Enum.GetValues(e.GetType()))
			{
				if (e.GetHashCode() == v.GetHashCode()) return i;
				++i;
			}
			return -1;
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
		internal static class TestEnumExtensions
		{
			public enum ArrayEnum
			{
				One,
				Two,
				Three,
			}
			public enum SparceEnum
			{
				One = 1,
				Four = 4,
				Ten = 10,
			}
			[Flags] public enum FlagsEnum
			{
				A = 1 << 0,
				B = 1 << 1,
				C = 1 << 2
			}
			public enum Tags
			{
				SomeCompoundName,
				ABBRValue01
			}
			[Flags] public enum BigFlags :uint
			{
				A = 0x80000000,
				B = 0xC0000000,
				C = 0xE0000000,
			}

			[Test] public static void TestToString1()
			{
				Assert.AreEqual(ArrayEnum.One   .ToString(), ArrayEnum.One   .ToStringFast());
				Assert.AreEqual(ArrayEnum.Two   .ToString(), ArrayEnum.Two   .ToStringFast());
				Assert.AreEqual(ArrayEnum.Three .ToString(), ArrayEnum.Three .ToStringFast());
			}
			[Test] public static void TestToString2()
			{
				Assert.AreEqual(SparceEnum.One  .ToString(), SparceEnum.One  .ToStringFast());
				Assert.AreEqual(SparceEnum.Four .ToString(), SparceEnum.Four .ToStringFast());
				Assert.AreEqual(SparceEnum.Ten  .ToString(), SparceEnum.Ten  .ToStringFast());
			}
			[Test] public static void TestToString3()
			{
				var f = FlagsEnum.A;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
				f = FlagsEnum.A|FlagsEnum.B;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
				f = FlagsEnum.A|FlagsEnum.C;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
				f = FlagsEnum.A|FlagsEnum.B|FlagsEnum.C;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
			}
			[Test] public static void TestToString4()
			{
				Assert.AreEqual("Some Compound Name", Tags.SomeCompoundName.ToPrettyString());
				Assert.AreEqual("ABBR Value 01", Tags.ABBRValue01.ToPrettyString());
			}
			[Test] public static void TestToString5()
			{
				Assert.AreEqual(BigFlags.A.ToString(), BigFlags.A.ToStringFast());
				Assert.AreEqual(BigFlags.B.ToString(), BigFlags.B.ToStringFast());
				Assert.AreEqual(BigFlags.C.ToString(), BigFlags.C.ToStringFast());
			}
		}
	}
}
#endif
