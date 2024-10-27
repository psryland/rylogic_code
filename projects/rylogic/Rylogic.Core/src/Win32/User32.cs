using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Rylogic.Extn;
using Rylogic.Utility;
using HDC = System.IntPtr;
using HICON = System.IntPtr;
using HRGN = System.IntPtr;
using HWND = System.IntPtr;
using LPARAM = System.IntPtr;
using WPARAM = System.IntPtr;

namespace Rylogic.Interop.Win32
{
	public static partial class Win32
	{
		[Flags]
		public enum EPeekMessageFlags
		{
			/// <summary>PM_NOREMOVE - Messages are not removed from the queue after processing by PeekMessage.</summary>
			NoRemove = 0x0000,

			/// <summary>PM_REMOVE - Messages are removed from the queue after processing by PeekMessage.</summary>
			Remove = 0x0001,

			/// <summary>PM_NOYIELD - Prevents the system from releasing any thread that is waiting for the caller to go idle (see WaitForInputIdle). Combine this value with either PM_NOREMOVE or PM_REMOVE.</summary>
			NoYield = 0x0002,
		}

		/// <summary></summary>
		[Flags]
		public enum EDeviceNotifyFlags
		{
			/// <summary>DEVICE_NOTIFY_WINDOW_HANDLE - The hRecipient parameter is a window handle.</summary>
			WindowHandle = 0x00000000,

			/// <summary>DEVICE_NOTIFY_SERVICE_HANDLE - The hRecipient parameter is a service status handle.</summary>
			ServiceHandle = 0x00000001,

			/// <summary>
			/// DEVICE_NOTIFY_ALL_INTERFACE_CLASSES - Notifies the recipient of device interface events for all
			/// device interface classes (the 'dbcc_classguid' member is ignored). This value can be used only if the 
			/// 'dbch_devicetype' member is 'DBT_DEVTYP_DEVICEINTERFACE'.</summary>
			AllInterface_Classes = 0x00000004,
		}

		/// <summary>DEV_BROADCAST_HDR structure types</summary>
		public enum EDeviceBroadcaseType :uint
		{
			/// <summary>DBT_DEVTYP_OEM - OEM- or IHV-defined device type. This structure is a DEV_BROADCAST_OEM structure.</summary>
			OEM = 0x00000000,

			/// <summary>DBT_DEVTYP_VOLUME - Logical volume.This structure is a DEV_BROADCAST_VOLUME structure.</summary>
			Volume = 0x00000002,

			/// <summary>DBT_DEVTYP_PORT - Port device (serial or parallel). This structure is a DEV_BROADCAST_PORT structure.</summary>
			Port = 0x00000003,

			/// <summary>DBT_DEVTYP_DEVICEINTERFACE - Class of devices. This structure is a DEV_BROADCAST_DEVICEINTERFACE structure.</summary>
			DeviceInterface = 0x00000005,

			/// <summary>DBT_DEVTYP_HANDLE - File system handle. This structure is a DEV_BROADCAST_HANDLE structure.</summary>
			Handle = 0x00000006,
		}

		/// <summary>Flags for the MonitorFromPoint function</summary>
		public enum EMonitorFromFlags
		{
			DEFAULT_TO_NULL = 0x00000000,
			DEFAULT_TO_PRIMARY = 0x00000001,
			DEFAULT_TO_NEAREST = 0x00000002,
		}

		/// <summary></summary>
		[Flags]
		public enum EFlashWindowFlags :uint
		{
			/// <summary>Stop flashing. The system restores the window to its original state.</summary>
			FLASHW_STOP = 0,

			/// <summary>Flash the window caption.</summary>
			FLASHW_CAPTION = 1,

			/// <summary>Flash the taskbar button.</summary>
			FLASHW_TRAY = 2,

			/// <summary>Flash both the window caption and taskbar button. This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.</summary>
			FLASHW_ALL = 3,

			/// <summary>Flash continuously, until the FLASHW_STOP flag is set.</summary>
			FLASHW_TIMER = 4,

