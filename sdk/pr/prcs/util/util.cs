//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using pr.common;
using pr.maths;
using pr.extn;

namespace pr.util
{
	/// <summary>Three state values</summary>
	public enum Tri
	{
		False   = 0,
		True    = 1,
		NotSet  = 2,

		No      = 0,
		Yes     = 1,
		Unknown = 2,
	}

	/// <summary>Utility function container</summary>
	public static class Util
	{
		/// <summary>Compare two ranges within a byte array</summary>
		public static int Compare(byte[] lhs, int lstart, int llength, byte[] rhs, int rstart, int rlength)
		{
			for (;llength != 0 && rlength != 0; ++lstart, ++rstart, --llength, --rlength)
			{
				if (lhs[lstart] == rhs[rstart]) continue;
				return Maths.Compare(lhs[lstart], rhs[rstart]);
			}
			return Maths.Compare(llength, rlength);
		}

		/// <summary>Convert a collection of 'U' into a collection of 'T' using 'conv' to do the conversion</summary>
		public static IEnumerable<T> Conv<T,U>(IEnumerable<U> collection, Func<U, T> conv)
		{
			foreach (U i in collection) yield return conv(i);
		}

		/// <summary>Helper for allocating an array of default constructed classes</summary>
		public static T[] NewArray<T>(uint count) where T : new()
		{
			T[] arr = new T[count];
			for (uint i = 0; i != count; ++i) arr[i] = new T();
			return arr;
		}

		/// <summary>Helper for allocating an array of constructed classes</summary>
		public static T[] NewArray<T>(uint count, Func<uint, T> construct)
		{
			T[] arr = new T[count];
			for (uint i = 0; i != count; ++i) arr[i] = construct(i);
			return arr;
		}

		/// <summary>Returns the number to add to pad 'size' up to 'alignment'</summary>
		public static int Pad(int size, int alignment)
		{
			return (alignment - (size % alignment)) % alignment;
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
			var v = Convert.ToInt32(zoom_type);
			return (v + 1) % Count(typeof(T));
		}

		/// <summary>Convert a structure into an array of bytes. Remember to use the MarshalAs attribute for structure fields that need it</summary>
		public static byte[] ToBytes(object structure)
		{
			var size = Marshal.SizeOf(structure);
			var arr = new byte[size];
			using (var ptr = MarshalEx.AllocHGlobal(size))
			{
				Marshal.StructureToPtr(structure, ptr.State, true);
				Marshal.Copy(ptr.State, arr, 0, size);
				return arr;
			}
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
			using (var ptr = MarshalEx.AllocHGlobal(sz))
			{
				Marshal.Copy(arr, offset, ptr.State, sz);
				return (T)Marshal.PtrToStructure(ptr.State, typeof(T));
			}
		}

		/// <summary>Convert an array of bytes to a structure</summary>
		public static T FromBytes<T>(byte[] arr)
		{
			Debug.Assert(arr.Length >= Marshal.SizeOf(typeof(T)), "FromBytes<T>: Insufficient data");
			using (var handle = GCHandleEx.Alloc(arr, GCHandleType.Pinned))
				return (T)Marshal.PtrToStructure(handle.State.AddrOfPinnedObject(), typeof(T));
		}

		/// <summary>Return the distance between to points</summary>
		public static double Distance(Point lhs, Point rhs)
		{
			int dx = lhs.X - rhs.X;
			int dy = lhs.Y - rhs.Y;
			return Math.Sqrt(dx*dx + dy*dy);
		}

		/// <summary>Return an assembly attribute</summary>
		public static T GetAssemblyAttribute<T>(Assembly ass)
		{
			if (ass == null) ass = Assembly.GetEntryAssembly();
			if (ass == null) ass = Assembly.GetExecutingAssembly();
			object[] attr = ass.GetCustomAttributes(typeof(T), false);
			if (attr.Length == 0) throw new ApplicationException("Assembly does not have attribute "+typeof(T).Name);
			return (T)(attr[0]);
		}
		public static T GetAssemblyAttribute<T>() { return GetAssemblyAttribute<T>(null); }

