//***************************************************
// Xml Helper Functions
//  Copyright © Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using pr.extn;
using pr.util;

namespace pr.extn
{
	/// <summary>Xml helper methods</summary>
	public static class XmlExtensions
	{
		private const string TypeAttr = "ty";

		#region ToXml Binding

		/// <summary>Signature of the methods used to convert a type to an XElement</summary>
		public delegate XElement ToFunc(object obj, XElement node);

		/// <summary>A map from type to 'ToXml' method</summary>
		private static readonly ToBinding ToMap = new ToBinding();
		private class ToBinding :Dictionary<Type, ToFunc>
		{
			public ToBinding()
			{
				this[typeof(string         )] = ToXmlDefault;
				this[typeof(bool           )] = ToXmlDefault;
				this[typeof(byte           )] = ToXmlDefault;
				this[typeof(sbyte          )] = ToXmlDefault;
				this[typeof(char           )] = ToXmlDefault;
				this[typeof(short          )] = ToXmlDefault;
				this[typeof(ushort         )] = ToXmlDefault;
				this[typeof(int            )] = ToXmlDefault;
				this[typeof(uint           )] = ToXmlDefault;
				this[typeof(long           )] = ToXmlDefault;
				this[typeof(ulong          )] = ToXmlDefault;
				this[typeof(float          )] = ToXmlDefault;
				this[typeof(double         )] = ToXmlDefault;
				this[typeof(Enum           )] = ToXmlDefault;
				this[typeof(DateTimeOffset )] = ToXmlDefault;
				this[typeof(Guid           )] = ToXmlDefault;
				this[typeof(Color)] = (obj, node) =>
					{
						var col = ((Color)obj).ToArgb().ToString("X8");
						node.SetValue(col);
						return node;
					};
				this[typeof(Size)] = (obj, node) =>
					{
						var sz = (Size)obj;
						node.Add
						(
							sz.Width .ToXml(Reflect<Size>.MemberName(x => x.Width ), false),
							sz.Height.ToXml(Reflect<Size>.MemberName(x => x.Height), false)
						);
						return node;
					};
				this[typeof(Point)] = (obj, node) =>
					{
						var pt = (Point)obj;
						node.Add
						(
							pt.X .ToXml(Reflect<Point>.MemberName(x => x.X), false),
							pt.Y .ToXml(Reflect<Point>.MemberName(x => x.Y), false)
						);
						return node;
					};
				this[typeof(Font)] = (obj, node) =>
					{
						var font = (Font)obj;
						node.Add
						(
							font.FontFamily.Name.ToXml(Reflect<Font>.MemberName(x => x.FontFamily     ), false),
							font.Size           .ToXml(Reflect<Font>.MemberName(x => x.Size           ), false),
							font.Style          .ToXml(Reflect<Font>.MemberName(x => x.Style          ), false),
							font.Unit           .ToXml(Reflect<Font>.MemberName(x => x.Unit           ), false),
							font.GdiCharSet     .ToXml(Reflect<Font>.MemberName(x => x.GdiCharSet     ), false),
							font.GdiVerticalFont.ToXml(Reflect<Font>.MemberName(x => x.GdiVerticalFont), false)
						);
						return node;
					};
			}

