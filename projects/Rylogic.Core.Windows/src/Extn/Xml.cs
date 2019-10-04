using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Rylogic.Extn.Windows
{
	public static class XmlWindowsExtensions
	{
		// Use: Xml_.Config.SupportAnonymousTypes();
		public static XmlConfig SupportAnonymousTypes(this XmlConfig cfg)
		{
			// Replace the AnonymousType mapping
			Xml_.AsMap[typeof(Xml_.AnonymousType)] = (elem, type, ctor) =>
			{
				var props = elem.Elements().Select(x => new AnonTypeProp(x.Name.LocalName, x.ToObject())).ToList();
				return CreateAnonymousTypeInstance(props);
			};
			return cfg;
		}

		/// <summary>Return an instance of an anonymous type</summary>
		private static object CreateAnonymousTypeInstance(IList<AnonTypeProp> props)
		{
			// Generate a key for the anonymous type
			var s = new StringBuilder();
			foreach (var prop in props) s.Append(prop.Type.FullName);
			var anon_type_key = s.ToString();

			var types = props.Select(x => x.Type).ToArray();
			var values = props.Select(x => x.Value).ToArray();

			// See if the anonymous type already exists
			if (!m_anon_types.TryGetValue(anon_type_key, out var anon_type))
			{
				// An existing type was not found, emit one
				var tb = ModuleBuilder.DefineType($"Rylogic.AnonymousType{m_anon_types.Count}", 
					TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.AutoClass |
					TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout);

				// Constructor
				var cb = tb.DefineConstructor(MethodAttributes.Public | MethodAttributes.SpecialName |
					MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, types);

				// Add properties for each 'prop'
				var args = new List<ParameterBuilder>();
				var fields = new List<FieldBuilder>();
				foreach (var prop in props)
				{
					// Note: Anonymous types don't have setters.
					var name = prop.Name;
					var type = prop.Type;

					// Define builders
					var ab = args.Add2(cb.DefineParameter(args.Count, ParameterAttributes.In, name));
					var fb = fields.Add2(tb.DefineField($"m_{name}", type, FieldAttributes.Private | FieldAttributes.InitOnly));
					var gb = tb.DefineMethod($"get_{name}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, type, Type.EmptyTypes);
					//var sb = tb.DefineMethod($"set_{name}", MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { type });
					var pb = tb.DefineProperty($"{name}", PropertyAttributes.None, type, null);

					// Create IL for the getter
					var getter = gb.GetILGenerator();
					getter.Emit(OpCodes.Ldarg_0);
					getter.Emit(OpCodes.Ldfld, fb);
					getter.Emit(OpCodes.Ret);

					//// Create IL for the setter
					//var setter = sb.GetILGenerator();
					//setter.MarkLabel(setter.DefineLabel());
					//setter.Emit(OpCodes.Ldarg_0);
					//setter.Emit(OpCodes.Ldarg_1);
					//setter.Emit(OpCodes.Stfld, fb);
					//setter.Emit(OpCodes.Nop);
					//setter.MarkLabel(setter.DefineLabel());
					//setter.Emit(OpCodes.Ret);

					// Set the accessors.
					pb.SetGetMethod(gb);
					//pb.SetSetMethod(sb);
				}

				// Create a constructor for the anonymous type
				{
					var ctor = cb.GetILGenerator();
					ctor.Emit(OpCodes.Ldarg_0);
					ctor.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

					var i = -1;
					if (++i < args.Count)
					{
						ctor.Emit(OpCodes.Ldarg_0);
						ctor.Emit(OpCodes.Ldarg_1);
						ctor.Emit(OpCodes.Stfld, fields[i]);
					}
					if (++i < args.Count)
					{
						ctor.Emit(OpCodes.Ldarg_0);
						ctor.Emit(OpCodes.Ldarg_2);
						ctor.Emit(OpCodes.Stfld, fields[i]);
					}
					if (++i < args.Count)
					{
						ctor.Emit(OpCodes.Ldarg_0);
						ctor.Emit(OpCodes.Ldarg_3);
						ctor.Emit(OpCodes.Stfld, fields[i]);
					}
					for (++i; i < args.Count; ++i)
					{
						ctor.Emit(OpCodes.Ldarg_0);
						ctor.Emit(OpCodes.Ldarg_S, args[i].Name);
						ctor.Emit(OpCodes.Stfld, fields[i]);
					}
					ctor.Emit(OpCodes.Ret);
				}

				// Add the anonymous type to the dictionary
				anon_type = tb.CreateType();
				m_anon_types.Add(anon_type_key, anon_type);
			}
			return anon_type.GetConstructor(types).Invoke(values);
		}
		private static readonly Dictionary<string, Type> m_anon_types = new Dictionary<string, Type>();

		/// <summary>A property in an anonymous type</summary>
		private struct AnonTypeProp
		{
			public AnonTypeProp(string name, object? value)
			{
				Name = name;
				Value = value;
				Type = value?.GetType() ?? typeof(object);
			}
			public string Name;
			public object? Value;
			public Type Type;
		}

		/// <summary>A module builder instance for anonymous XML types</summary>
		private static ModuleBuilder ModuleBuilder
		{
			get
			{
				if (m_module_builder == null)
				{
					var ass_name = new AssemblyName("Rylogic.XmlDynamicTypes");
					var ass_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(ass_name, AssemblyBuilderAccess.Run);
					m_module_builder = ass_builder.DefineDynamicModule(ass_name.Name);
				}
				return m_module_builder;
			}
		}
		private static ModuleBuilder? m_module_builder;
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;
	using Extn.Windows;

	[TestFixture]
	public class TestXml
	{
		[Test]
		public void ToXmlAnonymous()
		{
			Xml_.Config.SupportAnonymousTypes();

			var obj0 = new { One = "one", Two = 2, Three = 6.28 };
			var obj1 = new[]
			{
				new { One = "one", Two = 2, Three = 6.28 },
				new { One = "won", Two = 22, Three = 2.86 },
				new { One = "111", Two = 222, Three = 8.62 },
			};

			var node0 = obj0.ToXml("obj0", true);
			var node1 = obj1.ToXml("obj1", true);

			var OBJ0 = (dynamic?)node0.ToObject() ?? throw new NullReferenceException();
			Assert.Equal("one", OBJ0.One);
			Assert.Equal(2, OBJ0.Two);
			Assert.Equal(6.28, OBJ0.Three);

			var OBJ1 = (dynamic[]?)node1.ToObject() ?? throw new NullReferenceException();
			Assert.Equal(3, OBJ1.Length);

			Assert.Equal("one", OBJ1[0].One);
			Assert.Equal(2    , OBJ1[0].Two);
			Assert.Equal(6.28 , OBJ1[0].Three);

			Assert.Equal("won", OBJ1[1].One);
			Assert.Equal(22   , OBJ1[1].Two);
			Assert.Equal(2.86 , OBJ1[1].Three);

			Assert.Equal("111", OBJ1[2].One);
			Assert.Equal(222  , OBJ1[2].Two);
			Assert.Equal(8.62 , OBJ1[2].Three);
		}
	}
}
#endif