		/// <summary>Return the version number for an assembly</summary>
		public static Version AssemblyVersion(Assembly ass)
		{
			if (ass == null) ass = Assembly.GetEntryAssembly();
			if (ass == null) ass = Assembly.GetExecutingAssembly();
			return ass.GetName().Version;
		}
		public static Version AssemblyVersion() { return AssemblyVersion(null); }

		/// <summary>Returns the timestamp of an assembly. Use 'Assembly.GetCallingAssembly()'</summary>
		public static DateTime AssemblyTimestamp(Assembly ass)
		{
			const int PeHeaderOffset = 60;
			const int LinkerTimestampOffset = 8;

			byte[] b = new byte[2048];
			if (ass == null) ass = Assembly.GetEntryAssembly();
			if (ass == null) ass = Assembly.GetExecutingAssembly();
			using (Stream s = new FileStream(ass.Location, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, 2048);

			DateTime ts = new DateTime(1970, 1, 1, 0, 0, 0);
			ts = ts.AddSeconds(BitConverter.ToInt32(b, BitConverter.ToInt32(b, PeHeaderOffset) + LinkerTimestampOffset));
			ts = ts.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(ts).Hours);
			return ts;
		}
		public static DateTime AssemblyTimestamp() { return AssemblyTimestamp(null); }

		/// <summary>Read a text file embedded resource returning it as a string</summary>
		public static string TextResource(string resource_name, Assembly ass)
		{
			ass = ass ?? Assembly.GetExecutingAssembly(); // this will look in pr.dll for resources.. probably not what's wanted
			var stream = ass.GetManifestResourceStream(resource_name);
			if (stream == null) throw new IOException("No resource with name "+resource_name+" found");
			using (var src = new StreamReader(stream))
				return src.ReadToEnd();
		}

