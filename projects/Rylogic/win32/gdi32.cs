using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace pr.win32
{
	public static partial class Win32
	{
		[DllImport("gdi32.dll")] public static extern int  SetGraphicsMode(IntPtr hdc, int iGraphicsMode);
		[DllImport("gdi32.dll")] public static extern int  SetMapMode(IntPtr hdc, int fnMapMode);
		[DllImport("gdi32.dll")] public static extern bool SetWorldTransform(IntPtr hdc, ref XFORM lpXform);
		[DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
	}
}
