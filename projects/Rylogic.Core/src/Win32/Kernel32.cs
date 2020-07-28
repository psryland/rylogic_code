using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#pragma warning disable CA1401 // P/Invokes should not be visible
namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		public const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr SecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		[DllImport("Kernel32.dll", SetLastError = true, EntryPoint = "LoadLibraryW")] public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)]string path);
		[DllImport("Kernel32.dll", SetLastError = true)] public static extern bool   FreeLibrary(IntPtr module);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AllocConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   FreeConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AttachConsole(int dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteConsoleW")] public static extern bool WriteConsole(IntPtr hConsoleOutput, [MarshalAs(UnmanagedType.LPWStr)] string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern uint   FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr pArguments);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   SystemTimeToFileTime([In] ref SYSTEMTIME st, [Out] out FILETIME ft);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   FileTimeToSystemTime([In] ref FILETIME ft, [Out] out SYSTEMTIME st);
	}
}
#pragma warning restore CA1401 // P/Invokes should not be visible