		/// <summary>
		/// Event handler used to load a dll from an embedded resource.<para/>
		/// Use:<para/>
		///  Add dependent dlls to the project and select "Embedded Resource" in their properties<para/>
		///  AppDomain.CurrentDomain.AssemblyResolve += (s,a) => Util.ResolveEmbeddedAssembly(a.Name);</summary>
		public static Assembly ResolveEmbeddedAssembly(string assembly_name)
		{
			var resource_name = "AssemblyLoadingAndReflection." + new AssemblyName(assembly_name).Name + ".dll";
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource_name))
			{
				Debug.Assert(stream != null, "Embedded assembly not found");
				var assembly_data = new Byte[stream.Length];
				stream.Read(assembly_data, 0, assembly_data.Length);
				return Assembly.Load(assembly_data);
			}
		}

		/// <summary>Convert a Unix time (i.e. seconds past 1/1/1970) to a date time</summary>
		public static DateTime UnixTimeToDateTime(long milliseconds)
		{
			const long ticks_per_ms = 10000;
			DateTime time = m_time0;
			return time.AddTicks(milliseconds*ticks_per_ms);
		}
		private static readonly DateTime m_time0 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

		/// <summary>Helper to ignore the requirement for matched called to Cursor.Show()/Hide()</summary>
		public static bool ShowCursor
		{
			get { return m_cursor_visible; }
			set
			{
				if (ShowCursor == value) return;
				m_cursor_visible = value;
				if (value) Cursor.Show(); else Cursor.Hide();
			}
		}
		private static bool m_cursor_visible = true;

		/// <summary>Returns true if 'point' is more than the drag size from 'ref_point'</summary>
		public static bool Moved(Point point, Point ref_point)
		{
			return Moved(point, ref_point, SystemInformation.DragSize.Width, SystemInformation.DragSize.Height);
		}

		/// <summary>Returns true if 'point' is more than than 'dx' or 'dy' from 'ref_point'</summary>
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

		/// <summary>Excute a function in a background thread and terminate it if it doesn't finished before 'timeout'</summary>
		public static TResult RunWithTimeout<TResult>(Func<TResult> proc, int duration)
		{
			var r = default(TResult);
			Exception ex = null;
			using (var reset = new AutoResetEvent(false))
			{
				// Can't use the thread pool for this because we abort the thread
				var thread = new Thread(() =>
					{
						// ReSharper disable AccessToDisposedClosure
						try { r = proc(); }
						catch (Exception e) { ex = e; }
						reset.Set();
						// ReSharper restore AccessToDisposedClosure
					});
				thread.Start();
				if (!reset.WaitOne(duration))
				{
					thread.Abort();
					throw new TimeoutException();
				}
			}
			if (ex != null)
				throw ex;
			return r;
		}

		/// <summary>Helper method for converting a filename extension to a mime type</summary>
		public static string MimeFromExtn(string extn)
		{
			string mime;
			return m_mime_type_map.TryGetValue(extn.TrimStart('.'), out mime) ? mime : "unknown/unknown";
		}
		#region Mime type dictionary
		private static readonly Dictionary<string, string> m_mime_type_map = new Dictionary<string, string>
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

		/// <summary>Return the value for 'key', if it doesn't exist, insert and return the result of calling 'def'</summary>
		public static V GetOrAdd<K,V>(this Dictionary<K, V> dic, K key, Func<K,V> def)
		{
			V value;
			if (dic.TryGetValue(key, out value)) return value;
			dic.Add(key, value = def(key)); // 'def' is only evaluated if 'key' is not found
			return value;
		}

		/// <summary>A stack trace that selected a band of stack frames</summary>
		public static string StackTrace(int skip, int count)
		{
			StringBuilder sb = new StringBuilder();
			var st = new StackTrace(1,true).GetFrames();
			if (st == null) return "";
			foreach (var s in st.Skip(skip).Take(count))
			{
				var m = s.GetMethod();
				sb.AppendFormat(" {0,-60}({1,3})  {2}.{3}\n" ,s.GetFileName() ,s.GetFileLineNumber() ,m.ReflectedType.FullName ,m.Name);
			}
			return sb.ToString();
		}

		/// <summary>Return a sub range of an array of T</summary>
		public static T[] SubRange<T>(T[] arr, int start, int length)
		{
			var sub = new T[length];
			Array.Copy(arr, start, sub, 0, length);
			return sub;
		}

		/// <summary>Convert a size in bytes to a 'pretty' size in KB,MB,GB,etc</summary>
		/// <param name="bytes">The input data size</param>
		/// <param name="si">True to use 1000bytes = 1kb, false for 1024bytes = 1kb</param>
		/// <param name="dp">The number of decimal places to use</param>
		public static string PrettySize(long bytes, bool si, int dp)
		{
			int unit = si ? 1000 : 1024;
			if (bytes < unit) return bytes + (si ? "B" : "iB");
			int exp = (int)(Math.Log(bytes) / Math.Log(unit));
			char prefix = "KMGTPE"[exp-1];
			var fmt = "{0:N" + dp + "}{1}" + (si ? "B" : "iB");
			return string.Format(fmt, bytes / Math.Pow(unit, exp), prefix);
		}

		/// <summary>
		/// A helper for copying lib files to the current output directory
		/// 'src_filepath' is the source lib name with {platform} and {config} optional substitution tags
		/// 'dst_filepath' is the output lib name with {platform} and {config} optional substitution tags
		/// If 'dst_filepath' already exists then it is overridden if 'overwrite' is true, otherwise 'DestExists' is returned
		/// 'src_filepath' can be a relative path, if so, the search order is: local directory, Q:\sdk\pr\lib
		/// </summary>
		public static ELibCopyResult LibCopy(string src_filepath, string dst_filepath, bool overwrite)
		{
			// Do text substitutions
			src_filepath = src_filepath.Replace("{platform}", Environment.Is64BitProcess ? "x64" : "x86");
			dst_filepath = dst_filepath.Replace("{platform}", Environment.Is64BitProcess ? "x64" : "x86");

			#if DEBUG
			const string config = "debug";
			#else
			const string config = "release";
			#endif
			src_filepath = src_filepath.Replace("{config}", config);
			dst_filepath = dst_filepath.Replace("{config}", config);

			// Check if 'dst_filepath' already exists
			dst_filepath = Path.GetFullPath(dst_filepath);
			if (!overwrite && PathEx.FileExists(dst_filepath))
			{
				Log.Info(null, "LibCopy: Not copying {0} as {1} already exists".Fmt(src_filepath, dst_filepath));
				return ELibCopyResult.DestExists;
			}

			// Get the full path for 'src_filepath'
			for (;!Path.IsPathRooted(src_filepath);)
			{
				// If 'src_filepath' exists in the local directory
				var full = Path.GetFullPath(src_filepath);
				if (File.Exists(full))
				{
					src_filepath = full;
					break;
				}

				// Get the pr libs directory
				var pr_root = Environment.GetEnvironmentVariable("pr_root");
				if (!pr_root.HasValue()) pr_root = @"q:";

				full = Path.Combine(pr_root+@"\sdk\pr\lib", src_filepath);
				if (File.Exists(full))
				{
					src_filepath = full;
					break;
				}

				// Can't find 'src_filepath'
				Log.Info(null, "LibCopy: Not copying {0}, file not found".Fmt(src_filepath));
				return ELibCopyResult.SrcNotFound;
			}

			// Copy the file
			Log.Info(null, "LibCopy: {0} -> {1}".Fmt(src_filepath, dst_filepath));
			File.Copy(src_filepath, dst_filepath, true);
			return ELibCopyResult.Success;
		}
		public enum ELibCopyResult { Success, DestExists, SrcNotFound }
	}

	/// <summary>Type specific utility methods</summary>
	public static class Util<T>
	{
		/// <summary>Serialise 'obj' to a byte array. 'T' must have the [Serializable] attribute</summary>
		public static byte[] ToBlob(T obj)
		{
			using (var ms = new MemoryStream())
			{
				new BinaryFormatter().Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		/// <summary>Deserialise 'blob' to an instance of 'T'</summary>
		public static T FromBlob(byte[] blob)
		{
			using (var ms = new MemoryStream(blob, false))
			{
				var obj = new BinaryFormatter().Deserialize(ms);
				return (T)obj;
			}
		}

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
			using (var r = XmlReader.Create(new StringReader(xml)))
			{
				var s = new DataContractSerializer(typeof(T));
				return (T)s.ReadObject(r);
			}
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
		internal static partial class TestUtils
		{
			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct Struct
			{
				public int m_int;
				public byte m_byte;
				public ushort m_ushort;

				public override bool Equals(object obj) { return base.Equals(obj); }
				public bool Equals(Struct other)        { return m_int == other.m_int && m_byte == other.m_byte && m_ushort == other.m_ushort; }
				public override int GetHashCode()       { unchecked { int hashCode = m_int; hashCode = (hashCode*397) ^ m_byte.GetHashCode(); hashCode = (hashCode*397) ^ m_ushort.GetHashCode(); return hashCode; } }
			}

			[DataContract] [Serializable]
			public class SerialisableType
			{
				public enum SomeEnum
				{
					[EnumMember] One,
					[EnumMember] Two,
					[EnumMember] Three,
				}
				[DataMember] public int      Int    { get; set; }
				[DataMember] public string   String { get; set; }
				[DataMember] public Point    Point  { get; set; }
				[DataMember] public SomeEnum EEnum  { get; set; }
				[DataMember] public int[]    Data   { get; set; }
			}

			[Test] public static void ByteArrayCompare()
		{
			byte[] lhs = new byte[]{1,2,3,4,5};
			byte[] rhs = new byte[]{3,4,5,6,7};

			Assert.AreEqual(-1, Util.Compare(lhs, 0, 5, rhs, 0, 5));
			Assert.AreEqual( 0, Util.Compare(lhs, 2, 3, rhs, 0, 3));
			Assert.AreEqual( 1, Util.Compare(lhs, 3, 2, rhs, 0, 2));
			Assert.AreEqual(-1, Util.Compare(lhs, 2, 3, rhs, 0, 4));
			Assert.AreEqual( 1, Util.Compare(lhs, 2, 3, rhs, 0, 2));
		}
			[Test] public static void Convert()
		{
			int[] src = {1,2,3,4};
			List<int> dst = new List<int>(Util.Conv(src, i=>i*2));
			for (int i = 0; i != src.Length; ++i) Assert.AreEqual(dst[i], 2*src[i]);
		}
			[Test] public static void ToFromByteArray()
			{
				const ulong num = 12345678910111213;
				var bytes = Util.ToBytes(num);
				Assert.AreEqual(8, bytes.Length);
				var NUM = Util.FromBytes<ulong>(bytes);
				Assert.AreEqual(num, NUM);

				var s = new Struct{m_int = 42, m_byte = 0xab, m_ushort = 0xfedc};
				bytes = Util.ToBytes(s);
				Assert.AreEqual(7, bytes.Length);

				var S = Util.FromBytes<Struct>(bytes);
				Assert.AreEqual(s,S);

				var b = Util.FromBytes<byte>(bytes, 4);
				Assert.AreEqual(s.m_byte, b);
			}
			[Test] public static void ToFromXml()
			{
				var x1 = new SerialisableType{Int = 1, String = "2", Point = new Point(3,4), EEnum = SerialisableType.SomeEnum.Two, Data = new[]{1,2,3,4}};
				var xml = Util<SerialisableType>.ToXml(x1, true);
				var x2 = Util<SerialisableType>.FromXml(xml);
				Assert.AreEqual(x1.Int, x2.Int);
				Assert.AreEqual(x1.String, x2.String);
				Assert.AreEqual(x1.Point, x2.Point);
				Assert.AreEqual(x1.EEnum, x2.EEnum);
				Assert.IsTrue(x1.Data.SequenceEqual(x2.Data));
			}
			[Test] public static void ToFromBinary()
			{
				var x1 = new SerialisableType{Int = 1, String = "2", Point = new Point(3,4), EEnum = SerialisableType.SomeEnum.Two, Data = new[]{1,2,3,4}};
				var xml = Util<SerialisableType>.ToBlob(x1);
				var x2 = Util<SerialisableType>.FromBlob(xml);
				Assert.AreEqual(x1.Int, x2.Int);
				Assert.AreEqual(x1.String, x2.String);
				Assert.AreEqual(x1.Point, x2.Point);
				Assert.AreEqual(x1.EEnum, x2.EEnum);
				Assert.IsTrue(x1.Data.SequenceEqual(x2.Data));
			}
			[Test] public static void PrettySize()
			{
				Func<long,string> pretty = size => { return Util.PrettySize(size, true, 1) + " " + Util.PrettySize(size, false, 1); };

				Assert.AreEqual(      "0B 0iB"      , pretty(0));
				Assert.AreEqual(     "27B 27iB"     , pretty(27));
				Assert.AreEqual(    "999B 999iB"    , pretty(999));
				Assert.AreEqual(   "1.0KB 1000iB"   , pretty(1000));
				Assert.AreEqual(   "1.0KB 1023iB"   , pretty(1023));
				Assert.AreEqual(   "1.0KB 1.0KiB"   , pretty(1024));
				Assert.AreEqual(   "1.7KB 1.7KiB"   , pretty(1728));
				Assert.AreEqual( "110.6KB 108.0KiB" , pretty(110592));
				Assert.AreEqual(   "7.1MB 6.8MiB"   , pretty(7077888));
				Assert.AreEqual( "453.0MB 432.0MiB" , pretty(452984832));
				Assert.AreEqual(  "29.0GB 27.0GiB"  , pretty(28991029248));
				Assert.AreEqual(   "1.9TB 1.7TiB"   , pretty(1855425871872));
				Assert.AreEqual(   "9.2EB 8.0EiB"   , pretty(9223372036854775807));
			}
		}
	}
}
#endif