			/// <summary>
			/// Saves 'obj' into 'node' using the bound ToXml methods.
			/// 'type_attr' controls whether the 'ty' attribute is added. By default it should
			/// be added so that xml can be deserialised to 'object'. If this is never needed
			/// however it can be omitted.</summary>
			public XElement Convert(object obj, XElement node, bool type_attr)
			{
				if (obj == null)
					return ToXmlDefault(string.Empty, node);

				var type = obj.GetType();
				type = Nullable.GetUnderlyingType(type) ?? type;
				if (type_attr) node.SetAttributeValue(TypeAttr, type.FullName);

				// Handle strings here because they are IEnumerable and
				// enums because the type will not be in the map
				if (type == typeof(string) || type.IsEnum)
					return ToXmlDefault(obj, node);

				// Enumerable objects convert to arrays
				var obj_enum = obj as IEnumerable;
				if (obj_enum != null)
				{
					var name = node.Name.LocalName;
					var elem_name = name.Length > 1 && name.EndsWith("s") ? name.Substring(0,name.Length-1) : "_";
					foreach (var i in obj_enum) node.Add(Convert(i, new XElement(elem_name), type_attr));
					return node;
				}

				// Otherwise, use the bound ToXml function
				ToFunc func = TryGetValue(type, out func) ? func : null;
				for (;func == null;)
				{
					// See if 'obj' has a native 'ToXml' method
					var mi = type.GetMethods().FirstOrDefault(IsToXmlFunc);
					if (mi != null) { func = this[type] = ToXmlMethod; break; }

					// Try DataContract binding
					var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
					if (dca != null) { func = this[type] = ToXmlDataContract; break; }

					throw new NotSupportedException("There is no 'ToXml' binding for type {0}".Fmt(type.Name));
				}
				return func(obj, node);
			}
		}

		/// <summary>Returns true if 'm' is a 'ToXml' method</summary>
		private static bool IsToXmlFunc(MethodInfo m)
		{
			ParameterInfo[] parms;
			return m.Name == "ToXml" && m.ReturnType == typeof(XElement) && (parms = m.GetParameters()).Length == 1 && parms[0].ParameterType == typeof(XElement);
		}

		/// <summary>Return an XElement using reflection.</summary>
		public static XElement ToXmlDefault(object obj, XElement node)
		{
			node.SetValue(obj);
			return node;
		}

		/// <summary>Return an XElement using the 'ToXml' method on the type</summary>
		public static XElement ToXmlMethod(object obj, XElement node)
		{
			// Find the native method on the type
			var type = obj.GetType();
			var mi = type.GetMethods().FirstOrDefault(IsToXmlFunc);
			if (mi == null) throw new NotSupportedException("{0} does not have a 'ToXml' method");

			// Replace the mapping with a call directly to that method
			ToMap[type] = (o,n) => (XElement)mi.Invoke(o, new object[]{n});
			return ToMap[type](obj, node);
		}

		/// <summary>Return an XElement object for a type that specifies the DataContract attribute</summary>
		public static XElement ToXmlDataContract(object obj, XElement node)
		{
			var type = obj.GetType();

			// Look for the DataContract attribute
			var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
			if (dca == null) throw new NotSupportedException("{0} does not have the DataContractAttribute".Fmt(type.Name));

			// Find all fields and properties with the DataMember attribute
			var members =
				type.AllFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Cast<MemberInfo>().Concat(
				type.AllProps(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
				.Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute),false).Any())
				.Select(x => new{Member = x, Attr = (DataMemberAttribute)x.GetCustomAttributes(typeof(DataMemberAttribute),false).First()})
				.ToList();

			// Replace the mapping function with a call that already has the members found
			ToMap[type] = (o,n) =>
				{
					foreach (var m in members)
					{
						var name = m.Attr.Name ?? m.Member.Name;
						var child = new XElement(name);

						// Read the member to be written
						FieldInfo field;
						PropertyInfo prop;
						object val = null;
						if      ((prop  = m.Member as PropertyInfo) != null) val = prop.GetValue(o,null);
						else if ((field = m.Member as FieldInfo   ) != null) val = field.GetValue(o);

						n.Add(ToMap.Convert(val, child, true));
					}
					return n;
				};
			return ToMap[type](obj, node);
		}

		/// <summary>Write this object into an xml node with tag 'elem_name'</summary>
		public static XElement ToXml<T>(this T obj, string elem_name, bool type_attr = true)
		{
			return obj.ToXml(new XElement(elem_name), type_attr);
		}

		/// <summary>Write this object into 'node' as it's value or as child elements. Returns 'node'</summary>
		public static XElement ToXml<T>(this T obj, XElement node, bool type_attr = true)
		{
			return ToMap.Convert(obj, node, type_attr);
		}

		#endregion

		#region As Binding

