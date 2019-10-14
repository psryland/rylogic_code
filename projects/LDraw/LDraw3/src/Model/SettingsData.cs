using System;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace LDraw
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			AutoRefresh = false;
			ResetOnLoad = true;
			ClearErrorLogOnReload = true;
			RecentFiles = string.Empty;
			IncludePaths = Array.Empty<string>();
			Scene = new ChartControl.OptionsData
			{
				BackgroundColour = Colour32.Gray,
				ShowAxes = false,
				ShowGridLines = false,
				FocusPointVisible = true,
				OriginPointVisible = false,
				NavigationMode = ChartControl.ENavMode.Scene3D,
				LockAspect = 1.0,
			};

			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath, ESettingsLoadFlags.None)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>Auto reload script sources when changes are detected</summary>
		public bool AutoRefresh
		{
			get => get<bool>(nameof(AutoRefresh));
			set => set(nameof(AutoRefresh), value);
		}

		/// <summary>True if the scene should auto range after loading files</summary>
		public bool ResetOnLoad
		{
			get => get<bool>(nameof(ResetOnLoad));
			set => set(nameof(ResetOnLoad), value);
		}

		/// <summary>Clear the error log when source data is reloaded</summary>
		public bool ClearErrorLogOnReload
		{
			get => get<bool>(nameof(ClearErrorLogOnReload));
			set => set(nameof(ClearErrorLogOnReload), value);
		}

		/// <summary>Recently loaded files</summary>
		public string RecentFiles
		{
			get => get<string>(nameof(RecentFiles));
			set => set(nameof(RecentFiles), value);
		}

		/// <summary>Includes paths to use when resolving includes in script files</summary>
		public string[] IncludePaths
		{
			get => get<string[]>(nameof(IncludePaths));
			set => set(nameof(IncludePaths), value);
		}

		/// <summary>Options for scene behaviour</summary>
		public ChartControl.OptionsData Scene
		{
			get => get<ChartControl.OptionsData>(nameof(Scene));
			set => set(nameof(Scene), value);
		}
	}
}
