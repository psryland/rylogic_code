#if DEBUG
#define UNITS_ENABLED
#endif

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Rylogic.Container;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	[DebuggerDisplay("{ToString(true),nq}")]
	public struct Unit<T> : IComparable<Unit<T>>, IComparable<T>, IComparable where T : IComparable
	{
		public Unit(T value, int unit)
		{
			Value = value;
			UnitId = unit;
		}
		public Unit(XElement node)
		{
			this = Parse(node.Value);
		}
		public XElement ToXml(XElement node)
		{
			node.Value = UnitId != Unit_.NoUnitsId ? $"{Value} {Unit_.Types[UnitId]}" : $"{Value}";
			return node;
		}

		internal T Value;
		internal int UnitId;

		#region Operators
		// Constants
		public static readonly Unit<T> MaxValue = Operators<T>.MaxValue;
		public static readonly Unit<T> MinValue = Operators<T>.MinValue;

		// Conversion
		[DebuggerStepThrough] public static implicit operator T(Unit<T> u)
		{
			return u.Value;
		}
		[DebuggerStepThrough] public static implicit operator Unit<T>(T v)
		{
			return new Unit<T>(v, Unit_.NoUnitsId);
		}

		// Unary operators
		[DebuggerStepThrough] public static Unit<T> operator +(Unit<T> lhs)
		{
			return new Unit<T>(Operators<T>.Plus(lhs.Value), lhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator -(Unit<T> lhs)
		{
			return new Unit<T>(Operators<T>.Neg(lhs.Value), lhs.UnitId);
		}

		// Binary operators
		[DebuggerStepThrough] public static Unit<T> operator +(Unit<T> lhs, Unit<T> rhs)
		{
			// Allow add to scalar zero
			if (rhs.IsScalarZero) return lhs;
			if (lhs.IsScalarZero) return rhs;
			if (lhs.UnitId != rhs.UnitId) throw new Exception("Unit types don't match");
			return new Unit<T>(Operators<T>.Add(lhs.Value, rhs.Value), lhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator +(T lhs, Unit<T> rhs)
		{
			return new Unit<T>(Operators<T>.Add(lhs, rhs.Value), rhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator +(Unit<T> lhs, T rhs)
		{
			return new Unit<T>(Operators<T>.Add(lhs.Value, rhs), lhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator -(Unit<T> lhs, Unit<T> rhs)
		{
			// Allow subtract with scalar zero
			if (rhs.IsScalarZero) return lhs;
			if (lhs.IsScalarZero) return -rhs;
			if (lhs.UnitId != rhs.UnitId) throw new Exception("Unit types don't match");
			return new Unit<T>(Operators<T>.Sub(lhs.Value, rhs.Value), lhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator -(T lhs, Unit<T> rhs)
		{
			return new Unit<T>(Operators<T>.Sub(lhs, rhs.Value), rhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator -(Unit<T> lhs, T rhs)
		{
			return new Unit<T>(Operators<T>.Sub(lhs.Value, rhs), lhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator *(Unit<T> lhs, Unit<T> rhs)
		{
			var id = Unit_.CombineUnits(lhs.UnitId, rhs.UnitId, divide: false);
			return new Unit<T>(Operators<T>.Mul(lhs.Value, rhs.Value), id);
		}
		[DebuggerStepThrough] public static Unit<T> operator *(T lhs, Unit<T> rhs)
		{
			return new Unit<T>(Operators<T>.Mul(lhs, rhs.Value), rhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator *(Unit<T> lhs, T rhs)
		{
			return new Unit<T>(Operators<T>.Mul(lhs.Value, rhs), lhs.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> operator /(Unit<T> lhs, Unit<T> rhs)
		{
			var id = Unit_.CombineUnits(lhs.UnitId, rhs.UnitId, divide: true);
			return new Unit<T>(Operators<T>.Div(lhs.Value, rhs.Value), id);
		}
		[DebuggerStepThrough] public static Unit<T> operator /(T lhs, Unit<T> rhs)
		{
			var id = Unit_.CombineUnits(Unit_.NoUnitsId, rhs.UnitId, divide: true);
			return new Unit<T>(Operators<T>.Div(lhs, rhs.Value), id);
		}
		[DebuggerStepThrough] public static Unit<T> operator /(Unit<T> lhs, T rhs)
		{
			return new Unit<T>(Operators<T>.Div(lhs.Value, rhs), lhs.UnitId);
		}

		// Equality operators
		[DebuggerStepThrough] public static bool operator ==(Unit<T> lhs, Unit<T> rhs)
		{
			// Comparing different units is an error. A == B cannot have
			// different behaviour depending on whether checking is enabled or not.
			if (lhs.UnitId != rhs.UnitId && !lhs.IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return lhs.Value.CompareTo(rhs.Value) == 0;
		}
		[DebuggerStepThrough] public static bool operator !=(Unit<T> lhs, Unit<T> rhs)
		{
			// Comparing different units is an error. A == B cannot have
			// different behaviour depending on whether checking is enabled or not.
			if (lhs.UnitId != rhs.UnitId && !lhs.IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return lhs.Value.CompareTo(rhs.Value) != 0;
		}
		[DebuggerStepThrough] public static bool operator <(Unit<T> lhs, Unit<T> rhs)
		{
			if (lhs.UnitId != rhs.UnitId && !lhs.IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return lhs.Value.CompareTo(rhs.Value) < 0;
		}
		[DebuggerStepThrough] public static bool operator >(Unit<T> lhs, Unit<T> rhs)
		{
			if (lhs.UnitId != rhs.UnitId && !lhs.IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return lhs.Value.CompareTo(rhs.Value) > 0;
		}
		[DebuggerStepThrough] public static bool operator <=(Unit<T> lhs, Unit<T> rhs)
		{
			if (lhs.UnitId != rhs.UnitId && !lhs.IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return lhs.Value.CompareTo(rhs.Value) <= 0;
		}
		[DebuggerStepThrough] public static bool operator >=(Unit<T> lhs, Unit<T> rhs)
		{
			if (lhs.UnitId != rhs.UnitId && !lhs.IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return lhs.Value.CompareTo(rhs.Value) >= 0;
		}
		#endregion

		/// <summary>ToString</summary>
		public override string ToString()
		{
			return Value.ToString();
		}
		public string ToString(bool include_units)
		{
			var str = Value.ToString();
			return include_units ? $"{str} {Unit_.Types[UnitId]}" : str;
		}
		public string ToString(IFormatProvider fp, bool include_units = false)
		{
			var str = Operators<T>.ToString(Value, fp);
			return include_units ? $"{str} {Unit_.Types[UnitId]}" : str;
		}
		public string ToString(string fmt, bool include_units = false)
		{
			var str = Operators<T>.ToString(Value, fmt);
			return include_units ? $"{str} {Unit_.Types[UnitId]}" : str;
		}
		public string ToString(string fmt, IFormatProvider fp, bool include_units = false)
		{
			var str = Operators<T>.ToString(Value, fmt, fp);
			return include_units ? $"{str} {Unit_.Types[UnitId]}" : str;
		}
		public string ToString(int significant_digits, bool include_units = false)
		{
			var str =
				Value is float f ? f.ToString(significant_digits) :
				Value is double d ? d.ToString(significant_digits) :
				Value is decimal m ? m.ToString(significant_digits) :
				throw new Exception($"ToString(signficant_digit) not available for type:{Value.GetType().Name}");
			return include_units ? $"{str} {Unit_.Types[UnitId]}" : str;
		}

		/// <summary>Parse a unit from a string</summary>
		public static Unit<T> Parse(string str)
		{
			if (str == null)
				throw new ArgumentNullException();

			var parts = str.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 1 && parts.Length != 2)
				throw new FormatException();

			return new Unit<T>(Operators<T>.Parse(parts[0]), parts.Length == 2 ? Unit_.UnitId(parts[1]) : Unit_.NoUnitsId);
		}
		public static bool TryParse(string str, out Unit<T> unit)
		{
			unit = default(Unit<T>);

			if (str == null)
				return false;

			var parts = str.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 1 && parts.Length != 2)
				return false;

			if (!Operators<T>.TryParse(parts[0], out unit.Value))
				return false;

			if (parts.Length == 2)
				unit.UnitId = Unit_.UnitId(parts[1]);

			return true;
		}

		/// <summary></summary>
		[DebuggerStepThrough] public bool Equals(Unit<T> rhs)
		{
			// Comparing different units is an error. A == B cannot have
			// different behaviour depending on whether checking is enabled or not.
			if (UnitId != rhs.UnitId && !IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return Value.Equals(rhs.Value);
		}
		[DebuggerStepThrough] public override bool Equals(object rhs)
		{
			return rhs is Unit<T> && Equals((Unit<T>)rhs);
		}
		[DebuggerStepThrough] public override int GetHashCode()
		{
			return new { Value, UnitId }.GetHashCode();
		}
		[DebuggerStepThrough] public int CompareTo(Unit<T> rhs)
		{
			if (UnitId != rhs.UnitId && !IsScalarZero && !rhs.IsScalarZero) throw new Exception("Unit types don't match");
			return Value.CompareTo(rhs.Value);
		}
		[DebuggerStepThrough] public int CompareTo(T rhs)
		{
			return Value.CompareTo(rhs);
		}
		int IComparable.CompareTo(object obj)
		{
			return CompareTo((Unit<T>)obj);
		}

		/// <summary>True if this unit is '0' with no units</summary>
		private bool IsScalarZero
		{
			get { return UnitId == Unit_.NoUnitsId && Equals(Value, default(T)); }
		}

		/// <summary>Check units</summary>
		public bool IsUnit(int unit_id) => UnitId == unit_id;
		public bool IsUnit(string unit) => IsUnit(Unit_.UnitId(unit));
	}

	/// <summary>Utility methods for 'Unit<T>'</summary>
	public static class Unit_
	{
		public const int NoUnitsId = 0;

		/// <summary>A map from unit type to string representation</summary>
		public static BiDictionary<string, int> Types = new BiDictionary<string, int>() { [NoUnitsId] = string.Empty };
		public static int UnitTypeId = NoUnitsId;

		/// <summary>A cache of the result of combining two types</summary>
		private static ConcurrentDictionary<ulong, int> m_cache_combined_types = new ConcurrentDictionary<ulong, int>();

		/// <summary>Return the unique id assigned to a unit</summary>
		public static int UnitId(string unit_name)
		{
#if UNITS_ENABLED
			return Types.GetOrAdd(unit_name, s => ++UnitTypeId);
#else
			return NoUnitsId;
#endif
		}

		/// <summary>Combine unit ids into a new unit</summary>
		public static int CombineUnits(int lhs, int rhs, bool divide)
		{
#if UNITS_ENABLED
			// Unit ids are always positive, so the sign bit is free
			Debug.Assert(lhs >= 0 && rhs >= 0);

			// Combine the units into a cache key.
			// If 'divide' is false then the order doesn't matter so,
			// put the smallest index at the top
			var x = (ulong)lhs;
			var y = (ulong)rhs;
			var key =
				divide ? (x << 32) | y | (1UL << 63) :
				(x < y) ? (x << 32) | y :
						  (y << 32) | x;

			return m_cache_combined_types.GetOrAdd(key, k =>
			{
				return UnitId(CombineUnits(Types[lhs], Types[rhs], divide));
			});
#else
			return NoUnitsId;
#endif
		}

		/// <summary>Combine unit strings into a new unit</summary>
		public static string CombineUnits(string lhs, string rhs, bool divide)
		{
			// Units have the form: A².B/C.D
			//  i.e. sorted left to right alphabetically.
			//  A.A = A², B.B.B = B³, C.C.C.C = C^4, D.D.D.D.D = D^5, etc
			// There is only one divide symbol per unit.
			// Units are delimited by '.' characters
			// If 'divide' is true, the new unit will be the result of 'Types[lhs] / Types[rhs]'
			// otherwise it will be the result of 'Types[lhs] . Types[rhs]'

			// The powers of each unit type
			var powers = new Accumulator<string, int>();

			// Characters that are allowed for unit strings
			Func<string, int, bool> IsUnitChar = (string str, int idx) =>
			{
				// '1' is a special case. It's a unit char if not followed by [./²³^] or the end of string
				var ch = str[idx];
				if (ch == '1' && idx + 1 == str.Length) return false;
				if (ch == '1' && idx + 1 < str.Length) ch = str[idx + 1];
				return ch != '.' && ch != '/' && ch != '²' && ch != '³' && ch != '^';
			};

			// Decomposes a unit string into powers of each unit type
			Action<string, int> Decompose = (str, sign) =>
			{
				// Parse the unit string to determine the counts of each unit type
				var prev = string.Empty;
				for (int s, e = 0; e != str.Length;)
				{
					// A².B^5/C.D - extract a unit
					for (s = e; e != str.Length && IsUnitChar(str, e); ++e) { }
					if (s != e) powers[prev = str.Substring(s, e - s)] += sign;

					// Find the power of the unit
					if (e == str.Length) break;
					else if (str[e] == '1') { ++e; }
					else if (str[e] == '²') { powers[prev] += 1 * sign; ++e; }
					else if (str[e] == '³') { powers[prev] += 2 * sign; ++e; }
					else if (str[e] == '^')
					{
						for (s = ++e; e != str.Length && char.IsDigit(str[e]); ++e) { }
						powers[prev] += (int.Parse(str.Substring(s, e - s)) - 1) * sign;
					}

					// Multiply/Divide separator
					if (e == str.Length) break;
					else if (str[e] == '.') { ++e; }
					else if (str[e] == '/') { ++e; sign = -sign; }
				}
			};

			// Counts of each unit type
			Decompose(lhs, +1);
			Decompose(rhs, divide ? -1 : +1);

			// Helper for converting the power of a unit to a string
			Func<int, string> power_value = i => i == 1 ? "" : i == 2 ? "²" : i == 3 ? "³" : $"^{i}";

			// Recompose the powers back into a unit string
			var numer = new StringBuilder();
			var denom = new StringBuilder();
			foreach (var unit in powers.OrderBy(x => x.Key))
			{
				if (unit.Value > 0)
					numer.Append(unit.Key).Append(power_value(+unit.Value)).Append(".");
				if (unit.Value < 0)
					denom.Append(unit.Key).Append(power_value(-unit.Value)).Append(".");
			}
			numer.TrimEnd('.');
			denom.TrimEnd('.');

			if (denom.Length == 0) return numer.ToString();
			if (numer.Length == 0) return "1/" + denom.ToString();
			return numer.Append('/').Append(denom).ToString();
		}

		/// <summary>Convert this raw value into a type with a known unit</summary>
		[DebuggerStepThrough] public static Unit<T> _<T>(this T x, string unit) where T : IComparable
		{
			return new Unit<T>(x, UnitId(unit));
		}
		[DebuggerStepThrough] public static Unit<T> _<T>(this T x, Unit<T> unit) where T : IComparable
		{
			return new Unit<T>(x, unit.UnitId);
		}
		[DebuggerStepThrough] public static Unit<T> _<T>(this T x) where T : IComparable
		{
			return new Unit<T>(x, NoUnitsId);
		}

		/// <summary>Cast one unit to another</summary>
		[DebuggerStepThrough] public static Unit<T> _<T>(this Unit<T> x, string unit) where T : IComparable
		{
			return new Unit<T>(x, UnitId(unit));
		}
		[DebuggerStepThrough] public static Unit<T> _<T>(this Unit<T> x, Unit<T> unit) where T : IComparable
		{
			return new Unit<T>(x, unit.UnitId);
		}

		/// <summary>True if this value is in the range [beg,end) or [end,beg) (whichever is a positive range)</summary>
		public static bool Within<T>(this Unit<T> x, Unit<T> beg, Unit<T> end) where T : IComparable
		{
			return beg <= end
				? x >= beg && x < end
				: x >= end && x < beg;
		}
		public static bool Within<T>(this Unit<T>? x, Unit<T> beg, Unit<T> end) where T : IComparable
		{
			return x.HasValue && x.Value.Within(beg, end);
		}
		public static bool WithinInclusive<T>(this Unit<T> x, Unit<T> beg, Unit<T> end) where T : IComparable
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive<T>(this Unit<T>? x, Unit<T> beg, Unit<T> end) where T : IComparable
		{
			return x.HasValue && x.Value.WithinInclusive(beg, end);
		}

		/// <summary>ToString</summary>
		public static string ToString<T>(this Unit<T>? x, bool include_units) where T : IComparable
		{
			return x?.ToString(include_units) ?? string.Empty;
		}
		public static string ToString<T>(this Unit<T>? x, IFormatProvider fp, bool include_units = false) where T : IComparable
		{
			return x?.ToString(fp, include_units) ?? string.Empty;
		}
		public static string ToString<T>(this Unit<T>? x, string fmt, bool include_units = false) where T : IComparable
		{
			return x?.ToString(fmt, include_units) ?? string.Empty;
		}
		public static string ToString<T>(this Unit<T>? x, string fmt, IFormatProvider fp, bool include_units = false) where T : IComparable
		{
			return x?.ToString(fmt, fp, include_units) ?? string.Empty;
		}
	}
}
namespace Rylogic.Maths
{
	/// <summary>Maths overloads for Unit</summary>
	public static partial class Math_
	{
		public static Utility.Unit<long> RoundSD(Utility.Unit<long> d, int significant_digits, MidpointRounding rounding = MidpointRounding.ToEven)
		{
			return Utility.Unit_._(RoundSD(d.Value, significant_digits, rounding), d);
		}
		public static Utility.Unit<double> RoundSD(Utility.Unit<double> d, int significant_digits, MidpointRounding rounding = MidpointRounding.ToEven)
		{
			return Utility.Unit_._(RoundSD(d.Value, significant_digits, rounding), d);
		}
		public static Utility.Unit<decimal> RoundSD(Utility.Unit<decimal> d, int significant_digits, MidpointRounding rounding = MidpointRounding.ToEven)
		{
			return Utility.Unit_._(RoundSD(d.Value, significant_digits, rounding), d);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture] public class TestUnits
	{
		[Test] public void DefaultUse()
		{
			var v0 = 0.123m._("A");
			var v1 = 0.456m._("A");
			var v2 = 0.456m._("B");

			// Unary Operators
			Assert.Equal(+v0, +0.123m._("A"));
			Assert.Equal(-v0, -0.123m._("A"));

			// Binary Operators
			Assert.Equal(v0 + 0m, 0.123m._("A"));
			Assert.Equal(0m - v1, -0.456m._("A"));
			Assert.Equal(v0 + 0.456m, 0.579m._("A"));
			Assert.Equal(0.123m + v1, 0.579m._("A"));
			Assert.Equal(v0 + v1, 0.579m._("A"));
			Assert.Throws<Exception>(() => { var r1 = v0 + v2; });

			Assert.Equal(v1 - 0.123m, 0.333m._("A"));
			Assert.Equal(0.456m - v0, 0.333m._("A"));
			Assert.Equal(v1 - v0, 0.333m._("A"));
			Assert.Throws<Exception>(() => { var r1 = v2 - v0; });

			Assert.Equal(v0 * 5m, (0.123m * 5m)._("A"));
			Assert.Equal(5m * v1, (5m * 0.456m)._("A"));
			Assert.Equal(v0 * v1, (0.123m * 0.456m)._("A²"));
			Assert.Equal(v0 * v2, (0.123m * 0.456m)._("A.B"));

			Assert.Equal(v0 / 5m, (0.123m / 5m)._("A"));
			Assert.Equal(5m / v1, (5m / 0.456m)._("1/A"));
			Assert.Equal(v0 / v1, (0.123m / 0.456m)._(""));
			Assert.Equal(v0 / v2, (0.123m / 0.456m)._("A/B"));
			Assert.Equal(v2 / v0, (0.456m / 0.123m)._("B/A"));

			// Comparisons
			Assert.True(v0 == 0.123m._("A"));
			Assert.True(v0 != 0.456m._("A"));
			Assert.True(v0 < v1);
			Assert.True(v1 > v0);
			Assert.True(v0 <= 0.123m._("A"));
			Assert.True(v1 >= 0.456m._("A"));

			#if UNITS_ENABLED
			Assert.Throws<Exception>(() => { var b = 0.123._("A") == 0.123._("B"); });
			Assert.Throws<Exception>(() => { var b = 0.123._("A") != 0.123._("B"); });
			Assert.Throws<Exception>(() => { var b = v0 < v2; });
			Assert.Throws<Exception>(() => { var b = v2 > v0; });
			Assert.Throws<Exception>(() => { var b = v0 <= 0.123m._("B"); });
			Assert.Throws<Exception>(() => { var b = v1 >= 0.456m._("B"); });

			// Cast between units
			var v3 = v0._("C");
			Assert.Throws<Exception>(() => { var b = v0 == v3; });
			#endif
		}
		[Test] public void CombineUnits()
		{
			Assert.Equal(Unit_.CombineUnits("","", divide:false)                      , ""        );
			Assert.Equal(Unit_.CombineUnits("","", divide:true)                       , ""        );
			Assert.Equal(Unit_.CombineUnits("1","1", divide:false)                    , ""        );
			Assert.Equal(Unit_.CombineUnits("1","1", divide:true)                     , ""        );
			Assert.Equal(Unit_.CombineUnits("1","A", divide:false)                    , "A"       );
			Assert.Equal(Unit_.CombineUnits("1","A", divide:true)                     , "1/A"     );
			Assert.Equal(Unit_.CombineUnits("1","A.B", divide:false)                  , "A.B"     );
			Assert.Equal(Unit_.CombineUnits("1","A.B", divide:true)                   , "1/A.B"   );
			Assert.Equal(Unit_.CombineUnits("A","A", divide:false)                    , "A²"      );
			Assert.Equal(Unit_.CombineUnits("A","A", divide:true)                     , ""        );
			Assert.Equal(Unit_.CombineUnits("A.B","A", divide:false)                  , "A².B"    );
			Assert.Equal(Unit_.CombineUnits("12","12", divide:false)                  , "12²"     );
			Assert.Equal(Unit_.CombineUnits("A.B","A", divide:true)                   , "B"       );
			Assert.Equal(Unit_.CombineUnits("A.B","A.B", divide:false)                , "A².B²"   );
			Assert.Equal(Unit_.CombineUnits("A.B","A.A", divide:true)                 , "B/A"     );
			Assert.Equal(Unit_.CombineUnits("A.B/C","C", divide:false)                , "A.B"     );
			Assert.Equal(Unit_.CombineUnits("A/B.C","B/A", divide:false)              , "1/C"     );
			Assert.Equal(Unit_.CombineUnits("A³.B²/C.D^4","B³.C.D/A².C²", divide:true) , "A^5/B.D^5" );
		}
		[Test] public void Parse()
		{
			var val = 0.123m._("Spon");
			var str = val.ToString(true);
			var VAL = Unit<decimal>.Parse(str);
			Assert.Equal(val, VAL);

			var r = Unit<decimal>.TryParse(str, out VAL);
			Assert.True(r);
			Assert.Equal(val, VAL);
		}
	}
}
#endif
