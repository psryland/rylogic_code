//***************************************************
// Xml Helper Functions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;
using pr.maths;
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

		/// <summary>
		/// A map from type to 'ToXml' method
		/// User ToXml functions can be added to this map.
		/// Note, they are only needed if ToBinding.Convert() method fails</summary>
		public static ToBinding ToMap { [DebuggerStepThrough] get { return m_impl_ToMap; } }
		private static readonly ToBinding m_impl_ToMap = new ToBinding();
		public class ToBinding :Dictionary<Type, ToFunc>
		{
			public ToBinding()
			{
				this[typeof(XElement)] = (obj, node) =>
				{
					node.Add(obj);
					return node;
				};
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
						node.SetValue("{0} {1}".Fmt(sz.Width, sz.Height));
						return node;
					};
				this[typeof(SizeF)] = (obj, node) =>
					{
						var sz = (SizeF)obj;
						node.SetValue("{0} {1}".Fmt(sz.Width, sz.Height));
						return node;
					};
				this[typeof(Point)] = (obj, node) =>
					{
						var pt = (Point)obj;
						node.SetValue("{0} {1}".Fmt(pt.X, pt.Y));
						return node;
					};
				this[typeof(PointF)] = (obj, node) =>
					{
						var pt = (PointF)obj;
						node.SetValue("{0} {1}".Fmt(pt.X, pt.Y));
						return node;
					};
				this[typeof(Rectangle)] = (obj, node) =>
					{
						var rc = (Rectangle)obj;
						node.SetValue("{0} {1} {2} {3}".Fmt(rc.X, rc.Y, rc.Width, rc.Height));
						return node;
					};
				this[typeof(RectangleF)] = (obj, node) =>
					{
						var rc = (RectangleF)obj;
						node.SetValue("{0} {1} {2} {3}".Fmt(rc.X, rc.Y, rc.Width, rc.Height));
						return node;
					};
				this[typeof(Padding)] = (obj, node) =>
					{
						var p = (Padding)obj;
						node.SetValue("{0} {1} {2} {3}".Fmt(p.Left, p.Top, p.Right, p.Bottom));
						return node;
					};
				this[typeof(Font)] = (obj, node) =>
					{
						var font = (Font)obj;
						node.Add(
							font.FontFamily.Name.ToXml(Reflect<Font>.MemberName(x => x.FontFamily     ), false),
							font.Size           .ToXml(Reflect<Font>.MemberName(x => x.Size           ), false),
							font.Style          .ToXml(Reflect<Font>.MemberName(x => x.Style          ), false),
							font.Unit           .ToXml(Reflect<Font>.MemberName(x => x.Unit           ), false),
							font.GdiCharSet     .ToXml(Reflect<Font>.MemberName(x => x.GdiCharSet     ), false),
							font.GdiVerticalFont.ToXml(Reflect<Font>.MemberName(x => x.GdiVerticalFont), false));
						return node;
					};
				this[typeof(v2)] = (obj, node) =>
					{
						var vec = (v2)obj;
						node.SetValue(vec.ToString());
						return node;
					};
				this[typeof(v4)] = (obj, node) =>
					{
						var vec = (v4)obj;
						node.SetValue(vec.ToString4());
						return node;
					};
				this[typeof(m4x4)] = (obj, node) =>
					{
						var mat = (m4x4)obj;
						node.Add(
							mat.x.ToXml(Reflect<m4x4>.MemberName(x => x.x), false),
							mat.y.ToXml(Reflect<m4x4>.MemberName(x => x.y), false),
							mat.z.ToXml(Reflect<m4x4>.MemberName(x => x.z), false),
							mat.w.ToXml(Reflect<m4x4>.MemberName(x => x.w), false));
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
					var mi = type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(IsToXmlFunc);
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
			var mi = type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(IsToXmlFunc);
			if (mi == null) throw new NotSupportedException("{0} does not have a 'ToXml' method".Fmt(type.Name));

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
		public static XElement ToXml<T>(this T obj, string elem_name, bool type_attr)
		{
			return obj.ToXml(new XElement(elem_name), type_attr);
		}

		/// <summary>Write this object into 'node' as it's value or as child elements. Returns 'node'</summary>
		public static XElement ToXml<T>(this T obj, XElement node, bool type_attr)
		{
			return ToMap.Convert(obj, node, type_attr);
		}

		#endregion

		#region As Binding

		/// <summary>Signature of the methods used to convert an XElement to a type instance</summary>
		public delegate object AsFunc(XElement elem, Type type, Func<Type, object> factory);

		/// <summary>
		/// A map from type to 'As' method
		/// User conversion functions can be added to this map
		/// Note, they are only needed if AsBinding.Convert() method fails</summary>
		public static AsBinding AsMap { [DebuggerStepThrough] get { return m_impl_AsMap; } }
		private static readonly AsBinding m_impl_AsMap = new AsBinding();
		public class AsBinding :Dictionary<Type, AsFunc>
		{
			private readonly char[] WhiteSpace = new[]{' ','\t','\r','\n','\v'};
			public AsBinding()
			{
				this[typeof(XElement       )] = (elem, type, ctor) => elem;
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
						var wh = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new Size(int.Parse(wh[0]), int.Parse(wh[1]));
					};
				this[typeof(SizeF)] = (elem, type, ctor) =>
					{
						var wh = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new SizeF(float.Parse(wh[0]), float.Parse(wh[1]));
					};
				this[typeof(Point)] = (elem, type, ctor) =>
					{
						var xy = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new Point(int.Parse(xy[0]), int.Parse(xy[1]));
					};
				this[typeof(PointF)] = (elem, type, ctor) =>
					{
						var xy = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new PointF(float.Parse(xy[0]), float.Parse(xy[1]));
					};
				this[typeof(Rectangle)] = (elem, type, ctor) =>
					{
						var xywh = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new Rectangle(int.Parse(xywh[0]), int.Parse(xywh[1]), int.Parse(xywh[2]), int.Parse(xywh[3]));
					};
				this[typeof(RectangleF)] = (elem, type, ctor) =>
					{
						var xywh = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new RectangleF(float.Parse(xywh[0]), float.Parse(xywh[1]), float.Parse(xywh[2]), float.Parse(xywh[3]));
					};
				this[typeof(Padding)] = (elem, type, ctor) =>
					{
						var ltrb = elem.As<string>().Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries);
						return new Padding(int.Parse(ltrb[0]), int.Parse(ltrb[1]), int.Parse(ltrb[2]), int.Parse(ltrb[3]));
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
				this[typeof(v2)] = (elem, type, instance) =>
					{
						return v2.Parse(elem.Value);
					};
				this[typeof(v4)] = (elem, type, instance) =>
					{
						return v4.Parse4(elem.Value);
					};
				this[typeof(m4x4)] = (elem, type, instance) =>
					{
						var x = elem.Element(Reflect<m4x4>.MemberName(m => m.x)).As<v4>();
						var y = elem.Element(Reflect<m4x4>.MemberName(m => m.y)).As<v4>();
						var z = elem.Element(Reflect<m4x4>.MemberName(m => m.z)).As<v4>();
						var w = elem.Element(Reflect<m4x4>.MemberName(m => m.w)).As<v4>();
						return new m4x4(x,y,z,w);
					};
			}

			/// <summary>
			/// Converts 'elem' to 'type'.
			/// If 'type' is 'object' then the type is inferred from the node.
			/// 'factory' is used to construct instances of type, unless type is
			/// an array, in which case factory is used to construct the array elements</summary>
			public object Convert(XElement elem, Type type, Func<Type,object> factory_)
			{
				var factory = factory_ ?? Activator.CreateInstance;

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
				if (!elem.HasElements && !elem.Value.HasValue())
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
				for (;func == null;) // There is no 'As' binding for this type, try a few possibilities
				{
					// Try a constructor that takes a single XElement parameter
					var ctor = type.GetConstructor(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(XElement)}, null);
					if (ctor != null) { func = this[type] = AsCtor; break; }

					// Try a method called 'FromXml' that takes a single XElement parameter
					var method = type.GetMethod("FromXml", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(XElement)}, null);
					if (method != null) { func = this[type] = AsFromXmlMethod; break; }

					// Try DataContract binding
					var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
					if (dca != null) { func = this[type] = AsDataContract; break; }

					// If a factory method has been supplied, rely on that
					if (factory_ != null) { func = this[type] = AsFromFactory; break; }

					// Note, the constructor can be private, but not inherited
					Debug.Assert(false, "{0} does not have a constructor taking an XElement parameter or is not a data contract class".Fmt(type.Name));
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
			var ctor = type.GetConstructor(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(XElement)}, null);
			if (ctor == null) throw new NotSupportedException("{0} does not have a constructor taking a single XElement argument".Fmt(type.Name));

			// Replace the mapping with a call that doesn't need to search for the constructor
			AsMap[type] = (e,t,i) => ctor.Invoke(new object[]{e});
			return AsMap[type](elem, type, factory);
		}

		/// <summary>An 'As' method that expects 'type' to have a method called 'FromXml' taking a single XElement argument</summary>
		public static object AsFromXmlMethod(XElement elem, Type type, Func<Type,object> factory)
		{
			var method = type.GetMethod("FromXml", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(XElement)}, null);
			if (method == null) throw new NotSupportedException("{0} does not have a method called 'FromXml(XElement)'".Fmt(type.Name));

			// Replace the mapping with a call that doesn't need to search for the constructor
			AsMap[type] = (e,t,i) =>
				{
					var obj = factory(type);
					method.Invoke(obj, new object[]{e});
					return obj;
				};
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

					// This will always deserialise as a default object ignoring the elements
					if (members.Count == 0 && el.HasElements)
						throw new Exception("{0} has the DataContract attribute, but no DataMembers.".Fmt(ty.Name));

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
		
		/// <summary>An 'As' method that relies of the factory function to read 'elem'</summary>
		public static object AsFromFactory(XElement elem, Type type, Func<Type,object> factory)
		{
			return factory(type);
		}

		/// <summary>Returns this xml node as an instance of the type implied by it's node attributes</summary>
		public static object ToObject(this XElement elem, Func<Type,object> factory = null)
		{
			// Passing a factory function to handle any of the types 'elem' might be
			// can work, but the factory function won't be available further down the
			// hierarchy and so is of limited use. A better way is to add custom handlers
			// to the 'AsMap'.
			if (elem == null) throw new ArgumentNullException("xml element is null. Key not found?");
			return AsMap.Convert(elem, typeof(object), factory);
		}

		/// <summary>Returns this xml node as an instance of the type implied by it's node attributes</summary>
		public static object ToObject(this XElement elem, object optional_default, Func<Type,object> factory = null)
		{
			if (elem == null) return optional_default;
			return ToObject(elem, factory);
		}

		/// <summary>
		/// Return this element as an instance of 'T'.
		/// Use 'ToObject' if the type should be inferred from the node attributes
		/// 'factory' can be used to create new instances of 'T' if doesn't have a default constructor.
		/// E.g:<para\>
		///  val = node.Element("val").As&lt;int&gt;() <para\>
		///  val = node.Element("val").As&lt;int&gt;(default_val) <para\></summary>
		public static T As<T>(this XElement elem, Func<Type,object> factory = null)
		{
			if (elem == null) throw new ArgumentNullException("xml element is null. Key not found?");
			return (T)AsMap.Convert(elem, typeof(T), factory);
		}

		/// <summary>
		/// Return this element as an instance of 'T'.<para/>
		/// Use 'ToObject' if the type should be inferred from the node attributes<para/>
		/// 'factory' can be used to create new instances of 'T' if doesn't have a default constructor.<para/>
		/// E.g:<para/>
		///  val = node.Element("val").As&lt;int&gt;(); <para/>
		///  val = node.Element("val").As&lt;int&gt;(default_val); <para/></summary>
		public static T As<T>(this XElement elem, T optional_default, Func<Type,object> factory = null)
		{
			if (elem == null) return optional_default;
			return As<T>(elem, factory);
		}

		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'factory' and optionally overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, T optional_default, Func<T,T,bool> is_duplicate = null, Func<Type,object> factory = null)
		{
			AsList<T>(parent, list, elem_name, e => e.As<T>(optional_default, factory), is_duplicate);
		}

		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'factory' and optionally overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, Func<T,T,bool> is_duplicate = null, Func<Type,object> factory = null)
		{
			AsList<T>(parent, list, elem_name, e => e.As<T>(factory), is_duplicate);
		}
		private static void AsList<T>(XElement parent, IList<T> list, string elem_name, Func<XElement,T> conv, Func<T,T,bool> is_duplicate)
		{
			if (parent == null) throw new ArgumentNullException("xml element is null. Key not found?");

			var count = 0;
			foreach (var e in parent.Elements(elem_name))
			{
				var item = conv(e);
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

		/// <summary>
		/// Add 'child' to this element and return child. Useful for the syntax: "var thing = root.Add2(new Thing());"
		/// WARNING: returns the *child* object added, *not* parent.</summary>
		public static T Add2<T>(this XContainer parent, T child)
		{
			parent.Add(child);
			return child;
		}

		/// <summary>
		/// Add 'child' to this element and return child. Useful for the syntax: "var element = root.Add2("name", child);"
		/// WARNING: returns the *child* object added, *not* parent.
		/// 'type_attr' is needed when the type of 'child' is not known at load time.</summary>
		public static XElement Add2(this XContainer parent, string name, object child, bool type_attr)
		{
			var elem = child.ToXml(name, type_attr);
			return parent.Add2(elem);
		}

		/// <summary>Adds a list of elements of type 'T' each with name 'elem_name' within an element called 'name'</summary>
		public static XElement Add2<T>(this XContainer parent, string list_name, string elem_name, IEnumerable<T> list, bool type_attr)
		{
			var list_elem = parent.Add2(new XElement(list_name));
			foreach (var item in list) list_elem.Add2(elem_name, item, type_attr);
			return list_elem;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Drawing;
	using util;

	[TestFixture] public class TestXml
		{
		#region Types
			private enum EEnum
			{
				Dog
			}

			/// <summary>A custom type with an XElement constructor and explicit ToXml() method</summary>
			private class Elem1
			{
			public readonly uint m_uint;

				public Elem1() :this(0)              {}
			public Elem1(uint i)                 { m_uint = i; }
			public Elem1(XElement elem)          { m_uint = uint.Parse(elem.Value.Substring(5)); }
			public XElement ToXml(XElement node) { node.SetValue("elem_"+m_uint); return node; }
			public override int GetHashCode()    { return (int)m_uint; }
			private bool Equals(Elem1 other)     { return m_uint == other.m_uint; }
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

			private class Elem4
			{
				public int m_int;
				public XElement ToXml(XElement node) { node.SetValue("elem_"+m_int); return node; }
				public void FromXml(XElement node) { m_int = int.Parse(node.Value.Substring(5)); }
			}
		#endregion

		[Test] public void ToXml1()
			{
				// Built in types
				var node = 5.ToXml("five", false);
				var five = node.As<int>();
				Assert.AreEqual(5, five);
			}
		[Test] public void ToXml2()
			{
				var font = SystemFonts.DefaultFont;
				var node = font.ToXml("font", false);
				var FONT = node.As<Font>();
			Assert.True(font.Equals(FONT));
			}
		[Test] public void ToXml3()
			{
				var pt = new Point(1,2);
				var node = pt.ToXml("pt", false);
				var PT = node.As<Point>();
			Assert.True(pt.Equals(PT));
			}
		[Test] public void ToXml3a()
			{
				var pt = new PointF(1f,2f);
				var node = pt.ToXml("pt", false);
				var PT = node.As<PointF>();
			Assert.True(pt.Equals(PT));
			}
		[Test] public void ToXml4()
			{
				var node = DateTimeOffset.MinValue.ToXml("min_time", true);
				var dto = node.As<DateTimeOffset>();
				Assert.AreEqual(DateTimeOffset.MinValue, dto);
			}
		[Test] public void ToXml5()
			{
				var guid = Guid.NewGuid();
				var node = guid.ToXml("guid", false);
				var GUID = node.As<Guid>();
				Assert.AreEqual(guid, GUID);
			}
		[Test] public void ToXml6()
			{
				// XElement constructible class
				var node = new Elem1(4).ToXml("four", false);
				var four = node.As<Elem1>();
			Assert.AreEqual(4U, four.m_uint);
			}
		[Test] public void ToXml7()
			{
				// DC class
				var dc = new Elem2(2,"3");
				var node = dc.ToXml("dc", false);
				var DC = node.As<Elem2>(factory:t => new Elem2(0,null));
				Assert.AreEqual(dc.m_int, DC.m_int);
				Assert.AreEqual(dc.m_string, DC.m_string);
			}
		[Test] public void ToXml8()
			{
				// Arrays
			var arr = new int[]{0,1,2,3,4};
				var node = arr.ToXml("arr", false);
				var ARR = node.As<int[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml9()
			{
			var arr = new string[]{"hello", "world"};
				var node = arr.ToXml("arr", false);
				var ARR = node.As<string[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml10()
			{
				var arr = new[]{new Point(1,1), new Point(2,2)};
				var node = arr.ToXml("arr", true);
				var ARR = node.As<Point[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml11()
			{
				var arr = new[]{new Elem2(1,"1"), new Elem2(2,"2"), new Elem2(3,"3")};
				var node = arr.ToXml("arr", false);
				var ARR = node.As<Elem2[]>(factory:t => new Elem2(0,null));
				Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml12()
			{
				var arr = new[]{new Elem2(1,"1"), null, new Elem2(3,"3")};
				var node = arr.ToXml("arr", false);
				var ARR = node.As<Elem2[]>(factory:t => new Elem2(0,""));
			Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml13()
			{
				// nullables
				int? three = 3;
				var node = three.ToXml("three", false);
				var THREE = node.As<int?>();
				Assert.True(three == THREE);
			}
		[Test] public void ToXml14()
			{
				var arr = new int?[]{1, null, 2};
				var node = arr.ToXml("arr", true);
				var ARR = node.As<int?[]>();
			Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml15()
			{
				var arr = new object[]{null, new Elem1(1), new Elem2(2,"2"), new Elem3(3,"3")};
				var node = arr.ToXml("arr", true);
				var ARR = node.As<object[]>(factory:ty =>
					{
						if (ty == typeof(Elem1)) return new Elem1();
						if (ty == typeof(Elem2)) return new Elem2(0,string.Empty);
						if (ty == typeof(Elem3)) return new Elem3();
						throw new Exception("Unexpected type");
					});
				Assert.True(arr.SequenceEqual(ARR));
			}
		[Test] public void ToXml16()
			{
			}
		[Test] public void ToXml17()
			{
				var e4 = new Elem4{m_int = 3};
				var node = e4.ToXml("e4", false);
				var E4 = node.As<Elem4>();
				Assert.AreEqual(e4.m_int, E4.m_int);
			}
		[Test] public void ToXml18()
			{
				var rc = new Rectangle(1,2,3,4);
				var node = rc.ToXml("rect", false);
				var RC = node.As<Rectangle>();
			Assert.True(Equals(rc, RC));
			}
		[Test] public void ToXml19()
			{
				var rc = new RectangleF(1f,2f,3f,4f);
				var node = rc.ToXml("rect", false);
				var RC = node.As<RectangleF>();
			Assert.True(Equals(rc, RC));
			}
		[Test] public void XmlAs()
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
				Assert.AreEqual((char)('a' + j), chars[j]);

				ints.Clear();
				root.Element("e").As(ints, "i", (lhs,rhs) => lhs == rhs);
				Assert.AreEqual(4, ints.Count);
				for (int i = 0; i != 4; ++i)
					Assert.AreEqual(i, ints[i]);
			}
		[Test] public void XmlAdd()
			{
				var xml  = new XDocument();
				var cmt  = xml.Add2(new XComment("comments"));  Assert.AreEqual(cmt.Value, "comments");
				var root = xml.Add2(new XElement("root"));      Assert.AreSame(xml.Root, root);

				var ints = new List<int>{0,1,2,3,4};
				var elems = Util.NewArray(5, i => new Elem1((uint)i));
				string s;

				var xint = root.Add2("elem", 42, true);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
						"<elem ty=\"System.Int32\">42</elem>" +
					"</root>"
					,s);
				xint.Remove();

				var xelem = root.Add2("elem", elems[0], true);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
					"<elem ty=\"pr.unittests.TestXml+Elem1\">elem_0</elem>" +
					"</root>"
					,s);
				xelem.Remove();

				var xints = root.Add2("ints", "i", ints, true);
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

				var xelems = root.Add2("elems", "i", elems, true);
				s = root.ToString(SaveOptions.DisableFormatting);
				Assert.AreEqual(
					"<root>" +
						"<elems>" +
						"<i ty=\"pr.unittests.TestXml+Elem1\">elem_0</i>" +
						"<i ty=\"pr.unittests.TestXml+Elem1\">elem_1</i>" +
						"<i ty=\"pr.unittests.TestXml+Elem1\">elem_2</i>" +
						"<i ty=\"pr.unittests.TestXml+Elem1\">elem_3</i>" +
						"<i ty=\"pr.unittests.TestXml+Elem1\">elem_4</i>" +
						"</elems>" +
					"</root>"
					,s);
				xelems.Remove();
			}
		}
	}

#endif
