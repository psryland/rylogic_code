using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace pr.win32
{
	public static partial class Win32
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public int Width()                                  { return right - left; }
			public int Height()                                 { return bottom - top; }
			public static RECT FromRectangle(Rectangle rect)    { return new RECT{left=rect.Left, top=rect.Top, right=rect.Right, bottom=rect.Bottom}; }
			public Rectangle   ToRectangle()                    { return new Rectangle(left, top, Width(), Height()); }
			public static RECT FromSize(Size size)              { return new RECT{left=0, top=0, right=size.Width, bottom=size.Height}; }
			public Size        ToSize()                         { return new Size(right - left, bottom - top); }
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
			public static POINT FromPoint(Point pt)             { return new POINT{X=pt.X, Y=pt.Y}; }
			public Point ToPoint()                              { return new Point(X, Y); }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SCROLLINFO
		{
			public uint cbSize;
			public uint fMask;
			public int nMin;
			public int nMax;
			public uint nPage;
			public int nPos;
			public int nTrackPos;
			public static SCROLLINFO Default                     { get {return new SCROLLINFO{cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO))};} }
		}

		// Structure pointed to by WM_GETMINMAXINFO lParam
		[StructLayout(LayoutKind.Sequential)]
		public struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct  XFORM
		{
			public float eM11;
			public float eM12;
			public float eM21;
			public float eM22;
			public float eDx;
			public float eDy;
		}

		// Used by the FindFirstFile or FindNextFile functions.
		[Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
		public class WIN32_FIND_DATA
		{
			public FileAttributes dwFileAttributes;
			public uint ftCreationTime_dwLowDateTime;
			public uint ftCreationTime_dwHighDateTime;
			public uint ftLastAccessTime_dwLowDateTime;
			public uint ftLastAccessTime_dwHighDateTime;
			public uint ftLastWriteTime_dwLowDateTime;
			public uint ftLastWriteTime_dwHighDateTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public int dwReserved0;
			public int dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;

			/// <summary>The filename.extn of the file</summary>
			public string FileName { get { return cFileName; } }

			/// <summary>Attributes of the file.</summary>
			public FileAttributes Attributes { get { return dwFileAttributes; } }

			/// <summary>The file size</summary>
			public long FileSize { get { return ToLong(nFileSizeHigh, nFileSizeLow); } }

			/// <summary>File creation time in local time</summary>
			public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }

			/// <summary>File creation time in UTC</summary>
			public DateTime CreationTimeUtc { get { return ToDateTime(ftCreationTime_dwHighDateTime, ftCreationTime_dwLowDateTime); } }

			/// <summary>Gets the last access time in local time.</summary>
			public DateTime LastAccessTime { get { return LastAccessTimeUtc.ToLocalTime(); } }

			/// <summary>File last access time in UTC</summary>
			public DateTime LastAccessTimeUtc { get { return ToDateTime(ftLastAccessTime_dwHighDateTime, ftLastAccessTime_dwLowDateTime); } }

			/// <summary>Gets the last access time in local time.</summary>
			public DateTime LastWriteTime { get { return LastWriteTimeUtc.ToLocalTime(); } }

			/// <summary>File last write time in UTC</summary>
			public DateTime LastWriteTimeUtc { get { return ToDateTime(ftLastWriteTime_dwHighDateTime, ftLastWriteTime_dwLowDateTime); } }

			public override string ToString()                       { return cFileName; }
			private static DateTime ToDateTime(uint high, uint low) { return DateTime.FromFileTimeUtc(ToLong(high,low)); }
			private static long ToLong(uint high, uint low)         { return ((long)high << 0x20) | low; }
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct COMDLG_FILTERSPEC
		{
			[MarshalAs(UnmanagedType.LPWStr)] public string pszName;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct PROPERTYKEY
		{
			public Guid fmtid;
			public uint pid;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct KNOWNFOLDER_DEFINITION
		{
			public KF_CATEGORY category;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszName;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszCreator;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszDescription;
			public Guid fidParent;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszRelativePath;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszParsingName;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszToolTip;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszLocalizedName;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszIcon;
			[MarshalAs(UnmanagedType.LPWStr)] public string pszSecurity;
			public uint dwAttributes;
			public KF_DEFINITION_FLAGS kfdFlags;
			public Guid ftidType;
		}
	}
}
