﻿using System;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		public const int ATTACH_PARENT_PROCESS = -1;

		[DllImport("Kernel32.dll", SetLastError = true)] public static extern IntPtr LoadLibrary(string path);
		[DllImport("Kernel32.dll", SetLastError = true)] public static extern bool   FreeLibrary(IntPtr module);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AllocConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   FreeConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AttachConsole(int dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   WriteConsole(IntPtr hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern uint   FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr pArguments);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   SystemTimeToFileTime([In] ref SYSTEMTIME st, [Out] out FILETIME ft);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   FileTimeToSystemTime([In] ref FILETIME ft, [Out] out SYSTEMTIME st);
	}
}