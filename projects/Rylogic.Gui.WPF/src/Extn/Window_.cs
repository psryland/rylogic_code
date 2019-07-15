using System.Windows;

namespace Rylogic.Gui.WPF
{
	public static class Window_
	{
		/// <summary>Move this window to within the virtual screen area. Returns this for fluent style calling</summary>
		public static TWindow OnScreen<TWindow>(this TWindow wnd) where TWindow:Window
		{
			var rect = new Rect(wnd.Left, wnd.Top, wnd.ActualWidth, wnd.ActualHeight);
			rect = WPFUtil.OnScreen(rect);
			wnd.Left = rect.Left;
			wnd.Top = rect.Top;
			return wnd;
		}
	}
}
