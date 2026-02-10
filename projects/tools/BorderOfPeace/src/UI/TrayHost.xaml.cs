using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

		public TrayHost(Settings settings)
		{
			m_settings = settings;
			InitializeComponent();
		}

		/// <summary>Dynamically populate the tray context menu when opened</summary>
		private void HandleTrayMenuOpened(object sender, RoutedEventArgs e)
		{
			if (sender is not ContextMenu menu)
				return;

			menu.Items.Clear();

			// Get the foreground window info
			var hwnd = User32.GetForegroundWindow();
			var title = GetWindowTitle(hwnd);
			var display_title = string.IsNullOrWhiteSpace(title) ? "(No Window)" : title;
			if (display_title.Length > 40)
				display_title = display_title[..37] + "...";

			// Window title header (disabled, just for context)
			var header = new MenuItem
			{
				Header = display_title,
				IsEnabled = false,
				FontWeight = FontWeights.Bold,
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
			reset_item.Click += (s, args) =>
			{
				WindowColorizer.ResetColors(reset_hwnd);
				m_colored_windows.Remove(reset_hwnd);
			};
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
			item.Click += (s, args) =>
			{
				WindowColorizer.ApplyColor(capture_hwnd, capture_colour);
				m_colored_windows[capture_hwnd] = capture_colour;
			};

			return item;
		}

		/// <summary>Show the colour picker for custom color</summary>
		private void HandleCustomColor(IntPtr hwnd)
		{
			// Use existing color if window was previously colored
			Colour32? initial = m_colored_windows.TryGetValue(hwnd, out var existing) ? existing : null;

			var dlg = new ColourPickerUI(this, initial);
			if (dlg.ShowDialog() == true)
			{
				var colour = dlg.Colour;
				WindowColorizer.ApplyColor(hwnd, colour);
				m_colored_windows[hwnd] = colour;
			}
		}

		/// <summary>Show the settings window</summary>
		private void HandleShowSettings()
		{
			var dlg = new SettingsWindow(m_settings) { Owner = this };
			if (dlg.ShowDialog() == true)
			{
				m_settings = dlg.ResultSettings;
				m_settings.Save();
			}
		}

		/// <summary>Exit the application</summary>
		private void HandleExit()
		{
			// Reset all colored windows before exiting
			foreach (var hwnd in m_colored_windows.Keys)
				WindowColorizer.ResetColors(hwnd);

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

		protected override void OnClosing(CancelEventArgs e)
		{
			// Don't close the host window, just hide it
			e.Cancel = true;
			Hide();
		}
	}
}
