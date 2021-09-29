using System;
using System.Windows;
using System.Windows.Interop;

namespace Rylogic.Gui.WPF
{
	public static class Window_
	{
		/// <summary>Return the window handle for this window</summary>
		public static IntPtr Hwnd(this Window wnd)
		{
			if (wnd == null) return IntPtr.Zero;
			return new WindowInteropHelper(wnd).Handle;
		}

		/// <summary>Return the window handle of the containing window</summary>
		public static IntPtr Hwnd(this DependencyObject obj)
		{
			var window = Window.GetWindow(obj);
			return window?.Hwnd() ?? IntPtr.Zero;
		}

		/// <summary>True if the HWND for this window has been created</summary>
		public static bool IsHandleCreated(this Window wnd)
		{
			return wnd.Hwnd() != IntPtr.Zero;
		}

		/// <summary>True if the HWND for the containing window has been created</summary>
		public static bool IsHandleCreated(this DependencyObject obj)
		{
			var window = Window.GetWindow(obj);
			return window?.IsHandleCreated() ?? false;
		}

		/// <summary>Move this window to within the virtual screen area. Returns this for fluent style calling</summary>
		public static TWindow OnScreen<TWindow>(this TWindow wnd) where TWindow:Window
		{
			var rect = new Rect(wnd.Left, wnd.Top, wnd.ActualWidth, wnd.ActualHeight);
			rect = WPFUtil.OnScreen(rect);
			wnd.Left = rect.Left;
			wnd.Top = rect.Top;
			return wnd;
		}

		/// <summary>Fluent show method</summary>
		public static TWindow Show2<TWindow>(this TWindow wnd) where TWindow : Window
		{
			wnd.Show();
			return wnd;
		}

		/// <summary>The size of the window (in screen space)</summary>
		public static Rect Bounds(this Window ctrl)
		{
			return new Rect(ctrl.Left, ctrl.Top, ctrl.Width, ctrl.Height);
		}
	}
}