			/// <summary>Flash continuously until the window comes to the foreground.</summary>
			FLASHW_TIMERNOFG = 12,
		}

		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct DEV_BROADCAST_HDR
		{
			/// <summary>
			/// The size of this structure, in bytes. If this is a user-defined event, this member must be the size of
			/// this header, plus the size of the variable-length data in the _DEV_BROADCAST_USERDEFINED structure.</summary>
			public int dbch_size;
			public EDeviceBroadcaseType dbch_devicetype;
			public int dbch_reserved;
		}
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DEV_BROADCAST_DEVICEINTERFACE
		{
			private const int NameLen = 255;

			public DEV_BROADCAST_HDR hdr;
			public Guid class_guid;
			public string name
			{
				get
				{
					return name_?.IndexOf('\0') is int end && end != -1
						? new string(name_, 0, end) : string.Empty;
				}
				set
				{
					name_ = new char[NameLen];
					var len = Math.Min(value.Length, 255);
					Array.Copy(value.ToCharArray(), name_, len);
				}
			}
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = NameLen)] private char[] name_;
		}


		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate IntPtr WNDPROC(HWND hwnd, int code, IntPtr wparam, IntPtr lparam);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate bool EnumWindowsProc(HWND hwnd, int lParam);

		/// <summary>Return the DC for a window</summary>
		public static Scope<IntPtr> WindowDC(HWND hwnd)
		{
			return Scope.Create(
				() => GetDC(hwnd),
				dc => ReleaseDC(hwnd, dc));
		}

		/// <summary></summary>
		public static bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem) => AppendMenu_(hMenu, uFlags, uIDNewItem, lpNewItem);
		public static bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem) => AppendMenu_(hMenu, uFlags, uIDNewItem, lpNewItem);
		[DllImport("user32.dll", EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode)]
		private static extern bool AppendMenu_(IntPtr hMenu, uint uFlags, int uIDNewItem, [MarshalAs(UnmanagedType.LPWStr)] string lpNewItem);
		[DllImport("user32.dll", EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode)]
		private static extern bool AppendMenu_(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, [MarshalAs(UnmanagedType.LPWStr)] string lpNewItem);

