using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace pr.win32
{
	public static partial class Win32
	{
		[DllImport("ole32.dll")] public static extern void CoTaskMemFree(IntPtr ptr);
	}
}
