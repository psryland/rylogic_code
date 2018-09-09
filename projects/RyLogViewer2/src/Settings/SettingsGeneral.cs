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
			FileChangesAdditive = true;
		}

		/// <summary>Filepath of the settings file</summary>
		public string SettingsFilepath
		{
			get { return (Parent as SettingsBase<Settings>).Filepath; }
		}

		/// <summary>The main window position on screen</summary>
		public Point ScreenPosition
		{
			get { return get<Point>(nameof(ScreenPosition)); }
			set { set(nameof(ScreenPosition), value); }
		}

		/// <summary>The size of the main window</summary>
		public Size WindowSize
		{
			get { return get<Size>(nameof(WindowSize)); }
			set { set(nameof(WindowSize), value); }
		}

		/// <summary></summary>
		public bool FullPathInTitle
		{
			get { return get<bool>(nameof(FullPathInTitle)); }
			set { set(nameof(FullPathInTitle), value); }
		}

		/// <summary></summary>
		public bool LoadLastFile
		{
			get { return get<bool>(nameof(LoadLastFile)); }
			set { set(nameof(LoadLastFile), value); }
		}

		/// <summary></summary>
		public string LastLoadedFile
		{
			get { return get<string>(nameof(LastLoadedFile)); }
			set { set(nameof(LastLoadedFile), value); }
		}

		/// <summary>Restore the screen location on startup</summary>
		public bool RestoreScreenLocation
		{
			get { return get<bool>(nameof(RestoreScreenLocation)); }
			set { set(nameof(RestoreScreenLocation), value); }
		}

		/// <summary></summary>
		public bool ShowTotD
		{
			get { return get<bool>(nameof(ShowTotD)); }
			set { set(nameof(ShowTotD), value); }
		}

		/// <summary></summary>
		public bool CheckForUpdates
		{
			get { return get<bool>(nameof(CheckForUpdates)); }
			set { set(nameof(CheckForUpdates), value); }
		}

		/// <summary></summary>
		public bool FileChangesAdditive
		{
			get { return get<bool>(nameof(FileChangesAdditive)); }
			set { set(nameof(FileChangesAdditive), value); }
		}

		/// <summary>Validate settings</summary>
		internal void Validate()
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
		}
	}
}
