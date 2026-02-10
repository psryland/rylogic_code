using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Rylogic.Gfx;
using Rylogic.Interop.Win32;

namespace BorderOfPeace.Services
{
	/// <summary>A transparent, click-through window that draws a thick colored border over a target window</summary>
	public class BorderOverlay : Window
	{
		private readonly IntPtr m_target_hwnd;
		private readonly Border m_border;

		public BorderOverlay(IntPtr target_hwnd, Colour32 colour, uint thickness)
		{
			m_target_hwnd = target_hwnd;

			// Frameless, transparent, no taskbar entry
			WindowStyle = WindowStyle.None;
			AllowsTransparency = true;
			Background = Brushes.Transparent;
			ShowInTaskbar = false;
			Topmost = true;
			ResizeMode = ResizeMode.NoResize;
			IsHitTestVisible = false;

			// The border element that renders the colored frame
			m_border = new Border();
			Content = m_border;

			UpdateBorder(colour, thickness);
			SyncPosition();
			Show();

			// Make click-through (WS_EX_TRANSPARENT) after the window handle exists
			MakeClickThrough();
		}

		/// <summary>Update the border color and thickness</summary>
		public void UpdateBorder(Colour32 colour, uint thickness)
		{
			var wpf_colour = Color.FromRgb(colour.R, colour.G, colour.B);
			m_border.BorderBrush = new SolidColorBrush(wpf_colour);
			m_border.BorderThickness = new Thickness(thickness);
			m_border.Background = Brushes.Transparent;
		}

		/// <summary>Sync overlay position/size to the target window</summary>
		public bool SyncPosition()
		{
			if (!User32.IsWindowVisible(m_target_hwnd))
			{
				Hide();
				return true; // Target still exists, just hidden
			}

			var rect = User32.GetWindowRect(m_target_hwnd);
			var w = rect.right - rect.left;
			var h = rect.bottom - rect.top;

			if (w <= 0 || h <= 0)
			{
				Hide();
				return true;
			}

			// Convert from screen pixels to WPF device-independent units
			var source = PresentationSource.FromVisual(this);
			var dpi_x = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
			var dpi_y = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

			Left = rect.left * dpi_x;
			Top = rect.top * dpi_y;
			Width = w * dpi_x;
			Height = h * dpi_y;

			if (!IsVisible)
				Show();

			return true;
		}

		/// <summary>Check if the target window still exists</summary>
		public bool IsTargetAlive => User32.IsWindowVisible(m_target_hwnd) || User32.GetWindowLong(m_target_hwnd, Win32.GWL_STYLE) != 0;

		/// <summary>Make the overlay completely click-through</summary>
		private void MakeClickThrough()
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			var ex_style = (uint)User32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
			User32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE,
				ex_style | (uint)Win32.WS_EX_TRANSPARENT | (uint)Win32.WS_EX_TOOLWINDOW);
		}

		/// <summary>Keep the overlay topmost but just above the target window</summary>
		public void EnsureZOrder()
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			if (hwnd == IntPtr.Zero) return;

			// Place just above the target window
			User32.SetWindowPos(hwnd, m_target_hwnd,
				0, 0, 0, 0,
				(uint)(Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE));
		}
	}
}
