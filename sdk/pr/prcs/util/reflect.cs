using System;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;

namespace pr.util
{
	/// <summary>Type specific utility methods</summary>
	public static class Reflect<T>
	{
		/// <summary>
		/// Derives the name of a property from the given lambda expression and returns it as string.
		/// Example: Reflect&lt;DateTime&gt;.MemberName(s => s.Ticks) returns "Ticks" </summary>
		public static string MemberName<Ret>(Expression<Func<T,Ret>> expression)
		{
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression) ue.Operand).Member.Name;
			
			return ((MemberExpression) expression.Body).Member.Name;
		}

		/// <summary>
		/// Derives the name of a property or method from the given lambda expression and returns it as string.
		/// Example: Reflect&lt;DateTime&gt;.MemberName(s => s.Ticks) returns "Ticks" </summary>
		public static string MemberName(Expression<Action<T>> expression)
		{
			var mce = expression.Body as MethodCallExpression;
			if (mce != null)
				return mce.Method.Name;
			
			var ue = expression.Body as UnaryExpression;
			if (ue != null && ue.NodeType == ExpressionType.Convert)
				return ((MemberExpression) ue.Operand).Member.Name;
			
			return ((MemberExpression) expression.Body).Member.Name;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using util;
	
	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestReflect
		{
			[Test] public static void TestUtils_MemberName()
			{
				Assert.AreEqual("X", Reflect<Point>.MemberName(p => p.X));
				Assert.AreEqual("Offset", Reflect<Point>.MemberName(p => p.Offset(0,0)));
				Assert.AreEqual("BaseStream", Reflect<StreamWriter>.MemberName(s => s.BaseStream));
			}
		}
	}
}
#endif