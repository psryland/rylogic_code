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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

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
		/// <summary>Enable double buffering for the control</summary>
		public static void DblBuffer(this Control ctl)
		{
			PropertyInfo pi = ctl.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance|BindingFlags.NonPublic);
			pi.SetValue(ctl, true, null);
			MethodInfo mi = ctl.GetType().GetMethod("SetStyle", BindingFlags.Instance|BindingFlags.NonPublic);
			mi.Invoke(ctl, new object[]{ControlStyles.DoubleBuffer|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint, true});
		}

		/// <summary>Convert a collection of 'U' into a collection of 'T' using 'conv' to do the conversion</summary>
		public static IEnumerable<T> Conv<T,U>(IEnumerable<U> collection, Func<U,T> conv)
		{
			foreach (U i in collection) yield return conv(i);
		}

		/// <summary>Helper for allocating an array of default constructed classes</summary>
		public static T[] New<T>(uint count) where T : new()
		{
			T[] arr = new T[count];
			for (uint i = 0; i != count; ++i) arr[i] = new T();
			return arr;
		}

		/// <summary>Helper for allocating an array of constructed classes</summary>
		public static T[] New<T>(uint count, Func<uint,T> construct)
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
			return Enum.GetValues(enum_type).Length;
		}

		/// <summary>Return the next value in an enum, rolling over at the last value. Must be used only on Enums with range [0, count)</summary>
		public static int Next<T>(T zoom_type)
		{
			int v = Convert.ToInt32(zoom_type);
			return (v + 1) % Count(typeof(T));
		}

		/// <summary>Convert a structure into an array of bytes. Remember to use the MarshalAs attribute for structure fields that need it</summary>
		public static byte[] ToBytes(object structure)
		{
			int size = Marshal.SizeOf(structure);
			byte[] arr = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(structure, ptr, true);
			Marshal.Copy(ptr, arr, 0, size);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

		/// <summary>Create an array of bytes with the same size as 'T'</summary>
		public static byte[] ToBytes<T>()
		{
			return new byte[Marshal.SizeOf(typeof(T))];
		}

		/// <summary>Convert a subrange of bytes to a structure</summary>
		public static T FromBytes<T>(byte[] arr, int offset)
		{
			Debug.Assert(arr.Length - offset >= Marshal.SizeOf(typeof(T)), "FromBytes<T>: Insufficient data");
			int sz = Marshal.SizeOf(typeof(T));
			IntPtr ptr = Marshal.AllocHGlobal(sz);
			Marshal.Copy(arr, offset, ptr, sz);
			T structure = (T)Marshal.PtrToStructure(ptr, typeof(T));
			Marshal.FreeHGlobal(ptr);
			return structure;
		}

		/// <summary>Convert an array of bytes to a structure</summary>
		public static T FromBytes<T>(byte[] arr)
		{
			return FromBytes<T>(arr, 0);
		}

		/// <summary>Return the distance between to points</summary>
		public static double Distance(Point lhs, Point rhs)
		{
			int dx = lhs.X - rhs.X;
			int dy = lhs.Y - rhs.Y;
			return Math.Sqrt(dx*dx + dy*dy);
		}
	
		/// <summary>Return an assembly attribute</summary>
		public static T GetAssemblyAttribute<T>(Assembly ass = null)
		{
			if (ass == null) ass = Assembly.GetEntryAssembly();
			object[] attr = ass.GetCustomAttributes(typeof(T), false);
			if (attr.Length == 0) throw new ApplicationException("Assembly does not have attribute "+typeof(T).Name);
			return (T)(attr[0]);
		}
		
		/// <summary>Return the version number for an assembly</summary>
		public static Version AssemblyVersion(Assembly ass = null)
		{
			if (ass == null) ass = Assembly.GetEntryAssembly();
			return ass.GetName().Version;
		}

		/// <summary>Returns the timestamp of an assembly. Use 'Assembly.GetCallingAssembly()'</summary>
		public static DateTime AssemblyTimestamp(Assembly ass = null)
		{
			const int PeHeaderOffset = 60;
			const int LinkerTimestampOffset = 8;

			byte[] b = new byte[2048];
			if (ass == null) ass = Assembly.GetEntryAssembly();
			using (Stream s = new FileStream(ass.Location, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, 2048);

			DateTime ts = new DateTime(1970, 1, 1, 0, 0, 0);
			ts = ts.AddSeconds(BitConverter.ToInt32(b, BitConverter.ToInt32(b, PeHeaderOffset) + LinkerTimestampOffset));
			ts = ts.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(ts).Hours);
			return ts;
		}

		/// <summary>Convert a unix time (i.e. seconds past 1/1/1970) to a date time</summary>
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
		
		/// <summary>Set the tooltip for this control</summary>
		public static void ToolTip(this Control ctrl, ToolTip tt, string caption)
		{
			tt.SetToolTip(ctrl, caption);
		}
	}
	
	/// <summary>Type specific utility methods</summary>
	public static class Util<T>
	{
		/// <summary>
		/// Derives the name of a property from the given lambda expression and returns it as string.
		/// Example: DateTime.Now.PropertyName(s => s.Ticks) returns "Ticks"
		/// </summary>
		public static string MemberName<Ret>(Expression<Func<T,Ret>> expression)
		{
			UnaryExpression unex = expression.Body as UnaryExpression;
			return unex != null && unex.NodeType == ExpressionType.Convert
				? ((MemberExpression) unex.Operand).Member.Name
				: ((MemberExpression) expression.Body).Member.Name;
		}
		
		/// <summary>Returns all inherited fields for a type</summary>
		private static IEnumerable<FieldInfo> AllFields(Type type, BindingFlags flags)
		{
			if (type == null) return Enumerable.Empty<FieldInfo>();
			return AllFields(type.BaseType, flags).Union(type.GetFields(flags|BindingFlags.DeclaredOnly));
		}

		/// <summary>Copies all of the fields of 'from' into 'to'. Returns 'to' for method chaining</summary>
		public static T ShallowCopy(T from, T to)
		{
			foreach (var x in AllFields(typeof(T), BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
				x.SetValue(to, x.GetValue(from));
			return to;
		}
	}

	/// <summary>Utils unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestUtils()
		{
			{// Conv
				int[] src = {1,2,3,4};
				List<int> dst = new List<int>(Util.Conv(src, i=>i*2));
				for (int i = 0; i != src.Length; ++i) Assert.AreEqual(dst[i], 2*src[i]);
			}
			{// ToBytes[]/FromBytes[]
				const ulong num = 12345678910111213;
				byte[] bytes = Util.ToBytes(num);
				Assert.AreEqual(8, bytes.Length);
				Util.FromBytes<ulong>(bytes);
			}
		}

		[Test] public static void TestPropertyName()
		{
			Assert.AreEqual("X", Util<Point>.MemberName(p=>p.X));
			Assert.AreEqual("BaseStream", Util<StreamWriter>.MemberName(s=>s.BaseStream));
		}
	}
}
