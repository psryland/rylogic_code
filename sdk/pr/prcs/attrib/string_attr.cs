//***************************************************
// String Attribute
//  Copyright © Rylogic Ltd 2009
//***************************************************

using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using pr.common;
using pr.util;
using pr.extn;

namespace pr.attrib
{
	// Allows a string to be attached to an enum value or type property

	/// <summary>An attribute for adding a string to a field, property, or enum value</summary>
	[AttributeUsage(AttributeTargets.Enum|AttributeTargets.Property|AttributeTargets.Field)]
	public sealed class StringAttribute :Attribute
	{
		private readonly string m_str;
		public StringAttribute(string str) { m_str = str; }
		public string Str                  { get { return m_str; } }
	}

	/// <summary>String Attribute access class</summary>
	public static class StringAttr
	{
		// Reflecting on attributes is slow => caching
		private static readonly Cache<string,object> m_str_cache  = new Cache<string,object>();
		private static readonly Cache<string,object> m_desc_cache = new Cache<string,object>();

		/// <summary>A cache key for an enum value</summary>
		private static string Key<TEnum>(TEnum enum_) where TEnum :struct ,IConvertible
		{
			return enum_.GetType().Name + "." + enum_.ToStringFast();
		}

		/// <summary>A cache key for member of an object</summary>
		private static string Key(object obj, string member_name)
		{
			return obj.GetType().Name + "." + member_name;
		}

		/// <summary>A cache key for a type</summary>
		private static string Key(Type ty)
		{
			return ty.Name;
		}

		#region String Attribute

		/// <summary>Return the associated string attribute for an enum value</summary>
		public static string StrAttr<TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
		{
			return (string)m_str_cache.Get(Key(enum_), e =>
				{
					var fi = enum_.GetType().GetField(enum_.ToStringFast());
					if (fi == null) return string.Empty;
					var attr = Attribute.GetCustomAttribute(fi, typeof(StringAttribute), false) as StringAttribute;
					return attr != null ? attr.Str : string.Empty;
				});
		}

