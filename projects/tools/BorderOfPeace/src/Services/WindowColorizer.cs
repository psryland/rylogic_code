using System;
using System.Runtime.InteropServices;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;

namespace BorderOfPeace.Services
{
	/// <summary>Applies border and title bar colors to windows using DWM</summary>
	public static class WindowColorizer
	{
		/// <summary>Set the border color of a window (COLORREF: 0x00BBGGRR)</summary>
		public static bool SetBorderColor(IntPtr hwnd, uint color_ref)
		{
			var hr = Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_BORDER_COLOR, ref color_ref, sizeof(uint));
			return hr == 0;
		}

		/// <summary>Set the caption/title bar color of a window (COLORREF: 0x00BBGGRR)</summary>
		public static bool SetCaptionColor(IntPtr hwnd, uint color_ref)
		{
			var hr = Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_CAPTION_COLOR, ref color_ref, sizeof(uint));
			return hr == 0;
		}

		/// <summary>Set the title bar text color of a window (COLORREF: 0x00BBGGRR)</summary>
		public static bool SetTextColor(IntPtr hwnd, uint color_ref)
		{
			var hr = Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_TEXT_COLOR, ref color_ref, sizeof(uint));
			return hr == 0;
		}

		/// <summary>Set border and caption color from a Colour32</summary>
		public static bool ApplyColor(IntPtr hwnd, Colour32 colour)
		{
			var color_ref = (uint)((colour.B << 16) | (colour.G << 8) | colour.R);
			var ok = SetBorderColor(hwnd, color_ref);
			ok &= SetCaptionColor(hwnd, color_ref);
			return ok;
		}

		/// <summary>Reset window colors to system defaults</summary>
		public static bool ResetColors(IntPtr hwnd)
		{
			var default_color = Dwmapi.DWMWA_COLOR_DEFAULT;
			var ok = Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_BORDER_COLOR, ref default_color, sizeof(uint)) == 0;
			ok &= Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_CAPTION_COLOR, ref default_color, sizeof(uint)) == 0;
			ok &= Dwmapi.DwmSetWindowAttribute(hwnd, Dwmapi.DWMWA_TEXT_COLOR, ref default_color, sizeof(uint)) == 0;
			return ok;
		}
	}
}
