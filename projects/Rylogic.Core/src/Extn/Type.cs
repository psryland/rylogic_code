using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Rylogic.Common;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Extn
{
	public static class Type_
	{
		/// <summary>Return the type of this instance, falling back to its declared type if null</summary>
		public static Type Ty<T>(this T t)
		{
			return t?.GetType() ?? typeof(T);
		}

		/// <summary>Call the constructor of this type to create a new instance. Use Util'T.New if the type is known at compile time</summary>
		public static object New(this Type ty)
		{
			if (!ty.IsClass)
				return Activator.CreateInstance(ty) ?? throw new Exception($"Default constructor for {ty.Name} not found"); ;

			var cons = ty.GetConstructor(Array.Empty<Type>()) ?? throw new Exception($"Default constructor for {ty.Name} not found");
			return cons.Invoke(null);
		}
		public static object New<A0>(this Type ty, A0 a0)
		{
			var cons = ty.GetConstructor(new Type[] { a0.Ty() }) ?? throw new Exception($"Arity 1 constructor for {ty.Name} not found");
			return cons.Invoke(new object?[] { a0 });
		}
		public static object New<A0, A1>(this Type ty, A0 a0, A1 a1)
		{
			var cons = ty.GetConstructor(new Type[] { a0.Ty(), a1.Ty() }) ?? throw new Exception($"Arity 2 constructor for {ty.Name} not found");
			return cons.Invoke(new object?[] { a0, a1 });
		}
		public static object New<A0, A1, A2>(this Type ty, A0 a0, A1 a1, A2 a2)
		{
			var cons = ty.GetConstructor(new Type[] { a0.Ty(), a1.Ty(), a2.Ty() }) ?? throw new Exception($"Arity 3 constructor for {ty.Name} not found");
			return cons.Invoke(new object?[] { a0, a1, a2 });
		}
		public static object New<A0, A1, A2, A3>(this Type ty, A0 a0, A1 a1, A2 a2, A3 a3)
		{
			var cons = ty.GetConstructor(new Type[] { a0.Ty(), a1.Ty(), a2.Ty(), a3.Ty() }) ?? throw new Exception($"Arity 3 constructor for {ty.Name} not found");
			return cons.Invoke(new object?[] { a0, a1, a2, a3 });
		}
		public static object New(this Type ty, object?[]? arg_list)
		{
			var arg_types = arg_list?.Select(x => x.Ty()).ToArray() ?? Array.Empty<Type>();
			var cons = ty.GetConstructor(arg_types) ?? throw new Exception($"Arity {arg_types.Length} constructor for {ty.Name} not found");
			return cons.Invoke(arg_list);
		}

		/// <summary>Return a default instance of this type</summary>
		public static object? DefaultInstance(this Type type)
		{
			// If no Type was supplied, if the Type was a reference type, or if the Type was a System.Void, return null
			if (type == null || !type.IsValueType || type == typeof(void))
				return null;

			// If the supplied Type has generic parameters, its default value cannot be determined
			if (type.ContainsGenericParameters)
				throw new ArgumentException($"{type.FullName} contains generic parameters, so the default value cannot be retrieved");

			// If the Type is a primitive type, or if it is another publicly-visible
			// value type (i.e. struct/enum), return a default instance of the value type
			if (type.IsPrimitive || !type.IsNotPublic)
			{
				try { return Activator.CreateInstance(type); }
				catch (Exception e)
				{
					throw new ArgumentException($"Activator.CreateInstance could not create a default instance of {type.FullName}. {e.Message}", e);
				}
			}

			// Fail with exception
			throw new ArgumentException($"{type.FullName} is not a publicly-visible type, so the default value cannot be retrieved");
		}

		/// <summary>True if this type is a scalar value</summary>
		public static bool IsScalar(this Type t)
		{
			return
				t == typeof(byte) ||
				t == typeof(sbyte) ||
				t == typeof(char) ||
				t == typeof(short) ||
				t == typeof(ushort) ||
				t == typeof(int) ||
				t == typeof(uint) ||
				t == typeof(long) ||
				t == typeof(ulong) ||
				t == typeof(float) ||
				t == typeof(double) ||
				t == typeof(decimal);
		}

		/// <summary>Resolve a type name to a type</summary>
		public static Type Resolve(string name, bool throw_on_error = true)
		{
			// Temporary conversion from namespace 'pr' to namespace 'Rylogic'
			{
				string Replace(string old, string nue) => name.StartsWith(old) ? name.Replace(old, nue) : name;
				if (name.StartsWith("pr."))
				{
					name = Replace("pr.attrib.", "Rylogic.Attrib.");
					name = Replace("pr.audio.", "Rylogic.Audio.");
					name = Replace("pr.common.", "Rylogic.Common.");
					name = Replace("pr.container.", "Rylogic.Container.");
					name = Replace("pr.crypt.", "Rylogic.Crypt.");
					name = Replace("pr.db.", "Rylogic.Db.");
					name = Replace("pr.extn.", "Rylogic.Extn.");
					name = Replace("pr.gfx.", "Rylogic.Graphix.");
					name = Replace("pr.gui.", "Rylogic.Gui.");
					name = Replace("pr.inet.", "Rylogic.INet.");
					name = Replace("pr.ldr.", "Rylogic.LDraw.");
					name = Replace("pr.maths.", "Rylogic.Maths.");
					name = Replace("pr.scintilla.", "Rylogic.Scintilla.");
					name = Replace("pr.stream.", "Rylogic.Streams.");
					name = Replace("pr.util.", "Rylogic.Utility.");
					name = Replace("pr.view3d.", "Rylogic.Graphix.");
					name = Replace("pr.win32.", "Rylogic.Windows32.");
				}
				name = Replace("Rylogic.Extn.ToolStripLocations", "Rylogic.Gui.WinForms.ToolStripLocations");
			}

			var type = Type.GetType(name, ResolveAssemblyCB, ResolveTypeCB, throw_on_error);
			return type ?? throw new TypeLoadException($"Type {name} not found");

			// Type resolver callback
			static Assembly ResolveAssemblyCB(AssemblyName name)
			{
				// This is only called if 'name' specifies an assembly
				var ass = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName() == name && x.CodeBase == name.CodeBase);
				if (ass == null) ass = AppDomain.CurrentDomain.Load(name);
				if (ass == null) throw new TypeLoadException($"No assembly called {name.FullName} is currently loaded");
				return ass;
			}
			static Type? ResolveTypeCB(Assembly? assembly, string type_name, bool ignore_case)
			{
				// Notes:
				//  - There is an issue when assemblies are loaded dynamically. Dependent assemblies
				//    get loaded again and the types within those assemblies are considered different
				//    to the types of the same name (even thought the GUIDs are the same).
				//  - "Types are per-assembly; if you have "the same" assembly loaded twice, then types
				//    in each "copy" of the assembly are not considered to be the same type."
				//  - "If assemblies are loaded from different paths, they're considered different
				//    assemblies and therefore their types are different."
				//  - "Loading an assembly into multiple contexts can cause type identity problems. If the
				//    same type is loaded from the same assembly into two different contexts, it is as if
				//    two different types with the same name had been loaded. An InvalidCastException is
				//    thrown if you try to cast one type to the other, with the confusing message that
				//    'type MyType cannot be cast to type MyType'.

				var assems = assembly != null ? new[] { assembly } : AppDomain.CurrentDomain.GetAssemblies();
				var types = assems.Select(ass => ass.GetType(type_name, false, ignore_case)).Where(t => t != null);
				Debug.WriteIf(!types.AllSame(), $"WARNING: Multiple types called {type_name} are currently loaded!");
				return types.LastOrDefault();
				//if (!types.AllSame()) throw new TypeLoadException($"Multiple types called {type_name} are currently loaded");
				//return types.FirstOrDefault();
			}
		}
		private static readonly Cache<string, Type> m_type_resolve_cache = new Cache<string, Type>(100);

		/// <summary>Returns all inherited members for a type (including private members)</summary>
		public static IEnumerable<MemberInfo> AllMembers(this Type type, BindingFlags flags)
		{
			return type != null && type != typeof(object) && type.BaseType != null
				? AllMembers(type.BaseType, flags).Concat(type.GetMembers(flags|BindingFlags.DeclaredOnly))
				: Enumerable.Empty<MemberInfo>();
		}

		/// <summary>Returns all inherited properties for a type</summary>
		public static IEnumerable<PropertyInfo> AllProps(this Type type, BindingFlags flags)
		{
			return type != null && type != typeof(object) && type.BaseType != null
				? AllProps(type.BaseType, flags).Concat(type.GetProperties(flags|BindingFlags.DeclaredOnly))
				: Enumerable.Empty<PropertyInfo>();
		}

		/// <summary>Returns all inherited fields for a type</summary>
		public static IEnumerable<FieldInfo> AllFields(this Type type, BindingFlags flags)
		{
			return type != null && type != typeof(object) && type.BaseType != null
				? AllFields(type.BaseType, flags).Concat(type.GetFields(flags|BindingFlags.DeclaredOnly))
				: Enumerable.Empty<FieldInfo>();
		}

		/// <summary>Returns all inherited events for a type</summary>
		public static IEnumerable<EventInfo> AllEvents(this Type type, BindingFlags flags)
		{
			return type != null && type != typeof(object) && type.BaseType != null
				? AllEvents(type.BaseType, flags).Concat(type.GetEvents(flags|BindingFlags.DeclaredOnly))
				: Enumerable.Empty<EventInfo>();
		}

		/// <summary>Find all types derived from this type</summary>
		public static List<Type> DerivedTypes(this Type type)
		{
			var ass = Assembly.GetAssembly(type) ?? throw new Exception($"The assembly containing {type.Name} could not be found");
			return ass.GetTypes().Where(t => t != type && type.IsAssignableFrom(t)).ToList();
		}

		/// <summary>Returns true if 'type' is or inherits 'baseOrInterface'</summary>
		public static bool Inherits(this Type type, Type baseOrInterface)
		{
			return baseOrInterface.IsAssignableFrom(type);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static Attribute FindAttribute(this Type type, Type attribute_type, bool inherit = true)
		{
			if (!attribute_type.Inherits(typeof(Attribute))) throw new Exception("Expected 'attribute_type' to be a subclass of 'Attribute'");
			return type.GetCustomAttributes(attribute_type, inherit).Cast<Attribute>().FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static Attribute FindAttribute(this Assembly ass, Type attribute_type, bool inherit = true)
		{
			if (!attribute_type.Inherits(typeof(Attribute))) throw new Exception("Expected 'attribute_type' to be a subclass of 'Attribute'");
			return ass.GetCustomAttributes(attribute_type, inherit).Cast<Attribute>().FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this method or null.</summary>
		public static Attribute FindAttribute(this MethodInfo mi, Type attribute_type, bool inherit = true)
		{
			if (!attribute_type.Inherits(typeof(Attribute))) throw new Exception("Expected 'attribute_type' to be a subclass of 'Attribute'");
			return mi.GetCustomAttributes(attribute_type, inherit).Cast<Attribute>().FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static T FindAttribute<T>(this Type type, bool inherit = true) where T:Attribute
		{
			return (T)type.FindAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this assembly or null.</summary>
		public static T FindAttribute<T>(this Assembly ass, bool inherit = true) where T:Attribute
		{
			return (T)ass.FindAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type or null.</summary>
		public static T FindAttribute<T>(this MethodInfo mi, bool inherit = true) where T:Attribute
		{
			return (T)mi.FindAttribute(typeof(T), inherit);
		}

		/// <summary>Returns an attribute associated with a member or null</summary>
		public static T FindAttribute<T>(this MemberInfo mi, bool inherit = true) where T:Attribute
		{
			return (T)mi.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type. Throws if not found</summary>
		public static Attribute GetAttribute(this Type type, Type attribute_type, bool inherit = true)
		{
			var attr = FindAttribute(type, attribute_type, inherit);
			if (attr == null) throw new Exception($"Type '{type.FullName}' is not decorated with the '{attribute_type.FullName}' attribute.");
			return attr;
		}

		/// <summary>Returns the first instance of 'attribute_type' for this assembly. Throws if not found</summary>
		public static Attribute GetAttribute(this Assembly ass, Type attribute_type, bool inherit = true)
		{
			var attr = FindAttribute(ass, attribute_type, inherit);
			if (attr == null) throw new Exception($"Type '{ass.FullName}' is not decorated with the '{attribute_type.FullName}' attribute.");
			return attr;
		}

		/// <summary>Returns the first instance of 'attribute_type' for this type. Throws if not found</summary>
		public static T GetAttribute<T>(this Type type, bool inherit = true) where T:Attribute
		{
			return (T)type.GetAttribute(typeof(T), inherit);
		}

		/// <summary>Returns the first instance of 'attribute_type' for this assembly. Throws if not found</summary>
		public static T GetAttribute<T>(this Assembly ass, bool inherit = true) where T:Attribute
		{
			return (T)ass.GetAttribute(typeof(T), inherit);
		}

		/// <summary>Returns true if 'type' has an instance of attribute 'T'</summary>
		public static bool HasAttribute<T>(this Type type, bool inherit = true) where T :Attribute
		{
			return type.FindAttribute<T>(inherit) != null;
		}

		/// <summary>True if this is an anonymous type</summary>
		public static bool IsAnonymousType(this Type type)
		{
			return
				type.FindAttribute<CompilerGeneratedAttribute>(inherit: false) != null &&
				type.FullName != null && type.FullName.Contains("AnonymousType");
		}

		/// <summary>Returns the methods on this type that are decorated with the attribute 'attribute_type'</summary>
		public static IEnumerable<MethodInfo> FindMethodsWithAttribute(this Type type, Type attribute_type, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance)
		{
			return type.GetMethods(flags).Where(x => x.FindAttribute(attribute_type) != null);
		}

		/// <summary>Returns the methods on this type that are decorated with the attribute 'attribute_type'</summary>
		public static IEnumerable<MethodInfo> FindMethodsWithAttribute<T>(this Type type, BindingFlags flags = BindingFlags.Public|BindingFlags.Instance) where T:Attribute
		{
			return type.FindMethodsWithAttribute(typeof(T), flags);
		}

		/// <summary>Convert this type to is equivalent DbType</summary>
		public static DbType DbType(this Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			type = type.IsEnum ? type.GetEnumUnderlyingType() : type;

			return type.Name switch
			{
				"String"  => System.Data.DbType.String,
				"Byte[]"  => System.Data.DbType.Binary,
				"Boolean" => System.Data.DbType.Boolean,
				"Byte"    => System.Data.DbType.Byte,
				"SByte"   => System.Data.DbType.SByte,
				"Int16"   => System.Data.DbType.Int16,
				"Int32"   => System.Data.DbType.Int32,
				"Int64"   => System.Data.DbType.Int64,
				"UInt16"  => System.Data.DbType.UInt16,
				"UInt32"  => System.Data.DbType.UInt32,
				"UInt64"  => System.Data.DbType.UInt64,
				"Single"  => System.Data.DbType.Single,
				"Double"  => System.Data.DbType.Double,
				"Decimal" => System.Data.DbType.Decimal,
				"Guid"    => System.Data.DbType.Guid,
				_ => throw new Exception($"Unknown conversion from {type.Name} to DbType"),
			};
		}

		/// <summary>Reformat the stack trace string to suit the debugger output window</summary>
		public static string OutputWindowFormat(this StackTrace st)
		{
			const string file_pattern = @" in " + Regex_.FullPathPattern + @":line\s(?<line>\d+)";
			return Util.FormatForOutputWindow(st.ToString(), file_pattern);
		}
	}

	/// <summary>'bool' type extensions</summary>
	public static class bool_
	{
		public static bool? TryParse(string val)
		{
			return bool.TryParse(val, out var o) ? o : (bool?)null;
		}
	}

	/// <summary>'int' type extensions</summary>
	public static class int_
	{
		/// <summary>Enumerate ints in the range [beg, end)</summary>
		public static IEnumerable<int> Range(int beg, int end, bool inclusive = false)
		{
			// Handle forward and backward iteration
			for (;beg < end;)
				yield return beg++;
			for (;beg > end;)
				yield return beg--;
			if (inclusive)
				yield return end;
		}

		/// <summary>Enumerate ints in the range [0, count)</summary>
		public static IEnumerable<int> Range(int count)
		{
			return Range(0, count);
		}

		/// <summary>Parse an integer returning the value or null</summary>
		public static int? TryParse(string val, NumberStyles style = NumberStyles.Integer)
		{
			return int.TryParse(val, style, null, out var o) ? (int?)o : null;
		}

		/// <summary>Parse an array of integer values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of integer values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed integers</returns>
		public static int[] ParseArray(string val, NumberStyles style = NumberStyles.Integer, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => int.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end) or [end,beg) (whichever is a positive range)</summary>
		public static bool Within(this int x, int beg, int end)
		{
			return beg <= end
				? x >= beg && x < end
				: x >= end && x < beg;
		}
		public static bool Within(this int? x, int beg, int end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool Within(this int x, RangeI range)
		{
			return Within(x, range.Begi, range.Endi);
		}
		public static bool WithinInclusive(this int x, int beg, int end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this int? x, int beg, int end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}
		public static bool WithinInclusive(this int x, RangeI range)
		{
			return x.WithinInclusive(range.Begi, range.Endi);
		}
	}

	/// <summary>'uint' type extensions</summary>
	public static class uint_
	{
		/// <summary>Parse an unsigned integer returning the value or null</summary>
		public static uint? TryParse(string val, NumberStyles style = NumberStyles.Integer)
		{
			return uint.TryParse(val, style, null, out var o) ? (uint?)o : null;
		}

		/// <summary>Parse an array of integer values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of integer values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed integers</returns>
		public static uint[] ParseArray(string val, NumberStyles style = NumberStyles.Integer, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => uint.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end) or [end,beg) (whichever is a positive range)</summary>
		public static bool Within(this uint x, uint beg, uint end)
		{
			return beg <= end
				? x >= beg && x < end
				: x >= end && x < beg;
		}
		public static bool Within(this uint? x, uint beg, uint end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool WithinInclusive(this uint x, uint beg, uint end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this uint? x, uint beg, uint end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}
	}

	/// <summary>'long' type extensions</summary>
	public static class long_
	{
		/// <summary>Parse a long returning the value or null</summary>
		public static long? TryParse(string val, NumberStyles style = NumberStyles.Integer)
		{
			return long.TryParse(val, style, null, out var o) ? (long?)o : null;
		}

		/// <summary>Parse an array of integer values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of integer values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed integers</returns>
		public static long[] ParseArray(string val, NumberStyles style = NumberStyles.Integer, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => long.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end) or [end,beg) (whichever is a positive range)</summary>
		public static bool Within(this long x, long beg, long end)
		{
			return beg <= end
				? x >= beg && x < end
				: x >= end && x < beg;
		}
		public static bool Within(this long? x, long beg, long end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool Within(this long x, RangeI range)
		{
			return x.Within(range.Beg, range.End);
		}
		public static bool WithinInclusive(this long x, long beg, long end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this long? x, long beg, long end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}
		public static bool WithinInclusive(this long x, RangeI range)
		{
			return x.WithinInclusive(range.Beg, range.End);
		}
	}

	/// <summary>'ulong' type extensions</summary>
	public static class ulong_
	{
		public static ulong? TryParse(string val, NumberStyles style = NumberStyles.Integer)
		{
			return ulong.TryParse(val, style, null, out var o) ? (ulong?)o : null;
		}

		/// <summary>Parse an array of integer values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of integer values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed integers</returns>
		public static ulong[] ParseArray(string val, NumberStyles style = NumberStyles.Integer, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => ulong.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end) or [end,beg) (whichever is a positive range)</summary>
		public static bool Within(this ulong x, ulong beg, ulong end)
		{
			return beg <= end
				? x >= beg && x < end
				: x >= end && x < beg;
		}
		public static bool Within(this ulong? x, ulong beg, ulong end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool WithinInclusive(this ulong x, ulong beg, ulong end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this ulong? x, ulong beg, ulong end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}
	}

	/// <summary>'float' type extensions</summary>
	public static class float_
	{
		/// <summary>Enumerate floats in the range [beg, end) with a step size of 'step'</summary>
		public static IEnumerable<float> Range(float beg, float end, float step)
		{
			for (;beg < end; beg += step)
				yield return beg;
		}
		public static IEnumerable<float> Range(RangeF range, float step)
		{
			return Range(range.Begf, range.Endf, step);
		}

		/// <summary>Try parse a float from a string</summary>
		public static float? TryParse(string val, NumberStyles style = NumberStyles.Float)
		{
			return float.TryParse(val, style, null, out var o) ? (float?)o : null;
		}

		/// <summary>Parse an array of floating point values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of floating point values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed floating point values</returns>
		public static float[] ParseArray(string val, NumberStyles style = NumberStyles.Float, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => float.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end] or [end,beg] (whichever is a positive range)</summary>
		public static bool Within(this float x, float beg, float end)
		{
			return beg <= end
				? x >= beg && x <= end
				: x >= end && x <= beg;
		}
		public static bool Within(this float? x, float beg, float end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool Within(this float x, RangeF range)
		{
			return x.Within(range.Begf, range.Endf);
		}
		public static bool WithinInclusive(this float x, float beg, float end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this float? x, float beg, float end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}
		public static bool WithinInclusive(this float x, RangeF range)
		{
			return x.WithinInclusive(range.Begf, range.Endf);
		}

		/// <summary>Format the value with the indicated number of significant digits.</summary>
		public static string ToString(this float value, int significant_digits)
		{
			return ((double)value).ToString(significant_digits);
		}
	}

	/// <summary>'double' type extensions</summary>
	public static class double_
	{
		/// <summary>Enumerate floats in the range [beg, end) with a step size of 'step'</summary>
		public static IEnumerable<double> Range(double beg, double end, double step)
		{
			for (;beg < end; beg += step)
				yield return beg;
		}
		public static IEnumerable<double> Range(RangeF range, double step)
		{
			return Range(range.Beg, range.End, step);
		}

		/// <summary>Decompose an IEEE754 double into the mantissa and exponent</summary>
		public static void Decompose(this double x, out long mantissa, out int exponent, out int sign, bool raw = false)
		{
			//    Normal numbers: (-1)^sign x 2^(e - 1023) x 1.fraction
			// Subnormal numbers: (-1)^sign x 2^(1 - 1023) x 0.fraction

			// Translate the double into sign, exponent, and mantissa.
			var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(x));
			sign     = bits.BitAt(63) != 0 ? -1 : +1;
			exponent = (int)bits.Bits(52, 11);
			mantissa = (long)bits.Bits(0, 52);
			if (raw)
				return;

			// Normal numbers: add the '1' to the front of the mantissa.
			if (exponent != 0)
				mantissa |= 1L << 52;
			else
				exponent++;

			// Bias the exponent
			exponent -= 1023;
		}
		public static long Mantissa(this double x)
		{
			x.Decompose(out var mantissa, out _, out _);
			return mantissa;
		}
		public static long Exponent(this double x)
		{
			x.Decompose(out _, out var exponent, out _);
			return exponent;
		}

		/// <summary>Create an IEEE754 double from mantissa and exponent</summary>
		public static double Compose(long mantissa, int exponent, int sign, bool raw = false)
		{
			//    Normal numbers: (-1)^sign x 2^(e - 1023) x 1.fraction
			// Subnormal numbers: (-1)^sign x 2^(1 - 1023) x 0.fraction

			if (!raw)
			{
				// Bias the exponent
				exponent += 1023;

				// Normal numbers: If the mantissa has the 52nd bit set, then it's a normal number
				if (mantissa.BitAt(52) == 1)
					mantissa &= ~(1L << 52);
				else
					exponent--;
			}

			// Translate the double into exponent and mantissa.
			var s = sign < 0 ? 1UL << 63 : 0UL;
			var e = ((ulong)exponent & 0x7FFUL) << 52;
			var m = (ulong)mantissa & 0xFFFFFFFFFFFFFUL;
			return BitConverter.Int64BitsToDouble(unchecked((long)(s | e | m)));
		}

		/// <summary>Try parse a double from a string</summary>
		public static double? TryParse(string val, NumberStyles style = NumberStyles.Float)
		{
			return double.TryParse(val, style, null, out var o) ? (double?)o : null;
		}

		/// <summary>Parse an array of floating point values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of floating point values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed floating point values</returns>
		public static double[] ParseArray(string val, NumberStyles style = NumberStyles.Float, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => double.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end] or [end,beg] (whichever is a positive range)</summary>
		public static bool Within(this double x, double beg, double end)
		{
			return beg <= end
				? x >= beg && x <= end
				: x >= end && x <= beg;
		}
		public static bool Within(this double? x, double beg, double end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool Within(this double x, RangeF range)
		{
			return x.Within(range.Beg, range.End);
		}
		public static bool WithinInclusive(this double x, double beg, double end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this double? x, double beg, double end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}
		public static bool WithinInclusive(this double x, RangeF range)
		{
			return x.WithinInclusive(range.Beg, range.End);
		}

		/// <summary>Format the value with the indicated number of significant digits.</summary>
		public static string ToString(this double value, int significant_digits)
		{
			// Use G format to get significant digits.
			// Then convert to double and use F format.
			var format1 = "{0:G" + significant_digits.ToString() + "}";
			var result = Convert.ToDouble(string.Format(format1, value)).ToString("F99");

			// Remove trailing 0s.
			result = result.TrimEnd('0');

			// Remove the decimal point and leading 0s, leaving just the digits.
			var test = result.Replace(".", "").TrimStart('0');

			// See if we have enough significant digits.
			if (significant_digits > test.Length)
			{
				// Add trailing 0s.
				result += new string('0', significant_digits - test.Length);
			}
			else
			{
				// See if we should remove the trailing decimal point.
				if (significant_digits < test.Length && result.EndsWith("."))
					result = result.Substring(0, result.Length - 1);
			}

			return result;
		}
	}

	/// <summary>'decimal' type extensions</summary>
	public static class decimal_
	{
		/// <summary>The smallest positive number representable with a decimal</summary>
		public const decimal Epsilon = 1e-28m;

		/// <summary>Enumerate values in the range [beg, end) with a step size of 'step'</summary>
		public static IEnumerable<decimal> Range(decimal beg, decimal end, decimal step)
		{
			for (;beg < end; beg += step)
				yield return beg;
		}

		/// <summary>Try parse a decimal from a string</summary>
		public static decimal? TryParse(string val, NumberStyles style = NumberStyles.Float)
		{
			return decimal.TryParse(val, style, null, out var o) ? (decimal?)o : null;
		}

		/// <summary>Parse an array of values separated by delimiters given in 'delim'</summary>
		/// <param name="val">The string containing the array of floating point values</param>
		/// <param name="delim">The set of delimiters. If null, then " ", "\t", "," are used</param>
		/// <returns>An array of the parsed floating point values</returns>
		public static decimal[] ParseArray(string val, NumberStyles style = NumberStyles.Float, string[]? delim = null, StringSplitOptions opts = StringSplitOptions.RemoveEmptyEntries)
		{
			var strs = val.Split(delim ?? new[]{" ","\t",","}, opts);
			return strs.Select(s => decimal.Parse(s, style, null)).ToArray();
		}

		/// <summary>True if this value is in the range [beg,end] or [end,beg] (whichever is a positive range)</summary>
		public static bool Within(this decimal x, decimal beg, decimal end)
		{
			return beg <= end
				? x >= beg && x <= end
				: x >= end && x <= beg;
		}
		public static bool Within(this decimal? x, decimal beg, decimal end)
		{
			return x.HasValue && x.Value.Within(beg,end);
		}
		public static bool WithinInclusive(this decimal x, decimal beg, decimal end)
		{
			return x.Within(beg, end) || x == end;
		}
		public static bool WithinInclusive(this decimal? x, decimal beg, decimal end)
		{
			return x.HasValue && x.Value.WithinInclusive(beg,end);
		}

		/// <summary>Format the value with the indicated number of significant digits.</summary>
		public static string ToString(this decimal value, int significant_digits)
		{
			// Use G format to get significant digits.
			// Then convert to double and use F format.
			var format1 = "{0:G" + significant_digits.ToString() + "}";
			var result = Convert.ToDouble(string.Format(format1, value)).ToString("F99");

			// Remove trailing 0s.
			result = result.TrimEnd('0');

			// Remove the decimal point and leading 0s, leaving just the digits.
			var trimmed = result.Replace(".", "").TrimStart('0');

			// See if we have enough significant digits.
			if (significant_digits > trimmed.Length)
			{
				// Add trailing 0s.
				result += new string('0', significant_digits - trimmed.Length);
			}
			else
			{
				// See if we should remove the trailing decimal point.
				if (result.EndsWith(".") && significant_digits <= trimmed.Length)
					result = result.Substring(0, result.Length - 1);
			}

			return result;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture]
	public class TestTypeExtns
	{
		#pragma warning disable 169, 649
		private class ThingBase
		{
			private int B_PrivateField;
			protected int B_ProtectedField;
			internal int B_InternalField;
			public int B_PublicField;
			private int B_PrivateAutoProp { get; set; }
			protected int B_ProtectedAutoProp { get; set; }
			internal int B_InternalAutoProp { get; set; }
			public int B_PublicAutoProp { get; set; }
			private int B_PrivateMethod() { return 0; }
			protected int B_ProtectedMethod() { return 0; }
			internal int B_InternalMethod() { return 0; }
			public int B_PublicMethod() { return 0; }
			private static int B_PrivateStaticMethod() { return 0; }
			protected static int B_ProtectedStaticMethod() { return 0; }
			internal static int B_InternalStaticMethod() { return 0; }
			public static int B_PublicStaticMethod() { return 0; }
		}
		private class Thing : ThingBase
		{
			private int D_PrivateField;
			protected int D_ProtectedField;
			internal int D_InternalField;
			public int D_PublicField;
			private int D_PrivateAutoProp { get; set; }
			protected int D_ProtectedAutoProp { get; set; }
			internal int D_InternalAutoProp { get; set; }
			public int D_PublicAutoProp { get; set; }
			private int D_PrivateMethod() { return 0; }
			protected int D_ProtectedMethod() { return 0; }
			internal int D_InternalMethod() { return 0; }
			public int D_PublicMethod() { return 0; }
			private static int D_PrivateStaticMethod() { return 0; }
			protected static int D_ProtectedStaticMethod() { return 0; }
			internal static int D_InternalStaticMethod() { return 0; }
			public static int D_PublicStaticMethod() { return 0; }
		}
		private class NotSoAnonymousType { }
		#pragma warning restore 169, 649

		[Test]
		public void AllMembers()
		{
			// In base:
			//  4 - 4 - fields
			//  8 - 4 - auto properties
			// 12 - 4 - auto prop backing fields
			// 20 - 8 - get/set auto prop methods
			// 24 - 4 - instance methods
			// 28 - 4 - static methods
			// 29 - 1 - constructor
			// 58 - x2 - for the same again in derived
			var members = typeof(Thing).AllMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
			Assert.Equal(58, members.Count);

			members = typeof(Thing).AllMembers(BindingFlags.Instance | BindingFlags.Public).ToList();
			Assert.Equal(12, members.Count);
		}
		[Test]
		public void AllFields()
		{
			var fields = typeof(Thing).AllFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
			Assert.Equal(16, fields.Count);

			fields = typeof(Thing).AllFields(BindingFlags.Instance | BindingFlags.Public).ToList();
			Assert.Equal(2, fields.Count);
		}
		[Test]
		public void AllProps()
		{
			var props = typeof(Thing).AllProps(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
			Assert.Equal(8, props.Count);

			props = typeof(Thing).AllProps(BindingFlags.Instance | BindingFlags.Public).ToList();
			Assert.Equal(2, props.Count);
		}
		[Test]
		public void Resolve()
		{
			var ty0 = Type_.Resolve("System.String");
			Assert.Equal(typeof(string), ty0);

			var ty1 = Type_.Resolve("Rylogic.Extn.int_");
			Assert.Equal(typeof(Rylogic.Extn.int_), ty1);
		}
		[Test]
		public void IntExtn()
		{
			var x0 = int_.TryParse("1234");
			Assert.Equal(x0, 1234);

			var x1 = int_.TryParse("abc");
			Assert.Equal(x1, null);

			var x2 = int_.ParseArray("1  2,3\t\t\t4");
			Assert.True(x2.SequenceEqual(new[] { 1, 2, 3, 4 }));
		}
		[Test]
		public void AnonTypes()
		{
			var ty0 = new { One = "one" }.GetType();
			Assert.True(ty0.IsAnonymousType());

			var ty1 = typeof(NotSoAnonymousType);
			Assert.False(ty1.IsAnonymousType());
		}
		[Test]
		public void Decompose()
		{
			(-12.34).Decompose(out var man, out var exp, out var sign);
			var d = double_.Compose(man, exp, sign);
			Assert.Equal(-12.34, d);
		}
	}
}
#endif