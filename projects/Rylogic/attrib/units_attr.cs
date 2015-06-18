//***************************************************
// UnitsAttribute
//  Copyright (c) Rylogic Ltd 2009
//***************************************************

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using pr.extn;
using pr.util;

namespace pr.attrib
{
	// Usage:
	//	enum EType
	//	{
	//		[Units("°C", 1)] Value0,
	//	}
	//	struct Test
	//	{
	//		[Units("m/s", 0.001)] private int m_speed_mm_p_s;
	//		[Units("kg", 0.001)]  public int Mass {get;set;}
	//		void func()
	//		{
	//			string s = Units.Str(EType.Value0);
	//			double scale = Units.Scale(typeof(Test).GetField("m_speed_mm_p_s"));
	//		}
	//	}
	//
	// EType.Value0.Label(true);
	// EType.Value0.Scale();
	// Units<Test>.Scale(x => x.Mass);

	// A custom attribute for adding units to a value
	[AttributeUsage(AttributeTargets.Enum|AttributeTargets.Property|AttributeTargets.Field)]
	public sealed class UnitsAttribute :Attribute
	{
		// 'scale' is the factor to multiply the associated value
		// by to get it's value in units of 'units'
		public UnitsAttribute(string units)                                     { Label = units; Scale = 1.0;   DecimalPlaces = 0; }
		public UnitsAttribute(string units, double scale)                       { Label = units; Scale = scale; DecimalPlaces = 0; }
		public UnitsAttribute(string units, double scale, int decimal_places)   { Label = units; Scale = scale; DecimalPlaces = decimal_places; }
		public string Label                                                     { get; private set; }
		public string LabelInBrackets                                           { get { return Label.HasValue() ? " ("+Label+")" : string.Empty; } }
		public double Scale                                                     { get; private set; }
		public int    DecimalPlaces                                             { get; private set; }
	}

	public static class UnitsAttr
	{
		/// <summary>Return the UnitsAttribute for an enum value</summary>
		private static UnitsAttribute GetAttr<TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
		{
			var fi = enum_.GetType().GetField(enum_.ToString());
			return fi.FindAttribute<UnitsAttribute>(false);
		}

		/// <summary>Returns the unit label associated with an enum value</summary>
		[DebuggerStepThrough]
		public static string Label<TEnum>(this TEnum enum_, bool in_brackets = false) where TEnum :struct ,IConvertible
		{
			var attr = GetAttr(enum_);
			if (attr == null) throw new Exception("Member does not have the UnitsAttribute");
			return in_brackets ? attr.LabelInBrackets : attr.Label;
		}

		/// <summary>Returns the unit scale associated with an enum value</summary>
		[DebuggerStepThrough]
		public static double Scale<TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
		{
			var attr = GetAttr(enum_);
			if (attr == null) throw new Exception("Member does not have the UnitsAttribute");
			return attr.Scale;
		}

		/// <summary>Returns the unit decimal places associated with an enum value</summary>
		[DebuggerStepThrough]
		public static int DecimalPlaces<TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
		{
			var attr = GetAttr(enum_);
			if (attr == null) throw new Exception("Member does not have the UnitsAttribute");
			return attr.DecimalPlaces;
		}
	}

	public static class Units<T>
	{
		/// <summary>Returns the unit label associated with a member</summary>
		[DebuggerStepThrough]
		public static string Label<Ret>(Expression<Func<T,Ret>> expression, bool in_brackets = false)
		{
			var attr = R<T>.Units(expression);
			return in_brackets ? attr.LabelInBrackets : attr.Label;
		}

		/// <summary>Returns the unit label associated with a member</summary>
		[DebuggerStepThrough]
		public static string Label(Expression<Action<T>> expression, bool in_brackets = false)
		{
			var attr = R<T>.Units(expression);
			return in_brackets ? attr.LabelInBrackets : attr.Label;
		}

		/// <summary>Returns the unit scale associated with a member</summary>
		[DebuggerStepThrough]
		public static double Scale<Ret>(Expression<Func<T,Ret>> expression)
		{
			return R<T>.Units(expression).Scale;
		}

		/// <summary>Returns the unit scale associated with a member</summary>
		[DebuggerStepThrough]
		public static double Scale(Expression<Action<T>> expression)
		{
			return R<T>.Units(expression).Scale;
		}

		/// <summary>Returns the unit decimal places associated with a member</summary>
		[DebuggerStepThrough]
		public static int DecimalPlaces<Ret>(Expression<Func<T,Ret>> expression)
		{
			return R<T>.Units(expression).DecimalPlaces;
		}

		/// <summary>Returns the unit decimal places associated with a member</summary>
		[DebuggerStepThrough]
		public static int DecimalPlaces(Expression<Action<T>> expression)
		{
			return R<T>.Units(expression).DecimalPlaces;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using attrib;

	[TestFixture] public class TestUnitsAttr
	{
		public class Whatsit
		{
			[Units("m", 1.0)]     public double Distance { get { return 2.0; } }
			[Units("m/s", 0.001)] public double Speed { get { return 3.0; } }
		}

		[Test] public void Units()
		{
			Assert.AreEqual("m", Units<Whatsit>.Label(x => x.Distance));
			Assert.AreEqual("m/s", Units<Whatsit>.Label(x => x.Speed));
				
			Assert.AreEqual(1.0, Units<Whatsit>.Scale(x => x.Distance));
			Assert.AreEqual(0.001, Units<Whatsit>.Scale(x => x.Speed));

			Assert.AreEqual(0, Units<Whatsit>.DecimalPlaces(x => x.Distance));
			Assert.AreEqual(0, Units<Whatsit>.DecimalPlaces(x => x.Speed));
		}
	}
}
#endif
