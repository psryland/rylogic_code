using System;
using System.Runtime.InteropServices;
using Rylogic.Interop.Win32;

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
			//  - This should be the first method to use with fallback to nearest monitor
			// https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow
			var h = Win32.LoadLibrary("user32.dll");
			var ptr = Win32.GetProcAddress(h, "GetDpiForWindow"); // Windows 10 1607
			return ptr != IntPtr.Zero
				? Marshal.GetDelegateForFunctionPointer<GetDpiForWindowFn>(ptr)(hwnd)
				: DpiForNearestMonitor(hwnd);
		}

		/// <summary>Get the DPI for a monitor</summary>
		public static int DpiForMonitor(IntPtr monitor, EMonitorDpiType type = EMonitorDpiType.Effective)
		{
			var h = Win32.LoadLibrary("shcore.dll");
			var ptr = Win32.GetProcAddress(h, "GetDpiForMonitor"); // Windows 8.1
			if (ptr == IntPtr.Zero)
				return DpiForDesktop();

			var hr = Marshal.GetDelegateForFunctionPointer<GetDpiForMonitorFn>(ptr)(monitor, type, out int x, out int y);
			if (hr < 0)
				return DpiForDesktop();

			return x;
		}
		public static int DpiForNearestMonitor(IntPtr hwnd) => DpiForMonitor(Win32.MonitorFromWindow(hwnd));
		public static int DpiForNearestMonitor(int x, int y) => DpiForMonitor(Win32.MonitorFromPoint(x, y));

		/// <summary>Get the DPI setting for the whole desktop</summary>
		public static int DpiForDesktop()
		{
			var hr = D2D1CreateFactory(D2D1_FACTORY_TYPE.SINGLE_THREADED, typeof(ID2D1Factory).GUID, IntPtr.Zero, out ID2D1Factory factory);
			if (hr < 0)
				return 96; // we really hit the ground, don't know what to do next!

			factory.GetDesktopDpi(out float x, out float y); // Windows 7
			Marshal.ReleaseComObject(factory);
			return (int)x;
		}

		/// <summary>Return the monitor associated with the desktop window</summary>
		public static IntPtr DesktopMonitor => Win32.MonitorFromWindow(Win32.GetDesktopWindow());

		/// <summary>Return the monitor associated with the shell window</summary>
		public static IntPtr ShellMonitor => Win32.MonitorFromWindow(Win32.GetShellWindow());

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