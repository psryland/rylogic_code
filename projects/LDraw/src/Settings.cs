using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Rylogic.Utility;

namespace LDraw
{
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			RecentFiles        = string.Empty;
			AutoRefresh        = false;
			ResetOnLoad        = true;
			FocusPointSize     = 1.0f;
			OriginPointSize    = 1.0f;
			FocusPointVisible  = true;
			OriginPointVisible = false;
			LinkCameras        = ELinkCameras.None;
			LinkAxes           = ELinkAxes.None;
			FilterHistory      = new Pattern[0];
			UI                 = new UISettings();
			Scenes             = new SceneSettings[1] { new SceneSettings() };
			Connections        = new ConnectionSettings[0];

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}
		public override string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>Recently loaded files</summary>
		public string RecentFiles
		{
			get { return get<string>(nameof(RecentFiles)); }
			set { set(nameof(RecentFiles), value); }
		}

		/// <summary>Auto reload script sources when changes are detected</summary>
		public bool AutoRefresh
		{
			get { return get<bool>(nameof(AutoRefresh)); }
			set { set(nameof(AutoRefresh), value); }
		}

		/// <summary>True if the scene should auto range after loading files</summary>
		public bool ResetOnLoad
		{
			get { return get<bool>(nameof(ResetOnLoad)); }
			set { set(nameof(ResetOnLoad), value); }
		}

		/// <summary>The size of the focus point</summary>
		public float FocusPointSize
		{
			get { return get<float>(nameof(FocusPointSize)); }
			set { set(nameof(FocusPointSize), value); }
		}

		/// <summary>The size of the focus point</summary>
		public float OriginPointSize
		{
			get { return get<float>(nameof(OriginPointSize)); }
			set { set(nameof(OriginPointSize), value); }
		}

		/// <summary>True if the focus point should be visible</summary>
		public bool FocusPointVisible
		{
			get { return get<bool>(nameof(FocusPointVisible)); }
			set { set(nameof(FocusPointVisible), value); }
		}

		/// <summary>True if the origin point should be visible</summary>
		public bool OriginPointVisible
		{
			get { return get<bool>(nameof(OriginPointVisible)); }
			set { set(nameof(OriginPointVisible), value); }
		}

		/// <summary>Bitmask of the navigation actions that are applied to all scenes</summary>
		public ELinkCameras LinkCameras
		{
			get { return get<ELinkCameras>(nameof(LinkCameras)); }
			set { set(nameof(LinkCameras), value); }
		}

		/// <summary>Bitmask of linked grid axes in all scenes</summary>
		public ELinkAxes LinkAxes
		{
			get { return get<ELinkAxes>(nameof(LinkAxes)); }
			set { set(nameof(LinkAxes), value); }
		}

		/// <summary>The filter patterns used in the object manager</summary>
		public Pattern[] FilterHistory
		{
			get { return get<Pattern[]>(nameof(FilterHistory)); }
			set { set(nameof(FilterHistory), value); }
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get<UISettings>(nameof(UI)); }
			set
			{
				if (value == null) throw new ArgumentNullException($"Setting '{nameof(UI)}' cannot be null");
				set(nameof(UI), value);
			}
		}

		/// <summary>Style options for scenes</summary>
		public SceneSettings[] Scenes
		{
			get { return get<SceneSettings[]>(nameof(Scenes)); }
			set { set(nameof(Scenes), value); }
		}

		/// <summary>Connection settings</summary>
		public ConnectionSettings[] Connections
		{
			get { return get<ConnectionSettings[]>(nameof(Connections)); }
			set { set(nameof(Connections), value); }
		}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class CameraSettings :SettingsSet<CameraSettings>
	{
		public CameraSettings()
		{
			AlignAxis    = v4.Zero;
			ResetForward = -v4.ZAxis;
			ResetUp      = +v4.YAxis;
		}
		public CameraSettings(CameraSettings rhs)
		{
			AlignAxis    = rhs.AlignAxis;
			ResetForward = rhs.ResetForward;
			ResetUp      = rhs.ResetUp;
		}

		/// <summary>The camera align axis</summary>
		public v4 AlignAxis
		{
			get { return get<v4>(nameof(AlignAxis)); }
			set { set(nameof(AlignAxis), value); }
		}

		/// <summary>The reset camera forward direction</summary>
		public v4 ResetForward
		{
			get { return get<v4>(nameof(ResetForward)); }
			set { set(nameof(ResetForward), value); Debug.Assert(value != v4.Zero); }
		}

		/// <summary>The reset camera up direction</summary>
		public v4 ResetUp
		{
			get { return get<v4>(nameof(ResetUp)); }
			set { set(nameof(ResetUp), value); Debug.Assert(value != v4.Zero); }
		}

		private class TyConv :GenericTypeConverter<CameraSettings> {}
	}

	/// <summary>Settings for a scene</summary>
	[TypeConverter(typeof(TyConv))]
	public class SceneSettings :SettingsSet<SceneSettings>
	{
		public SceneSettings()
			:this(null)
		{}
		public SceneSettings(string name)
		{
			Name = name ?? string.Empty;
			ShowBBoxes = false;
			ShowSelectionBox = false;

			Camera = new CameraSettings();

			Options                  = new ChartControl.RdrOptions();
			Options.NavigationMode   = ChartControl.ENavMode.Scene3D;
			Options.AntiAliasing     = true;
			Options.LockAspect       = 1.0;
			Options.ShowAxes         = false;
			Options.ShowGridLines    = false;
			Options.BkColour         = Color_.FromArgb(0xFFB9D1EA);
			Options.ChartBkColour    = Color_.FromArgb(0xFF808080);
			Options.XAxis.GridColour = Color_.FromArgb(0xFF8C8C8C);
			Options.YAxis.GridColour = Color_.FromArgb(0xFF8C8C8C);

			var light = new View3d.Light(0x00000000, 0xFF808080, 0xFFFFFFFF, 1000, direction:new v4(-1,-1,-10, 0)) { CameraRelative = true };
			Light = light.ToXml(new XElement(nameof(View3d.Light)));
		}
		public SceneSettings(string name, SceneSettings rhs)
			:this(name)
		{
			ShowBBoxes = rhs.ShowBBoxes;
			ShowSelectionBox = rhs.ShowSelectionBox;
			Camera = new CameraSettings(rhs.Camera);
			Options = new ChartControl.RdrOptions(rhs.Options);
			Light = rhs.Light.As<View3d.Light>().ToXml(new XElement(nameof(View3d.Light)));
		}

		/// <summary>The name of the scene that this settings apply to</summary>
		public string Name
		{
			get { return get<string>(nameof(Name)); }
			set { set(nameof(Name), value); }
		}

		/// <summary>Show bounding boxes for objects</summary>
		public bool ShowBBoxes
		{
			get { return get<bool>(nameof(ShowBBoxes)); }
			set { set(nameof(ShowBBoxes), value); }
		}

		/// <summary>Show the selection box</summary>
		public bool ShowSelectionBox
		{
			get { return get<bool>(nameof(ShowSelectionBox)); }
			set { set(nameof(ShowSelectionBox), value); }
		}

		/// <summary>Camera settings</summary>
		public CameraSettings Camera
		{
			get { return get<CameraSettings>(nameof(Camera)); }
			set
			{
				if (value == null) throw new ArgumentNullException($"Setting '{nameof(Camera)}' cannot be null");
				set(nameof(Camera), value);
			}
		}

		/// <summary>Scene rendering options</summary>
		public ChartControl.RdrOptions Options
		{
			get { return get<ChartControl.RdrOptions>(nameof(Options)); }
			set { set(nameof(Options), value); }
		}

		/// <summary>Light settings</summary>
		public XElement Light
		{
			get { return get<XElement>(nameof(Light)); }
			set { set(nameof(Light), value); }
		}

		private class TyConv :GenericTypeConverter<SceneSettings> {}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class UISettings :SettingsSet<UISettings>
	{
		public UISettings()
		{
			ClearErrorLogOnReload     = true;
			ShowErrorLogOnNewMessages = true;
			UILayout                  = null;
			WindowPosition            = Rectangle.Empty;
			WindowMaximised           = false;
		}

		/// <summary>Clear the error log when source data is reloaded</summary>
		public bool ClearErrorLogOnReload
		{
			get { return get<bool>(nameof(ClearErrorLogOnReload)); }
			set { set(nameof(ClearErrorLogOnReload), value); }
		}

		/// <summary>Show the log window when new errors are added</summary>
		public bool ShowErrorLogOnNewMessages
		{
			get { return get<bool>(nameof(ShowErrorLogOnNewMessages)); }
			set { set(nameof(ShowErrorLogOnNewMessages), value); }
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get<XElement>(nameof(UILayout)); }
			set { set(nameof(UILayout), value); }
		}

		/// <summary>The last position on screen</summary>
		public Rectangle WindowPosition
		{
			get { return get<Rectangle>(nameof(WindowPosition)); }
			set { set(nameof(WindowPosition), value); }
		}
		public bool WindowMaximised
		{
			get { return get<bool>(nameof(WindowMaximised)); }
			set { set(nameof(WindowMaximised), value); }
		}

		private class TyConv :GenericTypeConverter<UISettings> {}
	}

	/// <summary>Connection from an external source</summary>
	[TypeConverter(typeof(TyConv))]
	public class ConnectionSettings
	{
		public ConnectionSettings()
		{
		}

		public string Name { get; set; }

		private class TyConv : GenericTypeConverter<ConnectionSettings> { }
	}
}
