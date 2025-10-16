//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Utility
{
	/// <summary>Utility function container</summary>
	public static class Util
	{
		/// <summary>Return the result of a function or 'def' if the function throws, swallowing the exception</summary>
		[DebuggerStepThrough]
		public static T Try<T>(Func<T> func, T def)
		{
			try
			{
				return func();
			}
			catch
			{
				return def;
			}
		}

		/// <summary>Dispose and return null for one-line disposing, e.g. thing = Util.Dispose(thing);</summary>
		[DebuggerStepThrough]
		public static T? Dispose<T>(ref T? doomed, bool gc_ok = false) where T : class, IDisposable
		{
			// Set 'doomed' to null before disposing to catch accidental
			// use of 'doomed' in a partially disposed state
			if (doomed == null) return null;
			BreakIf(!gc_ok && IsGCFinalizerThread, "Disposing in the GC finalizer thread");

			var junk = doomed;
			doomed = null;

			junk.Dispose();
			return null;
		}
		[DebuggerStepThrough]
		public static T? Dispose<T>(T? doomed) where T : class, IDisposable
		{
			return Dispose(ref doomed);
		}

		/// <summary>Dispose all items in the 'doomed' array, start at the last item. On return, the array will be full of nulls</summary>
		[DebuggerStepThrough]
		public static T?[]? DisposeAll<T>(T?[]? doomed) where T : class, IDisposable
		{
			if (doomed == null) return null;
			for (var i = doomed.Length; i-- != 0; )
				Dispose(ref doomed[i]);

			return null;
		}

		/// <summary>Disposes items in the 'doomed' list, started at the last item. On return, 'doomed' will be an empty list</summary>
		[DebuggerStepThrough]
		public static void DisposeAll<T>(IList<T?> doomed) where T : class, IDisposable
		{
			// Pop each item from the collection and dispose it
			if (doomed == null) return;
			for (var i = doomed.Count; i-- != 0;)
			{
				// We have to remove each item, because the list might be a binding list
				// that performs some action when items are removed.
				var item = doomed[i];
				doomed.RemoveAt(i);
				Dispose(ref item);
			}
		}

		/// <summary>Disposes values in the 'doomed' dictionary. On return, 'doomed' will be an empty dictionary</summary>
		[DebuggerStepThrough]
		public static void DisposeAll<K,V>(IDictionary<K,V>? doomed)
			where K : notnull
			where V : class, IDisposable
		{
			if (doomed == null) return;
			foreach (var kv in doomed)
				kv.Value?.Dispose();

			doomed.Clear();
		}

		/// <summary>Dispose a range of doomed items</summary>
		[DebuggerStepThrough]
		public static IEnumerable<T?> DisposeRange<T>(IEnumerable<T?>? doomed) where T : class, IDisposable
		{
			if (doomed == null) return null!;
			foreach (var d in doomed.Where(x => x != null).ToList())
				Dispose(d);

			return doomed;
		}

		/// <summary>Dispose a range within a collection</summary>
		[DebuggerStepThrough]
		public static void DisposeRange(IList doomed, int start, int count)
		{
			for (int i = start, iend = start + count; i != iend; ++i)
				doomed[i] = Dispose((IDisposable?)doomed[i]);
		}

		/// <summary>True if the current thread is the 'main' thread</summary>
		public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;
		private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;

		/// <summary>True if the current thread has the name 'GC Finalizer Thread'</summary>
		public static bool IsGCFinalizerThread => Thread.CurrentThread.ManagedThreadId == GCThread?.ManagedThreadId;
		public static Thread? GCThread
		{
			get
			{
				// I have no idea why this doesn't work the first time.
				for (var attempt = 0; attempt != 5 && GCThreadGrabber.m_gc_thread == null; ++attempt)
				{
					// This doesn't work if called from the GC thread.
					if (!IsMainThread)
						return null;

					// Create the thread grabber, then collect it
					var grabber = new GCThreadGrabber();
					grabber = null!;
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}
				return GCThreadGrabber.m_gc_thread;
			}
		}

		/// <summary>A helper class that gets created then collected so we can grab details about the GC thread</summary>
		private class GCThreadGrabber
		{
			public static Thread? m_gc_thread = null;
			~GCThreadGrabber() { m_gc_thread = Thread.CurrentThread; }
		}

		/// <summary>Replacement for Debug.Assert that doesn't run off into infinite loops trying to display a dialog</summary>
		[Conditional("DEBUG")] public static void Assert(bool condition, string? msg = null)
		{
			if (condition || m_assert_visible) return;
			using (Scope.Create(() => m_assert_visible = true, v => m_assert_visible = v))
			{
				if (msg == null)
					Debug.Assert(condition);
				else
					Debug.Assert(condition, msg);
			}
		}
		private static bool m_assert_visible;

		/// <summary>Stop in the debugger if 'condition' is true. For when assert dialogs cause problems with threading</summary>
		[Conditional("DEBUG")] public static void BreakIf(bool condition, string? msg = null)
		{
			if (!condition || !Debugger.IsAttached) return;
			if (msg != null) Debug.WriteLine(msg);
			Debugger.Break();
		}

		/// <summary>Asserts or stops the debugger if the caller is not the main thread</summary>
		[Conditional("DEBUG")] public static void AssertMainThread()
		{
			// Don't put the test in the Assert because assert doesn't stop immediately, it pumps the thread
			// queue in order to display the dialog. Having the 'if' like this allows a breakpoint to be put
			// on the Debug.Assert() statement.
			if (!IsMainThread)
			{
				if (Debugger.IsAttached) Debugger.Break(); // If debugging, stop all threads immediately
				Debug.Assert(false, "Not the main thread");
			}
		}

		/// <summary>Blocks until the debugger is attached</summary>
		public static void WaitForDebugger(Func<string, bool>? wait_cb = null)
		{
			// Note: Use WPFUtil.WaitForDebugger() for WPF apps
			if (DebuggerAttached()) return;
			var t = new Thread(new ThreadStart(() =>
			{
				try
				{
					var proc = Process.GetCurrentProcess();
					var info =
						$"Machine Name: {proc.MachineName}\n" +
						$"Process Name: {proc.ProcessName}\n" +
						$"Process ID: {proc.Id}\n" +
						"Waiting for Debugger...";

					// Default wait function
					wait_cb ??= (_ =>
					{
						Thread.Sleep(10);
						return true;
					});

					for (; !DebuggerAttached() && wait_cb(info);)
					{ }
				}
				catch { }
			}));
			if (Platform.IsWindows())
				t.SetApartmentState(ApartmentState.STA); // So 'wait_cb' can display a MsgBox

			t.Start();
			t.Join();

			// Helper method for debugging (return false)
			static bool DebuggerAttached() => Debugger.IsAttached;
		}

		/// <summary>__FILE__</summary>
		public static string ThisFile([CallerFilePath] string caller_filepath = "")
		{
			return caller_filepath;
		}
		public static string ThisDirectory([CallerFilePath] string caller_filepath = "")
		{
			return Path.GetDirectoryName(caller_filepath) ?? string.Empty;
		}

		/// <summary>__LINE__</summary>
		public static int ThisLine([CallerLineNumber] int caller_line_number = 0)
		{
			return caller_line_number;
		}

		/// <summary>Output a VS output window style "file(line)"</summary>
		public static string FileAndLine([CallerFilePath] string caller_filepath = "", [CallerLineNumber] int caller_line_number = 0)
		{
			return $"{caller_filepath}({caller_line_number})";
		}

		// Returns the number to add to pad 'size' up to 'alignment'
		public static long Pad(long size, long alignment)
		{
			return (alignment - (size % alignment)) % alignment;
		}

		// Returns 'size' increased to a multiple of 'alignment'
		public static long PadTo(long size, long alignment)
		{
			return size + Pad(size, alignment);
		}

		/// <summary>Swap two values</summary>
		public static void Swap<T>(ref T lhs, ref T rhs)
		{
			(rhs, lhs) = (lhs, rhs);
		}

		/// <summary>Convert a collection of 'U' into a collection of 'T' using 'conv' to do the conversion</summary>
		public static IEnumerable<T> Conv<T,U>(IEnumerable<U> collection, Func<U, T> conv)
		{
			foreach (var i in collection) yield return conv(i);
		}

		/// <summary>Attempts to robustly convert 'value' into type 'result_type' using reflection and a load of special cases</summary>
		public static object? ConvertTo(object? value, Type result_type, bool ignore_case = false)
		{
			var is_nullable = Nullable.GetUnderlyingType(result_type) != null;
			var root_type   = Nullable.GetUnderlyingType(result_type) ?? result_type;

			// 'value' is null? If T is not a value type return default(T)
			if (value == null)
			{
				// Note: IsClass != !IsValueType. Nullables are still value types
				if (!result_type.IsClass && !is_nullable)
					throw new ArgumentNullException($"Cannot convert null to type {result_type}");
				return null;
			}

			var value_type = value.GetType();

			// 'value' is already the desired type
			if (result_type.IsAssignableFrom(value_type))
				return value;

			// Convert from string or integral to enum
			if (root_type.IsEnum)
			{
				// Parse string
				if (value is string v)
					return Enum.Parse(root_type, v, ignore_case);

				// Convert from integral type
				var enum_ty = Enum.GetUnderlyingType(root_type);
				value = value_type != enum_ty ? Convert.ChangeType(value, enum_ty) : value;
				if (Enum.IsDefined(result_type, value))
					return Enum.ToObject(root_type, value);
				
				throw new Exception($"Cannot convert {value}(of type {value_type.Name}) to type {result_type.Name}");
			}

			// Convert to guid
			if (root_type == typeof(Guid))
			{
				if (value is string v) value = new Guid(v);
				if (value is byte[] v1) value = new Guid(v1);
				return Convert.ChangeType(value, root_type);
			}

			// System.Convert only works with IConvertible
			if (value is IConvertible)
				return Convert.ChangeType(value, root_type);

			throw new Exception($"Cannot convert from:\n" +
				$"  {value_type.FullName}\n" +
				$"  (Assembly: {value_type.Assembly.FullName})\n" +
				$"  (Location: {value_type.Assembly.Location})\n" +
				$"to \n" +
				$"  {result_type.FullName}\n" +
				$"  (Assembly: {result_type.Assembly.FullName})\n" +
				$"  (Location: {result_type.Assembly.Location})\n");
		}
		
		/// <summary>Attempts to robustly convert 'value' into type 'T' using reflection and a load of special cases</summary>
		public static T ConvertTo<T>(object? value, bool ignore_case = false)
		{
			if (value is T t) return t;
			var obj = ConvertTo(value, typeof(T), ignore_case);
			return obj is T t2 ? t2 : default!;
		}

		/// <summary>Helper for allocating a dictionary preloaded with data</summary>
		public static Dictionary<TKey,TValue> NewDict<TKey,TValue>(int count, Func<int,TKey> key, Func<int,TValue> value)
			where TKey : notnull
		{
			var dick = new Dictionary<TKey,TValue>();
			for (var i = 0; i != count; ++i) dick.Add(key(i), value(i));
			return dick;
		}

		/// <summary>Returns the number to add to pad 'size' up to 'alignment'</summary>
		public static long Pad(long size, int alignment)
		{
			return (alignment - (size % alignment)) % alignment;
		}
		public static int Pad(int size, int alignment)
		{
			return (int)Pad((long)size, alignment);
		}

		/// <summary>Returns 'size' rounded up to a multiple of 'alignment'</summary>
		public static long PadTo(long size, int alignment)
		{
			return size + Pad(size, alignment);
		}
		public static int PadTo(int size, int alignment)
		{
			return (int)PadTo((long)size, alignment);
		}

		/// <summary>Helper for returning the number of fields in an enum</summary>
		public static int Count(Type enum_type)
		{
			Debug.Assert(enum_type.IsEnum);
			return Enum.GetNames(enum_type).Length;
		}

		/// <summary>Return the next value in an enum, rolling over at the last value. Must be used only on Enums with range [0, count)</summary>
		public static int Next<T>(T zoom_type)
		{
			var v = System.Convert.ToInt32(zoom_type);
			return (v + 1) % Count(typeof(T));
		}

		/// <summary>Convert a structure into an array of bytes. Remember to use the MarshalAs attribute for structure fields that need it</summary>
		public static byte[] ToBytes(object structure)
		{
			var size = Marshal.SizeOf(structure);
			var arr = new byte[size];
			using var ptr = Marshal_.Alloc(EHeap.HGlobal, size);
			Marshal.StructureToPtr(structure, ptr.Value.Ptr, true);
			Marshal.Copy(ptr.Value.Ptr, arr, 0, size);
			return arr;
		}

		/// <summary>Create an array of bytes with the same size as 'T'</summary>
		public static byte[] CreateByteArrayOfSize<T>()
		{
			return new byte[Marshal.SizeOf(typeof(T))];
		}

		/// <summary>Convert a sub range of bytes to a structure</summary>
		public static T FromBytes<T>(byte[] arr, int offset)
		{
			Debug.Assert(arr.Length - offset >= Marshal.SizeOf(typeof(T)), "FromBytes<T>: Insufficient data");
			var sz = Marshal.SizeOf(typeof(T));
			using var ptr = Marshal_.Alloc(EHeap.HGlobal, sz);
			Marshal.Copy(arr, offset, ptr.Value.Ptr, sz);
			var strukt = Marshal.PtrToStructure<T>(ptr.Value.Ptr) ?? throw new Exception("FromBytes<T>: Failed to convert pointer");
			return strukt;
		}

		/// <summary>Convert an array of bytes to a structure</summary>
		public static T FromBytes<T>(byte[] arr)
		{
			Debug.Assert(arr.Length >= Marshal.SizeOf(typeof(T)), "FromBytes<T>: Insufficient data");
			using var handle = GCHandle_.Alloc(arr, GCHandleType.Pinned);
			var strukt = Marshal.PtrToStructure<T>(handle.Handle.AddrOfPinnedObject()) ?? throw new Exception("FromBytes<T>: Failed to convert pointer");
			return strukt;
		}

		/// <summary>Simple binary serialise</summary>
		public static byte[] Serialise(params object[] args)
		{
			using var ms = new MemoryStream();
			Serialise(ms, args);
			ms.Seek(0, SeekOrigin.Begin);
			return ms.ToArray();
		}
		public static void Serialise(MemoryStream ms, params object[] args)
		{
			// Using BinaryReader to deserialise
			using var bw = new BinaryWriter(ms, Encoding.UTF8, true);
			foreach (var arg in args)
				Write(arg);

			void Write(object value)
			{
				switch (value)
				{
				default: throw new Exception($"Serialise: Unsupported type: {value.GetType().Name}");
				case bool x: bw.Write(x); break;
				case byte x: bw.Write(x); break;
				case sbyte x: bw.Write(x); break;
				case byte[] x: bw.Write(x); break;
				case char x: bw.Write(x); break;
				case char[] x: bw.Write(x); break;
				case short x: bw.Write(x); break;
				case ushort x: bw.Write(x); break;
				case int x: bw.Write(x); break;
				case uint x: bw.Write(x); break;
				case long x: bw.Write(x); break;
				case ulong x: bw.Write(x); break;
				case float x: bw.Write(x); break;
				case double x: bw.Write(x); break;
				case decimal x: bw.Write(x); break;
				case string x: bw.Write(x); break;
				case v2 x: bw.Write(x.x); bw.Write(x.y); break;
				case v3 x: bw.Write(x.x); bw.Write(x.y); bw.Write(x.z); break;
				case v4 x: bw.Write(x.x); bw.Write(x.y); bw.Write(x.z); bw.Write(x.w); break;
				case m4x4 x: Write(x.x); Write(x.y); Write(x.z); Write(x.w); break;
				case BBox x: Write(x.Centre); Write(x.Radius); break;
				}
			}
		}

		/// <summary>Returns true if 'x' is a valid hexadecimal character</summary>
		public static bool IsHexChar(char x)
		{
			return (x >= '0' && x <= '9') || (x >= 'a' && x <= 'f') || (x >= 'A' && x <= 'F');			
		}

		/// <summary>Convert a byte array to a hex string. e.g A3 FF 12 4D etc</summary>
		public static string ToHexString(byte[] arr, int start = 0, int count = int.MaxValue, string sep = " ", string line_sep = "\n", int width = 16)
		{
			count = Math.Min(arr.Length - start, count);
			if (!sep.HasValue() && !line_sep.HasValue())
			{
				var sb = new StringBuilder { Length = 2 * count };
				for (int i = 0, j = 0; i != count; ++i)
				{
					var b = arr[start + i];
					var h = (b >> 4) & 0xF;
					var l = (b >> 0) & 0xF;
					sb[j++] = (char)(h + (h >= 0xA ? 'A' - 0xA : '0'));
					sb[j++] = (char)(l + (l >= 0xA ? 'A' - 0xA : '0'));
				}
				return sb.ToString();
			}
			else
			{
				var sb = new StringBuilder();
				for (var i = 0; i != count; ++i)
				{
					if (sb.Length != 0)
						sb.Append((i % width) != 0 ? sep : line_sep);

					var b = arr[start + i];
					var h = (b >> 4) & 0xF;
					var l = (b >> 0) & 0xF;
					sb.Append((char)(h + (h >= 0xA ? 'A' - 0xA : '0')));
					sb.Append((char)(l + (l >= 0xA ? 'A' - 0xA : '0')));
				}
				return sb.ToString();
			}
		}

		/// <summary>Convert a hex string to a byte array</summary>
		public static byte[] FromHexString(string hex, bool sanitise = true)
		{
			var str = sanitise ? hex.Strip(x => !IsHexChar(x)) : hex;
			if ((str.Length & 1) != 0)
				throw new ArgumentException("Hex string must have each byte represented by 2 characters","hex");

			// A more efficient implementation than calling SubString and byte.Parse
			var arr = new byte[str.Length/2];
			for (int i = 0, j = 0; i != arr.Length; ++i)
			{
				var h = str[j++];
				var l = str[j++];
				arr[i] = (byte)(
					(h - (h >= 'a' ? 'a' - 0xA : h >= 'A' ? 'A' - 0xA : '0') << 4) |
					(l - (l >= 'a' ? 'a' - 0xA : l >= 'A' ? 'A' - 0xA : '0') << 0) );
			}
			return arr;
		}

		/// <summary>Add 'item' to a history list of items.</summary>
		public static void AddToHistoryList<T>(IList<T> history, T item, int max_history_length, int index = 0, Func<T, T, bool>? cmp = null)
		{
			if (Equals(item, null))
				throw new NullReferenceException("Null cannot be added to a history list");
			if (history.IsReadOnly)
				throw new ArgumentException("'history' must be an editable list");

			// Remove any existing occurrences of 'item'
			cmp ??= ((l, r) => Equals(l, r));
			history.RemoveIf(i => cmp(i, item));

			// Insert 'item' into the history
			index = Math_.Min(index, max_history_length - 1, history.Count);
			history.Insert(index, item);

			// Ensure the history doesn't grow too long
			history.RemoveToEnd(max_history_length);
		}
		public static void AddToHistoryList<T>(IList<T> history, T item, bool ignore_case, int max_history_length, int index = 0, Func<T, T, bool>? cmp = null)
		{
			// Note: this is the 'ignore case' overload, which implies it's a string compare by default
			var flags = ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			cmp ??= (left, right) => string.Compare(left?.ToString() ?? string.Empty, right?.ToString() ?? string.Empty, flags) == 0;
			AddToHistoryList(history, item, max_history_length, index, cmp);
		}

		/// <summary>Add 'item' to a history list of items.</summary>
		public static T[] AddToHistoryList<T>(T[] history, T item, int max_history_length, int index = 0, Func<T,T,bool>? cmp = null)
		{
			// Convert the history to a list
			var list = history?.ToList() ?? [];

			// Add 'item' to the list
			AddToHistoryList(list, item, max_history_length, index, cmp);

			// Return the history as an array
			return list.ToArray();
		}
		public static T[] AddToHistoryList<T>(T[] history, T item, bool ignore_case, int max_history_length, int index = 0)
		{
			var flags = ignore_case ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			bool Cmp(T left, T right) => string.Compare(left?.ToString() ?? string.Empty, right?.ToString() ?? string.Empty, flags) == 0;
			return AddToHistoryList(history, item, max_history_length, index, Cmp);
		}

		/// <summary>Return an assembly attribute</summary>
		public static T GetAssemblyAttribute<T>(Assembly? ass = null)
		{
			if (ass == null) ass = Assembly.GetEntryAssembly();
			if (ass == null) ass = Assembly.GetExecutingAssembly();
			var attr = ass.GetCustomAttributes(typeof(T), false);
			if (attr.Length == 0) throw new ApplicationException("Assembly does not have attribute "+typeof(T).Name);
			return (T)(attr[0]);
		}

		/// <summary>Read the framework type of an assembly</summary>
		public static string GetTargetFramework(string assemblyPath)
		{
			static string? GetAttributeTypeName(MetadataReader reader, EntityHandle ctorHandle)
			{
				if (ctorHandle.Kind == HandleKind.MemberReference)
				{
					var memberRef = reader.GetMemberReference((MemberReferenceHandle)ctorHandle);
					var parent = memberRef.Parent;
					if (parent.Kind == HandleKind.TypeReference)
					{
						var typeRef = reader.GetTypeReference((TypeReferenceHandle)parent);
						return reader.GetString(typeRef.Namespace) + "." + reader.GetString(typeRef.Name);
					}
				}
				return null;
			}
			static string? GetTargetFrameworkValue(MetadataReader reader, CustomAttribute attribute)
			{
				var valueBlob = reader.GetBlobReader(attribute.Value);
				// Attribute value blob format: Prolog (2 bytes), followed by constructor args
				valueBlob.ReadUInt16(); // skip prolog
				return valueBlob.ReadSerializedString();
			}

			using var stream = File.OpenRead(assemblyPath);
			using var peReader = new PEReader(stream);
			var mdReader = peReader.GetMetadataReader();
			foreach (var handle in mdReader.GetCustomAttributes(EntityHandle.AssemblyDefinition))
			{
				var attr = mdReader.GetCustomAttribute(handle);
				var ctorHandle = attr.Constructor;
				var attrType = GetAttributeTypeName(mdReader, ctorHandle);

				if (attrType != typeof(TargetFrameworkAttribute).FullName)
					continue;

				if (GetTargetFrameworkValue(mdReader, attr) is not string value)
					continue;

				return value;
			}
			return "Unknown";
		}

		/// <summary>Return the main assembly for the currently running application</summary>
		public static Assembly MainAssembly => (Assembly?)null ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
		public static string MainAssemblyName => MainAssembly?.GetName().Name ?? string.Empty;

		/// <summary>Return the version number for an assembly</summary>
		public static Version AssemblyVersion(Assembly? ass = null)
		{
			ass ??= MainAssembly;
			return ass?.GetName().Version ?? new Version();
		}
		public static Version AssemblyVersion(Type type)
		{
			return AssemblyVersion(Assembly.GetAssembly(type));
		}
		public static string AppVersion => AssemblyVersion().ToString(3);

		/// <summary>Returns the time stamp of an assembly. Use 'Assembly.GetCallingAssembly()'</summary>
		public static DateTime AssemblyTimestamp(Assembly? ass = null)
		{
			const int PeHeaderOffset = 60;
			const int LinkerTimestampOffset = 8;

			var b = new byte[2048];
			ass ??= MainAssembly;
			using (var s = new FileStream(ass.Location, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, 2048);

			var ts = DateTime_.UnixEpoch;
			ts = ts.AddSeconds(BitConverter.ToInt32(b, BitConverter.ToInt32(b, PeHeaderOffset) + LinkerTimestampOffset));
			ts = ts.AddHours(TimeZoneInfo.Local.GetUtcOffset(ts).Hours);
			return ts;
		}
		public static DateTime AssemblyTimestamp(Type type)
		{
			return AssemblyTimestamp(Assembly.GetAssembly(type));
		}
		public static DateTime AppTimestamp => AssemblyTimestamp();

		/// <summary>Return the copyright string from an assembly</summary>
		public static string AssemblyCopyright(Assembly? ass = null)
		{
			ass ??= MainAssembly;
			var attr = ass.CustomAttributes.First(x => x.AttributeType == typeof(AssemblyCopyrightAttribute));
			return attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
		}
		public static string AssemblyCopyright(Type type)
		{
			return AssemblyCopyright(Assembly.GetAssembly(type));
		}
		public static string AppCopyright
		{
			get { return AssemblyCopyright(); }
		}

		/// <summary>Return the company string from an assembly</summary>
		public static string AssemblyCompany(Assembly? ass = null)
		{
			ass ??= MainAssembly;
			var attr = ass.CustomAttributes.First(x => x.AttributeType == typeof(AssemblyCompanyAttribute));
			return attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
		}
		public static string AssemblyCompany(Type type)
		{
			return AssemblyCompany(Assembly.GetAssembly(type));
		}
		public static string AppCompany
		{
			get { return AssemblyCompany(); }
		}

		/// <summary>Return the product name string from an assembly</summary>
		public static string AssemblyProductName(Assembly? ass = null)
		{
			ass ??= MainAssembly;
			var attr = ass.CustomAttributes.First(x => x.AttributeType == typeof(AssemblyProductAttribute));
			return attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
		}
		public static string AssemblyProductName(Type type)
		{
			return AssemblyProductName(Assembly.GetAssembly(type));
		}
		public static string AppProductName
		{
			get { return AssemblyProductName(); }
		}

		/// <summary>The full path of the executable</summary>
		public static string ExecutablePath
		{
			get { return MainAssembly.Location; }
		}

		/// <summary>The directory that this application executable is in</summary>
		public static string AppDirectory
		{
			get { return Path_.Directory(ExecutablePath); }
		}

		/// <summary>Read a text file embedded resource returning it as a string</summary>
		public static string TextResource(string resource_name, Assembly ass)
		{
			ass ??= Assembly.GetExecutingAssembly(); // this will look in Rylogic.Main.dll for resources.. probably not what's wanted
			var stream = ass.GetManifestResourceStream(resource_name) ?? throw new IOException("No resource with name "+resource_name+" found");
			using var src = new StreamReader(stream);
			return src.ReadToEnd();
		}

		/// <summary>Returns the full path to a file or directory relative to the app executable</summary>
		public static string ResolveAppPath(params string[] paths)
		{
			return ResolveAppPath((IEnumerable<string>)paths);
		}
		public static string ResolveAppPath(IEnumerable<string> paths)
		{
			return Path_.CombinePath(paths.Prepend(AppDirectory));
		}

		/// <summary>The application directory for a roaming user</summary>
		public static string ResolveAppDataPath(params string[] paths)
		{
			return ResolveAppDataPath((IEnumerable<string>)paths);
		}
		public static string ResolveAppDataPath(IEnumerable<string> paths)
		{
			var app_data_dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			return Path_.CombinePath(paths.Prepend(app_data_dir));
		}

		/// <summary>The temp directory</summary>
		public static string ResolveTempPath(params string[] paths)
		{
			return ResolveTempPath((IEnumerable<string>)paths);
		}
		public static string ResolveTempPath(IEnumerable<string> paths)
		{
			var temp_dir = Path.GetTempPath();
			return Path_.CombinePath(paths.Prepend(temp_dir));
		}

		/// <summary>The user's 'home' directory</summary>
		public static string ResolveUserProfilePath(params string[] paths)
		{
			return ResolveUserProfilePath((IEnumerable<string>)paths);
		}
		public static string ResolveUserProfilePath(IEnumerable<string> paths)
		{
			var home_dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path_.CombinePath(paths.Prepend(home_dir));
		}

		/// <summary>The user's 'Documents' directory</summary>
		public static string ResolveUserDocumentsPath(params string[] paths)
		{
			return ResolveUserDocumentsPath((IEnumerable<string>)paths);
		}
		public static string ResolveUserDocumentsPath(IEnumerable<string> paths)
		{
			var my_documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			return Path_.CombinePath(paths.Prepend(my_documents));
		}

		/// <summary>The ProgramFiles directory</summary>
		public static string ResolveProgramFilesPath(params string[] paths)
		{
			return ResolveProgramFilesPath((IEnumerable<string>)paths);
		}
		public static string ResolveProgramFilesPath(IEnumerable<string> paths)
		{
			var program_files_dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			var path = Path_.CombinePath(paths.Prepend(program_files_dir));
			return path;
		}

		/// <summary>The ProgramFiles directory</summary>
		public static string ResolveProgramFilesX86Path(params string[] paths)
		{
			return ResolveProgramFilesX86Path((IEnumerable<string>)paths);
		}
		public static string ResolveProgramFilesX86Path(IEnumerable<string> paths)
		{
			var program_files_dir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			var path = Path_.CombinePath(paths.Prepend(program_files_dir));
			return path;
		}

		/// <summary>The ProgramData directory</summary>
		public static string ResolveProgramDataPath(params string[] paths)
		{
			return ResolveProgramDataPath((IEnumerable<string>)paths);
		}
		public static string ResolveProgramDataPath(IEnumerable<string> paths)
		{
			var program_data_dir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			return Path_.CombinePath(paths.Prepend(program_data_dir));
		}

		/// <summary>
		/// Event handler used to load a dll from an embedded resource.<para/>
		/// Use:<para/>
		///  Add dependent dlls to the project and select "Embedded Resource" in their properties<para/>
		///  AppDomain.CurrentDomain.AssemblyResolve += (s,a) => Util.ResolveEmbeddedAssembly(a.Name);</summary>
		public static Assembly ResolveEmbeddedAssembly(string assembly_name)
		{
			var resource_name = "AssemblyLoadingAndReflection." + new AssemblyName(assembly_name).Name + ".dll";
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource_name);
			if (stream == null) throw new Exception("Embedded assembly not found");
			var assembly_data = new Byte[stream.Length];
			stream.Read(assembly_data, 0, assembly_data.Length);
			return Assembly.Load(assembly_data);
		}

		/// <summary>Returns true if 'point' is more than 'dx' or 'dy' from 'ref_point'</summary>
		public static bool Moved(Point point, Point ref_point, int dx, int dy)
		{
			return Math.Abs(point.X - ref_point.X) > dx || Math.Abs(point.Y - ref_point.Y) > dy;
		}

		/// <summary>A try-lock that remembers to call Monitor.Exit()</summary>
		public static bool TryLock(object obj, int millisecondsTimeout, Action action)
		{
			if (!Monitor.TryEnter(obj, millisecondsTimeout)) return false;
			try { action(); } finally { Monitor.Exit(obj); }
			return true;
		}

		///// <summary>Execute a function in a background thread and terminate it if it doesn't finished before 'timeout'</summary>
		//public static TResult RunWithTimeout<TResult>(Func<TResult> proc, int duration)
		//{
		//	var r = default(TResult)!;
		//	var ex = (Exception?)null;
		//	using (var reset = new AutoResetEvent(false))
		//	{
		//		// Can't use the thread pool for this because we abort the thread
		//		var thread = new Thread(() =>
		//		{
		//			try { r = proc(); }
		//			catch (Exception e) { ex = e; }
		//			reset.Set();
		//		});
		//		thread.Start();
		//		if (!reset.WaitOne(duration))
		//		{
		//			thread.Abort();
		//			throw new TimeoutException();
		//		}
		//	}
		//	if (ex != null)
		//		throw ex;
		//	return r;
		//}

		/// <summary>Helper method for converting a filename extension to a mime type</summary>
		public static string MimeFromExtn(string extn)
		{
			return m_mime_type_map.TryGetValue(extn.TrimStart('.'), out var mime) ? mime : "unknown/unknown";
		}
		#region Mime type dictionary
		private static readonly Dictionary<string, string> m_mime_type_map = new()
		{
			{"ai", "application/postscript"},
			{"aif", "audio/x-aiff"},
			{"aifc", "audio/x-aiff"},
			{"aiff", "audio/x-aiff"},
			{"asc", "text/plain"},
			{"atom", "application/atom+xml"},
			{"au", "audio/basic"},
			{"avi", "video/x-msvideo"},
			{"bcpio", "application/x-bcpio"},
			{"bin", "application/octet-stream"},
			{"bmp", "image/bmp"},
			{"cdf", "application/x-netcdf"},
			{"cgm", "image/cgm"},
			{"class", "application/octet-stream"},
			{"cpio", "application/x-cpio"},
			{"cpt", "application/mac-compactpro"},
			{"csh", "application/x-csh"},
			{"css", "text/css"},
			{"dcr", "application/x-director"},
			{"dif", "video/x-dv"},
			{"dir", "application/x-director"},
			{"djv", "image/vnd.djvu"},
			{"djvu", "image/vnd.djvu"},
			{"dll", "application/octet-stream"},
			{"dmg", "application/octet-stream"},
			{"dms", "application/octet-stream"},
			{"doc", "application/msword"},
			{"docx","application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
			{"dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
			{"docm","application/vnd.ms-word.document.macroEnabled.12"},
			{"dotm","application/vnd.ms-word.template.macroEnabled.12"},
			{"dtd", "application/xml-dtd"},
			{"dv", "video/x-dv"},
			{"dvi", "application/x-dvi"},
			{"dxr", "application/x-director"},
			{"eps", "application/postscript"},
			{"etx", "text/x-setext"},
			{"exe", "application/octet-stream"},
			{"ez", "application/andrew-inset"},
			{"gif", "image/gif"},
			{"gram", "application/srgs"},
			{"grxml", "application/srgs+xml"},
			{"gtar", "application/x-gtar"},
			{"hdf", "application/x-hdf"},
			{"hqx", "application/mac-binhex40"},
			{"htm", "text/html"},
			{"html", "text/html"},
			{"ice", "x-conference/x-cooltalk"},
			{"ico", "image/x-icon"},
			{"ics", "text/calendar"},
			{"ief", "image/ief"},
			{"ifb", "text/calendar"},
			{"iges", "model/iges"},
			{"igs", "model/iges"},
			{"jnlp", "application/x-java-jnlp-file"},
			{"jp2", "image/jp2"},
			{"jpe", "image/jpeg"},
			{"jpeg", "image/jpeg"},
			{"jpg", "image/jpeg"},
			{"js", "application/x-javascript"},
			{"kar", "audio/midi"},
			{"latex", "application/x-latex"},
			{"lha", "application/octet-stream"},
			{"lzh", "application/octet-stream"},
			{"m3u", "audio/x-mpegurl"},
			{"m4a", "audio/mp4a-latm"},
			{"m4b", "audio/mp4a-latm"},
			{"m4p", "audio/mp4a-latm"},
			{"m4u", "video/vnd.mpegurl"},
			{"m4v", "video/x-m4v"},
			{"mac", "image/x-macpaint"},
			{"man", "application/x-troff-man"},
			{"mathml", "application/mathml+xml"},
			{"me", "application/x-troff-me"},
			{"mesh", "model/mesh"},
			{"mid", "audio/midi"},
			{"midi", "audio/midi"},
			{"mif", "application/vnd.mif"},
			{"mov", "video/quicktime"},
			{"movie", "video/x-sgi-movie"},
			{"mp2", "audio/mpeg"},
			{"mp3", "audio/mpeg"},
			{"mp4", "video/mp4"},
			{"mpe", "video/mpeg"},
			{"mpeg", "video/mpeg"},
			{"mpg", "video/mpeg"},
			{"mpga", "audio/mpeg"},
			{"ms", "application/x-troff-ms"},
			{"msh", "model/mesh"},
			{"mxu", "video/vnd.mpegurl"},
			{"nc", "application/x-netcdf"},
			{"oda", "application/oda"},
			{"ogg", "application/ogg"},
			{"pbm", "image/x-portable-bitmap"},
			{"pct", "image/pict"},
			{"pdb", "chemical/x-pdb"},
			{"pdf", "application/pdf"},
			{"pgm", "image/x-portable-graymap"},
			{"pgn", "application/x-chess-pgn"},
			{"pic", "image/pict"},
			{"pict", "image/pict"},
			{"png", "image/png"},
			{"pnm", "image/x-portable-anymap"},
			{"pnt", "image/x-macpaint"},
			{"pntg", "image/x-macpaint"},
			{"ppm", "image/x-portable-pixmap"},
			{"ppt", "application/vnd.ms-powerpoint"},
			{"pptx","application/vnd.openxmlformats-officedocument.presentationml.presentation"},
			{"potx","application/vnd.openxmlformats-officedocument.presentationml.template"},
			{"ppsx","application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
			{"ppam","application/vnd.ms-powerpoint.addin.macroEnabled.12"},
			{"pptm","application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
			{"potm","application/vnd.ms-powerpoint.template.macroEnabled.12"},
			{"ppsm","application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
			{"ps", "application/postscript"},
			{"qt", "video/quicktime"},
			{"qti", "image/x-quicktime"},
			{"qtif", "image/x-quicktime"},
			{"ra", "audio/x-pn-realaudio"},
			{"ram", "audio/x-pn-realaudio"},
			{"ras", "image/x-cmu-raster"},
			{"rdf", "application/rdf+xml"},
			{"rgb", "image/x-rgb"},
			{"rm", "application/vnd.rn-realmedia"},
			{"roff", "application/x-troff"},
			{"rtf", "text/rtf"},
			{"rtx", "text/richtext"},
			{"sgm", "text/sgml"},
			{"sgml", "text/sgml"},
			{"sh", "application/x-sh"},
			{"shar", "application/x-shar"},
			{"silo", "model/mesh"},
			{"sit", "application/x-stuffit"},
			{"skd", "application/x-koan"},
			{"skm", "application/x-koan"},
			{"skp", "application/x-koan"},
			{"skt", "application/x-koan"},
			{"smi", "application/smil"},
			{"smil", "application/smil"},
			{"snd", "audio/basic"},
			{"so", "application/octet-stream"},
			{"spl", "application/x-futuresplash"},
			{"src", "application/x-wais-source"},
			{"sv4cpio", "application/x-sv4cpio"},
			{"sv4crc", "application/x-sv4crc"},
			{"svg", "image/svg+xml"},
			{"swf", "application/x-shockwave-flash"},
			{"t", "application/x-troff"},
			{"tar", "application/x-tar"},
			{"tcl", "application/x-tcl"},
			{"tex", "application/x-tex"},
			{"texi", "application/x-texinfo"},
			{"texinfo", "application/x-texinfo"},
			{"tif", "image/tiff"},
			{"tiff", "image/tiff"},
			{"tr", "application/x-troff"},
			{"tsv", "text/tab-separated-values"},
			{"txt", "text/plain"},
			{"ustar", "application/x-ustar"},
			{"vcd", "application/x-cdlink"},
			{"vrml", "model/vrml"},
			{"vxml", "application/voicexml+xml"},
			{"wav", "audio/x-wav"},
			{"wbmp", "image/vnd.wap.wbmp"},
			{"wbmxl", "application/vnd.wap.wbxml"},
			{"wml", "text/vnd.wap.wml"},
			{"wmlc", "application/vnd.wap.wmlc"},
			{"wmls", "text/vnd.wap.wmlscript"},
			{"wmlsc", "application/vnd.wap.wmlscriptc"},
			{"wrl", "model/vrml"},
			{"xbm", "image/x-xbitmap"},
			{"xht", "application/xhtml+xml"},
			{"xhtml", "application/xhtml+xml"},
			{"xls", "application/vnd.ms-excel"},
			{"xml", "application/xml"},
			{"xpm", "image/x-xpixmap"},
			{"xsl", "application/xml"},
			{"xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
			{"xltx","application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
			{"xlsm","application/vnd.ms-excel.sheet.macroEnabled.12"},
			{"xltm","application/vnd.ms-excel.template.macroEnabled.12"},
			{"xlam","application/vnd.ms-excel.addin.macroEnabled.12"},
			{"xlsb","application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
			{"xslt", "application/xslt+xml"},
			{"xul", "application/vnd.mozilla.xul+xml"},
			{"xwd", "image/x-xwindowdump"},
			{"xyz", "chemical/x-xyz"},
			{"zip", "application/zip"}
		};
		#endregion

		/// <summary>A stack trace that selected a band of stack frames</summary>
		public static string StackTrace(int skip, int count)
		{
			var sb = new StringBuilder();
			var st = new StackTrace(1,true).GetFrames();
			if (st == null)
				return string.Empty;

			foreach (var s in st.Skip(skip).Take(count))
			{
				if (s == null) continue;
				var m = s.GetMethod();
				sb.AppendFormat(" {0,-60}({1,3})  {2}.{3}\n", s.GetFileName(), s.GetFileLineNumber(), m?.ReflectedType?.FullName ?? "<unknown>", m?.Name ?? "<unknown>");
			}
			return sb.ToString();
		}

		/// <summary>Format a multi-line string that probably contains file/line info so that it becomes click-able in the debugger output window</summary>
		public static string FormatForOutputWindow(string text, string? pattern = null)
		{
			pattern ??= Regex_.FullPathPattern;

			var sb = new StringBuilder();
			var lines = text.Split('\n');
			foreach (var line in lines.Select(x => x.TrimEnd('\r')))
			{
				var m = Regex.Match(line, pattern);
				if (m.Success)
				{
					var cap0 = m.Groups[0];
					var path = m.Groups["path"];
					var lnum = m.Groups["line"];

					var str = new StringBuilder();
					var pre = string.Empty;
					if (path != null) { str.Append(path.Value.Trim('"', '\'')); pre = ": "; }
					if (lnum != null) { str.Append($"({lnum.Value})");          pre = ": "; }
					str.Append(pre).Append(line.Remove(cap0.Index, cap0.Length));
					sb.AppendLine(str.ToString());
				}
				else
				{
					sb.AppendLine(line);
				}
			}
			return sb.ToString();
		}

		/// <summary>Convert a size in bytes to a 'pretty' size in KB,MB,GB,etc</summary>
		/// <param name="bytes">The input data size</param>
		/// <param name="si">True to use 1000bytes = 1kb, false for 1024bytes = 1kb</param>
		/// <param name="dp">The number of decimal places to use</param>
		public static string PrettySize(long bytes, bool si, int dp)
		{
			var unit = si ? 1000 : 1024;
			if (bytes < unit) return bytes + (si ? "B" : "iB");
			var exp = (int)(Math.Log(bytes) / Math.Log(unit));
			var prefix = "KMGTPE"[exp-1];
			var fmt = "{0:N" + dp + "}{1}" + (si ? "B" : "iB");
			return string.Format(fmt, bytes / Math.Pow(unit, exp), prefix);
		}

		/// <summary>Returns a string showing the match results of 'pattern' used on 'str'</summary>
		public static string RegexTest(string str, string pattern, RegexOptions opts = RegexOptions.None)
		{
			var sb = new StringBuilder();

			try
			{
				var midx = 0;
				foreach (var m in Regex.Matches(str, pattern, opts).Cast<Match>())
				{
					sb.AppendLine($"Match: {midx++}");
					int gidx = 0;
					foreach (var g in m.Groups.Cast<Group>())
						sb.Append($"\t Group {gidx++}: ").Append(g.Value).AppendLine();
					sb.AppendLine();
				}
			}
			catch (Exception ex)
			{
				sb.Append(ex.MessageFull());
			}

			return sb.ToString();
		}

		/// <summary>
		/// Helper for making a file dialog file type filter string.
		/// Strings starting with *. are assumed to be extensions
		/// Use: FileDialogFilter("Text Files","*.txt","Bitmaps","*.bmp","*.png"); </summary>
		public static string FileDialogFilter(params string[] desc)
		{
			if (desc.Length == 0) return string.Empty;
			if (desc[0].StartsWith("*.")) throw new Exception("First string should be a file type description");

			var sb = new StringBuilder();
			var extn = new List<string>();
			for (int i = 0; i != desc.Length;)
			{
				if (sb.Length != 0) sb.Append("|");
				sb.Append(desc[i]);

				extn.Clear();
				for (++i; i != desc.Length && desc[i].StartsWith("*."); ++i)
					extn.Add(desc[i]);

				if (extn.Count == 0) throw new Exception("Expected file extension patterns to follow description");

				sb.Append(" (").Append(string.Join(",", extn)).Append(")");
				sb.Append("|").Append(string.Join(";", extn));
			}

			return sb.ToString();
		}

		/// <summary>Profile a block of code</summary>
		public static Scope Profile(string name)
		{
			var sw = new Stopwatch();
			return Scope.Create(
				() => sw.Start(),
				() =>
				{
					sw.Stop();
					Debug.WriteLine($"{name}: {sw.Elapsed.TotalMilliseconds:N4}ms");
				});
		}

		/// <summary>Execute a shell command (as a child process)</summary>
		public static string ShellCmd(string cmd)
		{
			var start_info =
				RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new ProcessStartInfo
				{
					FileName = "cmd",
					Arguments = $"/C {cmd}",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				} :
				RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = $"-c \"{cmd.Replace("\"", "\\\"")}\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				} :
				throw new Exception($"Unsupported platform");

			var process = new Process { StartInfo = start_info };
			process.Start();

			var result = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return result;
		}

		///<summary>Random names for testing</summary>
		public static string[] PeopleNames = new string[]
		{
			"Angela", "Boris", "Craig", "Darren", "Elvis", "Frank", "Gerard", "Hermes", "Imogen", "John",
			"Kaylee", "Luke", "Mormont", "Nigella", "Oscar", "Pavarotti", "Quin", "Russel", "Stan", "Toby",
			"Ulrick", "Vernon", "William", "Xavior", "Yvonne", "Zack",
		};

#if DEBUG
		public const bool IsDebug = true;
#else
		public const bool IsDebug = false;
#endif
	}

	/// <summary>Type specific utility methods</summary>
	public static class Util<T>
	{
		/// <summary>Call the constructor of 'T' to create a new instance. Use x.GetType().New() if the type is unknown at compile time</summary>
		public static T New()
		{
			m_cons0 ??= typeof(T).GetConstructor(new Type[] { }) ?? throw new Exception($"No default constructor found for {typeof(T).Name}");
			return (T)m_cons0.Invoke(null);
		}
		public static T New<A0>(A0 a0)
		{
			m_cons1 ??= typeof(T).GetConstructor(new Type[] { typeof(A0) }) ?? throw new Exception($"No arity 1 constructor found for {typeof(T).Name}");
			return (T)m_cons1.Invoke(new object?[] { a0 });
		}
		public static T New<A0, A1>(A0 a0, A1 a1)
		{
			m_cons2 ??= typeof(T).GetConstructor(new Type[] { typeof(A0), typeof(A1) }) ?? throw new Exception($"No arity 2 constructor found for {typeof(T).Name}");
			return (T)m_cons2.Invoke(new object?[] { a0, a1 });
		}
		public static T New<A0, A1, A2>(A0 a0, A1 a1, A2 a2)
		{
			m_cons3 ??= typeof(T).GetConstructor(new Type[] { typeof(A0), typeof(A1), typeof(A2) }) ?? throw new Exception($"No arity 3 constructor found for {typeof(T).Name}");
			return (T)m_cons3.Invoke(new object?[] { a0, a1, a2 });
		}
		private static ConstructorInfo? m_cons0;
		private static ConstructorInfo? m_cons1;
		private static ConstructorInfo? m_cons2;
		private static ConstructorInfo? m_cons3;

		///// <summary>Serialise 'obj' to a byte array. 'T' must have the [Serializable] attribute</summary>
		//public static byte[] ToBlob(T obj)
		//{
		//	using (var ms = new MemoryStream())
		//	{
		//		new BinaryFormatter().Serialize(ms, obj);
		//		return ms.ToArray();
		//	}
		//}

		///// <summary>Deserialise 'blob' to an instance of 'T'</summary>
		//public static T FromBlob(byte[] blob)
		//{
		//	using var ms = new MemoryStream(blob, false);
		//	var obj = new BinaryFormatter().Deserialize(ms);
		//	return (T)obj;
		//}

		/// <summary>
		/// Serialise an instance of type 'T' to xml.
		/// 'T' should be decorated with [DataContract],[DataMember] attributes.
		/// You also need to add custom types as '[KnownType(typeof(CustomType))]' attributes</summary>
		public static string ToXml(T obj, bool pretty = false)
		{
			var sb = new StringBuilder();
			var settings = pretty ? new XmlWriterSettings{Indent = true, IndentChars = "  ", Encoding = Encoding.UTF8, NewLineOnAttributes = false} : new XmlWriterSettings();
			using (var x = XmlWriter.Create(sb, settings))
			{
				var s = new DataContractSerializer(typeof(T));
				s.WriteObject(x, obj);
			}
			return sb.ToString();
		}

		/// <summary>Deserialise an instance of type 'T' from xml.</summary>
		public static T FromXml(string xml)
		{
			using var r = XmlReader.Create(new StringReader(xml));
			var s = new DataContractSerializer(typeof(T));
			return (T?)s.ReadObject(r) ?? throw new NullReferenceException();
		}
	}

	/// <summary>Three state values</summary>
	public enum Tri
	{
		False   = 0,
		True    = 1,
		NotSet  = 2,

		No      = 0,
		Yes     = 1,
		Unknown = 2,

		Clear   = 0,
		Set     = 1,
		Toggle  = 2,
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture] public class TestUtils
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct Struct
		{
			public int m_int;
			public byte m_byte;
			public ushort m_ushort;

			public static bool operator ==(Struct left, Struct right)
			{
				return left.Equals(right);
			}
			public static bool operator !=(Struct left, Struct right)
			{
				return !(left == right);
			}
			public override bool Equals(object? obj)
			{
				return base.Equals(obj);
			}
			public bool Equals(Struct other)
			{
				return m_int == other.m_int && m_byte == other.m_byte && m_ushort == other.m_ushort;
			}
			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = m_int;
					hashCode = (hashCode * 397) ^ m_byte.GetHashCode();
					hashCode = (hashCode * 397) ^ m_ushort.GetHashCode();
					return hashCode;
				}
			}
		}

		[DataContract][Serializable]
		public class SerialisableType
		{
			public enum SomeEnum
			{
				[EnumMember] One,
				[EnumMember] Two,
				[EnumMember] Three,
			}
			[DataMember] public int      Int    { get; set; }
			[DataMember] public string?  String { get; set; }
			[DataMember] public Point    Point  { get; set; }
			[DataMember] public SomeEnum EEnum  { get; set; }
			[DataMember] public int[]?   Data   { get; set; }
		}

		[Test] public void Convert()
		{
			var src = new[] {1,2,3,4};
			var dst = new List<int>(Util.Conv(src, i=>i*2));
			for (int i = 0; i != src.Length; ++i) Assert.Equal(dst[i], 2*src[i]);
	    }
		[Test] public void ToFromByteArray()
		{
			const ulong num = 12345678910111213;
			var bytes = Util.ToBytes(num);
			Assert.Equal(8, bytes.Length);
			var NUM = Util.FromBytes<ulong>(bytes);
			Assert.Equal(num, NUM);

			var s = new Struct{m_int = 42, m_byte = 0xab, m_ushort = 0xfedc};
			bytes = Util.ToBytes(s);
			Assert.Equal(7, bytes.Length);

			var S = Util.FromBytes<Struct>(bytes);
			Assert.Equal(s,S);

			var b = Util.FromBytes<byte>(bytes, 4);
			Assert.Equal(s.m_byte, b);
		}
		[Test] public void ToFromXml()
		{
			var x1 = new SerialisableType
			{
				Int = 1,
				String = "2",
				Point = new Point(3, 4),
				EEnum = SerialisableType.SomeEnum.Two,
				Data = new[] { 1, 2, 3, 4 }
			};
			var xml = Util<SerialisableType>.ToXml(x1, true);
			var x2 = Util<SerialisableType>.FromXml(xml);
			Assert.Equal(x1.Int, x2.Int);
			Assert.Equal(x1.String, x2.String);
			Assert.Equal(x1.Point, x2.Point);
			Assert.Equal(x1.EEnum, x2.EEnum);
			Assert.True(x1.Data.SequenceEqual(x2.Data!));
		}
		//[Test] public void ToFromBinary()
		//{
		//	var x1 = new SerialisableType{Int = 1, String = "2", Point = new Point(3,4), EEnum = SerialisableType.SomeEnum.Two, Data = new[]{1,2,3,4}};
		//	var xml = Util<SerialisableType>.ToBlob(x1);
		//	var x2 = Util<SerialisableType>.FromBlob(xml);
		//	Assert.Equal(x1.Int, x2.Int);
		//	Assert.Equal(x1.String, x2.String);
		//	Assert.Equal(x1.Point, x2.Point);
		//	Assert.Equal(x1.EEnum, x2.EEnum);
		//	Assert.True(x1.Data.SequenceEqual(x2.Data));
		//}
		[Test] public void PrettySize()
		{
			Func<long,string> pretty = size => { return Util.PrettySize(size, true, 1) + " " + Util.PrettySize(size, false, 1); };

			Assert.Equal(      "0B 0iB"      , pretty(0));
			Assert.Equal(     "27B 27iB"     , pretty(27));
			Assert.Equal(    "999B 999iB"    , pretty(999));
			Assert.Equal(   "1.0KB 1000iB"   , pretty(1000));
			Assert.Equal(   "1.0KB 1023iB"   , pretty(1023));
			Assert.Equal(   "1.0KB 1.0KiB"   , pretty(1024));
			Assert.Equal(   "1.7KB 1.7KiB"   , pretty(1728));
			Assert.Equal( "110.6KB 108.0KiB" , pretty(110592));
			Assert.Equal(   "7.1MB 6.8MiB"   , pretty(7077888));
			Assert.Equal( "453.0MB 432.0MiB" , pretty(452984832));
			Assert.Equal(  "29.0GB 27.0GiB"  , pretty(28991029248));
			Assert.Equal(   "1.9TB 1.7TiB"   , pretty(1855425871872));
			Assert.Equal(   "9.2EB 8.0EiB"   , pretty(9223372036854775807));
		}
		[Test] public void ToFromHexString()
		{
			var data = new byte[]{1,3,5,7,9,10,8,6,4,2,0,255,128};
			var s = Util.ToHexString(data,sep:",",line_sep:"\r\n",width:4);
			Assert.Equal(s, "01,03,05,07\r\n09,0A,08,06\r\n04,02,00,FF\r\n80");
			var d = Util.FromHexString(s);
			Assert.True(data.SequenceEqual(d));
		}
	}
}
#endif
