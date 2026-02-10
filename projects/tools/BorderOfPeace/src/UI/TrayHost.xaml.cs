using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BorderOfPeace.Model;
using BorderOfPeace.Services;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Interop.Win32;

namespace BorderOfPeace.UI
{
	public partial class TrayHost : Window
	{
		private Settings m_settings;

		// Track which windows we've colored so we can reset them
		private readonly Dictionary<IntPtr, Colour32> m_colored_windows = new();

		// Border overlay windows for tracked windows
		private readonly Dictionary<IntPtr, BorderOverlay> m_overlays = new();

		// The last foreground window that isn't ours
		private IntPtr m_last_foreground_hwnd;
		private string m_last_foreground_title = string.Empty;

		// WinEvent hook handle and delegate (prevent GC collection of delegate)
		private IntPtr m_hook_handle;
		private User32.WinEventDelegate? m_win_event_delegate;

		// Timer for periodically re-applying DWM colors (handles Electron-style resets)
		private readonly DispatcherTimer m_reapply_timer;

		public TrayHost(Settings settings)
		{
			m_settings = settings;
			InitializeComponent();
			StartForegroundTracking();

			// Slow timer for DWM color re-application (Electron workaround)
			m_reapply_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2000) };
			m_reapply_timer.Tick += OnReapplyDwmColors;
			m_reapply_timer.Start();

