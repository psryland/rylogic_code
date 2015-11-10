using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using pr.extn;
using pr.util;

namespace pr.attrib
{
	/// <summary>
	/// An attribute for associating a type with a member.
	/// *WARNING* If used on enums containing members with the same value, the association
	/// is not guaranteed to match the member it is associated with.</summary>
	[AttributeUsage(AttributeTargets.Enum|AttributeTargets.Property|AttributeTargets.Field, AllowMultiple=true)]
	public sealed class AssocAttribute :Attribute
	{
		public AssocAttribute(object t)
		{
			Name = null;
			AssocItem = t;
		}
		public AssocAttribute(string name, object t)
		{
			Name = name;
			AssocItem = t;
		}

		/// <summary>A name to identify the associated type</summary>
		public string Name { get; private set; }

		/// <summary>The instance associated with the owning property/enum value/field</summary>
		public object AssocItem { get; private set; }
	}

	/// <summary>Access class for AssocAttribute</summary>
	public static class AssocAttr
	{
		/// <summary>Returns the AssocAttribute instance associated with an enum value</summary>
		private static AssocAttribute Get<T>(MemberInfo mi, string name = null)
		{
			if (mi == null) return null;
			return mi
				.GetCustomAttributes(typeof(AssocAttribute), false)
				.Cast<AssocAttribute>()
				.FirstOrDefault(x => x.Name == name && (x.AssocItem is T || x.AssocItem == null));
		}

		/// <summary>Returns the AssocItem instance associated with an enum value</summary>
		public static T Assoc<T>(MemberInfo mi, string name = null)
		{
			var attr = Get<T>(mi, name);
			if (attr != null) return (T)attr.AssocItem;
			throw name == null
				? new Exception("Member {0} does not have an unnamed AssocAttribute<{1}>".Fmt(mi.Name, typeof(T).Name))
				: new Exception("Member {0} does not have an AssocAttribute<{1}> with name {2}".Fmt(mi.Name, typeof(T).Name, name));
		}

		/// <summary>Returns the AssocItem instance associated with a property or field</summary>
		public static T Assoc<T>(Type ty, string member_name, string name = null)
		{
			var mi =
				(MemberInfo)ty.GetProperty(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic) ??
				(MemberInfo)ty.GetField   (member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			return Assoc<T>(mi, name);
		}

		/// <summary>Returns true if 'ty.member_name' has an AssocAttribute of type 'T'</summary>
		public static bool HasAssoc<T>(Type ty, string member_name, string name = null)
		{
			var mi =
				(MemberInfo)ty.GetProperty(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic) ??
				(MemberInfo)ty.GetField   (member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			return Get<T>(mi, name) != null;
		}

		/// <summary>Return the AssocItem associated with an enum value</summary>
		public static T Assoc<T>(this Enum enum_, string name = null)
		{
			return Assoc<T>(enum_.GetType(), enum_.ToString(), name);
		}

		/// <summary>Returns true if 'enum_' has an AssocAttribute of type 'T'</summary>
		public static bool HasAssoc<T>(this Enum enum_, string name = null)
		{
			return HasAssoc<T>(enum_.GetType(), enum_.ToString(), name);
		}
	}

	public static class Assoc<Ty,T>
	{
		/// <summary>Returns the item associated with a member</summary>
		[DebuggerStepThrough]
		public static T Get<Ret>(Expression<Func<Ty,Ret>> expression, string name = null)
		{
			return R<Ty>.Assoc<T,Ret>(expression, name);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using attrib;

	[TestFixture] public class TestAssocAttr
	{
		public enum EType
		{
			// NOTE: the Associated value doesn't work if the enum members don't have
			// unique values. This is a language thing, enum members with the same value
			// *are* optimised to a single value (even tho the reflection data still keeps them separate)
			[Assoc("#", 1)] A = 3,
			[Assoc("#", 2)] B = 2,
			[Assoc("#", 3)] C = 1,
		}

		public class Whatsit
		{
			[Assoc(5), Assoc(EType.A)]
			public double Distance { get { return 2.0; } }

			[Assoc(0.001), Assoc(EType.C)]
			public double Speed { get { return 3.0; } }
		}

		[Test] public void Assoc()
		{
			var w = new Whatsit();

			Assert.AreEqual(EType.A, Assoc<Whatsit,EType>.Get(x => x.Distance));
			Assert.AreEqual(EType.C, Assoc<Whatsit,EType>.Get(x => x.Speed));

			Assert.AreEqual(5    , Assoc<Whatsit, int   >.Get(x => x.Distance));
			Assert.AreEqual(0.001, Assoc<Whatsit, double>.Get(x => x.Speed));

			// Enum members with the same value still have unique associated values
			Assert.AreEqual(EType.A.Assoc<int>("#"), 1);
			Assert.AreEqual(EType.B.Assoc<int>("#"), 2);
			Assert.AreEqual(EType.C.Assoc<int>("#"), 3);

			// Literal or variable works
			var e = EType.B;
			Assert.AreEqual(EType.A.Assoc<int>("#"), 1);
			Assert.AreEqual(e.Assoc<int>("#"), 2);
		}
	}
}
#endif
