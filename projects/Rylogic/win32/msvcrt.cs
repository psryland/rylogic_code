using System;
using System.Runtime.InteropServices;

namespace pr.win32
{
	public static partial class Win32
	{
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)] public static extern IntPtr memset(IntPtr dest, int c, int count);
	}
}