#pragma warning disable CA1401 // P/Invokes should not be visible
		[DllImport("user32.dll")]
		public static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);

		[DllImport("user32.dll")]
		public static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

		[DllImport("user32.dll", EntryPoint = "CheckMenuItem")]
		public static extern int CheckMenuItem(IntPtr hMenu, int uIDCheckItem, int uCheck);

		[DllImport("user32.dll")]
		public static extern HWND ChildWindowFromPointEx(HWND parent, POINT point, int flags);

		[DllImport("user32.dll")]
		public static extern bool ClientToScreen(HWND hwnd, ref POINT pt);

		[DllImport("user32.dll")]
		public static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

		[DllImport("user32.dll")]
		public static extern IntPtr CreatePopupMenu();

		/// <summary>Create a new window instance</summary>
		public static HWND CreateWindow(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam)
		{
			return CreateWindow_(dwExStyle, lpClassName, lpWindowName, dwStyle, x, y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
		}
		public static HWND CreateWindow(int dwExStyle, int lpClassAtom, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam)
		{
			return CreateWindow_(dwExStyle, (IntPtr)lpClassAtom, lpWindowName, dwStyle, x, y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
		}
		[DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern HWND CreateWindow_(int dwExStyle, [MarshalAs(UnmanagedType.LPWStr)] string lpClassName, [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
		[DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern HWND CreateWindow_(int dwExStyle, IntPtr lpClassAtom, [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, HWND hWnd, int Msg, WPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr DefWindowProc(HWND hWnd, int Msg, WPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool DestroyIcon(HICON hicon);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern bool DestroyWindow(HWND hwnd);

		/// <summary></summary>
		public static int DispatchMessage(ref Message msg) => DispatchMessage(ref msg);
		[DllImport("user32.dll", EntryPoint = "DispatchMessageW", CharSet = CharSet.Unicode)]
		private static extern int DispatchMessage_(ref Message lpMsg);

		/// <summary>Draw an icon into an HDC</summary>
		public static void DrawIcon(HDC hDC, int X, int Y, HICON hIcon)
		{
			if (!DrawIcon_(hDC, X, Y, hIcon))
				throw new Win32Exception("DrawIcon failed");
		}
		[DllImport("user32.dll", EntryPoint = "DrawIcon", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool DrawIcon_(HDC hDC, int X, int Y, HICON hIcon);

		[DllImport("user32.dll")]
		public static extern int EnumWindows(EnumWindowsProc ewp, int lParam);

		/// <summary>Flash an application window</summary>
		public static bool FlashWindow(HWND hwnd, EFlashWindowFlags flags, uint count = uint.MaxValue, uint flash_rate = 0)
		{
			var info = new FLASHWINFO
			{
				cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
				hwnd = hwnd,
				dwFlags = (uint)flags,
				uCount = count,
				dwTimeout = flash_rate,
			};
			return FlashWindowEx_(ref info);
		}
		[DllImport("user32.dll", EntryPoint = "FlashWindowEx")]
		private static extern bool FlashWindowEx_(ref FLASHWINFO pwfi);

		[DllImport("user32.dll")]
		public static extern HWND GetAncestor(HWND hwnd, uint flags);

		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(EKeyCodes vKey);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetCaretPos(ref POINT point);

		/// <summary>Get the window class description for a window class name</summary>
		/// <param name="hinstance">A handle to the instance of the application that created the class. To retrieve information about classes
		/// defined by the system (such as buttons or list boxes),set this parameter to NULL.</param>
		/// <param name="class_name">The name of the window class to look up</param>
		/// <returns>Returns the window class data if the window class is registered</returns>
		public static WNDCLASSEX? GetClassInfo(IntPtr hinstance, string class_name, out int atom)
		{
			var wc = new WNDCLASSEX { cbSize = Marshal.SizeOf<WNDCLASSEX>() };
			atom = GetClassInfoEx_(hinstance, class_name, ref wc);
			return atom != 0 ? wc : (WNDCLASSEX?)null;
		}
		[DllImport("user32.dll", EntryPoint = "GetClassInfoExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern int GetClassInfoEx_(IntPtr hinstance, [MarshalAs(UnmanagedType.LPWStr)] string class_name, ref WNDCLASSEX lpwcx);

		[DllImport("user32.dll", EntryPoint = "GetClassLongPtrW", SetLastError = true)]
		public static extern IntPtr GetClassLongPtr(HWND hwnd, int index);

		/// <summary></summary>
		public static RECT GetClientRect(HWND hwnd)
		{
			return GetClientRect_(hwnd, out var rect) ? rect : throw new Win32Exception("GetClientRect failed"); ;
		}
		[DllImport("user32.dll", EntryPoint = "GetClientRect")]
		private static extern bool GetClientRect_(HWND hwnd, out RECT rect);

		/// <summary>Return the mouse position in screen coordinates</summary>
		public static POINT GetCursorPos()
		{
			return GetCursorPos_(out var pt) ? pt : throw new Win32Exception("GetCursorPos failed");
		}
		[DllImport("user32.dll", EntryPoint = "GetCursorPos")]
		private static extern bool GetCursorPos_(out POINT lpPoint);

		/// <summary>Return info about the current mouse cursor</summary>
		public static CURSORINFO GetCursorInfo()
		{
			var info = CURSORINFO.Default;
			GetCursorInfo_(ref info);
			return info;
		}
		[DllImport("user32.dll", EntryPoint = "GetCursorInfo", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool GetCursorInfo_(ref CURSORINFO pci);

		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(HWND hwnd);

		/// <summary></summary>
		public static IntPtr GetDesktopWindow() => GetDesktopWindow_();
		[DllImport("user32", EntryPoint = "GetDesktopWindow")]
		private static extern IntPtr GetDesktopWindow_();

		[DllImport("user32.dll")]
		public static extern int GetDoubleClickTime();

		[DllImport("user32.dll")]
		public static extern HWND GetFocus();

		[DllImport("user32.dll")]
		public static extern HWND GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern bool GetIconInfo(IntPtr hIcon, ref ICONINFO pIconInfo);

		[DllImport("user32.dll")]
		public static extern int GetKeyboardState(byte[] pbKeyState);

		[DllImport("user32.dll")]
		public static extern short GetKeyState(EKeyCodes vKey);

		/// <summary></summary>
		public static int GetMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax)
		{
			return GetMessage_(out msg, hWnd, messageFilterMin, messageFilterMax);
		}
		[DllImport("user32.dll", EntryPoint = "GetMessageW", CharSet = CharSet.Unicode)]
		private static extern int GetMessage_(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax);

		[DllImport("user32.dll")]
		public static extern int GetMessageTime();

		/// <summary>Return info about the given monitor handle</summary>
		public static MONITORINFOEX GetMonitorInfo(IntPtr hMonitor)
		{
			var info = new MONITORINFOEX { cbSize = (uint)Marshal.SizeOf<MONITORINFOEX>() };
			return GetMonitorInfoW_(hMonitor, ref info) ? info : throw new Win32Exception("Monitor info not available");
		}
		[DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
		private static extern bool GetMonitorInfoW_(IntPtr hMonitor, ref MONITORINFOEX info);

		/// <summary></summary>
		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern IntPtr GetNextDlgTabItem(IntPtr hDlg, IntPtr hCtl, [MarshalAs(UnmanagedType.Bool)] bool bPrevious);

		[DllImport("user32.dll")]
		public static extern HWND GetParent(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool GetScrollInfo(HWND hwnd, int BarType, ref SCROLLINFO lpsi);

		[DllImport("user32.dll")]
		public static extern int GetScrollPos(HWND hWnd, int nBar);

		/// <summary></summary>
		public static IntPtr GetShellWindow() => GetShellWindow_();
		[DllImport("user32", EntryPoint = "GetShellWindow")]
		private static extern IntPtr GetShellWindow_();

		[DllImport("user32.dll")]
		public static extern IntPtr GetSystemMenu(HWND hwnd, bool bRevert);

		/// <summary>System metrics</summary>
		public static int GetSystemMetrics(ESystemMetrics metric)
		{
			return GetSystemMetrics_((int)metric);
		}
		[DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
		private static extern int GetSystemMetrics_(int nIndex);

		[DllImport("user32.dll")]
		public static extern bool GetUpdateRect(HWND hwnd, out Win32.RECT rect, bool erase);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowLong(HWND hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern long GetWindowLongPtr(HWND hWnd, int nIndex); // This is only defined in 64bit builds, otherwise it's a #define to GetWindowLong

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowModuleFileName(HWND hwnd, StringBuilder title, int size);

		/// <summary>Return the area of a window in screen space</summary>
		public static RECT GetWindowRect(HWND hwnd)
		{
			return GetWindowRect_(hwnd, out var rect) ? rect : throw new Win32Exception("GetWindowRect failed");
		}
		[DllImport("user32.dll", EntryPoint = "GetWindowRect")]
		private static extern bool GetWindowRect_(HWND hwnd, out RECT rect);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(HWND hwnd, StringBuilder title, int size);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetWindowThreadProcessId(HWND hWnd, ref IntPtr lpdwProcessId);

		[DllImport("user32.dll")]
		public static extern int HideCaret(HWND hwnd);

		[DllImport("user32.dll", EntryPoint = "InsertMenu", CharSet = CharSet.Unicode)]
		public static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);

		[DllImport("user32.dll", EntryPoint = "InsertMenu", CharSet = CharSet.Unicode)]
		public static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, IntPtr wIDNewItem, string lpNewItem);

		[DllImport("user32.dll")]
		public static extern bool InvalidateRect(HWND hwnd, IntPtr lpRect, bool bErase);

		[DllImport("user32.dll")]
		public static extern bool InvalidateRect(HWND hwnd, ref Win32.RECT lpRect, bool bErase);

		[DllImport("user32.dll")]
		public static extern bool IsChild(HWND parent, HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool IsIconic(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool IsWindow(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool IsWindowVisible(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool IsZoomed(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool LockWindowUpdate(HWND hWndLock);

		[DllImport("user32.dll")]
		public static extern IntPtr LoadCursorFromFile([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint uCode, uint uMapType);

		/// <summary>Return the monitor that contains the given point</summary>
		public static IntPtr MonitorFromPoint(int x, int y, EMonitorFromFlags flags = EMonitorFromFlags.DEFAULT_TO_NEAREST) => MonitorFromPoint(new POINT { X = x, Y = y }, flags);
		public static IntPtr MonitorFromPoint(POINT pt, EMonitorFromFlags flags = EMonitorFromFlags.DEFAULT_TO_NEAREST) => MonitorFromPoint_(pt, (int)flags);
		[DllImport("user32", EntryPoint = "MonitorFromPoint")]
		private static extern IntPtr MonitorFromPoint_(POINT pt, int flags);

		/// <summary>Return the monitor that contains the given rectangle</summary>
		public static IntPtr MonitorFromRect(RECT rc, EMonitorFromFlags flags = EMonitorFromFlags.DEFAULT_TO_NEAREST) => MonitorFromRect_(rc, (int)flags);
		[DllImport("user32", EntryPoint = "MonitorFromRect")]
		private static extern IntPtr MonitorFromRect_(RECT rc, int flags);

		/// <summary>Return the monitor that contains the given window handle</summary>
		public static IntPtr MonitorFromWindow(IntPtr hwnd, EMonitorFromFlags flags = EMonitorFromFlags.DEFAULT_TO_NEAREST) => MonitorFromWindow_(hwnd, (int)flags);
		[DllImport("user32", EntryPoint = "MonitorFromWindow")]
		private static extern IntPtr MonitorFromWindow_(IntPtr hwnd, int flags);

		[DllImport("user32.dll")]
		public static extern bool MoveWindow(HWND hWnd, int X, int Y, int nWidth, int nHeight, bool repaint);

		/// <summary></summary>
		public static bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, EPeekMessageFlags flags)
		{
			return PeekMessage_(out msg, hWnd, messageFilterMin, messageFilterMax, (int)flags);
		}
		[DllImport("user32.dll", EntryPoint = "PeekMessageW", CharSet = CharSet.Unicode)]
		private static extern bool PeekMessage_(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, int flags);

		/// <summary></summary>
		public static bool PostMessage(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam)
		{
			return PostMessage_(hwnd, message, wparam, lparam);
		}
		[DllImport("user32.dll", EntryPoint = "PostMessageW", CharSet = CharSet.Unicode)]
		private static extern bool PostMessage_(IntPtr hwnd, uint message, IntPtr wparam, IntPtr lparam);

		[DllImport("user32.dll", EntryPoint = "PostThreadMessage")]
		public static extern int PostThreadMessage(int idThread, uint msg, int wParam, int lParam);

		[DllImport("user32.dll")]
		public static extern bool RedrawWindow(HWND hWnd, ref RECT lprcUpdate, HRGN hrgnUpdate, uint flags);
		
		[DllImport("user32.dll")]
		public static extern bool RedrawWindow(HWND hWnd, IntPtr lprcUpdate, HRGN hrgnUpdate, uint flags);

		/// <summary>Register a window class</summary>
		public static ushort RegisterClass(WNDCLASS wnd_class)
		{
			// RegisterClass only sets last error if there is an error
			Win32.SetLastError(Win32.ERROR_SUCCESS);
			var atom = RegisterClass_(ref wnd_class);
			var err = Marshal.GetLastWin32Error();
			if (err != Win32.ERROR_SUCCESS) throw new Win32Exception();
			return atom;
		}
		public static ushort RegisterClass(WNDCLASSEX wnd_class)
		{
			// RegisterClass only sets last error if there is an error
			Win32.SetLastError(Win32.ERROR_SUCCESS);
			var atom = RegisterClassEx_(ref wnd_class);
			var err = Marshal.GetLastWin32Error();
			if (err != Win32.ERROR_SUCCESS) throw new Win32Exception(); 
			return atom;
		}
		[DllImport("user32.dll", EntryPoint = "RegisterClassW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern ushort RegisterClass_(ref WNDCLASS lpWndClass);
		[DllImport("user32.dll", EntryPoint = "RegisterClassExW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern ushort RegisterClassEx_(ref WNDCLASSEX lpWndClass);

		/// <summary>Registry for notifications about devices being adding/removed</summary>
		public static SafeDevNotifyHandle RegisterDeviceNotification(IntPtr recipient, DEV_BROADCAST_HDR notificationFilter, EDeviceNotifyFlags flags)
		{
			// This function expects a contiguous struct with a 'DEV_BROADCAST_HDR'
			// as the header. Create overloads for any new struct types.
			var handle = RegisterDeviceNotification_(recipient, ref notificationFilter, (int)flags);
			return new SafeDevNotifyHandle(handle, true);
		}
		public static SafeDevNotifyHandle RegisterDeviceNotification(IntPtr recipient, DEV_BROADCAST_DEVICEINTERFACE notificationFilter, EDeviceNotifyFlags flags)
		{
			var handle = RegisterDeviceNotification_(recipient, ref notificationFilter, (int)flags);
			return new SafeDevNotifyHandle(handle, true);
		}
		[DllImport("user32.dll", EntryPoint = "RegisterDeviceNotificationW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr RegisterDeviceNotification_(IntPtr recipient, ref DEV_BROADCAST_HDR filter, int flags);
		[DllImport("user32.dll", EntryPoint = "RegisterDeviceNotificationW", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr RegisterDeviceNotification_(IntPtr recipient, ref DEV_BROADCAST_DEVICEINTERFACE filter, int flags);

		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW")]
		public static extern uint RegisterWindowMessage([MarshalAs(UnmanagedType.LPWStr)] string lpString);

		[DllImport("user32.dll")]
		public static extern bool ReleaseDC(HWND hWnd, IntPtr hDC);

		[DllImport("user32.dll")]
		public static extern bool ScreenToClient(HWND hwnd, ref POINT pt);

		[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
		public static extern IntPtr SendMessage(HWND hwnd, uint msg, int wparam, int lparam); // Don't return int, it truncates on 64bit

		[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
		public static extern IntPtr SendMessage(HWND hwnd, int msg, IntPtr wparam, IntPtr lparam);

		[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
		public static extern IntPtr SendMessage(HWND hwnd, uint msg, IntPtr wparam, IntPtr lparam);

		[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
		public static extern IntPtr SendMessage(HWND hwnd, uint msg, IntPtr wparam, ref POINT lparam);

		[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
		public static extern IntPtr SendMessage(HWND hwnd, uint msg, IntPtr wparam, ref RECT lparam);

		[DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true)]
		public static extern IntPtr SendMessage(HWND hwnd, uint msg, IntPtr wparam, out COMBOBOXINFO lparam);

		[DllImport("user32.dll")]
		public static extern HWND SetActiveWindow(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern IntPtr SetCursor(IntPtr cursor);

		[DllImport("user32.dll")]
		public static extern HWND SetFocus(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(HWND hwnd);

		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(HWND hWndChild, HWND hWndNewParent);

		[DllImport("user32.dll")]
		public static extern bool SetProcessDPIAware();

		[DllImport("user32.dll")]
		public static extern int SetScrollInfo(HWND hwnd, int fnBar, ref SCROLLINFO lpsi, bool fRedraw);

		[DllImport("user32.dll")]
		public static extern int SetScrollPos(HWND hWnd, int nBar, int nPos, bool bRedraw);

		[DllImport("user32.dll")]
		public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(HWND hWnd, int nIndex, uint dwNewLong);

		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll")]
		public static extern int ShowCaret(IntPtr hwnd);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool ShowWindowAsync(HWND hwnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);

		[DllImport("user32.dll")]
		public static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

		[DllImport("user32.dll")]
		public static extern bool TranslateMessage(ref Message lpMsg);

		[DllImport("user32.dll")]
		public static extern int UnhookWindowsHookEx(int idHook);

		/// <summary>Release a device notification handle</summary>
		public static bool UnregisterDeviceNotification(IntPtr handle) => UnregisterDeviceNotification_(handle);
		[DllImport("user32.dll", EntryPoint = "UnregisterDeviceNotification", SetLastError = true)]
		private static extern bool UnregisterDeviceNotification_(IntPtr handle);

		[DllImport("user32.dll")]
		public static extern HWND WindowFromPoint(POINT Point);

		[DllImport("user32.dll")]
		public static extern bool ValidateRect(HWND hwnd, IntPtr lpRect);

		[DllImport("user32.dll")]
		public static extern bool ValidateRect(HWND hwnd, ref RECT lpRect);

		// This static method is required because legacy OSes do not support SetWindowLongPtr 
		public static IntPtr SetWindowLongPtr(HWND hWnd, int nIndex, IntPtr dwNewLong)
		{
			return IntPtr.Size == 8
				? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
				: new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
		}
		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		private static extern int SetWindowLong32(HWND hWnd, int nIndex, int dwNewLong);
		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr64(HWND hWnd, int nIndex, IntPtr dwNewLong);


		[DllImport("uxtheme.dll")]
		public static extern int SetWindowTheme(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string appname, [MarshalAs(UnmanagedType.LPWStr)] string idlist);

		#pragma warning restore CA1401 // P/Invokes should not be visible
	}
	
	public sealed class SafeDevNotifyHandle :SafeHandleZeroOrMinusOneIsInvalid
	{
		public SafeDevNotifyHandle(IntPtr handle, bool owns_handle) : base(owns_handle) => SetHandle(handle);
		protected override bool ReleaseHandle() => Win32.UnregisterDeviceNotification(DangerousGetHandle());
	}
}
