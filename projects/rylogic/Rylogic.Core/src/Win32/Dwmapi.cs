using System;
using System.Runtime.InteropServices;

namespace Rylogic.Interop.Win32
{
	public static class Dwmapi
	{
		/// <summary>Sets a Desktop Window Manager (DWM) attribute for a window.</summary>
		[DllImport("dwmapi.dll", PreserveSig = true)]
		public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

		/// <summary>Gets a Desktop Window Manager (DWM) attribute for a window.</summary>
		[DllImport("dwmapi.dll", PreserveSig = true)]
		public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out uint pvAttribute, int cbAttribute);

		// DWM Window Attribute constants (Windows 11 22H2+)
		public const int DWMWA_BORDER_COLOR = 34;
		public const int DWMWA_CAPTION_COLOR = 35;
		public const int DWMWA_TEXT_COLOR = 36;
		public const int DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37;

		// Special value meaning "use the system default color"
		public const uint DWMWA_COLOR_DEFAULT = 0xFFFFFFFF;

		// Special value meaning "no color" (transparent)
		public const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;
	}
}