		/// <summary>Signature of the methods used to convert an XElement to a type instance</summary>
		public delegate object AsFunc(XElement elem, Type type, Func<Type, object> factory);

		/// <summary>A map from type to 'As' method</summary>
		private static readonly AsBinding AsMap = new AsBinding();
		public class AsBinding :Dictionary<Type, AsFunc>
		{
			public AsBinding()
			{
				this[typeof(string         )] = (elem, type, ctor) => elem.Value;
				this[typeof(bool           )] = (elem, type, ctor) => bool           .Parse(elem.Value);
				this[typeof(byte           )] = (elem, type, ctor) => byte           .Parse(elem.Value);
				this[typeof(sbyte          )] = (elem, type, ctor) => sbyte          .Parse(elem.Value);
				this[typeof(char           )] = (elem, type, ctor) => char           .Parse(elem.Value);
				this[typeof(short          )] = (elem, type, ctor) => short          .Parse(elem.Value);
				this[typeof(ushort         )] = (elem, type, ctor) => ushort         .Parse(elem.Value);
				this[typeof(int            )] = (elem, type, ctor) => int            .Parse(elem.Value);
				this[typeof(uint           )] = (elem, type, ctor) => uint           .Parse(elem.Value);
				this[typeof(long           )] = (elem, type, ctor) => long           .Parse(elem.Value);
				this[typeof(ulong          )] = (elem, type, ctor) => ulong          .Parse(elem.Value);
				this[typeof(float          )] = (elem, type, ctor) => float          .Parse(elem.Value);
				this[typeof(double         )] = (elem, type, ctor) => double         .Parse(elem.Value);
				this[typeof(Enum           )] = (elem, type, ctor) => Enum           .Parse(type, elem.Value);
				this[typeof(DateTimeOffset )] = (elem, type, ctor) => DateTimeOffset .Parse(elem.Value);
				this[typeof(Guid           )] = (elem, type, ctor) => Guid           .Parse(elem.Value);
				this[typeof(Color          )] = (elem, type, ctor) => Color          .FromArgb(int.Parse(elem.Value, NumberStyles.HexNumber));
				this[typeof(Size)] = (elem, type, ctor) =>
					{
						var W = elem.Element(Reflect<Size>.MemberName(x => x.Width)).As<int>();
						var H = elem.Element(Reflect<Size>.MemberName(x => x.Height)).As<int>();
						return new Size(W,H);
					};
				this[typeof(Point)] = (elem, type, ctor) =>
					{
						var X = elem.Element(Reflect<Point>.MemberName(x => x.X)).As<int>();
						var Y = elem.Element(Reflect<Point>.MemberName(x => x.Y)).As<int>();
						return new Point(X,Y);
					};
				this[typeof(Font)] = (elem, type, instance) =>
					{
						var font_family       = elem.Element(Reflect<Font>.MemberName(x => x.FontFamily     )).As<string>();
						var size              = elem.Element(Reflect<Font>.MemberName(x => x.Size           )).As<float>();
						var style             = elem.Element(Reflect<Font>.MemberName(x => x.Style          )).As<FontStyle>();
						var unit              = elem.Element(Reflect<Font>.MemberName(x => x.Unit           )).As<GraphicsUnit>();
						var gdi_charset       = elem.Element(Reflect<Font>.MemberName(x => x.GdiCharSet     )).As<byte>();
						var gdi_vertical_font = elem.Element(Reflect<Font>.MemberName(x => x.GdiVerticalFont)).As<bool>();
						return new Font(font_family, size, style, unit, gdi_charset, gdi_vertical_font);
					};
			}

			/// <summary>
			/// Converts 'elem' to 'type'.
			/// If 'type' is 'object' then the type is inferred from the node.
			/// 'factory' is used to construct instances of type, unless type is
			/// an array, in which case factory is used to construct the array elements</summary>
			public object Convert(XElement elem, Type type, Func<Type,object> factory)
			{
				factory = factory ?? Activator.CreateInstance;

