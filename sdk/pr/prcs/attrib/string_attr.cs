//***************************************************
// String Attribute
//  Copyright © Rylogic Ltd 2009
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using pr.attrib;

namespace pr.attrib
{
	// Allows a string to be attached to an enum value or type property

	/// <summary>An attribute for adding a string to a field, property, or enum value</summary>
	[AttributeUsage(AttributeTargets.Enum|AttributeTargets.Property|AttributeTargets.Field)]
	public sealed class StringAttribute :Attribute
	{
		private readonly string m_str;
		public StringAttribute(string str) { m_str = str; }
		public string Str                  { get {return m_str;} }
	}

	/// <summary>String Attribute access class</summary>
	public static class StringAttr
	{
		/// <summary>Return the associated string attribute for an enum value</summary>
		public static string StrAttr(this Enum e)
		{
			FieldInfo fi = e.GetType().GetField(e.ToString());
			if (fi != null)
			{
				StringAttribute attr = Attribute.GetCustomAttribute(fi, typeof(StringAttribute), false) as StringAttribute;
				return attr != null ? attr.Str : "";
			}
			return "";
		}

		/// <summary>Return the associated string attribute for a property or field</summary>
		public static string StrAttr(this object t, string name)
		{
			Type type = t.GetType();
			PropertyInfo pi = type.GetProperty(name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			if (pi != null)
			{
				StringAttribute attr = Attribute.GetCustomAttribute(pi, typeof(StringAttribute), false) as StringAttribute;
				return attr != null ? attr.Str : "";
			}
			FieldInfo fi = type.GetField(name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			if (fi != null)
			{
				StringAttribute attr = Attribute.GetCustomAttribute(fi, typeof(StringAttribute), false) as StringAttribute;
				return attr != null ? attr.Str : "";
			}
			return "";
		}
		
		/// <summary>Return the Description attribute string for a member of an enum</summary>
		public static string DescAttr(this Enum e)
		{
			FieldInfo fi = e.GetType().GetField(e.ToString());
			if (fi != null)
			{
				DescriptionAttribute da = Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
				return da != null ? da.Description : "";
			}
			return "";
		}
		
		/// <summary>Return the Description attribute string for a property of a type</summary>
		public static string DescAttr(this object t, string name)
		{
			Type type = t.GetType();
			PropertyInfo pi = type.GetProperty(name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			if (pi != null)
			{
				DescriptionAttribute attr = Attribute.GetCustomAttribute(pi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
				return attr != null ? attr.Description : "";
			}
			FieldInfo fi = type.GetField(name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			if (fi != null)
			{
				DescriptionAttribute attr = Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
				return attr != null ? attr.Description : "";
			}
			return "";
		}
		
		/// <summary>Return an array of the strings associated with an enum type</summary>
		public static string[] StringArray(Type type, bool include_empty_strings)
		{
			List<string> arr = new List<string>();
			foreach (FieldInfo fi in type.GetFields(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
			{
				StringAttribute attr = Attribute.GetCustomAttribute(fi, typeof(StringAttribute), false) as StringAttribute;
				if (attr != null && !string.IsNullOrEmpty(attr.Str)) arr.Add(attr.Str);
				else if (include_empty_strings) arr.Add("");
			}
			return arr.ToArray();
		}
	
		/// <summary>Enumerate the strings associated with an enum type</summary>
		public static IEnumerable<string> GetStrings(Type type, bool include_empty_strings)
		{
			foreach (FieldInfo fi in type.GetFields(BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
			{
				StringAttribute attr = Attribute.GetCustomAttribute(fi, typeof(StringAttribute), false) as StringAttribute;
				if (attr != null && !string.IsNullOrEmpty(attr.Str)) yield return attr.Str;
				else if (include_empty_strings) yield return "";
			}
		}
		public static IEnumerable<string> GetStrings(Type type)
		{
			return GetStrings(type, false);
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using DescriptionAttribute=System.ComponentModel.DescriptionAttribute;

	[TestFixture] internal static partial class UnitTests
	{
		private enum EType
		{
			[String("Value 0")]
			[Description("The zeroth value")]
			Value0,

			[String("Value 1")]
			[Description("The first value")]
			Value1,
		}
		private class C
		{
			[String("C Field")]    private readonly int m_field;
			[String("C Property")] private int Prop {get;set;}
			public C() { Prop = 1; m_field = 2; Prop = m_field; m_field = Prop; }
		}
		[Test] public static void TestStrAttr()
		{
			const EType e = EType.Value0;
			Assert.AreEqual("Value 0", e.StrAttr());
			Assert.AreEqual("Value 1", EType.Value1.StrAttr());
			Assert.AreEqual("The zeroth value", e.DescAttr());
			Assert.AreEqual("The first value", EType.Value1.DescAttr());
			Assert.AreEqual("", ((EType)100).StrAttr());
			Assert.AreEqual("", ((EType)100).DescAttr());

			C c = new C();
			Assert.AreEqual("C Property" ,c.StrAttr("Prop"));
			Assert.AreEqual("C Field"    ,c.StrAttr("m_field"));
			Assert.AreEqual(""           ,c.StrAttr("NotThere"));

			string[] strs = StringAttr.StringArray(typeof(EType), false);
			Assert.AreEqual("Value 0", strs[0]);
			Assert.AreEqual("Value 1", strs[1]);
		}
	}
}
#endif
