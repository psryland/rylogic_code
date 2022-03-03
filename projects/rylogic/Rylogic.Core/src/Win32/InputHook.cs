using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	// A collection of 
	public static class MouseKeyboardHook
	{
#if false
		// The MSLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
		[StructLayout(LayoutKind.Sequential)]
		private struct MSLLHOOKSTRUCT
		{
			public Win32.POINT Point;
			public int MouseData;
			public int Flags;
			public int Time;
			public int ExtraInfo;
		}

		// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
		[StructLayout(LayoutKind.Sequential)]
		private struct KBDLLHOOKSTRUCT
		{
			public int VirtualKeyCode;
			public int ScanCode;
			public int Flags;
			public int Time;
			public int ExtraInfo;
		}

		// Mouse down
		private static event MouseEventHandler m_mouse_down;
		public static event MouseEventHandler MouseDown
		{
			add    { EnsureSubscribedToGlobalMouseEvents(); m_mouse_down += value; }
			remove { m_mouse_down -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		}

		// Mouse up
		private static event MouseEventHandler m_mouse_up;
		public static event MouseEventHandler MouseUp
		{
			add    { EnsureSubscribedToGlobalMouseEvents(); m_mouse_up += value; }
			remove { m_mouse_up -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		}

		// Mouse moved
		private static event MouseEventHandler m_mouse_move;
		public static event MouseEventHandler MouseMove
		{
			add    { EnsureSubscribedToGlobalMouseEvents(); m_mouse_move += value; }
			remove { m_mouse_move -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		}

		// Mouse clicked
		private static event MouseEventHandler m_mouse_click;
		public static event MouseEventHandler MouseClick
		{
			add    { EnsureSubscribedToGlobalMouseEvents(); m_mouse_click += value; }
			remove { m_mouse_click -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		}

		/// Mouse wheel
		private static event MouseEventHandler m_mouse_wheel;
		public static event MouseEventHandler MouseWheel
		{
			add    { EnsureSubscribedToGlobalMouseEvents(); m_mouse_wheel += value; }
			remove { m_mouse_wheel -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		}

		// Key down
		private static event KeyEventHandler m_keydown;
		public static event KeyEventHandler KeyDown
		{
			add    { EnsureSubscribedToGlobalKeyboardEvents(); m_keydown += value; }
			remove { m_keydown -= value; TryUnsubscribeFromGlobalKeyboardEvents(); }
		}

		// Key up
		private static event KeyEventHandler m_keyup;
		public static event KeyEventHandler KeyUp
		{
			add    { EnsureSubscribedToGlobalKeyboardEvents(); m_keyup += value; }
			remove { m_keyup -= value; TryUnsubscribeFromGlobalKeyboardEvents(); }
		}

		// Key press
		private static event KeyPressEventHandler m_keypress;
		public static event KeyPressEventHandler KeyPress
		{
			add    { EnsureSubscribedToGlobalKeyboardEvents(); m_keypress += value; }
			remove { m_keypress -= value; TryUnsubscribeFromGlobalKeyboardEvents(); }
		}

		// Install Mouse hook only if it is not installed and must be installed
		private static int m_mouse_hook_handle; // Stores the handle to the mouse hook procedure.
		private static Win32.HookProc m_mouse_hook_proc; // To prevent the GC collecting the delegate
		private static void EnsureSubscribedToGlobalMouseEvents()
		{
			if (m_mouse_hook_handle != 0) return;
			m_mouse_hook_proc = MouseHookProc;
			m_mouse_hook_handle = Win32.SetWindowsHookEx((int)Win32.WH_MOUSE_LL, m_mouse_hook_proc, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
			if (m_mouse_hook_handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		// If no subscribers are registered remove the hook
		private static void TryUnsubscribeFromGlobalMouseEvents()
		{
			if (m_mouse_down  == null && 
				m_mouse_up    == null &&
				m_mouse_move  == null &&
				m_mouse_click == null &&
				m_mouse_wheel == null)
				ForceUnsunscribeFromGlobalMouseEvents();
		}

		// Remove the mouse hook
		private static void ForceUnsunscribeFromGlobalMouseEvents()
		{
			if (m_mouse_hook_handle == 0) return;
			int result = Win32.UnhookWindowsHookEx(m_mouse_hook_handle);
			m_mouse_hook_handle = 0;
			m_mouse_hook_proc = null;
			if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		// The hook callback function which will be called for mouse activity.
		private static int m_prev_mouse_x, m_prev_mouse_y;
		private static int MouseHookProc(int nCode, int wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				MSLLHOOKSTRUCT info = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
				switch ((uint)wParam)
				{
				case Win32.WM_LBUTTONDOWN: if (m_mouse_down  != null) { m_mouse_down.Invoke(null, new MouseEventArgs(MouseButtons.Left , 1, info.Point.X, info.Point.Y, 0)); } break;
				case Win32.WM_LBUTTONUP:   if (m_mouse_up    != null) { m_mouse_up  .Invoke(null, new MouseEventArgs(MouseButtons.Left , 1, info.Point.X, info.Point.Y, 0)); } break;
				case Win32.WM_RBUTTONDOWN: if (m_mouse_down  != null) { m_mouse_down.Invoke(null, new MouseEventArgs(MouseButtons.Right, 1, info.Point.X, info.Point.Y, 0)); } break;
				case Win32.WM_RBUTTONUP:   if (m_mouse_up    != null) { m_mouse_up  .Invoke(null, new MouseEventArgs(MouseButtons.Right, 1, info.Point.X, info.Point.Y, 0)); } break;
				case Win32.WM_MOUSEWHEEL:
					int delta = (short)((info.MouseData >> 16) & 0xffff);
					if (m_mouse_wheel != null && delta != 0) m_mouse_wheel.Invoke(null, new MouseEventArgs(MouseButtons.None, 1, info.Point.X, info.Point.Y, 0));
					break;
				}
				if (m_mouse_move != null && (m_prev_mouse_x != info.Point.X || m_prev_mouse_y != info.Point.Y))
				{
					m_prev_mouse_x = info.Point.X;
					m_prev_mouse_y = info.Point.Y;
					m_mouse_move.Invoke(null, new MouseEventArgs(MouseButtons.None, 1, info.Point.X, info.Point.Y, 0));
				}
			}
			return Win32.CallNextHookEx(m_mouse_hook_handle, nCode, wParam, lParam);
		}

		// Install Keyboard hook only if it is not installed and must be installed
		private static int m_keyboard_hook_handle; // Stores the handle to the Keyboard hook procedure.
		private static Win32.HookProc m_keyboard_hook_proc; // To prevent the GC collecting the delegate
		private static void EnsureSubscribedToGlobalKeyboardEvents()
		{
			if (m_keyboard_hook_handle != 0) return;
			m_keyboard_hook_proc = KeyboardHookProc;
			m_keyboard_hook_handle = Win32.SetWindowsHookEx((int)Win32.WH_KEYBOARD_LL, m_keyboard_hook_proc, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
			if (m_keyboard_hook_handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		// If no subscribers are registered remove the hook
		private static void TryUnsubscribeFromGlobalKeyboardEvents()
		{
			if (m_keydown  == null &&
				m_keyup    == null &&
				m_keypress == null)
				ForceUnsunscribeFromGlobalKeyboardEvents();
		}

		// Remove the keyboard hook
		private static void ForceUnsunscribeFromGlobalKeyboardEvents()
		{
			if (m_keyboard_hook_handle == 0) return;
			int result = Win32.UnhookWindowsHookEx(m_keyboard_hook_handle);
			m_keyboard_hook_handle = 0;
			m_keyboard_hook_proc = null;
			if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		// The hook callback function which will be called for keyboard activity.
		private static int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				KBDLLHOOKSTRUCT info = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
				if (m_keydown != null && (wParam == Win32.WM_KEYDOWN || wParam == Win32.WM_SYSKEYDOWN))
				{
					m_keydown.Invoke(null, new KeyEventArgs((Keys)info.VirtualKeyCode));
				}
				if (m_keypress != null && wParam == Win32.WM_KEYDOWN)
				{
					byte[] inbuf = new byte[2];
					byte[] keystate = new byte[256];
					Win32.GetKeyboardState(keystate);
					if (Win32.ToAscii(info.VirtualKeyCode, info.ScanCode, keystate, inbuf, info.Flags) == 1)
					{
						char key = (char)inbuf[0];
						if ((Win32.KeyDown(Keys.CapsLock) ^ Win32.KeyDown(Keys.Shift)) && Char.IsLetter(key)) key = Char.ToUpper(key);
						m_keypress.Invoke(null, new KeyPressEventArgs(key));
					}
				}
				if (m_keyup != null && (wParam == Win32.WM_KEYUP || wParam == Win32.WM_SYSKEYUP))
				{
					m_keyup.Invoke(null, new KeyEventArgs((Keys)info.VirtualKeyCode));
				}
			}
			return Win32.CallNextHookEx(m_keyboard_hook_handle, nCode, wParam, lParam);
		}
	#endif

		//// Occurs when the mouse pointer is moved.
		//// This event provides extended arguments of type <see cref="MouseEventArgs"/> enabling you to 
		//// suppress further processing of mouse movement in other applications.
		//private static event EventHandler<MouseEventExtArgs> s_MouseMoveExt;
		//public static event EventHandler<MouseEventExtArgs> MouseMoveExt
		//{
		//    add    { EnsureSubscribedToGlobalMouseEvents(); s_MouseMoveExt += value; }
		//    remove { s_MouseMoveExt -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		//}

		///// Occurs when a click was performed by the mouse. 
		///// This event provides extended arguments of type <see cref="MouseEventArgs"/> enabling you to 
		///// suppress further processing of mouse click in other applications.
		//private static event EventHandler<MouseEventExtArgs> s_MouseClickExt;
		//public static event EventHandler<MouseEventExtArgs> MouseClickExt
		//{
		//    add    { EnsureSubscribedToGlobalMouseEvents(); s_MouseClickExt += value; }
		//    remove { s_MouseClickExt -= value; TryUnsubscribeFromGlobalMouseEvents(); }
		//}


		////The double click event will not be provided directly from hook.
		////To fire the double click event wee need to monitor mouse up event and when it occurs 
		////Two times during the time interval which is defined in Windows as a double click time
		////we fire this event.

		///// <summary>
		///// Occurs when a double clicked was performed by the mouse. 
		///// </summary>
		//private static event MouseEventHandler s_MouseDoubleClick;
		//public static event MouseEventHandler MouseDoubleClick
		//{
		//    add
		//    {
		//        EnsureSubscribedToGlobalMouseEvents();
		//        if (s_MouseDoubleClick == null)
		//        {
		//            //We create a timer to monitor interval between two clicks
		//            s_DoubleClickTimer = new Timer
		//            {
		//                //This interval will be set to the value we retrive from windows. This is a windows setting from contro planel.
		//                Interval = GetDoubleClickTime(),
		//                //We do not start timer yet. It will be start when the click occures.
		//                Enabled = false
		//            };
		//            //We define the callback function for the timer
		//            s_DoubleClickTimer.Tick += DoubleClickTimeElapsed;
		//            //We start to monitor mouse up event.
		//            MouseUp += OnMouseUp;
		//        }
		//        s_MouseDoubleClick += value;
		//    }
		//    remove
		//    {
		//        if (s_MouseDoubleClick != null)
		//        {
		//            s_MouseDoubleClick -= value;
		//            if (s_MouseDoubleClick == null)
		//            {
		//                //Stop monitoring mouse up
		//                MouseUp -= OnMouseUp;
		//                //Dispose the timer
		//                s_DoubleClickTimer.Tick -= DoubleClickTimeElapsed;
		//                s_DoubleClickTimer = null;
		//            }
		//        }
		//        TryUnsubscribeFromGlobalMouseEvents();
		//    }
		//}

		////This field remembers mouse button pressed because in addition to the short interval it must be also the same button.
		//private static MouseButtons s_PrevClickedButton;
		////The timer to monitor time interval between two clicks.
		//private static Timer s_DoubleClickTimer;

		//private static void DoubleClickTimeElapsed(object sender, EventArgs e)
		//{
		//    //Timer is alapsed and no second click ocuured
		//    s_DoubleClickTimer.Enabled = false;
		//    s_PrevClickedButton = MouseButtons.None;
		//}

		///// <summary>
		///// This method is designed to monitor mouse clicks in order to fire a double click event if interval between 
		///// clicks was short enough.
		///// </summary>
		///// <param name="sender">Is always null</param>
		///// <param name="e">Some information about click happened.</param>
		//private static void OnMouseUp(object sender, MouseEventArgs e)
		//{
		//    //This should not heppen
		//    if (e.Clicks < 1) { return; }
		//    //If the secon click heppened on the same button
		//    if (e.Button.Equals(s_PrevClickedButton))
		//    {
		//        if (s_MouseDoubleClick != null)
		//        {
		//            //Fire double click
		//            s_MouseDoubleClick.Invoke(null, e);
		//        }
		//        //Stop timer
		//        s_DoubleClickTimer.Enabled = false;
		//        s_PrevClickedButton = MouseButtons.None;
		//    }
		//    else
		//    {
		//        //If it was the firts click start the timer
		//        s_DoubleClickTimer.Enabled = true;
		//        s_PrevClickedButton = e.Button;
		//    }
		//}
	}
}
