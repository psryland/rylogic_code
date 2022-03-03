using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using HWND     = System.IntPtr;
using HCURSOR  = System.IntPtr;
using UINT     = System.UInt32;
using WORD     = System.UInt16;
using DWORD    = System.UInt32;
using COLORREF = System.UInt32;
using WPARAM   = System.IntPtr;
using LPARAM   = System.IntPtr;

namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Message
		{
			public HWND hwnd;
			public UINT message;
			public WPARAM wparam;
			public LPARAM lparam;
			public DWORD time;
			public POINT pt;
			public DWORD lPrivate;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public int width                                        { get { return right - left; } }
			public int height                                       { get { return bottom - top; } }

			public static RECT FromRectangle(Rectangle rect)        { return new RECT{left=rect.Left, top=rect.Top, right=rect.Right, bottom=rect.Bottom}; }
			public Rectangle   ToRectangle()                        { return new Rectangle(left, top, width, height); }
			public static RECT FromSize(Size size)                  { return new RECT{left=0, top=0, right=size.Width, bottom=size.Height}; }
			public Size        ToSize()                             { return new Size(right - left, bottom - top); }
			public static RECT FromLTRB(int l, int t, int r, int b) { return new RECT{left=l, top=t, right=r, bottom=b}; }
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
			public static POINT FromPoint(Point pt)         { return new POINT{X=pt.X, Y=pt.Y}; }
			public Point ToPoint()                          { return new Point(X, Y); }
			public static implicit operator Point(POINT pt) { return pt.ToPoint(); }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct CURSORINFO
		{
			public DWORD cbSize;
			public DWORD flags;
			public HCURSOR hCursor;
			public POINT ptScreenPos;

			public static CURSORINFO Default => new CURSORINFO { cbSize = (uint)Marshal.SizeOf(typeof(CURSORINFO)) };
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NMHDR
		{
			// Notification message (WM_NOTIFY) header
			public HWND hwndFrom;
			public UINT idFrom;
			public UINT code; // NM_code
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

			public static SCROLLINFO Default => new SCROLLINFO { cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO)) };
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MINMAXINFO
		{
			// Structure pointed to by WM_GETMINMAXINFO lParam
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPOS
		{
			public HWND hwnd;
			public HWND hwndInsertAfter;
			public int  x;
			public int  y;
			public int  cx;
			public int  cy;
			public UINT flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEMTIME
		{
			public WORD wYear;
			public WORD wMonth;
			public WORD wDayOfWeek;
			public WORD wDay;
			public WORD wHour;
			public WORD wMinute;
			public WORD wSecond;
			public WORD wMilliseconds;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct FILETIME
		{
			[FieldOffset(0)] public DWORD dwLowDateTime;
			[FieldOffset(4)] public DWORD dwHighDateTime;
			[FieldOffset(0)] public long value;
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

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
		public struct WIN32_FIND_DATA
		{
			// Used by the FindFirstFile or FindNextFile functions.
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

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct COLORSCHEME
		{
			public DWORD    dwSize;
			public COLORREF clrBtnHighlight;       // highlight color
			public COLORREF clrBtnShadow;          // shadow color
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct PBRANGE
		{
			public int iLow;
			public int iHigh;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct CHARRANGE
		{
			/// <summary>First character of range (0 for start of doc)</summary>
			public int min;

			/// <summary>Last character of range (-1 for end of doc)</summary>
			public int max;

			public CHARRANGE(int mn, int mx) { min = mn; max = mx; }
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
		public struct FORMATRANGE
		{
			/// <summary>DC to draw on</summary>
			public IntPtr hdc;

			/// <summary>Target DC for determining text formatting</summary>
			public IntPtr hdcTarget;

			/// <summary>Region of the DC to draw to (in twips)</summary>
			public RECT rc;

			/// <summary>Region of the whole DC (page size) (in twips)</summary>
			public RECT rcPage;

			/// <summary>Range of text to draw (see earlier declaration)</summary>
			public CHARRANGE char_range;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
		public struct TEXTRANGE
		{
			/// <summary>The range of text to retrieve</summary>
			public CHARRANGE char_range;

			/// <summary>The buffer to receive the text</summary>
			[MarshalAs(UnmanagedType.LPWStr)]
			public string text; // Allocated by caller, zero terminated by RichEdit

			public TEXTRANGE(int min, int max)
			{
				char_range = new CHARRANGE(min, max);
				text = new string('\0', max - min);
			}
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
		public struct ICONINFO
		{
			public bool fIcon;
			public int xHotspot;
			public int yHotspot;
			public IntPtr hbmMask;
			public IntPtr hbmColor;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto, Pack=1)] 
		public struct COMBOBOXINFO
		{
			public Int32 cbSize;
			public RECT rcItem;
			public RECT rcButton;
			public int buttonState;
			public IntPtr hwndCombo;
			public IntPtr hwndEdit;
			public IntPtr hwndList;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BY_HANDLE_FILE_INFORMATION
		{
			public uint FileAttributes;
			public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
			public uint VolumeSerialNumber;
			public uint FileSizeHigh;
			public uint FileSizeLow;
			public uint NumberOfLinks;
			public uint FileIndexHigh;
			public uint FileIndexLow;
		};

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct CREATESTRUCT
		{
			public IntPtr lpCreateParams;
			public IntPtr hInstance;
			public IntPtr hMenu;
			public HWND hwndParent;
			public int cy;
			public int cx;
			public int y;
			public int x;
			public int style;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszName;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszClass;
			public uint dwExStyle;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WNDCLASS
		{
			public uint style;
			public WNDPROC lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WNDCLASSEX
		{
			public int cbSize; // = sizeof(WNDCLASSEX)
			public uint style;
			public WNDPROC lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
			[MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
			public IntPtr hIconSm;
		}
	}
}
