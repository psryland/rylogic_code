using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace LDraw
{
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			RecentFiles = string.Empty;
			AutoRefresh = false;
			ResetOnLoad = true;
			LinkSceneCameras = false;
			Scenes = new SceneSettings[1] { new SceneSettings() };
			UI = new UISettings();

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath)
			:base(filepath)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>Recently loaded files</summary>
		public string RecentFiles
		{
			get { return get(x => x.RecentFiles); }
			set { set(x => x.RecentFiles, value); }
		}

		/// <summary>Auto reload script sources when changes are detected</summary>
		public bool AutoRefresh
		{
			get { return get(x => x.AutoRefresh); }
			set { set(x => x.AutoRefresh, value); }
		}

		/// <summary>True if the scene should auto range after loading files</summary>
		public bool ResetOnLoad
		{
			get { return get(x => x.ResetOnLoad); }
			set { set(x => x.ResetOnLoad, value); }
		}

		/// <summary>True if navigation actions are applied to all scenes</summary>
		public bool LinkSceneCameras
		{
			get { return get(x => x.LinkSceneCameras); }
			set { set(x => x.LinkSceneCameras, value); }
		}

		/// <summary>Style options for scenes</summary>
		public SceneSettings[] Scenes
		{
			get { return get(x => x.Scenes); }
			set { set(x => x.Scenes, value); }
		}

		/// <summary>UI settings</summary>
		public UISettings UI
		{
			get { return get(x => x.UI); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(UI)));
				set(x => x.UI, value);
			}
		}

		/// <summary>Settings version</summary>
		protected override string Version
		{
			get { return "v1.0"; }
		}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class CameraSettings :SettingsSet<CameraSettings>
	{
		public CameraSettings()
		{
			AlignAxis          = v4.Zero;
			ResetForward       = -v4.ZAxis;
			ResetUp            = +v4.YAxis;
			FocusPointSize     = 1.0f;
			OriginPointSize    = 1.0f;
			FocusPointVisible  = true;
			OriginPointVisible = false;
		}

		/// <summary>The camera align axis</summary>
		public v4 AlignAxis
		{
			get { return get(x => x.AlignAxis); }
			set { set(x => x.AlignAxis, value); }
		}

		/// <summary>The reset camera forward direction</summary>
		public v4 ResetForward
		{
			get { return get(x => x.ResetForward); }
			set { set(x => x.ResetForward, value); Debug.Assert(value != v4.Zero); }
		}

		/// <summary>The reset camera up direction</summary>
		public v4 ResetUp
		{
			get { return get(x => x.ResetUp); }
			set { set(x => x.ResetUp, value); Debug.Assert(value != v4.Zero); }
		}

		/// <summary>The size of the focus point</summary>
		public float FocusPointSize
		{
			get { return get(x => x.FocusPointSize); }
			set { set(x => x.FocusPointSize, value); }
		}

		/// <summary>The size of the focus point</summary>
		public float OriginPointSize
		{
			get { return get(x => x.OriginPointSize); }
			set { set(x => x.OriginPointSize, value); }
		}

		/// <summary>True if the focus point should be visible</summary>
		public bool FocusPointVisible
		{
			get { return get(x => x.FocusPointVisible); }
			set { set(x => x.FocusPointVisible, value); }
		}

		/// <summary>True if the origin point should be visible</summary>
		public bool OriginPointVisible
		{
			get { return get(x => x.OriginPointVisible); }
			set { set(x => x.OriginPointVisible, value); }
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

			Camera = new CameraSettings();

			Options                  = new ChartControl.RdrOptions();
			Options.AntiAliasing     = false;
			Options.BkColour         = Color_.FromArgb(0xFFB9D1EA);
			Options.ChartBkColour    = Color_.FromArgb(0xFF808080);
			Options.XAxis.GridColour = Color_.FromArgb(0xFF8C8C8C);
			Options.YAxis.GridColour = Color_.FromArgb(0xFF8C8C8C);
			Options.GridZOffset      = -0.001f;

			var light = new View3d.Light(0x00000000, 0xFF808080, 0xFFFFFFFF, 1000, direction:new v4(-1,-1,-10, 0)) { CameraRelative = true };
			Light = light.ToXml(new XElement(nameof(View3d.Light)));
		}

		/// <summary>The name of the scene that this settings apply to</summary>
		public string Name
		{
			get { return get(x => x.Name); }
			set { set(x => x.Name, value); }
		}

		/// <summary>Show bounding boxes for objects</summary>
		public bool ShowBBoxes
		{
			get { return get(x => x.ShowBBoxes); }
			set { set(x => x.ShowBBoxes, value); }
		}

		/// <summary>Camera settings</summary>
		public CameraSettings Camera
		{
			get { return get(x => x.Camera); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(Camera)));
				set(x => x.Camera, value);
			}
		}

		/// <summary>Scene rendering options</summary>
		public ChartControl.RdrOptions Options
		{
			get { return get(x => x.Options); }
			set { set(x => x.Options, value); }
		}

		/// <summary>Light settings</summary>
		public XElement Light
		{
			get { return get(x => x.Light); }
			set { set(x => x.Light, value); }
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
			get { return get(x => x.ClearErrorLogOnReload); }
			set { set(x => x.ClearErrorLogOnReload, value); }
		}

		/// <summary>Show the log window when new errors are added</summary>
		public bool ShowErrorLogOnNewMessages
		{
			get { return get(x => x.ShowErrorLogOnNewMessages); }
			set { set(x => x.ShowErrorLogOnNewMessages, value); }
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}

		/// <summary>The last position on screen</summary>
		public Rectangle WindowPosition
		{
			get { return get(x => x.WindowPosition); }
			set { set(x => x.WindowPosition, value); }
		}
		public bool WindowMaximised
		{
			get { return get(x => x.WindowMaximised); }
			set { set(x => x.WindowMaximised, value); }
		}

		private class TyConv :GenericTypeConverter<UISettings> {}
	}
}
