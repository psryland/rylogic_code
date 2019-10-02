//***************************************************
// String Attribute
//  Copyright (c) Rylogic Ltd 2009
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Attrib
{
	/// <summary>
	/// An attribute for adding a string to a field, property, or enum value.
	/// Basically the same as the SystemComponentModel.DescriptionAttribute except
	/// that DescriptionAttribute is only allowed on methods.
	/// *WARNING* If used on enums containing members with the same value, the description
	/// is not guaranteed to match the member it is associated with.</summary>
	[AttributeUsage(AttributeTargets.Enum|AttributeTargets.Property|AttributeTargets.Field)]
	public sealed class DescAttribute :Attribute
	{
		private readonly string m_str;
		public DescAttribute(string str) { m_str = str; }
		public string Str { get { return m_str; } }
	}

	/// <summary>Access class for DescAttribute</summary>
	public static class DescAttr
	{
		// Reflecting on attributes is slow => caching
		private static readonly Cache<string, object> m_str_cache = new Cache<string, object>(1000) { ThreadSafe = true };

		/// <summary>Get the DescAttribute or DescriptionAttribute associated with a member</summary>
		public static Attribute? Find(MemberInfo? mi)
		{
			if (mi == null) return null;
			var d0 = Attribute.GetCustomAttribute(mi, typeof(DescAttribute), false) as DescAttribute;
			if (d0 != null) return d0;
			var d1 = Attribute.GetCustomAttribute(mi, typeof(DescriptionAttribute), false) as DescriptionAttribute;
			if (d1 != null) return d1;
			return null;
		}

		/// <summary>Return the description for a property or field</summary>
		public static string? Desc(Type ty, MemberInfo? mi)
		{
			if (mi == null) return null; // Return null to distinguish between Desc("") and no DescAttribute
			return (string)m_str_cache.Get(Key(ty, mi.Name), OnMiss);
			object OnMiss(object k)
			{
				var attr = Find(mi);
				if (attr is DescAttribute d0) return d0.Str;
				if (attr is DescriptionAttribute d1) return d1.Description;
				return null!; // Return null to distinguish between Desc("") and no DescAttribute
			}
		}

		/// <summary>Return the associated description attribute for a property or field</summary>
		public static string? Desc(Type ty, string member_name)
		{
			var mi =
				(MemberInfo?)ty.GetProperty(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic) ??
				(MemberInfo?)ty.GetField   (member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			return Desc(ty, mi);
		}

		/// <summary>The description attribute associated with a property or field</summary>
		public static string? Desc<T>(this T obj, string member_name) where T : notnull
		{
			return Desc(obj.GetType(), member_name);
		}

		/// <summary>The description attribute associated with a property or field</summary>
		public static string? Desc<T,Ret>(this T obj, Expression<Func<T,Ret>> expression) where T : notnull
		{
			return Desc(obj.GetType(), R<T>.Name(expression));
		}

		/// <summary>Return the associated description attribute for an enum value</summary>
		public static string? Desc(this Enum enum_)
		{
			return Desc(enum_.GetType(), enum_.ToString());
		}

		/// <summary>Return an array of the description strings associated with an enum type</summary>
		public static string[] AllDesc(this Enum type)
		{
			return (string[])m_str_cache.Get(Key(type), OnMiss);
			object OnMiss(object _)
			{
				return Enum.GetValues(type.GetType()).OfType<Enum>().Select(x => x.Desc()).ToArray();
			}
		}

		/// <summary>Return an array of the strings associated with a type</summary>
		public static string[] AllDesc(this Type type)
		{
			return (string[])m_str_cache.Get(Key(type), OnMiss);
			object OnMiss(object _)
			{
				var strs = new List<string>();
				var members = type.AllMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var mi in members)
				{
					if (mi.GetCustomAttributes(typeof(DescAttribute), false).FirstOrDefault() is DescAttribute d0)
					{
						strs.Add(d0.Str);
						continue;
					}
					if (mi.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute d1)
					{
						strs.Add(d1.Description);
						continue;
					}
				}
				return strs.ToArray();
			}
		}

		/// <summary>A cache key for an enum value</summary>
		private static string Key(Enum enum_)
		{
			return enum_.GetType().Name + "." + enum_.ToString();
		}

		/// <summary>A cache key for member of an object</summary>
		private static string Key(object obj, string member_name)
		{
			return Key(obj.GetType(), member_name);
		}

		/// <summary>A cache key for member of a type</summary>
		private static string Key(Type type, string member_name)
		{
			return type.Name + "." + member_name;
		}

		/// <summary>A cache key for a type</summary>
		private static string Key(Type ty)
		{
			return ty.Name;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.ComponentModel;
	using Attrib;

	[TestFixture]
	public class TestDescAttr
	{
		private enum EType
		{
			[Desc("Value 0")]
			Value0,

			[Description("Value 1")]
			Value1,

			// no attributes
			Value2,

			[Desc("Third")]
			Value3,
		}
		private class C
		{
			[Description("Field Desc")]
			public readonly int m_field;

			[Desc("Prop Desc")]
			public int Prop { get; private set; }

			public string NoAttr { get; private set; }

			public C()
			{
				Prop    = 1;
				m_field = 2;
				NoAttr  = "Blah";
			}
		}
		[Test]
		public void DescAttr1()
		{
			const EType e = EType.Value0;
			Assert.Equal("Value 0",e.Desc());
			Assert.Equal("Value 1",EType.Value1.Desc());
			Assert.Equal(null,((EType)100).Desc());
			Assert.True(Enum<EType>.Desc.SequenceEqual(new[] { "Value 0","Value 1","Third" }));
		}
		[Test]
		public void DescAttr2()
		{
			var c = new C();
			Assert.Equal("Field Desc",c.Desc(nameof(C.m_field)));
			Assert.Equal("Field Desc",R<C>.Desc(x => x.m_field));
			Assert.Equal("Prop Desc",R<C>.Desc(x => x.Prop));
			Assert.Equal(null,c.Desc(x => x.NoAttr));
			Assert.Equal(null,R<C>.Desc(x => x.NoAttr));
		}
		[Test]
		public void DescAttr3()
		{
			var strs = typeof(EType).AllDesc();
			Assert.True(strs.SequenceEqual(new[] { "Value 0","Value 1","Third" }));
		}
		[Test]
		public void DescAttr4()
		{
			var strs = typeof(C).AllDesc();
			Assert.True(strs.SequenceEqual(new[] { "Prop Desc","Field Desc" }));
		}
	}
}
#endif