			// Fast render-frame callback for smooth overlay tracking
			CompositionTarget.Rendering += OnSyncOverlays;
		}

		/// <summary>Install a WinEvent hook to track foreground window changes</summary>
		private void StartForegroundTracking()
		{
			m_win_event_delegate = OnForegroundChanged;
			m_hook_handle = User32.SetWinEventHook(
				Win32.EVENT_SYSTEM_FOREGROUND,
				Win32.EVENT_SYSTEM_FOREGROUND,
				IntPtr.Zero,
				m_win_event_delegate,
				0, 0,
				Win32.WINEVENT_OUTOFCONTEXT | Win32.WINEVENT_SKIPOWNPROCESS);
		}

		/// <summary>Re-apply DWM colors periodically (handles Electron-style resets)</summary>
		private void OnReapplyDwmColors(object? sender, EventArgs e)
		{
			var dead = new List<IntPtr>();
			foreach (var (hwnd, colour) in m_colored_windows)
			{
				if (!User32.IsWindowVisible(hwnd) && User32.GetWindowLong(hwnd, Win32.GWL_STYLE) == 0)
				{
					dead.Add(hwnd);
					continue;
				}
				WindowColorizer.ApplyColor(hwnd, colour, m_settings.BorderThickness);
			}
			foreach (var hwnd in dead)
				ResetWindow(hwnd);
		}

		/// <summary>Sync overlay positions every render frame for smooth tracking</summary>
		private void OnSyncOverlays(object? sender, EventArgs e)
		{
			foreach (var (hwnd, overlay) in m_overlays)
			{
				overlay.SyncPosition();
				overlay.EnsureZOrder();
			}
		}

		/// <summary>Called when the foreground window changes</summary>
		private void OnForegroundChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			if (hwnd == IntPtr.Zero)
				return;

			// Skip our own windows
			var our_pid = (IntPtr)Environment.ProcessId;
			var foreign_pid = IntPtr.Zero;
			User32.GetWindowThreadProcessId(hwnd, ref foreign_pid);
			if (foreign_pid == our_pid)
				return;

			// Only track windows that appear in the taskbar
			if (!IsTaskbarWindow(hwnd))
				return;

			m_last_foreground_hwnd = hwnd;
			m_last_foreground_title = GetWindowTitle(hwnd);
		}

		/// <summary>True if 'hwnd' is the kind of window that has a taskbar entry</summary>
		private static bool IsTaskbarWindow(IntPtr hwnd)
		{
			// Must be visible
			if (!User32.IsWindowVisible(hwnd))
				return false;

			var ex_style = (int)User32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);

			// WS_EX_APPWINDOW forces a taskbar entry
			if ((ex_style & Win32.WS_EX_APPWINDOW) != 0)
				return true;

			// WS_EX_TOOLWINDOW never gets a taskbar entry
			if ((ex_style & Win32.WS_EX_TOOLWINDOW) != 0)
				return false;

			// Unowned top-level windows get a taskbar entry by default
			var owner = User32.GetWindow(hwnd, Win32.GW_OWNER);
			return owner == IntPtr.Zero;
		}

		/// <summary>Dynamically populate the tray context menu when opened</summary>
		private void HandleTrayMenuOpened(object sender, RoutedEventArgs e)
		{
			if (sender is not ContextMenu menu)
				return;

			menu.Items.Clear();

			var hwnd = m_last_foreground_hwnd;
			var display_title = string.IsNullOrWhiteSpace(m_last_foreground_title) ? "(No Window)" : m_last_foreground_title;
			if (display_title.Length > 40)
				display_title = display_title[..37] + "...";

			// Window title header (disabled, just for context)
			var header = new MenuItem
			{
				Header = display_title,
				IsEnabled = false,
				FontWeight = FontWeights.Bold,
				Icon = GetWindowIcon(hwnd) is ImageSource icon_src
					? new Image { Source = icon_src, Width = 16, Height = 16 }
					: null,
			};
			menu.Items.Add(header);
			menu.Items.Add(new Separator());

			// Color preset items
			foreach (var preset in m_settings.Presets)
			{
				var item = CreateColorMenuItem(preset, hwnd);
				menu.Items.Add(item);
			}

			// Custom color option
			var custom_item = new MenuItem { Header = "Custom..." };
			var custom_hwnd = hwnd;
			custom_item.Click += (s, args) => HandleCustomColor(custom_hwnd);
			menu.Items.Add(custom_item);

			menu.Items.Add(new Separator());

			// Reset option
			var reset_item = new MenuItem { Header = "Reset" };
			var reset_hwnd = hwnd;
			reset_item.Click += (s, args) => ResetWindow(reset_hwnd);
			menu.Items.Add(reset_item);

			menu.Items.Add(new Separator());

			// Settings
			var settings_item = new MenuItem { Header = "Settings..." };
			settings_item.Click += (s, args) => HandleShowSettings();
			menu.Items.Add(settings_item);

			// Exit
			var exit_item = new MenuItem { Header = "Exit" };
			exit_item.Click += (s, args) => HandleExit();
			menu.Items.Add(exit_item);
		}

		/// <summary>Create a menu item for a color preset with a swatch</summary>
		private MenuItem CreateColorMenuItem(ColorPreset preset, IntPtr hwnd)
		{
			var colour = preset.Colour;

			// Create a small colored rectangle as the icon
			var swatch = new Border
			{
				Width = 16,
				Height = 16,
				Background = new SolidColorBrush(Color.FromRgb(colour.R, colour.G, colour.B)),
				BorderBrush = Brushes.DarkGray,
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(2),
			};

			var item = new MenuItem
			{
				Header = preset.Name,
				Icon = swatch,
			};

			var capture_hwnd = hwnd;
			var capture_colour = colour;
			item.Click += (s, args) => ApplyColorToWindow(capture_hwnd, capture_colour);

			return item;
		}

		/// <summary>Apply color to a window and manage the border overlay</summary>
		private void ApplyColorToWindow(IntPtr hwnd, Colour32 colour)
		{
			WindowColorizer.ApplyColor(hwnd, colour, m_settings.BorderThickness);
			m_colored_windows[hwnd] = colour;

			// Create or update the border overlay
			var thickness = m_settings.BorderThickness;
			if (thickness > 0)
			{
				if (m_overlays.TryGetValue(hwnd, out var existing))
				{
					existing.UpdateBorder(colour, thickness);
					existing.SyncPosition();
				}
				else
				{
					m_overlays[hwnd] = new BorderOverlay(hwnd, colour, thickness);
				}
			}
		}

		/// <summary>Reset a window's colors and remove its overlay</summary>
		private void ResetWindow(IntPtr hwnd)
		{
			WindowColorizer.ResetColors(hwnd);
			m_colored_windows.Remove(hwnd);
			if (m_overlays.Remove(hwnd, out var overlay))
				overlay.Close();
		}

		/// <summary>Show the colour picker for custom color</summary>
		private void HandleCustomColor(IntPtr hwnd)
		{
			// Use existing color if window was previously colored
			Colour32? initial = m_colored_windows.TryGetValue(hwnd, out var existing) ? existing : null;

			var dlg = new ColourPickerUI(null, initial) { Width = 440, Height = 400 };
			if (dlg.ShowDialog() == true)
				ApplyColorToWindow(hwnd, dlg.Colour);
		}

		/// <summary>Show the settings window</summary>
		private void HandleShowSettings()
		{
			var dlg = new SettingsWindow(m_settings);
			if (dlg.ShowDialog() == true)
			{
				m_settings = dlg.ResultSettings;
				m_settings.Save();
			}
		}

		/// <summary>Exit the application</summary>
		private void HandleExit()
		{
			// Stop timers and hooks
			m_reapply_timer.Stop();
			CompositionTarget.Rendering -= OnSyncOverlays;

			// Unhook the foreground tracker
			if (m_hook_handle != IntPtr.Zero)
			{
				User32.UnhookWinEvent(m_hook_handle);
				m_hook_handle = IntPtr.Zero;
			}

			// Reset all colored windows before exiting
			foreach (var hwnd in new List<IntPtr>(m_colored_windows.Keys))
				ResetWindow(hwnd);

			m_colored_windows.Clear();
			Gui_.DisposeChildren(this);
			Application.Current.Shutdown();
		}

		/// <summary>Get the title of a window by its handle</summary>
		private static string GetWindowTitle(IntPtr hwnd)
		{
			var sb = new StringBuilder(256);
			User32.GetWindowText(hwnd, sb, 256);
			return sb.ToString();
		}

		/// <summary>Get the icon of a window as an ImageSource</summary>
		private static ImageSource? GetWindowIcon(IntPtr hwnd)
		{
			try
			{
				const int ICON_SMALL = 0;
				const int ICON_BIG = 1;

				// Try WM_GETICON (small, then big)
				var h_icon = User32.SendMessage(hwnd, (uint)Win32.WM_GETICON, ICON_SMALL, 0);
				if (h_icon == IntPtr.Zero)
					h_icon = User32.SendMessage(hwnd, (uint)Win32.WM_GETICON, ICON_BIG, 0);

				// Fall back to the window class icon
				if (h_icon == IntPtr.Zero)
					h_icon = User32.GetClassLongPtr(hwnd, Win32.GCLP_HICONSM);
				if (h_icon == IntPtr.Zero)
					h_icon = User32.GetClassLongPtr(hwnd, Win32.GCLP_HICON);

				if (h_icon != IntPtr.Zero)
					return Imaging.CreateBitmapSourceFromHIcon(h_icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			catch
			{
				// Best effort — some windows may not have accessible icons
			}
			return null;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			// Don't close the host window, just hide it
			e.Cancel = true;
			Hide();
		}
	}
}
