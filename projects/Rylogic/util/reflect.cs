using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using pr.attrib;

namespace pr.util
{
	/// <summary>Type specific utility methods</summary>
	public static class Reflect<T>
	{
		/// <summary>
		/// Derives the name of a property from the given lambda expression and returns it as string.
		/// Example: Reflect&lt;DateTime&gt;.MemberName(s => s.Ticks) returns "Ticks" </summary>
		[DebuggerStepThrough]
		public static string MemberName<Ret>(Expression<Func<T,Ret>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression) ue.Operand).Member.Name;

			var mc = expression.Body as MethodCallExpression;
			if (mc != null)
				return mc.Method.Name;

			var me = expression.Body as MemberExpression;
			if (me != null)
				return me.Member.Name;

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>
		/// Derives the name of a property or method from the given lambda expression and returns it as string.
		/// Example: Reflect&lt;DateTime&gt;.MemberName(s => s.Ticks) returns "Ticks" </summary>
		[DebuggerStepThrough]
		public static string MemberName(Expression<Action<T>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression) ue.Operand).Member.Name;

			var mce = expression.Body as MethodCallExpression;
			if (mce != null)
				return mce.Method.Name;

			var me = expression.Body as MemberExpression;
			if (me != null)
				return me.Member.Name;

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>Returns an array of attributes associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static Attribute[] Attrs<Ret>(Expression<Func<T,Ret>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression)ue.Operand).Member.GetCustomAttributes(false).Cast<Attribute>().ToArray();

			var mc = expression.Body as MethodCallExpression;
			if (mc != null)
				return mc.Method.GetCustomAttributes(false).Cast<Attribute>().ToArray();

			var me = expression.Body as MemberExpression;
			if (me != null)
				return me.Member.GetCustomAttributes(false).Cast<Attribute>().ToArray();

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>Returns an array of attributes associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static Attribute[] Attrs(Expression<Action<T>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression) ue.Operand).Member.GetCustomAttributes(false).Cast<Attribute>().ToArray();

			var mce = expression.Body as MethodCallExpression;
			if (mce != null)
				return mce.Method.GetCustomAttributes(false).Cast<Attribute>().ToArray();

			var me = expression.Body as MemberExpression;
			if (me != null)
				return me.Member.GetCustomAttributes(false).Cast<Attribute>().ToArray();

			throw new NotImplementedException("Unknown expression type");
		}

		/// <summary>Returns a string description from the DescAttribute or DescriptionAttribute associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static string Desc<Ret>(Expression<Func<T,Ret>> expression)
		{
			var d0 = Attrs(expression).FirstOrDefault(x => x is DescAttribute) as DescAttribute;
			if (d0 != null) return d0.Str;
			var d1 = Attrs(expression).FirstOrDefault(x => x is DescriptionAttribute) as DescriptionAttribute;
			if (d1 != null) return d1.Description;
			return null;
		}

		/// <summary>Returns an array of attributes associated with the return type of 'expression'</summary>
		[DebuggerStepThrough]
		public static string Desc(Expression<Action<T>> expression)
		{
			var d0 = Attrs(expression).FirstOrDefault(x => x is DescAttribute) as DescAttribute;
			if (d0 != null) return d0.Str;
			var d1 = Attrs(expression).FirstOrDefault(x => x is DescriptionAttribute) as DescriptionAttribute;
			if (d1 != null) return d1.Description;
			return null;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using attrib;
	using util;
	
	[TestFixture] public class TestReflect
	{
		internal class Thing
		{
			[Description("My name is Desc")] public void Desc() {}
			[Desc("Short For")] public string Bob { get { return "kate"; } }
		}

		[Test] public void MemberName()
		{
			Assert.AreEqual("X", Reflect<Point>.MemberName(p => p.X));
			Assert.AreEqual("Offset", Reflect<Point>.MemberName(p => p.Offset(0,0)));
			Assert.AreEqual("BaseStream", Reflect<StreamWriter>.MemberName(s => s.BaseStream));
		}
		[Test] public void Attrib()
		{
			var attr = Reflect<Thing>.Attrs(x => x.Desc());
			Assert.AreEqual(1, attr.Length);
			Assert.True(attr[0] is DescriptionAttribute);

			attr = Reflect<Thing>.Attrs(x => x.Bob);
			Assert.AreEqual(1, attr.Length);
			Assert.True(attr[0] is DescAttribute);
		}
	}
}
#endif
