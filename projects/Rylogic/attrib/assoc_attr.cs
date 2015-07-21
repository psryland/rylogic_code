using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using pr.extn;
using pr.util;

namespace pr.attrib
{
	/// <summary>An attribute for associating a type with a member</summary>
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

	/// <summary>Accessor class for AssocAttribute</summary>
	public static class AssocAttr
	{
		/// <summary>Returns the instance associated with an enum value</summary>
		[DebuggerStepThrough]
		public static T Assoc<T>(this Enum enum_, string name = null)
		{
			var fi = enum_.GetType().GetField(enum_.ToString());
			var attr = fi
				.GetCustomAttributes(typeof(AssocAttribute), false)
				.Cast<AssocAttribute>()
				.FirstOrDefault(x => x.Name == name && (x.AssocItem is T || x.AssocItem == null));
			if (attr == null)
			{
				if (name == null)
					throw new Exception("Member does not have an unnamed AssocAttribute<{0}>".Fmt(typeof(T).Name));
				else
					throw new Exception("Member does not have an AssocAttribute<{0}> with name {1}".Fmt(typeof(T).Name, name));
			}
			return (T)attr.AssocItem;
		}
	}

	public static class Assoc<Ty,T>
	{
		/// <summary>Returns the item associated with a member</summary>
		[DebuggerStepThrough] public static T Get<Ret>(Expression<Func<Ty,Ret>> expression, string name = null)
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
			[Assoc("#", 1)] A,
			[Assoc("#", 2)] B,
			[Assoc("#", 3)] C,
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

			var e = EType.B;
			Assert.AreEqual(EType.A.Assoc<int>("#"), 1);
			Assert.AreEqual(e.Assoc<int>("#"), 2);
		}
	}
}
#endif
