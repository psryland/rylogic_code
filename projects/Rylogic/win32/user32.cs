using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace pr.win32
{
	using UINT = System.UInt32;
	using HWND = System.IntPtr;
	using HRGN = System.IntPtr;
	using WPARAM = System.IntPtr;
	using LPARAM = System.IntPtr;
	using util;

	public static partial class Win32
	{
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate IntPtr WNDPROC(HWND hwnd, int code, IntPtr wparam, IntPtr lparam);

		/// <summary>Return the DC for a window</summary>
		public static Scope<IntPtr> WindowDC(HWND hwnd)
		{
			return Scope<IntPtr>.Create(
				() => GetDC(hwnd),
				dc => ReleaseDC(hwnd, dc));
		}

		// User32 (in alphabetical order)
		[DllImport("user32.dll", EntryPoint="AppendMenuW", CharSet=CharSet.Unicode)]                          public static extern bool   AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);
		[DllImport("user32.dll", EntryPoint="AppendMenuW", CharSet=CharSet.Unicode)]                          public static extern bool   AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);
		[DllImport("user32.dll")]                                                                             public static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);
		[DllImport("user32.dll")]                                                                             public static extern int    CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint="CheckMenuItem")]                                                 public static extern int    CheckMenuItem(IntPtr hMenu,int uIDCheckItem, int uCheck);
		[DllImport("user32.dll")]                                                                             public static extern HWND   ChildWindowFromPointEx(HWND parent, POINT point, int flags);
		[DllImport("User32.dll")]                                                                             public static extern bool   ClientToScreen(HWND hwnd, ref POINT pt);
		[DllImport("user32.dll")]                                                                             public static extern IntPtr CreatePopupMenu();
		[DllImport("user32.dll", SetLastError = true, CharSet=CharSet.Unicode)]                               public static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
		[DllImport("user32.dll", SetLastError = true, CharSet=CharSet.Unicode)]                               public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, HWND hWnd, int Msg, WPARAM wParam, LPARAM lParam);
		[DllImport("user32.dll", SetLastError = true, CharSet=CharSet.Unicode)]                               public static extern IntPtr DefWindowProc(HWND hWnd, int Msg, WPARAM wParam, LPARAM lParam);
		[DllImport("user32.dll", CharSet=CharSet.Unicode)]                                                    public static extern bool   DestroyWindow(IntPtr hwnd);
		[DllImport("user32.dll")]                                                                             public static extern short  GetAsyncKeyState(Keys vKey);
		[DllImport("user32.dll", SetLastError = true, CharSet=CharSet.Unicode)]                               public static extern bool   GetCaretPos(ref POINT point);
		[DllImport("user32.dll")]                                                                             public static extern bool   GetClientRect(HWND hwnd, out RECT rect);
		[DllImport("User32.dll")]                                                                             public static extern IntPtr GetDC(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern int    GetDoubleClickTime();
		[DllImport("user32.dll")]                                                                             public static extern HWND   GetFocus();
		[DllImport("user32.dll")]                                                                             public static extern HWND   GetForegroundWindow();
		[DllImport("user32.dll")]                                                                             public static extern int    GetKeyboardState(byte[] pbKeyState);
		[DllImport("user32.dll")]                                                                             public static extern short  GetKeyState(Keys vKey);
		[DllImport("user32.dll")]                                                                             public static extern HWND   GetParent(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   GetScrollInfo(HWND hwnd, int BarType, ref SCROLLINFO lpsi);
		[DllImport("user32.dll")]                                                                             public static extern int    GetScrollPos(HWND hWnd, int nBar);
		[DllImport("user32.dll")]                                                                             public static extern IntPtr GetSystemMenu(HWND hwnd, bool bRevert);
		[DllImport("user32.dll")]                                                                             public static extern bool   GetUpdateRect(HWND hwnd, out Win32.RECT rect, bool erase);
		[DllImport("user32.dll", SetLastError=true)]                                                          public static extern uint   GetWindowLong(HWND hWnd, int nIndex);
		[DllImport("user32.dll", EntryPoint="GetWindowLongPtrW", CharSet=CharSet.Unicode, SetLastError=true)] public static extern long   GetWindowLongPtr(HWND hWnd, int nIndex); // This is only defined in 64bit builds, otherwise it's a #define to GetWindowLong
		[DllImport("user32.dll")]                                                                             public static extern bool   GetWindowRect(HWND hwnd, out RECT rect);
		[DllImport("user32.dll", SetLastError = true)]                                                        public static extern IntPtr GetWindowThreadProcessId(HWND hWnd, ref IntPtr lpdwProcessId);
		[DllImport("user32.dll")]                                                                             public static extern int    HideCaret(IntPtr hwnd);
		[DllImport("user32.dll", EntryPoint="InsertMenu", CharSet=CharSet.Unicode)]                           public static extern bool   InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);
		[DllImport("user32.dll", EntryPoint="InsertMenu", CharSet=CharSet.Unicode)]                           public static extern bool   InsertMenu(IntPtr hMenu, int wPosition, int wFlags, IntPtr wIDNewItem, string lpNewItem);
		[DllImport("user32.dll")]                                                                             public static extern bool   InvalidateRect(IntPtr hwnd, IntPtr lpRect, bool bErase);
		[DllImport("user32.dll")]                                                                             public static extern bool   InvalidateRect(IntPtr hwnd, ref Win32.RECT lpRect, bool bErase);
		[DllImport("user32.dll")]                                                                             public static extern bool   IsIconic(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   IsWindow(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   IsWindowVisible(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   IsZoomed(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   LockWindowUpdate(HWND hWndLock);
		[DllImport("user32.dll")]                                                                             public static extern uint   MapVirtualKey(uint uCode, uint uMapType);
		[DllImport("user32.dll")]                                                                             public static extern bool   MoveWindow(HWND hWnd, int X, int Y, int nWidth, int nHeight, bool repaint);
		[DllImport("user32.dll", EntryPoint="PeekMessage", CharSet=CharSet.Auto)]                             public static extern bool   PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);
		[DllImport("user32.dll", EntryPoint="PostThreadMessage")]                                             public static extern int    PostThreadMessage(int idThread, uint msg, int wParam, int lParam);
		[DllImport("user32.dll")]                                                                             public static extern bool   RedrawWindow(HWND hWnd, ref RECT lprcUpdate, HRGN hrgnUpdate, uint flags);
		[DllImport("user32.dll")]                                                                             public static extern bool   RedrawWindow(HWND hWnd, IntPtr lprcUpdate, HRGN hrgnUpdate, uint flags);
		[DllImport("User32.dll")]                                                                             public static extern bool   ReleaseDC(HWND hWnd, IntPtr hDC);
		[DllImport("User32.dll")]                                                                             public static extern bool   ScreenToClient(HWND hwnd, ref POINT pt);
		[DllImport("user32.dll", EntryPoint="SendMessage", SetLastError=true)]                                public static extern int    SendMessage(HWND hwnd, uint msg, int wparam, int lparam);
		[DllImport("user32.dll", EntryPoint="SendMessage", SetLastError=true)]                                public static extern int    SendMessage(HWND hwnd, uint msg, IntPtr wparam, IntPtr lparam);
		[DllImport("user32.dll", EntryPoint="SendMessage", SetLastError=true)]                                public static extern int    SendMessage(HWND hwnd, uint msg, IntPtr wparam, ref POINT lparam);
		[DllImport("user32.dll")]                                                                             public static extern IntPtr SetCursor(IntPtr cursor);
		[DllImport("user32.dll")]                                                                             public static extern HWND   SetFocus(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   SetForegroundWindow(HWND hwnd);
		[DllImport("user32.dll")]                                                                             public static extern IntPtr SetParent(HWND hWndChild, HWND hWndNewParent);
		[DllImport("user32.dll")]                                                                             public static extern int    SetScrollInfo(HWND hwnd, int fnBar, ref SCROLLINFO lpsi, bool fRedraw);
		[DllImport("user32.dll")]                                                                             public static extern int    SetScrollPos(HWND hWnd, int nBar, int nPos, bool bRedraw);
		[DllImport("user32.dll")]                                                                             public static extern int    SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);
		[DllImport("user32.dll")]                                                                             public static extern int    SetWindowLong(HWND hWnd, int nIndex, uint dwNewLong);
		[DllImport("user32.dll")]                                                                             public static extern bool   SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		[DllImport("user32.dll")]                                                                             public static extern int    ShowCaret(IntPtr hwnd);
		[DllImport("user32.dll")]                                                                             public static extern bool   ShowWindow(IntPtr hWnd, int nCmdShow);
		[DllImport("user32.dll")]                                                                             public static extern bool   ShowWindowAsync(HWND hwnd, int nCmdShow);
		[DllImport("user32.dll")]                                                                             public static extern int    ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);
		[DllImport("user32.dll")]                                                                             public static extern int    ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex=4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
		[DllImport("user32.dll")]                                                                             public static extern bool   TranslateMessage(ref Message lpMsg);
		[DllImport("user32.dll")]                                                                             public static extern int    UnhookWindowsHookEx(int idHook);
		[DllImport("user32.dll")]                                                                             public static extern HWND   WindowFromPoint(POINT Point);

		// This static method is required because legacy OSes do not support SetWindowLongPtr 
		public static IntPtr SetWindowLongPtr(HWND hWnd, int nIndex, IntPtr dwNewLong)
		{
			return IntPtr.Size == 8
				? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
				: new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
		}
		[DllImport("user32.dll", EntryPoint="SetWindowLong")] private static extern int SetWindowLong32(HWND hWnd, int nIndex, int dwNewLong);
		[DllImport("user32.dll", EntryPoint="SetWindowLongPtr")] private static extern IntPtr SetWindowLongPtr64(HWND hWnd, int nIndex, IntPtr dwNewLong);
	}
}
