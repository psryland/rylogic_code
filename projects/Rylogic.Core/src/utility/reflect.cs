using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Rylogic.Attrib;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	/// <summary>Type Reflection methods</summary>
	public static class R<T>
	{
		/// <summary>
		/// Derives the name of a property from the given lambda expression and returns it as string.
		/// Example: Reflect&lt;DateTime&gt;.Name(s => s.Ticks) returns "Ticks" </summary>
		[DebuggerStepThrough]
		public static string Name<Ret>(Expression<Func<T,Ret>> expression)
		{
			if (expression.Body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression)ue.Operand).Member.Name;

			if (expression.Body is MethodCallExpression mc)
				return mc.Method.Name;

			if (expression.Body is MemberExpression me)
				return me.Member.Name;

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>
		/// Derives the name of a property or method from the given lambda expression and returns it as string.
		/// Example: Reflect&lt;DateTime&gt;.Name(s => s.Ticks) returns "Ticks" </summary>
		[DebuggerStepThrough]
		public static string Name(Expression<Action<T>> expression)
		{
			if (expression.Body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression)ue.Operand).Member.Name;

			if (expression.Body is MethodCallExpression mce)
				return mce.Method.Name;

			if (expression.Body is MemberExpression me)
				return me.Member.Name;

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>Derives the name of a property from the given lambda and returns it within brackets</summary>
		[DebuggerStepThrough]
		public static string NameBkt<Ret>(Expression<Func<T,Ret>> expression, string open="[", string close="]")
		{
			return open + Name(expression) + close;
		}

		/// <summary>Derives the name of a property from the given lambda and returns it within brackets</summary>
		[DebuggerStepThrough]
		public static string NameBkt<Ret>(Expression<Action<T>> expression, string open="[", string close="]")
		{
			return open + Name(expression) + close;
		}

		/// <summary>Derives the name of a property from the given lambda and returns it in the form 'TypeName.Name'</summary>
		[DebuggerStepThrough]
		public static string TypeName<Ret>(Expression<Func<T,Ret>> expression)
		{
			return typeof(T).Name + "." + Name(expression);
		}

		/// <summary>Derives the name of a property from the given lambda and returns it in the form 'TypeName.Name'</summary>
		[DebuggerStepThrough]
		public static string TypeName<Ret>(Expression<Action<T>> expression)
		{
			return typeof(T).Name + "." + Name(expression);
		}

		/// <summary>Derives the name of a property from the given lambda and returns it in the form 'TypeName.[name]'</summary>
		[DebuggerStepThrough]
		public static string TypeNameBkt<Ret>(Expression<Func<T,Ret>> expression, string open="[", string close="]")
		{
			return typeof(T).Name + "." + NameBkt(expression, open, close);
		}

		/// <summary>Derives the name of a property from the given lambda and returns it in the form 'TypeName.[name]'</summary>
		[DebuggerStepThrough]
		public static string TypeNameBkt<Ret>(Expression<Action<T>> expression, string open="[", string close="]")
		{
			return typeof(T).Name + "." + NameBkt<Ret>(expression, open, close);
		}

		/// <summary>Return the type of the property from the given lambda</summary>
		[DebuggerStepThrough]
		public static Type Type<Ret>(Expression<Func<T, Ret>> expression)
		{
			// untested... might be valid but I don't know what expression has this type
			//if (expression.Body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
			//	return ((MemberExpression)ue.Operand).Type;

			if (expression.Body is MethodCallExpression mc)
				return mc.Type;

			if (expression.Body is MemberExpression me)
				return me.Type;

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>Return the name of type 'T'</summary>
		public static string NameOf
		{
			get { return typeof(T).Name; }
		}

		/// <summary>Return the size of type 'T' as determined by the interop marshaller</summary>
		public static int SizeOf
		{
			[DebuggerStepThrough] get { return Marshal.SizeOf(typeof(T)); }
		}

		#region Attributes

		/// <summary>Returns a collection of attributes associated with the member with name 'member_name'</summary>
		[DebuggerStepThrough]
		public static IEnumerable<Attribute> Attrs(string member_name, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static)
		{
			var member = typeof(T).GetMember(member_name, flags);
			if (member == null) return Enumerable.Empty<Attribute>();
			return member.SelectMany(x => x.GetCustomAttributes(false)).Cast<Attribute>();
		}

		/// <summary>Returns a collection of attributes associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static IEnumerable<Attribute> Attrs<Ret>(Expression<Func<T,Ret>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression)ue.Operand).Member.GetCustomAttributes(false).Cast<Attribute>();

			var mc = expression.Body as MethodCallExpression;
			if (mc != null)
				return mc.Method.GetCustomAttributes(false).Cast<Attribute>();

			var me = expression.Body as MemberExpression;
			if (me != null)
				return me.Member.GetCustomAttributes(false).Cast<Attribute>();

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>Returns a collection of attributes associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static IEnumerable<Attribute> Attrs(Expression<Action<T>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression) ue.Operand).Member.GetCustomAttributes(false).Cast<Attribute>();

			var mce = expression.Body as MethodCallExpression;
			if (mce != null)
				return mce.Method.GetCustomAttributes(false).Cast<Attribute>();

			var me = expression.Body as MemberExpression;
			if (me != null)
				return me.Member.GetCustomAttributes(false).Cast<Attribute>();

			throw new NotImplementedException("Unknown expression type");
		}

		#endregion

		#region DescAttribute

		/// <summary>Returns a string description from the DescAttribute or DescriptionAttribute associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static string? Desc(string member_name)
		{
			var d0 = Attrs(member_name).OfType<DescAttribute>().FirstOrDefault();
			return d0 != null ? d0.Str : null;
		}

		/// <summary>Returns a string description from the DescAttribute or DescriptionAttribute associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static string? Desc<Ret>(Expression<Func<T,Ret>> expression)
		{
			var d0 = Attrs(expression).OfType<DescAttribute>().FirstOrDefault();
			if (d0 != null) return d0.Str;
			var d1 = Attrs(expression).OfType<DescriptionAttribute>().FirstOrDefault();
			if (d1 != null) return d1.Description;
			return null;
		}

		/// <summary>Returns a string description from the DescAttribute or DescriptionAttribute associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static string? Desc(Expression<Action<T>> expression)
		{
			var d0 = Attrs(expression).OfType<DescAttribute>().FirstOrDefault();
			if (d0 != null) return d0.Str;
			var d1 = Attrs(expression).OfType<DescriptionAttribute>().FirstOrDefault();
			if (d1 != null) return d1.Description;
			return null;
		}

		#endregion

		#region UnitsAttribute

		[DebuggerStepThrough]
		public static UnitsAttribute Units(string member_name)
		{
			var attr = Attrs(member_name).OfType<UnitsAttribute>().FirstOrDefault();
			if (attr == null) throw new Exception("Member does not have the UnitsAttribute");
			return attr;
		}

		/// <summary>Returns the UnitsAttribute associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static UnitsAttribute Units<Ret>(Expression<Func<T,Ret>> expression)
		{
			var attr = Attrs(expression).OfType<UnitsAttribute>().FirstOrDefault();
			if (attr == null) throw new Exception("Member does not have the UnitsAttribute");
			return attr;
		}

		/// <summary>Returns the UnitsAttribute associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static UnitsAttribute Units(Expression<Action<T>> expression)
		{
			var attr = Attrs(expression).OfType<UnitsAttribute>().FirstOrDefault();
			if (attr == null) throw new Exception("Member does not have the UnitsAttribute");
			return attr;
		}

		#endregion

		#region AssocAttribute

		/// <summary>Returns the item of type 'Ret' associated with the member named 'member_name'</summary>
		[DebuggerStepThrough]
		public static Ret Assoc<Ret>(string member_name, string? name = null)
		{
			var attr = Attrs(member_name).OfType<AssocAttribute>().FirstOrDefault(x => x.AssocItem is Ret && x.Name == name);
			if (attr == null)
			{
				var with_name = name != null ? $" with name {name}" : string.Empty;
				throw new Exception($"Member does not have the AssocAttribute for type {typeof(Ret).Name}{with_name}");
			}
			return (Ret)attr.AssocItem!;
		}

		/// <summary>Returns the item of type 'Ret' associated with the member returned in 'expression'</summary>
		[DebuggerStepThrough]
		public static Ret Assoc<Ret,Res>(Expression<Func<T,Res>> expression, string? name = null)
		{
			var attr = Attrs(expression).OfType<AssocAttribute>().FirstOrDefault(x => x.AssocItem is Ret && x.Name == name);
			if (attr == null)
			{
				var with_name = name != null ? $" with name {name}" : string.Empty;
				throw new Exception($"Member does not have the AssocAttribute for type {typeof(Ret).Name}{with_name}");
			}
			return (Ret)attr.AssocItem!;
		}

		/// <summary>Returns the item of type 'Ret' associated with the member returned in 'expression'</summary>
		[DebuggerStepThrough]
		public static Ret Assoc<Ret>(Expression<Action<T>> expression, string? name = null)
		{
			var attr = Attrs(expression).OfType<AssocAttribute>().FirstOrDefault(x => x.AssocItem is Ret && x.Name == name);
			if (attr == null)
			{
				var with_name = name != null ? $" with name {name}" : string.Empty;
				throw new Exception($"Member does not have the AssocAttribute for type {typeof(Ret).Name}{with_name}");
			}
			return (Ret)attr.AssocItem!;
		}

		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Text;
	using Attrib;
	using Utility;

	[TestFixture]
	public class TestReflect
	{
		internal class Thing
		{
			[Description("My name is Desc")] public void Desc() { }
			[Desc("Short For")] public string Bob { get { return "kate"; } }
		}

		[Test]
		public void MemberName()
		{
			Assert.Equal("X", R<Point>.Name(p => p.X));
			Assert.Equal("Offset", R<Point>.Name(p => p.Offset(0, 0)));
			Assert.Equal("BaseStream", R<StreamWriter>.Name(s => s.BaseStream));
		}
		[Test]
		public void MemberType()
		{
			Assert.Equal(typeof(int), R<Point>.Type(p => p.X));
			Assert.Equal(typeof(bool), R<Point>.Type(p => p.Equals(null)));
			Assert.Equal(typeof(Encoding), R<StreamWriter>.Type(s => s.Encoding));
		}
		[Test]
		public void Attrib()
		{
			var attr = R<Thing>.Attrs(x => x.Desc()).ToArray();
			Assert.Equal(1, attr.Length);
			Assert.True(attr[0] is DescriptionAttribute);

			attr = R<Thing>.Attrs(x => x.Bob).ToArray();
			Assert.Equal(1, attr.Length);
			Assert.True(attr[0] is DescAttribute);
		}
	}
}
#endif