		/// <summary>Return the associated string attribute for a property or field</summary>
		public static string StrAttr(object obj, string member_name)
		{
			return (string)m_str_cache.Get(Key(obj,member_name), k =>
				{
					var type = obj.GetType();
					var pi = type.GetProperty(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					if (pi != null)
					{
						var attr = Attribute.GetCustomAttribute(pi, typeof(StringAttribute), false) as StringAttribute;
						return attr != null ? attr.Str : string.Empty;
					}
					var fi = type.GetField(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					if (fi != null)
					{
						var attr = Attribute.GetCustomAttribute(fi, typeof(StringAttribute), false) as StringAttribute;
						return attr != null ? attr.Str : string.Empty;
					}
					return string.Empty;
				});
		}

		/// <summary>The string attribute associated with a property or field</summary>
		public static string StrAttr<T,Ret>(this T obj, Expression<Func<T,Ret>> expression)
		{
			return StrAttr(obj, Reflect<T>.MemberName(expression));
		}

		/// <summary>Return an array of the strings associated with an enum type</summary>
		public static string[] AllStrAttr<TEnum>(this TEnum type) where TEnum :struct ,IConvertible
		{
			return (string[])m_str_cache.Get(Key(type), k =>
				{
					return Enum<TEnum>.Values.Select(e => e.StrAttr()).Where(s => s.HasValue()).ToArray();
				});
		}

		/// <summary>Return an array of the strings associated with an enum type</summary>
		public static string[] AllStrAttr(this Type type)
		{
			return (string[])m_str_cache.Get(Key(type), k =>
				{
					return type.AllMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.Select(mi => mi.GetCustomAttributes(typeof(StringAttribute),false).FirstOrDefault() as StringAttribute)
						.Where(attr => attr != null && attr.Str.HasValue())
						.Select(attr => attr.Str)
						.ToArray();
				});
		}

		#endregion

		#region Description Attribute

		/// <summary>Return the description attribute string for a member of an enum</summary>
		public static string DescAttr<TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
		{
			return (string)m_desc_cache.Get(Key(enum_), k =>
				{
					var fi = enum_.GetType().GetField(enum_.ToStringFast());
					if (fi == null) return string.Empty;
					var attr = Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
					return attr != null ? attr.Description : string.Empty;
				});
		}

		/// <summary>Return the description attribute string for a property of a type</summary>
		public static string DescAttr(this object obj, string member_name)
		{
			return (string)m_desc_cache.Get(Key(obj, member_name), k =>
				{
					var type = obj.GetType();
					var pi = type.GetProperty(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					if (pi != null)
					{
						var attr = Attribute.GetCustomAttribute(pi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
						return attr != null ? attr.Description : string.Empty;
					}
					var fi = type.GetField(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
					if (fi != null)
					{
						var attr = Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
						return attr != null ? attr.Description : string.Empty;
					}
					return string.Empty;
				});
		}

		/// <summary>The description attribute associated with a property or field</summary>
		public static string DescAttr<T,Ret>(this T obj, Expression<Func<T,Ret>> expression)
		{
			return DescAttr(obj, Reflect<T>.MemberName(expression));
		}

		#endregion
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using attrib;

	[TestFixture] internal static partial class UnitTests
	{
		// ReSharper disable UnusedMember.Local
		internal static class TestStringAttr
		{
			private enum EType
			{
				[String("Value 0")]
				[System.ComponentModel.DescriptionAttribute("The zeroth value")]
				Value0,

				[String("Value 1")]
				[System.ComponentModel.DescriptionAttribute("The first value")]
				Value1,

				// no attributes
				Value2,
			}
			private class C
			{
				[String("Field Str")]
				[System.ComponentModel.DescriptionAttribute("Field Desc")]
				public readonly int m_field;

				[String("Prop Str")]
				[System.ComponentModel.DescriptionAttribute("Prop Desc")]
				public int Prop { get; private set; }

				public string NoAttr { get; private set; }
				public C()
				{
					Prop    = 1;
					m_field = 2;
					NoAttr  = "Blah";
				}
			}
			[Test] public static void StrAttr1()
			{
				const EType e = EType.Value0;
				Assert.AreEqual("Value 0"          ,e.StrAttr());
				Assert.AreEqual("Value 1"          ,EType.Value1.StrAttr());
				Assert.AreEqual("The zeroth value" ,e.DescAttr());
				Assert.AreEqual("The first value"  ,EType.Value1.DescAttr());
				Assert.AreEqual(string.Empty       ,((EType)100).StrAttr());
				Assert.AreEqual(string.Empty       ,((EType)100).DescAttr());
			}
			[Test] public static void StrAttr2()
			{
				var c = new C();
				Assert.AreEqual("Field Str"  ,c.StrAttr(x => x.m_field));
				Assert.AreEqual("Prop Str"   ,c.StrAttr(x => x.Prop));
				Assert.AreEqual(string.Empty ,c.StrAttr(x => x.NoAttr));
				Assert.AreEqual("Field Desc" ,c.DescAttr(x => x.m_field));
				Assert.AreEqual("Prop Desc"  ,c.DescAttr(x => x.Prop));
				Assert.AreEqual(string.Empty ,c.DescAttr(x => x.NoAttr));
				Assert.AreEqual(string.Empty ,StringAttr.StrAttr(c, "NotThere"));
			}
			[Test] public static void StrAttr3()
			{
				var strs = typeof(EType).AllStrAttr();
				Assert.AreEqual(2, strs.Length);
				Assert.Contains("Value 0", strs);
				Assert.Contains("Value 1", strs);
			}
			[Test] public static void StrAttr4()
			{
				var strs = typeof(C).AllStrAttr();
				Assert.AreEqual(2, strs.Length);
				Assert.Contains("Field Str", strs);
				Assert.Contains("Prop Str", strs);
			}
		}
		// ReSharper restore UnusedMember.Local
	}
}
#endif
