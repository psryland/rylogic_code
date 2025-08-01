﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace LDraw
{
	public class SettingsData : SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			RecentFiles = string.Empty;
			Profiles = [new SettingsProfile { Name = SettingsProfile.DefaultProfileName }];
			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath, ESettingsLoadFlags.ThrowOnError)
		{
			AutoSaveOnChanges = true;
		}

		/// <inheritdoc/>
		public override string Version => "v2.0";

		/// <summary>Recently loaded files</summary>
		public string RecentFiles
		{
			get => get<string>(nameof(RecentFiles));
			set => set(nameof(RecentFiles), value);
		}

		/// <summary>Saved configurations</summary>
		public List<SettingsProfile> Profiles
		{
			get => get<List<SettingsProfile>>(nameof(Profiles));
			private set => set(nameof(Profiles), value);
		}
	}

	/// <summary>Per Scene settings</summary>
	public class SettingsProfile :SettingsSet<SettingsProfile>
	{
		public SettingsProfile()
		{
			Name = "Profile";
			FontName = "Consolas";
			FontSize = 10.0;
			AutoRefresh = false;
			ResetOnLoad = true;
			ReloadChangedScripts = null;
			ClearErrorLogOnReload = true;
			CheckForChangesPollPeriodS = 1.0;
			IncludePaths = Array.Empty<string>();
			StreamingPort = 1976;
			SceneState = new List<SceneStateData>();
			UILayout = null;
		}
		public SettingsProfile(SettingsProfile rhs)
			:base(rhs)
		{}

		/// <summary>Name of the default profile</summary>
		public static string DefaultProfileName => "Default Profile";

		/// <summary>The name of this profile</summary>
		public string Name
		{
			get => get<string>(nameof(Name));
			set => set(nameof(Name), value);
		}
		
		/// <summary>The font to use in scripts UIs</summary>
		public string FontName
		{
			get => get<string>(nameof(FontName));
			set => set(nameof(FontName), value);
		}

		/// <summary>The font size</summary>
		public double FontSize
		{
			get => get<double>(nameof(FontSize));
			set => set(nameof(FontSize), value);
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

		/// <summary>Where scripts changed externally are automatically reloaded. Null means prompt</summary>
		public bool? ReloadChangedScripts
		{
			get => get<bool?>(nameof(ReloadChangedScripts));
			set => set(nameof(ReloadChangedScripts), value);
		}

		/// <summary>Clear the error log when source data is reloaded</summary>
		public bool ClearErrorLogOnReload
		{
			get => get<bool>(nameof(ClearErrorLogOnReload));
			set => set(nameof(ClearErrorLogOnReload), value);
		}

		/// <summary>The period between checking for changed files</summary>
		public double CheckForChangesPollPeriodS
		{
			get => get<double>(nameof(CheckForChangesPollPeriodS));
			set => set(nameof(CheckForChangesPollPeriodS), value);
		}

		/// <summary>Includes paths to use when resolving includes in script files</summary>
		public string[] IncludePaths
		{
			get => get<string[]>(nameof(IncludePaths));
			set => set(nameof(IncludePaths), value);
		}

		/// <summary>The port to listen to for incoming streaming connections</summary>
		public int StreamingPort
		{
			get => get<int>(nameof(StreamingPort));
			set => set(nameof(StreamingPort), value);
		}

		/// <summary>Per Scene settings</summary>
		public List<SceneStateData> SceneState
		{
			get => get<List<SceneStateData>>(nameof(SceneState));
			private set => set(nameof(SceneState), value);
		}

		/// <summary>Layout state of the main UI</summary>
		public XElement? UILayout
		{
			get => get<XElement>(nameof(UILayout));
			set => set(nameof(UILayout), value);
		}
	}

	/// <summary>Per Scene settings</summary>
	public class SceneStateData :SettingsSet<SceneStateData>
	{
		public SceneStateData()
		{
			Name = string.Empty;
			ViewPreset = EViewPreset.Current;
			AlignDirection = EAlignDirection.None;
			Chart = new ChartControl.OptionsData
			{
				BackgroundColour = Colour32.Gray,
				ShowAxes = false,
				ShowGridLines = false,
				FocusPointVisible = true,
				OriginPointVisible = false,
				NavigationMode = ChartControl.ENavMode.Scene3D,
				LockAspect = 1.0,
			};
		}

		/// <summary>The name of the scene that this state data belongs to</summary>
		public string Name
		{
			get => get<string>(nameof(Name));
			set => set(nameof(Name), value);
		}

		/// <summary>Pre-set view directions</summary>
		public EViewPreset ViewPreset
		{
			get => get<EViewPreset>(nameof(ViewPreset));
			set => set(nameof(ViewPreset), value);
		}

		/// <summary>Directions to align the camera up-axis to</summary>
		public EAlignDirection AlignDirection
		{
			get => get<EAlignDirection>(nameof(AlignDirection));
			set => set(nameof(AlignDirection), value);
		}

		/// <summary>Options for scene behaviour, common to all scenes</summary>
		public ChartControl.OptionsData Chart
		{
			get => get<ChartControl.OptionsData>(nameof(Chart));
			set => set(nameof(Chart), value);
		}
	}

	/// <summary>Extensions</summary>
	public static class SettingsData_
	{
		/// <summary>Access the scene state data for a scene by name</summary>
		public static SceneStateData get(this IList<SceneStateData> container, string name)
		{
			var ssd = container.FirstOrDefault(x => x.Name == name);
			ssd ??= container.Add2(new SceneStateData { Name = name });
			return ssd;
		}
	}
}
