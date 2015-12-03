using System;
using System.Runtime.InteropServices;
using pr.util;
using HDC = System.IntPtr;

namespace pr.win32
{
	public static partial class Win32
	{
		/// <summary>
		/// Specifies a raster-operation code. These codes define how the colour data for the
		/// source rectangle is to be combined with the colour data for the destination
		/// rectangle to achieve the final colour.</summary>
		public enum TernaryRasterOperations :uint
		{
			/// <summary>dest = source</summary>
			SRCCOPY = 0x00CC0020,

			/// <summary>dest = source OR dest</summary>
			SRCPAINT = 0x00EE0086,

			/// <summary>dest = source AND dest</summary>
			SRCAND = 0x008800C6,

			/// <summary>dest = source XOR dest</summary>
			SRCINVERT = 0x00660046,

			/// <summary>dest = source AND (NOT dest)</summary>
			SRCERASE = 0x00440328,

			/// <summary>dest = (NOT source)</summary>
			NOTSRCCOPY = 0x00330008,

			/// <summary>dest = (NOT src) AND (NOT dest)</summary>
			NOTSRCERASE = 0x001100A6,

			/// <summary>dest = (source AND pattern)</summary>
			MERGECOPY = 0x00C000CA,

			/// <summary>dest = (NOT source) OR dest</summary>
			MERGEPAINT = 0x00BB0226,

			/// <summary>dest = pattern</summary>
			PATCOPY    = 0x00F00021,

			/// <summary>dest = DPSnoo</summary>
			PATPAINT = 0x00FB0A09,

			/// <summary>dest = pattern XOR dest</summary>
			PATINVERT = 0x005A0049,

			/// <summary>dest = (NOT dest)</summary>
			DSTINVERT = 0x00550009,

			/// <summary>dest = BLACK</summary>
			BLACKNESS = 0x00000042,

			/// <summary>dest = WHITE</summary>
			WHITENESS = 0x00FF0062,

			/// <summary>
			/// Capture window as seen on screen.  This includes layered windows 
			/// such as WPF windows with AllowsTransparency="true"</summary>
			CAPTUREBLT = 0x40000000
		}

		/// <summary>A scope for selecting a GDI object</summary>
		public static Scope SelectObjectScope(IntPtr hdc, IntPtr obj)
		{
			return Scope.Create<IntPtr>(
				() => SelectObject(hdc, obj),
				ob => SelectObject(hdc, ob));
		} 

		[DllImport("gdi32.dll")]                              public static extern IntPtr CreateSolidBrush(uint color);
		[DllImport("gdi32.dll")]                              public static extern int    SetGraphicsMode(HDC hdc, int iGraphicsMode);
		[DllImport("gdi32.dll")]                              public static extern int    SetMapMode(HDC hdc, int fnMapMode);
		[DllImport("gdi32.dll")]                              public static extern bool   SetWorldTransform(HDC hdc, ref XFORM lpXform);
		[DllImport("gdi32.dll", EntryPoint = "SelectObject")] public static extern HDC    SelectObject(HDC hdc, IntPtr hgdiobj);
		[DllImport("gdi32.dll")]                              public static extern bool   DeleteObject(HDC hObject);
		[DllImport("gdi32.dll")]                              public static extern bool   PatBlt(HDC hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, TernaryRasterOperations dwRop);
	}
}
