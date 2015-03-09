using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace pr.win32
{
	public static partial class Win32
	{
		[DllImport("Kernel32.dll", SetLastError = true)] public static extern IntPtr LoadLibrary(string path);
		[DllImport("Kernel32.dll", SetLastError = true)] public static extern bool   FreeLibrary(IntPtr module);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AllocConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   FreeConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AttachConsole(int dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   WriteConsole(IntPtr hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern uint   FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr pArguments);
	}
}