				// If 'type' is nullable then returning the underlying type will
				// automatically convert to the nullable type
				var ty = Nullable.GetUnderlyingType(type);
				var is_nullable = ty != null;
				if (is_nullable) type = ty;

				// If 'type' is of type object, then try to read the type from the node
				if (type == typeof(object))
				{
					XAttribute attr;
					if ((attr = elem.Attribute(TypeAttr)) == null)
					{
						// Only throw if a value is given and we can't determine it's type
						if (string.IsNullOrEmpty(elem.Value)) return null;
						throw new Exception("Cannot determine the type for xml element: {0}" + elem);
					}
					type = TypeExtensions.Resolve(attr.Value);
				}

				// Empty elements return null or default instances
				if (string.IsNullOrEmpty(elem.Value))
				{
					if (type == typeof(string))
					{
						return string.Empty;
					}
					if (typeof(IEnumerable).IsAssignableFrom(type))
					{
						var child_type = type.GetElementType();
						return Array.CreateInstance(child_type, 0);
					}
					if (type.IsClass || is_nullable) // includes typeof(object)
					{
						return null;
					}
					return factory(type);
				}

				// If 'type' is an enum, use the enum parse
				if (type.IsEnum)
				{
					return Enum.Parse(type, elem.Value);
				}

				// If 'type' is an array...
				if (type.IsArray)
				{
					var child_type = type.GetElementType();
					var children = elem.Elements().ToList();
					var array = Array.CreateInstance(child_type, children.Count);
					for (int i = 0; i != array.Length; ++i)
						array.SetValue(Convert(children[i], child_type, factory), i); // The 'factory' must handle both the array type and the element types
					return array;
				}

				AsFunc func;

				// Find the function that converts the string to 'type'
				func = TryGetValue(type, out func) ? func : null;
				for (;func == null;)
				{
					// There is no 'As' binding for this type, try a few possibilities
					var ctor = type.GetConstructor(new[]{typeof(XElement)});
					if (ctor != null) { func = this[type] = AsCtor; break; }

					// Try DataContract binding
					var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
					if (dca != null) { func = this[type] = AsDataContract; break; }

					throw new NotSupportedException("No binding for converting XElement to type {0}".Fmt(type.Name));
				}
				return func(elem, type, factory);
			}
		}

		/// <summary>An 'As' method that just uses Convert.ChangeType</summary>
		public static object AsConvert(XElement elem, Type type, Func<Type,object> factory)
		{
			return Convert.ChangeType(elem.Value, type);
		}

		/// <summary>An 'As' method that expects 'type' to have a constructor taking a single XElement argument</summary>
		public static object AsCtor(XElement elem, Type type, Func<Type,object> factory)
		{
			var ctor = type.GetConstructor(new[]{typeof(XElement)});
			if (ctor == null) throw new NotSupportedException("{0} does not have a constructor taking a single XElement argument".Fmt(type.Name));

			// Replace the mapping with a call that doesn't need to search for the constructor
			AsMap[type] = (e,t,i) => ctor.Invoke(new object[]{e});
			return AsMap[type](elem, type, factory);
		}

		/// <summary>An 'As' method for types that specify the DataContract attribute</summary>
		public static object AsDataContract(XElement elem, Type type, Func<Type,object> factory)
		{
			// Look for the DataContract attribute
			var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
			if (dca == null) throw new NotSupportedException("{0} does not have the DataContractAttribute".Fmt(type.Name));

			// Find all fields and properties with the DataMember attribute
			var members =
				type.AllFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Cast<MemberInfo>().Concat(
				type.AllProps(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
				.Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute),false).Any())
				.Select(x => new{Member = x, Attr = (DataMemberAttribute)x.GetCustomAttributes(typeof(DataMemberAttribute),false).First()})
				.OrderBy(x => x.Attr.Name ?? x.Member.Name)
				.ToList();

