using System;
using System.Runtime.InteropServices;
using Rylogic.Interop.Win32;
using Rylogic.Utility;

namespace Rylogic.Gfx
{
	public static class Dpi
	{
		// Notes:
		//  - Assumes DPIX == DPIY
		public enum EMonitorDpiType
		{
			Effective = 0,
			Angular = 1,
			Raw = 2,
		}

		// Dynamically loaded function signatures
		private delegate int GetDpiForWindowFn(IntPtr hwnd);
		private delegate int GetDpiForMonitorFn(IntPtr hmonitor, EMonitorDpiType dpiType, out int dpiX, out int dpiY);

		/// <summary>Get the DPI for a given window</summary>
		public static int DpiForWindow(IntPtr hwnd)
		{
			// Notes:
			//  - This should be the first method to use with fall-back to nearest monitor
			// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow
			var h = Kernel32.LoadLibrary("user32.dll");
			var ptr = Kernel32.GetProcAddress(h, "GetDpiForWindow"); // Windows 10 1607
			return ptr != IntPtr.Zero
				? Marshal.GetDelegateForFunctionPointer<GetDpiForWindowFn>(ptr)(hwnd)
				: DpiForNearestMonitor(hwnd);
		}

		/// <summary>Get the DPI for a monitor</summary>
		public static int DpiForMonitor(IntPtr monitor, EMonitorDpiType type = EMonitorDpiType.Effective)
		{
			var h = Kernel32.LoadLibrary("shcore.dll");
			var ptr = Kernel32.GetProcAddress(h, "GetDpiForMonitor"); // Windows 8.1
			if (ptr == IntPtr.Zero)
				return DpiForDesktop();

			var hr = Marshal.GetDelegateForFunctionPointer<GetDpiForMonitorFn>(ptr)(monitor, type, out var x, out var y);
			if (hr < 0)
				return DpiForDesktop();

			return x;
		}
		public static int DpiForNearestMonitor(IntPtr hwnd) => DpiForMonitor(User32.MonitorFromWindow(hwnd));
		public static int DpiForNearestMonitor(int x, int y) => DpiForMonitor(User32.MonitorFromPoint(x, y));

		/// <summary>Get the DPI setting for the whole desktop</summary>
		public static int DpiForDesktop()
		{
			var hr = D2D1CreateFactory(D2D1_FACTORY_TYPE.SINGLE_THREADED, typeof(ID2D1Factory).GUID, IntPtr.Zero, out var factory);
			if (hr < 0)
				return 96; // we really hit the ground, don't know what to do next!

			factory.GetDesktopDpi(out var x, out var y); // Windows 7
			Marshal.ReleaseComObject(factory);
			return (int)x;
		}

		/// <summary>Return the monitor associated with the desktop window</summary>
		public static IntPtr DesktopMonitor => User32.MonitorFromWindow(User32.GetDesktopWindow());

		/// <summary>Return the monitor associated with the shell window</summary>
		public static IntPtr ShellMonitor => User32.MonitorFromWindow(User32.GetShellWindow());

		#region D2D1 Interop

		private enum D2D1_FACTORY_TYPE
		{
			SINGLE_THREADED = 0,
			MULTI_THREADED = 1,
		}

		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("06152247-6f50-465a-9245-118bfd3b6007")]
		private interface ID2D1Factory
		{
			int ReloadSystemMetrics();

			[PreserveSig]
			void GetDesktopDpi(out float dpiX, out float dpiY);

			// The rest is not implemented as we don't need it
		}

		/// <summary>D2D1 interop functions</summary>
		[DllImport("d2d1")]
		private static extern int D2D1CreateFactory(D2D1_FACTORY_TYPE factoryType, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, IntPtr pFactoryOptions, out ID2D1Factory ppIFactory);

		#endregion
	}
}