using System;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		[DllImport("ole32.dll")] public static extern void CoTaskMemFree(IntPtr ptr);
	}
}