			// Replace the mapped function with one that doesn't need to search for members
			AsMap[type] = (el,ty,new_inst) =>
				{
					new_inst = new_inst ?? Activator.CreateInstance;

					// Read nodes from the xml, and populate any members with matching names
					object obj = new_inst(ty);
					foreach (var e in el.Elements())
					{
						// Look for the property or field by name
						var name = e.Name.LocalName;
						var m = members.BinarySearchFind(x => string.CompareOrdinal(x.Attr.Name ?? x.Member.Name, name));
						if (m == null) continue;

						FieldInfo field;
						PropertyInfo prop;
						if      ((prop  = m.Member as PropertyInfo) != null) prop.SetValue(obj, AsMap.Convert(e, prop.PropertyType, new_inst), null);
						else if ((field = m.Member as FieldInfo   ) != null) field.SetValue(obj, AsMap.Convert(e, field.FieldType, new_inst));
					}
					return obj;
				};
			return AsMap[type](elem, type, factory);
		}

		/// <summary>Returns this xml node as an instance of the type implied by it's node attributes</summary>
		public static object ToObject(this XElement elem)
		{
			return AsMap.Convert(elem, typeof(object), null);
		}

		/// <summary>
		/// Return this element as an instance of 'T'.
		/// 'factory' can be used to create new instances of 'T' if doesn't have a default constructor</summary>
		public static T As<T>(this XElement elem, Func<Type,object> factory = null)
		{
			return (T)AsMap.Convert(elem, typeof(T), factory);
		}

		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'factory' and optionally overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, Func<T,T,bool> is_duplicate = null, Func<Type,object> factory = null)
		{
			var count = 0;
			foreach (var e in parent.Elements(elem_name))
			{
				var item = e.As<T>(factory);
				var index = count;
				if (is_duplicate != null)
				{
					for (int i = 0; i != list.Count; ++i)
						if (is_duplicate(list[i], item)) { index = i; break; }
				}
				if (index != list.Count) list[index] = item;
				else list.Add(item);
				++count;
			}
		}

		#endregion

		/// <summary>Add 'child' to this element and return child. Useful for the syntax: "var thing = root.Add2(new Thing());"</summary>
		public static T Add2<T>(this XContainer parent, T child)
		{
			parent.Add(child);
			return child;
		}

		/// <summary>Add 'child' to this element and return child. Useful for the syntax: "var element = root.Add2("name", child);"</summary>
		public static XElement Add2(this XContainer parent, string name, object child)
		{
			var elem = child.ToXml(name);
			return parent.Add2(elem);
		}

		/// <summary>Adds a list of elements of type 'T' each with name 'elem_name' within an element called 'name'</summary>
		public static XElement Add2<T>(this XContainer parent, string list_name, string elem_name, IEnumerable<T> list)
		{
			var list_elem = parent.Add2(new XElement(list_name));
			foreach (var item in list) list_elem.Add2(elem_name, item);
			return list_elem;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using System.Drawing;
	using NUnit.Framework;
	using util;

	[TestFixture] internal partial class UnitTests
	{
		internal static class TestXml
		{
			// ReSharper disable UnusedMember.Local
			private enum EEnum
			{
				Dog
			}

			/// <summary>A custom type with an XElement constructor and explicit ToXml() method</summary>
			private class Elem1
			{
				public readonly uint m_int;

				public Elem1() :this(0)              {}
				public Elem1(uint i)                 { m_int = i; }
				public Elem1(XElement elem)          { m_int = uint.Parse(elem.Value.Substring(5)); }
				public XElement ToXml(XElement node) { node.SetValue("elem_"+m_int); return node; }
				public override int GetHashCode()    { return (int)m_int; }
				private bool Equals(Elem1 other)     { return m_int == other.m_int; }
				public override bool Equals(object obj)
				{
					if (ReferenceEquals(null, obj)) return false;
					if (ReferenceEquals(this, obj)) return true;
					if (obj.GetType() != GetType()) return false;
					return Equals((Elem1)obj);
				}
			}

			/// <summary>A custom DataContract type without a default constructor</summary>
			[DataContract(Name = "ELEM2")] internal class Elem2
			{
				[DataMember(Name = "eye")] public readonly int m_int;
				[DataMember]               public readonly string m_string;

				public Elem2(int i, string s)      { m_int = i; m_string = s; }
				public override string ToString()  { return m_int.ToString(CultureInfo.InvariantCulture) + " " + m_string; }
				public override int GetHashCode()  { unchecked { return (m_int * 397) ^ (m_string != null ? m_string.GetHashCode() : 0); } }
				private bool Equals(Elem2 other)   { return m_int == other.m_int && string.Equals(m_string, other.m_string); }
				public override bool Equals(object obj)
				{
					if (ReferenceEquals(null, obj)) return false;
					if (ReferenceEquals(this, obj)) return true;
					if (obj.GetType() != GetType()) return false;
					return Equals((Elem2)obj);
				}
			}

			/// <summary>A custom DataContract type with a default constructor</summary>
			[DataContract(Name = "ELEM3")] internal class Elem3
			{
				[DataMember] public readonly int m_int;
				[DataMember] public readonly string m_string;

				public Elem3()                     { m_int = 0; m_string = string.Empty; }
				public Elem3(int i, string s)      { m_int = i; m_string = s; }
				public override int GetHashCode()  { unchecked { return (m_int*397) ^ (m_string != null ? m_string.GetHashCode() : 0); } }
				private bool Equals(Elem3 other)   { return m_int == other.m_int && string.Equals(m_string,other.m_string); }
				public override bool Equals(object obj)
				{
					if (ReferenceEquals(null,obj)) return false;
					if (ReferenceEquals(this,obj)) return true;
					if (obj.GetType() != GetType()) return false;
					return Equals((Elem3)obj);
				}
			}
			// ReSharper restore UnusedMember.Local

			[Test] public static void TestToXml1()
			{
				// Built in types
				var node = 5.ToXml("five");
				var five = node.As<int>();
				Assert.AreEqual(5, five);
			}
			[Test] public static void TestToXml2()
			{
				var font = SystemFonts.DefaultFont;
				var node = font.ToXml("font", false);
				var FONT = node.As<Font>();
				Assert.IsTrue(font.Equals(FONT));
			}
			[Test] public static void TestToXml3()
			{
				var pt = new Point(1,2);
				var node = pt.ToXml("pt");
				var PT = node.As<Point>();
				Assert.IsTrue(pt.Equals(PT));
			}
			[Test] public static void TestToXml4()
			{
				var node = DateTimeOffset.MinValue.ToXml("min_time");
				var dto = node.As<DateTimeOffset>();
				Assert.AreEqual(DateTimeOffset.MinValue, dto);
			}
			[Test] public static void TestToXml5()
			{
				var guid = Guid.NewGuid();
				var node = guid.ToXml("guid");
				var GUID = node.As<Guid>();
				Assert.AreEqual(guid, GUID);
			}
			[Test] public static void TestToXml6()
			{
				// XElement constructible class
				var node = new Elem1(4).ToXml("four");
				var four = node.As<Elem1>();
				Assert.AreEqual(4, four.m_int);
			}
			[Test] public static void TestToXml7()
			{
				// DC class
				var dc = new Elem2(2,"3");
				var node = dc.ToXml("dc");
				var DC = node.As<Elem2>(t => new Elem2(0,null));
				Assert.AreEqual(dc.m_int, DC.m_int);
				Assert.AreEqual(dc.m_string, DC.m_string);
			}
			[Test] public static void TestToXml8()
			{
				// Arrays
				var arr = new[]{0,1,2,3,4};
				var node = arr.ToXml("arr");
				var ARR = node.As<int[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml9()
			{
				var arr = new[]{"hello", "world"};
				var node = arr.ToXml("arr");
				var ARR = node.As<string[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml10()
			{
				var arr = new[]{new Point(1,1), new Point(2,2)};
				var node = arr.ToXml("arr");
				var ARR = node.As<Point[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml11()
			{
				var arr = new[]{new Elem2(1,"1"), new Elem2(2,"2"), new Elem2(3,"3")};
				var node = arr.ToXml("arr");
				var ARR = node.As<Elem2[]>(t => new Elem2(0,null));
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml12()
			{
				var arr = new[]{new Elem2(1,"1"), null, new Elem2(3,"3")};
				var node = arr.ToXml("arr");
				var ARR = node.As<Elem2[]>(t => new Elem2(0,""));
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml13()
			{
				// nullables
				int? three = 3;
				var node = three.ToXml("three");
				var THREE = node.As<int?>();
				Assert.True(three == THREE);
			}
			[Test] public static void TestToXml14()
			{
				var arr = new int?[]{1, null, 2};
				var node = arr.ToXml("arr");
				var ARR = node.As<int?[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml15()
			{
				var arr = new object[]{null, new Elem1(1), new Elem2(2,"2"), new Elem3(3,"3")};
				var node = arr.ToXml("arr");
				var ARR = node.As<object[]>(ty =>
					{
						if (ty == typeof(Elem1)) return new Elem1();
						if (ty == typeof(Elem2)) return new Elem2(0,string.Empty);
						if (ty == typeof(Elem3)) return new Elem3();
						throw new Exception("Unexpected type");
					});
				Assert.True(arr.SequenceEqual(ARR));
			}
			[Test] public static void TestToXml16()
			{
			}
			[Test] public static void TestXmlAs()
			{
				var xml = new XDocument(
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

				var ints = new List<int>();
				root.Element("e").As(ints, "i");
				Assert.AreEqual(5, ints.Count);
				for (int i = 0; i != 4; ++i)
					Assert.AreEqual(i, ints[i]);

				var chars = new List<char>();
				root.Element("e").As(chars, "j");
				Assert.AreEqual(5, chars.Count);
				for (int j = 0; j != 4; ++j)
					Assert.AreEqual('a' + j, chars[j]);

				ints.Clear();
				root.Element("e").As(ints, "i", (lhs,rhs) => lhs == rhs);
				Assert.AreEqual(4, ints.Count);
				for (int i = 0; i != 4; ++i)
					Assert.AreEqual(i, ints[i]);
			}
			[Test] public static void TestXmlAdd()
			{
				var xml  = new XDocument();
				var cmt  = xml.Add2(new XComment("comments"));  Assert.AreEqual(cmt.Value, "comments");
				var root = xml.Add2(new XElement("root"));      Assert.AreSame(xml.Root, root);

				var ints = new List<int>{0,1,2,3,4};
				var elems = Util.NewArray(5, i => new Elem1(i));
				string s;

				var xint = root.Add2("elem", 42);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
						"<elem ty=\"System.Int32\">42</elem>" +
					"</root>"
					,s);
				xint.Remove();

				var xelem = root.Add2("elem", elems[0]);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
						"<elem ty=\"pr.UnitTests+TestXml+Elem1\">elem_0</elem>" +
					"</root>"
					,s);
				xelem.Remove();

				var xints = root.Add2("ints", "i", ints);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
						"<ints>" +
							"<i ty=\"System.Int32\">0</i>" +
							"<i ty=\"System.Int32\">1</i>" +
							"<i ty=\"System.Int32\">2</i>" +
							"<i ty=\"System.Int32\">3</i>" +
							"<i ty=\"System.Int32\">4</i>" +
						"</ints>" +
					"</root>"
					,s);
				xints.Remove();

				var xelems = root.Add2("elems", "i", elems);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
						"<elems>" +
							"<i ty=\"pr.UnitTests+TestXml+Elem1\">elem_0</i>" +
							"<i ty=\"pr.UnitTests+TestXml+Elem1\">elem_1</i>" +
							"<i ty=\"pr.UnitTests+TestXml+Elem1\">elem_2</i>" +
							"<i ty=\"pr.UnitTests+TestXml+Elem1\">elem_3</i>" +
							"<i ty=\"pr.UnitTests+TestXml+Elem1\">elem_4</i>" +
						"</elems>" +
					"</root>"
					,s);
				xelems.Remove();
			}
		}
	}
}

#endif
