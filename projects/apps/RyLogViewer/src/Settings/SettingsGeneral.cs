using System;
using System.Windows;
using Rylogic.Common;

namespace RyLogViewer.Options
{
	public class General :SettingsSet<General>
	{
		public General()
		{
			ScreenPosition = new Point(100, 100);
			WindowSize = new Size(640, 480);
			FullPathInTitle = true;
			LoadLastFile = false;
			LastLoadedFile = string.Empty;
			RestoreScreenLocation = false; // False so that first runs start in the default window position
			ShowTotD = true;
			CheckForUpdates = false;
		}

		/// <summary>Filepath of the settings file</summary>
		public string SettingsFilepath => Parent is SettingsBase<Settings> parent ? parent.Filepath : string.Empty;

		/// <summary>The main window position on screen</summary>
		public Point ScreenPosition
		{
			get => get<Point>(nameof(ScreenPosition));
			set => set(nameof(ScreenPosition), value);
		}

		/// <summary>The size of the main window</summary>
		public Size WindowSize
		{
			get => get<Size>(nameof(WindowSize));
			set => set(nameof(WindowSize), value);
		}

		/// <summary></summary>
		public bool FullPathInTitle
		{
			get => get<bool>(nameof(FullPathInTitle));
			set => set(nameof(FullPathInTitle), value);
		}

		/// <summary></summary>
		public bool LoadLastFile
		{
			get => get<bool>(nameof(LoadLastFile));
			set => set(nameof(LoadLastFile), value);
		}

		/// <summary></summary>
		public string LastLoadedFile
		{
			get => get<string>(nameof(LastLoadedFile));
			set => set(nameof(LastLoadedFile), value);
		}

		/// <summary>Restore the screen location on startup</summary>
		public bool RestoreScreenLocation
		{
			get => get<bool>(nameof(RestoreScreenLocation));
			set => set(nameof(RestoreScreenLocation), value);
		}

		/// <summary></summary>
		public bool ShowTotD
		{
			get => get<bool>(nameof(ShowTotD));
			set => set(nameof(ShowTotD), value);
		}

		/// <summary></summary>
		public bool CheckForUpdates
		{
			get => get<bool>(nameof(CheckForUpdates));
			set => set(nameof(CheckForUpdates), value);
		}

		/// <summary>Validate settings</summary>
		public override Exception? Validate()
		{
			// If restoring the screen location, ensure it's on screen
			if (RestoreScreenLocation)
			{
				var sz = WindowSize;
				var pt = ScreenPosition;
				var valid =
					sz.Width < SystemParameters.VirtualScreenWidth &&
					sz.Height < SystemParameters.VirtualScreenHeight &&
					pt.X >= SystemParameters.VirtualScreenLeft && pt.X < (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 20) &&
					pt.Y >= SystemParameters.VirtualScreenTop && pt.Y < (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 20);
				if (!valid)
					RestoreScreenLocation = false;
			}

			return null;
		}
	}
}
