using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace pr.extn
{
	public static class TypeExtensions
	{
		/// <summary>Resolve a type name to a type</summary>
		public static Type Resolve(string name, bool throw_on_error = true)
		{
			return Type.GetType(name, null
				,(assembly,type_name,ignore_case) =>
					{
						var assems = assembly != null ? new[]{assembly} : AppDomain.CurrentDomain.GetAssemblies();
						return assems.Select(ass => ass.GetType(type_name,false,ignore_case)).FirstOrDefault(t => t != null);
					}
				,throw_on_error);
		}

		/// <summary>Returns all inherited members for a type (including private members)</summary>
		public static IEnumerable<MemberInfo> AllMembers(this Type type, BindingFlags flags)
		{
			if (type == null || type == typeof(object)) return Enumerable.Empty<MemberInfo>();
			return AllMembers(type.BaseType, flags).Concat(type.GetMembers(flags|BindingFlags.DeclaredOnly));
		}

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

		/// <summary>Returns all inherited events for a type</summary>
		public static IEnumerable<EventInfo> AllEvents(this Type type, BindingFlags flags)
		{
			if (type == null || type == typeof(object)) return Enumerable.Empty<EventInfo>();
			return AllEvents(type.BaseType, flags).Concat(type.GetEvents(flags|BindingFlags.DeclaredOnly));
		}

		/// <summary>Find all types derived from this type</summary>
		public static List<Type> DerivedTypes(this Type type)
		{
			return Assembly.GetAssembly(type).GetTypes().Where(t => t != type && type.IsAssignableFrom(t)).ToList();
		}

		/// <summary>Returns true if 'type' is or inherits 'baseOrInterface'</summary>
		public static bool Inherits(this Type type, Type baseOrInterface)
		{
			return baseOrInterface.IsAssignableFrom(type);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static Attribute FindAttribute(this Type type, Type attribute_type, bool inherit = true)
		{
			if (!attribute_type.Inherits(typeof(Attribute))) throw new Exception("Expected 'attribute_type' to be a subclass of 'Attribute'");
			return type.GetCustomAttributes(attribute_type, inherit).Cast<Attribute>().FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static Attribute FindAttribute(this Assembly ass, Type attribute_type, bool inherit = true)
		{
			if (!attribute_type.Inherits(typeof(Attribute))) throw new Exception("Expected 'attribute_type' to be a subclass of 'Attribute'");
			return ass.GetCustomAttributes(attribute_type, inherit).Cast<Attribute>().FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this method or null.</summary>
		public static Attribute FindAttribute(this MethodInfo mi, Type attribute_type, bool inherit = true)
		{
			if (!attribute_type.Inherits(typeof(Attribute))) throw new Exception("Expected 'attribute_type' to be a subclass of 'Attribute'");
			return mi.GetCustomAttributes(attribute_type, inherit).Cast<Attribute>().FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static T FindAttribute<T>(this Type type, bool inherit = true) where T:Attribute
		{
			return (T)type.FindAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this assembly or null.</summary>
		public static T FindAttribute<T>(this Assembly ass, bool inherit = true) where T:Attribute
		{
			return (T)ass.FindAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static T FindAttribute<T>(this MethodInfo mi, bool inherit = true) where T:Attribute
		{
			return (T)mi.FindAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type. Throws if not found</summary>
		public static Attribute GetAttribute(this Type type, Type attribute_type, bool inherit = true)
		{
			var attr = FindAttribute(type, attribute_type, inherit);
			if (attr == null) throw new Exception("Type '{0}' is not decorated with the '{1}' attribute.".Fmt(type.FullName, attribute_type.FullName));
			return attr;
		}

		/// <summary>Returns the first instance of 'attribute_type' for this assembly. Throws if not found</summary>
		public static Attribute GetAttribute(this Assembly ass, Type attribute_type, bool inherit = true)
		{
			var attr = FindAttribute(ass, attribute_type, inherit);
			if (attr == null) throw new Exception("Type '{0}' is not decorated with the '{1}' attribute.".Fmt(ass.FullName, attribute_type.FullName));
			return attr;
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type. Throws if not found</summary>
		public static T GetAttribute<T>(this Type type, bool inherit = true) where T:Attribute
		{
			return (T)type.GetAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this assembly. Throws if not found</summary>
		public static T GetAttribute<T>(this Assembly ass, bool inherit = true) where T:Attribute
		{
			return (T)ass.GetAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the methods on this type that are decorated with the attribute 'attribute_type'</summary>
		public static IEnumerable<MethodInfo> FindMethodsWithAttribute(this Type type, Type attribute_type, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance)
		{
			return type.GetMethods(flags).Where(x => x.FindAttribute(attribute_type) != null);
		}

		/// <summary>Returns the methods on this type that are decorated with the attribute 'attribute_type'</summary>
		public static IEnumerable<MethodInfo> FindMethodsWithAttribute<T>(this Type type, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance) where T:Attribute
		{
			return type.FindMethodsWithAttribute(typeof(T), flags);
		}
	}

	/// <summary>'bool' type extensions</summary>
	public static class bool_
	{
		public static bool? TryParse(string val)
		{
			bool o;
			return bool.TryParse(val, out o) ? (bool?)o : null;
		}
	}

	/// <summary>'int' type extensions</summary>
	public static class int_
	{
		public static int? TryParse(string val)
		{
			int o;
			return int.TryParse(val, out o) ? (int?)o : null;
		}

		/// <summary>Parse an array of integer values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of integer values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed integers</returns>
		public static int[] ParseArray(string val, string[] delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(int.Parse).ToArray();
		}
	}

	public static class double_
	{
		/// <summary>Parse an array of floating point values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of floating point values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed floating point values</returns>
		public static double[] ParseArray(string val, string[] delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(double.Parse).ToArray();
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestTypeExtns
	{
		// ReSharper disable UnusedMember.Local
		#pragma warning disable 169, 649
		private class ThingBase
		{
			private          int B_PrivateField            ;
			protected        int B_ProtectedField          ;
			internal         int B_InternalField           ;
			public           int B_PublicField             ;
			private          int B_PrivateAutoProp         { get; set; }
			protected        int B_ProtectedAutoProp       { get; set; }
			internal         int B_InternalAutoProp        { get; set; }
			public           int B_PublicAutoProp          { get; set; }
			private          int B_PrivateMethod        () { return 0; }
			protected        int B_ProtectedMethod      () { return 0; }
			internal         int B_InternalMethod       () { return 0; }
			public           int B_PublicMethod         () { return 0; }
			private   static int B_PrivateStaticMethod  () { return 0; }
			protected static int B_ProtectedStaticMethod() { return 0; }
			internal  static int B_InternalStaticMethod () { return 0; }
			public    static int B_PublicStaticMethod   () { return 0; }
		}
		private class Thing :ThingBase
		{
			private          int D_PrivateField            ;
			protected        int D_ProtectedField          ;
			internal         int D_InternalField           ;
			public           int D_PublicField             ;
			private          int D_PrivateAutoProp         { get; set; }
			protected        int D_ProtectedAutoProp       { get; set; }
			internal         int D_InternalAutoProp        { get; set; }
			public           int D_PublicAutoProp          { get; set; }
			private          int D_PrivateMethod        () { return 0; }
			protected        int D_ProtectedMethod      () { return 0; }
			internal         int D_InternalMethod       () { return 0; }
			public           int D_PublicMethod         () { return 0; }
			private   static int D_PrivateStaticMethod  () { return 0; }
			protected static int D_ProtectedStaticMethod() { return 0; }
			internal  static int D_InternalStaticMethod () { return 0; }
			public    static int D_PublicStaticMethod   () { return 0; }
		}
		#pragma warning restore 169, 649
		// ReSharper restore UnusedMember.Local

		[Test] public void AllMembers()
		{
			// In base:
			//  4 - 4 - fields
			//  8 - 4 - auto properties
			// 12 - 4 - auto prop backing fields
			// 20 - 8 - get/set auto prop methods
			// 24 - 4 - instance methods
			// 28 - 4 - static methods
			// 29 - 1 - constructor
			// 58 - x2 - for the same again in derived
			var members = typeof(Thing).AllMembers(BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
			Assert.AreEqual(58, members.Count);

			members = typeof(Thing).AllMembers(BindingFlags.Instance|BindingFlags.Public).ToList();
			Assert.AreEqual(12, members.Count);
		}
		[Test] public void AllFields()
		{
			var fields = typeof(Thing).AllFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
			Assert.AreEqual(16, fields.Count);

			fields = typeof(Thing).AllFields(BindingFlags.Instance|BindingFlags.Public).ToList();
			Assert.AreEqual(2, fields.Count);
		}
		[Test] public void AllProps()
		{
			var props = typeof(Thing).AllProps(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).ToList();
			Assert.AreEqual(8, props.Count);

			props = typeof(Thing).AllProps(BindingFlags.Instance|BindingFlags.Public).ToList();
			Assert.AreEqual(2, props.Count);
		}
		[Test] public void Resolve()
		{
			var ty0 = TypeExtensions.Resolve("System.String");
			Assert.AreEqual(typeof(string), ty0);

			var ty1 = TypeExtensions.Resolve("pr.util.CRC32");
			Assert.AreEqual(typeof(util.CRC32), ty1);
		}
		[Test] public void IntExtn()
		{
			var x0 = int_.TryParse("1234");
			Assert.AreEqual(x0, 1234);

			var x1 = int_.TryParse("abc");
			Assert.AreEqual(x1, null);

			var x2 = int_.ParseArray("1  2,3\t\t\t4");
			Assert.True(x2.SequenceEqual(new[]{1,2,3,4}));
		}
	}
}
#endif