//***************************************************
// UnitsAttribute
//  Copyright (c) Rylogic Ltd 2009
//***************************************************

using System;
using System.Reflection;

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

	// A custom attribute for adding units to a value
	public sealed class UnitsAttribute :Attribute
	{
		// 'scale' is the factor to multiply the associated value
		// by to get it's value in units of 'units'
		public UnitsAttribute(string units)                                     { Units = units; Scale = 1.0;   DecimalPlaces = 0; }
		public UnitsAttribute(string units, double scale)                       { Units = units; Scale = scale; DecimalPlaces = 0; }
		public UnitsAttribute(string units, double scale, int decimal_places)   { Units = units; Scale = scale; DecimalPlaces = decimal_places; }
		public string Units                                                     { get; private set; }
		public double Scale                                                     { get; private set; }
		public int    DecimalPlaces                                             { get; private set; }
	}

	// Helper class for accessing the units attribute
	public static class UnitsAttr
	{
		// Return the units attributes for a field
		public static UnitsAttribute[] Get(FieldInfo fi)             { return fi.GetCustomAttributes(typeof(UnitsAttribute), false) as UnitsAttribute[]; }
		public static UnitsAttribute[] Get(PropertyInfo pi)          { return pi.GetCustomAttributes(typeof(UnitsAttribute), false) as UnitsAttribute[]; }
		public static UnitsAttribute[] Get(Type type)                { return type.GetCustomAttributes(typeof(UnitsAttribute), false) as UnitsAttribute[]; }
		public static UnitsAttribute[] Get(Enum value)               { return Get(value.GetType().GetField(value.ToString())); }

		// Return a string representation of the units
		// Use: Units.Str(typeof(MyType).GetField("field_name"))
		private static string Str(UnitsAttribute[] attrs)            { return (attrs == null || attrs.Length == 0) ? "" : attrs[0].Units; }
		public static string Str(FieldInfo fi)                       { return Str(Get(fi)); }
		public static string Str(PropertyInfo pi)                    { return Str(Get(pi)); }
		public static string Str(Type type)                          { return Str(Get(type)); }
		public static string Str(Enum value)                         { return Str(value.GetType().GetField(value.ToString())); }

		// Return the units string in the form " (units)"
		private static string StrWithBrackets(string units)          { return string.IsNullOrEmpty(units) ? "" : " ("+units+")"; }
		public static string StrWithBrackets(FieldInfo fi)           { return StrWithBrackets(Str(fi)); }
		public static string StrWithBrackets(PropertyInfo pi)        { return StrWithBrackets(Str(pi)); }
		public static string StrWithBrackets(Type type)              { return StrWithBrackets(Str(type)); }
		public static string StrWithBrackets(Enum value)             { return StrWithBrackets(value.GetType().GetField(value.ToString())); }

		// Return the scaling factor associated with the units
		private static double Scale(UnitsAttribute[] attrs)          { return (attrs == null || attrs.Length == 0) ? 1.0 : attrs[0].Scale; }
		public static double Scale(FieldInfo fi)                     { return Scale(Get(fi)); }
		public static double Scale(PropertyInfo pi)                  { return Scale(Get(pi)); }
		public static double Scale(Type type)                        { return Scale(Get(type)); }
		public static double Scale(Enum value)                       { return Scale(value.GetType().GetField(value.ToString())); }

		// Return the number of decimal places associated with a unit
		private static int DecimalPlaces(UnitsAttribute[] attrs)     { return (attrs == null || attrs.Length == 0) ? 0 : attrs[0].DecimalPlaces; }
		public static int DecimalPlaces(FieldInfo fi)                { return DecimalPlaces(Get(fi)); }
		public static int DecimalPlaces(PropertyInfo pi)             { return DecimalPlaces(Get(pi)); }
		public static int DecimalPlaces(Type type)                   { return DecimalPlaces(Get(type)); }
		public static int DecimalPlaces(Enum value)                  { return DecimalPlaces(value.GetType().GetField(value.ToString())); }
	}
}
