//***************************************************
// Xml Helper Functions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	// Object hierarchy:
	//   XObject
	//   +- XAttribute
	//   +- XNode
	//      +- XComment
	//      +- XContainer
	//      |  +- XDocument
	//      |  +- XElement
	//      +- XDocumentType
	//      +- XProcessingInstruction
	//      +- XText
	//         +- XCData

	/// <summary>Marker type for fluent API configuration extension methods</summary>
	public class XmlConfig
	{
		// Notes:
		//  - Xml_.Config.SupportRylogicMathsTypes           in 'Rylogic.Core\src\Maths\Math.cs'
		//  - Xml_.Config.SupportSystemDrawingPrimitiveTypes in 'Rylogic.Core\src\Extn\Drawing.cs'
		//  - Xml_.Config.SupportRylogicGraphicsTypes        in 'Rylogic.Core\src\Gfx\Gfx.cs'
		//  - Xml_.Config.SupportSystemDrawingCommonTypes    in 'Rylogic.Core.Windows\src\Extn\Drawing.cs'
		//  - Xml_.Config.SupportWinFormsTypes               in 'Rylogic.Gui.WinForms\src\Extn\Xml.cs'
		//  - Xml_.Config.SupportWPFTypes                    in 'Rylogic.Gui.WPF\src\Extn\Xml.cs'
	};

	/// <summary>XML helper methods</summary>
	public static class Xml_
	{
		public static readonly char[] WhiteSpace = new[]{' ','\t','\r','\n','\v'};
		public const string TypeAttr = "ty";

		#region ToXml Binding

		/// <summary>Signature of the methods used to convert a type to an XElement</summary>
		public delegate XElement ToFunc(object obj, XElement node);

		/// <summary>
		/// A map from type to 'ToXml' method
		/// User ToXml functions can be added to this map.
		/// Note, they are only needed if ToBinding.Convert() method fails</summary>
		public static ToBinding ToMap { [DebuggerStepThrough] get; } = new ToBinding();
		public class ToBinding
		{
			private readonly Dictionary<Type, ToFunc> m_bind;
			public ToBinding()
			{
				m_bind = new Dictionary<Type, ToFunc>();
				this[typeof(XElement)] = (obj, node) =>
				{
					node.Add(obj);
					return node;
				};
				this[typeof(string)] = ToXmlDefault;
				this[typeof(bool)] = ToXmlDefault;
				this[typeof(byte)] = ToXmlDefault;
				this[typeof(sbyte)] = ToXmlDefault;
				this[typeof(char)] = ToXmlDefault;
				this[typeof(short)] = ToXmlDefault;
				this[typeof(ushort)] = ToXmlDefault;
				this[typeof(int)] = ToXmlDefault;
				this[typeof(uint)] = ToXmlDefault;
				this[typeof(long)] = ToXmlDefault;
				this[typeof(ulong)] = ToXmlDefault;
				this[typeof(float)] = ToXmlDefault;
				this[typeof(double)] = ToXmlDefault;
				this[typeof(decimal)] = ToXmlDefault;
				this[typeof(Enum)] = ToXmlDefault;
				this[typeof(Guid)] = ToXmlDefault;
				this[typeof(DateTimeOffset)] = (obj, node) =>
				{
					var dto = (DateTimeOffset)obj;
					node.SetValue(dto.ToString("o"));
					return node;
				};
				this[typeof(DateTime)] = (obj, node) =>
				{
					var dt = (DateTime)obj;
					node.SetValue(dt.ToString("o"));
					return node;
				};
				this[typeof(TimeSpan)] = (obj, node) =>
				{
					var ts = (TimeSpan)obj;
					node.SetValue(ts.Ticks);
					return node;
				};
				this[typeof(KeyValuePair<,>)] = (obj, node) =>
				{
					var ty = obj.GetType();
					node.Add2("k", ty.GetProperty(nameof(KeyValuePair<int,int>.Key  ))!.GetValue(obj), false);
					node.Add2("v", ty.GetProperty(nameof(KeyValuePair<int,int>.Value))!.GetValue(obj), false);
					return node;
				};
				this[typeof(List<>)] = (obj, node) =>
				{
					foreach (var x in (IEnumerable)obj)
						node.Add2("_", x, false);
					if (!node.HasElements)
						node.SetValue(string.Empty);
					return node;
				};
				this[typeof(ObservableCollection<>)] = (obj, node) =>
				{
					foreach (var x in (IEnumerable)obj)
						node.Add2("_", x, false);
					if (!node.HasElements)
						node.SetValue(string.Empty);
					return node;
				};
				this[typeof(Dictionary<,>)] = (obj, node) =>
				{
					foreach (var x in (IEnumerable)obj)
						node.Add2("_", x, false);
					if (!node.HasElements)
						node.SetValue(string.Empty);
					return node;
				};
				this[typeof(LazyDictionary<,>)] = (obj, node) =>
				{
					foreach (var x in (IEnumerable)obj)
						node.Add2("_", x, false);
					if (!node.HasElements)
						node.SetValue(string.Empty);
					return node;
				};
				this[typeof(HashSet<>)] = (obj, node) =>
				{
					foreach (var x in (IEnumerable)obj)
						node.Add2("_", x, false);
					if (!node.HasElements)
						node.SetValue(string.Empty);
					return node;
				};
				this[typeof(AnonymousType)] = (obj, node) =>
				{
					var type = obj.GetType();
					foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
						node.Add2(prop.Name, prop.GetValue(obj), true);
					return node;
				};
			}

			/// <summary>The number of bindings</summary>
			public int Count => m_bind.Count;

			/// <summary>Get/Set a binding for a type</summary>
			public ToFunc this[Type type]
			{
				get => m_bind[type];
				set
				{
					if (value != null) m_bind[type] = value;
					else m_bind.Remove(type);
				}
			}

			/// <summary>Find a binding function</summary>
			public bool TryGetValue(Type type, out ToFunc func)
			{
				return m_bind.TryGetValue(type, out func!);
			}

			/// <summary>
			/// Saves 'obj' into 'node' using the bound ToXml methods.
			/// 'type_attr' controls whether the 'ty' attribute is added. By default it should
			/// be added so that XML can be de-serialised to 'object'. If this is never needed
			/// however it can be omitted.</summary>
			public XElement Convert(object? obj, XElement node, bool type_attr)
			{
				if (obj == null)
				{
					if (!node.IsEmpty) throw new Exception("Null objects should serialise to empty XElements");
					return node;
				}

				var type = obj.GetType();
				type = Nullable.GetUnderlyingType(type) ?? type;
				if (type.IsAnonymousType()) type = typeof(AnonymousType);
				if (type.GetElementType()?.IsAnonymousType() ?? false) type = typeof(AnonymousType[]);

				// Add the type attribute
				if (type_attr)
					node.SetAttributeValue(TypeAttr, type.FullName);

				// Get the generic type if generic
				var gen_type = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

				// Handle strings here because they are IEnumerable.
				// Handle enums because the type will not be in the map.
				if (type == typeof(string) || type.IsEnum)
					return ToXmlDefault(obj, node);

				// Use the generalised generic type if generic
				var lookup_type = type.IsGenericType ? gen_type : type;

				// Find a function that contains the type to XML
				ToFunc? func = null;
				for (;;)
				{
					// Look for a binding function
					func = m_bind.TryGetValue(lookup_type, out var f) ? f : null;
					if (func != null) { break; }

					// See if 'obj' has a native 'ToXml' method
					var mi = type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(IsToXmlFunc);
					if (mi != null) { func = this[type] = ToXmlMethod; break; }

					// Try DataContract binding
					var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
					if (dca != null) { func = this[type] = ToXmlDataContract; break; }

					// Handle unknown collections as arrays.
					// Handle 'IEnumerable' after checking that 'obj' doesn't have a ToXml method
					if (obj is IEnumerable enumerable)
					{
						// Derive an element name from the singular of the array name
						var name = node.Name.LocalName;
						var elem_name = name.Length > 1 && name.EndsWith("s") ? name.Substring(0, name.Length - 1) : "_";

						// Determine the type of the array elements
						var elem_type = type.GetElementType();

						// Add each element from the collection
						foreach (var i in enumerable)
						{
							// The type attribute is not needed if actual type of the element matches the array element type.
							// It is needed if the element is a sub-class of the element type, or the array element type is
							// 'object', or the element is null (so 'As' knows what type of null to create).
							var ty_attr = type_attr && (i == null || elem_type == typeof(object) || i.GetType() != elem_type);
							node.Add(Convert(i, new XElement(elem_name), ty_attr));
						}

						// Make <elem></elem> different to <elem/>
						if (!node.HasElements)
							node.SetValue(string.Empty);

						return node;
					}

					throw new NotSupportedException($"There is no 'ToXml' binding for type {type.FullName}");
				}
				return func(obj, node);
			}

			/// <summary>Return an XElement using reflection.</summary>
			private XElement ToXmlDefault(object obj, XElement node)
			{
				node.SetValue(obj);
				return node;
			}

			/// <summary>Return an XElement using the 'ToXml' method on the type</summary>
			private XElement ToXmlMethod(object obj, XElement node)
			{
				// Find the native method on the type
				var type = obj.GetType();
				var mi = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(IsToXmlFunc);
				if (mi == null) throw new NotSupportedException($"{type.Name} does not have a 'ToXml' method");

				// Replace the mapping with a call directly to that method
				this[type] = (o, n) => (XElement?)mi.Invoke(o, new object[] { n }) ?? throw new Exception("ToXml method returned null");
				return this[type](obj, node);
			}

			/// <summary>Return an XElement object for a type that specifies the DataContract attribute</summary>
			private XElement ToXmlDataContract(object obj, XElement node)
			{
				var type = obj.GetType();

				// Look for the DataContract attribute
				var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
				if (dca == null) throw new NotSupportedException($"{type.Name} does not have the DataContractAttribute");

				// Find all fields and properties with the DataMember attribute
				var members =
					type.AllFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Cast<MemberInfo>().Concat(
					type.AllProps(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
					.Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute), false).Any())
					.Select(x => new { Member = x, Attr = (DataMemberAttribute)x.GetCustomAttributes(typeof(DataMemberAttribute), false).First() })
					.ToList();

				// Replace the mapping function with a call that already has the members found
				this[type] = (o, n) =>
				{
					foreach (var m in members)
					{
						var name = m.Attr.Name ?? m.Member.Name;
						var child = new XElement(name);

						// Read the member to be written
						object? val = 
							m.Member is PropertyInfo prop ? prop.GetValue(o, null) :
							m.Member is FieldInfo field ? field.GetValue(o) :
							null;

						n.Add(ToMap.Convert(val, child, true));
					}
					return n;
				};
				return this[type](obj, node);
			}

			/// <summary>Returns true if 'm' is a 'ToXml' method</summary>
			private static bool IsToXmlFunc(MethodInfo m)
			{
				ParameterInfo[] parms;
				return
					m.Name == "ToXml" &&
					m.ReturnType == typeof(XElement) &&
					(parms = m.GetParameters()).Length == 1 &&
					parms[0].ParameterType == typeof(XElement);
			}
		}

		/// <summary>Write this object into an XML node with tag 'elem_name'</summary>
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
		public static AsBinding AsMap { [DebuggerStepThrough] get; } = new AsBinding();
		public class AsBinding
		{
			private readonly Dictionary<Type, AsFunc> m_bind;
			public AsBinding()
			{
				m_bind = new Dictionary<Type, AsFunc>();
				this[typeof(XElement)] = (elem, type, ctor) =>
				{
					return elem.Elements().FirstOrDefault();
				};
				this[typeof(string)] = (elem, type, ctor) =>
				{
					return elem.Value;
				};
				this[typeof(bool)] = (elem, type, ctor) =>
				{
					return bool.Parse(elem.Value);
				};
				this[typeof(byte)] = (elem, type, ctor) =>
				{
					return elem.Value.StartsWith("0x")
						? byte.Parse(elem.Value.Substring(2), NumberStyles.HexNumber)
						: byte.Parse(elem.Value, NumberStyles.Any);
				};
				this[typeof(sbyte)] = (elem, type, ctor) =>
				{
					return sbyte.Parse(elem.Value, NumberStyles.Any);
				};
				this[typeof(char)] = (elem, type, ctor) =>
				{
					return char.Parse(elem.Value);
				};
				this[typeof(short)] = (elem, type, ctor) =>
				{
					return short.Parse(elem.Value, NumberStyles.Any);
				};
				this[typeof(ushort)] = (elem, type, ctor) =>
				{
					return elem.Value.StartsWith("0x")
						? ushort.Parse(elem.Value.Substring(2), NumberStyles.HexNumber)
						: ushort.Parse(elem.Value, NumberStyles.Any);
				};
				this[typeof(int)] = (elem, type, ctor) =>
				{
					return int.Parse(elem.Value, NumberStyles.Any);
				};
				this[typeof(uint)] = (elem, type, ctor) =>
				{
					return elem.Value.StartsWith("0x")
						? uint.Parse(elem.Value.Substring(2), NumberStyles.HexNumber)
						: uint.Parse(elem.Value);
				};
				this[typeof(long)] = (elem, type, ctor) =>
				{
					return long.Parse(elem.Value, NumberStyles.Any);
				};
				this[typeof(ulong)] = (elem, type, ctor) =>
				{
					return elem.Value.StartsWith("0x")
						? ulong.Parse(elem.Value.Substring(2), NumberStyles.HexNumber)
						: ulong.Parse(elem.Value);
				};
				this[typeof(float)] = (elem, type, ctor) =>
				{
					return float.Parse(elem.Value);
				};
				this[typeof(double)] = (elem, type, ctor) =>
				{
					return double.Parse(elem.Value);
				};
				this[typeof(decimal)] = (elem, type, ctor) =>
				{
					return decimal.Parse(elem.Value);
				};
				this[typeof(Enum)] = (elem, type, ctor) =>
				{
					return Enum.Parse(type, elem.Value);
				};
				this[typeof(Guid)] = (elem, type, ctor) =>
				{
					return Guid.Parse(elem.Value);
				};
				this[typeof(DateTimeOffset)] = (elem, type, ctor) =>
				{
					return DateTimeOffset.ParseExact(elem.Value, "o", null);
				};
				this[typeof(DateTime)] = (elem, type, ctor) =>
				{
					return DateTime.ParseExact(elem.Value, "o", null);
				};
				this[typeof(TimeSpan)] = (elem, type, ctor) =>
				{
					return TimeSpan.FromTicks(long.Parse(elem.Value));
				};
				this[typeof(KeyValuePair<,>)] = (elem, type, ctor) =>
				{
					var ty_args = type.GetGenericArguments();
					var key = elem.Element("k").As(ty_args[0]);
					var val = elem.Element("v").As(ty_args[1]);
					return type.New(key, val);
				};
				this[typeof(List<>)] = (elem, type, ctor) =>
				{
					var ty_args = type.GetGenericArguments();
					var list = type.New();
					var mi_add = type.GetMethod(nameof(List<int>.Add))!;

					foreach (var li_elem in elem.Elements())
					{
						var li = li_elem.As(ty_args[0]);
						mi_add.Invoke(list, new object?[] { li });
					}
					return list;
				};
				this[typeof(ObservableCollection<>)] = (elem, type, ctor) =>
				{
					var ty_args = type.GetGenericArguments();
					var list = type.New();
					var mi_add = type.GetMethod(nameof(ObservableCollection<int>.Add))!;

					foreach (var li_elem in elem.Elements())
					{
						var li = li_elem.As(ty_args[0]);
						mi_add.Invoke(list, new object?[] { li });
					}
					return list;
				};
				this[typeof(Dictionary<,>)] = (elem, type, ctor) =>
				{
					var ty_args = type.GetGenericArguments();
					var kv_type = typeof(KeyValuePair<,>).MakeGenericType(ty_args);
					var mi_add = type.GetMethod(nameof(Dictionary<int,int>.Add))!;

					var dic = type.New();
					foreach (var kv_elem in elem.Elements())
					{
						var key = kv_elem.Element("k").As(ty_args[0]);
						var val = kv_elem.Element("v").As(ty_args[1]);
						mi_add.Invoke(dic, new object?[] { key, val });
					}
					return dic;
				};
				this[typeof(LazyDictionary<,>)] = (elem, type, ctor) =>
				{
					var ty_args = type.GetGenericArguments();
					var kv_type = typeof(KeyValuePair<,>).MakeGenericType(ty_args);
					var mi_add = type.GetMethod(nameof(LazyDictionary<int, int>.Add))!;

					var dic = type.New();
					foreach (var kv_elem in elem.Elements())
					{
						var key = kv_elem.Element("k").As(ty_args[0]);
						var val = kv_elem.Element("v").As(ty_args[1]);
						mi_add.Invoke(dic, new object?[] { key, val });
					}
					return dic;
				};
				this[typeof(HashSet<>)] = (elem, type, ctor) =>
				{
					var ty_args = type.GetGenericArguments();
					var set = type.New();
					var mi_add = type.GetMethod(nameof(HashSet<int>.Add))!;

					foreach (var si_elem in elem.Elements())
					{
						var si = si_elem.As(ty_args[0]);
						mi_add.Invoke(set, new object?[] { si });
					}
					return set;
				};
				this[typeof(AnonymousType)] = (elem, type, ctor) =>
				{
					throw new NotSupportedException(
						$".NETStandard 2.0 does not have support for Reflection.Emit (even though there is " +
						$"support in .NET Core 2 and .NetFramework 4.6+). To support deserialisation of " +
						$"anonymous types, you need to reference 'Rylogic.Core.Windows' and add a call to " +
						$"'Xml_.Config.SupportAnonymousTypes()' during initialisation. Alternatively, anonymous " +
						$"types can be returned as 'Dictionary<string,object>' by calling 'Xml_.Config.AnonymousTypesAsDictionary();");
				};
			}

			/// <summary>The number of bindings</summary>
			public int Count => m_bind.Count;

			/// <summary>Get/Set a binding for a type</summary>
			public AsFunc this[Type type]
			{
				get => m_bind[type];
				set
				{
					if (value != null) m_bind[type] = value;
					else m_bind.Remove(type);
				}
			}

			/// <summary>Find a binding function</summary>
			public bool TryGetValue(Type type, out AsFunc func)
			{
				return m_bind.TryGetValue(type, out func!);
			}

			/// <summary>
			/// Converts 'elem' to 'type'.
			/// If 'type' is 'object' then the type is inferred from the node.
			/// 'factory' is used to construct instances of type, unless type is
			/// an array, in which case factory is used to construct the array elements</summary>
			public object? Convert(XElement elem, Type type, Func<Type,object>? factory_)
			{
				// XElement values are strings already
				if (type == typeof(string))
					return elem.Value;

				var factory = factory_ ?? Type_.New;

				// If 'type' is nullable then returning the underlying type will
				// automatically convert to the nullable type
				var ty = Nullable.GetUnderlyingType(type);
				var is_nullable = ty != null;
				if (is_nullable) type = ty!;

				// If the node has a type attribute use it instead of 'type'
				if (elem.Attribute(TypeAttr) is XAttribute attr && type.FullName != attr.Value)
				{
					try { type = Type_.Resolve(attr.Value); }
					catch (TypeLoadException ex)
					{
						// If you get this error you can use GC.KeepAlive(typeof(TheTypeName)); to force
						// the assembly to be loaded before you try to load the XML.
						throw new TypeLoadException(
							$"Failed to resolve type name {attr.Value}. No type with this name found in the loaded assemblies.\r\n"+
							"This error indicates that the XML being parsed contains a type that this application does not recognise.\r\n"+
							"If the type should be recognised by this application, it may be that the assembly has not been loaded yet. ", ex);
					}
				}

				// If 'type' is still 'object', report unresolvable
				if (type == typeof(object))
				{
					// Only throw if a value is given and we can't determine it's type
					if (string.IsNullOrEmpty(elem.Value)) return null;
					throw new Exception($"Cannot determine the type for XML element: {elem}");
				}

				// 'IsEmpty' elements return null, 'elem.Value == ""' returns default instances.
				// <thing/> means null, <thing><thing/> means default constructed instance
				// To serialise to '<thing><thing/>' set the node.Value to string.Empty
				// e.g. public XElement ToXml(XElement node) { node.Value = string.Empty; return node; }
				if (!elem.HasElements && !elem.Value.HasValue())
				{
					if (type == typeof(string))
					{
						return string.Empty;
					}
					if (type.IsArray && type.GetElementType() is Type child_type)
					{
						return Array.CreateInstance(child_type, 0);
					}
					if (type.IsClass || is_nullable) // includes typeof(object)
					{
						return !elem.IsEmpty ? factory(type) : null;
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
					// Get the child elements as a list so we know how long to make the array
					var children = elem.Elements().ToList();

					// Create an array of the correct type and length
					var ty_elem = type.GetElementType();
					ty_elem = ty_elem != null && ty_elem != typeof(AnonymousType) ? ty_elem : typeof(object);
					var array = Array.CreateInstance(ty_elem, children.Count);

					// Note: the 'factory' must handle both the array type and the element types
					for (int i = 0; i != children.Count; ++i)
						array.SetValue(children[i].As(ty_elem, factory), i);

					return array;
				}

				// Use the generalised generic type if generic
				var lookup_type = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

				// Find the function that converts the string to 'type'
				AsFunc? func = TryGetValue(lookup_type, out var f) ? f : null;
				for (;func == null;) // There is no 'As' binding for this type, try a few possibilities
				{
					// Try a constructor that takes a single XElement parameter. The constructor can be private, but not inherited
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

					// Notes
					//  - If it appears a type is in the map but TryGetValue returns false, it is probably
					//    because libraries loaded dynamically seem to be considered different types.
					//  - "Types are per-assembly; if you have "the same" assembly loaded twice, then types
					//    in each "copy" of the assembly are not considered to be the same type."
					throw new NotSupportedException(
						$"No binding for converting XElement to type: {lookup_type.Name}\n" +
						$"As-Bindings: [{Count}] {string.Join(",", m_bind.Keys.Select(x => x.Name))}");
				}
				return func(elem, type, factory);
			}

			/// <summary>An 'As' method that expects 'type' to have a constructor taking a single XElement argument</summary>
			private object AsCtor(XElement elem, Type type, Func<Type,object> factory)
			{
				var ctor = type.GetConstructor(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(XElement)}, null);
				if (ctor == null) throw new NotSupportedException($"{type.Name} does not have a constructor taking a single XElement argument");

				// Replace the mapping with a call that doesn't need to search for the constructor
				this[type] = (e,t,i) => ctor.Invoke(new object[]{e});
				return this[type](elem, type, factory);
			}

			/// <summary>An 'As' method that expects 'type' to have a method called 'FromXml' taking a single XElement argument</summary>
			private object AsFromXmlMethod(XElement elem, Type type, Func<Type,object> factory)
			{
				var method = type.GetMethod("FromXml", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic, null, new[]{typeof(XElement)}, null);
				if (method == null) throw new NotSupportedException($"{type.Name} does not have a method called 'FromXml(XElement)'");

				// Replace the mapping with a call that doesn't need to search for the method
				this[type] = (e,t,i) =>
				{
					try
					{
						var obj = factory(type);
						method.Invoke(obj, new object[] { e });
						return obj;
					}
					catch (TargetInvocationException ex) when (ex.InnerException != null)
					{
						throw ex.InnerException;
					}
				};
				return this[type](elem, type, factory);
			}

			/// <summary>An 'As' method for types that specify the DataContract attribute</summary>
			private object AsDataContract(XElement elem, Type type, Func<Type,object> factory)
			{
				// Look for the DataContract attribute
				var dca = type.GetCustomAttributes(typeof(DataContractAttribute), true).FirstOrDefault();
				if (dca == null) throw new NotSupportedException($"{type.Name} does not have the DataContractAttribute");

				// Find all fields and properties with the DataMember attribute
				var members =
					type.AllFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Cast<MemberInfo>().Concat(
					type.AllProps(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
					.Where(x => x.GetCustomAttributes(typeof(DataMemberAttribute),false).Any())
					.Select(x => new{Member = x, Attr = (DataMemberAttribute)x.GetCustomAttributes(typeof(DataMemberAttribute),false).First()})
					.OrderBy(x => x.Attr.Name ?? x.Member.Name)
					.ToList();

				// Replace the mapped function with one that doesn't need to search for members
				this[type] = (el,ty,new_inst) =>
				{
					new_inst ??= Type_.New;// Activator.CreateInstance;

					// This will always de-serialise as a default object ignoring the elements
					if (members.Count == 0 && el.HasElements)
						throw new Exception($"{ty.Name} has the DataContract attribute, but no DataMembers.");

					// Read nodes from the XML, and populate any members with matching names
					object obj = new_inst(ty);
					foreach (var e in el.Elements())
					{
						// Look for the property or field by name
						var name = e.Name.LocalName;
						var m = members.BinarySearchFind(x => string.CompareOrdinal(x.Attr.Name ?? x.Member.Name, name));
						if (m == null) continue;

						if (m.Member is PropertyInfo prop) prop.SetValue(obj, AsMap.Convert(e, prop.PropertyType, new_inst), null);
						if (m.Member is FieldInfo field) field.SetValue(obj, AsMap.Convert(e, field.FieldType, new_inst));
					}
					return obj;
				};
				return this[type](elem, type, factory);
			}

			/// <summary>An 'As' method that relies of the factory function to read 'elem'</summary>
			private object AsFromFactory(XElement elem, Type type, Func<Type,object> factory)
			{
				return factory(type);
			}
		}

		/// <summary>Returns this XML node as an instance of the type implied by it's node attributes</summary>
		public static object? ToObject(this XElement elem, Func<Type,object>? factory = null)
		{
			// Passing a factory function to handle any of the types 'elem' might be
			// can work, but the factory function won't be available further down the
			// hierarchy and so is of limited use. A better way is to add custom handlers
			// to the 'AsMap'.
			if (elem == null) throw new ArgumentNullException("XML element is null. Key not found?");
			return AsMap.Convert(elem, typeof(object), factory);
		}

		/// <summary>Returns this XML node as an instance of the type implied by it's node attributes</summary>
		public static object? ToObject(this XElement elem, object optional_default, Func<Type,object>? factory = null)
		{
			if (elem == null) return optional_default;
			return ToObject(elem, factory);
		}

		/// <summary>
		/// Return this element as an instance of 'type'.
		/// Use 'ToObject' if the type should be inferred from the node attributes
		/// 'factory' can be used to create new instances of 'T' if doesn't have a default constructor.
		/// E.g:<para\>
		///  val = node.Element("val").As(typeof(int)) <para\>
		///  val = node.Element("val").As(typeof(int), default_val) <para\></summary>
		public static object? As(this XElement elem, Type type, Func<Type,object>? factory = null)
		{
			if (elem == null) throw new ArgumentNullException("XML element is null. Key not found?");
			return AsMap.Convert(elem, type, factory);
		}
		public static object? As(this XElement elem, Type type, object? optional_default, Func<Type,object>? factory = null)
		{
			if (elem == null) return optional_default;
			return As(elem, type, factory);
		}

		/// <summary>
		/// Return this element as an instance of 'T'.
		/// Use 'ToObject' if the type should be inferred from the node attributes
		/// 'factory' can be used to create new instances of 'T' if it doesn't have a default constructor.
		/// E.g:<para\>
		///  val = node.Element("val").As&lt;int&gt;() <para\>
		///  val = node.Element("val").As&lt;int&gt;(default_val) <para\></summary>
		public static T As<T>(this XElement elem, Func<Type,object>? factory = null)
		{
			return (T)As(elem, typeof(T), factory)!;
		}
		public static T As<T>(this XElement elem, T optional_default, Func<Type,object>? factory = null)
		{
			return (T)As(elem, typeof(T), optional_default, factory)!;
		}
		public static T OrDefault<T>(this XElement elem, T def)
		{
			return (T)As(elem, typeof(T), def, null)!;
		}

		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'factory' and optionally overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, T optional_default, Func<T,T,bool>? is_duplicate = null, Func<Type,object>? factory = null)
		{
			AsList<T>(parent, list, elem_name, e => e.As<T>(optional_default, factory), is_duplicate);
		}

		/// <summary>Read all elements with name 'elem_name' into 'list' constructing them using 'factory' and optionally overwriting duplicates.</summary>
		public static void As<T>(this XElement parent, IList<T> list, string elem_name, Func<T,T,bool>? is_duplicate = null, Func<Type,object>? factory = null)
		{
			AsList<T>(parent, list, elem_name, e => e.As<T>(factory), is_duplicate);
		}
		private static void AsList<T>(XElement parent, IList<T> list, string elem_name, Func<XElement,T> conv, Func<T,T,bool>? is_duplicate)
		{
			if (parent == null) throw new ArgumentNullException("XML element is null. Key not found?");

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
				if (index != list.Count)
					list[index] = item;
				else
					list.Add(item);
				++count;
			}
		}

		#endregion

		/// <summary>Configuration object</summary>
		public static readonly XmlConfig Config = new XmlConfig()
			.SupportRylogicCommonTypes()
			.SupportRylogicMathsTypes()
			.SupportRylogicGraphicsTypes()
			;

		/// <summary>Magic type for handling anonymous types</summary>
		public class AnonymousType { private AnonymousType() { } }
		public static XmlConfig AnonymousTypesAsDictionary(this XmlConfig cfg)
		{
			// Replace the 'As' mapping for anonymous types
			AsMap[typeof(AnonymousType)] = (elem, type, instance) =>
			{
				return elem.Elements().ToDictionary(x => x.Name.LocalName, x => x.ToObject());
			};
			return cfg;
		}

		/// <summary>Returns the number of child nodes in this node (by counting them linearly)</summary>
		public static int ChildCount(this XContainer node)
		{
			return node.Nodes().Count();
		}

		/// <summary>Returns the number of child nodes of type 'T' in this node (by counting them linearly)</summary>
		public static int ChildCount<T>(this XContainer node) where T :XNode
		{
			return node.Nodes().OfType<T>().Count();
		}

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
		public static XElement Add2(this XContainer parent, string name, object? child, bool type_attr)
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

		/// <summary>Insert 'child' at index position 'index'. 'index' must be in the range [0,ChildCount()]. Returns 'child'</summary>
		public static XNode Insert(this XContainer parent, int index, XNode child)
		{
			var child_count = parent.ChildCount();
			if (index < 0 || index > child_count)
				throw new Exception($"XML insert node. Index {index} out of range [0,{child_count}]");

			if      (index == 0)           parent.AddFirst(child);
			else if (index == child_count) parent.LastNode.AddAfterSelf(child);
			else                           parent.Nodes().Skip(index).First().AddBeforeSelf(child);
			return child;
		}

		/// <summary>Returns all child elements from the combined path of child element tags. I.e. a full tree search</summary>
		public static IEnumerable<XElement> Elements(this XElement node, params XName[] name)
		{
			return Elements(node, name, 0);
		}
		public static IEnumerable<XElement> Elements(this XElement node, params string[] name)
		{
			return Elements(node, name.Select(x => (XName)x).ToArray(), 0);
		}
		private static IEnumerable<XElement> Elements(XElement node, XName[] name, int index)
		{
			if (index < name.Length - 1)
			{
				foreach (var n in node.Elements(name[index]))
					foreach (var e in Elements(n, name, index + 1))
						yield return e;
			}
			else if (index < name.Length)
			{
				foreach (var n in node.Elements(name[index]))
					yield return n;
			}
		}

		/// <summary>Return the 'i'th' child of this node</summary>
		public static XNode ChildByIndex(this XContainer node, int index)
		{
			return node.Nodes().Skip(index).First();
		}

		/// <summary>Remove child nodes for which 'pred(child)' returns true</summary>
		public static void RemoveNodes(this XContainer parent, Func<XElement, bool> pred)
		{
			foreach (var elem in parent.Elements().ToList())
			{
				if (!pred(elem)) continue;
				elem.Remove();
			}
		}

		/// <summary>Remove child names with the name 'elem_name'</summary>
		public static void RemoveNodes(this XContainer parent, string elem_name)
		{
			RemoveNodes(parent, e => e.Name == elem_name);
		}

		/// <summary>Returns the value of the attribute called 'name' for this element, or 'def' if the element does not have the attribute</summary>
		public static string? AttrValue(this XElement elem, XName name, string? def = null)
		{
			var attr = elem.Attribute(name);
			return attr?.Value ?? def;
		}

		/// <summary>Fluent method for setting the value of an attribute</summary>
		public static XElement AttrValueSet(this XElement elem, XName name, object value)
		{
			elem.SetAttributeValue(name, value);
			return elem;
		}

		/// <summary>Enumerates the leaf XNodes of 'node'. (Depth-first). (Use LeafElements() if you just want elements)</summary>
		public static IEnumerable<XNode> LeafNodes(this XContainer node)
		{
			// See the object inheritance hierarchy at the top of this file
			// only XDocument and XElement subclass XContainer. Anything that
			// isn't a container is a leaf node
			if (node.ChildCount<XContainer>() == 0)
			{
				yield return node;
			}
			else
			{
				foreach (var child in node.Nodes())
				{
					// Not a container => a leaf node
					if (child is XContainer container)
					{
						foreach (var leaf in LeafNodes(container))
							yield return leaf;
					}
					else
					{
						yield return child;
					}
				}
			}
		}

		/// <summary>Enumerates the leaf XElements of 'node' (Depth-first). (Use LeafNodes() for all leaf nodes)</summary>
		public static IEnumerable<XElement> LeafElements(this XContainer node)
		{
			return node.LeafNodes().OfType<XElement>();
		}
	}

	/// <summary>XML Diff/Patch</summary>
	public static class XmlDiff
	{
		/// <summary>Modes that control the type of patch created</summary>
		public enum Mode
		{
			/// <summary>
			/// The difference created represents the operations needed to transform the
			/// LHS into the RHS. i.e. p = lhs.Diff(rhs, Mode.Transform) => lhs.Patch(p) == rhs<para/>
			/// If 'lhs' contains nodes that aren't in 'rhs', these nodes will be removed<para/>
			/// If 'rhs' contains nodes that aren't in 'lhs', these nodes will be added</summary>
			Transform,

			/// <summary>
			/// The difference created represents the operations needed to add nodes from
			/// RHS to LHS. i.e. p = lhs.Diff(rhs, Mode.Merge) => lhs.Patch(p) == UNION(lhs,rhs)<para/>
			/// If 'lhs' contains nodes that aren't in 'rhs', these nodes will remain unchanged<para/>
			/// If 'rhs' contains nodes that aren't in 'lhs', these nodes will be added</summary>
			Merge,
		}

		/// <summary>The operations that the generate diff XML can contain</summary>
		private enum EOpType
		{
			/// <summary>Replace the value text of an element</summary>
			Value,

			/// <summary>Remove an element</summary>
			Remove,

			/// <summary>Insert an element</summary>
			Insert,

			/// <summary>Represents a group of changes to an element</summary>
			Change,

			/// <summary>Add, Replace, or Remove an attribute</summary>
			Attr,
		}

		/// <summary>Operation element attributes</summary>
		private static class Attr
		{
			public const string Idx = "idx";
			public const string NodeType = "node_type";
			public const string Name = "name";
		}

		/// <summary>An operation for patching an XElement tree</summary>
		private class Op
		{
			public Op(XElement elem)
			{
				OpType   = Enum<EOpType>.Parse(elem.Name.LocalName, ignore_case:true);
				NodeType = Enum<XmlNodeType>.Parse((string)elem.Attribute(Attr.NodeType) ?? "element", ignore_case: true);
				Name     = (string)elem.Attribute(Attr.Name) ?? string.Empty;
				Index    = (int?)elem.Attribute(Attr.Idx) ?? 0;
				Value    = (OpType == EOpType.Value || OpType == EOpType.Attr) && !elem.IsEmpty ? elem.Value : null; // Note: IsEmpty means <element/> => Value==null, Value=="" means <element></element>
				FullName = MakeFullName(elem).ToString();
			}

			/// <summary>Generates a full name for this op</summary>
			private StringBuilder MakeFullName(XElement elem, StringBuilder? sb = null)
			{
				// If not the root element, call recursively, then build the name as the stack unwinds
				sb ??= new StringBuilder();
				if (elem.Parent != null)
				{
					MakeFullName(elem.Parent, sb);

					// Append this nodes name to CachedSB
					var op = Enum<EOpType>.Parse(elem.Name.LocalName, ignore_case:true);
					if (op == EOpType.Change || op == EOpType.Insert || op == EOpType.Remove)
					{
						var name = (string)elem.Attribute(Attr.Name);
						if (name != null)
						{
							if (sb.Length != 1) sb.Append("/");
							sb.Append(name);
						}
					}
				}
				else
				{
					sb.Length = 0;
					sb.Append("/");
				}

				return sb;
			}

			/// <summary>The operation type</summary>
			public EOpType OpType { get; private set; }

			/// <summary>The index of the child element that the operation applies to</summary>
			public int Index { get; private set; }

			/// <summary>The node type associated with the op</summary>
			public XmlNodeType NodeType { get; private set; }

			/// <summary>The name of the affected node</summary>
			public string Name { get; private set; }

			/// <summary>The full name from the root to the current node</summary>
			public string FullName { get; private set; }

			/// <summary>The value text associated with the op</summary>
			public string? Value { get; private set; }

			/// <summary>Locate the child element that this op applies to</summary>
			public XNode? FindChild(XContainer tree)
			{
				return Index < tree.ChildCount() ? tree.ChildByIndex(Index) : null;
			}

			/// <summary>Debugging string</summary>
			public override string ToString()
			{
				return $"OpType: {OpType}  Idx: {Index}  Name: {Name}  Value: {Value}";
			}
		}

		/// <summary>Exception type created during diff/patching</summary>
		public class Exception :System.Exception
		{
			public Exception() : base() {}
			public Exception(string message) :base(message) {}
		}

		/// <summary>
		/// Generate a tree of operations that describe how this tree can be transformed into 'rhs'.<para/>
		/// If: patch = this.Diff(that), then this.Patch(patch) == that</summary>
		public static XElement Diff(this XContainer lhs, XContainer rhs, Mode mode = Mode.Transform)
		{
			return Diff(lhs, rhs, new XElement("root"), mode);
		}
		private static XElement Diff(this XContainer lhs, XContainer rhs, XElement diff, Mode mode)
		{
			// We are recording the operations to perform on 'lhs' that
			// will turn it into 'rhs'. This function should not modify
			// 'lhs' or 'rhs'. It should probably be using the 'Zhang and Shasha'
			// minimum tree edit distance algorithm, but it's not..

			var output_node_index = 0;

			// Loop through the nodes at this level
			using var i = lhs.Nodes().GetIterator<XNode>();
			using var j = rhs.Nodes().GetIterator<XNode>();
			for (; !i.AtEnd || !j.AtEnd;)
			{
				// If i still has elements but j does not
				#region !i.AtEnd && j.AtEnd
				if (!i.AtEnd && j.AtEnd)
				{
					for (; !i.AtEnd; i.MoveNext())
					{
						switch (mode)
						{
						default: throw new System.Exception($"Unknown XmlDiff mode: {mode}");
						case Mode.Transform:
							{
								// Remove the remaining i elements
								diff.Add2(RemoveOp(i.Current, ref output_node_index));
								break;
							}
						case Mode.Merge:
							{
								// Leave the remaining i elements in 'lhs'
								++output_node_index;
								break;
							}
						}
					}
					continue;
				}
				#endregion

				// If j still has elements but i does not
				#region i.AtEnd && !j.AtEnd
				if (i.AtEnd && !j.AtEnd)
				{
					for (; !j.AtEnd; j.MoveNext())
					{
						switch (mode)
						{
						default: throw new System.Exception($"Unknown XmlDiff mode: {mode}");
						case Mode.Transform:
						case Mode.Merge:
							{
								// Add the remaining j elements
								diff.Add2(InsertOp(j.Current, ref output_node_index, mode));
								break;
							}
						}
					}
					continue;
				}
				#endregion

				// If i and j are nodes of the same type, then look for cases where we can change one to the other
				if (i.Current.GetType() == j.Current.GetType())
				{
					// Common ones first...
					#region XElement
					if (i.Current is XElement)
					{
						var ni = (XElement)i.Current;
						var nj = (XElement)j.Current;

						// Only change XElements if they have the same name, otherwise treat them as different nodes
						if (ni.Name == nj.Name)
						{
							var op = new XElement(nameof(EOpType.Change));

							// Compare the attributes of the nodes
							var cmp_names  = Eql<XAttribute>.From((l,r) => l.Name == r.Name);
							var cmp_kvpair = Eql<XAttribute>.From((l,r) => l.Name == r.Name && l.Value == r.Value);
							var ai = ni.Attributes().ExceptBy(nj.Attributes(), cmp_names ).OrderBy(x => x.Name.LocalName).ToArray();
							var aj = nj.Attributes().ExceptBy(ni.Attributes(), cmp_kvpair).OrderBy(x => x.Name.LocalName).ToArray();
							ai.ForEach(a => op.Add2(AttrOp(a.Name, null)));    // Remove the attributes that aren't in 'nj'
							aj.ForEach(a => op.Add2(AttrOp(a.Name, a.Value))); // Add/Replace the attributes that are in 'nj'

							// Recursively find the differences in the child trees.
							if (ni.Nodes().Any() || nj.Nodes().Any())
								ni.Diff(nj, op, mode);

							// If the change operation contains child operations then add it to 'diff'
							if (op.HasElements)
							{
								op.SetAttributeValue(Attr.Idx, output_node_index);
								op.SetAttributeValue(Attr.Name, ni.Name);
								diff.Add2(op);
							}

							i.MoveNext();
							j.MoveNext();
							++output_node_index;
							continue;
						}
					}
					#endregion
					#region XCData
					else if (i.Current is XCData)
					{
						var ni = (XCData)i.Current;
						var nj = (XCData)j.Current;
						if (ni.Value != nj.Value)
						{
							var op = diff.Add2(new XElement(nameof(EOpType.Value), new XCData(nj.Value)));
							op.SetAttributeValue(Attr.Idx, output_node_index);
						}
						i.MoveNext();
						j.MoveNext();
						++output_node_index;
						continue;
					}
					#endregion
					#region XText
					else if (i.Current is XText)
					{
						var ni = (XText)i.Current;
						var nj = (XText)j.Current;
						if (ni.Value != nj.Value)
						{
							var op = diff.Add2(new XElement(nameof(EOpType.Value), nj.Value));
							op.SetAttributeValue(Attr.Idx, output_node_index);
						}
						i.MoveNext();
						j.MoveNext();
						++output_node_index;
						continue;
					}
					#endregion
					#region XComment
					else if (i.Current is XComment)
					{
						var ni = (XComment)i.Current;
						var nj = (XComment)j.Current;
						if (ni.Value != nj.Value)
						{
							var op = diff.Add2(new XElement(nameof(EOpType.Value), nj.Value));
							op.SetAttributeValue(Attr.Idx, output_node_index);
						}
						i.MoveNext();
						j.MoveNext();
						++output_node_index;
						continue;
					}
					#endregion
					#region XDocumentType
					else if (i.Current is XDocumentType)
					{
						var ni = (XDocumentType)i.Current;
						var nj = (XDocumentType)j.Current;
						throw new NotImplementedException();
					}
					#endregion
					#region XProcessingInstruction
					else if (i.Current is XProcessingInstruction)
					{
						var ni = (XProcessingInstruction)i.Current;
						var nj = (XProcessingInstruction)j.Current;
						throw new NotImplementedException();
					}
					#endregion
					#region XDocument
					else if (i.Current is XDocument ni && j.Current is XDocument nj)
					{
						// Recursively find the differences in the child trees.
						var op = new XElement(nameof(EOpType.Change));
						if (ni.Nodes().Any() || nj.Nodes().Any())
							ni.Diff(nj, op, mode);

						// If the change operation contains child operations then add it to 'diff'
						if (op.HasElements)
						{
							op.SetAttributeValue(Attr.Idx, output_node_index);
							diff.Add2(op);
						}

						i.MoveNext();
						j.MoveNext();
						++output_node_index;
						continue;
					}
					#endregion
				}

				// At this point, i is not a node that can be changed to j. We need to either add nodes from j
				// or remove nodes from i until i points to a node that can be changed into j. We want to minimise
				// the number of nodes added/removed. Find the nearest re-sync point
				var resync = Seq.FindNearestMatch<XNode>(i.Enumerate(), j.Enumerate(), (l,r) =>
					{
						// Not changeable if different types
						if (l.GetType() != r.GetType())
							return false;

						// Both are XElements, but the names are different, not changeable
						if (l is XElement && ((XElement)l).Name != ((XElement)r).Name)
							return false;

						// Same type => changeable
						return true;
					})
					// If no re-sync point could be found, then the remaining nodes in i
					// should be removed and the remaining nodes in j should be added
					?? Tuple.Create(int.MaxValue, int.MaxValue);

				// Remove or skip nodes in 'i' up to the sync point
				for (var c = resync.Item1; !i.AtEnd && c-- != 0; i.MoveNext())
				{
					switch (mode)
					{
					default: throw new System.Exception($"Unknown XmlDiff mode: {mode}");
					case Mode.Transform:
						{
							diff.Add2(RemoveOp(i.Current, ref output_node_index));
							break;
						}
					case Mode.Merge:
						{
							++output_node_index;
							break;
						}
					}
				}

				// Insert nodes up to the sync point
				for (var c = resync.Item2; !j.AtEnd && c-- != 0; j.MoveNext())
				{
					switch (mode)
					{
					default: throw new System.Exception($"Unknown XmlDiff mode: {mode}");
					case Mode.Transform:
					case Mode.Merge:
						{
							diff.Add2(InsertOp(j.Current, ref output_node_index, mode));
							break;
						}
					}
				}
			}

			return diff;
		}

		/// <summary>Apply the changes described in 'diff' to this element tree</summary>
		public static XContainer Patch(this XContainer tree, XContainer diff)
		{
			// Each child element in 'diff' describes an operation to perform on 'tree'
			foreach (var op_elem in diff.Elements())
			{
				var op = new Op(op_elem);
				switch (op.OpType)
				{
				// Replace the Value for the element
				case EOpType.Value:
					{
						// If 'tree' contains no 'XText' node
						var child = op.FindChild(tree);
						if      (child == null) tree.Insert(op.Index, new XText(op.Value));
						else if (child is XText    xt) xt.Value = op.Value;
						else if (child is XElement xe) xe.Value = op.Value;
						else if (child is XComment xc) xc.Value = op.Value;
						else throw new Exception($"Cannot change value on node type {child.NodeType}");
						break;
					}

				// 'Remove' the child node
				case EOpType.Remove:
					{
						var child = op.FindChild(tree);
						if (op.Name.HasValue() && child is XElement xe && xe.Name != op.Name)
							throw new Exception($"Name mismatch for remove operation. Expected: {op.Name}  Actual: {xe.Name}");
						if (child != null)
							child.Remove();
						break;
					}

				// 'Insert' an element at the given index position
				case EOpType.Insert:
					{
						// Insert a new node, and apply any child operations to it
						if (op.Index < 0 || op.Index > tree.ChildCount())
							throw new Exception($"Insert node at invalid index position. Given: {op.Index}  Valid range: [0,{tree.ChildCount()}]");

						switch (op.NodeType)
						{
						default:
							throw new NotSupportedException($"XmlDiff insert node type {op.NodeType} has not been implemented");
						case XmlNodeType.Element:
							{
								var node = (XContainer)tree.Insert(op.Index, new XElement(op.Name));
								node.Patch(op_elem);
								break;
							}
						case XmlNodeType.Comment:
							{
								var value = op_elem.Element(nameof(EOpType.Value)).As<string>();
								var node = tree.Insert(op.Index, new XComment(value));
								break;
							}
						}
						break;
					}

				// 'Change' is a group of operations to apply to a child node
				case EOpType.Change:
					{
						// Find the child node
						var child = (XContainer?)op.FindChild(tree);
						if (op.Name.HasValue() && child is XElement xe && xe.Name != op.Name)
							throw new Exception($"Name mismatch for change operation. Expected: {op.Name}  Actual: {xe.Name}");
						if (child != null)
							child.Patch(op_elem);
						break;
					}

				// Add, Replace, or Remove an attribute
				case EOpType.Attr:
					{
						// SetAttributeValue 
						((XElement)tree).SetAttributeValue(op.Name, op.Value);
						break;
					}
				}
			}
			return tree;
		}

		/// <summary>Convert an XTree of differences into a string report.</summary>
		public static string Report(XContainer diff)
		{
			var sb = new StringBuilder();
			Report(diff, sb);
			return sb.ToString();
		}
		public static void Report(XContainer diff, StringBuilder sb)
		{
			// Note, this can't be changed to report the operations that would be applied to some 'tree'
			// because the order of operations requires nodes to be added/remove which would modify 'tree'
			foreach (var op_elem in diff.Elements())
			{
				var op = new Op(op_elem);
				switch (op.OpType)
				{
				// Replace the Value for the element
				case EOpType.Value:
					{
						sb.Append($"'{op.FullName}': value changed to '{op.Value}'\n");
						break;
					}

				// 'Remove' the child node
				case EOpType.Remove:
					{
						sb.Append($"'{op.FullName}': {op.NodeType} removed\n");
						break;
					}

				// 'Insert' an element at the given index position
				case EOpType.Insert:
					{
						sb.Append($"'{op.FullName}': {op.NodeType} inserted\n");
						break;
					}

				// 'Change' is a group of operations to apply to a child node
				case EOpType.Change:
					{
						Report(op_elem, sb);
						break;
					}

				// Add, Replace, or Remove an attribute
				case EOpType.Attr:
					{
						// SetAttributeValue 
						sb.Append($"'{op.FullName}': Attribute '{op.Name}' value changed to '{op.Value}'\n");
						break;
					}
				}
			}
		}

		/// <summary>Return a remove operation XML element</summary>
		private static XElement RemoveOp(XNode node, ref int output_node_index)
		{
			var op = new XElement(nameof(EOpType.Remove));
			op.SetAttributeValue(Attr.Idx, output_node_index);
			if (node is XElement xe) // Name the removed element, for sanity checking
				op.SetAttributeValue(Attr.Name, xe.Name);

			// Don't need to increment 'output_node_index'
			// as we're always removing from the same index position
			return op;
		}

		/// <summary>Return an insert operation</summary>
		private static XElement InsertOp(XNode node, ref int output_node_index, Mode mode)
		{
			if (node is XText xt)
			{
				var op = new XElement(nameof(EOpType.Value), xt.Value);
				op.SetAttributeValue(Attr.Idx, output_node_index++);
				return op;
			}
			if (node is XElement xe)
			{
				var op = new XElement(nameof(EOpType.Insert));
				op.SetAttributeValue(Attr.Idx, output_node_index++);
				op.SetAttributeValue(Attr.NodeType, XmlNodeType.Element);
				op.SetAttributeValue(Attr.Name, xe.Name);

				// Add the attributes from 'node'
				var attrs = xe.Attributes();
				attrs.ForEach(a => op.Add2(AttrOp(a.Name, a.Value)));

				// Recursively add operations for the children of j.Current
				if (xe.Nodes().Any())
					new XElement("dummy").Diff(xe, op, mode);

				return op;
			}
			if (node is XComment xc)
			{
				var op = new XElement(nameof(EOpType.Insert));
				op.SetAttributeValue(Attr.Idx, output_node_index++);
				op.SetAttributeValue(Attr.NodeType, XmlNodeType.Comment);
				op.Add2(new XElement(nameof(EOpType.Value), xc.Value));
				return op;
			}
			throw new NotImplementedException();
		}

		/// <summary>Return an attribute operation that adds, replaces, or removes and attribute</summary>
		private static XElement AttrOp(XName name, string? value)
		{
			// 'value' == null will remove the attribute
			var op = new XElement(nameof(EOpType.Attr));
			op.SetAttributeValue(Attr.Name, name);
			if (value != null) op.Value = value;
			return op;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Drawing;
	using Extn;

	[TestFixture]
	public class TestXml
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

			public Elem1() : this(0) { }
			public Elem1(uint i) { m_uint = i; }
			public Elem1(XElement elem) { m_uint = uint.Parse(elem.Value.Substring(5)); }
			public XElement ToXml(XElement node) { node.SetValue("elem_" + m_uint); return node; }
			public override int GetHashCode() { return (int)m_uint; }
			private bool Equals(Elem1 other) { return m_uint == other.m_uint; }
			public override bool Equals(object? obj)
			{
				if (obj is null) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((Elem1)obj);
			}
		}

		/// <summary>A custom DataContract type without a default constructor</summary>
		[DataContract(Name = "ELEM2")]
		internal class Elem2
		{
			[DataMember(Name = "eye")] public readonly int m_int;
			[DataMember] public readonly string? m_string;

			public Elem2(int i, string? s) { m_int = i; m_string = s; }
			public override string ToString() { return m_int.ToString(CultureInfo.InvariantCulture) + " " + m_string; }
			public override int GetHashCode() { unchecked { return (m_int * 397) ^ (m_string != null ? m_string.GetHashCode() : 0); } }
			private bool Equals(Elem2 other) { return m_int == other.m_int && string.Equals(m_string, other.m_string); }
			public override bool Equals(object? obj)
			{
				if (obj is null) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((Elem2)obj);
			}
		}

		/// <summary>A custom DataContract type with a default constructor</summary>
		[DataContract(Name = "ELEM3")]
		internal class Elem3
		{
			[DataMember] public readonly int m_int;
			[DataMember] public readonly string? m_string;

			public Elem3() { m_int = 0; m_string = string.Empty; }
			public Elem3(int i, string? s) { m_int = i; m_string = s; }
			public override int GetHashCode() { unchecked { return (m_int * 397) ^ (m_string != null ? m_string.GetHashCode() : 0); } }
			private bool Equals(Elem3 other) { return m_int == other.m_int && string.Equals(m_string, other.m_string); }
			public override bool Equals(object? obj)
			{
				if (obj is null) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((Elem3)obj);
			}
		}

		private class Elem4
		{
			public int m_int;
			public XElement ToXml(XElement node) { node.SetValue("elem_" + m_int); return node; }
			public void FromXml(XElement node) { m_int = int.Parse(node.Value.Substring(5)); }
		}
		#endregion

		[Test]
		public void ToXmlBuiltInTypes()
		{
			// Built in types
			var node = 5.ToXml("five", false);
			var five = node.As<int>();
			Assert.Equal(5, five);
		}
		[Test]
		public void ToXmlDrawing()
		{
			Xml_.Config.SupportSystemDrawingPrimitiveTypes();

			{
				var pt = new Point(1, 2);
				var node = pt.ToXml("pt", false);
				var PT = node.As<Point>();
				Assert.True(pt.Equals(PT));
			}
			{
				var pt = new PointF(1f, 2f);
				var node = pt.ToXml("pt", false);
				var PT = node.As<PointF>();
				Assert.True(pt.Equals(PT));
			}
			{
				var arr = new[] { new Point(1, 1), new Point(2, 2) };
				var node = arr.ToXml("arr", true);
				var ARR = node.As<Point[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
			{
				var rc = new Rectangle(1, 2, 3, 4);
				var node = rc.ToXml("rect", false);
				var RC = node.As<Rectangle>();
				Assert.True(Equals(rc, RC));
			}
			{
				var rc = new RectangleF(1f, 2f, 3f, 4f);
				var node = rc.ToXml("rect", false);
				var RC = node.As<RectangleF>();
				Assert.True(Equals(rc, RC));
			}
		}
		[Test]
		public void ToXmlDateTime()
		{
			{
				var dto0 = DateTimeOffset.MinValue;
				var dto1 = dto0.ToXml("min_time", true).As<DateTimeOffset>();
				Assert.Equal(dto0, dto1);
			}
			{
				var dto0 = new DateTimeOffset(2015, 11, 02, 14, 04, 23, 456, TimeSpan.Zero);
				var dto1 = dto0.ToXml("today", true).As<DateTimeOffset>();
				Assert.Equal(dto0, dto1);
			}
		}
		[Test]
		public void ToXmlGuid()
		{
			var guid = Guid.NewGuid();
			var node = guid.ToXml("guid", false);
			var GUID = node.As<Guid>();
			Assert.Equal(guid, GUID);
		}
		[Test]
		public void ToXmlCustomTypes()
		{
			{
				// XElement constructible class
				var node = new Elem1(4).ToXml("four", false);
				var four = node.As<Elem1>();
				Assert.Equal(4U, four.m_uint);
			}
			{
				var arr = new[] { new Elem2(1, "1"), null, new Elem2(3, "3") };
				var node = arr.ToXml("arr", false);
				var ARR = node.As<Elem2[]>(factory: t => new Elem2(0, ""));
				Assert.True(arr.SequenceEqual(ARR));
			}
			{
				var arr = new[] { new Elem2(1, "1"), new Elem2(2, "2"), new Elem2(3, "3") };
				var node = arr.ToXml("arr", false);
				var ARR = node.As<Elem2[]>(factory: t => new Elem2(0, null));
				Assert.True(arr.SequenceEqual(ARR));
			}
			{
				var dc = new Elem2(2, "3");
				var node = dc.ToXml("dc", false);
				var DC = node.As<Elem2>(factory: t => new Elem2(0, null));
				Assert.Equal(dc.m_int, DC.m_int);
				Assert.Equal(dc.m_string, DC.m_string);
			}
			{
				var e4 = new Elem4 { m_int = 3 };
				var node = e4.ToXml("e4", false);
				var E4 = node.As<Elem4>();
				Assert.Equal(e4.m_int, E4.m_int);
			}
		}
		[Test]
		public void ToXmlArrays()
		{
			{
				var arr = new int[] { 0, 1, 2, 3, 4 };
				var node = arr.ToXml("arr", false);
				var ARR = node.As<int[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
			{
				var arr = new string[] { "hello", "world" };
				var node = arr.ToXml("arr", false);
				var ARR = node.As<string[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
		}
		[Test]
		public void ToXmlNullables()
		{
			{
				int? three = 3;
				var node = three.ToXml("three", false);
				var THREE = node.As<int?>();
				Assert.True(three == THREE);
			}
			{
				var arr = new int?[] { 1, null, 2 };
				var node = arr.ToXml("arr", true);
				var ARR = node.As<int?[]>();
				Assert.True(arr.SequenceEqual(ARR));
			}
		}
		[Test]
		public void ToXmlObjectArrays()
		{
			var arr = new object?[] { null, new Elem1(1), new Elem2(2, "2"), new Elem3(3, "3") };
			var node = arr.ToXml("arr", true);
			var ARR = node.As<object[]>(factory: ty =>
				 {
					 if (ty == typeof(Elem1)) return new Elem1();
					 if (ty == typeof(Elem2)) return new Elem2(0, string.Empty);
					 if (ty == typeof(Elem3)) return new Elem3();
					 throw new Exception("Unexpected type");
				 });
			Assert.True(arr.SequenceEqual(ARR));
		}
		[Test]
		public void ToXmlPrTypes()
		{
			{
				var v = new v2(1f, -2f);
				var node = v.ToXml("v", true);
				var V = node.As<v2>();
				Assert.True(v == V);
			}
			{
				var v = new v4(1f, -2f, 0f, 1f);
				var node = v.ToXml("v", true);
				var V = node.As<v4>();
				Assert.True(v == V);
			}
			{
				var r = new Rylogic.Common.RangeI(-1, +1);
				var node = r.ToXml("r", true);
				var R = node.As<Rylogic.Common.RangeI>();
				Assert.True(r == R);
			}
			{
				var r = new RangeF(-0.2, +0.2);
				var node = r.ToXml("r", true);
				var R = node.As<Rylogic.Common.RangeF>();
				Assert.True(r == R);
			}
		}
		[Test]
		public void ToXmlContainers()
		{
			{
				var kv = new KeyValuePair<int, string>(42, "fortytwo");
				var node = kv.ToXml("kv", false);
				var KV = node.As<KeyValuePair<int, string>>();
				Assert.True(Equals(kv, KV));
			}
			{
				var list = new List<string> { "one", "two" };
				var node = list.ToXml("list", false);
				var LIST = node.As<List<string>>();
				Assert.True(list.SequenceEqual(LIST));
			}
			{
				var dic = new Dictionary<int, float> { [1] = 1.1f, [2] = 2.2f };
				var node = dic.ToXml("dic", false);
				var DIC = node.As<Dictionary<int, float>>();
				Assert.True(dic.SequenceEqualUnordered(DIC));
			}
			{
				var seq = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Where(x => x % 2 == 1);
				var node = seq.ToXml("seq", false);
				var SEQ0 = node.As<int[]>();
				var SEQ1 = node.As<List<int>>();
				var SEQ2 = node.As<HashSet<int>>();
				Assert.True(seq.SequenceEqual(SEQ0));
				Assert.True(seq.SequenceEqual(SEQ1));
				Assert.True(seq.SequenceEqualUnordered(SEQ2));
			}
		}
		[Test]
		public void ToXmlAnonymous()
		{
			var obj0 = new { One = "one", Two = 2, Three = 6.28 };
			var obj1 = new[]
			{
				new { One = "one", Two = 2, Three = 6.28 },
				new { One = "won", Two = 22, Three = 2.86 },
				new { One = "111", Two = 222, Three = 8.62 },
			};
			
			var node0 = obj0.ToXml("obj0", true);
			var node1 = obj1.ToXml("obj1", true);

			Xml_.Config.AnonymousTypesAsDictionary();

			var OBJ0 = (IDictionary<string, object>?)node0.ToObject();
			if (OBJ0 == null) throw new NullReferenceException();
			Assert.Equal("one", OBJ0["One"]);
			Assert.Equal(2    , OBJ0["Two"]);
			Assert.Equal(6.28 , OBJ0["Three"]);

			var OBJ1 = (object[]?)node1.ToObject();
			if (OBJ1 == null) throw new NullReferenceException();
			Assert.Equal(3, OBJ1.Length);

			Assert.Equal("one", ((IDictionary<string, object>)OBJ1[0])["One"]);
			Assert.Equal(2    , ((IDictionary<string, object>)OBJ1[0])["Two"]);
			Assert.Equal(6.28 , ((IDictionary<string, object>)OBJ1[0])["Three"]);

			Assert.Equal("won", ((IDictionary<string, object>)OBJ1[1])["One"]);
			Assert.Equal(22   , ((IDictionary<string, object>)OBJ1[1])["Two"]);
			Assert.Equal(2.86 , ((IDictionary<string, object>)OBJ1[1])["Three"]);

			Assert.Equal("111", ((IDictionary<string, object>)OBJ1[2])["One"]);
			Assert.Equal(222  , ((IDictionary<string, object>)OBJ1[2])["Two"]);
			Assert.Equal(8.62 , ((IDictionary<string, object>)OBJ1[2])["Three"]);
		}
		[Test]
		public void XmlAs()
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
			Assert.Equal(1, root.Element("a").As<int>());
			Assert.Equal(2.0f, root.Element("b").As<float>());
			Assert.Equal("cat", root.Element("c").As<string>());
			Assert.Equal(EEnum.Dog, root.Element("d").As<EEnum>());

			var ints = new List<int>();
			root.Element("e").As(ints, "i");
			Assert.Equal(5, ints.Count);
			for (int i = 0; i != 4; ++i)
				Assert.Equal(i, ints[i]);

			var chars = new List<char>();
			root.Element("e").As(chars, "j");
			Assert.Equal(5, chars.Count);
			for (int j = 0; j != 4; ++j)
				Assert.Equal((char)('a' + j), chars[j]);

			ints.Clear();
			root.Element("e").As(ints, "i", (lhs, rhs) => lhs == rhs);
			Assert.Equal(4, ints.Count);
			for (int i = 0; i != 4; ++i)
				Assert.Equal(i, ints[i]);
		}
		[Test]
		public void XmlAdd()
		{
			var xml = new XDocument();
			var cmt = xml.Add2(new XComment("comments")); Assert.Equal(cmt.Value, "comments");
			var root = xml.Add2(new XElement("root")); Assert.AreSame(xml.Root, root);

			var ints = new List<int> { 0, 1, 2, 3, 4 };
			var elems = Array_.New(5, i => new Elem1((uint)i));
			string s;

			var xint = root.Add2("elem", 42, true);
			s = root.ToString(SaveOptions.DisableFormatting);
			Assert.Equal(
				"<root>" +
					"<elem ty=\"System.Int32\">42</elem>" +
				"</root>"
				, s);
			xint.Remove();

			var xelem = root.Add2("elem", elems[0], true);
			s = root.ToString(SaveOptions.DisableFormatting);
			Assert.Equal(
				"<root>" +
				"<elem ty=\"Rylogic.UnitTests.TestXml+Elem1\">elem_0</elem>" +
				"</root>"
				, s);
			xelem.Remove();

			var xints = root.Add2("ints", "i", ints, true);
			s = root.ToString(SaveOptions.DisableFormatting);
			Assert.Equal(
				"<root>" +
					"<ints>" +
						"<i ty=\"System.Int32\">0</i>" +
						"<i ty=\"System.Int32\">1</i>" +
						"<i ty=\"System.Int32\">2</i>" +
						"<i ty=\"System.Int32\">3</i>" +
						"<i ty=\"System.Int32\">4</i>" +
					"</ints>" +
				"</root>"
				, s);
			xints.Remove();

			var xelems = root.Add2("elems", "i", elems, true);
			s = root.ToString(SaveOptions.DisableFormatting);
			Assert.Equal(
				"<root>" +
					"<elems>" +
					"<i ty=\"Rylogic.UnitTests.TestXml+Elem1\">elem_0</i>" +
					"<i ty=\"Rylogic.UnitTests.TestXml+Elem1\">elem_1</i>" +
					"<i ty=\"Rylogic.UnitTests.TestXml+Elem1\">elem_2</i>" +
					"<i ty=\"Rylogic.UnitTests.TestXml+Elem1\">elem_3</i>" +
					"<i ty=\"Rylogic.UnitTests.TestXml+Elem1\">elem_4</i>" +
					"</elems>" +
				"</root>"
				, s);
			xelems.Remove();
		}
		[Test]
		public void XmlElements()
		{
			const string src =
				"<root>" +
					"<one>" +
						"<red/>" +
						"<red/>" +
					"</one>" +
					"<one>" +
						"<red/>" +
						"<red/>" +
					"</one>" +
					"<one/>" +
				"</root>";
			var xml = XDocument.Parse(src);
			var cnt = xml.Root.Elements("one", "red").Count();
			Assert.Equal(cnt, 4);
		}
		[Test]
		public void XmlEnumerateLeaves()
		{
			const string src =
			#region
@"<root>
	0
	<branch>
		<branch>
			<leaf>1</leaf>
		</branch>
		<branch>
			<!-- comment -->
			<leaf>2</leaf>
		</branch>
	</branch>
	<branch>
		<leaf>3</leaf>
		<branch>
			<branch>
				<leaf>4</leaf>
			</branch>
		</branch>
	</branch>
	<leaf>5</leaf>
</root>";
			#endregion

			var xml = XDocument.Parse(src).Root;

			var leaf_nodes = xml.LeafNodes().Select(x => x.GetType().Name).ToList();
			Assert.True(leaf_nodes.SequenceEqual(new[] { "XText", "XElement", "XComment", "XElement", "XElement", "XElement", "XElement" }));

			var leaf_elems = xml.LeafElements().Select(x => x.Value).ToList();
			Assert.True(leaf_elems.SequenceEqual(new[] { "1", "2", "3", "4", "5" }));
		}
		[Test]
		public void XmlDiffPatch0()
		{
			const string xml0_src =
			#region xml0
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
	text1
	<one>
		<red />
		<red />
		<two>
			<blue />
			<blue />
		</two>
	</one>
	<two>
		<has_child/>
	</two>
	<changed0>
		<blue />
		<red />
	</changed0>
	<changed1 ty=""car"" same=""same"" old=""boris"">
		<red />
		<blue />
	</changed1>
	<unchanged0 ty=""flat"">
		<red>RED</red>
		<blue />
	</unchanged0>
	<removed>
		<green />
		<blue>BLUE</blue>
	</removed>
</root>";
			#endregion
			const string xml1_src =
			#region xml1
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
	text0
	<one>
		<red />
		<red />
		<two>
			<blue />
			<blue />
		</two>
	</one>
	<two />
	<added ty=""string"">
		<red />
		<blue>BLUE</blue>
	</added>
	<added1 ty=""fish"">
		<green>GREEN</green>
	</added1>
	<changed0>
		<red />
		<blue />
	</changed0>
	<changed1 ty=""boat"" same=""same"" new=""fred"">
		<red />
		<blue />
	</changed1>
	<added2>hamburger</added2>
	<!-- A comment -->
	<unchanged0 ty=""flat"">
		<red>RED</red>
		<blue />
	</unchanged0>
</root>";
			#endregion
			const string xml_patch =
			#region patch xml
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
	<Value idx='0'>
	text0
	</Value>
	<Change idx='2' name='two'>
		<Remove idx='0' name='has_child' />
	</Change>
	<Insert idx='3' node_type='Element' name='added'>
		<Attr name='ty'>string</Attr>
		<Insert idx='0' node_type='Element' name='red' />
		<Insert idx='1' node_type='Element' name='blue'>
			<Value idx='0'>BLUE</Value>
		</Insert>
	</Insert>
	<Insert idx='4' node_type='Element' name='added1'>
		<Attr name='ty'>fish</Attr>
		<Insert idx='0' node_type='Element' name='green'>
			<Value idx='0'>GREEN</Value>
		</Insert>
	</Insert>
	<Change idx='5' name='changed0'>
		<Insert idx='0' node_type='Element' name='red' />
		<Remove idx='2' name='red' />
	</Change>
	<Change idx='6' name='changed1'>
		<Attr name='old' />
		<Attr name='new'>fred</Attr>
		<Attr name='ty'>boat</Attr>
	</Change>
	<Insert idx='7' node_type='Element' name='added2'>
		<Value idx='0'>hamburger</Value>
	</Insert>
	<Insert idx='8' node_type='Comment'>
		<Value> A comment </Value>
	</Insert>
	<Remove idx='10' name='removed' />
</root>";
			#endregion

			var xml0 = XDocument.Parse(xml0_src).Root;
			var xml1 = XDocument.Parse(xml1_src).Root;
			var xmlp = XDocument.Parse(xml_patch).Root;

			// Find how xml0 is different from xml1
			var patch = xml0.Diff(xml1);
			Assert.True(XDocument.DeepEquals(patch, xmlp));

			var report = XmlDiff.Report(patch);

			// Patch xml0 so that it becomes the same as xml1
			xml0.Patch(xmlp);
			Assert.True(XDocument.DeepEquals(xml0, xml1));
		}
		[Test]
		public void XmlDiffPatch1()
		{
			const string xml0_src =
			#region xml0
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
	text1
	<one>0</one>
	<two>
		<has_child/>
	</two>
	<three>
		<blue />
		<red />
	</three>
</root>";
			#endregion
			const string xml1_src =
			#region xml1
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
	<one>1</one>
	<two>
		<child>c</child>
	</two>
	<four ty='string'>
		four
	</four>
</root>";
			#endregion
			const string xml_patch =
			#region patch xml
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <Change idx='1' name='one'>
    <Value idx='0'>1</Value>
  </Change>
  <Change idx='2' name='two'>
    <Insert idx='1' node_type='Element' name='child'>
      <Value idx='0'>c</Value>
    </Insert>
  </Change>
  <Insert idx='4' node_type='Element' name='four'>
    <Attr name='ty'>string</Attr>
    <Value idx='0'>
		four
	</Value>
  </Insert>
</root>";
			#endregion
			const string xml_result =
			#region result
@"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
	text1
	<one>1</one>
	<two>
		<has_child/>
		<child>c</child>
	</two>
	<three>
		<blue />
		<red />
	</three>
	<four ty=""string"">
		four
	</four>
</root>";
			#endregion

			var xml0 = XDocument.Parse(xml0_src).Root;
			var xml1 = XDocument.Parse(xml1_src).Root;
			var xmlp = XDocument.Parse(xml_patch).Root;
			var xmlr = XDocument.Parse(xml_result).Root;

			// Find how xml0 is different from xml1
			var patch = xml0.Diff(xml1, XmlDiff.Mode.Merge);
			Assert.True(XDocument.DeepEquals(patch, xmlp));

			var report = XmlDiff.Report(patch);

			// Patch xml0 so that it becomes the same as xml1
			xml0.Patch(xmlp);
			Assert.True(XDocument.DeepEquals(xml0, xmlr));

		}
	}
}
#endif
