//***************************************************
// Xml Helper Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using pr.extn;

namespace pr.extn
{
	// Example:
	//public class Thing :IToXml
	//{
	//    readonly double m_field = 0;
	//    public Thing() {}
	//    public Thing(XElement node) :this()
	//    {
	//        foreach (XElement n in node.Elements())
	//        {
	//            switch (n.Name.LocalName){
	//            default: break;
	//            case "whatever": m_field = n.As<double>(); break;
	//            }
	//        }
	//    }
	//    public XElement ToXml(string elem_name)
	//    {
	//        XElement node = new XElement(elem_name);
	//        node.Add2("whatever", m_field);
	//        return node;
	//    }
	//}

	public interface IToXml
	{
		/// <summary>Return a representation of this object as an XElement</summary>
		XElement ToXml(string elem_name);
	}
	
	/// <summary>Xml helper methods</summary>
	public static class XmlExtensions
	{
		/// <summary>Return the contents of this node as a 'T'</summary>
		public static T As<T>(this XElement node)
		{
			if (typeof(T).IsEnum) return (T)Enum.Parse(typeof(T), node.Value);
			return (T)Convert.ChangeType(node.Value, typeof(T));
		}

		/// <summary>Return the contents of this node as a 'T' constructed using 'construct'</summary>
		public static T As<T>(this XElement node, Func<XElement,T> construct)
		{
			return construct(node);
		}
		
		/// <summary>Read all elements with name 'elem_name' into 'list'</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name)
		{
			parent.As(list, elem_name, null, null);
		}
		
		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'construct'</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, Func<XElement,T> construct)
		{
			parent.As(list, elem_name, construct, null);
		}
		
		/// <summary>Read all elements with name 'elem_name' into 'list' overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, Func<T,T,bool> is_duplicate)
		{
			parent.As(list, elem_name, null, is_duplicate);
		}
		
		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'construct' and overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, Func<XElement,T> construct, Func<T,T,bool> is_duplicate)
		{
			int count = 0;
			foreach (XElement n in parent.Elements(elem_name))
			{
				T item = construct != null ? n.As(construct) : n.As<T>();
				int index = count;
				if (is_duplicate != null) 
				{
					for (int i = 0; i != list.Count; ++i)
						if (is_duplicate(list[i], item)) { index = i; break; }
				}
				if (index != list.Count || list is Array) list[index] = item;
				else list.Add(item);
				++count;
			}
		}
		
		/// <summary>Add 'child' to this element and return child. Useful for the syntax: "XElement elem = root.Add2(new XElement("child"));"</summary>
		public static T Add2<T>(this XContainer parent, T child)
		{
			parent.Add(child);
			return child;
		}
		
		/// <summary>Add 'child' to this element and return the created XElement. Useful for the syntax: "XElement elem = root.Add2("child", value);"</summary>
		public static XElement Add2<T>(this XContainer parent, string name, T child)
		{
			if (typeof(IToXml).IsAssignableFrom(typeof(T)))
				return parent.Add2(((IToXml)child).ToXml(name));
			return parent.Add2(new XElement(name, child.ToString()));
		}
		
		/// <summary>Adds a list of elements of type 'T' each with name 'elem_name' within an element called 'name'</summary>
		public static XElement Add2<T>(this XContainer parent, string name, string elem_name, IEnumerable<T> list)
		{
			XElement node = parent.Add2(new XElement(name));
			if (typeof(IToXml).IsAssignableFrom(typeof(T)))
				foreach (T elem in list) node.Add(((IToXml)elem).ToXml(elem_name));
			else
				foreach (T elem in list) node.Add(new XElement(elem_name, elem.ToString()));
			return node;
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using util;

	[TestFixture] internal partial class UnitTests
	{
		internal static class TestXml
		{
			private enum EEnum { Dog }
			[Test] public static void TestXmlAs()
			{
				XDocument xml = new XDocument(
					new XElement("root",
						new XElement("a", "1"),
						new XElement("b", "2.0"),
						new XElement("c", "cat"),
						new XElement("d", "Dog"),
						new XElement("e",
							new XElement("i", "0"),
							new XElement("j", "a"),
							new XElement("i", "1"),
							new XElement("j", "b"),
							new XElement("i", "2"),
							new XElement("j", "c"),
							new XElement("i", "3"),
							new XElement("j", "d"),
							new XElement("i", "3"),
							new XElement("j", "d")
							)
						)
					);

				XElement root = xml.Root;
				Assert.NotNull(root);
				Assert.AreEqual(1         ,root.Element("a").As<int>());
				Assert.AreEqual(2.0f      ,root.Element("b").As<float>());
				Assert.AreEqual("cat"     ,root.Element("c").As<string>());
				Assert.AreEqual(EEnum.Dog ,root.Element("d").As<EEnum>());
				Assert.AreEqual(3         ,root.Element("a").As(n => 2+n.As<int>()));

				List<int> ints = new List<int>();
				root.Element("e").As(ints, "i"); Assert.AreEqual(5, ints.Count);
				for (int i = 0; i != 4; ++i) Assert.AreEqual(i, ints[i]);

				List<char> chars = new List<char>();
				root.Element("e").As(chars, "j"); Assert.AreEqual(5, chars.Count);
				for (int j = 0; j != 4; ++j) Assert.AreEqual('a' + j, chars[j]);

				ints.Clear();
				root.Element("e").As(ints, "i", (lhs,rhs) => lhs == rhs); Assert.AreEqual(4, ints.Count);
				for (int i = 0; i != 4; ++i) Assert.AreEqual(i, ints[i]);

				ints.Clear();
				root.Element("e").As(ints, "i", n => 2+n.As<int>()); Assert.AreEqual(5, ints.Count);
				for (int i = 0; i != 4; ++i) Assert.AreEqual(2+i, ints[i]);
			}
	
			internal class Elem :IToXml
			{
				private readonly uint m_i;
				public Elem(uint i) { m_i = i; }
				public XElement ToXml(string name) { return new XElement(name, "elem_"+m_i); }
			}
			[Test] public static void TextXmlAdd()
			{
				XDocument xml = new XDocument();
				XComment cmt  = xml.Add2(new XComment("comments"));  Assert.AreEqual(cmt.Value, "comments");
				XElement root = xml.Add2(new XElement("root"));      Assert.AreSame(xml.Root, root);
			
				List<int> ints = new List<int>{0,1,2,3,4};
				Elem[] elems = Util.NewArray(5, i => new Elem(i));
				string s;

				XElement xint = root.Add2("elem", 42);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual("<root><elem>42</elem></root>", s);
				xint.Remove();

				XElement xelem = root.Add2("elem", elems[0]);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual("<root><elem>elem_0</elem></root>", s);
				xelem.Remove();

				XElement xints = root.Add2("ints", "i", ints);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual("<root><ints><i>0</i><i>1</i><i>2</i><i>3</i><i>4</i></ints></root>", s);
				xints.Remove();

				Assert.True(typeof(IToXml).IsAssignableFrom(typeof(Elem)));
				XElement xelems = root.Add2("elems", "i", elems);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual("<root><elems><i>elem_0</i><i>elem_1</i><i>elem_2</i><i>elem_3</i><i>elem_4</i></elems></root>", s);
				xelems.Remove();
			}
		}
	}
}
#endif
