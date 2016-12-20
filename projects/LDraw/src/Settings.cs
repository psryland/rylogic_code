using System;
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
			ShowBBoxes = false;

			var light = new View3d.Light(0x00000000, 0xFF808080, 0xFFFFFFFF, 1000, direction:new v4(-1,-1,-10, 0)) { CameraRelative = true };
			Light = light.ToXml(new XElement(nameof(View3d.Light)));

			Scene = new ChartControl.RdrOptions();
			Scene.AntiAliasing     = false;
			Scene.ChartBkColour    = Color.FromArgb(0x00,0x00,0x00);
			Scene.XAxis.GridColour = Color.FromArgb(0x20,0x20,0x20);
			Scene.YAxis.GridColour = Color.FromArgb(0x20,0x20,0x20);

			Camera = new CameraSettings();
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
			get { return get(x => x.ShowBBoxes); }
			set { set(x => x.ShowBBoxes, value); }
		}

		/// <summary>True if the scene should auto range after loading files</summary>
		public bool ResetOnLoad
		{
			get { return get(x => x.ResetOnLoad); }
			set { set(x => x.ResetOnLoad, value); }
		}

		/// <summary>Show bounding boxes for objects</summary>
		public bool ShowBBoxes
		{
			get { return get(x => x.ShowBBoxes); }
			set { set(x => x.ShowBBoxes, value); }
		}

		/// <summary>Light settings</summary>
		public XElement Light
		{
			get { return get(x => x.Light); }
			set { set(x => x.Light, value); }
		}

		/// <summary>Style options for charts</summary>
		public ChartControl.RdrOptions Scene
		{
			get { return get(x => x.Scene); }
			set { set(x => x.Scene, value); }
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
			AlignAxis    = v4.Zero;
			ResetForward = -v4.ZAxis;
			ResetUp      = +v4.YAxis;
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

		private class TyConv :GenericTypeConverter<CameraSettings> {}
	}

	/// <summary>Settings associated with a connection to Rex via RexLink</summary>
	[TypeConverter(typeof(TyConv))]
	public class UISettings :SettingsSet<UISettings>
	{
		public UISettings()
		{
			UILayout             = null;
			WindowPosition       = Rectangle.Empty;
			WindowMaximised      = false;
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
