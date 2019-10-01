using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Rylogic.Utility;

namespace Rylogic.Attrib
{
	/// <summary>
	/// An attribute for associating a type with a member.
	/// *WARNING* If used on enums containing members with the same value, the association
	/// is not guaranteed to match the member it is associated with.</summary>
	[AttributeUsage(AttributeTargets.Enum|AttributeTargets.Property|AttributeTargets.Field, AllowMultiple=true)]
	public sealed class AssocAttribute :Attribute
	{
		/// <summary>Associate an instance of a constant value</summary>
		public AssocAttribute(object t)
			:this(null, t)
		{}

		/// <summary>Associate a named instance of a constant value</summary>
		public AssocAttribute(string? name, object t)
			:this(name, t.GetType(), () => t)
		{}

		/// <summary>Associate a property or field via reflection</summary>
		public AssocAttribute(Type ty, string prop_or_field_name, bool non_public = false)
			:this(null, ty, prop_or_field_name, non_public)
		{}

		/// <summary>Associate a named property or field via reflection</summary>
		public AssocAttribute(string? name, Type ty, string prop_or_field_name, bool non_public = false)
			:this(name, ty, null)
		{
			var mi = ty.GetProperty(prop_or_field_name, BindingFlags.Static|BindingFlags.Public|(non_public?BindingFlags.NonPublic:0))?.GetGetMethod(non_public);
			if (mi != null)
			{
				m_get = () => mi.Invoke(null, null);
				return;
			}

			var fi = ty.GetField(prop_or_field_name, BindingFlags.Static|BindingFlags.Public|(non_public?BindingFlags.NonPublic:0));
			if (fi != null)
			{
				m_get = () => fi.GetValue(null);
				return;
			}

			throw new Exception("AssocAttribute: Cannot match named static property or field");
		}
		private AssocAttribute(string? name, Type ty, Func<object>? get)
		{
			Name = name;
			Type = ty;
			m_get = get;
		}

		/// <summary>A name to identify the associated type</summary>
		public string? Name { get; }

		/// <summary>The type of the associated value</summary>
		public Type Type { get; }

		/// <summary>The instance associated with the owning property/enum value/field</summary>
		public object? AssocItem => m_get?.Invoke();
		private Func<object>? m_get;
	}

	/// <summary>Access class for AssocAttribute</summary>
	public static class Assoc_
	{
		/// <summary>Returns the AssocAttribute instance associated with an enum value</summary>
		private static AssocAttribute? Get<T>(MemberInfo mi, string? name = null)
		{
			if (mi == null) return null;
			return mi
				.GetCustomAttributes(typeof(AssocAttribute), false)
				.Cast<AssocAttribute>()
				.FirstOrDefault(x => x.Name == name && (x.AssocItem == null || x.AssocItem is T));
		}

		/// <summary>Returns the AssocItem instance associated with an enum value</summary>
		public static T Assoc<T>(MemberInfo mi, string? name = null)
		{
			var attr = Get<T>(mi, name);
			if (attr != null && attr.AssocItem != null)
				return (T)attr.AssocItem;

			throw name == null
				? new Exception($"Member {mi.Name} does not have an unnamed AssocAttribute<{typeof(T).Name}>")
				: new Exception($"Member {mi.Name} does not have an AssocAttribute<{typeof(T).Name}> with name {name}");
		}

		/// <summary>Returns the AssocItem instance associated with a property or field</summary>
		public static T Assoc<T>(Type ty, string member_name, string? name = null)
		{
			var mi =
				(MemberInfo)ty.GetProperty(member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic) ??
				(MemberInfo)ty.GetField   (member_name, BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
			return Assoc<T>(mi, name);
		}

		/// <summary>Return the AssocItem associated with an enum value</summary>
		public static T Assoc<T>(this Enum enum_, string? name = null)
		{
			return Assoc<T>(enum_.GetType(), enum_.ToString(), name);
		}

		/// <summary>Returns true if 'ty.member_name' has an AssocAttribute of type 'T'</summary>
		public static bool TryAssoc<T>(MemberInfo mi, out T assoc, string? name = null)
		{
			var attr = Get<T>(mi, name);
			assoc = (T)(attr?.AssocItem ?? default(T)!);
			return attr != null;
		}

		/// <summary>Returns true if 'ty.member_name' has an AssocAttribute of type 'T'</summary>
		public static bool TryAssoc<T>(Type ty, string member_name, out T assoc, string? name = null)
		{
			var mi =
				(MemberInfo)ty.GetProperty(member_name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
				(MemberInfo)ty.GetField(member_name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			return TryAssoc<T>(mi, out assoc, name);
		}

		/// <summary>Returns true if 'enum_' has an AssocAttribute of type 'T'</summary>
		public static bool TryAssoc<T>(this Enum enum_, out T assoc, string? name = null)
		{
			return TryAssoc<T>(enum_.GetType(), enum_.ToString(), out assoc, name);
		}
	}

	/// <summary></summary>
	public static class Assoc<Ty,T>
	{
		/// <summary>Returns the item associated with a member</summary>
		[DebuggerStepThrough]
		public static T Get<Ret>(Expression<Func<Ty,Ret>> expression, string? name = null)
		{
			return R<Ty>.Assoc<T,Ret>(expression, name);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Drawing;
	using Attrib;

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
		public enum EType2
		{
			[Assoc(typeof(Color), nameof(Color.Red))]   R = 0,
			[Assoc(typeof(Color), nameof(Color.Green))] G = 1,
			[Assoc(typeof(Color), nameof(Color.Blue))]  B = 2,
		}

		public class Whatsit
		{
			[Assoc(5), Assoc(EType.A)]
			public double Distance => 2.0;

			[Assoc(0.001), Assoc(EType.C)]
			public double Speed => 3.0;
		}

		[Test] public void Assoc()
		{
			var w = new Whatsit();

			Assert.Equal(EType.A, Assoc<Whatsit,EType>.Get(x => x.Distance));
			Assert.Equal(EType.C, Assoc<Whatsit,EType>.Get(x => x.Speed));

			Assert.Equal(5    , Assoc<Whatsit, int   >.Get(x => x.Distance));
			Assert.Equal(0.001, Assoc<Whatsit, double>.Get(x => x.Speed));

			// Enum members with the same value still have unique associated values
			Assert.Equal(EType.A.Assoc<int>("#"), 1);
			Assert.Equal(EType.B.Assoc<int>("#"), 2);
			Assert.Equal(EType.C.Assoc<int>("#"), 3);

			// Literal or variable works
			var e = EType.B;
			Assert.Equal(EType.A.Assoc<int>("#"), 1);
			Assert.Equal(e.Assoc<int>("#"), 2);

			// Associated reflected type
			Assert.Equal(Color.Red  , EType2.R.Assoc<Color>());
			Assert.Equal(Color.Green, EType2.G.Assoc<Color>());
			Assert.Equal(Color.Blue , EType2.B.Assoc<Color>());
		}
	}
}
#endif
