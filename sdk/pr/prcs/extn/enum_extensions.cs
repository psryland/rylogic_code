//***************************************************
// Enum Extensions
//  Copyright © Rylogic Ltd 2008
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
			public abstract string ToStringInternal(int value);
			public abstract int    ParseInternal(string value, bool ignoreCase, bool parseNumber);
			public abstract bool   TryParseInternal(string value, bool ignore_case, bool parse_number, out int result);
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
			public override string ToStringInternal(int value)
			{
				return value >= 0 && value < m_names.Length ? m_names[value] : value.ToString(CultureInfo.InvariantCulture);
			}
			public override int ParseInternal(string value, bool ignoreCase, bool parseNumber)
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
			public override bool TryParseInternal(string value, bool ignore_case, bool parse_number, out int result)
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
			protected readonly Dictionary<int, string> m_dic;

			public DictionaryEnumConverter(string[] names, T[] values)
			{
				m_dic = new Dictionary<int, string>(names.Length);
				for (var j = 0; j < names.Length; j++)
					m_dic.Add(Convert.ToInt32(values[j], null), names[j]);
			}

			public override string ToStringInternal(int value)
			{
				string n;
				return m_dic.TryGetValue(value, out n) ? n : value.ToString(CultureInfo.InvariantCulture);
			}

			public override int ParseInternal(string value, bool ignoreCase, bool parseNumber)
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

			public override bool TryParseInternal(string value, bool ignore_case, bool parse_number, out int result)
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
			private static readonly string[] Seps = new[] { "|" };
			private readonly uint[] m_values;

			public FlagsEnumConverter(string[] names, T[] values)
				:base(names, values)
			{
				m_values = new uint[values.Length];
				for (var i = 0; i < values.Length; i++)
					m_values[i] = values[i].ToUInt32(null);
			}

			public override string ToStringInternal(int value)
			{
				string n;
				if (m_dic.TryGetValue(value, out n)) return n;
				var sb = new StringBuilder();
				const string sep = ", ";
				uint uval;
				unchecked
				{
					uval = (uint)value;

					for (int i = m_values.Length - 1; i >= 0; i--)
					{
						uint v = m_values[i];
						if (v == 0) continue;
						if ((v & uval) == v)
						{
							uval &= ~v;
							sb.Insert(0, sep).Insert(0, m_dic[(int)v]);
						}
					}
				}
				return uval == 0 && sb.Length > sep.Length ? sb.ToString(0, sb.Length - sep.Length) : value.ToString(CultureInfo.InvariantCulture);
			}

			public override int ParseInternal(string value, bool ignoreCase, bool parseNumber)
			{
				var parts = value.Split(Seps, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 1) return base.ParseInternal(value, ignoreCase, parseNumber);
				var val = 0;
				foreach (var part in parts)
				{
					var t = base.ParseInternal(part.Trim(), ignoreCase, parseNumber);
					val |= t;
				}
				return val;
			}

			public override bool TryParseInternal(string value, bool ignore_case, bool parse_number, out int result)
			{
				string[] parts = value.Split(Seps, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length == 1) return base.TryParseInternal(value, ignore_case, parse_number, out result);
				int val = 0;
				for (int i = 0; i < parts.Length; i++)
				{
					string part = parts[i];
					int t;
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
		public static string ToString(int value)
		{
			return Converter.ToStringInternal(value);
		}

		/// <summary>Converts enum value to string</summary>
		/// <returns>If <paramref name="value"/> is defined, the enum member name; otherwise the string representation of the <paramref name="value"/>.
		/// If <see cref="FlagsAttribute"/> is applied, can return comma-separated list of values</returns>
		public static string ToString(T value)
		{
			return Converter.ToStringInternal(value.ToInt32(null));
		}

		public static T Parse(string value, bool ignore_case = false, bool parse_numeric = true)
		{
			return (T) Enum.ToObject(typeof(T), Converter.ParseInternal(value, ignore_case, parse_numeric));
		}

		public static bool TryParse(string value, bool ignoreCase, bool parseNumeric, out T result)
		{
			int ir;
			var b = Converter.TryParseInternal(value, ignoreCase, parseNumeric, out ir);
			result = (T) Enum.ToObject(typeof(T), ir);
			return b;
		}

		public static bool TryParse(string value, bool ignoreCase, out T result)
		{
			int ir;
			var b = Converter.TryParseInternal(value, ignoreCase, true, out ir);
			result = (T)Enum.ToObject(typeof(T), ir);
			return b;
		}

		public static bool TryParse(string value, out T result)
		{
			int ir;
			bool b = Converter.TryParseInternal(value, false, true, out ir);
			result = (T)Enum.ToObject(typeof(T), ir);
			return b;
		}

		/// <summary>Returns the next enum value after 'value'. Note: this is really enum abuse. Use sparingly</summary>
		public static T Cycle(T src)
		{
			T[] arr = (T[])Enum.GetValues(typeof(T));
			int i = Array.IndexOf(arr, src) + 1;
			return (i >= 0 && i < arr.Length) ? arr[i] : arr[0];
		}

		/// <summary>Returns all values of the enum as a collection</summary>
		public static IEnumerable<T> Values
		{
			get { return Enum.GetValues(typeof(T)).Cast<T>(); }
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

			[Test] public static void TestToString()
			{
				Assert.AreEqual(ArrayEnum.One   .ToString(), ArrayEnum.One   .ToStringFast());
				Assert.AreEqual(ArrayEnum.Two   .ToString(), ArrayEnum.Two   .ToStringFast());
				Assert.AreEqual(ArrayEnum.Three .ToString(), ArrayEnum.Three .ToStringFast());

				Assert.AreEqual(SparceEnum.One  .ToString(), SparceEnum.One  .ToStringFast());
				Assert.AreEqual(SparceEnum.Four .ToString(), SparceEnum.Four .ToStringFast());
				Assert.AreEqual(SparceEnum.Ten  .ToString(), SparceEnum.Ten  .ToStringFast());

				var f = FlagsEnum.A;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
				f = FlagsEnum.A|FlagsEnum.B;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
				f = FlagsEnum.A|FlagsEnum.C;
				Assert.AreEqual(f.ToString(), f.ToStringFast());
				f = FlagsEnum.A|FlagsEnum.B|FlagsEnum.C;
				Assert.AreEqual(f.ToString(), f.ToStringFast());

				Assert.AreEqual("Some Compound Name", Tags.SomeCompoundName.ToPrettyString());
				Assert.AreEqual("ABBR Value 01", Tags.ABBRValue01.ToPrettyString());
			}
		}
	}
}
#endif
