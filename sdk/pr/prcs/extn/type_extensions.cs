using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace pr.extn
{
	public static class TypeExtensions
	{
		/// <summary>Returns all inherited properties for a type</summary>
		public static IEnumerable<PropertyInfo> AllProps(this Type type, BindingFlags flags)
		{
			if (type == null || type == typeof(object)) return Enumerable.Empty<PropertyInfo>();
			return AllProps(type.BaseType, flags).Concat(type.GetProperties(flags|BindingFlags.DeclaredOnly));
		}

		/// <summary>Returns all inherited fields for a type</summary>
		public static IEnumerable<FieldInfo> AllFields(this Type type, BindingFlags flags)
		{
			if (type == null || type == typeof(object)) return Enumerable.Empty<FieldInfo>();
			return AllFields(type.BaseType, flags).Concat(type.GetFields(flags|BindingFlags.DeclaredOnly));
		}

		/// <summary>Find all types derived from this type</summary>
		public static List<Type> DerivedTypes(this Type type)
		{
			return Assembly.GetAssembly(type).GetTypes().Where(t => t != type && type.IsAssignableFrom(t)).ToList();
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
		internal static class TestTypeExtensions
		{
			// ReSharper disable UnusedMember.Local
			#pragma warning disable 169, 649
			private class ThingBase
			{
				private int   B_PrivateField;
				protected int B_ProtectedField;
				internal int  B_InternalField;
				public int    B_PublicField;
				private int   B_PrivateAutoProp { get; set; }
				protected int B_ProtectedAutoProp { get; set; }
				internal int  B_InternalAutoProp { get; set; }
				public int    B_PublicAutoProp { get; set; }
			}
			private class Thing :ThingBase
			{
				private int   D_PrivateField;
				protected int D_ProtectedField;
				internal int  D_InternalField;
				public int    D_PublicField;
				private int   D_PrivateAutoProp { get; set; }
				protected int D_ProtectedAutoProp { get; set; }
				internal int  D_InternalAutoProp { get; set; }
				public int    D_PublicAutoProp { get; set; }
			}
			#pragma warning restore 169, 649
			// ReSharper restore UnusedMember.Local

			[Test] public static void ShallowCopy()
			{
				var fields = typeof(Thing).AllFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
				Assert.AreEqual(16, fields.Count);

				fields = typeof(Thing).AllFields(BindingFlags.Instance|BindingFlags.Public).ToList();
				Assert.AreEqual(2, fields.Count);

				var props = typeof(Thing).AllProps(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
				Assert.AreEqual(8, props.Count);

				props = typeof(Thing).AllProps(BindingFlags.Instance|BindingFlags.Public).ToList();
				Assert.AreEqual(2, props.Count);
			}
		}
	}
}
#endif