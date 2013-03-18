using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace pr.extn
{
	public static class ObjectExtensions
	{
		/// <summary>Returns a string containing a description of this object and its member values</summary>
		public static string Dump(this object obj)
		{
			if (obj == null) return "null";
			var type = obj.GetType();
			return
				"{0}:\n".Fmt(type.Name) +
				string.Join("\n", type.GetProperties().Select(p => p.Name + ":  " + p.GetValue(obj, null)))+
				"\n";
		}

		// Doesn't quite work.. :(
		///// <summary>Returns a shallow copy of 'obj'</summary>
		//public static T ShallowCopy<T>(this T obj) where T :new()
		//{
		//    return ObjectExtCloneCache<T>.ShallowCopy(obj);
		//}

		///// <summary>Returns a shallow copy of 'obj' using its underlying type</summary>
		//public static object ShallowCopy(this object obj)
		//{
		//    var x = typeof(ObjectExtCloneCache<>).MakeGenericType(obj.GetType());
		//    var m = x.GetMethod("ShallowCopy", BindingFlags.Static|BindingFlags.Public);
		//    return m.Invoke(null, new[]{obj});
		//}
		//public static class ObjectExtCloneCache<T> where T :new()
		//{
		//    private static readonly Func<T,T> m_cloner;
		//    public static T ShallowCopy(T obj)
		//    {
		//        return m_cloner(obj);
		//    }
		//    static ObjectExtCloneCache()
		//    {
		//        var fields = typeof(T).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public); // all public and private inherited fields
		//        var param = Expression.Parameter(typeof(T), "in");
		//        var bindings = fields.Select(f => (MemberBinding)Expression.Bind(f, Expression.Field(param,f)));
		//        m_cloner = Expression.Lambda<Func<T,T>>(Expression.MemberInit(Expression.New(typeof(T)), bindings), param).Compile(DebugInfoGenerator.CreatePdbGenerator());
		//    }
		//}
	}
}
#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using extn;
	
	[TestFixture] internal static partial class UnitTests
	{
		internal static class TextObjectExtensions
		{
			//public class Thing
			//{
			//    private readonly int PrivateField;
			//    protected int ProtectedField;
			//    internal int  InternalField;
			//    public string PublicField;
			//    private int   PrivateAutoProp { get; set; }
			//    protected int ProtectedAutoProp { get; set; }
			//    internal int  InternalAutoProp { get; set; }
			//    public string PublicAutoProp { get; set; }
			
			//    public Thing() {}
			//    public Thing(int i)
			//    {
			//        PrivateField      = i;
			//        ProtectedField    = i;
			//        InternalField     = i;
			//        PublicField       = i.ToString();
			//        PrivateAutoProp   = i;
			//        ProtectedAutoProp = i;
			//        InternalAutoProp  = i;
			//        PublicAutoProp    = i.ToString();
			//    }
			//    public bool Equals(Thing other)
			//    {
			//        return
			//        PrivateField      == other.PrivateField      &&
			//        ProtectedField    == other.ProtectedField    &&
			//        InternalField     == other.InternalField     &&
			//        PublicField       == other.PublicField       &&
			//        PrivateAutoProp   == other.PrivateAutoProp   &&
			//        ProtectedAutoProp == other.ProtectedAutoProp &&
			//        InternalAutoProp  == other.InternalAutoProp  &&
			//        PublicAutoProp    == other.PublicAutoProp    ;
			//    }
			//}
			//[Test] public static void ShallowCopy()
			//{
			//    var t0 = new Thing(1);
			//    var t1 = t0.ShallowCopy();
			//    Assert.IsTrue(t0.Equals(t1));

			//    object t2 = t0;
			//    object t3 = t2.ShallowCopy();
			//    Assert.IsTrue(((Thing)t2).Equals((Thing)t3));
			//}
		}
	}
}
#endif