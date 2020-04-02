using System;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace LDraw
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			FontName = "consolas";
			FontSize = 10.0;
			AutoRefresh = false;
			ResetOnLoad = true;
			ReloadChangedScripts = null;
			ClearErrorLogOnReload = true;
			CheckForChangesPollPeriodS = 1.0;
			RecentFiles = string.Empty;
			IncludePaths = Array.Empty<string>();
			EmbeddedCSharpBoilerPlate = EmbeddedCSharpBoilerPlateDefault;
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
			UILayout = null;

			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath, ESettingsLoadFlags.None)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The font to use in scripts</summary>
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

		/// <summary>Boilerplate code for embedded C# in ldr script files</summary>
		public string EmbeddedCSharpBoilerPlate
		{
			get => get<string>(nameof(EmbeddedCSharpBoilerPlate));
			set => set(nameof(EmbeddedCSharpBoilerPlate), value);
		}
		private static string EmbeddedCSharpBoilerPlateDefault =>
		#region Embedded C# Source
$@"//
//Assembly: netstandard.dll
//Assembly: System.dll
//Assembly: System.Drawing.dll
//Assembly: System.IO.dll
//Assembly: System.Linq.dll
//Assembly: System.Windows.Forms.dll
//Assembly: System.ValueTuple.dll
//Assembly: System.Xml.dll
//Assembly: System.Xml.Linq.dll
//Assembly: .\Rylogic.Core.dll
//Assembly: .\Rylogic.Core.Windows.dll
//Assembly: .\Rylogic.View3d.dll
//Assembly: .\LDraw.exe
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;
using Rylogic.Utility;

namespace ldr
{{
	public class Main
	{{
		private StringBuilder Out = new StringBuilder();

		<<<support>>>

		public string Execute()
		{{
			//LDraw.Log.Write(ELogLevel.Info, ""Starting..."");

			<<<code>>>

			return Out.ToString();
		}}
	}}
}}
";
		#endregion

		/// <summary>Options for scene behaviour</summary>
		public ChartControl.OptionsData Scene
		{
			get => get<ChartControl.OptionsData>(nameof(Scene));
			set => set(nameof(Scene), value);
		}

		/// <summary>Layout state of the main UI</summary>
		public XElement? UILayout
		{
			get => get<XElement>(nameof(UILayout));
			set => set(nameof(UILayout), value);
		}
	}
}
