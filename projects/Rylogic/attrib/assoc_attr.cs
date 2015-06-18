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
		public AssocAttribute(object t) { AssocItem = t; }
		public object AssocItem { get; private set; }
	}

	///// <summary>Accessor class for AssocAttribute</summary>
	//public static class AssocAttr
	//{
	//	/// <summary>Return the AssocAttribute for an enum value</summary>
	//	private static AssocAttribute GetAttr<TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
	//	{
	//		var fi = enum_.GetType().GetField(enum_.ToString());
	//		return fi.FindAttribute<AssocAttribute>(false);
	//	}

	//	/// <summary>Returns the instance associated with an enum value</summary>
	//	[DebuggerStepThrough]
	//	public static T Assoc<T,TEnum>(this TEnum enum_) where TEnum :struct ,IConvertible
	//	{
	//		var attr = GetAttr<TEnum>(enum_);
	//		if (attr == null) throw new Exception("Member does not have an AssocAttribute<{0}>".Fmt(typeof(T).Name));
	//		return attr..AssocItem;
	//	}
	//}

	public static class Assoc<Ty,T>
	{
		/// <summary>Returns the item associated with a member</summary>
		[DebuggerStepThrough]
		public static T Get<Ret>(Expression<Func<Ty,Ret>> expression)
		{
			return R<Ty>.Assoc<T,Ret>(expression);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using attrib;

	[TestFixture] public class TestAssocAttr
	{
		public enum EType { A, B, C }

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
		}
	}
}
#endif
